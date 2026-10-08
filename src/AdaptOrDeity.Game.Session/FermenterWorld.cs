using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The stage-1 world: one fermenter cell (circulatory-autogen-modules cell/prokaryote fermenter) in a Proterozoic sea.
    /// The cell panel shows its metabolic rate and energy budget; its transport proteins' expression and kinetic
    /// midpoints are adaptable traits. Values exchanged here are SI.
    /// </summary>
    public sealed class FermenterWorld : ISimWorld, IInspectableWorld, IBathWorld
    {
        /// <summary>Sim-seconds per real second: pause, 1x, 10x, 100x.</summary>
        public static readonly double[] SpeedPresets = { 0.0, 1.0, 10.0, 100.0 };

        private const double Avogadro = FermenterCell.AvogadroConstant;
        private const double CellDensity = 1100.0;   // kg/m³, for the specific metabolic rate

        private static readonly OverlayInfo[] OverlayList =
        {
            new OverlayInfo("v", "Membrane potential", "F1", "V", "mV", 1e3, -140.0, -60.0),
            new OverlayInfo("ph", "Cytosol pH", "F2", "pH", "pH", 1.0, 7.0, 8.2),
            new OverlayInfo("atp", "ATP", "F3", "mol/m³", "mM", 1.0, 0.0, 12.0),
            new OverlayInfo("na", "Na⁺ inside", "F4", "mol/m³", "mM", 1.0, 0.0, 120.0),
        };

        private readonly FermenterCell _cell;
        private readonly DetailInfo[] _details;
        private readonly Func<double>[] _read;
        private readonly TraitInfo[] _traits;

        public FermenterWorld()
        {
            _cell = new FermenterCell(new Prokaryote_fermenter());
            var details = new List<DetailInfo>();
            var read = new List<Func<double>>();
            void Add(DetailInfo info, Func<double> f)
            {
                details.Add(info);
                read.Add(f);
            }
            const string rate = "Metabolic rate", made = "ATP made", used = "ATP used", upkeep = "Protein turnover",
                state = "Cell state";
            Add(new DetailInfo("atp_turnover", rate, "ATP turnover", "mol/s", "ATP/s", Avogadro,
                "ATP hydrolysed per second by everything in the cell"), () => _cell.Budget().AtpConsumed);
            Add(new DetailInfo("heat", rate, "Heat", "W", "fW", 1e15, "reaction enthalpy released"), () => _cell.Budget().Heat);
            Add(new DetailInfo("specific_heat", rate, "Specific metabolic rate", "W/kg", "W/kg", 1.0,
                "heat per kg of cell (density 1100 kg/m³)"), () => _cell.Budget().Heat / (Volume * CellDensity));
            Add(new DetailInfo("glucose_in", rate, "Glucose taken up", "mol/s", "molecules/s", Avogadro),
                () => _cell.AlgebraicValue("membrane_face__J_glc"));
            Add(new DetailInfo("lactate_out", rate, "Lactate exported", "mol/s", "molecules/s", Avogadro),
                () => -_cell.AlgebraicValue("membrane_face__J_lac"));
            Add(new DetailInfo("atp_glycolysis", made, "Glycolysis (2 per glucose)", "mol/s", "ATP/s", Avogadro),
                () => _cell.Budget().AtpProduced);
            Add(new DetailInfo("atp_pumps", used, "H⁺-ATPase pumping", "mol/s", "ATP/s", Avogadro,
                "powers the proton gradient that drives NhaA, Trk and KefC"), () => _cell.Budget().AtpPumps);
            Add(new DetailInfo("atp_protein_turnover", used, "Protein turnover", "mol/s", "ATP/s", Avogadro,
                "remaking degraded protein at the ribosomes (1%/h)"), () => _cell.Budget().AtpProteinTurnover);
            Add(new DetailInfo("atp_protein_build", used, "Building new protein", "mol/s", "ATP/s", Avogadro,
                "while expression rises"), () => _cell.Budget().AtpProteinBuild);
            Add(new DetailInfo("atp_other", used, "Other maintenance", "mol/s", "ATP/s", Avogadro,
                "DNA, RNA and lipid upkeep and everything not yet modelled"), () => _cell.Budget().AtpOtherMaintenance);
            Add(new DetailInfo("atp_net", used, "Net (made - used)", "mol/s", "ATP/s", Avogadro),
                () => _cell.Budget().AtpProduced - _cell.Budget().AtpConsumed);
            foreach (var key in new[] { "H_ATPase", "NhaA", "KefC", "Trk", "NCX", "Glf", "LacT", "proteome" })
            {
                string k = key;
                Add(new DetailInfo("turnover_" + k, upkeep, ProteinLabel(k), "mol/s", "ATP/s", Avogadro),
                    () => _cell.AlgebraicValue("protein__J_ATP_" + k));
            }
            Add(new DetailInfo("v", state, "Membrane potential", "V", "mV", 1e3), () => 1e-3 * _cell.State("membrane__V"));
            Add(new DetailInfo("ph", state, "Cytosol pH", "pH", "", 1.0), () => _cell.AlgebraicValue("H__pH_i"));
            Add(new DetailInfo("atp", state, "ATP", "mol/m³", "mM", 1.0), () => _cell.State("cytosol__C_ATP"));
            Add(new DetailInfo("atp_free_energy", state, "ATP free energy", "J/mol", "kJ/mol", 1e-3),
                () => _cell.AlgebraicValue("cytosol__A_hyd"));
            Add(new DetailInfo("na", state, "Na⁺", "mol/m³", "mM", 1.0), () => _cell.State("Na_K__Nai"));
            Add(new DetailInfo("k", state, "K⁺", "mol/m³", "mM", 1.0), () => _cell.State("Na_K__Ki"));
            Add(new DetailInfo("ca", state, "Ca²⁺ (free)", "mol/m³", "µM", 1e3), () => _cell.State("Ca__Cai"));
            _details = details.ToArray();
            _read = read.ToArray();

            _traits = new TraitInfo[_cell.Traits.Count];
            for (int i = 0; i < _traits.Length; i++)
            {
                var t = _cell.Traits[i];
                _traits[i] = new TraitInfo(t.Id, t.Label, t.Protein, t.Kind == TraitKind.Expression, t.Min, t.Max, t.LogScale);
            }
        }

        public IReadOnlyList<OverlayInfo> Overlays => OverlayList;

        public int CellCount => 1;

        /// <summary>Backward Euler macro step [s]: stable for the stiff cell, fine for its slow (s-h) dynamics.</summary>
        public double MacroDt => 0.1;

        public IReadOnlyList<DetailInfo> Details => _details;

        public IReadOnlyList<TraitInfo> Traits => _traits;

        public FermenterCell Cell => _cell;

        /// <summary>Medium glucose [mol/m³].</summary>
        public double BathConcentration
        {
            get => _cell.External("C_glc_e_mod_membrane_face");
            set
            {
                if (!(value >= 0.0))
                    throw new ArgumentOutOfRangeException(nameof(value), "must be >= 0 mol/m³");
                _cell.SetExternal("C_glc_e_mod_membrane_face", value);
            }
        }

        private double Volume => _cell.AlgebraicValue("membrane__Vol");

        public void Step(double t, double dt) => _cell.Step(dt);

        public double Read(int cell, int channel)
        {
            Check(cell);
            switch (channel)
            {
                case 0: return 1e-3 * _cell.State("membrane__V");
                case 1: return _cell.AlgebraicValue("H__pH_i");
                case 2: return _cell.State("cytosol__C_ATP");
                case 3: return _cell.State("Na_K__Nai");
                default: throw new ArgumentOutOfRangeException(nameof(channel));
            }
        }

        public double ReadDetail(int cell, int detail)
        {
            Check(cell);
            return _read[detail]();
        }

        public double ReadTrait(int cell, int trait)
        {
            Check(cell);
            return _cell.TraitValue(trait);
        }

        public double ReadTraitTarget(int cell, int trait)
        {
            Check(cell);
            return _cell.TraitTarget(trait);
        }

        public void SetTrait(int cell, int trait, double multiplier)
        {
            Check(cell);
            if (trait < 0 || trait >= _traits.Length)
                throw new ArgumentOutOfRangeException(nameof(trait));
            _cell.SetTrait(trait, multiplier);
        }

        private static void Check(int cell)
        {
            if (cell != 0)
                throw new ArgumentOutOfRangeException(nameof(cell));
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
                default: return "Rest of the proteome";
            }
        }
    }
}
