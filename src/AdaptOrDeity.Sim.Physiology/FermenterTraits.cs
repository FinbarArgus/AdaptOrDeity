using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>
    /// One adaptable trait: how many copies of a protein a cell group makes, as a multiplier on its baseline. It scales
    /// the protein's density or capacity and its copy number in protein_expression together. Mirrors
    /// content/traits/fermenter.json (a test keeps them in step).
    /// </summary>
    public sealed class TraitDefinition
    {
        public TraitDefinition(string id, string label, string protein, string[] parameters, string copyKey,
            double footprintNm2)
        {
            Id = id;
            Label = label;
            Protein = protein;
            Parameters = parameters;
            CopyKey = copyKey;
            FootprintNm2 = footprintNm2;
        }

        public string Id { get; }

        public string Label { get; }

        /// <summary>Protein id in content/timeline.json.</summary>
        public string Protein { get; }

        /// <summary>Supermodule parameters (<c>&lt;variable&gt;_&lt;submodule&gt;</c>) the multiplier scales.</summary>
        public IReadOnlyList<string> Parameters { get; }

        /// <summary>The protein_expression key (n_X, L_X) whose copies the trait scales.</summary>
        public string CopyKey { get; }

        /// <summary>Membrane area one copy takes [nm²]; 0 for cytosolic proteins.</summary>
        public double FootprintNm2 { get; }

        public double Min => FermenterTraits.ExpressionMin;

        public double Max => FermenterTraits.ExpressionMax;
    }

    /// <summary>The fermenter cell's adaptable traits (content/traits/fermenter.json).</summary>
    public static class FermenterTraits
    {
        /// <summary>Expression range, two octaves either way (placeholder, 2026-10-08).</summary>
        public const double ExpressionMin = 0.25, ExpressionMax = 4.0;

        /// <summary>Time constant with which expression approaches its target [s] (user decision, 2026-10-08).</summary>
        public const double ExpressionTimeConstant = 10.0;

        /// <summary>Most of the membrane the adaptable transporters may take, as a fraction of the cell surface (placeholder).</summary>
        public const double MembraneShareMax = 0.08;

        /// <summary>Most ATP the cell can put into building protein, as a multiple of its resting ATP turnover (placeholder;
        /// a growing cell builds protein at many times its maintenance rate).</summary>
        public const double BuildCapacity = 5.0;

        /// <summary>ATP [mM] at which building new protein stops; full speed at the resting ATP (placeholder).</summary>
        public const double BuildAtpFloor = 4.0;

        public static readonly IReadOnlyList<TraitDefinition> All = new[]
        {
            Expr("H_ATPase_expression", "F1Fo H+-ATPase", "rotary_atpase", "rho_H_ATPase_i_H_ATPase", "H_ATPase", 100.0),
            Expr("NhaA_expression", "NhaA Na+/H+ antiporter", "nhaa", "g_spec_i_NaH", "NhaA", 15.0),
            Expr("KefC_expression", "KefC K+/H+ exchanger", "kefc", "g_spec_i_KH", "KefC", 20.0),
            Expr("Trk_expression", "Trk K+ channel", "trk_ktr", "rho_leak_K_i_K_Trk", "Trk", 60.0),
            Expr("NCX_expression", "Na+/Ca2+ exchanger", "ncx_prokaryotic", "rho_NaCa_i_NaCa", "NCX", 12.0),
            Expr("Glf_expression", "Glf glucose facilitator", "mfs_sugar_porter", "jmax_glc_membrane_face", "Glf", 12.0),
            Expr("lactate_exporter_expression", "Lactate-/H+ symporter", "lactate_carrier_bacterial",
                "jmax_lac_membrane_face", "LacT", 15.0),
            Expr("glycolysis_expression", "Glycolytic enzymes", "glycolysis_sugar", "V_gly_cytosol", "Glyc", 0.0),
        };

        /// <summary>
        /// Kernel external name of a supermodule parameter: libcuflynx names them <c>&lt;variable&gt;_mod_&lt;submodule&gt;</c>.
        /// </summary>
        public static string KernelName(string supermoduleParameter)
        {
            foreach (var sub in Submodules)
                if (supermoduleParameter.EndsWith("_" + sub, StringComparison.Ordinal))
                    return supermoduleParameter.Substring(0, supermoduleParameter.Length - sub.Length - 1) + "_mod_" + sub;
            throw new ArgumentException($"no submodule suffix on {supermoduleParameter}", nameof(supermoduleParameter));
        }

        // Longest first, so "i_leak_Na" wins over "Na" endings.
        private static readonly string[] Submodules =
        {
            "membrane_face", "i_H_ATPase", "i_leak_Na", "i_leak_Ca", "i_leak_H", "i_K_Trk", "i_NaCa", "i_NaH", "i_KH",
            "membrane", "cytosol", "protein", "Na_K", "bio", "Ca", "H",
        };

        private static TraitDefinition Expr(string id, string label, string protein, string parameter, string copyKey,
            double footprint) =>
            new TraitDefinition(id, label + " copies", protein, new[] { parameter, "n_" + copyKey + "_protein" }, copyKey,
                footprint);
    }
}
