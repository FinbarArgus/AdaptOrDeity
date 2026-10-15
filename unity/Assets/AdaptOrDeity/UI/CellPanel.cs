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
    /// cell in the selected cell's group. Once Regulation is unlocked each expression slider, the growth slider and each
    /// part of the maintenance has a cog that opens a popup choosing which falling species lower it. Everything shown comes from the snapshot schema.
    /// IMGUI for the vertical slice.
    /// </summary>
    public sealed class CellPanel : MonoBehaviour
    {
        public const float Width = 400f;
        private const float Top = 34f;

        // Colours of the ATP sinks in the stacked bar, by detail id.
        private static readonly Dictionary<string, Color> SinkColours = new Dictionary<string, Color>
        {
            ["atp_pumps"] = new Color(0.95f, 0.55f, 0.25f),
            ["atp_swimming"] = new Color(0.30f, 0.75f, 0.95f),
            ["atp_protein_turnover"] = new Color(0.40f, 0.70f, 0.95f),
            ["atp_protein_build"] = new Color(0.70f, 0.50f, 0.95f),
            ["atp_protein_breakdown"] = new Color(0.85f, 0.40f, 0.85f),
            ["atp_rna"] = new Color(0.35f, 0.85f, 0.75f),
            ["atp_ribosome_rebuild"] = new Color(0.20f, 0.60f, 0.55f),
            ["atp_lipids"] = new Color(0.95f, 0.85f, 0.35f),
            ["atp_repair"] = new Color(0.95f, 0.45f, 0.55f),
            ["atp_growth"] = new Color(0.55f, 0.90f, 0.40f),
            ["atp_storage"] = new Color(0.80f, 0.65f, 0.45f),
            ["atp_carriers"] = new Color(0.45f, 0.60f, 0.95f),
            ["atp_matrix"] = new Color(0.75f, 0.85f, 0.55f),
            ["atp_other"] = new Color(0.60f, 0.65f, 0.70f),
        };

        // Colours of the ATP sources in the energy balance, by detail id.
        private static readonly Dictionary<string, Color> SourceColours = new Dictionary<string, Color>
        {
            ["atp_glycolysis"] = new Color(0.30f, 0.80f, 0.45f),
            ["atp_from_glycogen"] = new Color(0.80f, 0.65f, 0.45f),
            ["atp_recycled_protein"] = new Color(0.55f, 0.75f, 0.95f),
        };

        private static readonly Color Good = new Color(0.55f, 0.90f, 0.55f), Watch = new Color(1f, 0.80f, 0.35f),
            Bad = new Color(1f, 0.45f, 0.40f);

        private Vector2 _scroll;
        private bool _showProteinTurnover, _showRates, _showState, _showAtpItems, _showSwimming, _showExpressionHelp, _showTools;
        private bool _showFeatures = true;
        private float[] _sliders = new float[0];
        private float[] _controls = new float[0];
        private int _slidersForGroup = -1;
        private int _regulationTarget = -1;   // the target whose regulation popup is open (a trait, growth or maintenance), or -1
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

            DrawVitals(schema, frame, cell);
            DrawEnergyBalance(schema, frame, cell);
            DrawLeakHint(schema, frame, cell);
            if (Unlocked(schema, frame, GameFeatures.GrowthAndDivision))
                DrawGrowth(bridge, schema, frame, cell);

            _showAtpItems = Foldout(_showAtpItems, "ATP made and used, item by item");
            if (_showAtpItems)
            {
                DrawGroup(schema, frame, cell, "ATP made");
                DrawGroup(schema, frame, cell, "ATP used");
            }
            _showProteinTurnover = Foldout(_showProteinTurnover, "Protein upkeep, protein by protein");
            if (_showProteinTurnover)
                DrawGroup(schema, frame, cell, "Protein turnover", header: false);
            _showRates = Foldout(_showRates, "Metabolic rate, uptake and heat");
            if (_showRates)
                DrawMetabolicRate(schema, frame, cell);
            _showState = Foldout(_showState, "Ions and cell state");
            if (_showState)
                DrawGroup(schema, frame, cell, "Cell state", header: false);
            _showSwimming = Foldout(_showSwimming, "Swimming");
            if (_showSwimming)
                DrawSwimming(frame);

            GUILayout.Space(10);
            GUILayout.Label("Protein expression", _header);
            _showExpressionHelp = Foldout(_showExpressionHelp, "How expression works");
            if (_showExpressionHelp)
                GUILayout.Label("Copies relative to the baseline cell, for every cell in this group; a carrier you evolved is " +
                            "set as its share of the membrane's room for proteins. New copies cost ATP to build (shown " +
                            "under ATP used) and a little each second to keep. The membrane holds only so many proteins. " +
                            "Each membrane protein shows as an arc of the cell's rim in its colour, as long as the share " +
                            "of the surface it takes. Drag a flagellum's base (on the selected cell) to move it around the " +
                            "membrane. Changes take minutes: a protein rises as fast as its mRNA, the ribosomes and ATP " +
                            "allow (hours for the big ones), and lowering a target has proteases remove the extra copies " +
                            "(about 1 ATP per amino acid, shown under ATP used); their amino acids go to building other " +
                            "proteins, which then cost less (Recycled protein, under ATP made). Each slider shows the " +
                            "level now → the target.", _small);
            for (int t = 0; t < schema.Traits.Count; t++)
                if (schema.Traits[t].Feature == null || Unlocked(schema, frame, schema.Traits[t].Feature))
                    DrawTraitSlider(bridge, schema.Traits[t], frame, cell, group, t);

            GUILayout.Space(10);
            GUILayout.Label("Energy", _header);
            for (int c = 0; c < schema.Controls.Count; c++)
                if (Active(schema, frame, schema.Controls[c].Feature))
                    DrawControl(bridge, schema, frame, schema.Controls[c], group, c);
            DrawMaintenance(schema, frame, group);

            GUILayout.Space(6);
            _showFeatures = Foldout(_showFeatures, "Features (your choice once unlocked)");
            if (_showFeatures)
                DrawFeatures(bridge, schema, frame);

            GUILayout.Space(6);
            _showTools = Foldout(_showTools, "Tools");
            if (_showTools && GUILayout.Button("Export creature model (module array)"))
                bridge.ExportModel();
            if (bridge.LastExport != null)
                GUILayout.Label($"Written to {bridge.LastExport}", _small);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if (_regulationTarget >= 0)
                DrawRegulationPopup(bridge, schema, frame, group);
        }

        /// <summary>
        /// The cog beside a slider once Regulation is unlocked and on: opens the popup for <paramref name="target"/> (a
        /// trait, or <see cref="SessionSchema.GrowthRegulation"/>). Shows how many species lower it.
        /// </summary>
        private void DrawCog(SessionSchema schema, SnapshotFrame frame, int group, int target)
        {
            if (!Active(schema, frame, GameFeatures.Regulation))
                return;
            int on = 0;
            for (int s = 0; s < SessionSchema.RegulationSpecies.Count; s++)
                if (frame.Regulation[SnapshotFrame.RegulationRow(target, s)][group])
                    on++;
            string label = on > 0 ? $"⚙{on}" : "⚙";
            if (GUILayout.Button(new GUIContent(label, "Regulation: which falling species turn this down"), GUILayout.Width(30)))
                _regulationTarget = _regulationTarget == target ? -1 : target;
        }

        /// <summary>
        /// The maintenance the cell can turn down as species fall (once Regulation is unlocked and on): a row with a cog
        /// per entry of <see cref="SessionSchema.MaintenanceRegulation"/>. All off by default.
        /// </summary>
        private void DrawMaintenance(SessionSchema schema, SnapshotFrame frame, int group)
        {
            if (!Active(schema, frame, GameFeatures.Regulation))
                return;
            GUILayout.Space(6);
            GUILayout.Label("Maintenance", _header);
            GUILayout.Label("What the cell spends to stay as it is. Starving bacteria cut it: tick a falling species to turn " +
                            "a part down as that species falls (no effect at rest).", _small);
            for (int k = 0; k < SessionSchema.MaintenanceRegulation.Count; k++)
            {
                var (_, name, effect) = SessionSchema.MaintenanceRegulation[k];
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(name, effect), GUILayout.Width(Width - 84));
                DrawCog(schema, frame, group, schema.MaintenanceTarget(k));
                GUILayout.EndHorizontal();
            }
        }

        // The regulation popup: a toggle per species ("dropping lowers this") and the function, fixed for now.
        private void DrawRegulationPopup(SessionBridge bridge, SessionSchema schema, SnapshotFrame frame, int group)
        {
            if (!Active(schema, frame, GameFeatures.Regulation) || _regulationTarget >= schema.RegulationTargets)
            {
                _regulationTarget = -1;
                return;
            }
            const float w = 360f, h = 330f;
            var rect = new Rect(Screen.width - Width - w - 8f, Top + 40f, w, h);
            string what = _regulationTarget < schema.Traits.Count ? schema.Traits[_regulationTarget].Label
                : _regulationTarget == schema.GrowthRegulation ? "Energy to growth"
                : SessionSchema.MaintenanceRegulation[_regulationTarget - schema.GrowthRegulation - 1].Name;
            GUILayout.Window(0x5E6017, rect, id => DrawRegulation(bridge, frame, group, what), "Regulation");
        }

        private void DrawRegulation(SessionBridge bridge, SnapshotFrame frame, int group, string what)
        {
            int target = _regulationTarget;
            GUILayout.Label(what, _header);
            GUILayout.Label($"For every cell of group {group + 1}: when a ticked species falls, this target is turned down.", _small);
            for (int s = 0; s < SessionSchema.RegulationSpecies.Count; s++)
            {
                bool on = frame.Regulation[SnapshotFrame.RegulationRow(target, s)][group];
                bool want = GUILayout.Toggle(on, $"Dropping {SessionSchema.RegulationSpecies[s].Name} lowers this");
                if (want != on)
                    bridge.Send(SimCommands.SetRegulation(group, target, s, want));
            }
            GUILayout.Space(6);
            GUILayout.Label("Function (fixed for now)", _header);
            GUILayout.Label("target × f for each ticked species: f = 1 / (1 + uⁿ), u = the shortfall below the resting level " +
                            "(1 at half of it), n = 4. f is exactly 1 at and above rest (no effect), 0.5 at half the resting " +
                            "level, 0.06 when the species is gone. For pH, f falls as the pH drops (half-way 0.3 below rest). " +
                            "Several ticked species multiply.", _small);
            GUILayout.Label("Experts will be able to choose the function.", _small);
            GUILayout.Space(4);
            GUILayout.Label("A lowered protein target stops the protein being made, but the copies the cell has fall only as " +
                            "they are degraded (about 1% an hour), so it saves little in a famine of minutes; turning down " +
                            "maintenance acts at once. Turning down glycolysis or the sugar carriers for long leaves the cell " +
                            "short of them when food returns.", _small);
            if (GUILayout.Button("Close"))
                _regulationTarget = -1;
        }

        /// <summary>
        /// The creature's swimming and where its energy goes: the motors' proton-motive power, the mechanical power of the
        /// thrust (only a fraction: flagella are inefficient) and the heat the body's motion has put into the water.
        /// </summary>
        // Glucose leaving through the facilitator (its own flux, not the total): the cell is paying to bring in glucose
        // that runs back out.
        private void DrawLeakHint(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            if (!CellHints.GlfLeaking(schema, frame, cell))
                return;
            var old = GUI.color;
            GUI.color = new Color(1f, 0.8f, 0.4f);
            GUILayout.Label("Glucose is leaking out through Glf: there is more sugar inside the cell than outside. " +
                            "Turn Glf down, or evolve Carbon regulation to do it automatically.", _small);
            GUI.color = old;
        }

        private void DrawSwimming(SnapshotFrame frame)
        {
            GUILayout.Space(6);
            GUILayout.Label("Swimming (the whole creature)", _header);
            float v = Mathf.Sqrt(frame.CreatureVx * frame.CreatureVx + frame.CreatureVy * frame.CreatureVy) * 1e6f;
            Row("Speed over the ground", $"{v:F1} µm/s");
            Row("Turning", $"{frame.Spin * Mathf.Rad2Deg:F1} °/s");
            Row("Motors", frame.MotorOn ? "running" : "paused");
            Row("Motor power (proton motive)", $"{frame.MotorPower * 1e15f:F3} fW");
            Row("Mechanical power of the thrust", $"{frame.MechanicalPower * 1e18f:F2} aW");
            float eff = frame.MotorPower > 0f ? frame.MechanicalPower / frame.MotorPower : 0f;
            Row("Propulsive efficiency", frame.MotorPower > 0f ? $"{100f * eff:F2} %" : "–");
            Row("Heat put into the water by moving", $"{frame.HeatToWater * 1e15:F3} fJ");
        }

        private void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _small);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, _right);
            GUILayout.EndHorizontal();
        }

        // The cell's vital signs, always shown: ATP, glucose inside and at the membrane, pH, membrane potential, damage,
        // each coloured by how far it is from a healthy cell's.
        private void DrawVitals(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            GUILayout.Label("Vital signs", _header);
            double V(string id) { int d = Find(schema, id); return d >= 0 ? Display(schema, frame, cell, d) : double.NaN; }
            double atp = V("atp"), glcIn = V("glucose_inside"), glcOut = V("medium_glucose"), ph = V("ph"), v = V("v"), damage = V("damage");
            Vital("ATP", $"{atp:F2} mM", atp >= 8.0 ? Good : atp >= 5.0 ? Watch : Bad,
                "the energy currency; a healthy fermenter holds ~10 mM. Below half that its cells count as starving; at zero it cannot restart glycolysis");
            Vital("Glucose", $"{glcIn:G3} mM inside · {glcOut:G3} mM at the membrane", glcIn > 0.01 ? Good : glcIn > 0.001 ? Watch : Bad,
                "glucose inside the cell (what glycolysis uses) and in the water at its membrane");
            Vital("pH", $"{ph:F2}", ph >= 7.2 ? Good : ph >= 6.8 ? Watch : Bad,
                "the cytosol's pH (healthy ~7.6); lactic acid and failing H⁺ pumps lower it");
            Vital("Membrane potential", $"{v:F0} mV", v <= -80.0 ? Good : v <= -40.0 ? Watch : Bad,
                "inside negative (healthy ~ -100 mV); when the pumps stop it runs down towards 0 as ions leak");
            Vital("Damage", $"{damage:F0}% of lethal", DamageColour(damage),
                "the sum of the parts in the bar below; the cell dies at 100%. Repair mends each part (and costs ATP)");
            var parts = new double[DamageParts.Length];
            for (int k = 0; k < DamageParts.Length; k++)
            {
                int d = Find(schema, DamageParts[k].Id);
                parts[k] = d >= 0 ? frame.Details[d][cell] : 0.0;
            }
            DamageBar(GUILayoutUtility.GetRect(Width - 40, 14), parts, 1.0);
            GUILayout.BeginHorizontal();
            for (int k = 0; k < DamageParts.Length; k++)
                if (parts[k] > 0.0005)
                    Swatch(DamageParts[k].Colour, $"{DamageParts[k].Name} {100.0 * parts[k]:F1}%", DamageParts[k].Tip);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        /// <summary>The parts of a cell's damage, in the bar's order: detail id, name, colour, and what it is.</summary>
        public static readonly (string Id, string Name, Color Colour, string Tip)[] DamageParts =
        {
            ("damage_physical", "Physical", new Color(0.90f, 0.55f, 0.30f), "the envelope (membrane and wall), from attacks"),
            ("damage_radiation", "Radiation", new Color(0.85f, 0.85f, 0.35f), "DNA lesions from radiation"),
            ("damage_ribosome", "Ribosome", new Color(0.75f, 0.45f, 0.90f),
                "wear, Ca²⁺ overload and K⁺ loss: ribosomes need Mg²⁺ and K⁺ to hold together and translate"),
            ("damage_energy", "De-energisation", new Color(0.95f, 0.35f, 0.40f),
                "proteins aggregating once ATP runs out and chaperones stop refolding them"),
        };

        /// <summary>
        /// A damage bar: each part (a share of a lethal load, in <see cref="DamageParts"/> order, then any extra parts in
        /// grey) stacked from the left over a bar that is 100% at its right end; a white tick at <paramref name="worst"/>
        /// when below 1.
        /// </summary>
        public static void DamageBar(Rect bar, IReadOnlyList<double> parts, double worst, Color? extra = null)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.2f, 0.22f, 0.25f), 0f, 0f);
            float x = bar.x;
            for (int k = 0; k < parts.Count; k++)
            {
                float w = Mathf.Min(bar.xMax - x, (float)System.Math.Max(0.0, parts[k]) * bar.width);
                var colour = k < DamageParts.Length ? DamageParts[k].Colour : extra ?? new Color(0.45f, 0.45f, 0.5f);
                GUI.DrawTexture(new Rect(x, bar.y, w, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                    colour, 0f, 0f);
                x += w;
            }
            if (worst < 1.0)
            {
                float wx = bar.x + Mathf.Clamp01((float)worst) * bar.width;
                GUI.DrawTexture(new Rect(wx - 1f, bar.y - 2f, 2f, bar.height + 4f), Texture2D.whiteTexture, ScaleMode.StretchToFill,
                    false, 0f, Color.white, 0f, 0f);
            }
        }

        private static void Swatch(Color colour, string text, string tip)
        {
            var dot = GUILayoutUtility.GetRect(10, 16, GUILayout.Width(10));
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(dot.x, dot.y + 4, 8, 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, colour, 0f, 0f);
            GUILayout.Label(new GUIContent(text, tip), GUILayout.ExpandWidth(false));
        }

        private static Color DamageColour(double percent) => percent < 30.0 ? Good : percent < 70.0 ? Watch : Bad;

        private void Vital(string name, string value, Color colour, string tip)
        {
            GUILayout.BeginHorizontal();
            var dot = GUILayoutUtility.GetRect(10, 18, GUILayout.Width(10));
            if (Event.current.type == EventType.Repaint)
                GUI.DrawTexture(new Rect(dot.x, dot.y + 5, 8, 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, colour, 0f, 4f);
            GUILayout.Label(new GUIContent(name, tip), GUILayout.Width(140));
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent(value, tip), _right);
            GUILayout.EndHorizontal();
        }

        // ATP made and used as two stacked bars on one scale (the larger fills the width), each split by where it comes
        // from or goes, and the balance: how starved the cell is at a glance.
        private void DrawEnergyBalance(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            GUILayout.Label("Energy balance", _header);
            double made = 0.0, used = 0.0;
            for (int d = 0; d < schema.Details.Count; d++)
            {
                if (InBalance(schema.Details[d], "ATP made"))
                    made += System.Math.Max(0.0, frame.Details[d][cell]);
                if (InBalance(schema.Details[d], "ATP used"))
                    used += System.Math.Max(0.0, frame.Details[d][cell]);
            }
            double scale = System.Math.Max(made, used);
            int any = Find(schema, "atp_turnover");
            double perSecond = any >= 0 ? schema.Details[any].DisplayMultiplier : 1.0;   // mol/s -> ATP/s
            BalanceBar(schema, frame, cell, "Generation", "ATP made", SourceColours, made, scale, perSecond);
            BalanceBar(schema, frame, cell, "Usage", "ATP used", SinkColours, used, scale, perSecond);
            double net = (made - used) * perSecond;
            var old = GUI.color;
            GUI.color = net >= 0.0 ? Good : Bad;
            GUILayout.BeginHorizontal();
            GUILayout.Label(net >= 0.0 ? "Surplus" : "Deficit", _header, GUILayout.Width(84));
            GUILayout.FlexibleSpace();
            GUILayout.Label(net >= 0.0 ? $"+{Format(net)} ATP/s" : $"-{Format(-net)} ATP/s", _header);
            GUILayout.EndHorizontal();
            if (net < 0.0)
                GUILayout.Label("using more than it makes: living off its reserves", _small);
            GUI.color = old;
            GUILayout.Space(4);
        }

        // An item of the energy balance: every detail of its group but the net.
        private static bool InBalance(DetailInfo detail, string group) => detail.Group == group && detail.Id != "atp_net";

        private void BalanceBar(SessionSchema schema, SnapshotFrame frame, int cell, string label, string group,
            Dictionary<string, Color> colours, double total, double scale, double perSecond)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(84));
            var bar = GUILayoutUtility.GetRect(Width - 230, 16, GUILayout.Width(Width - 230));
            GUILayout.Label($"{Format(total * perSecond)} ATP/s", _right, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            if (Event.current.type != EventType.Repaint)
                return;
            GUI.DrawTexture(bar, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, new Color(0.2f, 0.22f, 0.25f), 0f, 0f);
            if (!(scale > 0.0))
                return;
            float x = bar.x;
            for (int d = 0; d < schema.Details.Count; d++)
            {
                if (!InBalance(schema.Details[d], group))
                    continue;
                var colour = colours.TryGetValue(schema.Details[d].Id, out var c) ? c : new Color(0.6f, 0.65f, 0.7f);
                float w = (float)(System.Math.Max(0.0, frame.Details[d][cell]) / scale) * bar.width;
                GUI.DrawTexture(new Rect(x, bar.y, w, bar.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f,
                    colour, 0f, 0f);
                x += w;
            }
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
                Color? swatch = SinkColours.TryGetValue(schema.Details[d].Id, out var c) ? c
                    : SourceColours.TryGetValue(schema.Details[d].Id, out var m) ? m : (Color?)null;
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
            if (trait.Step > 0.0)
            {
                DrawCountSlider(bridge, trait, frame, cell, group, t);
                return;
            }
            if (trait.IsShare)
            {
                DrawShareSlider(bridge, trait, frame, cell, group, t);
                return;
            }
            float current = frame.TraitValues[t][cell], target = frame.TraitTargets[t][cell];
            GUILayout.BeginHorizontal();
            var swatch = GUI.color;
            GUI.color = AdaptOrDeity.Presentation.ProteinColours.For(t);
            GUILayout.Label("■", GUILayout.Width(14));   // its colour on the membrane
            GUI.color = swatch;
            GUILayout.Label(trait.Label, GUILayout.Width(226));
            GUILayout.FlexibleSpace();
            string status = Mathf.Abs(current - target) > 1e-3f ? $"×{current:F2} → ×{target:F2}" : $"×{current:F2}";
            GUILayout.Label(status, _right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            float lo = Mathf.Log((float)trait.Min), hi = Mathf.Log((float)trait.Max);
            float moved = GUILayout.HorizontalSlider(Mathf.Log(_sliders[t]), lo, hi, GUILayout.Width(Width - 124));
            if (GUILayout.Button("1×", GUILayout.Width(34)))
                moved = 0f;
            DrawCog(bridge.Schema, frame, group, t);
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

        /// <summary>
        /// A carrier the starting cell lacks: its share of the membrane's room for proteins, from 0 to the room the other
        /// proteins leave.
        /// </summary>
        private void DrawShareSlider(SessionBridge bridge, TraitInfo trait, SnapshotFrame frame, int cell, int group, int t)
        {
            float current = frame.TraitValues[t][cell], target = frame.TraitTargets[t][cell];
            GUILayout.BeginHorizontal();
            var swatch = GUI.color;
            GUI.color = AdaptOrDeity.Presentation.ProteinColours.For(t);
            GUILayout.Label("■", GUILayout.Width(14));   // its colour on the membrane
            GUI.color = swatch;
            GUILayout.Label(trait.Label, GUILayout.Width(226));
            GUILayout.FlexibleSpace();
            string status = Mathf.Abs(current - target) > 1e-3f
                ? $"{100f * current:F1}% → {100f * target:F1}%"
                : $"{100f * current:F1}%";
            GUILayout.Label(status, _right);
            GUILayout.EndHorizontal();
            float room = Mathf.Max((float)CellHints.ShareRoom(bridge.Schema, frame, cell, t), target);
            GUILayout.BeginHorizontal();
            float moved = GUILayout.HorizontalSlider(_sliders[t], 0f, Mathf.Max(room, 1e-3f), GUILayout.Width(Width - 84));
            DrawCog(bridge.Schema, frame, group, t);
            GUILayout.EndHorizontal();
            if (Mathf.Abs(moved - _sliders[t]) > 1e-4f)
            {
                _sliders[t] = moved;
                bridge.Send(SimCommands.SetTrait(group, t, moved));
            }
            else if (Mathf.Abs(_sliders[t] - target) > 1e-4f && GUIUtility.hotControl == 0)
                _sliders[t] = target;   // the membrane room clamped the request: show what was accepted
        }

        /// <summary>A whole-number trait (e.g. flagella): the count built so far and the number asked for.</summary>
        private void DrawCountSlider(SessionBridge bridge, TraitInfo trait, SnapshotFrame frame, int cell, int group, int t)
        {
            float current = frame.TraitValues[t][cell], target = frame.TraitTargets[t][cell];
            GUILayout.BeginHorizontal();
            var swatch = GUI.color;
            GUI.color = AdaptOrDeity.Presentation.ProteinColours.For(t);
            GUILayout.Label("■", GUILayout.Width(14));   // its colour on the membrane
            GUI.color = swatch;
            GUILayout.Label(new GUIContent(trait.Label, "each one costs ATP to build and protons while it runs"), GUILayout.Width(226));
            GUILayout.FlexibleSpace();
            string status = Mathf.Abs(current - target) > 1e-3f ? $"{current:F1} → {target:F0} (building)" : $"{target:F0}";
            GUILayout.Label(status, _right);
            GUILayout.EndHorizontal();
            float step = (float)trait.Step;
            GUILayout.BeginHorizontal();
            float moved = GUILayout.HorizontalSlider(_sliders[t], (float)trait.Min, (float)trait.Max, GUILayout.Width(Width - 84));
            DrawCog(bridge.Schema, frame, group, t);
            GUILayout.EndHorizontal();
            float value = Mathf.Round(moved / step) * step;
            if (Mathf.Abs(value - _sliders[t]) > 1e-4f)
            {
                _sliders[t] = value;
                bridge.Send(SimCommands.SetTrait(group, t, value));
            }
            else if (Mathf.Abs(_sliders[t] - target) > 1e-3f && GUIUtility.hotControl == 0)
                _sliders[t] = target;   // the membrane room clamped the request: show what was accepted
        }

        private void DrawControl(SessionBridge bridge, SessionSchema schema, SnapshotFrame frame, ControlInfo control, int group,
            int c)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(control.Label, control.Description), GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{_controls[c]:F2} {control.Unit}", _right);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            float moved = GUILayout.HorizontalSlider(_controls[c], (float)control.Min, (float)control.Max, GUILayout.Width(Width - 84));
            if (control.Id == "growth")
                DrawCog(schema, frame, group, schema.GrowthRegulation);
            GUILayout.EndHorizontal();
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
                    GUILayout.Label(info.Cost > 0 && info.UnlockLevel <= frame.Level
                        ? $"{info.Name}: evolve it in Skills ({info.Cost} points)"
                        : $"{info.Name}: unlocks at level {info.UnlockLevel}", _small);
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
