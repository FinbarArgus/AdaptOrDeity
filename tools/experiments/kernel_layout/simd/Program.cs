using System; using System.Diagnostics; using System.Threading.Tasks; using System.Runtime.Intrinsics;
using AdaptOrDeity.Sim.Kernels.Generated; using AdaptOrDeity.Sim.Modules;

// The lean kernel on one core (scalar), four cells per AVX2 register (SIMD), and SIMD on all threads, .NET 10.
var kernel = new GenericAdaptiveCell_fermenter(); var d = kernel.Descriptor;
Console.WriteLine($".NET {Environment.Version}, Vector256 accelerated: {Vector256.IsHardwareAccelerated}, {Environment.ProcessorCount} threads");
Console.WriteLine("cells   lean scalar   lean SIMD   SIMD, all threads   (ns per cell per evaluation)");
foreach (int pass in new[] { 1, 2 })
foreach (int n in new[] { 256, 4096, 65536 })
{
    var a = Batch(n); var b = Batch(n);
    LeanFermenter.ComputeRates(1.0, a, 0, n); SimdFermenter.ComputeRates(1.0, b, 0, n);
    double maxRel = 0;
    for (int i = 0; i < d.States.Count; i++)
        for (int k = 0; k < n; k += 37)
            maxRel = Math.Max(maxRel, Math.Abs(a.Rates[i][k] - b.Rates[i][k]) / Math.Max(Math.Abs(a.Rates[i][k]), 1e-30));
    int chunks = Math.Min(Environment.ProcessorCount, n / 64);
    double scalar = T(n, () => LeanFermenter.ComputeRates(1.0, a, 0, n));
    double simd = T(n, () => SimdFermenter.ComputeRates(1.0, b, 0, n));
    double simdP = T(n, () => Parallel.For(0, chunks, c => SimdFermenter.ComputeRates(1.0, b, n * c / chunks / 4 * 4, n * (c + 1) / chunks / 4 * 4)));
    if (pass == 2) Console.WriteLine($"{n,6} {scalar,13:F0} {simd,11:F0} {simdP,19:F0}    SIMD rates agree with scalar to {maxRel:E1}");
}

KernelBatch Batch(int n)
{
    var b = new KernelBatch(d, n) { Count = n }; b.ApplyInstance("default", 0, n); kernel.Initialise(b, 0, n);
    for (int i = 0; i < b.States.Length; i++)        // away from steady state, different for each cell
        for (int k = 0; k < n; k++) b.States[i][k] *= 1.0 + 0.05 * Math.Sin(i + 0.1 * k);
    return b;
}
static double T(int cells, Action call) { call(); int reps = 1; var sw = Stopwatch.StartNew(); while (sw.Elapsed.TotalSeconds < 0.05) { call(); reps++; }
    reps = Math.Max(1, (int)(reps * 0.3 / sw.Elapsed.TotalSeconds)); double best = double.MaxValue;
    for (int r = 0; r < 3; r++) { sw.Restart(); for (int i = 0; i < reps; i++) call(); best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells); } return best; }
