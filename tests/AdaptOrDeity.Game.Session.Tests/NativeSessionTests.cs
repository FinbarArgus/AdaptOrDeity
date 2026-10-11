using System;
using System.IO;
using System.Linq;
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
            // tools/scripts/build_native_sim.sh publishes to bin/publish/<runtime identifier>
            string publish = Path.Combine(dir!.FullName, "src", "AdaptOrDeity.Native", "bin", "publish");
            string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aod_sim.dll"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "aod_sim.dylib" : "aod_sim.so";
            string? candidate = Directory.Exists(publish)
                ? Directory.EnumerateFiles(publish, file, SearchOption.AllDirectories).FirstOrDefault() : null;
            if (candidate == null)
            {
                if (Environment.GetEnvironmentVariable("AOD_REQUIRE_NATIVE") == "1")
                    Assert.Fail($"{file} is not built under {publish}, and AOD_REQUIRE_NATIVE=1");
                return;
            }
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

        private static void Grow(ISessionFront session, int cells)
        {
            session.Enqueue(SimCommands.SetLevel(2));
            for (int i = 1; i < cells; i++)
            {
                session.Enqueue(SimCommands.DevCompleteDaughter(i - 1));
                session.Enqueue(SimCommands.Divide(i - 1));
            }
        }

        [Test]
        public void FramesGrowPastTheFirstCapacityInBothSessions()
        {
            if (_library == null)
                Assert.Ignore("the simulation library is not built (tools/scripts/build_native_sim.sh)");
            using var managed = SimSession.CreateFermenter();
            using var native = NativeSession.CreateFermenter();
            Grow(managed, 100);
            Grow(native, 100);
            var a = managed.RunHeadless(5.0);
            var b = native.RunHeadless(5.0);
            Assert.That(a.CellCount, Is.EqualTo(100));
            Assert.That(b.CellX.Length, Is.GreaterThanOrEqualTo(100));
            Assert.That(Bytes(b), Is.EqualTo(Bytes(a)));
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
