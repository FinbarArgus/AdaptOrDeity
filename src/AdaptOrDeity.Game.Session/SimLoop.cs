using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Core;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>Settings for a <see cref="SimLoop"/>.</summary>
    public sealed class SimLoopOptions
    {
        /// <summary>Sim-seconds per real second per speed preset; index 0 must be 0 (pause).</summary>
        public double[] SpeedPresets { get; set; } = { 0.0, 1.0, 2.0, 3.0 };

        /// <summary>Simulated time between checkpoints [s].</summary>
        public double CheckpointInterval { get; set; } = 600.0;

        /// <summary>Most probes that may be active at once.</summary>
        public int MaxProbes { get; set; } = 8;

        /// <summary>Samples kept per probe.</summary>
        public int ProbeSamples { get; set; } = 600;

        /// <summary>Simulated time between probe samples [s]; rounded to a whole number of macro steps.</summary>
        public double ProbeInterval { get; set; } = 0.01;
    }

    /// <summary>
    /// The deterministic core of a session: applies commands, advances the world in fixed macro steps and
    /// writes snapshots. Real-time pacing and threading live in <see cref="SimHost"/>, so tests can drive
    /// this directly. Results depend only on the commands and the number of steps taken.
    /// </summary>
    public sealed class SimLoop
    {
        private readonly SimLoopOptions _options;
        private readonly ConcurrentQueue<ISimCommand> _commands = new ConcurrentQueue<ISimCommand>();
        private readonly ConcurrentQueue<SimEvent> _events = new ConcurrentQueue<SimEvent>();
        private readonly List<Probe> _probes = new List<Probe>();
        private double _pending;
        private readonly long _stepsPerProbeSample;
        private int _speedIndex = 1;
        private long _sequence;

        public SimLoop(ISimWorld world, SimLoopOptions? options = null)
        {
            _options = options ?? new SimLoopOptions();
            if (_options.SpeedPresets.Length < 2 || _options.SpeedPresets[0] != 0.0)
                throw new ArgumentException("speed presets need pause (0) at index 0 plus at least one speed");
            if (!(world.MacroDt > 0.0))
                throw new ArgumentException("world macro step must be > 0 s", nameof(world));
            World = world;
            Clock = new SimClock(_options.CheckpointInterval);
            Schema = new SessionSchema(world.Overlays, _options.SpeedPresets);
            // Sample on step counts, not accumulated float times, so sampling is exact and deterministic.
            _stepsPerProbeSample = Math.Max(1L, (long)Math.Round(_options.ProbeInterval / world.MacroDt));
        }

        public ISimWorld World { get; }

        public SimClock Clock { get; }

        public SessionSchema Schema { get; }

        public SimLoopOptions Options => _options;

        public int SpeedIndex
        {
            get => _speedIndex;
            set
            {
                if (value < 0 || value >= _options.SpeedPresets.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _speedIndex = value;
            }
        }

        /// <summary>Sim-seconds per real second at the current speed.</summary>
        public double SpeedMultiplier => _options.SpeedPresets[_speedIndex];

        /// <summary>True if the last <see cref="Advance"/> hit its step budget and dropped the backlog.</summary>
        public bool Lagging { get; private set; }

        public int ProbeCount => _probes.Count;

        /// <summary>Thread-safe: queue a command for the next tick.</summary>
        public void Enqueue(ISimCommand command) => _commands.Enqueue(command);

        /// <summary>Thread-safe: take the next simulation event, if any.</summary>
        public bool TryDequeueEvent(out SimEvent? simEvent) => _events.TryDequeue(out simEvent);

        /// <summary>Applies all queued commands. A command that throws is reported as <see cref="CommandRejected"/>.</summary>
        public void ApplyCommands()
        {
            while (_commands.TryDequeue(out var command))
            {
                try
                {
                    command.Apply(this);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
                {
                    _events.Enqueue(new CommandRejected(Clock.SimTime, ex.Message));
                }
            }
        }

        /// <summary>
        /// Advances by <paramref name="simSeconds"/> of simulated time, in whole macro steps (the remainder carries
        /// over). If more than <paramref name="maxSteps"/> steps are due, takes <paramref name="maxSteps"/>, drops
        /// the rest and sets <see cref="Lagging"/>. Returns the number of steps taken.
        /// </summary>
        public int Advance(double simSeconds, int maxSteps)
        {
            if (simSeconds < 0.0)
                throw new ArgumentOutOfRangeException(nameof(simSeconds));
            double dt = World.MacroDt;
            _pending += simSeconds;
            long due = (long)Math.Floor(_pending / dt + 1e-9);
            Lagging = due > maxSteps;
            int steps = Lagging ? maxSteps : (int)due;
            _pending = Lagging ? 0.0 : Math.Max(0.0, _pending - steps * dt);
            for (int i = 0; i < steps; i++)
                StepOnce(dt);
            return steps;
        }

        /// <summary>Takes exactly <paramref name="steps"/> macro steps, ignoring speed (headless runs).</summary>
        public void StepExactly(long steps)
        {
            for (long i = 0; i < steps; i++)
                StepOnce(World.MacroDt);
        }

        internal void AddProbe(int cell, int channel)
        {
            if (cell < 0 || cell >= World.CellCount)
                throw new ArgumentOutOfRangeException(nameof(cell), $"no cell {cell}");
            if (channel < 0 || channel >= World.Overlays.Count)
                throw new ArgumentOutOfRangeException(nameof(channel), $"no channel {channel}");
            if (_probes.Count >= _options.MaxProbes)
                throw new InvalidOperationException($"at most {_options.MaxProbes} probes");
            var probe = new Probe(cell, channel, _options.ProbeSamples);
            probe.Add(Clock.SimTime, (float)World.Read(cell, channel));
            _probes.Add(probe);
        }

        internal void ClearProbes() => _probes.Clear();

        /// <summary>Creates a frame sized for this loop's world and options.</summary>
        public SnapshotFrame CreateFrame() =>
            new SnapshotFrame(World.Overlays.Count, World.CellCount, _options.MaxProbes, _options.ProbeSamples);

        /// <summary>Writes the current state into <paramref name="frame"/> (no allocation).</summary>
        public void WriteSnapshot(SnapshotFrame frame)
        {
            frame.Sequence = ++_sequence;
            frame.SimTime = Clock.SimTime;
            frame.CheckpointIndex = Clock.CheckpointIndex;
            frame.SpeedIndex = _speedIndex;
            frame.Lagging = Lagging;
            frame.CellCount = World.CellCount;
            for (int c = 0; c < frame.Channels.Length; c++)
            {
                float[] channel = frame.Channels[c];
                for (int k = 0; k < World.CellCount; k++)
                    channel[k] = (float)World.Read(k, c);
            }
            frame.ProbeCount = _probes.Count;
            for (int p = 0; p < frame.Probes.Length; p++)
            {
                if (p < _probes.Count)
                    frame.Probes[p].CopyFrom(_probes[p]);
                else
                    frame.Probes[p].Clear();
            }
        }

        private void StepOnce(double dt)
        {
            World.Step(Clock.SimTime, dt);
            if (Clock.Advance(dt))
                _events.Enqueue(new CheckpointReached(Clock.SimTime, Clock.CheckpointIndex));
            if (_probes.Count > 0 && Clock.TickIndex % _stepsPerProbeSample == 0)
            {
                foreach (var probe in _probes)
                    probe.Add(Clock.SimTime, (float)World.Read(probe.Cell, probe.Channel));
            }
        }
    }
}
