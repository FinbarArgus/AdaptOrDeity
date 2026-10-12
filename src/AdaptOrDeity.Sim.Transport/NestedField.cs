using System;
using AdaptOrDeity.Sim.Core;

namespace AdaptOrDeity.Sim.Transport
{
    /// <summary>The geometry of a <see cref="NestedField"/>. Every value is SI.</summary>
    public sealed class NestedFieldSettings
    {
        /// <summary>Side of a cell of the finest grid [m].</summary>
        public double FineSpacing { get; set; } = 1e-6;

        /// <summary>Water around the colony the finest grid covers on each side [m].</summary>
        public double Margin { get; set; } = 8e-6;

        /// <summary>
        /// The outermost grid spans this multiple of the colony's diameter, rounded up to a whole number of its own cells.
        /// </summary>
        public double ExtentMultiple { get; set; } = 60.0;

        /// <summary>
        /// Distance from the colony's centre the outermost grid covers in every direction, its edge cells and recentring
        /// slack aside [m]: where things outside the colony (free organisms) still exchange with the field. 0: only the
        /// extent multiple.
        /// </summary>
        public double Reach { get; set; }

        /// <summary>Depth of the water layer [m].</summary>
        public double Depth { get; set; } = 1e-5;

        /// <summary>Fewest cells along a side of each grid (a multiple of 4, at least 24 so each grid nests in the next).</summary>
        public int MinCells { get; set; } = 24;

        /// <summary>
        /// One grid at the finest spacing over the nested grids' whole extent instead of nested grids: the reference the
        /// nested grids are compared with (costly beyond small colonies).
        /// </summary>
        public bool SingleGrid { get; set; }

        /// <summary>Steps of the field per call of <see cref="NestedField.Step"/>, sources held over them (1 in the game).</summary>
        public int Substeps { get; set; } = 1;

        /// <summary>
        /// Steps the finest level takes per step of the others, sources and the coarse inflow held over them: its short
        /// modes (a moving cell's own near field) decay over a few Douglas steps, so a cell drifting at 2 um/s would see
        /// ~7% less of its own depletion with one.
        /// </summary>
        public int FineSubsteps { get; set; } = 1;
    }

    /// <summary>
    /// Species around a colony on nested square grids (<see cref="DiffusionField"/> levels) that follow it. Level 0 is the
    /// finest, covering the colony and a margin; each further level has 4x the spacing and 4x the extent (the same number
    /// of cells, N per side), except the outermost, whose own number of cells (between N/2 and 2N) makes it span the extent
    /// multiple of the colony's diameter; beyond it lies the far field (the epoch's ocean). Level k's corner sits on level
    /// k+1's grid lines, so every coarse cell under a finer grid (its shadow) holds exactly 4x4 fine cells.
    ///
    /// <para>A step (<see cref="Step"/>), coarsest level first: each level takes a Douglas step (species in parallel;
    /// <see cref="DiffusionScheme.Douglas"/>, whose steady state is exact at any step, where locally one-dimensional steps
    /// at the exchange interval halve the gradients near a cell; each level's rows, then columns, shared between threads).
    /// Sources act on their finest level and, summed over each shadow cell, on every coarser level. A finer level's edges are closed but for what the coarser level's step carried
    /// into its shadow through each coarse face, which enters the finer level through the edge cells along that face. Then,
    /// finest first, each shadow takes the mean of the finer cells over it (restriction). So the finer grid's boundary flux
    /// is the coarse one, and the content over all levels (each region counted once at its finest level: the outermost
    /// level's total) changes only by the sources and what crosses the outermost edges, to rounding. The coarse level
    /// resolves the flux at its own spacing, so the finest grid's margin keeps the colony a few coarse cells inside it.</para>
    ///
    /// <para>The grids recentre by whole cells of the next coarser level as the colony moves, and regrid as it grows (the
    /// finest grid's extent tracks the colony and margin, the outermost the extent multiple); both refill each level from
    /// the old levels (copying where a level covered the cell, else a mean-preserving limited-linear prolongation from the
    /// next coarser level), conserving content to rounding apart from water that enters or leaves at the outermost edge
    /// (counted in <see cref="OceanInflow"/>).</para>
    ///
    /// <para>The water is a thin layer of depth d (<see cref="NestedFieldSettings.Depth"/>); the grids hold its depth
    /// average, which is exactly 2D. A cell of radius a (a &lt;&lt; d, at mid-depth) exchanging J sees the 3D field near it
    /// (<see cref="AtMembrane"/>, <see cref="MembraneResistance"/>): C_grid + J [1/(4 pi D a) - ln(r*/r_eff)/(2 pi D d)],
    /// the thin layer's analytic near field (r* = 2 d e^-gamma) less the part of the 2D field the grid already resolves
    /// around its own source (r_eff, from the lattice Green's function at the cell's place in its grid cell).</para>
    /// </summary>
    public sealed class NestedField
    {
        /// <summary>Spacing and extent ratio between successive levels.</summary>
        public const int Ratio = 4;

        private readonly NestedFieldSettings _settings;
        private readonly double[] _diffusivity, _far;
        private readonly FieldEdge _outerEdge;
        private DiffusionField[] _levels, _nextLevels = Array.Empty<DiffusionField>();
        private long[] _nextOx = Array.Empty<long>(), _nextOy = Array.Empty<long>();
        private bool[] _move = Array.Empty<bool>();
        private readonly double[] _before;
        private readonly Hierarchy _hierarchy;
        private DiffusionField?[] _spare;
        private long[] _ox, _oy;   // each level's lower-left corner, in cells of its own spacing
        private readonly bool[] _active;
        private readonly double[] _ocean, _sourced, _pending;
        private bool _hasPending;
        private ParallelWork? _workers;
        private int[] _activeList;
        private int _activeCount;
        private Action<int, int>? _rowPass, _columnPass, _restrictPass;
        private double _lastDt;
        private readonly int[] _stride, _waited;
        private readonly double[] _speciesDt;
        private readonly int[] _fineSubsteps, _passList;
        private int _passCount;
        private int _stepLevel, _chunks, _stepCells;

        /// <summary>Rows (or columns) per work item when a level's lines are shared between threads.</summary>
        private const int LinesPerItem = 32;

        /// <summary>Levels narrower than this many cells step on one thread (sharing them costs more than it saves).</summary>
        private const int ParallelFromCells = 40;

        /// <summary>
        /// Grids for a colony of diameter <paramref name="diameter"/> [m] centred at (<paramref name="cx"/>,
        /// <paramref name="cy"/>), every species at its far-field value; beyond the outermost grid the far field, or a
        /// closed edge (for tests).
        /// </summary>
        public NestedField(NestedFieldSettings settings, double[] diffusivity, double[] farField, double cx, double cy,
            double diameter, FieldEdge outerEdge = FieldEdge.FarField)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (diffusivity.Length != farField.Length)
                throw new ArgumentException("one diffusivity and one far-field value per species");
            if (settings.MinCells < 24 || settings.MinCells % Ratio != 0)
                throw new ArgumentOutOfRangeException(nameof(settings), "at least 24 cells a side, a multiple of 4");
            _diffusivity = (double[])diffusivity.Clone();
            _far = (double[])farField.Clone();
            _outerEdge = outerEdge;
            _active = new bool[Species];
            Array.Fill(_active, true);
            _ocean = new double[Species];
            _sourced = new double[Species];
            _pending = new double[Species];
            _activeList = new int[Species];
            _before = new double[Species];
            _stride = new int[Species];
            Array.Fill(_stride, 1);
            _waited = new int[Species];
            _speciesDt = new double[Species];
            _fineSubsteps = new int[Species];
            _passList = new int[Species];
            _hierarchy = new Hierarchy(_far);
            _levels = Array.Empty<DiffusionField>();
            _spare = Array.Empty<DiffusionField?>();
            _ox = Array.Empty<long>();
            _oy = Array.Empty<long>();
            Layout(diameter, 0, 0, out int n, out int levels, out int outer);
            Rebuild(n, levels, outer, cx, cy, null);
            Diameter = diameter;
            Array.Clear(_ocean, 0, _ocean.Length);   // the first fill is the starting state, not an exchange
        }

        public int Species => _diffusivity.Length;

        /// <summary>Number of levels; level 0 is the finest.</summary>
        public int Levels => _levels.Length;

        /// <summary>Cells along each side of every level but the outermost.</summary>
        public int CellsPerSide { get; private set; }

        /// <summary>Cells along each side of the outermost level.</summary>
        public int OuterCells { get; private set; }

        /// <summary>The colony diameter the grids were last fitted to [m].</summary>
        public double Diameter { get; private set; }

        public DiffusionField Level(int k) => _levels[k];

        /// <summary>Per species [m²/s].</summary>
        public double Diffusivity(int s) => _diffusivity[s];

        public double FarField(int s) => _far[s];

        /// <summary>
        /// Whether species <paramref name="s"/> is stepped. A species that is uniform at its far-field value with no
        /// sources may be marked idle (false); a source makes it active again.
        /// </summary>
        public bool IsActive(int s) => _active[s];

        public void SetActive(int s, bool active) => _active[s] = active;

        /// <summary>Amount of species <paramref name="s"/> over all levels, each region counted at its finest level [mol].</summary>
        public double Total(int s) => _levels[_levels.Length - 1].Total(s);

        /// <summary>
        /// What has entered the field from beyond the outermost edge since it was made [mol]: diffusion through the edges,
        /// water coming into view as the grids move or grow, and changes of the sea (<see cref="SetUniform"/>).
        /// </summary>
        public double OceanInflow(int s) => _ocean[s];

        /// <summary>What sources have put in since the field was made [mol] (negative: taken out).</summary>
        public double Sourced(int s) => _sourced[s];

        /// <summary>The finest level whose interior (one cell in from its edges) holds (x, y), or -1 if none does.</summary>
        public int LevelAt(double x, double y)
        {
            for (int k = 0; k < _levels.Length; k++)
            {
                var L = _levels[k];
                double fx = (x - L.OriginX) / L.Spacing, fy = (y - L.OriginY) / L.Spacing;
                if (fx >= 1.0 && fy >= 1.0 && fx <= L.Nx - 1.0 && fy <= L.Ny - 1.0)
                    return k;
            }
            return -1;
        }

        /// <summary>Concentration of species <paramref name="s"/> at (x, y) on the finest level there; the far field beyond.</summary>
        public double Sample(int s, double x, double y)
        {
            int k = LevelAt(x, y);
            return k < 0 ? _far[s] : _levels[k].Sample(s, x, y);
        }

        /// <summary>
        /// Concentration difference between the membrane of a sphere of radius <paramref name="radius"/> [m] at (x, y) and
        /// the grid's value there, per unit of its release of species <paramref name="s"/> [s/m³]: the thin layer's near
        /// field, 1/(4 pi D a) + (ln(rho/(2 d)) + gamma)/(2 pi D d) at rho = a (a point source at mid-depth between the layer's
        /// closed floor and top, whose images give the 2D field ln rho/(2 pi D d) beyond ~d), less what the grid resolves of
        /// the 2D field around the source it is given: the field at r_eff = h e^-gamma 2^-3/2 e^(2 pi delta), with delta from
        /// the lattice Green's function for the source's spreading and sampling at its place in the grid cell
        /// (<see cref="DiffusionField.SelfCoupling"/>; delta 0 at a cell centre: Peaceman's 0.2 h). So
        /// 1/(4 pi D a) - (ln(4 sqrt(2) d / h) - 2 pi delta)/(2 pi D d), the same wherever the cell sits on any grid. Valid
        /// for a well below d and h; beyond the outermost level, 1/(4 pi D a).
        /// </summary>
        public double MembraneResistance(int s, double x, double y, double radius)
        {
            double D = _diffusivity[s], threeD = 1.0 / (4.0 * Math.PI * D * radius);
            int k = LevelAt(x, y);
            if (k < 0)
                return threeD;
            var L = _levels[k];
            double delta = L.SelfCoupling(x, y, DepositLevel(x, y) == 0 && k == 0);
            return threeD - (Math.Log(4.0 * Math.Sqrt(2.0) * _settings.Depth / L.Spacing) - 2.0 * Math.PI * delta)
                / (2.0 * Math.PI * D * _settings.Depth);
        }

        /// <summary>
        /// Concentration at the membrane of a cell of radius <paramref name="radius"/> at (x, y) that releases
        /// <paramref name="release"/> [mol/s] of species <paramref name="s"/> (negative: takes up): the grid's value plus
        /// release x <see cref="MembraneResistance"/>.
        /// </summary>
        public double AtMembrane(int s, double x, double y, double radius, double release) =>
            release == 0.0 ? Sample(s, x, y) : Sample(s, x, y) + release * MembraneResistance(s, x, y, radius);

        /// <summary>
        /// Adds a source of species <paramref name="s"/> at (x, y) [mol/s] for the next step, on the finest level whose
        /// region holds it: shared bilinearly between four cells on the finest grid (weight beyond its edge on the edge
        /// cell), into the cell holding it on a coarser grid (a coarse cell lies wholly inside or outside a finer grid's
        /// shadow, so nothing lands where restriction would replace it). Returns false (and adds nothing) beyond the
        /// outermost level.
        /// </summary>
        public bool Deposit(int s, double x, double y, double molPerSecond)
        {
            int k = DepositLevel(x, y);
            if (k < 0)
                return false;
            if (molPerSecond == 0.0)
                return true;
            if (k == 0)
                _levels[0].Deposit(s, x, y, molPerSecond);
            else
                _levels[k].AddSource(s, x, y, molPerSecond);
            _pending[s] += molPerSecond;
            _active[s] = true;
            _hasPending = true;
            return true;
        }

        // The finest level whose region holds (x, y), or -1 beyond the outermost.
        private int DepositLevel(double x, double y)
        {
            for (int q = 0; q < _levels.Length; q++)
            {
                var L = _levels[q];
                double fx = (x - L.OriginX) / L.Spacing, fy = (y - L.OriginY) / L.Spacing;
                if (fx >= 0.0 && fy >= 0.0 && fx < L.Nx && fy < L.Ny)
                    return q;
            }
            return -1;
        }

        /// <summary>Sets species <paramref name="s"/> to <paramref name="value"/> everywhere and beyond (a new sea).</summary>
        public void SetUniform(int s, double value)
        {
            double before = Total(s);
            _far[s] = value;
            foreach (var L in _levels)
            {
                L.FarField[s] = value;
                Array.Fill(L.Values(s), value);
            }
            _ocean[s] += Total(s) - before;
            _active[s] = true;
        }

        /// <summary>
        /// Makes every shadow the mean of the finer cells over it, finest first: after values were set on the levels
        /// directly, so the levels agree.
        /// </summary>
        public void Restrict()
        {
            for (int k = 0; k + 1 < Levels; k++)
                for (int s = 0; s < Species; s++)
                    Restrict(k, s);
        }

        /// <summary>
        /// Grid sizes for a colony of this diameter: cells per side of every level but the outermost, the number of levels,
        /// and the outermost level's cells per side.
        /// </summary>
        public void Size(double diameter, out int cells, out int levels, out int outerCells) =>
            Layout(diameter, 0, 0, out cells, out levels, out outerCells);

        // The sizes for this diameter with at least atLeastCells per side and atLeastLevels levels (grids never shrink).
        // Nested: N covers the colony and its margins; the levels are the fewest whose outermost, of spacing h 4^(L-1),
        // spans the extent (and the reach) in at most 2N cells; the outermost has that many cells, rounded up (and at least N/4 + 8, so the
        // next finer level's shadow keeps a cell to its edges as both recentre). A single grid covers the nested grids'
        // extent at the finest spacing.
        private void Layout(double diameter, int atLeastCells, int atLeastLevels, out int cells, out int levels,
            out int outerCells)
        {
            double h = _settings.FineSpacing;
            int n = (int)Math.Ceiling((diameter + 2.0 * _settings.Margin) / (Ratio * h) - 1e-9) * Ratio;
            n = Math.Max(_settings.MinCells, n);
            int L = 1, outer = n;
            if (n < OuterFor(diameter, h, 1))
            {
                L = 2;
                while (OuterFor(diameter, h, L) > 2 * n)
                    L++;
            }
            if (_settings.SingleGrid)
            {
                if (L > 1)
                    n = Math.Max(n / Ratio + 8, OuterFor(diameter, h, L)) * (int)Math.Pow(Ratio, L - 1);
                cells = outerCells = Math.Max(n, atLeastCells);
                levels = 1;
                return;
            }
            n = Math.Max(n, atLeastCells);
            L = Math.Max(L, atLeastLevels);
            if (L > 1)
                outer = Math.Max(n / Ratio + 8, OuterFor(diameter, h, L));
            cells = n;
            levels = L;
            outerCells = L > 1 ? outer : n;
        }

        // Cells of the outermost of L levels that span the extent multiple of the diameter, and the reach each way with two
        // of its cells to spare (its edge cell, and recentring by one cell).
        private int OuterFor(double diameter, double h, int levels)
        {
            double hOuter = h * Math.Pow(Ratio, levels - 1);
            double extent = Math.Max(_settings.ExtentMultiple * diameter,
                _settings.Reach > 0.0 ? 2.0 * (_settings.Reach + 2.0 * hOuter) : 0.0);
            return (int)Math.Ceiling(extent / hOuter - 1e-9);
        }

        /// <summary>
        /// Fits the grids to the colony centred at (cx, cy) with this diameter [m]: regrids when it needs more cells per
        /// side or a different number of levels, and recentres each level that is more than one coarser cell off the
        /// colony. Call before adding the step's sources.
        /// </summary>
        public void Follow(double cx, double cy, double diameter)
        {
            Layout(diameter, CellsPerSide, Levels, out int n, out int levels, out int outer);
            if (n > CellsPerSide || levels > Levels || outer > OuterCells)
            {
                CatchUp();
                Rebuild(n, levels, levels > Levels || n > CellsPerSide ? outer : Math.Max(outer, OuterCells), cx, cy, null);
                Diameter = diameter;
                return;
            }
            if (_move.Length != Levels)
                _move = new bool[Levels];
            bool any = false;
            for (int k = 0; k < Levels; k++)
            {
                // a level recentres by whole cells of the next coarser level; the outermost by its own cells
                var L = _levels[k];
                double h = L.Spacing, half = 0.5 * L.Nx, step = k == Levels - 1 ? 1.0 : Ratio;
                _move[k] = Math.Abs(cx / h - (_ox[k] + half)) > step || Math.Abs(cy / h - (_oy[k] + half)) > step;
                any |= _move[k];
            }
            if (any)
            {
                CatchUp();
                Rebuild(CellsPerSide, Levels, OuterCells, cx, cy, _move);
            }
        }

        /// <summary>
        /// Advances every level by <paramref name="dt"/> [s] with the sources added since the last step (see the class
        /// summary), on <paramref name="workers"/> when given (results are the same for any number of workers).
        /// </summary>
        public void Step(double dt, ParallelWork? workers = null)
        {
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "the step must be positive");
            _lastDt = dt;
            _activeCount = 0;
            for (int s = 0; s < Species; s++)
            {
                _sourced[s] += _pending[s] * dt;
                _pending[s] = 0.0;
                if (!_active[s])
                    continue;
                // a species with a stride steps every stride-th call, over the calls since its last step
                if (++_waited[s] >= _stride[s])
                    _activeList[_activeCount++] = s;
            }
            _hasPending = false;
            Advance(dt, workers);
        }

        /// <summary>
        /// Steps species <paramref name="s"/> once every <paramref name="stride"/> calls of <see cref="Step"/>, over the
        /// time since its last step, with the sources added meanwhile (their mean rate): for species nothing reads every
        /// step (smells). Exact in content; the calls must have the same step.
        /// </summary>
        public void SetStride(int s, int stride)
        {
            if (stride < 1)
                throw new ArgumentOutOfRangeException(nameof(stride), "at least 1");
            _stride[s] = stride;
        }

        // Steps the listed species (_activeList[0.._activeCount)) over the calls each has waited: sources held as their mean
        // rate (and over the substeps), coarse first.
        private void Advance(double dt, ParallelWork? workers)
        {
            int substeps = Math.Max(1, _settings.Substeps);
            for (int a = 0; a < _activeCount; a++)
            {
                int s = _activeList[a];
                if (_waited[s] > 1)
                    for (int k = 0; k < Levels; k++)
                    {
                        double[] q = _levels[k].Sources(s);
                        double mean = 1.0 / _waited[s];
                        for (int i = 0; i < q.Length; i++)
                            q[i] *= mean;
                    }
                _speciesDt[s] = dt * _waited[s] / substeps;
                _waited[s] = 0;
            }
            _workers = workers;
            _rowPass ??= RowPass;
            _columnPass ??= ColumnPass;
            _restrictPass ??= RestrictPass;
            // the coarser levels see the finer levels' sources in their shadows (restriction replaces the result there)
            for (int a = 0; a < _activeCount; a++)
                for (int k = 0; k + 1 < Levels; k++)
                    RestrictSources(k, _activeList[a]);
            // with substeps, every substep takes the same sources (held as rates over the call)
            double[][][]? held = substeps > 1 ? HoldSources() : null;
            for (int sub = 0; sub < substeps; sub++)
            {
                if (sub > 0)
                    RestoreSources(held!);
                for (_stepLevel = Levels - 1; _stepLevel >= 0; _stepLevel--)
                {
                    _stepCells = _levels[_stepLevel].Nx;
                    _chunks = (_stepCells + LinesPerItem - 1) / LinesPerItem;
                    for (int a = 0; a < _activeCount; a++)
                        if (_stepLevel + 1 < Levels)
                            BoundaryInflow(_stepLevel, _activeList[a]);
                    if (!(_stepLevel == 0 && Levels > 1))
                    {
                        // a coarser level (or a single grid): every species one step
                        for (int a = 0; a < _activeCount; a++)
                        {
                            _levels[_stepLevel].Prepare(_activeList[a], _speciesDt[_activeList[a]]);
                            _passList[a] = _activeList[a];
                        }
                        _passCount = _activeCount;
                        Run(_passCount * _chunks, _rowPass);
                        Run(_passCount * _chunks, _columnPass);
                        continue;
                    }
                    // the finest level: each species its own number of substeps, its sources held over them
                    int fineMost = 1;
                    if (_stepLevel == 0 && Levels > 1)
                        for (int a = 0; a < _activeCount; a++)
                            fineMost = Math.Max(fineMost, FineSubstepsOf(_activeList[a]));
                    double[][]? heldFine = fineMost > 1 ? HoldLevel(0) : null;
                    for (int f = 0; f < fineMost; f++)
                    {
                        _passCount = 0;
                        for (int a = 0; a < _activeCount; a++)
                        {
                            int s = _activeList[a], fine = fineMost > 1 ? FineSubstepsOf(s) : 1;
                            if (f >= fine)
                                continue;
                            if (f > 0)
                                Array.Copy(heldFine![a], _levels[0].Sources(s), heldFine[a].Length);
                            _levels[_stepLevel].Prepare(s, _speciesDt[s] / fine);
                            _passList[_passCount++] = s;
                        }
                        Run(_passCount * _chunks, _rowPass);
                        Run(_passCount * _chunks, _columnPass);
                    }
                }
                Run(_activeCount, _restrictPass);
                var outer = _levels[Levels - 1];
                for (int a = 0; a < _activeCount; a++)
                {
                    int s = _activeList[a];
                    double out_ = 0.0;
                    foreach (double f in outer.EdgeOutflow(s))
                        out_ += f;
                    _ocean[s] -= out_;
                }
            }
            _workers = null;
        }

        /// <summary>
        /// Steps the finest level takes for species <paramref name="s"/> per step of the others (default
        /// <see cref="NestedFieldSettings.FineSubsteps"/>).
        /// </summary>
        public void SetFineSubsteps(int s, int substeps)
        {
            if (substeps < 1)
                throw new ArgumentOutOfRangeException(nameof(substeps), "at least 1");
            _fineSubsteps[s] = substeps;
        }

        private int FineSubstepsOf(int s) => _fineSubsteps[s] > 0 ? _fineSubsteps[s] : Math.Max(1, _settings.FineSubsteps);

        /// <summary>
        /// Steps every species that has waited since its last step (a stride), over the time since: then every source added
        /// is in the field. The grids do this before they move.
        /// </summary>
        public void CatchUp()
        {
            _activeCount = 0;
            for (int s = 0; s < Species; s++)
                if (_active[s] && _waited[s] > 0)
                    _activeList[_activeCount++] = s;
            if (_activeCount > 0)
                Advance(_lastDt, null);
        }

        // Copies of the active species' sources on every level, before the first substep uses them.
        private double[][][] HoldSources()
        {
            var held = new double[Levels][][];
            for (int k = 0; k < Levels; k++)
            {
                held[k] = new double[_activeCount][];
                for (int a = 0; a < _activeCount; a++)
                    held[k][a] = (double[])_levels[k].Sources(_activeList[a]).Clone();
            }
            return held;
        }

        // Level k's sources of the active species (with the coarse inflow), before its first fine substep uses them; into
        // buffers kept between steps (stepping does not allocate once warm).
        private double[][] HoldLevel(int k)
        {
            if (_heldFine.Length < Species)
                _heldFine = new double[Species][];
            for (int a = 0; a < _activeCount; a++)
            {
                var src = _levels[k].Sources(_activeList[a]);
                if (_heldFine[a] == null || _heldFine[a].Length != src.Length)
                    _heldFine[a] = new double[src.Length];
                Array.Copy(src, _heldFine[a], src.Length);
            }
            return _heldFine;
        }

        private double[][] _heldFine = Array.Empty<double[]>();

        private void RestoreSources(double[][][] held)
        {
            for (int k = 0; k < Levels; k++)
                for (int a = 0; a < _activeCount; a++)
                    Array.Copy(held[k][a], _levels[k].Sources(_activeList[a]), held[k][a].Length);
        }

        // A pass over count items, on the workers when the level is large enough to repay sharing it.
        private void Run(int count, Action<int, int> pass)
        {
            if (_workers == null || _workers.Workers == 1 || count < 2 || _stepCells < ParallelFromCells)
                pass(0, count);
            else
                _workers.Run(count, 1, pass);
        }

        // Level _stepLevel's rows, then its columns, by (species, block of lines) items.
        private void RowPass(int start, int end)
        {
            for (int item = start; item < end; item++)
            {
                int j0 = item % _chunks * LinesPerItem;
                _levels[_stepLevel].RowPass(_passList[item / _chunks], j0, Math.Min(_stepCells, j0 + LinesPerItem));
            }
        }

        private void ColumnPass(int start, int end)
        {
            for (int item = start; item < end; item++)
            {
                int i0 = item % _chunks * LinesPerItem;
                _levels[_stepLevel].ColumnPass(_passList[item / _chunks], i0, Math.Min(_stepCells, i0 + LinesPerItem));
            }
        }

        // Level k's sources added into the cells of its shadow on level k+1 (the same amount per second).
        private void RestrictSources(int k, int s)
        {
            DiffusionField F = _levels[k], P = _levels[k + 1];
            Shadow(k, out int i0, out int j0, out int m);
            double[] fine = F.Sources(s), coarse = P.Sources(s);
            int fnx = F.Nx, nx = P.Nx;
            const double share = 1.0 / (Ratio * Ratio);
            for (int J = 0; J < m; J++)
                for (int I = 0; I < m; I++)
                {
                    double sum = 0.0;
                    for (int b = 0; b < Ratio; b++)
                    {
                        int row = (Ratio * J + b) * fnx + Ratio * I;
                        for (int a = 0; a < Ratio; a++)
                            sum += fine[row + a];
                    }
                    if (sum != 0.0)
                        coarse[(j0 + J) * nx + i0 + I] += sum * share;
                }
        }

        // What the coarser level's step carried into level k's shadow through each coarse face enters level k through the
        // four fine edge cells along that face, shared equally, as a source over the step.
        private void BoundaryInflow(int k, int s)
        {
            DiffusionField F = _levels[k], P = _levels[k + 1];
            int m = CellsPerSide / Ratio, n = CellsPerSide;
            double[] coarse = P.InnerOutflow(s);
            double share = -1.0 / (Ratio * _speciesDt[s]);
            for (int f = 0; f < m; f++)
                for (int r = 0; r < Ratio; r++)
                {
                    int t = Ratio * f + r;
                    F.AddCellSource(s, 0, t, coarse[f] * share);
                    F.AddCellSource(s, n - 1, t, coarse[m + f] * share);
                    F.AddCellSource(s, t, 0, coarse[2 * m + f] * share);
                    F.AddCellSource(s, t, n - 1, coarse[3 * m + f] * share);
                }
        }

        // Per active species, finest first: each shadow takes the mean of the finer cells over it.
        private void RestrictPass(int start, int end)
        {
            for (int a = start; a < end; a++)
            {
                int s = _activeList[a];
                for (int k = 0; k + 1 < Levels; k++)
                    Restrict(k, s);
            }
        }

        // The shadow of level k on level k+1: its first cell and its side in level k+1's cells.
        private void Shadow(int k, out int i0, out int j0, out int side)
        {
            i0 = (int)(_ox[k] / Ratio - _ox[k + 1]);
            j0 = (int)(_oy[k] / Ratio - _oy[k + 1]);
            side = CellsPerSide / Ratio;
        }

        // Each cell of level k's shadow on level k+1 takes the mean of the 4x4 fine cells over it.
        private void Restrict(int k, int s)
        {
            DiffusionField F = _levels[k], P = _levels[k + 1];
            Shadow(k, out int i0, out int j0, out int m);
            double[] fine = F.Values(s), c = P.Values(s);
            int fnx = F.Nx, nx = P.Nx;
            const double share = 1.0 / (Ratio * Ratio);
            for (int J = 0; J < m; J++)
                for (int I = 0; I < m; I++)
                {
                    double sum = 0.0;
                    for (int b = 0; b < Ratio; b++)
                    {
                        int row = (Ratio * J + b) * fnx + Ratio * I;
                        for (int a = 0; a < Ratio; a++)
                            sum += fine[row + a];
                    }
                    c[(j0 + J) * nx + i0 + I] = sum * share;
                }
        }

        // Builds levels for n cells a side (outer for the outermost) and this many levels around (cx, cy), filled from the
        // current ones (all levels when move is null, else only those marked; the rest keep their grids).
        private void Rebuild(int n, int levels, int outer, double cx, double cy, bool[]? move)
        {
            if (_hasPending)
                throw new InvalidOperationException("the grids move before the step's sources are added");
            var oldLevels = _levels;
            long[] oox = _ox, ooy = _oy;
            double[] before = _before;
            for (int s = 0; s < Species && oldLevels.Length > 0; s++)
                before[s] = Total(s);
            bool sameShape = n == CellsPerSide && levels == oldLevels.Length && outer == OuterCells;
            // recentring keeps the shape: the other buffers of the pair take the new levels (stepping does not allocate)
            bool reuse = sameShape && move != null && _nextLevels.Length == levels;
            var next = reuse ? _nextLevels : new DiffusionField[levels];
            var ox = reuse ? _nextOx : new long[levels];
            var oy = reuse ? _nextOy : new long[levels];
            double h0 = _settings.FineSpacing;
            for (int k = 0; k < levels; k++)
            {
                bool rebuild = move == null || move[k];
                double h = h0 * Math.Pow(Ratio, k);
                int nk = k == levels - 1 ? outer : n;
                if (!rebuild)
                {
                    next[k] = oldLevels[k];
                    ox[k] = oox[k];
                    oy[k] = ooy[k];
                    continue;
                }
                // the corner on the next coarser level's grid lines (the outermost's on its own), nearest to centring the colony
                int align = k == levels - 1 ? 1 : Ratio;
                ox[k] = (long)Math.Round((cx / h - 0.5 * nk) / align) * align;
                oy[k] = (long)Math.Round((cy / h - 0.5 * nk) / align) * align;
                var edge = k == levels - 1 ? _outerEdge : FieldEdge.Closed;   // finer levels: inflow from the coarser one
                DiffusionField? L = sameShape ? _spare.Length > k ? _spare[k] : null : null;
                if (L == null || L.Edge != edge || L.Nx != nk)
                    L = new DiffusionField(nk, nk, h, _settings.Depth, _diffusivity, _far, edge)
                    {
                        Scheme = DiffusionScheme.Douglas,   // the right steady state near sources at the exchange step
                    };
                for (int s = 0; s < Species; s++)
                    L.FarField[s] = _far[s];
                L.OriginX = ox[k] * h;
                L.OriginY = oy[k] * h;
                next[k] = L;
            }
            // fill every rebuilt level from the old levels before any of them is replaced
            var old = _hierarchy;
            old.Set(oldLevels, oox, ooy);
            for (int k = 0; k < levels; k++)
            {
                if (!(move == null || move[k]))
                    continue;
                var L = next[k];
                int nk = L.Nx;
                for (int s = 0; s < Species; s++)
                {
                    double[] c = L.Values(s);
                    for (int j = 0; j < nk; j++)
                        for (int i = 0; i < nk; i++)
                            c[j * nk + i] = old.Value(k, ox[k] + i, oy[k] + j, s);
                }
            }
            // the old grids of moved levels become their spares (the others keep theirs)
            if (!(sameShape && move != null) || _spare.Length != levels)
                _spare = new DiffusionField?[levels];
            for (int k = 0; k < levels && sameShape && move != null; k++)
                if (move[k])
                    _spare[k] = oldLevels[k];
            _nextLevels = oldLevels.Length == levels ? oldLevels : Array.Empty<DiffusionField>();
            _nextOx = oox;
            _nextOy = ooy;
            _levels = next;
            _ox = ox;
            _oy = oy;
            CellsPerSide = n;
            OuterCells = outer;
            for (int k = 0; k + 1 < levels; k++)
            {
                Shadow(k, out int i0, out int j0, out int m);
                _levels[k + 1].SetInnerRegion(i0, j0, i0 + m, j0 + m);
            }
            Restrict();
            for (int s = 0; s < Species && oldLevels.Length > 0; s++)
                _ocean[s] += Total(s) - before[s];
        }

        // The old levels, read as a composite: the mean of species s over a cell of any level's lattice.
        private sealed class Hierarchy
        {
            private DiffusionField[] _levels = Array.Empty<DiffusionField>();
            private long[] _ox = Array.Empty<long>(), _oy = Array.Empty<long>();
            private readonly double[] _far;

            public Hierarchy(double[] far) => _far = far;

            public void Set(DiffusionField[] levels, long[] ox, long[] oy)
            {
                _levels = levels;
                _ox = ox;
                _oy = oy;
            }

            public double Value(int k, long gi, long gj, int s)
            {
                int levels = _levels.Length;
                if (levels == 0)
                    return _far[s];
                if (k < levels)
                {
                    var L = _levels[k];
                    long li = gi - _ox[k], lj = gj - _oy[k];
                    if (li >= 0 && lj >= 0 && li < L.Nx && lj < L.Ny)
                        return L[s, (int)li, (int)lj];
                    if (k + 1 < levels)
                        return Prolong(k + 1, FloorDiv(gi), FloorDiv(gj), (int)(gi - Ratio * FloorDiv(gi)),
                            (int)(gj - Ratio * FloorDiv(gj)), s);
                    return _far[s];
                }
                // coarser than every old level: the far field, or the mean of the 16 finer cells where it overlaps them
                var outer = _levels[levels - 1];
                long scale = 1;
                for (int q = levels - 1; q < k; q++)
                    scale *= Ratio;
                long lo = gi * scale - _ox[levels - 1], hi = lo + scale, loJ = gj * scale - _oy[levels - 1], hiJ = loJ + scale;
                if (hi <= 0 || lo >= outer.Nx || hiJ <= 0 || loJ >= outer.Ny)
                    return _far[s];
                double sum = 0.0;
                for (int b = 0; b < Ratio; b++)
                    for (int a = 0; a < Ratio; a++)
                        sum += Value(k - 1, Ratio * gi + a, Ratio * gj + b, s);
                return sum / (Ratio * Ratio);
            }

            // The fine cell (a, b) of coarse cell (pi, pj) on level p: the coarse value plus minmod-limited slopes, whose
            // contributions cancel over the 4x4 fine cells (the mean is kept).
            private double Prolong(int p, long pi, long pj, int a, int b, int s)
            {
                double v = Value(p, pi, pj, s);
                double sx = MinMod(v - Value(p, pi - 1, pj, s), Value(p, pi + 1, pj, s) - v);
                double sy = MinMod(v - Value(p, pi, pj - 1, s), Value(p, pi, pj + 1, s) - v);
                return v + sx * ((a - 1.5) / Ratio) + sy * ((b - 1.5) / Ratio);
            }

            private static double MinMod(double l, double r) =>
                l * r <= 0.0 ? 0.0 : Math.Abs(l) < Math.Abs(r) ? l : r;

            private static long FloorDiv(long g) => g >= 0 ? g / Ratio : -((-g + Ratio - 1) / Ratio);
        }
    }
}
