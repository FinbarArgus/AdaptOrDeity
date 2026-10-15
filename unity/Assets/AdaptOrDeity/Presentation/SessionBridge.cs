using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The only bridge between Unity and the simulation. Owns the threaded session (<see cref="ISessionFront"/>), picks up
    /// the newest snapshot each frame and forwards commands. Everything else in Unity reads <see cref="Frame"/>.
    /// </summary>
    public sealed class SessionBridge : MonoBehaviour
    {
        private const float CellDiameterMetres = 2e-6f;

        private ISessionFront _session;
        private readonly List<CellView> _views = new List<CellView>();
        private readonly List<PacketView> _packets = new List<PacketView>();
        private readonly List<PreyView> _prey = new List<PreyView>();
        private readonly List<OrganismView> _organisms = new List<OrganismView>();
        private Camera _camera;
        private float _zoom = 1f;   // multiplies the size that frames the colony

        /// <summary>Screen width the cell panel takes on the right [px]; the camera never draws under it.</summary>
        public float RightPanelWidth { get; set; }

        /// <summary>Newest snapshot (pooled: read it this frame, don't keep references across frames).</summary>
        public SnapshotFrame Frame { get; private set; }

        public SessionSchema Schema => _session.Schema;

        /// <summary>Overlay channel shown, or -1 for the plain view (or a field overlay).</summary>
        public int ActiveOverlay
        {
            get => _activeOverlay;
            set
            {
                _activeOverlay = value;
                if (value >= 0)
                    _activeField = -1;
            }
        }

        private int _activeOverlay = -1, _activeField = -1;

        /// <summary>Field overlay shown (<see cref="FieldView.Overlays"/>), or -1; showing one clears the cell overlay.</summary>
        public int ActiveField
        {
            get => _activeField;
            set
            {
                _activeField = value;
                if (value >= 0)
                    _activeOverlay = -1;
            }
        }

        /// <summary>The water overlay's view (its colour range is the legend's).</summary>
        public FieldView FieldView { get; private set; }

        /// <summary>Cell shown in the cell panel, or -1 for none. Frames carry details for this cell only.</summary>
        public int SelectedCell
        {
            get => _selectedCell;
            set
            {
                if (value == _selectedCell)
                    return;
                _selectedCell = value;
                _session?.Enqueue(SimCommands.Inspect(value));
            }
        }

        private int _selectedCell = -1;

        /// <summary>
        /// Id of the free organism shown in the organism panel (read-only), or -1 for none. Frames carry its details only.
        /// </summary>
        public int SelectedOrganism
        {
            get => _selectedOrganism;
            set
            {
                if (value == _selectedOrganism)
                    return;
                _selectedOrganism = value;
                _session?.Enqueue(SimCommands.InspectOrganism(value));
            }
        }

        private int _selectedOrganism = -1;

        public static SessionBridge Instance { get; private set; }

        /// <summary>
        /// The body's pose as drawn this render frame: the newest snapshot's pose carried forward along its velocity and
        /// spin for the simulated time since it arrived (capped), so motion is smooth between the simulation's 0.1 s
        /// steps instead of jumping. In world units (DisplayX/Y) and radians.
        /// </summary>
        public double DisplayX { get; private set; }

        public double DisplayY { get; private set; }

        public float DisplayOrientation { get; private set; }

        /// <summary>Turn of the drawn body relative to the snapshot [rad]: rotate snapshot positions by it.</summary>
        public float DeltaAngle { get; private set; }

        /// <summary>Snapshot-relative positions plus this offset are relative to the drawn body [world units].</summary>
        public Vector2 Shift { get; private set; }

        private float _frameArrived;
        private bool _faultReported;
        private float _frameSpan = 0.1f;      // simulated seconds between the last two frames: carry the pose no further
        private double _lastFrameTime;

        /// <summary>A position on the body (metres, body frame, from its centre of mass), as drawn now (world units).</summary>
        public Vector2 BodyRelative(float x, float y)
        {
            float c = Mathf.Cos(DisplayOrientation), s = Mathf.Sin(DisplayOrientation), u = CellView.UnitsPerMetre;
            return new Vector2((c * x - s * y) * u, (s * x + c * y) * u);
        }

        private void UpdateDisplayPose()
        {
            var f = Frame;
            if (f == null)
                return;
            double speed = f.SpeedIndex < Schema.SpeedPresets.Count ? Schema.SpeedPresets[f.SpeedIndex] : 0.0;
            float lead = Mathf.Min(_frameSpan, (float)((Time.unscaledTime - _frameArrived) * speed));
            float u = CellView.UnitsPerMetre;
            DisplayX = f.CreatureX * u + f.CreatureVx * u * lead;
            DisplayY = f.CreatureY * u + f.CreatureVy * u * lead;
            DeltaAngle = f.Spin * lead;
            DisplayOrientation = f.Orientation + DeltaAngle;
            Shift = new Vector2((float)(f.CreatureX * u - DisplayX), (float)(f.CreatureY * u - DisplayY));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SessionBridge>() != null)
                return;
            var root = new GameObject("AdaptOrDeity");
            root.AddComponent<SessionBridge>();
        }

        public void Send(SimCommand command) => _session.Enqueue(command);

        private float _pendingScroll;

        /// <summary>Zooms by scroll steps (positive = out); the HUD forwards wheel events that are not over the panel.</summary>
        public void Zoom(float steps) => _pendingScroll += steps;

        /// <summary>Counts the sessions started (1 for the first); windows that remember a session's progress reset when it changes.</summary>
        public int Generation { get; private set; }

        private void Awake()
        {
            Instance = this;
            StartSession();

            _camera = EnsureCamera();
            gameObject.AddComponent<BackgroundView>().Initialise(this, _camera);
            var field = new GameObject("Water overlay");
            field.transform.SetParent(transform, false);
            FieldView = field.AddComponent<FieldView>();
            FieldView.Initialise(this);
            var seabed = new GameObject("Seabed");
            seabed.transform.SetParent(transform, false);
            seabed.AddComponent<SeabedView>().Initialise(this);
            gameObject.AddComponent<MotionView>().Initialise(this);
            SyncViews();
        }

        // The simulation library (NativeAOT: modern .NET, threaded) when it loads, else the managed simulation.
        private void StartSession()
        {
            _session = Sessions.CreateFermenter(preferNative: true, out string where);
            Debug.Log($"Simulation: {where}");
            _session.TryGetFrame(out var first);
            Frame = first;
            _session.Enqueue(SimCommands.ReseedSeabed(new System.Random().Next()));   // every game a new floor
            _session.Start();
            Generation++;
        }

        /// <summary>Ends this session and starts a new game from level 1 (after the evolutionary pathway has ended).</summary>
        public void Restart()
        {
            _session?.Dispose();
            _selectedCell = -1;
            _selectedOrganism = -1;
            StartSession();
            SyncViews();
        }

        /// <summary>Creates views for cells born since the last frame and frames the colony.</summary>
        private void SyncViews()
        {
            if (Frame == null)
                return;
            while (_views.Count < Frame.CellCount)
            {
                var go = new GameObject($"Cell {_views.Count}");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<CellView>();
                view.Initialise(this, _views.Count, CellDiameterMetres);
                _views.Add(view);
            }
            while (_packets.Count < Frame.PacketX.Length)
            {
                var go = new GameObject($"Food packet {_packets.Count}");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<PacketView>();
                view.Initialise(this, _packets.Count, CellView.CircleSprite());
                _packets.Add(view);
            }
            while (_prey.Count < Frame.PreyX.Length)
            {
                var go = new GameObject($"Prey {_prey.Count}");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<PreyView>();
                view.Initialise(this, _prey.Count, CellView.CircleSprite());
                _prey.Add(view);
            }
            while (_organisms.Count < Frame.OrganismX.Length)
            {
                var go = new GameObject($"Free organism {_organisms.Count}");
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<OrganismView>();
                view.Initialise(this, _organisms.Count, CellDiameterMetres, CellView.CircleSprite());
                _organisms.Add(view);
            }
            FrameCamera();
        }

        /// <summary>The free organism under a world point, or null.</summary>
        public OrganismView OrganismAt(Vector2 world)
        {
            foreach (var o in _organisms)
                if (o.Contains(world))
                    return o;
            return null;
        }

        /// <summary>The food packet under a world point, or null.</summary>
        public PacketView PacketAt(Vector2 world)
        {
            foreach (var p in _packets)
                if (p.Contains(world))
                    return p;
            return null;
        }

        /// <summary>The prey cell under a world point, or null.</summary>
        public PreyView PreyAt(Vector2 world)
        {
            foreach (var p in _prey)
                if (p.Contains(world))
                    return p;
            return null;
        }

        /// <summary>
        /// The view follows the creature: every position in a frame is relative to it, so the creature sits at the origin
        /// and the background, food and wake move past (a floating origin, which keeps float precision however far it
        /// travels). The camera draws only the screen left of the cell panel (its viewport stops at the panel's edge), so framing
        /// and zoom never reach under the panel. Zoom (mouse wheel) is clamped between a close-up of one cell and the
        /// whole colony with a margin.
        /// </summary>
        private void FrameCamera()
        {
            if (_camera == null)
                return;
            float panel = Screen.width > 0 ? Mathf.Clamp01(RightPanelWidth / Screen.width) : 0f;
            _camera.rect = new Rect(0f, 0f, 1f - panel, 1f);
            float radius = 0f;
            foreach (var v in _views)
                radius = Mathf.Max(radius, v.transform.localPosition.magnitude);
            // Frame the colony and the food field around it.
            float field = Schema != null ? (float)Schema.FieldHalfSize * CellView.UnitsPerMetre : 0f;
            float fit = Mathf.Max(0.6f, radius + 0.35f, field * 1.05f);
            _zoom = Mathf.Clamp(_zoom, 0.15f / fit, 1.5f);
            _camera.orthographicSize = fit * _zoom;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void Update()
        {
            if (_session.TryGetFrame(out var frame))
            {
                Frame = frame;
                _frameArrived = Time.unscaledTime;
                _frameSpan = Mathf.Clamp((float)(frame.SimTime - _lastFrameTime), 0f, 60f);
                _lastFrameTime = frame.SimTime;
                SyncViews();
            }
            UpdateDisplayPose();
            // Mouse-wheel zoom, read through IMGUI-independent legacy-or-new input: the scroll delta from the event queue.
            if (Mathf.Abs(_pendingScroll) > 0f)
            {
                _zoom *= Mathf.Pow(1.1f, _pendingScroll);
                _pendingScroll = 0f;
            }
            FrameCamera();
            if (_session.Fault != null && !_faultReported)
            {
                _faultReported = true;
                Debug.LogError($"The simulation stopped: {_session.Fault}");
            }
            while (_session.TryDequeueEvent(out var e))
            {
                if (e is CommandRejected rejected)
                    Debug.LogWarning($"Simulation rejected a command: {rejected.Reason}");
                else if (e is ModelExported exported)
                {
                    LastExport = exported.Path;
                    Debug.Log($"Creature model written: {exported.Path}");
                }
            }
        }

        /// <summary>The module array file of the last export, if any.</summary>
        public string LastExport { get; private set; }

        /// <summary>
        /// Writes the creature as a module array (libcuflynx and PhLynx's format) under the persistent data folder, one
        /// folder per export.
        /// </summary>
        public void ExportModel()
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "models",
                System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture));
            Send(SimCommands.ExportModel(dir, "creature"));
        }

        private void OnDestroy()
        {
            _session?.Dispose();
            if (Instance == this)
                Instance = null;
        }

        private static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 0.6f; // world units: 1 unit = 10 µm, so the view is 12 µm tall around a 2 µm cell
            cam.transform.position = new Vector3(0.25f, 0f, -10f); // cell left of centre, clear of the cell panel
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.23f, 0.33f); // water
            return cam;
        }
    }
}
