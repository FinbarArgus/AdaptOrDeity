using System;
using System.Diagnostics;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless field-bench [cells...]</c>: the environment field's cost per 0.1 s macro step for colonies of
    /// these sizes (default 1, 64, 512, 5000) on 1 and the default number of worker threads: fitting the grids, every
    /// cell's exchange of glucose, lactate and acid, and the step (O2 and the traces idle, as without food).
    /// </summary>
    internal static class FieldBench
    {
        public static int Run(string[] args)
        {
            int[] sizes = args.Length > 0 ? Array.ConvertAll(args, a => int.Parse(a, CultureInfo.InvariantCulture))
                : new[] { 1, 64, 512, 5000 };
            Console.WriteLine("cells  grid N x levels  workers  ms/step  us/cell/step");
            foreach (int cells in sizes)
                foreach (int workers in new[] { 1, ParallelWork.DefaultWorkers })
                {
                    double ms = Time(cells, workers, out int n, out int levels);
                    Console.WriteLine(FormattableString.Invariant(
                        $"{cells,5}  {n,6} x {levels}  {workers,9}  {ms,7:F3}  {1e3 * ms / cells,12:F2}"));
                }
            return 0;
        }

        private static double Time(int cells, int workers, out int n, out int levels)
        {
            const double dt = 0.1, radius = 1e-6;
            double spacing = 2.0 * radius + GameParameters.Default.Value("colony.cell_gap");
            var x = new double[cells];
            var y = new double[cells];
            double r = 0.0;
            for (int i = 0; i < cells; i++)
            {
                ColonyLayout.Slot(i, spacing, out x[i], out y[i]);
                r = Math.Max(r, Math.Sqrt(x[i] * x[i] + y[i] * y[i]));
            }
            double diameter = 2.0 * (r + radius);
            var env = new EnvironmentField(GameParameters.Default.Environment(), 0.1, 5e-5, 0.0, 0.0, diameter);
            using var pool = workers > 1 ? new ParallelWork(workers) : null;
            int steps = cells >= 5000 ? 50 : cells >= 512 ? 200 : 1000;
            Stopwatch? sw = null;
            for (int s = -Math.Min(1000, 4 * steps); s < steps; s++)   // the first steps also let the JIT optimise
            {
                if (s == 0)
                    sw = Stopwatch.StartNew();
                double drift = s * 0.2e-6;   // the creature drifts with the current: the grids recentre now and then
                env.Follow(drift, 0.0, diameter);
                for (int i = 0; i < cells; i++)
                {
                    double cx = x[i] + drift;
                    double c = env.AtMembrane(EnvironmentField.Glucose, cx, y[i], radius, -4e-19);
                    env.AtMembrane(EnvironmentField.Lactate, cx, y[i], radius, 8e-19);
                    env.Protons(env.AtMembrane(EnvironmentField.Acid, cx, y[i], radius, 8e-19));
                    env.Exchange(EnvironmentField.Glucose, cx, y[i], -4e-19 * c / (c + 0.01));
                    env.Exchange(EnvironmentField.Lactate, cx, y[i], 8e-19);
                    env.Exchange(EnvironmentField.Acid, cx, y[i], 8e-19);
                }
                env.Step(dt, pool);
            }
            n = env.Field.CellsPerSide;
            levels = env.Field.Levels;
            return sw!.Elapsed.TotalMilliseconds / steps;
        }
    }
}
