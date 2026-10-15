using System;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>
    /// A current blown along a band of water: from a tail to a head (an arrow), <see cref="HalfWidth"/> to each side of its
    /// axis, flowing at <see cref="Speed"/> along the axis. Across the band the flow falls as cos² to 0 at its edges, and
    /// along it as it nears the head (to 0 at the head, over one half-width), so it pushes what lies on the arrow towards
    /// the head and lets it go there. Positions are relative to an anchor (the creature), in metres.
    /// </summary>
    public sealed class DirectedCurrent
    {
        /// <summary>Whether it flows.</summary>
        public bool On { get; private set; }

        public double TailX { get; private set; }

        public double TailY { get; private set; }

        public double HeadX { get; private set; }

        public double HeadY { get; private set; }

        /// <summary>Half the band's width [m].</summary>
        public double HalfWidth { get; set; }

        /// <summary>Flow on the axis, away from the head's taper [m/s].</summary>
        public double Speed { get; private set; }

        /// <summary>Blows from (tailX, tailY) towards (headX, headY) at <paramref name="speed"/>.</summary>
        public void Blow(double tailX, double tailY, double headX, double headY, double speed)
        {
            TailX = tailX;
            TailY = tailY;
            HeadX = headX;
            HeadY = headY;
            Speed = Math.Max(0.0, speed);
            On = Length > 0.0 && Speed > 0.0;
        }

        public void Stop()
        {
            On = false;
            Speed = 0.0;
        }

        /// <summary>Tail to head [m].</summary>
        public double Length => Math.Sqrt((HeadX - TailX) * (HeadX - TailX) + (HeadY - TailY) * (HeadY - TailY));

        /// <summary>The flow at (x, y), relative to the anchor [m/s]; 0 outside the band or when off.</summary>
        public void At(double x, double y, out double ux, out double uy)
        {
            ux = uy = 0.0;
            double length = Length;
            if (!On || length <= 0.0 || HalfWidth <= 0.0)
                return;
            double ex = (HeadX - TailX) / length, ey = (HeadY - TailY) / length;
            double px = x - TailX, py = y - TailY;
            double along = px * ex + py * ey, across = px * ey - py * ex;
            if (along < 0.0 || along > length || Math.Abs(across) >= HalfWidth)
                return;
            double c = Math.Cos(0.5 * Math.PI * across / HalfWidth);
            double taper = Math.Min(1.0, (length - along) / HalfWidth);
            double u = Speed * c * c * taper;
            ux = u * ex;
            uy = u * ey;
        }
    }
}
