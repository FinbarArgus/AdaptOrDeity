using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// One of the game's CellML modules in the <c>aod_modules</c> library: libcuflynx's generated C for every module of
    /// content/modules.lock.json (tools/scripts/build_cell_library.py; docs/plans/libcuflynx-solver.md). The library holds
    /// one module array with an instance of each module (cell_0, vol_0, ...): this is one of them, described as a
    /// <see cref="KernelDescriptor"/> built from the library itself. States and algebraic values are those of component
    /// <c>&lt;name&gt;_module</c>; externals are its parameters, <c>&lt;external&gt;_&lt;name&gt;</c>.
    /// </summary>
    public sealed class NativeModule
    {
        internal const string Library = "aod_modules";

        private static readonly Dictionary<string, NativeModule> Loaded = new Dictionary<string, NativeModule>();
        private static string? _unavailable;

        private NativeModule(int index)
        {
            Name = Text(aod_module_name(index));
            string component = Name + "_module", suffix = "_" + Name;

            var states = new List<VariableInfo>();
            var stateIndex = new List<int>();
            var blocks = new SortedSet<int>();
            for (int i = 0; i < bk_state_count(); i++)
            {
                bk_state_info(i, out var n, out var u, out var c);
                if (Text(c) != component)
                    continue;
                states.Add(new VariableInfo(Text(n), Text(u), 1.0, VariableRole.Variable));
                stateIndex.Add(i);
                blocks.Add(bk_state_block(i));
            }
            var externals = new List<VariableInfo>();
            var externalIndex = new List<int>();
            var algebraic = new List<VariableInfo>();
            var algebraicIndex = new List<int>();
            for (int i = 0; i < bk_variable_count(); i++)
            {
                int kind = bk_variable_info(i, out var n, out var u, out var c);
                string name = Text(n), owner = Text(c);
                if (kind == ConstantKind && owner == "parameters" && name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    externals.Add(new VariableInfo(name.Substring(0, name.Length - suffix.Length), Text(u), 1.0, VariableRole.Constant));
                    externalIndex.Add(i);
                }
                else if ((kind == ComputedConstantKind || kind == AlgebraicKind) && owner == component)
                {
                    algebraic.Add(new VariableInfo(name, Text(u), 1.0, VariableRole.Variable));
                    algebraicIndex.Add(i);
                }
            }
            StateIndex = stateIndex.ToArray();
            ExternalIndex = externalIndex.ToArray();
            AlgebraicIndex = algebraicIndex.ToArray();
            Blocks = new int[blocks.Count];
            blocks.CopyTo(Blocks);

            var kernelIndex = new Dictionary<int, int>();
            for (int a = 0; a < AlgebraicIndex.Length; a++)
                kernelIndex[AlgebraicIndex[a]] = a;
            var outputs = new List<(int, int)>();
            for (int o = 0; o < bk_output_count(); o++)
                if (kernelIndex.TryGetValue(bk_output_index(o), out int a))
                    outputs.Add((a, bk_output_index(o)));
            Outputs = outputs.ToArray();

            // named parameter sets: the library's values (its default instance) with each set's own values over them
            var defaults = new double[ExternalIndex.Length];
            using (var probe = new NativeInstance(this, 1e-4))
                for (int e = 0; e < defaults.Length; e++)
                    defaults[e] = probe.Variable(ExternalIndex[e]);
            var instances = new Dictionary<string, IReadOnlyDictionary<string, double>>();
            for (int s = 0; s < aod_instance_count(index); s++)
            {
                var values = new Dictionary<string, double>();
                for (int e = 0; e < externals.Count; e++)
                    values[externals[e].Name] = defaults[e];
                for (int k = 0; k < aod_instance_param_count(index, s); k++)
                    values[Text(aod_instance_param_name(index, s, k))] = aod_instance_param_value(index, s, k);
                instances[Text(aod_instance_name(index, s))] = values;
            }
            var none = new int[states.Count];
            for (int i = 0; i < none.Length; i++)
                none[i] = -1;
            Descriptor = new KernelDescriptor(Text(aod_module_type(index)), Text(aod_module_version(index)), component,
                Text(aod_module_licence(index)), Text(aod_module_sha256(index)), states.ToArray(), externals.ToArray(),
                algebraic.ToArray(), none, new double[states.Count], new PortInfo[0], instances);
        }

        /// <summary>The module's name in the library's module array (cell_0, vol_0).</summary>
        public string Name { get; }

        /// <summary>The module described for the game: states, externals (its parameters), algebraic values, parameter sets.</summary>
        public KernelDescriptor Descriptor { get; }

        /// <summary>Per descriptor state, external and algebraic value: its index in the library's arrays.</summary>
        internal int[] StateIndex { get; }

        internal int[] ExternalIndex { get; }

        internal int[] AlgebraicIndex { get; }

        /// <summary>The library's blocks that hold this module's states (an instance integrates only these).</summary>
        internal int[] Blocks { get; }

        /// <summary>The library's outputs in this module: (algebraic index in the descriptor, library variable).</summary>
        public (int Kernel, int Library)[] Outputs { get; }

        /// <summary>A new copy of the module, integrated with TR-BDF2 at <paramref name="relativeTolerance"/>.</summary>
        public NativeInstance CreateInstance(double relativeTolerance = 1e-4) => new NativeInstance(this, relativeTolerance);

        /// <summary>Every module in the library.</summary>
        public static IReadOnlyList<NativeModule> All()
        {
            var all = new List<NativeModule>();
            int count;
            try
            {
                count = aod_module_count();
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                throw new InvalidOperationException($"the {Library} library is not available ({e.Message})", e);
            }
            for (int m = 0; m < count; m++)
                all.Add(Get(Text(aod_module_type(m)), Text(aod_module_version(m))));
            return all;
        }

        /// <summary>The module of this type and version, or an exception saying why the library cannot give it.</summary>
        public static NativeModule Get(string moduleType, string version)
        {
            lock (Loaded)
            {
                string key = moduleType + "@" + version;
                if (Loaded.TryGetValue(key, out var known))
                    return known;
                if (_unavailable != null)
                    throw new InvalidOperationException(_unavailable);
                try
                {
                    for (int m = 0; m < aod_module_count(); m++)
                        if (Text(aod_module_type(m)) == moduleType && Text(aod_module_version(m)) == version)
                        {
                            var module = new NativeModule(m);
                            Loaded[key] = module;
                            return module;
                        }
                }
                catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    _unavailable = $"the {Library} library is not available ({e.Message}): build it with "
                                   + "tools/scripts/build_cell_library.py";
                    throw new InvalidOperationException(_unavailable, e);
                }
                throw new InvalidOperationException($"the {Library} library has no module {key}");
            }
        }

        private const int ConstantKind = 2, ComputedConstantKind = 3, AlgebraicKind = 4;

        private static string Text(IntPtr utf8) => Marshal.PtrToStringAnsi(utf8) ?? "";

        [DllImport(Library)] private static extern int aod_module_count();
        [DllImport(Library)] private static extern IntPtr aod_module_name(int m);
        [DllImport(Library)] private static extern IntPtr aod_module_type(int m);
        [DllImport(Library)] private static extern IntPtr aod_module_version(int m);
        [DllImport(Library)] private static extern IntPtr aod_module_licence(int m);
        [DllImport(Library)] private static extern IntPtr aod_module_sha256(int m);
        [DllImport(Library)] private static extern int aod_instance_count(int m);
        [DllImport(Library)] private static extern IntPtr aod_instance_name(int m, int i);
        [DllImport(Library)] private static extern int aod_instance_param_count(int m, int i);
        [DllImport(Library)] private static extern IntPtr aod_instance_param_name(int m, int i, int k);
        [DllImport(Library)] private static extern double aod_instance_param_value(int m, int i, int k);
        [DllImport(Library)] private static extern int bk_state_count();
        [DllImport(Library)] private static extern int bk_variable_count();
        [DllImport(Library)] private static extern void bk_state_info(int i, out IntPtr name, out IntPtr units, out IntPtr component);
        [DllImport(Library)] private static extern int bk_variable_info(int i, out IntPtr name, out IntPtr units, out IntPtr component);
        [DllImport(Library)] private static extern int bk_state_block(int state);
        [DllImport(Library)] private static extern int bk_output_count();
        [DllImport(Library)] private static extern int bk_output_index(int i);
    }
}
