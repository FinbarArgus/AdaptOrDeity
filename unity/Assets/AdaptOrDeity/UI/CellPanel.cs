using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The window on the right that opens when a cell is selected (ONI's side screen): the cell's metabolic rate, where
    /// its ATP comes from and goes, its state, and sliders to adapt its proteins: expression for each transport protein,
    /// and, behind a drop-down, the kinetic midpoints. Everything shown comes from the snapshot schema, so the panel
    /// needs no knowledge of the model. IMGUI for the vertical slice.
    /// </summary>
    public sealed class CellPanel : MonoBehaviour
    {
        public const float Width = 380f;
        private const float Top = 34f;

        private static readonly Color[] SinkColours =
        {
            new Color(0.95f, 0.55f, 0.25f), // H+-ATPase
            new Color(0.40f, 0.70f, 0.95f), // protein turnover
            new Color(0.70f, 0.50f, 0.95f), // building protein
            new Color(0.60f, 0.65f, 0.70f), // other maintenance
        };

        private Vector2 _scroll;
        private bool _showMidpoints;
        private bool _showProteinTurnover;
        private float[] _sliders = new float[0];
        private int _slidersForCell = -1;
        private GUIStyle _header, _big, _small, _right;

        /// <summary>True while the panel is drawn (other HUD elements move left of it).</summary>
        public static bool Visible { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<CellPanel>() == null)
                new GameObject("CellPanel").AddComponent<CellPanel>();
        }

        private void OnGUI()
        {
            var bridge = SessionBridge.Instance;
            var frame = bridge != null ? bridge.Frame : null;
            int cell = bridge != null ? bridge.SelectedCell : -1;
            Visible = frame != null && cell >= 0 && cell < frame.CellCount && bridge.Schema.Details.Count > 0;
            if (!Visible)
                return;
            EnsureStyles();
            var schema = bridge.Schema;
            SyncSliders(schema, frame, cell);

            var area = new Rect(Screen.width - Width, Top, Width, Screen.height - Top);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Cell {cell}", _header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(26)))
                bridge.SelectedCell = -1;
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawMetabolicRate(schema, frame, cell);
            DrawGroup(schema, frame, cell, "ATP made");
            DrawGroup(schema, frame, cell, "ATP used");
            _showProteinTurnover = Foldout(_showProteinTurnover, "Protein turnover by protein");
            if (_showProteinTurnover)
                DrawGroup(schema, frame, cell, "Protein turnover", header: false);
            DrawGroup(schema, frame, cell, "Cell state");

            GUILayout.Space(10);
            GUILayout.Label("Protein expression", _header);
            GUILayout.Label("Copies relative to the baseline cell. Raising expression costs ATP to build the new copies " +
                            "(shown under ATP used) and a little more each second to keep them.", _small);
            for (int t = 0; t < schema.Traits.Count; t++)
                if (schema.Traits[t].IsExpression)
                    DrawTraitSlider(bridge, schema.Traits[t], frame, cell, t);

            GUILayout.Space(6);
            _showMidpoints = Foldout(_showMidpoints, "Kinetics midpoints (±10%)");
            if (_showMidpoints)
            {
                GUILayout.Label("Half-saturation and half-activation constants. These change at once.", _small);
                for (int t = 0; t < schema.Traits.Count; t++)
                    if (!schema.Traits[t].IsExpression)
                        DrawTraitSlider(bridge, schema.Traits[t], frame, cell, t);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawMetabolicRate(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            GUILayout.Label("Metabolic rate", _header);
            int turnover = Find(schema, "atp_turnover");
            if (turnover >= 0)
                GUILayout.Label($"{Display(schema, frame, cell, turnover):N0} ATP/s", _big);
            for (int d = 0; d < schema.Details.Count; d++)
                if (schema.Details[d].Group == "Metabolic rate" && d != turnover)
                    Row(schema, frame, cell, d);

            // Where the ATP goes, as one stacked bar.
            string[] sinks = { "atp_pumps", "atp_protein_turnover", "atp_protein_build", "atp_other" };
            double total = 0.0;
            var values = new double[sinks.Length];
            for (int i = 0; i < sinks.Length; i++)
            {
                int d = Find(schema, sinks[i]);
                values[i] = d >= 0 ? System.Math.Max(0.0, frame.Details[d][cell]) : 0.0;
                total += values[i];
            }
            var bar = GUILayoutUtility.GetRect(Width - 40, 14);
            if (Event.current.type == EventType.Repaint && total > 0.0)
            {
                float x = bar.x;
                for (int i = 0; i < sinks.Length; i++)
                {
                    float w = (float)(values[i] / total) * bar.width;
                    GUI.DrawTexture(new Rect(x, bar.y, w, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                        false, 0f, SinkColours[i], 0f, 0f);
                    x += w;
                }
            }
            GUILayout.Space(4);
        }

        private void DrawGroup(SessionSchema schema, SnapshotFrame frame, int cell, string group, bool header = true)
        {
            if (header)
            {
                GUILayout.Space(6);
                GUILayout.Label(group, _header);
            }
            int sink = 0;
            for (int d = 0; d < schema.Details.Count; d++)
            {
                if (schema.Details[d].Group != group)
                    continue;
                Color? swatch = null;
                if (group == "ATP used" && schema.Details[d].Id != "atp_net" && sink < SinkColours.Length)
                    swatch = SinkColours[sink++];
                Row(schema, frame, cell, d, swatch);
            }
        }

        private void Row(SessionSchema schema, SnapshotFrame frame, int cell, int d, Color? swatch = null)
        {
            var info = schema.Details[d];
            GUILayout.BeginHorizontal();
            if (swatch.HasValue)
            {
                var r = GUILayoutUtility.GetRect(10, 10, GUILayout.Width(10));
                if (Event.current.type == EventType.Repaint)
                    GUI.DrawTexture(new Rect(r.x, r.y + 5, 10, 10), Texture2D.whiteTexture, ScaleMode.StretchToFill, false,
                        0f, swatch.Value, 0f, 0f);
            }
            GUILayout.Label(new GUIContent(info.Name, info.Description), GUILayout.Width(190));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{Format(Display(schema, frame, cell, d))} {info.DisplayUnit}", _right);
            GUILayout.EndHorizontal();
        }

        private void DrawTraitSlider(SessionBridge bridge, TraitInfo trait, SnapshotFrame frame, int cell, int t)
        {
            float current = frame.TraitValues[t][cell], target = frame.TraitTargets[t][cell];
            GUILayout.BeginHorizontal();
            GUILayout.Label(trait.Label, GUILayout.Width(230));
            GUILayout.FlexibleSpace();
            string status = Mathf.Abs(current - target) > 1e-3f ? $"×{current:F2} → ×{target:F2}" : $"×{current:F2}";
            GUILayout.Label(status, _right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float lo = trait.LogScale ? Mathf.Log((float)trait.Min) : (float)trait.Min;
            float hi = trait.LogScale ? Mathf.Log((float)trait.Max) : (float)trait.Max;
            float position = trait.LogScale ? Mathf.Log(_sliders[t]) : _sliders[t];
            float moved = GUILayout.HorizontalSlider(position, lo, hi, GUILayout.Width(Width - 90));
            if (GUILayout.Button("1×", GUILayout.Width(34)))
                moved = trait.LogScale ? 0f : 1f;
            GUILayout.EndHorizontal();
            float value = trait.LogScale ? Mathf.Exp(moved) : moved;
            if (Mathf.Abs(value - _sliders[t]) > 1e-4f)
            {
                _sliders[t] = value;
                bridge.Send(SimCommands.SetTrait(cell, t, value));
            }
        }

        private void SyncSliders(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            if (_sliders.Length == schema.Traits.Count && _slidersForCell == cell)
                return;
            _sliders = new float[schema.Traits.Count];
            for (int t = 0; t < _sliders.Length; t++)
                _sliders[t] = frame.TraitTargets[t][cell];
            _slidersForCell = cell;
        }

        private static bool Foldout(bool open, string label) =>
            GUILayout.Toggle(open, (open ? "▾ " : "▸ ") + label, GUI.skin.label);

        private static int Find(SessionSchema schema, string id)
        {
            IReadOnlyList<DetailInfo> details = schema.Details;
            for (int d = 0; d < details.Count; d++)
                if (details[d].Id == id)
                    return d;
            return -1;
        }

        private static double Display(SessionSchema schema, SnapshotFrame frame, int cell, int d) =>
            frame.Details[d][cell] * schema.Details[d].DisplayMultiplier;

        private static string Format(double v)
        {
            double a = System.Math.Abs(v);
            if (a >= 1e4)
                return v.ToString("N0");
            if (a >= 100)
                return v.ToString("F0");
            if (a >= 1)
                return v.ToString("F2");
            return v.ToString("G3");
        }

        private void EnsureStyles()
        {
            if (_header != null)
                return;
            _header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            _big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            _right = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
        }
    }
}
