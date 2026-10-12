using System;

namespace AdaptOrDeity.Sim.Transport
{
    /// <summary>What lies beyond the field's edges.</summary>
    public enum FieldEdge
    {
        /// <summary>The far field: each species held at its far-field value just outside the edge.</summary>
        FarField,

        /// <summary>No flux through the edges (a closed box: the field's content is conserved).</summary>
        Closed,
    }

    /// <summary>How a <see cref="DiffusionField"/> steps.</summary>
    public enum DiffusionScheme
    {
        /// <summary>
        /// Locally one-dimensional backward Euler: L-stable and non-negative at any step, but at steps far beyond the
        /// explicit limit (dx²/4D) its splitting error (λ² Lx Ly c) shrinks the gradients near sources, even at steady state.
        /// </summary>
        LocallyOneDimensional,

        /// <summary>
        /// Douglas ADI in delta form: (1 − λLx)(1 − λLy)(c' − c) = λ(Lx + Ly)c + dt q. Unconditionally stable, and its
        /// steady state is the exact discrete one at any step; first order in time. Modes short in both directions decay
        /// over several steps (by (1 + λ²ax·ay)/((1 + λax)(1 + λay)) each) rather than at once.
        /// </summary>
        Douglas,
    }

    /// <summary>
    /// Species diffusing in a thin layer of water over the ground: a square grid of nx x ny cells of side
    /// <see cref="Spacing"/> and depth <see cref="Depth"/>, with concentrations at the cell centres [mol/m³].
    /// Each step is implicit, one row and then one column at a time (<see cref="DiffusionScheme"/>), so the field steps at
    /// the cells' exchange interval; it is first-order accurate in time. Each step records what crossed each
    /// edge face and, optionally, the faces of an inner rectangle (<see cref="SetInnerRegion"/>), so nested grids can
    /// exchange content exactly (<see cref="NestedField"/>).
    /// </summary>
    public sealed class DiffusionField
    {
        private readonly double[][] _c, _source, _edge, _edgeOut;
        private double[][] _innerOut;
        private readonly double[][][] _scratch;
        private readonly double[]?[] _delta;   // per species, the Douglas step's change
        private readonly double[] _lambda, _stepDt;   // per species, of the step under way   // per species: a, b, r, work (species step independently)
        private int _i0, _j0, _i1, _j1;
        private bool _hasInner;

        public DiffusionField(int nx, int ny, double spacing, double depth, double[] diffusivity, double[] farField,
            FieldEdge edge = FieldEdge.FarField)
        {
            if (nx < 1 || ny < 1)
                throw new ArgumentOutOfRangeException(nameof(nx), "the grid needs at least one cell each way");
            if (!(spacing > 0.0) || !(depth > 0.0))
                throw new ArgumentOutOfRangeException(nameof(spacing), "spacing and depth must be positive");
            if (diffusivity.Length != farField.Length)
                throw new ArgumentException("one diffusivity and one far-field value per species");
            Nx = nx;
            Ny = ny;
            Spacing = spacing;
            Depth = depth;
            Edge = edge;
            Diffusivity = (double[])diffusivity.Clone();
            FarField = (double[])farField.Clone();
            _c = new double[Species][];
            _source = new double[Species][];
            _edge = new double[Species][];
            _edgeOut = new double[Species][];
            _innerOut = new double[Species][];
            _scratch = new double[Species][][];
            _delta = new double[Species][];
            _lambda = new double[Species];
            _stepDt = new double[Species];
            int n = Math.Max(nx, ny);
            for (int s = 0; s < Species; s++)
            {
                _c[s] = new double[nx * ny];
                _source[s] = new double[nx * ny];
                _edge[s] = new double[2 * (nx + ny)];
                _edgeOut[s] = new double[2 * (nx + ny)];
                _innerOut[s] = Array.Empty<double>();
                Array.Fill(_c[s], FarField[s]);
                Array.Fill(_edge[s], FarField[s]);
                _scratch[s] = new[] { new double[n], new double[n], new double[n], new double[n] };
            }
        }

        public int Nx { get; }

        public int Ny { get; }

        public int Species => Diffusivity.Length;

        /// <summary>Side of a grid cell [m].</summary>
        public double Spacing { get; }

        /// <summary>Depth of the water layer [m]: a grid cell holds Spacing² x Depth of water.</summary>
        public double Depth { get; }

        /// <summary>Water in one grid cell [m³].</summary>
        public double CellVolume => Spacing * Spacing * Depth;

        public FieldEdge Edge { get; }

        /// <summary>How each step is taken (locally one-dimensional by default).</summary>
        public DiffusionScheme Scheme { get; set; } = DiffusionScheme.LocallyOneDimensional;

        /// <summary>Per species [m²/s].</summary>
        public double[] Diffusivity { get; }

        /// <summary>Per species, the concentration beyond the edges [mol/m³] (with <see cref="FieldEdge.FarField"/>).</summary>
        public double[] FarField { get; }

        /// <summary>Position of the grid's lower-left corner [m]; cell (i, j) is centred at origin + (i + ½, j + ½) x spacing.</summary>
        public double OriginX { get; set; }

        public double OriginY { get; set; }

        /// <summary>Concentration of species <paramref name="s"/> in grid cell (i, j) [mol/m³].</summary>
        public double this[int s, int i, int j]
        {
            get => _c[s][j * Nx + i];
            set => _c[s][j * Nx + i] = value;
        }

        /// <summary>Species <paramref name="s"/>'s concentrations, row by row (cell (i, j) at j·Nx + i).</summary>
        internal double[] Values(int s) => _c[s];

        /// <summary>What left through each edge face in the last step [mol] (negative: came in), in face order.</summary>
        public double[] EdgeOutflow(int s) => _edgeOut[s];

        /// <summary>
        /// Index of an edge face: the west edge's faces by row j, then the east's by row, the south's by column i, the
        /// north's by column (side 0-3: west, east, south, north).
        /// </summary>
        public int EdgeFace(int side, int k) => side switch
        {
            0 => k,
            1 => Ny + k,
            2 => 2 * Ny + k,
            _ => 2 * Ny + Nx + k,
        };

        /// <summary>
        /// Records, from the next step on, what crosses the faces of the rectangle of cells [i0, i1) x [j0, j1) (outwards
        /// positive, <see cref="InnerOutflow"/>). The rectangle must leave at least one cell to each edge.
        /// </summary>
        public void SetInnerRegion(int i0, int j0, int i1, int j1)
        {
            if (i0 < 1 || j0 < 1 || i1 > Nx - 1 || j1 > Ny - 1 || i1 <= i0 || j1 <= j0)
                throw new ArgumentOutOfRangeException(nameof(i0), "the inner region must lie inside the grid with a cell to spare");
            _i0 = i0;
            _j0 = j0;
            _i1 = i1;
            _j1 = j1;
            _hasInner = true;
            int faces = 2 * ((i1 - i0) + (j1 - j0));
            for (int s = 0; s < Species; s++)
                if (_innerOut[s].Length != faces)
                    _innerOut[s] = new double[faces];
        }

        /// <summary>
        /// What left the inner region through each of its faces in the last step [mol], in face order: west faces by row
        /// from j0, east by row, south by column from i0, north by column.
        /// </summary>
        public double[] InnerOutflow(int s) => _innerOut[s];

        /// <summary>Amount of species <paramref name="s"/> in the field [mol].</summary>
        public double Total(int s)
        {
            double sum = 0.0;
            foreach (double v in _c[s])
                sum += v;
            return sum * CellVolume;
        }

        /// <summary>
        /// Adds a source of species <paramref name="s"/> at (x, y) [mol/s] (negative: uptake) to the grid cell holding it,
        /// for the next step only. A point outside the field is ignored. Uptake must not exceed what the grid cell holds
        /// over the step (the caller limits it), or the concentration goes negative.
        /// </summary>
        public void AddSource(int s, double x, double y, double molPerSecond)
        {
            int i = (int)Math.Floor((x - OriginX) / Spacing), j = (int)Math.Floor((y - OriginY) / Spacing);
            if (i < 0 || j < 0 || i >= Nx || j >= Ny)
                return;
            _source[s][j * Nx + i] += molPerSecond / CellVolume;
        }

        /// <summary>
        /// Adds a source at (x, y) [mol/s] for the next step, shared between the four nearest cell centres with the
        /// weights <see cref="Sample"/> reads them with (the transpose of sampling). Weight that falls beyond an edge goes
        /// to the nearest edge cell, so the whole source enters the field.
        /// </summary>
        public void Deposit(int s, double x, double y, double molPerSecond)
        {
            double fx = (x - OriginX) / Spacing - 0.5, fy = (y - OriginY) / Spacing - 0.5;
            int i = (int)Math.Floor(fx), j = (int)Math.Floor(fy);
            double u = fx - i, v = fy - j, q = molPerSecond / CellVolume;
            double[] src = _source[s];
            src[Clamp(j, Ny) * Nx + Clamp(i, Nx)] += (1 - u) * (1 - v) * q;
            src[Clamp(j, Ny) * Nx + Clamp(i + 1, Nx)] += u * (1 - v) * q;
            src[Clamp(j + 1, Ny) * Nx + Clamp(i, Nx)] += (1 - u) * v * q;
            src[Clamp(j + 1, Ny) * Nx + Clamp(i + 1, Nx)] += u * v * q;
        }

        /// <summary>Adds a source to grid cell (i, j) [mol/s] for the next step.</summary>
        public void AddCellSource(int s, int i, int j, double molPerSecond) =>
            _source[s][j * Nx + i] += molPerSecond / CellVolume;

        private static int Clamp(int k, int n) => k < 0 ? 0 : k >= n ? n - 1 : k;

        /// <summary>Species <paramref name="s"/>'s sources for the next step, per cell [mol/(m³·s)].</summary>
        internal double[] Sources(int s) => _source[s];

        /// <summary>The concentration of species <paramref name="s"/> at (x, y), interpolated between cell centres.</summary>
        public double Sample(int s, double x, double y)
        {
            double fx = (x - OriginX) / Spacing - 0.5, fy = (y - OriginY) / Spacing - 0.5;
            int i = (int)Math.Floor(fx), j = (int)Math.Floor(fy);
            double u = fx - i, v = fy - j;
            return (1 - u) * (1 - v) * At(s, i, j) + u * (1 - v) * At(s, i + 1, j)
                   + (1 - u) * v * At(s, i, j + 1) + u * v * At(s, i + 1, j + 1);
        }

        /// <summary>
        /// How much less a unit source at (x, y) raises the value <see cref="Sample"/> reads there than a source at a cell
        /// centre raises its own cell, in units of the lattice Green's function (D d (sum of neighbours - 4 c) = -J):
        /// g(0) - sum_ij w_i v_j g(p_i - p_j), with v the source's shares (bilinear when <paramref name="spread"/>, as
        /// <see cref="Deposit"/>; else all in the cell holding the point, as <see cref="AddSource"/>) and w the sampling
        /// weights, using g(0) - g(1, 0) = 1/4 and g(0) - g(1, 1) = 1/pi. 0 at a cell centre, 0.205 at a corner with
        /// bilinear spreading. Edges are ignored (the source is assumed away from them).
        /// </summary>
        public double SelfCoupling(double x, double y, bool spread)
        {
            double fx = (x - OriginX) / Spacing - 0.5, fy = (y - OriginY) / Spacing - 0.5;
            double u = fx - Math.Floor(fx), v = fy - Math.Floor(fy);
            // sampling weights of the four cells (0,0), (1,0), (0,1), (1,1) around the point
            double a = (1 - u) * (1 - v), b = u * (1 - v), c = (1 - u) * v, d = u * v;
            const double adjacent = 0.25, diagonal = 1.0 / Math.PI;
            if (spread)
                return adjacent * 2.0 * (a * b + c * d + a * c + b * d) + diagonal * 2.0 * (a * d + b * c);
            // the source's cell among the four: offsets to the others are adjacent or diagonal
            int si = u >= 0.5 ? 1 : 0, sj = v >= 0.5 ? 1 : 0;
            double sum = 0.0;
            double[] w = { a, b, c, d };
            for (int k = 0; k < 4; k++)
            {
                int di = Math.Abs((k & 1) - si), dj = Math.Abs((k >> 1) - sj);
                sum += w[k] * (di + dj == 0 ? 0.0 : di + dj == 1 ? adjacent : diagonal);
            }
            return sum;
        }

        // A cell's value, with the edge's rule beyond the grid.
        private double At(int s, int i, int j)
        {
            if (i >= 0 && j >= 0 && i < Nx && j < Ny)
                return _c[s][j * Nx + i];
            if (Edge == FieldEdge.FarField)
                return FarField[s];
            int ci = Clamp(i, Nx), cj = Clamp(j, Ny);
            return _c[s][cj * Nx + ci];
        }

        /// <summary>
        /// Advances by <paramref name="dt"/> [s]: the sources added since the last step act over it, then every species
        /// diffuses (rows, then columns, each implicit). The sources are cleared.
        /// </summary>
        public void Step(double dt)
        {
            for (int s = 0; s < Species; s++)
                StepSpecies(s, dt);
        }

        /// <summary>
        /// Advances species <paramref name="s"/> alone by <paramref name="dt"/> [s] (see <see cref="Step"/>). Different
        /// species may step at once on different threads.
        /// </summary>
        public void StepSpecies(int s, double dt)
        {
            Prepare(s, dt);
            RowPass(s, 0, Ny);
            ColumnPass(s, 0, Nx);
        }

        /// <summary>
        /// The first part of a step of species <paramref name="s"/>: then <see cref="RowPass"/> over every row and
        /// <see cref="ColumnPass"/> over every column. The rows of one pass (and the columns of the other) are independent,
        /// so they may be shared between threads in any way with the same result.
        /// </summary>
        internal void Prepare(int s, double dt)
        {
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "the step must be positive");
            Array.Clear(_edgeOut[s], 0, _edgeOut[s].Length);
            Array.Clear(_innerOut[s], 0, _innerOut[s].Length);
            if (Edge == FieldEdge.FarField)
                Array.Fill(_edge[s], FarField[s]);
            double lambda = Diffusivity[s] * dt / (Spacing * Spacing);
            _stepDt[s] = dt;
            _lambda[s] = lambda;
            bool open = Edge != FieldEdge.Closed;
            var scratch = _scratch[s];
            Factor(Nx, lambda, open, scratch[0], scratch[1]);
            Factor(Ny, lambda, open, scratch[2], scratch[3]);
            if (Douglas(s))
                _delta[s] ??= new double[Nx * Ny];
        }

        // Douglas steps (1 − λLx)(1 − λLy) Δ = dt q + λ(Lx + Ly) c, then c += Δ; locally one-dimensional ones add the sources
        // and solve on c itself.
        private bool Douglas(int s) => Scheme == DiffusionScheme.Douglas && _lambda[s] != 0.0;

        /// <summary>
        /// Rows [j0, j1) of a step: the sources (and with Douglas the explicit part, whose face fluxes are recorded here),
        /// then the implicit solve along each row.
        /// </summary>
        internal void RowPass(int s, int j0, int j1)
        {
            double[] c = _c[s], q = _source[s], edge = _edge[s], edgeOut = _edgeOut[s], innerOut = _innerOut[s];
            double lambda = _lambda[s], dt = _stepDt[s];
            bool open = Edge != FieldEdge.Closed, douglas = Douglas(s);
            double flux = lambda * CellVolume;   // amount per unit concentration difference across a face
            int h = _j1 - _j0, w = _i1 - _i0;
            double[] v = douglas ? _delta[s]! : c;
            if (!douglas)
            {
                for (int k = j0 * Nx; k < j1 * Nx; k++)
                {
                    c[k] += dt * q[k];
                    q[k] = 0.0;
                }
                if (lambda == 0.0)
                    return;
            }
            else
            {
                for (int j = j0; j < j1; j++)
                {
                    int row = j * Nx;
                    if (j > 0 && j < Ny - 1)
                        for (int k = row + 1; k < row + Nx - 1; k++)
                        {
                            v[k] = dt * q[k] + lambda * (c[k - 1] + c[k + 1] + c[k - Nx] + c[k + Nx] - 4.0 * c[k]);
                            q[k] = 0.0;
                        }
                    bool edgeRow = j == 0 || j == Ny - 1;
                    for (int i = 0; i < Nx; i += edgeRow || i == Nx - 1 ? 1 : Nx - 1)
                    {
                        int k = row + i;
                        double x = c[k];
                        double west = i > 0 ? c[k - 1] : open ? edge[EdgeFace(0, j)] : x;
                        double east = i < Nx - 1 ? c[k + 1] : open ? edge[EdgeFace(1, j)] : x;
                        double south = j > 0 ? c[k - Nx] : open ? edge[EdgeFace(2, i)] : x;
                        double north = j < Ny - 1 ? c[k + Nx] : open ? edge[EdgeFace(3, i)] : x;
                        v[k] = dt * q[k] + lambda * (west + east + south + north - 4.0 * x);
                        q[k] = 0.0;
                    }
                    if (open)
                    {
                        edgeOut[EdgeFace(0, j)] += flux * (c[row] - edge[EdgeFace(0, j)]);
                        edgeOut[EdgeFace(1, j)] += flux * (c[row + Nx - 1] - edge[EdgeFace(1, j)]);
                        if (j == 0)
                            for (int i = 0; i < Nx; i++)
                                edgeOut[EdgeFace(2, i)] += flux * (c[i] - edge[EdgeFace(2, i)]);
                        if (j == Ny - 1)
                            for (int i = 0; i < Nx; i++)
                                edgeOut[EdgeFace(3, i)] += flux * (c[row + i] - edge[EdgeFace(3, i)]);
                    }
                    if (_hasInner)
                    {
                        if (j >= _j0 && j < _j1)
                        {
                            innerOut[j - _j0] += flux * (c[row + _i0] - c[row + _i0 - 1]);
                            innerOut[h + j - _j0] += flux * (c[row + _i1 - 1] - c[row + _i1]);
                        }
                        if (j == _j0)
                            for (int i = _i0; i < _i1; i++)
                                innerOut[2 * h + i - _i0] += flux * (c[row + i] - c[row - Nx + i]);
                        if (j == _j1 - 1)
                            for (int i = _i0; i < _i1; i++)
                                innerOut[2 * h + w + i - _i0] += flux * (c[row + i] - c[row + Nx + i]);
                    }
                }
            }
            // beyond an open edge: the given value, or zero for Douglas's change (the value's part is explicit above)
            var scratch = _scratch[s];
            double[] inv = scratch[0], wk = scratch[1];
            for (int j = j0; j < j1; j++)
            {
                int row = j * Nx;
                if (open)
                {
                    v[row] += lambda * (douglas ? 0.0 : edge[EdgeFace(0, j)]);
                    v[row + Nx - 1] += lambda * (douglas ? 0.0 : edge[EdgeFace(1, j)]);
                }
            }
            int j4 = j0 + (j1 - j0) / 4 * 4;
            for (int j = j0; j < j4; j += 4)   // four rows side by side: independent recurrences
            {
                int r0 = j * Nx, r1 = r0 + Nx, r2 = r1 + Nx, r3 = r2 + Nx;
                double f = inv[0];
                v[r0] *= f;
                v[r1] *= f;
                v[r2] *= f;
                v[r3] *= f;
                for (int k = 1; k < Nx; k++)
                {
                    f = inv[k];
                    v[r0 + k] = (v[r0 + k] + lambda * v[r0 + k - 1]) * f;
                    v[r1 + k] = (v[r1 + k] + lambda * v[r1 + k - 1]) * f;
                    v[r2 + k] = (v[r2 + k] + lambda * v[r2 + k - 1]) * f;
                    v[r3 + k] = (v[r3 + k] + lambda * v[r3 + k - 1]) * f;
                }
                for (int k = Nx - 2; k >= 0; k--)
                {
                    double g = wk[k];
                    v[r0 + k] -= g * v[r0 + k + 1];
                    v[r1 + k] -= g * v[r1 + k + 1];
                    v[r2 + k] -= g * v[r2 + k + 1];
                    v[r3 + k] -= g * v[r3 + k + 1];
                }
            }
            for (int j = j4; j < j1; j++)
            {
                int row = j * Nx;
                v[row] *= inv[0];
                for (int k = 1; k < Nx; k++)
                    v[row + k] = (v[row + k] + lambda * v[row + k - 1]) * inv[k];
                for (int k = Nx - 2; k >= 0; k--)
                    v[row + k] -= wk[k] * v[row + k + 1];
            }
            for (int j = j0; j < j1; j++)
            {
                int row = j * Nx;
                if (open)
                {
                    edgeOut[EdgeFace(0, j)] += flux * (v[row] - (douglas ? 0.0 : edge[EdgeFace(0, j)]));
                    edgeOut[EdgeFace(1, j)] += flux * (v[row + Nx - 1] - (douglas ? 0.0 : edge[EdgeFace(1, j)]));
                }
                if (_hasInner && j >= _j0 && j < _j1)
                {
                    innerOut[j - _j0] += flux * (v[row + _i0] - v[row + _i0 - 1]);
                    innerOut[h + j - _j0] += flux * (v[row + _i1 - 1] - v[row + _i1]);
                }
            }
        }

        /// <summary>
        /// Columns [i0, i1) of a step (after every row's <see cref="RowPass"/>): the implicit solve along each column, swept
        /// row by row across the columns; with Douglas the change is then added.
        /// </summary>
        internal void ColumnPass(int s, int i0, int i1)
        {
            double lambda = _lambda[s];
            if (lambda == 0.0)
                return;
            double[] c = _c[s], edge = _edge[s], edgeOut = _edgeOut[s], innerOut = _innerOut[s];
            bool open = Edge != FieldEdge.Closed, douglas = Douglas(s);
            double flux = lambda * CellVolume;
            int h = _j1 - _j0, w = _i1 - _i0, top = (Ny - 1) * Nx;
            double[] v = douglas ? _delta[s]! : c;
            var scratch = _scratch[s];
            double[] inv = scratch[2], wk = scratch[3];
            if (open)
                for (int i = i0; i < i1; i++)
                {
                    v[i] += lambda * (douglas ? 0.0 : edge[EdgeFace(2, i)]);
                    v[top + i] += lambda * (douglas ? 0.0 : edge[EdgeFace(3, i)]);
                }
            for (int i = i0; i < i1; i++)
                v[i] *= inv[0];
            for (int j = 1; j < Ny; j++)
            {
                int row = j * Nx, below = row - Nx;
                double f = inv[j];
                for (int i = i0; i < i1; i++)
                    v[row + i] = (v[row + i] + lambda * v[below + i]) * f;
            }
            for (int j = Ny - 2; j >= 0; j--)
            {
                int row = j * Nx, above = row + Nx;
                double g = wk[j];
                for (int i = i0; i < i1; i++)
                    v[row + i] -= g * v[above + i];
            }
            for (int i = i0; i < i1; i++)
            {
                if (open)
                {
                    edgeOut[EdgeFace(2, i)] += flux * (v[i] - (douglas ? 0.0 : edge[EdgeFace(2, i)]));
                    edgeOut[EdgeFace(3, i)] += flux * (v[top + i] - (douglas ? 0.0 : edge[EdgeFace(3, i)]));
                }
                if (_hasInner && i >= _i0 && i < _i1)
                {
                    innerOut[2 * h + i - _i0] += flux * (v[_j0 * Nx + i] - v[(_j0 - 1) * Nx + i]);
                    innerOut[2 * h + w + i - _i0] += flux * (v[(_j1 - 1) * Nx + i] - v[_j1 * Nx + i]);
                }
            }
            if (douglas)
                for (int j = 0; j < Ny; j++)
                {
                    int row = j * Nx;
                    for (int i = i0; i < i1; i++)
                        c[row + i] += v[row + i];
                }
        }

        // The Thomas algorithm's factors for (1 + 2λ) x_k − λ (x_{k−1} + x_{k+1}) = r_k along a line of n cells (a closed end
        // has one neighbour, an open one a value beyond it): solve by x_0 = r_0·inv_0, x_k = (r_k + λ x_{k−1})·inv_k, then
        // x_k −= w_k x_{k+1} backwards. The same for every line of a step.
        private static void Factor(int n, double lambda, bool open, double[] inv, double[] w)
        {
            for (int k = 0; k < n; k++)
            {
                int neighbours = open ? 2 : (k == 0 && k == n - 1) ? 0 : (k == 0 || k == n - 1) ? 1 : 2;
                double m = 1.0 + neighbours * lambda + (k > 0 ? lambda * w[k - 1] : 0.0);
                inv[k] = 1.0 / m;
                w[k] = -lambda * inv[k];
            }
        }

        /// <summary>
        /// Moves the grid by whole cells over the ground (it follows the creature): what stays in view keeps its values,
        /// cells coming into view take the far-field values.
        /// </summary>
        public void Shift(int di, int dj)
        {
            if (di == 0 && dj == 0)
                return;
            for (int s = 0; s < Species; s++)
            {
                var moved = new double[Nx * Ny];
                for (int j = 0; j < Ny; j++)
                    for (int i = 0; i < Nx; i++)
                    {
                        int oi = i + di, oj = j + dj;
                        moved[j * Nx + i] = oi >= 0 && oj >= 0 && oi < Nx && oj < Ny ? _c[s][oj * Nx + oi] : FarField[s];
                    }
                _c[s] = moved;
                Array.Clear(_source[s], 0, _source[s].Length);
            }
            OriginX += di * Spacing;
            OriginY += dj * Spacing;
        }
    }
}
