using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>A trace species a food packet carries: something a cell could smell once it has the receptor.</summary>
    public sealed class TraceSpecies
    {
        public TraceSpecies(string id, string name, string source)
        {
            Id = id;
            Name = name;
            Source = source;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>What releases it (what the smell tells the cell).</summary>
        public string Source { get; }
    }

    /// <summary>How a food packet moves: lying still on the ground, or carried by another creature.</summary>
    public enum PacketKind
    {
        /// <summary>Food lying in the environment (or a dead creature): still over the ground.</summary>
        Lying,

        /// <summary>Food carried by another creature (prey, or an ally bringing it): moves with its carrier.</summary>
        Carried,
    }

    /// <summary>A patch of food: glucose plus traces of other species, in a disc of the 2D sheet.</summary>
    public sealed class FoodPacket
    {
        internal FoodPacket(int traceCount) => Traces = new double[traceCount];

        public PacketKind Kind { get; internal set; }

        /// <summary>Centre, over the ground [m].</summary>
        public double X { get; internal set; }

        public double Y { get; internal set; }

        /// <summary>Velocity over the ground [m/s] (0 for lying food).</summary>
        public double Vx { get; internal set; }

        public double Vy { get; internal set; }

        /// <summary>Radius [m].</summary>
        public double Radius { get; internal set; }

        /// <summary>Glucose left [mol], free and in polymer.</summary>
        public double Glucose { get; internal set; }

        /// <summary>
        /// The part of <see cref="Glucose"/> already small molecules [mol]: it leaks out by diffusion with no enzymes
        /// (<see cref="FoodField.Leak"/>); the rest is polymer that only enzymes release (<see cref="FoodField.Hydrolyse"/>).
        /// </summary>
        public double Free { get; internal set; }

        /// <summary>Relative strength of each trace species (1 for the main one), in <see cref="FoodField.TraceSpecies"/> order.</summary>
        public double[] Traces { get; }

        /// <summary>The trace species this packet is richest in.</summary>
        public int MainTrace { get; internal set; }

        public bool Active { get; internal set; }

        /// <summary>Where a stuck packet sits on the body, in the body frame [m].</summary>
        internal double LocalX { get; set; }

        internal double LocalY { get; set; }

        /// <summary>True once the packet has met a cell: it stays with the creature until it is eaten.</summary>
        public bool Stuck { get; internal set; }
    }

    /// <summary>
    /// The food field's settings: content/parameters.json (food.*), with the background glucose and sheet depth from the
    /// cell kernel. Every value is canonical SI.
    /// </summary>
    public sealed class FoodSettings
    {
        /// <summary>Glucose between packets [mol/m³].</summary>
        public double BackgroundGlucose { get; set; }

        /// <summary>Share of a fresh packet's glucose already small molecules (it leaks without enzymes).</summary>
        public double FreeFraction { get; set; }

        /// <summary>Rate constant of the free part leaking out of a packet [1/s].</summary>
        public double FreeLeakRate { get; set; }

        /// <summary>
        /// Glucose equivalents per volume of a packet [mol/m³]: a dead cell's or organic particle's carbon counted as
        /// glucose (content: cell density × dry share × carbon share / (carbon's molar mass × 6)).
        /// </summary>
        public double PacketDensity { get; set; }

        /// <summary>
        /// Glucose equivalents in a fresh packet [mol]: a sphere of <see cref="PacketRadius"/> at
        /// <see cref="PacketDensity"/>. Contact with a cell uses the disc of that radius in the plane (the sphere's equator).
        /// </summary>
        public double PacketAmount => SphereAmount(PacketRadius);

        /// <summary>Glucose equivalents in a sphere of radius <paramref name="radius"/> at the packet density [mol].</summary>
        public double SphereAmount(double radius) => PacketDensity * 4.0 / 3.0 * Math.PI * radius * radius * radius;

        /// <summary>Glucose a cell touching a packet sees at its membrane while the packet lasts [mol/m³].</summary>
        public double ContactGlucose { get; set; }

        /// <summary>Concentration of a packet's main trace species at a touching membrane [mol/m³].</summary>
        public double ContactTrace { get; set; }

        /// <summary>The other trace species, relative to the main one.</summary>
        public double PacketTraceMinorShare { get; set; }

        /// <summary>Radius of a fresh packet [m]; a packet keeps its density as it is eaten, so its volume shrinks in
        /// proportion to the glucose left.</summary>
        public double PacketRadius { get; set; }

        /// <summary>Speed of a creature carrying food [m/s].</summary>
        public double CarrierSpeed { get; set; }

        /// <summary>Food lying on the ground per area when the field is first filled [1/m²].</summary>
        public double LyingDensity { get; set; }

        /// <summary>
        /// Food packets arriving per second [1/s] (the level's and region's rate), whatever is already in the field: a
        /// colony holding much food still gets new food at this rate.
        /// </summary>
        public double ArrivalsPerSecond { get; set; }

        /// <summary>Share of arrivals brought by other creatures; the rest lie on the ground and come into view.</summary>
        public double CarriedShare { get; set; }

        /// <summary>A packet with less than this share of its glucose left is used up and removed.</summary>
        public double UsedUpFraction { get; set; }

        /// <summary>Carried packets head inwards within half this angle either side of the boundary's normal [rad].</summary>
        public double EntryAngleSpread { get; set; }

        /// <summary>Half the side of the square field around the creature [m].</summary>
        public double HalfSize { get; set; }

        /// <summary>Packet slots made at the start; more are added as needed (there is no limit).</summary>
        public int InitialPacketCapacity { get; set; }

        /// <summary>Random seed (determinism).</summary>
        public int Seed { get; set; }

        public IReadOnlyList<TraceSpecies> TraceSpecies { get; set; } = Array.Empty<TraceSpecies>();
    }

    /// <summary>
    /// Food around the creature: packets of glucose, each with traces of other species, in a square window that follows
    /// the creature over the ground. A packet lies still (food on the ground) or is carried by another creature; as the
    /// creature drifts or swims, lying food enters the window ahead and leaves behind it. A cell in contact with a packet
    /// sees the packet's glucose (on top of the background) at its membrane and takes it up through its own carriers;
    /// the packet then stays with the creature and loses what the cells take until it is used up. Traces are recorded at
    /// the cell surface for sensing later. Deterministic for a given seed. Its settings come from content
    /// (<see cref="FoodSettings"/>). A stand-in for the environment field of the transport graph.
    /// </summary>
    public sealed class FoodField
    {
        private readonly Random _random;
        private readonly List<FoodPacket> _packets = new List<FoodPacket>();
        private double _untilNext, _angle;

        public FoodField(FoodSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _random = new Random(settings.Seed);
            TraceSpecies = settings.TraceSpecies;
            BackgroundGlucose = settings.BackgroundGlucose;
            for (int i = 0; i < settings.InitialPacketCapacity; i++)
                _packets.Add(new FoodPacket(TraceSpecies.Count));
            _untilNext = Exponential() / Math.Max(settings.ArrivalsPerSecond, 1e-300);
        }

        public FoodSettings Settings { get; }

        public IReadOnlyList<TraceSpecies> TraceSpecies { get; }

        /// <summary>Packet slots; only <see cref="FoodPacket.Active"/> ones are in the field.</summary>
        public IReadOnlyList<FoodPacket> Packets => _packets;

        public double HalfSize => Settings.HalfSize;

        /// <summary>Glucose between packets [mol/m³] (the player's environment control may change it).</summary>
        public double BackgroundGlucose { get; set; }

        public double PacketRadius => Settings.PacketRadius;

        /// <summary>Share of a packet's glucose left (0-1).</summary>
        public double Fill(FoodPacket p) => p.Glucose / Settings.PacketAmount;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var p in _packets)
                    if (p.Active)
                        n++;
                return n;
            }
        }

        /// <summary>Centre of the window: the creature's position [m].</summary>
        public double CentreX { get; private set; }

        public double CentreY { get; private set; }

        /// <summary>Scatters lying food in the window around (cx, cy) at <see cref="FoodSettings.LyingDensity"/>.</summary>
        public void Populate(double cx, double cy)
        {
            CentreX = cx;
            CentreY = cy;
            int n = (int)Math.Round(Settings.LyingDensity * 4.0 * HalfSize * HalfSize);
            for (int i = 0; i < n; i++)
            {
                var slot = FreeSlot();
                Fill(slot, PacketKind.Lying, cx + (2.0 * _random.NextDouble() - 1.0) * HalfSize,
                    cy + (2.0 * _random.NextDouble() - 1.0) * HalfSize, 0.0, 0.0);
            }
        }

        /// <summary>
        /// Moves packets (carried ones with their carrier, those met by a cell with the creature), recentres the window on
        /// the creature at (cx, cy) moving at (vx, vy), retires packets that left it or are spent, and releases new ones at
        /// its boundary.
        /// </summary>
        public void Step(double dt, double cx = 0.0, double cy = 0.0, double vx = 0.0, double vy = 0.0, double angle = 0.0)
        {
            CentreX = cx;
            CentreY = cy;
            _angle = angle;
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            double margin = PacketRadius;
            foreach (var p in _packets)
            {
                if (!p.Active)
                    continue;
                if (p.Stuck)
                {
                    // food a cell found moves rigidly with the body: carried along and turned with it
                    p.X = cx + cos * p.LocalX - sin * p.LocalY;
                    p.Y = cy + sin * p.LocalX + cos * p.LocalY;
                }
                else
                {
                    p.X += p.Vx * dt;
                    p.Y += p.Vy * dt;
                }
                if (Math.Abs(p.X - cx) > HalfSize + margin || Math.Abs(p.Y - cy) > HalfSize + margin
                    || Fill(p) < Settings.UsedUpFraction)
                    p.Active = false;
                else
                    p.Radius = PacketRadius * Math.Pow(Fill(p), 1.0 / 3.0);   // its volume shrinks as it is eaten
            }
            // Food arrives at the level's rate, Poisson (exponential intervals), whatever is already in the field: carried by
            // another creature, or lying on the ground and coming into view on the sides the creature moves towards.
            _untilNext -= dt;
            while (_untilNext <= 0.0 && Settings.ArrivalsPerSecond > 0.0)
            {
                Spawn(_random.NextDouble() < Settings.CarriedShare, vx, vy);
                Arrived++;
                _untilNext += Exponential() / Settings.ArrivalsPerSecond;
            }
        }

        /// <summary>
        /// Glucose [mol/m³] a cell of radius <paramref name="r"/> at (x, y) sees at its membrane: the background, or the
        /// contact concentration while it touches a packet (several packets do not add: the membrane is saturated).
        /// </summary>
        public double GlucoseAt(double x, double y, double r)
        {
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    return Math.Max(BackgroundGlucose, Settings.ContactGlucose);
            return BackgroundGlucose;
        }

        /// <summary>Trace concentration [mol/m³] of species <paramref name="trace"/> at a cell's membrane.</summary>
        public double TraceAt(int trace, double x, double y, double r)
        {
            double c = 0.0;
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    c = Math.Max(c, Settings.ContactTrace * p.Traces[trace]);
            return c;
        }

        /// <summary>
        /// Takes <paramref name="mol"/> of glucose that a cell at (x, y) took up beyond the background, from the packets it
        /// touches in proportion to what each holds. Returns what they gave (no more than they held).
        /// </summary>
        public double Withdraw(double x, double y, double r, double mol)
        {
            if (mol <= 0.0)
                return 0.0;
            double total = 0.0;
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    total += p.Glucose;
            if (total <= 0.0)
                return 0.0;
            double share = Math.Min(1.0, mol / total);
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                {
                    p.Glucose -= p.Glucose * share;
                    p.Free -= p.Free * share;
                }
            return total * share;
        }

        /// <summary>
        /// Enzymes hydrolyse up to <paramref name="mol"/> of packet <paramref name="p"/>'s polymer; below the used-up share
        /// the packet gives all it has left. Returns the glucose released [mol], which leaves the packet. A packet with no
        /// enzyme acting on it keeps its polymer.
        /// </summary>
        public double Hydrolyse(FoodPacket p, double mol)
        {
            if (!p.Active || p.Glucose <= 0.0 || !(mol > 0.0))
                return 0.0;
            double polymer = p.Glucose - p.Free;
            double left = p.Glucose - Math.Min(mol, polymer);
            if (left < Settings.UsedUpFraction * Settings.PacketAmount)
            {
                left = 0.0;
                p.Free = 0.0;
            }
            double released = p.Glucose - left;
            p.Glucose = left;
            return released;
        }

        /// <summary>
        /// The packet's free small molecules leak out over <paramref name="dt"/> [s] at <see cref="FoodSettings.FreeLeakRate"/>
        /// (first order, exact over the step). Returns the glucose released [mol], which leaves the packet.
        /// </summary>
        public double Leak(FoodPacket p, double dt)
        {
            if (!p.Active || p.Free <= 0.0)
                return 0.0;
            double released = p.Free * (1.0 - Math.Exp(-Settings.FreeLeakRate * dt));
            p.Free -= released;
            p.Glucose -= released;
            return released;
        }

        /// <summary>Stops every packet that touches a cell at (x, y) of radius r: food stays with the cell that found it.</summary>
        public void StickTo(double x, double y, double r)
        {
            foreach (var p in _packets)
                if (p.Active && !p.Stuck && Touches(p, x, y, r))
                {
                    p.Stuck = true;
                    p.Vx = p.Vy = 0.0;
                    double dx = p.X - CentreX, dy = p.Y - CentreY, cos = Math.Cos(_angle), sin = Math.Sin(_angle);
                    p.LocalX = cos * dx + sin * dy;    // into the body frame
                    p.LocalY = -sin * dx + cos * dy;
                }
        }

        /// <summary>Takes packet <paramref name="p"/> out of the field (a cell engulfed it): its content leaves with it.</summary>
        public void Remove(FoodPacket p) => p.Active = false;

        /// <summary>Whether a cell at (x, y) of radius r touches any packet.</summary>
        public bool TouchesAny(double x, double y, double r)
        {
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    return true;
            return false;
        }

        public bool Touches(FoodPacket p, double x, double y, double r)
        {
            double dx = p.X - x, dy = p.Y - y, d = p.Radius + r;
            return dx * dx + dy * dy <= d * d;
        }

        /// <summary>Food packets that have arrived since the field was made (diagnostics).</summary>
        public long Arrived { get; private set; }

        /// <summary>The rate of arrivals changes (a new level or region): the next arrival is drawn at the new rate.</summary>
        public void SetArrivals(double perSecond, double carriedShare)
        {
            Settings.ArrivalsPerSecond = perSecond;
            Settings.CarriedShare = carriedShare;
            _untilNext = perSecond > 0.0 ? Exponential() / perSecond : double.PositiveInfinity;
        }

        // A free packet slot, or a new one: there is no limit on the food in the field.
        private FoodPacket FreeSlot()
        {
            foreach (var p in _packets)
                if (!p.Active)
                    return p;
            var added = new FoodPacket(TraceSpecies.Count);
            _packets.Add(added);
            return added;
        }

        // Inward normals of the window's sides: left, right, bottom, top.
        private static readonly double[] SideNx = { 1.0, -1.0, 0.0, 0.0 }, SideNy = { 0.0, 0.0, 1.0, -1.0 };

        /// <summary>
        /// A new packet at the window's boundary. Carried food enters from a random side heading inwards; lying food comes
        /// into view on the sides the creature moves towards (in proportion to the inflow), uniformly when it is still.
        /// </summary>
        private double Exponential() => -Math.Log(1.0 - _random.NextDouble());

        private void Spawn(bool carried, double vx, double vy)
        {
            var slot = FreeSlot();
            int side;
            if (carried)
                side = _random.Next(4);
            else
            {
                // lying food moves at -v relative to the creature: it enters through sides whose inward normal n has n.(-v) > 0
                double speed = Math.Sqrt(vx * vx + vy * vy);
                var w = new double[4];
                double total = 0.0;
                for (int k = 0; k < 4; k++)
                {
                    w[k] = speed > 0.0 ? Math.Max(0.0, -(SideNx[k] * vx + SideNy[k] * vy)) / speed : 1.0;
                    total += w[k];
                }
                double u = _random.NextDouble() * total;
                side = 3;
                for (int k = 0; k < 4; k++)
                {
                    if (u < w[k])
                    {
                        side = k;
                        break;
                    }
                    u -= w[k];
                }
            }
            double along = (2.0 * _random.NextDouble() - 1.0) * HalfSize;
            double edge = HalfSize + PacketRadius;
            double nx = SideNx[side], ny = SideNy[side];
            // a point on the side: -edge along the inward normal, 'along' across it
            double x = CentreX - nx * edge + (nx == 0.0 ? along : 0.0);
            double y = CentreY - ny * edge + (ny == 0.0 ? along : 0.0);
            double pvx = 0.0, pvy = 0.0;
            if (carried)
            {
                double angle = (_random.NextDouble() - 0.5) * Settings.EntryAngleSpread;
                double c = Math.Cos(angle), s = Math.Sin(angle);
                pvx = Settings.CarrierSpeed * (nx * c - ny * s);
                pvy = Settings.CarrierSpeed * (nx * s + ny * c);
            }
            Fill(slot, carried ? PacketKind.Carried : PacketKind.Lying, x, y, pvx, pvy);
        }

        private void Fill(FoodPacket slot, PacketKind kind, double x, double y, double vx, double vy)
        {
            slot.Kind = kind;
            slot.X = x;
            slot.Y = y;
            slot.Vx = vx;
            slot.Vy = vy;
            slot.Radius = PacketRadius;
            slot.Glucose = Settings.PacketAmount;
            slot.Free = Settings.FreeFraction * Settings.PacketAmount;
            slot.MainTrace = _random.Next(TraceSpecies.Count);
            for (int i = 0; i < slot.Traces.Length; i++)
                slot.Traces[i] = i == slot.MainTrace ? 1.0 : Settings.PacketTraceMinorShare;   // relative strengths
            slot.Stuck = false;
            slot.Active = true;
        }
    }
}
