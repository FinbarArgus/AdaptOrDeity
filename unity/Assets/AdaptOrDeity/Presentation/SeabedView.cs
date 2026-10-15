using System.Collections.Generic;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The seabed at the bottom of the world: rocky ground with a jagged top and pebbles below its line, and the rocks
    /// around the creature (boulders, stacks and arches, from the simulation's frames) as jagged, faceted stones lit from
    /// above. Each shape is drawn from its place in the world, so a rock or a stretch of ground looks the same every time
    /// it comes into view. The shapes stay inside the simulation's collision shapes (each rock's disc; the ground's line
    /// is the tops of its peaks), so bodies are seen resting on stone, not hovering over it. Placed relative to the
    /// creature like the food, so it scrolls as the creature moves.
    /// </summary>
    public sealed class SeabedView : MonoBehaviour
    {
        private static readonly Color Bedrock = new Color(0.30f, 0.29f, 0.28f, 1f);
        private static readonly Color BedrockDeep = new Color(0.17f, 0.17f, 0.18f, 1f);
        private static readonly Color Crust = new Color(0.44f, 0.41f, 0.37f, 1f);
        private static readonly Color Stone = new Color(0.46f, 0.47f, 0.49f, 1f);
        private static readonly Color StoneRim = new Color(0.20f, 0.21f, 0.23f, 1f);
        private const float GroundDepth = 200f;          // world units: the solid band drawn below the line
        private const float GroundWidth = 400f;          // the plain bedrock under the detailed strip, for far zooms
        private const float CrustDepth = 0.35f;          // the jagged top layer [world units]
        private const float Step = 0.05f;                // ground profile resolution [world units] (0.5 µm)
        private const float HalfWindow = 60f;            // detailed ground each side of where it was built [units]
        private const float Rebuild = 20f;               // rebuild once the creature is this far from that place
        private const int MaxCachedRocks = 512;

        private SessionBridge _bridge;
        private SpriteRenderer _bedrock;
        private MeshFilter _groundMesh;
        private double _groundBuiltAt = double.NaN;      // creature x [m] the detailed ground was built around
        private readonly MeshRenderer[] _rocks = new MeshRenderer[AdaptOrDeity.Game.Session.SnapshotFrame.MaxRocks];
        private readonly MeshFilter[] _rockMeshes = new MeshFilter[AdaptOrDeity.Game.Session.SnapshotFrame.MaxRocks];
        private readonly Dictionary<long, Mesh> _rockShapes = new Dictionary<long, Mesh>();
        private static Material _material;

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            var square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            var go = new GameObject("Bedrock");
            go.transform.SetParent(transform, false);
            _bedrock = go.AddComponent<SpriteRenderer>();
            _bedrock.sprite = square;
            _bedrock.color = BedrockDeep;
            _bedrock.sortingOrder = -21;
            _bedrock.enabled = false;
            _groundMesh = MeshPart("Ground", -20).GetComponent<MeshFilter>();
            for (int i = 0; i < _rocks.Length; i++)
            {
                _rocks[i] = MeshPart($"Rock {i}", -17);
                _rockMeshes[i] = _rocks[i].GetComponent<MeshFilter>();
            }
        }

        // Vertex-coloured meshes drawn like sprites: under the food, prey and cells, over the water overlay.
        private MeshRenderer MeshPart(string name, int order)
        {
            _material ??= new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _material;
            r.sortingOrder = order;
            r.enabled = false;
            return r;
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null)
                return;
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;
            float top = f.GroundY * u + shift.y;
            _bedrock.enabled = true;
            _bedrock.transform.localPosition = new Vector3(shift.x, top - CrustDepth - 0.5f * GroundDepth, 0.31f);
            _bedrock.transform.localScale = new Vector3(GroundWidth, GroundDepth, 1f);

            if (double.IsNaN(_groundBuiltAt) || System.Math.Abs(f.CreatureX - _groundBuiltAt) * u > Rebuild)
            {
                _groundBuiltAt = f.CreatureX;
                _groundMesh.sharedMesh = GroundMesh(_groundBuiltAt * u);
            }
            var ground = _groundMesh.GetComponent<MeshRenderer>();
            ground.enabled = true;
            _groundMesh.transform.localPosition = new Vector3((float)((_groundBuiltAt - f.CreatureX) * u) + shift.x, top, 0.3f);

            for (int i = 0; i < _rocks.Length; i++)
            {
                bool shown = i < f.RockCount;
                _rocks[i].enabled = shown;
                if (!shown)
                    continue;
                // the rock's place in the world, to 0.01 µm, names its shape
                long key = (long)System.Math.Round((f.RockX[i] + f.CreatureX) * 1e8) * 1000003L
                           + (long)System.Math.Round((f.RockY[i] + f.CreatureY) * 1e8);
                _rockMeshes[i].sharedMesh = RockShape(key);
                float radius = f.RockRadius[i] * u;
                _rocks[i].transform.localPosition = new Vector3(f.RockX[i] * u + shift.x, f.RockY[i] * u + shift.y, 0.25f);
                _rocks[i].transform.localScale = new Vector3(radius, radius, 1f);
            }
        }

        private Mesh RockShape(long key)
        {
            if (_rockShapes.TryGetValue(key, out var mesh))
                return mesh;
            if (_rockShapes.Count >= MaxCachedRocks)
            {
                foreach (var m in _rockShapes.Values)
                    Destroy(m);
                _rockShapes.Clear();
            }
            var b = new MeshBuilder();
            float tint = 0.9f + 0.2f * Rand(key, 99);
            DrawStone(b, key, Vector2.zero, 1f, 11 + (int)(Hash(key, 0) % 5), Stone * tint);
            mesh = b.Build();
            _rockShapes[key] = mesh;
            return mesh;
        }

        /// <summary>
        /// A jagged stone of radius at most <paramref name="radius"/> about <paramref name="centre"/>: a dark rim round an
        /// outline of alternating points and notches, faceted inside, each facet lit by its slope (light from above left).
        /// </summary>
        private static void DrawStone(MeshBuilder b, long seed, Vector2 centre, float radius, int n, Color colour)
        {
            var outer = new Vector2[n];
            var inner = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = 2f * Mathf.PI * (i + 0.4f * (Rand(seed, 3 * i + 1) - 0.5f)) / n;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float r = i % 2 == 0 ? 0.86f + 0.1f * Rand(seed, 3 * i + 2) : 0.66f + 0.14f * Rand(seed, 3 * i + 2);
                outer[i] = centre + dir * (radius * r);
                inner[i] = centre + dir * (radius * (0.38f + 0.18f * Rand(seed, 3 * i + 3)));
            }
            // the rim: the outline pushed out a little, in the dark (still inside the disc: points reach 0.96 x 1.04)
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                b.Triangle(centre, centre + (outer[i] - centre) * 1.04f, centre + (outer[j] - centre) * 1.04f, StoneRim);
            }
            var core = centre + new Vector2(Rand(seed, 501) - 0.5f, Rand(seed, 502) - 0.5f) * (0.15f * radius);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                var mid = 0.5f * (outer[i] + outer[j]) - centre;
                var normal = mid.sqrMagnitude > 0f ? mid.normalized : Vector2.up;
                float light = 0.62f + 0.32f * Mathf.Max(0f, normal.y) - 0.12f * normal.x + 0.08f * (Rand(seed, 700 + i) - 0.5f);
                var face = Shade(colour, light);
                b.Triangle(outer[i], outer[j], inner[j], face);
                b.Triangle(outer[i], inner[j], inner[i], Shade(face, 0.93f));
                b.Triangle(core, inner[i], inner[j], Shade(colour, 0.95f + 0.12f * Rand(seed, 800 + i) + 0.08f * normal.y));
            }
        }

        private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        // The detailed ground: a jagged crust whose peaks reach the ground line (y = 0 in the mesh) and whose notches dip
        // up to ~1 µm below it, pebbles resting on it, bedrock darkening below; across ±HalfWindow about centreX [units].
        private static Mesh GroundMesh(double centreX)
        {
            var b = new MeshBuilder();
            long k0 = (long)System.Math.Floor((centreX - HalfWindow) / Step);
            int count = (int)(2f * HalfWindow / Step);
            float Depth(long k) =>
                0.012f + 0.06f * Smooth(k, 23) + 0.035f * Smooth(k, 5) + 0.03f * Rand(k, 17) * Rand(k, 18);
            float X(long k) => (float)(k * (double)Step - centreX);
            for (int s = 0; s < count; s++)
            {
                long k = k0 + s;
                float x0 = X(k), x1 = X(k + 1), d0 = Depth(k), d1 = Depth(k + 1);
                var crust = Shade(Crust, 0.85f + 0.25f * Smooth(k, 41) + 0.06f * Rand(k, 19));
                b.Quad(new Vector2(x0, -d0), new Vector2(x1, -d1), new Vector2(x1, -CrustDepth), new Vector2(x0, -CrustDepth),
                    crust, crust, Bedrock, Bedrock);
                b.Quad(new Vector2(x0, -CrustDepth), new Vector2(x1, -CrustDepth), new Vector2(x1, -3f), new Vector2(x0, -3f),
                    Bedrock, Bedrock, BedrockDeep, BedrockDeep);
                if (Rand(k, 7) < 0.035f)
                {
                    float r = 0.03f + 0.05f * Rand(k, 8);
                    DrawStone(b, k * 31 + 7, new Vector2(x0, -d0 - 0.2f * r), r, 7 + (int)(Hash(k, 9) % 3),
                        Shade(Stone, 0.8f + 0.3f * Rand(k, 10)));
                }
            }
            return b.Build();
        }

        // Value noise over the profile's index: smooth between hashed values every `period` steps.
        private static float Smooth(long k, int period)
        {
            long cell = k >= 0 ? k / period : (k - period + 1) / period;
            float t = (k - cell * period) / (float)period;
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(Rand(cell, period), Rand(cell + 1, period), t);
        }

        private static uint Hash(long a, int b)
        {
            unchecked
            {
                ulong x = (ulong)a * 0x9E3779B97F4A7C15UL ^ ((ulong)b + 0x632BE59BD9B4E019UL) * 0xC2B2AE3D27D4EB4FUL;
                x ^= x >> 31;
                x *= 0xBF58476D1CE4E5B9UL;
                x ^= x >> 29;
                return (uint)x;
            }
        }

        private static float Rand(long a, int b) => Hash(a, b) / (float)uint.MaxValue;

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Color> _colours = new List<Color>();
            private readonly List<int> _triangles = new List<int>();

            public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color colour)
            {
                // Unity's sprites face -z: wind clockwise
                if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0f)
                    (b, c) = (c, b);
                int n = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _colours.Add(colour);
                _colours.Add(colour);
                _colours.Add(colour);
                _triangles.Add(n);
                _triangles.Add(n + 1);
                _triangles.Add(n + 2);
            }

            // A quadrilateral a b c d (in order round it) with a colour at each corner.
            public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ca, Color cb, Color cc, Color cd)
            {
                int n = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);
                _colours.Add(ca);
                _colours.Add(cb);
                _colours.Add(cc);
                _colours.Add(cd);
                bool clockwise = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) < 0f;
                if (clockwise)
                    _triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
                else
                    _triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
            }

            public Mesh Build()
            {
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(_vertices);
                mesh.SetColors(_colours);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
