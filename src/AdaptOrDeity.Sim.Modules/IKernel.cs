namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// A CellML module compiled to C#. Kernels are stateless; all data lives in a <see cref="KernelBatch"/>
    /// and each call processes instances [start, end).
    /// </summary>
    public interface IKernel
    {
        KernelDescriptor Descriptor { get; }

        /// <summary>Sets states to their initial values (externals must already be filled).</summary>
        void Initialise(KernelBatch batch, int start, int end);

        /// <summary>Computes algebraic variables and state rates at time <paramref name="t"/> [s].</summary>
        void ComputeRates(double t, KernelBatch batch, int start, int end);

        /// <summary>Computes algebraic variables only (e.g. after a step, for output).</summary>
        void ComputeVariables(double t, KernelBatch batch, int start, int end);
    }
}
