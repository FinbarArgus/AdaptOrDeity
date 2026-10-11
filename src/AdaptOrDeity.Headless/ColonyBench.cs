using System;
using System.Diagnostics;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Sim.Core;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless colony-bench [cells] [simSeconds]</c>: a colony of fermenter cells (fed, motors running and
    /// pausing) stepped on 1, 2, 4, 8 and the default number of worker threads: real-time speed and speed-up.
    /// </summary>
    internal static class ColonyBench
    {
        public static int Run(string[] args)
        {
            int cells = args.Length > 0 ? int.Parse(args[0]) : FermenterWorld.CellLimit;
            double seconds = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 60.0;
            Console.WriteLine($"{cells} cells, {seconds} simulated s, {Environment.ProcessorCount} hardware threads");
            Console.WriteLine("workers   wall s   x real time   speed-up");
            double one = 0;
            Time(cells, 1, 2 * seconds);   // throwaway: lets the JIT recompile the kernel before anything is timed
            foreach (int workers in new[] { 1, 2, 4, 8, ParallelWork.DefaultWorkers })
            {
                double wall = Time(cells, workers, seconds);
                if (workers == 1)
                    one = wall;
                Console.WriteLine($"{workers,7} {wall,8:F2} {seconds / wall,13:F0} {one / wall,10:F1}x");
            }
            return 0;
        }

        // Wall time for the same simulated stretch (after the same 10 s settling) with this many workers.
        private static double Time(int cells, int workers, double seconds)
        {
            {
                var world = new FermenterWorld { Workers = workers };
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
                loop.StepExactly((long)Math.Round(seconds / world.MacroDt));
                double wall = sw.Elapsed.TotalSeconds;
                world.Dispose();
                return wall;
            }
        }
    }
}
