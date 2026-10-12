using System;
using System.Collections.Generic;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>An achievement the level-end window shows (content/timeline.json achievements[]).</summary>
    public sealed class AchievementInfo
    {
        public AchievementInfo(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public string Id { get; }

        public string Name { get; }

        public string Description { get; }
    }

    /// <summary>The achievements, in content order; the world decides when each is earned (FermenterWorld.Achievements.cs).</summary>
    public static class GameAchievements
    {
        public const string FirstMeal = "first_meal";
        public const string SurvivedFamine = "survived_famine";
        public const string NeverStarved = "never_starved";
        public const string FirstDaughter = "first_daughter";
        public const string Grandchild = "grandchild";
        public const string FirstPrey = "first_prey";
        public const string TenPrey = "ten_prey";

        /// <summary>Every achievement the world knows how to award.</summary>
        public static readonly IReadOnlyList<string> Known =
            new[] { FirstMeal, SurvivedFamine, NeverStarved, FirstDaughter, Grandchild, FirstPrey, TenPrey };

        public static readonly IReadOnlyList<AchievementInfo> All = Load();

        public static int IndexOf(string id)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Id == id)
                    return i;
            return -1;
        }

        private static AchievementInfo[] Load()
        {
            var list = GameTimeline.Default.Achievements;
            var all = new AchievementInfo[list.Count];
            for (int i = 0; i < all.Length; i++)
                all[i] = new AchievementInfo(list[i].Id, list[i].Name, list[i].Description);
            return all;
        }
    }

    /// <summary>What the creature can see around it, counted once per individual per level.</summary>
    public enum SeenKind
    {
        /// <summary>Food packets (lying or carried).</summary>
        FoodPacket,

        /// <summary>Dead cells' bodies.</summary>
        DeadCell,

        /// <summary>Prey cells.</summary>
        PreyCell,

        /// <summary>The creature's own daughters living on their own.</summary>
        Daughter,

        /// <summary>Their descendants (generation 2 and later).</summary>
        Descendant,
    }

    /// <summary>
    /// Achievements and the "seen" record. Each achievement is earned once, in the level it happens (its level is kept);
    /// each level counts the individuals of each <see cref="SeenKind"/> that came into view (within the food window around
    /// the creature), each once per level.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        /// <summary>Kinds of things counted as seen (<see cref="SeenKind"/>).</summary>
        public static readonly int SeenKinds = Enum.GetValues(typeof(SeenKind)).Length;

        private readonly int[] _achievedLevel = new int[GameAchievements.All.Count];
        private readonly int[] _seen = new int[SeenKinds];
        private bool _famine;   // a gap without food long enough to unlock regulation, not yet ended

        /// <summary>The level an achievement was earned in, 0 if not yet.</summary>
        public int AchievedLevel(int achievement) => _achievedLevel[achievement];

        /// <summary>Individuals of a kind seen in this level so far.</summary>
        public int Seen(SeenKind kind) => _seen[(int)kind];

        private void Achieve(string id)
        {
            int a = GameAchievements.IndexOf(id);
            if (a >= 0 && _achievedLevel[a] == 0)
                _achievedLevel[a] = _level;
        }

        // After a step: what the creature did and saw.
        private void NoticeAchievementsAndSights(bool hadFood)
        {
            if (hadFood)
            {
                Achieve(GameAchievements.FirstMeal);
                if (_famine && StarvingShare < 1.0 && AnyAliveNotStarving())
                    Achieve(GameAchievements.SurvivedFamine);
                _famine = false;
            }
            else if (TimeWithoutFood >= _foodGapToUnlock)
                _famine = true;
            if (Swallowed >= 1)
                Achieve(GameAchievements.FirstPrey);
            if (Swallowed >= 10)
                Achieve(GameAchievements.TenPrey);
            if (_divisions > 0)
                Achieve(GameAchievements.FirstDaughter);
            if (Offspring > OwnOffspring)
                Achieve(GameAchievements.Grandchild);
            See();
        }

        private bool AnyAliveNotStarving()
        {
            foreach (var c in _cells)
                if (c.Alive && c.Atp >= _starvingAtp * c.RestingAtp)
                    return true;
            return false;
        }

        // Counts what is in view for the first time this level (a mark per individual: the level it was last counted in).
        private void See()
        {
            var b = _body.Body;
            double half = _food.HalfSize;
            var packets = _food.Packets;
            for (int k = 0; k < packets.Count; k++)
            {
                var p = packets[k];
                if (p.Active && p.SeenLevel != _level && InView(p.X, p.Y, b, half))
                {
                    p.SeenLevel = _level;
                    _seen[(int)(p.Kind == PacketKind.Body ? SeenKind.DeadCell : SeenKind.FoodPacket)]++;
                }
            }
            var prey = _prey.Prey;
            for (int k = 0; k < prey.Count; k++)
            {
                var p = prey[k];
                if (p.Active && p.SeenLevel != _level && InView(p.X, p.Y, b, half))
                {
                    p.SeenLevel = _level;
                    _seen[(int)SeenKind.PreyCell]++;
                }
            }
            foreach (var o in _organisms)
                if (o.SeenLevel != _level && InView(o.X, o.Y, b, half))
                {
                    o.SeenLevel = _level;
                    _seen[(int)(o.Generation == 1 ? SeenKind.Daughter : SeenKind.Descendant)]++;
                }
        }

        private static bool InView(double x, double y, RigidBody2D b, double half) =>
            Math.Abs(x - b.X) <= half && Math.Abs(y - b.Y) <= half;

        private int _divisions;   // the creature's cells' divisions so far
    }
}
