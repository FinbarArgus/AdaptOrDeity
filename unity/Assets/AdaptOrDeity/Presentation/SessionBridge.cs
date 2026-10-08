using System.Collections.Generic;
using AdaptOrDeity.Game.Session;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The only bridge between Unity and the simulation. Owns the threaded <see cref="SimSession"/>, picks up
    /// the newest snapshot each frame and forwards commands. Everything else in Unity reads <see cref="Frame"/>.
    /// </summary>
    public sealed class SessionBridge : MonoBehaviour
    {
        private const float CellDiameterMetres = 2e-6f;

        private SimSession _session;
        private readonly List<CellView> _views = new List<CellView>();
        private readonly List<PacketView> _packets = new List<PacketView>();
        private Camera _camera;
        private float _zoom = 1f;   // multiplies the size that frames the colony

        /// <summary>Screen width the cell panel takes on the right [px]; the camera never draws under it.</summary>
        public float RightPanelWidth { get; set; }

        /// <summary>Newest snapshot (pooled: read it this frame, don't keep references across frames).</summary>
        public SnapshotFrame Frame { get; private set; }

        public SessionSchema Schema => _session.Schema;

        /// <summary>Overlay channel shown, or -1 for the plain view.</summary>
        public int ActiveOverlay { get; set; } = -1;

        /// <summary>Cell shown in the cell panel, or -1 for none.</summary>
        public int SelectedCell { get; set; } = -1;

        public static SessionBridge Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SessionBridge>() != null)
                return;
            var root = new GameObject("AdaptOrDeity");
            root.AddComponent<SessionBridge>();
        }

        public void Send(ISimCommand command) => _session.Enqueue(command);

        private float _pendingScroll;

        /// <summary>Zooms by scroll steps (positive = out); the HUD forwards wheel events that are not over the panel.</summary>
        public void Zoom(float steps) => _pendingScroll += steps;

        private void Awake()
        {
            Instance = this;
            _session = SimSession.CreateFermenter();
            _session.TryGetFrame(out var first);
            Frame = first;
            _session.Start();

            _camera = EnsureCamera();
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
                // Positions come from the simulation (metres), so food contact and drawing agree.
                go.transform.localPosition = new Vector3(Frame.CellX[_views.Count] * CellView.UnitsPerMetre,
                    Frame.CellY[_views.Count] * CellView.UnitsPerMetre, 0f);
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
            FrameCamera();
        }

        /// <summary>The food packet under a world point, or null.</summary>
        public PacketView PacketAt(Vector2 world)
        {
            foreach (var p in _packets)
                if (p.Contains(world))
                    return p;
            return null;
        }

        /// <summary>
        /// The camera draws only the screen left of the cell panel (its viewport stops at the panel's edge), so framing
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
                SyncViews();
            }
            // Mouse-wheel zoom, read through IMGUI-independent legacy-or-new input: the scroll delta from the event queue.
            if (Mathf.Abs(_pendingScroll) > 0f)
            {
                _zoom *= Mathf.Pow(1.1f, _pendingScroll);
                _pendingScroll = 0f;
            }
            FrameCamera();
            while (_session.TryDequeueEvent(out var e))
            {
                if (e is CommandRejected rejected)
                    Debug.LogWarning($"Simulation rejected a command: {rejected.Reason}");
            }
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
