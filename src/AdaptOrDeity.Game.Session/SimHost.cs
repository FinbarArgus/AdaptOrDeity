using System;
using System.Diagnostics;
using System.Threading;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Runs a <see cref="SimLoop"/> on its own thread, paced to real time × speed, publishing snapshots through
    /// a <see cref="TripleBuffer{T}"/>. One persistent worker, as in CUFLynx's live-simulation worker.
    /// </summary>
    public sealed class SimHost : IDisposable
    {
        private readonly SimLoop _loop;
        private readonly Func<double> _realClock;
        private readonly int _maxStepsPerTick;
        private readonly TripleBuffer<SnapshotFrame> _frames;
        private Thread? _thread;
        private volatile bool _stop;
        private double _lastReal = double.NaN;
        private readonly Stopwatch _tickClock = new Stopwatch();
        private readonly Func<bool> _outOfTime;   // cached: a tick must not allocate

        /// <summary>Wall-clock time a tick may spend stepping before it publishes [s]: ~30 frames a second at any speed.</summary>
        public double TickBudget { get; set; } = 0.03;

        /// <summary>The exception that stopped the simulation thread, or null while it runs.</summary>
        public Exception? Fault { get; private set; }

        /// <param name="loop">The loop to run. Only this host may touch it once started.</param>
        /// <param name="realClock">Real time in seconds; defaults to a monotonic stopwatch. Injectable for tests.</param>
        /// <param name="maxStepsPerTick">Step budget per tick; beyond it the loop reports lagging instead of catching up.</param>
        public SimHost(SimLoop loop, Func<double>? realClock = null, int maxStepsPerTick = 2000)
        {
            _loop = loop;
            var stopwatch = Stopwatch.StartNew();
            _realClock = realClock ?? (() => stopwatch.Elapsed.TotalSeconds);
            _maxStepsPerTick = maxStepsPerTick;
            _outOfTime = () => _tickClock.Elapsed.TotalSeconds > TickBudget;
            _frames = new TripleBuffer<SnapshotFrame>(loop.CreateFrame(), loop.CreateFrame(), loop.CreateFrame());
            _loop.WriteSnapshot(_frames.Back);
            _frames.Publish();
        }

        public bool IsRunning => _thread != null;

        /// <summary>Starts the simulation thread.</summary>
        public void Start()
        {
            if (_thread != null)
                throw new InvalidOperationException("already started");
            _thread = new Thread(Run) { IsBackground = true, Name = "AdaptOrDeity.Sim" };
            _thread.Start();
        }

        /// <summary>One iteration of the simulation thread. Public so tests can pump it with a fake clock.</summary>
        public void Tick()
        {
            double now = _realClock();
            double realDt = double.IsNaN(_lastReal) ? 0.0 : Math.Max(0.0, now - _lastReal);
            _lastReal = now;

            _loop.ApplyCommands();
            _tickClock.Restart();
            _loop.Advance(realDt * _loop.SpeedMultiplier, _maxStepsPerTick, _outOfTime);
            _loop.WriteSnapshot(_frames.Back);
            _frames.Publish();
        }

        /// <summary>Front end: the newest snapshot, if one was published since the last call.</summary>
        public bool TryAcquire(out SnapshotFrame frame) => _frames.TryAcquire(out frame);

        public void Dispose()
        {
            _stop = true;
            _thread?.Join();
            _thread = null;
            (_loop.World as IDisposable)?.Dispose();
        }

        private void Run()
        {
            try
            {
                while (!_stop)
                {
                    Tick();
                    Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                // a background thread dies silently: keep the reason so the front end can report it
                Fault = e;
            }
        }
    }
}
