using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>
    /// The regulation switches in the game's kernel (upstream cell/expression_regulation): off they change nothing, on they
    /// lower a starving cell's targets as its ATP falls, and regulating the pumps, exchangers, flagellum and hydrolase by
    /// ATP keeps a cell on half food out of the ATP trap.
    /// </summary>
    public class RegulationKernelTests
    {
        private const double GameDt = 0.1;

        private static FermenterCell NewCell() => new FermenterCell(TraitCatalog.Fermenter);

        private static void Run(FermenterCell cell, double seconds)
        {
            long steps = (long)Math.Round(seconds / GameDt);
            for (long i = 0; i < steps; i++)
                cell.Step(GameDt);
        }

        private static int Trait(FermenterCell cell, string id)
        {
            for (int t = 0; t < cell.Traits.Count; t++)
                if (cell.Traits[t].Id == id)
                    return t;
            throw new ArgumentException(id);
        }

        private static int Species(string id) => Array.IndexOf(FermenterCell.RegulationSpecies, id);

        [Test]
        public void SwitchesOffLeaveTheCellExactlyAsWithoutRegulation()
        {
            var cell = NewCell();
            for (int target = 0; target < cell.RegulationTargets; target++)
            {
                for (int s = 0; s < FermenterCell.RegulationSpecies.Length; s++)
                    Assert.That(cell.Regulated(target, s), Is.False);
                Assert.That(cell.RegulationFactor(target), Is.EqualTo(1.0), "R_reg is exactly 1");
            }
            // the signals are at their resting value (x = 2: rest is twice the half-way level)
            foreach (var s in new[] { "ATP", "G6P", "glc", "H" })
                Assert.That(cell.AlgebraicValue($"reg__f_{s}_reg"), Is.EqualTo(16.0 / 17.0).Within(1e-9), s);
        }

        [Test]
        public void ATargetFollowsItsSpeciesAsAStarvingCellsAtpFalls()
        {
            var cell = NewCell();
            cell.MembraneGlucose = 0.0;
            cell.GrowthFraction = 1.0;
            int atpase = Trait(cell, "H_ATPase_expression"), atp = Species("ATP");
            cell.SetRegulated(atpase, atp, true);
            cell.SetRegulated(cell.GrowthRegulation, atp, true);
            double lastAtp = cell.Atp, lastF = cell.RegulationFactor(atpase);
            for (int k = 0; k < 12; k++)
            {
                Run(cell, 10.0);
                double f = cell.AlgebraicValue("reg__f_ATP_reg"), c = cell.Atp, k4 = Math.Pow(c / (0.5 * 9.6), 4);
                Assert.That(f, Is.EqualTo(k4 / (1.0 + k4)).Within(1e-12), "f = x^4/(1 + x^4), x = ATP / (half the resting ATP)");
                Assert.That(cell.RegulationFactor(atpase), Is.EqualTo(f));
                Assert.That(cell.AlgebraicValue("pool_H_ATPase__m_goal"),
                    Is.EqualTo(cell.AlgebraicValue("pool_H_ATPase__m_target") * f).Within(1e-12));
                Assert.That(cell.RegulationFactor(cell.GrowthRegulation), Is.EqualTo(f));
                Assert.That(cell.AlgebraicValue("bio__J_growth"), Is.EqualTo(cell.External("J_ref_mod_bio")
                    * cell.AlgebraicValue("bio__throttle") * f).Within(1e-30));
                Assert.That((f - lastF) * (c - lastAtp), Is.GreaterThanOrEqualTo(0.0), "f moves with ATP");
                lastAtp = c;
                lastF = f;
            }
            Assert.That(cell.Atp, Is.LessThan(4.8), "ATP has fallen below half its resting level");
            Assert.That(cell.RegulationFactor(atpase), Is.LessThan(0.5));
            Assert.That(cell.TraitValue(atpase), Is.LessThan(0.8), "the H+-ATPase copies follow the lowered target down");
        }

        [Test]
        public void RegulatingThePumpsByAtpAvoidsTheAtpTrapOnHalfFood()
        {
            // Half the resting medium glucose (0.05 mM) for an hour, then full food (0.1 mM) for an hour. Without
            // regulation ATP runs out and stays out (hexokinase needs ATP to restart glycolysis); with the pumps,
            // exchangers, flagellum and hydrolase regulated by ATP, ATP holds above 4 mM and the cell returns to rest.
            string[] regulated = { "H_ATPase_expression", "NhaA_expression", "KefC_expression", "Trk_expression",
                "NCX_expression", "flagella", "Hydrolase_expression" };
            var plain = NewCell();
            var regulatedCell = NewCell();
            foreach (var id in regulated)
                regulatedCell.SetRegulated(Trait(regulatedCell, id), Species("ATP"), true);
            double minAtp = double.MaxValue;
            foreach (var c in new[] { plain, regulatedCell })
                c.MembraneGlucose = 0.05;
            for (int k = 0; k < 360; k++)
            {
                Run(plain, 10.0);
                Run(regulatedCell, 10.0);
                minAtp = Math.Min(minAtp, regulatedCell.Atp);
            }
            TestContext.WriteLine($"half food, 1 h: ATP {plain.Atp:G3} mM unregulated, {regulatedCell.Atp:G3} mM regulated " +
                                  $"(lowest {minAtp:G3}); H+-ATPase x{regulatedCell.TraitValue(Trait(regulatedCell, "H_ATPase_expression")):F2}");
            Assert.That(plain.Atp, Is.LessThan(1e-3));
            Assert.That(minAtp, Is.GreaterThan(4.0));
            foreach (var c in new[] { plain, regulatedCell })
                c.MembraneGlucose = 0.1;
            Run(plain, 3600.0);
            Run(regulatedCell, 3600.0);
            TestContext.WriteLine($"full food, 1 h: ATP {plain.Atp:G3} mM unregulated, {regulatedCell.Atp:G3} mM regulated; " +
                                  $"V {regulatedCell.State("membrane__V"):F1} mV");
            Assert.That(plain.Atp, Is.LessThan(1e-3), "the trap: food is back but glycolysis cannot restart");
            Assert.That(regulatedCell.Atp, Is.GreaterThan(9.0));
            Assert.That(regulatedCell.State("membrane__V"), Is.LessThan(-100.0));
        }
    }
}
