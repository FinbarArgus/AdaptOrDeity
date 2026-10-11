using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Kernels.Tests
{
    /// <summary>ComputeOutputs gives each output exactly as ComputeVariables does, off the initial state too.</summary>
    public class OutputKernelTests
    {
        private static IEnumerable<TestCaseData> Cases()
        {
            foreach (var kernel in GeneratedKernels.All())
                if (kernel is IOutputKernel o && o.Outputs.Count > 0)
                    foreach (double offset in new[] { 0.0, 0.2 })
                        yield return new TestCaseData(kernel, offset).SetName($"{kernel.Descriptor.Key} {offset:P0} off the initial state");
        }

        [TestCaseSource(nameof(Cases))]
        public void OutputsMatchComputeVariables(IKernel kernel, double offset)
        {
            var d = kernel.Descriptor;
            var full = new KernelBatch(d, 1) { Count = 1 };
            var outputs = new KernelBatch(d, 1) { Count = 1 };
            foreach (var batch in new[] { full, outputs })
            {
                batch.ApplyInstance("default", 0, 1);
                kernel.Initialise(batch, 0, 1);
                for (int i = 0; i < d.States.Count; i++)
                    batch.States[i][0] *= 1.0 + (i % 2 == 0 ? offset : -offset);
            }
            kernel.ComputeVariables(1.0, full, 0, 1);
            ((IOutputKernel)kernel).ComputeOutputs(1.0, outputs, 0, 1);
            foreach (int i in ((IOutputKernel)kernel).Outputs)
                Assert.That(BitConverter.DoubleToInt64Bits(outputs.Algebraic[i][0]),
                    Is.EqualTo(BitConverter.DoubleToInt64Bits(full.Algebraic[i][0])), d.Algebraic[i].Name);
        }
    }
}
