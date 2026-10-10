using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A request from the front end, applied on the simulation thread at the start of the next tick.
    /// The front end never touches simulation state directly.
    /// </summary>
    public interface ISimCommand
    {
        void Apply(SimLoop loop);
    }

    /// <summary>Factory for the commands the front end may send.</summary>
    public static class SimCommands
    {
        public static ISimCommand SetSpeed(int speedIndex) => new SetSpeedCommand(speedIndex);

        public static ISimCommand AddProbe(int cell, int channel) => new AddProbeCommand(cell, channel);

        public static ISimCommand ClearProbes() => new ClearProbesCommand();

        public static ISimCommand SetBathConcentration(double concentration) => new SetBathCommand(concentration);

        /// <summary>Sets the target multiplier of trait <paramref name="trait"/> for every cell of <paramref name="group"/>.</summary>
        public static ISimCommand SetTrait(int group, int trait, double multiplier) =>
            new WorldCommand(w => w.SetTrait(group, trait, multiplier));

        /// <summary>Sets an energy control (growth, storage, repair) for every cell of <paramref name="group"/>.</summary>
        public static ISimCommand SetControl(int group, int control, double value) =>
            new WorldCommand(w => w.SetControl(group, control, value));

        /// <summary>Divides <paramref name="cell"/>, spending glycogen to finish the daughter if needed.</summary>
        public static ISimCommand Divide(int cell) => new WorldCommand(w => w.Divide(cell));

        /// <summary>Turns an unlocked optional feature on or off.</summary>
        public static ISimCommand SetFeature(int feature, bool enabled) =>
            new WorldCommand(w => w.SetFeature(feature, enabled));

        /// <summary>Sets the level being played (unlocks features); a development control until the time bar drives it.</summary>
        public static ISimCommand SetLevel(int level) => new WorldCommand(w =>
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), "levels start at 1");
            w.Level = level;
        });

        private sealed class SetSpeedCommand : ISimCommand
        {
            private readonly int _index;

            public SetSpeedCommand(int index) => _index = index;

            public void Apply(SimLoop loop) => loop.SpeedIndex = _index;
        }

        private sealed class AddProbeCommand : ISimCommand
        {
            private readonly int _cell;
            private readonly int _channel;

            public AddProbeCommand(int cell, int channel)
            {
                _cell = cell;
                _channel = channel;
            }

            public void Apply(SimLoop loop) => loop.AddProbe(_cell, _channel);
        }

        private sealed class ClearProbesCommand : ISimCommand
        {
            public void Apply(SimLoop loop) => loop.ClearProbes();
        }

        private sealed class SetBathCommand : ISimCommand
        {
            private readonly double _concentration;

            public SetBathCommand(double concentration) => _concentration = concentration;

            public void Apply(SimLoop loop)
            {
                if (!(loop.World is IBathWorld bath))
                    throw new InvalidOperationException("this world has no bath");
                bath.BathConcentration = _concentration;
            }
        }

        /// <summary>Development aid: completes <paramref name="cell"/>'s daughter without charging its ATP (not for play).</summary>
        public static ISimCommand DevCompleteDaughter(int cell) => new WorldCommand(w =>
        {
            if (!(w is FermenterWorld f))
                throw new InvalidOperationException("this world has no growing cells");
            f.DevCompleteDaughter(cell);
        });

        /// <summary>Moves flagellum <paramref name="k"/> of a cell to a base angle on its membrane (body frame) [rad].</summary>
        public static ISimCommand SetFlagellumAngle(int cell, int k, double angle) => new WorldCommand(w =>
        {
            if (!(w is FermenterWorld f))
                throw new InvalidOperationException("this world has no flagella");
            f.SetFlagellumAngle(cell, k, angle);
        });

        private sealed class WorldCommand : ISimCommand
        {
            private readonly Action<IInspectableWorld> _action;

            public WorldCommand(Action<IInspectableWorld> action) => _action = action;

            public void Apply(SimLoop loop)
            {
                if (!(loop.World is IInspectableWorld world))
                    throw new InvalidOperationException("this world has no adaptable cells");
                _action(world);
            }
        }
    }

    /// <summary>Something that happened in the simulation, delivered to the front end in order.</summary>
    public abstract class SimEvent
    {
        protected SimEvent(double simTime) => SimTime = simTime;

        /// <summary>When it happened [s].</summary>
        public double SimTime { get; }
    }

    public sealed class CheckpointReached : SimEvent
    {
        public CheckpointReached(double simTime, long index) : base(simTime) => Index = index;

        public long Index { get; }
    }

    /// <summary>A command was rejected (invalid arguments); the simulation carried on.</summary>
    public sealed class CommandRejected : SimEvent
    {
        public CommandRejected(double simTime, string reason) : base(simTime) => Reason = reason;

        public string Reason { get; }
    }
}
