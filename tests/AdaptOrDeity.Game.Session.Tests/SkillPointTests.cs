using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Skill points from surviving (FermenterWorld.Scoring.cs): each level's survival meter falls while the creature
    /// starves and its damage meter follows the worst damage load; at the level's end each converts to points. Food eaten
    /// is a statistic and earns nothing by itself; points buy features from their level.
    /// </summary>
    public class SkillPointTests
    {
        private static readonly int Pts = GameFeatures.IndexOf(GameFeatures.PtsTransport);

        private static int SurvivalAtFull => GameParameters.Default.Integer("scoring.survival_points");

        private static int DamageAtFull => GameParameters.Default.Integer("scoring.damage_points");

        // A world without food arriving or lying, its ocean at the given glucose [mol/m³], not swimming.
        private static FermenterWorld Quiet(double ocean)
        {
            var world = new FermenterWorld { Workers = 1 };
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            world.BathConcentration = ocean;
            return world;
        }

        private static void Run(FermenterWorld world, double from, double seconds)
        {
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
                world.Step(from + k * world.MacroDt, world.MacroDt);
        }

        [Test]
        public void AFedUndamagedLevelConvertsBothFullMetersToPoints()
        {
            using var world = Quiet(0.1);   // a rich sea: the cell stays at rest
            Run(world, 0.0, 600.0);
            Assert.That((world.SurvivalMeter, world.StarvingShare), Is.EqualTo((100.0, 0.0)));
            // a cell at rest carries a little damage, which its repair holds steady: the level adds next to none
            Assert.That(world.DamageMeter, Is.GreaterThan(98.0).And.LessThanOrEqualTo(100.0));
            Assert.That((world.SurvivalPointsNow, world.DamagePointsNow), Is.EqualTo((SurvivalAtFull, DamageAtFull)));
            Assert.That(world.SkillPoints, Is.EqualTo(0), "nothing until the level ends");
            world.DevEndLevel();
            Assert.That(world.Level, Is.EqualTo(2));
            Assert.That((world.SurvivalPoints, world.DamagePoints), Is.EqualTo((SurvivalAtFull, DamageAtFull)));
            Assert.That(world.SkillPoints, Is.EqualTo(SurvivalAtFull + DamageAtFull));
            var score = world.LastScore!;
            Assert.That((score.Level, score.Survival, score.SurvivalPoints, score.DamagePoints),
                Is.EqualTo((1, 100.0, SurvivalAtFull, DamageAtFull)));
            Assert.That(score.Damage, Is.GreaterThan(98.0));
            Assert.That((world.SurvivalMeter, world.DamageMeter), Is.EqualTo((100.0, 100.0)), "a new level starts full");
        }

        [Test]
        public void TheSurvivalMeterFallsWhileTheCreatureStarves()
        {
            using var world = Quiet(0.0);   // no glucose at all: ATP runs out within minutes
            double starving = 0.0, t = 0.0, dt = world.MacroDt;
            double fraction = GameParameters.Default.Value("scoring.starving_atp_fraction");
            double empty = GameParameters.Default.Value("scoring.starvation_to_empty") * GameTimeline.Default.DurationOf(1);
            for (int k = 0; k < 6000; k++, t += dt)
            {
                world.Step(t, dt);
                var c = world.Cell(0);
                double share = c.Alive ? (c.Atp < fraction * c.RestingAtp ? 1.0 : 0.0) : 1.0;
                Assert.That(world.StarvingShare, Is.EqualTo(share));
                starving += share * dt;
            }
            Assert.That(starving, Is.GreaterThan(300.0), "starving most of the 10 minutes");
            Assert.That(world.SurvivalMeter, Is.EqualTo(100.0 * (1.0 - starving / empty)).Within(1e-9));
            Assert.That(world.SurvivalMeter, Is.LessThan(100.0));
        }

        [Test]
        public void TheDamageMeterFollowsTheWorstLoadAndPointsAreInProportion()
        {
            // the meter follows the damage the level has added (worst so far), emptying at scoring.damage_to_empty of a
            // lethal load
            using var world = Quiet(0.1);
            double start = world.Cell(0).Damage, empty = GameParameters.Default.Value("scoring.damage_to_empty");
            world.Cell(0).SetState("bio__D_ribo", start + 0.4 * empty);
            Run(world, 0.0, 0.1);
            double worst = 100.0 * (1.0 - (world.Cell(0).Damage - start) / empty);
            Assert.That(worst, Is.InRange(50.0, 70.0));
            Assert.That(world.DamageMeter, Is.EqualTo(worst).Within(1e-9));
            world.Cell(0).SetState("bio__D_ribo", start);
            Run(world, 0.1, 0.1);
            Assert.That(world.DamageMeter, Is.EqualTo(worst).Within(1e-9), "repair does not refill it");
            Assert.That(world.DamagePointsNow, Is.EqualTo((int)Math.Round(DamageAtFull * worst / 100.0, MidpointRounding.AwayFromZero)));
            world.DevEndLevel();
            Assert.That(world.DamagePoints, Is.EqualTo((int)Math.Round(DamageAtFull * worst / 100.0, MidpointRounding.AwayFromZero)));

            // a dead creature: no damage points, and it starves
            world.Cell(0).SetState("bio__D_ribo", 1.1);
            Run(world, 0.2, 0.2);
            Assert.That(world.CellAlive(0), Is.False);
            Assert.That((world.DamageMeter, world.StarvingShare), Is.EqualTo((0.0, 1.0)));
        }

        [Test]
        public void FoodEatenEarnsNoPointsByItself()
        {
            using var world = Quiet(0.0);
            world.CellPosition(0, out double x, out double y);
            var s = world.Food.Settings;
            world.Food.AddBody(x + world.CellRadius, y, 0.0, 0.0, s.PacketRadius, s.PacketAmount, s.FreeFraction * s.PacketAmount, 0);
            Run(world, 0.0, 600.0);
            Assert.That(world.FoodEaten, Is.GreaterThan(1e-16));
            Assert.That(world.SkillPoints, Is.EqualTo(0));
        }

        [Test]
        public void PointsBuyAFeatureFromItsLevel()
        {
            using var world = Quiet(0.1);
            world.DevGrantPoints(GameFeatures.All[Pts].Cost);
            Assert.That(world.FeatureUnlocked(Pts), Is.False);
            Assert.Throws<InvalidOperationException>(() => world.BuyFeature(Pts), "not before its level");
            world.Level = 2;
            Assert.That(world.FeatureUnlocked(Pts), Is.False, "level 2 makes it buyable, not bought");
            int before = world.SkillPoints;
            world.BuyFeature(Pts);
            Assert.That(world.FeatureUnlocked(Pts), Is.True);
            Assert.That(world.SkillPoints, Is.EqualTo(before - GameFeatures.All[Pts].Cost));
            Assert.Throws<InvalidOperationException>(() => world.BuyFeature(Pts), "bought once");
        }

        [Test]
        public void NoPointsNoPurchase()
        {
            using var world = new FermenterWorld();
            world.Level = 2;
            Assert.That(world.SkillPoints, Is.EqualTo(0), "setting the level scores nothing");
            Assert.Throws<InvalidOperationException>(() => world.BuyFeature(Pts));
            Assert.That(world.FeatureUnlocked(Pts), Is.False);
        }

        [Test]
        public void FramesCarryTheMetersAndTheLastLevelsScore()
        {
            var world = Quiet(0.1);
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(10);
            world.Cell(0).SetState("bio__D_ribo", 0.3);
            loop.StepExactly(1);
            world.DevEndLevel();
            world.Cell(0).SetState("bio__D_ribo", 0.5);
            loop.StepExactly(1);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.SkillPoints, Is.EqualTo(world.SkillPoints));
            Assert.That((frame.SurvivalPoints, frame.DamagePoints), Is.EqualTo((world.SurvivalPoints, world.DamagePoints)));
            Assert.That((frame.SurvivalMeter, frame.DamageMeter), Is.EqualTo(((float)world.SurvivalMeter, (float)world.DamageMeter)));
            Assert.That((frame.SurvivalPointsNow, frame.DamagePointsNow), Is.EqualTo((world.SurvivalPointsNow, world.DamagePointsNow)));
            Assert.That(frame.DamageMeter, Is.LessThan(51f));
            Assert.That((frame.ScoredLevel, frame.ScoredDamage, frame.ScoredDamagePoints),
                Is.EqualTo((1, (float)world.LastScore!.Damage, world.LastScore.DamagePoints)));
            Assert.That(frame.ScoredDamage, Is.LessThan(71f));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That((back.SkillPoints, back.SurvivalPoints, back.DamagePoints, back.SurvivalMeter, back.DamageMeter),
                Is.EqualTo((frame.SkillPoints, frame.SurvivalPoints, frame.DamagePoints, frame.SurvivalMeter, frame.DamageMeter)));
            Assert.That((back.SurvivalPointsNow, back.DamagePointsNow, back.StarvingShare, back.TimeWithoutFood),
                Is.EqualTo((frame.SurvivalPointsNow, frame.DamagePointsNow, frame.StarvingShare, frame.TimeWithoutFood)));
            Assert.That((back.ScoredLevel, back.ScoredSurvival, back.ScoredDamage, back.ScoredSurvivalPoints, back.ScoredDamagePoints),
                Is.EqualTo((frame.ScoredLevel, frame.ScoredSurvival, frame.ScoredDamage, frame.ScoredSurvivalPoints, frame.ScoredDamagePoints)));
            Assert.That(back.FoodEaten, Is.EqualTo(frame.FoodEaten));
            world.Dispose();
        }
    }
}
