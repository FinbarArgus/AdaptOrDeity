using System.Collections.Generic;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Game features and the level each unlocks at: content/timeline.json's <c>features</c> (a test keeps them in step).
    /// Once unlocked, an optional feature is always the player's choice to use.
    /// </summary>
    public static class GameFeatures
    {
        public const string ProteinExpression = "protein_expression";
        public const string RepairLevel = "repair_level";
        public const string GrowthAndDivision = "growth_and_division";
        public const string GlycogenStorage = "glycogen_storage";
        public const string AutoDivide = "auto_divide";
        public const string MembraneRespiration = "membrane_respiration";
        public const string Mitochondria = "mitochondria";
        public const string MultipleGroups = "multiple_groups";
        public const string FixedBodySize = "fixed_body_size";

        public static readonly IReadOnlyList<FeatureInfo> All = new[]
        {
            new FeatureInfo(ProteinExpression, "Protein expression", 1, false, true,
                "Expression sliders for a cell group's transport proteins and glycolytic enzymes; copies cost ATP to build and to keep, and the membrane holds only so many."),
            new FeatureInfo(RepairLevel, "Repair", 1, true, true,
                "How much a group spends repairing damage. Low repair saves energy, but cells die at a lethal load and the colony relies on division."),
            new FeatureInfo(GrowthAndDivision, "Growth and division", 2, true, true,
                "Put energy into building a daughter cell; click a cell to divide once it is built."),
            new FeatureInfo(GlycogenStorage, "Glycogen storage", 2, true, true,
                "Store glucose as glycogen; spent when ATP falls, or to finish a nearly built daughter when you click Divide."),
            new FeatureInfo(AutoDivide, "Automatic division", 2, true, false, "Cells divide by themselves once a daughter is built."),
            new FeatureInfo(MembraneRespiration, "Membrane respiration", 3, true, false, "Planned."),
            new FeatureInfo(Mitochondria, "Mitochondria", 4, true, false, "Planned."),
            new FeatureInfo(MultipleGroups, "Several cell groups", 5, true, false,
                "Split cells into groups adapted separately (organs later); sliders change every cell in a group."),
            new FeatureInfo(FixedBodySize, "Fixed body size", 6, true, false,
                "A group holds its number of cells: growth energy replaces dead cells, and goes to repair while the group is full."),
        };

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id)
                    return i;
            return -1;
        }
    }
}
