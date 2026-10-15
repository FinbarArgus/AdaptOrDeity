using AdaptOrDeity.Game.Session;
using AdaptOrDeity.Presentation;
using UnityEngine;

namespace AdaptOrDeity.UI
{
    /// <summary>
    /// Heads-up display in the style of Oxygen Not Included: overlay tabs with hotkeys, speed controls, a legend, hover
    /// tooltips and a probe plot for the clicked cell; clicking a cell also opens the <see cref="CellPanel"/>. The level's
    /// survival and damage meters are in the top bar. A feature that unlocks during play (e.g. Regulation, after the first
    /// gap without food) is announced in a box under the top bar until dismissed. IMGUI for the vertical slice; all input
    /// comes through IMGUI events so it works with either Unity input backend.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        private const float BarHeight = 34f;
        private static readonly string[] SpeedLabels = { "II", "1x", "10x", "100x", "1000x" };

        private int _lastRunningSpeed = 1;
        private GUIStyle _tooltipStyle;
        private bool[] _wasUnlocked;         // per feature, as of the last frame (null before the first)
        private int _announced = -1;         // the feature whose unlocking is shown, or -1
        private bool _announcedDetails;
        private int _noticeGeneration = -1;   // the session the unlock notice is for (SessionBridge.Generation)
        private int _noticeLevel = -1;        // and its level

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<Hud>() == null)
                new GameObject("Hud").AddComponent<Hud>();
        }

        private void OnGUI()
        {
            var bridge = SessionBridge.Instance;
            var frame = bridge != null ? bridge.Frame : null;
            if (frame == null)
                return;

            bridge.RightPanelWidth = RightInset();
            HandleInput(bridge, frame);
            DrawTopBar(bridge, frame);
            if (bridge.Schema.Features.Count > 0)
                DrawMeters(frame);
            if (bridge.ActiveOverlay >= 0)
                DrawLegend(bridge.Schema.Overlays[bridge.ActiveOverlay]);
            else if (bridge.ActiveField >= 0 && frame.FieldExtent > 0f && bridge.FieldView != null)
            {
                var info = FieldView.Overlays[bridge.ActiveField];
                DrawLegend(new OverlayInfo(info.Id, info.Name, info.Hotkey, info.SiUnit, info.DisplayUnit, 1.0,
                    bridge.FieldView.RangeMin, bridge.FieldView.RangeMax));
            }
            DrawCurrent(frame);
            _minimap.Draw(frame, _currentArea.y - 6f);
            DrawProbePlot(bridge, frame);
            DrawTooltip(bridge, frame);
            DrawUnlockNotice(bridge, frame);
            if (frame.PathwayEnded)
                DrawPathwayEnded(bridge, frame);
        }

        private GUIStyle _endTitle, _endText;

        // Every cell of the creature has died: the game is over. What killed it, how far it got, and a new start.
        private void DrawPathwayEnded(SessionBridge bridge, SnapshotFrame frame)
        {
            _endTitle ??= new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                wordWrap = true };
            _endText ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                    false, 0f, new Color(0f, 0f, 0f, 0.55f), 0f, 0f);
            const float w = 460f, h = 300f;
            var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUILayout.Window(0x5C113, rect, id =>
            {
                GUILayout.Space(8);
                GUILayout.Label("Your evolutionary pathway has ended", _endTitle);
                GUILayout.Space(8);
                GUILayout.Label($"Every cell of your creature has died, in level {frame.Level} after {FormatTime(frame.SimTime)}.", _endText);
                GUILayout.Space(6);
                GUILayout.Label("What killed it:", _endText);
                var bar = GUILayoutUtility.GetRect(w - 40f, 16f);
                var values = new double[frame.CreatureDamage.Length];
                for (int k = 0; k < values.Length; k++)
                    values[k] = frame.CreatureDamage[k];
                CellPanel.DamageBar(bar, values, 1.0);
                for (int k = 0; k < CellPanel.DamageParts.Length && k < frame.CreatureDamage.Length; k++)
                    if (frame.CreatureDamage[k] > 0.005f)
                        GUILayout.Label($"{CellPanel.DamageParts[k].Name}: {100f * frame.CreatureDamage[k]:F0}% (" +
                                        $"{CellPanel.DamageParts[k].Tip})", _endText);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Start a new pathway", GUILayout.Height(32)))
                    bridge.Restart();
            }, "");
        }

        // Announces a feature the first frame it is unlocked (not those unlocked when the session starts).
        private void DrawUnlockNotice(SessionBridge bridge, SnapshotFrame frame)
        {
            var features = bridge.Schema.Features;
            if (_noticeGeneration != bridge.Generation)
            {
                // a new game: nothing of the last one is announced, and what it starts with is not news
                _noticeGeneration = bridge.Generation;
                _noticeLevel = frame.Level;
                _announced = -1;
                _announcedDetails = false;
                _wasUnlocked = null;
            }
            else if (_noticeLevel != frame.Level)
            {
                // a new level: the last level's notice goes (what unlocks in this one is still announced)
                _noticeLevel = frame.Level;
                _announced = -1;
                _announcedDetails = false;
            }
            if (_wasUnlocked == null || _wasUnlocked.Length != frame.FeatureUnlocked.Length)
                _wasUnlocked = (bool[])frame.FeatureUnlocked.Clone();
            for (int f = 0; f < features.Count; f++)
            {
                if (frame.FeatureUnlocked[f] && !_wasUnlocked[f])
                {
                    _announced = f;
                    _announcedDetails = false;
                }
                _wasUnlocked[f] = frame.FeatureUnlocked[f];
            }
            if (_announced < 0 || _announced >= features.Count)
                return;
            var info = features[_announced];
            float w = Mathf.Min(560f, Screen.width - RightInset() - 24f);
            var rect = new Rect(12f, BarHeight + 66f, w, _announcedDetails ? 380f : 120f);   // below the meters
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label($"New: {info.Name}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 };
            GUILayout.Label(info.Effect, wrap);
            if (_announcedDetails)
                GUILayout.Label(info.Explanation, wrap);
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(info.Explanation) && GUILayout.Button(_announcedDetails ? "Less" : "Why?", GUILayout.Width(60)))
                _announcedDetails = !_announcedDetails;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(60)))
                _announced = -1;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void HandleInput(SessionBridge bridge, SnapshotFrame frame)
        {
            var e = Event.current;
            if (HandleLuck(bridge, e))
                return;
            if (e.type == EventType.KeyDown)
            {
                var overlays = bridge.Schema.Overlays;
                for (int i = 0; i < overlays.Count; i++)
                    if (e.keyCode.ToString() == overlays[i].Hotkey)
                    {
                        bridge.ActiveOverlay = bridge.ActiveOverlay == i ? -1 : i;
                        e.Use();
                    }
                for (int i = 0; i < FieldView.Overlays.Length && frame.FieldExtent > 0f; i++)
                    if (e.keyCode.ToString() == FieldView.Overlays[i].Hotkey)
                    {
                        bridge.ActiveField = bridge.ActiveField == i ? -1 : i;
                        e.Use();
                    }
                switch (e.keyCode)
                {
                    case KeyCode.Escape:
                        bridge.ActiveOverlay = -1;
                        bridge.ActiveField = -1;
                        e.Use();
                        break;
                    case KeyCode.Space:
                        SetSpeed(bridge, frame.SpeedIndex == 0 ? _lastRunningSpeed : 0);
                        e.Use();
                        break;
                    case KeyCode.Alpha1:
                    case KeyCode.Alpha2:
                    case KeyCode.Alpha3:
                    case KeyCode.Alpha4:
                        if (e.keyCode - KeyCode.Alpha0 < bridge.Schema.SpeedPresets.Count)
                            SetSpeed(bridge, e.keyCode - KeyCode.Alpha0);
                        e.Use();
                        break;
                }
            }
            else if (e.type == EventType.ScrollWheel && e.mousePosition.x < Screen.width - RightInset()
                     && e.mousePosition.y > BarHeight)
            {
                bridge.Zoom(e.delta.y > 0 ? 1f : -1f);   // wheel down zooms out
                e.Use();
            }
            else if (_dragCell >= 0 && (e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
            {
                DragFlagellum(bridge, e);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && e.mousePosition.y > BarHeight
                     && e.mousePosition.x < Screen.width - RightInset() && StartFlagellumDrag(bridge, e.mousePosition))
            {
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.mousePosition.y > BarHeight
                     && e.mousePosition.x < Screen.width - RightInset() && !OrganismPanel.Covers(e.mousePosition)
                     && !ProbePlotCovers(e.mousePosition) && !_currentArea.Contains(e.mousePosition)
                     && !_minimap.Covers(e.mousePosition))
            {
                var cell = CellUnderMouse(e.mousePosition);
                var organism = cell == null ? bridge.OrganismAt(MouseWorld(e.mousePosition)) : null;
                if (e.button == 0 && cell != null)
                {
                    // Select the cell (opens the cell panel) and follow it on the shown overlay with one probe.
                    bridge.SelectedCell = cell.CellIndex;
                    bridge.SelectedOrganism = -1;
                    bridge.Send(SimCommands.ClearProbes());
                    bridge.Send(SimCommands.AddProbe(cell.CellIndex, Mathf.Max(0, bridge.ActiveOverlay)));
                    e.Use();
                }
                else if (e.button == 0 && organism != null && organism.Slot < frame.OrganismCount)
                {
                    // a free organism: its read-only panel (OrganismPanel)
                    bridge.SelectedOrganism = frame.OrganismId[organism.Slot];
                    bridge.SelectedCell = -1;
                    bridge.Send(SimCommands.ClearProbes());
                    e.Use();
                }
                else if (e.button == 1 && e.mousePosition.x < Screen.width - RightInset())
                {
                    bridge.SelectedCell = -1;
                    bridge.SelectedOrganism = -1;
                    bridge.Send(SimCommands.ClearProbes());
                    e.Use();
                }
            }
        }

        private void SetSpeed(SessionBridge bridge, int index)
        {
            if (index > 0)
                _lastRunningSpeed = index;
            bridge.Send(SimCommands.SetSpeed(index));
        }

        private void DrawTopBar(SessionBridge bridge, SnapshotFrame frame)
        {
            GUI.Box(new Rect(0, 0, Screen.width, BarHeight), GUIContent.none);
            GUILayout.BeginArea(new Rect(8, 5, Screen.width - 16, BarHeight - 8));
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(bridge.ActiveOverlay < 0 && bridge.ActiveField < 0, "Plain [Esc]", GUI.skin.button, GUILayout.Width(100)))
            {
                bridge.ActiveOverlay = -1;
                bridge.ActiveField = -1;
            }
            var overlays = bridge.Schema.Overlays;
            for (int i = 0; i < overlays.Count; i++)
            {
                bool on = GUILayout.Toggle(bridge.ActiveOverlay == i, $"{overlays[i].Name} [{overlays[i].Hotkey}]",
                    GUI.skin.button, GUILayout.Width(120));
                if (on && bridge.ActiveOverlay != i)
                    bridge.ActiveOverlay = i;
                else if (!on && bridge.ActiveOverlay == i)
                    bridge.ActiveOverlay = -1;
            }
            // the water around the creature (the environment field), coloured on the range it shows now
            for (int i = 0; i < FieldView.Overlays.Length && frame.FieldExtent > 0f; i++)
            {
                var info = FieldView.Overlays[i];
                bool on = GUILayout.Toggle(bridge.ActiveField == i, new GUIContent($"{info.Name} [{info.Hotkey}]",
                    "the water around the creature; the colours span what is in view now (see the legend)"),
                    GUI.skin.button, GUILayout.Width(130));
                if (on && bridge.ActiveField != i)
                    bridge.ActiveField = i;
                else if (!on && bridge.ActiveField == i)
                    bridge.ActiveField = -1;
            }

            GUILayout.FlexibleSpace();
            if (frame.Lagging)
            {
                var old = GUI.color;
                GUI.color = new Color(1f, 0.45f, 0.35f);
                GUILayout.Label("Simulation lagging", GUILayout.Width(140));
                GUI.color = old;
            }
            float groundSpeed = Mathf.Sqrt(frame.CreatureVx * frame.CreatureVx + frame.CreatureVy * frame.CreatureVy) * 1e6f;
            GUILayout.Label(new GUIContent($"Moving {groundSpeed:F1} µm/s", "speed over the ground (drifting with the current)"),
                GUILayout.Width(120));
            GUILayout.Label($"t = {FormatTime(frame.SimTime)}", GUILayout.Width(130));
            if (bridge.Schema.Features.Count > 0)
            {
                // Development control until the evolutionary time bar sets the level: features unlock by level.
                GUILayout.Label(new GUIContent($"Level {frame.Level} · {GoalText(frame)}",
                    "the next level begins when all its goals are met"), GUILayout.Width(220));
                if (GUILayout.Button("−", GUILayout.Width(24)) && frame.Level > 1)
                    bridge.Send(SimCommands.SetLevel(frame.Level - 1));
                if (GUILayout.Button("+", GUILayout.Width(24)))
                    bridge.Send(SimCommands.SetLevel(frame.Level + 1));
                if (GUILayout.Button(new GUIContent("End", "development: end the level now and score it"), GUILayout.Width(40)))
                    bridge.Send(SimCommands.DevEndLevel());
                GUILayout.Label(new GUIContent(new string('★', frame.StarsNow) + new string('☆', 3 - Mathf.Clamp(frame.StarsNow, 0, 3)),
                    $"this level's points so far: {frame.LevelPointsNow} (meters and reproduction); stars at the level's end"),
                    GUILayout.Width(44));
                int forSale = SkillTree.ForSale(bridge.Schema, frame);
                string earned = $"earned: survival {frame.SurvivalPoints}, damage {frame.DamagePoints}, reproduction {frame.ReproductionPoints}";
                if (GUILayout.Button(new GUIContent($"Skills ({frame.SkillPoints})",
                        (forSale > 0 ? $"{forSale} to evolve; " : "") + earned), GUILayout.Width(90)))
                    SkillTree.Toggle();
                if (frame.Level >= 2)
                {
                    // prey swallowed (10 open the mitochondria path) and daughters that left (reproduction points)
                    GUILayout.Label(new GUIContent($"Swallowed {frame.Swallowed}",
                        "prey cells your cells have engulfed; 10 make mitochondria available"), GUILayout.Width(95));
                    GUILayout.Label(new GUIContent($"Offspring {frame.Offspring}",
                        $"daughters that swam off on their own (without multicellularity), and their daughters in turn; " +
                        $"each earns reproduction points. Your creature's own: {frame.OwnOffspring}; their descendants: " +
                        $"{frame.Offspring - frame.OwnOffspring}"), GUILayout.Width(85));
                    GUILayout.Label(new GUIContent($"Free {frame.OrganismCount}",
                        $"daughters and their offspring living nearby on their own (pale, white outline; click one to see " +
                        $"its state). Released {frame.OrganismsReleased}, died {frame.OrganismsDied} (their bodies are " +
                        $"food, grey); out of range and frozen until you come back: {frame.OrganismsFrozen} living, " +
                        $"{frame.BodiesFrozen} dead; forgotten {frame.OrganismsGone}"), GUILayout.Width(60));
                    // development aids until play-testing is done: points and a prey cell swimming at the creature
                    if (GUILayout.Button(new GUIContent("+5", "development: 5 skill points"), GUILayout.Width(30)))
                        bridge.Send(SimCommands.DevGrantPoints(5));
                    if (GUILayout.Button(new GUIContent("Prey", "development: a prey cell swimming at the creature"),
                            GUILayout.Width(44)))
                        bridge.Send(SimCommands.DevSpawnPrey());
                }
                GUILayout.Space(12);
            }
            var labels = new string[bridge.Schema.SpeedPresets.Count];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = i < SpeedLabels.Length ? SpeedLabels[i] : $"{bridge.Schema.SpeedPresets[i]:G3}x";
            int speed = GUILayout.Toolbar(frame.SpeedIndex, labels, GUILayout.Width(56 * labels.Length));
            if (speed != frame.SpeedIndex)
                SetSpeed(bridge, speed);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // A meter in the top bar: its value as a small bar and its worth in points if the level ended now.
        // The survival and damage meters, in their own panel under the top bar on the left (always visible from level 1).
        private static void DrawMeters(SnapshotFrame frame)
        {
            GUILayout.BeginArea(new Rect(8f, BarHeight + 6f, 250f, 52f), GUI.skin.box);
            DrawMeter("Survival", frame.SurvivalMeter, frame.SurvivalPointsNow,
                "falls while your cells are starving (ATP below half its resting level)" +
                (frame.StarvingShare > 0f ? $"; starving now: {frame.StarvingShare * 100f:F0}% of your cells" : "") +
                $"; {frame.TimeWithoutFood:F0} s since your creature last had food");
            DrawDamage(frame);
            GUILayout.EndArea();
        }

        // The creature's damage as one bar of its parts (physical, radiation, ribosome, de-energisation, dead cells), 100%
        // at the right end; a white tick marks the worst it has reached, which repair does not undo.
        private static void DrawDamage(SnapshotFrame frame)
        {
            float total = 0f;
            for (int k = 0; k < frame.CreatureDamage.Length; k++)
                total += frame.CreatureDamage[k];
            var parts = new System.Text.StringBuilder();
            for (int k = 0; k < frame.CreatureDamage.Length; k++)
                if (frame.CreatureDamage[k] > 0.0005f)
                    parts.Append($"; {(k < CellPanel.DamageParts.Length ? CellPanel.DamageParts[k].Name : "dead cells")} " +
                                 $"{100f * frame.CreatureDamage[k]:F1}%");
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent($"Damage {100f * total:F0}%",
                $"your creature's damage, the mean over its cells of each kind as a share of a lethal load{parts}. At 100% " +
                $"your evolutionary pathway ends. The tick marks the worst it has reached ({100f * frame.CreatureDamageWorst:F0}%): " +
                $"repair mends the damage but not the record. The damage this level added is worth {frame.DamagePointsNow} " +
                "points now"), GUILayout.Width(110));
            var bar = GUILayoutUtility.GetRect(120f, 14f, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            bar.y += 4f;
            bar.height = 10f;
            var values = new double[frame.CreatureDamage.Length];
            for (int k = 0; k < values.Length; k++)
                values[k] = frame.CreatureDamage[k];
            CellPanel.DamageBar(bar, values, frame.CreatureDamageWorst, new Color(0.35f, 0.35f, 0.38f));
        }

        private static void DrawMeter(string name, float meter, int points, string tip)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent($"{name} {meter:F0}/100", $"{name} meter {meter:F0}/100, worth {points} points now: {tip}"),
                GUILayout.Width(110));
            var bar = GUILayoutUtility.GetRect(120f, 14f, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            if (Event.current.type != EventType.Repaint)
                return;
            bar.y += 4f;
            bar.height = 10f;
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.2f, 0.2f, 0.2f), 0f, 0f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(meter / 100f), bar.height), Texture2D.whiteTexture,
                ScaleMode.StretchToFill, false, 0f, LevelEndWindow.MeterColour(meter), 0f, 0f);
        }

        // The level's goals: a survival goal shows the time still to survive.
        private static string GoalText(SnapshotFrame frame)
        {
            var goals = AdaptOrDeity.Game.Content.GameTimeline.Default.GoalsOf(frame.Level);
            if (goals.Count == 0)
                return "no goal";
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < goals.Count; i++)
                parts.Add(goals[i].Kind == "survive" && frame.LevelTimeLeft > 0.0
                    ? $"{goals[i].Name}: {FormatTime(frame.LevelTimeLeft)} to go"
                    : goals[i].Name);
            return string.Join(", ", parts);
        }

        private void DrawLegend(OverlayInfo info)
        {
            const float width = 26f, height = 220f;
            var area = new Rect(Screen.width - RightInset() - 120, BarHeight + 16, 110, height + 50);
            GUI.Box(area, info.Name);
            var bar = new Rect(area.x + 10, area.y + 24, width, height);
            if (Event.current.type == EventType.Repaint)
            {
                const int bands = 64;
                for (int i = 0; i < bands; i++)
                {
                    float t0 = (float)i / bands;
                    var band = new Rect(bar.x, bar.yMax - (t0 + 1f / bands) * height, width, height / bands + 1f);
                    GUI.DrawTexture(band, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                        Colormap.Evaluate(t0 + 0.5f / bands), 0f, 0f);
                }
            }
            GUI.Label(new Rect(bar.xMax + 6, bar.y - 8, 70, 20), $"{info.DisplayMax:G4}");
            GUI.Label(new Rect(bar.xMax + 6, bar.yMax - 12, 70, 20), $"{info.DisplayMin:G4}");
            GUI.Label(new Rect(bar.xMax + 6, bar.y + height * 0.5f - 10, 70, 20), info.DisplayUnit);
        }

        private void DrawTooltip(SessionBridge bridge, SnapshotFrame frame)
        {
            var mouse = Event.current.mousePosition;
            var cell = CellUnderMouse(mouse);
            if (cell == null)
            {
                DrawPacketTooltip(bridge, frame, mouse);
                return;
            }
            var overlays = bridge.Schema.Overlays;
            var text = $"Cell {cell.CellIndex}";
            for (int c = 0; c < overlays.Count; c++)
                text += $"\n{overlays[c].Name}: {frame.Channels[c][cell.CellIndex] * overlays[c].DisplayMultiplier:G4} {overlays[c].DisplayUnit}";
            if (frame.CellAlive.Length > cell.CellIndex && !frame.CellAlive[cell.CellIndex])
                text += "\n<b>Dead</b>";
            text += "\n<i>Click to inspect and plot, right-click to close</i>";

            _tooltipStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, padding = new RectOffset(8, 8, 6, 6) };
            var content = new GUIContent(text);
            var size = _tooltipStyle.CalcSize(content);
            GUI.Box(new Rect(mouse.x + 16, mouse.y + 16, size.x, size.y), content, _tooltipStyle);
        }

        // The background flow, bottom left of the view: an arrow along the current (in water) or the wind (on land), its
        // speed, and the speed where the creature is when the seabed or a rock shelters it.
        private static readonly Color CurrentColour = new Color(0.45f, 0.75f, 1f), WindColour = new Color(0.92f, 0.9f, 0.8f);
        private Texture2D _arrow;
        private readonly MinimapPanel _minimap = new MinimapPanel();
        private GUIStyle _currentText;
        private Rect _currentArea;

        private void DrawCurrent(SnapshotFrame frame)
        {
            _currentText ??= new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true, richText = true };
            _arrow ??= ArrowTexture();
            _currentArea = new Rect(8, Screen.height - 110, 220, 102);
            GUI.Box(_currentArea, GUIContent.none);
            float free = Mathf.Sqrt(frame.FreeCurrentX * frame.FreeCurrentX + frame.FreeCurrentY * frame.FreeCurrentY);
            float local = Mathf.Sqrt(frame.LocalCurrentX * frame.LocalCurrentX + frame.LocalCurrentY * frame.LocalCurrentY);
            string name = frame.OnLand ? "Wind" : "Current";
            var square = new Rect(_currentArea.x + 6, _currentArea.y + 6, 64, 64);
            if (free > 1e-9f)   // above a nanometre per second
            {
                // world y grows upwards, GUI y downwards
                float degrees = -Mathf.Atan2(frame.FreeCurrentY, frame.FreeCurrentX) * Mathf.Rad2Deg;
                var saved = GUI.matrix;
                GUIUtility.RotateAroundPivot(degrees, square.center);
                GUI.DrawTexture(square, _arrow, ScaleMode.ScaleToFit, true, 0f, frame.OnLand ? WindColour : CurrentColour, 0f, 0f);
                GUI.matrix = saved;
            }
            string text = free > 1e-9f
                ? $"<b>{name}</b> {1e6f * free:0.00} \u00B5m/s"
                : $"<b>{name}</b> slack" + (frame.OnLand ? "" : " (the tide is turning)");
            if (free > 1e-9f && local < 0.9f * free)
                text += $"\nhere {1e6f * local:0.00} \u00B5m/s, " + (frame.OnLand ? "sheltered" : "sheltered by the seabed or a rock");
            GUI.Label(new Rect(square.xMax + 6, _currentArea.y + 6, _currentArea.width - 82, 64), text, _currentText);
            DrawLuck(frame, new Rect(_currentArea.x + 6, _currentArea.y + 74, _currentArea.width - 12, 24));
        }

        // Luck's points as a bar, and how to use them
        private void DrawLuck(SnapshotFrame frame, Rect r)
        {
            if (frame.LuckMax <= 0f)
                return;
            float share = Mathf.Clamp01(frame.Luck / frame.LuckMax);
            var bar = new Rect(r.x, r.y + 2, 90, 12);
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0f, 0f, 0f, 0.5f), 0f, 0f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * share, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                false, 0f, frame.LuckOn ? new Color(1f, 0.92f, 0.5f) : new Color(1f, 0.78f, 0.25f), 0f, 0f);
            string hint = LuckView.Aiming ? "aiming: press on the edge" : "L: aim her current";
            GUI.Label(new Rect(bar.xMax + 6, r.y - 2, r.width - 96, 20),
                new GUIContent($"Luck {frame.Luck:0} \u00B7 {hint}",
                    "Luck's points build up over time. Press L, then press on the edge of the view and drag: she blows a " +
                    "current from there towards the mouse, as strong as the arrow is long, for as long as you hold it, " +
                    "spending her points. It carries food, prey and your creature. L, Esc or a right-click stops aiming."),
                _currentText);
        }

        // Luck's current: L toggles aiming; pressing in the view starts an arrow at the nearest edge of the view, dragging
        // guides its head, releasing stops the current. The arrow is sent relative to the creature.
        private Vector2 _luckSentHead;

        private bool HandleLuck(SessionBridge bridge, Event e)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.L)
            {
                LuckView.Aiming = !LuckView.Aiming;
                if (!LuckView.Aiming)
                    StopLuck(bridge);
                e.Use();
                return true;
            }
            if (!LuckView.Aiming)
                return false;
            var view = new Rect(0f, BarHeight, Screen.width - RightInset(), Screen.height - BarHeight);
            if ((e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) || (e.type == EventType.MouseDown && e.button == 1))
            {
                LuckView.Aiming = false;
                StopLuck(bridge);
                e.Use();
                return true;
            }
            if (e.type == EventType.MouseDown && e.button == 0 && view.Contains(e.mousePosition) && !CoversUi(e.mousePosition))
            {
                LuckView.AimTail = CreatureRelative(bridge, NearestEdge(view, e.mousePosition));
                LuckView.AimHead = CreatureRelative(bridge, e.mousePosition);
                LuckView.Dragging = true;
                SendLuck(bridge);
                e.Use();
                return true;
            }
            if (LuckView.Dragging && e.type == EventType.MouseDrag)
            {
                var p = new Vector2(Mathf.Clamp(e.mousePosition.x, view.xMin, view.xMax), Mathf.Clamp(e.mousePosition.y, view.yMin, view.yMax));
                LuckView.AimHead = CreatureRelative(bridge, p);
                if ((LuckView.AimHead - _luckSentHead).magnitude > 0.2e-6f)   // re-aim every 0.2 um the head moves
                    SendLuck(bridge);
                e.Use();
                return true;
            }
            if (LuckView.Dragging && e.type == EventType.MouseUp)
            {
                StopLuck(bridge);
                e.Use();
                return true;
            }
            return false;
        }

        private void SendLuck(SessionBridge bridge)
        {
            _luckSentHead = LuckView.AimHead;
            Vector2 t = LuckView.AimTail, h = LuckView.AimHead;
            bridge.Send(SimCommands.LuckBlow(t.x, t.y, h.x, h.y));
        }

        private static void StopLuck(SessionBridge bridge)
        {
            if (LuckView.Dragging)
                bridge.Send(SimCommands.LuckStop());
            LuckView.Dragging = false;
        }

        private bool CoversUi(Vector2 p) =>
            OrganismPanel.Covers(p) || ProbePlotCovers(p) || _currentArea.Contains(p) || _minimap.Covers(p);

        // The point on the view's edge nearest a GUI point
        private static Vector2 NearestEdge(Rect view, Vector2 p)
        {
            float left = p.x - view.xMin, right = view.xMax - p.x, top = p.y - view.yMin, bottom = view.yMax - p.y;
            float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
            if (m == left)
                return new Vector2(view.xMin, p.y);
            if (m == right)
                return new Vector2(view.xMax, p.y);
            return m == top ? new Vector2(p.x, view.yMin) : new Vector2(p.x, view.yMax);
        }

        // A GUI point as a position relative to the creature [m], as the snapshot has it
        private static Vector2 CreatureRelative(SessionBridge bridge, Vector2 guiPoint) =>
            (MouseWorld(guiPoint) - bridge.Shift) / CellView.UnitsPerMetre;

        // A white arrow pointing right (+x), 64 x 64, for tinting and rotating
        private static Texture2D ArrowTexture()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dy = Mathf.Abs(y - (n - 1) / 2f);
                    bool shaft = x >= 8 && x < 40 && dy <= 4f;
                    bool head = x >= 36 && x <= 58 && dy <= (58 - x) * 0.9f;
                    pixels[y * n + x] = shaft || head ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            t.SetPixels32(pixels);
            t.Apply(false);
            return t;
        }

        // The clicked cell's probe trace, under a drop-down at the bottom right of the view: closed by default.
        private bool _showProbePlot;
        private Texture2D _plotTexture;
        private Color32[] _plotPixels;
        private static Rect _probeArea;   // where the plot (or its closed header) was drawn: clicks there are not world clicks

        /// <summary>Whether the probe plot or its header covers a screen point (GUI coordinates).</summary>
        public static bool ProbePlotCovers(Vector2 point) => _probeArea.Contains(point);

        private void DrawProbePlot(SessionBridge bridge, SnapshotFrame frame)
        {
            _probeArea = default;
            if (frame.ProbeCount == 0)
                return;
            var probe = frame.Probes[0];
            if (probe.Count < 2)
                return;
            var info = bridge.Schema.Overlays[probe.Channel];
            string title = $"Cell {probe.Cell}: {info.Name} ({info.DisplayUnit})";
            float left = Screen.width - RightInset() - 420;
            if (!_showProbePlot)
            {
                _probeArea = new Rect(left, Screen.height - 30, 410, 24);
                GUI.Box(_probeArea, GUIContent.none);
                _showProbePlot = GUI.Toggle(new Rect(left + 6, Screen.height - 28, 398, 20), false, "\u25B8 " + title, GUI.skin.label);
                return;
            }

            var area = new Rect(left, Screen.height - 240, 410, 230);
            _probeArea = area;
            GUI.Box(area, GUIContent.none);
            _showProbePlot = GUI.Toggle(new Rect(area.x + 6, area.y + 2, 398, 20), true, "\u25BE " + title, GUI.skin.label);
            var plot = new Rect(area.x + 50, area.y + 28, area.width - 64, area.height - 58);

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < probe.Count; i++)
            {
                float v = probe.Values[i] * (float)info.DisplayMultiplier;
                if (float.IsNaN(v) || float.IsInfinity(v))
                    continue;
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
            if (min > max)
                return;
            float pad = Mathf.Max(1e-3f, 0.05f * (max - min));
            min -= pad;
            max += pad;
            double t0 = probe.Times[0], t1 = probe.Times[probe.Count - 1];

            GUI.Label(new Rect(area.x + 4, plot.y - 8, 46, 20), $"{max:G4}");
            GUI.Label(new Rect(area.x + 4, plot.yMax - 12, 46, 20), $"{min:G4}");
            GUI.Label(new Rect(plot.x, plot.yMax + 4, 150, 20), FormatTime(t0));
            GUI.Label(new Rect(plot.xMax - 90, plot.yMax + 4, 90, 20), FormatTime(t1));
            if (Event.current.type != EventType.Repaint)
                return;

            // the trace is drawn into a texture shown in the plot rect, so it shares the GUI's coordinates (GL's pixel
            // matrix follows the camera viewport, which stops at the cell panel)
            int w = Mathf.Max(2, (int)plot.width), h = Mathf.Max(2, (int)plot.height);
            if (_plotTexture == null || _plotTexture.width != w || _plotTexture.height != h)
            {
                if (_plotTexture != null)
                    Destroy(_plotTexture);
                _plotTexture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                    { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                _plotPixels = new Color32[w * h];
            }
            System.Array.Clear(_plotPixels, 0, _plotPixels.Length);
            Color32 colour = Colormap.Evaluate(0.8f);
            double span = System.Math.Max(t1 - t0, 1e-12);
            int px = -1, py = -1;
            for (int i = 0; i < probe.Count; i++)
            {
                float v = probe.Values[i] * (float)info.DisplayMultiplier;
                if (float.IsNaN(v) || float.IsInfinity(v))
                {
                    px = -1;
                    continue;
                }
                int x = Mathf.Clamp((int)System.Math.Round((probe.Times[i] - t0) / span * (w - 1)), 0, w - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt((v - min) / (max - min) * (h - 1)), 0, h - 1);   // texture rows go up
                if (px >= 0)
                    PlotLine(px, py, x, y, w, colour);
                else
                    _plotPixels[y * w + x] = colour;
                px = x;
                py = y;
            }
            _plotTexture.SetPixels32(_plotPixels);
            _plotTexture.Apply(false);
            GUI.DrawTexture(plot, _plotTexture, ScaleMode.StretchToFill, true);
        }

        // Bresenham line into the plot's pixels, one pixel thick
        private void PlotLine(int x0, int y0, int x1, int y1, int w, Color32 colour)
        {
            int dx = System.Math.Abs(x1 - x0), dy = -System.Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                _plotPixels[y0 * w + x0] = colour;
                if (x0 == x1 && y0 == y1)
                    return;
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawPacketTooltip(SessionBridge bridge, SnapshotFrame frame, Vector2 mouse)
        {
            var cam = Camera.main;
            if (cam == null || mouse.x > Screen.width - RightInset())
                return;
            var world = (Vector2)cam.ScreenToWorldPoint(new Vector3(mouse.x, Screen.height - mouse.y, 0f));
            var organism = bridge.OrganismAt(world);
            if (organism != null && organism.Slot < frame.OrganismCount)
            {
                int k = organism.Slot;
                ShowTooltip(mouse, $"Free organism {frame.OrganismId[k]} (generation {frame.OrganismGeneration[k]})\n" +
                    (frame.OrganismGeneration[k] > 1 ? "Offspring of a daughter that left your creature" :
                        "A daughter that left your creature") + "\n" +
                    "Lives on its own: not yours to control\n<i>Click to see its state</i>");
                return;
            }
            var prey = bridge.PreyAt(world);
            if (prey != null && prey.Slot < frame.PreyCount)
            {
                bool canEngulf = frame.Level >= 2;
                ShowTooltip(mouse, "Prey cell\nA small bacterium, swimming by run and tumble\n" +
                    (canEngulf ? "Your cells engulf it on contact (Engulfing)" : "Engulfing comes at level 2"));
                return;
            }
            var packet = bridge.PacketAt(world);
            if (packet == null || packet.Slot >= frame.PacketCount)
                return;
            var traces = bridge.Schema.TraceSpecies;
            int t = frame.PacketTrace[packet.Slot];
            string text = (frame.PacketBody[packet.Slot]
                              ? "Dead cell\nA free organism that died: food for whoever finds it\n"
                              : "Food packet\n") +
                          $"Glucose left: {100f * frame.PacketFill[packet.Slot]:F0}%\n" +
                          $"Smells of {(t < traces.Count ? traces[t] : "?")} (needs a receptor to sense)";
            _tooltipStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, padding = new RectOffset(8, 8, 6, 6) };
            var content = new GUIContent(text);
            var size = _tooltipStyle.CalcSize(content);
            GUI.Box(new Rect(mouse.x + 16, mouse.y + 16, size.x, size.y), content, _tooltipStyle);
        }

        private void ShowTooltip(Vector2 mouse, string text)
        {
            _tooltipStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = true, padding = new RectOffset(8, 8, 6, 6) };
            var content = new GUIContent(text);
            var size = _tooltipStyle.CalcSize(content);
            GUI.Box(new Rect(mouse.x + 16, mouse.y + 16, size.x, size.y), content, _tooltipStyle);
        }

        /// <summary>Width taken by the cell panel on the right, if open.</summary>
        private static float RightInset() => CellPanel.Visible ? CellPanel.Width : 0f;

        private int _dragCell = -1, _dragFlagellum = -1;
        private float _dragSent = float.NaN;

        private static Vector2 MouseWorld(Vector2 guiMouse) =>
            Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(guiMouse.x, Screen.height - guiMouse.y, 0f)) : Vector2.zero;

        /// <summary>
        /// On the selected cell, grabbing a flagellum's base (the dot where it meets the membrane) starts moving it around
        /// the membrane; the simulation keeps it there (in the body frame) until it is moved again.
        /// </summary>
        private bool StartFlagellumDrag(SessionBridge bridge, Vector2 guiMouse)
        {
            if (bridge.SelectedCell < 0)
                return false;
            CellView view = null;
            foreach (var c in FindObjectsByType<CellView>(FindObjectsSortMode.None))
                if (c.CellIndex == bridge.SelectedCell)
                    view = c;
            if (view == null)
                return false;
            var world = MouseWorld(guiMouse);
            int n = Mathf.CeilToInt(view.FlagellaCount);
            for (int k = 0; k < n && k < AdaptOrDeity.Sim.World.CreatureBody.MaxFlagella; k++)
                if ((view.FlagellumBase(k) - world).sqrMagnitude <= 0.035f * 0.035f)
                {
                    _dragCell = bridge.SelectedCell;
                    _dragFlagellum = k;
                    _dragSent = float.NaN;
                    return true;
                }
            return false;
        }

        private void DragFlagellum(SessionBridge bridge, Event e)
        {
            CellView view = null;
            foreach (var c in FindObjectsByType<CellView>(FindObjectsSortMode.None))
                if (c.CellIndex == _dragCell)
                    view = c;
            if (view != null && bridge.Frame != null)
            {
                var d = MouseWorld(e.mousePosition) - (Vector2)view.transform.position;
                // the angle on the membrane in the body frame: the body may be turning under the mouse
                float angle = Mathf.Atan2(d.y, d.x) - bridge.DisplayOrientation;
                if (float.IsNaN(_dragSent) || Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, _dragSent * Mathf.Rad2Deg)) > 1f)
                {
                    bridge.Send(SimCommands.SetFlagellumAngle(_dragCell, _dragFlagellum, angle));
                    _dragSent = angle;
                }
            }
            if (e.type == EventType.MouseUp)
                _dragCell = _dragFlagellum = -1;
        }

        private static CellView CellUnderMouse(Vector2 guiMouse)
        {
            var cam = Camera.main;
            if (cam == null)
                return null;
            var world = (Vector2)cam.ScreenToWorldPoint(new Vector3(guiMouse.x, Screen.height - guiMouse.y, 0f));
            foreach (var cell in FindObjectsByType<CellView>(FindObjectsSortMode.None))
                if (cell.Contains(world))
                    return cell;
            return null;
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 60.0)
                return $"{seconds:F2} s";
            if (seconds < 3600.0)
                return $"{seconds / 60.0:F1} min";
            if (seconds < 86400.0)
                return $"{seconds / 3600.0:F2} h";
            return $"{seconds / 86400.0:F1} d";
        }

        private void OnDestroy()
        {
            if (_plotTexture != null)
                Destroy(_plotTexture);
            if (_arrow != null)
                Destroy(_arrow);
            _minimap.Dispose();
        }
    }
}
