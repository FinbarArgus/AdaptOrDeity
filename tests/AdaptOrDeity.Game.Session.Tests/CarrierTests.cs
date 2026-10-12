using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// The fermenter's glucose carriers and membrane shares in the world: the carriers bought as skills (PTS, ABC, H+
    /// symport), the medium glucose every carrier sees, uptake as their sum, the Glf leak hint, the surface hydrolase on food
    /// packets in the environment field, and the flagella's share of the membrane.
    /// </summary>
    public class CarrierTests
    {
        private static int Trait(FermenterWorld world, string id) => world.Traits.ToList().FindIndex(t => t.Id == id);

        private static int Detail(FermenterWorld world, string id) => world.Details.ToList().FindIndex(d => d.Id == id);

        // A world with enough points to buy the carriers at level 2, not swimming, without food.
        private static FermenterWorld Level2(bool field = false)
        {
            var world = new FermenterWorld(environmentField: field);
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Withdraw(p.X, p.Y, 1e-12, p.Glucose);
            typeof(FermenterWorld).GetProperty(nameof(FermenterWorld.FoodEaten))!
                .SetValue(world, 20 * GameParameters.Default.Value("skills.glucose_per_point"));
            world.Level = 2;
            return world;
        }

        private static void Run(FermenterWorld world, double seconds, ref double t)
        {
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++, t += world.MacroDt)
                world.Step(t, world.MacroDt);
        }

        [Test]
        public void BuyingATransporterOffersItsSliderAndItsFluxAppears()
        {
            using var world = Level2();
            int pts = Trait(world, "PTS_share"), feature = GameFeatures.IndexOf(GameFeatures.PtsTransport);
            Assert.That(world.Traits[pts].IsShare, Is.True);
            Assert.That(world.Traits[pts].Feature, Is.EqualTo(GameFeatures.PtsTransport));
            Assert.That(world.ReadTraitTarget(0, pts), Is.EqualTo(0.0), "the starting cell has none");
            Assert.Throws<InvalidOperationException>(() => world.SetTrait(0, pts, 0.05), "not before it is bought");
            Assert.That(world.Cell(0).CarrierUptake(1), Is.EqualTo(0.0));

            world.BuyFeature(feature);
            world.SetTrait(0, pts, 0.05);
            Assert.That(world.ReadTraitTarget(0, pts), Is.EqualTo(0.05));
            Assert.That(world.Cell(0).External("s_target_mod_pool_PTS"), Is.EqualTo(0.05));
            world.BathConcentration = 0.05;   // a richer sea: ATP to build the carriers
            double t = 0.0;
            Run(world, 600.0, ref t);
            var cell = world.Cell(0);
            Assert.That(world.ReadTrait(0, pts), Is.EqualTo(0.05).Within(2e-3), "built to its share");
            Assert.That(cell.CarrierUptake(1), Is.GreaterThan(0.0), "the PTS brings glucose in");
            Assert.That(world.ReadDetail(0, Detail(world, "glucose_in")), Is.EqualTo(cell.GlucoseUptake));
            Assert.That(world.ReadDetail(0, Detail(world, "atp_carriers")), Is.EqualTo(cell.CarrierUptake(1)).Within(1e-12 * cell.CarrierUptake(1)),
                "one PEP per glucose");
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.TraitArea[pts][0], Is.EqualTo((float)(world.ReadTrait(0, pts) * cell.TransporterAreaMax * 1e-12)),
                "drawn as an arc as long as its share of the surface");
        }

        [Test]
        public void SharesStayWithinTheCeilingAndTheFlagellaCount()
        {
            using var world = Level2();
            world.BuyFeature(GameFeatures.IndexOf(GameFeatures.PtsTransport));
            world.BuyFeature(GameFeatures.IndexOf(GameFeatures.AbcTransport));
            var cell = world.Cell(0);
            int flagella = Trait(world, "flagella");
            double one = cell.TraitShare(flagella);
            Assert.That(one, Is.EqualTo(0.0006).Within(1e-4), "a flagellum's share of the ceiling (1700 nm2)");
            Assert.That(cell.MembraneShareClaimed(), Is.EqualTo(cell.MembraneShareUsed()).Within(1e-12), "at rest");
            world.SetTrait(0, flagella, 3.0);
            Assert.That(cell.TraitTargetShare(flagella), Is.EqualTo(3.0 * one).Within(1e-12), "counted at its target");
            Assert.That(cell.MembraneShareClaimed(), Is.EqualTo(cell.MembraneShareUsed() + 2.0 * one).Within(1e-12));

            // the new carriers fill the room the others leave; then nothing more fits, flagella included
            world.SetTrait(0, Trait(world, "PTS_share"), 0.5);
            world.SetTrait(0, Trait(world, "ABC_share"), 0.5);
            Assert.That(cell.MembraneShareClaimed(), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(world.ReadTraitTarget(0, Trait(world, "ABC_share")), Is.LessThan(0.5), "clamped to the room left");
            world.SetTrait(0, flagella, 8.0);
            Assert.That(world.ReadTraitTarget(0, flagella), Is.EqualTo(3.0), "no room for another flagellum");
            world.SetTrait(0, Trait(world, "H_ATPase_expression"), 4.0);
            Assert.That(cell.MembraneShareClaimed(), Is.LessThanOrEqualTo(1.0 + 1e-12));
            world.SetTrait(0, Trait(world, "PTS_share"), 0.0);
            world.SetTrait(0, flagella, 8.0);
            Assert.That(world.ReadTraitTarget(0, flagella), Is.EqualTo(8.0), "room again");
            Assert.That(cell.MembraneShareClaimed(), Is.LessThanOrEqualTo(1.0 + 1e-12));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EveryCarrierSeesTheMediumGlucose(bool field)
        {
            using var world = Level2(field);
            world.BathConcentration = 0.02;
            double t = 0.0;
            Run(world, 1.0, ref t);
            var cell = world.Cell(0);
            Assert.That(cell.MembraneGlucose, Is.GreaterThan(0.0));
            foreach (var carrier in FermenterCell.Carriers)
                Assert.That(cell.External("C_glc_e_mod_" + carrier), Is.EqualTo(cell.MembraneGlucose), carrier);
        }

        [Test]
        public void TheFieldTakesTheUptakeOfEveryCarrier()
        {
            using var world = Level2(field: true);
            world.BuyFeature(GameFeatures.IndexOf(GameFeatures.PtsTransport));
            world.SetTrait(0, Trait(world, "PTS_share"), 0.05);
            world.BathConcentration = 0.05;
            double t = 0.0;
            Run(world, 600.0, ref t);
            var cell = world.Cell(0);
            double before = world.CellGlucoseExchanged;
            Run(world, world.MacroDt, ref t);
            Assert.That(cell.CarrierUptake(1), Is.GreaterThan(0.1 * cell.GlucoseUptake), "the PTS carries a real share");
            Assert.That(world.CellGlucoseExchanged - before, Is.EqualTo(cell.GlucoseUptake * world.MacroDt)
                .Within(1e-12 * cell.GlucoseUptake * world.MacroDt), "every carrier's glucose leaves the field");
        }

        [Test]
        public void TheLeakHintReadsGlfsOwnFlux()
        {
            // An H+ symporter pulls glucose in against its gradient; the cytosol rises above the sea and Glf carries some
            // straight back out, while the cell as a whole still takes glucose up.
            using var world = Level2();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets }) { InspectedCell = 0 };
            var frame = loop.CreateFrame();
            var cell = world.Cell(0);
            world.BathConcentration = 0.03;
            double t = 0.0;
            Run(world, 10.0, ref t);
            loop.WriteSnapshot(frame);
            Assert.That(cell.GlfUptake, Is.GreaterThan(0.0), "the starting cell takes glucose in through Glf");
            Assert.That(CellHints.GlfLeaking(loop.Schema, frame, 0), Is.False);

            world.BuyFeature(GameFeatures.IndexOf(GameFeatures.ProtonSymport));
            int galP = Trait(world, "GalP_share");
            world.SetTrait(0, galP, 0.2);
            // the symporters as if already built (building a fifth of the membrane takes hours)
            cell.SetState("pool_GalP__m", 0.2 / cell.AlgebraicValue("pool_GalP__s_unit"));
            Run(world, 300.0, ref t);
            loop.WriteSnapshot(frame);
            Assert.That(cell.State("cytosol__C_glc"), Is.GreaterThan(0.03), "more sugar inside than outside");
            Assert.That(cell.GlfUptake, Is.LessThan(0.0), "Glf leaks");
            Assert.That(frame.Details[Detail(world, "glucose_in")][0], Is.GreaterThan(0.0), "the cell still takes glucose up");
            Assert.That(CellHints.GlfLeaking(loop.Schema, frame, 0), Is.True);
            Assert.That(CellHints.ShareRoom(loop.Schema, frame, 0, galP), Is.EqualTo(1.0 - (cell.MembraneShareUsed() - 0.2)).Within(1e-6),
                "the symporter's slider reaches to the room the other proteins leave");
        }

        [Test]
        public void TheSurfaceHydrolaseDigestsATouchingPacketInTheField()
        {
            // V_hydrolysis_max S / (K_m_poly + S), with S the packet's polymer over a disc of its radius as deep as the cell.
            using var world = Level2(field: true);
            var food = world.Food;
            food.Settings.LyingDensity = 0.0;
            var packet = food.Packets.First();
            world.CellPosition(0, out double x, out double y);
            foreach (var (name, value) in new[] { ("Active", (object)true), ("X", x), ("Y", y), ("Vx", 0.0), ("Vy", 0.0),
                         ("Glucose", food.Settings.PacketAmount), ("Free", 0.0), ("Radius", food.Settings.PacketRadius) })
                typeof(FoodPacket).GetProperty(name)!.SetValue(packet, value);
            foreach (var p in food.Packets.Where(p => p != packet))
                typeof(FoodPacket).GetProperty("Active")!.SetValue(p, false);
            var cell = world.Cell(0);
            double depth = cell.External("h_cell_mod_membrane") * 1e-6;
            double s = packet.Glucose / (Math.PI * packet.Radius * packet.Radius * depth);
            Assert.That(world.PolymerConcentration(packet), Is.EqualTo(s).Within(1e-12 * s));
            double rate = cell.HydrolysisMax * s / (cell.HydrolysisHalfSaturation + s);
            Assert.That(rate, Is.GreaterThan(0.99 * cell.HydrolysisMax), "a fresh packet saturates the enzyme");
            double start = packet.Glucose, released0 = world.Environment!.PacketGlucose, t = 0.0;
            Run(world, 10.0, ref t);
            double released = start - packet.Glucose;
            Assert.That(released, Is.EqualTo(rate * 10.0).Within(1e-4 * released), "the hydrolase's rate over 10 s");
            Assert.That(world.Environment.PacketGlucose - released0, Is.EqualTo(released).Within(1e-12 * released), "into the field");

            // the test hook still overrides the cells' enzymes
            world.HydrolysisCapacity = _ => 0.0;
            double held = packet.Glucose;
            Run(world, 10.0, ref t);
            Assert.That(packet.Glucose, Is.EqualTo(held), "no enzyme, no free part: the polymer stays");
        }
    }
}
