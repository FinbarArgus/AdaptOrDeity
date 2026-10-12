using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The space between the cells of a colony (level 4), from the slots of <see cref="ColonyLayout"/>: cells sit on a
    /// triangular lattice, so every lattice triangle whose three slots hold cells encloses one volume of water (the
    /// triangle less the three 60° sectors of its cells). Two volumes meet through the gap between the two cells of the
    /// link they share; a volume whose link has no volume on its other side opens to the water outside through that gap.
    /// Each cell's edge is six 60° sectors, one per lattice triangle around it: a sector faces its triangle's volume, or
    /// open water when the triangle is not a volume. A cell with any sector on open water is a boundary cell; the others
    /// are inner cells. The layout depends only on the number of slots (slots are never removed; dead cells keep theirs).
    /// </summary>
    public sealed class ColonyGaps
    {
        /// <summary>A volume of the space between cells: the three slots around it and its centre (lattice triangle centroid).</summary>
        public readonly struct Volume
        {
            public Volume(int a, int b, int c, double x, double y)
            {
                A = a;
                B = b;
                C = c;
                X = x;
                Y = y;
            }

            public int A { get; }
            public int B { get; }
            public int C { get; }

            /// <summary>Centre in the colony's frame [m].</summary>
            public double X { get; }
            public double Y { get; }
        }

        /// <summary>
        /// The gap between cells <see cref="CellA"/> and <see cref="CellB"/>: between volumes <see cref="VolumeA"/> and
        /// <see cref="VolumeB"/>, or from <see cref="VolumeA"/> to open water when <see cref="VolumeB"/> is -1. (X, Y) is
        /// the middle of the gap, in the colony's frame [m].
        /// </summary>
        public readonly struct Face
        {
            public Face(int volumeA, int volumeB, int cellA, int cellB, double x, double y)
            {
                VolumeA = volumeA;
                VolumeB = volumeB;
                CellA = cellA;
                CellB = cellB;
                X = x;
                Y = y;
            }

            public int VolumeA { get; }
            public int VolumeB { get; }
            public int CellA { get; }
            public int CellB { get; }
            public double X { get; }
            public double Y { get; }
            public bool Open => VolumeB < 0;
        }

        // lattice steps in angular order (60° apart): (a, b) is at a e1 + b e2, e1 = (1, 0), e2 = (1/2, √3/2)
        private static readonly int[] Da = { 1, 0, -1, -1, 0, 1 };
        private static readonly int[] Db = { 0, 1, 1, 0, -1, -1 };

        private readonly List<Volume> _volumes = new List<Volume>();
        private readonly List<Face> _faces = new List<Face>();
        private readonly int[] _sectorVolume;   // slot * 6 + sector -> volume, or -1 for open water
        private readonly bool[] _boundary;

        /// <param name="slots">Slots in use, 0 .. slots-1.</param>
        /// <param name="spacing">Distance between neighbouring cell centres [m].</param>
        public ColonyGaps(int slots, double spacing)
        {
            if (slots < 0)
                throw new ArgumentOutOfRangeException(nameof(slots));
            Slots = slots;
            Spacing = spacing;
            var at = new Dictionary<(int, int), int>();
            var lattice = new (int A, int B)[slots];
            for (int i = 0; i < slots; i++)
            {
                lattice[i] = Lattice(i);
                at[lattice[i]] = i;
            }
            int Slot(int a, int b) => at.TryGetValue((a, b), out int s) ? s : -1;

            // each triangle is found from its slot with the lowest index, so it is added once, in a fixed order
            var triangle = new Dictionary<(int, int, int), int>();
            _sectorVolume = new int[6 * slots];
            for (int i = 0; i < slots; i++)
                for (int k = 0; k < 6; k++)
                {
                    var (a, b) = lattice[i];
                    int p = Slot(a + Da[k], b + Db[k]), q = Slot(a + Da[(k + 1) % 6], b + Db[(k + 1) % 6]);
                    int v = -1;
                    if (p >= 0 && q >= 0)
                    {
                        var key = Key(i, p, q);
                        if (!triangle.TryGetValue(key, out v))
                        {
                            v = _volumes.Count;
                            triangle[key] = v;
                            ColonyLayout.Slot(i, spacing, out double xi, out double yi);
                            ColonyLayout.Slot(p, spacing, out double xp, out double yp);
                            ColonyLayout.Slot(q, spacing, out double xq, out double yq);
                            _volumes.Add(new Volume(key.Item1, key.Item2, key.Item3, (xi + xp + xq) / 3, (yi + yp + yq) / 3));
                        }
                    }
                    _sectorVolume[6 * i + k] = v;
                }

            // faces: each link between two cells with a volume on at least one side (sector k - 1 and sector k of cell i
            // lie either side of its link in direction k)
            _boundary = new bool[slots];
            for (int i = 0; i < slots; i++)
            {
                for (int k = 0; k < 6; k++)
                    if (_sectorVolume[6 * i + k] < 0)
                        _boundary[i] = true;
                for (int k = 0; k < 6; k++)
                {
                    var (a, b) = lattice[i];
                    int j = Slot(a + Da[k], b + Db[k]);
                    if (j <= i)
                        continue;   // each link once, from its lower slot
                    int left = _sectorVolume[6 * i + k], right = _sectorVolume[6 * i + (k + 5) % 6];
                    if (left < 0 && right < 0)
                        continue;
                    ColonyLayout.Slot(i, spacing, out double xi, out double yi);
                    ColonyLayout.Slot(j, spacing, out double xj, out double yj);
                    double mx = 0.5 * (xi + xj), my = 0.5 * (yi + yj);
                    _faces.Add(left >= 0 && right >= 0
                        ? new Face(Math.Min(left, right), Math.Max(left, right), i, j, mx, my)
                        : new Face(Math.Max(left, right), -1, i, j, mx, my));
                }
            }
        }

        public int Slots { get; }

        public double Spacing { get; }

        public IReadOnlyList<Volume> Volumes => _volumes;

        public IReadOnlyList<Face> Faces => _faces;

        /// <summary>The volume sector <paramref name="sector"/> (0-5) of the cell faces, or -1 for open water.</summary>
        public int SectorVolume(int slot, int sector) => _sectorVolume[6 * slot + sector];

        /// <summary>True if part of the cell's edge faces open water.</summary>
        public bool IsBoundary(int slot) => _boundary[slot];

        /// <summary>Share of the cell's edge on open water (0 for an inner cell, 1 for a cell with no volume around it).</summary>
        public double OpenShare(int slot)
        {
            int open = 0;
            for (int k = 0; k < 6; k++)
                if (_sectorVolume[6 * slot + k] < 0)
                    open++;
            return open / 6.0;
        }

        /// <summary>Water area of one volume [m²]: the lattice triangle less the three 60° sectors of radius <paramref name="r"/>.</summary>
        public static double VolumeArea(double spacing, double r) =>
            Math.Sqrt(3.0) / 4.0 * spacing * spacing - Math.PI * r * r / 2.0;

        /// <summary>Distance from a volume's centre to the middle of each of its gaps [m]: s / (2√3).</summary>
        public static double CentreToGap(double spacing) => spacing / (2.0 * Math.Sqrt(3.0));

        /// <summary>Lattice coordinates of a slot of <see cref="ColonyLayout"/>'s hexagonal spiral.</summary>
        public static (int A, int B) Lattice(int index)
        {
            if (index == 0)
                return (0, 0);
            int ring = 1, start = 1;
            while (index >= start + 6 * ring)
            {
                start += 6 * ring;
                ring++;
            }
            int k = index - start, side = k / ring, step = k % ring;
            int next = (side + 1) % 6;
            return (ring * Da[side] + step * (Da[next] - Da[side]), ring * Db[side] + step * (Db[next] - Db[side]));
        }

        private static (int, int, int) Key(int a, int b, int c)
        {
            if (a > b) (a, b) = (b, a);
            if (b > c) (b, c) = (c, b);
            if (a > b) (a, b) = (b, a);
            return (a, b, c);
        }
    }
}
