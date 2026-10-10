using System;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class RigidBodyTests
    {
        // a 2 um cell in seawater: m ~ 7e-15 kg, xi = 6 pi eta R ~ 2e-8 N s/m, so the velocity relaxes in ~3e-7 s
        private const double R = 1e-6, Eta = 1.08e-3, Mass = 6.9e-15;

        private static CreatureBody OneCell(double[]? angles = null)
        {
            var body = new CreatureBody(R, Eta, new RigidBody2D());
            body.AddCell(0.0, 0.0, Mass, angles ?? CreatureBody.DefaultAngles(0.5));
            return body;
        }

        [Test]
        public void MomentumAndEnergyBalanceExactlyAtAnyStep()
        {
            // Changing forces and torques, steps from far below to far above the relaxation time: M dv = F dt - xi dx and
            // work = kinetic energy + heat to round-off.
            var b = new RigidBody2D();
            b.SetProperties(Mass, 0.5 * Mass * R * R, 6 * Math.PI * Eta * R, 8 * Math.PI * Eta * R * R * R);
            var rng = new Random(4);
            foreach (double dt in new[] { 1e-9, 1e-7, 1e-3, 0.1 })
                for (int i = 0; i < 200; i++)
                    b.Step(dt, 1e-13 * (rng.NextDouble() - 0.5), 1e-13 * (rng.NextDouble() - 0.5), 1e-19 * (rng.NextDouble() - 0.5));
            // to round-off of the impulses summed (they nearly cancel: the body's momentum is tiny next to them)
            Assert.That(b.Mass * b.Vx, Is.EqualTo(b.ImpulseX - b.DragImpulseX)
                .Within(1e-9 * (Math.Abs(b.ImpulseX) + Math.Abs(b.DragImpulseX)) + 1e-30));
            Assert.That(b.Mass * b.Vy, Is.EqualTo(b.ImpulseY - b.DragImpulseY)
                .Within(1e-9 * (Math.Abs(b.ImpulseY) + Math.Abs(b.DragImpulseY)) + 1e-30));
            Assert.That(b.Work, Is.EqualTo(b.KineticEnergy + b.Dissipated).Within(1e-9 * Math.Abs(b.Work) + 1e-40));
            Assert.That(b.Dissipated, Is.GreaterThanOrEqualTo(0.0), "drag only ever takes energy out, as heat");
        }

        [Test]
        public void TerminalSpeedAndSpinAreForceOverDrag()
        {
            var b = new RigidBody2D();
            double xi = 6 * Math.PI * Eta * R, xr = 8 * Math.PI * Eta * R * R * R;
            b.SetProperties(Mass, 0.5 * Mass * R * R, xi, xr);
            b.Step(0.1, 3e-13, 0.0, 2e-19);
            Assert.That(b.Vx, Is.EqualTo(3e-13 / xi).Within(1e-12 * 3e-13 / xi));
            Assert.That(b.Omega, Is.EqualTo(2e-19 / xr).Within(1e-12 * 2e-19 / xr));
            // from rest the body lags the terminal speed by its relaxation time tau = m/xi (~3e-7 s): x = vt (dt - tau)
            Assert.That(b.X, Is.EqualTo(3e-13 / xi * (0.1 - Mass / xi)).Within(1e-12 * 3e-13 / xi * 0.1));
        }

        [Test]
        public void AFlagellumAtTheBackDrivesStraightAhead()
        {
            var body = OneCell();
            const double f = 2.6e-13;   // ~15 um/s against 6 pi eta R
            for (int i = 0; i < 100; i++)
                body.Step(0.1, _ => 1.0, _ => f);
            var b = body.Body;
            Assert.That(b.Vx, Is.EqualTo(f / (6 * Math.PI * Eta * R)).Within(1e-9));
            Assert.That(Math.Abs(b.Vy), Is.LessThan(1e-15));
            Assert.That(Math.Abs(b.Angle), Is.LessThan(1e-12), "thrust through the centre turns nothing");
        }

        [Test]
        public void SymmetricFlagellaCancelTheirTorqueAndAnOffsetOneTurnsTheBody()
        {
            var two = OneCell(new[] { Math.PI - 0.6, Math.PI + 0.6, 0, 0, 0, 0, 0, 0 });
            two.Step(0.1, _ => 2.0, _ => 1e-13);
            Assert.That(Math.Abs(two.Body.Omega), Is.LessThan(1e-12));
            Assert.That(two.Body.Vx, Is.GreaterThan(0.0));
            // one flagellum whose thrust is off by 0.2 rad: a torque, so the run curves
            var bent = OneCell();
            bent.NewRun(new FixedRandom(), 0.2);
            bent.Step(0.1, _ => 1.0, _ => 2.6e-13);
            Assert.That(Math.Abs(bent.Body.Omega), Is.GreaterThan(0.1), "rad/s");
            Assert.That(Math.Abs(bent.Torque), Is.EqualTo(R * 2.6e-13 * Math.Abs(Math.Sin(0.2 * FixedRandom.Normal))).Within(1e-25));
        }

        [Test]
        public void TheBodyCarriesItsCellsAsItTurns()
        {
            var body = new CreatureBody(R, Eta, new RigidBody2D());
            // the first cell's flagellum on its side (pi/2): it pushes across the line through the centre of mass
            body.AddCell(0.0, 0.0, Mass, new[] { 0.5 * Math.PI, 0, 0, 0, 0, 0, 0, 0 });
            body.AddCell(2.3e-6, 0.0, Mass, CreatureBody.DefaultAngles(0.5));
            body.RelativePosition(1, out double x0, out double y0);
            Assert.That(x0, Is.EqualTo(1.15e-6).Within(1e-15), "the centre of mass is between them");
            body.WorldPosition(0, out double wx, out _);
            Assert.That(wx, Is.EqualTo(0.0).Within(1e-15), "adding a cell does not move the first one");
            for (int i = 0; i < 50; i++)
                body.Step(0.1, _ => 1.0, k => k == 0 ? 2e-13 : 0.0);   // only the first cell pushes: off-centre, it turns
            body.RelativePosition(1, out double x1, out double y1);
            Assert.That(Math.Sqrt(x1 * x1 + y1 * y1), Is.EqualTo(1.15e-6).Within(1e-15), "rigid: distances kept");
            Assert.That(Math.Abs(body.Body.Angle), Is.GreaterThan(0.01));
        }

        [Test]
        public void OneCellSlowsToAStopAfterOneImpulse()
        {
            // One short push (impulse J), then nothing: the cell coasts and drag takes all its momentum. Its velocity
            // decays as exp(-t/tau) with tau = m/xi (~0.3 us in water), it travels exactly J/xi in all, and stops.
            var body = OneCell();
            var b = body.Body;
            double xi = 6 * Math.PI * Eta * R, tau = Mass / xi;
            const double f = 2.6e-13, push = 1e-9;   // a 1 ns push of the thrust of one flagellum
            body.Step(push, _ => 1.0, _ => f);
            double v0 = b.Vx, x0 = b.X;
            Assert.That(v0, Is.GreaterThan(0.0));
            double last = v0;
            for (int i = 1; i <= 40; i++)
            {
                body.Step(0.1 * tau, _ => 1.0, _ => 0.0);
                Assert.That(b.Vx, Is.LessThan(last), "slowing down every step");
                Assert.That(b.Vx, Is.EqualTo(v0 * Math.Exp(-0.1 * tau * i / tau)).Within(1e-9 * v0), "exponential decay");
                last = b.Vx;
            }
            for (int i = 0; i < 100; i++)
                body.Step(0.1, _ => 1.0, _ => 0.0);
            Assert.That(b.Vx, Is.EqualTo(0.0).Within(1e-30), "stopped");
            Assert.That(b.ImpulseX - b.DragImpulseX, Is.EqualTo(0.0).Within(1e-12 * b.ImpulseX), "drag took all the momentum");
            Assert.That(b.X, Is.EqualTo(f * push / xi).Within(1e-6 * f * push / xi), "it travels J/xi in all");
            Assert.That(b.Dissipated, Is.EqualTo(b.Work).Within(1e-9 * b.Work), "all the work ends as heat in the water");
            Assert.That(b.X - x0, Is.GreaterThan(0.0));
        }

        /// <summary>A Random whose Box-Muller draw is a fixed normal deviate.</summary>
        private sealed class FixedRandom : Random
        {
            public static readonly double Normal = Math.Sqrt(-2.0 * Math.Log(1.0 - 0.3)) * Math.Cos(2.0 * Math.PI * 0.3);
            private bool _first = true;

            public override double NextDouble()
            {
                _first = !_first;
                return 0.3;
            }
        }
    }
}
