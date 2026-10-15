using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class FermenterSessionTests
    {
        internal static SimLoop NewLoop(out FermenterWorld world)
        {
            world = new FermenterWorld();
            return new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
        }

        private static int Detail(SimLoop loop, string id) => loop.Schema.Details.ToList().FindIndex(d => d.Id == id);

        private static int Feature(string id) => GameFeatures.IndexOf(id);

        internal static void FinishDaughter(FermenterWorld world, int cell)
        {
            var c = world.Cell(cell);
            c.SetState("bio__P_growth", c.DaughterCost);
        }

        [Test]
        public void SchemaOffersThePanelTraitsControlsAndFeatures()
        {
            var loop = NewLoop(out _);
            var schema = loop.Schema;
            Assert.That(schema.Details.Select(d => d.Group).Distinct(), Is.EquivalentTo(new[]
                { "Food", "Metabolic rate", "ATP made", "ATP used", "Protein turnover", "Growth and storage", "Cell state" }));
            Assert.That(schema.Traits.Count, Is.EqualTo(13), "8 expression traits, the number of flagella, 3 carriers to evolve and the surface hydrolase");
            Assert.That(schema.Traits.Where(t => t.IsShare).Select(t => t.Feature), Is.EqualTo(new[]
                { GameFeatures.PtsTransport, GameFeatures.AbcTransport, GameFeatures.ProtonSymport }));
            Assert.That(schema.Controls.Select(c => c.Id), Is.EqualTo(new[] { "growth", "storage", "repair", "matrix" }));
            Assert.That(schema.InitialCellCapacity, Is.EqualTo(FermenterWorld.InitialCells));
            Assert.That(schema.Details.Single(d => d.Id == "atp_turnover").SiUnit, Is.EqualTo("mol/s"));
            Assert.That(schema.Details.Single(d => d.Id == "free_energy_power").SiUnit, Is.EqualTo("W"));
        }

        [Test]
        public void SnapshotCarriesTheEnergyBudget()
        {
            var loop = NewLoop(out var world);
            world.BathConcentration = 0.1;   // the medium the cell's resting budget is calibrated in (0.1 mM)
            loop.InspectedCell = 0;
            var frame = loop.CreateFrame();
            loop.StepExactly(10);
            loop.WriteSnapshot(frame);
            int turnover = Detail(loop, "atp_turnover"), made = Detail(loop, "atp_glycolysis"), power = Detail(loop, "free_energy_power");
            Assert.That(frame.Details[turnover][0] * loop.Schema.Details[turnover].DisplayMultiplier, Is.EqualTo(5.46e5).Within(0.01e5));
            // at rest ATP made balances ATP used (to 1e-3: the cell's inputs still follow its own depletion of the water)
            Assert.That(frame.Details[made][0], Is.EqualTo(frame.Details[turnover][0]).Within(1e-3 * frame.Details[made][0]));
            // ATP is the currency; its free energy (48.6 kJ/mol at rest) turns it into watts
            Assert.That(frame.Details[power][0] / frame.Details[turnover][0], Is.EqualTo(48.65e3).Within(0.1e3));
            Assert.That(frame.Level, Is.EqualTo(1));
            Assert.That(frame.CellAlive[0], Is.True);
        }

        [Test]
        public void EnergyBalanceBarsAddUpToTheCellsAtpMadeAndUsed()
        {
            var loop = NewLoop(out var world);
            loop.InspectedCell = 0;
            var frame = loop.CreateFrame();
            loop.StepExactly(60);
            loop.WriteSnapshot(frame);
            var details = loop.Schema.Details;
            double Sum(string group) => Enumerable.Range(0, details.Count)
                .Where(d => details[d].Group == group && details[d].Id != "atp_net").Sum(d => frame.Details[d][0]);
            var budget = world.Cell(0).Budget();
            Assert.That(Sum("ATP made"), Is.EqualTo(budget.AtpProduced).Within(1e-9 * budget.AtpProduced));
            Assert.That(Sum("ATP used"), Is.EqualTo(budget.AtpConsumed).Within(1e-9 * budget.AtpConsumed));
            int glucose = Detail(loop, "glucose_inside");
            Assert.That(frame.Details[glucose][0], Is.EqualTo(world.Cell(0).State("cytosol__C_glc")));
        }

        [Test]
        public void TheCreaturesDamageBarHasItsPartsAndAWorstMarkRepairDoesNotLower()
        {
            var loop = NewLoop(out var world);
            world.BathConcentration = 1.0;   // food enough to pay for the repair
            var cell = world.Cell(0);
            cell.AddPhysicalDamage(0.3);
            loop.StepExactly(10);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.CreatureDamage[0], Is.EqualTo((float)cell.DamagePhysical).Within(1e-6f), "physical");
            Assert.That(frame.CreatureDamage[2], Is.EqualTo((float)cell.DamageRibosome).Within(1e-6f), "ribosome (wear)");
            Assert.That(frame.CreatureDamage.Sum(), Is.EqualTo((float)cell.Damage).Within(1e-6f));
            float worst = frame.CreatureDamageWorst;
            Assert.That(worst, Is.EqualTo((float)cell.Damage).Within(1e-4f));
            loop.StepExactly(6000);   // ten minutes of repair
            loop.WriteSnapshot(frame);
            Assert.That(frame.CreatureDamage.Sum(), Is.LessThan(worst - 0.003f), "repair mends the damage");
            Assert.That(frame.CreatureDamageWorst, Is.EqualTo(worst).Within(1e-6f), "but not the record");
            Assert.That(frame.PathwayEnded, Is.False);
        }

        [Test]
        public void WhenEveryCellHasDiedThePathwayEndsAndTimeStops()
        {
            var loop = NewLoop(out var world);
            world.Cell(0).SetState("bio__D_energy", 0.6);
            world.Cell(0).SetState("bio__D_ribo", 0.6);   // 1.2 of a lethal load: half and half
            loop.StepExactly(2);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(world.PathwayEnded, Is.True);
            Assert.That(frame.PathwayEnded, Is.True);
            // the bar shows what killed it, as shares of the cells' damage at death
            Assert.That(frame.CreatureDamage[3], Is.EqualTo(0.6f / 1.2f).Within(0.01f), "de-energisation");
            Assert.That(frame.CreatureDamage[2], Is.EqualTo(0.6f / 1.2f).Within(0.01f), "ribosome");
            Assert.That(frame.CreatureDamage.Sum(), Is.EqualTo(1f).Within(1e-5f));
            double t = frame.SimTime;
            loop.StepExactly(100);
            loop.WriteSnapshot(frame);
            Assert.That(frame.SimTime, Is.EqualTo(t), "time stops");
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var copy = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, copy);
            Assert.That((copy.PathwayEnded, copy.CreatureDamage[3], copy.CreatureDamageWorst),
                Is.EqualTo((true, frame.CreatureDamage[3], 1f)));
        }

        private static SimLoop Swimming(int cells, out FermenterWorld world)
        {
            var loop = NewLoop(out world);
            loop.Enqueue(SimCommands.SetLevel(2));
            Colonies.Multicellular(loop.Enqueue);
            loop.ApplyCommands();
            while (world.CellCount < cells)
            {
                FinishDaughter(world, world.CellCount - 1);
                loop.Enqueue(SimCommands.Divide(world.CellCount - 1));
                loop.ApplyCommands();
            }
            loop.StepExactly(300);   // runs and turns
            return loop;
        }

        [Test]
        public void FramePositionsTurnedWithTheBodyAreTheCellsOnTheGround()
        {
            var loop = Swimming(5, out var world);
            Assert.That(world.Body.Body.Angle, Is.Not.EqualTo(0.0), "the body should have turned");
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            double c = Math.Cos(frame.Orientation), s = Math.Sin(frame.Orientation);
            for (int k = 0; k < world.CellCount; k++)
            {
                world.CellPosition(k, out double x, out double y);
                double fx = frame.CreatureX + c * frame.CellX[k] - s * frame.CellY[k];
                double fy = frame.CreatureY + s * frame.CellX[k] + c * frame.CellY[k];
                Assert.That(fx, Is.EqualTo(x).Within(1e-9), $"cell {k} x");
                Assert.That(fy, Is.EqualTo(y).Within(1e-9), $"cell {k} y");
            }
        }

        [Test]
        public void StepsAndFramesComputeOnlyTheKernelsOutputs()
        {
            // Every cell's whole set of algebraic variables costs ~6x its outputs: a step, and a frame with no cell
            // inspected, must not need them.
            var loop = Swimming(4, out var world);
            loop.StepExactly(1);
            loop.WriteSnapshot(loop.CreateFrame());
            for (int k = 0; k < world.CellCount; k++)
                Assert.That(world.Cell(k).AllVariablesCurrent, Is.False, $"cell {k}");
        }

        [Test]
        public void FramesCarryDetailsForTheInspectedCellOnly()
        {
            var loop = Swimming(3, out var world);
            loop.Enqueue(SimCommands.Inspect(1));
            loop.ApplyCommands();
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            int atp = Detail(loop, "atp_turnover");
            Assert.That(frame.Details[atp][1], Is.EqualTo(world.ReadDetail(1, atp)));
            Assert.That(frame.Details[atp][1], Is.Not.EqualTo(0.0));
            Assert.That(frame.Details[atp][0], Is.EqualTo(0.0));
            Assert.That(frame.Details[atp][2], Is.EqualTo(0.0));
        }

        [Test]
        public void ThreadedFramesGrowWithTheColony()
        {
            using var session = SimSession.CreateFermenter();
            session.Enqueue(SimCommands.SetLevel(2));
            Colonies.Multicellular(session.Enqueue);
            for (int i = 1; i < 150; i++)
            {
                session.Enqueue(SimCommands.DevCompleteDaughter(i - 1));
                session.Enqueue(SimCommands.Divide(i - 1));
            }
            session.Start();
            SnapshotFrame? frame = null;
            for (int tries = 0; tries < 200 && (frame == null || frame.CellCount < 150); tries++)
            {
                System.Threading.Thread.Sleep(10);
                if (session.TryGetFrame(out var f))
                    frame = f;
            }
            Assert.That(session.Fault, Is.Null);
            Assert.That(frame!.CellCount, Is.EqualTo(150));
            Assert.That(frame.CellX.Length, Is.GreaterThanOrEqualTo(150));
        }

        [Test]
        public void TraitsApplyToEveryCellOfTheGroup()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(2));
            Colonies.Multicellular(loop.Enqueue);
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
            Colonies.Multicellular(loop.Enqueue);
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
            loop.Enqueue(SimCommands.SetLevel(7));
            Colonies.Multicellular(loop.Enqueue);
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
            dying.SetState("bio__D_ribo", 1.0);
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
                Assert.That(json[i].TryGetProperty("cost", out var cost) ? cost.GetInt32() : 0, Is.EqualTo(f.Cost), f.Id);
                Assert.That(f.Explanation, Is.EqualTo(json[i].GetProperty("explanation").GetString()), f.Id);
                Assert.That(f.Requires, Is.EqualTo(Strings(json[i], "requires")), f.Id + " requires");
                Assert.That(f.Excludes, Is.EqualTo(Strings(json[i], "excludes")), f.Id + " excludes");
                Assert.That(json[i].TryGetProperty("requires_swallowed", out var n) ? n.GetInt32() : 0, Is.EqualTo(f.RequiresSwallowed),
                    f.Id + " requires_swallowed");
                Assert.That(json[i].TryGetProperty("requires_food_gap_s", out var gap) ? gap.GetDouble() : 0.0, Is.EqualTo(f.RequiresFoodGap),
                    f.Id + " requires_food_gap_s");
                foreach (var other in f.Excludes)
                    Assert.That(GameFeatures.All[GameFeatures.IndexOf(other)].Excludes, Does.Contain(f.Id), $"{f.Id} and {other} exclude each other");
            }
            foreach (var level in doc.RootElement.GetProperty("levels").EnumerateArray())
                foreach (var id in level.GetProperty("unlocks").EnumerateArray())
                    Assert.That(GameFeatures.All.Single(f => f.Id == id.GetString()).UnlockLevel,
                        Is.EqualTo(level.GetProperty("level").GetInt32()), id.GetString());
        }

        private static string[] Strings(JsonElement e, string name) =>
            e.TryGetProperty(name, out var a) ? a.EnumerateArray().Select(x => x.GetString()!).ToArray() : new string[0];

        [Test]
        public void AtTopSpeedFramesKeepComingInSmallSteps()
        {
            // At 1000x the simulation cannot keep up (one cell runs ~800x): each tick has a wall-clock budget, so frames
            // still come many times a second, each a small step of simulated time, instead of a jump every half second.
            using var session = SimSession.CreateFermenter();
            session.Start();
            session.Enqueue(SimCommands.SetSpeed(FermenterWorld.SpeedPresets.Length - 1));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frames = 0;
            double last = -1.0, biggestJump = 0.0;
            while (watch.Elapsed.TotalSeconds < 2.0)
            {
                if (session.TryGetFrame(out var f))
                {
                    if (last >= 0.0)
                        biggestJump = Math.Max(biggestJump, f.SimTime - last);
                    last = f.SimTime;
                    frames++;
                }
                // poll every 5 ms as a front end's frame loop does; Thread.Sleep(5) can take ~45 ms on macOS (timer
                // coalescing), which would measure this thread's sleep rather than the simulation's frames
                var poll = watch.Elapsed + TimeSpan.FromMilliseconds(5);
                while (watch.Elapsed < poll)
                    System.Threading.Thread.Yield();
            }
            Assert.That(session.Fault, Is.Null);
            Assert.That(frames, Is.GreaterThan(20), "frames per 2 s");
            Assert.That(biggestJump, Is.LessThan(60.0), "simulated seconds between frames");
            Assert.That(last, Is.GreaterThan(200.0), "it still runs fast");
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
