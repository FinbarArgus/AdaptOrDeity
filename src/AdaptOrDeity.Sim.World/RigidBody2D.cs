using System;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>
    /// The creature as one rigid body moving in the 2D sheet through water: position, orientation, and its velocity and
    /// spin relative to the water. Forces and torques (the flagella's thrust) act against linear Stokes drag, the regime
    /// of a microscopic swimmer (Reynolds number ~1e-5):
    /// <code>
    ///   M dv/dt = F - xi v          I dw/dt = T - xi_r w
    /// </code>
    /// Each step is integrated exactly for forces held over the step (the exponential solution), so the momentum balance
    /// M dv = F dt - xi dx and the energy balance (work done by the thrust = change in kinetic energy + heat dissipated by
    /// drag) hold to round-off at any step, although the velocity relaxes in ~1e-7 s. The ground moves under the water at
    /// the current, so the body's velocity over the ground is the current plus its velocity through the water.
    /// </summary>
    public sealed class RigidBody2D
    {
        /// <summary>Mass [kg], moment of inertia about the centre of mass [kg m²], drag [N s/m] and rotational drag [N m s].</summary>
        public void SetProperties(double mass, double inertia, double drag, double rotationalDrag)
        {
            if (!(mass > 0.0 && inertia > 0.0 && drag > 0.0 && rotationalDrag > 0.0))
                throw new ArgumentOutOfRangeException(nameof(mass), "mass, inertia and drags must be > 0");
            Mass = mass;
            Inertia = inertia;
            Drag = drag;
            RotationalDrag = rotationalDrag;
        }

        public double Mass { get; private set; } = 1.0;

        public double Inertia { get; private set; } = 1.0;

        public double Drag { get; private set; } = 1.0;

        public double RotationalDrag { get; private set; } = 1.0;

        /// <summary>Centre of mass over the ground [m].</summary>
        public double X { get; private set; }

        public double Y { get; private set; }

        /// <summary>Orientation of the body frame [rad].</summary>
        public double Angle { get; private set; }

        /// <summary>Velocity through the water [m/s] and spin [rad/s].</summary>
        public double Vx { get; private set; }

        public double Vy { get; private set; }

        public double Omega { get; private set; }

        /// <summary>Water velocity over the ground [m/s] (the current carries the body).</summary>
        public double CurrentVx { get; set; }

        public double CurrentVy { get; set; }

        /// <summary>Velocity over the ground [m/s].</summary>
        public double GroundVx => CurrentVx + Vx;

        public double GroundVy => CurrentVy + Vy;

        /// <summary>Kinetic energy relative to the water [J].</summary>
        public double KineticEnergy => 0.5 * Mass * (Vx * Vx + Vy * Vy) + 0.5 * Inertia * Omega * Omega;

        /// <summary>Work done on the body by the applied forces and torques since the start [J].</summary>
        public double Work { get; private set; }

        /// <summary>Energy dissipated by drag into the water as heat since the start [J].</summary>
        public double Dissipated { get; private set; }

        /// <summary>Mechanical power of the applied forces in the last step [W].</summary>
        public double Power { get; private set; }

        /// <summary>Linear impulse delivered by the applied forces since the start [N s] (x, y).</summary>
        public double ImpulseX { get; private set; }

        public double ImpulseY { get; private set; }

        /// <summary>Impulse taken by drag since the start [N s] (x, y).</summary>
        public double DragImpulseX { get; private set; }

        public double DragImpulseY { get; private set; }

        /// <summary>Moves the reference point without moving the body (when cells join and the centre of mass shifts).</summary>
        public void Shift(double dx, double dy)
        {
            X += dx;
            Y += dy;
        }

        /// <summary>Places the body at (x, y) over the ground [m], turned by <paramref name="angle"/> [rad], at rest in the water.</summary>
        public void Place(double x, double y, double angle)
        {
            X = x;
            Y = y;
            Angle = angle;
            Vx = Vy = Omega = 0.0;
        }

        /// <summary>Advances by dt with force (fx, fy) [N] and torque t [N m] held over the step.</summary>
        public void Step(double dt, double fx, double fy, double torque)
        {
            double ke0 = KineticEnergy;
            Advance(dt, Mass, Drag, fx, Vx, out double vx1, out double dx);
            Advance(dt, Mass, Drag, fy, Vy, out double vy1, out double dy);
            Advance(dt, Inertia, RotationalDrag, torque, Omega, out double w1, out double dth);
            // work = F . displacement through the water (exact for a force held over the step); the rest of it that is not
            // kinetic energy went into the water as heat
            double work = fx * dx + fy * dy + torque * dth;
            Vx = vx1;
            Vy = vy1;
            Omega = w1;
            X += dx + CurrentVx * dt;
            Y += dy + CurrentVy * dt;
            Angle += dth;
            Work += work;
            Dissipated += work - (KineticEnergy - ke0);
            Power = dt > 0.0 ? work / dt : 0.0;
            ImpulseX += fx * dt;
            ImpulseY += fy * dt;
            DragImpulseX += Drag * dx;
            DragImpulseY += Drag * dy;
        }

        /// <summary>
        /// Exact solution of m dv/dt = f - c v over dt: v relaxes to f/c with time constant m/c; dx is its integral.
        /// </summary>
        private static void Advance(double dt, double m, double c, double f, double v0, out double v1, out double dx)
        {
            double vt = f / c, tau = m / c;
            double decay = Math.Exp(-dt / tau);
            double oneMinus = OneMinusExp(dt / tau);   // 1 - exp(-dt/tau), accurate when dt << tau too
            v1 = vt + (v0 - vt) * decay;
            dx = vt * dt + (v0 - vt) * tau * oneMinus;
        }

        private static double OneMinusExp(double x) =>
            x < 1e-4 ? x * (1.0 - x * (0.5 - x / 6.0)) : 1.0 - Math.Exp(-x);
    }
}
