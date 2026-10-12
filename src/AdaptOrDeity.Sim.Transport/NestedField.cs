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

        /// <summary>The outermost grid spans at least this multiple of the colony's diameter.</summary>
        public double ExtentMultiple { get; set; } = 60.0;

        /// <summary>Depth of the water layer [m].</summary>
        public double Depth { get; set; } = 1e-5;

        /// <summary>Fewest cells along a side of each grid (a multiple of 4, at least 24 so each grid nests in the next).</summary>
        public int MinCells { get; set; } = 24;
    }

    /// <summary>
    /// Species around a colony on nested square grids (<see cref="DiffusionField"/> levels) that follow it. Level 0 is the
    /// finest, covering the colony and a margin; each further level has 4x the spacing and 4x the extent (the same number
    /// of cells, N per side), out to the extent multiple of the colony's diameter; beyond the outermost lies the far field
    /// (the epoch's ocean). Level k's corner sits on level k+1's grid lines, so every coarse cell under a finer grid (its
    /// shadow) holds exactly 4x4 fine cells.
    ///
    /// <para>A step (<see cref="Step"/>), coarsest level first: each level takes a Douglas step (species in parallel;
    /// <see cref="DiffusionScheme.Douglas"/>, whose steady state is exact at any step, where locally one-dimensional steps
    /// at the exchange interval halve the gradients near a cell). Sources act on their finest level and on the cell
    /// holding them on every coarser level. A finer level's edges are closed but for what the coarser level's step carried
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
    /// <para>The water is a thin layer of depth <see cref="NestedFieldSettings.Depth"/> (2D). A cell of radius a exchanging
    /// J with it sees C_grid + J/(4 pi D a) at its membrane (<see cref="AtMembrane"/>): the 3D concentration drop between
    /// the grid's resolution and the membrane. A correction for a layer of finite depth goes in
    /// <see cref="MembraneConductance"/>.</para>
    /// </summary>
    public sealed class NestedField
    {
        /// <summary>Spacing and extent ratio between successive levels.</summary>
        public const int Ratio = 4;

        private readonly NestedFieldSettings _settings;
        private readonly double[] _diffusivity, _far;
        private readonly FieldEdge _outerEdge;
        private DiffusionField[] _levels;
        private DiffusionField?[] _spare;
        private long[] _ox, _oy;   // each level's lower-left corner, in cells of its own spacing
        private readonly bool[] _active;
        private readonly double[] _ocean, _sourced, _pending;
        private bool _hasPending;
        private ParallelWork? _workers;
        private int[] _activeList;
        private int _activeCount;
        private Action<int, int>? _solvePass, _restrictPass;
        private double _dt;
        private int _stepLevel;

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
            _levels = Array.Empty<DiffusionField>();
            _spare = Array.Empty<DiffusionField?>();
            _ox = Array.Empty<long>();
            _oy = Array.Empty<long>();
            Size(diameter, out int n, out int levels);
            Rebuild(n, levels, cx, cy, null);
            Diameter = diameter;
            Array.Clear(_ocean, 0, _ocean.Length);   // the first fill is the starting state, not an exchange
        }

        public int Species => _diffusivity.Length;

        /// <summary>Number of levels; level 0 is the finest.</summary>
        public int Levels => _levels.Length;

        /// <summary>Cells along each side of every level.</summary>
        public int CellsPerSide { get; private set; }

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
        /// Rate [m³/s] at which diffusion carries species <paramref name="s"/> between the grid and the membrane of a sphere
        /// of radius <paramref name="radius"/> [m] per unit concentration difference: 4 pi D a (Berg and Purcell 1977).
        /// </summary>
        public double MembraneConductance(int s, double radius) => 4.0 * Math.PI * _diffusivity[s] * radius;

        /// <summary>
        /// Concentration at the membrane of a cell of radius <paramref name="radius"/> at (x, y) that releases
        /// <paramref name="release"/> [mol/s] of species <paramref name="s"/> (negative: takes up): C_grid + J/(4 pi D a).
        /// </summary>
        public double AtMembrane(int s, double x, double y, double radius, double release) =>
            Sample(s, x, y) + release / MembraneConductance(s, radius);

        /// <summary>
        /// Adds a source of species <paramref name="s"/> at (x, y) [mol/s] for the next step, on the finest level whose
        /// region holds it: shared bilinearly between four cells on the finest grid (weight beyond its edge on the edge
        /// cell), into the cell holding it on a coarser grid (a coarse cell lies wholly inside or outside a finer grid's
        /// shadow, so nothing lands where restriction would replace it). Returns false (and adds nothing) beyond the
        /// outermost level.
        /// </summary>
        public bool Deposit(int s, double x, double y, double molPerSecond)
        {
            int k = -1;
            for (int q = 0; q < _levels.Length && k < 0; q++)
            {
                var L = _levels[q];
                double fx = (x - L.OriginX) / L.Spacing, fy = (y - L.OriginY) / L.Spacing;
                if (fx >= 0.0 && fy >= 0.0 && fx < L.Nx && fy < L.Ny)
                    k = q;
            }
            if (k < 0)
                return false;
            if (molPerSecond == 0.0)
                return true;
            if (k == 0)
                _levels[0].Deposit(s, x, y, molPerSecond);
            else
                _levels[k].AddSource(s, x, y, molPerSecond);
            for (int p = k + 1; p < _levels.Length; p++)
                _levels[p].AddSource(s, x, y, molPerSecond);   // the coarser levels see it too; restriction replaces them
            _pending[s] += molPerSecond;
            _active[s] = true;
            _hasPending = true;
            return true;
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

        /// <summary>Grid sizes for a colony of this diameter: cells per side and number of levels.</summary>
        public void Size(double diameter, out int cells, out int levels)
        {
            double h = _settings.FineSpacing;
            cells = (int)Math.Ceiling((diameter + 2.0 * _settings.Margin) / (Ratio * h) - 1e-9) * Ratio;
            cells = Math.Max(_settings.MinCells, cells);
            levels = 1;
            double extent = cells * h;
            while (extent < _settings.ExtentMultiple * diameter * (1.0 - 1e-12))
            {
                extent *= Ratio;
                levels++;
            }
        }

        /// <summary>
        /// Fits the grids to the colony centred at (cx, cy) with this diameter [m]: regrids when it needs more cells per
        /// side or a different number of levels, and recentres each level that is more than one coarser cell off the
        /// colony. Call before adding the step's sources.
        /// </summary>
        public void Follow(double cx, double cy, double diameter)
        {
            Size(diameter, out int n, out int levels);
            if (n > CellsPerSide || levels > Levels)
            {
                Rebuild(Math.Max(n, CellsPerSide), Math.Max(levels, Levels), cx, cy, null);
                Diameter = diameter;
                return;
            }
            bool[]? move = null;
            for (int k = 0; k < Levels; k++)
            {
                double h = _levels[k].Spacing, half = 0.5 * CellsPerSide;
                if (Math.Abs(cx / h - (_ox[k] + half)) > Ratio || Math.Abs(cy / h - (_oy[k] + half)) > Ratio)
                {
                    move ??= new bool[Levels];
                    move[k] = true;
                }
            }
            if (move != null)
                Rebuild(CellsPerSide, Levels, cx, cy, move);
        }

        /// <summary>
        /// Advances every level by <paramref name="dt"/> [s] with the sources added since the last step (see the class
        /// summary), on <paramref name="workers"/> when given (results are the same for any number of workers).
        /// </summary>
        public void Step(double dt, ParallelWork? workers = null)
        {
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "the step must be positive");
            _dt = dt;
            _activeCount = 0;
            for (int s = 0; s < Species; s++)
            {
                _sourced[s] += _pending[s] * dt;
                _pending[s] = 0.0;
                if (_active[s])
                    _activeList[_activeCount++] = s;
            }
            _hasPending = false;
            _workers = workers;
            _solvePass ??= SolvePass;
            _restrictPass ??= RestrictPass;
            for (_stepLevel = Levels - 1; _stepLevel >= 0; _stepLevel--)
                Run(_activeCount, _solvePass);
            Run(_activeCount, _restrictPass);
            _workers = null;
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

        private void Run(int count, Action<int, int> pass)
        {
            if (_workers == null || _workers.Workers == 1 || count < 2)
                pass(0, count);
            else
                _workers.Run(count, 1, pass);
        }

        // Level _stepLevel steps each active species; a finer level takes in, through its edges, what the next coarser
        // level's step carried into its shadow.
        private void SolvePass(int start, int end)
        {
            int k = _stepLevel;
            for (int a = start; a < end; a++)
            {
                int s = _activeList[a];
                if (k + 1 < Levels)
                    BoundaryInflow(k, s);
                _levels[k].StepSpecies(s, _dt);
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

        // What the coarser level's step carried into level k's shadow through each coarse face enters level k through the
        // four fine edge cells along that face, shared equally, as a source over the step.
        private void BoundaryInflow(int k, int s)
        {
            DiffusionField F = _levels[k], P = _levels[k + 1];
            int m = CellsPerSide / Ratio, n = CellsPerSide;
            double[] coarse = P.InnerOutflow(s);
            double share = -1.0 / (Ratio * _dt);
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

        // Builds levels for n cells a side and this many levels around (cx, cy), filled from the current ones (all levels
        // when move is null, else only those marked; the rest keep their grids).
        private void Rebuild(int n, int levels, double cx, double cy, bool[]? move)
        {
            if (_hasPending)
                throw new InvalidOperationException("the grids move before the step's sources are added");
            var oldLevels = _levels;
            long[] oox = _ox, ooy = _oy;
            double[] before = new double[Species];
            for (int s = 0; s < Species && oldLevels.Length > 0; s++)
                before[s] = Total(s);
            bool sameShape = n == CellsPerSide && levels == oldLevels.Length;
            var next = new DiffusionField[levels];
            var ox = new long[levels];
            var oy = new long[levels];
            double h0 = _settings.FineSpacing;
            for (int k = 0; k < levels; k++)
            {
                bool rebuild = move == null || move[k];
                double h = h0 * Math.Pow(Ratio, k);
                if (!rebuild)
                {
                    next[k] = oldLevels[k];
                    ox[k] = oox[k];
                    oy[k] = ooy[k];
                    continue;
                }
                // the corner on the next coarser level's grid lines, nearest to centring the colony
                ox[k] = (long)Math.Round((cx / h - 0.5 * n) / Ratio) * Ratio;
                oy[k] = (long)Math.Round((cy / h - 0.5 * n) / Ratio) * Ratio;
                var edge = k == levels - 1 ? _outerEdge : FieldEdge.Closed;   // finer levels: inflow from the coarser one
                DiffusionField? L = sameShape ? _spare.Length > k ? _spare[k] : null : null;
                if (L == null || L.Edge != edge)
                    L = new DiffusionField(n, n, h, _settings.Depth, _diffusivity, _far, edge)
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
            var old = new Hierarchy(oldLevels, oox, ooy, _far);
            for (int k = 0; k < levels; k++)
            {
                if (!(move == null || move[k]))
                    continue;
                var L = next[k];
                for (int s = 0; s < Species; s++)
                {
                    double[] c = L.Values(s);
                    for (int j = 0; j < n; j++)
                        for (int i = 0; i < n; i++)
                            c[j * n + i] = old.Value(k, ox[k] + i, oy[k] + j, s);
                }
            }
            // the old grids of moved levels become the spares
            var spare = new DiffusionField?[levels];
            for (int k = 0; k < levels; k++)
                spare[k] = sameShape && move != null && move[k] ? oldLevels[k] : null;
            _spare = spare;
            _levels = next;
            _ox = ox;
            _oy = oy;
            CellsPerSide = n;
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
            private readonly DiffusionField[] _levels;
            private readonly long[] _ox, _oy;
            private readonly double[] _far;

            public Hierarchy(DiffusionField[] levels, long[] ox, long[] oy, double[] far)
            {
                _levels = levels;
                _ox = ox;
                _oy = oy;
                _far = far;
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
