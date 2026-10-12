using System;
using AdaptOrDeity.Game.Content;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class StopAtMacroStepsTests
    {
        [Test]
        public void SoftRestartsCutTheWorkWhenInputsChangeEveryStepAndAgree()
        {
            // Glucose at the membrane changes every 0.1 s (as the environment field does): with steps ending on the macro
            // step and soft restarts the cell keeps its step size and factorisation, and gets the same answer.
            FermenterCell Run(bool soft)
            {
                var cell = new FermenterCell(TraitCatalog.Fermenter) { StopAtMacroSteps = soft };
                double c0 = cell.MembraneGlucose;
                for (int k = 0; k < 3000; k++)
                {
                    cell.MembraneGlucose = c0 * (1.0 + 0.05 * Math.Sin(2.0 * Math.PI * k * 0.1 / 20.0));
                    cell.Step(0.1);
                }
                return cell;
            }
            var hard = Run(false);
            var soft = Run(true);
            Assert.That(soft.Statistics.Jacobians, Is.LessThan(hard.Statistics.Jacobians / 3), "far fewer Jacobians");
            Assert.That(soft.Statistics.RateEvaluations, Is.LessThan(hard.Statistics.RateEvaluations / 2), "and rate evaluations");
            Assert.That(soft.GlucoseUptake, Is.EqualTo(hard.GlucoseUptake).Within(1e-3 * Math.Abs(hard.GlucoseUptake)));
            Assert.That(soft.Budget().AtpGlycolysis, Is.EqualTo(hard.Budget().AtpGlycolysis).Within(1e-3 * hard.Budget().AtpGlycolysis));
        }
    }
}
