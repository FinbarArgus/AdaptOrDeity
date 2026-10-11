using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The simulated content that <see cref="SimLoop"/> steps: one organism plus its environment.
    /// Values exchanged through this interface are canonical SI.
    /// </summary>
    public interface ISimWorld
    {
        /// <summary>Overlay channels this world can report, in channel order.</summary>
        IReadOnlyList<OverlayInfo> Overlays { get; }

        int CellCount { get; }

        /// <summary>Fixed macro step [s]. The loop only ever steps by exactly this amount (determinism).</summary>
        double MacroDt { get; }

        /// <summary>Advances the world from <paramref name="t"/> by <paramref name="dt"/> seconds.</summary>
        void Step(double t, double dt);

        /// <summary>Current value of overlay <paramref name="channel"/> in <paramref name="cell"/> (SI).</summary>
        double Read(int cell, int channel);
    }

    /// <summary>A world with a well-mixed bath whose concentration the player or a test can set.</summary>
    public interface IBathWorld
    {
        /// <summary>Bath concentration [mol/m³].</summary>
        double BathConcentration { get; set; }
    }

    /// <summary>
    /// A world of cells that can be inspected in the cell panel and adapted: traits and energy controls are set per cell
    /// group, cells can divide and die, and features unlock by level.
    /// </summary>
    public interface IInspectableWorld
    {
        IReadOnlyList<DetailInfo> Details { get; }

        IReadOnlyList<TraitInfo> Traits { get; }

        IReadOnlyList<ControlInfo> Controls { get; }

        IReadOnlyList<FeatureInfo> Features { get; }

        /// <summary>Cells the snapshot frames are first sized for; frames grow with the colony (there is no cap).</summary>
        int InitialCellCapacity { get; }

        int MaxGroups { get; }

        int GroupCount { get; }

        /// <summary>Level of the evolutionary time bar being played (features unlock by level).</summary>
        int Level { get; set; }

        /// <summary>Detail <paramref name="detail"/> of <paramref name="cell"/> (SI).</summary>
        double ReadDetail(int cell, int detail);

        double ReadTrait(int cell, int trait);

        double ReadTraitTarget(int cell, int trait);

        int CellGroup(int cell);

        bool CellAlive(int cell);

        bool CellCanDivide(int cell);

        double ReadControl(int group, int control);

        bool FeatureUnlocked(int feature);

        bool FeatureEnabled(int feature);

        /// <summary>Sets the trait's target multiplier for every cell of the group (clamped).</summary>
        void SetTrait(int group, int trait, double multiplier);

        void SetControl(int group, int control, double value);

        void SetFeature(int feature, bool enabled);

        /// <summary>Divides the cell (spending glycogen to finish the daughter if needed).</summary>
        void Divide(int cell);
    }

    /// <summary>A world where the creature moves over the ground among food packets.</summary>
    public interface IFoodWorld
    {
        AdaptOrDeity.Sim.World.FoodField Food { get; }

        /// <summary>The creature's body: one rigid body (position, orientation, velocity, spin), its cells and flagella.</summary>
        AdaptOrDeity.Sim.World.CreatureBody Body { get; }

        /// <summary>Position of cell <paramref name="cell"/> over the ground [m].</summary>
        void CellPosition(int cell, out double x, out double y);

        /// <summary>Cell radius [m].</summary>
        double CellRadius { get; }
    }

    /// <summary>A world that can run a pass over its cells on worker threads (each cell's work independent).</summary>
    public interface IParallelWorld
    {
        /// <summary>Calls <paramref name="pass"/> over ranges of cells covering [0, CellCount), possibly in parallel.</summary>
        void ForEachCell(Action<int, int> pass);
    }
}
