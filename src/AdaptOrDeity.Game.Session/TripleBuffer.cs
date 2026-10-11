using System.Threading;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Lock-free single-producer / single-consumer triple buffer. The producer always has a back buffer to
    /// write; the consumer always gets the most recently published buffer and never blocks the producer.
    /// The ready-buffer index and its "fresh" flag share one atomic word, so a buffer is never handed out twice.
    /// </summary>
    public sealed class TripleBuffer<T> where T : class
    {
        private const int Fresh = 4;

        private readonly T[] _buffers;
        private int _back;
        private int _front;
        private int _ready; // index of the ready buffer | Fresh when it has not been acquired yet

        public TripleBuffer(T first, T second, T third)
        {
            _buffers = new[] { first, second, third };
            _back = 0;
            _ready = 1;
            _front = 2;
        }

        /// <summary>Producer: the buffer to write next.</summary>
        public T Back => _buffers[_back];

        /// <summary>Consumer: the buffer most recently acquired.</summary>
        public T Front => _buffers[_front];

        /// <summary>Producer: replaces the buffer it writes next (e.g. with a larger one).</summary>
        public void ReplaceBack(T buffer) => _buffers[_back] = buffer;

        /// <summary>Producer: publishes <see cref="Back"/> and takes a free buffer to write next.</summary>
        public void Publish()
        {
            int previous = Interlocked.Exchange(ref _ready, _back | Fresh);
            _back = previous & 3;
        }

        /// <summary>Consumer: swaps in the newest published buffer if there is one. Returns false if nothing new.</summary>
        public bool TryAcquire(out T front)
        {
            if ((Volatile.Read(ref _ready) & Fresh) == 0)
            {
                front = _buffers[_front];
                return false;
            }
            int previous = Interlocked.Exchange(ref _ready, _front);
            _front = previous & 3;
            front = _buffers[_front];
            return true;
        }
    }
}
