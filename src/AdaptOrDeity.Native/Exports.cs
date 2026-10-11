using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AdaptOrDeity.Game.Session;

namespace AdaptOrDeity.Native
{
    /// <summary>
    /// The C entry points of the simulation library. Sessions are numbered handles. Every call returns a negative number
    /// on failure (the reason through <c>aod_session_fault</c>): exceptions cannot cross the boundary. Strings are UTF-8;
    /// buffers belong to the caller. The front-end side is <see cref="NativeSession"/>.
    /// </summary>
    public static unsafe class Exports
    {
        private sealed class Entry
        {
            public SimSession Session = null!;
            public string? Error;
            public SimEvent? Pending;
        }

        private static readonly List<Entry?> Sessions = new List<Entry?>();

        [UnmanagedCallersOnly(EntryPoint = "aod_ping")]
        public static int Ping() => 1;

        /// <summary>A new session: world 0 the one-cell bath, 1 the fermenter. Returns its handle.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_session_create")]
        public static int Create(int world)
        {
            try
            {
                var session = world == 1 ? SimSession.CreateFermenter() : SimSession.CreateOneCell();
                lock (Sessions)
                {
                    Sessions.Add(new Entry { Session = session });
                    return Sessions.Count - 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "aod_session_start")]
        public static int Start(int handle) => Guard(handle, e => { e.Session.Start(); return 0; });

        [UnmanagedCallersOnly(EntryPoint = "aod_session_command")]
        public static int Command(int handle, int kind, int a, int b, int c, double x, byte* text1, byte* text2) =>
            Guard(handle, e =>
            {
                e.Session.Enqueue(new SimCommand((SimCommandKind)kind, a, b, c, x, Text(text1), Text(text2)));
                return 0;
            });

        /// <summary>The newest frame since the last call into <paramref name="buffer"/>: its bytes, or 0 if there is none.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_session_take_frame")]
        public static int TakeFrame(int handle, byte* buffer, int length) =>
            Guard(handle, e => e.Session.TryGetFrame(out var frame)
                ? SnapshotCodec.Write(frame, new Span<byte>(buffer, length)) : 0);

        /// <summary>Applies queued commands, simulates unpaced on the caller's thread and writes the frame.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_session_run_headless")]
        public static int RunHeadless(int handle, double simSeconds, byte* buffer, int length) =>
            Guard(handle, e => SnapshotCodec.Write(e.Session.RunHeadless(simSeconds), new Span<byte>(buffer, length)));

        /// <summary>
        /// The next event: 1 and its kind (1 checkpoint, 2 rejected command, 3 model exported), time, index and text; 0
        /// when there is none. A text longer than the buffer keeps the event for a retry with a larger one.
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_session_next_event")]
        public static int NextEvent(int handle, int* kind, double* time, long* index, byte* text, int textLength, int* length) =>
            Guard(handle, e =>
            {
                if (e.Pending == null && !e.Session.TryDequeueEvent(out e.Pending))
                    return 0;
                var ev = e.Pending!;
                string message = ev is CommandRejected r ? r.Reason : ev is ModelExported m ? m.Path : "";
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                *length = bytes.Length;
                if (bytes.Length > textLength)
                    return -2;
                *kind = ev is CheckpointReached ? 1 : ev is CommandRejected ? 2 : 3;
                *time = ev.SimTime;
                *index = ev is CheckpointReached c ? c.Index : 0;
                Marshal.Copy(bytes, 0, (IntPtr)text, bytes.Length);
                e.Pending = null;
                return 1;
            });

        /// <summary>Why the session stopped or the last call failed (UTF-8, its length), or 0 if it has not.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_session_fault")]
        public static int Fault(int handle, byte* text, int textLength)
        {
            var e = Find(handle);
            string? message = e == null ? $"no session {handle}" : e.Session.Fault?.ToString() ?? e.Error;
            if (message == null)
                return 0;
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            int n = Math.Min(bytes.Length, textLength);
            Marshal.Copy(bytes, 0, (IntPtr)text, n);
            return n;
        }

        [UnmanagedCallersOnly(EntryPoint = "aod_session_dispose")]
        public static void Dispose(int handle)
        {
            lock (Sessions)
            {
                if (handle >= 0 && handle < Sessions.Count && Sessions[handle] is Entry e)
                {
                    e.Session.Dispose();
                    Sessions[handle] = null;
                }
            }
        }

        private static Entry? Find(int handle)
        {
            lock (Sessions)
                return handle >= 0 && handle < Sessions.Count ? Sessions[handle] : null;
        }

        private static int Guard(int handle, Func<Entry, int> call)
        {
            var e = Find(handle);
            if (e == null)
                return -1;
            try
            {
                e.Error = null;
                return call(e);
            }
            catch (Exception ex)
            {
                e.Error = ex.ToString();
                return -1;
            }
        }

        private static string? Text(byte* utf8) => utf8 == null ? null : Marshal.PtrToStringUTF8((IntPtr)utf8);
    }
}
