using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// The creature's motion against the analytic solution of its rigid body in Stokes flow: M dv/dt = F - xi v through
    /// the water (xi = 6 pi eta R), carried over the ground by the current U. With the motors running a cell's flagellum
    /// pushes with the cell model's thrust F; the velocity through the water relaxes to F/xi with time constant M/xi.
    /// </summary>
    public class MotionTests
    {
        // One game cell (the world's radius, viscosity, mass and thrust scale) with its flagellum based on its +x side,
        // so it pushes the cell towards -x, upstream of the current; F is its thrust with the motors fully on.
        private static (CreatureBody Body, double F, double Mass) OneCell()
        {
            using var world = new FermenterWorld();
            var cell = world.Cell(0);
            cell.MotorActivation = 1.0;
            for (int k = 0; k < 100; k++)
                cell.Step(0.1);   // the bundle forms (0.3 s) and the thrust settles
            double f = cell.FlagellumThrust;
            double mass = GameParameters.Default.Value("body.cell_density") * cell.AlgebraicValue("membrane__Vol");
            var body = new CreatureBody(world.CellRadius, cell.External("eta_mod_i_flagellum"), GameParameters.Default.Body());
            var angles = CreatureBody.DefaultAngles(GameParameters.Default.Value("motility.flagellum_spacing"));
            body.AddCell(0.0, 0.0, mass, angles);
            body.SetFlagellumAngle(0, 0, 0.0);
            return (body, f, mass);
        }

        [Test]
        public void AfterFiveFlagellarRunsTheCreatureIsWhereTheAnalyticSolutionPutsIt()
        {
            var (body, f, _) = OneCell();
            var b = body.Body;
            double xi = b.Drag, tau = b.Mass / xi, ux = b.CurrentVx, uy = b.CurrentVy;
            Assert.That(f, Is.GreaterThan(0.0));
            Assert.That(xi, Is.EqualTo(6.0 * Math.PI * body.Eta * body.CellRadius).Within(1e-12 * xi), "Stokes drag");
            const double dt = 0.1, run = 2.0, pause = 4.0;
            const int runs = 5;
            double v = 0.0, x = 0.0, t = 0.0;   // velocity through the water along x and its displacement, analytically
            for (int r = 0; r < runs; r++)
                foreach (var (duration, force) in new[] { (run, -f), (pause, 0.0) })
                {
                    int steps = (int)Math.Round(duration / dt);
                    for (int k = 0; k < steps; k++)
                        body.Step(dt, _ => 1.0, _ => force < 0.0 ? f : 0.0);
                    double vt = force / xi, decay = Math.Exp(-duration / tau);
                    x += vt * duration + (v - vt) * tau * (1.0 - decay);
                    v = vt + (v - vt) * decay;
                    t += duration;
                }
            double expectedX = ux * t + x, expectedY = uy * t;
            Assert.That(b.X, Is.EqualTo(expectedX).Within(1e-9 * Math.Abs(ux * t) + 1e-15), "x over the ground");
            Assert.That(b.Y, Is.EqualTo(expectedY).Within(1e-15), "no sideways motion");
            Assert.That(b.Angle, Is.EqualTo(0.0).Within(1e-15), "a flagellum through the centre does not turn it");
            TestContext.Out.WriteLine($"thrust {f:G3} N, F/xi {f / xi * 1e6:F2} um/s against the current {ux * 1e6:F2} um/s; " +
                                      $"after {runs} runs ({t:F0} s) x = {b.X * 1e6:F2} um");
        }

        [Test]
        public void AtPeakTideTheFlagellaOutrunTheCurrent()
        {
            // During a run a cell pushing straight upstream moves over the ground at U - F/xi: with the game's numbers its
            // swimming beats the tidal current's amplitude, so it makes headway even at peak tide.
            var (body, f, _) = OneCell();
            var b = body.Body;
            double swim = f / b.Drag, u = b.CurrentVx;
            for (int k = 0; k < 100; k++)
                body.Step(0.1, _ => 1.0, _ => f);   // 10 s running upstream
            Assert.That(b.GroundVx, Is.EqualTo(u - swim).Within(1e-9 * swim));
            Assert.That(b.GroundVx, Is.LessThan(0.0), "upstream against the peak tide");
            TestContext.Out.WriteLine($"swimming {swim * 1e6:F2} um/s against a tide of up to {u * 1e6:F2} um/s");
        }

        [Test]
        public void TheCurrentIsATide()
        {
            // The free current is the amplitude times cos(2 pi t / period): it slows, turns and flows back.
            using var world = new FermenterWorld();
            double amp = GameParameters.Default.Value("environment.current_speed"), period = GameParameters.Default.Value("environment.tide_period");
            double t = 0.0;
            foreach (double target in new[] { 0.0, 0.25 * period, 0.5 * period })
            {
                while (t < target - 1e-9)
                {
                    world.Step(t, world.MacroDt);
                    t += world.MacroDt;
                }
                world.Step(t, world.MacroDt);
                Assert.That(world.FreeCurrentX, Is.EqualTo(amp * Math.Cos(2.0 * Math.PI * t / period)).Within(1e-12 * amp), $"t = {t:F0} s");
                t += world.MacroDt;
            }
        }

        [Test]
        public void TheFlagellumsProtonCostIsScaledWithItsThrust()
        {
            using var world = new FermenterWorld();
            var fresh = new FermenterCell(TraitCatalog.Fermenter);
            Assert.That(world.Cell(0).External("n_H_rev_mod_i_flagellum"),
                Is.EqualTo(GameParameters.Default.Value("motility.proton_cost_scale") * fresh.External("n_H_rev_mod_i_flagellum")));
        }
    }
}
