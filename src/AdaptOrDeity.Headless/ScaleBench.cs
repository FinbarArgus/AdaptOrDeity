using System;
using System.Diagnostics;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless scale-bench [cells ...]</c>: how the simulation scales with the number of cells (default 64,
    /// 256, 1024, 5000), on 1 worker and the default number:
    /// the game world (colony grown by division, fed, motors running and pausing: food, body and cells) per simulated
    /// second, the cost and size of one snapshot frame, and the cells' integration alone (the 300 s food protocol of
    /// tools/interop, every cell fed or none).
    /// </summary>
    internal static class ScaleBench
    {
        public static int Run(string[] args)
        {
            var sizes = args.Length > 0 ? Array.ConvertAll(args, a => int.Parse(a, CultureInfo.InvariantCulture))
                                        : new[] { 64, 256, 1024, 5000 };
            int many = ParallelWork.DefaultWorkers;
            Console.WriteLine($"{Environment.ProcessorCount} hardware threads; default workers {many}");
            Console.WriteLine("cells  world 1w (ms/sim s)  world {0}w  frame (ms, MB)  cells busy 1w / {0}w (ms/sim s)  cells calm 1w / {0}w", many);
            Cells(64, true, 1);   // warm the JIT
            foreach (int n in sizes)
            {
                double w1 = World(n, 1, out _, out _), wm = World(n, many, out double frameMs, out double frameMb);
                double b1 = Cells(n, true, 1), bm = Cells(n, true, many), c1 = Cells(n, false, 1), cm = Cells(n, false, many);
                Console.WriteLine(FormattableString.Invariant(
                    $"{n,5} {w1,20:F1} {wm,10:F1} {frameMs,9:F2} {frameMb,5:F1} {b1,14:F1} / {bm,-14:F1} {c1,10:F2} / {cm:F2}"));
            }
            return 0;
        }

        // Milliseconds of wall time per simulated second for the game world with n cells.
        private static double World(int n, int workers, out double frameMs, out double frameMb)
        {
            using var world = new FermenterWorld(n) { Workers = workers };
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            while (world.CellCount < n)
            {
                int parent = world.CellCount - 1;
                var p = world.Cell(parent);
                p.SetState("bio__P_growth", p.DaughterCost);
                loop.Enqueue(SimCommands.Divide(parent));
                loop.ApplyCommands();
            }
            world.BathConcentration = 20.0;
            loop.StepExactly(50);
            const double seconds = 20.0;
            var sw = Stopwatch.StartNew();
            loop.StepExactly((long)Math.Round(seconds / world.MacroDt));
            double perSecond = sw.Elapsed.TotalMilliseconds / seconds;
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            sw.Restart();
            for (int i = 0; i < 10; i++)
                loop.WriteSnapshot(frame);
            frameMs = sw.Elapsed.TotalMilliseconds / 10;
            frameMb = SnapshotCodec.Size(frame) / 1e6;
            return perSecond;
        }

        // Milliseconds of wall time per simulated second for n independent cells through the food protocol.
        private static double Cells(int n, bool fed, int workers)
        {
            var kernel = new GenericAdaptiveCell_fermenter();
            var cells = new FermenterCell[n];
            for (int i = 0; i < n; i++)
                cells[i] = new FermenterCell(kernel, TraitCatalog.Fermenter);
            using var pool = new ParallelWork(workers);
            Action<int, int> step = (start, end) =>
            {
                for (int i = start; i < end; i++)
                    cells[i].Step(1.0);
            };
            var sw = Stopwatch.StartNew();
            foreach (var (seconds, glucose) in new[] { (60, 0.1), (120, fed ? 50.0 : 0.1), (120, 0.1) })
            {
                foreach (var c in cells)
                    c.SetExternal("C_glc_e_mod_membrane_face", glucose);
                for (int s = 0; s < seconds; s++)
                    pool.Run(n, Math.Max(1, n / (8 * workers)), step);
            }
            return sw.Elapsed.TotalMilliseconds / 300.0;
        }
    }
}
