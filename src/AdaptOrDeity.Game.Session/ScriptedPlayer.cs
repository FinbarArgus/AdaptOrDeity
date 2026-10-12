using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>When a scripted player lets the creature swim.</summary>
    public enum SwimPolicy
    {
        /// <summary>As the game starts: the motor runs at random all the time.</summary>
        Always,

        /// <summary>Swimming off: the creature drifts with the current.</summary>
        Never,

        /// <summary>Off while the creature touches or holds food, on while it has none (it stays on food, looks for more).</summary>
        WithoutFood,
    }

    /// <summary>
    /// A scripted player for calibrating the game (docs/plans/scoring.md): a deterministic strategy applied to a world each
    /// step. It sets trait targets and controls at the start, switches regulation on when it unlocks, and sets swimming by
    /// its policy. The bots in <see cref="Bots"/> are the calibration's reference players. Not part of play.
    /// </summary>
    public sealed class ScriptedPlayer
    {
        public ScriptedPlayer(string name) => Name = name;

        public string Name { get; }

        /// <summary>Trait targets set at the start (trait id → value: a multiplier, a count or a share).</summary>
        public Dictionary<string, double> Traits { get; } = new Dictionary<string, double>();

        /// <summary>Controls set at the start (control id → value).</summary>
        public Dictionary<string, double> Controls { get; } = new Dictionary<string, double>();

        /// <summary>
        /// Regulation switched on once it unlocks: (target, species), target a trait id or "growth" (or another regulated
        /// target's id), species ATP, G6P, glc or H. Targets the cell does not have are skipped.
        /// </summary>
        public List<(string Target, string Species)> Regulation { get; } = new List<(string, string)>();

        public SwimPolicy Swim { get; set; } = SwimPolicy.Always;

        private bool _started, _regulated;

        /// <summary>Applies the strategy before the world's next step.</summary>
        public void Act(FermenterWorld world)
        {
            if (!_started)
            {
                _started = true;
                foreach (var (id, value) in Traits)
                    world.SetTrait(0, TraitIndex(world, id), value);
                foreach (var (id, value) in Controls)
                    world.SetControl(0, world.Controls.ToList().FindIndex(c => c.Id == id), value);
            }
            int regulation = GameFeatures.IndexOf(GameFeatures.Regulation);
            if (!_regulated && world.FeatureUnlocked(regulation) && world.FeatureEnabled(regulation))
            {
                _regulated = true;
                foreach (var (target, species) in Regulation)
                {
                    int t = RegulationTarget(world, target);
                    if (t >= 0)
                        world.SetRegulation(0, t, Array.IndexOf(AdaptOrDeity.Sim.Physiology.FermenterCell.RegulationSpecies, species), true);
                }
            }
            int swimming = GameFeatures.IndexOf(GameFeatures.Swimming);
            bool swim = Swim == SwimPolicy.Always || (Swim == SwimPolicy.WithoutFood && world.TimeWithoutFood > 0.0);
            if (world.FeatureUnlocked(swimming) && world.FeatureEnabled(swimming) != swim)
                world.SetFeature(swimming, swim);
        }

        private static int TraitIndex(FermenterWorld world, string id)
        {
            int t = world.Traits.ToList().FindIndex(x => x.Id == id);
            return t >= 0 ? t : throw new ArgumentException($"no trait {id}");
        }

        /// <summary>The regulated target with this id (a trait's, or "growth"), or -1.</summary>
        public static int RegulationTarget(FermenterWorld world, string id) =>
            id == "growth" ? world.GrowthRegulation : world.Traits.ToList().FindIndex(x => x.Id == id);

        /// <summary>The strategy in one line (for logs and notes).</summary>
        public override string ToString() =>
            string.Join(" ", new[] { Name, "swim=" + Swim }
                .Concat(Traits.Select(p => FormattableString.Invariant($"{p.Key}={p.Value:G3}")))
                .Concat(Controls.Select(p => FormattableString.Invariant($"{p.Key}={p.Value:G3}")))
                .Concat(Regulation.Select(r => $"reg:{r.Target}<-{r.Species}")));

        /// <summary>
        /// Parses "name key=value ...": swim=Always|Never|WithoutFood, a trait or control id = value, reg=target:species.
        /// </summary>
        public static ScriptedPlayer Parse(string name, IEnumerable<string> settings)
        {
            var p = new ScriptedPlayer(name);
            foreach (var s in settings)
            {
                var kv = s.Split('=');
                if (kv[0] == "swim")
                    p.Swim = (SwimPolicy)Enum.Parse(typeof(SwimPolicy), kv[1]);
                else if (kv[0] == "reg")
                {
                    var ts = kv[1].Split(':');
                    p.Regulation.Add((ts[0], ts[1]));
                }
                else if (Array.Exists(new[] { "growth", "storage", "repair", "matrix" }, c => c == kv[0]))
                    p.Controls[kv[0]] = double.Parse(kv[1], CultureInfo.InvariantCulture);
                else
                    p.Traits[kv[0]] = double.Parse(kv[1], CultureInfo.InvariantCulture);
            }
            return p;
        }
    }

    /// <summary>The calibration's reference players (docs/plans/scoring.md).</summary>
    public static class Bots
    {
        /// <summary>Regulation a mediocre player turns on: falling ATP lowers the pumps and maintenance.</summary>
        public static readonly (string, string)[] SavingRegulation =
        {
            ("H_ATPase_expression", "ATP"), ("maintenance", "ATP"),
        };

        /// <summary>Touches nothing.</summary>
        public static ScriptedPlayer LeftAlone() => new ScriptedPlayer("left alone");

        /// <summary>Turns regulation on when it unlocks (ATP lowers the pumps and maintenance); otherwise the defaults.</summary>
        public static ScriptedPlayer Mediocre()
        {
            var p = new ScriptedPlayer("mediocre");
            p.Regulation.AddRange(SavingRegulation);
            return p;
        }

        /// <summary>
        /// The mediocre player's regulation, plus sensible expression: more glucose carrier and surface hydrolase to eat
        /// faster while on food, swimming only while it has no food.
        /// </summary>
        public static ScriptedPlayer Great()
        {
            var p = Mediocre();
            p = Rename(p, "great");
            p.Traits["Glf_expression"] = 2.0;
            p.Traits["Hydrolase_expression"] = 2.0;
            p.Swim = SwimPolicy.WithoutFood;
            return p;
        }

        private static ScriptedPlayer Rename(ScriptedPlayer from, string name)
        {
            var p = new ScriptedPlayer(name) { Swim = from.Swim };
            foreach (var t in from.Traits)
                p.Traits[t.Key] = t.Value;
            foreach (var c in from.Controls)
                p.Controls[c.Key] = c.Value;
            p.Regulation.AddRange(from.Regulation);
            return p;
        }
    }

    /// <summary>What a scripted player's level came to.</summary>
    public sealed class BotOutcome
    {
        public string Player { get; set; } = "";

        public LevelScore? Score { get; set; }

        /// <summary>When regulation unlocked, and when the creature started starving for the last time (NaN: never) [s].</summary>
        public double RegulationAt { get; set; } = double.NaN;

        public double StarvingFrom { get; set; } = double.NaN;

        /// <summary>Simulated time spent starving in the level [s].</summary>
        public double TimeStarving { get; set; }

        public bool Alive { get; set; }

        public double FoodEaten { get; set; }

        public double Seconds { get; set; }

        public override string ToString() => FormattableString.Invariant(
            $"{Player}: points {Score?.Points} (survival {Score?.Survival:F1} -> {Score?.SurvivalPoints}, damage {Score?.Damage:F1} -> {Score?.DamagePoints}, reproduction {Score?.ReproductionPoints}), stars {Score?.Stars}; starving {TimeStarving / 86400:F2} d (last from day {StarvingFrom / 86400:F2}), regulation at {RegulationAt:F0} s, alive {Alive}, ate {FoodEaten:G3} mol; {Seconds:F0} s to run");

        /// <summary>
        /// Plays <paramref name="player"/> through the world's current level until the next begins (one level), from a fresh
        /// world unless one is given.
        /// </summary>
        public static BotOutcome Play(ScriptedPlayer player, FermenterWorld? world = null, int workers = 1)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            bool own = world == null;
            world ??= new FermenterWorld { Workers = workers };
            try
            {
                int level = world.Level;
                double t = 0.0, dt = world.MacroDt, starving = 0.0, from = double.NaN;
                while (world.Level == level)
                {
                    player.Act(world);
                    world.Step(t, dt);
                    t += dt;
                    if (world.Level != level)
                        break;
                    if (world.StarvingShare > 0.0)
                    {
                        starving += world.StarvingShare * dt;
                        if (double.IsNaN(from))
                            from = t;
                    }
                    else
                        from = double.NaN;
                }
                return new BotOutcome
                {
                    Player = player.ToString(), Score = world.LastScore, RegulationAt = world.FoodGapMetAt,
                    StarvingFrom = from, TimeStarving = starving, Alive = world.CellAlive(0), FoodEaten = world.FoodEaten,
                    Seconds = clock.Elapsed.TotalSeconds,
                };
            }
            finally
            {
                if (own)
                    world.Dispose();
            }
        }
    }
}
