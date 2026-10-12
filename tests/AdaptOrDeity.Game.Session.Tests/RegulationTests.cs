using System;
using System.Linq;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Regulation by falling metabolites (feature Regulation): it unlocks when the creature first goes the feature's gap
    /// without touching or holding food; then each group's switches (per trait and growth, per species) set the kernel's reg_on_* parameters
    /// of its cells, daughters and released organisms inherit them, and frames carry them.
    /// </summary>
    public class RegulationTests
    {
        private static int Feature(string id) => GameFeatures.IndexOf(id);

        private static int Species(string id) => Array.IndexOf(FermenterCell.RegulationSpecies, id);

        // A world without food or prey, its ocean at the given glucose [mol/m³], no swimming.
        private static FermenterWorld Quiet(int level = 1, double ocean = 0.0)
        {
            var world = new FermenterWorld { Workers = 1 };
            world.Level = level;
            world.SetFeature(Feature(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            world.Prey.SetArrivals(0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            world.BathConcentration = ocean;
            return world;
        }

        private static readonly System.Collections.Generic.Dictionary<FermenterWorld, long> Steps =
            new System.Collections.Generic.Dictionary<FermenterWorld, long>();

        // Steps the world on from where it is (simulated time carries on across calls).
        private static void Run(FermenterWorld world, double seconds)
        {
            Steps.TryGetValue(world, out long done);
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++, done++)
                world.Step(done * world.MacroDt, world.MacroDt);
            Steps[world] = done;
        }

        private static double Gap => GameFeatures.All[Feature(GameFeatures.Regulation)].RequiresFoodGap;

        // Runs a world without food until regulation unlocks (the feature's gap without food).
        private static void Unlock(FermenterWorld world)
        {
            for (int k = 0; k < 3000 && !world.FoodGapMet; k++)
                Run(world, world.MacroDt);
            Assert.That(world.FoodGapMet, Is.True);
        }

        // A fresh packet on cell 0 (it touches it from the next step).
        private static FoodPacket PacketOnCell(FermenterWorld world)
        {
            world.CellPosition(0, out double x, out double y);
            var p = world.Food.AddBody(x + world.CellRadius, y, 0.0, 0.0, world.Food.PacketRadius,
                world.Food.Settings.PacketAmount, world.Food.Settings.FreeFraction * world.Food.Settings.PacketAmount, 0);
            return p;
        }

        private static int Trait(FermenterWorld world, string id) => world.Traits.ToList().FindIndex(t => t.Id == id);

        [Test]
        public void TheFeatureMatchesTheKernelsSpecies()
        {
            var info = GameFeatures.All[Feature(GameFeatures.Regulation)];
            Assert.That(info.UnlockLevel, Is.EqualTo(1));
            Assert.That(info.Cost, Is.EqualTo(0), "free: unlocked by the energy shortage itself");
            Assert.That(info.RequiresFoodGap, Is.EqualTo(30.0), "s: half a minute without food");
            Assert.That(SessionSchema.RegulationSpecies.Select(s => s.Id), Is.EqualTo(FermenterCell.RegulationSpecies));
        }

        [Test]
        public void ItUnlocksAfterTheFirstGapWithoutFoodAndNotBefore()
        {
            int f = Feature(GameFeatures.Regulation);
            // touching food for twice the gap: locked, the gap stays 0
            using var world = Quiet(ocean: 0.1);
            var packet = PacketOnCell(world);
            Run(world, 2.0 * Gap);
            Assert.That(packet.Active, Is.True);
            Assert.That(world.TimeWithoutFood, Is.EqualTo(0.0));
            Assert.That(world.FeatureUnlocked(f), Is.False, "fed: never unlocked");
            Assert.Throws<InvalidOperationException>(() => world.SetRegulation(0, 0, 0, true));

            // the food goes: locked until the gap has passed, unlocked on the step it reaches it
            world.Food.Remove(packet);
            int steps = 0;
            while (!world.FeatureUnlocked(f) && steps < 3000)
            {
                Assert.That(world.TimeWithoutFood, Is.LessThan(Gap));
                Run(world, world.MacroDt);
                steps++;
            }
            Assert.That(steps * world.MacroDt, Is.EqualTo(Gap).Within(world.MacroDt + 1e-9));
            Assert.That(world.FoodGapMet, Is.True);
            Assert.That(world.FeatureEnabled(f), Is.True, "on by default once unlocked");

            // food again and another gap: it stays unlocked, and the gap restarts with the food
            PacketOnCell(world);
            Run(world, 1.0);
            Assert.That(world.TimeWithoutFood, Is.EqualTo(0.0));
            Assert.That(world.FeatureUnlocked(f), Is.True);
        }

        [Test]
        public void FoodInAVacuoleCountsAsFood()
        {
            int f = Feature(GameFeatures.Regulation);
            using var world = Quiet(level: 2, ocean: 0.1);
            var packet = PacketOnCell(world);
            // engulfing is on at level 2: a packet smaller than the cell is swallowed and digested for minutes
            Run(world, 1.0);
            Assert.That(packet.Active, Is.False, "swallowed");
            Assert.That(world.VacuoleOf(0), Is.Not.Null);
            Run(world, 2.0 * Gap);
            Assert.That(world.VacuoleOf(0), Is.Not.Null, "still digesting");
            Assert.That(world.FeatureUnlocked(f), Is.False);
        }

        [Test]
        public void ASwitchSetsTheKernelParameterForEveryCellOfTheGroup()
        {
            using var world = Quiet(ocean: 0.1);
            Unlock(world);
            int atpase = Trait(world, "H_ATPase_expression"), atp = Species("ATP"), ph = Species("H");
            world.SetRegulation(0, atpase, atp, true);
            world.SetRegulation(0, world.GrowthRegulation, ph, true);
            var c = world.Cell(0);
            Assert.That(c.External("reg_on_ATP_mod_pool_H_ATPase"), Is.EqualTo(1.0));
            Assert.That(c.External("reg_on_H_mod_bio"), Is.EqualTo(1.0));
            Assert.That(world.ReadRegulation(0, atpase, atp), Is.True);
            Assert.That(world.ReadRegulation(0, atpase, ph), Is.False);
            int on = c.PlayerParameters.Count(p => p.StartsWith("reg_on_", StringComparison.Ordinal) && c.External(p) != 0.0);
            Assert.That(on, Is.EqualTo(2), "nothing else switched on");
            Assert.That(c.PlayerParameters.Count(p => p.StartsWith("reg_on_", StringComparison.Ordinal)),
                Is.EqualTo((world.Traits.Count + 1) * 4), "every slider's pool and growth, four species each (exported)");

            // turning the feature off clears the kernel's switches; the group keeps the choice for when it is back on
            world.SetFeature(Feature(GameFeatures.Regulation), false);
            Assert.That(c.External("reg_on_ATP_mod_pool_H_ATPase"), Is.EqualTo(0.0));
            Assert.That(world.ReadRegulation(0, atpase, atp), Is.True);
            Assert.Throws<InvalidOperationException>(() => world.SetRegulation(0, atpase, atp, false));
            world.SetFeature(Feature(GameFeatures.Regulation), true);
            Assert.That(c.External("reg_on_ATP_mod_pool_H_ATPase"), Is.EqualTo(1.0));
            world.SetRegulation(0, atpase, atp, false);
            Assert.That(c.External("reg_on_ATP_mod_pool_H_ATPase"), Is.EqualTo(0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.SetRegulation(0, world.GrowthRegulation + 1, 0, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.SetRegulation(0, 0, 4, true));
        }

        [Test]
        public void DaughtersAndReleasedOrganismsInheritTheSwitches()
        {
            using var world = Quiet(level: 2, ocean: 0.1);
            Unlock(world);
            int atpase = Trait(world, "H_ATPase_expression"), atp = Species("ATP");
            world.SetRegulation(0, atpase, atp, true);
            world.SetRegulation(0, world.GrowthRegulation, atp, true);
            // without multicellularity the daughter is released as a free organism with its parent's settings
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            var free = world.Organisms[world.Organisms.Count - 1].Cell;
            Assert.That(free.Regulated(atpase, atp), Is.True);
            Assert.That(free.Regulated(free.GrowthRegulation, atp), Is.True);
            Assert.That(free.Regulated(atpase, Species("G6P")), Is.False);
            // a daughter that stays in the colony is in its parent's group
            world.DevGrantPoints(10);
            world.BuyFeature(Feature(GameFeatures.Multicellularity));
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            Assert.That(world.CellCount, Is.EqualTo(2));
            Assert.That(world.Cell(1).External("reg_on_ATP_mod_pool_H_ATPase"), Is.EqualTo(1.0));
            Assert.That(world.Cell(1).External("reg_on_ATP_mod_bio"), Is.EqualTo(1.0));
        }

        [Test]
        public void FramesCarryEachGroupsSwitches()
        {
            var world = Quiet(ocean: 0.1);
            Unlock(world);
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            int atpase = Trait(world, "H_ATPase_expression"), glc = Species("glc");
            loop.Enqueue(SimCommands.SetRegulation(0, atpase, glc, true));
            loop.Enqueue(SimCommands.SetRegulation(0, loop.Schema.GrowthRegulation, Species("G6P"), true));
            loop.ApplyCommands();
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.Regulation.Length, Is.EqualTo((world.Traits.Count + 1) * 4));
            Assert.That(frame.Regulation[SnapshotFrame.RegulationRow(atpase, glc)][0], Is.True);
            Assert.That(frame.Regulation[SnapshotFrame.RegulationRow(loop.Schema.GrowthRegulation, Species("G6P"))][0], Is.True);
            Assert.That(frame.Regulation.Sum(r => r.Count(x => x)), Is.EqualTo(2));
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var copy = loop.CreateFrame();
            SnapshotCodec.Read(bytes, copy);
            Assert.That(copy.Regulation[SnapshotFrame.RegulationRow(atpase, glc)][0], Is.True);
            Assert.That(copy.Regulation.Sum(r => r.Count(x => x)), Is.EqualTo(2));
            world.Dispose();
        }
    }
}
