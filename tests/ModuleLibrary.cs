using System;
using System.IO;
using System.Runtime.InteropServices;
using AdaptOrDeity.Sim.Modules;
using NUnit.Framework;

/// <summary>
/// The game's modules run in the aod_modules library (tools/scripts/build_cell_library.py): every test assembly that
/// simulates them points the library's imports at the built file before its tests run, and fails at once, saying how to
/// build it, when it is missing.
/// </summary>
[SetUpFixture]
public class ModuleLibraryForTests
{
    [OneTimeSetUp]
    public void Resolve() => AdaptOrDeity.Tests.ModuleLibrary.Resolve();
}

namespace AdaptOrDeity.Tests
{
    /// <summary>Finds the aod_modules library built under src/AdaptOrDeity.Native/bin/cells.</summary>
    public static class ModuleLibrary
    {
        private static readonly object Gate = new object();
        private static bool _done;

        public static string? Path { get; private set; }

        public static void Resolve()
        {
            lock (Gate)
            {
                if (_done)
                    return;
                var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (dir != null && !File.Exists(System.IO.Path.Combine(dir.FullName, "AdaptOrDeity.sln")))
                    dir = dir.Parent;
                string file = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "aod_modules.dll"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libaod_modules.dylib" : "libaod_modules.so";
                string candidate = System.IO.Path.Combine(dir!.FullName, "src", "AdaptOrDeity.Native", "bin", "cells", file);
                if (!File.Exists(candidate))
                    Assert.Fail($"{candidate} is not built: run tools/scripts/build_cell_library.py --build (CMake and a C compiler)");
                Path = candidate;
                NativeLibrary.SetDllImportResolver(typeof(NativeModule).Assembly, (name, assembly, search) =>
                    name == "aod_modules" ? NativeLibrary.Load(candidate) : IntPtr.Zero);
                _done = true;
            }
        }
    }
}
