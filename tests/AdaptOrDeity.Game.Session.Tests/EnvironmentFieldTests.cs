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
            Colonies.Multicellular(world);
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Engulfing), false);   // packets on a cell stay outside it
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

        // Puts the first active packet on cell 0, still, and retires the others.
        private static FoodPacket PacketOnCell(FermenterWorld world)
        {
            var food = world.Food;
            food.SetArrivals(0.0, 0.0);
            food.Settings.LyingDensity = 0.0;
            var packet = food.Packets.First(p => p.Active);
            foreach (var p in food.Packets.Where(p => p != packet))
                typeof(FoodPacket).GetProperty("Active")!.SetValue(p, false);
            world.CellPosition(0, out double x, out double y);
            foreach (var (name, value) in new[] { ("X", (object)x), ("Y", y), ("Vx", 0.0), ("Vy", 0.0) })
                typeof(FoodPacket).GetProperty(name)!.SetValue(packet, value);
            return packet;
        }

        [Test]
        public void WithoutEnzymesOnlyTheFreeFractionLeaks()
        {
            // A packet's small molecules leak out by diffusion (first order); its polymer stays until enzymes act on it.
            var (world, loop) = Colony(1, food: true);
            var packet = PacketOnCell(world);
            world.HydrolysisCapacity = _ => 0.0;   // a cell without its surface hydrolase
            var s = world.Food.Settings;
            double start = packet.Glucose, free = packet.Free;
            Assert.That(free, Is.EqualTo(s.FreeFraction * start).Within(1e-15 * start));
            loop.StepExactly(3000);   // 300 s
            double leaked = free * (1.0 - Math.Exp(-s.FreeLeakRate * 300.0));
            Assert.That(start - packet.Glucose, Is.EqualTo(leaked).Within(1e-9 * leaked), "the free part leaks, exactly");
            Assert.That(packet.Glucose - packet.Free, Is.EqualTo(start - free).Within(1e-15 * start), "the polymer stays");
            Assert.That(world.Environment!.PacketGlucose, Is.EqualTo(leaked).Within(1e-9 * leaked), "into the field");
            var b = world.Body.Body;
            Assert.That(world.Environment.Field.Sample(AdaptOrDeity.Sim.World.EnvironmentField.FirstTrace + packet.MainTrace, b.X, b.Y),
                Is.GreaterThan(0.0), "it smells before anything digests it");
            world.Dispose();
        }

        [Test]
        public void EnzymesHydrolyseAPacketIntoTheFieldExactly()
        {
            var (world, loop) = Colony(1, food: true);
            var packet = PacketOnCell(world);
            const double rate = 1e-17;   // mol glucose/s from cell 0's surface enzymes
            world.HydrolysisCapacity = i => i == 0 ? rate : 0.0;
            double start = packet.Glucose, free = packet.Free;
            loop.StepExactly(100);
            double leaked = free * (1.0 - Math.Exp(-world.Food.Settings.FreeLeakRate * 10.0));
            double released = start - packet.Glucose;
            Assert.That(released, Is.EqualTo(rate * 10.0 + leaked).Within(1e-9 * released), "the enzymes' rate over 10 s, and the free leak");
            Assert.That(world.Environment!.PacketGlucose, Is.EqualTo(released).Within(1e-12 * released), "into the field");
            world.Dispose();
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
            // Glucose hydrolysed from packets and the cells' uptake enter the field exactly: what the field gained is what the
            // packets released less what the cells took, plus what came in from the ocean, to 1e-12.
            var (world, loop) = Colony(8, food: true);
            PacketOnCell(world);
            world.HydrolysisCapacity = _ => 2e-18;
            var env = world.Environment!;
            var field = env.Field;
            const int g = AdaptOrDeity.Sim.World.EnvironmentField.Glucose, lac = AdaptOrDeity.Sim.World.EnvironmentField.Lactate;
            double start = field.Total(g), sourced0 = field.Sourced(g), ocean0 = field.OceanInflow(g);
            double packets0 = env.PacketGlucose, cells0 = world.CellGlucoseExchanged;
            loop.StepExactly(300);
            double packets = env.PacketGlucose - packets0, cells = world.CellGlucoseExchanged - cells0;
            Assert.That(packets, Is.GreaterThan(0.0), "the enzymes released the packet's glucose into the field");
            double sourced = field.Sourced(g) - sourced0;
            Assert.That(sourced, Is.EqualTo(packets - cells).Within(1e-12 * (packets + Math.Abs(cells))));
            Assert.That(field.Total(g), Is.EqualTo(start + sourced + field.OceanInflow(g) - ocean0).Within(1e-12 * start));
            Assert.That(AtColony(world, lac), Is.GreaterThan(0.0), "the cells' lactate is in the water around them");
            Assert.That(world.Cell(0).MembraneLactate, Is.GreaterThan(0.0));
            var b = world.Body.Body;
            double acid = env.AtMembrane(AdaptOrDeity.Sim.World.EnvironmentField.Acid, b.X, b.Y, world.CellRadius, 0.0);
            Assert.That(env.Protons(acid), Is.GreaterThan(env.OceanProtons), "and the acid that came with it");
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
            var b = world.Body.Body;
            Assert.That(world.Environment.AtMembrane(g, b.X, b.Y, world.CellRadius, world.Cell(0).GlucoseRelease),
                Is.LessThan(world.Environment.Field.Sample(g, b.X, b.Y)), "a cell taking glucose up sees less at its membrane");
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

        // The game's settings (nested grids, one coarse step and field.fine_substeps fine steps per macro step, the input
        // tolerance) against the reference (one 1 um grid over the same extent, 64 steps per macro step, every input change
        // passed, cells at 1e-8), for a colony of this many cells in a 0.1 mM sea drifting with the current. Measured
        // 2026-10-13: within 0.6% for 1 and 7 cells everywhere below. Bounds: 2% at the membrane and for the cell's input
        // (the input moves on a change of 1% of its departure from the sea, and the drift adds ~0.5%); the water 5-40 um
        // away within 2% of the membrane's depletion; uptake within 0.1%.
        private static void MatchTheReference(int cells, double seconds)
        {
            (FermenterWorld World, SimLoop Loop) Run(FieldAccuracy accuracy)
            {
                var world = new FermenterWorld(environmentField: true, accuracy: accuracy);
                var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
                if (cells > 1)
                {
                    world.Level = 2;
                    Colonies.Multicellular(world);
                    world.SetFeature(GameFeatures.IndexOf(GameFeatures.Engulfing), false);
                    while (world.CellCount < cells)
                    {
                        var parent = world.Cell(world.CellCount - 1);
                        parent.SetState("bio__P_growth", parent.DaughterCost);
                        world.Divide(world.CellCount - 1);
                    }
                }
                world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
                world.Food.SetArrivals(0.0, 0.0);
                foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                    world.Food.Withdraw(p.X, p.Y, 1e-12, p.Glucose);
                world.BathConcentration = 0.1;   // a sea where the cells' uptake draws the water down
                loop.StepExactly((long)Math.Round(seconds / world.MacroDt));
                return (world, loop);
            }
            var (game, _) = Run(FieldAccuracy.Game);
            var (reference, _) = Run(FieldAccuracy.Reference);
            Assert.That(reference.Environment!.Field.Levels, Is.EqualTo(1), "the reference is one grid");
            var outer = game.Environment!.Field.Level(game.Environment.Field.Levels - 1);
            Assert.That(reference.Environment.Field.Level(0).Nx * reference.Environment.Field.Level(0).Spacing,
                Is.EqualTo(outer.Nx * outer.Spacing).Within(1e-12), "over the same extent");
            const int g = AdaptOrDeity.Sim.World.EnvironmentField.Glucose;
            const double ocean = 0.1;
            double Membrane(FermenterWorld w, int i)
            {
                w.CellPosition(i, out double x, out double y);
                return w.Environment!.AtMembrane(g, x, y, w.CellRadius, w.Cell(i).GlucoseRelease);
            }
            double membrane0 = ocean - Membrane(reference, 0);
            for (int i = 0; i < cells; i++)
            {
                double depletionRef = ocean - Membrane(reference, i);
                Assert.That(depletionRef, Is.GreaterThan(0.0));
                Assert.That(ocean - Membrane(game, i), Is.EqualTo(depletionRef).Within(0.02 * depletionRef), $"cell {i}: depletion at the membrane");
                double inputRef = ocean - reference.Cell(i).MembraneGlucose;
                Assert.That(ocean - game.Cell(i).MembraneGlucose, Is.EqualTo(inputRef).Within(0.02 * inputRef), $"cell {i}: the depletion it sees");
                Assert.That(game.Cell(i).GlucoseRelease, Is.EqualTo(reference.Cell(i).GlucoseRelease)
                    .Within(1e-3 * Math.Abs(reference.Cell(i).GlucoseRelease)), $"cell {i}: uptake");
            }
            var b = game.Body.Body;
            var rb = reference.Body.Body;
            foreach (double r in new[] { 5e-6, 10e-6, 20e-6, 40e-6 })
                Assert.That(game.Environment.Field.Sample(g, b.X + r, b.Y), Is.EqualTo(reference.Environment.Field.Sample(g, rb.X + r, rb.Y))
                    .Within(0.02 * membrane0), $"the water {r * 1e6} um away");
            game.Dispose();
            reference.Dispose();
        }

        [Test]
        public void TheGameSettingsMatchTheReferenceForOneCell() => MatchTheReference(1, 600.0);

        [Test]
        public void TheGameSettingsMatchTheReferenceForASmallColony() => MatchTheReference(7, 60.0);

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
    
        [Test]
        public void FramesCarryTheGlucoseAroundTheCreature()
        {
            var (world, loop) = Colony(8);
            world.BathConcentration = 0.1;
            loop.StepExactly(300);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.FieldExtent, Is.GreaterThanOrEqualTo(2.0 * world.Food.HalfSize * 0.999));
            int m = SnapshotFrame.FieldRasterSize;
            float centre = 0.25f * (frame.FieldGlucose[(m / 2 - 1) * m + m / 2 - 1] + frame.FieldGlucose[(m / 2 - 1) * m + m / 2]
                + frame.FieldGlucose[m / 2 * m + m / 2 - 1] + frame.FieldGlucose[m / 2 * m + m / 2]);
            Assert.That(centre, Is.EqualTo(AtColony(world, 0)).Within(2e-4 * 0.1));
            Assert.That(frame.FieldGlucose[0], Is.GreaterThan(centre), "the corner is less depleted than the colony");
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var back = SnapshotCodec.NewFrameFor(bytes);
            SnapshotCodec.Read(bytes, back);
            Assert.That(back.FieldGlucose, Is.EqualTo(frame.FieldGlucose));
            world.Dispose();

            var plain = new FermenterWorld(environmentField: false);
            var plainLoop = new SimLoop(plain, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var plainFrame = plainLoop.CreateFrame();
            plainLoop.WriteSnapshot(plainFrame);
            Assert.That(plainFrame.FieldExtent, Is.EqualTo(0f), "no field, no raster");
            plain.Dispose();
        }
    
        [Test]
        public void ACellsInputsChangeOnlyWhenTheFieldMovesBeyondTheTolerance()
        {
            // Each changed input restarts the cell's integrator, so inputs are passed on a change above field.input_tolerance
            // of their departure from the sea; what the cell sees stays within it of the field's value at its membrane.
            var (world, loop) = Colony(8);
            world.BathConcentration = 0.1;
            loop.StepExactly(300);
            double tolerance = GameParameters.Default.Value("field.input_tolerance");
            var env = world.Environment!;
            var c = world.Cell(3);
            int changes = 0;
            double last = c.MembraneGlucose;
            for (int n = 0; n < 50; n++)
            {
                loop.StepExactly(1);
                if (c.MembraneGlucose != last)
                    changes++;
                last = c.MembraneGlucose;
            }
            Assert.That(changes, Is.LessThan(25), "near steady, most steps keep the input");
            world.CellPosition(3, out double cx, out double cy);
            double field = env.AtMembrane(AdaptOrDeity.Sim.World.EnvironmentField.Glucose, cx, cy, world.CellRadius, c.GlucoseRelease);
            Assert.That(c.MembraneGlucose, Is.EqualTo(field).Within(2 * tolerance * (0.1 - field)));
            Assert.That(0.1 - field, Is.GreaterThan(1e-4 * 0.1), "a departure the cell sees");
            world.Dispose();
        }
    }
}
