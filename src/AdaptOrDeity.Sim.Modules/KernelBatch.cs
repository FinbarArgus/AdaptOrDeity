using System;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// Structure-of-arrays storage for every instance of one kernel: one <c>double[]</c> per variable,
    /// indexed by instance slot.
    /// </summary>
    public sealed class KernelBatch
    {
        public KernelBatch(KernelDescriptor descriptor, int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            Descriptor = descriptor;
            States = Allocate(descriptor.States.Count, capacity);
            Rates = Allocate(descriptor.States.Count, capacity);
            Externals = Allocate(descriptor.Externals.Count, capacity);
            Algebraic = Allocate(descriptor.Algebraic.Count, capacity);
            Capacity = capacity;
        }

        public KernelDescriptor Descriptor { get; }

        /// <summary>Number of allocated instance slots.</summary>
        public int Capacity { get; private set; }

        /// <summary>Number of slots in use; instances occupy [0, Count).</summary>
        public int Count { get; set; }

        public double[][] States { get; }

        public double[][] Rates { get; }

        public double[][] Externals { get; }

        public double[][] Algebraic { get; }

        /// <summary>Copies the named instance's values into the externals of slots [start, end).</summary>
        public void ApplyInstance(string instance, int start, int end)
        {
            if (!Descriptor.Instances.TryGetValue(instance, out var values))
                throw new ArgumentException($"{Descriptor.Key} has no instance '{instance}'", nameof(instance));
            foreach (var pair in values)
            {
                int e = Descriptor.ExternalIndex(pair.Key);
                if (e < 0)
                    continue; // Instance values for non-external variables (e.g. none yet) are ignored.
                double[] column = Externals[e];
                for (int k = start; k < end; k++)
                    column[k] = pair.Value;
            }
        }

        /// <summary>Grows every array to at least <paramref name="capacity"/> slots, keeping contents.</summary>
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= Capacity)
                return;
            int newCapacity = Math.Max(capacity, Capacity * 2);
            Grow(States, newCapacity);
            Grow(Rates, newCapacity);
            Grow(Externals, newCapacity);
            Grow(Algebraic, newCapacity);
            Capacity = newCapacity;
        }

        private static double[][] Allocate(int variables, int capacity)
        {
            var arrays = new double[variables][];
            for (int i = 0; i < variables; i++)
                arrays[i] = new double[capacity];
            return arrays;
        }

        private static void Grow(double[][] arrays, int capacity)
        {
            for (int i = 0; i < arrays.Length; i++)
                Array.Resize(ref arrays[i], capacity);
        }
    }
}
