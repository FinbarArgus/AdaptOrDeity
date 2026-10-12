using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    // The coupling to the environment field: each macro step the field fits the colony, the enzymes of the cells touching a
    // food packet hydrolyse it into the field, each cell reads glucose, lactate and pH at its membrane (with the membrane
    // correction for its own exchange in the last step), steps, and its exchange goes back into the field in cell order;
    // then the field steps.
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

        private readonly EnvironmentField? _environment;
        private int _colonyCells;
        private readonly double _inputTolerance = GameParameters.Default.Value("field.input_tolerance");   // 0 for the reference
        private double _colonyRadius;
        private Func<FoodPacket, double>? _hydrolysis;

        /// <summary>The environment field, or null when cells use the contact rule.</summary>
        public EnvironmentField? Environment => _environment;

        /// <summary>Glucose the cells have taken from the environment field and released into it so far, net [mol].</summary>
        public double CellGlucoseExchanged { get; private set; }

        /// <summary>
        /// Test hook: the rate at which cell <c>i</c>'s surface enzymes hydrolyse a food packet it touches [mol glucose/s],
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

        // Packet p is hydrolysed by every living cell touching it.
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
            return rate;
        }

        private EnvironmentField CreateEnvironment(FermenterCell first)
        {
            var b = _body.Body;
            var settings = GameParameters.Default.Environment();
            if (Accuracy == FieldAccuracy.Reference)
            {
                settings.Grid.SingleGrid = true;
                settings.Grid.Substeps = GameParameters.Default.Integer("field.reference_substeps");
            }
            return new EnvironmentField(settings, _food.BackgroundGlucose,
                first.External("H_o_mod_H"), b.X, b.Y, 2.0 * CellRadius);
        }

        // The open water's glucose: the food field's background, and the field's sea when there is one.
        private void SetOcean(double glucose)
        {
            _food.BackgroundGlucose = glucose;
            if (_environment != null)
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
            _environment!.Follow(b.X, b.Y, ColonyDiameter);
            _hydrolysis ??= Hydrolysis;
            _environment.Dissolve(_food, dt, _hydrolysis);
        }

        // Cell i at (x, y) reads the field at its membrane, corrected for what it exchanged in the last step.
        private void ReadEnvironment(int i, double x, double y)
        {
            var env = _environment!;
            var c = _cells[i];
            double r = CellRadius;
            double glucose = env.AtMembrane(EnvironmentField.Glucose, x, y, r, c.Alive ? c.GlucoseRelease : 0.0);
            double lactate = env.AtMembrane(EnvironmentField.Lactate, x, y, r, c.Alive ? c.LactateRelease : 0.0);
            double protons = env.Protons(env.AtMembrane(EnvironmentField.Acid, x, y, r, c.Alive ? c.ProtonRelease : 0.0));
            // an input that moved less than the tolerance keeps its value: each change restarts the cell's integrator
            if (Moved(c.MembraneGlucose, glucose))
                c.MembraneGlucose = glucose;
            if (Moved(c.MembraneLactate, lactate))
                c.MembraneLactate = lactate;
            if (Moved(c.MembraneProtons, protons))
                c.MembraneProtons = protons;
            int nt = env.TraceCount;
            for (int t = 0; t < nt; t++)
                _traceAtCell[i * nt + t] = env.Field.Sample(EnvironmentField.FirstTrace + t, x, y);
        }

        private bool Moved(double current, double next) =>
            Math.Abs(next - current) > _inputTolerance * Math.Max(Math.Abs(current), Math.Abs(next));

        // Each living cell's exchange over the step goes into the field, in cell order; then the field steps.
        private void AfterCells(double dt)
        {
            var env = _environment!;
            double ocean = env.OceanGlucose;
            for (int i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (!c.Alive)
                    continue;
                // only the share of the cell's edge on open water (all of it before level 4; the rest went into the
                // space between cells)
                double glucose = OpenGlucoseRelease(i), w = _openReleaseShare[i];
                env.Exchange(EnvironmentField.Glucose, _cellX[i], _cellY[i], glucose);
                env.Exchange(EnvironmentField.Lactate, _cellX[i], _cellY[i], w * c.LactateRelease);
                env.Exchange(EnvironmentField.Acid, _cellX[i], _cellY[i], w * c.ProtonRelease);
                CellGlucoseExchanged -= glucose * dt;
                // food eaten: the share of the uptake above the open water's glucose
                double m = _gaps != null ? _openGlucose[i] : _outsideGlucose[i];
                if (glucose < 0.0 && m > ocean)
                    FoodEaten += -glucose * dt * (m - ocean) / m;
            }
            if (_workerCount > 1)
                _workers ??= new ParallelWork(_workerCount);
            env.Step(dt, _workers);
        }
    }
}
