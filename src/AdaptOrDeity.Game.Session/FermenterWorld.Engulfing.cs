using System;
using System.Collections.Generic;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// What a cell has engulfed: one vacuole, a compartment topologically outside the cytosol, holding the items swallowed
    /// (food packets and prey cells) merged. Its polymer is digested by the cell's surface hydrolase and its free sugar is
    /// taken up by the cell's own carriers, which see the vacuole's concentration on the share of their membrane that lines
    /// it. Its lumen keeps the largest volume its items have filled (at the packets' density); hydrolysis stops while its
    /// free sugar is at the concentration a packet offers a touching membrane (product inhibition), so the carriers never
    /// see more than a packet outside would offer. SI.
    /// </summary>
    public sealed class Vacuole
    {
        /// <summary>Glucose units still in polymer [mol].</summary>
        public double Polymer { get; internal set; }

        /// <summary>Free glucose in the vacuole [mol].</summary>
        public double Free { get; internal set; }

        /// <summary>Everything it holds [mol glucose equivalents].</summary>
        public double Glucose => Polymer + Free;

        /// <summary>Volume of its lumen [m³]: the most its items have filled at the packet density.</summary>
        public double Volume { get; internal set; }

        /// <summary>Prey cells swallowed into it.</summary>
        public int Prey { get; internal set; }

        /// <summary>Where it sits in its cell, from the cell's centre in the body frame [m].</summary>
        public double LocalX { get; internal set; }

        public double LocalY { get; internal set; }

        /// <summary>Uptake per unit vacuole concentration in the last step [m³/s] (the implicit step's coefficient).</summary>
        internal double Coefficient { get; set; }

        /// <summary>Concentration passed to the cell this step [mol/m³].</summary>
        internal double Input { get; set; }
    }

    public sealed partial class FermenterWorld
    {
        private readonly PreyField _prey;
        private readonly List<Vacuole?> _vacuoles = new List<Vacuole?>();   // per cell
        private double[] _vacuoleShare = new double[0];    // per cell: share of its glucose uptake drawn from its vacuole
        private double[] _outsideGlucose = new double[0];  // per cell: glucose outside its membrane (without the vacuole)
        private readonly double _packetDensity = GameParameters.Default.PacketDensity;

        /// <summary>Prey cells around the creature.</summary>
        public PreyField Prey => _prey;

        /// <summary>Prey cells the creature's cells have engulfed since the start.</summary>
        public int Swallowed { get; private set; }

        /// <summary>Daughters that left the creature (divisions without multicellularity).</summary>
        public int Offspring { get; private set; }

        /// <summary>Glucose equivalents engulfed into vacuoles so far [mol] (packets and prey).</summary>
        public double Engulfed { get; private set; }

        /// <summary>Glucose the cells have taken up from their vacuoles so far [mol] (part of <see cref="FoodEaten"/>).</summary>
        public double EatenFromVacuoles { get; private set; }

        /// <summary>What vacuoles held when they were emptied below the used-up share, released to the water unused [mol].</summary>
        public double Egested { get; private set; }

        /// <summary>The cell's vacuole, or null.</summary>
        public Vacuole? VacuoleOf(int cell) => _vacuoles[Check(cell)];

        /// <summary>Radius of a sphere holding <paramref name="glucose"/> at the packet density [m].</summary>
        public double VacuoleRadius(double glucose) =>
            glucose > 0.0 ? Math.Pow(3.0 * glucose / (4.0 * Math.PI * _packetDensity), 1.0 / 3.0) : 0.0;

        /// <summary>Development aid: a prey cell swimming straight at the creature from a few cell widths away. Not for play.</summary>
        public void DevSpawnPrey()
        {
            var b = _body.Body;
            double angle = 2.0 * Math.PI * (Swallowed * 0.618034 % 1.0);   // a different side each time, deterministic
            double d = 4.0 * CellRadius + ColonyDiameter / 2.0;
            _prey.Add(b.X + d * Math.Cos(angle), b.Y + d * Math.Sin(angle), angle + Math.PI);
        }

        private static PreyField CreatePrey() => new PreyField(GameParameters.Default.Prey());

        // Each living cell engulfs, in cell order, the prey cells and then the food packets it touches that are smaller than
        // itself, while its vacuole stays smaller than the cell (a packet only while room for a prey cell is left beside
        // it); then every vacuole's polymer
        // is hydrolysed over the step and its concentration for the cells' carriers is predicted (implicit in the uptake).
        private void EngulfAndDigest(double dt)
        {
            bool engulfing = Active(GameFeatures.Engulfing);
            for (int i = 0; i < _cells.Count; i++)
            {
                var c = _cells[i];
                if (!c.Alive)
                    continue;
                if (engulfing)
                {
                    _body.WorldPosition(i, _cos, _sin, out double x, out double y);
                    var prey = _prey.Prey;
                    for (int k = 0; k < prey.Count; k++)   // indexed: no enumerator to allocate
                    {
                        var p = prey[k];
                        if (p.Active && p.Radius < CellRadius && PreyField.Touches(p, x, y, CellRadius)
                            && Fits(i, p.Glucose))
                        {
                            Take(i, x, y, p.X, p.Y, p.Glucose, p.Free).Prey++;
                            _prey.Remove(p);
                            Swallowed++;
                        }
                    }
                    var packets = _food.Packets;
                    for (int k = 0; k < packets.Count; k++)
                    {
                        var p = packets[k];
                        // a packet only if a prey cell would still fit beside it: prey are never shut out by food
                        if (p.Active && p.Radius < CellRadius && _food.Touches(p, x, y, CellRadius)
                            && Fits(i, p.Glucose + _prey.Content))
                        {
                            Take(i, x, y, p.X, p.Y, p.Glucose, p.Free);
                            _food.Remove(p);
                        }
                    }
                }
                var v = _vacuoles[i];
                if (v == null)
                    continue;
                // the hydrolase on the polymer, held back while free sugar is at the cap (product inhibition)
                double room = Math.Max(0.0, Settings.ContactGlucose * v.Volume - v.Free);
                double hydrolysed = Math.Min(Math.Min(v.Polymer, room), c.Hydrolysis(v.Polymer / v.Volume) * dt);
                v.Polymer -= hydrolysed;
                v.Free += hydrolysed;
                v.Input = v.Free / (v.Volume + v.Coefficient * dt);   // implicit in the uptake over the step
            }
        }

        // Whether cell i's vacuole, with glucose added, stays smaller than the cell.
        private bool Fits(int i, double glucose) =>
            VacuoleRadius((_vacuoles[i]?.Glucose ?? 0.0) + glucose) < CellRadius;

        // An item at (ix, iy) goes into cell i's vacuole (made where it came in, inside the cell).
        private Vacuole Take(int i, double x, double y, double ix, double iy, double glucose, double free)
        {
            var v = _vacuoles[i];
            if (v == null)
            {
                v = new Vacuole();
                double dx = ix - x, dy = iy - y, d = Math.Sqrt(dx * dx + dy * dy);
                double inside = d > 0.0 ? 0.3 * CellRadius / d : 0.0;
                v.LocalX = (_cos * dx + _sin * dy) * inside;   // into the body frame, a little off the cell's centre
                v.LocalY = (-_sin * dx + _cos * dy) * inside;
                _vacuoles[i] = v;
                // its inputs now change every macro step, as with the environment field: steps end there and restart softly
                _cells[i].StopAtMacroSteps = true;
            }
            v.Polymer += glucose - free;
            v.Free += free;
            v.Volume = Math.Max(v.Volume, v.Glucose / _packetDensity);
            Engulfed += glucose;
            return v;
        }

        // Cell i's membrane glucose with its vacuole: the share of its membrane lining the vacuole, (r_v / R)² as spheres,
        // sees the vacuole's concentration, the rest what is outside. The uptake splits in proportion.
        private void AddVacuoleToMembrane(int i, FermenterCell c)
        {
            _outsideGlucose[i] = c.MembraneGlucose;
            _vacuoleShare[i] = 0.0;
            var v = _vacuoles[i];
            if (v == null || !c.Alive)
                return;
            double r = VacuoleRadius(v.Volume * _packetDensity) / CellRadius, w = r * r;   // its lumen's share
            double outside = c.MembraneGlucose, combined = (1.0 - w) * outside + w * v.Input;
            if (Moved(c.MembraneGlucose, combined))
                c.MembraneGlucose = combined;
            double m = c.MembraneGlucose;
            _vacuoleShare[i] = m > 0.0 ? Math.Min(1.0, w * v.Input / m) : 0.0;
            _openGlucoseShare[i] *= 1.0 - _vacuoleShare[i];
        }

        // Each vacuole gives the cell what its carriers took from it over the step (no more than it holds); emptied
        // vacuoles go.
        private void TakeUpFromVacuoles(double dt)
        {
            double usedUp = Settings.UsedUpFraction * _prey.Content;
            for (int i = 0; i < _cells.Count; i++)
            {
                var v = _vacuoles[i];
                var c = _cells[i];
                if (v == null || !c.Alive)
                    continue;
                double j = c.GlucoseUptake * _vacuoleShare[i];   // mol/s from the vacuole
                double taken = j > 0.0 ? Math.Min(v.Free, j * dt) : 0.0;
                v.Free -= taken;
                FoodEaten += taken;
                EatenFromVacuoles += taken;
                v.Coefficient = j > 0.0 && v.Input > 0.0 ? j / v.Input : 0.0;
                if (v.Glucose < usedUp)
                {
                    Egested += v.Glucose;
                    _vacuoles[i] = null;
                }
            }
        }

        private FoodSettings Settings => _food.Settings;
    }
}
