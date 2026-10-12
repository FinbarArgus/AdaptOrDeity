using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The skill window: features with a cost (<see cref="FeatureInfo.Cost"/>) bought with skill points, earned by food
    /// eaten and by daughters that leave the creature (reproduction). It opens when the level goes up and there is something
    /// new to buy, and from the Skills button. Each entry shows its short effect, what it still needs (a feature, prey
    /// swallowed) and which path it closes; buying a feature that closes another asks first. Its full explanation opens
    /// under it.
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

        /// <summary>Features of the current level or earlier that can still be bought (not closed by a choice made).</summary>
        public static int ForSale(SessionSchema schema, SnapshotFrame frame)
        {
            int n = 0;
            for (int f = 0; f < schema.Features.Count; f++)
                if (schema.Features[f].Cost > 0 && schema.Features[f].UnlockLevel <= frame.Level && !frame.FeatureUnlocked[f]
                    && ClosedBy(schema, frame, f) == null)
                    n++;
            return n;
        }

        private static int Index(SessionSchema schema, string id)
        {
            for (int f = 0; f < schema.Features.Count; f++)
                if (schema.Features[f].Id == id)
                    return f;
            return -1;
        }

        /// <summary>The name of the feature whose choice closed feature <paramref name="f"/>, or null.</summary>
        private static string ClosedBy(SessionSchema schema, SnapshotFrame frame, int f)
        {
            foreach (var other in schema.Features[f].Excludes)
            {
                int o = Index(schema, other);
                if (o >= 0 && frame.FeatureUnlocked[o])
                    return schema.Features[o].Name;
            }
            return null;
        }

        /// <summary>The first required feature not yet unlocked, or null.</summary>
        private static string Missing(SessionSchema schema, SnapshotFrame frame, int f)
        {
            foreach (var other in schema.Features[f].Requires)
            {
                int o = Index(schema, other);
                if (o >= 0 && !frame.FeatureUnlocked[o])
                    return schema.Features[o].Name;
            }
            return null;
        }

        private static string Names(SessionSchema schema, System.Collections.Generic.IReadOnlyList<string> ids)
        {
            var names = new List<string>();
            foreach (var id in ids)
            {
                int o = Index(schema, id);
                names.Add(o >= 0 ? schema.Features[o].Name : id);
            }
            return string.Join(", ", names);
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

        private int _confirm = -1;   // an exclusive feature waiting for the player to confirm

        private void Draw(SessionBridge bridge, SnapshotFrame frame)
        {
            var schema = bridge.Schema;
            var features = schema.Features;
            GUILayout.Label($"Skill points: {frame.SkillPoints} to spend", _title);
            GUILayout.Label($"Earned so far: food {frame.FoodPoints}, reproduction {frame.ReproductionPoints} " +
                            $"({frame.Offspring} daughters left on their own: {frame.OwnOffspring} your creature's, " +
                            $"{frame.Offspring - frame.OwnOffspring} theirs). Eaten: {frame.FoodEaten * 6.02214076e23:G3} glucose " +
                            $"molecules. Prey swallowed: {frame.Swallowed}.", _wrap);
            if (_confirm >= 0)
            {
                DrawConfirm(bridge, frame, _confirm);
                return;
            }
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int f = 0; f < features.Count; f++)
            {
                var info = features[f];
                if (info.Cost == 0)
                    continue;
                bool reached = info.UnlockLevel <= frame.Level, owned = frame.FeatureUnlocked[f];
                string closedBy = owned ? null : ClosedBy(schema, frame, f), missing = owned ? null : Missing(schema, frame, f);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label(info.Name, _title);
                GUILayout.FlexibleSpace();
                if (owned)
                    GUILayout.Label("Evolved");
                else if (closedBy != null)
                    GUILayout.Label($"Closed: you chose {closedBy}");
                else if (!reached)
                    GUILayout.Label($"Level {info.UnlockLevel}");
                else if (missing != null)
                    GUILayout.Label($"Needs {missing}");
                else if (frame.Swallowed < info.RequiresSwallowed)
                    GUILayout.Label($"Swallowed {frame.Swallowed}/{info.RequiresSwallowed}");
                else
                {
                    GUI.enabled = frame.SkillPoints >= info.Cost;
                    if (GUILayout.Button($"Evolve ({info.Cost} points)", GUILayout.Width(150)))
                    {
                        if (info.Excludes.Count > 0)
                            _confirm = f;   // a choice that closes another path: ask first
                        else
                            bridge.Send(SimCommands.BuyFeature(f));
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
                GUILayout.Label(info.Effect, _wrap);
                if (info.RequiresSwallowed > 0 && !owned)
                    GUILayout.Label($"Needs {info.RequiresSwallowed} prey cells swallowed: {Mathf.Min(frame.Swallowed, info.RequiresSwallowed)}" +
                                    $"/{info.RequiresSwallowed} so far.", _wrap);
                if (info.Excludes.Count > 0 && !owned && closedBy == null)
                    GUILayout.Label($"Choosing this closes {Names(schema, info.Excludes)} for good.", _wrap);
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

        private void DrawConfirm(SessionBridge bridge, SnapshotFrame frame, int f)
        {
            var info = bridge.Schema.Features[f];
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Evolve {info.Name}?", _title);
            GUILayout.Label($"This closes {Names(bridge.Schema, info.Excludes)} for good: the two are separate paths, and " +
                            "you cannot take the other one later.", _wrap);
            GUILayout.BeginHorizontal();
            GUI.enabled = frame.SkillPoints >= info.Cost;
            if (GUILayout.Button($"Evolve {info.Name} ({info.Cost} points)"))
            {
                bridge.Send(SimCommands.BuyFeature(f));
                _confirm = -1;
            }
            GUI.enabled = true;
            if (GUILayout.Button("Back"))
                _confirm = -1;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }
    }
}
