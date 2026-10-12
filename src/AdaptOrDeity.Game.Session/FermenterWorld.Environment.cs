using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    // The coupling to the environment field: each macro step the field fits the colony, food packets dissolve into it,
    // each cell reads glucose, lactate and pH at its membrane (with the membrane correction for its own exchange in the
    // last step), steps, and its exchange goes back into the field in cell order; then the field steps.
    public sealed partial class FermenterWorld
    {
        private readonly EnvironmentField? _environment;
        private int _colonyCells;
        private double _colonyRadius;

        /// <summary>The environment field, or null when cells use the contact rule.</summary>
        public EnvironmentField? Environment => _environment;

        /// <summary>Glucose the cells have taken from the environment field and released into it so far, net [mol].</summary>
        public double CellGlucoseExchanged { get; private set; }

        private EnvironmentField CreateEnvironment(FermenterCell first)
        {
            var b = _body.Body;
            return new EnvironmentField(GameParameters.Default.Environment(), _food.BackgroundGlucose,
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
            _environment.Dissolve(_food, dt);
        }

        // Cell i at (x, y) reads the field at its membrane, corrected for what it exchanged in the last step.
        private void ReadEnvironment(int i, double x, double y)
        {
            var env = _environment!;
            var c = _cells[i];
            double r = CellRadius;
            c.MembraneGlucose = env.AtMembrane(EnvironmentField.Glucose, x, y, r, c.Alive ? c.GlucoseRelease : 0.0);
            c.MembraneLactate = env.AtMembrane(EnvironmentField.Lactate, x, y, r, c.Alive ? c.LactateRelease : 0.0);
            c.MembraneProtons = env.Protons(env.AtMembrane(EnvironmentField.Acid, x, y, r, c.Alive ? c.ProtonRelease : 0.0));
            int nt = env.TraceCount;
            for (int t = 0; t < nt; t++)
                _traceAtCell[i * nt + t] = env.Field.Sample(EnvironmentField.FirstTrace + t, x, y);
        }

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
                double glucose = c.GlucoseRelease;
                env.Exchange(EnvironmentField.Glucose, _cellX[i], _cellY[i], glucose);
                env.Exchange(EnvironmentField.Lactate, _cellX[i], _cellY[i], c.LactateRelease);
                env.Exchange(EnvironmentField.Acid, _cellX[i], _cellY[i], c.ProtonRelease);
                CellGlucoseExchanged -= glucose * dt;
                // food eaten: the share of the uptake above the open water's glucose
                double m = c.MembraneGlucose;
                if (glucose < 0.0 && m > ocean)
                    FoodEaten += -glucose * dt * (m - ocean) / m;
            }
            if (_workerCount > 1)
                _workers ??= new ParallelWork(_workerCount);
            env.Step(dt, _workers);
        }
    }
}
