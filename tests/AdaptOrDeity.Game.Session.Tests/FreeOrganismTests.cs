using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Daughters released at division without multicellularity live on as free organisms: the daughter's cell as divided,
    /// a body that swims and drifts as the creature's does, eating, engulfing, dying and dividing by the creature's rules,
    /// carried in frames, and no longer simulated out of range or beyond the cap.
    /// </summary>
    public class FreeOrganismTests
    {
        private static int Feature(string id) => GameFeatures.IndexOf(id);

        // A level-2 world without food or prey arriving: what a test puts in is all there is.
        private static FermenterWorld Quiet(bool swimming = false, bool engulfing = false, bool? environmentField = false,
            int workers = 1)
        {
            var world = new FermenterWorld(environmentField: environmentField) { Workers = workers };
            world.Level = 2;
            world.SetFeature(Feature(GameFeatures.Swimming), swimming);
            world.SetFeature(Feature(GameFeatures.Engulfing), engulfing);
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            return world;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<FermenterWorld, StrongBox> Clocks =
            new System.Runtime.CompilerServices.ConditionalWeakTable<FermenterWorld, StrongBox>();

        private sealed class StrongBox
        {
            public long Steps;
        }

        // Steps the world on from where it is (simulated time carries on across calls).
        private static void Run(FermenterWorld world, double seconds)
        {
            var clock = Clocks.GetOrCreateValue(world);
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++, clock.Steps++)
                world.Step(clock.Steps * world.MacroDt, world.MacroDt);
        }

        // The creature's cell 0 divides: its daughter is released and returned.
        private static FreeOrganism Release(FermenterWorld world)
        {
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            return world.Organisms[world.Organisms.Count - 1];
        }

        // A fresh packet of this radius, still, centred at (x, y), in the first packet slot.
        private static FoodPacket PacketAt(FermenterWorld world, double x, double y, double radius, int slot = 0)
        {
            var packet = world.Food.Packets[slot];
            var s = world.Food.Settings;
            foreach (var (name, value) in new[] { ("Active", (object)true), ("Stuck", false), ("Holder", 0), ("X", x), ("Y", y),
                         ("Vx", 0.0), ("Vy", 0.0), ("Radius", radius), ("Glucose", s.SphereAmount(radius)),
                         ("Free", s.FreeFraction * s.SphereAmount(radius)) })
                typeof(FoodPacket).GetProperty(name)!.SetValue(packet, value);
            return packet;
        }

        [Test]
        public void AReleasedDaughterKeepsItsStateAndLivesOnAsItsParentWould()
        {
            using var world = Quiet();
            double perOffspring = GameParameters.Default.Value("skills.points_per_offspring");
            var parent = world.Cell(0);
            var o = Release(world);
            Assert.That(world.CellCount, Is.EqualTo(1), "the daughter left the creature");
            Assert.That(world.Organisms, Has.Count.EqualTo(1), "and lives on its own");
            Assert.That(world.Offspring, Is.EqualTo(1));
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(perOffspring)));
            Assert.That((o.Id, o.Parent, o.Generation), Is.EqualTo((1, 0, 1)));
            // the division: both halves keep the concentrations, traits, controls and solver settings
            var d = o.Cell;
            for (int s = 0; s < d.Batch.States.Length; s++)
                Assert.That(d.Batch.States[s][0], Is.EqualTo(parent.Batch.States[s][0]), $"state {s}");
            for (int e = 0; e < d.Batch.Externals.Length; e++)
                Assert.That(d.Batch.Externals[e][0], Is.EqualTo(parent.Batch.Externals[e][0]), $"external {e}");
            Assert.That(d.RelativeTolerance, Is.EqualTo(parent.RelativeTolerance));
            Assert.That(d.StopAtMacroSteps, Is.EqualTo(parent.StopAtMacroSteps));
            Assert.That(d.Time, Is.EqualTo(parent.Time));
            // A cell beside its parent in the same water, with the same controls: it goes on exactly as its parent does.
            double atp0 = d.State("cytosol__C_ATP");
            Run(world, 600.0);
            Assert.That(d.Time, Is.EqualTo(parent.Time), "it integrated from its release on");
            Assert.That(d.State("cytosol__C_ATP"), Is.Not.EqualTo(atp0), "its ATP moved on from release");
            for (int s = 0; s < d.Batch.States.Length; s++)
                Assert.That(d.Batch.States[s][0], Is.EqualTo(parent.Batch.States[s][0]), $"state {s} after 10 min");
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(perOffspring)), "still one daughter's points");
        }

        [Test]
        public void ItSwimsByRunAndTumbleAndDriftsWithTheCurrent()
        {
            using var world = Quiet(swimming: true);
            var swimmer = Release(world);
            world.SetFeature(Feature(GameFeatures.Swimming), false);
            var drifter = Release(world);
            Assert.That((swimmer.Swims, drifter.Swims), Is.EqualTo((true, false)), "each keeps what it was released with");
            var b0 = (swimmer.X, swimmer.Y, drifter.X, drifter.Y);
            var water = world.Body.Body;
            Assert.That((drifter.Body.Body.CurrentVx, drifter.Body.Body.CurrentVy), Is.EqualTo((water.CurrentVx, water.CurrentVy)));
            int switches = 0;
            bool on = swimmer.MotorOn;
            const double seconds = 120.0;
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
            {
                world.Step(k * world.MacroDt, world.MacroDt);
                if (swimmer.MotorOn != on)
                {
                    switches++;
                    on = swimmer.MotorOn;
                }
            }
            double t = steps * world.MacroDt;
            Assert.That(drifter.X - b0.Item3, Is.EqualTo(water.CurrentVx * t).Within(1e-9 * Math.Abs(water.CurrentVx * t) + 1e-15),
                "without swimming it moves with the water");
            Assert.That(drifter.Y - b0.Item4, Is.EqualTo(water.CurrentVy * t).Within(1e-9 * Math.Abs(water.CurrentVy * t) + 1e-15));
            Assert.That(switches, Is.GreaterThan(4), "runs and pauses");
            Assert.That(swimmer.Body.Body.Work, Is.GreaterThan(0.0), "its flagella pushed it");
            double sx = swimmer.X - b0.Item1 - water.CurrentVx * t, sy = swimmer.Y - b0.Item2 - water.CurrentVy * t;
            Assert.That(Math.Sqrt(sx * sx + sy * sy), Is.GreaterThan(0.1 * world.CellRadius), "it swam through the water");
        }

        [Test]
        public void ItEatsThePacketsItTouchesSharingThemConservativelyWithTheCreature()
        {
            using var world = Quiet();
            var o = Release(world);
            world.CellPosition(0, out double cx, out double cy);
            // one packet larger than a cell between the two, touching both
            var shared = PacketAt(world, 0.5 * (cx + o.X), 0.5 * (cy + o.Y), 1.2 * world.CellRadius);
            double glucose0 = shared.Glucose, eaten0 = world.FoodEaten;
            Run(world, 60.0);
            double creature = world.FoodEaten - eaten0, organism = o.Eaten;
            Assert.That(creature, Is.GreaterThan(0.0));
            Assert.That(organism, Is.GreaterThan(0.0), "the organism ate from the packet too");
            Assert.That(world.OrganismsEaten, Is.EqualTo(organism));
            Assert.That(glucose0 - shared.Glucose, Is.EqualTo(creature + organism).Within(1e-12 * glucose0),
                "what the packet lost is what the two ate");
            Assert.That(world.FoodPoints, Is.EqualTo((int)Math.Floor(world.FoodEaten / GameParameters.Default.Value(
                "skills.glucose_per_point"))), "the organism's food earns the player nothing");
            Assert.That((shared.Stuck, shared.Holder), Is.EqualTo((true, 0)), "the creature's cell met it first");

            // a packet only the organism touches stays with the organism
            world.Food.Remove(shared);
            var b = o.Body.Body;
            double ax = Math.Cos(b.Angle + Math.PI), ay = Math.Sin(b.Angle + Math.PI);
            var own = PacketAt(world, o.X + 1.2 * world.CellRadius * ax, o.Y + 1.2 * world.CellRadius * ay,
                0.6 * world.CellRadius, slot: 1);
            Run(world, 0.2);
            Assert.That((own.Stuck, own.Holder), Is.EqualTo((true, o.Id)));
            double dx = own.X - o.X, dy = own.Y - o.Y;
            Run(world, 30.0);
            Assert.That(own.X - o.X, Is.EqualTo(dx).Within(1e-12), "it moves with the organism");
            Assert.That(own.Y - o.Y, Is.EqualTo(dy).Within(1e-12));
        }

        [Test]
        public void WithEngulfingItSwallowsPreyAndPacketsAsTheCreatureDoes()
        {
            using var world = Quiet(engulfing: true);
            var o = Release(world);
            world.SetFeature(Feature(GameFeatures.Engulfing), false);   // the creature stops; the organism keeps it
            world.Prey.Add(o.X, o.Y, 0.0);
            var packet = PacketAt(world, o.X, o.Y, world.Food.Settings.PacketRadius);
            double amount = packet.Glucose + world.Prey.Content;
            Run(world, 0.2);
            Assert.That(o.Swallowed, Is.EqualTo(1));
            Assert.That(world.Swallowed, Is.EqualTo(0), "the creature's count is its own");
            Assert.That(packet.Active, Is.False);
            Assert.That(o.Engulfed, Is.EqualTo(amount).Within(1e-12 * amount));
            Run(world, 3600.0);
            double held = o.Vacuole?.Glucose ?? 0.0;
            Assert.That(o.EatenFromVacuole, Is.GreaterThan(0.0), "it digests and takes up what it swallowed");
            Assert.That(o.EatenFromVacuole + o.Egested + held, Is.EqualTo(amount).Within(1e-12 * amount),
                "engulfed = taken up + egested + still held");
            Assert.That(world.EatenFromVacuoles, Is.EqualTo(0.0));
        }

        [Test]
        public void ItCanDieAndIsRemovedAfterAWhile()
        {
            using var world = Quiet();
            var o = Release(world);
            var packet = PacketAt(world, o.X, o.Y, 0.5 * world.CellRadius);
            Run(world, 0.2);
            Assert.That((packet.Stuck, packet.Holder), Is.EqualTo((true, o.Id)));
            o.Cell.SetState("bio__D", 1.1);   // a lethal damage load
            Run(world, 0.1);
            Assert.That(o.Alive, Is.False);
            Assert.That(world.OrganismsDied, Is.EqualTo(1));
            Assert.That(packet.Stuck, Is.False, "what it held lies where it is");
            Assert.That(world.Organisms, Does.Contain(o), "a dead organism stays a while");
            double kept = GameParameters.Default.Value("organisms.dead_kept");
            Run(world, kept - 1.0);
            Assert.That(world.Organisms, Does.Contain(o));
            Run(world, 1.5);
            Assert.That(world.Organisms, Is.Empty, "then it is removed");
            Assert.That(world.OrganismsGone, Is.EqualTo(0), "removed as dead, not gone out of range");
        }

        [Test]
        public void ItDividesWhenItsInheritedSettingsAllowAndItsDaughtersEarnNothing()
        {
            using var world = Quiet();
            int points = world.ReproductionPoints;
            var plain = Release(world);   // released without automatic division
            world.SetFeature(Feature(GameFeatures.AutoDivide), true);
            world.SetControl(0, FermenterWorld.GrowthControl, 3.0);
            var dividing = Release(world);
            int reproduction = world.ReproductionPoints;
            Assert.That(reproduction, Is.GreaterThan(points));
            Assert.That((plain.AutoDivides, dividing.AutoDivides), Is.EqualTo((false, true)));
            Assert.That(dividing.Cell.GrowthFraction, Is.EqualTo(world.Cell(0).GrowthFraction), "the group's growth control");
            double growth = dividing.Cell.GrowthFraction;
            world.SetControl(0, FermenterWorld.GrowthControl, 0.0);   // the player's change reaches only the creature
            Assert.That(world.Cell(0).GrowthFraction, Is.EqualTo(0.0));
            Assert.That(dividing.Cell.GrowthFraction, Is.EqualTo(growth).And.GreaterThan(0.0));
            foreach (var o in new[] { plain, dividing })
                o.Cell.SetState("bio__P_growth", o.Cell.DaughterCost);
            Run(world, 0.1);
            Assert.That(world.Organisms, Has.Count.EqualTo(3));
            var grandchild = world.Organisms[2];
            Assert.That((grandchild.Parent, grandchild.Generation), Is.EqualTo((dividing.Id, 2)));
            Assert.That(dividing.Daughters, Is.EqualTo(1));
            Assert.That(plain.Daughters, Is.EqualTo(0), "released without automatic division: it never divides by itself");
            Assert.That(grandchild.AutoDivides, Is.True, "its daughter divides as it does");
            Assert.That(world.Offspring, Is.EqualTo(2), "offspring counts the creature's own daughters");
            Assert.That(world.ReproductionPoints, Is.EqualTo(reproduction), "and only they earn points");
            Assert.That(world.OrganismsReleased, Is.EqualTo(3));
        }

        [Test]
        public void OrganismsOutOfRangeOrBeyondTheCapAreNoLongerSimulated()
        {
            using var world = Quiet();
            var o = Release(world);
            var near = Release(world);
            var b = world.Body.Body;
            o.Body.Body.Place(b.X + 1.01 * world.OrganismRange, b.Y, 0.0);
            near.Body.Body.Place(b.X + 0.99 * world.OrganismRange, b.Y - 0.99 * world.OrganismRange, 0.0);
            Run(world, 0.1);
            Assert.That(world.Organisms, Is.EquivalentTo(new[] { near }));
            Assert.That(world.OrganismsGone, Is.EqualTo(1));
            Assert.That(world.OrganismRange, Is.EqualTo(2.0 * 2.0 * world.Food.HalfSize), "two view widths");

            // at the cap a new daughter replaces the farthest
            while (world.OrganismsReleased < world.MaxOrganisms + 2)
                Release(world);
            Assert.That(world.Organisms, Has.Count.EqualTo(world.MaxOrganisms));
            Assert.That(world.Organisms, Does.Not.Contain(near), "the farthest went");
            Assert.That(world.OrganismsGone, Is.EqualTo(2));
            // and a dead one before any living one
            world.Organisms[5].Cell.SetState("bio__D", 1.1);
            Run(world, 0.1);
            var dead = world.Organisms[5];
            Assert.That(dead.Alive, Is.False);
            Release(world);
            Assert.That(world.Organisms, Does.Not.Contain(dead));
            Assert.That(world.Organisms.All(x => x.Alive), Is.True);
        }

        [Test]
        public void WithTheEnvironmentFieldItExchangesWithTheFieldConservatively()
        {
            using var world = Quiet(environmentField: true);
            var o = Release(world);
            var env = world.Environment!;
            const int g = EnvironmentField.Glucose, lac = EnvironmentField.Lactate;
            double sourced0 = env.Field.Sourced(g), packets0 = env.PacketGlucose, cells0 = world.CellGlucoseExchanged;
            Run(world, 60.0);
            double sourced = env.Field.Sourced(g) - sourced0;
            double packets = env.PacketGlucose - packets0, cells = world.CellGlucoseExchanged - cells0;
            Assert.That(sourced, Is.EqualTo(packets - cells).Within(1e-12 * (packets + Math.Abs(cells))),
                "the creature's and the organism's exchange entered the field exactly");
            Assert.That(o.Cell.MembraneLactate, Is.GreaterThan(0.0), "it sees the lactate it releases");
            Assert.That(env.Field.Sample(lac, o.X, o.Y), Is.GreaterThan(0.0), "its lactate is in the water around it");
            Assert.That(env.Field.LevelAt(o.X + world.OrganismRange, o.Y + world.OrganismRange), Is.GreaterThanOrEqualTo(0),
                "the field covers every organism in range");
        }

        [Test]
        public void ResultsAreTheSameOnAnyNumberOfThreads()
        {
            double[]? reference = null;
            foreach (int workers in new[] { 1, 4, 8 })
            {
                // two swimmers that leave in a minute or two, five that drift with the creature, one of them dividing
                using var world = Quiet(swimming: true, engulfing: true, workers: workers);
                world.SetFeature(Feature(GameFeatures.AutoDivide), true);
                world.SetControl(0, FermenterWorld.GrowthControl, 3.0);
                world.Food.SetArrivals(0.05, 0.8);
                world.Prey.SetArrivals(0.2);
                for (int k = 0; k < 7; k++)
                {
                    if (k == 2)
                        world.SetFeature(Feature(GameFeatures.Swimming), false);
                    Release(world);
                }
                world.Organisms[2].Cell.SetState("bio__P_growth", world.Organisms[2].Cell.DaughterCost);
                Run(world, 120.0);
                var print = world.Organisms.SelectMany(o => new[]
                {
                    o.Id, o.X, o.Y, o.Body.Body.Angle, o.Cell.State("cytosol__C_ATP"), o.Cell.MembraneGlucose, o.Eaten,
                    o.Engulfed, o.Vacuole?.Glucose ?? -1.0,
                }).Concat(new[]
                {
                    world.OrganismsReleased, world.OrganismsEaten, world.OrganismsEngulfed, world.FoodEaten,
                    world.Cell(0).State("cytosol__C_ATP"), world.Body.Body.X,
                }).ToArray();
                if (reference == null)
                {
                    Assert.That(world.Organisms.Any(o => o.Generation == 2), Is.True, "an organism divided");
                    reference = print;
                    continue;
                }
                Assert.That(print.Length, Is.EqualTo(reference.Length), $"{workers} workers");
                for (int i = 0; i < print.Length; i++)
                    Assert.That(BitConverter.DoubleToInt64Bits(print[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(reference[i])),
                        $"value {i} differs with {workers} workers");
            }
        }

        [Test]
        public void FramesAndTheCodecCarryTheOrganismsAndGrowWithThem()
        {
            using var world = Quiet(swimming: true);
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var frame = loop.CreateFrame();
            int capacity = frame.OrganismX.Length;
            Assert.That(capacity, Is.EqualTo(GameParameters.Default.Integer("organisms.initial_capacity")));
            var o = Release(world);
            loop.StepExactly(50);
            o.Cell.SetState("bio__D", 1.1);
            while (world.Organisms.Count <= capacity)
                Release(world);
            loop.StepExactly(1);
            Assert.That(loop.Outgrown(frame), Is.True, "more organisms than the frame has room for");
            frame = loop.CreateFrame();
            var second = world.Organisms[1];
            loop.Enqueue(SimCommands.InspectOrganism(second.Id));
            loop.ApplyCommands();
            loop.WriteSnapshot(frame);
            Assert.That(frame.OrganismCount, Is.EqualTo(world.Organisms.Count));
            Assert.That(frame.OrganismId[0], Is.EqualTo(o.Id));
            Assert.That(frame.OrganismAlive[0], Is.False);
            Assert.That(frame.OrganismAlive[1], Is.True);
            Assert.That(frame.OrganismX[1], Is.EqualTo((float)(second.X - world.Body.Body.X)));
            Assert.That(frame.OrganismY[1], Is.EqualTo((float)(second.Y - world.Body.Body.Y)));
            Assert.That(frame.OrganismOrientation[1], Is.EqualTo((float)second.Body.Body.Angle));
            Assert.That(frame.OrganismGeneration[1], Is.EqualTo(1));
            Assert.That(frame.OrganismFlagella[1], Is.EqualTo((float)world.FlagellaOf(second)));
            Assert.That(frame.OrganismFlagellumAngles[1][0], Is.EqualTo((float)second.Body.FlagellumAngle(0, 0)));
            int atp = world.Overlays.ToList().FindIndex(c => c.Id == "atp");
            Assert.That(frame.OrganismChannels[atp][1], Is.EqualTo((float)second.Cell.State("cytosol__C_ATP")));
            Assert.That(frame.InspectedOrganism, Is.EqualTo(second.Id));
            int atpDetail = loop.Schema.Details.ToList().FindIndex(d => d.Id == "atp");
            Assert.That(frame.OrganismDetails[atpDetail], Is.EqualTo(second.Cell.State("cytosol__C_ATP")));
            Assert.That((frame.OrganismsReleased, frame.OrganismsDied), Is.EqualTo((world.OrganismsReleased, 1)));

            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            Assert.That(SnapshotCodec.Fits(bytes, loop.CreateFrame()), Is.True);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.OrganismCount, Is.EqualTo(frame.OrganismCount));
            Assert.That(back.InspectedOrganism, Is.EqualTo(frame.InspectedOrganism));
            Assert.That((back.OrganismsReleased, back.OrganismsDied, back.OrganismsGone),
                Is.EqualTo((frame.OrganismsReleased, frame.OrganismsDied, frame.OrganismsGone)));
            Assert.That(back.OrganismId, Is.EqualTo(frame.OrganismId));
            Assert.That(back.OrganismGeneration, Is.EqualTo(frame.OrganismGeneration));
            Assert.That(back.OrganismX, Is.EqualTo(frame.OrganismX));
            Assert.That(back.OrganismY, Is.EqualTo(frame.OrganismY));
            Assert.That(back.OrganismOrientation, Is.EqualTo(frame.OrganismOrientation));
            Assert.That(back.OrganismAlive, Is.EqualTo(frame.OrganismAlive));
            Assert.That(back.OrganismMotorOn, Is.EqualTo(frame.OrganismMotorOn));
            Assert.That(back.OrganismFlagella, Is.EqualTo(frame.OrganismFlagella));
            Assert.That(back.OrganismVacuoleRadius, Is.EqualTo(frame.OrganismVacuoleRadius));
            Assert.That(back.OrganismDetails, Is.EqualTo(frame.OrganismDetails));
            for (int k = 0; k < frame.OrganismCount; k++)
                Assert.That(back.OrganismFlagellumAngles[k], Is.EqualTo(frame.OrganismFlagellumAngles[k]));
            for (int c = 0; c < frame.OrganismChannels.Length; c++)
                Assert.That(back.OrganismChannels[c], Is.EqualTo(frame.OrganismChannels[c]));
        }
    }
}
