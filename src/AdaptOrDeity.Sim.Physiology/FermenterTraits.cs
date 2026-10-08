using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.Physiology
{
    public enum TraitKind
    {
        /// <summary>How many copies of a protein the cell makes: scales its density or capacity and its copy number.</summary>
        Expression,

        /// <summary>A half-saturation or half-activation constant of the protein's kinetics.</summary>
        Midpoint,
    }

    /// <summary>
    /// One adaptable trait: a multiplier on one or more parameters of the cell's default instance. Mirrors
    /// content/traits/fermenter.json (a test keeps them in step).
    /// </summary>
    public sealed class TraitDefinition
    {
        public TraitDefinition(string id, string label, string protein, TraitKind kind, string[] parameters,
            string? copyKey = null)
        {
            Id = id;
            Label = label;
            Protein = protein;
            Kind = kind;
            Parameters = parameters;
            CopyKey = copyKey;
        }

        public string Id { get; }

        public string Label { get; }

        /// <summary>Protein id in content/protein_timeline.json.</summary>
        public string Protein { get; }

        public TraitKind Kind { get; }

        /// <summary>Supermodule parameters (<c>&lt;variable&gt;_&lt;submodule&gt;</c>) the multiplier scales.</summary>
        public IReadOnlyList<string> Parameters { get; }

        /// <summary>For expression traits: the protein_expression key (n_X, L_X) whose copies it scales.</summary>
        public string? CopyKey { get; }

        public double Min => Kind == TraitKind.Expression ? FermenterTraits.ExpressionMin : FermenterTraits.MidpointMin;

        public double Max => Kind == TraitKind.Expression ? FermenterTraits.ExpressionMax : FermenterTraits.MidpointMax;

        /// <summary>Sliders move on a log scale (expression) or a linear one (midpoints).</summary>
        public bool LogScale => Kind == TraitKind.Expression;
    }

    /// <summary>The fermenter cell's adaptable traits (content/traits/fermenter.json).</summary>
    public static class FermenterTraits
    {
        /// <summary>Expression range, two octaves either way (placeholder, 2026-10-08).</summary>
        public const double ExpressionMin = 0.25, ExpressionMax = 4.0;

        /// <summary>Kinetic midpoints ±10% (user decision, 2026-10-08).</summary>
        public const double MidpointMin = 0.9, MidpointMax = 1.1;

        public static readonly IReadOnlyList<TraitDefinition> All = new[]
        {
            Expr("H_ATPase_expression", "F1Fo H+-ATPase", "rotary_atpase", "rho_H_ATPase_i_H_ATPase", "H_ATPase"),
            Mid("H_ATPase_ATP_midpoint", "H+-ATPase ATP half-saturation", "rotary_atpase", "K_m_ATP_H_i_H_ATPase"),
            Mid("H_ATPase_pH_midpoint", "H+-ATPase H+ half-activation (pH set point)", "rotary_atpase", "K_act_H_i_H_ATPase"),
            Expr("NhaA_expression", "NhaA Na+/H+ antiporter", "nhaa", "g_spec_i_NaH", "NhaA"),
            Expr("KefC_expression", "KefC K+/H+ exchanger", "kefc", "g_spec_i_KH", "KefC"),
            Expr("Trk_expression", "Trk K+ channel", "trk_ktr", "rho_leak_K_i_K_Trk", "Trk"),
            Expr("NCX_expression", "Na+/Ca2+ exchanger", "ncx_prokaryotic", "rho_NaCa_i_NaCa", "NCX"),
            Mid("NCX_Ca_midpoint", "NCX Ca2+ half-saturation", "ncx_prokaryotic", "KmCa_i_NaCa"),
            Mid("NCX_Na_midpoint", "NCX Na+ half-saturation", "ncx_prokaryotic", "KmNai_i_NaCa"),
            Expr("Glf_expression", "Glf glucose facilitator", "mfs_sugar_porter", "jmax_glc_membrane_face", "Glf"),
            Mid("Glf_midpoint", "Glf glucose half-saturation", "mfs_sugar_porter", "K_glc_t_membrane_face"),
            Expr("lactate_exporter_expression", "Lactate-/H+ symporter", "lactate_carrier_bacterial",
                "jmax_lac_membrane_face", "LacT"),
            Mid("lactate_exporter_midpoint", "Lactate exporter half-saturation", "lactate_carrier_bacterial",
                "K_lac_t_membrane_face"),
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
            "membrane", "cytosol", "protein", "Na_K", "Ca", "H",
        };

        private static TraitDefinition Expr(string id, string label, string protein, string parameter, string copyKey) =>
            new TraitDefinition(id, label + " copies", protein, TraitKind.Expression,
                new[] { parameter, "n_" + copyKey + "_protein" }, copyKey);

        private static TraitDefinition Mid(string id, string label, string protein, string parameter) =>
            new TraitDefinition(id, label, protein, TraitKind.Midpoint, new[] { parameter });
    }
}
