using System;
using AdaptOrDeity.Sim.Kernels.Generated;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class SingleCellInBathTests
    {
        // 20 µm square cell, 20 µm deep (V_P = 8e-15 m³ in the default instance); O2 diffusivity in cytoplasm
        // ~2e-9 m²/s; diffusion distance = half the cell width. Only the four in-plane faces exchange; the
        // into-screen and out-of-screen surfaces are insulated (adapting_plan.md, "Game design").
        private const double CellWidth = 20e-6;
        private const double CellDepth = 20e-6;
        private const double DiffusivityO2 = 2e-9;
        private const double Conductance = DiffusivityO2 * 4 * CellWidth * CellDepth / (CellWidth / 2);

        [TestCase(0.2)]   // air-saturated water at ~37 °C
        [TestCase(0.02)]  // hypoxic water
        public void ReachesAnalyticSteadyState(double bath)
        {
            var cell = new SingleCellInBath(new TissueDiffusionVolume_nn(), "default", "C_P", "flux_c", Conductance, bath);
            var d = cell.Batch.Descriptor;
            double v = cell.Batch.Externals[d.ExternalIndex("V_P")][0];
            double m = cell.Batch.Externals[d.ExternalIndex("M")][0];
            double c50 = cell.Batch.Externals[d.ExternalIndex("C50")][0];

            // G (Cb − C) = −M V C / (C + C50)  ⇒  G C² − b C − G Cb C50 = 0,  b = G (Cb − C50) + M V.
            double g = Conductance;
            double b = g * (bath - c50) + m * v;
            double expected = (b + Math.Sqrt(b * b + 4 * g * g * bath * c50)) / (2 * g);

            // Time constant ≈ V/G ≈ 0.025 s; run far past it with dt ≪ V/G.
            for (int i = 0; i < 20000; i++)
                cell.Step(1e-4);

            Assert.That(cell.Concentration, Is.EqualTo(expected).Within(1e-9 * expected + 1e-15));
            Assert.That(cell.Concentration, Is.LessThan(bath), "a consuming cell sits below the bath");
        }

        [Test]
        public void NoConsumptionEquilibratesWithBath()
        {
            var cell = new SingleCellInBath(new TissueDiffusionVolume_nn(), "default", "C_P", "flux_c", Conductance, 0.2);
            cell.Batch.Externals[cell.Batch.Descriptor.ExternalIndex("M")][0] = 0.0;
            for (int i = 0; i < 20000; i++)
                cell.Step(1e-4);
            Assert.That(cell.Concentration, Is.EqualTo(0.2).Within(1e-12));
        }
    }
}
