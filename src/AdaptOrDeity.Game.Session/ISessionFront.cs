using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// What a front end needs of a running simulation: commands in, snapshots and events out. Implemented in process
    /// (<see cref="SimSession"/>) and by the NativeAOT simulation library (<see cref="NativeSession"/>).
    /// </summary>
    public interface ISessionFront : IDisposable
    {
        SessionSchema Schema { get; }

        /// <summary>Why the simulation stopped, or null while it runs.</summary>
        Exception? Fault { get; }

        void Start();

        void Enqueue(SimCommand command);

        /// <summary>The newest snapshot since the last call; valid until the next call.</summary>
        bool TryGetFrame(out SnapshotFrame frame);

        bool TryDequeueEvent(out SimEvent? simEvent);

        /// <summary>Applies queued commands, simulates <paramref name="simSeconds"/> unpaced on the caller, and returns a snapshot.</summary>
        SnapshotFrame RunHeadless(double simSeconds);
    }

    /// <summary>Picks where the simulation runs.</summary>
    public static class Sessions
    {
        /// <summary>
        /// The fermenter session in the NativeAOT simulation library when it can be loaded (modern .NET: faster, and
        /// threaded), otherwise in process. <paramref name="why"/> says which and, for in process, why.
        /// </summary>
        public static ISessionFront CreateFermenter(bool preferNative, out string why)
        {
            if (preferNative)
            {
                try
                {
                    var native = NativeSession.CreateFermenter();
                    why = "simulation library (NativeAOT)";
                    return native;
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is BadImageFormatException)
                {
                    why = $"in process: the simulation library is not available ({e.GetType().Name}: {e.Message})";
                    return SimSession.CreateFermenter();
                }
            }
            why = "in process";
            return SimSession.CreateFermenter();
        }
    }
}
