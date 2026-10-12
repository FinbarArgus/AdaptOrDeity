using System;
using AdaptOrDeity.Game.Content;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class LateRestartTests
    {
        [Test]
        public void HardRestartsLateInARunSurviveTheMotorSwitchingOn()
        {
            // A cell whose inputs change every macro step restarts its integrator each time. Its motor has been off, so
            // its activation sits at zero (error weight 1/atol); when the motor switches on at a late restart the solver
            // once chose a first step too small for t + h to differ from t, and failed ("step too small").
            var cell = new FermenterCell(TraitCatalog.Fermenter) { StopAtMacroSteps = false };
            double c0 = cell.MembraneGlucose;
            for (int k = 0; k < 25_000; k++)   // 2,500 s
            {
                cell.MembraneGlucose = c0 * (1.0 + 0.01 * Math.Sin(0.7 * k));   // a hard restart at every step
                cell.MotorActivation = k < 20_400 ? 0.0 : 1.0;                  // on at t = 2040 s
                Assert.DoesNotThrow(() => cell.Step(0.1), $"t = {0.1 * k} s");
            }
        }

        [Test]
        public void AFailedRunSaysWhyAndWhen()
        {
            var cell = new FermenterCell(TraitCatalog.Fermenter);
            cell.MembraneGlucose = double.NaN;
            var e = Assert.Throws<InvalidOperationException>(() => cell.Step(0.1))!;
            Assert.That(e.Message, Does.Match("(step too small|too many steps|non-finite values) at t = "));
        }
    }
}
