using System;
using System.Linq;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Regulation by falling metabolites (feature Regulation): it unlocks when a cell's ATP first falls below half its
    /// resting level; then each group's switches (per trait and growth, per species) set the kernel's reg_on_* parameters
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

        // Starves the world's cells until regulation unlocks (ATP below half its resting level: about a minute).
        private static void Unlock(FermenterWorld world)
        {
            double ocean = world.BathConcentration;
            world.BathConcentration = 0.0;
            for (int k = 0; k < 3000 && !world.AtpDipped; k++)
                Run(world, world.MacroDt);
            Assert.That(world.AtpDipped, Is.True);
            world.BathConcentration = ocean;
        }

        private static int Trait(FermenterWorld world, string id) => world.Traits.ToList().FindIndex(t => t.Id == id);

        [Test]
        public void TheFeatureMatchesTheKernelsSpecies()
        {
            var info = GameFeatures.All[Feature(GameFeatures.Regulation)];
            Assert.That(info.UnlockLevel, Is.EqualTo(1));
            Assert.That(info.Cost, Is.EqualTo(0), "free: unlocked by the energy shortage itself");
            Assert.That(info.RequiresAtpBelow, Is.EqualTo(0.5));
            Assert.That(SessionSchema.RegulationSpecies.Select(s => s.Id), Is.EqualTo(FermenterCell.RegulationSpecies));
        }

        [Test]
        public void ItUnlocksWhenAtpFirstFallsBelowHalfItsRestingLevelAndNotBefore()
        {
            int f = Feature(GameFeatures.Regulation);
            using var fed = Quiet(ocean: 0.1);
            Run(fed, 120.0);
            Assert.That(fed.Cell(0).RestingAtp, Is.EqualTo(9.6).Within(1e-12), "mol/m³, upstream's resting ATP");
            Assert.That(fed.Cell(0).Atp, Is.GreaterThan(9.0));
            Assert.That(fed.FeatureUnlocked(f), Is.False, "a fed cell at rest never unlocks it");
            Assert.Throws<InvalidOperationException>(() => fed.SetRegulation(0, 0, 0, true));

            // starving: locked while ATP stays above half its resting level, unlocked the step it falls below
            using var starving = Quiet();
            bool below = false;
            for (int k = 0; k < 3000 && !below; k++)
            {
                Assert.That(starving.FeatureUnlocked(f), Is.False, $"step {k}: ATP {starving.Cell(0).Atp:G4} mM");
                Run(starving, starving.MacroDt);
                below = starving.Cell(0).Atp < 0.5 * 9.6;
            }
            Assert.That(below, Is.True);
            Assert.That(starving.FeatureUnlocked(f), Is.True);
            Assert.That(starving.FeatureEnabled(f), Is.True, "on by default once unlocked");
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
