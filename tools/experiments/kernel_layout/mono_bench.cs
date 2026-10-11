using System; using System.Diagnostics; using System.Threading.Tasks;
using AdaptOrDeity.Sim.Kernels.Generated; using AdaptOrDeity.Sim.Modules;
public static class MonoBench
{
    public static void Main()
    {
        var kernel = new GenericAdaptiveCell_fermenter(); var d = kernel.Descriptor;
        Console.WriteLine("runtime: " + (Type.GetType("Mono.Runtime") != null ? "Mono" : "CoreCLR") + ", " + Environment.Version);
        Console.WriteLine("cells   per-cell   full batch   lean batch   lean, threads   (ns per cell per evaluation)");
        foreach (int n in new[] { 256, 4096, 65536 })
        {
            var single = new KernelBatch[Math.Min(n, 4096)];
            for (int i = 0; i < single.Length; i++) { single[i] = new KernelBatch(d, 1) { Count = 1 }; single[i].ApplyInstance("default", 0, 1); kernel.Initialise(single[i], 0, 1); }
            var b = new KernelBatch(d, n) { Count = n }; b.ApplyInstance("default", 0, n); kernel.Initialise(b, 0, n);
            int chunks = Math.Min(Environment.ProcessorCount, n / 64);
            double per = T(single.Length, () => { for (int i = 0; i < single.Length; i++) kernel.ComputeRates(1.0, single[i], 0, 1); });
            double full = T(n, () => kernel.ComputeRates(1.0, b, 0, n));
            double lean = T(n, () => LeanFermenter.ComputeRates(1.0, b, 0, n));
            double leanP = T(n, () => Parallel.For(0, chunks, k => LeanFermenter.ComputeRates(1.0, b, n * k / chunks, n * (k + 1) / chunks)));
            Console.WriteLine(string.Format("{0,6} {1,9:F0} {2,12:F0} {3,12:F0} {4,15:F0}", n, per, full, lean, leanP));
        }
    }
    static double T(int cells, Action call) { call(); int reps = 1; var sw = Stopwatch.StartNew(); while (sw.Elapsed.TotalSeconds < 0.05) { call(); reps++; }
        reps = Math.Max(1, (int)(reps * 0.3 / sw.Elapsed.TotalSeconds)); double best = double.MaxValue;
        for (int r = 0; r < 3; r++) { sw.Restart(); for (int i = 0; i < reps; i++) call(); best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells); } return best; }
}
