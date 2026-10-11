using System.Collections.Generic;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// A kernel that can compute a few algebraic variables (its outputs, set in content/modules.lock.json) without the
    /// rest: what a caller reads every step, while <see cref="IKernel.ComputeVariables"/> fills in everything on demand.
    /// </summary>
    public interface IOutputKernel : IKernel
    {
        /// <summary>Algebraic indices that <see cref="ComputeOutputs"/> keeps current.</summary>
        IReadOnlyList<int> Outputs { get; }

        /// <summary>Computes the outputs (and the algebraic variables they read) for instances [start, end).</summary>
        void ComputeOutputs(double t, KernelBatch batch, int start, int end);
    }
}
