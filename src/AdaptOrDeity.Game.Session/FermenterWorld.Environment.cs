using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    // The coupling to the environment field: each macro step the field fits the colony, the enzymes of the cells touching a
    // food packet hydrolyse its polymer into its free sugar, which leaks into the field; each cell reads glucose, lactate
    // and pH at its membrane (with the membrane correction for its own exchange with the field in the last step) and, on
    // the share of its membrane facing them, the free sugar of the packets it touches; it steps, and its exchange goes back
    // into the field and out of those packets in cell order; then the field steps.
    /// <summary>How the environment field trades accuracy for speed.</summary>
    public enum FieldAccuracy
    {
        /// <summary>
        /// The game's settings: nested grids, one field step per macro step, cells' inputs passed when they move by more
        /// than field.input_tolerance, cells at their default tolerance.
        /// </summary>
        Game,

        /// <summary>
        /// No trade-offs, for tests: one fine grid over the whole extent, field.reference_substeps field steps per macro
        /// step, every change of a cell's inputs passed, cells at field.reference_cell_tolerance.
        /// </summary>
        Reference,
    }

    public sealed partial class FermenterWorld
    {
        /// <summary>The environment's accuracy setting.</summary>
        public FieldAccuracy Accuracy { get; }

        private readonly EnvironmentField _environment;
        private double[] _packetShare = new double[0];   // per cell: share of its glucose uptake drawn from the packets it touches
        private double[] _fieldGlucose = new double[0];  // per cell: the field's glucose at its membrane [mol/m³]
        private double[] _fieldRelease = new double[0];  // per cell: glucose it exchanged with the field in the last step [mol/s]
        private double[] _contactRelease = new double[0];  // per cell: glucose leaking from the packets it touches this step [mol/s]
        private double _contactDt;
        private int _colonyCells;
        private double _inputTolerance = GameParameters.Default.Value("field.input_tolerance");   // 0 for the reference

        /// <summary>
        /// Share of an input's departure from the open water by which it must move before a cell sees the change (content
        /// field.input_tolerance; 0 in the reference): each change restarts the cell's integrator.
        /// </summary>
        public double InputTolerance
        {
            get => _inputTolerance;
            set => _inputTolerance = value >= 0.0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }
        private double _colonyRadius;
        private Func<FoodPacket, double>? _hydrolysis;
        private Func<FoodPacket, double, bool>? _atContact;

        /// <summary>The environment field: the water around the creature.</summary>
        public EnvironmentField Environment => _environment;

        /// <summary>Glucose the creature's cells have taken up from the packets they touch so far [mol] (part of <see cref="FoodEaten"/>).</summary>
        public double EatenFromPackets { get; private set; }

        /// <summary>
        /// Glucose the creature's cells and free organisms have taken from the environment field and released into it so
        /// far, net [mol].
        /// </summary>
        public double CellGlucoseExchanged { get; private set; }

        /// <summary>
        /// Test hook: the rate at which the creature's cell <c>i</c>'s surface enzymes hydrolyse a food packet it touches [mol glucose/s],
        /// whatever the packet. Null (the default): each cell's surface hydrolase,
        /// V_hydrolysis_max S / (K_m_poly + S) (<see cref="FermenterCell.Hydrolysis"/>), with S the packet's polymer
        /// concentration (<see cref="PolymerConcentration"/>).
        /// </summary>
        public Func<int, double>? HydrolysisCapacity { get; set; }

        /// <summary>
        /// Polymer in packet <paramref name="p"/> [mol glucose units/m³]: its polymer glucose over its volume, a sphere of
        /// the packet's radius (the convention its amount is set by). A packet keeps its density as it is eaten, so this
        /// stays near the packet density (~2.2 M of polymer), far above the hydrolase's K_m_poly: the enzyme is saturated
        /// and digestion is limited by the enzyme on the cell's surface, not by the substrate.
        /// </summary>
        public double PolymerConcentration(FoodPacket p)
        {
            double volume = 4.0 / 3.0 * Math.PI * p.Radius * p.Radius * p.Radius;
            return volume > 0.0 ? Math.Max(0.0, p.Glucose - p.Free) / volume : 0.0;
        }

        // Packet p is hydrolysed by every living cell touching it: the creature's, then free organisms'.
        private double Hydrolysis(FoodPacket p)
        {
            double rate = 0.0, polymer = -1.0;
            for (int i = 0; i < _cells.Count; i++)
            {
                if (!_cells[i].Alive)
                    continue;
                _body.WorldPosition(i, out double x, out double y);
                if (!_food.Touches(p, x, y, CellRadius))
                    continue;
                if (HydrolysisCapacity != null)
                    rate += HydrolysisCapacity(i);
                else
                {
                    if (polymer < 0.0)
                        polymer = PolymerConcentration(p);
                    rate += _cells[i].Hydrolysis(polymer);
                }
            }
            foreach (var o in _organisms)
            {
                if (!o.Alive || !_food.Touches(p, o.X, o.Y, CellRadius))
                    continue;
                if (polymer < 0.0)
                    polymer = PolymerConcentration(p);
                rate += o.Cell.Hydrolysis(polymer);
            }
            return rate;
        }

        private EnvironmentField CreateEnvironment(FermenterCell first)
        {
            var b = _body.Body;
            var settings = GameParameters.Default.Environment();
            settings.Grid.Reach = OrganismRange;   // free organisms in range exchange with the field
            if (Accuracy == FieldAccuracy.Reference)
            {
                settings.Grid.SingleGrid = true;
                settings.Grid.Substeps = GameParameters.Default.Integer("field.reference_substeps");
            }
            return new EnvironmentField(settings, _food.BackgroundGlucose,
                first.External("H_o_mod_H"), b.X, b.Y, 2.0 * CellRadius);
        }

        // The open water's glucose: the field's sea (and the food field's background, its record of it).
        private void SetOcean(double glucose)
        {
            _food.BackgroundGlucose = glucose;
            _environment.OceanGlucose = glucose;
        }

        /// <summary>The colony's diameter [m]: twice the farthest cell centre from the body's centre, plus a cell.</summary>
        public double ColonyDiameter
        {
            get
            {
                if (_colonyCells != _cells.Count)
                {
                    var b = _body.Body;
                    double r2 = 0.0;
                    for (int i = 0; i < _cells.Count; i++)
                    {
                        _body.WorldPosition(i, out double x, out double y);
                        r2 = Math.Max(r2, (x - b.X) * (x - b.X) + (y - b.Y) * (y - b.Y));
                    }
                    _colonyRadius = Math.Sqrt(r2) + CellRadius;
                    _colonyCells = _cells.Count;
                }
                return 2.0 * _colonyRadius;
            }
        }

        private void BeforeCells(double dt)
        {
            var b = _body.Body;
            _environment.Follow(b.X, b.Y, ColonyDiameter);
            _hydrolysis ??= Hydrolysis;
            _atContact ??= AtContact;
            if (_contactRelease.Length < _cells.Count)
                Array.Resize(ref _contactRelease, 2 * _cells.Count);
            Array.Clear(_contactRelease, 0, _cells.Count);
            foreach (var o in _organisms)
                o.ContactRelease = 0.0;
            _contactDt = dt;
            _environment.Dissolve(_food, dt, _hydrolysis, LeakAtContact ? _atContact : null);
        }

        /// <summary>
        /// Whether what leaks from a packet living cells touch enters the field at those cells (true: the game) or where the
        /// packet lies (false: to measure the near field's share in tests).
        /// </summary>
        public bool LeakAtContact { get; set; } = true;

        /// <summary>Glucose that leaked from packets at the cells touching them so far [mol] (part of the packets' release).</summary>
        public double ContactGlucose { get; private set; }

        // What leaks from a packet that living cells touch comes out at the contact, on their surface: it enters the field
        // at each touching cell, in equal shares, and each sees it in its membrane correction as its own release (the mean
        // over a sphere's surface of a source on that surface is the same as of one at its centre, Q / (4 pi D a)). A cell
        // takes up its share of that near field, k / (k + 4 pi D a) of it for a clearance k; the rest spreads into the
        // water. Returns false (the packet's own place) when no living cell touches it.
        private bool AtContact(FoodPacket p, double mol)
        {
            int n = 0;
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i].Alive && TouchesCell(p, i))
                    n++;
            foreach (var o in _organisms)
                if (o.Alive && _food.Touches(p, o.X, o.Y, CellRadius) && _environment.Field.LevelAt(o.X, o.Y) >= 0)
                    n++;
            if (n == 0)
                return false;
            double rate = mol / _contactDt / n;
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i].Alive && TouchesCell(p, i))
                {
                    _body.WorldPosition(i, out double x, out double y);
                    _environment.Exchange(EnvironmentField.Glucose, x, y, rate);
                    _contactRelease[i] += rate;
                }
            foreach (var o in _organisms)
                if (o.Alive && _food.Touches(p, o.X, o.Y, CellRadius) && _environment.Field.LevelAt(o.X, o.Y) >= 0)
                {
                    _environment.Exchange(EnvironmentField.Glucose, o.X, o.Y, rate);
                    o.ContactRelease += rate;
                }
            ContactGlucose += mol;
            return true;
        }

        private bool TouchesCell(FoodPacket p, int i)
        {
            _body.WorldPosition(i, out double x, out double y);
            return _food.Touches(p, x, y, CellRadius);
        }

        // Cell i at (x, y) reads the field at its membrane, corrected for what it exchanged with the field in the last step,
        // and the packets it touches on the membrane facing them.
        private void ReadEnvironment(int i, double x, double y)
        {
            var env = _environment;
            var c = _cells[i];
            double r = CellRadius;
            double glucose = env.AtMembrane(EnvironmentField.Glucose, x, y, r, c.Alive ? _fieldRelease[i] + _contactRelease[i] : 0.0);
            double lactate = env.AtMembrane(EnvironmentField.Lactate, x, y, r, c.Alive ? c.LactateRelease : 0.0);
            double protons = env.Protons(env.AtMembrane(EnvironmentField.Acid, x, y, r, c.Alive ? c.ProtonRelease : 0.0));
            _fieldGlucose[i] = glucose;
            _food.Faces(x, y, r, out double faced, out double offered);
            double membrane = (1.0 - faced) * glucose + offered;
            // an input that moved less than the tolerance keeps its value: each change restarts the cell's integrator (Moved)
            if (Moved(c.MembraneGlucose, membrane, _food.BackgroundGlucose))
                c.MembraneGlucose = membrane;
            _packetShare[i] = c.MembraneGlucose > 0.0 ? Math.Min(1.0, offered / c.MembraneGlucose) : 0.0;
            _openGlucoseShare[i] = 1.0 - _packetShare[i];
            if (Moved(c.MembraneLactate, lactate, 0.0))
                c.MembraneLactate = lactate;
            if (Moved(c.MembraneProtons, protons, _oceanProtons))
                c.MembraneProtons = protons;
            int nt = env.TraceCount;
            for (int t = 0; t < nt; t++)
                _traceAtCell[i * nt + t] = env.Field.Sample(EnvironmentField.FirstTrace + t, x, y);
        }

        // A membrane input is passed on when it moved by more than field.input_tolerance of its departure from the open
        // water (ocean): so what a cell does to its own water, however small, reaches it to that share, and a cell in
        // unchanging water keeps its input.
        private bool Moved(double current, double next, double ocean) =>
            Math.Abs(next - current) > _inputTolerance * Math.Abs(next - ocean);

        private double _oceanProtons;

        // Each living cell's exchange over the step goes into the field and out of the packets it touches, in cell order;
        // then the free organisms', and the field steps.
        private void AfterCells(double dt)
        {
            var env = _environment;
            double ocean = env.OceanGlucose;
            for (int i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                _fieldRelease[i] = 0.0;
                if (!c.Alive)
                    continue;
                // only the share of the cell's edge on open water (all of it before level 4; the rest went into the
                // space between cells), and of its uptake not drawn from packets or its vacuole
                double glucose = OpenGlucoseRelease(i), w = _openReleaseShare[i];
                env.Exchange(EnvironmentField.Glucose, _cellX[i], _cellY[i], glucose);
                env.Exchange(EnvironmentField.Lactate, _cellX[i], _cellY[i], w * c.LactateRelease);
                env.Exchange(EnvironmentField.Acid, _cellX[i], _cellY[i], w * c.ProtonRelease);
                CellGlucoseExchanged -= glucose * dt;
                _fieldRelease[i] = glucose;
                // food eaten: the share of the field uptake above the open water's glucose, and all it took from packets
                double m = _gaps != null ? _openGlucose[i] : _fieldGlucose[i];
                if (glucose < 0.0 && m > ocean)
                    FoodEaten += -glucose * dt * (m - ocean) / m;
                if (_packetShare[i] > 0.0 && c.GlucoseUptake > 0.0)
                {
                    double drawn = _food.Draw(_cellX[i], _cellY[i], CellRadius, c.GlucoseUptake * _packetShare[i] * dt, dt);
                    FoodEaten += drawn;
                    EatenFromPackets += drawn;
                }
            }
            OrganismsExchange(env, dt);
            if (_workerCount > 1)
                _workers ??= new ParallelWork(_workerCount);
            env.Step(dt, _workers);
        }
    }
}
