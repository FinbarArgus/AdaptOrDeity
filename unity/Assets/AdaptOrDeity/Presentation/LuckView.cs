using System.Collections.Generic;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws Luck's stream. While the player aims (<see cref="Dragging"/>, set by the HUD) a faint thick arrow previews it.
    /// Once sent, the stream is a shaded band as wide as the water it moves (brightest on its axis, fading to its edges as
    /// the flow does, as bright as it is strong while it spins up, flows and coasts down), with streaks that travel along
    /// it at its speed and leave fading tails: its wake. Positions are relative to the creature.
    /// </summary>
    public sealed class LuckView : MonoBehaviour
    {
        private const int Segments = 12, Lanes = 5, StreaksPerLane = 4;
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f), Pale = new Color(1f, 0.96f, 0.78f);

        /// <summary>Whether the player is in Luck's aiming mode (the HUD's L key).</summary>
        public static bool Aiming { get; set; }

        /// <summary>Whether the player is dragging an arrow, and its tail and head relative to the creature [m].</summary>
        public static bool Dragging { get; set; }

        public static Vector2 AimTail { get; set; }

        public static Vector2 AimHead { get; set; }

        private SessionBridge _bridge;
        private Mesh _preview, _stream;
        private MeshRenderer _previewRenderer, _streamRenderer;
        private readonly List<Vector3> _v = new List<Vector3>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<int> _t = new List<int>();
        private double _lastSimTime;
        private float _phase;   // how far the wake has travelled along the stream [world units]

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            var material = new Material(Shader.Find("Sprites/Default"));
            (_stream, _streamRenderer) = Layer("Luck's stream", material, -3);   // over the water and seabed, under the cells
            (_preview, _previewRenderer) = Layer("Luck's aim", material, -2);
        }

        private (Mesh, MeshRenderer) Layer(string name, Material material, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = order;
            return (mesh, r);
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null)
                return;
            float u = CellView.UnitsPerMetre, w = Mathf.Max(f.LuckHalfWidth, 1e-7f) * u;
            var shift = _bridge.Shift;
            double dtSim = f.SimTime - _lastSimTime;
            _lastSimTime = f.SimTime;

            // the aim: a faint arrow, nothing flows yet
            _previewRenderer.enabled = Dragging && Arrow(AimTail * u + shift, AimHead * u + shift, w, 0.28f, false, 0f);
            if (_previewRenderer.enabled)
                Apply(_preview);

            // the stream: as bright as it is strong, its wake travelling along it at its speed
            bool flowing = f.LuckOn && f.LuckStrength > 0f;
            if (flowing && dtSim > 0.0 && dtSim < 10.0)
                _phase += (float)(f.LuckStrength * f.LuckMaxSpeed * u * dtSim);
            float strength = Mathf.Clamp01(f.LuckStrength);
            _streamRenderer.enabled = flowing && Arrow(new Vector2(f.LuckTailX, f.LuckTailY) * u + shift,
                new Vector2(f.LuckHeadX, f.LuckHeadY) * u + shift, w, 0.15f + 0.45f * strength, true, strength);
            if (_streamRenderer.enabled)
                Apply(_stream);
        }

        private void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(_v);
            mesh.SetColors(_c);
            mesh.SetTriangles(_t, 0);
            mesh.RecalculateBounds();
        }

        // A thick arrow from t to h (world units) into the vertex lists, with the wake's streaks when asked; false if too
        // short to draw.
        private bool Arrow(Vector2 t, Vector2 h, float w, float alpha, bool wake, float strength)
        {
            _v.Clear();
            _c.Clear();
            _t.Clear();
            Vector2 d = h - t;
            float length = d.magnitude;
            if (length < 1e-4f)
                return false;
            Vector2 e = d / length, n = new Vector2(-e.y, e.x);
            float headLength = Mathf.Min(1.6f * w, 0.5f * length), shaft = length - headLength;
            // shaft: transparent edges, coloured axis, brighter towards the head
            for (int i = 0; i <= Segments; i++)
            {
                float s = (float)i / Segments;
                Vector2 c = t + e * (shaft * s);
                float a = alpha * (0.35f + 0.65f * s);
                Vertex(c - n * w, Gold, 0f);
                Vertex(c, Gold, a);
                Vertex(c + n * w, Gold, 0f);
                if (i > 0)
                    for (int side = 0; side < 2; side++)
                    {
                        int p = 3 * (i - 1) + side;
                        Quad(p, p + 3, p + 1, p + 4);
                    }
            }
            // head
            Vector2 b = t + e * shaft;
            int k = _v.Count;
            Vertex(b - n * (1.6f * w), Gold, 0.6f * alpha);
            Vertex(h, Pale, Mathf.Min(1f, 1.4f * alpha));
            Vertex(b + n * (1.6f * w), Gold, 0.6f * alpha);
            _t.Add(k);
            _t.Add(k + 1);
            _t.Add(k + 2);
            if (wake)
                Wake(t, e, n, shaft, w, strength);
            return true;
        }

        // Streaks along lanes across the band, moving with the stream (by _phase) and fading at both ends of the shaft;
        // each has a comet tail behind it. Lanes near the edges, where the flow is weaker, are fainter.
        private void Wake(Vector2 t, Vector2 e, Vector2 n, float shaft, float w, float strength)
        {
            float spacing = shaft / StreaksPerLane, tail = Mathf.Min(0.6f * spacing, 3f * w), thick = 0.12f * w;
            for (int lane = 0; lane < Lanes; lane++)
            {
                float across = ((lane + 0.5f) / Lanes * 2f - 1f) * 0.8f;   // -0.8..0.8 of the half-width
                float profile = Mathf.Cos(0.5f * Mathf.PI * across);
                profile *= profile;
                float offset = 0.37f * lane * spacing;   // lanes out of step
                for (int j = 0; j < StreaksPerLane; j++)
                {
                    float along = Mathf.Repeat(_phase * profile + offset + j * spacing, shaft);
                    float fade = Mathf.Clamp01(along / (0.15f * shaft)) * Mathf.Clamp01((shaft - along) / (0.15f * shaft));
                    float a = 0.9f * strength * profile * fade;
                    if (a <= 0.01f)
                        continue;
                    Vector2 front = t + e * along + n * (across * w), back = front - e * Mathf.Min(tail, along);
                    int k = _v.Count;
                    Vertex(back - n * thick, Pale, 0f);
                    Vertex(back + n * thick, Pale, 0f);
                    Vertex(front - n * thick, Pale, a);
                    Vertex(front + n * thick, Pale, a);
                    Quad(k, k + 2, k + 1, k + 3);
                }
            }
        }

        private void Vertex(Vector2 p, Color colour, float alpha)
        {
            _v.Add(new Vector3(p.x, p.y, 0.3f));
            _c.Add(new Color(colour.r, colour.g, colour.b, alpha));
        }

        // two triangles a-c-b and b-c-d of a quad with a, b on one side and c, d on the other
        private void Quad(int a, int c, int b, int d)
        {
            _t.Add(a); _t.Add(c); _t.Add(b);
            _t.Add(b); _t.Add(c); _t.Add(d);
        }
    }
}
