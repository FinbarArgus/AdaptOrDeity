using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Daughters released at division without multicellularity live on as free organisms: the daughter's cell as divided,
    /// a body that swims and drifts as the creature's does, eating, engulfing, dying (its body becomes food) and dividing by
    /// the creature's rules (every daughter in the lineage earns points), carried in frames, and frozen out of range or
    /// beyond the cap until the creature comes back.
    /// </summary>
    public class FreeOrganismTests
    {
        private static int Feature(string id) => GameFeatures.IndexOf(id);

        // A level-2 world without food or prey arriving: what a test puts in is all there is.
        private static FermenterWorld Quiet(bool swimming = false, bool engulfing = false, int workers = 1)
        {
            var world = new FermenterWorld { Workers = workers };
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
            // A cell beside its parent in the same water, with the same controls: it goes on as its parent does (to 1e-5:
            // each sees the water at its own place, with the other's depletion a little different there).
            double atp0 = d.State("cytosol__C_ATP");
            Run(world, 600.0);
            Assert.That(d.Time, Is.EqualTo(parent.Time), "it integrated from its release on");
            Assert.That(d.State("cytosol__C_ATP"), Is.Not.EqualTo(atp0), "its ATP moved on from release");
            // (the recycled amino acids, a count per cell that is 0 at rest up to round-off, to within one amino acid)
            int aminoAcids = d.Batch.Descriptor.StateIndex("protein__N_aa");
            for (int s = 0; s < d.Batch.States.Length; s++)
                Assert.That(d.Batch.States[s][0], Is.EqualTo(parent.Batch.States[s][0])
                    .Within(1e-5 * Math.Abs(parent.Batch.States[s][0]) + (s == aminoAcids ? 1.0 : 1e-15)),
                    $"state {s} after 10 min");
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
            // the tide: x(t) = sum over the steps of U0 cos(2 pi t_k / T) dt (the current is held over each step)
            double amp = GameParameters.Default.Value("environment.current_speed"), period = GameParameters.Default.Value("environment.tide_period");
            double drift = 0.0;
            for (int k = 0; k < steps; k++)
                drift += amp * Math.Cos(2.0 * Math.PI * k * world.MacroDt / period) * world.MacroDt;
            Assert.That(drifter.X - b0.Item3, Is.EqualTo(drift).Within(1e-9 * Math.Abs(drift) + 1e-15),
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
            double glucose0 = shared.Glucose, eaten0 = world.FoodEaten, leaked0 = world.Environment.PacketGlucose;
            Run(world, 60.0);
            double creature = world.EatenFromPackets, organism = world.OrganismsEatenFromPackets;
            Assert.That(creature, Is.GreaterThan(0.0));
            Assert.That(organism, Is.GreaterThan(0.0), "the organism ate from the packet too");
            Assert.That(world.FoodEaten - eaten0, Is.GreaterThanOrEqualTo(creature));
            Assert.That(world.OrganismsEaten, Is.EqualTo(o.Eaten));
            Assert.That(o.Eaten, Is.GreaterThanOrEqualTo(organism));
            double leaked = world.Environment.PacketGlucose - leaked0;
            Assert.That(glucose0 - shared.Glucose, Is.EqualTo(creature + organism + leaked).Within(1e-12 * glucose0),
                "what the packet lost is what the two took from it and what leaked into the water");
            Assert.That(world.FoodEaten - eaten0, Is.LessThanOrEqualTo(creature + leaked),
                "the creature ate what it took from the packet and some of what leaked: the organism's food is its own");
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

        // Kills the organism (a lethal damage load) and steps once: its body becomes food. Returns the body.
        private static FoodPacket Kill(FermenterWorld world, FreeOrganism o)
        {
            o.Cell.SetState("bio__D_ribo", 1.1);
            Run(world, 0.1);
            Assert.That(o.Alive, Is.False);
            return o.Remains!;
        }

        // Puts the body on the creature's cell 0, overlapping it by a fifth of a cell.
        private static void OnCreature(FermenterWorld world, FoodPacket body)
        {
            world.CellPosition(0, out double x, out double y);
            typeof(FoodPacket).GetProperty("X")!.SetValue(body, x + 1.8 * world.CellRadius);
            typeof(FoodPacket).GetProperty("Y")!.SetValue(body, y);
        }

        [Test]
        public void ADeadOrganismsBodyIsFoodWhereItLiesAndStaysUntilEaten()
        {
            using var world = Quiet(engulfing: true);
            world.BathConcentration = 1.0;   // the creature lives through the hour it takes to find the body
            var o = Release(world);
            world.SetFeature(Feature(GameFeatures.Engulfing), false);   // only the organism engulfs
            var swallowed = PacketAt(world, o.X, o.Y, 0.5 * world.CellRadius);
            var b = o.Body.Body;
            double ax = Math.Cos(b.Angle + Math.PI), ay = Math.Sin(b.Angle + Math.PI);
            var held = PacketAt(world, o.X + 1.5 * world.CellRadius * ax, o.Y + 1.5 * world.CellRadius * ay,
                1.2 * world.CellRadius, slot: 1);   // too large to engulf: it holds it
            Run(world, 60.0);
            Assert.That(swallowed.Active, Is.False);
            Assert.That((held.Stuck, held.Holder), Is.EqualTo((true, o.Id)));
            var v = o.Vacuole!;
            int organisms = world.Organisms.Count;

            var body = Kill(world, o);
            Assert.That(world.Organisms, Does.Not.Contain(o), "a dead organism is no longer an organism");
            Assert.That(world.Organisms, Has.Count.EqualTo(organisms - 1));
            Assert.That(world.OrganismsDied, Is.EqualTo(1));
            Assert.That(world.OrganismsGone, Is.EqualTo(0));
            Assert.That(held.Stuck, Is.False, "what it held lies where it is");
            Assert.That(o.Vacuole, Is.Null);
            Assert.That((body.Active, body.Kind, body.Stuck), Is.EqualTo((true, PacketKind.Body, false)));
            Assert.That((body.X, body.Y), Is.EqualTo((o.X, o.Y)), "where it died");
            Assert.That(body.Radius, Is.EqualTo(world.CellRadius), "a dead cell, as large as the cell");
            Assert.That(world.Food.Fill(body), Is.EqualTo(1.0));
            Assert.That(world.Food.TraceSpecies[body.MainTrace].Id, Is.EqualTo("serine"), "it smells of lysed cells");
            // its carbon: the cell at the packet density (with the packets' free share), plus what its vacuole held
            var s = world.Food.Settings;
            double cell = s.SphereAmount(world.CellRadius);
            Assert.That(world.DeadCellAmount, Is.EqualTo(cell));
            Assert.That(cell, Is.EqualTo(GameParameters.Default.PacketDensity * 4.0 / 3.0 * Math.PI
                * Math.Pow(world.CellRadius, 3)).Within(1e-12 * cell));
            Assert.That(v.Glucose, Is.GreaterThan(0.0));
            Assert.That(body.Glucose, Is.EqualTo(cell + v.Glucose).Within(1e-12 * cell));
            Assert.That(body.Free, Is.EqualTo(s.FreeFraction * cell + v.Free).Within(1e-12 * cell));
            Assert.That(o.EatenFromVacuole + o.Egested + v.Glucose, Is.EqualTo(o.Engulfed).Within(1e-12 * o.Engulfed),
                "what it engulfed was taken up or is in its body: none is lost");

            // nobody touches it: it stays, drifting with the water it died in; only its free part leaks into the water
            double glucose = body.Glucose, polymer = body.Glucose - body.Free, x0 = body.X, y0 = body.Y;
            const double seconds = 1800.0;
            long first = Clocks.GetOrCreateValue(world).Steps;
            Run(world, seconds);
            // the tide it drifts in: the free current at each step (open water, far above the seabed)
            double amp = GameParameters.Default.Value("environment.current_speed"), period = GameParameters.Default.Value("environment.tide_period");
            double drift = 0.0;
            for (long k = first; k < first + (long)Math.Round(seconds / world.MacroDt); k++)
                drift += amp * Math.Cos(2.0 * Math.PI * k * world.MacroDt / period) * world.MacroDt;
            Assert.That(body.Active, Is.True, "a dead body is not removed with time");
            Assert.That(body.Glucose - body.Free, Is.EqualTo(polymer).Within(1e-12 * polymer), "its polymer stays");
            Assert.That(body.Free, Is.LessThan(0.1 * (glucose - polymer)), "its small molecules leak out");
            Assert.That(body.X - x0, Is.EqualTo(drift).Within(1e-9 * Math.Abs(drift) + 1e-15), "it drifts with the tide");
            Assert.That(body.Y - y0, Is.EqualTo(0.0).Within(1e-15));

            // the creature's cell digests it with its hydrolase and takes it up through the membrane facing it; it shrinks
            OnCreature(world, body);
            glucose = body.Glucose;
            double eaten0 = world.EatenFromPackets, leaked0 = world.Environment.PacketGlucose;
            Run(world, 600.0);
            double eaten = world.EatenFromPackets - eaten0, leaked = world.Environment.PacketGlucose - leaked0;
            Assert.That(eaten, Is.GreaterThan(0.0));
            Assert.That(glucose - body.Glucose, Is.EqualTo(eaten + leaked).Within(1e-12 * glucose),
                "what it lost was eaten or leaked into the water");
            Assert.That((body.Stuck, body.Holder), Is.EqualTo((true, 0)), "it stays with the creature that found it");
            Assert.That(body.Radius, Is.LessThan(world.CellRadius));
            Assert.That(body.Radius, Is.EqualTo(world.CellRadius * Math.Pow(world.Food.Fill(body), 1.0 / 3.0))
                .Within(1e-3 * world.CellRadius), "its radius goes as the cube root of what is left");
        }

        [Test]
        public void ADeadCellIsEngulfedOnlyOnceItIsSmallerThanTheCell()
        {
            using var world = Quiet(engulfing: true);
            var o = Release(world);
            var body = Kill(world, o);
            double glucose = body.Glucose, leaked0 = world.Environment.PacketGlucose;
            OnCreature(world, body);
            Run(world, 1.0);
            Assert.That((body.Active, world.Engulfed), Is.EqualTo((true, 0.0)),
                "a dead cell as large as the cell is not engulfed: it is eaten from outside");
            Assert.That(world.FoodEaten, Is.GreaterThan(0.0));
            // eaten down from outside until it fits beside room for a prey cell, then swallowed whole
            for (int k = 0; k < 720 && body.Active; k++)
            {
                Run(world, 10.0);
                Assert.That(!body.Active || body.Radius <= world.CellRadius, Is.True);
            }
            Assert.That(body.Active, Is.False, "swallowed once small enough");
            Assert.That(world.Engulfed, Is.GreaterThan(0.0));
            Assert.That(world.VacuoleRadius(world.Engulfed + world.Prey.Content), Is.LessThan(world.CellRadius));
            Assert.That(world.EatenFromPackets + world.Environment.PacketGlucose - leaked0 + world.Engulfed,
                Is.EqualTo(glucose).Within(1e-12 * glucose), "eaten from outside + leaked + engulfed = the body");
        }

        [Test]
        public void ItDividesWhenItsInheritedSettingsAllowAndItsDaughtersEarnPointsToo()
        {
            using var world = Quiet();
            double perOffspring = GameParameters.Default.Value("skills.points_per_offspring");
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
            Assert.That((world.Offspring, world.OwnOffspring), Is.EqualTo((3, 2)), "offspring counts the whole lineage");
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(3 * perOffspring)),
                "a granddaughter earns points as a daughter does");
            Assert.That(world.ReproductionPoints, Is.GreaterThan(reproduction));
            Assert.That(world.OrganismsReleased, Is.EqualTo(3));
            // and so on down the generations
            grandchild.Cell.SetState("bio__P_growth", grandchild.Cell.DaughterCost);
            Run(world, 0.1);
            Assert.That(world.Organisms[3].Generation, Is.EqualTo(3));
            Assert.That((world.Offspring, world.OwnOffspring), Is.EqualTo((4, 2)));
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(4 * perOffspring)));
        }

        [Test]
        public void OrganismsOutOfRangeOrBeyondTheCapAreFrozen()
        {
            using var world = Quiet();
            var o = Release(world);
            var near = Release(world);
            var b = world.Body.Body;
            o.Body.Body.Place(b.X + 1.01 * world.OrganismRange, b.Y, 0.0);
            near.Body.Body.Place(b.X + 0.99 * world.OrganismRange, b.Y - 0.99 * world.OrganismRange, 0.0);
            Run(world, 0.1);
            Assert.That(world.Organisms, Is.EquivalentTo(new[] { near }));
            Assert.That(world.FrozenOrganisms, Is.EquivalentTo(new[] { o }));
            Assert.That((o.Frozen, near.Frozen, world.OrganismsGone), Is.EqualTo((true, false, 0)), "frozen, not gone");
            Assert.That(world.OrganismRange, Is.EqualTo(2.0 * 2.0 * world.Food.HalfSize), "two view widths");

            // at the cap a new daughter freezes the farthest
            while (world.OrganismsReleased < world.MaxOrganisms + 2)
                Release(world);
            Assert.That(world.Organisms, Has.Count.EqualTo(world.MaxOrganisms));
            Assert.That(world.Organisms, Does.Not.Contain(near), "the farthest was frozen");
            Assert.That(world.FrozenOrganisms, Is.EqualTo(new[] { o, near }), "in id order");
            Assert.That(world.OrganismsGone, Is.EqualTo(0));
            // a dead one's body is food, not an organism: it makes room
            var dead = world.Organisms[5];
            var body = Kill(world, dead);
            Assert.That(world.Organisms, Does.Not.Contain(dead));
            Assert.That(world.Organisms.All(x => x.Alive), Is.True);
            Assert.That(body.Active, Is.True);
        }

        [Test]
        public void AFrozenOrganismIsKeptAsItWasAndResumesWhenTheCreatureComesBack()
        {
            using var world = Quiet(swimming: true);
            var o = Release(world);
            Run(world, 5.0);
            // it holds a dead body: the body freezes and thaws with it
            var held = world.Food.AddBody(o.X + 1.5 * world.CellRadius, o.Y, 0.0, 0.0, world.CellRadius, 1e-15, 0.0, 0);
            Run(world, 0.1);
            Assert.That((held.Stuck, held.Holder), Is.EqualTo((true, o.Id)));
            var b = world.Body.Body;
            double bx = b.X, by = b.Y;
            // the creature leaves: the organism freezes where it is
            b.Place(bx + 1.5 * world.OrganismRange, by, b.Angle);
            Run(world, 0.1);
            Assert.That(o.Frozen, Is.True);
            double[] at = Snapshot(o);
            var work = o.Cell.Statistics;
            double time = o.Cell.Time;
            Run(world, 30.0);
            Assert.That(Snapshot(o), Is.EqualTo(at), "not moved or integrated while frozen");
            Assert.That(o.Cell.Statistics, Is.EqualTo(work), "no integration steps: no cost");
            Assert.That(o.Cell.Time, Is.EqualTo(time));
            Assert.That(world.Organism(o.Id), Is.Null, "not simulated");
            Assert.That((held.Frozen, held.Active, held.Holder), Is.EqualTo((true, false, o.Id)), "frozen with its holder");

            // the creature comes back: it resumes from that state, and is simulated again
            b.Place(o.X + 0.5 * world.OrganismRange, o.Y, b.Angle);
            Run(world, 0.1);
            Assert.That((o.Frozen, world.Organism(o.Id)), Is.EqualTo((false, o)));
            Assert.That(world.FrozenOrganisms, Is.Empty);
            Assert.That((held.Frozen, held.Active, held.Holder), Is.EqualTo((false, true, o.Id)), "back with it");
            Run(world, 5.0);
            Assert.That(o.Cell.Time, Is.GreaterThan(time), "integrating again");
            Assert.That(o.Alive, Is.True);
        }

        // An organism's state: position, velocity, angle, random-run clock and its cell's states.
        private static double[] Snapshot(FreeOrganism o)
        {
            var r = o.Body.Body;
            var cells = o.Cell.Batch.States.Select(x => x[0]);
            double untilSwitch = (double)typeof(FreeOrganism).GetProperty("UntilSwitch",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(o)!;
            return new[] { r.X, r.Y, r.Angle, r.Vx, r.Vy, r.Omega, untilSwitch, o.Cell.Damage }.Concat(cells).ToArray();
        }

        [Test]
        public void FrozenBodiesAreKeptUntilTheCreatureComesBackUpToTheirCap()
        {
            using var world = Quiet();
            var o = Release(world);
            var body = Kill(world, o);
            Assert.That(world.Food.BodyRange, Is.EqualTo(world.OrganismRange));
            var cb = world.Body.Body;
            typeof(FoodPacket).GetProperty("X")!.SetValue(body, cb.X + 0.99 * world.OrganismRange);
            typeof(FoodPacket).GetProperty("Y")!.SetValue(body, cb.Y);
            Run(world, 0.1);
            Assert.That(body.Active, Is.True, "beyond the food window, within the organisms' range");
            typeof(FoodPacket).GetProperty("X")!.SetValue(body, cb.X + 1.01 * world.OrganismRange);
            Run(world, 0.1);
            Assert.That((body.Active, body.Frozen, world.Food.FrozenBodies, world.Food.BodiesGone), Is.EqualTo((false, true, 1, 0)));
            double x = body.X, y = body.Y, glucose = body.Glucose, free = body.Free;
            cb.Place(x - 4.0 * world.OrganismRange, y, cb.Angle);   // well away: the current does not bring it back in range
            double released = world.Environment.PacketGlucose;
            Run(world, 30.0);
            Assert.That((body.X, body.Y, body.Glucose, body.Free), Is.EqualTo((x, y, glucose, free)), "no drift, no leak");
            Assert.That(world.Environment.PacketGlucose, Is.EqualTo(released));
            Assert.That(world.Food.Packets.Count(p => ReferenceEquals(p, body)), Is.EqualTo(1));
            // new food does not take its slot; the creature coming back thaws it
            var other = world.Food.AddBody(cb.X, cb.Y, 0.0, 0.0, world.CellRadius, 1e-15, 0.0, 0);
            Assert.That((body.Frozen, ReferenceEquals(other, body)), Is.EqualTo((true, false)));
            world.Food.Remove(other);
            cb.Place(x - 0.5 * world.OrganismRange, y, cb.Angle);
            Run(world, 0.1);
            Assert.That((body.Active, body.Frozen), Is.EqualTo((true, false)));

            // beyond the cap the farthest frozen body is forgotten
            world.Food.MaxFrozenBodies = 1;
            var near = world.Food.AddBody(cb.X - 1.2 * world.OrganismRange, cb.Y, 0.0, 0.0, world.CellRadius, 1e-15, 0.0, 0);
            typeof(FoodPacket).GetProperty("X")!.SetValue(body, cb.X + 1.5 * world.OrganismRange);
            Run(world, 0.1);
            Assert.That((near.Frozen, body.Frozen, world.Food.BodiesGone), Is.EqualTo((true, false, 1)));
            Assert.That(body.Active, Is.False, "forgotten");
        }

        [Test]
        public void BeyondTheFrozenCapTheFarthestFrozenOrganismIsForgotten()
        {
            using var world = Quiet();
            world.MaxFrozenOrganisms = 1;
            var far = Release(world);
            var nearer = Release(world);
            var b = world.Body.Body;
            far.Body.Body.Place(b.X + 3.0 * world.OrganismRange, b.Y, 0.0);
            nearer.Body.Body.Place(b.X - 1.5 * world.OrganismRange, b.Y, 0.0);
            Run(world, 0.1);
            Assert.That(world.FrozenOrganisms, Is.EqualTo(new[] { nearer }));
            Assert.That(world.OrganismsGone, Is.EqualTo(1));
            Assert.That(world.Organisms, Is.Empty);
        }

        [Test]
        public void WithTheEnvironmentFieldItExchangesWithTheFieldConservatively()
        {
            using var world = Quiet();
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

            // dead, its body's free sugar leaks into the field: what the body loses, the field gains
            var body = Kill(world, o);
            double glucose = body.Glucose;
            sourced0 = env.Field.Sourced(g);
            packets0 = env.PacketGlucose;
            cells0 = world.CellGlucoseExchanged;
            Run(world, 60.0);
            packets = env.PacketGlucose - packets0;
            cells = world.CellGlucoseExchanged - cells0;
            Assert.That(packets, Is.GreaterThan(0.0));
            Assert.That(glucose - body.Glucose, Is.EqualTo(packets).Within(1e-12 * glucose));
            Assert.That(env.Field.Sourced(g) - sourced0, Is.EqualTo(packets - cells).Within(1e-12 * (packets + Math.Abs(cells))));
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
                world.Organisms[4].Cell.SetState("bio__D_ribo", 1.1);   // its body is food among them
                Run(world, 120.0);
                var print = world.Organisms.SelectMany(o => new[]
                {
                    o.Id, o.X, o.Y, o.Body.Body.Angle, o.Cell.State("cytosol__C_ATP"), o.Cell.MembraneGlucose, o.Eaten,
                    o.Engulfed, o.Vacuole?.Glucose ?? -1.0,
                }).Concat(world.Food.Packets.Where(p => p.Active && p.Kind == PacketKind.Body)
                    .SelectMany(p => new[] { p.X, p.Y, p.Glucose, p.Free })).Concat(new[]
                {
                    world.OrganismsReleased, world.OrganismsDied, world.Offspring, world.Food.BodiesGone, world.OrganismsEaten, world.OrganismsEngulfed, world.FoodEaten,
                    world.Cell(0).State("cytosol__C_ATP"), world.Body.Body.X,
                }).ToArray();
                if (reference == null)
                {
                    Assert.That(world.Organisms.Any(o => o.Generation == 2), Is.True, "an organism divided");
                    Assert.That(world.OrganismsDied, Is.EqualTo(1), "one died: its body is food among them");
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
            o.Cell.SetState("bio__D_ribo", 1.1);
            loop.StepExactly(1);
            var body = o.Remains!;
            while (world.Organisms.Count <= capacity)
                Release(world);
            loop.StepExactly(1);
            Assert.That(loop.Outgrown(frame), Is.True, "more organisms than the frame has room for");
            frame = loop.CreateFrame();
            var second = world.Organisms[0];
            loop.Enqueue(SimCommands.InspectOrganism(second.Id));
            loop.ApplyCommands();
            loop.WriteSnapshot(frame);
            Assert.That(frame.OrganismCount, Is.EqualTo(world.Organisms.Count));
            Assert.That(frame.OrganismId[0], Is.EqualTo(o.Id + 1), "the dead one is food, not an organism");
            Assert.That(frame.OrganismX[0], Is.EqualTo((float)(second.X - world.Body.Body.X)));
            Assert.That(frame.OrganismY[0], Is.EqualTo((float)(second.Y - world.Body.Body.Y)));
            Assert.That(frame.OrganismOrientation[0], Is.EqualTo((float)second.Body.Body.Angle));
            Assert.That(frame.OrganismGeneration[0], Is.EqualTo(1));
            Assert.That(frame.OrganismFlagella[0], Is.EqualTo((float)world.FlagellaOf(second)));
            Assert.That(frame.OrganismFlagellumAngles[0][0], Is.EqualTo((float)second.Body.FlagellumAngle(0, 0)));
            int atp = world.Overlays.ToList().FindIndex(c => c.Id == "atp");
            Assert.That(frame.OrganismChannels[atp][0], Is.EqualTo((float)second.Cell.State("cytosol__C_ATP")));
            // its body, among the packets: marked as a body, with its radius and fill
            Assert.That(frame.PacketCount, Is.EqualTo(1));
            Assert.That(frame.PacketBody[0], Is.True);
            Assert.That(frame.PacketCarried[0], Is.False);
            Assert.That(frame.PacketRadius[0], Is.EqualTo((float)body.Radius));
            Assert.That(frame.PacketFill[0], Is.EqualTo((float)world.Food.Fill(body)));
            Assert.That(frame.PacketX[0], Is.EqualTo((float)(body.X - world.Body.Body.X)));
            Assert.That((frame.Offspring, frame.OwnOffspring), Is.EqualTo((world.Offspring, world.Offspring)));
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
            Assert.That(back.PacketBody, Is.EqualTo(frame.PacketBody));
            Assert.That(back.PacketRadius, Is.EqualTo(frame.PacketRadius));
            Assert.That((back.Offspring, back.OwnOffspring), Is.EqualTo((frame.Offspring, frame.OwnOffspring)));
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
