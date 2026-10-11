using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Headless
{
    /// <summary>
    /// <c>AdaptOrDeity.Headless protocol obs_data.json cells out.csv [workers] [relativeTolerance]</c>: a colony of
    /// fermenter cells (each a separate cell, as libcuflynx builds the same colony from a module array) run through the
    /// protocol of a libcuflynx obs_data.json (first experiment: its sub-experiments in turn, each setting
    /// <c>params_to_change</c> named <c>cell_&lt;i&gt;/&lt;module input&gt;</c> before it runs) with the game's integrator on
    /// worker threads. Writes every state of every cell each second to <c>out.csv</c> and prints the integration's wall
    /// time as one JSON line. For tools/interop/test_solver_agreement.py.
    /// </summary>
    internal static class ProtocolRun
    {
        private const double OutputDt = 1.0;

        public static int Run(string[] args)
        {
            var obs = (Dictionary<string, object?>)Json.Parse(File.ReadAllText(args[0]))!;
            int cells = int.Parse(args[1], CultureInfo.InvariantCulture);
            string outPath = args[2];
            int workers = args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : ParallelWork.DefaultWorkers;
            double rtol = args.Length > 4 ? double.Parse(args[4], CultureInfo.InvariantCulture) : 1e-2;

            var protocol = (Dictionary<string, object?>)obs["protocol_info"]!;
            double preTime = Number(((List<object?>)protocol["pre_times"]!)[0]);
            var subTimes = (List<object?>)((List<object?>)protocol["sim_times"]!)[0]!;
            var changes = protocol.TryGetValue("params_to_change", out var p) && p is Dictionary<string, object?> c
                ? c : new Dictionary<string, object?>();

            var colony = new FermenterCell[cells];
            for (int i = 0; i < cells; i++)
                colony[i] = new FermenterCell(TraitCatalog.Fermenter) { RelativeTolerance = rtol };
            int n = colony[0].Batch.States.Length;

            using var pool = new ParallelWork(workers);
            Action<int, int> step = (start, end) =>
            {
                for (int i = start; i < end; i++)
                    colony[i].Step(OutputDt);
            };

            var rows = new List<double[]>();
            void Record(double t)
            {
                var row = new double[1 + cells * n];
                row[0] = t;
                for (int i = 0; i < cells; i++)
                    for (int s = 0; s < n; s++)
                        row[1 + i * n + s] = colony[i].Batch.States[s][0];
                rows.Add(row);
            }

            var sw = Stopwatch.StartNew();
            for (double t = 0; t < preTime - 1e-9; t += OutputDt)
                pool.Run(cells, 1, step);
            double time = 0.0;
            Record(time);
            for (int sub = 0; sub < subTimes.Count; sub++)
            {
                foreach (var entry in changes)
                {
                    var (cell, input) = Target(entry.Key);
                    double value = Number(((List<object?>)((List<object?>)entry.Value!)[0]!)[sub]);
                    if (cell < cells)
                        colony[cell].SetExternal(input, value);
                }
                long steps = (long)Math.Round(Number(subTimes[sub]) / OutputDt);
                for (long k = 0; k < steps; k++)
                {
                    pool.Run(cells, 1, step);
                    time += OutputDt;
                    Record(time);
                }
            }
            double wall = sw.Elapsed.TotalSeconds;

            var csv = new StringBuilder("time");
            for (int i = 0; i < cells; i++)
                foreach (var v in FermenterCell.Module.Descriptor.States)
                    csv.Append(",cell_").Append(i).Append('/').Append(v.Name);
            csv.Append('\n');
            foreach (var row in rows)
            {
                for (int k = 0; k < row.Length; k++)
                    csv.Append(k == 0 ? "" : ",").Append(row[k].ToString("R", CultureInfo.InvariantCulture));
                csv.Append('\n');
            }
            File.WriteAllText(outPath, csv.ToString());
            long accepted = 0;
            foreach (var cell in colony)
                accepted += cell.Statistics.Steps;
            Console.WriteLine(FormattableString.Invariant(
                $"{{\"wall_s\": {wall:R}, \"workers\": {workers}, \"cells\": {cells}, \"relative_tolerance\": {rtol:R}, \"accepted_steps\": {accepted}}}"));
            return 0;
        }

        private static (int cell, string input) Target(string name)
        {
            int slash = name.IndexOf('/');
            if (slash < 0 || !name.StartsWith("cell_", StringComparison.Ordinal))
                throw new FormatException($"params_to_change name '{name}' is not cell_<i>/<input>");
            return (int.Parse(name.Substring(5, slash - 5), CultureInfo.InvariantCulture), name.Substring(slash + 1));
        }

        private static double Number(object? value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}
