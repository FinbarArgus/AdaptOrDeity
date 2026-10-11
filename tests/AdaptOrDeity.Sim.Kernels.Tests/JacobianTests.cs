using System;
using System.Collections.Generic;
using System.Linq;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Kernels.Tests
{
    /// <summary>
    /// Each generated kernel's exact Jacobian agrees with central finite differences of its rates, at the initial state
    /// and at states moved off it, entry by entry (docs/plans/symbolic-jacobians.md).
    /// </summary>
    public class JacobianTests
    {
        private static IEnumerable<TestCaseData> Cases()
        {
            foreach (var kernel in GeneratedKernels.All())
                foreach (string instance in kernel.Descriptor.Instances.Keys)
                    foreach (double offset in new[] { 0.0, 0.05, 0.2 })
                        yield return new TestCaseData(kernel, instance, offset)
                            .SetName($"{kernel.Descriptor.Key} {instance} {offset:P0} off the initial state");
        }

        [TestCaseSource(nameof(Cases))]
        public void ExactJacobianMatchesFiniteDifferences(IKernel kernel, string instance, double offset)
        {
            Assert.That(kernel, Is.InstanceOf<IJacobianKernel>());
            var d = kernel.Descriptor;
            int n = d.States.Count;
            var batch = new KernelBatch(d, 1) { Count = 1 };
            batch.ApplyInstance(instance, 0, 1);
            kernel.Initialise(batch, 0, 1);
            for (int i = 0; i < n; i++)   // alternate up and down, so the state is off any steady state
                batch.States[i][0] *= 1.0 + (i % 2 == 0 ? offset : -offset);

            var exact = new double[n, n];
            ((IJacobianKernel)kernel).ComputeJacobian(1.0, batch, 0, exact);

            var f0 = Rates(kernel, batch, n);
            double[] rowScale = new double[n];
            var fd = new double[n, n];
            for (int j = 0; j < n; j++)
            {
                double y = batch.States[j][0];
                double h = 1e-6 * Math.Max(Math.Abs(y), 1e-3);   // a state at zero still gets a step above round-off
                batch.States[j][0] = y + h;
                var up = Rates(kernel, batch, n);
                batch.States[j][0] = y - h;
                var down = Rates(kernel, batch, n);
                batch.States[j][0] = y;
                for (int i = 0; i < n; i++)
                {
                    fd[i, j] = (up[i] - down[i]) / (2 * h);
                    // the size of each rate's sensitivity: a column's share of the row is what an entry is judged by
                    rowScale[i] = Math.Max(rowScale[i], Math.Abs(fd[i, j]) * Math.Max(Math.Abs(y), 1e-3));
                }
            }
            var failures = new List<string>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    // finite differences carry ~1e-8 relative error in the entry and round-off near cancelling terms,
                    // judged against the row's largest contribution; a wrong derivative is off by far more
                    double yj = Math.Max(Math.Abs(batch.States[j][0]), 1e-3);
                    double tolerance = 1e-5 * Math.Abs(fd[i, j]) + 1e-8 * rowScale[i] / yj + 1e-12;
                    if (!(Math.Abs(exact[i, j] - fd[i, j]) <= tolerance))
                        failures.Add($"d(d{d.States[i].Name}/dt)/d{d.States[j].Name}: exact {exact[i, j]:G6}, " +
                                     $"finite differences {fd[i, j]:G6}");
                }
            Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(20)));
        }

        private static double[] Rates(IKernel kernel, KernelBatch batch, int n)
        {
            kernel.ComputeRates(1.0, batch, 0, 1);
            var r = new double[n];
            for (int i = 0; i < n; i++)
                r[i] = batch.Rates[i][0];
            return r;
        }
    }
}
