using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Luck (FermenterWorld.Luck.cs): her points build up over time; she spends them blowing a current along an arrow she
    /// aims, relative to the creature (<see cref="DirectedCurrent"/>), which carries the creature, loose food and prey.
    /// </summary>
    public class LuckTests
    {
        private static double P(string key) => GameParameters.Default.Value(key);

        private static FermenterWorld Quiet()
        {
            var world = new FermenterWorld();
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            return world;
        }

        private static SimLoop Loop(FermenterWorld world) =>
            new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });

        private static void Run(SimLoop loop, double seconds) => loop.StepExactly((long)Math.Round(seconds / 0.1));

        [Test]
        public void TheCurrentFlowsAlongItsArrowAcrossABand()
        {
            // an arrow 20 um long along +x through the origin, band half-width 4 um, at 2 um/s
            var c = new DirectedCurrent { HalfWidth = 4e-6 };
            c.Blow(-10e-6, 0.0, 10e-6, 0.0, 2e-6);
            c.At(0.0, 0.0, out double ux, out double uy);
            Assert.That((ux, uy), Is.EqualTo((2e-6, 0.0)), "full speed on the axis, away from the head");
            c.At(0.0, 2e-6, out ux, out _);
            Assert.That(ux, Is.EqualTo(2e-6 * 0.5).Within(1e-18), "cos² across: half at half the half-width");
            c.At(0.0, 4e-6, out ux, out _);
            Assert.That(ux, Is.EqualTo(0.0), "nothing at the band's edge");
            c.At(8e-6, 0.0, out ux, out _);
            Assert.That(ux, Is.EqualTo(2e-6 * 0.5).Within(1e-18), "tapering over the last half-width to the head");
            c.At(-11e-6, 0.0, out ux, out _);
            Assert.That(ux, Is.EqualTo(0.0), "behind the tail");
            c.At(11e-6, 0.0, out ux, out _);
            Assert.That(ux, Is.EqualTo(0.0), "beyond the head");
            c.Blow(0.0, -10e-6, 0.0, 10e-6, 2e-6);   // turned to +y
            c.At(0.0, 0.0, out ux, out uy);
            Assert.That(ux, Is.EqualTo(0.0).Within(1e-21));
            Assert.That(uy, Is.EqualTo(2e-6));
            c.Stop();
            c.At(0.0, 0.0, out ux, out uy);
            Assert.That((ux, uy), Is.EqualTo((0.0, 0.0)));
        }

        [Test]
        public void LuckBuildsUpAndBlowingSpendsItInProportionToStrength()
        {
            using var world = Quiet();
            var loop = Loop(world);
            Assert.That(world.Luck, Is.EqualTo(P("luck.start")));
            Run(loop, 600.0);
            Assert.That(world.Luck, Is.EqualTo(P("luck.start") + 600.0 * P("luck.rate")).Within(1e-6));

            // half strength (an arrow half luck.full_length long) for a minute costs half the full rate
            double half = 0.5 * P("luck.full_length");
            world.Luck = 50.0;
            world.LuckBlow(-0.5 * half, 30e-6, 0.5 * half, 30e-6);   // well away from the creature
            Assert.That(world.LuckStrength, Is.EqualTo(0.5).Within(1e-12));
            Run(loop, 60.0);
            Assert.That(world.Luck, Is.EqualTo(50.0 + 60.0 * (P("luck.rate") - 0.5 * P("luck.cost_rate"))).Within(1e-6));

            // she stops when the points run out
            world.Luck = 1.0;
            world.LuckBlow(-half, 30e-6, half, 30e-6);   // full strength: 1 point lasts 1 s
            Run(loop, 5.0);
            Assert.That(world.LuckCurrent.On, Is.False);
            Assert.That(world.Luck, Is.LessThan(5.0 * P("luck.rate") + 1e-9));
            world.Luck = 0.0;
            world.LuckBlow(-half, 30e-6, half, 30e-6);
            Assert.That(world.LuckCurrent.On, Is.False, "no points, no current");
            world.Luck = 1e9;
            Assert.That(world.Luck, Is.EqualTo(P("luck.max")), "capped");
        }

        [Test]
        public void HerCurrentPushesTheCreatureAlongTheArrow()
        {
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            double l = P("luck.full_length");
            pushed.LuckBlow(-0.5 * l, 0.0, 0.5 * l, 0.0);   // through the creature, along +x
            Run(pushedLoop, 30.0);
            Run(stillLoop, 30.0);
            double dx = pushed.Body.Body.X - still.Body.Body.X, dy = pushed.Body.Body.Y - still.Body.Body.Y;
            // anchored to the creature, it keeps pushing: drag brings it to most of the flow within a second
            Assert.That(dx, Is.InRange(0.7 * 30.0 * P("luck.max_speed"), 1.01 * 30.0 * P("luck.max_speed")));
            Assert.That(Math.Abs(dy), Is.LessThan(0.05 * dx));
        }

        [Test]
        public void HerCurrentCarriesFoodTowardsTheHead()
        {
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            var s = pushed.Food.Settings;
            double r = s.PacketRadius, g = s.SphereAmount(r);
            // a dead cell 10 um to the creature's left; an arrow from beyond it towards the creature
            var b0 = pushed.Body.Body;
            var a = pushed.Food.AddBody(b0.X - 10e-6, b0.Y, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            var b = still.Food.AddBody(b0.X - 10e-6, b0.Y, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            pushed.LuckBlow(-14e-6, 0.0, -3e-6, 0.0);
            Run(pushedLoop, 2.0);
            Run(stillLoop, 2.0);
            double moved = (a.X - pushed.Body.Body.X) - (b.X - still.Body.Body.X);
            Assert.That(moved, Is.EqualTo(2.0 * pushed.LuckStrength * P("luck.max_speed")).Within(0.2e-6),
                "full flow on the axis (the arrow's strength), 7 um from the head");
        }

        [Test]
        public void TheCommandAndTheFrameCarryHerArrow()
        {
            using var world = Quiet();
            var loop = Loop(world);
            loop.Enqueue(SimCommands.LuckBlow(-12.5e-6, 3.25e-6, 7.0e-6, -1.0e-6));
            loop.ApplyCommands();
            var c = world.LuckCurrent;
            Assert.That((c.On, c.TailX, c.TailY, c.HeadX, c.HeadY), Is.EqualTo((true, -12.5e-6, 3.25e-6, 7.0e-6, -1.0e-6)));
            loop.StepExactly(1);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.LuckOn, Is.True);
            Assert.That((frame.LuckTailX, frame.LuckHeadY), Is.EqualTo(((float)-12.5e-6, (float)-1.0e-6)));
            Assert.That(frame.LuckHalfWidth, Is.EqualTo((float)P("luck.half_width")));
            Assert.That(frame.Luck, Is.EqualTo((float)world.Luck));
            Assert.That(frame.LuckMax, Is.EqualTo((float)P("luck.max")));
            loop.Enqueue(SimCommands.LuckStop());
            loop.ApplyCommands();
            Assert.That(world.LuckCurrent.On, Is.False);
        }
    }
}
