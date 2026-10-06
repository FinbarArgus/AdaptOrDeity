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
