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

    /// <summary>A world whose cells can be inspected in the cell panel and adapted through traits.</summary>
    public interface IInspectableWorld
    {
        IReadOnlyList<DetailInfo> Details { get; }

        IReadOnlyList<TraitInfo> Traits { get; }

        /// <summary>Detail <paramref name="detail"/> of <paramref name="cell"/> (SI).</summary>
        double ReadDetail(int cell, int detail);

        /// <summary>Current multiplier of a trait on the cell's baseline.</summary>
        double ReadTrait(int cell, int trait);

        /// <summary>The multiplier the cell is adapting towards.</summary>
        double ReadTraitTarget(int cell, int trait);

        /// <summary>Sets the trait's target multiplier (clamped to its range).</summary>
        void SetTrait(int cell, int trait, double multiplier);
    }
}
