using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class SkillPointTests
    {
        private static readonly int Pts = GameFeatures.IndexOf(GameFeatures.PtsTransport);

        // A world whose only food is one packet on the first cell, holding enough glucose for a few points.
        private static FermenterWorld Feeding(double packetGlucose)
        {
            var world = new FermenterWorld();
            var food = world.Food;
            food.SetArrivals(0.0, 0.0);
            food.Settings.LyingDensity = 0.0;
            foreach (var p in food.Packets.Skip(1))
                typeof(FoodPacket).GetProperty("Active")!.SetValue(p, false);
            world.CellPosition(0, out double x, out double y);
            var packet = food.Packets[0];
            foreach (var (name, value) in new[] { ("Active", (object)true), ("X", x), ("Y", y), ("Vx", 0.0), ("Vy", 0.0),
                         ("Glucose", packetGlucose) })
                typeof(FoodPacket).GetProperty(name)!.SetValue(packet, value);
            return world;
        }

        [Test]
        public void EatingFoodEarnsPointsThatBuyAFeatureFromItsLevel()
        {
            double perPoint = GameParameters.Default.Value("skills.glucose_per_point");
            using var world = Feeding(4 * perPoint);
            double t = 0.0;
            // a resting cell takes up only what it uses (~5e-19 mol/s): the first point within a day
            while (world.SkillPoints < 1 && t < 86_400.0)
            {
                world.Step(t, world.MacroDt);
                t += world.MacroDt;
            }
            Assert.That(world.FoodEaten, Is.GreaterThanOrEqualTo(perPoint));
            Assert.That(world.FoodEaten, Is.EqualTo(world.EatenFromPackets).Within(0.01 * world.FoodEaten),
                "eaten from the packet it touches (and a little of what leaked from it, from the water)");
            Assert.That(world.SkillPoints, Is.EqualTo((int)Math.Floor(world.FoodEaten / perPoint)));
            world.DevGrantPoints(GameFeatures.All[Pts].Cost - world.SkillPoints);
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
            Assert.That(world.SkillPoints, Is.EqualTo(0));
            Assert.Throws<InvalidOperationException>(() => world.BuyFeature(Pts));
            Assert.That(world.FeatureUnlocked(Pts), Is.False);
        }

        [Test]
        public void FramesCarryPointsAndPurchases()
        {
            double perPoint = GameParameters.Default.Value("skills.glucose_per_point");
            var world = Feeding(10 * perPoint + 4e-14);
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(3000);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.FoodEaten, Is.EqualTo(world.FoodEaten));
            Assert.That(frame.SkillPoints, Is.EqualTo(world.SkillPoints));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.SkillPoints, Is.EqualTo(frame.SkillPoints));
            Assert.That(back.FoodEaten, Is.EqualTo(frame.FoodEaten));
            world.Dispose();
        }
    }
}
