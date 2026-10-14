using System;
using AdaptOrDeity.Game.Content;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class LevelTests
    {
        private static double Day => GameTimeline.Default.DurationOf(1);

        private static void Run(FermenterWorld world, double seconds)
        {
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
                world.Step(k * world.MacroDt, world.MacroDt);
        }

        [Test]
        public void LevelOnesGoalIsToSurviveOneDay()
        {
            var goals = GameTimeline.Default.GoalsOf(1);
            Assert.That(goals.Count, Is.EqualTo(1));
            Assert.That(goals[0].Kind, Is.EqualTo("survive"));
            Assert.That(Day, Is.EqualTo(86400.0));
            using var world = new FermenterWorld();
            Run(world, 100.0);
            Assert.That(world.LevelTimeLeft, Is.EqualTo(Day - 100.0).Within(1e-6));
            Assert.That(world.LevelGoalsMet, Is.False);
            world.Level = 2;
            Assert.That(world.LevelTimeLeft, Is.EqualTo(GameTimeline.Default.DurationOf(2)).Within(1e-6), "a new level starts its clock");
        }

        [Test]
        public void SurvivingTheDayPassesTheLevel()
        {
            using var world = new FermenterWorld();
            world.Food.SetArrivals(0.05, 0.8);   // plenty of food: it survives
            Run(world, Day + 1.0);
            Assert.That(world.Level, Is.EqualTo(2), "level 2 begins once the day is survived");
        }

        [Test]
        public void ACreatureThatDiedDoesNotPass()
        {
            using var world = new FermenterWorld();
            world.Cell(0).SetState("bio__D", 5.0);   // a lethal damage load
            Run(world, Day + 1.0);
            Assert.That(world.CellAlive(0), Is.False);
            Assert.That(world.Level, Is.EqualTo(1));
            Assert.That(world.LevelGoalsMet, Is.False);
        }

        [Test]
        public void TheFlagellaPushAtTheContentShareOfTheCellModelsThrust()
        {
            using var world = new FermenterWorld();
            var fresh = new AdaptOrDeity.Sim.Physiology.FermenterCell(TraitCatalog.Fermenter);
            Assert.That(world.Cell(0).External("v_one_mod_i_flagellum"),
                Is.EqualTo(GameParameters.Default.Value("motility.thrust_scale") * fresh.External("v_one_mod_i_flagellum")));
        }

        [Test]
        public void TheOpenWaterIsTheLevelsOcean()
        {
            using var world = new FermenterWorld();
            Assert.That(world.BathConcentration, Is.EqualTo(GameTimeline.Default.OceanGlucose(1)));
        }
    }

    /// <summary>
    /// The level-1 calibration (docs/plans/scoring.md): each reference player plays the level once (its own fixture, so
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
        public void TheNextLevelBeginsWhenItsGoalsAreMet() => Assert.That(Outcome.Score!.Level, Is.EqualTo(1));
    }

    [Parallelizable(ParallelScope.Self)]
    [Explicit("to recalibrate: level 1 is now survive one day, with 0.5 um packets (2026-10-14)")]
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
    [Explicit("to recalibrate: level 1 is now survive one day, with 0.5 um packets (2026-10-14)")]
    public class LevelOneMediocre : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Mediocre();

        [Test]
        public void ItEarnsOneStar() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(1), Outcome.ToString());
    }

    [Parallelizable(ParallelScope.Self)]
    [Explicit("to recalibrate: level 1 is now survive one day, with 0.5 um packets (2026-10-14)")]
    public class LevelOneGreat : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Great();

        [Test]
        public void ItEarnsTwoStars() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(2), Outcome.ToString());
    }

    [Parallelizable(ParallelScope.Self)]
    [Explicit("to recalibrate: level 1 is now survive one day, with 0.5 um packets (2026-10-14)")]
    public class LevelOneOptimised : LevelOneBot
    {
        protected override ScriptedPlayer Player => Bots.Optimised();

        [Test]
        public void ItEarnsThreeStars() => Assert.That(Outcome.Score!.Stars, Is.EqualTo(3), Outcome.ToString());
    }
}
