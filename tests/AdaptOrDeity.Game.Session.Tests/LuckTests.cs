using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Luck (FermenterWorld.Luck.cs): her points build up over time; she spends them sending a stream of current along an
    /// arrow she aims, relative to the creature (<see cref="DirectedCurrent"/>), which spins up, flows for a few seconds and
    /// coasts to a stop, carrying the creature, loose food and prey.
    /// </summary>
    public class LuckTests
    {
        private static double P(string key) => GameParameters.Default.Value(key);

        private static FermenterWorld Quiet()
        {
            var world = new FermenterWorld();
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);   // only the water moves it
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            return world;
        }

        private static SimLoop Loop(FermenterWorld world) =>
            new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });

        private static void Run(SimLoop loop, double seconds) => loop.StepExactly((long)Math.Round(seconds / 0.1));

        // the stream's flow on its axis integrated over the steps taken [m]
        private static double Flowed(SimLoop loop, FermenterWorld world, double seconds)
        {
            double sum = 0.0;
            for (long k = 0; k < (long)Math.Round(seconds / 0.1); k++)
            {
                loop.StepExactly(1);
                sum += world.LuckCurrent.Speed * 0.1;
            }
            return sum;
        }

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
            c.SetSpeed(1e-6);
            c.At(0.0, 0.0, out _, out uy);
            Assert.That(uy, Is.EqualTo(1e-6), "spun down, same arrow");
            c.Stop();
            c.At(0.0, 0.0, out ux, out uy);
            Assert.That((ux, uy), Is.EqualTo((0.0, 0.0)));
        }

        [Test]
        public void LuckBuildsUpSlowlyAndAStreamIsPaidWhenSent()
        {
            using var world = Quiet();
            var loop = Loop(world);
            Assert.That(world.Luck, Is.EqualTo(P("luck.start")));
            Assert.That(P("luck.max") / P("luck.rate"), Is.EqualTo(12 * 3600.0).Within(1.0), "empty to full in 12 h");
            Run(loop, 600.0);
            Assert.That(world.Luck, Is.EqualTo(P("luck.start") + 600.0 * P("luck.rate")).Within(1e-6));

            double l = P("luck.full_length");
            world.Luck = 50.0;
            Assert.That(world.LuckCost(0.0, 30e-6, 0.5 * l, 30e-6), Is.EqualTo(0.5 * P("luck.stream_cost")).Within(1e-12));
            world.LuckLaunch(0.0, 30e-6, 0.5 * l, 30e-6);   // half strength, well away from the creature: paid at once
            Assert.That(world.Luck, Is.EqualTo(50.0 - 0.5 * P("luck.stream_cost")).Within(1e-12));
            Assert.That(world.LuckCurrent.On, Is.True);

            // short of points: a weaker stream, all she has
            world.Luck = 0.3 * P("luck.stream_cost");
            world.LuckLaunch(0.0, 30e-6, l, 30e-6);
            Assert.That(world.Luck, Is.EqualTo(0.0));
            Run(loop, 6.0);
            Assert.That(world.LuckStrength, Is.EqualTo(0.3).Within(1e-3), "spun up to 0.3 of full");

            world.LuckStop();
            world.Luck = 0.0;
            world.LuckLaunch(0.0, 30e-6, l, 30e-6);
            Assert.That(world.LuckCurrent.On, Is.False, "no points, no stream");
            world.Luck = 1e9;
            Assert.That(world.Luck, Is.EqualTo(P("luck.max")), "capped");
        }

        [Test]
        public void AStreamSpinsUpFlowsAFewSecondsAndCoastsToAStop()
        {
            using var world = Quiet();
            var loop = Loop(world);
            double l = P("luck.full_length"), up = P("luck.spin_up"), down = P("luck.spin_down"), flow = P("luck.stream_time");
            world.LuckLaunch(0.0, 30e-6, l, 30e-6);   // full strength, away from the creature
            Assert.That(world.LuckStrength, Is.EqualTo(0.0), "it starts at rest");
            Run(loop, up);
            Assert.That(world.LuckStrength, Is.EqualTo(1.0 - Math.Exp(-1.0)).Within(1e-3), "one spin-up time: 63%");
            Run(loop, flow - up);
            Assert.That(world.LuckStrength, Is.GreaterThan(0.99));
            Run(loop, down);
            double coasting = (1.0 - Math.Exp(-flow / up)) * Math.Exp(-1.0);
            Assert.That(world.LuckStrength, Is.EqualTo(coasting).Within(1e-3), "one spin-down time after it stops flowing: 37%");
            Assert.That(world.LuckCurrent.On, Is.True, "still coasting");
            Run(loop, down * Math.Log(100.0));
            Assert.That(world.LuckCurrent.On, Is.False, "gone once below 1%");
        }

        [Test]
        public void AStreamCarriesFoodAlongItsAxisByTheFlowItHad()
        {
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            var s = pushed.Food.Settings;
            double r = s.PacketRadius, g = s.SphereAmount(r);
            // a dead cell 10 um above the creature and 30 um to its left; a long arrow along that line, clear of the creature
            var b0 = pushed.Body.Body;
            var a = pushed.Food.AddBody(b0.X - 30e-6, b0.Y + 10e-6, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            var b = still.Food.AddBody(b0.X - 30e-6, b0.Y + 10e-6, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            pushed.LuckLaunch(-40e-6, 10e-6, 30e-6, 10e-6);
            double flowed = Flowed(pushedLoop, pushed, 20.0);
            Run(stillLoop, 20.0);
            Assert.That(pushed.LuckCurrent.On, Is.False, "the stream has ended");
            Assert.That(flowed, Is.GreaterThan(10e-6), "a full stream moves things over 10 um");
            double moved = (a.X - pushed.Body.Body.X) - (b.X - still.Body.Body.X);
            Assert.That(moved, Is.EqualTo(flowed).Within(0.01 * flowed), "the flow on its axis, all the way");
            Assert.That(pushed.Body.Body.X - still.Body.Body.X, Is.EqualTo(0.0).Within(1e-9), "the creature, off the band, stays");
        }

        [Test]
        public void AStreamThroughTheCreatureCarriesIt()
        {
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            double l = P("luck.full_length");
            pushed.LuckLaunch(-0.5 * l, 0.0, 0.5 * l, 0.0);   // through the creature, along +x
            double flowed = Flowed(pushedLoop, pushed, 20.0);
            Run(stillLoop, 20.0);
            double dx = pushed.Body.Body.X - still.Body.Body.X, dy = pushed.Body.Body.Y - still.Body.Body.Y;
            // anchored to the creature, it keeps carrying it; drag brings the body to most of the flow within a second
            Assert.That(dx, Is.InRange(0.7 * flowed, 1.01 * flowed));
            Assert.That(Math.Abs(dy), Is.LessThan(0.05 * dx));
        }

        [Test]
        public void TheCommandAndTheFrameCarryHerStream()
        {
            using var world = Quiet();
            var loop = Loop(world);
            loop.Enqueue(SimCommands.LuckLaunch(-12.5e-6, 3.25e-6, 7.0e-6, -1.0e-6));
            loop.ApplyCommands();
            var c = world.LuckCurrent;
            Assert.That((c.On, c.TailX, c.TailY, c.HeadX, c.HeadY), Is.EqualTo((true, -12.5e-6, 3.25e-6, 7.0e-6, -1.0e-6)));
            loop.StepExactly(5);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.LuckOn, Is.True);
            Assert.That((frame.LuckTailX, frame.LuckHeadY), Is.EqualTo(((float)-12.5e-6, (float)-1.0e-6)));
            Assert.That(frame.LuckHalfWidth, Is.EqualTo((float)P("luck.half_width")));
            Assert.That(frame.LuckStrength, Is.EqualTo((float)world.LuckStrength));
            Assert.That(frame.LuckStrength, Is.GreaterThan(0f));
            Assert.That(frame.Luck, Is.EqualTo((float)world.Luck));
            Assert.That((frame.LuckMax, frame.LuckStreamCost, frame.LuckFullLength, frame.LuckMaxSpeed),
                Is.EqualTo(((float)P("luck.max"), (float)P("luck.stream_cost"), (float)P("luck.full_length"), (float)P("luck.max_speed"))));
            loop.Enqueue(SimCommands.LuckStop());
            loop.ApplyCommands();
            Assert.That(world.LuckCurrent.On, Is.False);
        }
    }
}
