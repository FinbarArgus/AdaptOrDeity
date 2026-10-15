using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws Luck's current as a thick shaded arrow: a band as wide as the water it moves (brightest on its axis, fading to
    /// its edges as the flow does), brightening towards the head. While the player aims (<see cref="Dragging"/>, set by the
    /// HUD) it shows the aimed arrow; otherwise the one the frame says flows. Positions are relative to the creature.
    /// </summary>
    public sealed class LuckView : MonoBehaviour
    {
        private const int Segments = 12;
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        /// <summary>Whether the player is in Luck's aiming mode (the HUD's L key).</summary>
        public static bool Aiming { get; set; }

        /// <summary>Whether the player is dragging an arrow, and its tail and head relative to the creature [m].</summary>
        public static bool Dragging { get; set; }

        public static Vector2 AimTail { get; set; }

        public static Vector2 AimHead { get; set; }

        private SessionBridge _bridge;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private readonly Vector3[] _vertices = new Vector3[3 * (Segments + 1) + 3];
        private readonly Color[] _colours = new Color[3 * (Segments + 1) + 3];
        private int[] _triangles;

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            var go = new GameObject("Luck's current");
            go.transform.SetParent(transform, false);
            _mesh = new Mesh { name = "Luck's current" };
            _mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            _renderer.sortingOrder = -3;   // over the water and the seabed, under the cells and food
            _triangles = Triangles();
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null)
                return;
            bool flowing = f.LuckOn;
            Vector2 tail, head;
            float alpha;
            if (Dragging)
            {
                tail = AimTail;
                head = AimHead;
                alpha = flowing ? 0.55f : 0.25f;   // faint until the simulation blows it (or without points)
            }
            else if (flowing)
            {
                tail = new Vector2(f.LuckTailX, f.LuckTailY);
                head = new Vector2(f.LuckHeadX, f.LuckHeadY);
                alpha = 0.55f;
            }
            else
            {
                _renderer.enabled = false;
                return;
            }
            float u = CellView.UnitsPerMetre, w = Mathf.Max(f.LuckHalfWidth, 1e-7f) * u;
            var shift = _bridge.Shift;
            Vector2 t = tail * u + shift, h = head * u + shift, d = h - t;
            float length = d.magnitude;
            if (length < 1e-4f)
            {
                _renderer.enabled = false;
                return;
            }
            _renderer.enabled = true;
            Build(t, h, d / length, length, w, alpha);
        }

        // The shaft (Segments x 2 quads across: transparent edges, opaque axis) and the head (a wide triangle).
        private void Build(Vector2 t, Vector2 h, Vector2 e, float length, float w, float alpha)
        {
            var n = new Vector2(-e.y, e.x);
            float headLength = Mathf.Min(1.6f * w, 0.5f * length);
            float shaft = length - headLength;
            for (int i = 0; i <= Segments; i++)
            {
                float s = (float)i / Segments;
                Vector2 c = t + e * (shaft * s);
                float a = alpha * (0.35f + 0.65f * s);   // brighter towards the head
                _vertices[3 * i] = c - n * w;
                _vertices[3 * i + 1] = c;
                _vertices[3 * i + 2] = c + n * w;
                _colours[3 * i] = new Color(Gold.r, Gold.g, Gold.b, 0f);
                _colours[3 * i + 1] = new Color(Gold.r, Gold.g, Gold.b, a);
                _colours[3 * i + 2] = new Color(Gold.r, Gold.g, Gold.b, 0f);
            }
            int k = 3 * (Segments + 1);
            Vector2 b = t + e * shaft;
            _vertices[k] = b - n * (1.6f * w);
            _vertices[k + 1] = h;
            _vertices[k + 2] = b + n * (1.6f * w);
            _colours[k] = _colours[k + 2] = new Color(Gold.r, Gold.g, Gold.b, 0.6f * alpha);
            _colours[k + 1] = new Color(1f, 0.95f, 0.7f, Mathf.Min(1f, 1.4f * alpha));
            for (int i = 0; i < _vertices.Length; i++)
                _vertices[i].z = 0.3f;
            _mesh.vertices = _vertices;
            _mesh.colors = _colours;
            if (_mesh.triangles.Length != _triangles.Length)
                _mesh.triangles = _triangles;
            _mesh.RecalculateBounds();
        }

        private static int[] Triangles()
        {
            var tris = new int[Segments * 12 + 3];
            int n = 0;
            for (int i = 0; i < Segments; i++)
                for (int side = 0; side < 2; side++)
                {
                    int a = 3 * i + side, b = a + 1, c = a + 3, d = a + 4;
                    tris[n++] = a; tris[n++] = c; tris[n++] = b;
                    tris[n++] = b; tris[n++] = c; tris[n++] = d;
                }
            int k = 3 * (Segments + 1);
            tris[n++] = k;
            tris[n++] = k + 1;
            tris[n++] = k + 2;
            return tris;
        }
    }
}
