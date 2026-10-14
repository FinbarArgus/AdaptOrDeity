using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// What the front end sees of the simulation at one instant. Frames are pooled (see
    /// <see cref="TripleBuffer{T}"/>): the simulation overwrites them in place and never allocates per frame.
    /// </summary>
    public sealed class SnapshotFrame
    {
        public SnapshotFrame(int channelCount, int cellCapacity, int probeCapacity, int probeSamples, int detailCount = 0,
            int traitCount = 0, int controlCount = 0, int groupCapacity = 0, int featureCount = 0, int packetCapacity = 0,
            int preyCapacity = 0, int organismCapacity = 0)
        {
            OrganismId = new int[organismCapacity];
            OrganismGeneration = new int[organismCapacity];
            OrganismX = new float[organismCapacity];
            OrganismY = new float[organismCapacity];
            OrganismOrientation = new float[organismCapacity];
            OrganismMotorOn = new bool[organismCapacity];
            OrganismFlagella = new float[organismCapacity];
            OrganismFlagellumAngles = Allocate<float>(organismCapacity, AdaptOrDeity.Sim.World.CreatureBody.MaxFlagella);
            OrganismVacuoleRadius = new float[organismCapacity];
            OrganismChannels = Allocate<float>(channelCount, organismCapacity);
            OrganismDetails = new double[detailCount];
            PreyX = new float[preyCapacity];
            PreyY = new float[preyCapacity];
            PreyRadius = new float[preyCapacity];
            PreyHeading = new float[preyCapacity];
            VacuoleRadius = new float[cellCapacity];
            VacuoleX = new float[cellCapacity];
            VacuoleY = new float[cellCapacity];
            VacuolePrey = new int[cellCapacity];
            CellX = new float[cellCapacity];
            CellY = new float[cellCapacity];
            PacketX = new float[packetCapacity];
            RockX = new float[MaxRocks];
            RockY = new float[MaxRocks];
            RockRadius = new float[MaxRocks];
            PacketY = new float[packetCapacity];
            PacketRadius = new float[packetCapacity];
            PacketFill = new float[packetCapacity];
            PacketTrace = new int[packetCapacity];
            PacketCarried = new bool[packetCapacity];
            PacketBody = new bool[packetCapacity];
            FlagellumAngles = Allocate<float>(cellCapacity, AdaptOrDeity.Sim.World.CreatureBody.MaxFlagella);
            TraitArea = Allocate(traitCount, cellCapacity);
            CellSurface = new float[cellCapacity];
            CellGroup = new int[cellCapacity];
            CellAlive = new bool[cellCapacity];
            CellCanDivide = new bool[cellCapacity];
            Controls = Allocate<float>(controlCount, groupCapacity);
            Regulation = Allocate<bool>(traitCount > 0
                    ? (traitCount + 1 + SessionSchema.MaintenanceRegulation.Count) * SessionSchema.RegulationSpecies.Count : 0,
                groupCapacity);
            FeatureUnlocked = new bool[featureCount];
            FeatureEnabled = new bool[featureCount];
            ScoredUnlocked = new bool[featureCount];
            Achieved = new int[GameAchievements.All.Count];
            Seen = new int[FermenterWorld.SeenKinds];
            ScoredSeen = new int[FermenterWorld.SeenKinds];
            Details = Allocate<double>(detailCount, cellCapacity);
            TraitValues = Allocate(traitCount, cellCapacity);
            TraitTargets = Allocate(traitCount, cellCapacity);
            Channels = new float[channelCount][];
            for (int c = 0; c < channelCount; c++)
                Channels[c] = new float[cellCapacity];
            Probes = new ProbeSeries[probeCapacity];
            for (int p = 0; p < probeCapacity; p++)
                Probes[p] = new ProbeSeries(probeSamples);
        }

        /// <summary>Monotonic publish counter; lets the consumer detect skipped frames.</summary>
        public long Sequence { get; internal set; }

        /// <summary>Simulated time [s].</summary>
        public double SimTime { get; internal set; }

        public long CheckpointIndex { get; internal set; }

        public int SpeedIndex { get; internal set; }

        /// <summary>True when the simulation could not keep up with the requested speed in the last tick.</summary>
        public bool Lagging { get; internal set; }

        public int CellCount { get; internal set; }

        /// <summary>Per overlay channel (schema order), per cell: value in canonical SI.</summary>
        public float[][] Channels { get; }

        /// <summary>Probe time series; only the first <see cref="ProbeCount"/> are valid.</summary>
        public ProbeSeries[] Probes { get; }

        public int ProbeCount { get; internal set; }

        /// <summary>Per cell-panel detail (schema order), per cell: value in SI.</summary>
        public double[][] Details { get; }

        /// <summary>Per trait (schema order), per cell: current value (multiplier, count or membrane share).</summary>
        public float[][] TraitValues { get; }

        /// <summary>Per trait, per cell: target (multiplier, count or membrane share).</summary>
        public float[][] TraitTargets { get; }

        /// <summary>Per cell: its group.</summary>
        public int[] CellGroup { get; }

        public bool[] CellAlive { get; }

        /// <summary>Per cell: a daughter is built (or glycogen can finish it).</summary>
        public bool[] CellCanDivide { get; }

        /// <summary>Per energy control (schema order), per group: its value.</summary>
        public float[][] Controls { get; }

        public int GroupCount { get; internal set; }

        /// <summary>
        /// Per regulated target and species (row <see cref="RegulationRow"/>), per group: whether the species' fall lowers
        /// the target (feature Regulation). Targets are the traits, then growth (<see cref="SessionSchema.GrowthRegulation"/>),
        /// then the maintenance (<see cref="SessionSchema.MaintenanceRegulation"/>).
        /// </summary>
        public bool[][] Regulation { get; }

        /// <summary>The row of <see cref="Regulation"/> for a target and a species.</summary>
        public static int RegulationRow(int target, int species) => target * SessionSchema.RegulationSpecies.Count + species;

        /// <summary>Level of the evolutionary time bar being played.</summary>
        public int Level { get; internal set; }

        /// <summary>Glucose eaten from food since the start [mol] (a statistic), and the skill points not yet spent.</summary>
        public double FoodEaten { get; internal set; }

        public int SkillPoints { get; internal set; }

        /// <summary>
        /// Points earned so far (spent or not): by the survival and damage meters of finished levels, and by daughters
        /// released in the lineage (reproduction).
        /// </summary>
        public int SurvivalPoints { get; internal set; }

        public int DamagePoints { get; internal set; }

        public int ReproductionPoints { get; internal set; }

        /// <summary>This level's survival and damage meters (0-100), and the points each would convert to now.</summary>
        public float SurvivalMeter { get; internal set; }

        public float DamageMeter { get; internal set; }

        public int SurvivalPointsNow { get; internal set; }

        public int DamagePointsNow { get; internal set; }

        /// <summary>Share of the creature's living cells starving now (0-1), and its time since it last had food [s].</summary>
        public float StarvingShare { get; internal set; }

        public float TimeWithoutFood { get; internal set; }

        /// <summary>
        /// The last finished level (0 before one ends): its meters at the end and the points they converted to.
        /// </summary>
        public int ScoredLevel { get; internal set; }

        public float ScoredSurvival { get; internal set; }

        public float ScoredDamage { get; internal set; }

        public int ScoredSurvivalPoints { get; internal set; }

        public int ScoredDamagePoints { get; internal set; }

        /// <summary>The last finished level's reproduction points and stars (0-3).</summary>
        public int ScoredReproductionPoints { get; internal set; }

        public int ScoredStars { get; internal set; }

        /// <summary>Per feature: unlocked during the last finished level.</summary>
        public bool[] ScoredUnlocked { get; }

        /// <summary>Per <see cref="SeenKind"/>: individuals seen in the last finished level, and in this level so far.</summary>
        public int[] ScoredSeen { get; }

        public int[] Seen { get; }

        /// <summary>This level's points if it ended now (meters and reproduction) and the stars they would give.</summary>
        public int LevelPointsNow { get; internal set; }

        public int StarsNow { get; internal set; }

        /// <summary>Per achievement (<see cref="GameAchievements.All"/>): the level it was earned in, 0 if not yet.</summary>
        public int[] Achieved { get; }

        /// <summary>Simulated time left in the current level [s].</summary>
        public double LevelTimeLeft { get; internal set; }

        /// <summary>Per feature (schema order).</summary>
        public bool[] FeatureUnlocked { get; }

        public bool[] FeatureEnabled { get; }

        /// <summary>The creature's position over the ground [m] (the camera follows it).</summary>
        public double CreatureX { get; internal set; }

        public double CreatureY { get; internal set; }

        /// <summary>True while the flagella run.</summary>
        public bool MotorOn { get; internal set; }

        /// <summary>Orientation of the body [rad] and its spin [rad/s]: cells and flagella turn with it.</summary>
        public float Orientation { get; internal set; }

        public float Spin { get; internal set; }

        /// <summary>Proton-motive power of all the flagellar motors [W].</summary>
        public float MotorPower { get; internal set; }

        /// <summary>Mechanical power of the thrust on the body [W] (all of it ends as heat in the water).</summary>
        public float MechanicalPower { get; internal set; }

        /// <summary>Heat the body's motion has dissipated into the water since the start [J].</summary>
        public double HeatToWater { get; internal set; }

        /// <summary>Per cell, per flagellum: its base angle on the cell's membrane, in the body frame [rad].</summary>
        public float[][] FlagellumAngles { get; }

        /// <summary>Per trait, per cell: membrane area the protein takes [m²] (0 for cytosolic proteins).</summary>
        public float[][] TraitArea { get; }

        /// <summary>Per cell: its whole surface [m²].</summary>
        public float[] CellSurface { get; }

        /// <summary>The creature's velocity over the ground [m/s].</summary>
        public float CreatureVx { get; internal set; }

        public float CreatureVy { get; internal set; }

        /// <summary>
        /// Per cell: position on the body relative to its centre of mass, in the body's frame [m]. Turn by
        /// <see cref="Orientation"/> and add <see cref="CreatureX"/>, <see cref="CreatureY"/> for the ground.
        /// </summary>
        public float[] CellX { get; }

        public float[] CellY { get; }

        /// <summary>Most rocks a frame carries (those around the creature).</summary>
        public const int MaxRocks = 64;

        /// <summary>Height of the seabed's ground relative to the creature [m] (the world's y grows upwards).</summary>
        public float GroundY { get; internal set; }

        /// <summary>The rocks around the creature; only the first <see cref="RockCount"/> are valid. Positions relative to the
        /// creature and radii [m].</summary>
        public int RockCount { get; internal set; }

        public float[] RockX { get; }

        public float[] RockY { get; }

        public float[] RockRadius { get; }

        /// <summary>Food packets around the creature; only the first <see cref="PacketCount"/> are valid. Positions relative
        /// to the creature and radii [m].</summary>
        public int PacketCount { get; internal set; }

        public float[] PacketX { get; }

        public float[] PacketY { get; }

        public float[] PacketRadius { get; }

        /// <summary>Share of each packet's glucose left (0-1).</summary>
        public float[] PacketFill { get; }

        /// <summary>The trace species each packet is richest in (index into the schema's trace species).</summary>
        public int[] PacketTrace { get; }

        /// <summary>True if the packet is carried by another creature; false if it lies on the ground.</summary>
        public bool[] PacketCarried { get; }

        /// <summary>True if the packet is a dead free organism's body (drawn as a dead cell); its radius shrinks as it is
        /// eaten.</summary>
        public bool[] PacketBody { get; }

        /// <summary>Prey cells around the creature; only the first <see cref="PreyCount"/> are valid. Positions relative to
        /// the creature and radii [m], ground axes.</summary>
        public int PreyCount { get; internal set; }

        public float[] PreyX { get; }

        public float[] PreyY { get; }

        public float[] PreyRadius { get; }

        /// <summary>Direction each prey cell swims in [rad], ground axes.</summary>
        public float[] PreyHeading { get; }

        /// <summary>Prey cells the creature has engulfed since the start.</summary>
        public int Swallowed { get; internal set; }

        /// <summary>Daughters released in the creature's lineage (each earns reproduction points), and how many of them
        /// are its own (generation 1); the rest are its free organisms' daughters.</summary>
        public int Offspring { get; internal set; }

        public int OwnOffspring { get; internal set; }

        /// <summary>
        /// Free-living organisms (daughters released without multicellularity, and their offspring), in id order; only the
        /// first <see cref="OrganismCount"/> are valid. Not the player's: frames carry them to draw and inspect.
        /// </summary>
        public int OrganismCount { get; internal set; }

        /// <summary>Per organism: its id (stable while it is simulated) and generation (1: the creature's daughter).</summary>
        public int[] OrganismId { get; }

        public int[] OrganismGeneration { get; }

        /// <summary>Per organism: its cell's position relative to the creature, ground axes [m], and its orientation [rad].</summary>
        public float[] OrganismX { get; }

        public float[] OrganismY { get; }

        public float[] OrganismOrientation { get; }

        /// <summary>Per organism: true while its flagellar motors run.</summary>
        public bool[] OrganismMotorOn { get; }

        /// <summary>Per organism: flagella built (a fraction is a part-built one) and each one's base angle, body frame [rad].</summary>
        public float[] OrganismFlagella { get; }

        public float[][] OrganismFlagellumAngles { get; }

        /// <summary>Per organism: radius of its vacuole [m], 0 for none.</summary>
        public float[] OrganismVacuoleRadius { get; }

        /// <summary>Per overlay channel (schema order), per organism: value in canonical SI, as <see cref="Channels"/>.</summary>
        public float[][] OrganismChannels { get; }

        /// <summary>The organism whose details <see cref="OrganismDetails"/> holds (-1: none, or it is no longer simulated).</summary>
        public int InspectedOrganism { get; internal set; } = -1;

        /// <summary>Per cell-panel detail (schema order): the inspected organism's value in SI.</summary>
        public double[] OrganismDetails { get; }

        /// <summary>Free organisms released so far, that died (their bodies became food), and that were forgotten (frozen
        /// beyond the frozen cap).</summary>
        public int OrganismsReleased { get; internal set; }

        public int OrganismsDied { get; internal set; }

        public int OrganismsGone { get; internal set; }

        /// <summary>Free organisms and dead bodies frozen out of range now (kept until the creature comes back).</summary>
        public int OrganismsFrozen { get; internal set; }

        public int BodiesFrozen { get; internal set; }

        /// <summary>Per cell: radius of what it has engulfed (its vacuole) [m], 0 for none.</summary>
        public float[] VacuoleRadius { get; }

        /// <summary>Per cell: where its vacuole sits, from the cell's centre in the body frame [m].</summary>
        public float[] VacuoleX { get; }

        public float[] VacuoleY { get; }

        /// <summary>Per cell: prey cells in its vacuole (0: only food packets, or none).</summary>
        public int[] VacuolePrey { get; }

        /// <summary>Cells along each side of <see cref="FieldGlucose"/>.</summary>
        public const int FieldRasterSize = 64;

        /// <summary>
        /// Side of the square of water <see cref="FieldGlucose"/> covers, centred on the creature [m]; 0 when the world has
        /// no environment field.
        /// </summary>
        public float FieldExtent { get; internal set; }

        /// <summary>
        /// Glucose in the water around the creature [mol/m³]: <see cref="FieldRasterSize"/>² cell centres row by row from
        /// the square's lower-left corner, along the ground's axes (not turned with the body).
        /// </summary>
        public float[] FieldGlucose { get; } = new float[FieldRasterSize * FieldRasterSize];

        /// <summary>Lactate in the water around the creature [mol/m³], on the raster of <see cref="FieldGlucose"/>.</summary>
        public float[] FieldLactate { get; } = new float[FieldRasterSize * FieldRasterSize];

        /// <summary>The water's pH around the creature, on the raster of <see cref="FieldGlucose"/>.</summary>
        public float[] FieldPh { get; } = new float[FieldRasterSize * FieldRasterSize];

        private static T[][] Allocate<T>(int rows, int cells)
        {
            var a = new T[rows][];
            for (int i = 0; i < rows; i++)
                a[i] = new T[cells];
            return a;
        }

        private static float[][] Allocate(int rows, int cells) => Allocate<float>(rows, cells);
    }

    /// <summary>A copied-out probe time series in chronological order.</summary>
    public sealed class ProbeSeries
    {
        public ProbeSeries(int capacity)
        {
            Times = new double[capacity];
            Values = new float[capacity];
        }

        public int Cell { get; internal set; }

        public int Channel { get; internal set; }

        /// <summary>Number of valid samples in <see cref="Times"/> and <see cref="Values"/>.</summary>
        public int Count { get; internal set; }

        public double[] Times { get; }

        public float[] Values { get; }

        internal void CopyFrom(Probe probe)
        {
            Cell = probe.Cell;
            Channel = probe.Channel;
            Count = probe.CopyTo(Times, Values);
        }

        internal void Clear() => Count = 0;
    }

    /// <summary>Sim-side ring buffer of (time, value) samples for one cell and channel.</summary>
    internal sealed class Probe
    {
        private readonly double[] _times;
        private readonly float[] _values;
        private int _next;
        private int _count;

        public Probe(int cell, int channel, int capacity)
        {
            Cell = cell;
            Channel = channel;
            _times = new double[capacity];
            _values = new float[capacity];
        }

        public int Cell { get; }

        public int Channel { get; }

        public void Add(double time, float value)
        {
            _times[_next] = time;
            _values[_next] = value;
            _next = (_next + 1) % _times.Length;
            _count = Math.Min(_count + 1, _times.Length);
        }

        /// <summary>Copies samples oldest-first; returns the number copied.</summary>
        public int CopyTo(double[] times, float[] values)
        {
            int start = (_next - _count + _times.Length) % _times.Length;
            for (int i = 0; i < _count; i++)
            {
                int j = (start + i) % _times.Length;
                times[i] = _times[j];
                values[i] = _values[j];
            }
            return _count;
        }
    }
}
