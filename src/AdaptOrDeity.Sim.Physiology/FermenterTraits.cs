namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>
    /// One adaptable trait: the expression target of one protein_pool of the assembled cell. The pool (CellML,
    /// circulatory-autogen-modules cell/protein_pool) moves its expression to the target, pays to build and keep the copies,
    /// and scales the protein it belongs to. Loaded from content/traits/&lt;organism&gt;.json by Game.Content.
    /// </summary>
    public sealed class TraitDefinition
    {
        public TraitDefinition(string id, string label, string protein, string pool, double min, double max, double step = 0.0)
        {
            Step = step;
            Id = id;
            Label = label;
            Protein = protein;
            Pool = pool;
            Min = min;
            Max = max;
        }

        public string Id { get; }

        public string Label { get; }

        /// <summary>Protein id in content/timeline.json.</summary>
        public string Protein { get; }

        /// <summary>The protein_pool submodule of the assembled cell (kernel names <c>&lt;pool&gt;__m</c>,
        /// <c>m_target_mod_&lt;pool&gt;</c>).</summary>
        public string Pool { get; }

        /// <summary>Expression range the player may choose (content/parameters.json controls.expression_*).</summary>
        public double Min { get; }

        public double Max { get; }

        /// <summary>0 for a continuous expression multiplier (log scale); otherwise whole steps (e.g. a number of
        /// flagella).</summary>
        public double Step { get; }

        /// <summary>Kernel parameter holding the pool's expression target.</summary>
        public string TargetParameter => "m_target_mod_" + Pool;

        /// <summary>Kernel state holding the pool's expression.</summary>
        public string ExpressionState => Pool + "__m";

        /// <summary>Kernel variable: membrane area per unit of expression [µm²].</summary>
        public string AreaPerUnit => Pool + "__a_unit";
    }
}
