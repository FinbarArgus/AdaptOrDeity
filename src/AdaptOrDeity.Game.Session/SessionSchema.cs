using System.Collections.Generic;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// One overlay channel the front end can show (ONI-style overlay tab). Values in snapshots are canonical SI;
    /// the UI displays <c>value × DisplayMultiplier</c> in <see cref="DisplayUnit"/>.
    /// </summary>
    public sealed class OverlayInfo
    {
        public OverlayInfo(string id, string name, string hotkey, string siUnit, string displayUnit,
            double displayMultiplier, double displayMin, double displayMax)
        {
            Id = id;
            Name = name;
            Hotkey = hotkey;
            SiUnit = siUnit;
            DisplayUnit = displayUnit;
            DisplayMultiplier = displayMultiplier;
            DisplayMin = displayMin;
            DisplayMax = displayMax;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>Suggested key binding, e.g. "F1".</summary>
        public string Hotkey { get; }

        public string SiUnit { get; }

        public string DisplayUnit { get; }

        public double DisplayMultiplier { get; }

        /// <summary>Default legend range, in display units.</summary>
        public double DisplayMin { get; }

        public double DisplayMax { get; }
    }

    /// <summary>
    /// One quantity shown in the cell panel (metabolic rate, an ATP source or sink, a state). Snapshots carry it in SI;
    /// the UI shows <c>value × DisplayMultiplier</c> in <see cref="DisplayUnit"/>, grouped by <see cref="Group"/>.
    /// </summary>
    public sealed class DetailInfo
    {
        public DetailInfo(string id, string group, string name, string siUnit, string displayUnit, double displayMultiplier,
            string description = "")
        {
            Id = id;
            Group = group;
            Name = name;
            SiUnit = siUnit;
            DisplayUnit = displayUnit;
            DisplayMultiplier = displayMultiplier;
            Description = description;
        }

        public string Id { get; }

        public string Group { get; }

        public string Name { get; }

        public string SiUnit { get; }

        public string DisplayUnit { get; }

        public double DisplayMultiplier { get; }

        public string Description { get; }
    }

    /// <summary>
    /// A game mechanic unlocked at a level of the evolutionary time bar (content/timeline.json). Once unlocked, an optional
    /// feature is always the player's choice to use.
    /// </summary>
    public sealed class FeatureInfo
    {
        public FeatureInfo(string id, string name, int unlockLevel, bool optional, bool defaultEnabled, string effect,
            int cost = 0, string explanation = "")
        {
            Cost = cost;
            Explanation = explanation;
            Id = id;
            Name = name;
            UnlockLevel = unlockLevel;
            Optional = optional;
            DefaultEnabled = defaultEnabled;
            Effect = effect;
        }

        public string Id { get; }

        public string Name { get; }

        public int UnlockLevel { get; }

        public bool Optional { get; }

        public bool DefaultEnabled { get; }

        public string Effect { get; }

        /// <summary>Skill points to buy it once its level is reached (0: unlocked by the level alone).</summary>
        public int Cost { get; }

        /// <summary>The full explanation for the player (content/timeline.json features[].explanation).</summary>
        public string Explanation { get; }
    }

    /// <summary>A per-group energy control the panel offers as a slider (growth, storage, repair).</summary>
    public sealed class ControlInfo
    {
        public ControlInfo(string id, string label, double min, double max, double defaultValue, string feature, string unit,
            string description)
        {
            Id = id;
            Label = label;
            Min = min;
            Max = max;
            DefaultValue = defaultValue;
            Feature = feature;
            Unit = unit;
            Description = description;
        }

        public string Id { get; }

        public string Label { get; }

        public double Min { get; }

        public double Max { get; }

        public double DefaultValue { get; }

        /// <summary>Feature id that must be unlocked and enabled for the control to apply.</summary>
        public string Feature { get; }

        public string Unit { get; }

        public string Description { get; }
    }

    /// <summary>
    /// An adaptable trait the panel offers as a slider: a multiplier on the cell's baseline (log scale), a count, or (for a
    /// carrier the starting cell lacks) a share of the membrane ceiling.
    /// </summary>
    public sealed class TraitInfo
    {
        public TraitInfo(string id, string label, string? protein, bool isExpression, double min, double max, bool logScale,
            double step = 0.0, bool isShare = false, string? feature = null)
        {
            Step = step;
            IsShare = isShare;
            Feature = feature;
            Id = id;
            Label = label;
            Protein = protein;
            IsExpression = isExpression;
            Min = min;
            Max = max;
            LogScale = logScale;
        }

        public string Id { get; }

        public string Label { get; }

        /// <summary>Protein id in content/timeline.json, or null for a carrier bought as a feature.</summary>
        public string? Protein { get; }

        /// <summary>True if the value is a share of the membrane ceiling (0 to the room the other proteins leave).</summary>
        public bool IsShare { get; }

        /// <summary>Feature id that must be unlocked (bought) before the slider is offered, or null.</summary>
        public string? Feature { get; }

        /// <summary>True for protein expression; false for a kinetic midpoint.</summary>
        public bool IsExpression { get; }

        public double Min { get; }

        public double Max { get; }

        public bool LogScale { get; }

        /// <summary>0 for a continuous value; otherwise the slider moves in whole steps (e.g. one flagellum).</summary>
        public double Step { get; }
    }

    /// <summary>Everything the front end needs to build its menus, generated by the simulation (CUFLynx pattern).</summary>
    public sealed class SessionSchema
    {
        public SessionSchema(IReadOnlyList<OverlayInfo> overlays, IReadOnlyList<double> speedPresets,
            IReadOnlyList<DetailInfo>? details = null, IReadOnlyList<TraitInfo>? traits = null,
            IReadOnlyList<ControlInfo>? controls = null, IReadOnlyList<FeatureInfo>? features = null, int maxCells = 0,
            int maxGroups = 0, IReadOnlyList<string>? traceSpecies = null, double fieldHalfSize = 0.0, int initialPacketCapacity = 0)
        {
            TraceSpecies = traceSpecies ?? new string[0];
            FieldHalfSize = fieldHalfSize;
            InitialPacketCapacity = initialPacketCapacity;
            Overlays = overlays;
            SpeedPresets = speedPresets;
            Details = details ?? new DetailInfo[0];
            Traits = traits ?? new TraitInfo[0];
            Controls = controls ?? new ControlInfo[0];
            Features = features ?? new FeatureInfo[0];
            InitialCellCapacity = maxCells;
            MaxGroups = maxGroups;
        }

        /// <summary>Per-group energy controls; an entry's index is its index in <see cref="SnapshotFrame.Controls"/>.</summary>
        public IReadOnlyList<ControlInfo> Controls { get; }

        /// <summary>Game features by level; an entry's index is its index in <see cref="SnapshotFrame.FeatureEnabled"/>.</summary>
        public IReadOnlyList<FeatureInfo> Features { get; }

        /// <summary>Most cells (and so snapshot slots) the world holds.</summary>
        /// <summary>Cells snapshot frames are first sized for; they grow with the colony.</summary>
        public int InitialCellCapacity { get; }

        public int MaxGroups { get; }

        /// <summary>Names of the trace species food packets carry.</summary>
        public IReadOnlyList<string> TraceSpecies { get; }

        /// <summary>Half the side of the square food field [m] (0: no food field).</summary>
        public double FieldHalfSize { get; }

        /// <summary>Food packets snapshot frames are first sized for; they grow with the food in the field.</summary>
        public int InitialPacketCapacity { get; }

        /// <summary>Cell-panel quantities; an entry's index is its index in <see cref="SnapshotFrame.Details"/>.</summary>
        public IReadOnlyList<DetailInfo> Details { get; }

        /// <summary>Adaptable traits; an entry's index is its index in <see cref="SnapshotFrame.TraitValues"/>.</summary>
        public IReadOnlyList<TraitInfo> Traits { get; }

        /// <summary>Overlay channels; a channel's index here is its index in <see cref="SnapshotFrame.Channels"/>.</summary>
        public IReadOnlyList<OverlayInfo> Overlays { get; }

        /// <summary>Sim-seconds per real second for each speed preset (index 0 is pause).</summary>
        public IReadOnlyList<double> SpeedPresets { get; }
    }
}
