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
            var inspect = world as IInspectableWorld;
            var food = world as IFoodWorld;
            var traces = new List<string>();
            if (food != null)
                foreach (var t in food.Food.TraceSpecies)
                    traces.Add(t.Name);
            Schema = new SessionSchema(world.Overlays, _options.SpeedPresets, inspect?.Details, inspect?.Traits,
                inspect?.Controls, inspect?.Features, inspect?.InitialCellCapacity ?? world.CellCount, inspect?.MaxGroups ?? 0,
                traces, food?.Food.HalfSize ?? 0.0, food?.Food.Settings.InitialPacketCapacity ?? 0);
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

        /// <summary>Reports an event to the front end (from the simulation thread).</summary>
        internal void Post(SimEvent simEvent) => _events.Enqueue(simEvent);

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
        /// the rest and sets <see cref="Lagging"/>; so does running out of <paramref name="outOfTime"/> (a wall-clock budget, so
        /// frames keep coming at speeds the simulation cannot reach). Returns the number of steps taken.
        /// </summary>
        public int Advance(double simSeconds, int maxSteps, Func<bool>? outOfTime = null)
        {
            if (simSeconds < 0.0)
                throw new ArgumentOutOfRangeException(nameof(simSeconds));
            double dt = World.MacroDt;
            _pending += simSeconds;
            long due = (long)Math.Floor(_pending / dt + 1e-9);
            Lagging = due > maxSteps;
            int steps = Lagging ? maxSteps : (int)due;
            int taken = 0;
            while (taken < steps)
            {
                StepOnce(dt);
                taken++;
                // a real-time budget: stop and publish rather than fall seconds behind the screen
                if (taken < steps && outOfTime != null && outOfTime())
                {
                    Lagging = true;
                    break;
                }
            }
            _pending = Lagging ? 0.0 : Math.Max(0.0, _pending - taken * dt);
            return taken;
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

        /// <summary>The cell whose details frames carry (-1: none; <see cref="SimCommands.Inspect"/>).</summary>
        public int InspectedCell { get; set; } = -1;

        /// <summary>
        /// A frame for the current world: room for at least the cells and food packets there are, in powers of two.
        /// </summary>
        public SnapshotFrame CreateFrame() =>
            new SnapshotFrame(World.Overlays.Count, Capacity(Math.Max(World.CellCount, Schema.InitialCellCapacity)), _options.MaxProbes,
                _options.ProbeSamples, Schema.Details.Count, Schema.Traits.Count, Schema.Controls.Count, Schema.MaxGroups,
                Schema.Features.Count, Schema.InitialPacketCapacity == 0 ? 0
                    : Capacity(Math.Max(PacketsInField, Schema.InitialPacketCapacity)));

        /// <summary>True when <paramref name="frame"/> has no room for the current colony or food (frames grow, see SimHost).</summary>
        public bool Outgrown(SnapshotFrame frame) =>
            frame.CellX.Length < World.CellCount || (Schema.InitialPacketCapacity > 0 && frame.PacketX.Length < PacketsInField);

        private int PacketsInField => World is IFoodWorld f ? f.Food.ActiveCount : 0;

        private static int Capacity(int cells)
        {
            int c = 1;
            while (c < cells)
                c *= 2;
            return c;
        }

        /// <summary>Writes the current state into <paramref name="frame"/> (no allocation).</summary>
        public void WriteSnapshot(SnapshotFrame frame)
        {
            frame.Sequence = ++_sequence;
            frame.SimTime = Clock.SimTime;
            frame.CheckpointIndex = Clock.CheckpointIndex;
            frame.SpeedIndex = _speedIndex;
            frame.Lagging = Lagging;
            frame.CellCount = World.CellCount;
            // per-cell fields: each cell's are independent, so the world may write them on its worker threads
            _writing = frame;
            _writeCells ??= WriteCells;
            if (World is IParallelWorld parallel)
                parallel.ForEachCell(_writeCells);
            else
                WriteCells(0, World.CellCount);
            _writing = null;
            if (World is IInspectableWorld inspect)
            {
                // details for the inspected cell only: they cost the cell's whole set of algebraic variables
                int seen = InspectedCell;
                if (seen >= 0 && seen < World.CellCount)
                    for (int d = 0; d < frame.Details.Length; d++)
                        frame.Details[d][seen] = inspect.ReadDetail(seen, d);
                frame.GroupCount = inspect.GroupCount;
                for (int c = 0; c < frame.Controls.Length; c++)
                    for (int g = 0; g < inspect.GroupCount; g++)
                        frame.Controls[c][g] = (float)inspect.ReadControl(g, c);
                frame.Level = inspect.Level;
                for (int f = 0; f < frame.FeatureEnabled.Length; f++)
                {
                    frame.FeatureUnlocked[f] = inspect.FeatureUnlocked(f);
                    frame.FeatureEnabled[f] = inspect.FeatureEnabled(f);
                }
            }
            if (World is IFoodWorld foodWorld)
            {
                var body = foodWorld.Body.Body;
                frame.CreatureX = body.X;
                frame.CreatureY = body.Y;
                frame.CreatureVx = (float)body.GroundVx;
                frame.CreatureVy = (float)body.GroundVy;
                frame.Orientation = (float)body.Angle;
                frame.Spin = (float)body.Omega;
                frame.MechanicalPower = (float)body.Power;
                frame.HeatToWater = body.Dissipated;
                if (World is FermenterWorld fw)
                {
                    frame.MotorOn = fw.MotorOn && fw.FeatureEnabled(GameFeatures.IndexOf(GameFeatures.Swimming));
                    frame.MotorPower = (float)fw.MotorPower;
                }
                int n = 0;
                foreach (var p in foodWorld.Food.Packets)
                {
                    if (!p.Active || n >= frame.PacketX.Length)
                        continue;
                    frame.PacketX[n] = (float)(p.X - frame.CreatureX);
                    frame.PacketY[n] = (float)(p.Y - frame.CreatureY);
                    frame.PacketRadius[n] = (float)p.Radius;
                    frame.PacketFill[n] = (float)foodWorld.Food.Fill(p);
                    frame.PacketTrace[n] = p.MainTrace;
                    frame.PacketCarried[n] = p.Kind == AdaptOrDeity.Sim.World.PacketKind.Carried && !p.Stuck;
                    n++;
                }
                frame.PacketCount = n;
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

        private SnapshotFrame? _writing;
        private Action<int, int>? _writeCells;   // cached delegate: writing a frame must not allocate

        // Cells [start, end) of the frame being written.
        private void WriteCells(int start, int end)
        {
            var frame = _writing!;
            for (int c = 0; c < frame.Channels.Length; c++)
            {
                float[] channel = frame.Channels[c];
                for (int k = start; k < end; k++)
                    channel[k] = (float)World.Read(k, c);
            }
            if (World is IInspectableWorld inspect)
            {
                for (int t = 0; t < frame.TraitValues.Length; t++)
                    for (int k = start; k < end; k++)
                    {
                        frame.TraitValues[t][k] = (float)inspect.ReadTrait(k, t);
                        frame.TraitTargets[t][k] = (float)inspect.ReadTraitTarget(k, t);
                    }
                for (int k = start; k < end; k++)
                {
                    frame.CellGroup[k] = inspect.CellGroup(k);
                    frame.CellAlive[k] = inspect.CellAlive(k);
                    frame.CellCanDivide[k] = inspect.CellCanDivide(k);
                }
            }
            if (World is IFoodWorld foodWorld)
            {
                for (int k = start; k < end; k++)
                {
                    for (int f = 0; f < AdaptOrDeity.Sim.World.CreatureBody.MaxFlagella; f++)
                        frame.FlagellumAngles[k][f] = (float)foodWorld.Body.FlagellumAngle(k, f);
                    foodWorld.Body.LocalPosition(k, out double x, out double y);
                    frame.CellX[k] = (float)x;   // on the body: the front end turns them with the body
                    frame.CellY[k] = (float)y;
                }
            }
            if (World is FermenterWorld fw)
                for (int k = start; k < end; k++)
                {
                    var cell = fw.Cell(k);
                    frame.CellSurface[k] = (float)(cell.SurfaceArea * 1e-12);   // µm² -> m²
                    for (int t = 0; t < frame.TraitArea.Length && t < cell.Traits.Count; t++)
                        frame.TraitArea[t][k] = (float)(cell.TraitAreaPerUnit(t) * cell.TraitValue(t) * 1e-12);
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
