namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>How a trait's slider value maps onto its protein_pool's target.</summary>
    public enum TraitKind
    {
        /// <summary>
        /// Expression relative to the starting cell (×Min-×Max, log scale). A membrane protein's pool takes it as a share of
        /// the membrane ceiling (<c>s_target</c> = value × the starting share); a cytosolic one as <c>m_target</c>.
        /// </summary>
        Expression,

        /// <summary>Whole units (e.g. flagella): <c>m_target</c> in steps of <see cref="TraitDefinition.Step"/>.</summary>
        Count,

        /// <summary>
        /// A share of the membrane ceiling (0-1), for a carrier the starting cell does not make (its starting share is 0):
        /// <c>s_target</c> itself.
        /// </summary>
        Share,
    }

    /// <summary>
    /// One adaptable trait: the target of one protein_pool of the assembled cell. The pool (CellML,
    /// circulatory-autogen-modules cell/protein_pool) moves its expression to the target, pays to build and keep the copies,
    /// and scales the protein it belongs to. Loaded from content/traits/&lt;organism&gt;.json by Game.Content.
    /// </summary>
    public sealed class TraitDefinition
    {
        public TraitDefinition(string id, string label, string? protein, string pool, double min, double max, double step = 0.0,
            TraitKind kind = TraitKind.Expression, string? feature = null)
        {
            Id = id;
            Label = label;
            Protein = protein;
            Pool = pool;
            Min = min;
            Max = max;
            Step = step;
            Kind = step > 0.0 ? TraitKind.Count : kind;
            Feature = feature;
        }

        public string Id { get; }

        public string Label { get; }

        /// <summary>Protein id in content/timeline.json, or null for a carrier bought as a feature.</summary>
        public string? Protein { get; }

        /// <summary>The protein_pool submodule of the assembled cell (kernel names <c>&lt;pool&gt;__m</c>,
        /// <c>s_target_mod_&lt;pool&gt;</c> or <c>m_target_mod_&lt;pool&gt;</c>).</summary>
        public string Pool { get; }

        /// <summary>Range the player may choose: an expression multiplier (content/parameters.json
        /// controls.expression_*), a count, or a share of the membrane ceiling.</summary>
        public double Min { get; }

        public double Max { get; }

        /// <summary>0 for a continuous value; otherwise whole steps (e.g. a number of flagella).</summary>
        public double Step { get; }

        public TraitKind Kind { get; }

        /// <summary>Game feature (content/timeline.json features) that must be unlocked before the trait can be set, or
        /// null.</summary>
        public string? Feature { get; }

        /// <summary>The value the trait starts at: 1 (the starting cell's expression, or one unit), 0 for a share.</summary>
        public double Default => Kind == TraitKind.Share ? 0.0 : 1.0;

        /// <summary>Kernel parameter of a pool whose target is a share of the membrane ceiling.</summary>
        public string ShareTargetParameter => "s_target_mod_" + Pool;

        /// <summary>Kernel parameter of a pool whose target is in units of its starting expression.</summary>
        public string ExpressionTargetParameter => "m_target_mod_" + Pool;

        /// <summary>Kernel state holding the pool's expression.</summary>
        public string ExpressionState => Pool + "__m";

        /// <summary>Kernel variable: membrane area per unit of expression [µm²].</summary>
        public string AreaPerUnit => Pool + "__a_unit";
    }
}
