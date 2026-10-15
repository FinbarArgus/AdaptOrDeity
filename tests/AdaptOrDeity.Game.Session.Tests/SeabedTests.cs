using System;
using System.Collections.Generic;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class SeabedTests
    {
        private static Seabed NewSeabed() => new Seabed(GameParameters.Default.Seabed(), 0.0);

        // One game-sized cell body in water with the free current (as MotionTests), its flagellum based at angle phi.
        private static (CreatureBody Body, double F) OneCell(double phi)
        {
            using var world = new FermenterWorld();
            var cell = world.Cell(0);
            cell.MotorActivation = 1.0;
            for (int k = 0; k < 100; k++)
                cell.Step(0.1);
            double mass = GameParameters.Default.Value("body.cell_density") * cell.AlgebraicValue("membrane__Vol");
            var body = new CreatureBody(world.CellRadius, cell.External("eta_mod_i_flagellum"), GameParameters.Default.Body());
            body.AddCell(0.0, 0.0, mass, CreatureBody.DefaultAngles(GameParameters.Default.Value("motility.flagellum_spacing")));
            body.SetFlagellumAngle(0, 0, phi);
            return (body, cell.FlagellumThrust);
        }

        [Test]
        public void ACellSwimmingDownComesToRestOnTheGround()
        {
            var seabed = NewSeabed();
            var (body, f) = OneCell(Math.PI / 2.0);   // based on top: it pushes the cell downwards
            var b = body.Body;
            var rocks = new List<Rock>();
            double x0 = 0.0;
            for (seabed.RocksBetween(x0 - 10e-6, x0 + 10e-6, rocks); rocks.Count > 0; seabed.RocksBetween(x0 - 10e-6, x0 + 10e-6, rocks))
                x0 += 1e-6;   // above bare ground
            b.Place(x0, 0.0, 0.0);
            double ux = 0.0;   // still water: it falls straight down
            for (int k = 0; k < 600; k++)
            {
                double c = seabed.CurrentFactor(b.X, b.Y);
                b.CurrentVx = ux * c;
                body.Step(0.1, _ => 1.0, _ => f);
                seabed.CollideOne(b, b.X, b.Y, body.CellRadius);
            }
            Assert.That(b.Y, Is.EqualTo(seabed.GroundY + body.CellRadius).Within(1e-15), "resting on the ground");
            Assert.That(b.GroundVy, Is.EqualTo(0.0).Within(1e-18), "not moving into it");
            Assert.That(seabed.CurrentFactor(b.X, b.Y), Is.EqualTo(body.CellRadius / seabed.Settings.BoundaryLayer).Within(1e-12),
                "in the boundary layer the current is slow");
        }

        [Test]
        public void TheCurrentHoldsACellAgainstARockWithoutPassingThrough()
        {
            var seabed = NewSeabed();
            var rocks = new List<Rock>();
            seabed.RocksBetween(0.0, 200e-6, rocks);
            var rock = rocks.OrderBy(r => r.X).First();
            var (body, _) = OneCell(0.0);
            var b = body.Body;
            double r = body.CellRadius;
            b.Place(rock.X - rock.Radius - 3.0 * r, rock.Y, 0.0);   // upstream of the rock, at its centre's height
            b.CurrentVx = 20e-6;   // a strong current straight at it
            for (int k = 0; k < 600; k++)
            {
                body.Step(0.1, _ => 1.0, _ => 0.0);
                seabed.CollideOne(b, b.X, b.Y, r);
                double d = Math.Sqrt((b.X - rock.X) * (b.X - rock.X) + (b.Y - rock.Y) * (b.Y - rock.Y));
                Assert.That(d, Is.GreaterThanOrEqualTo(rock.Radius + r - 1e-12), "outside the rock");
            }
        }

        [Test]
        public void TheBoundaryLayerAndWakesSlowTheCurrent()
        {
            var seabed = NewSeabed();
            double g = seabed.GroundY, d = seabed.Settings.BoundaryLayer;
            double clearX = -1.0;
            var rocks = new List<Rock>();
            for (double x = 0.0; clearX < 0.0; x += 1e-6)
            {
                seabed.RocksBetween(x - 40e-6, x + 10e-6, rocks);
                if (rocks.Count == 0)
                    clearX = x;
            }
            Assert.That(seabed.CurrentFactor(clearX, g), Is.EqualTo(0.0), "no slip at the bed");
            Assert.That(seabed.CurrentFactor(clearX, g + 0.5 * d), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(seabed.CurrentFactor(clearX, g + 2.0 * d), Is.EqualTo(1.0), "the free current above the layer");
            seabed.RocksBetween(0.0, 200e-6, rocks);
            var rock = rocks.OrderBy(r => r.X).First();
            double behind = seabed.CurrentFactor(rock.X + rock.Radius * 1.5, rock.Y);
            double ahead = seabed.CurrentFactor(rock.X - rock.Radius * 1.5, rock.Y);
            Assert.That(behind, Is.LessThan(ahead), "sheltered in the wake downstream");
            Assert.That(seabed.CurrentFactor(rock.X, rock.Y), Is.EqualTo(0.0), "none inside the rock");
        }

        [Test]
        public void TheSeabedIsTheSameEveryTimeAndRestsOnTheGround()
        {
            var a = new List<Rock>();
            var b = new List<Rock>();
            NewSeabed().RocksBetween(-500e-6, 500e-6, a);
            NewSeabed().RocksBetween(-500e-6, 500e-6, b);
            Assert.That(a.Count, Is.GreaterThan(10));
            Assert.That(a.Select(r => (r.X, r.Y, r.Radius)), Is.EqualTo(b.Select(r => (r.X, r.Y, r.Radius))));
            double g = NewSeabed().GroundY;
            foreach (var r in a)
                Assert.That(r.Y, Is.GreaterThan(g), "every rock's centre is above the ground");
        }

        [Test]
        public void ANewSeedDrawsANewFloorAndNoRockIsNearTheStart()
        {
            var settings = GameParameters.Default.Seabed();
            var layouts = new List<string>();
            for (int seed = 1; seed <= 40; seed++)
            {
                settings.Seed = seed;
                var seabed = new Seabed(settings, 3e-6, -2e-6);
                var rocks = new List<Rock>();
                seabed.RocksBetween(-2000e-6, 2000e-6, rocks);
                layouts.Add(string.Join(";", rocks.Select(r => $"{r.X:G6},{r.Y:G6},{r.Radius:G6}")));
                foreach (var r in rocks)
                {
                    double d = Math.Sqrt((r.X + 2e-6) * (r.X + 2e-6) + (r.Y - 3e-6) * (r.Y - 3e-6));
                    Assert.That(d, Is.GreaterThanOrEqualTo(r.Radius + Seabed.StartClearance - 1e-15), $"seed {seed}: clear start");
                }
            }
            Assert.That(layouts.Distinct().Count(), Is.EqualTo(layouts.Count), "every seed its own floor");

            // over many stretches: some bare, some with one structure, some with two, of every kind
            settings.Seed = 7;
            var many = new List<Rock>();
            new Seabed(settings, 0.0).RocksBetween(-20000e-6, 20000e-6, many);
            int bare = 0;
            for (double x = -19000e-6; x < 19000e-6; x += settings.StretchWidth)
                if (!many.Any(r => r.X >= x && r.X < x + settings.StretchWidth))
                    bare++;
            Assert.That(bare, Is.InRange(100, 400), "about a fifth of ~950 stretches bare");
        }

        [Test]
        public void TheReseedCommandGivesTheWorldANewFloor()
        {
            using var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var before = new List<Rock>();
            world.Seabed.RocksBetween(-500e-6, 500e-6, before);
            loop.Enqueue(SimCommands.ReseedSeabed(12345));
            loop.ApplyCommands();
            var after = new List<Rock>();
            world.Seabed.RocksBetween(-500e-6, 500e-6, after);
            Assert.That(after.Select(r => r.X), Is.Not.EqualTo(before.Select(r => r.X)));
            Assert.That(world.Seabed.GroundY, Is.EqualTo(new FermenterWorld().Seabed.GroundY), "the same ground line");
            loop.StepExactly(10);
        }

        [Test]
        public void FoodSettlesOnTheFloorAtItsOwnRateBesideTheWaters()
        {
            // the floor's arrivals are their own Poisson stream: the water's arrivals are the same with or without them
            var settings = GameParameters.Default.Food(0.1);
            settings.ArrivalsPerSecond = 0.01;
            settings.FloorArrivalsPerSecond = 0.0;
            var plain = new FoodField(settings);
            var floorSettings = GameParameters.Default.Food(0.1);
            floorSettings.ArrivalsPerSecond = 0.01;
            floorSettings.FloorArrivalsPerSecond = 0.02;
            var withFloor = new FoodField(floorSettings);
            // the ground 10 um below the window's centre, in view (the window reaches 15 um)
            var seabed = new Seabed(GameParameters.Default.Seabed(), GameParameters.Default.Value("seabed.ground_depth") - 10e-6);
            const double dt = 0.1;
            for (int k = 0; k < 100000; k++)   // ~2.8 h
            {
                plain.Step(dt);
                withFloor.Step(dt);
                withFloor.ArriveOnFloor(seabed, dt);
            }
            Assert.That(withFloor.Arrived, Is.EqualTo(plain.Arrived), "the water's arrivals unchanged");
            Assert.That(withFloor.ArrivedOnFloor, Is.EqualTo(0.02 * 1e4).Within(4 * Math.Sqrt(200.0)), "Poisson at 0.02/s");
            int onFloor = 0;
            foreach (var p in withFloor.Packets.Where(p => p.Active && p.Kind == PacketKind.Lying))
            {
                double x = p.X, y = p.Y;
                bool resting = Math.Abs(y - (seabed.GroundY + p.FullRadius)) < 1e-12
                               || seabed.PushOut(ref x, ref y, p.FullRadius + 1e-9, out _, out _);
                if (resting)
                    onFloor++;
            }
            Assert.That(onFloor, Is.GreaterThan(0), "packets lie on the ground or on a rock");

            // with the ground out of view, nothing settles where the creature can see
            var high = new FoodField(floorSettings);
            var deep = new Seabed(GameParameters.Default.Seabed(), 0.0);   // the ground 40 um below
            for (int k = 0; k < 10000; k++)
            {
                high.Step(dt);
                high.ArriveOnFloor(deep, dt);
            }
            Assert.That(high.ArrivedOnFloor, Is.EqualTo(0));
        }

        [Test]
        public void FoodAndPreyStayOutOfTheSeabedAndFramesCarryIt()
        {
            using var world = new FermenterWorld();
            world.Level = 2;
            for (int k = 0; k < 20; k++)
                world.DevSpawnPrey();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(600);
            var seabed = world.Seabed;
            foreach (var p in world.Food.Packets.Where(p => p.Active))
                Assert.That(p.Y - p.Radius, Is.GreaterThanOrEqualTo(seabed.GroundY - 1e-15), "packets above the ground");
            foreach (var p in world.Prey.Prey.Where(p => p.Active))
                Assert.That(p.Y - p.Radius, Is.GreaterThanOrEqualTo(seabed.GroundY - 1e-15), "prey above the ground");
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.GroundY, Is.EqualTo((float)(seabed.GroundY - frame.CreatureY)));
            Assert.That(frame.RockCount, Is.GreaterThan(0));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.GroundY, Is.EqualTo(frame.GroundY));
            Assert.That(back.RockCount, Is.EqualTo(frame.RockCount));
            Assert.That(back.RockX[0], Is.EqualTo(frame.RockX[0]));
        }
    }
}
