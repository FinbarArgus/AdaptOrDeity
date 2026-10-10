using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one cell. In the plain view it uses a stylised cytoplasm colour; with an overlay active it is
    /// coloured by that channel's value on the overlay's legend range. The cell grows as it builds its daughter (its area
    /// up to double, drawn only: the kernel's geometry stays that of the starting cell), and draws its flagella: tails
    /// trailing opposite the swimming direction that beat while the motors run, a part-built one drawn short.
    /// </summary>
    public sealed class CellView : MonoBehaviour
    {
        /// <summary>World units per metre (1 unit = 10 µm).</summary>
        public const float UnitsPerMetre = 1e5f;

        private static readonly Color PlainColour = new Color(0.55f, 0.85f, 0.55f);
        private static readonly Color DeadColour = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color ReadyColour = new Color(0.75f, 0.95f, 0.55f);
        private static Sprite _circle;

        private SessionBridge _bridge;
        private SpriteRenderer _renderer;
        private int _cellIndex;
        private float _diameter;
        private int _growthChannel = -2, _flagellaTrait = -2;
        private readonly LineRenderer[] _tails = new LineRenderer[8];
        private Transform _tailRoot;
        private const int TailPoints = 16;

        public int CellIndex => _cellIndex;

        /// <summary>Radius in world units.</summary>
        public float Radius => 0.5f * _diameter;

        public void Initialise(SessionBridge bridge, int cellIndex, float diameterMetres)
        {
            _bridge = bridge;
            _cellIndex = cellIndex;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = CircleSprite();
            _diameter = diameterMetres * UnitsPerMetre;
            transform.localScale = new Vector3(_diameter, _diameter, 1f);
        }

        public bool Contains(Vector2 world) => ((Vector2)transform.position - world).sqrMagnitude <= Radius * Radius;

        private void LateUpdate()
        {
            var frame = _bridge.Frame;
            int overlay = _bridge.ActiveOverlay;
            if (frame == null || _cellIndex >= frame.CellCount)
                return;
            // Positions come from the simulation (metres, relative to the creature), so food contact and drawing agree.
            var at = _bridge.BodyRelative(frame.CellX[_cellIndex], frame.CellY[_cellIndex]);   // smooth between snapshots
            transform.localPosition = new Vector3(at.x, at.y, 0f);
            if (_growthChannel == -2)
            {
                _growthChannel = Index(_bridge.Schema.Overlays, "growth");
                _flagellaTrait = -1;
                for (int k = 0; k < _bridge.Schema.Traits.Count; k++)
                    if (_bridge.Schema.Traits[k].Id == "flagella")
                        _flagellaTrait = k;
            }
            // The cell grows as it builds its daughter (area x (1 + progress)); the selected cell is drawn slightly larger.
            float progress = _growthChannel >= 0 ? Mathf.Clamp01(frame.Channels[_growthChannel][_cellIndex]) : 0f;
            float grown = _diameter * Mathf.Sqrt(1f + progress);
            float scale = grown * (_bridge.SelectedCell == _cellIndex ? 1.12f : 1f);
            transform.localScale = new Vector3(scale, scale, 1f);
            DrawFlagella(frame, 0.5f * grown);
            if (!frame.CellAlive[_cellIndex])
            {
                _renderer.color = DeadColour;
                return;
            }
            if (overlay < 0 || overlay >= frame.Channels.Length)
            {
                _renderer.color = frame.CellCanDivide[_cellIndex] ? ReadyColour : PlainColour;
                return;
            }
            var info = _bridge.Schema.Overlays[overlay];
            double display = frame.Channels[overlay][_cellIndex] * info.DisplayMultiplier;
            float t = (float)((display - info.DisplayMin) / (info.DisplayMax - info.DisplayMin));
            _renderer.color = Colormap.Evaluate(t);
        }

        private static int Index(System.Collections.Generic.IReadOnlyList<AdaptOrDeity.Game.Session.OverlayInfo> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == id)
                    return i;
            return -1;
        }

        /// <summary>World position of flagellum k's base on this cell (for dragging it), and the cell's drawn radius.</summary>
        public Vector2 FlagellumBase(int k)
        {
            var f = _bridge.Frame;
            float a = f.FlagellumAngles[_cellIndex][k] + _bridge.DisplayOrientation;
            return (Vector2)transform.position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _drawnRadius;
        }

        /// <summary>Flagella built on this cell (a fraction is a part-built one).</summary>
        public float FlagellaCount => _flagellaTrait >= 0 && _bridge.Frame != null ? _bridge.Frame.TraitValues[_flagellaTrait][_cellIndex] : 0f;

        private float _drawnRadius;
        private readonly SpriteRenderer[] _handles = new SpriteRenderer[8];
        private LineRenderer[] _arcs;
        private const int ArcPoints = 24;

        private Material LineMaterial() => _lineMaterial != null ? _lineMaterial : (_lineMaterial = new Material(Shader.Find("Sprites/Default")));

        private Material _lineMaterial;

        private LineRenderer NewLine(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_tailRoot, false);
            var l = go.AddComponent<LineRenderer>();
            l.material = LineMaterial();
            l.useWorldSpace = false;
            l.sortingOrder = order;
            l.positionCount = 0;
            return l;
        }

        /// <summary>
        /// The flagella: fixed at their base angles on the membrane (they turn with the body, never around the cell), each
        /// trailing straight out from its base and beating while the motors run; a part-built one is drawn short.
        /// </summary>
        private void DrawFlagella(AdaptOrDeity.Game.Session.SnapshotFrame frame, float radius)
        {
            _drawnRadius = radius;
            if (_tailRoot == null)
            {
                // a sibling, not a child: the cell's transform is scaled by its diameter
                _tailRoot = new GameObject($"Flagella and proteins {_cellIndex}").transform;
                _tailRoot.SetParent(transform.parent, false);
                for (int i = 0; i < _tails.Length; i++)
                {
                    var l = NewLine($"Flagellum {i}", -2);
                    l.startWidth = 0.006f;
                    l.endWidth = 0.002f;
                    l.startColor = new Color(0.95f, 0.88f, 0.55f, 0.95f);
                    l.endColor = new Color(0.95f, 0.88f, 0.55f, 0.3f);
                    _tails[i] = l;
                    var h = new GameObject($"Flagellum base {i}");
                    h.transform.SetParent(_tailRoot, false);
                    h.transform.localScale = new Vector3(0.035f, 0.035f, 1f);
                    _handles[i] = h.AddComponent<SpriteRenderer>();
                    _handles[i].sprite = CircleSprite();
                    _handles[i].color = new Color(1f, 0.95f, 0.6f, 0.9f);
                    _handles[i].sortingOrder = 3;
                }
            }
            bool selected = _bridge.SelectedCell == _cellIndex;
            _tailRoot.localPosition = transform.localPosition;
            float count = frame.CellAlive[_cellIndex] ? FlagellaCount : 0f;
            float beat = frame.MotorOn ? Time.time * 25f : 0f;  // waves travel out along the tail while the motor runs
            const float length = 0.35f;                        // world units: a flagellum is several cell lengths (~3.5 µm)
            for (int i = 0; i < _tails.Length; i++)
            {
                float built = Mathf.Clamp01(count - i);
                var l = _tails[i];
                if (built <= 0.01f)
                {
                    l.positionCount = 0;
                    _handles[i].enabled = false;
                    continue;
                }
                float a = frame.FlagellumAngles[_cellIndex][i] + _bridge.DisplayOrientation;
                // the base: a handle to drag the flagellum around the membrane, shown on the selected cell
                _handles[i].enabled = selected;
                _handles[i].transform.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -0.1f);
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var normal = new Vector2(-dir.y, dir.x);
                var root = dir * radius;
                l.positionCount = TailPoints;
                for (int k = 0; k < TailPoints; k++)
                {
                    float s = k / (float)(TailPoints - 1) * length * built;
                    float wave = 0.025f * Mathf.Min(1f, s / 0.05f) * Mathf.Sin(s * 60f - beat + i);
                    var p = root + dir * s + normal * wave;
                    l.SetPosition(k, new Vector3(p.x, p.y, 0.2f));
                }
            }
            DrawMembrane(frame, radius);
        }

        /// <summary>
        /// The membrane proteins: each adaptable protein that sits in the membrane as an arc of the cell's rim, its length
        /// the share of the cell's surface its copies take (so it grows with expression), in its legend colour. Arcs sit
        /// end to end from the body's front, turning with the body.
        /// </summary>
        private void DrawMembrane(AdaptOrDeity.Game.Session.SnapshotFrame frame, float radius)
        {
            int n = frame.TraitArea.Length;
            if (_arcs == null)
            {
                _arcs = new LineRenderer[n];
                for (int t = 0; t < n; t++)
                {
                    var l = NewLine($"Protein {t}", 1);
                    l.startWidth = l.endWidth = 0.014f;
                    var c = ProteinColours.For(t);
                    l.startColor = l.endColor = c;
                    _arcs[t] = l;
                }
            }
            float surface = frame.CellSurface[_cellIndex];
            float start = _bridge.DisplayOrientation;
            const float gap = 0.04f;   // rad between arcs
            for (int t = 0; t < n; t++)
            {
                float share = surface > 0f && frame.CellAlive[_cellIndex] ? frame.TraitArea[t][_cellIndex] / surface : 0f;
                var l = _arcs[t];
                if (t == _flagellaTrait || share <= 0f)
                {
                    l.positionCount = 0;   // flagella show as tails; cytosolic proteins take no membrane
                    continue;
                }
                float sweep = 2f * Mathf.PI * share;
                l.positionCount = ArcPoints;
                for (int k = 0; k < ArcPoints; k++)
                {
                    float a = start + sweep * k / (ArcPoints - 1);
                    l.SetPosition(k, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0.1f));
                }
                start += sweep + gap;
            }
        }

        public static Sprite CircleSprite()
        {
            if (_circle != null)
                return _circle;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float r = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)(255 * Mathf.Clamp01(r - d));      // anti-aliased edge
                    byte shade = (byte)(d > r - 6f ? 200 : 255);           // darker membrane rim
                    pixels[y * size + x] = new Color32(shade, shade, shade, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            _circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _circle;
        }
    }
}
