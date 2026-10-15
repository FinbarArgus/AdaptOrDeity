using System;
using System.Collections.Generic;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>What the creature saw of one tile of the world when it last had the tile in view (the minimap's memory).</summary>
    public readonly struct ExploredTile
    {
        public ExploredTile(float glucose, float lactate, float ph, double seenAt, bool solid)
        {
            Glucose = glucose;
            Lactate = lactate;
            Ph = ph;
            SeenAt = seenAt;
            Solid = solid;
        }

        /// <summary>Glucose and lactate in the water there [mol/m³] (NaN for a solid tile).</summary>
        public float Glucose { get; }

        public float Lactate { get; }

        /// <summary>The water's pH there (NaN for a solid tile).</summary>
        public float Ph { get; }

        /// <summary>Simulated time it was last seen [s].</summary>
        public double SeenAt { get; }

        /// <summary>Rock or ground: its centre is below the seabed's ground line or inside a rock.</summary>
        public bool Solid { get; }
    }

    public sealed partial class FermenterWorld
    {
        private readonly double _tileSize = GameParameters.Default.Value("minimap.tile_size");
        private readonly double _exploreInterval = GameParameters.Default.Value("minimap.sample_interval");
        private readonly Dictionary<(long, long), ExploredTile> _explored = new Dictionary<(long, long), ExploredTile>();
        private readonly List<Rock> _exploreRocks = new List<Rock>();
        private double _nextExplore;

        /// <summary>Side of a minimap tile [m] (content minimap.tile_size).</summary>
        public double TileSize => _tileSize;

        /// <summary>Tiles the creature has seen in this game.</summary>
        public int ExploredTileCount => _explored.Count;

        /// <summary>The tile holding a world point: tile (i, j) spans [i, i + 1) x [j, j + 1) tile sizes.</summary>
        public (long I, long J) TileOf(double x, double y) => ((long)Math.Floor(x / _tileSize), (long)Math.Floor(y / _tileSize));

        /// <summary>What the creature saw of tile (i, j); false if it has never had it in view.</summary>
        public bool TryGetExplored(long i, long j, out ExploredTile tile) => _explored.TryGetValue((i, j), out tile);

        // Every minimap.sample_interval: the tiles whose centres are in the creature's view (the food window around its
        // body) are explored, with the water seen there now.
        private void Explore()
        {
            if (_time < _nextExplore)
                return;
            _nextExplore = _time + _exploreInterval;
            var b = _body.Body;
            double half = _food.HalfSize;
            var (i0, j0) = TileOf(b.X - half, b.Y - half);
            var (i1, j1) = TileOf(b.X + half, b.Y + half);
            _exploreRocks.Clear();
            Seabed.RocksBetween(b.X - half - _tileSize, b.X + half + _tileSize, _exploreRocks);
            var env = _environment;
            for (long j = j0; j <= j1; j++)
                for (long i = i0; i <= i1; i++)
                {
                    double x = (i + 0.5) * _tileSize, y = (j + 0.5) * _tileSize;
                    if (Math.Abs(x - b.X) > half || Math.Abs(y - b.Y) > half)
                        continue;
                    ExploredTile tile;
                    if (Solid(x, y))
                        tile = new ExploredTile(float.NaN, float.NaN, float.NaN, _time, true);
                    else
                        tile = new ExploredTile((float)env.Field.Sample(EnvironmentField.Glucose, x, y),
                            (float)env.Field.Sample(EnvironmentField.Lactate, x, y),
                            (float)(-Math.Log10(1e-3 * env.Protons(env.Field.Sample(EnvironmentField.Acid, x, y)))),   // mol/m³ to pH
                            _time, false);
                    _explored[(i, j)] = tile;
                }
        }

        private bool Solid(double x, double y)
        {
            if (y < Seabed.GroundY)
                return true;
            foreach (var r in _exploreRocks)
                if ((x - r.X) * (x - r.X) + (y - r.Y) * (y - r.Y) < r.Radius * r.Radius)
                    return true;
            return false;
        }
    }
}
