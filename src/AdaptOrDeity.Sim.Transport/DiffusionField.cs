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

    /// <summary>
    /// Species diffusing in a thin layer of water over the ground: a square grid of nx x ny cells of side
    /// <see cref="Spacing"/> and depth <see cref="Depth"/>, with concentrations at the cell centres [mol/m³].
    /// Each step is locally one-dimensional backward Euler: sources added, then an implicit diffusion solve along every
    /// row and then every column. It is stable and keeps concentrations non-negative at any step length, so the field
    /// steps at the cells' exchange interval; it is first-order accurate in time.
    /// </summary>
    public sealed class DiffusionField
    {
        private readonly double[][] _c, _source;
        private readonly double[] _a, _b, _r, _work;

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
            for (int s = 0; s < Species; s++)
            {
                _c[s] = new double[nx * ny];
                _source[s] = new double[nx * ny];
                Array.Fill(_c[s], FarField[s]);
            }
            int n = Math.Max(nx, ny);
            _a = new double[n];
            _b = new double[n];
            _r = new double[n];
            _work = new double[n];
        }

        public int Nx { get; }

        public int Ny { get; }

        public int Species => Diffusivity.Length;

        /// <summary>Side of a grid cell [m].</summary>
        public double Spacing { get; }

        /// <summary>Depth of the water layer [m]: a grid cell holds Spacing² x Depth of water.</summary>
        public double Depth { get; }

        public FieldEdge Edge { get; }

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

        /// <summary>Amount of species <paramref name="s"/> in the field [mol].</summary>
        public double Total(int s)
        {
            double sum = 0.0;
            foreach (double v in _c[s])
                sum += v;
            return sum * Spacing * Spacing * Depth;
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
            _source[s][j * Nx + i] += molPerSecond / (Spacing * Spacing * Depth);
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
            return _c[s][Math.Min(Ny - 1, Math.Max(0, j)) * Nx + Math.Min(Nx - 1, Math.Max(0, i))];
        }

        /// <summary>
        /// Advances by <paramref name="dt"/> [s]: the sources added since the last step act over it, then every species
        /// diffuses (rows, then columns, each implicit). The sources are cleared.
        /// </summary>
        public void Step(double dt)
        {
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "the step must be positive");
            for (int s = 0; s < Species; s++)
            {
                double[] c = _c[s], q = _source[s];
                for (int k = 0; k < c.Length; k++)
                {
                    c[k] += dt * q[k];
                    q[k] = 0.0;
                }
                double lambda = Diffusivity[s] * dt / (Spacing * Spacing);
                if (lambda == 0.0)
                    continue;
                for (int j = 0; j < Ny; j++)
                    SolveLine(c, j * Nx, 1, Nx, lambda, FarField[s]);
                for (int i = 0; i < Nx; i++)
                    SolveLine(c, i, Nx, Ny, lambda, FarField[s]);
            }
        }

        // (1 + 2λ) c_k − λ (c_{k−1} + c_{k+1}) = c_k(old) along one line, by the Thomas algorithm. Beyond a far-field edge
        // the neighbour is the far-field value; a closed edge has no neighbour (no flux).
        private void SolveLine(double[] c, int start, int stride, int n, double lambda, double far)
        {
            bool open = Edge == FieldEdge.FarField;
            for (int k = 0; k < n; k++)
            {
                bool first = k == 0, last = k == n - 1;
                int neighbours = (first && last) ? 0 : (first || last) ? 1 : 2;
                if (open)
                    neighbours = 2;
                _b[k] = 1.0 + neighbours * lambda;
                _a[k] = -lambda;
                _r[k] = c[start + k * stride];
                if (open && first)
                    _r[k] += lambda * far;
                if (open && last)
                    _r[k] += lambda * far;
            }
            // forward sweep (the matrix is symmetric: sub- and super-diagonals both −λ)
            _work[0] = _a[0] / _b[0];
            _r[0] /= _b[0];
            for (int k = 1; k < n; k++)
            {
                double m = _b[k] - _a[k] * _work[k - 1];
                _work[k] = _a[k] / m;
                _r[k] = (_r[k] - _a[k] * _r[k - 1]) / m;
            }
            for (int k = n - 2; k >= 0; k--)
                _r[k] -= _work[k] * _r[k + 1];
            for (int k = 0; k < n; k++)
                c[start + k * stride] = _r[k];
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
