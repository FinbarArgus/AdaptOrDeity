using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Luck (FermenterWorld.Luck.cs): her points build up over time; she spends them sending a stream of current, a
    /// <see cref="Jet"/>, into the view from its edge. The jet arrives from beyond the edge and runs on through the whole
    /// field, conserving the water's mass (divergence-free) and its momentum (it spreads and slows as it coasts).
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

        // a jet along +x through the origin, already at speed U with its front 30 um downstream
        private static Jet Flowing(double speed = 2e-6, double width = 3e-6)
        {
            var jet = new Jet { SpinUp = 1e-12, FrontWidth = 2e-6 };
            jet.Launch(0.0, 0.0, 1.0, 0.0, speed, width, 1e3);
            jet.Step(30e-6 / speed, 0.0, 0.0);   // spun up at once, its front 30 um on
            return jet;
        }

        [Test]
        public void TheJetConservesTheWatersMassEverywhere()
        {
            // the divergence of the flow, by central differences, is zero to round-off on a grid over the jet, its front
            // and the water beside it (relative to the jet's own shear, U / sigma)
            var jet = new Jet { SpinUp = 0.3, FrontWidth = 2e-6 };
            jet.Launch(1e-6, -2e-6, 3.0, 4.0, 2e-6, 3e-6, 10.0);   // a slanted jet, spinning up
            jet.Step(2.0, 0.1e-6, 0.0);
            double h = 1e-9, scale = jet.Speed / jet.Width, worst = 0.0;
            for (double s = -20e-6; s <= jet.Front + 10e-6; s += 0.7e-6)
                for (double n = -15e-6; n <= 15e-6; n += 0.7e-6)
                {
                    double x = jet.OriginX + s * jet.DirX - n * jet.DirY, y = jet.OriginY + s * jet.DirY + n * jet.DirX;
                    jet.At(x + h, y, out double uxp, out _);
                    jet.At(x - h, y, out double uxm, out _);
                    jet.At(x, y + h, out _, out double uyp);
                    jet.At(x, y - h, out _, out double uym);
                    worst = Math.Max(worst, Math.Abs((uxp - uxm + uyp - uym) / (2.0 * h)));
                }
            Assert.That(worst, Is.LessThan(1e-5 * scale));
        }

        [Test]
        public void TheJetComesFromUpstreamAndRunsOnToItsFront()
        {
            var jet = Flowing();
            jet.At(-50e-6, 0.0, out double ux, out double uy);
            Assert.That(ux, Is.EqualTo(2e-6).Within(1e-12), "upstream of where it entered, beyond the edge, it already flows");
            Assert.That(uy, Is.EqualTo(0.0).Within(1e-15));
            jet.At(10e-6, 3e-6, out ux, out _);
            Assert.That(ux, Is.EqualTo(2e-6 * Math.Exp(-0.5)).Within(1e-12), "a Gaussian profile across: e^-1/2 at one sigma");
            jet.At(60e-6, 0.0, out ux, out uy);
            Assert.That(Math.Abs(ux) + Math.Abs(uy), Is.LessThan(1e-15), "nothing beyond its front yet");
            // the momentum it carries along: the flow integrated across, U sigma sqrt(2 pi)
            double flux = 0.0;
            for (double n = -30e-6; n <= 30e-6; n += 0.05e-6)
            {
                jet.At(0.0, n, out ux, out _);
                flux += ux * 0.05e-6;
            }
            Assert.That(flux, Is.EqualTo(jet.FluxAcross).Within(1e-6 * jet.FluxAcross));
            // the front pushes the water ahead of it aside, outwards on both sides
            jet.At(jet.Front, 6e-6, out _, out double left);
            jet.At(jet.Front, -6e-6, out _, out double right);
            Assert.That(left, Is.GreaterThan(0.0));
            Assert.That(right, Is.EqualTo(-left).Within(1e-12 * left));
        }

        [Test]
        public void ACoastingJetKeepsItsMomentumAsItSpreadsAndDrifts()
        {
            double sigma = 3e-6, spread = 3.0 * sigma * sigma / (2.0 * 3.0);   // the width doubles in 3 s
            var jet = new Jet { SpinUp = 0.5, Spread = spread, Friction = 1e12, FrontWidth = 2e-6 };
            jet.Launch(0.0, 0.0, 0.0, 1.0, 2e-6, sigma, 2.0);   // up the y axis, driven for 2 s
            double front = 0.0;
            for (int k = 0; k < 20; k++)
            {
                jet.Step(0.1, 0.0, 0.0);
                front += jet.Speed * 0.1;
            }
            Assert.That(jet.Speed, Is.EqualTo(2e-6 * (1.0 - Math.Exp(-4.0))).Within(1e-12), "spun up over 2 s");
            Assert.That(jet.Width, Is.EqualTo(sigma), "it keeps its width while driven");
            double momentum = jet.Speed * jet.Width;
            for (int k = 0; k < 30; k++)
            {
                jet.Step(0.1, 0.3e-6, 0.0);   // coasting 3 s, drifting with water flowing along x
                front += jet.Speed * 0.1;
            }
            Assert.That(jet.Width, Is.EqualTo(2.0 * sigma).Within(1e-12), "twice as wide");
            Assert.That(jet.Speed * jet.Width, Is.EqualTo(momentum).Within(1e-9 * momentum), "momentum kept: half the speed");
            Assert.That(jet.Front, Is.EqualTo(front).Within(1e-15), "its front runs at its speed");
            Assert.That(jet.OriginX, Is.EqualTo(0.9e-6).Within(1e-15), "carried by the water it is in");
            Assert.That(jet.On, Is.True);

            var lossy = new Jet { SpinUp = 1e-12, Spread = 0.0, Friction = 2.0 };
            lossy.Launch(0.0, 0.0, 1.0, 0.0, 2e-6, sigma, 0.1);
            lossy.Step(0.1, 0.0, 0.0);
            lossy.Step(2.0, 0.0, 0.0);
            Assert.That(lossy.Speed, Is.EqualTo(2e-6 * Math.Exp(-1.0)).Within(1e-12), "friction takes its momentum");
            lossy.Step(8.0, 0.0, 0.0);
            Assert.That(lossy.On, Is.False, "it ends below 1% of its speed");
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
            world.LuckLaunch(0.0, 30e-6, 0.5 * l, 30e-6);   // half strength: paid at once
            Assert.That(world.Luck, Is.EqualTo(50.0 - 0.5 * P("luck.stream_cost")).Within(1e-12));
            Assert.That(world.LuckJet.On, Is.True);

            world.Luck = 0.3 * P("luck.stream_cost");   // short of points: a weaker stream, all she has
            world.LuckLaunch(0.0, 30e-6, l, 30e-6);
            Assert.That(world.Luck, Is.EqualTo(0.0));
            Run(loop, P("luck.stream_time"));
            Assert.That(world.LuckStrength, Is.EqualTo(0.3).Within(1e-3), "spun up to 0.3 of full");

            world.LuckStop();
            world.Luck = 0.0;
            world.LuckLaunch(0.0, 30e-6, l, 30e-6);
            Assert.That(world.LuckJet.On, Is.False, "no points, no stream");
            world.Luck = 1e9;
            Assert.That(world.Luck, Is.EqualTo(P("luck.max")), "capped");
        }

        [Test]
        public void AStreamSentFromTheEdgeRunsOnPastTheArrowToTheCreature()
        {
            // the arrow runs from 15 um to the creature's left to 5 um short of it; the stream enters there and its front
            // runs on, past the arrow's head, and carries the creature along its line
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            pushed.Luck = P("luck.max");
            pushed.LuckLaunch(-15e-6, 0.0, -5e-6, 0.0);
            var jet = pushed.LuckJet;
            Assert.That((jet.OriginX - pushed.Body.Body.X, jet.DirX, jet.DirY), Is.EqualTo((-15e-6, 1.0, 0.0)));
            Run(pushedLoop, 2.0);
            Run(stillLoop, 2.0);
            Assert.That(pushed.Body.Body.X - still.Body.Body.X, Is.LessThan(0.02e-6), "its front has not reached the creature");
            Run(pushedLoop, 20.0);
            Run(stillLoop, 20.0);
            double dx = pushed.Body.Body.X - still.Body.Body.X, dy = pushed.Body.Body.Y - still.Body.Body.Y;
            Assert.That(dx, Is.GreaterThan(1e-6), "carried on past the arrow's head");
            Assert.That(Math.Abs(dy), Is.LessThan(0.1 * dx));
        }

        [Test]
        public void AStreamCarriesFoodByTheFlowItMeets()
        {
            using var pushed = Quiet();
            using var still = Quiet();
            var pushedLoop = Loop(pushed);
            var stillLoop = Loop(still);
            var s = pushed.Food.Settings;
            double r = s.PacketRadius, g = s.SphereAmount(r);
            // a dead cell 12 um above the creature and 10 um to its left, on a stream sent from 25 um to its left
            var b0 = pushed.Body.Body;
            var a = pushed.Food.AddBody(b0.X - 10e-6, b0.Y + 12e-6, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            var b = still.Food.AddBody(b0.X - 10e-6, b0.Y + 12e-6, 0.0, 0.0, r, g, s.FreeFraction * g, 0);
            pushed.Luck = P("luck.max");
            pushed.LuckLaunch(-25e-6, 12e-6, 5e-6, 12e-6);
            double expected = 0.0;
            for (int k = 0; k < 300; k++)
            {
                pushed.LuckJet.At(a.X, a.Y, out double ux, out _);   // the flow where the packet is, this step
                expected += ux * 0.1;
                pushedLoop.StepExactly(1);
            }
            Run(stillLoop, 30.0);
            double moved = a.X - b.X;
            Assert.That(expected, Is.GreaterThan(5e-6), "the stream moved it several microns");
            // (the world samples the flow after the jet and the drift have moved this step: first order in the step, which
            // shows at the front)
            Assert.That(moved, Is.EqualTo(expected).Within(0.06 * expected), "by the flow it met on the way");
            Assert.That(pushed.Body.Body.X - still.Body.Body.X, Is.LessThan(0.05 * moved), "the creature, beside it, barely");
        }

        [Test]
        public void TheCommandAndTheFrameCarryHerStream()
        {
            using var world = Quiet();
            var loop = Loop(world);
            loop.Enqueue(SimCommands.LuckLaunch(-12.5e-6, 3.25e-6, 7.5e-6, 3.25e-6));
            loop.ApplyCommands();
            var jet = world.LuckJet;
            var b = world.Body.Body;
            Assert.That((jet.On, jet.OriginX - b.X, jet.OriginY - b.Y, jet.DirX, jet.DirY),
                Is.EqualTo((true, -12.5e-6, 3.25e-6, 1.0, 0.0)));
            loop.StepExactly(5);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.LuckOn, Is.True);
            Assert.That(frame.LuckOriginX, Is.EqualTo((float)(jet.OriginX - b.X)));
            Assert.That((frame.LuckDirX, frame.LuckDirY), Is.EqualTo((1f, 0f)));
            Assert.That(frame.LuckWidth, Is.EqualTo((float)P("luck.jet_width")));
            Assert.That(frame.LuckFront, Is.EqualTo((float)jet.Front));
            Assert.That(frame.LuckFront, Is.GreaterThan(0f));
            Assert.That(frame.LuckFrontWidth, Is.EqualTo((float)P("luck.front_width")));
            Assert.That(frame.LuckStrength, Is.EqualTo((float)world.LuckStrength));
            Assert.That(frame.Luck, Is.EqualTo((float)world.Luck));
            Assert.That((frame.LuckMax, frame.LuckStreamCost, frame.LuckFullLength, frame.LuckMaxSpeed),
                Is.EqualTo(((float)P("luck.max"), (float)P("luck.stream_cost"), (float)P("luck.full_length"), (float)P("luck.max_speed"))));
            loop.Enqueue(SimCommands.LuckStop());
            loop.ApplyCommands();
            Assert.That(world.LuckJet.On, Is.False);
        }
    }
}
