# Kernel layout and GPU measurements

Measurements behind [docs/plans/parallel-and-gpu.md](../../../docs/plans/parallel-and-gpu.md). Not part of the build.

```bash
cd tools/experiments/kernel_layout
python3 make_lean.py ../../../src/AdaptOrDeity.Sim.Kernels.Generated/Kernels/GenericAdaptiveCell_fermenter.g.cs > LeanFermenter.cs
dotnet run -c Release                      # current vs lean layout, one thread and all threads
dotnet run -c Release -- cell.txt          # one cell 5% off steady state, with its C# rates, for the GPU test
python3 -m venv .venv && .venv/bin/pip install pyopencl numpy
.venv/bin/python gpu_rates.py LeanFermenter.cs cell.txt   # OpenCL, double and float, against the C# rates

# four cells per AVX2 register (.NET 9+ for vectorised Exp/Log; simd/global.json asks for SDK 10)
python3 make_simd.py LeanFermenter.cs > simd/SimdFermenter.cs && (cd simd && dotnet run -c Release)

# the same kernels on Unity's Mono runtime (MB = <Unity editor>/Data/MonoBleedingEdge)
$MB/bin/mono $MB/lib/mono/4.5/csc.exe -optimize+ -langversion:latest -nullable:enable -out:mono_bench.exe \
    ../../../src/AdaptOrDeity.Sim.Modules/*.cs ../../../src/AdaptOrDeity.Sim.Kernels.Generated/Kernels/*.g.cs \
    LeanFermenter.cs mono_bench.cs && $MB/bin/mono mono_bench.exe
```
