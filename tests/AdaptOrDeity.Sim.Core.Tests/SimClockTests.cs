using System;
using AdaptOrDeity.Sim.Core;

namespace AdaptOrDeity.Sim.Core.Tests
{
    public class SimClockTests
    {
        [Test]
        public void AdvanceAccumulatesTimeAndTicks()
        {
            var clock = new SimClock(checkpointInterval: 10.0);
            clock.Advance(0.5);
            clock.Advance(0.25);
            Assert.That(clock.SimTime, Is.EqualTo(0.75));
            Assert.That(clock.TickIndex, Is.EqualTo(2));
        }

        [Test]
        public void RealTimeCheckpointFiresOncePerInterval()
        {
            var clock = new SimClock(checkpointInterval: 1.0);
            int fired = 0;
            for (int i = 0; i < 40; i++)
                if (clock.Advance(0.25))
                    fired++;
            Assert.That(fired, Is.EqualTo(10));
            Assert.That(clock.CheckpointIndex, Is.EqualTo(10));
        }

        [Test]
        public void LargeStepFiresOnlyOneCheckpoint()
        {
            var clock = new SimClock(checkpointInterval: 1.0);
            Assert.That(clock.Advance(3.5), Is.True);
            Assert.That(clock.CheckpointIndex, Is.EqualTo(1));
            Assert.That(clock.NextCheckpointTime, Is.EqualTo(4.0));
        }

        [Test]
        public void EveryEpochStepIsACheckpoint()
        {
            var clock = new SimClock(checkpointInterval: 1.0) { Mode = SimMode.EpochSteadyState };
            Assert.That(clock.Advance(1e7), Is.True);
            Assert.That(clock.Advance(1e7), Is.True);
            Assert.That(clock.CheckpointIndex, Is.EqualTo(2));
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        public void NonPositiveStepIsRejected(double dt)
        {
            var clock = new SimClock(checkpointInterval: 1.0);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(dt));
        }
    }
}
