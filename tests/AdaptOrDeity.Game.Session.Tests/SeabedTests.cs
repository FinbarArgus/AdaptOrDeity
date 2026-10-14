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
