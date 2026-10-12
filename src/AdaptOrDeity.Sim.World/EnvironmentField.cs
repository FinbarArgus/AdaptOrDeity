using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Transport;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>
    /// What a cell exchanges with: the environment at its position. Species are indices of the environment's own list.
    /// </summary>
    public interface ICellEnvironment
    {
        /// <summary>
        /// Concentration of <paramref name="species"/> at the membrane of a cell of radius <paramref name="radius"/> [m] at
        /// (x, y) that releases <paramref name="release"/> [mol/s] of it (negative: takes it up) [mol/m³].
        /// </summary>
        double AtMembrane(int species, double x, double y, double radius, double release);

        /// <summary>Puts <paramref name="molPerSecond"/> of the species into the environment at (x, y) over the next step.</summary>
        void Exchange(int species, double x, double y, double molPerSecond);
    }

    /// <summary>
    /// The environment field's settings: content/parameters.json (field.*, environment.*). Every
    /// value is SI.
    /// </summary>
    public sealed class EnvironmentSettings
    {
        public NestedFieldSettings Grid { get; set; } = new NestedFieldSettings();

        /// <summary>Per species in <see cref="EnvironmentField"/> order: glucose, lactate, acid, O2, then the traces [m²/s].</summary>
        public double[] Diffusivity { get; set; } = Array.Empty<double>();

        /// <summary>O2 in the open water [mol/m³].</summary>
        public double Oxygen { get; set; }

        /// <summary>The carbonate buffer that turns acid into pH: total [mol/m³] and its pK.</summary>
        public double BufferTotal { get; set; }

        public double BufferPk { get; set; }

        public IReadOnlyList<TraceSpecies> TraceSpecies { get; set; } = Array.Empty<TraceSpecies>();

        /// <summary>The trace species take one field step every this many (<see cref="NestedField.SetStride"/>).</summary>
        public int TraceStride { get; set; } = 1;
    }

    /// <summary>
    /// The chemistry around the creature (<see cref="NestedField"/>): glucose, lactate, acid, O2 and the trace species
    /// (smells). Glucose, lactate and O2 are concentrations; acid is the H+ added to the water relative to the ocean [mol/m³
    /// of H+ equivalents], diffusing with the carbonate buffer that carries it, and pH follows from it through the buffer
    /// (<see cref="Protons"/>). Food packets' free sugar leaks into it (<see cref="Dissolve"/>); cells exchange with it at their
    /// position (<see cref="ICellEnvironment"/>), seeing the membrane correction.
    /// </summary>
    public sealed class EnvironmentField : ICellEnvironment
    {
        public const int Glucose = 0, Lactate = 1, Acid = 2, Oxygen = 3, FirstTrace = 4;

        private readonly EnvironmentSettings _settings;
        private readonly double _k, _bound0;

        /// <param name="settings">Grid, diffusivities, buffer and packets.</param>
        /// <param name="oceanGlucose">Glucose in the open water [mol/m³].</param>
        /// <param name="oceanProtons">Free H+ in the open water [mol/m³].</param>
        /// <param name="cx">Colony centre [m].</param>
        /// <param name="cy">Colony centre [m].</param>
        /// <param name="diameter">Colony diameter [m].</param>
        public EnvironmentField(EnvironmentSettings settings, double oceanGlucose, double oceanProtons, double cx, double cy,
            double diameter)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.Diffusivity.Length != FirstTrace + settings.TraceSpecies.Count)
                throw new ArgumentException("one diffusivity per species: glucose, lactate, acid, O2 and each trace");
            OceanProtons = oceanProtons;
            _k = 1e3 * Math.Pow(10.0, -settings.BufferPk);   // mol/m³
            _bound0 = settings.BufferTotal * oceanProtons / (oceanProtons + _k);
            var far = new double[settings.Diffusivity.Length];
            far[Glucose] = oceanGlucose;
            far[Oxygen] = settings.Oxygen;
            Field = new NestedField(settings.Grid, settings.Diffusivity, far, cx, cy, diameter);
            Field.SetActive(Oxygen, false);   // nothing makes or takes O2 yet
            for (int t = 0; t < settings.TraceSpecies.Count; t++)
            {
                Field.SetActive(FirstTrace + t, false);
                Field.SetStride(FirstTrace + t, Math.Max(1, settings.TraceStride));
            }
        }

        public NestedField Field { get; }

        public int TraceCount => _settings.TraceSpecies.Count;

        /// <summary>Free H+ in the open water [mol/m³].</summary>
        public double OceanProtons { get; }

        /// <summary>Glucose released by food packets so far [mol].</summary>
        public double PacketGlucose { get; private set; }

        /// <summary>Glucose in the open water [mol/m³]; setting it makes a new sea (the whole field at the new value).</summary>
        public double OceanGlucose
        {
            get => Field.FarField(Glucose);
            set
            {
                if (value != Field.FarField(Glucose))
                    Field.SetUniform(Glucose, value);
            }
        }

        /// <summary>
        /// Free H+ [mol/m³] in water holding <paramref name="acid"/> [mol/m³] of H+ added to the ocean: the carbonate buffer
        /// (one weak acid of total B and constant K) takes up most of it, so H + B·H/(H + K) = H0 + B·H0/(H0 + K) + acid.
        /// </summary>
        public double Protons(double acid)
        {
            double total = OceanProtons + _bound0 + acid;
            if (total <= 0.0)
                return 0.0;
            // H² + (K + B - T) H - T K = 0, the positive root in a form free of cancellation
            double b = _k + _settings.BufferTotal - total, tk = total * _k, root = Math.Sqrt(b * b + 4.0 * tk);
            return b > 0.0 ? 2.0 * tk / (b + root) : 0.5 * (root - b);
        }

        /// <summary>Recentres and regrids the field on the colony (before the step's sources are added).</summary>
        public void Follow(double cx, double cy, double diameter) => Field.Follow(cx, cy, diameter);

        /// <inheritdoc />
        public double AtMembrane(int species, double x, double y, double radius, double release)
        {
            double c = Field.AtMembrane(species, x, y, radius, release);
            return species == Acid ? c : Math.Max(0.0, c);
        }

        /// <inheritdoc />
        public void Exchange(int species, double x, double y, double molPerSecond)
        {
            if (!Field.Deposit(species, x, y, molPerSecond))
                throw new InvalidOperationException("a cell outside the environment field");
        }

        /// <summary>
        /// Each food packet's enzymes (<paramref name="hydrolysis"/>: the rate acting on it [mol glucose/s]) cut its polymer
        /// into its free sugar (<see cref="FoodField.Hydrolyse"/>, held back at the product-inhibition level), its free sugar
        /// leaks into the field where it lies (<see cref="FoodField.Leak"/>) with its traces in proportion (the contact trace
        /// per contact glucose, times each trace's strength), and what it offers the membranes touching it over the step is
        /// set (<see cref="FoodField.Predict"/>). Returns the glucose released into the field [mol].
        /// </summary>
        public double Dissolve(FoodField food, double dt, Func<FoodPacket, double> hydrolysis)
        {
            double released = 0.0, tracePerGlucose = food.Settings.ContactTrace / food.Settings.ContactGlucose;
            var packets = food.Packets;
            for (int k = 0; k < packets.Count; k++)   // by index: no enumerator to allocate
            {
                var p = packets[k];
                if (!p.Active)
                    continue;
                food.Hydrolyse(p, hydrolysis(p) * dt);
                double mol = food.Leak(p, dt);
                food.Predict(p, dt);
                if (mol <= 0.0)
                    continue;
                if (!Field.Deposit(Glucose, p.X, p.Y, mol / dt))
                    throw new InvalidOperationException("a food packet outside the environment field");
                for (int t = 0; t < TraceCount; t++)
                    Field.Deposit(FirstTrace + t, p.X, p.Y, mol / dt * tracePerGlucose * p.Traces[t]);
                released += mol;
            }
            PacketGlucose += released;
            return released;
        }

        /// <summary>Advances the field by <paramref name="dt"/> [s] (on <paramref name="workers"/> when given).</summary>
        public void Step(double dt, ParallelWork? workers = null) => Field.Step(dt, workers);
    }
}
