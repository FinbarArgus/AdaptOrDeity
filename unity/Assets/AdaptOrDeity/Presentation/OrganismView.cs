using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one free-organism slot: a daughter that left the creature (or one of its offspring), drawn like the
    /// creature's cells but paler and with a white outline, so it is plainly not the player's. It grows as it builds a
    /// daughter, takes the overlay's colour when one is shown, and its flagella trail from their bases and beat while its
    /// motors run. Frames carry living organisms only: a dead one's body is food (<see cref="PacketView"/>). Hidden while
    /// the slot is empty.
    /// </summary>
    public sealed class OrganismView : MonoBehaviour
    {
        private static readonly Color PlainColour = new Color(0.74f, 0.90f, 0.74f, 0.9f);
        private static readonly Color OutlineColour = new Color(0.95f, 0.98f, 1f, 0.9f);
        private static readonly Color VacuoleColour = new Color(0.95f, 0.78f, 0.40f, 0.85f);
        private const int TailPoints = 12;

        private SessionBridge _bridge;
        private SpriteRenderer _body, _outline, _vacuole;
        private readonly LineRenderer[] _tails = new LineRenderer[8];
        private int _slot, _growthChannel = -2;
        private float _diameter;

        public int Slot => _slot;

        /// <summary>Radius as drawn, in world units; 0 while the slot is empty.</summary>
        public float Radius { get; private set; }

        public void Initialise(SessionBridge bridge, int slot, float diameterMetres, Sprite circle)
        {
            _bridge = bridge;
            _slot = slot;
            _diameter = diameterMetres * CellView.UnitsPerMetre;
            _outline = NewSprite("Outline", circle, 0);
            _body = NewSprite("Body", circle, 1);
            _vacuole = NewSprite("Vacuole", circle, 2);
            _vacuole.color = VacuoleColour;
            var material = new Material(Shader.Find("Sprites/Default"));
            for (int i = 0; i < _tails.Length; i++)
            {
                var go = new GameObject($"Flagellum {i}");
                go.transform.SetParent(transform, false);
                var l = go.AddComponent<LineRenderer>();
                l.material = material;
                l.useWorldSpace = false;
                l.sortingOrder = -2;
                l.startWidth = 0.005f;
                l.endWidth = 0.0015f;
                l.startColor = new Color(0.95f, 0.92f, 0.75f, 0.8f);
                l.endColor = new Color(0.95f, 0.92f, 0.75f, 0.2f);
                l.positionCount = 0;
                _tails[i] = l;
            }
        }

        private SpriteRenderer NewSprite(string name, Sprite circle, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = circle;
            r.sortingOrder = order;
            return r;
        }

        public bool Contains(Vector2 world) => Radius > 0f && ((Vector2)transform.position - world).sqrMagnitude <= Radius * Radius;

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null || _slot >= f.OrganismCount)
            {
                _body.enabled = _outline.enabled = _vacuole.enabled = false;
                foreach (var l in _tails)
                    l.positionCount = 0;
                Radius = 0f;
                return;
            }
            _body.enabled = _outline.enabled = true;
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;   // placed relative to the body as drawn, like the food and prey
            transform.localPosition = new Vector3(f.OrganismX[_slot] * u + shift.x, f.OrganismY[_slot] * u + shift.y, 0.02f);
            if (_growthChannel == -2)
            {
                _growthChannel = -1;
                for (int c = 0; c < _bridge.Schema.Overlays.Count; c++)
                    if (_bridge.Schema.Overlays[c].Id == "growth")
                        _growthChannel = c;
            }
            float progress = _growthChannel >= 0 ? Mathf.Clamp01(f.OrganismChannels[_growthChannel][_slot]) : 0f;
            float grown = _diameter * Mathf.Sqrt(1f + progress);
            bool selected = _bridge.SelectedOrganism >= 0 && _bridge.SelectedOrganism == f.OrganismId[_slot];
            float d = grown * (selected ? 1.12f : 1f);
            Radius = 0.5f * d;
            _body.transform.localScale = new Vector3(d, d, 1f);
            _outline.transform.localScale = new Vector3(d * 1.1f, d * 1.1f, 1f);
            _outline.color = OutlineColour;
            int overlay = _bridge.ActiveOverlay;
            if (overlay < 0 || overlay >= f.OrganismChannels.Length)
                _body.color = PlainColour;
            else
            {
                var info = _bridge.Schema.Overlays[overlay];
                double display = f.OrganismChannels[overlay][_slot] * info.DisplayMultiplier;
                var c = Colormap.Evaluate((float)((display - info.DisplayMin) / (info.DisplayMax - info.DisplayMin)));
                _body.color = Color.Lerp(c, Color.white, 0.25f);   // paler than the creature's cells
            }
            float vacuole = f.OrganismVacuoleRadius[_slot] * u;
            _vacuole.enabled = vacuole > 0f;
            _vacuole.transform.localScale = new Vector3(2f * vacuole, 2f * vacuole, 1f);
            DrawFlagella(f, 0.5f * grown);
        }

        // Fixed at their base angles on the membrane, turning with the organism, beating while its motors run.
        private void DrawFlagella(AdaptOrDeity.Game.Session.SnapshotFrame f, float radius)
        {
            float count = f.OrganismFlagella[_slot];
            float beat = f.OrganismMotorOn[_slot] ? Time.time * 25f : 0f;
            const float length = 0.35f;   // as the creature's: ~3.5 µm
            for (int i = 0; i < _tails.Length; i++)
            {
                float built = Mathf.Clamp01(count - i);
                var l = _tails[i];
                if (built <= 0.01f)
                {
                    l.positionCount = 0;
                    continue;
                }
                float a = f.OrganismFlagellumAngles[_slot][i] + f.OrganismOrientation[_slot];
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var normal = new Vector2(-dir.y, dir.x);
                l.positionCount = TailPoints;
                for (int k = 0; k < TailPoints; k++)
                {
                    float s = k / (float)(TailPoints - 1) * length * built;
                    float wave = 0.025f * Mathf.Min(1f, s / 0.05f) * Mathf.Sin(s * 60f - beat + i + _slot);
                    var p = dir * (radius + s) + normal * wave;
                    l.SetPosition(k, new Vector3(p.x, p.y, 0.2f));
                }
            }
        }
    }
}
