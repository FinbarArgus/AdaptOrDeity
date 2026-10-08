using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The window on the right that opens when a cell is selected (ONI's side screen): the cell's metabolic rate, where
    /// its ATP comes from and goes, growth and storage, its state, and the controls of its group: protein expression,
    /// energy to growth, glycogen storage and repair, a Divide button and the unlocked features. Sliders change every
    /// cell in the selected cell's group. Everything shown comes from the snapshot schema. IMGUI for the vertical slice.
    /// </summary>
    public sealed class CellPanel : MonoBehaviour
    {
        public const float Width = 400f;
        private const float Top = 34f;

        // Colours of the ATP sinks in the stacked bar, by detail id.
        private static readonly Dictionary<string, Color> SinkColours = new Dictionary<string, Color>
        {
            ["atp_pumps"] = new Color(0.95f, 0.55f, 0.25f),
            ["atp_protein_turnover"] = new Color(0.40f, 0.70f, 0.95f),
            ["atp_protein_build"] = new Color(0.70f, 0.50f, 0.95f),
            ["atp_rna"] = new Color(0.35f, 0.85f, 0.75f),
            ["atp_lipids"] = new Color(0.95f, 0.85f, 0.35f),
            ["atp_repair"] = new Color(0.95f, 0.45f, 0.55f),
            ["atp_growth"] = new Color(0.55f, 0.90f, 0.40f),
            ["atp_storage"] = new Color(0.80f, 0.65f, 0.45f),
            ["atp_other"] = new Color(0.60f, 0.65f, 0.70f),
        };

        private Vector2 _scroll;
        private bool _showProteinTurnover;
        private bool _showFeatures = true;
        private float[] _sliders = new float[0];
        private float[] _controls = new float[0];
        private int _slidersForGroup = -1;
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
            int group = frame.CellGroup[cell];
            SyncSliders(schema, frame, cell, group);

            var area = new Rect(Screen.width - Width, Top, Width, Screen.height - Top);
            // Opaque: the camera does not draw under the panel.
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                    new Color(0.12f, 0.16f, 0.20f, 1f), 0f, 0f);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            GUILayout.BeginHorizontal();
            int alive = 0, inGroup = 0;
            for (int k = 0; k < frame.CellCount; k++)
                if (frame.CellGroup[k] == group)
                {
                    inGroup++;
                    if (frame.CellAlive[k])
                        alive++;
                }
            GUILayout.Label($"Cell {cell}  ·  group {group + 1} ({alive} of {inGroup} alive)", _header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(26)))
                bridge.SelectedCell = -1;
            GUILayout.EndHorizontal();
            if (!frame.CellAlive[cell])
                GUILayout.Label("This cell died: damage reached a lethal load.", _small);
            _scroll = GUILayout.BeginScrollView(_scroll);

            DrawMetabolicRate(schema, frame, cell);
            DrawGroup(schema, frame, cell, "ATP made");
            DrawGroup(schema, frame, cell, "ATP used");
            _showProteinTurnover = Foldout(_showProteinTurnover, "Protein turnover by protein");
            if (_showProteinTurnover)
                DrawGroup(schema, frame, cell, "Protein turnover", header: false);
            if (Unlocked(schema, frame, GameFeatures.GrowthAndDivision))
                DrawGrowth(bridge, schema, frame, cell);
            DrawGroup(schema, frame, cell, "Cell state");

            GUILayout.Space(10);
            GUILayout.Label("Protein expression", _header);
            GUILayout.Label("Copies relative to the baseline cell, for every cell in this group. New copies cost ATP to " +
                            "build (shown under ATP used) and a little each second to keep. The membrane holds only so " +
                            "many transporters.", _small);
            for (int t = 0; t < schema.Traits.Count; t++)
                DrawTraitSlider(bridge, schema.Traits[t], frame, cell, group, t);

            GUILayout.Space(10);
            GUILayout.Label("Energy", _header);
            for (int c = 0; c < schema.Controls.Count; c++)
                if (Active(schema, frame, schema.Controls[c].Feature))
                    DrawControl(bridge, schema.Controls[c], group, c);

            GUILayout.Space(6);
            _showFeatures = Foldout(_showFeatures, "Features (your choice once unlocked)");
            if (_showFeatures)
                DrawFeatures(bridge, schema, frame);

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
            double total = 0.0;
            for (int d = 0; d < schema.Details.Count; d++)
                if (SinkColours.ContainsKey(schema.Details[d].Id))
                    total += System.Math.Max(0.0, frame.Details[d][cell]);
            var bar = GUILayoutUtility.GetRect(Width - 40, 14);
            if (Event.current.type == EventType.Repaint && total > 0.0)
            {
                float x = bar.x;
                for (int d = 0; d < schema.Details.Count; d++)
                {
                    if (!SinkColours.TryGetValue(schema.Details[d].Id, out var colour))
                        continue;
                    float w = (float)(System.Math.Max(0.0, frame.Details[d][cell]) / total) * bar.width;
                    GUI.DrawTexture(new Rect(x, bar.y, w, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                        false, 0f, colour, 0f, 0f);
                    x += w;
                }
            }
            GUILayout.Space(4);
        }

        private void DrawGrowth(SessionBridge bridge, SessionSchema schema, SnapshotFrame frame, int cell)
        {
            GUILayout.Space(6);
            GUILayout.Label("Growth and storage", _header);
            int built = Find(schema, "growth_progress"), glyc = Find(schema, "glycogen_progress");
            float b = built >= 0 ? (float)frame.Details[built][cell] : 0f, g = glyc >= 0 ? (float)frame.Details[glyc][cell] : 0f;
            var bar = GUILayoutUtility.GetRect(Width - 40, 16);
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.2f, 0.25f, 0.3f), 0f, 0f);
                float wb = Mathf.Clamp01(b) * bar.width, wg = Mathf.Clamp01(g) * bar.width;
                GUI.DrawTexture(new Rect(bar.x, bar.y, wb, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                    false, 0f, SinkColours["atp_growth"], 0f, 0f);
                GUI.DrawTexture(new Rect(bar.x + wb, bar.y, Mathf.Min(wg, bar.width - wb), bar.height), Texture2D.whiteTexture,
                    ScaleMode.StretchToFill, false, 0f, SinkColours["atp_storage"], 0f, 0f);
            }
            GUILayout.Label($"Daughter {100 * b:F2}% built + glycogen {100 * g:F2}%", _small);
            for (int d = 0; d < schema.Details.Count; d++)
                if (schema.Details[d].Group == "Growth and storage" && d != built && d != glyc)
                    Row(schema, frame, cell, d);
            GUI.enabled = frame.CellCanDivide[cell];
            if (GUILayout.Button(frame.CellCanDivide[cell] ? "Divide" : "Divide (daughter not built yet)"))
                bridge.Send(SimCommands.Divide(cell));
            GUI.enabled = true;
            if (Application.isEditor && frame.CellAlive[cell] && !frame.CellCanDivide[cell]
                && GUILayout.Button("Dev: finish daughter (editor only, skips the ATP)"))
                bridge.Send(SimCommands.DevCompleteDaughter(cell));
        }

        private void DrawGroup(SessionSchema schema, SnapshotFrame frame, int cell, string group, bool header = true)
        {
            if (header)
            {
                GUILayout.Space(6);
                GUILayout.Label(group, _header);
            }
            for (int d = 0; d < schema.Details.Count; d++)
            {
                if (schema.Details[d].Group != group)
                    continue;
                // Hide sinks and sources that are zero (e.g. growth before level 2) to keep the list short.
                if ((group == "ATP used" || group == "ATP made") && schema.Details[d].Id != "atp_net"
                    && frame.Details[d][cell] == 0.0)
                    continue;
                Color? swatch = SinkColours.TryGetValue(schema.Details[d].Id, out var c) ? c : (Color?)null;
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
            GUILayout.Label(new GUIContent(info.Name, info.Description), GUILayout.Width(210));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{Format(Display(schema, frame, cell, d))} {info.DisplayUnit}", _right);
            GUILayout.EndHorizontal();
        }

        private void DrawTraitSlider(SessionBridge bridge, TraitInfo trait, SnapshotFrame frame, int cell, int group, int t)
        {
            float current = frame.TraitValues[t][cell], target = frame.TraitTargets[t][cell];
            GUILayout.BeginHorizontal();
            GUILayout.Label(trait.Label, GUILayout.Width(240));
            GUILayout.FlexibleSpace();
            string status = Mathf.Abs(current - target) > 1e-3f ? $"×{current:F2} → ×{target:F2}" : $"×{current:F2}";
            GUILayout.Label(status, _right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float lo = Mathf.Log((float)trait.Min), hi = Mathf.Log((float)trait.Max);
            float moved = GUILayout.HorizontalSlider(Mathf.Log(_sliders[t]), lo, hi, GUILayout.Width(Width - 90));
            if (GUILayout.Button("1×", GUILayout.Width(34)))
                moved = 0f;
            GUILayout.EndHorizontal();
            float value = Mathf.Exp(moved);
            if (Mathf.Abs(value - _sliders[t]) > 1e-4f)
            {
                _sliders[t] = value;
                bridge.Send(SimCommands.SetTrait(group, t, value));
            }
            else if (Mathf.Abs(_sliders[t] - target) > 1e-3f && GUIUtility.hotControl == 0)
                _sliders[t] = target;   // the membrane ceiling clamped the request: show what was accepted
        }

        private void DrawControl(SessionBridge bridge, ControlInfo control, int group, int c)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(control.Label, control.Description), GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_controls[c]:F2} {control.Unit}", _right);
            GUILayout.EndHorizontal();
            float moved = GUILayout.HorizontalSlider(_controls[c], (float)control.Min, (float)control.Max, GUILayout.Width(Width - 50));
            if (Mathf.Abs(moved - _controls[c]) > 1e-4f)
            {
                _controls[c] = moved;
                bridge.Send(SimCommands.SetControl(group, c, moved));
            }
        }

        private void DrawFeatures(SessionBridge bridge, SessionSchema schema, SnapshotFrame frame)
        {
            for (int f = 0; f < schema.Features.Count; f++)
            {
                var info = schema.Features[f];
                if (!frame.FeatureUnlocked[f])
                {
                    GUILayout.Label($"{info.Name}: unlocks at level {info.UnlockLevel}", _small);
                    continue;
                }
                if (!info.Optional)
                {
                    GUILayout.Label($"{info.Name}: always on", _small);
                    continue;
                }
                bool on = GUILayout.Toggle(frame.FeatureEnabled[f], new GUIContent(info.Name, info.Effect));
                if (on != frame.FeatureEnabled[f])
                    bridge.Send(SimCommands.SetFeature(f, on));
            }
        }

        private void SyncSliders(SessionSchema schema, SnapshotFrame frame, int cell, int group)
        {
            if (_sliders.Length == schema.Traits.Count && _slidersForGroup == group)
                return;
            _sliders = new float[schema.Traits.Count];
            for (int t = 0; t < _sliders.Length; t++)
                _sliders[t] = frame.TraitTargets[t][cell];
            _controls = new float[schema.Controls.Count];
            for (int c = 0; c < _controls.Length; c++)
                _controls[c] = frame.Controls[c][group];
            _slidersForGroup = group;
        }

        private static bool Unlocked(SessionSchema schema, SnapshotFrame frame, string feature)
        {
            for (int f = 0; f < schema.Features.Count; f++)
                if (schema.Features[f].Id == feature)
                    return frame.FeatureUnlocked[f] && frame.FeatureEnabled[f];
            return false;
        }

        private static bool Active(SessionSchema schema, SnapshotFrame frame, string feature) => Unlocked(schema, frame, feature);

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
