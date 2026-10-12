using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>A small cell the creature can meet: it swims by run and tumble and drifts with the water.</summary>
    public sealed class Prey
    {
        internal Prey()
        {
        }

        /// <summary>Centre, over the ground [m].</summary>
        public double X { get; internal set; }

        public double Y { get; internal set; }

        /// <summary>Direction it swims in [rad].</summary>
        public double Heading { get; internal set; }

        /// <summary>Radius [m].</summary>
        public double Radius { get; internal set; }

        /// <summary>Glucose equivalents it holds [mol], free and in polymer.</summary>
        public double Glucose { get; internal set; }

        /// <summary>The part of <see cref="Glucose"/> already small molecules [mol].</summary>
        public double Free { get; internal set; }

        public bool Active { get; internal set; }

        internal double UntilTumble { get; set; }
    }

    /// <summary>The prey's settings: content/parameters.json (prey.*), at the food packets' density. SI.</summary>
    public sealed class PreySettings
    {
        /// <summary>Radius of a prey cell [m].</summary>
        public double Radius { get; set; }

        /// <summary>Swimming speed during a run [m/s].</summary>
        public double RunSpeed { get; set; }

        /// <summary>Mean run between tumbles [s] (exponential); each tumble picks a new heading at random.</summary>
        public double MeanRun { get; set; }

        /// <summary>Glucose equivalents per volume [mol/m³] (as a food packet).</summary>
        public double Density { get; set; }

        /// <summary>Share of its glucose equivalents already small molecules.</summary>
        public double FreeFraction { get; set; }

        /// <summary>Half the side of the square window around the creature [m] (the food field's).</summary>
        public double HalfSize { get; set; }

        /// <summary>New prey head inwards within half this angle either side of the boundary's normal [rad].</summary>
        public double EntryAngleSpread { get; set; } = Math.PI / 2.0;

        /// <summary>Prey arriving per second [1/s] (the level's rate), whatever is already in the window.</summary>
        public double ArrivalsPerSecond { get; set; }

        /// <summary>Prey slots made at the start; more are added as needed.</summary>
        public int InitialCapacity { get; set; }

        public int Seed { get; set; }
    }

    /// <summary>
    /// Prey around the creature: small cells that arrive at the window's edge at the level's rate, swim by run and tumble at
    /// <see cref="PreySettings.RunSpeed"/> while drifting with the water, and leave when they cross the window's edge or are
    /// engulfed. Their content (glucose equivalents at the packet density, a sphere of their radius) is food only once a
    /// cell engulfs them. Deterministic for a given seed, with its own random numbers (the food field's do not change).
    /// </summary>
    public sealed class PreyField
    {
        private readonly Random _random;
        private readonly List<Prey> _prey = new List<Prey>();
        private double _untilNext;

        public PreyField(PreySettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(settings.Seed);
            for (int i = 0; i < settings.InitialCapacity; i++)
                _prey.Add(new Prey());
            _untilNext = settings.ArrivalsPerSecond > 0.0 ? Exponential() / settings.ArrivalsPerSecond : double.PositiveInfinity;
        }

        public PreySettings Settings { get; }

        /// <summary>Prey slots; only <see cref="Prey.Active"/> ones are in the window.</summary>
        public IReadOnlyList<Prey> Prey => _prey;

        /// <summary>Glucose equivalents in a fresh prey cell [mol]: a sphere of its radius at the packet density.</summary>
        public double Content => Settings.Density * 4.0 / 3.0 * Math.PI * Math.Pow(Settings.Radius, 3);

        /// <summary>Prey that have arrived since the start (diagnostics).</summary>
        public long Arrived { get; private set; }

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var p in _prey)
                    if (p.Active)
                        n++;
                return n;
            }
        }

        /// <summary>
        /// Moves the prey (their swimming plus the water's drift <paramref name="driftX"/>, <paramref name="driftY"/>
        /// [m/s]), tumbles those whose run ended, retires those that left the window centred on the creature at
        /// (<paramref name="cx"/>, <paramref name="cy"/>), and brings in new ones at its edge.
        /// </summary>
        public void Step(double dt, double cx, double cy, double driftX = 0.0, double driftY = 0.0)
        {
            double half = Settings.HalfSize;
            foreach (var p in _prey)
            {
                if (!p.Active)
                    continue;
                p.X += (Settings.RunSpeed * Math.Cos(p.Heading) + driftX) * dt;
                p.Y += (Settings.RunSpeed * Math.Sin(p.Heading) + driftY) * dt;
                p.UntilTumble -= dt;
                while (p.UntilTumble <= 0.0)
                {
                    p.Heading = 2.0 * Math.PI * _random.NextDouble();
                    p.UntilTumble += Exponential() * Settings.MeanRun;
                }
                if (Math.Abs(p.X - cx) > half + p.Radius || Math.Abs(p.Y - cy) > half + p.Radius)
                    p.Active = false;
            }
            _untilNext -= dt;
            while (_untilNext <= 0.0 && Settings.ArrivalsPerSecond > 0.0)
            {
                Enter(cx, cy);
                Arrived++;
                _untilNext += Exponential() / Settings.ArrivalsPerSecond;
            }
        }

        /// <summary>The rate of arrivals changes (a new level): the next arrival is drawn at the new rate.</summary>
        public void SetArrivals(double perSecond)
        {
            Settings.ArrivalsPerSecond = perSecond;
            _untilNext = perSecond > 0.0 ? Exponential() / perSecond : double.PositiveInfinity;
        }

        /// <summary>A prey cell at (x, y) swimming along <paramref name="heading"/> [rad] (development aid and tests).</summary>
        public Prey Add(double x, double y, double heading)
        {
            var p = FreeSlot();
            Fill(p, x, y, heading);
            return p;
        }

        /// <summary>Takes prey <paramref name="p"/> out of the window (a cell engulfed it).</summary>
        public void Remove(Prey p) => p.Active = false;

        /// <summary>Whether prey <paramref name="p"/> touches a cell at (x, y) of radius r (discs in the plane).</summary>
        public static bool Touches(Prey p, double x, double y, double r)
        {
            double dx = p.X - x, dy = p.Y - y, d = p.Radius + r;
            return dx * dx + dy * dy <= d * d;
        }

        private double Exponential() => -Math.Log(1.0 - _random.NextDouble());

        private Prey FreeSlot()
        {
            foreach (var p in _prey)
                if (!p.Active)
                    return p;
            var added = new Prey();
            _prey.Add(added);
            return added;
        }

        // A new prey cell on a random side of the window, heading inwards.
        private void Enter(double cx, double cy)
        {
            int side = _random.Next(4);
            double nx = side == 0 ? 1.0 : side == 1 ? -1.0 : 0.0, ny = side == 2 ? 1.0 : side == 3 ? -1.0 : 0.0;   // inward normal
            double along = (2.0 * _random.NextDouble() - 1.0) * Settings.HalfSize;
            double edge = Settings.HalfSize + Settings.Radius;
            double x = cx - nx * edge + (nx == 0.0 ? along : 0.0);
            double y = cy - ny * edge + (ny == 0.0 ? along : 0.0);
            double heading = Math.Atan2(ny, nx) + (_random.NextDouble() - 0.5) * Settings.EntryAngleSpread;
            Fill(FreeSlot(), x, y, heading);
        }

        private void Fill(Prey p, double x, double y, double heading)
        {
            p.X = x;
            p.Y = y;
            p.Heading = heading;
            p.Radius = Settings.Radius;
            p.Glucose = Content;
            p.Free = Settings.FreeFraction * p.Glucose;
            p.UntilTumble = Exponential() * Settings.MeanRun;
            p.Active = true;
        }
    }
}
