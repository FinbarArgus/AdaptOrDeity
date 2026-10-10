using System.Collections.Generic;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The ground and water the creature moves through, drawn so its motion is obvious. The view is centred on the
    /// creature (a floating origin), so the creature stays still on screen and the background slides past: specks fixed to
    /// the ground (parallax 1, sediment) and fainter particles further down (parallax below 1, deeper sediment). Particles
    /// are generated per tile from a hash of the tile's coordinates, so the same place always looks the same.
    /// </summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        private sealed class Layer
        {
            public float Parallax;      // 1 = fixed to the ground under the creature
            public float Tile;          // world units
            public int PerTile;
            public float Size;          // world units
            public Color Colour;
            public int Order;
            public readonly List<SpriteRenderer> Pool = new List<SpriteRenderer>();
        }

        private const int MaxPerLayer = 2500;
        private SessionBridge _bridge;
        private Camera _camera;
        private Layer[] _layers;

        public void Initialise(SessionBridge bridge, Camera camera)
        {
            _bridge = bridge;
            _camera = camera;
            _layers = new[]
            {
                new Layer { Parallax = 0.45f, Tile = 0.4f, PerTile = 5, Size = 0.025f, Colour = new Color(0.55f, 0.70f, 0.78f, 0.25f), Order = -20 },
                new Layer { Parallax = 1.00f, Tile = 0.5f, PerTile = 4, Size = 0.045f, Colour = new Color(0.80f, 0.86f, 0.80f, 0.45f), Order = -10 },
            };
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null || _camera == null)
                return;
            double cx = _bridge.DisplayX, cy = _bridge.DisplayY;   // smooth between snapshots
            float halfH = _camera.orthographicSize, halfW = halfH * Mathf.Max(0.1f, _camera.aspect);
            foreach (var layer in _layers)
                Draw(layer, cx * layer.Parallax, cy * layer.Parallax, halfW, halfH);
        }

        private void Draw(Layer layer, double lx, double ly, float halfW, float halfH)
        {
            long i0 = (long)System.Math.Floor((lx - halfW) / layer.Tile), i1 = (long)System.Math.Floor((lx + halfW) / layer.Tile);
            long j0 = (long)System.Math.Floor((ly - halfH) / layer.Tile), j1 = (long)System.Math.Floor((ly + halfH) / layer.Tile);
            int used = 0;
            for (long i = i0; i <= i1; i++)
                for (long j = j0; j <= j1; j++)
                    for (int k = 0; k < layer.PerTile && used < MaxPerLayer; k++)
                    {
                        uint h = Hash(i, j, k, layer.Order);
                        double px = (i + Unit(h)) * layer.Tile, py = (j + Unit(h * 2654435761u + 1u)) * layer.Tile;
                        var r = Get(layer, used++);
                        // relative to the creature, which sits at the origin
                        r.transform.localPosition = new Vector3((float)(px - lx), (float)(py - ly), 1f);
                        float s = layer.Size * (0.6f + 0.8f * Unit(h ^ 0x9E3779B9u));
                        r.transform.localScale = new Vector3(s, s, 1f);
                        r.enabled = true;
                    }
            for (int k = used; k < layer.Pool.Count; k++)
                layer.Pool[k].enabled = false;
        }

        private SpriteRenderer Get(Layer layer, int index)
        {
            while (layer.Pool.Count <= index)
            {
                var go = new GameObject("Speck");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = CellView.CircleSprite();
                r.color = layer.Colour;
                r.sortingOrder = layer.Order;
                layer.Pool.Add(r);
            }
            return layer.Pool[index];
        }

        private static uint Hash(long i, long j, int k, int salt)
        {
            unchecked
            {
                uint h = (uint)i * 73856093u ^ (uint)(i >> 32) * 19349663u ^ (uint)j * 83492791u ^ (uint)(j >> 32) * 2971215073u
                         ^ (uint)k * 2246822519u ^ (uint)salt * 0x27d4eb2du;
                h ^= h >> 15;
                h *= 0x2c1b3c6du;
                h ^= h >> 12;
                h *= 0x297a2d39u;
                h ^= h >> 15;
                return h;
            }
        }

        private static float Unit(uint h) => (h & 0xFFFFFF) / 16777216f;
    }
}
