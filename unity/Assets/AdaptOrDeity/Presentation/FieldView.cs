using AdaptOrDeity.Game.Session;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The water's chemistry around the creature as an overlay (ONI-style): the frame's 64x64 raster of glucose, lactate or
    /// pH (<see cref="SnapshotFrame.FieldGlucose"/>), drawn under the cells and food, centred on the creature and along the
    /// ground's axes. Its colour range follows what the raster shows now (the legend says it), so a lone cell's slight
    /// depletion is visible as well as a packet's halo. Choose one with <see cref="SessionBridge.ActiveField"/>.
    /// </summary>
    public sealed class FieldView : MonoBehaviour
    {
        /// <summary>The field overlays: id, name, hotkey, unit and display multiplier, and the smallest range shown.</summary>
        public static readonly OverlayInfo[] Overlays =
        {
            new OverlayInfo("water_glucose", "Water glucose", "F7", "mol/m³", "mM", 1.0, 0.0, 0.0),
            new OverlayInfo("water_lactate", "Water lactate", "F8", "mol/m³", "mM", 1.0, 0.0, 0.0),
            new OverlayInfo("water_ph", "Water pH", "F9", "pH", "pH", 1.0, 0.0, 0.0),
        };

        // smallest span of the colour range per overlay, so rounding noise is not stretched over the whole scale
        private static readonly float[] MinSpan = { 1e-5f, 1e-5f, 1e-4f };

        private const int Size = SnapshotFrame.FieldRasterSize;
        private SessionBridge _bridge;
        private Texture2D _texture;
        private SpriteRenderer _renderer;
        private readonly Color32[] _pixels = new Color32[Size * Size];
        private long _drawn = -1;
        private int _drawnField = -1;

        /// <summary>The colour range now shown [display units], for the legend.</summary>
        public float RangeMin { get; private set; }

        public float RangeMax { get; private set; }

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            _texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = Sprite.Create(_texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            _renderer.sortingOrder = -40;   // under the sediment specks, the food and the cells
            _renderer.enabled = false;
        }

        /// <summary>The raster of overlay <paramref name="field"/> in the frame.</summary>
        public static float[] Raster(SnapshotFrame f, int field) =>
            field == 0 ? f.FieldGlucose : field == 1 ? f.FieldLactate : f.FieldPh;

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            int field = _bridge.ActiveField;
            if (f == null || field < 0 || field >= Overlays.Length || f.FieldExtent <= 0f)
            {
                _renderer.enabled = false;
                return;
            }
            if (f.Sequence != _drawn || field != _drawnField)
            {
                Paint(Raster(f, field), field);
                _drawn = f.Sequence;
                _drawnField = field;
            }
            // the raster is centred on the creature where the frame left it; the body is drawn carried forward from there
            float side = f.FieldExtent * CellView.UnitsPerMetre;
            transform.localPosition = new Vector3(_bridge.Shift.x, _bridge.Shift.y, 2f);
            transform.localScale = new Vector3(side, side, 1f);
            _renderer.enabled = true;
        }

        private void Paint(float[] raster, int field)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (float v in raster)
            {
                lo = Mathf.Min(lo, v);
                hi = Mathf.Max(hi, v);
            }
            float span = Mathf.Max(hi - lo, MinSpan[field]);
            float mid = 0.5f * (lo + hi);
            RangeMin = mid - 0.5f * span;
            RangeMax = mid + 0.5f * span;
            for (int k = 0; k < raster.Length; k++)
            {
                Color c = Colormap.Evaluate((raster[k] - RangeMin) / span);
                c.a = 0.85f;
                _pixels[k] = c;
            }
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }
    }
}
