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
            int traitCount = 0)
        {
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
