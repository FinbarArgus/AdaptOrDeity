using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// The level-end window, shown when a level's time runs out (before the skill window): the level's stars, its survival
    /// and damage meters and the points they converted to, its reproduction points, the achievements earned in it, what was
    /// unlocked during it and what becomes available with the next level, and the living things the creature saw in it,
    /// each with a small drawing and a count. Everything comes from the frame's record of the last finished level.
    /// </summary>
    public sealed class LevelEndWindow : MonoBehaviour
    {
        private const float Width = 560f, Height = 520f;
        private static LevelEndWindow _instance;

        private bool _open;
        private int _shownLevel;    // the last scored level the window opened for (0: none yet)
        private int _generation;    // the session it is for (SessionBridge.Generation)
        private Vector2 _scroll;
        private GUIStyle _wrap, _title, _stars;
        private Texture2D _disc, _ring;

        /// <summary>Whether the window is open (the skill window waits for it).</summary>
        public static bool IsOpen => _instance != null && _instance._open;

        private static readonly string[] SeenNames = { "Food packets", "Dead cells", "Prey cells", "Your daughters", "Their descendants" };

        private static readonly string[] SeenTips =
        {
            "particles of dead cells and organic matter", "the bodies of cells that died: food", "small cells your cells can swallow",
            "daughters that swam off on their own", "the daughters of your daughters, and theirs",
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<LevelEndWindow>() == null)
                new GameObject("LevelEndWindow").AddComponent<LevelEndWindow>();
        }

        private void Awake() => _instance = this;

        private void OnGUI()
        {
            var bridge = SessionBridge.Instance;
            var frame = bridge != null ? bridge.Frame : null;
            if (frame == null || bridge.Schema.Features.Count == 0)
                return;
            if (_generation != bridge.Generation)
            {
                _generation = bridge.Generation;
                _shownLevel = 0;
                _open = false;
            }
            if (frame.ScoredLevel > _shownLevel)
            {
                _shownLevel = frame.ScoredLevel;
                _open = true;
                _scroll = Vector2.zero;
            }
            if (!_open)
                return;
            _wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 };
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            _stars ??= new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            var rect = new Rect((Screen.width - Width) / 2f, (Screen.height - Height) / 2f, Width, Height);
            GUILayout.Window(0x5C112, rect, id => Draw(bridge, frame), $"Level {frame.ScoredLevel} complete");
        }

        private void Draw(SessionBridge bridge, SnapshotFrame frame)
        {
            int points = frame.ScoredSurvivalPoints + frame.ScoredDamagePoints + frame.ScoredReproductionPoints;
            var old = GUI.color;
            GUI.color = new Color(1f, 0.85f, 0.3f);
            GUILayout.Label(Stars(frame.ScoredStars), _stars);
            GUI.color = old;
            GUILayout.Label($"{points} skill points earned in level {frame.ScoredLevel}", _title);
            _scroll = GUILayout.BeginScrollView(_scroll);

            Section("Score");
            Meter("Survival", frame.ScoredSurvival, frame.ScoredSurvivalPoints,
                "falls while your cells are starving (their ATP below half its resting level)");
            Meter("Damage", frame.ScoredDamage, frame.ScoredDamagePoints,
                "100 less the worst damage your cells carried during the level, as a share of a lethal load");
            GUILayout.Label($"Reproduction: {frame.ScoredReproductionPoints} points from daughters released in your lineage", _wrap);

            Section("Achievements");
            int earned = 0;
            for (int a = 0; a < GameAchievements.All.Count && a < frame.Achieved.Length; a++)
            {
                if (frame.Achieved[a] != frame.ScoredLevel)
                    continue;
                earned++;
                var info = GameAchievements.All[a];
                GUILayout.Label($"★ {info.Name}: {info.Description}", _wrap);
            }
            if (earned == 0)
                GUILayout.Label("None this level.", _wrap);

            Section("Unlocked");
            var features = bridge.Schema.Features;
            var unlocked = new List<string>();
            var next = new List<string>();
            for (int f = 0; f < features.Count; f++)
            {
                if (f < frame.ScoredUnlocked.Length && frame.ScoredUnlocked[f])
                    unlocked.Add(features[f].Name);
                if (features[f].UnlockLevel == frame.ScoredLevel + 1)
                    next.Add(features[f].Cost > 0 ? $"{features[f].Name} ({features[f].Cost} points)" : features[f].Name);
            }
            GUILayout.Label(unlocked.Count > 0 ? "During the level: " + string.Join(", ", unlocked) : "Nothing new during the level.", _wrap);
            if (next.Count > 0)
                GUILayout.Label($"New with level {frame.ScoredLevel + 1}: " + string.Join(", ", next), _wrap);

            Section("Seen this level");
            for (int k = 0; k < frame.ScoredSeen.Length && k < SeenNames.Length; k++)
            {
                GUILayout.BeginHorizontal();
                var icon = GUILayoutUtility.GetRect(22f, 22f, GUILayout.Width(22f), GUILayout.Height(22f));
                if (Event.current.type == EventType.Repaint)
                    DrawIcon(icon, k);
                GUILayout.Label(new GUIContent($"{SeenNames[k]}: {frame.ScoredSeen[k]}", SeenTips[k]), GUILayout.Width(220));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button(SkillTree.ForSale(bridge.Schema, frame) > 0 ? "Continue to skills" : "Continue"))
                _open = false;
        }

        private static string Stars(int stars)
        {
            var s = "";
            for (int i = 0; i < 3; i++)
                s += i < stars ? "★" : "☆";
            return s;
        }

        private void Section(string name)
        {
            GUILayout.Space(6);
            GUILayout.Label(name, _title);
        }

        private void Meter(string name, float meter, int points, string tip)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent($"{name} {meter:F0}/100", tip), GUILayout.Width(130));
            var bar = GUILayoutUtility.GetRect(200f, 14f, GUILayout.Width(200f));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.2f, 0.2f, 0.2f), 0f, 0f);
                var fill = new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(meter / 100f), bar.height);
                GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, MeterColour(meter), 0f, 0f);
            }
            GUILayout.Label($"→ {points} points");
            GUILayout.EndHorizontal();
        }

        /// <summary>Green when full, through amber, to red when empty.</summary>
        public static Color MeterColour(float meter) =>
            meter > 50f ? Color.Lerp(new Color(0.95f, 0.7f, 0.2f), new Color(0.35f, 0.8f, 0.35f), (meter - 50f) / 50f)
                : Color.Lerp(new Color(0.85f, 0.25f, 0.2f), new Color(0.95f, 0.7f, 0.2f), meter / 50f);

        // A small drawing of each kind, in the colours the world view uses.
        private void DrawIcon(Rect r, int kind)
        {
            _disc ??= Circle(false);
            _ring ??= Circle(true);
            switch ((SeenKind)kind)
            {
                case SeenKind.FoodPacket:
                    GUI.DrawTexture(Inset(r, 3f), _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.9f, 0.65f, 0.25f), 0f, 0f);
                    break;
                case SeenKind.DeadCell:
                    GUI.DrawTexture(r, _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.45f, 0.45f, 0.45f), 0f, 0f);
                    GUI.DrawTexture(r, _ring, ScaleMode.ScaleToFit, true, 0f, new Color(0.85f, 0.85f, 0.85f), 0f, 0f);
                    break;
                case SeenKind.PreyCell:
                    GUI.DrawTexture(Inset(r, 5f), _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.6f, 0.4f, 0.85f), 0f, 0f);
                    GUI.DrawTexture(Inset(r, 9f), _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.3f, 0.15f, 0.45f), 0f, 0f);
                    break;
                case SeenKind.Daughter:
                    GUI.DrawTexture(r, _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.55f, 0.8f, 0.65f), 0f, 0f);
                    GUI.DrawTexture(r, _ring, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 0f);
                    break;
                default:
                    GUI.DrawTexture(Inset(r, 2f), _disc, ScaleMode.ScaleToFit, true, 0f, new Color(0.7f, 0.88f, 0.75f), 0f, 0f);
                    GUI.DrawTexture(Inset(r, 2f), _ring, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 0f);
                    break;
            }
        }

        private static Rect Inset(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - 2f * by, r.height - 2f * by);

        // A white disc, or a ring (its outline), with soft edges.
        private static Texture2D Circle(bool ring)
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = ring ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.1f) : Mathf.Clamp01((1f - d) * n / 2f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            return t;
        }
    }
}
