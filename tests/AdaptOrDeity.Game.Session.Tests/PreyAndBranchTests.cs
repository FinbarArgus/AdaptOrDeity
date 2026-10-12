using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Packets at a real density, the surface hydrolase as a trait, prey cells, engulfing into a vacuole, and the level-2
    /// branch between multicellularity and mitochondria.
    /// </summary>
    public class PreyAndBranchTests
    {
        private static int Feature(string id) => GameFeatures.IndexOf(id);

        // A level-2 world with no food, no prey arriving and no swimming: what a test puts in is all there is.
        private static FermenterWorld Quiet(int level = 2)
        {
            var world = new FermenterWorld();
            world.Level = level;
            world.SetFeature(Feature(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            return world;
        }

        private static void Run(FermenterWorld world, double seconds)
        {
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
                world.Step(k * world.MacroDt, world.MacroDt);
        }

        // A prey cell on cell 0.
        private static Prey PreyOnCell(FermenterWorld world, int cell = 0)
        {
            world.CellPosition(cell, out double x, out double y);
            return world.Prey.Add(x, y, 0.0);
        }

        // The first packet slot, fresh, on cell 0.
        private static FoodPacket PacketOnCell(FermenterWorld world, double radius)
        {
            var packet = world.Food.Packets[0];
            world.CellPosition(0, out double x, out double y);
            var s = world.Food.Settings;
            foreach (var (name, value) in new[] { ("Active", (object)true), ("Stuck", false), ("X", x), ("Y", y), ("Vx", 0.0),
                         ("Vy", 0.0), ("Radius", radius), ("Glucose", s.SphereAmount(radius)),
                         ("Free", s.FreeFraction * s.SphereAmount(radius)) })
                typeof(FoodPacket).GetProperty(name)!.SetValue(packet, value);
            return packet;
        }

        private static double InVacuoles(FermenterWorld world) =>
            Enumerable.Range(0, world.CellCount).Sum(i => world.VacuoleOf(i)?.Glucose ?? 0.0);

        [Test]
        public void APacketsAmountIsItsSphereAtTheDensityOfACell()
        {
            // 1.1 g/mL x 30% dry x 50% carbon / (12.011 g/mol x 6) = 2,289 mol glucose equivalents per m3.
            double density = GameParameters.Default.PacketDensity;
            Assert.That(density, Is.EqualTo(1100.0 * 0.3 * 0.5 / (0.012011 * 6.0)).Within(1e-9 * density));
            Assert.That(density, Is.EqualTo(2290.0).Within(5.0));
            var food = new FermenterWorld().Food.Settings;
            double r = food.PacketRadius;
            Assert.That(food.PacketAmount, Is.EqualTo(density * 4.0 / 3.0 * Math.PI * r * r * r).Within(1e-12 * food.PacketAmount));
            Assert.That(food.PacketAmount, Is.EqualTo(4.9e-15).Within(0.1e-15), "0.8 um: about 8x less than the old 4e-14 mol");
            var prey = GameParameters.Default.Prey();
            Assert.That(new PreyField(prey).Content, Is.EqualTo(food.SphereAmount(prey.Radius)).Within(1e-27), "prey at the same density");
        }

        [Test]
        public void TheHydrolaseSliderScalesItsCapacityAndCostsAtp()
        {
            using var world = Quiet(level: 1);
            world.BathConcentration = 20.0;   // well fed: building is not held back by ATP
            int trait = world.Traits.ToList().FindIndex(t => t.Id == "Hydrolase_expression");
            Assert.That(trait, Is.GreaterThanOrEqualTo(0));
            Assert.That(world.Traits[trait].Min, Is.EqualTo(0.25));
            Assert.That(world.Traits[trait].Max, Is.EqualTo(4.0));
            var cell = world.Cell(0);
            Run(world, 1.0);
            double capacity = cell.HydrolysisMax, upkeep = cell.AlgebraicValue("pool_Hydrolase__J_ATP");
            Assert.That(upkeep, Is.GreaterThan(0.0), "keeping the copies costs ATP");
            world.SetTrait(0, trait, 4.0);
            Run(world, 5.0);
            Assert.That(cell.Budget().AtpProteinBuild, Is.GreaterThan(0.0), "building the new copies costs ATP");
            Run(world, 1800.0);
            Assert.That(world.ReadTrait(0, trait), Is.EqualTo(4.0).Within(0.02), "expression reached the target");
            Assert.That(cell.HydrolysisMax / capacity, Is.EqualTo(4.0).Within(0.02 * 4.0), "capacity = copies x kcat");
            Assert.That(cell.AlgebraicValue("pool_Hydrolase__J_ATP") / upkeep, Is.EqualTo(4.0).Within(0.1),
                "turnover of four times the copies");
        }

        [Test]
        public void PreyArriveAtTheLevelsRateAndSwimByRunAndTumble()
        {
            var settings = GameParameters.Default.Prey();
            Assert.That(settings.ArrivalsPerSecond, Is.EqualTo(0.0), "no prey at level 1");
            Assert.That(GameTimeline.Default.ArrivalsAt(2).PreyPerSecond, Is.GreaterThan(0.0), "prey from level 2");
            settings.ArrivalsPerSecond = 0.05;
            var field = new PreyField(settings);
            const double dt = 0.1, seconds = 20_000.0;
            int turns = 0, moves = 0;
            for (int k = 0; k < seconds / dt; k++)
            {
                var before = field.Prey.Select(p => (p.Active, p.X, p.Y, p.Heading)).ToArray();
                field.Step(dt, 0.0, 0.0);
                for (int i = 0; i < before.Length; i++)
                {
                    var p = field.Prey[i];
                    if (!before[i].Active || !p.Active)
                        continue;
                    // each step moves the prey at its run speed along the heading it had
                    double d = Math.Sqrt((p.X - before[i].X) * (p.X - before[i].X) + (p.Y - before[i].Y) * (p.Y - before[i].Y));
                    if (d > 2.0 * settings.RunSpeed * dt)
                        continue;   // the slot was freed and taken by a new arrival in the same step
                    Assert.That(d, Is.EqualTo(settings.RunSpeed * dt).Within(1e-12 * d));
                    moves++;
                    if (p.Heading != before[i].Heading)
                        turns++;
                }
            }
            double expected = settings.ArrivalsPerSecond * seconds;
            Assert.That((double)field.Arrived, Is.EqualTo(expected).Within(4.0 * Math.Sqrt(expected)), "Poisson arrivals at the rate");
            Assert.That((double)turns / moves, Is.EqualTo(dt / settings.MeanRun).Within(0.2 * dt / settings.MeanRun),
                "a tumble every mean run");
            foreach (var p in field.Prey.Where(p => p.Active))
                Assert.That(Math.Max(Math.Abs(p.X), Math.Abs(p.Y)), Is.LessThanOrEqualTo(settings.HalfSize + p.Radius),
                    "prey leave when they cross the window's edge");
        }

        [Test]
        public void OnlyACellWithEngulfingSwallowsAndOnlySmallerItems()
        {
            using var world = Quiet(level: 1);
            PreyOnCell(world);
            Run(world, 0.2);
            Assert.That(world.Swallowed, Is.EqualTo(0), "no engulfing at level 1");
            world.Level = 2;
            world.SetFeature(Feature(GameFeatures.Engulfing), false);
            foreach (var p in world.Prey.Prey.Where(p => p.Active).ToList())
                world.Prey.Remove(p);
            PreyOnCell(world);
            Run(world, 0.2);
            Assert.That(world.Swallowed, Is.EqualTo(0), "engulfing turned off");
            world.SetFeature(Feature(GameFeatures.Engulfing), true);
            Run(world, 0.1);
            Assert.That(world.Swallowed, Is.EqualTo(1), "a touching prey cell smaller than the cell is swallowed");
            Assert.That(world.Prey.ActiveCount, Is.EqualTo(0));
            Assert.That(world.VacuoleOf(0)!.Prey, Is.EqualTo(1));

            var big = PacketOnCell(world, 1.2 * world.CellRadius);
            Run(world, 0.1);
            Assert.That(big.Active, Is.True, "a packet larger than the cell stays outside");
            Assert.That(big.Stuck, Is.True, "and is eaten from outside by the contact rule");
            world.Food.Remove(big);
            var small = PacketOnCell(world, 0.5 * world.CellRadius);
            Run(world, 0.1);
            Assert.That(small.Active, Is.False, "a smaller packet is engulfed");
            Assert.That(world.Swallowed, Is.EqualTo(1), "packets are not organisms: the count is prey only");
        }

        [Test]
        public void AVacuolesContentIsDigestedAndTakenUpConservatively()
        {
            using var world = Quiet();
            var packet = PacketOnCell(world, world.Food.Settings.PacketRadius);
            double amount = packet.Glucose;
            Run(world, 0.1);
            Assert.That(packet.Active, Is.False);
            var vacuole = world.VacuoleOf(0)!;
            Assert.That(world.Engulfed, Is.EqualTo(amount).Within(1e-12 * amount));
            double eaten0 = world.FoodEaten, fromVacuoles0 = world.EatenFromVacuoles;
            for (int hour = 0; hour < 12 && world.VacuoleOf(0) != null; hour++)
            {
                Run(world, 3600.0);
                double held = InVacuoles(world);
                Assert.That(world.EatenFromVacuoles + world.Egested + held, Is.EqualTo(amount).Within(1e-12 * amount),
                    $"hour {hour + 1}: engulfed = taken up + egested + still held");
            }
            Assert.That(world.VacuoleOf(0), Is.Null, "digested within hours");
            Assert.That(vacuole.Glucose, Is.LessThan(0.01 * world.Prey.Content), "digested and taken up to the used-up share");
            Assert.That(world.EatenFromVacuoles, Is.GreaterThan(0.99 * amount), "almost all of it reached the cell");
            Assert.That(world.FoodEaten - eaten0, Is.EqualTo(world.EatenFromVacuoles - fromVacuoles0).Within(1e-12 * amount),
                "and counts as food eaten");
        }

        [Test]
        public void WithoutMulticellularityDaughtersLeaveAndEarnReproductionPoints()
        {
            using var world = Quiet();
            double perOffspring = GameParameters.Default.Value("skills.points_per_offspring");
            Assert.That(world.FeatureUnlocked(Feature(GameFeatures.Multicellularity)), Is.False);
            int before = world.SkillPoints;
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            Assert.That(world.CellCount, Is.EqualTo(1), "the daughter left");
            Assert.That(world.Organisms, Has.Count.EqualTo(1), "and lives on its own (FreeOrganismTests)");
            Assert.That(world.Offspring, Is.EqualTo(1));
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(perOffspring)));
            Assert.That(world.SkillPoints, Is.EqualTo(before + world.ReproductionPoints));
            Assert.That(world.Cell(0).GrowthProgress, Is.LessThan(0.01), "the parent starts its next daughter");

            Colonies.Multicellular(world);
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            Assert.That(world.CellCount, Is.EqualTo(2), "with multicellularity the daughter stays");
            Assert.That(world.Organisms, Has.Count.EqualTo(1), "no new free organism");
            Assert.That(world.Offspring, Is.EqualTo(1), "and is not offspring");
            Assert.That(world.ReproductionPoints, Is.EqualTo((int)Math.Floor(perOffspring)), "a staying daughter earns no points");
        }

        [Test]
        public void ColonyFeaturesNeedMulticellularity()
        {
            using var world = Quiet(level: 7);
            foreach (var id in new[] { GameFeatures.BoundaryInnerGroups, GameFeatures.MultipleGroups, GameFeatures.FixedBodySize })
                Assert.That(world.FeatureUnlocked(Feature(id)), Is.False, id);
            Colonies.Multicellular(world);
            foreach (var id in new[] { GameFeatures.BoundaryInnerGroups, GameFeatures.MultipleGroups, GameFeatures.FixedBodySize })
                Assert.That(world.FeatureUnlocked(Feature(id)), Is.True, id);
        }

        [Test]
        public void MitochondriaNeedTenSwallowedAndMulticellularityStaysOpen()
        {
            // a lineage may evolve both: neither closes the other
            int mito = Feature(GameFeatures.Mitochondria), multi = Feature(GameFeatures.Multicellularity);
            Assert.That(GameFeatures.All[mito].RequiresSwallowed, Is.EqualTo(10));
            Assert.That(GameFeatures.All[mito].Excludes, Is.Empty);
            Assert.That(GameFeatures.All[multi].Excludes, Is.Empty);
            using (var world = Quiet())
            {
                world.DevGrantPoints(GameFeatures.All[mito].Cost + GameFeatures.All[multi].Cost);
                var e = Assert.Throws<InvalidOperationException>(() => world.BuyFeature(mito))!;
                Assert.That(e.Message, Does.Contain("10 prey"));
                typeof(FermenterWorld).GetProperty(nameof(FermenterWorld.Swallowed))!.SetValue(world, 10);
                world.BuyFeature(mito);
                Assert.That(world.FeatureUnlocked(mito), Is.True);
                world.BuyFeature(multi);
                Assert.That(world.FeatureUnlocked(multi), Is.True, "both");
            }
        }

        [Test]
        public void SwallowingTenPreyMakesMitochondriaAvailable()
        {
            // The real counter: prey on the cell, one after another, as the vacuole digests them.
            using var world = Quiet();
            int mito = Feature(GameFeatures.Mitochondria);
            world.DevGrantPoints(GameFeatures.All[mito].Cost);
            for (int k = 0; k < 600 && world.Swallowed < 10; k++)
            {
                if (world.Prey.ActiveCount == 0)
                    PreyOnCell(world);
                Run(world, 10.0);
            }
            Assert.That(world.Swallowed, Is.EqualTo(10));
            Assert.That(world.WhyNotForSale(mito), Is.Null);
        }

        [Test]
        public void FramesAndTheCodecCarryPreyVacuolesAndTheCounters()
        {
            var world = Quiet();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            PreyOnCell(world);
            loop.StepExactly(1);
            world.Prey.Add(world.Body.Body.X + 10e-6, world.Body.Body.Y, 1.0);   // one more, in view
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.Swallowed, Is.EqualTo(1));
            Assert.That((frame.Offspring, frame.OwnOffspring), Is.EqualTo((1, 1)));
            Assert.That(frame.ReproductionPoints, Is.EqualTo(world.ReproductionPoints));
            Assert.That(frame.FoodPoints, Is.EqualTo(world.FoodPoints));
            Assert.That(frame.PreyCount, Is.EqualTo(1));
            Assert.That(frame.PreyX[0], Is.EqualTo((float)10e-6).Within(1e-9f));
            Assert.That(frame.PreyHeading[0], Is.EqualTo(1f));
            Assert.That(frame.VacuoleRadius[0], Is.EqualTo((float)world.VacuoleRadius(world.VacuoleOf(0)!.Glucose)));
            Assert.That(frame.VacuoleRadius[0], Is.GreaterThan(0f).And.LessThan((float)world.CellRadius));
            Assert.That(frame.VacuolePrey[0], Is.EqualTo(1));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.PreyCount, Is.EqualTo(1));
            Assert.That(back.PreyX, Is.EqualTo(frame.PreyX));
            Assert.That(back.PreyY, Is.EqualTo(frame.PreyY));
            Assert.That(back.PreyRadius, Is.EqualTo(frame.PreyRadius));
            Assert.That(back.PreyHeading, Is.EqualTo(frame.PreyHeading));
            Assert.That(back.VacuoleRadius, Is.EqualTo(frame.VacuoleRadius));
            Assert.That(back.VacuoleX, Is.EqualTo(frame.VacuoleX));
            Assert.That(back.VacuoleY, Is.EqualTo(frame.VacuoleY));
            Assert.That(back.VacuolePrey, Is.EqualTo(frame.VacuolePrey));
            Assert.That(back.Swallowed, Is.EqualTo(1));
            Assert.That((back.Offspring, back.OwnOffspring), Is.EqualTo((1, 1)));
            Assert.That(back.ReproductionPoints, Is.EqualTo(frame.ReproductionPoints));
            Assert.That(back.FoodPoints, Is.EqualTo(frame.FoodPoints));
            world.Dispose();
        }

        [Test]
        public void FramesGrowWithThePreyInView()
        {
            var world = Quiet();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var frame = loop.CreateFrame();
            var b = world.Body.Body;
            for (int k = 0; k < 3 * frame.PreyX.Length; k++)
                world.Prey.Add(b.X + 5e-6 + 1e-7 * k, b.Y + 8e-6, 0.0);
            Assert.That(loop.Outgrown(frame), Is.True);
            frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.PreyCount, Is.EqualTo(world.Prey.ActiveCount));
            world.Dispose();
        }

        [Test]
        public void PreyAndEngulfingGiveTheSameResultsOnAnyNumberOfThreads()
        {
            double[]? reference = null;
            foreach (int workers in new[] { 1, 4, 8 })
            {
                var world = new FermenterWorld { Workers = workers };
                world.Level = 2;
                Colonies.Multicellular(world);
                while (world.CellCount < 12)
                {
                    FermenterSessionTests.FinishDaughter(world, world.CellCount - 1);
                    world.Divide(world.CellCount - 1);
                }
                world.Prey.SetArrivals(0.5);   // many prey, to meet some in a few minutes
                world.Food.SetArrivals(0.05, 0.8);
                Run(world, 300.0);
                var print = new double[world.CellCount * 3 + 4];
                for (int i = 0; i < world.CellCount; i++)
                {
                    print[3 * i] = world.Cell(i).State("cytosol__C_ATP");
                    print[3 * i + 1] = world.Cell(i).MembraneGlucose;
                    print[3 * i + 2] = world.VacuoleOf(i)?.Glucose ?? -1.0;
                }
                print[print.Length - 4] = world.Swallowed;
                print[print.Length - 3] = world.Engulfed;
                print[print.Length - 2] = world.EatenFromVacuoles;
                print[print.Length - 1] = world.FoodEaten;
                world.Dispose();
                if (reference == null)
                {
                    Assert.That(world.Swallowed, Is.GreaterThan(0), "no prey was swallowed: the test would miss engulfing");
                    reference = print;
                    continue;
                }
                for (int i = 0; i < print.Length; i++)
                    Assert.That(BitConverter.DoubleToInt64Bits(print[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(reference[i])),
                        $"value {i} differs with {workers} workers");
            }
        }
    }
}
