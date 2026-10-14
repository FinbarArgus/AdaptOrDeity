using System;
using System.Collections.Generic;
using System.Globalization;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The stage-1 world: fermenter cells (circulatory-autogen-modules cell/generic_adaptive_cell fermenter) in a Proterozoic
    /// sea. Cell physiology comes from the kernel; game-side values (food, colony, control ranges) from content/parameters.json.
    /// Cells belong to groups; traits and energy controls are set per group and apply to every cell in it. From level 2 a
    /// cell can put energy into a daughter and divide (the daughter stays only with multicellularity; otherwise it leaves,
    /// lives on its own in the same water (FermenterWorld.Organisms.cs) and earns reproduction points), and cells engulf prey and small food packets (FermenterWorld.Engulfing.cs); at a
    /// lethal damage load a cell dies. With a fixed body size a group
    /// keeps its number of cells: growth energy replaces dead cells, and goes to repair while the group is full. From level 4
    /// the water between the cells is a space of its own and the cells fall into boundary and inner groups
    /// (FermenterWorld.Colony.cs). Values exchanged here are SI.
    /// </summary>
    public sealed partial class FermenterWorld : ISimWorld, IInspectableWorld, IBathWorld, IFoodWorld, IParallelWorld, IDisposable
    {
        /// <summary>Sim-seconds per real second: pause, 1x, 10x, 100x, 1000x.</summary>
        public static readonly double[] SpeedPresets = { 0.0, 1.0, 10.0, 100.0, 1000.0 };

        public const int GroupLimit = 8;
        public const int GrowthControl = 0, StorageControl = 1, RepairControl = 2, MatrixControl = 3;

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
            new ControlInfo("matrix", "Extracellular matrix", 0.0, 1.0, 0.0, GameFeatures.ExtracellularMatrix, "of capacity",
                "how hard the cells make matrix for the space between cells (each glucose unit costs a glucose and about " +
                "3 ATP; matrix made on open water is lost to the sea)"),
        };

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
        private readonly Func<FermenterCell, double>[] _readCell;   // per detail, of any cell (a free organism's too)
        private readonly int[] _detailTrace;                         // per detail: its trace species, or -1
        private readonly TraitInfo[] _traits;
        private int _level = 1;
        private readonly bool[] _bought = new bool[GameFeatures.All.Count];
        private int _pointsSpent;
        private double _time, _levelStart;
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
            public Group(IReadOnlyList<TraitDefinition> traits)
            {
                Targets = new double[traits.Count];
                for (int i = 0; i < traits.Count; i++)
                    Targets[i] = traits[i].Default;
                for (int c = 0; c < ControlList.Length; c++)
                    Controls[c] = ControlList[c].DefaultValue;
                Regulation = new bool[(traits.Count + 1 + FermenterCell.MaintenanceRegulation.Length)
                                      * FermenterCell.RegulationSpecies.Length];
            }

            /// <summary>A group with another's trait targets and controls.</summary>
            public Group(Group other)
            {
                Targets = (double[])other.Targets.Clone();
                Array.Copy(other.Controls, Controls, Controls.Length);
                Regulation = (bool[])other.Regulation.Clone();
            }

            public double[] Targets { get; }

            public double[] Controls { get; } = new double[ControlList.Length];

            /// <summary>Per regulated target (traits, growth, maintenance), per species: whether its fall lowers it.</summary>
            public bool[] Regulation { get; }

            /// <summary>Cells the group holds with a fixed body size (set when the feature is turned on).</summary>
            public int FixedSize { get; set; }
        }

        /// <param name="cellLimit">Most cells, for tests; no limit when null.</param>
        /// <param name="accuracy">
        /// <see cref="FieldAccuracy.Reference"/>: the environment without its speed-for-accuracy trade-offs, to test the
        /// game's settings against.
        /// </param>
        public FermenterWorld(int? cellLimit = null, FieldAccuracy accuracy = FieldAccuracy.Game)
        {
            Accuracy = accuracy;
            if (accuracy == FieldAccuracy.Reference)
                _inputTolerance = 0.0;
            _cellLimit = cellLimit ?? int.MaxValue;
            _cells.Add(new FermenterCell(TraitCatalog.Fermenter));
            var first = _cells[0];
            if (accuracy == FieldAccuracy.Reference)
                first.RelativeTolerance = GameParameters.Default.Value("field.reference_cell_tolerance");   // daughters inherit it
            // Geometry from the kernel (upstream parameters.json); the open water and food field from content.
            CellRadius = first.External("R_cell_mod_membrane") * 1e-6;   // µm → m
            _oceanProtons = first.External("H_o_mod_H");
            _spacing = 2.0 * CellRadius + GameParameters.Default.Value("colony.cell_gap");
            _food = new FoodField(GameParameters.Default.Food(GameTimeline.Default.OceanGlucose(1)));
            _prey = CreatePrey();
            _vacuoles.Add(null);
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
            // flagellar thrust scaled for play (content motility.thrust_scale); daughters inherit it with their parameters
            first.SetExternal("v_one_mod_i_flagellum",
                first.External("v_one_mod_i_flagellum") * GameParameters.Default.Value("motility.thrust_scale"));
            // and its proton cost per revolution with it (content motility.proton_cost_scale): the same efficiency
            first.SetExternal("n_H_rev_mod_i_flagellum",
                first.External("n_H_rev_mod_i_flagellum") * GameParameters.Default.Value("motility.proton_cost_scale"));
            _body = new CreatureBody(CellRadius, first.External("eta_mod_i_flagellum"), GameParameters.Default.Body());
            _tideX = _body.Body.CurrentVx;   // the tidal current's amplitude above the seabed's boundary layer
            _tideY = _body.Body.CurrentVy;
            _tidePeriod = GameParameters.Default.Value("environment.tide_period");
            Tide(0.0);
            Seabed = new Seabed(GameParameters.Default.Seabed(), _body.Body.Y);
            _cellAt = i =>
            {
                _body.WorldPosition(i, out double x, out double y);
                return (x, y);
            };
            AddToBody(0, _defaultAngles);
            _food.Populate(_body.Body.X, _body.Body.Y);
            _food.BodyRange = OrganismRange;   // dead organisms' bodies are simulated as far as living ones are
            _food.MaxFrozenBodies = GameParameters.Default.Integer("organisms.frozen_bodies_max");
            _bodyTrace = BodyTrace();
            _environment = CreateEnvironment(first);
            first.StopAtMacroSteps = true;   // the field changes cells' inputs every macro step; daughters inherit it
            double avogadro = _avogadro;
            _cellGroup.Add(0);
            _groups.Add(new Group(_cells[0].Traits));
            _featureEnabled = new bool[GameFeatures.All.Count];
            for (int f = 0; f < _featureEnabled.Length; f++)
                _featureEnabled[f] = GameFeatures.All[f].DefaultEnabled;

            var details = new List<DetailInfo>();
            var read = new List<Func<int, double>>();   // by cell index
            var readCell = new List<Func<FermenterCell, double>>();
            var detailTrace = new List<int>();
            void Add(DetailInfo info, Func<FermenterCell, double> f)
            {
                details.Add(info);
                read.Add(i => f(_cells[i]));
                readCell.Add(f);
                detailTrace.Add(-1);
            }
            Add(new DetailInfo("medium_glucose", "Food", "Glucose at the membrane", "mol/m³", "mM", 1.0,
                "background plus any food packet the cell touches"), c => c.MembraneGlucose);
            for (int tr = 0; tr < _food.TraceSpecies.Count; tr++)
            {
                int t = tr;
                details.Add(new DetailInfo("trace_" + _food.TraceSpecies[t].Id, "Food", _food.TraceSpecies[t].Name + " (smell, needs a receptor)",
                    "mol/m³", "µM", 1e3, "a trace from " + _food.TraceSpecies[t].Source));
                read.Add(i => _traceAtCell.Length > 0 ? _traceAtCell[i * _food.TraceSpecies.Count + t] : 0.0);
                readCell.Add(c => 0.0);
                detailTrace.Add(t);
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
            Add(new DetailInfo("glucose_in", rate, "Glucose taken up", "mol/s", "molecules/s", avogadro,
                "by all the cell's glucose carriers"), c => c.GlucoseUptake);
            Add(new DetailInfo("glucose_glf", rate, "Through Glf", "mol/s", "molecules/s", avogadro,
                "the glucose facilitator; negative while it leaks glucose out"), c => c.GlfUptake);
            Add(new DetailInfo("lactate_out", rate, "Lactate exported", "mol/s", "molecules/s", avogadro),
                c => -c.AlgebraicValue("membrane_face__J_lac"));
            Add(new DetailInfo("atp_glycolysis", made, "Glycolysis (2 per glucose)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpGlycolysis);
            Add(new DetailInfo("atp_from_glycogen", made, "From glycogen (3 per glucose)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpFromGlycogen);
            Add(new DetailInfo("atp_recycled_protein", made, "Recycled protein", "mol/s", "ATP/s", avogadro,
                "amino acids of removed protein: reused to build new copies (saving about 23 ATP each against making them " +
                "from glucose) or broken down for a little ATP"), c => c.Budget().AtpRecycledProtein);
            Add(new DetailInfo("atp_pumps", used, "H⁺-ATPase pumping", "mol/s", "ATP/s", avogadro,
                "powers the proton gradient that drives NhaA, Trk and KefC"), c => c.Budget().AtpPumps);
            Add(new DetailInfo("atp_protein_turnover", used, "Protein turnover", "mol/s", "ATP/s", avogadro,
                "remaking degraded protein at the ribosomes (1%/h), all proteins including the unmodelled ones"),
                c => c.Budget().AtpProteinTurnover);
            Add(new DetailInfo("atp_protein_build", used, "Building new protein", "mol/s", "ATP/s", avogadro,
                "while expression rises, at the full cost of making the amino acids from glucose (recycled ones pay " +
                "back under ATP made)"), c => c.Budget().AtpProteinBuild);
            Add(new DetailInfo("atp_protein_breakdown", used, "Protein breakdown", "mol/s", "ATP/s", avogadro,
                "proteases removing copies above a lowered target, about 1 ATP per amino acid"),
                c => c.Budget().AtpProteinBreakdown);
            Add(new DetailInfo("atp_rna", used, "RNA turnover and ribosomes", "mol/s", "ATP/s", avogadro,
                "mRNA lasts about 5 min; ribosome RNA upkeep"), c => c.Budget().AtpRna);
            Add(new DetailInfo("atp_lipids", used, "Membrane lipids", "mol/s", "ATP/s", avogadro,
                "lipid turnover; scales with membrane area"), c => c.Budget().AtpLipids);
            Add(new DetailInfo("atp_repair", used, "Repair", "mol/s", "ATP/s", avogadro), c => c.Budget().AtpRepair);
            Add(new DetailInfo("atp_growth", used, "Growth (building a daughter)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpGrowth);
            Add(new DetailInfo("atp_storage", used, "Storing glycogen", "mol/s", "ATP/s", avogadro), c => c.Budget().AtpStorage);
            Add(new DetailInfo("atp_carriers", used, "Sugar carriers (PTS, ABC)", "mol/s", "ATP/s", avogadro,
                "a PEP per glucose for the PTS, about 2 ATP per glucose for the ABC importer"), c => c.Budget().AtpCarriers);
            Add(new DetailInfo("atp_matrix", used, "Making matrix", "mol/s", "ATP/s", avogadro,
                "about 3 ATP per glucose unit of matrix exported, besides the glucose"), c => c.Budget().AtpMatrix);
            Add(new DetailInfo("atp_other", used, "Other maintenance", "mol/s", "ATP/s", avogadro,
                "everything not yet itemised (Lynch & Marinov total less the items above)"),
                c => c.Budget().AtpOtherMaintenance);
            Add(new DetailInfo("atp_net", used, "Net (made - used)", "mol/s", "ATP/s", avogadro),
                c => c.Budget().AtpProduced - c.Budget().AtpConsumed);
            foreach (var key in new[] { "H_ATPase", "NhaA", "KefC", "Trk", "NCX", "Glf", "LacT", "Hydrolase", "PTS", "ABC", "GalP",
                         "Flagellum", "Glyc", "proteome" })
            {
                string k = key;
                Add(new DetailInfo("turnover_" + k, upkeep, ProteinLabel(k), "mol/s", "ATP/s", avogadro),
                    c => c.AlgebraicValue("pool_" + k + "__J_ATP"));
            }
            Add(new DetailInfo("growth_progress", growth, "Daughter built", "", "%", 100.0), c => c.GrowthProgress);
            Add(new DetailInfo("matrix_export", growth, "Matrix exported", "mol/s", "glucose units/s", avogadro,
                "into the space between cells (lost to the sea where the cell faces open water)"), c => c.MatrixExport);
            details.Add(new DetailInfo("open_share", growth, "Edge on open water", "", "%", 100.0,
                "the rest of the edge faces the space between cells (level 4)"));
            read.Add(OpenShare);
            readCell.Add(c => 1.0);   // a free organism's whole edge is on open water
            detailTrace.Add(-1);
            Add(new DetailInfo("glycogen_progress", growth, "Glycogen (share of a daughter)", "", "%", 100.0),
                c => c.GlycogenProgress);
            Add(new DetailInfo("glycogen", growth, "Glycogen", "mol/m³", "mM glucose", 1.0), c => c.Glycogen);
            Add(new DetailInfo("daughter_cost", growth, "Cost of a daughter", "mol", "ATP", avogadro), c => c.DaughterCost);
            Add(new DetailInfo("membrane_used", growth, "Membrane space used by proteins", "", "% of limit", 100.0,
                $"the membrane proteins may take {100 * first.External("area_max_fraction_mod_protein"):F0}% of the membrane; " +
                "the limit grows with the cell's surface"),
                c => c.MembraneShareUsed());
            Add(new DetailInfo("v", state, "Membrane potential", "V", "mV", 1e3), c => 1e-3 * c.State("membrane__V"));
            Add(new DetailInfo("ph", state, "Cytosol pH", "pH", "", 1.0), c => c.AlgebraicValue("H__pH_i"));
            Add(new DetailInfo("atp", state, "ATP", "mol/m³", "mM", 1.0), c => c.State("cytosol__C_ATP"));
            Add(new DetailInfo("glucose_inside", state, "Glucose inside", "mol/m³", "mM", 1.0), c => c.State("cytosol__C_glc"));
            Add(new DetailInfo("atp_free_energy", state, "ATP free energy", "J/mol", "kJ/mol", 1e-3),
                c => c.AlgebraicValue("cytosol__A_hyd"));
            Add(new DetailInfo("na", state, "Na⁺", "mol/m³", "mM", 1.0), c => c.State("Na_K__Nai"));
            Add(new DetailInfo("k", state, "K⁺", "mol/m³", "mM", 1.0), c => c.State("Na_K__Ki"));
            Add(new DetailInfo("ca", state, "Ca²⁺ (free)", "mol/m³", "µM", 1e3), c => c.State("Ca__Cai"));
            Add(new DetailInfo("damage", state, "Damage", "", "% of lethal", 100.0), c => c.Damage);
            Add(new DetailInfo("recycled_amino_acids", state, "Recycled amino acids", "", "amino acids", 1.0,
                "freed by removing protein above a lowered target; built into new protein first, the rest slowly broken " +
                "down"), c => c.RecycledAminoAcids);
            _details = details.ToArray();
            _read = read.ToArray();
            _readCell = readCell.ToArray();
            _detailTrace = detailTrace.ToArray();

            var traits = _cells[0].Traits;
            _traits = new TraitInfo[traits.Count];
            for (int i = 0; i < _traits.Length; i++)
                _traits[i] = new TraitInfo(traits[i].Id, traits[i].Label, traits[i].Protein, true, traits[i].Min,
                    traits[i].Max, traits[i].Kind == TraitKind.Expression, traits[i].Step, traits[i].Kind == TraitKind.Share,
                    traits[i].Feature);
            ResetMeters();   // level 1 starts
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
                _levelStart = _time;
                ResetMeters();
                ApplyControls();
                // what the level's world supplies per second (content/timeline.json levels[].arrivals)
                SetOcean(GameTimeline.Default.OceanGlucose(value));
                var arrivals = GameTimeline.Default.ArrivalsAt(value);
                _food.SetArrivals(arrivals.FoodPerSecond, arrivals.CarriedShare);
                _prey.SetArrivals(arrivals.PreyPerSecond);
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
                SetOcean(value);
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
            var d = FermenterCell.Module.Descriptor;
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

        public void Dispose()
        {
            _workers?.Dispose();
            _workers = null;
            _space?.Dispose();
            _space = null;
        }

        /// <summary>The ground and rocks at the bottom of the world.</summary>
        public Seabed Seabed { get; }

        /// <summary>True when the creature rested against the ground or a rock in the last step.</summary>
        public bool TouchingSeabed { get; private set; }

        private double _freeVx, _freeVy;
        private readonly double _tideX, _tideY, _tidePeriod;

        /// <summary>The free current now [m/s] (above the boundary layer): the tide, flowing along its heading and back.</summary>
        public double FreeCurrentX => _freeVx;

        public double FreeCurrentY => _freeVy;

        // The tidal current at time t: amplitude x cos(2 pi t / period).
        private void Tide(double t)
        {
            double phase = Math.Cos(2.0 * Math.PI * t / _tidePeriod);
            _freeVx = _tideX * phase;
            _freeVy = _tideY * phase;
        }
        private readonly Func<int, (double X, double Y)>? _cellAt;

        // A body's current: the free current scaled by the seabed's boundary layer and wakes at its position.
        private void CurrentAt(RigidBody2D body)
        {
            double f = Seabed.CurrentFactor(body.X, body.Y, _freeVx * _tideX + _freeVy * _tideY >= 0.0 ? 1.0 : -1.0);
            body.CurrentVx = _freeVx * f;
            body.CurrentVy = _freeVy * f;
        }

        public void Step(double t, double dt)
        {
            Tide(t);
            _time = t + dt;
            StepWorld(t, dt);
            // A level is passed when all its goals are met (content/timeline.json levels[].goals): its meters convert to
            // skill points and the next begins.
            if (LevelGoalsMet && GameTimeline.Default.HasLevel(_level + 1))
            {
                ScoreLevel();
                Level = _level + 1;
            }
        }

        /// <summary>Simulated time left until the level's survival goal [s] (0 once reached; infinite without one).</summary>
        public double LevelTimeLeft => Math.Max(0.0, GameTimeline.Default.DurationOf(_level) - (_time - _levelStart));

        /// <summary>
        /// True when every goal of the current level is met: survive — a living cell once the level has run its seconds;
        /// achievement — earned in this level.
        /// </summary>
        public bool LevelGoalsMet
        {
            get
            {
                var goals = GameTimeline.Default.GoalsOf(_level);
                if (goals.Count == 0)
                    return false;
                for (int i = 0; i < goals.Count; i++)
                {
                    var goal = goals[i];
                    if (goal.Kind == "survive" && (_time - _levelStart < goal.Seconds || !AnyCellAlive()))
                        return false;
                    if (goal.Kind == "achievement")
                    {
                        int a = GameAchievements.IndexOf(goal.Id);
                        if (a < 0 || _achievedLevel[a] != _level)
                            return false;
                    }
                }
                return true;
            }
        }

        private bool AnyCellAlive()
        {
            foreach (var c in _cells)
                if (c.Alive)
                    return true;
            return false;
        }

        private void StepWorld(double t, double dt)
        {
            // The creature moves over the ground and the food window follows it. Each cell sees the environment field at its
            // membrane and, on the share of its membrane facing them, the free sugar of the packets it touches; what it
            // takes through that share comes out of those packets. Glucose crosses the
            // membrane through the cell's own carriers (Glf, and the PTS, ABC and H+ symport once evolved) in the kernel,
            // each seeing the same membrane glucose. Per-cell work runs on the workers; what cells
            // share (the body's force, the packets) is summed or settled afterwards in cell order, so results do not
            // depend on the number of workers.
            _stepDt = dt;
            UpdateColony();
            int n = _cells.Count;
            if (_cellX.Length < n)
                Array.Resize(ref _cellX, 2 * n);
            if (_openGlucoseShare.Length < n)
                Array.Resize(ref _openGlucoseShare, 2 * n);
            if (_openReleaseShare.Length < n)
                Array.Resize(ref _openReleaseShare, 2 * n);
            if (_openGlucose.Length < n)
                Array.Resize(ref _openGlucose, 2 * n);
            if (_cellY.Length < n)
                Array.Resize(ref _cellY, 2 * n);
            if (_vacuoleShare.Length < n)
            {
                Array.Resize(ref _vacuoleShare, 2 * n);
                Array.Resize(ref _outsideGlucose, 2 * n);
                Array.Resize(ref _packetShare, 2 * n);
                Array.Resize(ref _fieldGlucose, 2 * n);
                Array.Resize(ref _fieldRelease, 2 * n);
            }
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
            LoadCreature();   // the food it holds weighs on it
            CurrentAt(_body.Body);   // slower in the seabed's boundary layer and behind rocks
            _body.Step(dt, fx, fy, torque);
            TouchingSeabed = Seabed.Collide(_body.Body, _cells.Count, _cellAt!, CellRadius);   // it rests against what it ran into

            var b = _body.Body;
            _food.DriftBodies(Seabed, _freeVx, _freeVy);   // dead bodies drift with the tide around them
            _food.Step(dt, b.X, b.Y, b.GroundVx, b.GroundVy, b.Angle);
            _prey.Step(dt, b.X, b.Y, _freeVx, _freeVy);   // prey swim in the free current
            _food.KeepOutOf(Seabed);
            _prey.KeepOutOf(Seabed);
            FreezeAndThaw();
            MoveOrganisms();
            _cos = Math.Cos(b.Angle);
            _sin = Math.Sin(b.Angle);
            EngulfAndDigest(dt);
            EngulfAndDigestOrganisms(dt);
            BeforeCells(dt);
            _cellPass ??= CellPass;
            ForEachCell(_cellPass);
            StepOrganismCells();

            // Food stays with the cell that found it; the cells' exchange goes into the field, what they took through the
            // membrane facing packets comes out of those packets, and the field steps.
            for (int i = 0; i < n; i++)
                if (_touching[i] && _cells[i].Alive)
                    _food.StickTo(_cellX[i], _cellY[i], CellRadius);
            OrganismsStick();
            // the space between cells first (its open gaps exchange with the field before the field steps)
            if (_gaps != null && _space != null)
                StepSpace(dt);
            AfterCells(dt);
            TakeUpFromVacuoles(dt);
            OrganismsEat(dt);
            bool hadFood = NoticeFoodGap(dt);
            if (Active(GameFeatures.AutoDivide) && Active(GameFeatures.GrowthAndDivision))
                for (int i = 0; i < _cells.Count; i++)
                    if (_cells[i].Alive && _cells[i].GrowthProgress >= 1.0 && Room(_cellGroup[i]))
                        DivideCell(i);
            ApplyFixedSize();
            OrganismLives();
            UpdateMeters(dt);
            NoticeAchievementsAndSights(hadFood);
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
            for (int i = start; i < end; i++)
            {
                _body.WorldPosition(i, _cos, _sin, out double x, out double y);
                _cellX[i] = x;
                _cellY[i] = y;
                _touching[i] = _food.TouchesAny(x, y, CellRadius);
                var c = _cells[i];
                _openGlucoseShare[i] = _openReleaseShare[i] = 1.0;
                if (_gaps != null && _space != null)
                    SetMembraneFromColony(i, c, x, y);
                else
                    ReadEnvironment(i, x, y);
                AddVacuoleToMembrane(i, c);
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

        public double Read(int cell, int channel) => ReadChannel(_cells[Check(cell)], channel);

        private static double ReadChannel(FermenterCell c, int channel)
        {
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

        public bool FeatureUnlocked(int feature)
        {
            var info = GameFeatures.All[feature];
            if (info.UnlockLevel > _level || (info.Cost > 0 && !_bought[feature]) || (info.RequiresFoodGap > 0.0 && !FoodGapMet))
                return false;
            foreach (var other in info.Requires)
                if (!FeatureUnlocked(GameFeatures.IndexOf(other)))
                    return false;
            return true;
        }

        /// <summary>
        /// Glucose the creature has eaten since the start [mol]: from packets, vacuoles, and its field uptake's share above
        /// the open water's glucose. A statistic: points come from surviving (FermenterWorld.Scoring.cs) and reproducing.
        /// </summary>
        public double FoodEaten { get; private set; }

        /// <summary>Skill points to spend: the finished levels' survival and damage points and the reproduction points, less
        /// those spent.</summary>
        public int SkillPoints => SurvivalPoints + DamagePoints + ReproductionPoints + _pointsGranted - _pointsSpent;

        /// <summary>
        /// Points earned by reproducing: content skills.points_per_offspring for each daughter released in the creature's
        /// lineage (<see cref="Offspring"/>: its own daughters without multicellularity, and the daughters of free
        /// organisms, any generation). Daughters that stay in a colony earn none.
        /// </summary>
        public int ReproductionPoints => (int)Math.Floor(Offspring * _pointsPerOffspring);

        private readonly double _pointsPerOffspring = GameParameters.Default.Value("skills.points_per_offspring");

        private int _pointsGranted;

        /// <summary>
        /// Development aid: ends the level now, as if its time had run out (its meters convert to points and the next level
        /// begins). Not for play.
        /// </summary>
        public void DevEndLevel()
        {
            if (!GameTimeline.Default.HasLevel(_level + 1))
                throw new InvalidOperationException($"level {_level} is the last");
            ScoreLevel();
            Level = _level + 1;
        }

        /// <summary>Development aid: adds skill points (to try features quickly; tests). Not for play.</summary>
        public void DevGrantPoints(int points)
        {
            if (points < 0)
                throw new ArgumentOutOfRangeException(nameof(points), "points must be >= 0");
            _pointsGranted += points;
        }

        /// <summary>
        /// Why the feature cannot be bought now, or null if it can: it has no cost or is bought, its level is not reached,
        /// a feature it requires is not unlocked, too few prey have been swallowed, a feature it excludes was bought, or
        /// there are too few points.
        /// </summary>
        public string? WhyNotForSale(int feature)
        {
            var info = GameFeatures.All[feature];
            if (info.Cost == 0 || _bought[feature])
                return $"{info.Name} is not for sale";
            if (info.UnlockLevel > _level)
                return $"{info.Name} can be bought from level {info.UnlockLevel}";
            foreach (var other in info.Excludes)
                if (_bought[GameFeatures.IndexOf(other)])
                    return $"{info.Name} is closed: {GameFeatures.All[GameFeatures.IndexOf(other)].Name} was chosen";
            foreach (var other in info.Requires)
                if (!FeatureUnlocked(GameFeatures.IndexOf(other)))
                    return $"{info.Name} needs {GameFeatures.All[GameFeatures.IndexOf(other)].Name}";
            if (Swallowed < info.RequiresSwallowed)
                return $"{info.Name} needs {info.RequiresSwallowed} prey swallowed; {Swallowed} so far";
            if (SkillPoints < info.Cost)
                return $"{info.Name} costs {info.Cost} points; there are {SkillPoints}";
            return null;
        }

        public void BuyFeature(int feature)
        {
            if (feature < 0 || feature >= _bought.Length)
                throw new ArgumentOutOfRangeException(nameof(feature), $"no feature {feature}");
            var info = GameFeatures.All[feature];
            string? why = WhyNotForSale(feature);
            if (why != null)
                throw new InvalidOperationException(why);
            _pointsSpent += info.Cost;
            _bought[feature] = true;
            ApplyControls();
        }

        public bool FeatureEnabled(int feature) => _featureEnabled[feature];

        public void SetTrait(int group, int trait, double value)
        {
            CheckGroup(group);
            if (trait < 0 || trait >= _traits.Length)
                throw new ArgumentOutOfRangeException(nameof(trait), $"no trait {trait}");
            string? feature = _traits[trait].Feature;
            if (feature != null && !FeatureUnlocked(GameFeatures.IndexOf(feature)))
                throw new InvalidOperationException($"{_traits[trait].Label} needs {GameFeatures.All[GameFeatures.IndexOf(feature)].Name}");
            for (int i = 0; i < _cells.Count; i++)
                if (_cellGroup[i] == group)
                    _cells[i].SetTrait(trait, value);
            // The group's target is what its cells accepted (the membrane ceiling may clamp it).
            int first = _cellGroup.IndexOf(group);
            _groups[group].Targets[trait] = first >= 0 ? _cells[first].TraitTarget(trait) : value;
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
                throw new InvalidOperationException(info.UnlockLevel > _level ? $"{info.Name} unlocks at level {info.UnlockLevel}"
                    : $"{info.Name} is not evolved yet");
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
            _divisions++;
            if (!Active(GameFeatures.Multicellularity))
            {
                // without multicellularity the daughter swims off on its own (FermenterWorld.Organisms.cs)
                Offspring++;
                OwnOffspring++;
                ReleaseFromCreature(cell, daughter);
                return;
            }
            int group = _cellGroup[cell];
            // With a fixed body size the daughter takes a dead cell's place; otherwise it joins the colony.
            int slot = Active(GameFeatures.FixedBodySize) ? DeadSlot(group) : -1;
            var angles = _body.FlagellumAngles(cell);   // the daughter inherits where the flagella sit
            if (slot >= 0)
            {
                _cells[slot] = daughter;
                Egested += _vacuoles[slot]?.Glucose ?? 0.0;   // what the dead cell held goes with it
                _vacuoles[slot] = null;
                _body.SetFlagellumAngles(slot, angles);
            }
            else
            {
                _cells.Add(daughter);
                _vacuoles.Add(null);
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
            bool matrixOn = Active(GameFeatures.ExtracellularMatrix), regulationOn = Active(GameFeatures.Regulation);
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
                c.MatrixAllocation = matrixOn ? g.Controls[MatrixControl] : 0.0;
                ApplyRegulation(c, g, regulationOn);
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
                case "Hydrolase": return "Surface hydrolase";
                case "PTS": return "PTS glucose carrier";
                case "ABC": return "ABC glucose importer";
                case "GalP": return "H⁺ glucose symporter";
                case "Flagellum": return "Flagella";
                case "Glyc": return "Glycolytic enzymes";
                default: return "Other proteins (ribosomal, nucleoid, enzymes)";
            }
        }
    }
}
