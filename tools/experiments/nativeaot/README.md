# The simulation as a NativeAOT library (prototype)

Measured on the C# kernels and integrator the game used before it moved to libcuflynx's generated C
(docs/plans/libcuflynx-solver.md); they were removed after commit 739a347, so check that commit out to rerun these.

The simulation libraries compiled ahead of time with .NET into one shared library with C entry points
(`Exports.cs`): what Unity would load as a native plugin, keeping drawing and UI on Unity (decision 2026-10-11,
docs/plans/parallel-and-gpu.md). Not part of the build.

```bash
dotnet publish -c Release -r linux-x64 -o publish \
    -p:CppCompilerAndLinker=<clang> -p:ObjCopyName=<llvm-objcopy>     # needs clang (e.g. libcellml/perf/llvm-env)
python3 call.py publish/aod_sim.so                                      # any C caller: ctypes here
```

Inside Unity: a throwaway project with the managed DLLs (`tools/scripts/build_sim_for_unity.sh` output) and
`aod_sim.so` in `Assets/Plugins`, and `unity/AotBench.cs` in `Assets/Editor`, run in batch mode:
`Unity -batchmode -nographics -projectPath <project> -executeMethod AotBench.Run` (writes `aot_bench.txt`).

Measured 2026-10-11 (i7-12700H, Unity 6000.3.25f1): the library loads beside Mono; the kernel takes 297-302 ns per
cell per rate evaluation natively against 532-543 ns on Mono; a 64-cell colony runs 1.5x faster natively on one
worker and ~10x faster on 4-8 workers, because Mono runs our worker pool slower than one thread (1.06 s on 4 workers
against 0.43 s on 1). The native library matches `dotnet run` (JIT) speed.
