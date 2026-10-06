using System;

namespace AdaptOrDeity.Sim.Core
{
    /// <summary>
    /// Single source of truth for simulated time. Nothing else in the simulation keeps its own time.
    /// Times are SI seconds.
    /// </summary>
    public sealed class SimClock
    {
        public SimClock(double checkpointInterval)
        {
            if (!(checkpointInterval > 0.0))
                throw new ArgumentOutOfRangeException(nameof(checkpointInterval), "must be > 0 s");
            CheckpointInterval = checkpointInterval;
            NextCheckpointTime = checkpointInterval;
        }

        /// <summary>Simulated time since the start of the game [s].</summary>
        public double SimTime { get; private set; }

        /// <summary>Number of completed macro steps (or epochs in <see cref="SimMode.EpochSteadyState"/>).</summary>
        public long TickIndex { get; private set; }

        public SimMode Mode { get; set; } = SimMode.RealTime;

        /// <summary>Simulated time between checkpoints in <see cref="SimMode.RealTime"/> [s].</summary>
        public double CheckpointInterval { get; }

        /// <summary>Simulated time at which the next checkpoint fires [s].</summary>
        public double NextCheckpointTime { get; private set; }

        /// <summary>Number of checkpoints crossed so far.</summary>
        public long CheckpointIndex { get; private set; }

        /// <summary>
        /// Advances the clock by one step of <paramref name="dt"/> seconds.
        /// Returns true when this step crossed a checkpoint. Every epoch step is a checkpoint.
        /// </summary>
        public bool Advance(double dt)
        {
            if (!(dt > 0.0))
                throw new ArgumentOutOfRangeException(nameof(dt), "must be > 0 s");

            SimTime += dt;
            TickIndex++;

            if (Mode == SimMode.EpochSteadyState)
            {
                CheckpointIndex++;
                NextCheckpointTime = SimTime + CheckpointInterval;
                return true;
            }

            if (SimTime < NextCheckpointTime)
                return false;

            CheckpointIndex++;
            // Skip any checkpoints passed within a single large step so they don't fire in a burst.
            while (NextCheckpointTime <= SimTime)
                NextCheckpointTime += CheckpointInterval;
            return true;
        }
    }
}
