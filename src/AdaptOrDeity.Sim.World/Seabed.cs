using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>The seabed's settings (content/parameters.json seabed.*). Every value is SI.</summary>
    public sealed class SeabedSettings
    {
        /// <summary>Height of the ground below where the creature starts [m] (the world's y grows upwards).</summary>
        public double GroundDepth { get; set; } = 15e-6;

        /// <summary>
        /// Thickness of the boundary layer over the ground [m]: the current grows linearly from zero at the ground to its
        /// full speed at this height (no slip at the bed).
        /// </summary>
        public double BoundaryLayer { get; set; } = 10e-6;

        /// <summary>Width of a stretch of seabed holding one structure [m] (structures are generated per stretch).</summary>
        public double StretchWidth { get; set; } = 40e-6;

        /// <summary>Smallest and largest rock radius [m].</summary>
        public double RockRadiusMin { get; set; } = 2e-6;

        public double RockRadiusMax { get; set; } = 7e-6;

        /// <summary>Length of the sheltered wake behind a rock, in rock radii: the current recovers linearly over it.</summary>
        public double WakeLength { get; set; } = 4.0;

        public int Seed { get; set; } = 11;
    }

    /// <summary>A rock: a disc of the 2D world resting on or above the ground.</summary>
    public readonly struct Rock
    {
        public Rock(double x, double y, double radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public double X { get; }

        public double Y { get; }

        public double Radius { get; }
    }

    /// <summary>
    /// The ground at the bottom of the world and the rocks on it, for bodies to swim towards, knock into, shelter behind
    /// and climb over. The world is a vertical slice of water: x along the current, y upwards; the ground is the line
    /// y = <see cref="GroundY"/>, solid below. Each stretch of seabed (<see cref="SeabedSettings.StretchWidth"/>) holds one
    /// structure built of rock discs, drawn from the seed and the stretch's index, so the seabed is endless and the same
    /// every time: a boulder, a stack of rocks, or an arch (two pillars and a lintel) to swim under. The current slows in
    /// the boundary layer over the ground and in the wake behind each rock (<see cref="CurrentFactor"/>). Bodies do not
    /// enter the ground or rocks (<see cref="PushOut"/>); chemicals in the environment field are not blocked by them.
    /// </summary>
    public sealed class Seabed
    {
        private readonly Dictionary<long, Rock[]> _stretches = new Dictionary<long, Rock[]>();

        public Seabed(SeabedSettings settings, double startY)
        {
            Settings = settings;
            GroundY = startY - settings.GroundDepth;
            for (long k = -PrebuiltStretches; k <= PrebuiltStretches; k++)
                Stretch(k);   // the seabed near the start is built up front, so stepping there allocates nothing
        }

        /// <summary>Stretches built when the seabed is made, each side of the start (further ones are built on first use).</summary>
        public const int PrebuiltStretches = 50;

        public SeabedSettings Settings { get; }

        /// <summary>Height of the ground [m].</summary>
        public double GroundY { get; }

        /// <summary>The rocks whose discs reach between x0 and x1 [m], into <paramref name="into"/> (cleared first).</summary>
        public void RocksBetween(double x0, double x1, List<Rock> into)
        {
            into.Clear();
            double w = Settings.StretchWidth;
            long first = (long)Math.Floor(x0 / w) - 1, last = (long)Math.Floor(x1 / w) + 1;
            for (long k = first; k <= last; k++)
                foreach (var r in Stretch(k))
                    if (r.X + r.Radius >= x0 && r.X - r.Radius <= x1)
                        into.Add(r);
        }

        // The stretches whose structures can reach between x0 and x1 (structures stay within a stretch and a rock radius).
        private void Stretches(double x0, double x1, out long first, out long last)
        {
            double w = Settings.StretchWidth;
            first = (long)Math.Floor(x0 / w) - 1;
            last = (long)Math.Floor(x1 / w) + 1;
        }

        // The structure of stretch k (cached; deterministic in the seed and k; safe to call from several threads).
        private Rock[] Stretch(long k)
        {
            lock (_stretches)
                if (_stretches.TryGetValue(k, out var cached))
                    return cached;
            Rock[] rocks;
            unchecked
            {
                int seed = Settings.Seed * 486187739 + (int)k * 16777619 + (int)(k >> 32);
                var random = new Random(seed);
                double w = Settings.StretchWidth, rMin = Settings.RockRadiusMin, rMax = Settings.RockRadiusMax;
                double x = (k + 0.2 + 0.6 * random.NextDouble()) * w;
                double r = rMin + (rMax - rMin) * random.NextDouble();
                var list = new List<Rock>();
                switch (random.Next(3))
                {
                    case 0:   // a boulder, partly sunk into the ground
                        list.Add(new Rock(x, GroundY + 0.6 * r, r));
                        break;
                    case 1:   // a stack: discs of shrinking size, each resting on the one below
                    {
                        double y = GroundY + 0.6 * r;
                        int n = 2 + random.Next(2);
                        for (int i = 0; i < n; i++)
                        {
                            list.Add(new Rock(x, y, r));
                            double next = 0.7 * r;
                            y += 0.85 * (r + next);
                            r = next;
                        }
                        break;
                    }
                    default:   // an arch: two pillars and a lintel across them, a gap to swim under
                    {
                        double pr = Math.Max(rMin, 0.5 * r), span = 3.0 * r, height = 2.0 * r;
                        for (double y = GroundY + pr; y <= GroundY + height; y += 1.5 * pr)
                        {
                            list.Add(new Rock(x - 0.5 * span, y, pr));
                            list.Add(new Rock(x + 0.5 * span, y, pr));
                        }
                        double top = GroundY + height + pr;
                        for (double lx = x - 0.5 * span; lx <= x + 0.5 * span + 1e-12; lx += 1.5 * pr)
                            list.Add(new Rock(lx, top, pr));
                        break;
                    }
                }
                rocks = list.ToArray();
            }
            lock (_stretches)
                _stretches[k] = rocks;
            return rocks;
        }

        /// <summary>
        /// The share of the free-stream current at (x, y): 0 at the ground, rising linearly to 1 at the top of the boundary
        /// layer; inside a rock 0; in a rock's wake (downstream, within its height band) it recovers linearly from 0 at the
        /// rock to 1 at <see cref="SeabedSettings.WakeLength"/> radii. The current flows along +x.
        /// </summary>
        public double CurrentFactor(double x, double y)
        {
            double h = y - GroundY;
            if (h <= 0.0)
                return 0.0;
            double f = Math.Min(1.0, h / Settings.BoundaryLayer);
            double reach = Settings.RockRadiusMax * (Settings.WakeLength + 1.0);
            Stretches(x - reach, x + Settings.RockRadiusMax, out long first, out long last);
            for (long k = first; k <= last; k++)
            foreach (var r in Stretch(k))
            {
                double dx = x - r.X, dy = y - r.Y;
                if (dx * dx + dy * dy <= r.Radius * r.Radius)
                    return 0.0;
                if (dx > 0.0 && Math.Abs(dy) < r.Radius)
                    f *= Math.Min(1.0, dx / (Settings.WakeLength * r.Radius));
            }
            return f;
        }

        /// <summary>
        /// Moves a disc of radius <paramref name="radius"/> at (x, y) out of the ground and any rock it overlaps, along
        /// their outward normals. Returns whether it touched anything, with (nx, ny) the outward normal of the deepest
        /// contact.
        /// </summary>
        public bool PushOut(ref double x, ref double y, double radius, out double nx, out double ny)
        {
            nx = ny = 0.0;
            bool touched = false;
            double deepest = 0.0;
            for (int pass = 0; pass < 3; pass++)   // a disc wedged between rocks settles in a few passes
            {
                bool moved = false;
                double below = GroundY + radius - y;
                if (below > 0.0)
                {
                    y += below;
                    moved = touched = true;
                    if (below > deepest)
                    {
                        deepest = below;
                        nx = 0.0;
                        ny = 1.0;
                    }
                }
                Stretches(x - radius, x + radius, out long first, out long last);
                for (long k = first; k <= last; k++)
                foreach (var r in Stretch(k))
                {
                    double dx = x - r.X, dy = y - r.Y, d = Math.Sqrt(dx * dx + dy * dy), overlap = r.Radius + radius - d;
                    if (overlap <= 0.0)
                        continue;
                    double ux = d > 0.0 ? dx / d : 0.0, uy = d > 0.0 ? dy / d : 1.0;
                    x += ux * overlap;
                    y += uy * overlap;
                    moved = touched = true;
                    if (overlap > deepest)
                    {
                        deepest = overlap;
                        nx = ux;
                        ny = uy;
                    }
                }
                if (!moved)
                    break;
            }
            return touched;
        }

        /// <summary>As <see cref="Collide"/> for a body of one disc at (x, y).</summary>
        public bool CollideOne(RigidBody2D body, double x, double y, double radius)
        {
            double x1 = x, y1 = y;
            if (!PushOut(ref x1, ref y1, radius, out double nx, out double ny))
                return false;
            body.Push(x1 - x, y1 - y);
            body.StopInto(nx, ny);
            return true;
        }

        /// <summary>
        /// Keeps a rigid body whose discs (radius r, at the given world positions) touch the seabed out of it: the body is
        /// moved out by the deepest overlap of any of its discs, and its velocity over the ground into the surface removed
        /// (it rests against it; Stokes flow has no bounce). Returns whether it touched.
        /// </summary>
        public bool Collide(RigidBody2D body, int discs, Func<int, (double X, double Y)> position, double radius)
        {
            double sx = 0.0, sy = 0.0, nx = 0.0, ny = 0.0, deepest = 0.0;
            for (int i = 0; i < discs; i++)
            {
                var (x0, y0) = position(i);
                double x = x0, y = y0;
                if (!PushOut(ref x, ref y, radius, out double ux, out double uy))
                    continue;
                double d = Math.Sqrt((x - x0) * (x - x0) + (y - y0) * (y - y0));
                if (d > deepest)
                {
                    deepest = d;
                    sx = x - x0;
                    sy = y - y0;
                    nx = ux;
                    ny = uy;
                }
            }
            if (deepest <= 0.0)
                return false;
            body.Push(sx, sy);
            body.StopInto(nx, ny);
            return true;
        }
    }
}
