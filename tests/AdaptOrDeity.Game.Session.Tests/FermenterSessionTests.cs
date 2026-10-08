using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class FermenterSessionTests
    {
        private static SimLoop NewLoop(out FermenterWorld world)
        {
            world = new FermenterWorld();
            return new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
        }

        private static int Detail(SimLoop loop, string id) => loop.Schema.Details.ToList().FindIndex(d => d.Id == id);

        private static int Feature(string id) => GameFeatures.IndexOf(id);

        private static void FinishDaughter(FermenterWorld world, int cell)
        {
            var c = world.Cell(cell);
            c.Batch.States[c.Batch.Descriptor.StateIndex("bio__P_growth")][0] = c.DaughterCost;
        }

        [Test]
        public void SchemaOffersThePanelTraitsControlsAndFeatures()
        {
            var loop = NewLoop(out _);
            var schema = loop.Schema;
            Assert.That(schema.Details.Select(d => d.Group).Distinct(), Is.EquivalentTo(new[]
                { "Metabolic rate", "ATP made", "ATP used", "Protein turnover", "Growth and storage", "Cell state" }));
            Assert.That(schema.Traits.Count, Is.EqualTo(8));
            Assert.That(schema.Controls.Select(c => c.Id), Is.EqualTo(new[] { "growth", "storage", "repair" }));
            Assert.That(schema.MaxCells, Is.EqualTo(FermenterWorld.CellLimit));
            Assert.That(schema.Details.Single(d => d.Id == "atp_turnover").SiUnit, Is.EqualTo("mol/s"));
            Assert.That(schema.Details.Single(d => d.Id == "free_energy_power").SiUnit, Is.EqualTo("W"));
        }

        [Test]
        public void SnapshotCarriesTheEnergyBudget()
        {
            var loop = NewLoop(out _);
            var frame = loop.CreateFrame();
            loop.StepExactly(10);
            loop.WriteSnapshot(frame);
            int turnover = Detail(loop, "atp_turnover"), made = Detail(loop, "atp_glycolysis"), power = Detail(loop, "free_energy_power");
            Assert.That(frame.Details[turnover][0] * loop.Schema.Details[turnover].DisplayMultiplier, Is.EqualTo(5.46e5).Within(0.01e5));
            Assert.That(frame.Details[made][0], Is.EqualTo(frame.Details[turnover][0]).Within(1e-9 * frame.Details[made][0]));
            // ATP is the currency; its free energy (48.6 kJ/mol at rest) turns it into watts
            Assert.That(frame.Details[power][0] / frame.Details[turnover][0], Is.EqualTo(48.65e3).Within(0.1e3));
            Assert.That(frame.Level, Is.EqualTo(1));
            Assert.That(frame.CellAlive[0], Is.True);
        }

        [Test]
        public void TraitsApplyToEveryCellOfTheGroup()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            FinishDaughter(world, 0);
            loop.Enqueue(SimCommands.Divide(0));
            int trait = loop.Schema.Traits.ToList().FindIndex(t => t.Id == "NhaA_expression");
            loop.Enqueue(SimCommands.SetTrait(0, trait, 3.0));
            loop.ApplyCommands();
            Assert.That(world.CellCount, Is.EqualTo(2));
            Assert.That(world.ReadTraitTarget(0, trait), Is.EqualTo(3.0));
            Assert.That(world.ReadTraitTarget(1, trait), Is.EqualTo(3.0));
            Assert.That(world.CellGroup(1), Is.EqualTo(0));
        }

        [Test]
        public void GrowthAndDivisionUnlockAtLevelTwo()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetControl(0, FermenterWorld.GrowthControl, 1.0));
            loop.ApplyCommands();
            Assert.That(loop.TryDequeueEvent(out var e) && e is CommandRejected, Is.True, "locked at level 1");
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(0.0));

            loop.Enqueue(SimCommands.SetLevel(2));
            loop.Enqueue(SimCommands.SetControl(0, FermenterWorld.GrowthControl, 1.0));
            loop.ApplyCommands();
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(1.0).Within(1e-12));

            // Turning the feature off stops growth: the player always chooses.
            loop.Enqueue(SimCommands.SetFeature(Feature(GameFeatures.GrowthAndDivision), false));
            loop.ApplyCommands();
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(0.0));
        }

        [Test]
        public void DivideIsRejectedUntilTheDaughterIsBuilt()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.Enqueue(SimCommands.Divide(0));
            loop.ApplyCommands();
            Assert.That(loop.TryDequeueEvent(out var e) && e is CommandRejected r && r.Reason.Contains("built"), Is.True);
            FinishDaughter(world, 0);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.CellCanDivide[0], Is.True);
            loop.Enqueue(SimCommands.Divide(0));
            loop.ApplyCommands();
            Assert.That(world.CellCount, Is.EqualTo(2));
        }

        [Test]
        public void FixedBodySizeReplacesTheDeadInsteadOfGrowing()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(6));
            loop.ApplyCommands();
            FinishDaughter(world, 0);
            loop.Enqueue(SimCommands.Divide(0));
            loop.Enqueue(SimCommands.SetFeature(Feature(GameFeatures.FixedBodySize), true));
            loop.Enqueue(SimCommands.SetControl(0, FermenterWorld.GrowthControl, 0.5));
            loop.ApplyCommands();
            Assert.That(world.CellCount, Is.EqualTo(2));
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(0.0), "a full group spends growth on repair");

            // Kill cell 1; growth resumes and a finished daughter takes its place, not a new slot.
            var dying = world.Cell(1);
            dying.RepairLevel = 0.0;
            dying.Batch.States[dying.Batch.Descriptor.StateIndex("bio__D")][0] = 1.0;
            loop.StepExactly(2);
            Assert.That(world.CellAlive(1), Is.False);
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(0.5).Within(1e-12));
            FinishDaughter(world, 0);
            loop.Enqueue(SimCommands.Divide(0));
            loop.ApplyCommands();
            Assert.That(world.CellCount, Is.EqualTo(2));
            Assert.That(world.CellAlive(1), Is.True);
        }

        [Test]
        public void FeatureTableMatchesTheTimeline()
        {
            // content/timeline.json is the design record; GameFeatures must say the same.
            var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "global.json")))
                d = d.Parent;
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(d!.FullName, "content", "timeline.json")));
            var json = doc.RootElement.GetProperty("features").EnumerateArray().ToArray();
            Assert.That(json.Length, Is.EqualTo(GameFeatures.All.Count));
            for (int i = 0; i < json.Length; i++)
            {
                var f = GameFeatures.All[i];
                Assert.That(json[i].GetProperty("id").GetString(), Is.EqualTo(f.Id));
                Assert.That(json[i].GetProperty("name").GetString(), Is.EqualTo(f.Name));
                Assert.That(json[i].GetProperty("unlock_level").GetInt32(), Is.EqualTo(f.UnlockLevel));
                Assert.That(json[i].GetProperty("optional").GetBoolean(), Is.EqualTo(f.Optional));
                Assert.That(json[i].GetProperty("default_enabled").GetBoolean(), Is.EqualTo(f.DefaultEnabled));
            }
            foreach (var level in doc.RootElement.GetProperty("levels").EnumerateArray())
                foreach (var id in level.GetProperty("unlocks").EnumerateArray())
                    Assert.That(GameFeatures.All.Single(f => f.Id == id.GetString()).UnlockLevel,
                        Is.EqualTo(level.GetProperty("level").GetInt32()), id.GetString());
        }

        [Test]
        public void BadCommandsAreRejectedNotFatal()
        {
            var loop = NewLoop(out _);
            loop.Enqueue(SimCommands.SetTrait(0, 99, 2.0));
            loop.Enqueue(SimCommands.SetTrait(5, 0, 2.0));
            loop.Enqueue(SimCommands.SetFeature(Feature(GameFeatures.FixedBodySize), true));
            loop.ApplyCommands();
            for (int i = 0; i < 3; i++)
                Assert.That(loop.TryDequeueEvent(out var e) && e is CommandRejected, Is.True, $"command {i}");
        }
    }
}
