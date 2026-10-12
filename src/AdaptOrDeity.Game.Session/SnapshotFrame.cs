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
            int traitCount = 0, int controlCount = 0, int groupCapacity = 0, int featureCount = 0, int packetCapacity = 0)
        {
            CellX = new float[cellCapacity];
            CellY = new float[cellCapacity];
            PacketX = new float[packetCapacity];
            PacketY = new float[packetCapacity];
            PacketRadius = new float[packetCapacity];
            PacketFill = new float[packetCapacity];
            PacketTrace = new int[packetCapacity];
            PacketCarried = new bool[packetCapacity];
            FlagellumAngles = Allocate<float>(cellCapacity, AdaptOrDeity.Sim.World.CreatureBody.MaxFlagella);
            TraitArea = Allocate(traitCount, cellCapacity);
            CellSurface = new float[cellCapacity];
            CellGroup = new int[cellCapacity];
            CellAlive = new bool[cellCapacity];
            CellCanDivide = new bool[cellCapacity];
            Controls = Allocate<float>(controlCount, groupCapacity);
            FeatureUnlocked = new bool[featureCount];
            FeatureEnabled = new bool[featureCount];
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

        /// <summary>Per trait (schema order), per cell: current multiplier.</summary>
        public float[][] TraitValues { get; }

        /// <summary>Per trait, per cell: target multiplier.</summary>
        public float[][] TraitTargets { get; }

        /// <summary>Per cell: its group.</summary>
        public int[] CellGroup { get; }

        public bool[] CellAlive { get; }

        /// <summary>Per cell: a daughter is built (or glycogen can finish it).</summary>
        public bool[] CellCanDivide { get; }

        /// <summary>Per energy control (schema order), per group: its value.</summary>
        public float[][] Controls { get; }

        public int GroupCount { get; internal set; }

        /// <summary>Level of the evolutionary time bar being played.</summary>
        public int Level { get; internal set; }

        /// <summary>Glucose eaten from food since the start [mol], and the skill points it has earned not yet spent.</summary>
        public double FoodEaten { get; internal set; }

        public int SkillPoints { get; internal set; }

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
