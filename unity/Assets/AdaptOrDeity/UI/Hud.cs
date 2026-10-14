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
        private Material _lineMaterial;
        private GUIStyle _tooltipStyle;
        private bool[] _wasUnlocked;         // per feature, as of the last frame (null before the first)
        private int _announced = -1;         // the feature whose unlocking is shown, or -1
        private bool _announcedDetails;

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
            DrawProbePlot(bridge, frame);
            DrawTooltip(bridge, frame);
            DrawUnlockNotice(bridge, frame);
        }

        // Announces a feature the first frame it is unlocked (not those unlocked when the session starts).
        private void DrawUnlockNotice(SessionBridge bridge, SnapshotFrame frame)
        {
            var features = bridge.Schema.Features;
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
                     && e.mousePosition.x < Screen.width - RightInset() && !OrganismPanel.Covers(e.mousePosition))
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
            DrawMeter("Damage", frame.DamageMeter, frame.DamagePointsNow,
                "100 less the worst damage your cells have carried this level (share of a lethal load)");
            GUILayout.EndArea();
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

        private void DrawProbePlot(SessionBridge bridge, SnapshotFrame frame)
        {
            if (frame.ProbeCount == 0)
                return;
            var probe = frame.Probes[0];
            if (probe.Count < 2)
                return;
            var info = bridge.Schema.Overlays[probe.Channel];

            var area = new Rect(Screen.width - RightInset() - 420, Screen.height - 240, 410, 230);
            GUI.Box(area, $"Cell {probe.Cell}: {info.Name} ({info.DisplayUnit})");
            var plot = new Rect(area.x + 50, area.y + 28, area.width - 64, area.height - 58);

            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < probe.Count; i++)
            {
                float v = probe.Values[i] * (float)info.DisplayMultiplier;
                min = Mathf.Min(min, v);
                max = Mathf.Max(max, v);
            }
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
            if (_lineMaterial == null)
            {
                _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            }
            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Begin(GL.LINES);
            GL.Color(Colormap.Evaluate(0.8f));
            Vector2 previous = default;
            for (int i = 0; i < probe.Count; i++)
            {
                float x = plot.x + (float)((probe.Times[i] - t0) / System.Math.Max(t1 - t0, 1e-12)) * plot.width;
                float v = probe.Values[i] * (float)info.DisplayMultiplier;
                float y = plot.yMax - (v - min) / (max - min) * plot.height;
                var point = new Vector2(x, Screen.height - y); // GUI y-down → pixel y-up
                if (i > 0)
                {
                    GL.Vertex3(previous.x, previous.y, 0f);
                    GL.Vertex3(point.x, point.y, 0f);
                }
                previous = point;
            }
            GL.End();
            GL.PopMatrix();
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
            if (_lineMaterial != null)
                Destroy(_lineMaterial);
        }
    }
}
