using System;
using System.IO;
using System.Runtime.InteropServices;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;

/// <summary>
/// Tests run cells on the C# integrator unless they ask for the native library (FermenterCell.NativeIntegration),
/// whatever ran before them; the game itself uses the library when it loads.
/// </summary>
[SetUpFixture]
public class CellsOnTheCSharpIntegratorByDefault
{
    [OneTimeSetUp]
    public void Before() => FermenterCell.NativeIntegration = false;
}

namespace AdaptOrDeity.Tests
{
    /// <summary>CI (AOD_REQUIRE_NATIVE=1) fails a test whose native library is missing, rather than skipping it.</summary>
    public static class NativeRequirement
    {
        public static void RequireNative(string what)
        {
            if (Environment.GetEnvironmentVariable("AOD_REQUIRE_NATIVE") == "1")
                Assert.Fail($"{what} is not built, and AOD_REQUIRE_NATIVE=1");
        }
    }

    /// <summary>Finds the cell library built by tools/scripts/build_cell_library.py, for tests.</summary>
    public static class CellLibrary
    {
        private static readonly object Gate = new object();
        private static bool _done;

        public static string? Path { get; private set; }

        /// <summary>Points the <c>aod_cell</c> imports at the built library; false when it has not been built.</summary>
        public static bool Resolve()
        {
            lock (Gate)
            {
                if (_done)
                    return Path != null;
                _done = true;
                var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (dir != null && !File.Exists(System.IO.Path.Combine(dir.FullName, "AdaptOrDeity.sln")))
                    dir = dir.Parent;
                string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aod_cell.dll"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libaod_cell.dylib" : "libaod_cell.so";
                string candidate = System.IO.Path.Combine(dir!.FullName, "src", "AdaptOrDeity.Native", "bin", "cells", file);
                if (!File.Exists(candidate))
                {
                    NativeRequirement.RequireNative(candidate);
                    return false;
                }
                Path = candidate;
                NativeLibrary.SetDllImportResolver(typeof(NativeCell).Assembly, (name, assembly, search) =>
                    name == "aod_cell" ? NativeLibrary.Load(candidate) : IntPtr.Zero);
                return true;
            }
        }
    }

}
