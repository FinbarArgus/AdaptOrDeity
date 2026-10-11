using System;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>
    /// M1 scenario: one cell, modelled by a species-pool module, exchanging with a well-mixed bath held at a
    /// fixed concentration. The module's exchange input is driven by diffusive conductance:
    /// flux = G (C_bath − C) [mol/s], with G = D A / d [m³/s] (same flux law as tissue_diffusion_face).
    /// The spatial transport solver replaces this in M3.
    /// </summary>
    public sealed class SingleCellInBath
    {
        private readonly NativeInstance _native;
        private readonly int _concentrationState;
        private readonly int _fluxExternal;

        public SingleCellInBath(NativeModule module, string instance, string concentrationState, string exchangeFluxInput,
            double conductance, double bathConcentration)
        {
            if (!(conductance >= 0.0))
                throw new ArgumentOutOfRangeException(nameof(conductance), "must be >= 0 m³/s");
            var d = module.Descriptor;
            _concentrationState = d.StateIndex(concentrationState);
            _fluxExternal = d.ExternalIndex(exchangeFluxInput);
            if (_concentrationState < 0)
                throw new ArgumentException($"{d.Key} has no state {concentrationState}", nameof(concentrationState));
            if (_fluxExternal < 0)
                throw new ArgumentException($"{d.Key} has no input {exchangeFluxInput}", nameof(exchangeFluxInput));

            Conductance = conductance;
            BathConcentration = bathConcentration;
            Batch = new KernelBatch(d, 1) { Count = 1 };
            Batch.ApplyInstance(instance, 0, 1);
            _native = module.CreateInstance(1e-8);
            _native.InitialiseStates(Batch);
            UpdateExchange();
            _native.SetInputs(Batch);
            _native.ComputeVariables(Batch);
        }

        public KernelBatch Batch { get; }

        /// <summary>Exchange conductance G [m³/s].</summary>
        public double Conductance { get; set; }

        /// <summary>Bath concentration [mol/m³].</summary>
        public double BathConcentration { get; set; }

        public double Time { get; private set; }

        /// <summary>Cell concentration [mol/m³].</summary>
        public double Concentration => Batch.States[_concentrationState][0];

        /// <summary>Current flux from the bath into the cell [mol/s].</summary>
        public double ExchangeFlux => Batch.Externals[_fluxExternal][0];

        /// <summary>
        /// Advances by <paramref name="dt"/> seconds. The exchange flux is held over the step (operator
        /// splitting); keep dt well below V/G for accuracy.
        /// </summary>
        public void Step(double dt)
        {
            UpdateExchange();
            _native.SetInputs(Batch);
            _native.Advance(dt, Batch);
            Time += dt;
        }

        private void UpdateExchange() =>
            Batch.Externals[_fluxExternal][0] = Conductance * (BathConcentration - Concentration);
    }
}
