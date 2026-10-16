using System.Collections.Generic;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws Luck's stream. While the player aims (<see cref="Dragging"/>, set by the HUD) a faint thick arrow previews it,
    /// with a fainter line on past its head (the stream runs on along it). Once sent, the stream is a jet along that line:
    /// a band shaded as its Gaussian profile, coming from beyond the edge of the view and reaching to its front, which
    /// moves on downstream and fades in over its length; as bright as it is strong (spinning up, driven, coasting as it
    /// spreads), with streaks that travel along it at the flow where they are and leave fading tails: its wake. Positions
    /// are relative to the creature.
    /// </summary>
    public sealed class LuckView : MonoBehaviour
    {
        private const int Segments = 12, AlongSegments = 40, Lanes = 5, StreaksPerLane = 6;
        private static readonly float[] Across = { -3f, -2f, -1f, 0f, 1f, 2f, 3f };   // band columns, in sigmas
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
        private readonly float[] _phase = new float[Lanes];   // how far each lane's wake has travelled [world units]
        private double _lastSimTime;

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
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;
            double dtSim = f.SimTime - _lastSimTime;
            _lastSimTime = f.SimTime;
            float reach = ViewReach();

            // the aim: a faint arrow, and a fainter line on past its head; nothing flows yet
            _previewRenderer.enabled = Dragging && Preview(AimTail * u + shift, AimHead * u + shift, 3f * f.LuckWidth * u, reach);
            if (_previewRenderer.enabled)
                Apply(_preview);

            bool flowing = f.LuckOn && f.LuckStrength > 0f;
            if (!flowing)
            {
                _streamRenderer.enabled = false;
                return;
            }
            float speed = f.LuckStrength * f.LuckMaxSpeed * u, sigma = f.LuckWidth * u;
            if (dtSim > 0.0 && dtSim < 10.0)
                for (int lane = 0; lane < Lanes; lane++)
                    _phase[lane] += speed * Profile(LaneAcross(lane)) * (float)dtSim;
            var origin = new Vector2(f.LuckOriginX, f.LuckOriginY) * u + shift;
            var dir = new Vector2(f.LuckDirX, f.LuckDirY);
            _streamRenderer.enabled = Jet(origin, dir, sigma, f.LuckFront * u, Mathf.Max(f.LuckFrontWidth * u, 1e-4f), reach,
                Mathf.Clamp01(f.LuckStrength));
            if (_streamRenderer.enabled)
                Apply(_stream);
        }

        // Half the diagonal of what the camera shows, and a margin [world units], so lines can run from beyond the view
        private static float ViewReach()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
                return 10f;
            float h = cam.orthographicSize, w = h * cam.aspect;
            return 1.2f * Mathf.Sqrt(h * h + w * w);
        }

        private static float Profile(float sigmas) => Mathf.Exp(-0.5f * sigmas * sigmas);

        private static float LaneAcross(int lane) => ((lane + 0.5f) / Lanes * 2f - 1f) * 1.6f;   // -1.3..1.3 sigma

        // The front's rise: 1 upstream, 0 beyond, as the flow (1 - tanh((s - front)/L))/2
        private static float Rise(float s, float front, float l) => 0.5f * (1f - (float)System.Math.Tanh((s - front) / l));

        private void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(_v);
            mesh.SetColors(_c);
            mesh.SetTriangles(_t, 0);
            mesh.RecalculateBounds();
        }

        private bool Preview(Vector2 t, Vector2 h, float w, float reach)
        {
            Clear();
            Vector2 d = h - t;
            float length = d.magnitude;
            if (length < 1e-4f)
                return false;
            Vector2 e = d / length, n = new Vector2(-e.y, e.x);
            Arrow(t, e, n, length, Mathf.Max(w, 0.05f), 0.28f);
            // on past the head, out of the view: where the stream will run
            float thin = 0.08f * Mathf.Max(w, 0.05f);
            int k = _v.Count;
            Vertex(h - n * thin, Gold, 0.18f);
            Vertex(h + n * thin, Gold, 0.18f);
            Vertex(h + e * reach - n * thin, Gold, 0f);
            Vertex(h + e * reach + n * thin, Gold, 0f);
            Quad(k, k + 2, k + 1, k + 3);
            return true;
        }

        // A thick arrow of length `length` from t along e, half-width w, into the vertex lists
        private void Arrow(Vector2 t, Vector2 e, Vector2 n, float length, float w, float alpha)
        {
            float headLength = Mathf.Min(1.6f * w, 0.5f * length), shaft = length - headLength;
            int start = _v.Count;
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
                        int p = start + 3 * (i - 1) + side;
                        Quad(p, p + 3, p + 1, p + 4);
                    }
            }
            Vector2 b = t + e * shaft;
            int k = _v.Count;
            Vertex(b - n * (1.6f * w), Gold, 0.6f * alpha);
            Vertex(t + e * length, Pale, Mathf.Min(1f, 1.4f * alpha));
            Vertex(b + n * (1.6f * w), Gold, 0.6f * alpha);
            _t.Add(k);
            _t.Add(k + 1);
            _t.Add(k + 2);
        }

        // The jet from beyond the view (s = -reach) to just past its front, its profile across, and its wake
        private bool Jet(Vector2 origin, Vector2 e, float sigma, float front, float l, float reach, float strength)
        {
            Clear();
            if (sigma <= 0f)
                return false;
            Vector2 n = new Vector2(-e.y, e.x);
            // where the line passes the creature (the view's centre): the band runs reach either side of that
            float centre = Vector2.Dot(-origin, e);
            float s0 = centre - reach, s1 = Mathf.Min(centre + reach, front + 3f * l);
            if (s1 <= s0)
                return false;
            float alpha = 0.12f + 0.38f * strength;
            int columns = Across.Length;
            for (int i = 0; i <= AlongSegments; i++)
            {
                float s = Mathf.Lerp(s0, s1, (float)i / AlongSegments);
                float rise = Rise(s, front, l);
                for (int c = 0; c < columns; c++)
                    Vertex(origin + e * s + n * (Across[c] * sigma), Gold, alpha * rise * Profile(Across[c]));
                if (i > 0)
                    for (int c = 0; c < columns - 1; c++)
                    {
                        int p = (i - 1) * columns + c;
                        Quad(p, p + columns, p + 1, p + columns + 1);
                    }
            }
            // the wake: streaks along lanes across the jet, each moving at the flow in its lane, fading in at the front
            float span = s1 - s0, spacing = span / StreaksPerLane, tail = Mathf.Min(0.5f * spacing, 4f * sigma), thick = 0.08f * sigma;
            for (int lane = 0; lane < Lanes; lane++)
            {
                float across = LaneAcross(lane), profile = Profile(across);
                for (int j = 0; j < StreaksPerLane; j++)
                {
                    float s = s0 + Mathf.Repeat(_phase[lane] + 0.37f * lane * spacing + j * spacing, span);
                    float a = 0.9f * strength * profile * Rise(s, front, l) * Mathf.Clamp01((s - s0) / (0.1f * span));
                    if (a <= 0.01f)
                        continue;
                    Vector2 head = origin + e * s + n * (across * sigma), back = head - e * Mathf.Min(tail, s - s0);
                    int k = _v.Count;
                    Vertex(back - n * thick, Pale, 0f);
                    Vertex(back + n * thick, Pale, 0f);
                    Vertex(head - n * thick, Pale, a);
                    Vertex(head + n * thick, Pale, a);
                    Quad(k, k + 2, k + 1, k + 3);
                }
            }
            return true;
        }

        private void Clear()
        {
            _v.Clear();
            _c.Clear();
            _t.Clear();
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
