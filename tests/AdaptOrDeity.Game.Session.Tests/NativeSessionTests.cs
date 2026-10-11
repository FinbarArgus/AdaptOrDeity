using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// The fermenter session in the NativeAOT simulation library behaves exactly as in process: the same commands give
    /// byte-identical frames, and events come back. Skipped until tools/scripts/build_native_sim.sh has built it.
    /// </summary>
    public class NativeSessionTests
    {
        private static string? _library;

        [OneTimeSetUp]
        public void FindLibrary()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AdaptOrDeity.sln")))
                dir = dir.Parent;
            string candidate = Path.Combine(dir!.FullName, "src", "AdaptOrDeity.Native", "bin", "publish", "linux-x64", "aod_sim.so");
            if (!File.Exists(candidate))
                return;
            _library = candidate;
            try
            {
                NativeLibrary.SetDllImportResolver(typeof(NativeSession).Assembly, (name, assembly, path) =>
                    name == "aod_sim" ? NativeLibrary.Load(_library) : IntPtr.Zero);
            }
            catch (InvalidOperationException)
            {
                // a resolver is already set (another fixture)
            }
        }

        private static byte[] Bytes(SnapshotFrame frame)
        {
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            return bytes;
        }

        private static void Drive(ISessionFront session)
        {
            session.Enqueue(SimCommands.SetLevel(2));
            session.Enqueue(SimCommands.SetBathConcentration(20.0));
            session.Enqueue(SimCommands.SetTrait(0, 1, 2.0));
            session.Enqueue(SimCommands.SetControl(0, FermenterWorld.GrowthControl, 3.0));
            session.Enqueue(SimCommands.AddProbe(0, 0));
        }

        [Test]
        public void SameCommandsGiveByteIdenticalFrames()
        {
            if (_library == null)
                Assert.Ignore("the simulation library is not built (tools/scripts/build_native_sim.sh)");
            using var managed = SimSession.CreateFermenter();
            using var native = NativeSession.CreateFermenter();
            Drive(managed);
            Drive(native);
            for (int round = 0; round < 3; round++)
            {
                var a = Bytes(managed.RunHeadless(20.0));
                var b = Bytes(native.RunHeadless(20.0));
                Assert.That(b, Is.EqualTo(a), $"frame after {20 * (round + 1)} s");
            }
        }

        [Test]
        public void ARejectedCommandComesBackAsAnEvent()
        {
            if (_library == null)
                Assert.Ignore("the simulation library is not built (tools/scripts/build_native_sim.sh)");
            using var native = NativeSession.CreateFermenter();
            native.Enqueue(SimCommands.Divide(5));   // there is no cell 5
            native.RunHeadless(0.1);
            Assert.That(native.TryDequeueEvent(out var ev), Is.True);
            Assert.That(ev, Is.InstanceOf<CommandRejected>());
            TestContext.WriteLine(((CommandRejected)ev!).Reason);
        }
    }
}
