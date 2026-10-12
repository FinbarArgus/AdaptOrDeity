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

    /// <summary>What a food packet is and how it moves until a cell meets it.</summary>
    public enum PacketKind
    {
        /// <summary>Food lying in the environment: still over the ground.</summary>
        Lying,

        /// <summary>Food carried by another creature (prey, or an ally bringing it): moves with its carrier.</summary>
        Carried,

        /// <summary>
        /// A dead free organism's body (<see cref="FoodField.AddBody"/>): drifts with the water it died in, and is simulated
        /// while it is within <see cref="FoodField.BodyRange"/> of the creature rather than the food window; beyond it, it is
        /// frozen where it is until the creature comes back (<see cref="FoodPacket.Frozen"/>).
        /// </summary>
        Body,
    }

    /// <summary>A patch of food: glucose plus traces of other species, in a disc of the 2D sheet.</summary>
    public sealed class FoodPacket
    {
        internal FoodPacket(int traceCount) => Traces = new double[traceCount];

        public PacketKind Kind { get; internal set; }

        /// <summary>Centre, over the ground [m].</summary>
        public double X { get; internal set; }

        public double Y { get; internal set; }

        /// <summary>Velocity over the ground [m/s] (0 for lying food and while held).</summary>
        public double Vx { get; internal set; }

        public double Vy { get; internal set; }

        /// <summary>A body's velocity with its water [m/s]: it drifts so again when its holder lets it go.</summary>
        internal double DriftVx { get; set; }

        internal double DriftVy { get; set; }

        /// <summary>Radius [m].</summary>
        public double Radius { get; internal set; }

        /// <summary>Glucose left [mol], free and in polymer.</summary>
        public double Glucose { get; internal set; }

        /// <summary>Glucose equivalents it held when it came into the field [mol], and its radius then [m]: its radius goes as
        /// the cube root of the share left, and it is used up below food.used_up_fraction of this amount.</summary>
        public double Amount { get; internal set; }

        public double FullRadius { get; internal set; }

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

        /// <summary>The last level the creature saw it in (0: not seen since it came into the field); kept by the game.</summary>
        public int SeenLevel { get; set; }

        /// <summary>
        /// A dead body out of range, kept as it was (not moved, leaked, digested or drawn in frames) until the creature comes
        /// back within <see cref="FoodField.BodyRange"/> or the body is held by a frozen organism that thaws. Not
        /// <see cref="Active"/> while frozen; its slot is not reused.
        /// </summary>
        public bool Frozen { get; internal set; }

        /// <summary>Where a stuck packet sits on its holder's body, from the body's centre in the body frame [m].</summary>
        public double LocalX { get; internal set; }

        public double LocalY { get; internal set; }

        /// <summary>True once the packet has met a cell: it stays with that cell's body until it is eaten.</summary>
        public bool Stuck { get; internal set; }

        /// <summary>Whose body a stuck packet moves with: 0 the creature (the window's centre), else a free organism's id.</summary>
        public int Holder { get; internal set; }

        /// <summary>
        /// Free sugar concentration the membranes touching it see this step [mol/m³] (<see cref="FoodField.Predict"/>):
        /// implicit in what they draw over the step.
        /// </summary>
        public double Input { get; internal set; }

        /// <summary>What touching membranes drew from it per unit of <see cref="Input"/> in the last step [m³/s].</summary>
        internal double Coefficient { get; set; }
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

        /// <summary>
        /// Free sugar in a packet (or a vacuole) at which enzymes stop hydrolysing its polymer [mol/m³] (product inhibition):
        /// the most a membrane facing it sees.
        /// </summary>
        public double ContactGlucose { get; set; }

        /// <summary>
        /// A packet's main trace species per its free sugar, as concentrations [mol/m³ per <see cref="ContactGlucose"/>]:
        /// what it releases with its sugar.
        /// </summary>
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
                _packets.Add(NewSlot());
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
        public double Fill(FoodPacket p) => p.Glucose / p.Amount;

        /// <summary>
        /// Distance from the creature along either ground axis within which a dead body (<see cref="PacketKind.Body"/>) is
        /// simulated [m]; beyond it, it is frozen (<see cref="FoodPacket.Frozen"/>). Other packets stay within the window
        /// (<see cref="HalfSize"/>) and leave it for good. Infinite unless set.
        /// </summary>
        public double BodyRange { get; set; } = double.PositiveInfinity;

        /// <summary>Most frozen bodies kept; beyond it the one farthest from the creature is forgotten (<see cref="BodiesGone"/>).</summary>
        public int MaxFrozenBodies { get; set; } = int.MaxValue;

        /// <summary>Frozen bodies forgotten at <see cref="MaxFrozenBodies"/> (diagnostics).</summary>
        public int BodiesGone { get; private set; }

        /// <summary>Dead bodies frozen out of range now.</summary>
        public int FrozenBodies
        {
            get
            {
                int n = 0;
                foreach (var p in _packets)
                    if (p.Frozen)
                        n++;
                return n;
            }
        }

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
            // bodies frozen out of range come back when the creature does (those held by a frozen organism thaw with it)
            foreach (var p in _packets)
                if (p.Frozen && !(p.Stuck && p.Holder != 0) && !Beyond(p, BodyRange))
                {
                    p.Frozen = false;
                    p.Active = true;
                }
            foreach (var p in _packets)
            {
                if (!p.Active)
                    continue;
                if (p.Stuck)
                {
                    // food a cell found moves rigidly with the body: carried along and turned with it (a free
                    // organism's packets move with its body in MoveHeld)
                    if (p.Holder == 0)
                    {
                        p.X = cx + cos * p.LocalX - sin * p.LocalY;
                        p.Y = cy + sin * p.LocalX + cos * p.LocalY;
                    }
                }
                else
                {
                    p.X += p.Vx * dt;
                    p.Y += p.Vy * dt;
                }
                if (Fill(p) < Settings.UsedUpFraction)
                    p.Active = false;
                else if (p.Kind == PacketKind.Body)
                {
                    // a body out of range is frozen where it is (one an organism holds moves and freezes with it)
                    if (!(p.Stuck && p.Holder != 0) && Beyond(p, BodyRange))
                        Freeze(p);
                }
                else if (Beyond(p, HalfSize + margin))
                    p.Active = false;
                if (p.Active)
                    p.Radius = p.FullRadius * Math.Pow(Fill(p), 1.0 / 3.0);   // its volume shrinks as it is eaten
            }
            ForgetFrozenBeyondCap();
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

        // Whether packet p is farther than reach from the window's centre (the creature) along either ground axis.
        private bool Beyond(FoodPacket p, double reach) => Math.Abs(p.X - CentreX) > reach || Math.Abs(p.Y - CentreY) > reach;

        private static void Freeze(FoodPacket p)
        {
            p.Active = false;
            p.Frozen = true;
        }

        /// <summary>
        /// The packets <paramref name="holder"/> (a free organism's id) holds freeze with it: they stay as they are, out of
        /// the field, until <see cref="Thaw"/>. Only dead bodies are kept: other food out of the window has already left it.
        /// </summary>
        public void Freeze(int holder)
        {
            foreach (var p in _packets)
                if (p.Active && p.Stuck && p.Holder == holder)
                {
                    if (p.Kind == PacketKind.Body)
                        Freeze(p);
                    else
                        p.Active = false;
                }
            ForgetFrozenBeyondCap();
        }

        /// <summary>The frozen packets <paramref name="holder"/> holds come back with it, as they were.</summary>
        public void Thaw(int holder)
        {
            foreach (var p in _packets)
                if (p.Frozen && p.Stuck && p.Holder == holder)
                {
                    p.Frozen = false;
                    p.Active = true;
                }
        }

        // Beyond the cap, the frozen bodies farthest from the creature are forgotten (the first of equals).
        private void ForgetFrozenBeyondCap()
        {
            int frozen = FrozenBodies;
            while (frozen > MaxFrozenBodies)
            {
                FoodPacket? far = null;
                double d2 = -1.0;
                foreach (var p in _packets)
                {
                    if (!p.Frozen)
                        continue;
                    double e2 = (p.X - CentreX) * (p.X - CentreX) + (p.Y - CentreY) * (p.Y - CentreY);
                    if (e2 > d2)
                    {
                        d2 = e2;
                        far = p;
                    }
                }
                far!.Frozen = false;
                far.Stuck = false;
                far.Holder = 0;
                BodiesGone++;
                frozen--;
            }
        }

        /// <summary>Volume of packet <paramref name="p"/>: a sphere of its radius [m³].</summary>
        public static double Volume(FoodPacket p) => 4.0 / 3.0 * Math.PI * p.Radius * p.Radius * p.Radius;

        /// <summary>
        /// Share of the membrane of a cell of radius <paramref name="r"/> that faces packet <paramref name="p"/> when they
        /// touch: the cap the packet's sphere covers seen from the cell's centre, (1 - cos t)/2 with sin t = a/(r + a).
        /// </summary>
        public static double FaceShare(FoodPacket p, double r)
        {
            double s = p.Radius / (r + p.Radius);
            return 0.5 * (1.0 - Math.Sqrt(1.0 - s * s));
        }

        /// <summary>
        /// The packets a cell of radius <paramref name="r"/> at (x, y) touches, as its membrane sees them: the share of its
        /// membrane facing them (<paramref name="share"/>, at most 1) and the sum of each one's share times the free sugar it
        /// offers (<see cref="FoodPacket.Input"/>) [mol/m³]. The rest of the membrane sees the water.
        /// </summary>
        public void Faces(double x, double y, double r, out double share, out double offered)
        {
            share = 0.0;
            offered = 0.0;
            for (int k = 0; k < _packets.Count; k++)   // indexed: no enumerator to allocate
            {
                var p = _packets[k];
                if (!p.Active || !Touches(p, x, y, r))
                    continue;
                double w = FaceShare(p, r);
                share += w;
                offered += w * p.Input;
            }
            if (share > 1.0)
            {
                offered /= share;
                share = 1.0;
            }
        }

        /// <summary>
        /// A cell of radius <paramref name="r"/> at (x, y) took up <paramref name="mol"/> through the membrane facing the
        /// packets it touches: drawn from their free sugar in proportion to what each offered (no more than each holds).
        /// Returns what they gave [mol].
        /// </summary>
        public double Draw(double x, double y, double r, double mol, double dt)
        {
            if (!(mol > 0.0))
                return 0.0;
            Faces(x, y, r, out _, out double offered);
            if (!(offered > 0.0))
                return 0.0;
            double given = 0.0;
            for (int k = 0; k < _packets.Count; k++)
            {
                var p = _packets[k];
                if (!p.Active || !Touches(p, x, y, r) || !(p.Input > 0.0))
                    continue;
                double want = mol * FaceShare(p, r) * p.Input / offered;
                double take = Math.Min(p.Free, want);
                p.Free -= take;
                p.Glucose -= take;
                p.Coefficient += want / (dt * p.Input);
                given += take;
            }
            return given;
        }

        /// <summary>
        /// The free sugar packet <paramref name="p"/> offers touching membranes over the next <paramref name="dt"/> [s],
        /// implicit in what they drew per unit concentration in the last step: Free / (V + coefficient dt). Call once per
        /// step, after hydrolysis and before the cells read it.
        /// </summary>
        public void Predict(FoodPacket p, double dt)
        {
            double v = Volume(p);
            p.Input = p.Active && v > 0.0 ? Math.Max(0.0, p.Free) / (v + p.Coefficient * dt) : 0.0;
            p.Coefficient = 0.0;
        }

        /// <summary>
        /// Enzymes hydrolyse up to <paramref name="mol"/> of packet <paramref name="p"/>'s polymer into its free sugar, held
        /// back while that is at <see cref="FoodSettings.ContactGlucose"/> over its volume (product inhibition, as in a
        /// vacuole). Returns the glucose hydrolysed [mol]; it stays in the packet until a membrane takes it or it leaks.
        /// </summary>
        public double Hydrolyse(FoodPacket p, double mol)
        {
            if (!p.Active || p.Glucose <= 0.0 || !(mol > 0.0))
                return 0.0;
            double polymer = Math.Max(0.0, p.Glucose - p.Free);
            double room = Math.Max(0.0, Settings.ContactGlucose * Volume(p) - p.Free);
            double hydrolysed = Math.Min(Math.Min(mol, polymer), room);
            p.Free += hydrolysed;
            return hydrolysed;
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
        public void StickTo(double x, double y, double r) => StickTo(x, y, r, 0, CentreX, CentreY, _angle);

        /// <summary>
        /// Stops every free packet that touches a cell at (x, y) of radius r and gives it to <paramref name="holder"/> (0 the
        /// creature, else a free organism's id), whose body is at (<paramref name="bx"/>, <paramref name="by"/>) turned by
        /// <paramref name="angle"/>. A packet already held stays with its holder.
        /// </summary>
        public void StickTo(double x, double y, double r, int holder, double bx, double by, double angle)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            foreach (var p in _packets)
                if (p.Active && !p.Stuck && Touches(p, x, y, r))
                {
                    p.Stuck = true;
                    p.Holder = holder;
                    p.Vx = p.Vy = 0.0;
                    double dx = p.X - bx, dy = p.Y - by;
                    p.LocalX = cos * dx + sin * dy;    // into the body frame
                    p.LocalY = -sin * dx + cos * dy;
                }
        }

        /// <summary>Moves the packets <paramref name="holder"/> holds with its body, now at (bx, by) turned by angle.</summary>
        public void MoveHeld(int holder, double bx, double by, double angle)
        {
            double cos = Math.Cos(angle), sin = Math.Sin(angle);
            foreach (var p in _packets)
                if (p.Active && p.Stuck && p.Holder == holder)
                {
                    p.X = bx + cos * p.LocalX - sin * p.LocalY;
                    p.Y = by + sin * p.LocalX + cos * p.LocalY;
                }
        }

        /// <summary>
        /// The packets <paramref name="holder"/> held are let go where they are (its body died or was forgotten): they lie
        /// still, and a dead body drifts with its water again (a frozen one stays frozen until the creature comes back).
        /// </summary>
        public void Release(int holder)
        {
            foreach (var p in _packets)
                if ((p.Active || p.Frozen) && p.Stuck && p.Holder == holder)
                {
                    p.Stuck = false;
                    p.Holder = 0;
                    if (p.Kind == PacketKind.Body)
                    {
                        p.Vx = p.DriftVx;
                        p.Vy = p.DriftVy;
                    }
                }
        }

        /// <summary>
        /// A dead cell's body becomes food where it lies: a packet of kind <see cref="PacketKind.Body"/> at (x, y), of radius
        /// <paramref name="radius"/>, holding <paramref name="glucose"/> [mol glucose equivalents] of which
        /// <paramref name="free"/> are small molecules, drifting at (vx, vy) with the water, smelling most of trace
        /// <paramref name="mainTrace"/>. It stays until it is eaten; out of <see cref="BodyRange"/> it is frozen. Draws no random
        /// numbers, so the food that arrives is the same with or without deaths.
        /// </summary>
        public FoodPacket AddBody(double x, double y, double vx, double vy, double radius, double glucose, double free,
            int mainTrace)
        {
            var slot = FreeSlot();
            slot.Kind = PacketKind.Body;
            slot.X = x;
            slot.Y = y;
            slot.Vx = slot.DriftVx = vx;
            slot.Vy = slot.DriftVy = vy;
            slot.Radius = slot.FullRadius = radius;
            slot.Glucose = slot.Amount = glucose;
            slot.Free = free;
            slot.MainTrace = mainTrace;
            for (int i = 0; i < slot.Traces.Length; i++)
                slot.Traces[i] = i == mainTrace ? 1.0 : Settings.PacketTraceMinorShare;
            slot.Stuck = false;
            slot.Holder = 0;
            slot.SeenLevel = 0;
            slot.Active = true;
            return slot;
        }

        /// <summary>Takes packet <paramref name="p"/> out of the field (a cell engulfed it): its content leaves with it.</summary>
        public void Remove(FoodPacket p) => p.Active = false;

        /// <summary>Whether <paramref name="holder"/> (0 the creature, else a free organism's id) holds a packet in the field.</summary>
        public bool Holds(int holder)
        {
            for (int k = 0; k < _packets.Count; k++)
            {
                var p = _packets[k];
                if (p.Active && p.Stuck && p.Holder == holder)
                    return true;
            }
            return false;
        }

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
                if (!p.Active && !p.Frozen)
                    return p;
            var added = NewSlot();
            _packets.Add(added);
            return added;
        }

        // An empty slot, sized as a fresh packet.
        private FoodPacket NewSlot() => new FoodPacket(TraceSpecies.Count)
        {
            Amount = Settings.PacketAmount,
            FullRadius = Settings.PacketRadius,
        };

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
            slot.Radius = slot.FullRadius = PacketRadius;
            slot.Glucose = slot.Amount = Settings.PacketAmount;
            slot.Free = Settings.FreeFraction * Settings.PacketAmount;
            slot.MainTrace = _random.Next(TraceSpecies.Count);
            for (int i = 0; i < slot.Traces.Length; i++)
                slot.Traces[i] = i == slot.MainTrace ? 1.0 : Settings.PacketTraceMinorShare;   // relative strengths
            slot.Stuck = false;
            slot.Holder = 0;
            slot.SeenLevel = 0;
            slot.Active = true;
        }
    }
}
