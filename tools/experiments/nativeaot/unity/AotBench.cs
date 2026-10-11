using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using UnityEditor;

// The same measurements as tools/experiments/nativeaot: managed, on Unity's Mono, and through the NativeAOT library.
public static class AotBench
{
    [DllImport("aod_sim")] static extern int aod_ping();
    [DllImport("aod_sim")] static extern double aod_kernel_rates_ns(int cells);
    [DllImport("aod_sim")] static extern double aod_colony_seconds(int cells, double simSeconds, int workers);

    public static void Run()
    {
        var log = new StringWriter();
        try
        {
            log.WriteLine($"Unity {UnityEngine.Application.unityVersion}, runtime {(Type.GetType("Mono.Runtime") != null ? "Mono" : "other")}");
            log.WriteLine($"native library loaded: {aod_ping() == 1}");
            foreach (int n in new[] { 256, 4096 })
                log.WriteLine($"kernel rates, {n} cells, 1 thread: Mono {KernelRatesNs(n):F0} ns, native {aod_kernel_rates_ns(n):F0} ns per cell");
            ColonySeconds(64, 60, 1); aod_colony_seconds(64, 60.0, 1);   // warm
            foreach (int w in new[] { 1, 4, 8 })
            {
                double mono = ColonySeconds(64, 60, w), native = aod_colony_seconds(64, 60.0, w);
                log.WriteLine($"colony 64 cells, 60 s, {w} workers: Mono {mono:F3} s ({60 / mono:F0}x), native {native:F3} s ({60 / native:F0}x) -> {mono / native:F1}x");
            }
        }
        catch (Exception e)
        {
            log.WriteLine("FAILED: " + e);
        }
        File.WriteAllText(Environment.GetEnvironmentVariable("AOT_BENCH_OUT") ?? "aot_bench.txt", log.ToString());
        EditorApplication.Exit(0);
    }

    static double KernelRatesNs(int cells)
    {
        var kernel = new GenericAdaptiveCell_fermenter();
        var batch = new KernelBatch(kernel.Descriptor, cells) { Count = cells };
        batch.ApplyInstance("default", 0, cells);
        kernel.Initialise(batch, 0, cells);
        for (int i = 0; i < 200; i++) kernel.ComputeRates(1.0, batch, 0, cells);
        double best = double.MaxValue;
        for (int run = 0; run < 5; run++)
        {
            var sw = Stopwatch.StartNew(); int reps = Math.Max(1, 2_000_000 / cells);
            for (int r = 0; r < reps; r++) kernel.ComputeRates(1.0, batch, 0, cells);
            best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e9 / reps / cells);
        }
        return best;
    }

    static double ColonySeconds(int cells, double simSeconds, int workers)
    {
        using var world = new FermenterWorld { Workers = workers };
        var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
        loop.Enqueue(SimCommands.SetLevel(2)); loop.ApplyCommands();
        while (world.CellCount < cells)
        {
            var parent = world.Cell(world.CellCount - 1);
            parent.SetState("bio__P_growth", parent.DaughterCost);
            loop.Enqueue(SimCommands.Divide(world.CellCount - 1)); loop.ApplyCommands();
        }
        world.BathConcentration = 20.0;
        loop.StepExactly(100);
        var sw = Stopwatch.StartNew();
        loop.StepExactly((long)Math.Round(simSeconds / world.MacroDt));
        return sw.Elapsed.TotalSeconds;
    }
}
