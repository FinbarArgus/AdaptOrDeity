using System;
using AdaptOrDeity.Sim.Core;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Facade the front end (Unity, Headless) talks to. M0 stub: it only advances the clock on the caller's
    /// thread. M2 moves stepping to a dedicated SimHost thread behind a command queue and snapshot buffers.
    /// </summary>
    public sealed class SimSession
    {
        /// <summary>Sim-seconds per real second for each speed preset (index 0 = paused).</summary>
        private static readonly double[] SpeedPresets = { 0.0, 1.0, 2.0, 3.0 };

        private readonly SimClock _clock;
        private int _speedIndex = 1;

        public SimSession(double checkpointInterval = 600.0)
        {
            _clock = new SimClock(checkpointInterval);
        }

        public double SimTime => _clock.SimTime;

        public long CheckpointIndex => _clock.CheckpointIndex;

        public int SpeedIndex
        {
            get => _speedIndex;
            set
            {
                if (value < 0 || value >= SpeedPresets.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _speedIndex = value;
            }
        }

        /// <summary>Advances the simulation by the amount of sim time that corresponds to <paramref name="realDt"/>.</summary>
        public void Update(double realDt)
        {
            double simDt = realDt * SpeedPresets[_speedIndex];
            if (simDt > 0.0)
                _clock.Advance(simDt);
        }
    }
}
