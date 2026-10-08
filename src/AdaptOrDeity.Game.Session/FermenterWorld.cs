using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The stage-1 world: fermenter cells (circulatory-autogen-modules cell/prokaryote fermenter) in a Proterozoic sea.
    /// Cells belong to groups; traits and energy controls are set per group and apply to every cell in it. From level 2 a
    /// cell can put energy into a daughter and divide; at a lethal damage load it dies. With a fixed body size a group
    /// keeps its number of cells: growth energy replaces dead cells, and goes to repair while the group is full. Cells
    /// share the medium but do not yet exchange anything (the transport graph comes later). Values exchanged here are SI.
    /// </summary>
    public sealed class FermenterWorld : ISimWorld, IInspectableWorld, IBathWorld, IFoodWorld
    {
        /// <summary>Sim-seconds per real second: pause, 1x, 10x, 100x, 1000x.</summary>
        public static readonly double[] SpeedPresets = { 0.0, 1.0, 10.0, 100.0, 1000.0 };

        public const int CellLimit = 64;
        public const int GroupLimit = 8;
        public const int GrowthControl = 0, StorageControl = 1, RepairControl = 2;

        private const double Avogadro = FermenterCell.AvogadroConstant;
        private const double CellDensity = 1100.0;   // kg/m³, for the specific metabolic rate

        private static readonly OverlayInfo[] OverlayList =
        {
            new OverlayInfo("v", "Membrane potential", "F1", "V", "mV", 1e3, -140.0, -60.0),
            new OverlayInfo("ph", "Cytosol pH", "F2", "pH", "pH", 1.0, 7.0, 8.2),
            new OverlayInfo("atp", "ATP", "F3", "mol/m³", "mM", 1.0, 0.0, 12.0),
            new OverlayInfo("na", "Na⁺ inside", "F4", "mol/m³", "mM", 1.0, 0.0, 120.0),
            new OverlayInfo("damage", "Damage", "F5", "", "% lethal", 100.0, 0.0, 100.0),
            new OverlayInfo("growth", "Daughter built", "F6", "", "%", 100.0, 0.0, 100.0),
        };

        private static readonly ControlInfo[] ControlList =
        {
            new ControlInfo("growth", "Energy to growth", 0.0, 2.0, 0.0, GameFeatures.GrowthAndDivision, "× resting ATP use",
                "ATP put into building a daughter, as a multiple of the cell's resting ATP use"),
            new ControlInfo("storage", "Glycogen storage", 0.0, 1.0, 0.0, GameFeatures.GlycogenStorage, "of capacity",
                "how hard the cell stores glucose as glycogen (2 ATP per glucose, 3 back later)"),
            new ControlInfo("repair", "Repair", 0.0, 1.0, 1.0, GameFeatures.RepairLevel, "",
                "how much the cell spends repairing damage; below about 0.1 damage outruns repair and the cell dies"),
        };

        private readonly IKernel _kernel = new Prokaryote_fermenter();
        private readonly List<FermenterCell> _cells = new List<FermenterCell>();
        private readonly List<int> _cellGroup = new List<int>();
        private readonly List<Group> _groups = new List<Group>();
        private readonly bool[] _featureEnabled;
        private readonly DetailInfo[] _details;
        private readonly Func<FermenterCell, double>[] _read;
        private readonly TraitInfo[] _traits;
        private int _level = 1;
        private readonly FoodField _food = new FoodField();
        private double[] _traceAtCell = new double[0];   // per cell, per trace species [mol/m³], for sensing later

        private sealed class Group
        {
            public Group(int traits)
            {
                Targets = new double[traits];
                for (int i = 0; i < traits; i++)
                    Targets[i] = 1.0;
                for (int c = 0; c < ControlList.Length; c++)
                    Controls[c] = ControlList[c].DefaultValue;
            }

            public double[] Targets { get; }

            public double[] Controls { get; } = new double[ControlList.Length];

            /// <summary>Cells the group holds with a fixed body size (set when the feature is turned on).</summary>
            public int FixedSize { get; set; }
        }

        public FermenterWorld()
        {
            _cells.Add(new FermenterCell(_kernel));
            _cellGroup.Add(0);
            _groups.Add(new Group(_cells[0].Traits.Count));
            _featureEnabled = new bool[GameFeatures.All.Count];
            for (int f = 0; f < _featureEnabled.Length; f++)
                _featureEnabled[f] = GameFeatures.All[f].DefaultEnabled;

            var details = new List<DetailInfo>();
            var read = new List<Func<FermenterCell, double>>();
            void Add(DetailInfo info, Func<FermenterCell, double> f)
            {
                details.Add(info);
                read.Add(f);
            }
            Add(new DetailInfo("medium_glucose", "Food", "Glucose at the membrane", "mol/m³", "mM", 1.0,
                "background plus any food packet the cell touches"), c => c.External("C_glc_e_mod_membrane_face"));
            for (int tr = 0; tr < _food.TraceSpecies.Count; tr++)
            {
                int t = tr;
                Add(new DetailInfo("trace_" + _food.TraceSpecies[t].Id, "Food", _food.TraceSpecies[t].Name + " (smell, needs a receptor)",
                    "mol/m³", "µM", 1e3, "a trace from " + _food.TraceSpecies[t].Source),
                    c => _traceAtCell.Length > 0 ? _traceAtCell[_cells.IndexOf(c) * _food.TraceSpecies.Count + t] : 0.0);
            }
            const string rate = "Metabolic rate", made = "ATP made", used = "ATP used", upkeep = "Protein turnover",
                state = "Cell state", growth = "Growth and storage";
            Add(new DetailInfo("atp_turnover", rate, "ATP turnover", "mol/s", "ATP/s", Avogadro,
                "ATP hydrolysed per second by everything in the cell"), c => c.Budget().AtpConsumed);
            Add(new DetailInfo("free_energy_power", rate, "Free-energy use", "W", "fW", 1e15,
                "ATP turnover x the free energy of each ATP now (it changes with ATP, ADP and Pi): the work the cell does"),
                c => c.Budget().AtpConsumed * c.AlgebraicValue("cytosol__A_hyd"));
            Add(new DetailInfo("heat", rate, "Heat", "W", "fW", 1e15, "reaction enthalpy released"), c => c.Budget().Heat);
            Add(new DetailInfo("specific_heat", rate, "Specific metabolic rate", "W/kg", "W/kg", 1.0,
                "heat per kg of cell (density 1100 kg/m³)"), c => c.Budget().Heat / (c.Volume * CellDensity));
            Add(new DetailInfo("glucose_in", rate, "Glucose taken up", "mol/s", "molecules/s", Avogadro),
                c => c.AlgebraicValue("membrane_face__J_glc"));
            Add(new DetailInfo("lactate_out", rate, "Lactate exported", "mol/s", "molecules/s", Avogadro),
                c => -c.AlgebraicValue("membrane_face__J_lac"));
            Add(new DetailInfo("atp_glycolysis", made, "Glycolysis (2 per glucose)", "mol/s", "ATP/s", Avogadro),
                c => c.Budget().AtpGlycolysis);
            Add(new DetailInfo("atp_from_glycogen", made, "From glycogen (3 per glucose)", "mol/s", "ATP/s", Avogadro),
                c => c.Budget().AtpFromGlycogen);
            Add(new DetailInfo("atp_pumps", used, "H⁺-ATPase pumping", "mol/s", "ATP/s", Avogadro,
                "powers the proton gradient that drives NhaA, Trk and KefC"), c => c.Budget().AtpPumps);
            Add(new DetailInfo("atp_protein_turnover", used, "Protein turnover", "mol/s", "ATP/s", Avogadro,
                "remaking degraded protein at the ribosomes (1%/h), all proteins including the unmodelled ones"),
                c => c.Budget().AtpProteinTurnover);
            Add(new DetailInfo("atp_protein_build", used, "Building new protein", "mol/s", "ATP/s", Avogadro,
                "while expression rises"), c => c.Budget().AtpProteinBuild);
            Add(new DetailInfo("atp_rna", used, "RNA turnover and ribosomes", "mol/s", "ATP/s", Avogadro,
                "mRNA lasts about 5 min; ribosome RNA upkeep"), c => c.Budget().AtpRna);
            Add(new DetailInfo("atp_lipids", used, "Membrane lipids", "mol/s", "ATP/s", Avogadro,
                "lipid turnover; scales with membrane area"), c => c.Budget().AtpLipids);
            Add(new DetailInfo("atp_repair", used, "Repair", "mol/s", "ATP/s", Avogadro), c => c.Budget().AtpRepair);
            Add(new DetailInfo("atp_growth", used, "Growth (building a daughter)", "mol/s", "ATP/s", Avogadro),
                c => c.Budget().AtpGrowth);
            Add(new DetailInfo("atp_storage", used, "Storing glycogen", "mol/s", "ATP/s", Avogadro), c => c.Budget().AtpStorage);
            Add(new DetailInfo("atp_other", used, "Other maintenance", "mol/s", "ATP/s", Avogadro,
                "everything not yet itemised (Lynch & Marinov total less the items above)"),
                c => c.Budget().AtpOtherMaintenance);
            Add(new DetailInfo("atp_net", used, "Net (made - used)", "mol/s", "ATP/s", Avogadro),
                c => c.Budget().AtpProduced - c.Budget().AtpConsumed);
            foreach (var key in new[] { "H_ATPase", "NhaA", "KefC", "Trk", "NCX", "Glf", "LacT", "Glyc", "proteome" })
            {
                string k = key;
                Add(new DetailInfo("turnover_" + k, upkeep, ProteinLabel(k), "mol/s", "ATP/s", Avogadro),
                    c => c.AlgebraicValue("protein__J_ATP_" + k));
            }
            Add(new DetailInfo("growth_progress", growth, "Daughter built", "", "%", 100.0), c => c.GrowthProgress);
            Add(new DetailInfo("glycogen_progress", growth, "Glycogen (share of a daughter)", "", "%", 100.0),
                c => c.GlycogenProgress);
            Add(new DetailInfo("glycogen", growth, "Glycogen", "mol/m³", "mM glucose", 1.0), c => c.Glycogen);
            Add(new DetailInfo("daughter_cost", growth, "Cost of a daughter", "mol", "ATP", Avogadro), c => c.DaughterCost);
            Add(new DetailInfo("membrane_used", growth, "Membrane space used by transporters", "", "% of limit", 100.0,
                "the adaptable transporters may take 8% of the membrane; the limit grows with the cell's surface"),
                c => c.TransporterArea() / c.TransporterAreaMax);
            Add(new DetailInfo("v", state, "Membrane potential", "V", "mV", 1e3), c => 1e-3 * c.State("membrane__V"));
            Add(new DetailInfo("ph", state, "Cytosol pH", "pH", "", 1.0), c => c.AlgebraicValue("H__pH_i"));
            Add(new DetailInfo("atp", state, "ATP", "mol/m³", "mM", 1.0), c => c.State("cytosol__C_ATP"));
            Add(new DetailInfo("atp_free_energy", state, "ATP free energy", "J/mol", "kJ/mol", 1e-3),
                c => c.AlgebraicValue("cytosol__A_hyd"));
            Add(new DetailInfo("na", state, "Na⁺", "mol/m³", "mM", 1.0), c => c.State("Na_K__Nai"));
            Add(new DetailInfo("k", state, "K⁺", "mol/m³", "mM", 1.0), c => c.State("Na_K__Ki"));
            Add(new DetailInfo("ca", state, "Ca²⁺ (free)", "mol/m³", "µM", 1e3), c => c.State("Ca__Cai"));
            Add(new DetailInfo("damage", state, "Damage", "", "% of lethal", 100.0), c => c.Damage);
            _details = details.ToArray();
            _read = read.ToArray();

            var traits = _cells[0].Traits;
            _traits = new TraitInfo[traits.Count];
            for (int i = 0; i < _traits.Length; i++)
                _traits[i] = new TraitInfo(traits[i].Id, traits[i].Label, traits[i].Protein, true, traits[i].Min,
                    traits[i].Max, true);
        }

        public IReadOnlyList<OverlayInfo> Overlays => OverlayList;

        public int CellCount => _cells.Count;

        /// <summary>Backward Euler macro step [s]: stable for the stiff cell, fine for its slow (s-h) dynamics.</summary>
        public double MacroDt => 0.1;

        public IReadOnlyList<DetailInfo> Details => _details;

        public IReadOnlyList<TraitInfo> Traits => _traits;

        public IReadOnlyList<ControlInfo> Controls => ControlList;

        public IReadOnlyList<FeatureInfo> Features => GameFeatures.All;

        public int MaxCells => CellLimit;

        public int MaxGroups => GroupLimit;

        public int GroupCount => _groups.Count;

        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                ApplyControls();
            }
        }

        /// <summary>The cell in slot <paramref name="cell"/> (for tests and the headless runner).</summary>
        public FermenterCell Cell(int cell) => _cells[Check(cell)];

        /// <summary>Background glucose between food packets [mol/m³].</summary>
        public double BathConcentration
        {
            get => _food.BackgroundGlucose;
            set
            {
                if (!(value >= 0.0))
                    throw new ArgumentOutOfRangeException(nameof(value), "must be >= 0 mol/m³");
                _food.BackgroundGlucose = value;
            }
        }

        public FoodField Food => _food;

        public double CellRadius => 1e-6;

        public void CellPosition(int cell, out double x, out double y) => ColonyLayout.Slot(Check(cell), out x, out y);

        public void Step(double t, double dt)
        {
            // Food: each cell sees the background plus the packets it touches; what it takes beyond the background comes
            // out of those packets. Glucose crosses the membrane through the cell's own carriers (Glf) in the kernel.
            _food.Step(dt);
            int nt = _food.TraceSpecies.Count;
            if (_traceAtCell.Length < _cells.Count * nt)
                _traceAtCell = new double[CellLimit * nt];
            for (int i = 0; i < _cells.Count; i++)
            {
                ColonyLayout.Slot(i, out double x, out double y);
                double c = _food.GlucoseAt(x, y, CellRadius);
                _cells[i].SetExternal("C_glc_e_mod_membrane_face", c);
                for (int tr = 0; tr < nt; tr++)
                    _traceAtCell[i * nt + tr] = _food.TraceAt(tr, x, y, CellRadius);
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].Step(dt);
                double c = _cells[i].External("C_glc_e_mod_membrane_face");
                double uptake = _cells[i].AlgebraicValue("membrane_face__J_glc") * dt;   // mol into the cell
                if (c > _food.BackgroundGlucose && uptake > 0.0)
                {
                    ColonyLayout.Slot(i, out double x, out double y);
                    _food.Withdraw(x, y, CellRadius, uptake * (c - _food.BackgroundGlucose) / c);
                }
            }
            if (Active(GameFeatures.AutoDivide) && Active(GameFeatures.GrowthAndDivision))
                for (int i = 0; i < _cells.Count; i++)
                    if (_cells[i].Alive && _cells[i].GrowthProgress >= 1.0 && Room(_cellGroup[i]))
                        DivideCell(i);
            ApplyFixedSize();
        }

        public double Read(int cell, int channel)
        {
            var c = _cells[Check(cell)];
            switch (channel)
            {
                case 0: return 1e-3 * c.State("membrane__V");
                case 1: return c.AlgebraicValue("H__pH_i");
                case 2: return c.State("cytosol__C_ATP");
                case 3: return c.State("Na_K__Nai");
                case 4: return c.Damage;
                case 5: return c.GrowthProgress;
                default: throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }

        public double ReadDetail(int cell, int detail) => _read[detail](_cells[Check(cell)]);

        public double ReadTrait(int cell, int trait) => _cells[Check(cell)].TraitValue(trait);

        public double ReadTraitTarget(int cell, int trait) => _cells[Check(cell)].TraitTarget(trait);

        public int CellGroup(int cell) => _cellGroup[Check(cell)];

        public bool CellAlive(int cell) => _cells[Check(cell)].Alive;

        public bool CellCanDivide(int cell) =>
            Active(GameFeatures.GrowthAndDivision) && _cells[Check(cell)].CanDivide && Room(_cellGroup[cell]);

        public double ReadControl(int group, int control) => _groups[CheckGroup(group)].Controls[control];

        public bool FeatureUnlocked(int feature) => GameFeatures.All[feature].UnlockLevel <= _level;

        public bool FeatureEnabled(int feature) => _featureEnabled[feature];

        public void SetTrait(int group, int trait, double multiplier)
        {
            CheckGroup(group);
            if (trait < 0 || trait >= _traits.Length)
                throw new ArgumentOutOfRangeException(nameof(trait), $"no trait {trait}");
            for (int i = 0; i < _cells.Count; i++)
                if (_cellGroup[i] == group)
                    _cells[i].SetTrait(trait, multiplier);
            // The group's target is what its cells accepted (the membrane ceiling may clamp it).
            int first = _cellGroup.IndexOf(group);
            _groups[group].Targets[trait] = first >= 0 ? _cells[first].TraitTarget(trait) : multiplier;
        }

        public void SetControl(int group, int control, double value)
        {
            CheckGroup(group);
            if (control < 0 || control >= ControlList.Length)
                throw new ArgumentOutOfRangeException(nameof(control), $"no control {control}");
            if (double.IsNaN(value))
                throw new ArgumentException("value is NaN", nameof(value));
            var info = ControlList[control];
            if (!Active(info.Feature))
                throw new InvalidOperationException($"{info.Label} needs {GameFeatures.All[GameFeatures.IndexOf(info.Feature)].Name}, " +
                                                    $"unlocked at level {GameFeatures.All[GameFeatures.IndexOf(info.Feature)].UnlockLevel}");
            _groups[group].Controls[control] = Math.Min(info.Max, Math.Max(info.Min, value));
            ApplyControls();
        }

        public void SetFeature(int feature, bool enabled)
        {
            if (feature < 0 || feature >= _featureEnabled.Length)
                throw new ArgumentOutOfRangeException(nameof(feature), $"no feature {feature}");
            var info = GameFeatures.All[feature];
            if (!FeatureUnlocked(feature))
                throw new InvalidOperationException($"{info.Name} unlocks at level {info.UnlockLevel}");
            if (!info.Optional && !enabled)
                throw new InvalidOperationException($"{info.Name} cannot be turned off");
            _featureEnabled[feature] = enabled;
            if (info.Id == GameFeatures.FixedBodySize && enabled)
                for (int g = 0; g < _groups.Count; g++)
                    _groups[g].FixedSize = AliveIn(g);
            ApplyControls();
        }

        public void Divide(int cell)
        {
            Check(cell);
            if (!Active(GameFeatures.GrowthAndDivision))
                throw new InvalidOperationException("Growth and division unlocks at level 2");
            if (!Room(_cellGroup[cell]))
                throw new InvalidOperationException(_cells.Count >= CellLimit
                    ? $"at most {CellLimit} cells"
                    : "the group has a fixed size and no dead cell to replace");
            if (!_cells[cell].CanDivide)
                throw new InvalidOperationException(
                    $"the daughter is {100 * _cells[cell].GrowthProgress:F1}% built and glycogen covers " +
                    $"{100 * _cells[cell].GlycogenProgress:F1}%: not enough yet");
            DivideCell(cell);
        }

        /// <summary>
        /// Development aid: marks the cell's daughter as fully built (the growth ATP is not charged), so division can be
        /// tried without waiting days of simulated time. Not for play.
        /// </summary>
        public void DevCompleteDaughter(int cell)
        {
            var c = _cells[Check(cell)];
            c.Batch.States[c.Batch.Descriptor.StateIndex("bio__P_growth")][0] = c.DaughterCost;
        }

        private void DivideCell(int cell)
        {
            var daughter = _cells[cell].Divide();
            if (daughter == null)
                return;
            int group = _cellGroup[cell];
            // With a fixed body size the daughter takes a dead cell's place; otherwise it joins the colony.
            int slot = Active(GameFeatures.FixedBodySize) ? DeadSlot(group) : -1;
            if (slot >= 0)
                _cells[slot] = daughter;
            else
            {
                _cells.Add(daughter);
                _cellGroup.Add(group);
            }
        }

        /// <summary>True if the group may gain a living cell.</summary>
        private bool Room(int group)
        {
            if (Active(GameFeatures.FixedBodySize))
                return AliveIn(group) < _groups[group].FixedSize && (DeadSlot(group) >= 0 || _cells.Count < CellLimit);
            return _cells.Count < CellLimit;
        }

        private void ApplyFixedSize() => ApplyControls();

        /// <summary>
        /// Pushes each group's controls to its cells: features that are locked or off contribute nothing, and a full group
        /// with a fixed size spends its growth share on repair instead.
        /// </summary>
        private void ApplyControls()
        {
            bool growthOn = Active(GameFeatures.GrowthAndDivision), storageOn = Active(GameFeatures.GlycogenStorage);
            bool repairOn = Active(GameFeatures.RepairLevel), fixedSize = Active(GameFeatures.FixedBodySize);
            for (int i = 0; i < _cells.Count; i++)
            {
                var g = _groups[_cellGroup[i]];
                double growth = growthOn ? g.Controls[GrowthControl] : 0.0;
                double repair = repairOn ? g.Controls[RepairControl] : 1.0;
                if (fixedSize && AliveIn(_cellGroup[i]) >= g.FixedSize)
                {
                    repair = Math.Min(1.0, repair + growth);
                    growth = 0.0;
                }
                var c = _cells[i];
                c.GrowthFraction = growth;
                c.StorageAllocation = storageOn ? g.Controls[StorageControl] : 0.0;
                c.RepairLevel = repair;
            }
        }

        private bool Active(string feature)
        {
            int f = GameFeatures.IndexOf(feature);
            return FeatureUnlocked(f) && _featureEnabled[f];
        }

        private int AliveIn(int group)
        {
            int n = 0;
            for (int i = 0; i < _cells.Count; i++)
                if (_cellGroup[i] == group && _cells[i].Alive)
                    n++;
            return n;
        }

        private int DeadSlot(int group)
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cellGroup[i] == group && !_cells[i].Alive)
                    return i;
            return -1;
        }

        private int Check(int cell)
        {
            if (cell < 0 || cell >= _cells.Count)
                throw new ArgumentOutOfRangeException(nameof(cell), $"no cell {cell}");
            return cell;
        }

        private int CheckGroup(int group)
        {
            if (group < 0 || group >= _groups.Count)
                throw new ArgumentOutOfRangeException(nameof(group), $"no group {group}");
            return group;
        }

        private static string ProteinLabel(string key)
        {
            switch (key)
            {
                case "H_ATPase": return "F1Fo H⁺-ATPase";
                case "NhaA": return "NhaA";
                case "KefC": return "KefC";
                case "Trk": return "Trk";
                case "NCX": return "Na⁺/Ca²⁺ exchanger";
                case "Glf": return "Glf glucose facilitator";
                case "LacT": return "Lactate exporter";
                case "Glyc": return "Glycolytic enzymes";
                default: return "Other proteins (ribosomal, nucleoid, enzymes)";
            }
        }
    }
}
