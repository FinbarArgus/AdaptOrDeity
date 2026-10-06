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
        private SimSession _session;

        /// <summary>Newest snapshot (pooled: read it this frame, don't keep references across frames).</summary>
        public SnapshotFrame Frame { get; private set; }

        public SessionSchema Schema => _session.Schema;

        /// <summary>Overlay channel shown, or -1 for the plain view.</summary>
        public int ActiveOverlay { get; set; } = -1;

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

        private void Awake()
        {
            Instance = this;
            _session = SimSession.CreateOneCell();
            _session.TryGetFrame(out var first);
            Frame = first;
            _session.Start();

            EnsureCamera();
            var cell = new GameObject("Cell");
            cell.transform.SetParent(transform, false);
            cell.AddComponent<CellView>().Initialise(this, cellIndex: 0);
        }

        private void Update()
        {
            if (_session.TryGetFrame(out var frame))
                Frame = frame;
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

        private static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.orthographicSize = 5f; // world units: 1 unit = 10 µm
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.23f, 0.33f); // water
        }
    }
}
