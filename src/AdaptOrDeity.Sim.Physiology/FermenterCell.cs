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

        /// <summary>
        /// F1Fo H+-ATPase pumping protons out, less <see cref="AtpSwimming"/>: what holds the gradient against the leaks
        /// and the exchangers.
        /// </summary>
        public double AtpPumps;

        /// <summary>
        /// Swimming: the flagellar motors run on protons flowing in, and the H+-ATPase spends this much ATP pumping them
        /// back out (the motors' proton flux over the pump's H+ per ATP; at most all the pumping).
        /// </summary>
        public double AtpSwimming;

        /// <summary>Turning over (degrading and remaking) the cell's proteins.</summary>
        public double AtpProteinTurnover;

        /// <summary>Building new protein copies while expression rises, at the full cost of making their amino acids
        /// from glucose (what recycled amino acids save is under <see cref="AtpRecycledProtein"/>).</summary>
        public double AtpProteinBuild;

        /// <summary>Regulated proteolysis: removing the copies above a lowered target.</summary>
        public double AtpProteinBreakdown;

        /// <summary>ATP returned by recycled protein: the amino acids of removed copies saved in building new ones, plus
        /// the little ATP made by catabolising those not reused.</summary>
        public double AtpRecycledProtein;

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

        /// <summary>Making extracellular matrix (exopolysaccharide).</summary>
        public double AtpMatrix;

        /// <summary>Sugar carriers that spend ATP or PEP to bring glucose in (PTS 1, ABC 2 per glucose).</summary>
        public double AtpCarriers;

        /// <summary>The remaining maintenance ATPase: everything not yet itemised.</summary>
        public double AtpOtherMaintenance;

        /// <summary>Heat released [W].</summary>
        public double Heat;

        public double AtpProduced => AtpGlycolysis + AtpFromGlycogen + AtpRecycledProtein;

        public double AtpConsumed => AtpPumps + AtpSwimming + AtpProteinTurnover + AtpProteinBuild + AtpProteinBreakdown + AtpRna + AtpLipids + AtpRepair
                                     + AtpGrowth + AtpStorage + AtpMatrix + AtpCarriers + AtpOtherMaintenance;
    }

    /// <summary>
    /// The fermenter cell (circulatory-autogen-modules cell/generic_adaptive_cell fermenter, assembled into one module),
    /// integrated by libcuflynx's generated C (<see cref="NativeInstance"/>: adaptive TR-BDF2, stable at world steps far
    /// longer than the Ca buffer's microsecond time constant). Its physiology is all in the module: each protein's
    /// expression approaches the target the player sets, building the new copies costs ATP and is capped and throttled
    /// by the cell's protein synthesis, growth is asked in multiples of the resting turnover and throttled the same
    /// way, and the daughter's cost and the share built are module outputs. Membrane proteins take shares of the membrane
    /// ceiling (protein__A_protein_max). This class sets targets and controls, keeps the membrane proteins' shares within the
    /// ceiling, sets the same medium glucose on every glucose carrier, runs the events (division, death) and reads the budget.
    /// </summary>
    public sealed class FermenterCell
    {
        private readonly NativeModule _module;
        private readonly NativeInstance _native;
        private readonly bool[] _isOutput;
        private bool _variablesCurrent;
        private readonly int[] _target, _expression, _areaPerUnit;
        private readonly bool[] _shareTarget;      // the trait's target is s_target (a share of the membrane ceiling)
        private readonly double[] _nominalShare;   // the starting cell's share of each such trait (its s_target)
        private readonly int[] _poolState, _poolArea, _poolTrait;   // every membrane protein pool: m, a_unit, trait or -1
        private readonly int[] _eCarrierGlucose, _aCarrierUptake;   // Glf, PTS, ABC, GalP: medium glucose input, J_glc
        private readonly int _aHydrolysisMax, _eHydrolysisKm, _aPtsCost, _aAbcCost;
        private readonly int _growthLevel, _storeAlloc, _repairLevel, _matrixAlloc, _jRef;
        private readonly int _aGly, _aMob, _aStore, _aMaint, _aPump, _aKeep, _aBuild, _aSaved, _aBreakdown, _aRecycled, _aRna, _aRibo, _aLipid, _aRepair;
        private readonly int _aGrowthUsed, _aHeat, _aVol, _aSurface, _aAreaMax, _aDaughter, _aProgress, _aGlycProgress;
        private readonly int _aGlycAtp, _aThrust, _aFlagPower, _eMotor, _sAminoAcids, _aFlagProtons;
        private readonly double _pumpProtonsPerAtp;
        private readonly int _aLacOut, _aHLac, _eLactate, _eHe, _eHo;
        private readonly int _aMatrix, _aAtpMatrix;
        private readonly int _sDamagePhysical, _sDamageRadiation, _sDamageRibosome, _sDamageEnergy, _sGrowth, _sGlycogen, _sAtp;
        private readonly int _ePhysicalDamage, _eDoseRate;
        private readonly int[] _regulationSwitch;   // per regulated target (traits, growth, maintenance), per species
        private readonly int[] _regulationFactor;   // per regulated target: its R_reg

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
            _shareTarget = new bool[traits.Count];
            _nominalShare = new double[traits.Count];
            var player = new List<string>();
            var starting = d.Instances["default"];
            for (int i = 0; i < traits.Count; i++)
            {
                var t = traits[i];
                int share = t.Kind == TraitKind.Count ? -1 : d.ExternalIndex(t.ShareTargetParameter);
                if (share >= 0)
                {
                    _target[i] = share;
                    _shareTarget[i] = true;
                    _nominalShare[i] = starting[t.ShareTargetParameter];
                    if (t.Kind == TraitKind.Expression && !(_nominalShare[i] > 0.0))
                        throw new ArgumentException($"{t.Id}: the starting cell has no {t.Pool}, so it is a share trait, not a multiplier");
                    if (t.Kind == TraitKind.Share && _nominalShare[i] != 0.0)
                        throw new ArgumentException($"{t.Id}: the starting cell has {t.Pool}, so it is a multiplier, not a share trait");
                    player.Add(t.ShareTargetParameter);
                }
                else
                {
                    if (t.Kind == TraitKind.Share)
                        throw new ArgumentException($"{d.Key} has no {t.ShareTargetParameter} for share trait {t.Id}");
                    _target[i] = Ext(t.ExpressionTargetParameter);
                    player.Add(t.ExpressionTargetParameter);
                }
                _expression[i] = St(t.ExpressionState);
                _areaPerUnit[i] = Alg(t.AreaPerUnit);
            }
            // every protein pool with a footprint on the membrane, whether or not the player sets it (e.g. the hydrolase)
            var pools = new List<(int State, int Area, int Trait)>();
            foreach (var v in d.Algebraic)
            {
                if (!v.Name.StartsWith("pool_", StringComparison.Ordinal) || !v.Name.EndsWith("__a_unit", StringComparison.Ordinal))
                    continue;
                string pool = v.Name.Substring(0, v.Name.Length - "__a_unit".Length);
                int trait = -1;
                for (int i = 0; i < traits.Count; i++)
                    if (traits[i].Pool == pool)
                        trait = i;
                pools.Add((St(pool + "__m"), Alg(v.Name), trait));
            }
            _poolState = new int[pools.Count];
            _poolArea = new int[pools.Count];
            _poolTrait = new int[pools.Count];
            for (int k = 0; k < pools.Count; k++)
                (_poolState[k], _poolArea[k], _poolTrait[k]) = pools[k];
            _eCarrierGlucose = new int[Carriers.Length];
            _aCarrierUptake = new int[Carriers.Length];
            for (int k = 0; k < Carriers.Length; k++)
            {
                _eCarrierGlucose[k] = Ext("C_glc_e_mod_" + Carriers[k]);
                _aCarrierUptake[k] = Alg(Carriers[k] + "__J_glc");
            }
            _aHydrolysisMax = Alg("hydrolase__V_hydrolysis_max");
            _eHydrolysisKm = Ext("K_m_poly_mod_hydrolase");
            _aPtsCost = Alg("glc_PTS__J_ATP_cost");
            _aAbcCost = Alg("glc_ABC__J_ATP_cost");
            _growthLevel = Ext(GrowthParameter);
            _storeAlloc = Ext(StorageParameter);
            _repairLevel = Ext(RepairParameter);
            _matrixAlloc = Ext(MatrixParameter);
            player.Add(GrowthParameter);
            player.Add(StorageParameter);
            player.Add(RepairParameter);
            player.Add(MatrixParameter);
            // regulation switches (cell/expression_regulation): per trait's pool, growth and each maintenance target, per
            // species
            int targets = traits.Count + 1 + MaintenanceRegulation.Length;
            _regulationSwitch = new int[targets * RegulationSpecies.Length];
            _regulationFactor = new int[targets];
            for (int target = 0; target < targets; target++)
            {
                string sub = target < traits.Count ? traits[target].Pool : "bio", suffix = "";
                if (target > traits.Count)
                    (_, sub, suffix) = MaintenanceRegulation[target - traits.Count - 1];
                _regulationFactor[target] = Alg($"{sub}__R_reg{suffix}");
                for (int s = 0; s < RegulationSpecies.Length; s++)
                {
                    string name = $"reg_on_{RegulationSpecies[s]}{suffix}_mod_{sub}";
                    _regulationSwitch[target * RegulationSpecies.Length + s] = Ext(name);
                    player.Add(name);
                }
            }
            PlayerParameters = player;
            _jRef = Ext("J_ref_mod_protein");
            _aGly = Alg("cytosol__J_ATP_gly");
            _aMob = Alg("cytosol__J_ATP_mob");
            _aStore = Alg("cytosol__J_ATP_store");
            _aMaint = Alg("cytosol__J_ATP_maint");
            _aPump = Alg("i_H_ATPase__v_ATP_H");
            _aFlagProtons = Alg("i_flagellum__j_H_flag");
            _pumpProtonsPerAtp = starting["n_H_mod_i_H_ATPase"];
            _aKeep = Alg("protein__J_keep_total");
            _aBuild = Alg("protein__J_build_total");
            _aSaved = Alg("protein__J_saved_total");
            _aBreakdown = Alg("protein__J_prot_total");
            _aRecycled = Alg("protein__J_recycled");
            _sAminoAcids = St("protein__N_aa");
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
            _aThrust = Alg("i_flagellum__F_one");
            _aFlagPower = Alg("i_flagellum__P_flag");
            _eMotor = Ext("a_motor_mod_i_flagellum");
            _aLacOut = Alg("membrane_face__J_lac_e");
            _aHLac = Alg("membrane_face__J_H_lac");
            _eLactate = Ext("C_lac_e_mod_membrane_face");
            _eHe = Ext("H_e_mod_membrane_face");
            _eHo = Ext("H_o_mod_H");
            _aMatrix = Alg("cytosol__J_mx");
            _aAtpMatrix = Alg("cytosol__J_ATP_mx");
            _sDamagePhysical = St("bio__D_phys");
            _sDamageRadiation = St("bio__D_rad");
            _sDamageRibosome = St("bio__D_ribo");
            _sDamageEnergy = St("bio__D_energy");
            _ePhysicalDamage = Ext("r_dmg_phys_mod_bio");
            _eDoseRate = Ext("dose_rate_mod_bio");
            _sGrowth = St("bio__P_growth");
            _sGlycogen = St("cytosol__C_glyc");
            _sAtp = St("cytosol__C_ATP");
            RestingAtp = starting["C_ATP_init_mod_cytosol"];
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
                _relativeTolerance = parent._relativeTolerance;
                _native.SetRelativeTolerance(_relativeTolerance);
                _stopAtMacroSteps = parent._stopAtMacroSteps;
                _native.SetStopAtOutputs(_stopAtMacroSteps);
                for (int i = 0; i < Batch.States.Length; i++)
                    Batch.States[i][0] = parent.Batch.States[i][0];
                for (int i = 0; i < Batch.Externals.Length; i++)
                    Batch.Externals[i][0] = parent.Batch.Externals[i][0];
                Time = parent.Time;
                Recompute();
            }
            _variablesCurrent = true;
        }

        /// <summary>
        /// The glucose carriers (submodules) the cell has: the facilitator Glf (membrane_face) and the PTS, ABC and H+ symport
        /// (GalP) carriers. Each reads the medium's glucose on its own input, so all are set together.
        /// </summary>
        public static readonly string[] Carriers = { "membrane_face", "glc_PTS", "glc_ABC", "glc_GalP" };

        private const string GrowthParameter = "growth_level_mod_bio", StorageParameter = "store_alloc_mod_cytosol",
            RepairParameter = "repair_level_mod_bio", MatrixParameter = "mx_alloc_mod_cytosol";

        public KernelBatch Batch { get; }

        /// <summary>
        /// Species whose fall can lower an expression, growth or maintenance target (kernel switches reg_on_&lt;species&gt;):
        /// ATP, glucose 6-phosphate, glucose inside and H+ (a falling pH). The function is upstream's
        /// (cell/expression_regulation): f = 1/(1 + u^n), u = max(0, 1 - x)/(1 - x_half), x = C/C_rest (for H+: H_rest/H),
        /// x_half = 1/2, n = 4: exactly 1 at and above rest, 1/2 at half the resting level (for H+: twice the resting H+).
        /// </summary>
        public static readonly string[] RegulationSpecies = { "ATP", "G6P", "glc", "H" };

        /// <summary>
        /// Maintenance the cell can turn down as species fall (upstream's switches, 0 by default), in the order of the
        /// regulated targets after growth: the id, the submodule and the suffix of its switches (reg_on_&lt;species&gt;&lt;suffix&gt;)
        /// and factor (R_reg&lt;suffix&gt;). Protein synthesis (replacing degraded copies and building new ones; cut, the
        /// cell's proteins are slowly lost), RNA and lipid upkeep, repair (cut, damage builds up) and the maintenance ATPase
        /// (everything else the cell spends at rest).
        /// </summary>
        public static readonly (string Id, string Submodule, string Suffix)[] MaintenanceRegulation =
        {
            ("protein_synthesis", "protein", ""), ("upkeep", "bio", "_upkeep"), ("repair", "bio", "_repair"),
            ("maintenance_atpase", "cytosol", ""),
        };

        /// <summary>
        /// Regulated targets: each trait's (its index), then growth (<see cref="GrowthRegulation"/>), then the maintenance
        /// (<see cref="MaintenanceTarget"/>).
        /// </summary>
        public int RegulationTargets => Traits.Count + 1 + MaintenanceRegulation.Length;

        /// <summary>The regulated target that is growth (the energy put into a daughter).</summary>
        public int GrowthRegulation => Traits.Count;

        /// <summary>The regulated target that is maintenance entry <paramref name="k"/> of <see cref="MaintenanceRegulation"/>.</summary>
        public int MaintenanceTarget(int k) => Traits.Count + 1 + k;

        /// <summary>Whether a fall of species <paramref name="species"/> lowers target <paramref name="target"/>.</summary>
        public bool Regulated(int target, int species) => Batch.Externals[RegulationIndex(target, species)][0] != 0.0;

        /// <summary>Switches regulation of target <paramref name="target"/> by species <paramref name="species"/> on or off.</summary>
        public void SetRegulated(int target, int species, bool on) =>
            Batch.Externals[RegulationIndex(target, species)][0] = on ? 1.0 : 0.0;

        /// <summary>The factor regulation puts on target <paramref name="target"/> now (R_reg: 1 with every switch off).</summary>
        public double RegulationFactor(int target) => A(_regulationFactor[CheckTarget(target)]);

        private int RegulationIndex(int target, int species)
        {
            if (species < 0 || species >= RegulationSpecies.Length)
                throw new ArgumentOutOfRangeException(nameof(species), $"no regulating species {species}");
            return _regulationSwitch[CheckTarget(target) * RegulationSpecies.Length + species];
        }

        private int CheckTarget(int target)
        {
            if (target < 0 || target >= RegulationTargets)
                throw new ArgumentOutOfRangeException(nameof(target), $"no regulated target {target}");
            return target;
        }

        /// <summary>Cytosol ATP now [mol/m³].</summary>
        public double Atp => Batch.States[_sAtp][0];

        /// <summary>Cytosol ATP of the starting cell at rest [mol/m³].</summary>
        public double RestingAtp { get; }

        /// <summary>The module's parameter set the cell started from.</summary>
        public string Instance { get; }

        /// <summary>Module parameters the player sets: each trait's target (s_target for a membrane protein, m_target
        /// otherwise), the growth, storage, repair and matrix controls, then the regulation switches.</summary>
        public IReadOnlyList<string> PlayerParameters { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public double Time { get; private set; }

        /// <summary>Resting ATP turnover of the cell [mol/s] (the module's J_ref): the scale of growth.</summary>
        public double RestingTurnover => Batch.Externals[_jRef][0];

        /// <summary>False once damage reached a lethal load; a dead cell is no longer stepped.</summary>
        public bool Alive { get; private set; } = true;

        /// <summary>
        /// The trait now: the pool's expression relative to the starting cell, or a count, or (a share trait) the share of
        /// the membrane ceiling its copies take.
        /// </summary>
        public double TraitValue(int trait)
        {
            double m = Batch.States[_expression[trait]][0];
            return Traits[trait].Kind == TraitKind.Share ? m * A(_areaPerUnit[trait]) / TransporterAreaMax : m;
        }

        /// <summary>The trait's target, in the units of <see cref="TraitValue"/>.</summary>
        public double TraitTarget(int trait)
        {
            double target = Batch.Externals[_target[trait]][0];
            return _shareTarget[trait] && Traits[trait].Kind == TraitKind.Expression ? target / _nominalShare[trait] : target;
        }

        /// <summary>The trait's starting share of the membrane ceiling (0 for a share trait or a cytosolic protein).</summary>
        public double NominalShare(int trait) => _nominalShare[trait];

        /// <summary>Kernel parameter holding the trait's target.</summary>
        public string TargetParameter(int trait) => PlayerParameters[trait];

        /// <summary>Total damage, the sum of its four types; 1 = lethal.</summary>
        public double Damage => DamagePhysical + DamageRadiation + DamageRibosome + DamageEnergy;

        /// <summary>Physical damage to the envelope (membrane and wall), from attacks; repaired by envelope repair.</summary>
        public double DamagePhysical => Batch.States[_sDamagePhysical][0];

        /// <summary>Radiation damage to DNA; repaired by DNA repair.</summary>
        public double DamageRadiation => Batch.States[_sDamageRadiation][0];

        /// <summary>
        /// Ribosome and protein damage: wear, Ca²⁺ overload displacing the ribosomes' Mg²⁺, and K⁺ falling below what
        /// ribosomes need to translate.
        /// </summary>
        public double DamageRibosome => Batch.States[_sDamageRibosome][0];

        /// <summary>De-energisation damage: proteins aggregating once ATP falls too low for chaperones to refold them.</summary>
        public double DamageEnergy => Batch.States[_sDamageEnergy][0];

        /// <summary>Physical damage the cell is taking [lethal loads/s], e.g. while attacked (0 by default).</summary>
        public double PhysicalDamageRate
        {
            get => Batch.Externals[_ePhysicalDamage][0];
            set => Batch.Externals[_ePhysicalDamage][0] = Math.Max(0.0, value);
        }

        /// <summary>Absorbed radiation dose rate [Gy/s] (0 by default).</summary>
        public double RadiationDoseRate
        {
            get => Batch.Externals[_eDoseRate][0];
            set => Batch.Externals[_eDoseRate][0] = Math.Max(0.0, value);
        }

        /// <summary>A blow: adds <paramref name="loads"/> of physical damage at once (a share of a lethal load).</summary>
        public void AddPhysicalDamage(double loads) => SetState("bio__D_phys", DamagePhysical + Math.Max(0.0, loads));

        /// <summary>Glucose at the membrane [mol/m³] (input of every glucose carrier).</summary>
        public double MembraneGlucose
        {
            get => Batch.Externals[_eCarrierGlucose[0]][0];
            set
            {
                for (int k = 0; k < _eCarrierGlucose.Length; k++)
                    Batch.Externals[_eCarrierGlucose[k]][0] = value;
            }
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

        /// <summary>Glucose the cell puts into the water [mol/s] (negative: takes up; the carriers' J_glc_e summed).</summary>
        public double GlucoseRelease => -GlucoseUptake;

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

        /// <summary>
        /// Glucose taken up by all the carriers [mol/s]: Glf's, the PTS's (delivered as glucose 6-phosphate), the ABC
        /// importer's and the H+ symporter's J_glc.
        /// </summary>
        public double GlucoseUptake
        {
            get
            {
                double j = 0.0;
                for (int k = 0; k < _aCarrierUptake.Length; k++)
                    j += A(_aCarrierUptake[k]);
                return j;
            }
        }

        /// <summary>Glucose through carrier <paramref name="carrier"/> of <see cref="Carriers"/> [mol/s] (negative: out).</summary>
        public double CarrierUptake(int carrier) => A(_aCarrierUptake[carrier]);

        /// <summary>Glucose through the facilitator Glf [mol/s]; negative while it leaks glucose out.</summary>
        public double GlfUptake => A(_aCarrierUptake[0]);

        /// <summary>The surface hydrolase's most rapid release of glucose from polymer [mol glucose/s] (saturating polymer).</summary>
        public double HydrolysisMax => A(_aHydrolysisMax);

        /// <summary>Polymer concentration at which the hydrolase runs at half its most rapid rate [mol glucose units/m³].</summary>
        public double HydrolysisHalfSaturation => Batch.Externals[_eHydrolysisKm][0];

        /// <summary>
        /// Glucose the surface hydrolase releases from polymer at concentration <paramref name="polymer"/> [mol glucose
        /// units/m³]: V_hydrolysis_max S / (K_m_poly + S) [mol glucose/s].
        /// </summary>
        public double Hydrolysis(double polymer) =>
            polymer > 0.0 ? HydrolysisMax * polymer / (HydrolysisHalfSaturation + polymer) : 0.0;

        /// <summary>Extracellular matrix exported [mol of glucose units/s].</summary>
        public double MatrixExport => A(_aMatrix);

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

        /// <summary>Recycled amino acids: those freed by removing protein copies above a lowered target and not yet reused
        /// or catabolised [amino acids per cell].</summary>
        public double RecycledAminoAcids => Batch.States[_sAminoAcids][0];

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

        /// <summary>Membrane area the proteins may take [µm²] (the ceiling): scales with the cell's surface.</summary>
        public double TransporterAreaMax => A(_aAreaMax);

        /// <summary>Membrane area one unit of trait <paramref name="trait"/>'s expression takes [µm²].</summary>
        public double TraitAreaPerUnit(int trait) => A(_areaPerUnit[trait]);

        /// <summary>Share of the membrane ceiling trait <paramref name="trait"/>'s copies take now.</summary>
        public double TraitShare(int trait) => Batch.States[_expression[trait]][0] * A(_areaPerUnit[trait]) / TransporterAreaMax;

        /// <summary>Share of the membrane ceiling trait <paramref name="trait"/>'s target would take.</summary>
        public double TraitTargetShare(int trait)
        {
            double target = Batch.Externals[_target[trait]][0];
            return _shareTarget[trait] ? target : target * A(_areaPerUnit[trait]) / TransporterAreaMax;
        }

        /// <summary>Share of the membrane ceiling every membrane protein takes now, the hydrolase and flagella included.</summary>
        public double MembraneShareUsed()
        {
            double s = 0.0;
            for (int k = 0; k < _poolState.Length; k++)
                s += Batch.States[_poolState[k]][0] * A(_poolArea[k]);
            return s / TransporterAreaMax;
        }

        /// <summary>
        /// Share of the membrane ceiling the proteins other than trait <paramref name="except"/> claim: each trait at the
        /// larger of its target and its present share, every other membrane pool (the hydrolase) at its present share.
        /// </summary>
        public double MembraneShareClaimed(int except = -1)
        {
            double max = TransporterAreaMax, s = 0.0;
            for (int k = 0; k < _poolState.Length; k++)
            {
                int t = _poolTrait[k];
                if (t == except && t >= 0)
                    continue;
                double now = Batch.States[_poolState[k]][0] * A(_poolArea[k]) / max;
                s += t >= 0 ? Math.Max(now, TraitTargetShare(t)) : now;
            }
            return s;
        }

        /// <summary>
        /// Sets the trait's target, clamped to its range and to the membrane room the other proteins leave (each counted
        /// at the larger of its target and its present share; the shares sum to at most the ceiling). An expression trait
        /// is a multiple of the starting cell's expression (a membrane protein's s_target is that multiple of its starting
        /// share), a count is whole units, a share trait is its share of the ceiling. The pool approaches it in the kernel.
        /// </summary>
        public void SetTrait(int trait, double value)
        {
            var t = Traits[trait];
            if (double.IsNaN(value))
                throw new ArgumentException("value is NaN", nameof(value));
            double v = Math.Min(t.Max, Math.Max(t.Min, value));
            // the share of the ceiling one unit of the value takes
            double unit = t.Kind == TraitKind.Share ? 1.0
                : _shareTarget[trait] ? _nominalShare[trait]
                : A(_areaPerUnit[trait]) / TransporterAreaMax;
            if (unit > 0.0)
                v = Math.Max(t.Min, Math.Min(v, (1.0 - MembraneShareClaimed(trait)) / unit));
            if (t.Step > 0.0)
                v = Math.Max(t.Min, Math.Floor(v / t.Step + 1e-9) * t.Step);   // whole units (e.g. flagella) that fit
            Batch.Externals[_target[trait]][0] = _shareTarget[trait] && t.Kind == TraitKind.Expression ? v * unit : v;
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

        /// <summary>Share of the matrix-making capacity in use, 0-1.</summary>
        public double MatrixAllocation
        {
            get => Batch.Externals[_matrixAlloc][0];
            set => Batch.Externals[_matrixAlloc][0] = Math.Min(1.0, Math.Max(0.0, value));
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

        /// <summary>
        /// Steps end on each macro step and input changes there restart softly (<see cref="NativeInstance.SetStopAtOutputs"/>):
        /// for cells whose inputs change every macro step (the environment field). Daughters inherit it.
        /// </summary>
        public bool StopAtMacroSteps
        {
            get => _stopAtMacroSteps;
            set
            {
                _stopAtMacroSteps = value;
                _native.SetStopAtOutputs(value);
            }
        }

        private bool _stopAtMacroSteps;

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
        /// controls, share the recycled amino acids, and the growth investment restarts. Returns the daughter, or null if the cell cannot divide yet.
        /// </summary>
        public FermenterCell? Divide()
        {
            if (!CanDivide)
                return null;
            double shortfall = Math.Max(0.0, DaughterCost - GrowthInvested);
            if (shortfall > 0.0)
                Batch.States[_sGlycogen][0] = Math.Max(0.0, Glycogen * (1.0 - shortfall / GlycogenAtp));
            Batch.States[_sGrowth][0] = 0.0;
            Batch.States[_sAminoAcids][0] *= 0.5;   // a count per cell, not a concentration: shared between the halves
            Recompute();
            return new FermenterCell(_module, Traits, Instance, this);
        }

        /// <summary>The energy budget now [mol ATP/s, W], read from the module.</summary>
        public EnergyBudget Budget()
        {
            EnsureVariables();
            double[][] a = Batch.Algebraic;
            double pumping = a[_aPump][0];
            double swimming = Math.Min(Math.Max(0.0, pumping), Math.Abs(a[_aFlagProtons][0]) / _pumpProtonsPerAtp);
            return new EnergyBudget
            {
                AtpGlycolysis = a[_aGly][0],
                AtpFromGlycogen = a[_aMob][0],
                AtpPumps = pumping - swimming,
                AtpSwimming = swimming,
                AtpProteinTurnover = a[_aKeep][0],
                AtpProteinBuild = a[_aBuild][0] + a[_aSaved][0],
                AtpProteinBreakdown = a[_aBreakdown][0],
                AtpRecycledProtein = a[_aRecycled][0],
                AtpRna = a[_aRna][0] + a[_aRibo][0],
                AtpLipids = a[_aLipid][0],
                AtpRepair = a[_aRepair][0],
                AtpGrowth = a[_aGrowthUsed][0],
                AtpStorage = a[_aStore][0],
                AtpMatrix = a[_aAtpMatrix][0],
                AtpCarriers = a[_aPtsCost][0] + a[_aAbcCost][0],
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
