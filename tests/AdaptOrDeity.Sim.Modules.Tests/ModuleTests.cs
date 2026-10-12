using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdaptOrDeity.Sim.Modules;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Modules.Tests
{
    /// <summary>
    /// The game's modules in the aod_modules library (libcuflynx's generated C and its integrator) against their golden
    /// traces from Myokit's CVODE (aod-kernelgen --goldens), and properties every module must have.
    /// </summary>
    public class ModuleTests
    {
        public static IEnumerable<TestCaseData> GoldenCases()
        {
            AdaptOrDeity.Tests.ModuleLibrary.Resolve();
            foreach (var module in NativeModule.All())
            {
                var d = module.Descriptor;
                string dir = Path.Combine(GoldenTrace.Dir(), d.ModuleType, d.Version);
                foreach (var instance in d.Instances.Keys)
                    yield return new TestCaseData(module, instance, Path.Combine(dir, instance + ".csv"))
                        .SetName($"Golden({d.Key}, {instance})");
            }
        }

        [TestCaseSource(nameof(GoldenCases))]
        public void MatchesGoldenTrace(NativeModule module, string instance, string goldenPath)
        {
            Assert.That(File.Exists(goldenPath), Is.True, $"missing golden {goldenPath}; run aod-kernelgen --goldens");
            var golden = GoldenTrace.Load(goldenPath);
            double[] times = golden.Column("time");
            using var meta = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(goldenPath, ".meta.json")));
            var replay = meta.RootElement.GetProperty("replay");
            double tolerance = replay.GetProperty("nrmse_tolerance").GetDouble();
            // "states": compare states only (whole cells: derived ratios such as mass-action quotients are ill-conditioned).
            bool statesOnly = replay.TryGetProperty("compare", out var cmp) && cmp.GetString() == "states";

            var batch = NewBatch(module, instance, out var native);
            const double rtol = 1e-9;
            native.SetRelativeTolerance(rtol);   // the module, not the tolerance, is under test
            var actual = golden.Columns.Skip(1).ToDictionary(c => c, _ => new double[times.Length]);
            double t = 0.0;
            for (int row = 0; row < times.Length; row++)
            {
                if (times[row] > t)
                {
                    native.Advance(times[row] - t, batch);
                    t = times[row];
                }
                native.ComputeVariables(batch);
                foreach (var name in actual.Keys)
                    actual[name][row] = Read(batch, name);
            }

            var d = module.Descriptor;
            foreach (var pair in actual)
            {
                if (statesOnly && d.StateIndex(pair.Key) < 0)
                    continue;
                double atol = meta.RootElement.TryGetProperty("atol", out var at) ? at.GetDouble() : 1e-300;
                double nrmse = statesOnly
                    ? GoldenTrace.Nrmse(golden.Column(pair.Key), pair.Value, atol, rtol)
                    : GoldenTrace.Nrmse(golden.Column(pair.Key), pair.Value);
                TestContext.WriteLine($"{d.Key} {instance} {pair.Key}: NRMSE {nrmse:E2}");
                Assert.That(nrmse, Is.LessThan(tolerance), $"{d.Key} {pair.Key}");
            }
            native.Dispose();
        }

        [Test]
        public void EveryModuleIsAllowlisted()
        {
            foreach (var module in NativeModule.All())
                Assert.That(module.Descriptor.Licence, Is.EqualTo("CC0-1.0"), module.Descriptor.Key);
        }

        [Test]
        public void DiffusionVolumeReachesAnalyticMichaelisMentenSteadyState()
        {
            // With J_in = J_out = 0: dC/dt = flux_c/V + M C/(C + C50) = 0 gives C* = C50 f / (1 - f),
            // f = (flux_c/V)/(-M). Choosing f = 1/2 puts the steady state at C50 with time constant ~4 C50/|M| ≈ 1 s.
            var module = NativeModule.Get("tissue_diffusion_volume", "nn");
            var d = module.Descriptor;
            var batch = NewBatch(module, "default", out var native);
            double m = batch.Externals[d.ExternalIndex("M")][0];
            double v = batch.Externals[d.ExternalIndex("V_P")][0];
            double c50 = batch.Externals[d.ExternalIndex("C50")][0];
            batch.Externals[d.ExternalIndex("flux_c")][0] = 0.5 * -m * v;
            native.SetRelativeTolerance(1e-10);
            native.SetInputs(batch);
            for (int i = 0; i < 600; i++)
                native.Advance(0.1, batch);
            Assert.That(batch.States[d.StateIndex("C_P")][0], Is.EqualTo(c50).Within(1e-9));
            native.Dispose();
        }

        [Test]
        public void InstancesAreIndependent()
        {
            // Three instances with different parameters stepped in turn must equal three separate runs, bit for bit.
            var module = NativeModule.Get("tissue_diffusion_volume", "nn");
            int mIndex = module.Descriptor.ExternalIndex("M");
            double[] ms = { -0.0558, -0.02, -0.1 };
            var together = ms.Select(_ => NewBatch(module, "default", out var native) is var b ? (b, native) : default).ToArray();
            for (int k = 0; k < ms.Length; k++)
            {
                together[k].b.Externals[mIndex][0] = ms[k];
                together[k].native.SetInputs(together[k].b);
            }
            for (int i = 0; i < 1000; i++)
                foreach (var (b, native) in together)
                    native.Advance(0.1, b);
            for (int k = 0; k < ms.Length; k++)
            {
                var single = NewBatch(module, "default", out var alone);
                single.Externals[mIndex][0] = ms[k];
                alone.SetInputs(single);
                for (int i = 0; i < 1000; i++)
                    alone.Advance(0.1, single);
                Assert.That(together[k].b.States[0][0], Is.EqualTo(single.States[0][0]));
            }
        }

        private static KernelBatch NewBatch(NativeModule module, string instance, out NativeInstance native)
        {
            var batch = new KernelBatch(module.Descriptor, 1) { Count = 1 };
            batch.ApplyInstance(instance, 0, 1);
            native = module.CreateInstance(1e-6);
            native.InitialiseStates(batch);
            native.ComputeVariables(batch);
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
