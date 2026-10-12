using System;
using System.Runtime.InteropServices;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// One copy of a <see cref="NativeModule"/> in the library, integrated by libcuflynx's runtime (adaptive TR-BDF2,
    /// modified Newton with the exact Jacobian) one step of its owner's choosing at a time. Its values are exchanged
    /// with a <see cref="KernelBatch"/> (instance 0) of the module's descriptor, which the game reads and writes. Copies
    /// share nothing, so they can be stepped on different threads, with the same results on any number of them.
    /// </summary>
    public sealed unsafe class NativeInstance : IDisposable
    {
        private IntPtr _handle;
        private readonly double* _states, _variables;
        private readonly long[] _stats = new long[7];

        internal NativeInstance(NativeModule module, double relativeTolerance)
            : this(module, bk_instance_create_blocks(module.Blocks.Length, module.Blocks))
        {
            SetRelativeTolerance(relativeTolerance);
        }

        /// <summary>The relative tolerance of the local error of each step (TR-BDF2, exact Jacobian, no step limits).</summary>
        public void SetRelativeTolerance(double relativeTolerance) =>
            bk_set_options(bk_instance_sim(_handle), relativeTolerance, 1e-12, 0.0, 0.0, 0.01, 6, 50, 10_000_000, 0, 1);

        /// <summary>
        /// Steps end on each <see cref="Advance"/>'s end, and an input change there keeps the step size, Jacobian and
        /// factorisation (a soft restart): much faster when inputs change every macro step; off by default.
        /// </summary>
        public void SetStopAtOutputs(bool on) => bk_set_stop_at_outputs(bk_instance_sim(_handle), on ? 1 : 0);

        private NativeInstance(NativeModule module, IntPtr handle)
        {
            Module = module;
            _handle = handle;
            _states = bk_instance_states(handle);
            _variables = bk_instance_variables(handle);
        }

        public NativeModule Module { get; }

        /// <summary>A copy that continues exactly as this one would (a daughter cell).</summary>
        public NativeInstance Copy() => new NativeInstance(Module, bk_instance_copy(_handle));

        /// <summary>The instance's time [s].</summary>
        public double Time => bk_instance_time(_handle);

        /// <summary>Steps accepted and rejected, rate evaluations and Jacobians so far.</summary>
        public (long Steps, long Rejected, long RateEvaluations, long Jacobians) Statistics
        {
            get
            {
                bk_stats(bk_instance_sim(_handle), _stats);
                return (_stats[0], _stats[1], _stats[2], _stats[3] + _stats[4]);
            }
        }

        internal double Variable(int library) => _variables[library];

        /// <summary>
        /// Copies the batch's externals into the instance's parameters; when one changed, the computed constants are
        /// recomputed and the blocks that read it restart. Returns whether any changed.
        /// </summary>
        public bool SetInputs(KernelBatch batch)
        {
            bool changed = false;
            int[] map = Module.ExternalIndex;
            for (int e = 0; e < map.Length; e++)
            {
                double v = batch.Externals[e][0];
                double* at = _variables + map[e];
                if (*at != v)
                {
                    *at = v;
                    changed = true;
                }
            }
            if (changed)
                bk_instance_parameters_changed(_handle);
            return changed;
        }

        /// <summary>
        /// Initial values for the batch's externals: states that name a parameter (or a value computed from them) for
        /// their initial value take it now; the states go into the batch.
        /// </summary>
        public void InitialiseStates(KernelBatch batch)
        {
            SetInputs(batch);
            bk_instance_initialise_states(_handle);
            CopyStatesTo(batch);
        }

        /// <summary>Takes the batch's states and integrates afresh from them.</summary>
        public void SetStates(KernelBatch batch)
        {
            int[] map = Module.StateIndex;
            for (int i = 0; i < map.Length; i++)
                _states[map[i]] = batch.States[i][0];
            bk_instance_restart(_handle);
        }

        /// <summary>Advances by <paramref name="dt"/> [s] and writes the states into the batch.</summary>
        public void Advance(double dt, KernelBatch batch)
        {
            if (bk_instance_advance(_handle, dt) != 0)
                throw new InvalidOperationException($"{Module.Descriptor.Key}: the integration failed: {FailureReason()}");
            CopyStatesTo(batch);
        }

        private static readonly string[] Statuses = { "ok", "step too small", "too many steps", "non-finite values" };

        // Why and when the first failed block of this module stopped.
        private string FailureReason()
        {
            IntPtr sim = bk_instance_sim(_handle);
            for (int b = 0; b < Module.Blocks.Length; b++)   // the instance's own blocks, in order
            {
                int status = bk_block_status(sim, b, out double failT);
                if (status != 0)
                    return $"{(status < Statuses.Length ? Statuses[status] : "status " + status)} at t = {failT} s (block {Module.Blocks[b]})";
            }
            return $"at t = {Time} s";
        }

        /// <summary>Computes the library's outputs in this module and writes them into the batch's algebraic values.</summary>
        public void Outputs(KernelBatch batch)
        {
            bk_instance_outputs(_handle);
            foreach (var (kernel, library) in Module.Outputs)
                batch.Algebraic[kernel][0] = _variables[library];
        }

        /// <summary>Computes every algebraic value and writes them into the batch.</summary>
        public void ComputeVariables(KernelBatch batch)
        {
            bk_instance_compute_variables(_handle);
            int[] map = Module.AlgebraicIndex;
            for (int a = 0; a < map.Length; a++)
                batch.Algebraic[a][0] = _variables[map[a]];
        }

        private void CopyStatesTo(KernelBatch batch)
        {
            int[] map = Module.StateIndex;
            for (int i = 0; i < map.Length; i++)
                batch.States[i][0] = _states[map[i]];
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
                bk_instance_destroy(_handle);
            _handle = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        ~NativeInstance()
        {
            if (_handle != IntPtr.Zero)
                bk_instance_destroy(_handle);
        }

        private const string Library = NativeModule.Library;
        [DllImport(Library)] private static extern IntPtr bk_instance_create_blocks(int n, int[] blocks);
        [DllImport(Library)] private static extern IntPtr bk_instance_copy(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_destroy(IntPtr instance);
        [DllImport(Library)] private static extern double* bk_instance_states(IntPtr instance);
        [DllImport(Library)] private static extern double* bk_instance_variables(IntPtr instance);
        [DllImport(Library)] private static extern double bk_instance_time(IntPtr instance);
        [DllImport(Library)] private static extern IntPtr bk_instance_sim(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_restart(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_parameters_changed(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_initialise_states(IntPtr instance);
        [DllImport(Library)] private static extern int bk_instance_advance(IntPtr instance, double dt);
        [DllImport(Library)] private static extern void bk_instance_outputs(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_compute_variables(IntPtr instance);
        [DllImport(Library)] private static extern void bk_stats(IntPtr sim, long[] stats);
        [DllImport(Library)] private static extern int bk_block_status(IntPtr sim, int block, out double failT);
        [DllImport(Library)] private static extern void bk_set_stop_at_outputs(IntPtr sim, int on);
        [DllImport(Library)] private static extern void bk_set_options(IntPtr sim, double rtol, double atol, double hMax, double hMin,
            double newtonTol, int maxNewton, int maxJacobianAge, long maxSteps, int finiteDifferenceJacobian, int method);
    }
}
