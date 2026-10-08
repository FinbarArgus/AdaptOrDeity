using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>The cell's energy budget at one instant: where its ATP comes from and goes [mol ATP/s, W].</summary>
    public struct EnergyBudget
    {
        /// <summary>ATP made by glycolysis from glucose (2 per glucose fermented to lactate).</summary>
        public double AtpGlycolysis;

        /// <summary>ATP made from glycogen (3 per glucose unit).</summary>
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

        /// <summary>Storing glucose as glycogen (2 per glucose).</summary>
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
    /// The fermenter cell (circulatory-autogen-modules cell/prokaryote fermenter, assembled into one kernel) with its
    /// adaptable traits and energy controls. Stepped by backward Euler, stable at macro steps far longer than the Ca
    /// buffer's microsecond time constant.
    /// <list type="bullet">
    /// <item>Expression approaches its target with <see cref="FermenterTraits.ExpressionTimeConstant"/>; building the new
    /// copies costs ATP (protein_expression's J_build), limited to <see cref="FermenterTraits.BuildCapacity"/> × the resting
    /// turnover and slowed as ATP runs out, so big changes take minutes. The adaptable transporters may take at most
    /// <see cref="FermenterTraits.MembraneShareMax"/> of the membrane: a ceiling that scales with membrane area.</item>
    /// <item>Growth puts ATP into a daughter (biosynthesis P_growth); the cell can divide when it reaches the daughter's
    /// cost, or spend glycogen to finish one.</item>
    /// <item>Damage accrues and is repaired at the chosen repair level; at a lethal load (1) the cell dies.</item>
    /// </list>
    /// </summary>
    public sealed class FermenterCell
    {
        public const double AvogadroConstant = 6.02214076e23;
        private const double AtpPerGlycogenUnit = 3.0;

        private readonly IKernel _kernel;
        private readonly BackwardEulerIntegrator _integrator = new BackwardEulerIntegrator();
        private readonly int[][] _traitExternals;
        private readonly double[][] _traitBase;
        private readonly double[] _value;
        private readonly double[] _target;
        private readonly double[] _planned;
        private readonly double[] _buildPerUnit;   // mol ATP to build the baseline copies of each trait's protein
        private readonly double[] _areaPerUnit;    // µm² the baseline copies of each trait's protein take
        private readonly int _jBuild, _jGrowth, _storeAlloc, _repairLevel;
        private readonly int _aVgly, _aVhyd, _aVstore, _aVmob, _aVol, _aPump, _aProtein, _aProteinFactor, _aHeat;
        private readonly int _aRna, _aRibo, _aLipid, _aRepair, _aGrowthUsed, _aBuildProtein, _aBuildBio, _aAmem;
        private readonly int _sDamage, _sGrowth, _sGlycogen, _sAtp;
        private readonly double _atpRest;

        public FermenterCell(IKernel kernel, string instance = "default", IReadOnlyList<TraitDefinition>? traits = null)
        {
            _kernel = kernel;
            var d = kernel.Descriptor;
            Traits = traits ?? FermenterTraits.All;
            Batch = new KernelBatch(d, 1) { Count = 1 };
            Batch.ApplyInstance(instance, 0, 1);

            _traitExternals = new int[Traits.Count][];
            _traitBase = new double[Traits.Count][];
            _value = new double[Traits.Count];
            _target = new double[Traits.Count];
            _planned = new double[Traits.Count];
            _buildPerUnit = new double[Traits.Count];
            _areaPerUnit = new double[Traits.Count];
            double cBuild = External("c_syn_mod_protein") + External("c_poly_mod_protein");
            for (int i = 0; i < Traits.Count; i++)
            {
                var t = Traits[i];
                _traitExternals[i] = new int[t.Parameters.Count];
                _traitBase[i] = new double[t.Parameters.Count];
                for (int p = 0; p < t.Parameters.Count; p++)
                {
                    int e = Require(d.ExternalIndex(FermenterTraits.KernelName(t.Parameters[p])), t.Parameters[p]);
                    _traitExternals[i][p] = e;
                    _traitBase[i][p] = Batch.Externals[e][0];
                }
                _value[i] = _target[i] = 1.0;
                double copies = External($"n_{t.CopyKey}_mod_protein");
                _buildPerUnit[i] = copies * External($"L_{t.CopyKey}_mod_protein") * cBuild / AvogadroConstant;
                _areaPerUnit[i] = copies * t.FootprintNm2 * 1e-6;   // nm² → µm²
            }
            _jBuild = Ext("J_build_mod_protein");
            _jGrowth = Ext("J_growth_mod_bio");
            _storeAlloc = Ext("store_alloc_mod_cytosol");
            _repairLevel = Ext("repair_level_mod_bio");
            _aVgly = Alg("cytosol__v_gly");
            _aVhyd = Alg("cytosol__v_hyd");
            _aVstore = Alg("cytosol__v_store");
            _aVmob = Alg("cytosol__v_mob");
            _aVol = Alg("membrane__Vol");
            _aAmem = Alg("membrane__A_mem");
            _aPump = Alg("i_H_ATPase__v_ATP_H");
            _aProtein = Alg("protein__J_ATP_protein");
            _aProteinFactor = Alg("protein__f_ATP_prot");
            _aHeat = Alg("cytosol__Q_met");
            _aRna = Alg("bio__J_RNA");
            _aRibo = Alg("bio__J_ribo");
            _aLipid = Alg("bio__J_lipid");
            _aRepair = Alg("bio__J_repair");
            _aGrowthUsed = Alg("bio__J_growth_used");
            _aBuildProtein = Alg("protein__M_build_protein");
            _aBuildBio = Alg("bio__M_build_bio");
            _sDamage = St("bio__D");
            _sGrowth = St("bio__P_growth");
            _sGlycogen = St("cytosol__C_glyc");
            _sAtp = St("cytosol__C_ATP");

            kernel.Initialise(Batch, 0, 1);
            kernel.ComputeVariables(0.0, Batch, 0, 1);
            RestingTurnover = Budget().AtpConsumed;
            _atpRest = Batch.States[_sAtp][0];
            SurfaceFactor = External("surface_factor_mod_bio");
        }

        // A daughter: same kernel, a copy of the parent's state, externals and traits.
        private FermenterCell(IKernel kernel, FermenterCell parent) : this(kernel, "default", parent.Traits)
        {
            for (int i = 0; i < Batch.States.Length; i++)
                Batch.States[i][0] = parent.Batch.States[i][0];
            for (int i = 0; i < Batch.Externals.Length; i++)
                Batch.Externals[i][0] = parent.Batch.Externals[i][0];
            Array.Copy(parent._value, _value, _value.Length);
            Array.Copy(parent._target, _target, _target.Length);
            GrowthFraction = parent.GrowthFraction;
            Batch.Externals[_jBuild][0] = 0.0;
            Time = parent.Time;
            _kernel.ComputeVariables(Time, Batch, 0, 1);
        }

        public KernelBatch Batch { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public double Time { get; private set; }

        /// <summary>Time constant with which expression approaches its target [s].</summary>
        public double ExpressionTimeConstant { get; set; } = FermenterTraits.ExpressionTimeConstant;

        /// <summary>ATP turnover of the default cell at rest [mol/s]: the scale for growth and the build capacity.</summary>
        public double RestingTurnover { get; }

        /// <summary>Whole cell surface / in-plane membrane area.</summary>
        public double SurfaceFactor { get; }

        /// <summary>False once damage reached a lethal load; a dead cell is no longer stepped.</summary>
        public bool Alive { get; private set; } = true;

        /// <summary>True if the last step's Newton iterations converged.</summary>
        public bool Converged => _integrator.LastConverged;

        /// <summary>Step halvings the last step needed (0 normally; more after abrupt changes).</summary>
        public int LastHalvings { get; private set; }

        public double TraitValue(int trait) => _value[trait];

        public double TraitTarget(int trait) => _target[trait];

        /// <summary>Damage, 1 = lethal.</summary>
        public double Damage => Batch.States[_sDamage][0];

        /// <summary>ATP invested in a daughter so far [mol].</summary>
        public double GrowthInvested => Batch.States[_sGrowth][0];

        /// <summary>ATP to build a daughter: its proteins, RNA, lipids and DNA [mol].</summary>
        public double DaughterCost => Batch.Algebraic[_aBuildProtein][0] + Batch.Algebraic[_aBuildBio][0];

        /// <summary>Fraction of a daughter built.</summary>
        public double GrowthProgress => GrowthInvested / DaughterCost;

        /// <summary>Glycogen [mol/m³ of glucose units].</summary>
        public double Glycogen => Batch.States[_sGlycogen][0];

        /// <summary>ATP the glycogen store would give [mol].</summary>
        public double GlycogenAtp => Glycogen * Volume * AtpPerGlycogenUnit;

        /// <summary>Fraction of a daughter the glycogen store could finish.</summary>
        public double GlycogenProgress => GlycogenAtp / DaughterCost;

        /// <summary>True if the daughter is built, or nearly built and glycogen can finish it.</summary>
        public bool CanDivide => Alive && GrowthProgress + GlycogenProgress >= 1.0;

        /// <summary>Cell volume [m³].</summary>
        public double Volume => Batch.Algebraic[_aVol][0];

        /// <summary>Whole cell surface [µm²].</summary>
        public double SurfaceArea => Batch.Algebraic[_aAmem][0] * SurfaceFactor;

        /// <summary>Membrane area the adaptable transporters may take [µm²]: scales with the cell's surface.</summary>
        public double TransporterAreaMax => FermenterTraits.MembraneShareMax * SurfaceArea;

        /// <summary>Membrane area the adaptable transporters take now [µm²].</summary>
        public double TransporterArea()
        {
            double a = 0.0;
            for (int i = 0; i < Traits.Count; i++)
                a += _areaPerUnit[i] * _value[i];
            return a;
        }

        /// <summary>
        /// Sets the trait's target multiplier, clamped to its range and to the membrane ceiling (the other traits held).
        /// Expression approaches the target over <see cref="ExpressionTimeConstant"/>.
        /// </summary>
        public void SetTrait(int trait, double multiplier)
        {
            var t = Traits[trait];
            if (double.IsNaN(multiplier))
                throw new ArgumentException("multiplier is NaN", nameof(multiplier));
            double m = Math.Min(t.Max, Math.Max(t.Min, multiplier));
            if (_areaPerUnit[trait] > 0.0)
            {
                double others = 0.0;
                for (int i = 0; i < Traits.Count; i++)
                    if (i != trait)
                        others += _areaPerUnit[i] * Math.Max(_target[i], _value[i]);
                double room = (TransporterAreaMax - others) / _areaPerUnit[trait];
                m = Math.Max(t.Min, Math.Min(m, room));
            }
            _target[trait] = m;
        }

        /// <summary>
        /// ATP the player asks to put into growth, as a multiple of the resting ATP turnover (0 = none). What reaches the
        /// daughter is throttled like protein building when ATP falls below rest.
        /// </summary>
        public double GrowthFraction { get; set; }

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

        public double AlgebraicValue(string name) => Batch.Algebraic[Alg(name)][0];

        public void Step(double dt)
        {
            if (!Alive)
                return;
            // Expression moves towards its target; building the extra copies is limited by the build capacity and slows
            // as ATP runs out (f_ATP), so the copies gained scale with the ATP actually spent.
            double build = 0.0;
            double relax = 1.0 - Math.Exp(-dt / ExpressionTimeConstant);
            for (int i = 0; i < Traits.Count; i++)
            {
                _planned[i] = 0.0;
                if (_value[i] == _target[i])
                    continue;
                double next = _value[i] + (_target[i] - _value[i]) * relax;
                if (Math.Abs(next - _target[i]) < 1e-9 * _target[i])
                    next = _target[i];
                _planned[i] = next - _value[i];
                if (_planned[i] > 0.0)
                    build += _planned[i] * _buildPerUnit[i];
            }
            // Build no faster than the build capacity, and back off as ATP falls below rest (the stringent response):
            // full speed at the resting ATP, none at BuildAtpFloor, so building never starves the cell.
            double atp = Batch.States[_sAtp][0];
            double throttle = Math.Min(1.0, Math.Max(0.0, (atp - FermenterTraits.BuildAtpFloor) / (_atpRest - FermenterTraits.BuildAtpFloor)));
            double cap = FermenterTraits.BuildCapacity * RestingTurnover * dt * throttle;
            Batch.Externals[_jGrowth][0] = Math.Max(0.0, GrowthFraction) * RestingTurnover * throttle;
            double scale = build > cap ? (build > 0.0 ? cap / build : 0.0) : 1.0;
            Batch.Externals[_jBuild][0] = build * scale / dt;
            LastHalvings = _integrator.StepRobust(_kernel, Batch, Time, dt, 0, 1);
            Time += dt;
            double f = Math.Min(1.0, Batch.Algebraic[_aProteinFactor][0]);
            for (int i = 0; i < Traits.Count; i++)
            {
                if (_planned[i] == 0.0)
                    continue;
                // Rising: as far as capacity and ATP allowed. Falling: degradation needs no ATP and is not refunded.
                _value[i] += _planned[i] > 0.0 ? _planned[i] * scale * f : _planned[i];
                if (Math.Abs(_value[i] - _target[i]) < 1e-9 * _target[i])
                    _value[i] = _target[i];
                Apply(i);
            }
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
                Batch.States[_sGlycogen][0] = Math.Max(0.0, Glycogen - shortfall / (AtpPerGlycogenUnit * Volume));
            Batch.States[_sGrowth][0] = 0.0;
            _kernel.ComputeVariables(Time, Batch, 0, 1);
            return new FermenterCell(_kernel, this);
        }

        /// <summary>The energy budget now [mol ATP/s, W].</summary>
        public EnergyBudget Budget()
        {
            double[][] a = Batch.Algebraic;
            double vol = a[_aVol][0];   // m³; rates are mM/s = mol/m³/s
            double f = Math.Min(1.0, a[_aProteinFactor][0]);
            return new EnergyBudget
            {
                AtpGlycolysis = 2.0 * a[_aVgly][0] * vol,
                AtpFromGlycogen = AtpPerGlycogenUnit * a[_aVmob][0] * vol,
                AtpPumps = a[_aPump][0],
                AtpProteinTurnover = a[_aProtein][0] * a[_aProteinFactor][0],
                AtpProteinBuild = Batch.Externals[_jBuild][0] * f,
                AtpRna = a[_aRna][0] + a[_aRibo][0],
                AtpLipids = a[_aLipid][0],
                AtpRepair = a[_aRepair][0],
                AtpGrowth = a[_aGrowthUsed][0],
                AtpStorage = 2.0 * a[_aVstore][0] * vol,
                AtpOtherMaintenance = a[_aVhyd][0] * vol,
                Heat = a[_aHeat][0],
            };
        }

        private void Apply(int trait)
        {
            int[] ext = _traitExternals[trait];
            for (int p = 0; p < ext.Length; p++)
                Batch.Externals[ext[p]][0] = _traitBase[trait][p] * _value[trait];
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
