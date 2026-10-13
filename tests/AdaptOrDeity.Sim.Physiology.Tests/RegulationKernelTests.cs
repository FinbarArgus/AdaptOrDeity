using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>
    /// The regulation switches in the game's kernel (upstream cell/expression_regulation): off they change nothing, on they
    /// change nothing at rest and lower a starving cell's targets as its ATP falls; regulating the maintenance by ATP keeps
    /// a cell on half food out of the ATP trap, where regulating its pumps' expression no longer does (their copies fall
    /// only as they are degraded, about 1% an hour).
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
            // the signals are exactly 1 at rest: a switch has no effect there
            foreach (var s in new[] { "ATP", "G6P", "glc", "H" })
                Assert.That(cell.AlgebraicValue($"reg__f_{s}_reg"), Is.EqualTo(1.0), s);
        }

        [Test]
        public void EverySwitchOnChangesNothingAtRest()
        {
            var plain = NewCell();
            var all = NewCell();
            for (int target = 0; target < all.RegulationTargets; target++)
                for (int s = 0; s < FermenterCell.RegulationSpecies.Length; s++)
                    all.SetRegulated(target, s, true);
            Run(plain, 600.0);
            Run(all, 600.0);
            foreach (var state in new[] { "cytosol__C_ATP", "membrane__V", "pool_H_ATPase__m", "pool_proteome__m", "bio__D" })
                Assert.That(all.State(state), Is.EqualTo(plain.State(state)).Within(1e-9 * Math.Abs(plain.State(state))), state);
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
                double f = cell.AlgebraicValue("reg__f_ATP_reg"), c = cell.Atp;
                double u4 = Math.Pow(Math.Max(0.0, 1.0 - c / 9.6) / 0.5, 4);
                Assert.That(f, Is.EqualTo(1.0 / (1.0 + u4)).Within(1e-12),
                    "f = 1/(1 + u^4), u = the shortfall below the resting ATP over half of it");
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
            Assert.That(cell.TraitValue(atpase), Is.LessThan(0.95),
                "the H+-ATPase copies above the falling goal are removed by proteolysis within minutes");
            Assert.That(cell.RecycledAminoAcids, Is.GreaterThan(1e6), "their amino acids go to the recycled pool");
        }

        [Test]
        public void RegulatingTheMaintenanceByAtpAvoidsTheAtpTrapOnHalfFood()
        {
            // Half the resting medium glucose (0.05 mM) for an hour, then full food (0.1 mM) for an hour. Without
            // regulation ATP runs out and stays out (hexokinase needs ATP to restart glycolysis), and regulating the pumps,
            // exchangers, flagellum and hydrolase by ATP does not help: their copies are removed within minutes (the
            // H+-ATPase to about half), the membrane depolarises and ATP still runs out. With the maintenance regulated by
            // ATP (protein synthesis, RNA and lipid upkeep, repair, the maintenance ATPase), ATP holds above 4 mM; when
            // food returns the cell rebuilds the protein it lost while synthesis was cut, mostly from the recycled amino
            // acids, and ATP returns towards rest.
            string[] pumps = { "H_ATPase_expression", "NhaA_expression", "KefC_expression", "Trk_expression",
                "NCX_expression", "flagella", "Hydrolase_expression" };
            var plain = NewCell();
            var pumped = NewCell();
            var maintained = NewCell();
            foreach (var id in pumps)
                pumped.SetRegulated(Trait(pumped, id), Species("ATP"), true);
            for (int k = 0; k < FermenterCell.MaintenanceRegulation.Length; k++)
                maintained.SetRegulated(maintained.MaintenanceTarget(k), Species("ATP"), true);
            var cells = new[] { plain, pumped, maintained };
            double minAtp = double.MaxValue;
            foreach (var c in cells)
                c.MembraneGlucose = 0.05;
            for (int k = 0; k < 360; k++)
            {
                foreach (var c in cells)
                    Run(c, 10.0);
                minAtp = Math.Min(minAtp, maintained.Atp);
            }
            double famineAtp = maintained.Atp;
            TestContext.WriteLine($"half food, 1 h: ATP {plain.Atp:G3} mM unregulated, {pumped.Atp:G3} pumps regulated, " +
                                  $"{maintained.Atp:G3} maintenance regulated (lowest {minAtp:G3})");
            Assert.That(plain.Atp, Is.LessThan(1e-3));
            Assert.That(pumped.Atp, Is.LessThan(1e-3));
            Assert.That(pumped.TraitValue(Trait(pumped, "H_ATPase_expression")), Is.LessThan(0.6));
            Assert.That(minAtp, Is.GreaterThan(4.0));
            foreach (var c in cells)
                c.MembraneGlucose = 0.1;
            foreach (var c in cells)
                Run(c, 3600.0);
            TestContext.WriteLine($"full food, 1 h: ATP {plain.Atp:G3} mM unregulated, {maintained.Atp:G3} regulated; " +
                                  $"V {maintained.State("membrane__V"):F1} mV");
            Assert.That(plain.Atp, Is.LessThan(1e-3), "the trap: food is back but glycolysis cannot restart");
            Assert.That(pumped.Atp, Is.LessThan(1e-3));
            Assert.That(maintained.Atp, Is.GreaterThan(9.0), "back near the resting 9.6 mM within the hour");
            Assert.That(maintained.State("membrane__V"), Is.LessThan(-100.0));
        }
    }
}
