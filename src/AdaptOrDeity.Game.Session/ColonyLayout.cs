using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>Where cell slots sit in the sheet: a hexagonal spiral around the origin (shared by the sim and the views).</summary>
    public static class ColonyLayout
    {
        /// <summary>Centre-to-centre spacing of neighbouring cells [m] (2 µm cells, slightly apart).</summary>
        public const double Spacing = 2.3e-6;

        /// <summary>Position of slot <paramref name="index"/> [m].</summary>
        public static void Slot(int index, out double x, out double y)
        {
            x = y = 0.0;
            if (index == 0)
                return;
            int ring = 1, start = 1;
            while (index >= start + 6 * ring)
            {
                start += 6 * ring;
                ring++;
            }
            int k = index - start, side = k / ring, step = k % ring;
            double a0 = side * Math.PI / 3, a1 = (side + 1) * Math.PI / 3, f = (double)step / ring;
            double ax = Math.Cos(a0) * ring, ay = Math.Sin(a0) * ring, bx = Math.Cos(a1) * ring, by = Math.Sin(a1) * ring;
            x = (ax + (bx - ax) * f) * Spacing;
            y = (ay + (by - ay) * f) * Spacing;
        }
    }
}
