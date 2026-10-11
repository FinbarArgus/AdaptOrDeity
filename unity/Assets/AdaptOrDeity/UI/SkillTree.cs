using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The skill window: features with a cost (<see cref="FeatureInfo.Cost"/>) bought with the skill points that food
    /// eaten earns. It opens when the level goes up and there is something new to buy, and from the Skills button.
    /// Each entry shows its short effect; its full explanation opens under it.
    /// </summary>
    public sealed class SkillTree : MonoBehaviour
    {
        private const float Width = 520f, Height = 460f;
        private static SkillTree _instance;

        private bool _open;
        private int _seenLevel = -1;
        private Vector2 _scroll;
        private readonly HashSet<int> _expanded = new HashSet<int>();
        private GUIStyle _wrap, _title;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SkillTree>() == null)
                new GameObject("SkillTree").AddComponent<SkillTree>();
        }

        private void Awake() => _instance = this;

        /// <summary>Opens or closes the window (the HUD's Skills button).</summary>
        public static void Toggle()
        {
            if (_instance != null)
                _instance._open = !_instance._open;
        }

        /// <summary>Features of the current level or earlier that can still be bought.</summary>
        public static int ForSale(SessionSchema schema, SnapshotFrame frame)
        {
            int n = 0;
            for (int f = 0; f < schema.Features.Count; f++)
                if (schema.Features[f].Cost > 0 && schema.Features[f].UnlockLevel <= frame.Level && !frame.FeatureUnlocked[f])
                    n++;
            return n;
        }

        private void OnGUI()
        {
            var bridge = SessionBridge.Instance;
            var frame = bridge != null ? bridge.Frame : null;
            if (frame == null || bridge.Schema.Features.Count == 0)
                return;
            if (_seenLevel >= 0 && frame.Level > _seenLevel && ForSale(bridge.Schema, frame) > 0)
                _open = true;   // a new level: spend the points earned
            _seenLevel = frame.Level;
            if (!_open)
                return;
            _wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 };
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            var rect = new Rect((Screen.width - Width) / 2f, (Screen.height - Height) / 2f, Width, Height);
            GUILayout.Window(0x5C111, rect, id => Draw(bridge, frame), "Evolve");
        }

        private void Draw(SessionBridge bridge, SnapshotFrame frame)
        {
            var features = bridge.Schema.Features;
            GUILayout.Label($"Skill points: {frame.SkillPoints}", _title);
            GUILayout.Label($"Earned by eating: {frame.FoodEaten * 6.02214076e23:G3} glucose molecules from food so far.", _wrap);
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int f = 0; f < features.Count; f++)
            {
                var info = features[f];
                if (info.Cost == 0)
                    continue;
                bool reached = info.UnlockLevel <= frame.Level, owned = frame.FeatureUnlocked[f];
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label(info.Name, _title);
                GUILayout.FlexibleSpace();
                if (owned)
                    GUILayout.Label("Evolved");
                else if (!reached)
                    GUILayout.Label($"Level {info.UnlockLevel}");
                else
                {
                    GUI.enabled = frame.SkillPoints >= info.Cost;
                    if (GUILayout.Button($"Evolve ({info.Cost} points)", GUILayout.Width(150)))
                        bridge.Send(SimCommands.BuyFeature(f));
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(info.Effect, _wrap);
                if (info.Explanation.Length > 0)
                {
                    bool open = _expanded.Contains(f);
                    if (GUILayout.Button(open ? "Less" : "Learn more", GUILayout.Width(100)))
                    {
                        if (open)
                            _expanded.Remove(f);
                        else
                            _expanded.Add(f);
                    }
                    if (open)
                        GUILayout.Label(info.Explanation, _wrap);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close"))
                _open = false;
        }
    }
}
