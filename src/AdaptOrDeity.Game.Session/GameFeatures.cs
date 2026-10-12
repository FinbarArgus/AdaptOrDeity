using System.Collections.Generic;
using AdaptOrDeity.Game.Content;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Game features and the level each unlocks at: content/timeline.json's <c>features</c> (a test keeps them in step).
    /// Once unlocked, an optional feature is always the player's choice to use. A feature with a cost is bought with skill
    /// points once its level is reached, the features it requires are unlocked and enough prey have been swallowed; buying
    /// it closes the features it excludes. Explanations are read from the timeline.
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
        public const string BoundaryInnerGroups = "boundary_inner_groups";
        public const string ExtracellularMatrix = "extracellular_matrix";
        public const string Engulfing = "engulfing";
        public const string Multicellularity = "multicellularity";
        public const string Regulation = "regulation";

        private static readonly string[] NeedsColony = { Multicellularity };

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
            new FeatureInfo(Engulfing, "Engulfing", 2, true, true,
                "A cell that touches a food packet or a prey cell smaller than itself takes it inside, in a vacuole. The cell's surface hydrolase digests it there and the cell's own sugar carriers take the sugar up from the vacuole, so none is lost or shared. What a cell swallows shares one vacuole, which must stay smaller than the cell (food packets always leave room for a prey cell). Prey cells swallowed are counted."),
            new FeatureInfo(Multicellularity, "Multicellularity", 2, false, true,
                "Daughters stay attached to the cell that made them, so the creature grows as a colony. Without it each daughter leaves when its parent divides: it counts as offspring and earns reproduction points, as do its own daughters and theirs, and the creature stays one cell. Later colony features (boundary and inner cells, several groups, a fixed body size) need it.",
                5),
            new FeatureInfo(Mitochondria, "Mitochondria", 2, false, true,
                "Available once your cells have swallowed 10 prey cells. A swallowed bacterium that is kept alive inside the cell becomes a partner that later breathes for it. Planned: respiration inside the cell is not modelled yet, so evolving it changes nothing in the cell for now.",
                5, requiresSwallowed: 10),
            new FeatureInfo(Chemosensing, "Smell (chemoreceptors)", 3, true, true,
                "Sense the trace species food packets carry, to swim towards them. Planned."),
            new FeatureInfo(MembraneRespiration, "Membrane respiration", 4, true, false, "Planned."),
            new FeatureInfo(BoundaryInnerGroups, "Boundary and inner cells", 4, false, true,
                "Water between three neighbouring cells becomes a shared space of its own. Cells with part of their edge on open water form the boundary group; cells surrounded by other cells form the inner group. Each group has its own sliders, and cells move between the groups as the colony grows.",
                requires: NeedsColony),
            new FeatureInfo(ExtracellularMatrix, "Extracellular matrix", 4, true, true,
                "A slider for each group sets how much matrix (a sugar polymer) its cells export into the space between cells. Each glucose unit of matrix costs one glucose and about 3 ATP. The matrix slows diffusion through the gaps and breaks down slowly, returning some glucose. Matrix made where a cell faces open water is lost to the sea.",
                requires: NeedsColony),
            new FeatureInfo(MultipleGroups, "Several cell groups", 6, true, false,
                "Split cells into groups adapted separately (organs later); sliders change every cell in a group.", requires: NeedsColony),
            new FeatureInfo(FixedBodySize, "Fixed body size", 7, true, false,
                "A group holds its number of cells: growth energy replaces dead cells, and goes to repair while the group is full.",
                requires: NeedsColony),
            new FeatureInfo(Regulation, "Regulation", 1, true, true,
                "Unlocked when your creature first goes half a minute without touching or holding food. A cog beside each expression slider and the growth slider chooses which falling species (ATP, G6P, glucose inside, pH) turn that protein or growth down, so a starving cell spends less.",
                requiresFoodGap: 30.0),
        });

        private static FeatureInfo[] WithExplanations(FeatureInfo[] features)
        {
            var timeline = GameTimeline.Default;
            for (int i = 0; i < features.Length; i++)
            {
                var f = features[i];
                features[i] = new FeatureInfo(f.Id, f.Name, f.UnlockLevel, f.Optional, f.DefaultEnabled, f.Effect, f.Cost,
                    timeline.Explanation(f.Id), f.Requires, f.RequiresSwallowed, f.Excludes, f.RequiresFoodGap);
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
