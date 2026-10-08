using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AdaptOrDeity.Sim.Kernels.Tests
{
    /// <summary>A golden trace written by tools/kernelgen (Myokit/CVODE): a time column plus named columns.</summary>
    internal sealed class GoldenTrace
    {
        private GoldenTrace(string[] columns, double[][] rows)
        {
            Columns = columns;
            Rows = rows;
        }

        public string[] Columns { get; }

        public double[][] Rows { get; }

        public double[] Column(string name)
        {
            int c = Array.IndexOf(Columns, name);
            if (c < 0)
                throw new ArgumentException($"golden has no column {name}");
            return Rows.Select(r => r[c]).ToArray();
        }

        public static GoldenTrace Load(string path)
        {
            var lines = File.ReadAllLines(path).Where(l => l.Length > 0).ToArray();
            var columns = lines[0].Split(',');
            var rows = lines.Skip(1)
                .Select(l => l.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray())
                .ToArray();
            return new GoldenTrace(columns, rows);
        }

        public static string Dir()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AdaptOrDeity.sln")))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException("AdaptOrDeity.sln not found above test directory");
            return Path.Combine(dir.FullName, "tests", "golden");
        }

        /// <summary>Root-mean-square error normalised by the golden's range (or max |value| if flat).</summary>
        /// <param name="floor">Smallest scale: the golden's absolute tolerance, so a quantity that stays at zero compares
        /// round-off with round-off instead of dividing by nothing.</param>
        public static double Nrmse(IReadOnlyList<double> expected, IReadOnlyList<double> actual, double floor = 1e-300)
        {
            double sum = 0.0, min = double.MaxValue, max = double.MinValue, maxAbs = 0.0;
            for (int i = 0; i < expected.Count; i++)
            {
                double d = actual[i] - expected[i];
                sum += d * d;
                min = Math.Min(min, expected[i]);
                max = Math.Max(max, expected[i]);
                maxAbs = Math.Max(maxAbs, Math.Abs(expected[i]));
            }
            double scale = Math.Max(max - min > 1e-12 * maxAbs ? max - min : maxAbs, floor);
            return Math.Sqrt(sum / expected.Count) / scale;
        }
    }
}
