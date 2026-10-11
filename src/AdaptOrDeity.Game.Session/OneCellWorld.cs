using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// M2 world: one 20 µm cell consuming O2 (tissue_diffusion_volume) in a well-mixed bath, exchanging through
    /// its four in-plane faces only. Replaced by the organism + transport solver in M3.
    /// </summary>
    public sealed class OneCellWorld : ISimWorld, IBathWorld
    {
        /// <summary>Air-saturated water at ~37 °C [mol/m³].</summary>
        public const double DefaultBathO2 = 0.2;

        private const double CellWidth = 20e-6;   // m
        private const double CellDepth = 20e-6;   // m
        private const double DiffusivityO2 = 2e-9; // m²/s, cytoplasm

        private static readonly OverlayInfo[] OverlayList =
        {
            new OverlayInfo("o2", "Oxygen", "F1", "mol/m³", "µM", 1000.0, 0.0, 250.0),
        };

        private readonly SingleCellInBath _cell;

        public OneCellWorld(double bathO2 = DefaultBathO2)
        {
            double conductance = DiffusivityO2 * 4 * CellWidth * CellDepth / (CellWidth / 2);
            _cell = new SingleCellInBath(NativeModule.Get("tissue_diffusion_volume", "nn"), "default", "C_P", "flux_c", conductance, bathO2);
            // Explicit exchange coupling is stable and accurate for dt well below V/G (~0.025 s).
            MacroDt = 1e-3;
        }

        public IReadOnlyList<OverlayInfo> Overlays => OverlayList;

        public int CellCount => 1;

        public double MacroDt { get; }

        public double BathConcentration
        {
            get => _cell.BathConcentration;
            set
            {
                if (!(value >= 0.0))
                    throw new ArgumentOutOfRangeException(nameof(value), "must be >= 0 mol/m³");
                _cell.BathConcentration = value;
            }
        }

        public void Step(double t, double dt) => _cell.Step(dt);

        public double Read(int cell, int channel)
        {
            if (cell != 0 || channel != 0)
                throw new ArgumentOutOfRangeException(cell != 0 ? nameof(cell) : nameof(channel));
            return _cell.Concentration;
        }
    }
}
