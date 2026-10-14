using System;
using System.Collections.Generic;
using AdaptOrDeity.Game.Content;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A finished level: its meters and the skill points they converted to, the reproduction points earned in it, its
    /// stars, what the creature saw and what was unlocked during it.
    /// </summary>
    public sealed class LevelScore
    {
        public LevelScore(int level, double survival, double damage, int survivalPoints, int damagePoints,
            int reproductionPoints, int stars, int[] seen, bool[] unlocked)
        {
            Level = level;
            Survival = survival;
            Damage = damage;
            SurvivalPoints = survivalPoints;
            DamagePoints = damagePoints;
            ReproductionPoints = reproductionPoints;
            Stars = stars;
            Seen = seen;
            Unlocked = unlocked;
        }

        /// <summary>Reproduction points earned in the level.</summary>
        public int ReproductionPoints { get; }

        /// <summary>All the points the level earned: its meters' and its reproduction points.</summary>
        public int Points => SurvivalPoints + DamagePoints + ReproductionPoints;

        /// <summary>0-3: how many of the content thresholds (scoring.star_points_1..3) its points reached.</summary>
        public int Stars { get; }

        /// <summary>Per <see cref="SeenKind"/>: individuals seen in the level.</summary>
        public IReadOnlyList<int> Seen { get; }

        /// <summary>Per feature (<see cref="GameFeatures.All"/>): unlocked during the level (bought, or met its condition).</summary>
        public IReadOnlyList<bool> Unlocked { get; }

        public int Level { get; }

        /// <summary>The survival and damage meters at the level's end (0-100).</summary>
        public double Survival { get; }

        public double Damage { get; }

        /// <summary>The points each meter converted to.</summary>
        public int SurvivalPoints { get; }

        public int DamagePoints { get; }
    }

    /// <summary>
    /// Skill points from surviving: each level has a survival meter and a damage meter, both starting at 100. The survival
    /// meter falls while the creature is starving: at 100 / (scoring.starvation_to_empty level durations) per second times
    /// the share of its living cells whose ATP is below scoring.starving_atp_fraction of their resting level (all of it
    /// once no cell lives), so a creature starving for that long empties it. The damage meter is 100 times one less the
    /// worst damage load the creature has had in the level (the mean over its cells, a dead cell counting as a lethal load):
    /// repair does not refill it. When the level's time runs out each meter converts to points, in proportion, rounded:
    /// scoring.survival_points and scoring.damage_points at 100. Food eaten earns nothing by itself; it is what keeps the
    /// creature from starving and lets it build proteins and daughters. The level's points (its meters' and the
    /// reproduction points earned in it) give it 0-3 stars at the thresholds scoring.star_points_1..3.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _starvingAtp = GameParameters.Default.Value("scoring.starving_atp_fraction");
        private readonly double _starvationToEmpty = GameParameters.Default.Value("scoring.starvation_to_empty");
        private readonly int _survivalPointsMax = GameParameters.Default.Integer("scoring.survival_points");
        private readonly int _damagePointsMax = GameParameters.Default.Integer("scoring.damage_points");
        private readonly int[] _starPoints =
        {
            GameParameters.Default.Integer("scoring.star_points_1"), GameParameters.Default.Integer("scoring.star_points_2"),
            GameParameters.Default.Integer("scoring.star_points_3"),
        };
        private int _reproductionAtLevelStart;
        private bool[]? _unlockedAtLevelStart;

        /// <summary>Points the level has earned so far if it ended now: its meters' worth and its reproduction points.</summary>
        public int LevelPointsNow => SurvivalPointsNow + DamagePointsNow + ReproductionPoints - _reproductionAtLevelStart;

        /// <summary>The stars <see cref="LevelPointsNow"/> would give.</summary>
        public int StarsNow => Stars(LevelPointsNow);

        /// <summary>Stars for a level's points: one per threshold reached (content scoring.star_points_1..3).</summary>
        public int Stars(int points)
        {
            int stars = 0;
            foreach (int threshold in _starPoints)
                if (points >= threshold)
                    stars++;
            return stars;
        }

        /// <summary>This level's survival meter (100 at its start; falls while the creature starves).</summary>
        public double SurvivalMeter { get; private set; } = 100.0;

        /// <summary>This level's damage meter (100 at its start; 100 x (1 - the worst mean damage load so far)).</summary>
        public double DamageMeter { get; private set; } = 100.0;

        /// <summary>Share of the creature's living cells starving now (ATP below the starving fraction of rest; 1 if none lives).</summary>
        public double StarvingShare { get; private set; }

        /// <summary>Points the survival and damage meters earned in finished levels (spent or not).</summary>
        public int SurvivalPoints { get; private set; }

        public int DamagePoints { get; private set; }

        /// <summary>What the meters are worth now: the points they would convert to if the level ended this step.</summary>
        public int SurvivalPointsNow => MeterPoints(SurvivalMeter, _survivalPointsMax);

        public int DamagePointsNow => MeterPoints(DamageMeter, _damagePointsMax);

        /// <summary>The last finished level's score, or null before the first level ends.</summary>
        public LevelScore? LastScore { get; private set; }

        private static int MeterPoints(double meter, int atFull) =>
            (int)Math.Round(atFull * meter / 100.0, MidpointRounding.AwayFromZero);

        // The meters after a step: starvation drains the survival meter; the damage meter follows the worst load.
        private void UpdateMeters(double dt)
        {
            int living = 0, starving = 0;
            double damage = 0.0;
            foreach (var c in _cells)
            {
                if (!c.Alive)
                {
                    damage += 1.0;
                    continue;
                }
                living++;
                if (c.Atp < _starvingAtp * c.RestingAtp)
                    starving++;
                damage += Math.Min(1.0, Math.Max(0.0, c.Damage));
            }
            StarvingShare = living > 0 ? (double)starving / living : 1.0;
            double empty = _starvationToEmpty * GameTimeline.Default.DurationOf(_level);
            SurvivalMeter = Math.Max(0.0, SurvivalMeter - 100.0 * StarvingShare * dt / empty);
            DamageMeter = Math.Min(DamageMeter, 100.0 * (1.0 - damage / _cells.Count));
        }

        // The level's time ran out: each meter converts to points, and the level is scored.
        private void ScoreLevel()
        {
            if (SurvivalMeter >= 100.0)
                Achieve(GameAchievements.NeverStarved);
            int survival = SurvivalPointsNow, damage = DamagePointsNow;
            int reproduction = ReproductionPoints - _reproductionAtLevelStart;
            SurvivalPoints += survival;
            DamagePoints += damage;
            var unlocked = new bool[GameFeatures.All.Count];
            for (int f = 0; f < unlocked.Length; f++)
                unlocked[f] = FeatureUnlocked(f) && !(_unlockedAtLevelStart?[f] ?? false);
            LastScore = new LevelScore(_level, SurvivalMeter, DamageMeter, survival, damage, reproduction,
                Stars(survival + damage + reproduction), (int[])_seen.Clone(), unlocked);
        }

        // A level starts: full meters, nothing seen, and what is unlocked as it starts.
        private void ResetMeters()
        {
            SurvivalMeter = DamageMeter = 100.0;
            _reproductionAtLevelStart = ReproductionPoints;
            Array.Clear(_seen, 0, _seen.Length);
            _unlockedAtLevelStart ??= new bool[GameFeatures.All.Count];
            for (int f = 0; f < _unlockedAtLevelStart.Length; f++)
                _unlockedAtLevelStart[f] = FeatureUnlocked(f);
        }
    }
}
