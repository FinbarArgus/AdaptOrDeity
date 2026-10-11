using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless kernel-bench</c>: the cost of one rate evaluation of the fermenter cell kernel per cell,
    /// for growing numbers of cells: one batch per cell (as cells are integrated today), all cells in one batch, and that
    /// batch split across threads. The basis for the parallelisation plan (docs/plans/parallel-and-gpu.md).
    /// </summary>
    internal static class KernelBench
    {
        public static int Run()
        {
            var kernel = new GenericAdaptiveCell_fermenter();
            var d = kernel.Descriptor;
            Console.WriteLine($"{d.Key}: {d.States.Count} states, {d.Externals.Count} externals, {d.Algebraic.Count} algebraic; " +
                              $"{Environment.ProcessorCount} hardware threads");
            Console.WriteLine("cells    per-cell batches   one batch   one batch, all threads   speed-up   (ns per cell per evaluation)");
            foreach (int n in new[] { 1, 16, 256, 4096, 16384, 65536 })
            {
                var single = new KernelBatch[n];
                for (int i = 0; i < n; i++)
                {
                    single[i] = new KernelBatch(d, 1) { Count = 1 };
                    single[i].ApplyInstance("default", 0, 1);
                    kernel.Initialise(single[i], 0, 1);
                }
                var batch = new KernelBatch(d, n) { Count = n };
                batch.ApplyInstance("default", 0, n);
                kernel.Initialise(batch, 0, n);

                double perCell = Time(n, () =>
                {
                    for (int i = 0; i < n; i++)
                        kernel.ComputeRates(1.0, single[i], 0, 1);
                });
                double batched = Time(n, () => kernel.ComputeRates(1.0, batch, 0, n));
                int chunks = Math.Max(1, Math.Min(Environment.ProcessorCount, n / 64));
                double threaded = Time(n, () => Parallel.For(0, chunks, c =>
                    kernel.ComputeRates(1.0, batch, n * c / chunks, n * (c + 1) / chunks)));
                Console.WriteLine($"{n,6} {perCell,17:F0} {batched,11:F0} {threaded,24:F0} {perCell / threaded,10:F1}x");
            }
            return 0;
        }

        // Nanoseconds per cell per call, from the best of several runs of enough calls to take ~0.2 s.
        private static double Time(int cells, Action call)
        {
            call();
            int reps = 1;
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < 0.05)
            {
                call();
                reps++;
            }
            reps = Math.Max(1, (int)(reps * 0.2 / sw.Elapsed.TotalSeconds));
            double best = double.MaxValue;
            for (int run = 0; run < 3; run++)
            {
                sw.Restart();
                for (int r = 0; r < reps; r++)
                    call();
                best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells);
            }
            return best;
        }
    }
}
