using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>
    /// The creature's body as one rigid body: its cells at fixed places in the body frame, each with flagella fixed at
    /// angles on its membrane. A running flagellum pushes its cell straight away from its base (thrust towards the cell
    /// centre), off by a small random angle drawn from a normal distribution for each run, so thrust that does not pass
    /// through the centre of mass turns the body. Forces come from the cell kernels (each flagellum's thrust); mass from
    /// the cells' volume and density; drag is Stokes drag of each cell in the water (translation 6 pi eta R, rotation
    /// 8 pi eta R^3 about its own centre plus 6 pi eta R r^2 for its distance r from the centre of mass), neglecting
    /// hydrodynamic interaction between cells.
    /// </summary>
    public sealed class CreatureBody
    {
        /// <summary>Most flagella a cell can carry (the trait's upper bound).</summary>
        public const int MaxFlagella = 8;

        private readonly List<double> _lx = new List<double>(), _ly = new List<double>();   // layout positions [m]
        private readonly List<double[]> _angles = new List<double[]>();                     // base angles, body frame
        private readonly List<double[]> _noise = new List<double[]>();                      // thrust angle errors, this run
        private readonly List<double> _mass = new List<double>();
        private double _cx, _cy;   // centre of mass in layout coordinates

        public CreatureBody(double cellRadius, double eta, RigidBody2D body)
        {
            if (!(cellRadius > 0.0 && eta > 0.0))
                throw new ArgumentOutOfRangeException(nameof(cellRadius), "cell radius and viscosity must be > 0");
            CellRadius = cellRadius;
            Eta = eta;
            Body = body;
        }

        public RigidBody2D Body { get; }

        public double CellRadius { get; }

        /// <summary>Viscosity of the water [Pa s].</summary>
        public double Eta { get; }

        public int CellCount => _lx.Count;

        /// <summary>Force [N] and torque [N m] of the flagella in the last step, in the world frame.</summary>
        public double ForceX { get; private set; }

        public double ForceY { get; private set; }

        public double Torque { get; private set; }

        /// <summary>Adds a cell at layout position (x, y) [m] with mass [kg] and its flagella's base angles (copied).</summary>
        public void AddCell(double x, double y, double mass, IReadOnlyList<double> flagellumAngles)
        {
            _lx.Add(x);
            _ly.Add(y);
            _mass.Add(mass);
            var a = new double[MaxFlagella];
            for (int k = 0; k < MaxFlagella; k++)
                a[k] = flagellumAngles[k];
            _angles.Add(a);
            _noise.Add(new double[MaxFlagella]);
            Recompute();
        }

        /// <summary>Default base angles: the first flagellum at the back (angle pi), the rest around it, spacing apart.</summary>
        public static double[] DefaultAngles(double spacing)
        {
            var a = new double[MaxFlagella];
            for (int k = 0; k < MaxFlagella; k++)
            {
                int side = (k + 1) / 2;
                a[k] = Math.PI + (k % 2 == 1 ? 1 : -1) * side * spacing;
            }
            return a;
        }

        /// <summary>Base angle of flagellum k of a cell, in the cell's body frame [rad] (0 = the body's +x axis).</summary>
        public double FlagellumAngle(int cell, int k) => _angles[cell][k];

        public double[] FlagellumAngles(int cell) => (double[])_angles[cell].Clone();

        public void SetFlagellumAngle(int cell, int k, double angle)
        {
            if (k < 0 || k >= MaxFlagella)
                throw new ArgumentOutOfRangeException(nameof(k), $"flagellum {k} of {MaxFlagella}");
            if (double.IsNaN(angle) || double.IsInfinity(angle))
                throw new ArgumentException("angle must be finite", nameof(angle));
            _angles[cell][k] = Math.IEEERemainder(angle, 2.0 * Math.PI);
        }

        /// <summary>Gives a cell (e.g. a daughter taking a dead cell's place) these base angles.</summary>
        public void SetFlagellumAngles(int cell, IReadOnlyList<double> angles)
        {
            for (int k = 0; k < MaxFlagella; k++)
                SetFlagellumAngle(cell, k, angles[k]);
        }

        /// <summary>A new run: every flagellum's thrust direction is off by a normal error with this standard deviation.</summary>
        public void NewRun(Random random, double sd)
        {
            foreach (var n in _noise)
                for (int k = 0; k < MaxFlagella; k++)
                    n[k] = sd * Gaussian(random);
        }

        /// <summary>Position of a cell's centre relative to the body's centre of mass, in the body's frame [m].</summary>
        public void LocalPosition(int cell, out double x, out double y)
        {
            x = _lx[cell] - _cx;
            y = _ly[cell] - _cy;
        }

        /// <summary>Position of a cell's centre relative to the body's centre of mass, in the world frame [m].</summary>
        public void RelativePosition(int cell, out double x, out double y)
        {
            double bx = _lx[cell] - _cx, by = _ly[cell] - _cy;
            double c = Math.Cos(Body.Angle), s = Math.Sin(Body.Angle);
            x = c * bx - s * by;
            y = s * bx + c * by;
        }

        /// <summary>Position of a cell's centre over the ground [m].</summary>
        public void WorldPosition(int cell, out double x, out double y) =>
            WorldPosition(cell, Math.Cos(Body.Angle), Math.Sin(Body.Angle), out x, out y);

        /// <summary>As <see cref="WorldPosition(int, out double, out double)"/>, with the body angle's cosine and sine given.</summary>
        public void WorldPosition(int cell, double cos, double sin, out double x, out double y)
        {
            double bx = _lx[cell] - _cx, by = _ly[cell] - _cy;
            x = Body.X + cos * bx - sin * by;
            y = Body.Y + sin * bx + cos * by;
        }

        /// <summary>
        /// Advances the body by dt. <paramref name="flagella"/>(cell) is the number of flagella built (a fraction counts as a
        /// part-built flagellum pushing in proportion), <paramref name="thrust"/>(cell) each flagellum's thrust [N] (0 when
        /// its motors are off).
        /// </summary>
        public void Step(double dt, Func<int, double> flagella, Func<int, double> thrust)
        {
            double fx = 0.0, fy = 0.0, torque = 0.0;   // body frame
            for (int i = 0; i < CellCount; i++)
            {
                CellForce(i, flagella(i), thrust(i), out double gx, out double gy, out double t);
                fx += gx;
                fy += gy;
                torque += t;
            }
            Step(dt, fx, fy, torque);
        }

        /// <summary>
        /// The force and torque about the centre of mass (body frame) of one cell's flagella: <paramref name="flagella"/>
        /// built, each pushing with <paramref name="thrust"/> [N]. Cells are independent, so this can run in parallel.
        /// </summary>
        public void CellForce(int cell, double flagella, double thrust, out double fx, out double fy, out double torque)
        {
            fx = fy = torque = 0.0;
            if (thrust == 0.0)
                return;
            double px = _lx[cell] - _cx, py = _ly[cell] - _cy;
            for (int k = 0; k < MaxFlagella && k < flagella; k++)
            {
                double share = Math.Min(1.0, flagella - k);
                double phi = _angles[cell][k];
                double ax = px + CellRadius * Math.Cos(phi), ay = py + CellRadius * Math.Sin(phi);   // the base
                double dir = phi + _noise[cell][k];
                double gx = -thrust * share * Math.Cos(dir), gy = -thrust * share * Math.Sin(dir);   // away from it
                fx += gx;
                fy += gy;
                torque += ax * gy - ay * gx;
            }
        }

        /// <summary>Advances the body by dt under the cells' summed force and torque (body frame).</summary>
        public void Step(double dt, double fx, double fy, double torque)
        {
            double c = Math.Cos(Body.Angle), s = Math.Sin(Body.Angle);
            ForceX = c * fx - s * fy;
            ForceY = s * fx + c * fy;
            Torque = torque;
            Body.Step(dt, ForceX, ForceY, torque);
        }

        private void Recompute()
        {
            double m = 0.0, cx = 0.0, cy = 0.0;
            for (int i = 0; i < CellCount; i++)
            {
                m += _mass[i];
                cx += _mass[i] * _lx[i];
                cy += _mass[i] * _ly[i];
            }
            cx /= m;
            cy /= m;
            // keep the cells where they are: move the body's reference point with its centre of mass
            double dx = cx - _cx, dy = cy - _cy;
            double c = Math.Cos(Body.Angle), s = Math.Sin(Body.Angle);
            Body.Shift(c * dx - s * dy, s * dx + c * dy);
            _cx = cx;
            _cy = cy;
            double inertia = 0.0, rotDrag = 0.0, drag = 0.0;
            double xi = 6.0 * Math.PI * Eta * CellRadius, xiRot = 8.0 * Math.PI * Eta * Math.Pow(CellRadius, 3);
            for (int i = 0; i < CellCount; i++)
            {
                double r2 = (_lx[i] - cx) * (_lx[i] - cx) + (_ly[i] - cy) * (_ly[i] - cy);
                inertia += _mass[i] * (0.5 * CellRadius * CellRadius + r2);   // a disc about its own centre, plus offset
                drag += xi;
                rotDrag += xiRot + xi * r2;
            }
            Body.SetProperties(m, inertia, drag, rotDrag);
        }

        private static double Gaussian(Random random)
        {
            double u1 = 1.0 - random.NextDouble(), u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);   // Box-Muller
        }
    }
}
