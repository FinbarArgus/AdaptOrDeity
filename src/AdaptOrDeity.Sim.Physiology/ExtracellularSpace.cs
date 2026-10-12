using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>
    /// The water between the cells of a colony as finite volumes, each an instance of the upstream module
    /// transport/extracellular_volume (glucose, lactate, H+ and O2, and the matrix the cells make), joined by faces with
    /// the upstream transport/extracellular_face law: G = A D f_a f_b / (d_a f_b + d_b f_a), each half slowed by its
    /// volume's matrix (f_hind, Mackie and Meares). A macro step is split in two:
    /// <list type="number">
    /// <item>Transport, here: diffusion between volumes and through open faces to the water outside, with the cells'
    /// exchange, by backward Euler for each species (stable at any step; the gaps relax in milliseconds). The cells'
    /// glucose uptake is an implicit sink (a coefficient times the volume's new concentration), so a volume is never
    /// overdrawn; their releases are sources. The new concentrations are rebuilt from the fluxes of the solved state, so
    /// every face moves exactly what it takes from one side to the other.</item>
    /// <item>Chemistry, in each volume's module, every <see cref="ChemistryInterval"/>: the matrix the cells delivered,
    /// its breakdown (part released as glucose) and its overflow above the densest gel (counted in the module's
    /// N_mx_lost).</item>
    /// </list>
    /// Values are SI (mol/m³ = mM, m³, mol/s). Volumes are added as the colony grows and never removed.
    /// </summary>
    public sealed class ExtracellularSpace : IDisposable
    {
        /// <summary>Species carried by the volumes and faces, in this order.</summary>
        public static readonly string[] Species = { "glc", "lac", "H", "O2" };

        public const int Glucose = 0, Lactate = 1, Proton = 2, Oxygen = 3;

        private readonly NativeModule _module;
        private readonly List<NativeInstance> _native = new List<NativeInstance>();
        private readonly List<KernelBatch> _batch = new List<KernelBatch>();
        private readonly int[] _sC = new int[Species.Length];
        private readonly int _sMatrix, _sLost, _eVolume, _eMatrixIn, _aHindrance;
        private readonly double _relativeTolerance;
        private Face[] _faces = Array.Empty<Face>();
        private OpenFace[] _open = Array.Empty<OpenFace>();

        // per volume (grown with the volumes)
        private double[] _capacity = Array.Empty<double>();   // [species * n + v]: V, or V (1 + b_H) for H+
        private double[] _old = Array.Empty<double>(), _x = Array.Empty<double>(), _r = Array.Empty<double>(),
            _p = Array.Empty<double>(), _q = Array.Empty<double>(), _diag = Array.Empty<double>(), _rhs = Array.Empty<double>();

        /// <summary>A face between volumes A and B: area [m²] and each centre's distance to it [m].</summary>
        public readonly struct Face
        {
            public Face(int a, int b, double area, double distanceA, double distanceB)
            {
                A = a;
                B = b;
                Area = area;
                DistanceA = distanceA;
                DistanceB = distanceB;
            }

            public int A { get; }
            public int B { get; }
            public double Area { get; }
            public double DistanceA { get; }
            public double DistanceB { get; }
        }

        /// <summary>A face from a volume to the water outside (open water at its mouth: no matrix, no distance).</summary>
        public readonly struct OpenFace
        {
            public OpenFace(int volume, double area, double distance)
            {
                Volume = volume;
                Area = area;
                Distance = distance;
            }

            public int Volume { get; }
            public double Area { get; }
            public double Distance { get; }
        }

        public ExtracellularSpace(double relativeTolerance = 1e-6)
            : this(NativeModule.Get(ModuleType, ModuleVersion), relativeTolerance)
        {
        }

        public const string ModuleType = "extracellular_volume", ModuleVersion = "biofilm_ArgusUNPUBLISHED_v01";

        public ExtracellularSpace(NativeModule module, double relativeTolerance)
        {
            _module = module;
            _relativeTolerance = relativeTolerance;
            var d = module.Descriptor;
            for (int s = 0; s < Species.Length; s++)
                _sC[s] = Require(d.StateIndex("C_" + Species[s]), "C_" + Species[s]);
            _sMatrix = Require(d.StateIndex("C_mx"), "C_mx");
            _sLost = Require(d.StateIndex("N_mx_lost"), "N_mx_lost");
            _eVolume = Require(d.ExternalIndex("V_e"), "V_e");
            _eMatrixIn = Require(d.ExternalIndex("j_mx_cell"), "j_mx_cell");
            _aHindrance = Require(d.AlgebraicIndex("f_hind"), "f_hind");
            var defaults = d.Instances["default"];
            OpenWater = new double[Species.Length];
            for (int s = 0; s < Species.Length; s++)
                OpenWater[s] = defaults["C_" + Species[s] + "_init"];
            BufferRatio = defaults["b_H"];
            DefaultVolume = defaults["V_e"];
            MatrixCeiling = defaults["C_mx_max"];
            Diffusivity = new double[Species.Length];
        }

        /// <summary>Diffusivity of each species in water [m²/s] (the face module's D_glc, D_lac, D_H, D_O2).</summary>
        public double[] Diffusivity { get; }

        /// <summary>The sea's composition [mol/m³]: the volume module's default (Proterozoic seawater).</summary>
        public double[] OpenWater { get; }

        /// <summary>Buffered H+ per free H+ (the module's b_H).</summary>
        public double BufferRatio { get; }

        /// <summary>The module's default volume [m³] (cells 1 µm in radius, 0.3 µm apart, 2 µm deep).</summary>
        public double DefaultVolume { get; }

        /// <summary>Densest matrix gel a volume holds [mol/m³ of glucose units].</summary>
        public double MatrixCeiling { get; }

        public int Count => _native.Count;

        /// <summary>Concentration of species <paramref name="s"/> in volume <paramref name="v"/> [mol/m³].</summary>
        public double Concentration(int v, int s) => _batch[v].States[_sC[s]][0];

        /// <summary>Matrix in the volume [mol/m³ of glucose units].</summary>
        public double Matrix(int v) => _batch[v].States[_sMatrix][0];

        /// <summary>Matrix that has left the colony from this volume since it was made [mol of glucose units].</summary>
        public double MatrixLost(int v) => _batch[v].States[_sLost][0];

        /// <summary>Diffusion in the volume over diffusion in water (1 without matrix).</summary>
        public double Hindrance(int v) => _batch[v].Algebraic[_aHindrance][0];

        public double Volume(int v) => _batch[v].Externals[_eVolume][0];

        /// <summary>Capacity for species <paramref name="s"/> [m³]: the volume, times 1 + b_H for H+.</summary>
        public double Capacity(int v, int s) => Volume(v) * (s == Proton ? 1.0 + BufferRatio : 1.0);

        /// <summary>Per volume and species: the cells' glucose uptake coefficient [m³/s] (implicit sink, glucose only).</summary>
        public double[] UptakeCoefficient { get; private set; } = Array.Empty<double>();

        /// <summary>Per species and volume ([s * Count + v]): what the cells release into it [mol/s].</summary>
        public double[] Source { get; private set; } = Array.Empty<double>();

        /// <summary>Per volume: matrix the cells deliver [mol glucose units/s].</summary>
        public double[] MatrixIn { get; private set; } = Array.Empty<double>();

        /// <summary>Per open face and species ([f * species + s]): concentration of the water outside [mol/m³].</summary>
        public double[] Outside { get; private set; } = Array.Empty<double>();

        /// <summary>After a step, per volume: glucose the cells took from it [mol] (UptakeCoefficient x new concentration x dt).</summary>
        public double[] GlucoseTaken { get; private set; } = Array.Empty<double>();

        /// <summary>After a step, per open face and species: what came in from outside [mol] (negative: went out).</summary>
        public double[] OpenInflow { get; private set; } = Array.Empty<double>();

        /// <summary>Running totals since the start, per species [mol]: in from the water outside (open faces and the water
        /// a closing gap encloses), in from the cells (releases less uptake).</summary>
        public double[] TotalFromOutside { get; } = new double[Species.Length];

        public double[] TotalFromCells { get; } = new double[Species.Length];

        /// <summary>Matrix delivered to the volumes since the start [mol glucose units] (see <see cref="MatrixPending"/>).</summary>
        public double TotalMatrixDelivered { get; private set; }

        public IReadOnlyList<Face> Faces => _faces;

        public IReadOnlyList<OpenFace> OpenFaces => _open;

        /// <summary>A new volume [m³] filled with <paramref name="water"/> (the water the closing gap encloses), no matrix.</summary>
        public int AddVolume(double volume, IReadOnlyList<double> water)
        {
            var batch = new KernelBatch(_module.Descriptor, 1) { Count = 1 };
            batch.ApplyInstance("default", 0, 1);
            batch.Externals[_eVolume][0] = volume;
            var native = _module.CreateInstance(_relativeTolerance);
            native.InitialiseStates(batch);
            for (int s = 0; s < Species.Length; s++)
                batch.States[_sC[s]][0] = water[s];
            native.SetStates(batch);
            native.Outputs(batch);
            _native.Add(native);
            _batch.Add(batch);
            int v = _native.Count - 1;
            for (int s = 0; s < Species.Length; s++)
                TotalFromOutside[s] += Capacity(v, s) * water[s];
            Grow();
            return v;
        }

        /// <summary>Sets the faces (they change as gaps close); volumes keep their contents.</summary>
        public void SetFaces(IReadOnlyList<Face> faces, IReadOnlyList<OpenFace> open)
        {
            _faces = new Face[faces.Count];
            for (int i = 0; i < faces.Count; i++)
                _faces[i] = faces[i];
            _open = new OpenFace[open.Count];
            for (int i = 0; i < open.Count; i++)
                _open[i] = open[i];
            Outside = new double[_open.Length * Species.Length];
            OpenInflow = new double[_open.Length * Species.Length];
            for (int f = 0; f < _open.Length; f++)
                for (int s = 0; s < Species.Length; s++)
                    Outside[f * Species.Length + s] = OpenWater[s];
        }

        /// <summary>Conductance of a face [m³/s], the extracellular_face law (two half-cells in series).</summary>
        public static double Conductance(double area, double diffusivity, double distanceA, double distanceB, double hindranceA,
            double hindranceB) =>
            area * diffusivity * hindranceA * hindranceB / (distanceA * hindranceB + distanceB * hindranceA);

        /// <summary>
        /// Advances by <paramref name="dt"/> [s] with the cells' exchange set in <see cref="UptakeCoefficient"/>,
        /// <see cref="Source"/> and <see cref="MatrixIn"/>, and the outside water in <see cref="Outside"/>.
        /// </summary>
        public void Step(double dt)
        {
            int n = Count;
            if (n == 0)
                return;
            Array.Clear(GlucoseTaken, 0, n);
            for (int s = 0; s < Species.Length; s++)
                Transport(s, dt);
            for (int v = 0; v < n; v++)
                _pending[v] += MatrixIn[v] * dt;
            _chemistryTime += dt;
            if (_chemistryTime >= ChemistryInterval * (1.0 - 1e-9))
                Chemistry();
        }

        /// <summary>
        /// How often the volumes' chemistry runs [s]: the matrix the cells exported meanwhile is delivered at its mean
        /// rate over the interval. The matrix changes over minutes to days, so a second loses nothing; a volume with no
        /// matrix and none arriving has no chemistry and is skipped.
        /// </summary>
        public double ChemistryInterval { get; set; } = 1.0;

        /// <summary>Matrix exported by the cells and not yet delivered to the volumes [mol glucose units].</summary>
        public double MatrixPending
        {
            get
            {
                double sum = 0.0;
                for (int v = 0; v < Count; v++)
                    sum += _pending[v];
                return sum;
            }
        }

        private double[] _pending = Array.Empty<double>();
        private double _chemistryTime;

        private void Chemistry()
        {
            double dt = _chemistryTime;
            _chemistryTime = 0.0;
            for (int v = 0; v < Count; v++)
            {
                var b = _batch[v];
                if (_pending[v] == 0.0 && b.States[_sMatrix][0] == 0.0)
                    continue;
                b.Externals[_eMatrixIn][0] = _pending[v] / dt;
                TotalMatrixDelivered += _pending[v];
                _pending[v] = 0.0;
                _native[v].SetInputs(b);
                _native[v].SetStates(b);
                _native[v].Advance(dt, b);
                _native[v].Outputs(b);
            }
        }

        // Backward Euler for one species: (K/dt + sum G + a) C - sum G C_neighbour = K C_old/dt + S + sum G_open C_out,
        // solved by conjugate gradients (symmetric, diagonally dominant), then the new state rebuilt from its fluxes.
        private void Transport(int s, double dt)
        {
            int n = Count;
            double d = Diffusivity[s];
            for (int v = 0; v < n; v++)
            {
                _old[v] = Concentration(v, s);
                double k = Capacity(v, s);
                _capacity[v] = k;
                _diag[v] = k / dt + (s == Glucose ? UptakeCoefficient[v] : 0.0);
                _rhs[v] = k * _old[v] / dt + Source[s * n + v];
            }
            foreach (var f in _faces)
            {
                double g = Conductance(f.Area, d, f.DistanceA, f.DistanceB, Hindrance(f.A), Hindrance(f.B));
                _diag[f.A] += g;
                _diag[f.B] += g;
            }
            for (int i = 0; i < _open.Length; i++)
            {
                var f = _open[i];
                double g = Conductance(f.Area, d, f.Distance, 0.0, Hindrance(f.Volume), 1.0);
                _diag[f.Volume] += g;
                _rhs[f.Volume] += g * Outside[i * Species.Length + s];
            }
            Solve(n, d);

            // the new state from the fluxes of the solved one: each face moves the same amount out of one volume and into
            // the other, so only the open faces and the cells change the total
            for (int v = 0; v < n; v++)
            {
                double uptake = s == Glucose ? UptakeCoefficient[v] * _x[v] : 0.0;
                _r[v] = (Source[s * n + v] - uptake) * dt;   // amount in [mol]
                TotalFromCells[s] += _r[v];
                if (s == Glucose)
                    GlucoseTaken[v] = uptake * dt;
            }
            foreach (var f in _faces)
            {
                double g = Conductance(f.Area, d, f.DistanceA, f.DistanceB, Hindrance(f.A), Hindrance(f.B));
                double flow = g * (_x[f.A] - _x[f.B]) * dt;
                _r[f.A] -= flow;
                _r[f.B] += flow;
            }
            for (int i = 0; i < _open.Length; i++)
            {
                var f = _open[i];
                double g = Conductance(f.Area, d, f.Distance, 0.0, Hindrance(f.Volume), 1.0);
                double flow = g * (Outside[i * Species.Length + s] - _x[f.Volume]) * dt;
                OpenInflow[i * Species.Length + s] = flow;
                TotalFromOutside[s] += flow;
                _r[f.Volume] += flow;
            }
            for (int v = 0; v < n; v++)
                _batch[v].States[_sC[s]][0] = _old[v] + _r[v] / _capacity[v];
        }

        // Jacobi-preconditioned conjugate gradients on the system above (matrix-free), into _x.
        private void Solve(int n, double d)
        {
            for (int v = 0; v < n; v++)
                _x[v] = _rhs[v] / _diag[v];
            Multiply(n, d, _x, _q);
            double rz = 0.0, norm = 0.0;
            for (int v = 0; v < n; v++)
            {
                _r[v] = _rhs[v] - _q[v];
                _p[v] = _r[v] / _diag[v];
                rz += _r[v] * _p[v];
                norm += _rhs[v] * _rhs[v] / (_diag[v] * _diag[v]);
            }
            double tolerance = 1e-30 * norm;
            for (int it = 0; it < 10 * n + 100 && rz > tolerance; it++)
            {
                Multiply(n, d, _p, _q);
                double pq = 0.0;
                for (int v = 0; v < n; v++)
                    pq += _p[v] * _q[v];
                if (!(pq > 0.0))
                    break;
                double alpha = rz / pq, next = 0.0;
                for (int v = 0; v < n; v++)
                {
                    _x[v] += alpha * _p[v];
                    _r[v] -= alpha * _q[v];
                    next += _r[v] * _r[v] / _diag[v];
                }
                double beta = next / rz;
                rz = next;
                for (int v = 0; v < n; v++)
                    _p[v] = _r[v] / _diag[v] + beta * _p[v];
            }
        }

        private void Multiply(int n, double d, double[] x, double[] y)
        {
            for (int v = 0; v < n; v++)
                y[v] = _diag[v] * x[v];
            foreach (var f in _faces)
            {
                double g = Conductance(f.Area, d, f.DistanceA, f.DistanceB, Hindrance(f.A), Hindrance(f.B));
                y[f.A] -= g * x[f.B];
                y[f.B] -= g * x[f.A];
            }
        }

        private void Grow()
        {
            int n = Count;
            if (_capacity.Length >= n)
            {
                ResizeExchange(n);
                return;
            }
            int m = 2 * n;
            _capacity = new double[m];
            _old = new double[m];
            _x = new double[m];
            _r = new double[m];
            _p = new double[m];
            _q = new double[m];
            _diag = new double[m];
            _rhs = new double[m];
            ResizeExchange(n);
        }

        // The exchange arrays are exactly Count long (Source is indexed s * Count + v), keeping what was set.
        private void ResizeExchange(int n)
        {
            var up = new double[n];
            Array.Copy(UptakeCoefficient, up, Math.Min(n, UptakeCoefficient.Length));
            UptakeCoefficient = up;
            int old = MatrixIn.Length;
            var source = new double[Species.Length * n];
            for (int s = 0; s < Species.Length; s++)
                Array.Copy(Source, s * old, source, s * n, Math.Min(old, n));
            Source = source;
            var mx = new double[n];
            Array.Copy(MatrixIn, mx, Math.Min(n, MatrixIn.Length));
            MatrixIn = mx;
            var pending = new double[n];
            Array.Copy(_pending, pending, Math.Min(n, _pending.Length));
            _pending = pending;
            GlucoseTaken = new double[n];
        }

        /// <summary>Total of species <paramref name="s"/> held in the volumes [mol] (H+ with its buffer).</summary>
        public double Held(int s)
        {
            double sum = 0.0;
            for (int v = 0; v < Count; v++)
                sum += Capacity(v, s) * Concentration(v, s);
            return sum;
        }

        /// <summary>Glucose units held as matrix, plus the matrix that has left the colony [mol].</summary>
        public double MatrixHeldAndLost()
        {
            double sum = 0.0;
            for (int v = 0; v < Count; v++)
                sum += Volume(v) * Matrix(v) + MatrixLost(v);
            return sum;
        }

        public void Dispose()
        {
            foreach (var n in _native)
                n.Dispose();
            _native.Clear();
        }

        private int Require(int index, string name)
        {
            if (index < 0)
                throw new ArgumentException($"{_module.Descriptor.Key} has no variable {name}");
            return index;
        }
    }
}
