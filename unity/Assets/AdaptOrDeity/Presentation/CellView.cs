using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Draws one cell. In the plain view it uses a stylised cytoplasm colour; with an overlay active it is
    /// coloured by that channel's value on the overlay's legend range.
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
            // The selected cell is drawn slightly larger.
            float scale = _diameter * (_bridge.SelectedCell == _cellIndex ? 1.12f : 1f);
            transform.localScale = new Vector3(scale, scale, 1f);
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

        private static Sprite CircleSprite()
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
