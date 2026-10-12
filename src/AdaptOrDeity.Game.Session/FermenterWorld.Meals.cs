using System;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Meals have mass: food a body holds weighs on its motion (<see cref="CreatureBody.SetLoad"/>). A packet or dead body
    /// stuck to it is a sphere of its radius at the cells' density (body.cell_density, ~1.1 g/mL) with the Stokes drag of
    /// that sphere, 6 pi eta r, where it sits (6 pi eta r d² + 8 pi eta r³ against turning, m d² of inertia); what a cell has
    /// engulfed is the mass of its contents' volume at the packet density, inside the cell (no drag of its own). In Stokes
    /// flow the speed is the flagella's force over the drag, so a held fresh packet (0.8 µm against the 1 µm cell) slows a
    /// one-cell body to 1/1.8 of its speed; eating it restores the speed as it shrinks. Hydrodynamic interaction between
    /// the meal and the cells is neglected, as between cells.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        // The creature's load: the packets it holds and its cells' vacuoles.
        private void LoadCreature()
        {
            double mass = 0.0, inertia = 0.0, drag = 0.0, rotDrag = 0.0;
            AddHeld(0, ref mass, ref inertia, ref drag, ref rotDrag);
            for (int i = 0; i < _cells.Count; i++)
            {
                var v = _vacuoles[i];
                if (v == null || !_cells[i].Alive)
                    continue;
                _body.LocalPosition(i, out double x, out double y);
                AddEngulfed(v, x, y, ref mass, ref inertia);
            }
            _body.SetLoad(mass, inertia, drag, rotDrag);
        }

        // A free organism's load: the packets it holds and its vacuole (its one cell at the body's centre).
        private void LoadOrganism(FreeOrganism o)
        {
            double mass = 0.0, inertia = 0.0, drag = 0.0, rotDrag = 0.0;
            AddHeld(o.Id, ref mass, ref inertia, ref drag, ref rotDrag);
            if (o.Vacuole != null)
                AddEngulfed(o.Vacuole, 0.0, 0.0, ref mass, ref inertia);
            o.Body.SetLoad(mass, inertia, drag, rotDrag);
        }

        // The packets holder holds, each a sphere at the cells' density with its own Stokes drag, at its place on the body.
        private void AddHeld(int holder, ref double mass, ref double inertia, ref double drag, ref double rotDrag)
        {
            double eta = _body.Eta;
            var packets = _food.Packets;
            for (int k = 0; k < packets.Count; k++)
            {
                var p = packets[k];
                if (!p.Active || !p.Stuck || p.Holder != holder)
                    continue;
                double r = p.Radius, m = _cellDensity * FoodField.Volume(p), xi = 6.0 * Math.PI * eta * r;
                double d2 = p.LocalX * p.LocalX + p.LocalY * p.LocalY;
                mass += m;
                inertia += m * (0.4 * r * r + d2);   // a sphere about its centre, plus its offset
                drag += xi;
                rotDrag += 8.0 * Math.PI * eta * r * r * r + xi * d2;
            }
        }

        // What a cell has engulfed: its contents' volume at the packet density, at the cells' density, where the cell sits.
        private void AddEngulfed(Vacuole v, double x, double y, ref double mass, ref double inertia)
        {
            double m = _cellDensity * v.Glucose / _packetDensity;
            mass += m;
            inertia += m * (x * x + y * y);
        }
    }
}
