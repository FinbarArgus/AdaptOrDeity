using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>
    /// What cutting maintenance costs (FermenterCell.MaintenanceRegulation, regulated by falling ATP): the RNA and lipid
    /// upkeep loses ribosomes, rebuilt at full cost when food returns, and damages the envelope; the maintenance ATPase
    /// lapses quality control and damages ribosomes and proteins. At rest (f = 1) none of it moves.
    /// </summary>
    public class MaintenanceCostTests
    {
        private const double GameDt = 0.1;   // FermenterWorld's macro step
        private const double HalfFood = 0.05, Food = 10.0;   // mM glucose at the membrane

        // the maintenance switches by ATP: (submodule, suffix), as FermenterCell.MaintenanceRegulation
        private static readonly (string Sub, string Suffix)[] Maintenance =
            { ("protein", ""), ("bio", "_upkeep"), ("bio", "_repair"), ("cytosol", "") };

        private static FermenterCell NewCell(bool upkeep = true, bool atpase = true)
        {
            var cell = new FermenterCell(TraitCatalog.Fermenter, "default");
            foreach (var (sub, suffix) in Maintenance)
            {
                bool on = (suffix != "_upkeep" || upkeep) && (sub != "cytosol" || atpase);
                cell.SetExternal($"reg_on_ATP{suffix}_mod_{sub}", on ? 1.0 : 0.0);
            }
            return cell;
        }

        private static void Run(FermenterCell cell, double seconds, Action<FermenterCell>? each = null)
        {
            long steps = (long)Math.Round(seconds / GameDt);
            for (long i = 0; i < steps; i++)
            {
                cell.Step(GameDt);
                each?.Invoke(cell);
            }
        }

        [Test]
        public void CuttingRnaUpkeepThroughAFamineLosesRibosomesRebuiltAtACostWhenFoodReturns()
        {
            var cell = NewCell();
            double capacity = cell.AlgebraicValue("protein__build_capacity");
            Assert.That(cell.Ribosomes, Is.EqualTo(1.0));
            cell.MembraneGlucose = HalfFood;
            Run(cell, 3600.0);
            double lost = 1.0 - cell.Ribosomes;
            Assert.That(lost, Is.InRange(0.002, 0.02), "an hour at about a third of the upkeep paid, k_ribo 1%/h");
            Assert.That(cell.AlgebraicValue("protein__build_capacity"), Is.EqualTo(capacity * cell.Ribosomes).Within(1e-9 * capacity),
                "protein synthesis builds in proportion to the ribosomes");

            cell.MembraneGlucose = Food;
            double rebuilt = 0.0;   // ATP [mol] spent rebuilding
            Run(cell, 3.0 * 3600.0, c => rebuilt += c.Budget().AtpRibosomeRebuild * GameDt);
            Assert.That(cell.Ribosomes, Is.GreaterThan(1.0 - 0.1 * lost), "rebuilt with a time constant of about an hour");
            double perRibosome = cell.AlgebraicValue("ribosomes__c_ribo_build") / cell.External("N_A_mod_ribosomes");
            double ribosomes = cell.External("n_ribo_rest_mod_ribosomes");
            Assert.That(rebuilt, Is.GreaterThan(0.8 * lost * ribosomes * perRibosome), "at de novo cost");
        }

        [Test]
        public void CuttingLipidUpkeepDamagesTheEnvelope()
        {
            var cut = NewCell();
            var kept = NewCell(upkeep: false);
            cut.MembraneGlucose = kept.MembraneGlucose = HalfFood;
            Run(cut, 3600.0);
            Run(kept, 3600.0);
            Assert.That(cut.DamagePhysical, Is.GreaterThan(0.007), "up to 2% of a lethal load per hour, at the unpaid share");
            Assert.That(kept.DamagePhysical, Is.LessThan(0.1 * cut.DamagePhysical));
            Assert.That(kept.Ribosomes, Is.GreaterThan(1.0 - 1e-3), "the upkeep paid, ribosomes are kept");
        }

        [Test]
        public void CuttingTheMaintenanceAtpaseDamagesRibosomesAndProteins()
        {
            var cut = NewCell();
            var kept = NewCell(atpase: false);
            cut.MembraneGlucose = kept.MembraneGlucose = HalfFood;
            double wear = cut.DamageRibosome;
            Run(cut, 3600.0);
            Run(kept, 3600.0);
            Assert.That(cut.DamageRibosome - wear, Is.GreaterThan(0.009), "a full cut costs a lethal load in two days");
            Assert.That(kept.DamageRibosome - wear, Is.LessThan(0.2 * (cut.DamageRibosome - wear)));
        }

        [Test]
        public void AtRestNoCutCostsAnything()
        {
            var cell = NewCell();   // every maintenance switch on: f = 1 at rest
            Run(cell, 3600.0);
            Assert.That(cell.Ribosomes, Is.EqualTo(1.0).Within(1e-12));
            Assert.That(cell.DamagePhysical, Is.LessThan(1e-12));
            Assert.That(cell.AlgebraicValue("bio__R_dmg_qc"), Is.EqualTo(0.0));
            Assert.That(cell.Budget().AtpRibosomeRebuild, Is.LessThan(1e-12 * cell.Budget().AtpConsumed));
        }
    }
}
