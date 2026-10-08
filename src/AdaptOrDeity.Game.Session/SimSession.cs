using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The facade front ends use (Unity, Headless). Commands go in, snapshots and events come out; the front
    /// end never touches simulation state. Call <see cref="Start"/> for real-time play, or
    /// <see cref="RunHeadless"/> for an unpaced run on the calling thread.
    /// </summary>
    public sealed class SimSession : IDisposable
    {
        private readonly SimLoop _loop;
        private readonly SimHost _host;

        public SimSession(ISimWorld world, SimLoopOptions? options = null)
        {
            _loop = new SimLoop(world, options);
            _host = new SimHost(_loop);
        }

        /// <summary>The M2 session: one cell in an oxygenated bath.</summary>
        public static SimSession CreateOneCell() => new SimSession(new OneCellWorld());

        /// <summary>The stage-1 session: the fermenter cell in a Proterozoic sea, at 1x, 10x and 100x.</summary>
        public static SimSession CreateFermenter() =>
            new SimSession(new FermenterWorld(), new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets, ProbeInterval = 1.0 });

        public SessionSchema Schema => _loop.Schema;

        public bool IsRunning => _host.IsRunning;

        public void Start() => _host.Start();

        public void Enqueue(ISimCommand command) => _loop.Enqueue(command);

        public bool TryGetFrame(out SnapshotFrame frame) => _host.TryAcquire(out frame);

        public bool TryDequeueEvent(out SimEvent? simEvent) => _loop.TryDequeueEvent(out simEvent);

        /// <summary>
        /// Applies queued commands, then simulates <paramref name="simSeconds"/> as fast as possible on the
        /// calling thread and returns a fresh snapshot. Not allowed while the threaded host is running.
        /// </summary>
        public SnapshotFrame RunHeadless(double simSeconds)
        {
            if (_host.IsRunning)
                throw new InvalidOperationException("session is running on its own thread");
            _loop.ApplyCommands();
            _loop.StepExactly((long)Math.Round(simSeconds / _loop.World.MacroDt));
            var frame = _loop.CreateFrame();
            _loop.WriteSnapshot(frame);
            return frame;
        }

        public void Dispose() => _host.Dispose();
    }
}
