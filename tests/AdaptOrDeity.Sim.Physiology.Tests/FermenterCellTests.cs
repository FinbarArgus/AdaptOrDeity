using System;
using System.IO;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    public class FermenterCellTests
    {
        private const double GameDt = 0.1;   // FermenterWorld's macro step

        private static FermenterCell NewCell(string instance = "default") =>
            new FermenterCell(new GenericAdaptiveCell_fermenter(), TraitCatalog.Fermenter, instance);

        private static double Na(FermenterCell cell) => cell.External("N_A_mod_pool_proteome");

        private static void Run(FermenterCell cell, double seconds)
        {
            long steps = (long)Math.Round(seconds / GameDt);
            for (long i = 0; i < steps; i++)
                cell.Step(GameDt);
        }

        [Test]
        public void SleepingCellsStayWithinTheirToleranceOfAwakeOnes()
        {
            // A cell rests, is fed for a minute, then rests again; asleep at 1e-3 it skips a third of its calls and stays within a
            // fraction of a percent of the same cell kept awake (the integrator itself is ~5% from CVODES at the game's
            // tolerance; tools/interop/test_solver_agreement.py).
            var awake = NewCell();
            var sleepy = NewCell();
            sleepy.SleepTolerance = 1e-3;
            int n = awake.Batch.States.Length;
            var scale = new double[n];
            var worst = new double[n];
            for (int s = 0; s < 6000; s++)
            {
                double t = s * GameDt, glucose = t >= 120 && t < 180 ? 50.0 : 0.1;
                awake.MembraneGlucose = sleepy.MembraneGlucose = glucose;
                awake.Step(GameDt);
                sleepy.Step(GameDt);
                for (int i = 0; i < n; i++)
                {
                    scale[i] = Math.Max(scale[i], Math.Abs(awake.Batch.States[i][0]));
                    worst[i] = Math.Max(worst[i], Math.Abs(sleepy.Batch.States[i][0] - awake.Batch.States[i][0]));
                }
            }
            Assert.That(sleepy.SleptSteps, Is.GreaterThan(1500), "a resting cell sleeps through many calls");
            for (int i = 0; i < n; i++)
                Assert.That(worst[i], Is.LessThan(5e-3 * scale[i] + 1e-15), awake.Batch.Descriptor.States[i].Name);
        }

        [Test]
        public void AlgebraicValuesReadAfterStepsAreThoseOfTheStateNow()
        {
            // The kernel's outputs are kept current every step and the rest computed when first read: whatever is read,
            // and in whatever order, it must be what a full ComputeVariables gives for the cell's state now.
            var cell = NewCell();
            cell.MembraneGlucose = 20.0;
            var kernel = new GenericAdaptiveCell_fermenter();
            var reference = new KernelBatch(kernel.Descriptor, 1) { Count = 1 };
            for (int round = 0; round < 3; round++)
            {
                Run(cell, 7.3);
                double thrust = cell.FlagellumThrust;   // an output first, then everything
                for (int i = 0; i < cell.Batch.States.Length; i++)
                    reference.States[i][0] = cell.Batch.States[i][0];
                for (int e = 0; e < cell.Batch.Externals.Length; e++)
                    reference.Externals[e][0] = cell.Batch.Externals[e][0];
                kernel.ComputeVariables(cell.Time, reference, 0, 1);
                Assert.That(thrust, Is.EqualTo(reference.Algebraic[kernel.Descriptor.AlgebraicIndex("i_flagellum__F_one")][0]));
                foreach (var v in kernel.Descriptor.Algebraic)
                    Assert.That(cell.AlgebraicValue(v.Name),
                        Is.EqualTo(reference.Algebraic[kernel.Descriptor.AlgebraicIndex(v.Name)][0]), $"{v.Name} after round {round}");
            }
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
            double na = Na(cell);
            double lynch = 0.39 * Math.Pow(Math.PI * 1.0 * 1.0 * 2.0, 0.88) * 1e9 / 3600.0 / na;
            Assert.That(b.AtpProduced, Is.EqualTo(b.AtpConsumed).Within(1e-9 * b.AtpConsumed));
            Assert.That(b.AtpConsumed, Is.EqualTo(lynch).Within(1e-6 * lynch));
            Assert.That(b.AtpPumps / b.AtpConsumed, Is.EqualTo(0.25).Within(1e-3));
            Assert.That(b.AtpProteinTurnover / b.AtpConsumed, Is.InRange(0.10, 0.25));
            Assert.That(b.AtpRna / b.AtpConsumed, Is.InRange(0.03, 0.15), "mRNA turnover and ribosome upkeep");
            Assert.That(b.AtpLipids / b.AtpConsumed, Is.InRange(0.01, 0.10));
            Assert.That(b.AtpRepair / b.AtpConsumed, Is.InRange(0.01, 0.10));
            Assert.That(b.AtpProteinBuild + b.AtpGrowth + b.AtpStorage + b.AtpFromGlycogen, Is.EqualTo(0.0));
            // A daughter costs about Lynch & Marinov's growth cost, 2.66e10 V^0.97 ATP (V in um3)
            double growthCost = 26.6e9 * Math.Pow(2 * Math.PI, 0.97) / na;
            Assert.That(cell.DaughterCost / growthCost, Is.InRange(0.9, 1.3));
        }

        [Test]
        public void RaisingExpressionPaysTheBuildCostOnceWithinTheBuildCapacity()
        {
            // Doubling the H+-ATPase (its protein_pool, in the kernel) spends exactly the ATP to build the extra copies,
            // never faster than the cell's protein synthesis allows.
            var cell = NewCell();
            int trait = cell.Traits.ToList().FindIndex(t => t.Id == "H_ATPase_expression");
            double n = cell.External("n_0_mod_pool_H_ATPase"), l = cell.External("L_mod_pool_H_ATPase");
            double cBuild = cell.External("c_syn_mod_pool_H_ATPase") + cell.External("c_poly_mod_pool_H_ATPase");
            double cap = cell.External("build_capacity_mod_protein") * cell.RestingTurnover;
            cell.Step(GameDt);
            // upkeep per unit of the ATP factor (synthesis slows a little as ATP moves)
            double upkeep0 = cell.AlgebraicValue("pool_H_ATPase__J_keep") / cell.AlgebraicValue("pool_H_ATPase__f_ATP");
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
            // exact in the model (upstream, under CVODE); here the build power is summed at the 0.1 s world steps while
            // the cell integrates with 1% adaptive steps
            Assert.That(built, Is.EqualTo((m - 1.0) * n * l * cBuild / Na(cell)).Within(1e-2 * built));
            Assert.That(peak, Is.LessThanOrEqualTo(cap * (1 + 1e-9)));
            Assert.That(cell.AlgebraicValue("pool_H_ATPase__J_keep") / cell.AlgebraicValue("pool_H_ATPase__f_ATP"),
                Is.EqualTo(m * upkeep0).Within(1e-9 * m * upkeep0));
            Assert.That(cell.State("membrane__V"), Is.LessThan(-105.0), "more pumps hyperpolarise the cell");
        }

        [Test]
        public void TransportersCannotOutgrowTheMembrane()
        {
            // Asking for x4 of every transporter hits the membrane ceiling (a share of the surface): later traits are clamped.
            var cell = NewCell();
            Assert.That(cell.TransporterArea() / cell.SurfaceArea, Is.InRange(0.02, 0.08), "baseline share");
            for (int t = 0; t < cell.Traits.Count; t++)
                cell.SetTrait(t, 4.0);
            Assert.That(cell.TraitTarget(0), Is.EqualTo(4.0), "the first request fits");
            double requested = 0.0;
            for (int t = 0; t < cell.Traits.Count; t++)
                requested += cell.AlgebraicValue(cell.Traits[t].AreaPerUnit) * cell.TraitTarget(t);
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
            cell.SetState("bio__P_growth", cell.DaughterCost);
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
            // The game's 0.1 s backward Euler against CVODE (rtol 1e-10) through 10 min of famine: NRMSE below 2%.
            string dir = Path.Combine(GoldenDir(), "generic_adaptive_cell", "fermenter");
            var lines = File.ReadAllLines(Path.Combine(dir, "famine.csv"));
            var header = lines[0].Split(',');
            var rows = lines.Skip(1).Select(l => l.Split(',').Select(double.Parse).ToArray()).ToArray();
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
            // target moves its expression there with the pool's time constant.
            var cell = NewCell();
            foreach (var t in cell.Traits)
            {
                Assert.That(cell.External(t.TargetParameter), Is.EqualTo(1.0), t.Id);
                Assert.That(cell.State(t.ExpressionState), Is.EqualTo(1.0), t.Id);
                Assert.That(t.Min, t.Step > 0 ? Is.LessThanOrEqualTo(1.0) : Is.LessThan(1.0), t.Id);
                Assert.That(t.Max, Is.GreaterThan(1.0), t.Id);
            }
            cell.RelativeTolerance = 1e-5;   // this checks the model's time constant, not the game's 1% integration
            int k = cell.Traits.ToList().FindIndex(t => t.Id == "KefC_expression");
            cell.SetTrait(k, 0.5);
            double tau = cell.External("tau_mod_pool_KefC");
            Run(cell, tau);
            Assert.That(cell.TraitValue(k), Is.EqualTo(0.5 + 0.5 * Math.Exp(-1.0)).Within(0.01), "falling needs no ATP: e-fold in tau");
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
            cell.SetExternal("C_glc_e_mod_membrane_face", 50.0);
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
            cell.SetExternal("C_glc_e_mod_membrane_face", 50.0);
            cell.GrowthFraction = 50.0;
            Run(cell, 3600.0);
            double perHour = cell.GrowthProgress;
            TestContext.WriteLine($"daughter built per hour on food: {perHour:P1}, ATP {cell.State("cytosol__C_ATP"):F2} mM, turnover {cell.Budget().AtpConsumed / cell.RestingTurnover:F1}x rest");
            Assert.That(perHour, Is.GreaterThan(0.25), "a daughter in under 4 h");
            Assert.That(cell.Converged, Is.True);
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
