using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A session running in the NativeAOT simulation library (src/AdaptOrDeity.Native, <c>aod_sim</c>), driven through
    /// its C entry points: commands as plain data, snapshots as bytes (<see cref="SnapshotCodec"/>). The schema and the
    /// frames' shape are built here from the same code and content, so nothing else crosses the boundary.
    /// </summary>
    public sealed class NativeSession : ISessionFront
    {
        private const string Library = "aod_sim";
        private readonly int _handle;
        private readonly byte[] _buffer;
        private readonly byte[] _text = new byte[4096];
        private readonly SnapshotFrame[] _frames;
        private int _front;
        private bool _disposed;

        private NativeSession(int world, SimLoop shape)
        {
            Schema = shape.Schema;
            _frames = new[] { shape.CreateFrame(), shape.CreateFrame() };
            _buffer = new byte[SnapshotCodec.Size(_frames[0])];
            _handle = aod_session_create(world);
            if (_handle < 0)
                throw new InvalidOperationException("the simulation library could not create the session");
        }

        /// <summary>The fermenter session (as <see cref="SimSession.CreateFermenter"/>) in the simulation library.</summary>
        public static NativeSession CreateFermenter()
        {
            if (aod_ping() != 1)
                throw new InvalidOperationException("the simulation library did not start");
            using var world = new FermenterWorld();
            return new NativeSession(1, new SimLoop(world, SimSession.FermenterOptions()));
        }

        public SessionSchema Schema { get; }

        public Exception? Fault
        {
            get
            {
                int n = aod_session_fault(_handle, _text, _text.Length);
                return n > 0 ? new InvalidOperationException("simulation library: " + Encoding.UTF8.GetString(_text, 0, n)) : null;
            }
        }

        public void Start() => Check(aod_session_start(_handle));

        public void Enqueue(SimCommand command) =>
            Check(aod_session_command(_handle, (int)command.Kind, command.A, command.B, command.C, command.X,
                Utf8(command.Text1), Utf8(command.Text2)));

        public bool TryGetFrame(out SnapshotFrame frame)
        {
            int n = aod_session_take_frame(_handle, _buffer, _buffer.Length);
            Check(n);
            if (n == 0)
            {
                frame = _frames[_front];
                return false;
            }
            _front ^= 1;
            SnapshotCodec.Read(new ReadOnlySpan<byte>(_buffer, 0, n), _frames[_front]);
            frame = _frames[_front];
            return true;
        }

        public bool TryDequeueEvent(out SimEvent? simEvent)
        {
            int got = aod_session_next_event(_handle, out int kind, out double time, out long index, _text, _text.Length, out int length);
            Check(got);
            simEvent = null;
            if (got == 0)
                return false;
            string text = Encoding.UTF8.GetString(_text, 0, length);
            simEvent = kind switch
            {
                1 => new CheckpointReached(time, index),
                2 => new CommandRejected(time, text),
                3 => new ModelExported(time, text),
                _ => throw new InvalidOperationException($"unknown event kind {kind} from the simulation library"),
            };
            return true;
        }

        public SnapshotFrame RunHeadless(double simSeconds)
        {
            int n = aod_session_run_headless(_handle, simSeconds, _buffer, _buffer.Length);
            Check(n);
            var frame = Fresh();   // one of the session's two frames: valid until the next frame is taken
            SnapshotCodec.Read(new ReadOnlySpan<byte>(_buffer, 0, n), frame);
            return frame;
        }

        private SnapshotFrame Fresh()
        {
            _front ^= 1;
            return _frames[_front];
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            aod_session_dispose(_handle);
        }

        private void Check(int result)
        {
            if (result < 0)
                throw new InvalidOperationException(Fault?.Message ?? "the simulation library rejected the call");
        }

        private static byte[]? Utf8(string? text) => text == null ? null : Encoding.UTF8.GetBytes(text + "\0");

        [DllImport(Library)] private static extern int aod_ping();
        [DllImport(Library)] private static extern int aod_session_create(int world);
        [DllImport(Library)] private static extern int aod_session_start(int handle);
        [DllImport(Library)] private static extern int aod_session_command(int handle, int kind, int a, int b, int c, double x, byte[]? text1, byte[]? text2);
        [DllImport(Library)] private static extern int aod_session_take_frame(int handle, byte[] buffer, int length);
        [DllImport(Library)] private static extern int aod_session_run_headless(int handle, double simSeconds, byte[] buffer, int length);
        [DllImport(Library)] private static extern int aod_session_next_event(int handle, out int kind, out double time, out long index, byte[] text, int textLength, out int length);
        [DllImport(Library)] private static extern int aod_session_fault(int handle, byte[] text, int textLength);
        [DllImport(Library)] private static extern void aod_session_dispose(int handle);
    }
}
