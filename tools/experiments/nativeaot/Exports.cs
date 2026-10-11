using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Native
{
    /// <summary>C entry points of the simulation library: the same measurements as the headless kernel-bench and
    /// colony-bench, so the native build compares directly with the JIT and with Unity's Mono.</summary>
    public static class Exports
    {
        /// <summary>1 when the library loaded and its runtime started.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_ping")]
        public static int Ping() => 1;

        /// <summary>Nanoseconds per cell per evaluation of the fermenter's rates, all cells in one batch, one thread.</summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_kernel_rates_ns")]
        public static double KernelRatesNs(int cells) => Safe(() => Bench.KernelRatesNs(cells));

        /// <summary>
        /// Wall seconds for a colony of <paramref name="cells"/> fermenter cells (fed, motors running and pausing) over
        /// <paramref name="simSeconds"/> on <paramref name="workers"/> threads, after the same settling as colony-bench.
        /// </summary>
        [UnmanagedCallersOnly(EntryPoint = "aod_colony_seconds")]
        public static double ColonySeconds(int cells, double simSeconds, int workers) =>
            Safe(() => Bench.ColonySeconds(cells, simSeconds, workers));

        // Exceptions cannot cross the C boundary: report them as NaN.
        private static double Safe(Func<double> f)
        {
            try
            {
                return f();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return double.NaN;
            }
        }
    }

    /// <summary>The measurements, as plain C# (the Unity Mono comparison calls these directly).</summary>
    public static class Bench
    {
        public static double KernelRatesNs(int cells)
        {
            var kernel = new GenericAdaptiveCell_fermenter();
            var batch = new KernelBatch(kernel.Descriptor, cells) { Count = cells };
            batch.ApplyInstance("default", 0, cells);
            kernel.Initialise(batch, 0, cells);
            for (int i = 0; i < 200; i++)
                kernel.ComputeRates(1.0, batch, 0, cells);
            double best = double.MaxValue;
            for (int run = 0; run < 5; run++)
            {
                var sw = Stopwatch.StartNew();
                int reps = Math.Max(1, 2_000_000 / cells);
                for (int r = 0; r < reps; r++)
                    kernel.ComputeRates(1.0, batch, 0, cells);
                best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells);
            }
            return best;
        }

        public static double ColonySeconds(int cells, double simSeconds, int workers)
        {
            using var world = new FermenterWorld { Workers = workers };
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            while (world.CellCount < cells)
            {
                var parent = world.Cell(world.CellCount - 1);
                parent.SetState("bio__P_growth", parent.DaughterCost);
                loop.Enqueue(SimCommands.Divide(world.CellCount - 1));
                loop.ApplyCommands();
            }
            world.BathConcentration = 20.0;
            loop.StepExactly(100);
            var sw = Stopwatch.StartNew();
            loop.StepExactly((long)Math.Round(simSeconds / world.MacroDt));
            return sw.Elapsed.TotalSeconds;
        }
    }
}
