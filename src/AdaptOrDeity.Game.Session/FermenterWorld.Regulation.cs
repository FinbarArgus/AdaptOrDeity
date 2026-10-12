using System;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Regulation by falling metabolites (feature <see cref="GameFeatures.Regulation"/>): per group, per regulated target
    /// (each trait's protein, then growth) and per species (<see cref="FermenterCell.RegulationSpecies"/>), whether the
    /// species' fall lowers that target. The cells' kernel does the regulating (upstream cell/expression_regulation: the
    /// target times f = x^4/(1 + x^4) for each species switched on); the world keeps each group's switches and pushes them
    /// to its cells, all off while the feature is locked or turned off. Daughters and released organisms copy their
    /// parent's switches with its other settings. The feature unlocks when the creature first goes
    /// <see cref="FeatureInfo.RequiresFoodGap"/> without food: no living cell touching a packet, holding one, or with food in
    /// its vacuole.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _foodGapToUnlock = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.Regulation)].RequiresFoodGap;

        /// <summary>
        /// Time since the creature last touched or held food [s]: no living cell touching a packet, no packet held by the
        /// creature, no food in a vacuole. Counts from the start of play until the first food.
        /// </summary>
        public double TimeWithoutFood { get; private set; }

        /// <summary>True once the creature has gone the gap that unlocks regulation without food.</summary>
        public bool FoodGapMet { get; private set; }

        /// <summary>When <see cref="FoodGapMet"/> became true [s of simulated time], or NaN before.</summary>
        public double FoodGapMetAt { get; private set; } = double.NaN;

        /// <summary>Regulated targets: each trait (its index), then growth (<see cref="GrowthRegulation"/>).</summary>
        public int RegulationTargets => _traits.Length + 1;

        /// <summary>The regulated target that is growth.</summary>
        public int GrowthRegulation => _traits.Length;

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
                    : $"Regulation unlocks when the creature first goes {_foodGapToUnlock:F0} s without food");
            _groups[group].Regulation[slot] = on;
            ApplyControls();
        }

        private int RegulationSlot(int target, int species)
        {
            if (target < 0 || target > _traits.Length)
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

        // The gap since the creature last had food; regulation unlocks the first time it reaches the feature's gap.
        private void NoticeFoodGap(double dt)
        {
            TimeWithoutFood = HasFood() ? 0.0 : TimeWithoutFood + dt;
            if (FoodGapMet || TimeWithoutFood < _foodGapToUnlock)
                return;
            FoodGapMet = true;
            FoodGapMetAt = _time;
            ApplyControls();
        }

        // Whether a living cell of the creature touches a packet or holds food in its vacuole, or the creature holds a packet.
        private bool HasFood()
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i].Alive && (_touching[i] || _vacuoles[i] != null))
                    return true;
            return _food.Holds(0);
        }
    }
}
