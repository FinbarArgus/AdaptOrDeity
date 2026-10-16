using System;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>
    /// A jet of current through the water along a line: it arrives from far upstream (beyond where it entered the view),
    /// runs along the line through its origin in direction (DirX, DirY), and its front moves on downstream at the jet's
    /// speed. In world coordinates [m], drifting with the water it is in.
    /// <para>
    /// The flow is the curl of a streamfunction, psi(s, n) = Q(n) F(s), with s along the line from the origin and n across
    /// it: Q(n) = U sigma sqrt(pi/2) erf(n / (sigma sqrt 2)) is the flux of a Gaussian profile U exp(-n²/2sigma²), and F(s)
    /// = (1 - tanh((s - front)/L))/2 is 1 upstream of the front and 0 beyond it. So the water is never made or lost: the
    /// flow is divergence-free everywhere, the profile is carried in from upstream, and where the front advances it pushes
    /// the water ahead of it aside (u_n = -Q F') as a starting jet does.
    /// </para>
    /// <para>
    /// While driven, the jet spins up to its speed with time constant SpinUp and keeps its width. Once the drive stops it
    /// coasts: its momentum per unit length (U sigma) is kept as it spreads (sigma² grows by 2 Spread t), so it widens and
    /// slows, and friction takes it away with time constant Friction. It ends when its speed has fallen below 1% of the
    /// driven speed.
    /// </para>
    /// </summary>
    public sealed class Jet
    {
        private static readonly double SqrtHalfPi = Math.Sqrt(0.5 * Math.PI), Sqrt2 = Math.Sqrt(2.0);
        private double _driven, _driveLeft;

        /// <summary>Whether it flows.</summary>
        public bool On { get; private set; }

        /// <summary>A point on its axis [m]: where it entered the view, drifting with the water.</summary>
        public double OriginX { get; private set; }

        public double OriginY { get; private set; }

        /// <summary>Its direction (a unit vector).</summary>
        public double DirX { get; private set; }

        public double DirY { get; private set; }

        /// <summary>Flow on the axis upstream of the front [m/s].</summary>
        public double Speed { get; private set; }

        /// <summary>Width of the Gaussian profile across the axis [m] (sigma).</summary>
        public double Width { get; private set; }

        /// <summary>Distance of the front from the origin along the axis [m].</summary>
        public double Front { get; private set; }

        /// <summary>Length over which the front rises (L) [m].</summary>
        public double FrontWidth { get; set; } = 2e-6;

        /// <summary>Time constant of spinning up while driven [s].</summary>
        public double SpinUp { get; set; } = 0.5;

        /// <summary>Lateral spread once the drive stops: sigma² grows by 2 Spread per second [m²/s].</summary>
        public double Spread { get; set; } = 8e-12;

        /// <summary>Time constant of friction taking the coasting jet's momentum [s].</summary>
        public double Friction { get; set; } = 6.0;

        /// <summary>Whether it is still being driven.</summary>
        public bool Driven => On && _driveLeft > 1e-9;

        /// <summary>
        /// Starts a jet through (x, y) in direction (dx, dy), driven towards <paramref name="speed"/> for
        /// <paramref name="drive"/> seconds with profile width <paramref name="width"/>; it starts at rest, its front at
        /// the origin.
        /// </summary>
        public void Launch(double x, double y, double dx, double dy, double speed, double width, double drive)
        {
            double norm = Math.Sqrt(dx * dx + dy * dy);
            if (norm <= 0.0 || speed <= 0.0 || width <= 0.0)
            {
                Stop();
                return;
            }
            OriginX = x;
            OriginY = y;
            DirX = dx / norm;
            DirY = dy / norm;
            _driven = speed;
            _driveLeft = drive;
            Speed = 0.0;
            Width = width;
            Front = 0.0;
            On = true;
        }

        public void Stop()
        {
            On = false;
            Speed = 0.0;
            _driveLeft = 0.0;
        }

        /// <summary>Advances it by <paramref name="dt"/> in water drifting at (driftX, driftY) [m/s].</summary>
        public void Step(double dt, double driftX, double driftY)
        {
            if (!On)
                return;
            OriginX += driftX * dt;
            OriginY += driftY * dt;
            if (_driveLeft > 1e-9)
            {
                Speed = _driven + (Speed - _driven) * Math.Exp(-dt / Math.Max(SpinUp, 1e-9));
                _driveLeft = Math.Max(0.0, _driveLeft - dt);
            }
            else
            {
                // coasting: momentum per unit length (Speed x Width) kept as it spreads, less friction
                double wider = Math.Sqrt(Width * Width + 2.0 * Spread * dt);
                Speed *= Width / wider * Math.Exp(-dt / Math.Max(Friction, 1e-9));
                Width = wider;
                if (Speed < 0.01 * _driven)
                {
                    Stop();
                    return;
                }
            }
            Front += Speed * dt;
        }

        /// <summary>Its momentum per unit length along the axis, upstream of the front: the integral of the flow across it
        /// [m²/s] (U sigma sqrt(2 pi)).</summary>
        public double FluxAcross => On ? Speed * Width * Math.Sqrt(2.0 * Math.PI) : 0.0;

        /// <summary>The flow at the world point (x, y) [m/s]; 0 when it is off.</summary>
        public void At(double x, double y, out double ux, out double uy)
        {
            ux = uy = 0.0;
            if (!On || Speed <= 0.0)
                return;
            double px = x - OriginX, py = y - OriginY;
            double s = px * DirX + py * DirY, n = -px * DirY + py * DirX;   // along, and across (to the left of the flow)
            double z = n / Width;
            if (Math.Abs(z) > 8.0 && Math.Abs(s - Front) > 12.0 * FrontWidth)
                return;   // far from the jet and its front: below 1e-9 of its speed (exp(-32), tanh's tail)
            double th = Math.Tanh((s - Front) / FrontWidth);
            double f = 0.5 * (1.0 - th), df = -0.5 * (1.0 - th * th) / FrontWidth;
            double along = Speed * Math.Exp(-0.5 * z * z) * f;
            double across = -Speed * Width * SqrtHalfPi * Erf(z / Sqrt2) * df;
            ux = along * DirX - across * DirY;
            uy = along * DirY + across * DirX;
        }

        /// <summary>The error function (Abramowitz and Stegun 7.1.26: absolute error below 1.5e-7).</summary>
        public static double Erf(double x)
        {
            double t = 1.0 / (1.0 + 0.3275911 * Math.Abs(x));
            double y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t
                * Math.Exp(-x * x);
            return x >= 0.0 ? y : -y;
        }
    }
}
