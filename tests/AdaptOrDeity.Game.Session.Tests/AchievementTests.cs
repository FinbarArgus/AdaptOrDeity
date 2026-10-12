using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// What the level-end window shows from the sim (FermenterWorld.Achievements.cs, .Scoring.cs): achievements earned
    /// once, in their level; the individuals seen in a level, each once; stars from the level's points; frames carry them.
    /// </summary>
    public class AchievementTests
    {
        private static int A(string id) => GameAchievements.IndexOf(id);

        // A world without food or prey arriving or lying, not swimming, its ocean rich enough that the cell stays at rest.
        private static FermenterWorld Quiet(int level = 1)
        {
            var world = new FermenterWorld { Workers = 1 };
            world.Level = level;
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            world.BathConcentration = 0.1;
            return world;
        }

        private static readonly System.Collections.Generic.Dictionary<FermenterWorld, long> Steps =
            new System.Collections.Generic.Dictionary<FermenterWorld, long>();

        private static void Run(FermenterWorld world, double seconds)
        {
            Steps.TryGetValue(world, out long done);
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++, done++)
                world.Step(done * world.MacroDt, world.MacroDt);
            Steps[world] = done;
        }

        private static FoodPacket PacketOnCell(FermenterWorld world)
        {
            world.CellPosition(0, out double x, out double y);
            var s = world.Food.Settings;
            return world.Food.AddBody(x + world.CellRadius, y, 0.0, 0.0, s.PacketRadius, s.PacketAmount,
                s.FreeFraction * s.PacketAmount, 0);
        }

        [Test]
        public void EveryContentAchievementIsAwardedByTheSim()
        {
            Assert.That(GameAchievements.All.Select(a => a.Id), Is.EquivalentTo(GameAchievements.Known));
            Assert.That(GameAchievements.All.All(a => a.Name.Length > 0 && a.Description.Length > 0), Is.True);
        }

        [Test]
        public void AFirstMealAfterAFamineWhileNotStarving()
        {
            using var world = Quiet();
            double gap = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.Regulation)].RequiresFoodGap;
            Run(world, gap + 1.0);
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstMeal)), Is.EqualTo(0));
            Assert.That(world.AchievedLevel(A(GameAchievements.SurvivedFamine)), Is.EqualTo(0), "not until food comes back");
            PacketOnCell(world);
            Run(world, 0.2);
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstMeal)), Is.EqualTo(1));
            Assert.That(world.AchievedLevel(A(GameAchievements.SurvivedFamine)), Is.EqualTo(1));
            // earned once: the level they were earned in stays
            world.Level = 2;
            Run(world, 0.2);
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstMeal)), Is.EqualTo(1));
        }

        [Test]
        public void AFamineEndedWhileStarvingIsNotSurvived()
        {
            using var world = Quiet();
            world.BathConcentration = 0.0;   // the cell starves within minutes
            Run(world, 600.0);
            Assert.That(world.StarvingShare, Is.EqualTo(1.0));
            PacketOnCell(world);
            Run(world, 0.2);
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstMeal)), Is.EqualTo(1));
            Assert.That(world.AchievedLevel(A(GameAchievements.SurvivedFamine)), Is.EqualTo(0));
        }

        [Test]
        public void DaughtersPreyAndAFullLevel()
        {
            using var world = Quiet(level: 2);
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.AutoDivide), true);
            world.SetControl(0, FermenterWorld.GrowthControl, 3.0);
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            Run(world, 0.1);
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstDaughter)), Is.EqualTo(2));
            Assert.That(world.AchievedLevel(A(GameAchievements.Grandchild)), Is.EqualTo(0));
            var o = world.Organisms[0];
            o.Cell.SetState("bio__P_growth", o.Cell.DaughterCost);
            Run(world, 0.2);
            Assert.That(world.Offspring, Is.GreaterThan(world.OwnOffspring));
            Assert.That(world.AchievedLevel(A(GameAchievements.Grandchild)), Is.EqualTo(2));

            world.CellPosition(0, out double x, out double y);
            world.Prey.Add(x, y, 0.0);
            Run(world, 0.2);
            Assert.That(world.Swallowed, Is.EqualTo(1));
            Assert.That(world.AchievedLevel(A(GameAchievements.FirstPrey)), Is.EqualTo(2));
            Assert.That(world.AchievedLevel(A(GameAchievements.TenPrey)), Is.EqualTo(0));
            typeof(FermenterWorld).GetProperty(nameof(FermenterWorld.Swallowed))!.SetValue(world, 10);
            Run(world, 0.1);
            Assert.That(world.AchievedLevel(A(GameAchievements.TenPrey)), Is.EqualTo(2));

            Assert.That(world.SurvivalMeter, Is.EqualTo(100.0));
            world.DevEndLevel();
            Assert.That(world.AchievedLevel(A(GameAchievements.NeverStarved)), Is.EqualTo(2), "a level ended with a full meter");
        }

        [Test]
        public void EachIndividualInViewIsSeenOncePerLevel()
        {
            using var world = new FermenterWorld { Workers = 1 };
            world.Food.SetArrivals(0.0, 0.0);
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            world.Step(0.0, world.MacroDt);
            var b = world.Body.Body;
            double half = world.Food.HalfSize;
            int inView = world.Food.Packets.Count(p => p.Active && Math.Abs(p.X - b.X) <= half && Math.Abs(p.Y - b.Y) <= half);
            Assert.That(inView, Is.GreaterThan(10), "the lying food");
            Assert.That(world.Seen(SeenKind.FoodPacket), Is.EqualTo(inView));
            world.Step(0.1, world.MacroDt);
            Assert.That(world.Seen(SeenKind.FoodPacket), Is.EqualTo(inView), "each once");
            // a prey cell and a dead body come into view
            world.Prey.Add(b.X + 2e-6, b.Y + 2e-6, 0.0);
            world.Food.AddBody(b.X - 5e-6, b.Y, 0.0, 0.0, world.CellRadius, 1e-15, 0.0, 0);
            world.Step(0.2, world.MacroDt);
            Assert.That((world.Seen(SeenKind.PreyCell), world.Seen(SeenKind.DeadCell)), Is.EqualTo((1, 1)));
            // a new level counts afresh; the finished level's record is in its score
            world.DevEndLevel();
            Assert.That(world.LastScore!.Seen[(int)SeenKind.FoodPacket], Is.EqualTo(inView));
            Assert.That(world.LastScore.Seen[(int)SeenKind.PreyCell], Is.EqualTo(1));
            Assert.That(world.Seen(SeenKind.FoodPacket), Is.EqualTo(0));
            world.Step(0.3, world.MacroDt);
            Assert.That(world.Seen(SeenKind.FoodPacket), Is.GreaterThan(10));
        }

        [Test]
        public void StarsCountTheThresholdsTheLevelsPointsReach()
        {
            using var world = Quiet();
            int one = GameParameters.Default.Integer("scoring.star_points_1"), two = GameParameters.Default.Integer("scoring.star_points_2"),
                three = GameParameters.Default.Integer("scoring.star_points_3");
            Assert.That(new[] { world.Stars(one - 1), world.Stars(one), world.Stars(two), world.Stars(three), world.Stars(1000) },
                Is.EqualTo(new[] { 0, 1, 2, 3, 3 }));
            Run(world, 1.0);
            Assert.That(world.StarsNow, Is.EqualTo(world.Stars(world.LevelPointsNow)));
            int points = world.LevelPointsNow;
            world.DevEndLevel();
            var score = world.LastScore!;
            Assert.That((score.Points, score.Stars), Is.EqualTo((points, world.Stars(points))));
            Assert.That(score.Unlocked.Count(u => u), Is.EqualTo(0), "nothing unlocked during it");
        }

        [Test]
        public void WhatIsUnlockedDuringALevelIsInItsScore()
        {
            using var world = Quiet();
            double gap = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.Regulation)].RequiresFoodGap;
            Run(world, gap + 1.0);
            world.DevEndLevel();
            Assert.That(world.LastScore!.Unlocked[GameFeatures.IndexOf(GameFeatures.Regulation)], Is.True);
            Assert.That(world.LastScore.Unlocked.Count(u => u), Is.EqualTo(1));
        }

        [Test]
        public void FramesCarryTheRecord()
        {
            var world = Quiet();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            PacketOnCell(world);
            loop.StepExactly(2);
            world.DevEndLevel();
            loop.StepExactly(1);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.Achieved[A(GameAchievements.FirstMeal)], Is.EqualTo(1));
            Assert.That(frame.ScoredLevel, Is.EqualTo(1));
            Assert.That((frame.ScoredStars, frame.ScoredReproductionPoints), Is.EqualTo((world.LastScore!.Stars, 0)));
            Assert.That(frame.ScoredSeen, Is.EqualTo(world.LastScore.Seen.ToArray()));
            Assert.That(frame.ScoredSeen[(int)SeenKind.DeadCell], Is.EqualTo(1));
            Assert.That((frame.LevelPointsNow, frame.StarsNow), Is.EqualTo((world.LevelPointsNow, world.StarsNow)));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.Achieved, Is.EqualTo(frame.Achieved));
            Assert.That(back.Seen, Is.EqualTo(frame.Seen));
            Assert.That(back.ScoredSeen, Is.EqualTo(frame.ScoredSeen));
            Assert.That(back.ScoredUnlocked, Is.EqualTo(frame.ScoredUnlocked));
            Assert.That((back.ScoredStars, back.LevelPointsNow, back.StarsNow, back.ScoredReproductionPoints),
                Is.EqualTo((frame.ScoredStars, frame.LevelPointsNow, frame.StarsNow, frame.ScoredReproductionPoints)));
            world.Dispose();
        }
    }
}
