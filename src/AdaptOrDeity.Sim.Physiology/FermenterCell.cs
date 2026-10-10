using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>The cell's energy budget at one instant: where its ATP comes from and goes [mol ATP/s, W].</summary>
    public struct EnergyBudget
    {
        /// <summary>ATP made by glycolysis from glucose.</summary>
        public double AtpGlycolysis;

        /// <summary>ATP made from glycogen.</summary>
        public double AtpFromGlycogen;

        /// <summary>F1Fo H+-ATPase pumping protons out.</summary>
        public double AtpPumps;

        /// <summary>Turning over (degrading and remaking) the cell's proteins.</summary>
        public double AtpProteinTurnover;

        /// <summary>Building new protein copies while expression rises.</summary>
        public double AtpProteinBuild;

        /// <summary>mRNA turnover (minutes) and ribosome upkeep.</summary>
        public double AtpRna;

        /// <summary>Membrane lipid turnover; scales with membrane area.</summary>
        public double AtpLipids;

        /// <summary>Repairing damage.</summary>
        public double AtpRepair;

        /// <summary>Building a daughter cell.</summary>
        public double AtpGrowth;

        /// <summary>Storing glucose as glycogen.</summary>
        public double AtpStorage;

        /// <summary>The remaining maintenance ATPase: everything not yet itemised.</summary>
        public double AtpOtherMaintenance;

        /// <summary>Heat released [W].</summary>
        public double Heat;

        public double AtpProduced => AtpGlycolysis + AtpFromGlycogen;

        public double AtpConsumed => AtpPumps + AtpProteinTurnover + AtpProteinBuild + AtpRna + AtpLipids + AtpRepair
                                     + AtpGrowth + AtpStorage + AtpOtherMaintenance;
    }

    /// <summary>
    /// The fermenter cell (circulatory-autogen-modules cell/generic_adaptive_cell fermenter, assembled into one kernel),
    /// stepped by backward Euler, stable at macro steps far longer than the Ca buffer's microsecond time constant.
    /// Its physiology is all in the kernel: each protein's expression approaches the target the player sets, building
    /// the new copies costs ATP and is capped and throttled by the cell's protein synthesis, growth is asked in
    /// multiples of the resting turnover and throttled the same way, and the daughter's cost and the share built are
    /// kernel outputs. This class sets targets and controls, keeps targets within the membrane room the proteins leave,
    /// runs the events (division, death) and reads the budget.
    /// </summary>
    public sealed class FermenterCell
    {
        private readonly IKernel _kernel;
        private readonly BackwardEulerIntegrator _integrator = new BackwardEulerIntegrator();
        private readonly int[] _target, _expression, _areaPerUnit;
        private readonly int _growthLevel, _storeAlloc, _repairLevel, _jRef;
        private readonly int _aGly, _aMob, _aStore, _aMaint, _aPump, _aKeep, _aBuild, _aRna, _aRibo, _aLipid, _aRepair;
        private readonly int _aGrowthUsed, _aHeat, _aVol, _aSurface, _aAreaMax, _aDaughter, _aProgress, _aGlycProgress;
        private readonly int _aGlycAtp;
        private readonly int _sDamage, _sGrowth, _sGlycogen;

        public FermenterCell(IKernel kernel, IReadOnlyList<TraitDefinition> traits, string instance = "default")
        {
            _kernel = kernel;
            Traits = traits;
            Batch = new KernelBatch(kernel.Descriptor, 1) { Count = 1 };
            Batch.ApplyInstance(instance, 0, 1);
            _target = new int[traits.Count];
            _expression = new int[traits.Count];
            _areaPerUnit = new int[traits.Count];
            for (int i = 0; i < traits.Count; i++)
            {
                _target[i] = Ext(traits[i].TargetParameter);
                _expression[i] = St(traits[i].ExpressionState);
                _areaPerUnit[i] = Alg(traits[i].AreaPerUnit);
            }
            _growthLevel = Ext("growth_level_mod_bio");
            _storeAlloc = Ext("store_alloc_mod_cytosol");
            _repairLevel = Ext("repair_level_mod_bio");
            _jRef = Ext("J_ref_mod_protein");
            _aGly = Alg("cytosol__J_ATP_gly");
            _aMob = Alg("cytosol__J_ATP_mob");
            _aStore = Alg("cytosol__J_ATP_store");
            _aMaint = Alg("cytosol__J_ATP_maint");
            _aPump = Alg("i_H_ATPase__v_ATP_H");
            _aKeep = Alg("protein__J_keep_total");
            _aBuild = Alg("protein__J_build_total");
            _aRna = Alg("bio__J_RNA");
            _aRibo = Alg("bio__J_ribo");
            _aLipid = Alg("bio__J_lipid");
            _aRepair = Alg("bio__J_repair");
            _aGrowthUsed = Alg("bio__J_growth_used");
            _aHeat = Alg("cytosol__Q_met");
            _aVol = Alg("membrane__Vol");
            _aSurface = Alg("protein__A_surface");
            _aAreaMax = Alg("protein__A_protein_max");
            _aDaughter = Alg("bio__M_daughter");
            _aProgress = Alg("bio__growth_progress");
            _aGlycProgress = Alg("bio__glycogen_progress");
            _aGlycAtp = Alg("cytosol__M_glyc_ATP");
            _sDamage = St("bio__D");
            _sGrowth = St("bio__P_growth");
            _sGlycogen = St("cytosol__C_glyc");
            kernel.Initialise(Batch, 0, 1);
            kernel.ComputeVariables(0.0, Batch, 0, 1);
        }

        // A daughter: same kernel, a copy of the parent's state and parameters (targets and controls included).
        private FermenterCell(IKernel kernel, FermenterCell parent) : this(kernel, parent.Traits)
        {
            for (int i = 0; i < Batch.States.Length; i++)
                Batch.States[i][0] = parent.Batch.States[i][0];
            for (int i = 0; i < Batch.Externals.Length; i++)
                Batch.Externals[i][0] = parent.Batch.Externals[i][0];
            Time = parent.Time;
            _kernel.ComputeVariables(Time, Batch, 0, 1);
        }

        public KernelBatch Batch { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public double Time { get; private set; }

        /// <summary>Resting ATP turnover of the cell [mol/s] (the kernel's J_ref): the scale of growth.</summary>
        public double RestingTurnover => Batch.Externals[_jRef][0];

        /// <summary>False once damage reached a lethal load; a dead cell is no longer stepped.</summary>
        public bool Alive { get; private set; } = true;

        /// <summary>True if the last step's Newton iterations converged.</summary>
        public bool Converged => _integrator.LastConverged;

        /// <summary>Step halvings the last step needed (0 normally; more after abrupt changes).</summary>
        public int LastHalvings { get; private set; }

        /// <summary>Expression now (the pool's state).</summary>
        public double TraitValue(int trait) => Batch.States[_expression[trait]][0];

        public double TraitTarget(int trait) => Batch.Externals[_target[trait]][0];

        /// <summary>Damage, 1 = lethal.</summary>
        public double Damage => Batch.States[_sDamage][0];

        /// <summary>ATP invested in a daughter so far [mol].</summary>
        public double GrowthInvested => Batch.States[_sGrowth][0];

        /// <summary>ATP to build a daughter: its proteins, RNA, lipids and DNA [mol].</summary>
        public double DaughterCost => Batch.Algebraic[_aDaughter][0];

        /// <summary>Fraction of a daughter built.</summary>
        public double GrowthProgress => Batch.Algebraic[_aProgress][0];

        /// <summary>Glycogen [mol/m³ of glucose units].</summary>
        public double Glycogen => Batch.States[_sGlycogen][0];

        /// <summary>ATP the glycogen store would give [mol].</summary>
        public double GlycogenAtp => Batch.Algebraic[_aGlycAtp][0];

        /// <summary>Fraction of a daughter the glycogen store could finish.</summary>
        public double GlycogenProgress => Batch.Algebraic[_aGlycProgress][0];

        /// <summary>True if the daughter is built, or nearly built and glycogen can finish it.</summary>
        public bool CanDivide => Alive && GrowthProgress + GlycogenProgress >= 1.0;

        /// <summary>Cell volume [m³].</summary>
        public double Volume => Batch.Algebraic[_aVol][0];

        /// <summary>Whole cell surface [µm²].</summary>
        public double SurfaceArea => Batch.Algebraic[_aSurface][0];

        /// <summary>Membrane area the proteins may take [µm²]: scales with the cell's surface.</summary>
        public double TransporterAreaMax => Batch.Algebraic[_aAreaMax][0];

        /// <summary>Membrane area the adaptable proteins take now [µm²].</summary>
        public double TransporterArea()
        {
            double a = 0.0;
            for (int i = 0; i < Traits.Count; i++)
                a += Batch.Algebraic[_areaPerUnit[i]][0] * TraitValue(i);
            return a;
        }

        /// <summary>
        /// Sets the trait's expression target, clamped to its range and to the membrane room the other proteins leave
        /// (each counted at the larger of its target and its present expression). The pool approaches it in the kernel.
        /// </summary>
        public void SetTrait(int trait, double multiplier)
        {
            var t = Traits[trait];
            if (double.IsNaN(multiplier))
                throw new ArgumentException("multiplier is NaN", nameof(multiplier));
            double m = Math.Min(t.Max, Math.Max(t.Min, multiplier));
            double unit = Batch.Algebraic[_areaPerUnit[trait]][0];
            if (unit > 0.0)
            {
                double others = 0.0;
                for (int i = 0; i < Traits.Count; i++)
                    if (i != trait)
                        others += Batch.Algebraic[_areaPerUnit[i]][0] * Math.Max(TraitTarget(i), TraitValue(i));
                m = Math.Max(t.Min, Math.Min(m, (TransporterAreaMax - others) / unit));
            }
            if (t.Step > 0.0)
                m = Math.Max(t.Min, Math.Floor(m / t.Step + 1e-9) * t.Step);   // whole units (e.g. flagella) that fit
            Batch.Externals[_target[trait]][0] = m;
        }

        /// <summary>
        /// ATP the player asks to put into growth, as a multiple of the resting ATP turnover (0 = none). The kernel
        /// throttles it as ATP falls, like protein building.
        /// </summary>
        public double GrowthFraction
        {
            get => Batch.Externals[_growthLevel][0];
            set => Batch.Externals[_growthLevel][0] = Math.Max(0.0, value);
        }

        /// <summary>Share of the glycogen storage capacity in use, 0-1.</summary>
        public double StorageAllocation
        {
            get => Batch.Externals[_storeAlloc][0];
            set => Batch.Externals[_storeAlloc][0] = Math.Min(1.0, Math.Max(0.0, value));
        }

        /// <summary>How much the cell invests in repair, 0-1.</summary>
        public double RepairLevel
        {
            get => Batch.Externals[_repairLevel][0];
            set => Batch.Externals[_repairLevel][0] = Math.Min(1.0, Math.Max(0.0, value));
        }

        public void SetExternal(string kernelName, double value) => Batch.Externals[Ext(kernelName)][0] = value;

        public double External(string kernelName) => Batch.Externals[Ext(kernelName)][0];

        public double State(string name) => Batch.States[St(name)][0];

        /// <summary>Sets a state (tests, development aids) and recomputes the kernel's outputs from it.</summary>
        public void SetState(string name, double value)
        {
            Batch.States[St(name)][0] = value;
            _kernel.ComputeVariables(Time, Batch, 0, 1);
        }

        public double AlgebraicValue(string name) => Batch.Algebraic[Alg(name)][0];

        public void Step(double dt)
        {
            if (!Alive)
                return;
            LastHalvings = _integrator.StepRobust(_kernel, Batch, Time, dt, 0, 1);
            Time += dt;
            if (Damage >= 1.0)
                Alive = false;
        }

        /// <summary>
        /// Divides: the daughter is built (glycogen finishes it if needed), both halves keep the concentrations, traits and
        /// controls, and the growth investment restarts. Returns the daughter, or null if the cell cannot divide yet.
        /// </summary>
        public FermenterCell? Divide()
        {
            if (!CanDivide)
                return null;
            double shortfall = Math.Max(0.0, DaughterCost - GrowthInvested);
            if (shortfall > 0.0)
                Batch.States[_sGlycogen][0] = Math.Max(0.0, Glycogen * (1.0 - shortfall / GlycogenAtp));
            Batch.States[_sGrowth][0] = 0.0;
            _kernel.ComputeVariables(Time, Batch, 0, 1);
            return new FermenterCell(_kernel, this);
        }

        /// <summary>The energy budget now [mol ATP/s, W], read from the kernel.</summary>
        public EnergyBudget Budget()
        {
            double[][] a = Batch.Algebraic;
            return new EnergyBudget
            {
                AtpGlycolysis = a[_aGly][0],
                AtpFromGlycogen = a[_aMob][0],
                AtpPumps = a[_aPump][0],
                AtpProteinTurnover = a[_aKeep][0],
                AtpProteinBuild = a[_aBuild][0],
                AtpRna = a[_aRna][0] + a[_aRibo][0],
                AtpLipids = a[_aLipid][0],
                AtpRepair = a[_aRepair][0],
                AtpGrowth = a[_aGrowthUsed][0],
                AtpStorage = a[_aStore][0],
                AtpOtherMaintenance = a[_aMaint][0],
                Heat = a[_aHeat][0],
            };
        }

        private int Ext(string name) => Require(_kernel.Descriptor.ExternalIndex(name), name);

        private int Alg(string name) => Require(_kernel.Descriptor.AlgebraicIndex(name), name);

        private int St(string name) => Require(_kernel.Descriptor.StateIndex(name), name);

        private int Require(int index, string name)
        {
            if (index < 0)
                throw new ArgumentException($"{_kernel.Descriptor.Key} has no variable {name}");
            return index;
        }
    }
}
