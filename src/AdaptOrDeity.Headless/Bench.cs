using System;
using System.Diagnostics;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless bench [simSeconds]</c>: the cost and accuracy of integrating one fermenter cell in the
    /// game's scenario (fed and growing, the motor running 2 s in every 6, 0.1 s game step) at several tolerances of the
    /// cells' integrator (libcuflynx's TR-BDF2): steps and rate evaluations per simulated second, wall time, the
    /// real-time factor on one core, and the error against a tight solution (rtol 1e-9), as the largest difference over
    /// the run relative to each state's size.
    /// </summary>
    internal static class Bench
    {
        private sealed class Result
        {
            public double[][] Trace = Array.Empty<double[]>();
            public double Wall;
            public (long Steps, long Rejected, long RateEvaluations, long Jacobians) Work;
        }

        public static int Run(string[] args)
        {
            double seconds = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 600.0;
            Scenario(seconds, true, "busy: fed (50 mM), growth 50x, motor 2 s on / 4 s off");
            Console.WriteLine();
            Scenario(seconds, false, "calm: fed (50 mM), growth 50x, motor paused (inputs steady)");
            return 0;
        }

        private static void Scenario(double seconds, bool swimming, string label)
        {
            Console.WriteLine(FormattableString.Invariant($"fermenter cell, {seconds:F0} simulated s, {label}, 0.1 s game step"));
            var accurate = Simulate(1e-9, seconds, swimming);
            Console.WriteLine($"{"relative tolerance",-20} {"steps/sim s",12} {"rates/sim s",12} {"wall s",8} {"x real time",12} {"max error",10}  worst state");
            foreach (double rtol in new[] { 1e-2, 1e-3, 1e-4, 1e-6 })
            {
                var r = Simulate(rtol, seconds, swimming);
                double error = Error(r, accurate, out string worst);
                Console.WriteLine(FormattableString.Invariant(
                    $"{rtol,-20:g} {r.Work.Steps / seconds,12:F1} {r.Work.RateEvaluations / seconds,12:F1} {r.Wall,8:F3} {seconds / r.Wall,12:F0} {error,10:E1}  {worst}"));
            }
        }

        private static Result Simulate(double rtol, double seconds, bool swimming)
        {
            var cell = new FermenterCell(TraitCatalog.Fermenter) { RelativeTolerance = rtol };
            cell.MembraneGlucose = 50.0;
            cell.GrowthFraction = 50.0;
            int steps = (int)Math.Round(seconds / 0.1), n = cell.Batch.States.Length;
            var trace = new double[steps][];
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < steps; i++)
            {
                cell.SetExternal("a_motor_mod_i_flagellum", swimming && i % 60 < 20 ? 1.0 : 0.0);
                cell.Step(0.1);
                trace[i] = new double[n];
                for (int k = 0; k < n; k++)
                    trace[i][k] = cell.Batch.States[k][0];
            }
            return new Result { Trace = trace, Wall = watch.Elapsed.TotalSeconds, Work = cell.Statistics };
        }

        private static double Error(Result r, Result reference, out string worst)
        {
            int n = reference.Trace[0].Length;
            var size = new double[n];
            foreach (var row in reference.Trace)
                for (int k = 0; k < n; k++)
                    size[k] = Math.Max(size[k], Math.Abs(row[k]));
            double error = 0.0;
            int arg = 0;
            for (int i = 0; i < reference.Trace.Length; i++)
                for (int k = 0; k < n; k++)
                {
                    double e = Math.Abs(r.Trace[i][k] - reference.Trace[i][k]) / (size[k] + 1e-30);
                    if (e > error)
                    {
                        error = e;
                        arg = k;
                    }
                }
            worst = FermenterCell.Module.Descriptor.States[arg].Name;
            return error;
        }
    }
}
