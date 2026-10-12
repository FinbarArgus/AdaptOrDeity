using System.Collections.Generic;
using AdaptOrDeity.Game.Content;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Game features and the level each unlocks at: content/timeline.json's <c>features</c> (a test keeps them in step).
    /// Once unlocked, an optional feature is always the player's choice to use. A feature with a cost is bought with skill
    /// points once its level is reached. Explanations are read from the timeline.
    /// </summary>
    public static class GameFeatures
    {
        public const string ProteinExpression = "protein_expression";
        public const string RepairLevel = "repair_level";
        public const string GrowthAndDivision = "growth_and_division";
        public const string GlycogenStorage = "glycogen_storage";
        public const string AutoDivide = "auto_divide";
        public const string MotilityControl = "motility_control";
        public const string Chemosensing = "chemosensing";
        public const string Swimming = "swimming";
        public const string MembraneRespiration = "membrane_respiration";
        public const string Mitochondria = "mitochondria";
        public const string MultipleGroups = "multiple_groups";
        public const string FixedBodySize = "fixed_body_size";
        public const string PtsTransport = "pts_transport";
        public const string AbcTransport = "abc_transport";
        public const string ProtonSymport = "proton_symport";
        public const string CarbonRegulation = "carbon_regulation";

        public static readonly IReadOnlyList<FeatureInfo> All = WithExplanations(new[]
        {
            new FeatureInfo(ProteinExpression, "Protein expression", 1, false, true,
                "Expression sliders for a cell group's transport proteins and glycolytic enzymes; copies cost ATP to build and to keep, and the membrane holds only so many."),
            new FeatureInfo(RepairLevel, "Repair", 1, true, true,
                "How much a group spends repairing damage. Low repair saves energy, but cells die at a lethal load and the colony relies on division."),
            new FeatureInfo(Swimming, "Swimming (flagellum)", 1, true, true,
                "The cell starts with one flagellum; its motor runs at random (runs and pauses) on the proton motive force. " +
                "The flagella slider adds more (each costs ATP to build and protons to run). Turn it off to save the energy."),
            new FeatureInfo(GrowthAndDivision, "Growth and division", 2, true, true,
                "Put energy into building a daughter cell; click a cell to divide once it is built."),
            new FeatureInfo(GlycogenStorage, "Glycogen storage", 2, true, true,
                "Store glucose as glycogen; spent when ATP falls, or to finish a nearly built daughter when you click Divide."),
            new FeatureInfo(AutoDivide, "Automatic division", 2, true, false, "Cells divide by themselves once a daughter is built."),
            new FeatureInfo(MotilityControl, "Swimming control", 2, true, false,
                "Processing to decide when to run the flagella (e.g. keep still on food, swim when it runs out). Planned."),
            new FeatureInfo(PtsTransport, "Sugar phosphotransferase (PTS)", 2, true, true,
                "A high-affinity glucose transporter that phosphorylates glucose as it brings it in, paid by one PEP from glycolysis.", 5),
            new FeatureInfo(AbcTransport, "ABC sugar importer", 2, true, true,
                "The highest-affinity glucose importer, powered by ATP: about 2 ATP per glucose.", 5),
            new FeatureInfo(ProtonSymport, "Proton-coupled sugar symporter", 2, true, true,
                "Brings glucose in with a proton, driven by the proton gradient the H+-ATPase builds.", 5),
            new FeatureInfo(CarbonRegulation, "Carbon regulation", 2, true, true,
                "Cells turn the glucose facilitator down by themselves while other carriers bring glucose in, so it stops leaking glucose back out. Planned.", 3),
            new FeatureInfo(Chemosensing, "Smell (chemoreceptors)", 3, true, true,
                "Sense the trace species food packets carry, to swim towards them. Planned."),
            new FeatureInfo(MembraneRespiration, "Membrane respiration", 4, true, false, "Planned."),
            new FeatureInfo(Mitochondria, "Mitochondria", 5, true, false, "Planned."),
            new FeatureInfo(MultipleGroups, "Several cell groups", 6, true, false,
                "Split cells into groups adapted separately (organs later); sliders change every cell in a group."),
            new FeatureInfo(FixedBodySize, "Fixed body size", 7, true, false,
                "A group holds its number of cells: growth energy replaces dead cells, and goes to repair while the group is full."),
        });

        private static FeatureInfo[] WithExplanations(FeatureInfo[] features)
        {
            var timeline = GameTimeline.Default;
            for (int i = 0; i < features.Length; i++)
            {
                var f = features[i];
                features[i] = new FeatureInfo(f.Id, f.Name, f.UnlockLevel, f.Optional, f.DefaultEnabled, f.Effect, f.Cost,
                    timeline.Explanation(f.Id));
            }
            return features;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id)
                    return i;
            return -1;
        }
    }
}
