using System;
using System.IO;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class FermenterCellTests
    {
        private const double GameDt = 0.1;   // FermenterWorld's macro step

        private static FermenterCell NewCell(string instance = "default") =>
            new FermenterCell(TraitCatalog.Fermenter, instance);

        private static double Na(FermenterCell cell) => cell.External("N_A_mod_pool_proteome");

        private static void Run(FermenterCell cell, double seconds)
        {
            long steps = (long)Math.Round(seconds / GameDt);
            for (long i = 0; i < steps; i++)
                cell.Step(GameDt);
        }

        [Test]
        public void AlgebraicValuesReadAfterStepsAreThoseOfTheStateNow()
        {
            // The module's outputs are kept current every step and the rest computed when first read: whatever is read,
            // and in whatever order, it must be what a cell given the same state and inputs computes in full.
            var cell = NewCell();
            cell.MembraneGlucose = 20.0;
            var d = cell.Batch.Descriptor;
            for (int round = 0; round < 3; round++)
            {
                Run(cell, 7.3);
                double thrust = cell.FlagellumThrust;   // an output first, then everything
                var reference = NewCell();
                foreach (var v in d.Externals)
                    reference.SetExternal(v.Name, cell.External(v.Name));
                foreach (var v in d.States)
                    reference.SetState(v.Name, cell.State(v.Name));   // each set recomputes every value
                Assert.That(thrust, Is.EqualTo(reference.AlgebraicValue("i_flagellum__F_one")));
                foreach (var v in d.Algebraic)
                    Assert.That(cell.AlgebraicValue(v.Name), Is.EqualTo(reference.AlgebraicValue(v.Name)), $"{v.Name} after round {round}");
            }
        }

        [Test]
        public void DefaultInstanceIsTheCalibratedRestAtGameStep()
        {
            // One hour at the game's 0.1 s step must hold the calibrated rest exactly.
            var cell = NewCell();
            Run(cell, 3600.0);
            Assert.That(cell.Alive, Is.True);
            Assert.That(cell.State("membrane__V"), Is.EqualTo(-105.0).Within(1e-4));
            Assert.That(cell.AlgebraicValue("H__pH_i"), Is.EqualTo(7.6).Within(1e-7));
            Assert.That(cell.State("cytosol__C_ATP"), Is.EqualTo(9.6).Within(1e-6));
            Assert.That(cell.State("Na_K__Nai"), Is.EqualTo(20.0).Within(1e-5));
        }

        [Test]
        public void EnergyBudgetBalancesAndMatchesLynchMarinovMaintenance()
        {
            // At rest ATP made = ATP used, and the turnover is Lynch & Marinov's maintenance cost for 2pi um3 at 20 C.
            var cell = NewCell();
            cell.Step(GameDt);
            var b = cell.Budget();
            double na = Na(cell);
            double lynch = 0.39 * Math.Pow(Math.PI * 1.0 * 1.0 * 2.0, 0.88) * 1e9 / 3600.0 / na;
            Assert.That(b.AtpProduced, Is.EqualTo(b.AtpConsumed).Within(1e-9 * b.AtpConsumed));
            Assert.That(b.AtpConsumed, Is.EqualTo(lynch).Within(1e-6 * lynch));
            Assert.That(b.AtpPumps / b.AtpConsumed, Is.EqualTo(0.25).Within(1e-3));
            Assert.That(b.AtpProteinTurnover / b.AtpConsumed, Is.InRange(0.10, 0.25));
            Assert.That(b.AtpRna / b.AtpConsumed, Is.InRange(0.03, 0.15), "mRNA turnover and ribosome upkeep");
            Assert.That(b.AtpLipids / b.AtpConsumed, Is.InRange(0.01, 0.10));
            Assert.That(b.AtpRepair / b.AtpConsumed, Is.InRange(0.01, 0.10));
            // nothing built or stored at rest (to round-off: the integrator's first step probes the rates once)
            Assert.That(b.AtpProteinBuild + b.AtpGrowth + b.AtpStorage + b.AtpFromGlycogen, Is.EqualTo(0.0).Within(1e-10 * b.AtpConsumed));
            // A daughter costs about Lynch & Marinov's growth cost, 2.66e10 V^0.97 ATP (V in um3)
            double growthCost = 26.6e9 * Math.Pow(2 * Math.PI, 0.97) / na;
            Assert.That(cell.DaughterCost / growthCost, Is.InRange(0.9, 1.3));
        }

        [Test]
        public void RaisingExpressionPaysTheBuildCostOnceWithinTheBuildCapacity()
        {
            // Doubling the H+-ATPase (its protein_pool, in the kernel) spends exactly the ATP to build the extra copies,
            // never faster than the cell's protein synthesis allows. It rises with its mRNA's mean lifetime (~7 min), slowed
            // on the lean background by the ATP it has to spare: two hours.
            var cell = NewCell();
            int trait = cell.Traits.ToList().FindIndex(t => t.Id == "H_ATPase_expression");
            double n = cell.External("n_0_mod_pool_H_ATPase"), l = cell.External("L_mod_pool_H_ATPase");
            double cBuild = cell.External("c_syn_mod_pool_H_ATPase") + cell.External("c_poly_mod_pool_H_ATPase");
            double cap = cell.External("build_capacity_mod_protein") * cell.RestingTurnover;
            cell.Step(GameDt);
            // upkeep per unit of the ATP factor (synthesis slows a little as ATP moves)
            double upkeep0 = cell.AlgebraicValue("pool_H_ATPase__J_keep") / Math.Min(cell.AlgebraicValue("pool_H_ATPase__f_ATP"), 1.0);
            cell.SetTrait(trait, 2.0);
            double built = 0.0, peak = 0.0;
            for (int i = 0; i < (int)(7200 / GameDt); i++)
            {
                cell.Step(GameDt);
                built += cell.Budget().AtpProteinBuild * GameDt;
                peak = Math.Max(peak, cell.Budget().AtpProteinBuild);
            }
            double m = cell.TraitValue(trait);
            Assert.That(m, Is.EqualTo(2.0).Within(1e-3));
            // exact in the model (upstream, under CVODE); here the build power is summed at the 0.1 s world steps while
            // the cell integrates with 1% adaptive steps
            Assert.That(built, Is.EqualTo((m - 1.0) * n * l * cBuild / Na(cell)).Within(1e-2 * built));
            Assert.That(peak, Is.LessThanOrEqualTo(cap * (1 + 1e-9)));
            Assert.That(cell.AlgebraicValue("pool_H_ATPase__J_keep") / Math.Min(cell.AlgebraicValue("pool_H_ATPase__f_ATP"), 1.0),
                Is.EqualTo(m * upkeep0).Within(1e-9 * m * upkeep0));
            Assert.That(cell.State("membrane__V"), Is.LessThan(-105.0), "more pumps hyperpolarise the cell");
        }

        [Test]
        public void TransportersCannotOutgrowTheMembrane()
        {
            // The membrane proteins take shares of a ceiling (15% of the surface): 36.8% of it at rest, the hydrolase and
            // the flagellum included. Asking for x4 of every protein and all of the ceiling for each new carrier hits it:
            // later traits are clamped, and the shares the targets claim sum to at most 1.
            var cell = NewCell();
            Assert.That(cell.TransporterAreaMax / cell.SurfaceArea, Is.EqualTo(0.15).Within(1e-12), "the ceiling");
            Assert.That(cell.MembraneShareUsed(), Is.EqualTo(0.3681).Within(5e-4), "at rest");
            Assert.That(cell.MembraneShareUsed() * cell.TransporterAreaMax / cell.SurfaceArea, Is.EqualTo(0.055).Within(5e-4),
                "5.5% of the surface");
            for (int t = 0; t < cell.Traits.Count; t++)
                cell.SetTrait(t, cell.Traits[t].Kind == TraitKind.Share ? 1.0 : 4.0);
            Assert.That(cell.TraitTarget(0), Is.EqualTo(4.0), "the first request fits");
            Assert.That(cell.MembraneShareClaimed(), Is.LessThanOrEqualTo(1.0 + 1e-12));
            Assert.That(cell.MembraneShareClaimed(), Is.EqualTo(1.0).Within(1e-9), "the ceiling is used up");
            Assert.That(Enumerable.Range(0, cell.Traits.Count).Count(t => cell.Traits[t].Kind == TraitKind.Expression
                && cell.TraitTarget(t) < 4.0), Is.GreaterThan(0), "something was clamped");
            Assert.That(cell.TraitTarget(cell.Traits.ToList().FindIndex(t => t.Id == "glycolysis_expression")), Is.EqualTo(4.0),
                "cytosolic enzymes take no membrane");
            int gal = cell.Traits.ToList().FindIndex(t => t.Id == "GalP_share");
            Assert.That(cell.TraitTarget(gal), Is.EqualTo(0.0), "no room left for the last new carrier");
        }

        [Test]
        public void ANewCarrierTakesTheShareOfTheCeilingAsked()
        {
            // A share trait's target is its share of the ceiling (s_target); the pool builds copies until they take it.
            var cell = NewCell();
            cell.MembraneGlucose = 50.0;   // food: ATP to spare for building
            int pts = cell.Traits.ToList().FindIndex(t => t.Id == "PTS_share");
            Assert.That(cell.TraitValue(pts), Is.EqualTo(0.0));
            cell.SetTrait(pts, 0.05);
            Assert.That(cell.External("s_target_mod_pool_PTS"), Is.EqualTo(0.05));
            Assert.That(cell.TraitTarget(pts), Is.EqualTo(0.05));
            // a protein rises with its mRNA's mean lifetime (tau ~7 min): within 1e-3 of the share after ~9 tau
            Run(cell, 4000.0);
            Assert.That(cell.TraitValue(pts), Is.EqualTo(0.05).Within(1e-3));
            Assert.That(cell.AlgebraicValue("pool_PTS__share"), Is.EqualTo(cell.TraitValue(pts)).Within(1e-12));
            Assert.That(cell.MembraneShareUsed(), Is.EqualTo(0.3681 + 0.05).Within(1e-3));
            Assert.That(cell.CarrierUptake(1), Is.GreaterThan(0.0), "the PTS brings glucose in");
            // more than the room left is clamped to it
            cell.SetTrait(pts, 1.0);
            Assert.That(cell.MembraneShareClaimed(), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(cell.TraitTarget(pts), Is.EqualTo(1.0 - cell.MembraneShareClaimed(pts)).Within(1e-12));
        }

        [Test]
        public void EveryGlucoseCarrierSeesTheMediumAndUptakeIsTheirSum()
        {
            var cell = NewCell();
            cell.MembraneGlucose = 0.3;
            foreach (var carrier in FermenterCell.Carriers)
                Assert.That(cell.External("C_glc_e_mod_" + carrier), Is.EqualTo(0.3), carrier);
            int pts = cell.Traits.ToList().FindIndex(t => t.Id == "PTS_share");
            int abc = cell.Traits.ToList().FindIndex(t => t.Id == "ABC_share");
            int gal = cell.Traits.ToList().FindIndex(t => t.Id == "GalP_share");
            cell.SetTrait(pts, 0.02);
            cell.SetTrait(abc, 0.02);
            cell.SetTrait(gal, 0.02);
            Run(cell, 300.0);
            double sum = cell.AlgebraicValue("membrane_face__J_glc") + cell.AlgebraicValue("glc_PTS__J_glc")
                         + cell.AlgebraicValue("glc_ABC__J_glc") + cell.AlgebraicValue("glc_GalP__J_glc");
            for (int k = 1; k < FermenterCell.Carriers.Length; k++)
                Assert.That(cell.CarrierUptake(k), Is.Not.EqualTo(0.0), FermenterCell.Carriers[k]);
            Assert.That(cell.GlucoseUptake, Is.EqualTo(sum).Within(1e-15 * Math.Abs(sum)));
            Assert.That(cell.AlgebraicValue("glc_PTS__J_G6P"), Is.EqualTo(cell.CarrierUptake(1)), "the PTS delivers it as G6P");
            Assert.That(cell.GlucoseRelease, Is.EqualTo(-sum).Within(1e-15 * Math.Abs(sum)));
            var b = cell.Budget();
            Assert.That(b.AtpCarriers, Is.EqualTo(cell.AlgebraicValue("glc_PTS__J_glc") + 2.0 * cell.AlgebraicValue("glc_ABC__J_glc"))
                .Within(1e-12 * b.AtpCarriers), "a PEP per PTS glucose, 2 ATP per ABC glucose");
        }

        [Test]
        public void TheSurfaceHydrolaseSaturatesWithPolymer()
        {
            var cell = NewCell();
            double vmax = cell.HydrolysisMax, km = cell.HydrolysisHalfSaturation;
            Assert.That(vmax, Is.EqualTo(cell.External("kcat_mod_hydrolase") * cell.External("n_0_mod_hydrolase")
                                         * cell.State("pool_Hydrolase__m") / Na(cell)).Within(1e-12 * vmax));
            Assert.That(km, Is.EqualTo(20.0), "K_m_poly, mM of glucose units");
            Assert.That(cell.Hydrolysis(km), Is.EqualTo(0.5 * vmax).Within(1e-15 * vmax));
            Assert.That(cell.Hydrolysis(0.0), Is.EqualTo(0.0));
        }

        [Test]
        public void GrowthFillsTheDaughterAndDivisionCopiesTheCell()
        {
            var cell = NewCell();
            cell.GrowthFraction = 1.0;
            for (int i = 0; i < 600; i++)
                cell.Step(GameDt);
            Assert.That(cell.GrowthInvested, Is.GreaterThan(0.5 * 60 * cell.RestingTurnover), "a minute at ~1x resting use");
            Assert.That(cell.CanDivide, Is.False);
            Assert.That(cell.Divide(), Is.Null);

            // Finish the daughter (as if after days of growth): division copies the state and restarts growth.
            cell.SetState("bio__P_growth", cell.DaughterCost);
            double v = cell.State("membrane__V");
            var daughter = cell.Divide();
            Assert.That(daughter, Is.Not.Null);
            Assert.That(daughter!.State("membrane__V"), Is.EqualTo(v));
            Assert.That(cell.GrowthInvested, Is.EqualTo(0.0));
            Assert.That(daughter.GrowthInvested, Is.EqualTo(0.0));
            Assert.That(daughter.GrowthFraction, Is.EqualTo(1.0).Within(1e-12), "controls are inherited");
            daughter.Step(GameDt);
            Assert.That(daughter.Alive, Is.True);
        }

        [Test]
        public void GlycogenStoresUnderAllocationAndFinishesADaughter()
        {
            var cell = NewCell();
            cell.StorageAllocation = 1.0;
            for (int i = 0; i < 3000; i++)
                cell.Step(GameDt);
            // An uptake-limited cell (5 uM glucose inside) stores slowly from 1 mM; on rich food it stores faster.
            Assert.That(cell.Glycogen, Is.GreaterThan(0.1));
            Assert.That(cell.Budget().AtpStorage, Is.GreaterThan(0.0));
            // A daughter 99.9% built: glycogen covers the rest, and exactly the shortfall is spent.
            double shortfall = Math.Min(0.5 * cell.GlycogenAtp, 0.001 * cell.DaughterCost);
            cell.SetState("bio__P_growth", cell.DaughterCost - shortfall);
            double before = cell.GlycogenAtp;
            Assert.That(cell.CanDivide, Is.True);
            Assert.That(cell.Divide(), Is.Not.Null);
            Assert.That(before - cell.GlycogenAtp, Is.EqualTo(shortfall).Within(1e-9 * shortfall));
        }

        [Test]
        public void WithoutRepairDamageKillsTheCell()
        {
            var cell = NewCell();
            Assert.That(cell.Damage, Is.InRange(0.005, 0.02), "steady damage at full repair");
            cell.RepairLevel = 0.0;
            cell.SetState("bio__D", 0.999);   // 0.001 of a lethal load left: ~14 min at 20 C
            for (int i = 0; i < 12000 && cell.Alive; i++)
                cell.Step(GameDt);
            Assert.That(cell.Alive, Is.False);
            Assert.That(cell.Budget().AtpRepair, Is.EqualTo(0.0));
        }

        [Test]
        public void FamineAtTheGameStepFollowsTheMyokitGolden()
        {
            // The game's cell at its 0.1 s step against CVODE (rtol 1e-10) through 10 min of famine: NRMSE below 2%.
            string dir = Path.Combine(GoldenDir(), "generic_adaptive_cell", "fermenter");
            var lines = File.ReadAllLines(Path.Combine(dir, "famine.csv"));
            var header = lines[0].Split(',');
            // Myokit writes inf where a quantity divides by a zero (the PTS's mass-action ratio over no glucose outside)
            var rows = lines.Skip(1).Select(l => l.Split(',').Select(x => x switch
            {
                "inf" => double.PositiveInfinity,
                "-inf" => double.NegativeInfinity,
                _ => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture),
            }).ToArray()).ToArray();
            var cell = NewCell("famine");
            foreach (var name in new[] { "membrane__V", "cytosol__C_ATP", "cytosol__C_glc", "H__H_i" })
            {
                int c = Array.IndexOf(header, name);
                Assert.That(c, Is.GreaterThan(0), name);
            }
            var sim = rows.Select(_ => new double[4]).ToArray();
            string[] names = { "membrane__V", "cytosol__C_ATP", "cytosol__C_glc", "H__H_i" };
            double t = 0.0;
            for (int r = 0; r < rows.Length; r++)
            {
                while (t < rows[r][0] - 1e-9)
                {
                    cell.Step(GameDt);
                    t += GameDt;
                }
                for (int j = 0; j < names.Length; j++)
                    sim[r][j] = cell.State(names[j]);
            }
            Assert.That(rows.Min(x => x[Array.IndexOf(header, "cytosol__C_ATP")]), Is.GreaterThanOrEqualTo(0.0),
                "ATP runs down to zero in famine, never below");
            for (int j = 0; j < names.Length; j++)
            {
                int c = Array.IndexOf(header, names[j]);
                var expected = rows.Select(x => x[c]).ToArray();
                var actual = sim.Select(x => x[j]).ToArray();
                double range = expected.Max() - expected.Min();
                double rmse = Math.Sqrt(expected.Zip(actual, (e, a) => (e - a) * (e - a)).Average());
                double nrmse = rmse / (range > 1e-12 * Math.Abs(expected.Max()) ? range : Math.Abs(expected.Max()));
                TestContext.WriteLine($"famine {names[j]}: NRMSE {nrmse:E2}");
                Assert.That(nrmse, Is.LessThan(0.02), names[j]);
            }
        }

        [Test]
        public void EveryTraitIsAPoolOfTheKernel()
        {
            // content/traits/fermenter.json names each trait's protein_pool; the kernel must have it, and a cell with a new
            // target lower than its copies loses them as they are degraded (about 1% an hour).
            var cell = NewCell();
            for (int i = 0; i < cell.Traits.Count; i++)
            {
                var t = cell.Traits[i];
                Assert.That(cell.TraitTarget(i), Is.EqualTo(t.Default), t.Id);
                Assert.That(cell.State(t.ExpressionState), Is.EqualTo(t.Default), t.Id);
                // a membrane protein's target is its share of the ceiling; at rest that is its share now
                bool share = cell.TargetParameter(i).StartsWith("s_target_mod_");
                Assert.That(share, Is.EqualTo(t.Kind == TraitKind.Share || t.Pool != "pool_Glyc" && t.Kind != TraitKind.Count), t.Id);
                if (share)
                    Assert.That(cell.External(cell.TargetParameter(i)), Is.EqualTo(cell.TraitShare(i)).Within(1e-9), t.Id);
                if (t.Kind == TraitKind.Share)
                {
                    Assert.That((t.Min, t.Max), Is.EqualTo((0.0, 1.0)), t.Id);
                    Assert.That(t.Feature, Is.Not.Null, t.Id);
                    continue;
                }
                Assert.That(t.Min, t.Step > 0 ? Is.LessThanOrEqualTo(1.0) : Is.LessThan(1.0), t.Id);
                Assert.That(t.Max, Is.GreaterThan(1.0), t.Id);
            }
            cell.RelativeTolerance = 1e-5;   // this checks the model's time constant, not the game's 1% integration
            int k = cell.Traits.ToList().FindIndex(t => t.Id == "KefC_expression");
            cell.SetTrait(k, 0.5);
            double kDeg = cell.External("k_deg_mod_pool_KefC");
            Assert.That(kDeg, Is.EqualTo(0.01 / 3600.0).Within(1e-12 * kDeg), "about 1% of the protein degraded per hour");
            Run(cell, 3600.0);
            Assert.That(cell.TraitValue(k), Is.EqualTo(0.5 + 0.5 * Math.Exp(-kDeg * 3600.0)).Within(1e-4),
                "above its target a protein falls only as it is degraded, not replaced");
        }

        [Test]
        public void TheFlagellumRunsOnTheProtonMotiveForceAndCostsATP()
        {
            // Motor on: H+ flow in through the motor (~1,200 per turn at ~100 Hz), the H+-ATPase works harder, the cell
            // swims ~15 um/s with one flagellum; four flagella swim faster but less than four times as fast.
            var cell = NewCell();
            cell.Step(GameDt);
            double pumps0 = cell.Budget().AtpPumps;
            cell.SetExternal("a_motor_mod_i_flagellum", 1.0);
            Run(cell, 60.0);
            double j = cell.AlgebraicValue("i_flagellum__j_H_flag") * Na(cell);
            Assert.That(j, Is.InRange(0.8e5, 1.6e5), "H+/s through one motor");
            // the H+-ATPase speeds up as the cytosol acidifies; the H+ leak and exchangers carry less as the PMF drops
            Assert.That(cell.Budget().AtpPumps, Is.GreaterThan(pumps0 * 1.02), "pumping replaces the protons the motor uses");
            double v1 = cell.AlgebraicValue("i_flagellum__v_swim");
            Assert.That(v1, Is.InRange(10e-6, 20e-6));
            int f = cell.Traits.ToList().FindIndex(t => t.Id == "flagella");
            cell.SetTrait(f, 4.0);
            Assert.That(cell.TraitTarget(f), Is.EqualTo(4.0));
            cell.SetTrait(f, 2.6);
            Assert.That(cell.TraitTarget(f), Is.EqualTo(2.0), "whole flagella");
            // building three more flagella (~8e8 ATP) needs spare ATP: on the lean background there is none, on food plenty
            cell.SetTrait(f, 4.0);
            Run(cell, 600.0);
            Assert.That(cell.TraitValue(f), Is.LessThan(3.0), "little spare ATP off food");
            cell.MembraneGlucose = 50.0;
            Run(cell, 1800.0);
            Assert.That(cell.TraitValue(f), Is.EqualTo(4.0).Within(0.05), "built within half an hour on food");
            double v4 = cell.AlgebraicValue("i_flagellum__v_swim");
            Assert.That(v4, Is.GreaterThan(1.5 * v1));
            Assert.That(v4, Is.LessThan(4.0 * v1));
            Assert.That(cell.Alive, Is.True);
        }

        [Test]
        public void OnRichFoodACellCanBuildADaughterInHours()
        {
            // At rest on the lean background the cell only maintains itself; touching food (50 mM at the membrane) its
            // carriers and glycolysis have the headroom of a growing fermenter (~40x maintenance), so growth asked at the
            // maximum builds a daughter in a few hours at 20 C.
            var cell = NewCell();
            cell.MembraneGlucose = 50.0;
            cell.GrowthFraction = 50.0;
            Run(cell, 3600.0);
            double perHour = cell.GrowthProgress;
            TestContext.WriteLine($"daughter built per hour on food: {perHour:P1}, ATP {cell.State("cytosol__C_ATP"):F2} mM, turnover {cell.Budget().AtpConsumed / cell.RestingTurnover:F1}x rest");
            Assert.That(perHour, Is.GreaterThan(0.25), "a daughter in under 4 h");
            Assert.That(cell.Alive, Is.True);
        }

        private static string RepoRoot()
        {
            var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "global.json")))
                d = d.Parent;
            return d?.FullName ?? throw new DirectoryNotFoundException("repo root");
        }

        private static string GoldenDir() => Path.Combine(RepoRoot(), "tests", "golden");
    }
}
