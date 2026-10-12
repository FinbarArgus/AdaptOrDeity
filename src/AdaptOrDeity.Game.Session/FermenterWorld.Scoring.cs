using System;
using AdaptOrDeity.Game.Content;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>A finished level's meters and the skill points they converted to.</summary>
    public sealed class LevelScore
    {
        public LevelScore(int level, double survival, double damage, int survivalPoints, int damagePoints)
        {
            Level = level;
            Survival = survival;
            Damage = damage;
            SurvivalPoints = survivalPoints;
            DamagePoints = damagePoints;
        }

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
    /// creature from starving and lets it build proteins and daughters.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _starvingAtp = GameParameters.Default.Value("scoring.starving_atp_fraction");
        private readonly double _starvationToEmpty = GameParameters.Default.Value("scoring.starvation_to_empty");
        private readonly int _survivalPointsMax = GameParameters.Default.Integer("scoring.survival_points");
        private readonly int _damagePointsMax = GameParameters.Default.Integer("scoring.damage_points");

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

        // The level's time ran out: each meter converts to points.
        private void ScoreLevel()
        {
            int survival = SurvivalPointsNow, damage = DamagePointsNow;
            SurvivalPoints += survival;
            DamagePoints += damage;
            LastScore = new LevelScore(_level, SurvivalMeter, DamageMeter, survival, damage);
        }

        private void ResetMeters()
        {
            SurvivalMeter = DamageMeter = 100.0;
        }
    }
}
