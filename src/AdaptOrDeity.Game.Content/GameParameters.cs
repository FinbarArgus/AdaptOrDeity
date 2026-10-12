using System;
using System.Collections.Generic;
using System.IO;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Content
{
    /// <summary>One game-side value with where it comes from (content/parameters.json).</summary>
    public sealed class ParameterEntry
    {
        public ParameterEntry(string key, double value, string units, string source, string status, string note)
        {
            Key = key;
            Value = value;
            Units = units;
            Source = source;
            Status = status;
            Note = note;
        }

        public string Key { get; }

        public double Value { get; }

        /// <summary>Canonical SI units.</summary>
        public string Units { get; }

        /// <summary>Bib key, "definitional" or "unreferenced_placeholder".</summary>
        public string Source { get; }

        /// <summary>sourced, placeholder, design or definitional.</summary>
        public string Status { get; }

        public string Note { get; }
    }

    /// <summary>
    /// The game-side values: content/parameters.json, embedded in this assembly so the headless build and Unity read the
    /// same file. Cell physiology is not here; it comes with the assembled kernel (its parameters), whose values and
    /// sources live upstream in circulatory-autogen-modules modules/cell/generic_adaptive_cell/parameters.json.
    /// </summary>
    public sealed class GameParameters
    {
        private static GameParameters? _default;
        private readonly Dictionary<string, ParameterEntry> _entries;

        private GameParameters(Dictionary<string, ParameterEntry> entries, IReadOnlyList<TraceSpecies> traces)
        {
            _entries = entries;
            TraceSpecies = traces;
        }

        /// <summary>The embedded content/parameters.json.</summary>
        public static GameParameters Default => _default ??= Parse(ContentFiles.Read("parameters.json"));

        public IReadOnlyCollection<ParameterEntry> Entries => _entries.Values;

        public IReadOnlyList<TraceSpecies> TraceSpecies { get; }

        public ParameterEntry Entry(string key) =>
            _entries.TryGetValue(key, out var e) ? e : throw new KeyNotFoundException($"content/parameters.json has no {key}");

        public double Value(string key) => Entry(key).Value;

        public int Integer(string key)
        {
            double v = Value(key);
            if (v != Math.Floor(v))
                throw new FormatException($"content/parameters.json: {key} = {v} is not an integer");
            return (int)v;
        }

        /// <summary>
        /// Glucose equivalents per volume of a food packet or prey cell [mol/m³]: a cell's density × its dry share × the
        /// carbon share of the dry mass, over carbon's molar mass × 6 carbons per glucose (≈ 2.3 M).
        /// </summary>
        public double PacketDensity =>
            Value("body.cell_density") * Value("food.dry_mass_fraction") * Value("food.dry_carbon_fraction")
            / (Value("food.carbon_molar_mass") * Value("food.carbons_per_glucose"));

        /// <summary>The prey's settings (prey.*); their content is at the packet density.</summary>
        public PreySettings Prey() => new PreySettings
        {
            Radius = Value("prey.radius"),
            RunSpeed = Value("prey.run_speed"),
            MeanRun = Value("prey.mean_run"),
            Density = PacketDensity,
            FreeFraction = Value("food.free_fraction"),
            HalfSize = Value("food.field_half_size"),
            EntryAngleSpread = Value("food.entry_angle_spread"),
            InitialCapacity = Integer("prey.initial_capacity"),
            Seed = Integer("prey.seed"),
            ArrivalsPerSecond = GameTimeline.Default.ArrivalsAt(1).PreyPerSecond,
        };

        /// <summary>The food field's settings; the background glucose comes from the cell kernel.</summary>
        public FoodSettings Food(double backgroundGlucose) => new FoodSettings
        {
            BackgroundGlucose = backgroundGlucose,
            PacketDensity = PacketDensity,
            FreeFraction = Value("food.free_fraction"),
            FreeLeakRate = Value("food.free_leak_rate"),
            ContactGlucose = Value("food.contact_glucose"),
            ContactTrace = Value("food.contact_trace"),
            PacketTraceMinorShare = Value("food.packet_trace_minor_share"),
            PacketRadius = Value("food.packet_radius"),
            CarrierSpeed = Value("food.carrier_speed"),
            LyingDensity = Value("food.lying_density"),
            UsedUpFraction = Value("food.used_up_fraction"),
            EntryAngleSpread = Value("food.entry_angle_spread"),
            HalfSize = Value("food.field_half_size"),
            InitialPacketCapacity = Integer("food.initial_packet_capacity"),
            ArrivalsPerSecond = GameTimeline.Default.ArrivalsAt(1).FoodPerSecond,
            CarriedShare = GameTimeline.Default.ArrivalsAt(1).CarriedShare,
            Seed = Integer("food.seed"),
            TraceSpecies = TraceSpecies,
        };

        /// <summary>The environment field's settings (field.*, environment.*).</summary>
        public EnvironmentSettings Environment()
        {
            var diffusivity = new List<double>
            {
                Value("field.diffusivity.glucose"), Value("field.diffusivity.lactate"), Value("field.diffusivity.acid"),
                Value("field.diffusivity.oxygen"),
            };
            foreach (var t in TraceSpecies)
                diffusivity.Add(Value("field.diffusivity." + t.Id));
            return new EnvironmentSettings
            {
                Grid = new AdaptOrDeity.Sim.Transport.NestedFieldSettings
                {
                    FineSpacing = Value("field.fine_spacing"),
                    Margin = Value("field.fine_margin"),
                    ExtentMultiple = Value("field.extent_multiple"),
                    Depth = Value("field.layer_depth"),
                    FineSubsteps = Integer("field.fine_substeps"),
                },
                Diffusivity = diffusivity.ToArray(),
                Oxygen = Value("environment.oxygen"),
                BufferTotal = Value("environment.carbonate_total"),
                BufferPk = Value("environment.carbonate_pk1"),
                TraceSpecies = TraceSpecies,
            };
        }

        /// <summary>A rigid body in water flowing over the ground at the current (content environment.current_*).</summary>
        public RigidBody2D Body()
        {
            double speed = Value("environment.current_speed"), heading = Value("environment.current_heading");
            return new RigidBody2D { CurrentVx = speed * Math.Cos(heading), CurrentVy = speed * Math.Sin(heading) };
        }

                public static GameParameters Parse(string json)
        {
            var root = Json.Parse(json) as Dictionary<string, object?> ?? throw new FormatException("parameters.json: not an object");
            var entries = new Dictionary<string, ParameterEntry>();
            foreach (var kv in (Dictionary<string, object?>)root["parameters"]!)
            {
                var e = (Dictionary<string, object?>)kv.Value!;
                entries[kv.Key] = new ParameterEntry(kv.Key, (double)e["value"]!, (string)e["units"]!, (string)e["source"]!,
                    (string)e["status"]!, (string)e["note"]!);
            }
            var traces = new List<TraceSpecies>();
            foreach (var o in (List<object?>)root["trace_species"]!)
            {
                var t = (Dictionary<string, object?>)o!;
                traces.Add(new TraceSpecies((string)t["id"]!, (string)t["name"]!, (string)t["source"]!));
            }
            return new GameParameters(entries, traces);
        }
    }

    /// <summary>The adaptable traits of a cell type (content/traits/&lt;organism&gt;.json, embedded).</summary>
    public static class TraitCatalog
    {
        private static IReadOnlyList<TraitDefinition>? _fermenter;

        /// <summary>The fermenter's traits, with the expression range of content/parameters.json.</summary>
        public static IReadOnlyList<TraitDefinition> Fermenter =>
            _fermenter ??= Parse(ContentFiles.Read("traits/fermenter.json"), GameParameters.Default);

        public static IReadOnlyList<TraitDefinition> Parse(string json, GameParameters parameters)
        {
            var root = (Dictionary<string, object?>)Json.Parse(json)!;
            double min = parameters.Value("controls.expression_min"), max = parameters.Value("controls.expression_max");
            var list = new List<TraitDefinition>();
            foreach (var o in (List<object?>)root["traits"]!)
            {
                var t = (Dictionary<string, object?>)o!;
                string? protein = t.TryGetValue("protein", out var p) ? (string?)p : null;
                string? feature = t.TryGetValue("feature", out var f) ? (string?)f : null;
                var kind = (string)t["kind"]! switch
                {
                    "expression" => TraitKind.Expression,
                    "count" => TraitKind.Count,
                    "share" => TraitKind.Share,
                    var other => throw new FormatException($"trait {t["id"]}: unknown kind {other}"),
                };
                // an expression multiplier uses the content-wide range; a count (e.g. flagella) or a share carries its own
                if (t.TryGetValue("range", out var r) && r is Dictionary<string, object?> range)
                    list.Add(new TraitDefinition((string)t["id"]!, (string)t["label"]!, protein, (string)t["pool"]!,
                        (double)range["min"]!, (double)range["max"]!,
                        range.TryGetValue("step", out var step) ? (double)step! : 0.0, kind, feature));
                else
                    list.Add(new TraitDefinition((string)t["id"]!, (string)t["label"]!, protein, (string)t["pool"]!, min, max,
                        0.0, kind, feature));
            }
            return list;
        }
    }

    /// <summary>Content files embedded in this assembly (see the csproj), by their path under content/.</summary>
    public static class ContentFiles
    {
        public static string Read(string path)
        {
            var asm = typeof(ContentFiles).Assembly;
            string name = "content/" + path;
            using Stream? s = asm.GetManifestResourceStream(name);
            if (s == null)
                throw new FileNotFoundException($"{name} is not embedded in {asm.GetName().Name}");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
