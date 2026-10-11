using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>
    /// One cell integrated by libcuflynx's generated C (the <c>aod_cell</c> library, tools/scripts/build_cell_library.py;
    /// docs/plans/libcuflynx-solver.md): an instance of the whole cell model with its own states and parameters,
    /// advanced by its owner one world step at a time with its own adaptive TR-BDF2 steps. Instances share nothing, so
    /// cells on different threads step independently and bit-identically.
    /// </summary>
    public sealed unsafe class NativeCell : IDisposable
    {
        private const string Library = "aod_cell";
        private IntPtr _handle;
        private readonly double* _states, _variables;

        /// <summary>Indices into the library's states and variables of the C# kernel's states and externals.</summary>
        public NativeCellMap Map { get; }

        private NativeCell(IntPtr handle, NativeCellMap map)
        {
            _handle = handle;
            Map = map;
            _states = bk_instance_states(handle);
            _variables = bk_instance_variables(handle);
        }

        /// <summary>A new instance for cells of <paramref name="descriptor"/>'s kernel.</summary>
        public static NativeCell Create(KernelDescriptor descriptor, double relativeTolerance)
        {
            var map = NativeCellMap.For(descriptor);
            var cell = new NativeCell(bk_instance_create(), map);
            // TR-BDF2 (method 1), modified Newton with the exact Jacobian; no step limits.
            bk_set_options(bk_instance_sim(cell._handle), relativeTolerance, 1e-12, 0.0, 0.0, 0.01, 6, 50, 10_000_000, 0, 1);
            return cell;
        }

        /// <summary>A copy that continues exactly as this one would (a daughter).</summary>
        public NativeCell Copy() => new NativeCell(bk_instance_copy(_handle), Map);

        private static readonly Dictionary<KernelDescriptor, string> Unavailable = new Dictionary<KernelDescriptor, string>();

        /// <summary>The library is present and holds this kernel's variables (the answer is kept).</summary>
        public static bool Available(KernelDescriptor descriptor, out string why)
        {
            lock (Unavailable)
                if (Unavailable.TryGetValue(descriptor, out why!))
                    return false;
            try
            {
                NativeCellMap.For(descriptor);
                why = "";
                return true;
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException || e is InvalidOperationException)
            {
                why = e.Message;
                lock (Unavailable)
                    Unavailable[descriptor] = why;
                return false;
            }
        }

        /// <summary>
        /// Copies the kernel batch's externals (instance 0) into the instance's parameters; recomputes the computed
        /// constants when one changed. Returns whether any changed.
        /// </summary>
        public bool SetInputs(KernelBatch batch)
        {
            bool changed = false;
            int[] map = Map.Externals;
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

        /// <summary>Takes the batch's states (instance 0) and integrates afresh from them.</summary>
        public void SetStates(KernelBatch batch)
        {
            int[] map = Map.States;
            for (int i = 0; i < map.Length; i++)
                _states[map[i]] = batch.States[i][0];
            bk_instance_restart(_handle);
        }

        /// <summary>Advances by <paramref name="dt"/> and writes the states into the batch (instance 0).</summary>
        public void Advance(double dt, KernelBatch batch)
        {
            int failed = bk_instance_advance(_handle, dt);
            if (failed != 0)
                throw new InvalidOperationException($"the cell's integration failed at t = {bk_instance_time(_handle)} s");
            int[] map = Map.States;
            for (int i = 0; i < map.Length; i++)
                batch.States[i][0] = _states[map[i]];
        }

        /// <summary>Computes the library's outputs and writes them into the batch's algebraic values (instance 0).</summary>
        public void Outputs(KernelBatch batch)
        {
            bk_instance_outputs(_handle);
            var outputs = Map.Outputs;
            for (int o = 0; o < outputs.Length; o++)
                batch.Algebraic[outputs[o].Kernel][0] = _variables[outputs[o].Library];
        }

        /// <summary>Computes every algebraic value and writes them into the batch (instance 0).</summary>
        public void ComputeVariables(KernelBatch batch)
        {
            bk_instance_compute_variables(_handle);
            int[] map = Map.Algebraic;
            for (int a = 0; a < map.Length; a++)
                batch.Algebraic[a][0] = _variables[map[a]];
        }

        public double Time => bk_instance_time(_handle);

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
                bk_instance_destroy(_handle);
            _handle = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        ~NativeCell()
        {
            if (_handle != IntPtr.Zero)
                bk_instance_destroy(_handle);
        }

        [DllImport(Library)] private static extern IntPtr bk_instance_create();
        [DllImport(Library)] private static extern IntPtr bk_instance_copy(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_destroy(IntPtr instance);
        [DllImport(Library)] private static extern double* bk_instance_states(IntPtr instance);
        [DllImport(Library)] private static extern double* bk_instance_variables(IntPtr instance);
        [DllImport(Library)] private static extern double bk_instance_time(IntPtr instance);
        [DllImport(Library)] private static extern IntPtr bk_instance_sim(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_restart(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_parameters_changed(IntPtr instance);
        [DllImport(Library)] private static extern int bk_instance_advance(IntPtr instance, double dt);
        [DllImport(Library)] private static extern void bk_instance_outputs(IntPtr instance);
        [DllImport(Library)] private static extern void bk_instance_compute_variables(IntPtr instance);
        [DllImport(Library)] internal static extern int bk_output_count();
        [DllImport(Library)] internal static extern int bk_output_index(int i);
        [DllImport(Library)] private static extern void bk_set_options(IntPtr sim, double rtol, double atol, double hMax, double hMin,
            double newtonTol, int maxNewton, int maxJacobianAge, long maxSteps, int finiteDifferenceJacobian, int method);
        [DllImport(Library)] internal static extern int bk_state_count();
        [DllImport(Library)] internal static extern int bk_variable_count();
        [DllImport(Library)] internal static extern void bk_state_info(int i, out IntPtr name, out IntPtr units, out IntPtr component);
        [DllImport(Library)] internal static extern int bk_variable_info(int i, out IntPtr name, out IntPtr units, out IntPtr component);
    }

    /// <summary>
    /// Where a C# kernel's states, externals and algebraic values are in the cell library: states and algebraic values
    /// (computed constants included) by name; each external is the module array parameter <c>&lt;name&gt;_cell_0</c>
    /// (libcuflynx names a module's parameters after its instance). Outputs: the library's, with their kernel indices.
    /// </summary>
    public sealed class NativeCellMap
    {
        private static readonly Dictionary<KernelDescriptor, NativeCellMap> Maps = new Dictionary<KernelDescriptor, NativeCellMap>();

        public int[] States { get; }

        public int[] Externals { get; }

        public int[] Algebraic { get; }

        public (int Kernel, int Library)[] Outputs { get; }

        private NativeCellMap(int[] states, int[] externals, int[] algebraic, (int, int)[] outputs)
        {
            States = states;
            Externals = externals;
            Algebraic = algebraic;
            Outputs = outputs;
        }

        public static NativeCellMap For(KernelDescriptor d)
        {
            lock (Maps)
            {
                if (Maps.TryGetValue(d, out var known))
                    return known;
                var states = new Dictionary<string, int>();
                for (int i = 0; i < NativeCell.bk_state_count(); i++)
                {
                    NativeCell.bk_state_info(i, out var name, out _, out _);
                    states[Marshal.PtrToStringAnsi(name)!] = i;
                }
                var variables = new Dictionary<string, int>();
                for (int i = 0; i < NativeCell.bk_variable_count(); i++)
                {
                    NativeCell.bk_variable_info(i, out var name, out _, out _);
                    variables[Marshal.PtrToStringAnsi(name)!] = i;
                }
                var s = new int[d.States.Count];
                for (int i = 0; i < s.Length; i++)
                    s[i] = states.TryGetValue(d.States[i].Name, out int k) ? k
                        : throw new InvalidOperationException($"the cell library has no state {d.States[i].Name}");
                if (s.Length != states.Count)
                    throw new InvalidOperationException($"the cell library has {states.Count} states, the kernel {s.Length}");
                var e = new int[d.Externals.Count];
                for (int i = 0; i < e.Length; i++)
                    e[i] = variables.TryGetValue(d.Externals[i].Name + "_cell_0", out int k) ? k
                        : throw new InvalidOperationException($"the cell library has no parameter {d.Externals[i].Name}_cell_0");
                var a = new int[d.Algebraic.Count];
                for (int i = 0; i < a.Length; i++)
                    a[i] = variables.TryGetValue(d.Algebraic[i].Name, out int k) ? k
                        : throw new InvalidOperationException($"the cell library has no variable {d.Algebraic[i].Name}");
                var kernelIndex = new Dictionary<int, int>();
                for (int i = 0; i < a.Length; i++)
                    kernelIndex[a[i]] = i;
                var o = new (int, int)[NativeCell.bk_output_count()];
                for (int i = 0; i < o.Length; i++)
                {
                    int library = NativeCell.bk_output_index(i);
                    o[i] = (kernelIndex[library], library);
                }
                var map = new NativeCellMap(s, e, a, o);
                Maps[d] = map;
                return map;
            }
        }
    }
}
