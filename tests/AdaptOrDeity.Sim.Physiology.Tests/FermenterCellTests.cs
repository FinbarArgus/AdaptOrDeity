using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class FermenterCellTests
    {
        private const double GameDt = 0.1;   // FermenterWorld's macro step
        private const double Na = FermenterCell.AvogadroConstant;

        private static FermenterCell NewCell() => new FermenterCell(new Prokaryote_fermenter());

        private static void Run(FermenterCell cell, double seconds)
        {
            long steps = (long)Math.Round(seconds / GameDt);
            for (long i = 0; i < steps; i++)
                cell.Step(GameDt);
        }

        [Test]
        public void DefaultInstanceIsTheCalibratedRestAtGameStep()
        {
            // One hour of backward Euler at the game's 0.1 s step must hold the calibrated rest exactly.
            var cell = NewCell();
            Run(cell, 3600.0);
            Assert.That(cell.Converged, Is.True);
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
            double lynch = 0.39 * Math.Pow(Math.PI * 1.0 * 1.0 * 2.0, 0.88) * 1e9 / 3600.0 / Na;
            Assert.That(b.AtpProduced, Is.EqualTo(b.AtpConsumed).Within(1e-9 * b.AtpConsumed));
            Assert.That(b.AtpConsumed, Is.EqualTo(lynch).Within(1e-6 * lynch));
            Assert.That(b.AtpPumps / b.AtpConsumed, Is.EqualTo(0.25).Within(1e-3));
            Assert.That(b.AtpProteinTurnover / b.AtpConsumed, Is.InRange(0.10, 0.25));
            Assert.That(b.AtpRna / b.AtpConsumed, Is.InRange(0.03, 0.15), "mRNA turnover and ribosome upkeep");
            Assert.That(b.AtpLipids / b.AtpConsumed, Is.InRange(0.01, 0.10));
            Assert.That(b.AtpRepair / b.AtpConsumed, Is.InRange(0.01, 0.10));
            Assert.That(b.AtpProteinBuild + b.AtpGrowth + b.AtpStorage + b.AtpFromGlycogen, Is.EqualTo(0.0));
            // A daughter costs about Lynch & Marinov's growth cost, 2.66e10 V^0.97 ATP (V in um3)
            double growthCost = 26.6e9 * Math.Pow(2 * Math.PI, 0.97) / Na;
            Assert.That(cell.DaughterCost / growthCost, Is.InRange(0.9, 1.3));
        }

        [Test]
        public void RaisingExpressionPaysTheBuildCostOnceWithinTheBuildCapacity()
        {
            // Doubling the H+-ATPase spends exactly the ATP to build the extra copies, never faster than the build
            // capacity (1x the resting turnover), so with a 10 s time constant it still takes minutes.
            var cell = NewCell();
            int trait = cell.Traits.ToList().FindIndex(t => t.Id == "H_ATPase_expression");
            double n = cell.External("n_H_ATPase_mod_protein"), l = cell.External("L_H_ATPase_mod_protein");
            double cBuild = cell.External("c_syn_mod_protein") + cell.External("c_poly_mod_protein");
            cell.Step(GameDt);
            double upkeep0 = cell.AlgebraicValue("protein__J_ATP_H_ATPase");
            cell.SetTrait(trait, 2.0);
            double built = 0.0, peak = 0.0;
            for (int i = 0; i < (int)(3600 / GameDt); i++)
            {
                cell.Step(GameDt);
                built += cell.Budget().AtpProteinBuild * GameDt;
                peak = Math.Max(peak, cell.Budget().AtpProteinBuild);
            }
            double m = cell.TraitValue(trait);
            Assert.That(m, Is.EqualTo(2.0).Within(1e-3));
            Assert.That(built, Is.EqualTo((m - 1.0) * n * l * cBuild / Na).Within(1e-9 * built));
            Assert.That(peak, Is.LessThanOrEqualTo(FermenterTraits.BuildCapacity * cell.RestingTurnover * (1 + 1e-12)));
            Assert.That(cell.AlgebraicValue("protein__J_ATP_H_ATPase"), Is.EqualTo(m * upkeep0).Within(1e-9 * upkeep0));
            Assert.That(cell.State("membrane__V"), Is.LessThan(-105.0), "more pumps hyperpolarise the cell");
        }

        [Test]
        public void TransportersCannotOutgrowTheMembrane()
        {
            // Asking for x4 of every transporter hits the membrane ceiling (8% of the surface): later traits are clamped.
            var cell = NewCell();
            Assert.That(cell.TransporterArea() / cell.SurfaceArea, Is.InRange(0.01, 0.04), "baseline share");
            for (int t = 0; t < cell.Traits.Count; t++)
                cell.SetTrait(t, 4.0);
            Assert.That(cell.TraitTarget(0), Is.EqualTo(4.0), "the first request fits");
            double requested = 0.0;
            for (int t = 0; t < cell.Traits.Count; t++)
                requested += cell.Traits[t].FootprintNm2 * cell.External($"n_{cell.Traits[t].CopyKey}_mod_protein") * 1e-6
                             * cell.TraitTarget(t);
            Assert.That(requested, Is.LessThanOrEqualTo(cell.TransporterAreaMax * (1 + 1e-12)));
            Assert.That(cell.Traits.Select((_, t) => cell.TraitTarget(t)).Min(), Is.LessThan(4.0), "something was clamped");
            Assert.That(cell.TraitTarget(cell.Traits.ToList().FindIndex(t => t.Id == "glycolysis_expression")), Is.EqualTo(4.0),
                "cytosolic enzymes take no membrane");
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
            int p = cell.Batch.Descriptor.StateIndex("bio__P_growth");
            cell.Batch.States[p][0] = cell.DaughterCost;
            double v = cell.State("membrane__V");
            var daughter = cell.Divide();
            Assert.That(daughter, Is.Not.Null);
            Assert.That(daughter!.State("membrane__V"), Is.EqualTo(v));
            Assert.That(cell.GrowthInvested, Is.EqualTo(0.0));
            Assert.That(daughter.GrowthInvested, Is.EqualTo(0.0));
            Assert.That(daughter.GrowthFraction, Is.EqualTo(1.0).Within(1e-12), "controls are inherited");
            daughter.Step(GameDt);
            Assert.That(daughter.Converged, Is.True);
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
            int p = cell.Batch.Descriptor.StateIndex("bio__P_growth");
            double shortfall = Math.Min(0.5 * cell.GlycogenAtp, 0.001 * cell.DaughterCost);
            cell.Batch.States[p][0] = cell.DaughterCost - shortfall;
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
            int d = cell.Batch.Descriptor.StateIndex("bio__D");
            cell.Batch.States[d][0] = 0.999;   // 0.001 of a lethal load left: ~14 min at 20 C
            for (int i = 0; i < 12000 && cell.Alive; i++)
                cell.Step(GameDt);
            Assert.That(cell.Alive, Is.False);
            Assert.That(cell.Budget().AtpRepair, Is.EqualTo(0.0));
        }

        [Test]
        public void FamineAtTheGameStepFollowsTheMyokitGolden()
        {
            // The game's 0.1 s backward Euler against CVODE (rtol 1e-10) through 10 min of famine: NRMSE below 2%.
            string dir = Path.Combine(GoldenDir(), "prokaryote", "fermenter");
            var lines = File.ReadAllLines(Path.Combine(dir, "famine.csv"));
            var header = lines[0].Split(',');
            var rows = lines.Skip(1).Select(l => l.Split(',').Select(double.Parse).ToArray()).ToArray();
            var cell = new FermenterCell(new Prokaryote_fermenter(), "famine");
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
        public void TraitTableMatchesContentFile()
        {
            // content/traits/fermenter.json is the design record; FermenterTraits must say the same.
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoRoot(), "content", "traits", "fermenter.json")));
            var root = doc.RootElement;
            var ranges = root.GetProperty("ranges").GetProperty("expression");
            Assert.That(ranges.GetProperty("min").GetDouble(), Is.EqualTo(FermenterTraits.ExpressionMin));
            Assert.That(ranges.GetProperty("max").GetDouble(), Is.EqualTo(FermenterTraits.ExpressionMax));
            Assert.That(root.GetProperty("expression_time_constant_s").GetDouble(), Is.EqualTo(FermenterTraits.ExpressionTimeConstant));
            Assert.That(root.GetProperty("membrane_share_max").GetDouble(), Is.EqualTo(FermenterTraits.MembraneShareMax));
            Assert.That(root.GetProperty("build_capacity").GetDouble(), Is.EqualTo(FermenterTraits.BuildCapacity));
            var json = root.GetProperty("traits").EnumerateArray().ToArray();
            Assert.That(json.Length, Is.EqualTo(FermenterTraits.All.Count));
            for (int i = 0; i < json.Length; i++)
            {
                var t = FermenterTraits.All[i];
                Assert.That(json[i].GetProperty("id").GetString(), Is.EqualTo(t.Id));
                Assert.That(json[i].GetProperty("protein").GetString(), Is.EqualTo(t.Protein));
                Assert.That(json[i].GetProperty("copy_key").GetString(), Is.EqualTo(t.CopyKey));
                Assert.That(json[i].GetProperty("footprint_nm2").GetDouble(), Is.EqualTo(t.FootprintNm2));
                Assert.That(json[i].GetProperty("parameters").EnumerateArray().Select(p => p.GetString()), Is.EqualTo(t.Parameters));
            }
        }

        [Test]
        public void BackwardEulerIsExactForAStiffLinearDecayAtSteadyState()
        {
            // dy/dt = -k (y - 1) with k = 1e6 /s: explicit steps of 0.1 s would explode; backward Euler converges to 1.
            var kernel = new LinearDecayKernel();
            var batch = new KernelBatch(kernel.Descriptor, 1) { Count = 1 };
            batch.States[0][0] = 5.0;
            var be = new BackwardEulerIntegrator();
            for (int i = 0; i < 5; i++)
                be.Step(kernel, batch, i * 0.1, 0.1, 0, 1);
            Assert.That(batch.States[0][0], Is.EqualTo(1.0).Within(1e-9));
            Assert.That(be.LastConverged, Is.True);
        }

        private sealed class LinearDecayKernel : IKernel
        {
            public KernelDescriptor Descriptor { get; } = new KernelDescriptor("decay", "test", "decay", "CC0-1.0", "",
                new[] { new VariableInfo("y", "dimensionless", 1.0, VariableRole.Variable) }, new VariableInfo[0],
                new VariableInfo[0], new[] { -1 }, new[] { 0.0 }, new PortInfo[0],
                new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IReadOnlyDictionary<string, double>>());

            public void Initialise(KernelBatch batch, int start, int end)
            {
            }

            public void ComputeRates(double t, KernelBatch batch, int start, int end)
            {
                for (int k = start; k < end; k++)
                    batch.Rates[0][k] = -1e6 * (batch.States[0][k] - 1.0);
            }

            public void ComputeVariables(double t, KernelBatch batch, int start, int end)
            {
            }
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
