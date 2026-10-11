using System; using System.Diagnostics; using System.Threading.Tasks;
using AdaptOrDeity.Sim.Kernels.Generated; using AdaptOrDeity.Sim.Modules;
if (args.Length > 0) { Dump.Write(args[0]); return; }
var kernel = new GenericAdaptiveCell_fermenter(); var d = kernel.Descriptor;
Console.WriteLine("cells   full batch   lean batch   full, threads   lean, threads   (ns per cell per evaluation)");
foreach (int n in new[] { 256, 4096, 16384, 65536 })
{
    var b = new KernelBatch(d, n) { Count = n }; b.ApplyInstance("default", 0, n); kernel.Initialise(b, 0, n);
    var c = new KernelBatch(d, n) { Count = n }; c.ApplyInstance("default", 0, n); kernel.Initialise(c, 0, n);
    kernel.ComputeRates(1.0, b, 0, n); LeanFermenter.ComputeRates(1.0, c, 0, n);
    double maxRel = 0; for (int i = 0; i < d.States.Count; i++) { double x = b.Rates[i][0], y = c.Rates[i][0]; maxRel = Math.Max(maxRel, Math.Abs(x - y) / Math.Max(1e-300, Math.Abs(x))); }
    int chunks = Math.Min(Environment.ProcessorCount, n / 64);
    double full = T(n, () => kernel.ComputeRates(1.0, b, 0, n)), lean = T(n, () => LeanFermenter.ComputeRates(1.0, c, 0, n));
    double fullP = T(n, () => Parallel.For(0, chunks, k => kernel.ComputeRates(1.0, b, n * k / chunks, n * (k + 1) / chunks)));
    double leanP = T(n, () => Parallel.For(0, chunks, k => LeanFermenter.ComputeRates(1.0, c, n * k / chunks, n * (k + 1) / chunks)));
    Console.WriteLine($"{n,6} {full,11:F0} {lean,12:F0} {fullP,15:F0} {leanP,15:F0}    rates agree to {maxRel:E1}");
}
static double T(int cells, Action call) { call(); int reps = 1; var sw = Stopwatch.StartNew(); while (sw.Elapsed.TotalSeconds < 0.05) { call(); reps++; }
    reps = Math.Max(1, (int)(reps * 0.3 / sw.Elapsed.TotalSeconds)); double best = double.MaxValue;
    for (int r = 0; r < 3; r++) { sw.Restart(); for (int i = 0; i < reps; i++) call(); best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells); } return best; }
