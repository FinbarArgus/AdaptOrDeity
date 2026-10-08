using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one food packet slot: a translucent disc coloured by the trace species it is richest in, fading as its
    /// glucose is eaten. Hidden while the slot is empty.
    /// </summary>
    public sealed class PacketView : MonoBehaviour
    {
        private static readonly Color[] TraceColours =
        {
            new Color(0.95f, 0.75f, 0.35f), new Color(0.95f, 0.55f, 0.65f), new Color(0.60f, 0.75f, 0.98f),
            new Color(0.70f, 0.95f, 0.70f),
        };

        private SessionBridge _bridge;
        private SpriteRenderer _renderer;
        private int _slot;
        private float _fullGlucose = 10f;

        public int Slot => _slot;

        public void Initialise(SessionBridge bridge, int slot, Sprite circle)
        {
            _bridge = bridge;
            _slot = slot;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = circle;
            _renderer.sortingOrder = -1;   // under the cells
        }

        /// <summary>Radius in world units, or 0 if the slot is empty.</summary>
        public float Radius { get; private set; }

        public bool Contains(Vector2 world) => Radius > 0f && ((Vector2)transform.position - world).sqrMagnitude <= Radius * Radius;

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null || _slot >= f.PacketCount)
            {
                _renderer.enabled = false;
                Radius = 0f;
                return;
            }
            _renderer.enabled = true;
            float u = CellView.UnitsPerMetre;
            transform.localPosition = new Vector3(f.PacketX[_slot] * u, f.PacketY[_slot] * u, 0.1f);
            Radius = f.PacketRadius[_slot] * u;
            transform.localScale = new Vector3(2f * Radius, 2f * Radius, 1f);
            _fullGlucose = Mathf.Max(_fullGlucose, f.PacketGlucose[_slot]);
            var c = TraceColours[f.PacketTrace[_slot] % TraceColours.Length];
            c.a = 0.15f + 0.45f * Mathf.Clamp01(f.PacketGlucose[_slot] / _fullGlucose);
            _renderer.color = c;
        }
    }
}
