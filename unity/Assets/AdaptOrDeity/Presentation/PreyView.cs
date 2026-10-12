using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one prey slot: a small violet cell with a darker core, smaller than the creature's cells, with a beating tail
    /// behind it pointing away from where it swims (it runs and tumbles). Hidden while the slot is empty.
    /// </summary>
    public sealed class PreyView : MonoBehaviour
    {
        private static readonly Color Body = new Color(0.72f, 0.50f, 0.95f, 0.95f);
        private static readonly Color Core = new Color(0.40f, 0.22f, 0.60f, 0.9f);
        private const int TailPoints = 10;

        private SessionBridge _bridge;
        private SpriteRenderer _body, _core;
        private LineRenderer _tail;
        private int _slot;

        public int Slot => _slot;

        /// <summary>Radius in world units, or 0 if the slot is empty.</summary>
        public float Radius { get; private set; }

        public void Initialise(SessionBridge bridge, int slot, Sprite circle)
        {
            _bridge = bridge;
            _slot = slot;
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            _body = body.AddComponent<SpriteRenderer>();
            _body.sprite = circle;
            _body.color = Body;
            _body.sortingOrder = 1;   // over food, beside the cells
            var core = new GameObject("Core");
            core.transform.SetParent(body.transform, false);
            core.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            _core = core.AddComponent<SpriteRenderer>();
            _core.sprite = circle;
            _core.color = Core;
            _core.sortingOrder = 2;
            var tail = new GameObject("Tail");
            tail.transform.SetParent(transform, false);
            _tail = tail.AddComponent<LineRenderer>();
            _tail.material = new Material(Shader.Find("Sprites/Default"));
            _tail.useWorldSpace = false;
            _tail.sortingOrder = 0;
            _tail.startWidth = 0.004f;
            _tail.endWidth = 0.0015f;
            _tail.startColor = new Color(0.80f, 0.65f, 1f, 0.9f);
            _tail.endColor = new Color(0.80f, 0.65f, 1f, 0.2f);
            _tail.positionCount = 0;
        }

        public bool Contains(Vector2 world) => Radius > 0f && ((Vector2)transform.position - world).sqrMagnitude <= Radius * Radius;

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null || _slot >= f.PreyCount)
            {
                _body.enabled = _core.enabled = false;
                _tail.positionCount = 0;
                Radius = 0f;
                return;
            }
            _body.enabled = _core.enabled = true;
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;   // placed relative to the body as drawn, like the food
            transform.localPosition = new Vector3(f.PreyX[_slot] * u + shift.x, f.PreyY[_slot] * u + shift.y, 0.05f);
            Radius = f.PreyRadius[_slot] * u;
            _body.transform.localScale = new Vector3(2f * Radius, 2f * Radius, 1f);
            // the tail trails behind the heading and beats
            float a = f.PreyHeading[_slot];
            var back = -new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var normal = new Vector2(-back.y, back.x);
            float beat = Time.time * 30f + _slot;
            const float length = 0.025f;   // world units: 2.5 um
            _tail.positionCount = TailPoints;
            for (int k = 0; k < TailPoints; k++)
            {
                float s = k / (float)(TailPoints - 1) * length;
                var p = back * (Radius + s) + normal * (0.004f * Mathf.Sin(s * 300f - beat));
                _tail.SetPosition(k, new Vector3(p.x, p.y, 0.1f));
            }
        }
    }
}
