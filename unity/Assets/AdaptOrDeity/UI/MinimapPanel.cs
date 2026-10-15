using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The minimap, bottom left of the view above the current: the tiles around the creature it has had in view, coloured by
    /// the water it saw there when it last did (glucose, lactate or pH, chosen with the buttons), rock and ground in grey,
    /// unexplored tiles dark, older sightings faded; the creature is the white mark. Drawn by the <see cref="Hud"/>.
    /// </summary>
    public sealed class MinimapPanel
    {
        private const float Width = 220f, MapSide = 208f;
        private static readonly string[] Species = { "Glucose", "Lactate", "pH" };
        private static readonly Color Unexplored = new Color(0.07f, 0.08f, 0.11f, 0.92f), Rock = new Color(0.46f, 0.42f, 0.37f, 1f);
        private const float FadeTime = 3600f;   // a sighting an hour old is drawn halfway to the unexplored colour

        private bool _open = true;
        private int _species;
        private Texture2D _texture;
        private readonly Color32[] _pixels = new Color32[SnapshotFrame.MinimapSize * SnapshotFrame.MinimapSize];
        private GUIStyle _small;
        private Rect _area;
        private float _min, _max;
        private bool _log;

        /// <summary>Whether the minimap is drawn over this GUI point (clicks there are the map's).</summary>
        public bool Covers(Vector2 guiPoint) => _area.Contains(guiPoint);

        /// <summary>Draws the minimap with its bottom edge at <paramref name="bottom"/> (GUI y).</summary>
        public void Draw(SnapshotFrame frame, float bottom)
        {
            _area = default;
            if (frame.MinimapTile <= 0f)
                return;
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };
            float height = _open ? MapSide + 66f : 24f;
            _area = new Rect(8f, bottom - height, Width, height);
            GUI.Box(_area, GUIContent.none);
            string title = $"Map: {frame.ExploredTiles} tiles of {1e6f * frame.MinimapTile:0} µm explored";
            _open = GUI.Toggle(new Rect(_area.x + 6, _area.y + 2, Width - 12, 20), _open, (_open ? "▾ " : "▸ ") + title,
                GUI.skin.label);
            if (!_open)
                return;
            _species = GUI.Toolbar(new Rect(_area.x + 6, _area.y + 24, Width - 12, 18), _species, Species);
            var map = new Rect(_area.x + 6, _area.y + 46, MapSide, MapSide);
            if (Event.current.type == EventType.Repaint)
            {
                Paint(frame);
                GUI.DrawTexture(map, _texture, ScaleMode.StretchToFill, true);
                // the creature: a white mark where it is on the raster (y up on the raster, down on the screen)
                int m = SnapshotFrame.MinimapSize;
                float u = -frame.MinimapOriginX / (frame.MinimapTile * m), v = -frame.MinimapOriginY / (frame.MinimapTile * m);
                var mark = new Rect(map.x + u * map.width - 3f, map.yMax - v * map.height - 3f, 6f, 6f);
                GUI.DrawTexture(mark, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
            }
            GUI.Label(new Rect(_area.x + 6, map.yMax + 1, Width - 12, 18), Legend(), _small);
        }

        // Colours each tile: unexplored dark, solid grey, water by the chosen value on the range seen on the map, faded
        // with the sighting's age.
        private void Paint(SnapshotFrame frame)
        {
            int m = SnapshotFrame.MinimapSize;
            if (_texture == null)
                _texture = new Texture2D(m, m, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var values = _species == 0 ? frame.MinimapGlucose : _species == 1 ? frame.MinimapLactate : frame.MinimapPh;
            _min = float.MaxValue;
            _max = float.MinValue;
            for (int k = 0; k < m * m; k++)
                if (frame.MinimapExplored[k] && !frame.MinimapSolid[k] && !float.IsNaN(values[k]))
                {
                    _min = Mathf.Min(_min, values[k]);
                    _max = Mathf.Max(_max, values[k]);
                }
            // concentrations spanning more than a decade are shown on a log scale
            _log = _species < 2 && _min > 0f && _max > 10f * _min;
            float lo = _log ? Mathf.Log10(_min) : _min, hi = _log ? Mathf.Log10(_max) : _max;
            for (int k = 0; k < m * m; k++)
            {
                Color c;
                if (!frame.MinimapExplored[k])
                    c = Unexplored;
                else if (frame.MinimapSolid[k])
                    c = Rock;
                else if (float.IsNaN(values[k]) || _min > _max)
                    c = Colormap.Evaluate(0.5f);
                else
                {
                    float x = _log ? Mathf.Log10(Mathf.Max(values[k], 1e-30f)) : values[k];
                    c = Colormap.Evaluate(hi > lo ? Mathf.Clamp01((x - lo) / (hi - lo)) : 0.5f);
                }
                if (frame.MinimapExplored[k])
                    c = Color.Lerp(c, Unexplored, 0.5f * frame.MinimapAge[k] / (frame.MinimapAge[k] + FadeTime));
                _pixels[k] = c;
            }
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private string Legend()
        {
            if (_min > _max)
                return "nothing seen yet";
            string unit = _species < 2 ? " mM" : "";
            string range = _species < 2 ? $"{_min:G2}-{_max:G2}{unit}" : $"pH {_min:0.00}-{_max:0.00}";
            return $"{Species[_species]} last seen: {range}" + (_log ? " (log)" : "");
        }

        /// <summary>Frees the map's texture.</summary>
        public void Dispose()
        {
            if (_texture != null)
                Object.Destroy(_texture);
        }
    }
}
