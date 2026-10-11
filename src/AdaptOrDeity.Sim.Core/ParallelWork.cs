using System;
using System.Threading;

namespace AdaptOrDeity.Sim.Core
{
    /// <summary>
    /// Runs independent work items on a fixed set of worker threads, the calling thread included. Items are cut into
    /// fixed chunks that the workers claim as they become free (so fast and slow cores share the load). Which thread
    /// runs an item varies, so items must not share mutable state; then results are the same for any worker count
    /// (ADR 0005), and anything items contribute to in common is summed afterwards in item order by the caller.
    /// Allocation-free once constructed. Not reentrant: one <see cref="Run"/> at a time.
    /// </summary>
    public sealed class ParallelWork : IDisposable
    {
        private readonly Thread[] _threads;
        private readonly ManualResetEventSlim[] _start;
        private readonly CountdownEvent _done;
        private Action<int, int>? _work;
        private int _count, _chunk, _next;
        private Exception? _error;
        private volatile bool _stopping;

        // Spins before a worker sleeps: waking a sleeping thread costs tens of microseconds, comparable to a step's work.
        private const int SpinCount = 2047;

        /// <param name="workers">Threads that run items, including the caller; 1 runs everything on the caller.</param>
        public ParallelWork(int workers)
        {
            if (workers < 1)
                throw new ArgumentOutOfRangeException(nameof(workers), "at least one worker");
            Workers = workers;
            _threads = new Thread[workers - 1];
            _start = new ManualResetEventSlim[workers - 1];
            _done = new CountdownEvent(1);
            for (int w = 1; w < workers; w++)
            {
                int index = w;
                _start[w - 1] = new ManualResetEventSlim(false, SpinCount);   // steps come back to back at speed
                _threads[w - 1] = new Thread(() => Loop(index)) { IsBackground = true, Name = $"AdaptOrDeity.Sim.Worker{w}" };
                _threads[w - 1].Start();
            }
        }

        /// <summary>
        /// Half the hardware threads less one (roughly the physical cores, leaving one for the front end), at most 8:
        /// with tens of cells per step, waking more threads costs more than they save (colony-bench).
        /// </summary>
        public static int DefaultWorkers => Math.Max(1, Math.Min(8, Environment.ProcessorCount / 2 - 1));

        public int Workers { get; }

        /// <summary>
        /// Calls <paramref name="work"/>(start, end) over [0, <paramref name="count"/>) in chunks of
        /// <paramref name="chunk"/> items and returns when all are done. An exception in any chunk is rethrown here.
        /// </summary>
        public void Run(int count, int chunk, Action<int, int> work)
        {
            if (chunk < 1)
                throw new ArgumentOutOfRangeException(nameof(chunk));
            int chunks = (count + chunk - 1) / chunk;
            if (Workers == 1 || chunks <= 1)
            {
                for (int c = 0; c < chunks; c++)
                    work(c * chunk, Math.Min(count, (c + 1) * chunk));
                return;
            }
            _work = work;
            _count = count;
            _chunk = chunk;
            _error = null;
            int helpers = Math.Min(Workers, chunks) - 1;
            _next = 0;
            _done.Reset(helpers);
            for (int w = 0; w < helpers; w++)
                _start[w].Set();
            RunChunks();
            _done.Wait();
            _work = null;
            if (_error != null)
                throw new AggregateException("a parallel work item failed", _error);
        }

        private void RunChunks()
        {
            int chunks = (_count + _chunk - 1) / _chunk;
            for (int c = Interlocked.Increment(ref _next) - 1; c < chunks; c = Interlocked.Increment(ref _next) - 1)
                _work!(c * _chunk, Math.Min(_count, (c + 1) * _chunk));
        }

        private void Loop(int index)
        {
            var start = _start[index - 1];
            while (true)
            {
                start.Wait();
                start.Reset();
                if (_stopping)
                    return;
                try
                {
                    RunChunks();
                }
                catch (Exception e)
                {
                    Interlocked.CompareExchange(ref _error, e, null);
                }
                _done.Signal();
            }
        }

        public void Dispose()
        {
            _stopping = true;
            foreach (var s in _start)
                s.Set();
            foreach (var t in _threads)
                t.Join();
            foreach (var s in _start)
                s.Dispose();
            _done.Dispose();
        }
    }
}
