using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Level 4: the space between the cells and the boundary and inner groups (docs/plans/level4-extracellular-space.md).
    /// The water between each three neighbouring cells is a volume (<see cref="ColonyGaps"/>), an instance of the upstream
    /// extracellular_volume module in <see cref="ExtracellularSpace"/>. Each 60° sector of a cell's edge faces either a
    /// volume or open water: the cell sees the mean of what its sectors face, and its exchange (glucose uptake, lactate
    /// and acid released, matrix exported) is shared out over its sectors the same way; what goes to open water leaves
    /// with the sea, and what a cell takes from open water beyond the background comes out of the food packets it
    /// touches, as before level 4. Volumes on the colony's edge open to the water outside through the gaps there.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        /// <summary>The automatic groups from level 4: cells with part of their edge on open water, and the rest.</summary>
        public const int BoundaryGroup = 0, InnerGroup = 1;

        private const string FaceModuleType = "extracellular_face";

        private ColonyGaps? _gaps;
        private ExtracellularSpace? _space;
        private readonly Dictionary<(int, int, int), int> _volumeOf = new Dictionary<(int, int, int), int>();
        private int[] _sectorSpace = new int[0];          // cell * 6 + sector -> volume in _space, or -1 for open water
        private double[] _openGlucoseShare = new double[0];   // per cell: share of its glucose seen from open water
        private readonly List<(int CellA, int CellB)> _openFaceCells = new List<(int, int)>();
        private double[] _mouthX = new double[0], _mouthY = new double[0];

        /// <summary>The colony's gaps and volumes (null before level 4).</summary>
        public ColonyGaps? Gaps => _gaps;

        /// <summary>The space between the cells (null before level 4 or before the colony has any).</summary>
        public ExtracellularSpace? Space => _space;

        /// <summary>True if part of the cell's edge faces open water (every cell before level 4).</summary>
        public bool IsBoundaryCell(int cell) => _gaps == null || _gaps.IsBoundary(Check(cell));

        /// <summary>Share of the cell's edge on open water (1 before level 4).</summary>
        public double OpenShare(int cell) => _gaps == null ? 1.0 : _gaps.OpenShare(Check(cell));

        /// <summary>Volume of the space between cells sector <paramref name="sector"/> (0-5) of the cell faces, or -1.</summary>
        public int SectorVolume(int cell, int sector) => _gaps == null ? -1 : _sectorSpace[6 * Check(cell) + sector];

        // Keeps the gaps, the volumes and the groups in step with the colony (new cells close new gaps).
        private void UpdateColony()
        {
            if (!Active(GameFeatures.BoundaryInnerGroups))
            {
                _gaps = null;
                return;
            }
            int n = _cells.Count;
            if (_gaps != null && _gaps.Slots == n)
                return;
            _gaps = new ColonyGaps(n, _spacing);
            double depth = _cells[0].External("h_cell_mod_membrane") * 1e-6;   // µm -> m
            double gap = _spacing - 2.0 * CellRadius;
            double area = gap * depth, half = ColonyGaps.CentreToGap(_spacing);
            if (_space == null)
            {
                _space = new ExtracellularSpace();
                var face = NativeModule.Get(FaceModuleType, ExtracellularSpace.ModuleVersion).Descriptor.Instances["default"];
                for (int s = 0; s < ExtracellularSpace.Species.Length; s++)
                    _space.Diffusivity[s] = face["D_" + ExtracellularSpace.Species[s]];
            }
            var water = new double[ExtracellularSpace.Species.Length];
            for (int s = 0; s < water.Length; s++)
                water[s] = _space.OpenWater[s];
            water[ExtracellularSpace.Glucose] = _food.BackgroundGlucose;
            var index = new int[_gaps.Volumes.Count];
            for (int v = 0; v < index.Length; v++)
            {
                var vol = _gaps.Volumes[v];
                var key = (vol.A, vol.B, vol.C);
                if (!_volumeOf.TryGetValue(key, out int at))
                {
                    at = _space.AddVolume(ColonyGaps.VolumeArea(_spacing, CellRadius) * depth, water);
                    _volumeOf[key] = at;
                }
                index[v] = at;
            }
            _sectorSpace = new int[6 * n];
            for (int i = 0; i < n; i++)
                for (int k = 0; k < 6; k++)
                {
                    int v = _gaps.SectorVolume(i, k);
                    _sectorSpace[6 * i + k] = v < 0 ? -1 : index[v];
                }
            var faces = new List<ExtracellularSpace.Face>();
            var open = new List<ExtracellularSpace.OpenFace>();
            _openFaceCells.Clear();
            foreach (var f in _gaps.Faces)
                if (f.Open)
                {
                    open.Add(new ExtracellularSpace.OpenFace(index[f.VolumeA], area, half));
                    _openFaceCells.Add((f.CellA, f.CellB));
                }
                else
                    faces.Add(new ExtracellularSpace.Face(index[f.VolumeA], index[f.VolumeB], area, half, half));
            _space.SetFaces(faces, open);
            _mouthX = new double[open.Count];
            _mouthY = new double[open.Count];
            AssignLayerGroups();
        }

        // The boundary and inner groups: the inner group starts as a copy of the boundary group's settings, and a cell
        // that changes group takes its new group's trait targets.
        private void AssignLayerGroups()
        {
            if (_gaps == null)
                return;
            if (_groups.Count < 2)
                _groups.Add(new Group(_groups[BoundaryGroup]));
            bool moved = false;
            for (int i = 0; i < _cells.Count; i++)
            {
                int g = _gaps.IsBoundary(i) ? BoundaryGroup : InnerGroup;
                if (_cellGroup[i] == g)
                    continue;
                _cellGroup[i] = g;
                moved = true;
                for (int t = 0; t < _traits.Length; t++)
                    _cells[i].SetTrait(t, _groups[g].Targets[t]);
            }
            if (moved)
                ApplyControls();
        }

        // Glucose, lactate and H+ at the cell's membrane: open water on its open sectors, each volume on its own.
        private void SetMembraneFromColony(int i, FermenterCell c, double foodGlucose)
        {
            var space = _space!;
            double w = _gaps!.OpenShare(i);
            double glc = w * foodGlucose, lac = w * space.OpenWater[ExtracellularSpace.Lactate];
            double h = w * space.OpenWater[ExtracellularSpace.Proton];
            for (int k = 0; k < 6; k++)
            {
                int v = _sectorSpace[6 * i + k];
                if (v < 0)
                    continue;
                glc += space.Concentration(v, ExtracellularSpace.Glucose) / 6.0;
                lac += space.Concentration(v, ExtracellularSpace.Lactate) / 6.0;
                h += space.Concentration(v, ExtracellularSpace.Proton) / 6.0;
            }
            // a changed input restarts the cell's integration, so values that moved by less than InputTolerance are left
            if (Moved(c.MembraneGlucose, glc))
                c.MembraneGlucose = glc;
            if (Moved(c.MembraneLactate, lac))
                c.MembraneLactate = lac;
            if (Moved(c.OutsideProton, h))
                c.OutsideProton = h;
            glc = c.MembraneGlucose;
            _openGlucoseShare[i] = glc > 0.0 ? Math.Min(1.0, w * foodGlucose / glc) : w;
        }

        /// <summary>
        /// Relative change below which the space's concentrations at a cell's membrane are not passed on (each change
        /// restarts the cell's integration; the cell's exchange is booked at the values it used, so nothing is lost).
        /// </summary>
        public double InputTolerance { get; set; } = 1e-3;

        private bool Moved(double old, double now) => Math.Abs(now - old) > InputTolerance * Math.Max(Math.Abs(old), Math.Abs(now));

        // The space's step: the cells' exchange shared over their sectors, the water outside at each open gap, then
        // what came in from packets at the gaps leaves them.
        private void StepSpace(double dt)
        {
            var space = _space!;
            int nv = space.Count;
            Array.Clear(space.UptakeCoefficient, 0, nv);
            Array.Clear(space.Source, 0, space.Source.Length);
            Array.Clear(space.MatrixIn, 0, nv);
            for (int i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (!c.Alive)
                    continue;
                double j = c.GlucoseUptake, cm = c.MembraneGlucose;
                double coefficient = j > 0.0 && cm > 0.0 ? j / cm / 6.0 : 0.0;
                double leak = j < 0.0 ? -j / 6.0 : 0.0;
                double lac = -c.LactateUptake / 6.0, h = -c.ProtonUptake / 6.0, mx = c.MatrixExport / 6.0;
                for (int k = 0; k < 6; k++)
                {
                    int v = _sectorSpace[6 * i + k];
                    if (v < 0)
                        continue;
                    space.UptakeCoefficient[v] += coefficient;
                    space.Source[ExtracellularSpace.Glucose * nv + v] += leak;
                    space.Source[ExtracellularSpace.Lactate * nv + v] += lac;
                    space.Source[ExtracellularSpace.Proton * nv + v] += h;
                    space.MatrixIn[v] += mx;
                }
            }
            int ns = ExtracellularSpace.Species.Length;
            double halfGap = 0.5 * (_spacing - 2.0 * CellRadius);
            for (int f = 0; f < _openFaceCells.Count; f++)
            {
                var (a, b) = _openFaceCells[f];
                _mouthX[f] = 0.5 * (_cellX[a] + _cellX[b]);
                _mouthY[f] = 0.5 * (_cellY[a] + _cellY[b]);
                for (int s = 0; s < ns; s++)
                    space.Outside[f * ns + s] = space.OpenWater[s];
                space.Outside[f * ns + ExtracellularSpace.Glucose] = _food.GlucoseAt(_mouthX[f], _mouthY[f], halfGap);
            }
            space.Step(dt);
            double background = _food.BackgroundGlucose;
            for (int f = 0; f < _openFaceCells.Count; f++)
            {
                double inflow = space.OpenInflow[f * ns + ExtracellularSpace.Glucose];
                double outside = space.Outside[f * ns + ExtracellularSpace.Glucose];
                if (inflow > 0.0 && outside > background && _food.TouchesAny(_mouthX[f], _mouthY[f], halfGap))
                    FoodEaten += _food.Withdraw(_mouthX[f], _mouthY[f], halfGap, inflow * (outside - background) / outside);
            }
        }
    }
}
