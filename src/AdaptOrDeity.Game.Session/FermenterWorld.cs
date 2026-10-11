using System;
using System.Collections.Generic;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The stage-1 world: fermenter cells (circulatory-autogen-modules cell/generic_adaptive_cell fermenter) in a Proterozoic
    /// sea. Cell physiology comes from the kernel; game-side values (food, colony, control ranges) from content/parameters.json.
    /// Cells belong to groups; traits and energy controls are set per group and apply to every cell in it. From level 2 a
    /// cell can put energy into a daughter and divide; at a lethal damage load it dies. With a fixed body size a group
    /// keeps its number of cells: growth energy replaces dead cells, and goes to repair while the group is full. Cells
    /// share the medium but do not yet exchange anything (the transport graph comes later). Values exchanged here are SI.
    /// </summary>
    public sealed class FermenterWorld : ISimWorld, IInspectableWorld, IBathWorld, IFoodWorld, IParallelWorld, IDisposable
    {
        /// <summary>Sim-seconds per real second: pause, 1x, 10x, 100x, 1000x.</summary>
        public static readonly double[] SpeedPresets = { 0.0, 1.0, 10.0, 100.0, 1000.0 };

        public const int GroupLimit = 8;
        public const int GrowthControl = 0, StorageControl = 1, RepairControl = 2;

        /// <summary>Cells snapshot frames are first sized for; they grow with the colony.</summary>
        public const int InitialCells = 64;

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
            new ControlInfo("growth", "Energy to growth", 0.0, GameParameters.Default.Value("controls.growth_max"), 0.0,
                GameFeatures.GrowthAndDivision, "× resting ATP use",
                "ATP put into building a daughter, as a multiple of the cell's resting ATP use (growing fermenters run 10-30x " +
                "their maintenance); limited by the glucose the cell can take up"),
            new ControlInfo("storage", "Glycogen storage", 0.0, 1.0, 0.0, GameFeatures.GlycogenStorage, "of capacity",
                "how hard the cell stores glucose as glycogen (2 ATP per glucose, 3 back later)"),
            new ControlInfo("repair", "Repair", 0.0, 1.0, 1.0, GameFeatures.RepairLevel, "",
                "how much the cell spends repairing damage; below about 0.1 damage outruns repair and the cell dies"),
        };

        private readonly IKernel _kernel = new GenericAdaptiveCell_fermenter();
        private readonly List<FermenterCell> _cells = new List<FermenterCell>();
        private ParallelWork? _workers;
        private Action<int, int>? _forcePass, _cellPass;   // cached delegates: stepping must not allocate
        private double _stepDt, _cos, _sin;
        private bool _motorsOn;
        private double[] _cellX = new double[0], _cellY = new double[0], _forceX = new double[0], _forceY = new double[0];
        private double[] _torque = new double[0];
        private bool[] _touching = new bool[0];
        private int[] _aliveIn = new int[0];   // living cells per group, counted by ApplyControls

        /// <summary>Cells per work chunk, and the fewest cells worth spreading over threads.</summary>
        private const int CellChunk = 2, ParallelFrom = 4;
        private readonly List<int> _cellGroup = new List<int>();
        private readonly List<Group> _groups = new List<Group>();
        private readonly bool[] _featureEnabled;
        private readonly DetailInfo[] _details;
        private readonly Func<int, double>[] _read;
        private readonly TraitInfo[] _traits;
        private int _level = 1;
        private readonly FoodField _food;
        private readonly CreatureBody _body;
        private readonly int _flagellaTrait;
        private readonly double _thrustAngleSd;
        private readonly double[] _defaultAngles;
        private readonly Random _motilityRandom;
        private readonly double _meanRun, _meanPause;
        private double _untilSwitch;
        private readonly double _avogadro, _cellDensity, _spacing, _cellMass;
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

        /// <param name="cellLimit">Most cells, for tests; no limit when null.</param>
        public FermenterWorld(int? cellLimit = null)
        {
            _cellLimit = cellLimit ?? int.MaxValue;
            _cells.Add(new FermenterCell(_kernel, TraitCatalog.Fermenter));
            var first = _cells[0];
            // Geometry and the medium from the kernel (upstream parameters.json); the rest of the food field from content.
            CellRadius = first.External("R_cell_mod_membrane") * 1e-6;   // µm → m
            _spacing = 2.0 * CellRadius + GameParameters.Default.Value("colony.cell_gap");
            _food = new FoodField(GameParameters.Default.Food(first.External("C_glc_e_mod_membrane_face")));
            _motilityRandom = new Random(GameParameters.Default.Integer("motility.seed"));
            _meanRun = GameParameters.Default.Value("motility.mean_run");
            _meanPause = GameParameters.Default.Value("motility.mean_pause");
            _untilSwitch = Exponential() * _meanPause;
            _thrustAngleSd = GameParameters.Default.Value("motility.thrust_angle_sd");
            _defaultAngles = CreatureBody.DefaultAngles(GameParameters.Default.Value("motility.flagellum_spacing"));
            _flagellaTrait = -1;
            for (int t = 0; t < first.Traits.Count; t++)
                if (first.Traits[t].Id == "flagella")
                    _flagellaTrait = t;
            _avogadro = first.External("N_A_mod_pool_proteome");
            _cellDensity = GameParameters.Default.Value("body.cell_density");
            // One rigid body in the water: each cell's mass from its volume, Stokes drag from the kernel's viscosity
            _cellMass = _cellDensity * first.AlgebraicValue("membrane__Vol");
            _body = new CreatureBody(CellRadius, first.External("eta_mod_i_flagellum"), GameParameters.Default.Body());
            AddToBody(0, _defaultAngles);
            _food.Populate(_body.Body.X, _body.Body.Y);
            double avogadro = _avogadro;
            _cellGroup.Add(0);
            _groups.Add(new Group(_cells[0].Traits.Count));
            _featureEnabled = new bool[GameFeatures.All.Count];
            for (int f = 0; f < _featureEnabled.Length; f++)
                _featureEnabled[f] = GameFeatures.All[f].DefaultEnabled;

            var details = new List<DetailInfo>();
            var read = new List<Func<int, double>>();   // by cell index
            void Add(DetailInfo info, Func<FermenterCell, double> f)
            {
                details.Add(info);
                read.Add(i => f(_cells[i]));
            }
            Add(new DetailInfo("medium_glucose", "Food", "Glucose at the membrane", "mol/m³", "mM", 1.0,
                "background plus any food packet the cell touches"), c => c.External("C_glc_e_mod_membrane_face"));
            for (int tr = 0; tr < _food.TraceSpecies.Count; tr++)
            {
                int t = tr;
                details.Add(new DetailInfo("trace_" + _food.TraceSpecies[t].Id, "Food", _food.TraceSpecies[t].Name + " (smell, needs a receptor)",
                    "mol/m³", "µM", 1e3, "a trace from " + _food.TraceSpecies[t].Source));
                read.Add(i => _traceAtCell.Length > 0 ? _traceAtCell[i * _food.TraceSpecies.Count + t] : 0.0);
            }
            const string rate = "Metabolic rate", made = "ATP made", used = "ATP used", upkeep = "Protein turnover",
                state = "Cell state", growth = "Growth and storage";
            Add(new DetailInfo("atp_turnover", rate, "ATP turnover", "mol/s", "ATP/s", avogadro,
                "ATP hydrolysed per second by everything in the cell"), c => c.Budget().AtpConsumed);
            Add(new DetailInfo("free_energy_power", rate, "Free-energy use", "W", "fW", 1e15,
                "ATP turnover x the free energy of each ATP now (it changes with ATP, ADP and Pi): the work the cell does"),
                c => c.Budget().AtpConsumed * c.AlgebraicValue("cytosol__A_hyd"));
            Add(new DetailInfo("heat", rate, "Heat", "W", "fW", 1e15, "reaction enthalpy released"), c => c.Budget().Heat);
            Add(new DetailInfo("specific_heat", rate, "Specific metabolic rate", "W/kg", "W/kg", 1.0,
                $"heat per kg of cell (density {_cellDensity:F0} kg/m³)"), c => c.Budget().Heat / (c.Volume * _cellDensity));
            Add(new DetailInfo("glucose_in", rate, "Glucose taken up", "mol/s", "molecules/s", avogadro),
                c => c.AlgebraicValue("membrane_face__J_glc"));
            Add(new DetailInfo("lactate_out", rate, "Lactate exported", "mol/s", "molecules/s", avogadro),
                c => -c.AlgebraicValue("membrane_face__J_lac"));
            Add(new DetailInfo("atp_glycolysis", made, "Glycolysis (2 per glucose)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpGlycolysis);
            Add(new DetailInfo("atp_from_glycogen", made, "From glycogen (3 per glucose)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpFromGlycogen);
            Add(new DetailInfo("atp_pumps", used, "H⁺-ATPase pumping", "mol/s", "ATP/s", avogadro,
                "powers the proton gradient that drives NhaA, Trk and KefC"), c => c.Budget().AtpPumps);
            Add(new DetailInfo("atp_protein_turnover", used, "Protein turnover", "mol/s", "ATP/s", avogadro,
                "remaking degraded protein at the ribosomes (1%/h), all proteins including the unmodelled ones"),
                c => c.Budget().AtpProteinTurnover);
            Add(new DetailInfo("atp_protein_build", used, "Building new protein", "mol/s", "ATP/s", avogadro,
                "while expression rises"), c => c.Budget().AtpProteinBuild);
            Add(new DetailInfo("atp_rna", used, "RNA turnover and ribosomes", "mol/s", "ATP/s", avogadro,
                "mRNA lasts about 5 min; ribosome RNA upkeep"), c => c.Budget().AtpRna);
            Add(new DetailInfo("atp_lipids", used, "Membrane lipids", "mol/s", "ATP/s", avogadro,
                "lipid turnover; scales with membrane area"), c => c.Budget().AtpLipids);
            Add(new DetailInfo("atp_repair", used, "Repair", "mol/s", "ATP/s", avogadro), c => c.Budget().AtpRepair);
            Add(new DetailInfo("atp_growth", used, "Growth (building a daughter)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpGrowth);
            Add(new DetailInfo("atp_storage", used, "Storing glycogen", "mol/s", "ATP/s", avogadro), c => c.Budget().AtpStorage);
            Add(new DetailInfo("atp_other", used, "Other maintenance", "mol/s", "ATP/s", avogadro,
                "everything not yet itemised (Lynch & Marinov total less the items above)"),
                c => c.Budget().AtpOtherMaintenance);
            Add(new DetailInfo("atp_net", used, "Net (made - used)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpProduced - c.Budget().AtpConsumed);
            foreach (var key in new[] { "H_ATPase", "NhaA", "KefC", "Trk", "NCX", "Glf", "LacT", "Glyc", "proteome" })
            {
                string k = key;
                Add(new DetailInfo("turnover_" + k, upkeep, ProteinLabel(k), "mol/s", "ATP/s", avogadro),
                    c => c.AlgebraicValue("pool_" + k + "__J_ATP"));
            }
            Add(new DetailInfo("growth_progress", growth, "Daughter built", "", "%", 100.0), c => c.GrowthProgress);
            Add(new DetailInfo("glycogen_progress", growth, "Glycogen (share of a daughter)", "", "%", 100.0),
                c => c.GlycogenProgress);
            Add(new DetailInfo("glycogen", growth, "Glycogen", "mol/m³", "mM glucose", 1.0), c => c.Glycogen);
            Add(new DetailInfo("daughter_cost", growth, "Cost of a daughter", "mol", "ATP", avogadro), c => c.DaughterCost);
            Add(new DetailInfo("membrane_used", growth, "Membrane space used by transporters", "", "% of limit", 100.0,
                $"the proteins may take {100 * first.External("area_max_fraction_mod_protein"):F0}% of the membrane; the " +
                "limit grows with the cell's surface"),
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
                    traits[i].Max, traits[i].Step <= 0.0, traits[i].Step);
        }

        public IReadOnlyList<OverlayInfo> Overlays => OverlayList;

        public int CellCount => _cells.Count;

        /// <summary>Backward Euler macro step [s]: stable for the stiff cell, fine for its slow (s-h) dynamics.</summary>
        public double MacroDt => 0.1;

        public IReadOnlyList<DetailInfo> Details => _details;

        public IReadOnlyList<TraitInfo> Traits => _traits;

        public IReadOnlyList<ControlInfo> Controls => ControlList;

        public IReadOnlyList<FeatureInfo> Features => GameFeatures.All;

        public int InitialCellCapacity => Math.Min(_cellLimit, InitialCells);

        private readonly int _cellLimit;

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

        public CreatureBody Body => _body;

        /// <summary>True while the flagellar motors run (a run); false in a pause.</summary>
        public bool MotorOn { get; private set; }

        /// <summary>Total proton-motive power of the flagellar motors now [W].</summary>
        public double MotorPower
        {
            get
            {
                double p = 0.0;
                foreach (var c in _cells)
                    if (c.Alive)
                        p += c.FlagellumPower;
                return p;
            }
        }

        /// <summary>
        /// The creature as a module array: each living cell an instance of the assembled cell module (content/assembled)
        /// named cell_&lt;index&gt;, with the parameters the player set. libcuflynx builds its CellML from it and PhLynx
        /// imports it. Flagellum angles and the body layout are game state, not CellML, so they are not in it.
        /// </summary>
        public ModuleArray ToModuleArray()
        {
            var d = _kernel.Descriptor;
            var array = new ModuleArray();
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                if (!cell.Alive)
                    continue;
                string name = "cell_" + i.ToString(CultureInfo.InvariantCulture);
                array.Records.Add(new ModuleRecord(name, d.ModuleType, d.Version, cell.Instance));
                var units = ModuleLibrary.InstanceUnits(d.ModuleType, d.Version, cell.Instance);
                foreach (var p in cell.PlayerParameters)
                    array.Parameters.Add(new ParameterRow(p + "_" + name, units[p], cell.External(p), "set by the player"));
            }
            return array;
        }

        /// <summary>Moves flagellum <paramref name="k"/> of a cell to a base angle on its membrane (body frame) [rad].</summary>
        public void SetFlagellumAngle(int cell, int k, double angle) => _body.SetFlagellumAngle(Check(cell), k, angle);

        private void AddToBody(int cell, IReadOnlyList<double> angles)
        {
            ColonyLayout.Slot(cell, _spacing, out double x, out double y);
            _body.AddCell(x, y, _cellMass, angles);
        }

        /// <summary>Cell radius [m] (the kernel's R_cell).</summary>
        public double CellRadius { get; }

        /// <summary>Position of the cell over the ground [m]: where it sits on the body, as the body moves and turns.</summary>
        public void CellPosition(int cell, out double x, out double y) => _body.WorldPosition(Check(cell), out x, out y);

        /// <summary>
        /// Threads that step cells (the simulation thread included). Results are the same for any number: each cell
        /// steps on its own, and what cells share (the food they take) is settled afterwards in cell order.
        /// </summary>
        public int Workers
        {
            get => _workers?.Workers ?? _workerCount;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "at least one worker");
                _workerCount = value;
                _workers?.Dispose();
                _workers = null;
            }
        }

        private int _workerCount = ParallelWork.DefaultWorkers;

        /// <summary>Steady cells sleep below this drift (<see cref="FermenterCell.SleepTolerance"/>); 0 never.</summary>
        public double SleepTolerance { get; set; }

        public void Dispose()
        {
            _workers?.Dispose();
            _workers = null;
        }

        public void Step(double t, double dt)
        {
            // The creature moves over the ground and the food window follows it. Each cell sees the background plus the
            // packets it touches; what it takes beyond the background comes out of those packets. Glucose crosses the
            // membrane through the cell's own carriers (Glf) in the kernel. Per-cell work runs on the workers; what cells
            // share (the body's force, the packets) is summed or settled afterwards in cell order, so results do not
            // depend on the number of workers.
            int n = _cells.Count;
            if (_cellX.Length < n)
                Array.Resize(ref _cellX, 2 * n);
            if (_cellY.Length < n)
                Array.Resize(ref _cellY, 2 * n);
            if (_forceX.Length < n)
            {
                Array.Resize(ref _forceX, 2 * n);
                Array.Resize(ref _forceY, 2 * n);
                Array.Resize(ref _torque, 2 * n);
                Array.Resize(ref _touching, 2 * n);
            }
            int nt = _food.TraceSpecies.Count;
            if (_traceAtCell.Length < n * nt)
                _traceAtCell = new double[2 * n * nt];

            _motorsOn = SwitchRuns(dt);
            _forcePass ??= ForcePass;
            ForEachCell(_forcePass);
            double fx = 0.0, fy = 0.0, torque = 0.0;
            for (int i = 0; i < n; i++)
            {
                fx += _forceX[i];
                fy += _forceY[i];
                torque += _torque[i];
            }
            _body.Step(dt, fx, fy, torque);

            var b = _body.Body;
            _food.Step(dt, b.X, b.Y, b.GroundVx, b.GroundVy, b.Angle);
            _cos = Math.Cos(b.Angle);
            _sin = Math.Sin(b.Angle);
            _stepDt = dt;
            _cellPass ??= CellPass;
            ForEachCell(_cellPass);

            // Food stays with the cell that found it; what cells took beyond the background leaves the packets.
            for (int i = 0; i < n; i++)
                if (_touching[i] && _cells[i].Alive)
                    _food.StickTo(_cellX[i], _cellY[i], CellRadius);
            for (int i = 0; i < n; i++)
            {
                if (!_touching[i])
                    continue;
                double c = _cells[i].MembraneGlucose;
                double uptake = _cells[i].GlucoseUptake * dt;   // mol into the cell
                if (c > _food.BackgroundGlucose && uptake > 0.0)
                    _food.Withdraw(_cellX[i], _cellY[i], CellRadius, uptake * (c - _food.BackgroundGlucose) / c);
            }
            if (Active(GameFeatures.AutoDivide) && Active(GameFeatures.GrowthAndDivision))
                for (int i = 0; i < _cells.Count; i++)
                    if (_cells[i].Alive && _cells[i].GrowthProgress >= 1.0 && Room(_cellGroup[i]))
                        DivideCell(i);
            ApplyFixedSize();
        }

        // Each cell's motors, and its flagella's force on the body from the last step's state (the motors' response to
        // the proton-motive force is not lagged by more than a step).
        private void ForcePass(int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                var c = _cells[i];
                c.MotorActivation = _motorsOn && c.Alive ? 1.0 : 0.0;
                double flagella = _flagellaTrait >= 0 ? c.TraitValue(_flagellaTrait) : 0.0;
                double thrust = c.Alive ? Math.Max(0.0, c.FlagellumThrust) : 0.0;
                _body.CellForce(i, flagella, thrust, out _forceX[i], out _forceY[i], out _torque[i]);
            }
        }

        // Each cell's position, the food at its membrane, and its own integration.
        private void CellPass(int start, int end)
        {
            int nt = _food.TraceSpecies.Count;
            for (int i = start; i < end; i++)
            {
                _body.WorldPosition(i, _cos, _sin, out double x, out double y);
                _cellX[i] = x;
                _cellY[i] = y;
                _touching[i] = _food.TouchesAny(x, y, CellRadius);
                var c = _cells[i];
                c.SleepTolerance = SleepTolerance;
                c.MembraneGlucose = _food.GlucoseAt(x, y, CellRadius);
                for (int tr = 0; tr < nt; tr++)
                    _traceAtCell[i * nt + tr] = _food.TraceAt(tr, x, y, CellRadius);
                c.Step(_stepDt);
            }
        }

        /// <summary>Runs a pass over every cell, on the worker threads when there are enough cells.</summary>
        public void ForEachCell(Action<int, int> pass)
        {
            if (_cells.Count < ParallelFrom || _workerCount == 1)
            {
                pass(0, _cells.Count);
                return;
            }
            _workers ??= new ParallelWork(_workerCount);
            _workers.Run(_cells.Count, CellChunk, pass);
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

        public double ReadDetail(int cell, int detail) => _read[detail](Check(cell));

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
                throw new InvalidOperationException(_cells.Count >= _cellLimit
                    ? $"at most {_cellLimit} cells"
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
            c.SetState("bio__P_growth", c.DaughterCost);
        }

        private void DivideCell(int cell)
        {
            var daughter = _cells[cell].Divide();
            if (daughter == null)
                return;
            int group = _cellGroup[cell];
            // With a fixed body size the daughter takes a dead cell's place; otherwise it joins the colony.
            int slot = Active(GameFeatures.FixedBodySize) ? DeadSlot(group) : -1;
            var angles = _body.FlagellumAngles(cell);   // the daughter inherits where the flagella sit
            if (slot >= 0)
            {
                _cells[slot] = daughter;
                _body.SetFlagellumAngles(slot, angles);
            }
            else
            {
                _cells.Add(daughter);
                _cellGroup.Add(group);
                AddToBody(_cells.Count - 1, angles);
            }
        }

        /// <summary>True if the group may gain a living cell.</summary>
        private bool Room(int group)
        {
            if (Active(GameFeatures.FixedBodySize))
                return AliveIn(group) < _groups[group].FixedSize && (DeadSlot(group) >= 0 || _cells.Count < _cellLimit);
            return _cells.Count < _cellLimit;
        }

        private void ApplyFixedSize() => ApplyControls();

        private double Exponential() => -Math.Log(1.0 - _motilityRandom.NextDouble());

        /// <summary>
        /// Runs and pauses at random (no sensing yet: level 2 adds deciding when to swim). In a run every living cell's
        /// motors turn; each flagellum pushes its cell away from its base with the kernel's thrust (proton motive force,
        /// number of flagella), off by a normal error drawn for the run, and the body moves and turns as one rigid body
        /// against the water's drag (<see cref="ForcePass"/>). With swimming turned off the motors stay off. Returns whether
        /// the motors run this step.
        /// </summary>
        private bool SwitchRuns(double dt)
        {
            bool on = Active(GameFeatures.Swimming);
            _untilSwitch -= dt;
            while (_untilSwitch <= 0.0)
            {
                MotorOn = !MotorOn;
                if (MotorOn)
                    _body.NewRun(_motilityRandom, _thrustAngleSd);
                _untilSwitch += Exponential() * (MotorOn ? _meanRun : _meanPause);
            }
            return on && MotorOn;
        }

        /// <summary>
        /// Pushes each group's controls to its cells: features that are locked or off contribute nothing, and a full group
        /// with a fixed size spends its growth share on repair instead.
        /// </summary>
        private void ApplyControls()
        {
            bool growthOn = Active(GameFeatures.GrowthAndDivision), storageOn = Active(GameFeatures.GlycogenStorage);
            bool repairOn = Active(GameFeatures.RepairLevel), fixedSize = Active(GameFeatures.FixedBodySize);
            if (fixedSize)
            {
                if (_aliveIn.Length < _groups.Count)
                    _aliveIn = new int[2 * _groups.Count];
                Array.Clear(_aliveIn, 0, _groups.Count);
                for (int i = 0; i < _cells.Count; i++)
                    if (_cells[i].Alive)
                        _aliveIn[_cellGroup[i]]++;
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                var g = _groups[_cellGroup[i]];
                double growth = growthOn ? g.Controls[GrowthControl] : 0.0;
                double repair = repairOn ? g.Controls[RepairControl] : 1.0;
                if (fixedSize && _aliveIn[_cellGroup[i]] >= g.FixedSize)
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
