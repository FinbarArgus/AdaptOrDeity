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
            Assert.That(b.AtpProteinBuild, Is.EqualTo(0.0));
        }

        [Test]
        public void RaisingExpressionPaysTheBuildCostOnceAndRaisesUpkeep()
        {
            // Doubling the H+-ATPase must spend exactly the ATP to build the extra copies, n L (c_syn + c_poly) / N_A,
            // spread over the adaptation, and double that protein's turnover cost.
            var cell = NewCell();
            int trait = cell.Traits.ToList().FindIndex(t => t.Id == "H_ATPase_expression");
            double n = cell.External("n_H_ATPase_mod_protein"), l = cell.External("L_H_ATPase_mod_protein");
            double cBuild = cell.External("c_syn_mod_protein") + cell.External("c_poly_mod_protein");
            cell.Step(GameDt);
            double upkeep0 = cell.AlgebraicValue("protein__J_ATP_H_ATPase");

            cell.SetTrait(trait, 2.0);
            double built = 0.0;
            for (int i = 0; i < (int)(12 * cell.ExpressionTimeConstant / GameDt); i++)
            {
                cell.Step(GameDt);
                built += cell.Budget().AtpProteinBuild * GameDt;
            }
            double m = cell.TraitValue(trait);
            Assert.That(m, Is.EqualTo(2.0).Within(1e-4));
            Assert.That(built, Is.EqualTo((m - 1.0) * n * l * cBuild / Na).Within(1e-9 * built));
            Assert.That(cell.AlgebraicValue("protein__J_ATP_H_ATPase"), Is.EqualTo(m * upkeep0).Within(1e-9 * upkeep0));
            Assert.That(cell.External("rho_H_ATPase_mod_i_H_ATPase"), Is.EqualTo(m * 226.81614751349176).Within(1e-9));
            // More pumps hyperpolarise the cell and spend more ATP on pumping.
            Assert.That(cell.State("membrane__V"), Is.LessThan(-105.0));
        }

        [Test]
        public void TraitsClampToTheirRangesAndMidpointsApplyAtOnce()
        {
            var cell = NewCell();
            int mid = cell.Traits.ToList().FindIndex(t => t.Id == "Glf_midpoint");
            int expr = cell.Traits.ToList().FindIndex(t => t.Id == "Glf_expression");
            double k0 = cell.External("K_glc_t_mod_membrane_face");
            cell.SetTrait(mid, 1.5);
            Assert.That(cell.TraitValue(mid), Is.EqualTo(1.1));
            Assert.That(cell.External("K_glc_t_mod_membrane_face"), Is.EqualTo(1.1 * k0).Within(1e-12));
            cell.SetTrait(expr, 100.0);
            Assert.That(cell.TraitTarget(expr), Is.EqualTo(4.0));
            Assert.That(cell.TraitValue(expr), Is.EqualTo(1.0), "expression moves over time, not at once");
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
            var ranges = root.GetProperty("ranges");
            Assert.That(ranges.GetProperty("expression").GetProperty("min").GetDouble(), Is.EqualTo(FermenterTraits.ExpressionMin));
            Assert.That(ranges.GetProperty("expression").GetProperty("max").GetDouble(), Is.EqualTo(FermenterTraits.ExpressionMax));
            Assert.That(ranges.GetProperty("midpoint").GetProperty("min").GetDouble(), Is.EqualTo(FermenterTraits.MidpointMin));
            Assert.That(ranges.GetProperty("midpoint").GetProperty("max").GetDouble(), Is.EqualTo(FermenterTraits.MidpointMax));
            var json = root.GetProperty("traits").EnumerateArray().ToArray();
            Assert.That(json.Length, Is.EqualTo(FermenterTraits.All.Count));
            for (int i = 0; i < json.Length; i++)
            {
                var t = FermenterTraits.All[i];
                Assert.That(json[i].GetProperty("id").GetString(), Is.EqualTo(t.Id));
                Assert.That(json[i].GetProperty("protein").GetString(), Is.EqualTo(t.Protein));
                Assert.That(json[i].GetProperty("kind").GetString(), Is.EqualTo(t.Kind == TraitKind.Expression ? "expression" : "midpoint"));
                Assert.That(json[i].GetProperty("parameters").EnumerateArray().Select(p => p.GetString()), Is.EqualTo(t.Parameters));
            }
            Assert.That(root.GetProperty("expression_time_constant_s").GetDouble(), Is.EqualTo(NewCell().ExpressionTimeConstant));
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
