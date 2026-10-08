using System.Linq;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class FermenterSessionTests
    {
        [Test]
        public void SchemaOffersTheCellPanelAndTraits()
        {
            var loop = new SimLoop(new FermenterWorld(), new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var schema = loop.Schema;
            Assert.That(schema.Details.Select(d => d.Group).Distinct(),
                Is.EquivalentTo(new[] { "Metabolic rate", "ATP made", "ATP used", "Protein turnover", "Cell state" }));
            Assert.That(schema.Traits.Count(t => t.IsExpression), Is.EqualTo(7));
            Assert.That(schema.Traits.Count(t => !t.IsExpression), Is.EqualTo(6));
        }

        [Test]
        public void SnapshotCarriesTheEnergyBudgetAndTraitCommandsAdaptTheCell()
        {
            var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var frame = loop.CreateFrame();
            int turnover = Index(loop, "atp_turnover"), made = Index(loop, "atp_glycolysis"), build = Index(loop, "atp_protein_build");
            int trait = loop.Schema.Traits.ToList().FindIndex(t => t.Id == "NhaA_expression");

            loop.StepExactly(10);
            loop.WriteSnapshot(frame);
            double atpPerSecond = frame.Details[turnover][0] * loop.Schema.Details[turnover].DisplayMultiplier;
            Assert.That(atpPerSecond, Is.EqualTo(5.46e5).Within(0.01e5));
            Assert.That(frame.Details[made][0], Is.EqualTo(frame.Details[turnover][0]).Within(1e-9 * frame.Details[made][0]));
            Assert.That(frame.Details[build][0], Is.EqualTo(0.0));

            loop.Enqueue(SimCommands.SetTrait(0, trait, 3.0));
            loop.ApplyCommands();
            loop.StepExactly(100);
            loop.WriteSnapshot(frame);
            Assert.That(frame.TraitTargets[trait][0], Is.EqualTo(3.0f));
            Assert.That(frame.TraitValues[trait][0], Is.InRange(1.0f, 3.0f));
            Assert.That(frame.Details[build][0], Is.GreaterThan(0.0), "building the extra NhaA costs ATP");
        }

        [Test]
        public void BadTraitCommandIsRejectedNotFatal()
        {
            var loop = new SimLoop(new FermenterWorld(), new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.Enqueue(SimCommands.SetTrait(0, 99, 2.0));
            loop.ApplyCommands();
            Assert.That(loop.TryDequeueEvent(out var e), Is.True);
            Assert.That(e, Is.InstanceOf<CommandRejected>());
        }

        private static int Index(SimLoop loop, string id) => loop.Schema.Details.ToList().FindIndex(d => d.Id == id);
    }
}
