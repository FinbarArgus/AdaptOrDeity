using System;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Regulation by falling metabolites (feature <see cref="GameFeatures.Regulation"/>): per group, per regulated target
    /// (each trait's protein, then growth, then the maintenance: <see cref="FermenterCell.MaintenanceRegulation"/>) and per
    /// species (<see cref="FermenterCell.RegulationSpecies"/>), whether the species' fall lowers that target. The cells'
    /// kernel does the regulating (upstream cell/expression_regulation: the target times f for each species switched on, f
    /// = 1 at and above the species' resting level); the world keeps each group's switches and pushes them
    /// to its cells, all off while the feature is locked or turned off. Daughters and released organisms copy their
    /// parent's switches with its other settings. The feature unlocks when a cell of the creature first has its ATP below
    /// <see cref="FeatureInfo.RequiresAtpBelow"/> of the resting level.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _atpDipFraction = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.Regulation)].RequiresAtpBelow;

        /// <summary>True once a cell of the creature has had its ATP below the fraction of rest that unlocks regulation.</summary>
        public bool AtpDipped { get; private set; }

        /// <summary>
        /// Regulated targets: each trait (its index), then growth (<see cref="GrowthRegulation"/>), then the maintenance
        /// (<see cref="MaintenanceRegulation"/>).
        /// </summary>
        public int RegulationTargets => _traits.Length + 1 + FermenterCell.MaintenanceRegulation.Length;

        /// <summary>The regulated target that is growth.</summary>
        public int GrowthRegulation => _traits.Length;

        /// <summary>The regulated target that is maintenance entry <paramref name="k"/> (<see cref="FermenterCell.MaintenanceRegulation"/>).</summary>
        public int MaintenanceRegulation(int k) => _traits.Length + 1 + k;

        /// <summary>Whether a fall of <paramref name="species"/> lowers <paramref name="target"/> in the group's cells.</summary>
        public bool ReadRegulation(int group, int target, int species) =>
            _groups[CheckGroup(group)].Regulation[RegulationSlot(target, species)];

        /// <summary>
        /// Switches regulation of <paramref name="target"/> (a trait, or <see cref="GrowthRegulation"/>) by the fall of
        /// <paramref name="species"/> on or off for every cell of the group. Needs the Regulation feature.
        /// </summary>
        public void SetRegulation(int group, int target, int species, bool on)
        {
            CheckGroup(group);
            int slot = RegulationSlot(target, species);
            if (!Active(GameFeatures.Regulation))
                throw new InvalidOperationException(FeatureUnlocked(GameFeatures.IndexOf(GameFeatures.Regulation))
                    ? "Regulation is turned off"
                    : "Regulation unlocks when a cell's ATP first falls below " +
                      $"{100.0 * _atpDipFraction:F0}% of its resting level");
            _groups[group].Regulation[slot] = on;
            ApplyControls();
        }

        private int RegulationSlot(int target, int species)
        {
            if (target < 0 || target >= RegulationTargets)
                throw new ArgumentOutOfRangeException(nameof(target), $"no regulated target {target}");
            if (species < 0 || species >= FermenterCell.RegulationSpecies.Length)
                throw new ArgumentOutOfRangeException(nameof(species), $"no regulating species {species}");
            return target * FermenterCell.RegulationSpecies.Length + species;
        }

        private static void ApplyRegulation(FermenterCell c, Group g, bool active)
        {
            int n = FermenterCell.RegulationSpecies.Length;
            for (int target = 0; target < c.RegulationTargets; target++)
                for (int s = 0; s < n; s++)
                    c.SetRegulated(target, s, active && g.Regulation[target * n + s]);
        }

        // Unlocks regulation the first time a living cell of the creature has its ATP below the fraction of rest.
        private void NoticeAtpDip()
        {
            if (AtpDipped)
                return;
            foreach (var c in _cells)
                if (c.Alive && c.Atp < _atpDipFraction * c.RestingAtp)
                {
                    AtpDipped = true;
                    ApplyControls();
                    return;
                }
        }
    }
}
