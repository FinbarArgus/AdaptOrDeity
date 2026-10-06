using System;
using System.Linq;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class SimLoopTests
    {
        [TestCase(1, 10.0)]
        [TestCase(2, 20.0)]
        [TestCase(3, 30.0)]
        public void HostAdvancesAtTheRequestedSpeed(int speedIndex, double expectedSimSeconds)
        {
            // Ten real seconds at 60 ticks/s with a fake clock; sim time must match speed × real time within 1%.
            var loop = new SimLoop(new OneCellWorld());
            double now = 0.0;
            using var host = new SimHost(loop, () => now, maxStepsPerTick: 100_000);
            loop.Enqueue(SimCommands.SetSpeed(speedIndex));
            host.Tick();
            for (int i = 0; i < 600; i++)
            {
                now += 1.0 / 60.0;
                host.Tick();
            }
            Assert.That(loop.Clock.SimTime, Is.EqualTo(expectedSimSeconds).Within(0.01 * expectedSimSeconds));
            Assert.That(loop.Lagging, Is.False);
        }

        [Test]
        public void PauseStopsTimeAndResumeContinues()
        {
            var loop = new SimLoop(new OneCellWorld());
            double now = 0.0;
            using var host = new SimHost(loop, () => now);
            host.Tick();
            loop.Enqueue(SimCommands.SetSpeed(0));
            for (int i = 0; i < 60; i++)
            {
                now += 1.0 / 60.0;
                host.Tick();
            }
            Assert.That(loop.Clock.SimTime, Is.Zero);

            loop.Enqueue(SimCommands.SetSpeed(1));
            now += 0.5;
            host.Tick(); // applies the command, then advances by the 0.5 s since the last tick
            Assert.That(loop.Clock.SimTime, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void OverBudgetTickReportsLaggingAndDropsBacklog()
        {
            var loop = new SimLoop(new OneCellWorld());
            int steps = loop.Advance(simSeconds: 10.0, maxSteps: 100);
            Assert.That(steps, Is.EqualTo(100));
            Assert.That(loop.Lagging, Is.True);

            steps = loop.Advance(simSeconds: 0.0, maxSteps: 100);
            Assert.That(steps, Is.Zero, "dropped backlog must not be replayed");
            Assert.That(loop.Lagging, Is.False);
        }

        [Test]
        public void SameCommandsGiveBitIdenticalResults()
        {
            double Run()
            {
                var loop = new SimLoop(new OneCellWorld());
                loop.Advance(0.3, int.MaxValue);
                loop.Enqueue(SimCommands.SetBathConcentration(0.05));
                loop.ApplyCommands();
                loop.Advance(0.7, int.MaxValue);
                return loop.World.Read(0, 0);
            }
            long first = BitConverter.DoubleToInt64Bits(Run());
            long second = BitConverter.DoubleToInt64Bits(Run());
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ProbeRecordsTimeSeriesIntoSnapshot()
        {
            var loop = new SimLoop(new OneCellWorld(), new SimLoopOptions { ProbeInterval = 0.01, ProbeSamples = 50 });
            loop.Enqueue(SimCommands.AddProbe(cell: 0, channel: 0));
            loop.ApplyCommands();
            loop.StepExactly(1000); // 1 s → 100 samples, ring keeps the newest 50

            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            var probe = frame.Probes[0];
            Assert.That(frame.ProbeCount, Is.EqualTo(1));
            Assert.That(probe.Count, Is.EqualTo(50));
            var times = probe.Times.Take(probe.Count).ToArray();
            Assert.That(times, Is.Ordered.Ascending);
            Assert.That(times[^1], Is.EqualTo(1.0).Within(1e-9));
            Assert.That(probe.Values[probe.Count - 1], Is.EqualTo(frame.Channels[0][0]));
        }

        [Test]
        public void InvalidCommandIsRejectedNotThrown()
        {
            var loop = new SimLoop(new OneCellWorld());
            loop.Enqueue(SimCommands.AddProbe(cell: 5, channel: 0));
            loop.Enqueue(SimCommands.SetSpeed(99));
            loop.ApplyCommands();

            int rejected = 0;
            while (loop.TryDequeueEvent(out var e))
                if (e is CommandRejected)
                    rejected++;
            Assert.That(rejected, Is.EqualTo(2));
            Assert.That(loop.ProbeCount, Is.Zero);
        }

        [Test]
        public void CheckpointEventsFireAtInterval()
        {
            var loop = new SimLoop(new OneCellWorld(), new SimLoopOptions { CheckpointInterval = 0.25 });
            loop.StepExactly(1000);
            int checkpoints = 0;
            while (loop.TryDequeueEvent(out var e))
                if (e is CheckpointReached)
                    checkpoints++;
            Assert.That(checkpoints, Is.EqualTo(4));
        }

        [Test]
        public void TickDoesNotAllocateOnceWarm()
        {
            var loop = new SimLoop(new OneCellWorld());
            double now = 0.0;
            using var host = new SimHost(loop, () => now);
            loop.Enqueue(SimCommands.AddProbe(0, 0));
            for (int i = 0; i < 120; i++)
            {
                now += 1.0 / 60.0;
                host.Tick();
                host.TryAcquire(out _);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 600; i++)
            {
                now += 1.0 / 60.0;
                host.Tick();
                host.TryAcquire(out _);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void ThreadedSessionPublishesFrames()
        {
            using var session = SimSession.CreateOneCell();
            session.Start();
            SnapshotFrame? frame = null;
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline && (frame == null || frame.SimTime < 0.05))
                if (session.TryGetFrame(out var f))
                    frame = f;
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame!.SimTime, Is.GreaterThanOrEqualTo(0.05));
            Assert.That(frame.Channels[0][0], Is.GreaterThan(0f).And.LessThan(0.2f));
        }
    }
}
