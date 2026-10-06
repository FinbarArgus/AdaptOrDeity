using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Kernels.Tests
{
    public class GeneratedKernelTests
    {
        private const double GoldenNrmseTolerance = 1e-6;

        public static IEnumerable<TestCaseData> GoldenCases()
        {
            foreach (var kernel in GeneratedKernels.All())
            {
                var d = kernel.Descriptor;
                string dir = Path.Combine(GoldenTrace.Dir(), d.ModuleType, d.Version);
                foreach (var instance in d.Instances.Keys)
                    yield return new TestCaseData(kernel, instance, Path.Combine(dir, instance + ".csv"))
                        .SetName($"Golden({d.Key}, {instance})");
            }
        }

        [TestCaseSource(nameof(GoldenCases))]
        public void MatchesGoldenTrace(IKernel kernel, string instance, string goldenPath)
        {
            Assert.That(File.Exists(goldenPath), Is.True, $"missing golden {goldenPath}; run aod-kernelgen --goldens");
            var golden = GoldenTrace.Load(goldenPath);
            double[] times = golden.Column("time");

            var batch = NewBatch(kernel, instance, 1);
            const double dt = 0.01;
            var rk4 = new Rk4Integrator();
            var actual = golden.Columns.Skip(1).ToDictionary(c => c, _ => new double[times.Length]);

            double t = 0.0;
            for (int row = 0; row < times.Length; row++)
            {
                while (t < times[row] - 1e-9)
                {
                    double h = Math.Min(dt, times[row] - t);
                    rk4.Step(kernel, batch, t, h, 0, 1);
                    t += h;
                }
                kernel.ComputeVariables(t, batch, 0, 1);
                foreach (var name in actual.Keys)
                    actual[name][row] = Read(batch, name);
            }

            foreach (var pair in actual)
            {
                double nrmse = GoldenTrace.Nrmse(golden.Column(pair.Key), pair.Value);
                TestContext.WriteLine($"{kernel.Descriptor.Key} {instance} {pair.Key}: NRMSE {nrmse:E2}");
                Assert.That(nrmse, Is.LessThan(GoldenNrmseTolerance), $"{kernel.Descriptor.Key} {pair.Key}");
            }
        }

        [Test]
        public void EveryKernelIsAllowlistedAndPortsResolve()
        {
            foreach (var kernel in GeneratedKernels.All())
            {
                var d = kernel.Descriptor;
                Assert.That(d.Licence, Is.EqualTo("CC0-1.0"), d.Key);
                foreach (var port in d.Ports)
                    foreach (var v in port.Variables)
                        Assert.That(d.StateIndex(v) >= 0 || d.ExternalIndex(v) >= 0 || d.AlgebraicIndex(v) >= 0,
                            Is.True, $"{d.Key} port {port.PortType} variable {v}");
            }
        }

        [Test]
        public void DiffusionVolumeReachesAnalyticMichaelisMentenSteadyState()
        {
            // With J_in = J_out = 0: dC/dt = flux_c/V + M C/(C + C50) = 0 gives C* = C50 f / (1 - f),
            // f = (flux_c/V)/(-M). Choosing f = 1/2 puts the steady state at C50 with time constant ~4 C50/|M| ≈ 1 s.
            var kernel = new TissueDiffusionVolume_nn();
            var d = kernel.Descriptor;
            var batch = NewBatch(kernel, "default", 1);
            double m = batch.Externals[d.ExternalIndex("M")][0];
            double v = batch.Externals[d.ExternalIndex("V_P")][0];
            double c50 = batch.Externals[d.ExternalIndex("C50")][0];
            batch.Externals[d.ExternalIndex("flux_c")][0] = 0.5 * -m * v;

            var rk4 = new Rk4Integrator();
            for (int i = 0; i < 6000; i++)
                rk4.Step(kernel, batch, i * 0.01, 0.01, 0, 1);

            Assert.That(batch.States[d.StateIndex("C_P")][0], Is.EqualTo(c50).Within(1e-9));
        }

        [Test]
        public void BatchedInstancesAreIndependent()
        {
            // Three instances with different parameters stepped together must equal three separate runs, bit for bit.
            var kernel = new TissueDiffusionVolume_nn();
            int mIndex = kernel.Descriptor.ExternalIndex("M");
            double[] ms = { -0.0558, -0.02, -0.1 };

            var batch = NewBatch(kernel, "default", ms.Length);
            for (int k = 0; k < ms.Length; k++)
                batch.Externals[mIndex][k] = ms[k];
            Run(kernel, batch, ms.Length);

            for (int k = 0; k < ms.Length; k++)
            {
                var single = NewBatch(kernel, "default", 1);
                single.Externals[mIndex][0] = ms[k];
                Run(kernel, single, 1);
                Assert.That(batch.States[0][k], Is.EqualTo(single.States[0][0]));
            }
        }

        private static void Run(IKernel kernel, KernelBatch batch, int count)
        {
            var rk4 = new Rk4Integrator();
            for (int i = 0; i < 1000; i++)
                rk4.Step(kernel, batch, i * 0.1, 0.1, 0, count);
        }

        private static KernelBatch NewBatch(IKernel kernel, string instance, int count)
        {
            var batch = new KernelBatch(kernel.Descriptor, count) { Count = count };
            batch.ApplyInstance(instance, 0, count);
            kernel.Initialise(batch, 0, count);
            kernel.ComputeVariables(0.0, batch, 0, count);
            return batch;
        }

        private static double Read(KernelBatch batch, string name)
        {
            var d = batch.Descriptor;
            int i = d.StateIndex(name);
            if (i >= 0)
                return batch.States[i][0];
            i = d.AlgebraicIndex(name);
            if (i >= 0)
                return batch.Algebraic[i][0];
            throw new ArgumentException($"{d.Key} has no state/algebraic {name}");
        }
    }
}
