using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class EnvironmentFieldTests
    {
        // A world on the environment field with this many cells, not swimming, without food.
        private static (FermenterWorld World, SimLoop Loop) Colony(int cells, int workers = 1, bool food = false)
        {
            var world = new FermenterWorld(environmentField: true) { Workers = workers };
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            world.Level = 2;
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            while (world.CellCount < cells)
            {
                var parent = world.Cell(world.CellCount - 1);
                parent.SetState("bio__P_growth", parent.DaughterCost);
                world.Divide(world.CellCount - 1);
            }
            if (!food)
            {
                world.Food.SetArrivals(0.0, 0.0);
                foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                    world.Food.Withdraw(p.X, p.Y, 1e-12, p.Glucose);   // emptied: retired at the next step
            }
            return (world, loop);
        }

        private static double AtColony(FermenterWorld world, int species)
        {
            var b = world.Body.Body;
            return world.Environment!.Field.Sample(species, b.X, b.Y);
        }

        [Test]
        public void PacketsDissolveAtTheirLifetimeAndKeepEveryMole()
        {
            var settings = GameParameters.Default.Food(0.0);
            settings.LyingDensity = 4.0 / (4.0 * settings.HalfSize * settings.HalfSize);
            var food = new FoodField(settings);
            food.Populate(0.0, 0.0);
            var p = food.Packets.First(q => q.Active);
            double start = p.Glucose, released = 0.0, lifetime = 50.0;
            int steps = 0;
            while (p.Glucose > 0.0)
            {
                released += food.Dissolve(p, 0.1, lifetime);
                steps++;
            }
            Assert.That(released, Is.EqualTo(start).Within(1e-15 * start), "all of it, exactly");
            Assert.That(steps * 0.1, Is.EqualTo(lifetime * (1.0 - Math.Sqrt(settings.UsedUpFraction))).Within(0.11),
                "the radius shrinks at a constant rate until the used-up share");
        }

        [Test]
        public void TheBufferTurnsAcidIntoPh()
        {
            var env = new FermenterWorld(environmentField: true).Environment!;
            double h0 = env.OceanProtons;
            Assert.That(env.Protons(0.0), Is.EqualTo(h0).Within(1e-12 * h0), "no acid added: the ocean's pH");
            double pH = -Math.Log10(env.Protons(0.1) / 1e3), pH0 = -Math.Log10(h0 / 1e3);
            // 0.1 mM of acid in 3 mM carbonate at pH 7.3 (pK 5.9): ln(10) B a(1 - a) ~ 0.27 mM per pH unit, so ~0.3 pH down (less, as the buffer's acid share grows)
            Assert.That(pH0 - pH, Is.InRange(0.25, 0.4));
            Assert.That(env.Protons(-0.05), Is.LessThan(h0), "base raises the pH");
            Assert.That(env.Protons(-100.0), Is.EqualTo(0.0), "never negative");
        }

        [Test]
        public void CellsAndFoodExchangeWithTheFieldConservatively()
        {
            // Glucose from dissolving packets and the cells' uptake enter the field exactly: what the field gained is what the
            // packets released less what the cells took, plus what came in from the ocean, to 1e-12.
            var (world, loop) = Colony(8, food: true);
            var env = world.Environment!;
            var field = env.Field;
            const int g = AdaptOrDeity.Sim.World.EnvironmentField.Glucose, lac = AdaptOrDeity.Sim.World.EnvironmentField.Lactate;
            double start = field.Total(g), sourced0 = field.Sourced(g), ocean0 = field.OceanInflow(g);
            double packets0 = env.PacketGlucose, cells0 = world.CellGlucoseExchanged;
            loop.StepExactly(300);
            double packets = env.PacketGlucose - packets0, cells = world.CellGlucoseExchanged - cells0;
            Assert.That(packets, Is.GreaterThan(0.0), "the packets dissolved into the field");
            double sourced = field.Sourced(g) - sourced0;
            Assert.That(sourced, Is.EqualTo(packets - cells).Within(1e-12 * (packets + Math.Abs(cells))));
            Assert.That(field.Total(g), Is.EqualTo(start + sourced + field.OceanInflow(g) - ocean0).Within(1e-12 * start));
            Assert.That(AtColony(world, lac), Is.GreaterThan(0.0), "the cells' lactate is in the water around them");
            Assert.That(world.Cell(0).MembraneLactate, Is.GreaterThan(0.0));
            Assert.That(world.Cell(0).MembraneProtons, Is.GreaterThan(env.OceanProtons), "and the acid that came with it");
            world.Dispose();
        }

        [Test]
        public void AColonyDepletesGlucoseAroundItselfAndItRecoversWhenTheColonyStops()
        {
            var (world, loop) = Colony(16);
            world.BathConcentration = 0.1;   // a new sea at 0.1 mM
            const int g = AdaptOrDeity.Sim.World.EnvironmentField.Glucose;
            loop.StepExactly(600);
            double ocean = world.Environment!.OceanGlucose, depleted = AtColony(world, g);
            Assert.That(depleted, Is.LessThan(0.995 * ocean), "the colony draws glucose down around itself");
            Assert.That(world.Cell(0).MembraneGlucose, Is.LessThan(depleted), "the membrane sits below the grid");
            var b = world.Body.Body;
            double farther = world.Environment.Field.Sample(g, b.X + 60e-6, b.Y);
            Assert.That(farther, Is.InRange(depleted, ocean), "less so farther out");

            for (int i = 0; i < world.CellCount; i++)
                world.Cell(i).SetState("bio__D", 5.0);   // the colony dies: no more uptake
            loop.StepExactly(100);
            Assert.That(world.CellAlive(0), Is.False);
            double early = AtColony(world, g);
            loop.StepExactly(3000);
            // in a 2D layer the deficit spreads out and thins slowly (as 1/t at the centre)
            Assert.That(AtColony(world, g), Is.GreaterThan(early), "the water recovers");
            Assert.That(ocean - AtColony(world, g), Is.LessThan(0.05 * (ocean - depleted)));
            world.Dispose();
        }

        [Test]
        public void TheFieldGrowsWithTheColony()
        {
            var (world, loop) = Colony(1);
            var field = world.Environment!.Field;
            int cells = field.CellsPerSide, levels = field.Levels;
            while (world.CellCount < 40)
            {
                var parent = world.Cell(world.CellCount - 1);
                parent.SetState("bio__P_growth", parent.DaughterCost);
                world.Divide(world.CellCount - 1);
            }
            loop.StepExactly(1);
            Assert.That(field.CellsPerSide, Is.GreaterThan(cells));
            Assert.That(field.Levels, Is.GreaterThanOrEqualTo(levels));
            var outer = field.Level(field.Levels - 1);
            Assert.That(outer.Nx * outer.Spacing, Is.GreaterThanOrEqualTo(60.0 * world.ColonyDiameter));
            world.Dispose();
        }

        [Test]
        public void ResultsAreTheSameOnAnyNumberOfWorkers()
        {
            double[] Run(int workers)
            {
                var (world, loop) = Colony(12, workers, food: true);
                loop.StepExactly(100);
                var field = world.Environment!.Field;
                var r = new double[] { field.Total(0), field.Total(1), field.Total(2), AtColony(world, 0),
                    world.Cell(5).State("cytosol__C_ATP"), world.Cell(11).MembraneGlucose, world.FoodEaten };
                world.Dispose();
                return r;
            }
            Assert.That(Run(4), Is.EqualTo(Run(1)), "bit for bit");
        }
    }
}
