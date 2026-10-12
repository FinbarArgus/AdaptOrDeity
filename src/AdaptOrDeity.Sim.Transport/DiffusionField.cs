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
        private readonly double[]?[] _delta;   // per species, the Douglas step's change   // per species: a, b, r, work (species step independently)
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

        /// <summary>True if the sources added since the last step hold anything for species <paramref name="s"/>.</summary>
        internal bool HasSources(int s)
        {
            foreach (double q in _source[s])
                if (q != 0.0)
                    return true;
            return false;
        }

        /// <summary>The concentration of species <paramref name="s"/> at (x, y), interpolated between cell centres.</summary>
        public double Sample(int s, double x, double y)
        {
            double fx = (x - OriginX) / Spacing - 0.5, fy = (y - OriginY) / Spacing - 0.5;
            int i = (int)Math.Floor(fx), j = (int)Math.Floor(fy);
            double u = fx - i, v = fy - j;
            return (1 - u) * (1 - v) * At(s, i, j) + u * (1 - v) * At(s, i + 1, j)
                   + (1 - u) * v * At(s, i, j + 1) + u * v * At(s, i + 1, j + 1);
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
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "the step must be positive");
            double[] c = _c[s], q = _source[s], edge = _edge[s], edgeOut = _edgeOut[s], innerOut = _innerOut[s];
            Array.Clear(edgeOut, 0, edgeOut.Length);
            Array.Clear(innerOut, 0, innerOut.Length);
            if (Edge == FieldEdge.FarField)
                Array.Fill(edge, FarField[s]);
            double lambda = Diffusivity[s] * dt / (Spacing * Spacing);
            if (Scheme == DiffusionScheme.LocallyOneDimensional || lambda == 0.0)
            {
                for (int k = 0; k < c.Length; k++)
                {
                    c[k] += dt * q[k];
                    q[k] = 0.0;
                }
                if (lambda != 0.0)
                    Sweeps(s, c, lambda, false);
                return;
            }
            // Douglas: (1 - λLx)(1 - λLy) Δ = dt q + λ(Lx + Ly) c, then c += Δ. The explicit part's face fluxes are recorded
            // here, the sweeps' (with zero beyond open edges: Δ of a given value is in the explicit part) in Sweeps.
            double[] d = _delta[s] ??= new double[Nx * Ny];
            bool open = Edge != FieldEdge.Closed;
            double flux = lambda * CellVolume;
            int h = _j1 - _j0, w = _i1 - _i0;
            for (int j = 0; j < Ny; j++)
                for (int i = 0; i < Nx; i++)
                {
                    int k = j * Nx + i;
                    double v = c[k];
                    double west = i > 0 ? c[k - 1] : open ? edge[EdgeFace(0, j)] : v;
                    double east = i < Nx - 1 ? c[k + 1] : open ? edge[EdgeFace(1, j)] : v;
                    double south = j > 0 ? c[k - Nx] : open ? edge[EdgeFace(2, i)] : v;
                    double north = j < Ny - 1 ? c[k + Nx] : open ? edge[EdgeFace(3, i)] : v;
                    d[k] = dt * q[k] + lambda * (west + east + south + north - 4.0 * v);
                    q[k] = 0.0;
                }
            if (open)
            {
                for (int j = 0; j < Ny; j++)
                {
                    edgeOut[EdgeFace(0, j)] += flux * (c[j * Nx] - edge[EdgeFace(0, j)]);
                    edgeOut[EdgeFace(1, j)] += flux * (c[j * Nx + Nx - 1] - edge[EdgeFace(1, j)]);
                }
                for (int i = 0; i < Nx; i++)
                {
                    edgeOut[EdgeFace(2, i)] += flux * (c[i] - edge[EdgeFace(2, i)]);
                    edgeOut[EdgeFace(3, i)] += flux * (c[(Ny - 1) * Nx + i] - edge[EdgeFace(3, i)]);
                }
            }
            if (_hasInner)
                InnerFluxes(c, innerOut, flux, h, w, true, true);
            Sweeps(s, d, lambda, true);
            for (int k = 0; k < c.Length; k++)
                c[k] += d[k];
        }

        // Implicit solves along every row, then every column, of v; records what crosses the edge and inner faces. With
        // homogeneous set, the value beyond an open edge is zero (v is a change), else the given or far-field value.
        private void Sweeps(int s, double[] v, double lambda, bool homogeneous)
        {
            double[] edge = _edge[s], edgeOut = _edgeOut[s], innerOut = _innerOut[s];
            bool open = Edge != FieldEdge.Closed;
            double flux = lambda * CellVolume;   // amount per unit concentration difference across a face
            var scratch = _scratch[s];
            int h = _j1 - _j0, w = _i1 - _i0;
            for (int j = 0; j < Ny; j++)
            {
                int row = j * Nx;
                double west = homogeneous ? 0.0 : edge[EdgeFace(0, j)], east = homogeneous ? 0.0 : edge[EdgeFace(1, j)];
                SolveLine(v, row, 1, Nx, lambda, open, west, east, scratch);
                if (open)
                {
                    edgeOut[EdgeFace(0, j)] += flux * (v[row] - west);
                    edgeOut[EdgeFace(1, j)] += flux * (v[row + Nx - 1] - east);
                }
            }
            if (_hasInner)
                InnerFluxes(v, innerOut, flux, h, w, true, false);
            for (int i = 0; i < Nx; i++)
            {
                double south = homogeneous ? 0.0 : edge[EdgeFace(2, i)], north = homogeneous ? 0.0 : edge[EdgeFace(3, i)];
                SolveLine(v, i, Nx, Ny, lambda, open, south, north, scratch);
                if (open)
                {
                    edgeOut[EdgeFace(2, i)] += flux * (v[i] - south);
                    edgeOut[EdgeFace(3, i)] += flux * (v[(Ny - 1) * Nx + i] - north);
                }
            }
            if (_hasInner)
                InnerFluxes(v, innerOut, flux, h, w, false, true);
        }

        // What crosses the inner region's west and east faces (rows) and south and north faces (columns) for values v.
        private void InnerFluxes(double[] v, double[] innerOut, double flux, int h, int w, bool rows, bool columns)
        {
            if (rows)
                for (int j = _j0; j < _j1; j++)
                {
                    int row = j * Nx;
                    innerOut[j - _j0] += flux * (v[row + _i0] - v[row + _i0 - 1]);
                    innerOut[h + j - _j0] += flux * (v[row + _i1 - 1] - v[row + _i1]);
                }
            if (columns)
                for (int i = _i0; i < _i1; i++)
                {
                    innerOut[2 * h + i - _i0] += flux * (v[_j0 * Nx + i] - v[(_j0 - 1) * Nx + i]);
                    innerOut[2 * h + w + i - _i0] += flux * (v[(_j1 - 1) * Nx + i] - v[_j1 * Nx + i]);
                }
        }

        // (1 + 2λ) c_k − λ (c_{k−1} + c_{k+1}) = c_k(old) along one line, by the Thomas algorithm. Beyond an open edge the
        // neighbour is the given value (first, last); a closed edge has no neighbour (no flux).
        private static void SolveLine(double[] c, int start, int stride, int n, double lambda, bool open, double first,
            double last, double[][] scratch)
        {
            double[] a = scratch[0], b = scratch[1], r = scratch[2], work = scratch[3];
            for (int k = 0; k < n; k++)
            {
                bool isFirst = k == 0, isLast = k == n - 1;
                int neighbours = open ? 2 : (isFirst && isLast) ? 0 : (isFirst || isLast) ? 1 : 2;
                b[k] = 1.0 + neighbours * lambda;
                a[k] = -lambda;
                r[k] = c[start + k * stride];
                if (open && isFirst)
                    r[k] += lambda * first;
                if (open && isLast)
                    r[k] += lambda * last;
            }
            // forward sweep (the matrix is symmetric: sub- and super-diagonals both −λ)
            work[0] = a[0] / b[0];
            r[0] /= b[0];
            for (int k = 1; k < n; k++)
            {
                double m = b[k] - a[k] * work[k - 1];
                work[k] = a[k] / m;
                r[k] = (r[k] - a[k] * r[k - 1]) / m;
            }
            for (int k = n - 2; k >= 0; k--)
                r[k] -= work[k] * r[k + 1];
            for (int k = 0; k < n; k++)
                c[start + k * stride] = r[k];
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
