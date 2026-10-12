using System;
using AdaptOrDeity.Game.Content;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class LevelTests
    {
        private static double Week => GameTimeline.Default.DurationOf(1);

        private static void Run(FermenterWorld world, double seconds)
        {
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
                world.Step(k * world.MacroDt, world.MacroDt);
        }

        [Test]
        public void LevelOneLastsAWeekOfSimulatedTime()
        {
            Assert.That(Week, Is.EqualTo(7 * 86400.0));
            using var world = new FermenterWorld();
            Run(world, 100.0);
            Assert.That(world.LevelTimeLeft, Is.EqualTo(Week - 100.0).Within(1e-6));
            world.Level = 2;
            Assert.That(world.LevelTimeLeft, Is.EqualTo(GameTimeline.Default.DurationOf(2)).Within(1e-6), "a new level starts its clock");
        }

        [Test]
        public void TheOpenWaterIsTheLevelsOcean()
        {
            using var world = new FermenterWorld();
            Assert.That(world.BathConcentration, Is.EqualTo(GameTimeline.Default.OceanGlucose(1)));
        }

        // A week-long calibration: a cell left alone earns one 5-point transporter and starts to starve before the week
        // ends; one kept on food earns two. Then level 2 begins by itself.
        [Test]
        public void ALevelOneCellLeftAloneEarnsOneTransporterAndStartsToStarve()
        {
            using var world = new FermenterWorld();
            int cost = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.PtsTransport)].Cost;
            int damage = -1;
            for (int d = 0; d < world.Details.Count; d++)
                if (world.Details[d].Id == "damage")
                    damage = d;
            double start = world.ReadDetail(0, damage);
            Run(world, Week + 1.0);
            Assert.That(world.SkillPoints, Is.InRange(cost, 2 * cost - 1), $"one transporter (ate {world.FoodEaten:G4} mol)");
            Assert.That(world.ReadDetail(0, damage), Is.GreaterThan(start + 0.05), "starting to starve");
            Assert.That(world.Level, Is.EqualTo(2), "the next level begins when the week is up");
        }

        [Test]
        public void ALevelOneCellKeptOnFoodEarnsTwoTransporters()
        {
            using var world = new FermenterWorld();
            int cost = GameFeatures.All[GameFeatures.IndexOf(GameFeatures.PtsTransport)].Cost;
            world.Food.SetArrivals(0.01, 0.8);   // food everywhere: it never runs out
            Run(world, Week - 1.0);
            Assert.That(world.SkillPoints, Is.GreaterThanOrEqualTo(2 * cost), $"ate {world.FoodEaten:G4} mol");
        }
    }
}
