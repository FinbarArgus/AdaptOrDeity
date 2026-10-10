using System;
using System.Diagnostics;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless bench [simSeconds]</c>: the cost and accuracy of integrating one fermenter cell in the
    /// game's scenario (fed and growing, the motor running 2 s in every 6, 0.1 s game step) for each scheme: rate
    /// evaluations per simulated second, wall time, the real-time factor on one core, the speed-up over the original
    /// scheme, and the error against an accurate solution (5 ms steps), as the largest difference over the run relative
    /// to each state's size.
    /// </summary>
    internal static class Bench
    {
        private sealed class Scheme
        {
            public string Name = "";
            public bool Reuse = true, Adaptive;
            public double Tolerance = 1e-3;
            public int Substeps = 1;
        }

        private sealed class Result
        {
            public double[][] Trace = Array.Empty<double[]>();
            public double Wall;
            public long Rates;
            public FermenterCell Cell = null!;
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
            var accurate = Simulate(new Scheme { Name = "accurate (kept Jacobian, 5 ms steps)", Substeps = 20 }, seconds, swimming);
            var schemes = new[]
            {
                new Scheme { Name = "Newton, Jacobian every iteration (before)", Reuse = false },
                new Scheme { Name = "Modified Newton, Jacobian kept (step 1)" },
                new Scheme { Name = "+ adaptive steps, tolerance 1e-2 (step 2)", Adaptive = true, Tolerance = 1e-2 },
                new Scheme { Name = "+ adaptive steps, tolerance 3e-3 (step 2)", Adaptive = true, Tolerance = 3e-3 },
                new Scheme { Name = "+ adaptive steps, tolerance 1e-3 (step 2)", Adaptive = true, Tolerance = 1e-3 },
            };
            Console.WriteLine($"{"scheme",-44} {"rates/sim s",12} {"wall s",8} {"x real time",12} {"speed-up",9} {"max error",10}  worst state");
            double baseWall = 0.0;
            foreach (var s in schemes)
            {
                var r = Simulate(s, seconds, swimming);
                if (baseWall == 0.0)
                    baseWall = r.Wall;
                double error = Error(r, accurate, out string worst);
                Console.WriteLine(FormattableString.Invariant(
                    $"{s.Name,-44} {r.Rates / seconds,12:F0} {r.Wall,8:F3} {seconds / r.Wall,12:F0} {baseWall / r.Wall,8:F1}x {error,10:E1}  {worst}"));
                if (s.Adaptive)
                    Console.WriteLine(FormattableString.Invariant(
                        $"{"",4}{r.Cell.AcceptedSteps} adaptive steps ({seconds / r.Cell.AcceptedSteps * 1e3:F0} ms mean), {r.Cell.RejectedSteps} rejected"));
            }
        }

        private static Result Simulate(Scheme s, double seconds, bool swimming)
        {
            var cell = new FermenterCell(new GenericAdaptiveCell_fermenter(), TraitCatalog.Fermenter);
            cell.Integrator.ReuseJacobian = s.Reuse;
            cell.Adaptive = s.Adaptive;
            cell.RelativeTolerance = s.Tolerance;
            cell.SetExternal("C_glc_e_mod_membrane_face", 50.0);
            cell.GrowthFraction = 50.0;
            int steps = (int)Math.Round(seconds / 0.1), n = cell.Batch.States.Length;
            var trace = new double[steps][];
            long rates0 = cell.Integrator.RateEvaluations;
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < steps; i++)
            {
                cell.SetExternal("a_motor_mod_i_flagellum", swimming && i % 60 < 20 ? 1.0 : 0.0);
                for (int j = 0; j < s.Substeps; j++)
                    cell.Step(0.1 / s.Substeps);
                trace[i] = new double[n];
                for (int k = 0; k < n; k++)
                    trace[i][k] = cell.Batch.States[k][0];
            }
            return new Result { Trace = trace, Wall = watch.Elapsed.TotalSeconds, Rates = cell.Integrator.RateEvaluations - rates0, Cell = cell };
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
            worst = r.Cell.Batch.Descriptor.States[arg].Name;
            return error;
        }
    }
}
