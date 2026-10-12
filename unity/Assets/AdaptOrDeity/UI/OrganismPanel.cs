using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// A read-only window on the left for the selected free organism (a daughter that left the creature, or one of its
    /// offspring): whether it lives and swims, then its cell's details grouped as in the <see cref="CellPanel"/>. It has
    /// no controls: it keeps the ones it had when it left. IMGUI for the vertical slice.
    /// </summary>
    public sealed class OrganismPanel : MonoBehaviour
    {
        private const float Width = 340f, Top = 44f;

        private Vector2 _scroll;
        private GUIStyle _header, _small, _right;
        private readonly HashSet<string> _open = new HashSet<string> { "Metabolic rate", "Cell state", "Growth and storage" };
        private static Rect _area;
        private static bool _visible;

        /// <summary>Whether the panel is drawn over this GUI point (clicks there are the panel's).</summary>
        public static bool Covers(Vector2 guiPoint) => _visible && _area.Contains(guiPoint);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<OrganismPanel>() == null)
                new GameObject("OrganismPanel").AddComponent<OrganismPanel>();
        }

        private void OnGUI()
        {
            var bridge = SessionBridge.Instance;
            var frame = bridge != null ? bridge.Frame : null;
            int id = bridge != null ? bridge.SelectedOrganism : -1;
            _visible = frame != null && id >= 0;
            if (!_visible)
                return;
            EnsureStyles();
            int slot = -1;
            for (int k = 0; k < frame.OrganismCount; k++)
                if (frame.OrganismId[k] == id)
                    slot = k;
            var area = new Rect(8f, Top, Width, Mathf.Min(560f, Screen.height - Top - 8f));
            _area = area;
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(area, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                    new Color(0.12f, 0.16f, 0.20f, 0.95f), 0f, 0f);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            GUILayout.BeginHorizontal();
            GUILayout.Label(slot >= 0 ? $"Free organism {id}  ·  generation {frame.OrganismGeneration[slot]}"
                : $"Free organism {id}", _header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(26)))
                bridge.SelectedOrganism = -1;
            GUILayout.EndHorizontal();
            if (slot < 0)
            {
                GUILayout.Label("No longer simulated: it swam or drifted out of range, or was removed.", _small);
                GUILayout.EndArea();
                return;
            }
            GUILayout.Label((frame.OrganismGeneration[slot] > 1 ? "Offspring of a daughter that left your creature" :
                                "A daughter that left your creature") +
                            ". It is not yours to control: it keeps the traits and energy controls it had when it left.",
                _small);
            Row("State", frame.OrganismAlive[slot] ? (frame.OrganismMotorOn[slot] ? "alive, swimming" : "alive")
                : "dead (damage reached a lethal load)");
            var overlays = bridge.Schema.Overlays;
            for (int c = 0; c < overlays.Count; c++)
                Row(overlays[c].Name,
                    $"{Format(frame.OrganismChannels[c][slot] * overlays[c].DisplayMultiplier)} {overlays[c].DisplayUnit}");
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (frame.InspectedOrganism == id)
                DrawDetails(bridge.Schema, frame);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // Every detail of the organism's cell, by group; groups fold open and closed.
        private void DrawDetails(SessionSchema schema, SnapshotFrame frame)
        {
            string group = null;
            bool open = false;
            for (int d = 0; d < schema.Details.Count; d++)
            {
                var info = schema.Details[d];
                if (info.Group != group)
                {
                    group = info.Group;
                    open = _open.Contains(group);
                    bool now = GUILayout.Toggle(open, (open ? "▾ " : "▸ ") + group, GUI.skin.label);
                    if (now != open)
                    {
                        if (now)
                            _open.Add(group);
                        else
                            _open.Remove(group);
                        open = now;
                    }
                }
                if (open)
                    Row(info.Name, $"{Format(frame.OrganismDetails[d] * info.DisplayMultiplier)} {info.DisplayUnit}");
            }
        }

        private void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _small);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, _right);
            GUILayout.EndHorizontal();
        }

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
            _small = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            _right = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
        }
    }
}
