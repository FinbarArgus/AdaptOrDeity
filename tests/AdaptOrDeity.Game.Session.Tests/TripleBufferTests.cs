using System.Threading;
using System.Threading.Tasks;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class TripleBufferTests
    {
        private sealed class Slot
        {
            public long A;
            public long B;
        }

        [Test]
        public void AcquireReturnsNewestPublishedOnlyOnce()
        {
            var buffer = new TripleBuffer<Slot>(new Slot(), new Slot(), new Slot());
            Assert.That(buffer.TryAcquire(out _), Is.False);

            buffer.Back.A = 1;
            buffer.Publish();
            buffer.Back.A = 2;
            buffer.Publish();

            Assert.That(buffer.TryAcquire(out var front), Is.True);
            Assert.That(front.A, Is.EqualTo(2));
            Assert.That(buffer.TryAcquire(out front), Is.False);
            Assert.That(front.A, Is.EqualTo(2));
        }

        [Test]
        public void ConsumerNeverSeesTornOrOlderFrames()
        {
            var buffer = new TripleBuffer<Slot>(new Slot(), new Slot(), new Slot());
            const long frames = 2_000_000;
            long lastSeen = 0, torn = 0, backwards = 0;
            using var done = new ManualResetEventSlim();

            var producer = Task.Run(() =>
            {
                for (long n = 1; n <= frames; n++)
                {
                    var slot = buffer.Back;
                    slot.A = n;
                    Thread.MemoryBarrier();
                    slot.B = n;
                    buffer.Publish();
                }
                done.Set();
            });

            while (!done.IsSet || buffer.TryAcquire(out _))
            {
                if (!buffer.TryAcquire(out var front))
                    continue;
                long b = front.B, a = front.A;
                if (a != b)
                    torn++;
                if (a < lastSeen)
                    backwards++;
                lastSeen = a;
            }
            producer.Wait();
            buffer.TryAcquire(out var last);

            Assert.That(torn, Is.Zero);
            Assert.That(backwards, Is.Zero);
            Assert.That(last.A, Is.EqualTo(frames));
        }
    }
}
