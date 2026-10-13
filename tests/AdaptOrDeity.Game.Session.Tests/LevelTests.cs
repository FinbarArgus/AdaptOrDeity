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
    }

    /// <summary>
    /// The level-1 calibration (docs/plans/scoring.md): each reference player plays the week once (its own fixture, so
    /// each stays under 10 minutes and they run in parallel), and the stars it earns are the designer's targets: left alone
    /// the cell starves within the level (no star); a mediocre player one star, a great one two, the best found three.
    /// </summary>
    public abstract class LevelOneBot
    {
        protected BotOutcome Outcome { get; private set; } = new BotOutcome();

        protected abstract ScriptedPlayer Player { get; }

        [OneTimeSetUp]
        public void Play()
        {
            Outcome = BotOutcome.Play(Player);
            TestContext.Progress.WriteLine(Outcome.ToString());
        }

        [Test]
        public void TheNextLevelBeginsWhenTheWeekIsUp() => Assert.That(Outcome.Score!.Level, Is.EqualTo(1));
    }

    [Parallelizable(ParallelScope.Self)]
    public class LevelOneLeftAlone : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.LeftAlone();

        [Test]
        public void ItStarvesWithinTheLevelAndEarnsNoStar()
        {
            Assert.That(Outcome.StarvingFrom, Is.LessThan(GameTimeline.Default.DurationOf(1)), "starving at the end");
            Assert.That(Outcome.Score!.Stars, Is.EqualTo(0), Outcome.ToString());
        }
    }

    [Parallelizable(ParallelScope.Self)]
    public class LevelOneMediocre : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Mediocre();

        [Test]
        public void ItEarnsOneStar() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(1), Outcome.ToString());
    }

    [Parallelizable(ParallelScope.Self)]
    public class LevelOneGreat : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Great();

        [Test]
        public void ItEarnsTwoStars() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(2), Outcome.ToString());
    }

    [Parallelizable(ParallelScope.Self)]
    public class LevelOneOptimised : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Optimised();

        [Test]
        public void ItEarnsThreeStars() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(3), Outcome.ToString());
    }
}
