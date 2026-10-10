using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one food packet slot: a disc smaller than a cell, coloured by the trace species it is richest in, shrinking as
    /// its glucose is eaten. Food carried by another creature shows the carrier (a dark body in the middle); lying food has
    /// none. Hidden while the slot is empty.
    /// </summary>
    public sealed class PacketView : MonoBehaviour
    {
        private static readonly Color[] TraceColours =
        {
            new Color(0.95f, 0.75f, 0.35f), new Color(0.95f, 0.55f, 0.65f), new Color(0.60f, 0.75f, 0.98f),
            new Color(0.70f, 0.95f, 0.70f),
        };

        private SessionBridge _bridge;
        private SpriteRenderer _renderer, _carrier;
        private int _slot;

        public int Slot => _slot;

        public void Initialise(SessionBridge bridge, int slot, Sprite circle)
        {
            _bridge = bridge;
            _slot = slot;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = circle;
            _renderer.sortingOrder = -1;   // under the cells
            var carrier = new GameObject("Carrier");
            carrier.transform.SetParent(transform, false);
            carrier.transform.localScale = new Vector3(0.7f, 0.7f, 1f);   // the creature carrying it, on top
            _carrier = carrier.AddComponent<SpriteRenderer>();
            _carrier.sprite = circle;
            _carrier.color = new Color(0.35f, 0.22f, 0.30f, 0.95f);
            _carrier.sortingOrder = 0;
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
                _carrier.enabled = false;
                Radius = 0f;
                return;
            }
            _renderer.enabled = true;
            _carrier.enabled = f.PacketCarried[_slot];
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;   // the body is drawn a little ahead of the snapshot: food is placed relative to it
            transform.localPosition = new Vector3(f.PacketX[_slot] * u + shift.x, f.PacketY[_slot] * u + shift.y, 0.1f);
            Radius = f.PacketRadius[_slot] * u;
            transform.localScale = new Vector3(2f * Radius, 2f * Radius, 1f);
            // the radius shrinks with what is left (area in proportion, from the simulation); it also pales a little
            var c = TraceColours[f.PacketTrace[_slot] % TraceColours.Length];
            c.a = 0.45f + 0.4f * Mathf.Clamp01(f.PacketFill[_slot]);
            _renderer.color = c;
        }
    }
}
