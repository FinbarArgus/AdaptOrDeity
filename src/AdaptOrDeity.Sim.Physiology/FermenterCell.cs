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
    /// The fermenter cell (circulatory-autogen-modules cell/generic_adaptive_cell fermenter, assembled into one module),
    /// integrated by libcuflynx's generated C (<see cref="NativeInstance"/>: adaptive TR-BDF2, stable at world steps far
    /// longer than the Ca buffer's microsecond time constant). Its physiology is all in the module: each protein's
    /// expression approaches the target the player sets, building the new copies costs ATP and is capped and throttled
    /// by the cell's protein synthesis, growth is asked in multiples of the resting turnover and throttled the same
    /// way, and the daughter's cost and the share built are module outputs. This class sets targets and controls, keeps
    /// targets within the membrane room the proteins leave, runs the events (division, death) and reads the budget.
    /// </summary>
    public sealed class FermenterCell
    {
        private readonly NativeModule _module;
        private readonly NativeInstance _native;
        private readonly bool[] _isOutput;
        private bool _variablesCurrent;
        private readonly int[] _target, _expression, _areaPerUnit;
        private readonly int _growthLevel, _storeAlloc, _repairLevel, _jRef;
        private readonly int _aGly, _aMob, _aStore, _aMaint, _aPump, _aKeep, _aBuild, _aRna, _aRibo, _aLipid, _aRepair;
        private readonly int _aGrowthUsed, _aHeat, _aVol, _aSurface, _aAreaMax, _aDaughter, _aProgress, _aGlycProgress;
        private readonly int _aGlycAtp, _aUptake, _aThrust, _aFlagPower, _eGlucose, _eMotor;
        private readonly int _aGlcOut, _aLacOut, _aHLac, _eLactate, _eHe, _eHo;
        private readonly int _sDamage, _sGrowth, _sGlycogen;

        /// <summary>The fermenter module in the game's module library.</summary>
        public static NativeModule Module => NativeModule.Get("generic_adaptive_cell", "fermenter");

        /// <summary>Relative tolerance new cells are integrated at.</summary>
        public static double DefaultRelativeTolerance { get; set; } = 1e-4;

        /// <summary>A cell of the fermenter module with the named parameter set's values and initial values.</summary>
        public FermenterCell(IReadOnlyList<TraitDefinition> traits, string instance = "default")
            : this(Module, traits, instance, null)
        {
        }

        // A new cell, or a daughter: same module, a copy of the parent's state, parameters and integration.
        private FermenterCell(NativeModule module, IReadOnlyList<TraitDefinition> traits, string instance, FermenterCell? parent)
        {
            _module = module;
            var d = module.Descriptor;
            Traits = traits;
            Instance = instance;
            Batch = new KernelBatch(d, 1) { Count = 1 };
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
            _growthLevel = Ext(GrowthParameter);
            _storeAlloc = Ext(StorageParameter);
            _repairLevel = Ext(RepairParameter);
            var player = new List<string>();
            foreach (var t in traits)
                player.Add(t.TargetParameter);
            player.Add(GrowthParameter);
            player.Add(StorageParameter);
            player.Add(RepairParameter);
            PlayerParameters = player;
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
            _aUptake = Alg("membrane_face__J_glc");
            _aThrust = Alg("i_flagellum__F_one");
            _aFlagPower = Alg("i_flagellum__P_flag");
            _eGlucose = Ext("C_glc_e_mod_membrane_face");
            _eMotor = Ext("a_motor_mod_i_flagellum");
            _aGlcOut = Alg("membrane_face__J_glc_e");
            _aLacOut = Alg("membrane_face__J_lac_e");
            _aHLac = Alg("membrane_face__J_H_lac");
            _eLactate = Ext("C_lac_e_mod_membrane_face");
            _eHe = Ext("H_e_mod_membrane_face");
            _eHo = Ext("H_o_mod_H");
            _sDamage = St("bio__D");
            _sGrowth = St("bio__P_growth");
            _sGlycogen = St("cytosol__C_glyc");
            _isOutput = new bool[d.Algebraic.Count];
            foreach (var (k, _) in module.Outputs)
                _isOutput[k] = true;
            if (parent == null)
            {
                _native = module.CreateInstance(DefaultRelativeTolerance);
                _native.InitialiseStates(Batch);
                _native.ComputeVariables(Batch);
            }
            else
            {
                _native = parent._native.Copy();
                for (int i = 0; i < Batch.States.Length; i++)
                    Batch.States[i][0] = parent.Batch.States[i][0];
                for (int i = 0; i < Batch.Externals.Length; i++)
                    Batch.Externals[i][0] = parent.Batch.Externals[i][0];
                Time = parent.Time;
                Recompute();
            }
            _variablesCurrent = true;
        }

        private const string GrowthParameter = "growth_level_mod_bio", StorageParameter = "store_alloc_mod_cytosol",
            RepairParameter = "repair_level_mod_bio";

        public KernelBatch Batch { get; }

        /// <summary>The module's parameter set the cell started from.</summary>
        public string Instance { get; }

        /// <summary>Module parameters the player sets: each trait's target, then the growth, storage and repair controls.</summary>
        public IReadOnlyList<string> PlayerParameters { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public double Time { get; private set; }

        /// <summary>Resting ATP turnover of the cell [mol/s] (the module's J_ref): the scale of growth.</summary>
        public double RestingTurnover => Batch.Externals[_jRef][0];

        /// <summary>False once damage reached a lethal load; a dead cell is no longer stepped.</summary>
        public bool Alive { get; private set; } = true;

        /// <summary>Expression now (the pool's state).</summary>
        public double TraitValue(int trait) => Batch.States[_expression[trait]][0];

        public double TraitTarget(int trait) => Batch.Externals[_target[trait]][0];

        /// <summary>Damage, 1 = lethal.</summary>
        public double Damage => Batch.States[_sDamage][0];

        /// <summary>Glucose at the membrane [mol/m³] (input).</summary>
        public double MembraneGlucose
        {
            get => Batch.Externals[_eGlucose][0];
            set => Batch.Externals[_eGlucose][0] = value;
        }

        /// <summary>Lactate at the membrane [mol/m³] (input).</summary>
        public double MembraneLactate
        {
            get => Batch.Externals[_eLactate][0];
            set => Batch.Externals[_eLactate][0] = value;
        }

        /// <summary>Free H+ outside the membrane [mol/m³] (input: the lactate carrier's H_e and the proton balance's H_o).</summary>
        public double MembraneProtons
        {
            get => Batch.Externals[_eHe][0];
            set
            {
                Batch.Externals[_eHe][0] = value;
                Batch.Externals[_eHo][0] = value;
            }
        }

        /// <summary>Glucose the cell puts into the water [mol/s] (negative: takes up; the module's J_glc_e).</summary>
        public double GlucoseRelease => A(_aGlcOut);

        /// <summary>Lactate the cell puts into the water [mol/s] (J_lac_e).</summary>
        public double LactateRelease => A(_aLacOut);

        /// <summary>H+ the cell puts into the water with lactate [mol/s] (minus J_H_lac, which counts H+ into the cell).</summary>
        public double ProtonRelease => -A(_aHLac);

        /// <summary>Flagellar motor activation, 0 or 1 (input).</summary>
        public double MotorActivation
        {
            get => Batch.Externals[_eMotor][0];
            set => Batch.Externals[_eMotor][0] = value;
        }

        /// <summary>Glucose taken up [mol/s].</summary>
        public double GlucoseUptake => A(_aUptake);

        /// <summary>Thrust of one flagellum [N].</summary>
        public double FlagellumThrust => A(_aThrust);

        /// <summary>Proton-motive power of the flagellar motors [W].</summary>
        public double FlagellumPower => A(_aFlagPower);

        /// <summary>ATP invested in a daughter so far [mol].</summary>
        public double GrowthInvested => Batch.States[_sGrowth][0];

        /// <summary>ATP to build a daughter: its proteins, RNA, lipids and DNA [mol].</summary>
        public double DaughterCost => A(_aDaughter);

        /// <summary>Fraction of a daughter built.</summary>
        public double GrowthProgress => A(_aProgress);

        /// <summary>Glycogen [mol/m³ of glucose units].</summary>
        public double Glycogen => Batch.States[_sGlycogen][0];

        /// <summary>ATP the glycogen store would give [mol].</summary>
        public double GlycogenAtp => A(_aGlycAtp);

        /// <summary>Fraction of a daughter the glycogen store could finish.</summary>
        public double GlycogenProgress => A(_aGlycProgress);

        /// <summary>True if the daughter is built, or nearly built and glycogen can finish it.</summary>
        public bool CanDivide => Alive && GrowthProgress + GlycogenProgress >= 1.0;

        /// <summary>Cell volume [m³].</summary>
        public double Volume => A(_aVol);

        /// <summary>Whole cell surface [µm²].</summary>
        public double SurfaceArea => A(_aSurface);

        /// <summary>Membrane area the proteins may take [µm²]: scales with the cell's surface.</summary>
        public double TransporterAreaMax => A(_aAreaMax);

        /// <summary>Membrane area one unit of trait <paramref name="trait"/> takes [µm²].</summary>
        public double TraitAreaPerUnit(int trait) => A(_areaPerUnit[trait]);

        /// <summary>Membrane area the adaptable proteins take now [µm²].</summary>
        public double TransporterArea()
        {
            double a = 0.0;
            for (int i = 0; i < Traits.Count; i++)
                a += A(_areaPerUnit[i]) * TraitValue(i);
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
            double unit = A(_areaPerUnit[trait]);
            if (unit > 0.0)
            {
                double others = 0.0;
                for (int i = 0; i < Traits.Count; i++)
                    if (i != trait)
                        others += A(_areaPerUnit[i]) * Math.Max(TraitTarget(i), TraitValue(i));
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

        /// <summary>Sets a state (tests, development aids) and recomputes the module's values from it.</summary>
        public void SetState(string name, double value)
        {
            Batch.States[St(name)][0] = value;
            Recompute();
        }

        public double AlgebraicValue(string name) => A(Alg(name));

        // Algebraic variable i now: the module's outputs are kept current after every step, the rest are computed when
        // first read (most steps nobody reads them).
        private double A(int i)
        {
            if (!_variablesCurrent && !_isOutput[i])
                EnsureVariables();
            return Batch.Algebraic[i][0];
        }

        /// <summary>Whether every algebraic variable is current (diagnostics: reading a non-output computes them all).</summary>
        public bool AllVariablesCurrent => _variablesCurrent;

        // Every algebraic value, read on demand: the states are those of the last step.
        private void EnsureVariables()
        {
            if (_variablesCurrent)
                return;
            _native.SetInputs(Batch);
            _native.ComputeVariables(Batch);
            _variablesCurrent = true;
        }

        // Every algebraic value after the batch's states were set from outside; the integration starts afresh from them.
        private void Recompute()
        {
            _native.SetInputs(Batch);
            _native.SetStates(Batch);
            _native.ComputeVariables(Batch);
            _variablesCurrent = true;
        }

        /// <summary>Relative local error allowed per step of the cell's integration.</summary>
        public double RelativeTolerance
        {
            get => _relativeTolerance;
            set
            {
                _relativeTolerance = value;
                _native.SetRelativeTolerance(value);
            }
        }

        private double _relativeTolerance = DefaultRelativeTolerance;

        /// <summary>The integration's steps accepted and rejected, rate evaluations and Jacobians so far (diagnostics).</summary>
        public (long Steps, long Rejected, long RateEvaluations, long Jacobians) Statistics => _native.Statistics;

        /// <summary>
        /// Advances by <paramref name="dt"/> [s]: the cell's inputs go to the library (a changed one restarts what reads
        /// it), its own adaptive steps cover dt, and the states and the module's outputs come back.
        /// </summary>
        public void Step(double dt)
        {
            if (!Alive)
                return;
            _native.SetInputs(Batch);
            _native.Advance(dt, Batch);
            Time += dt;
            _native.Outputs(Batch);   // the rest on demand (EnsureVariables)
            _variablesCurrent = false;
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
            Recompute();
            return new FermenterCell(_module, Traits, Instance, this);
        }

        /// <summary>The energy budget now [mol ATP/s, W], read from the module.</summary>
        public EnergyBudget Budget()
        {
            EnsureVariables();
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

        private int Ext(string name) => Require(_module.Descriptor.ExternalIndex(name), name);

        private int Alg(string name) => Require(_module.Descriptor.AlgebraicIndex(name), name);

        private int St(string name) => Require(_module.Descriptor.StateIndex(name), name);

        private int Require(int index, string name)
        {
            if (index < 0)
                throw new ArgumentException($"{_module.Descriptor.Key} has no variable {name}");
            return index;
        }
    }
}
