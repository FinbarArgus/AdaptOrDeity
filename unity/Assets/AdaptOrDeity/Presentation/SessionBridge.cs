using AdaptOrDeity.Game.Session;
using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The only bridge between Unity and the simulation: owns the <see cref="SimSession"/>, feeds it frame
    /// time and shows its state. M0 stub (IMGUI); M2 replaces this with the threaded session and the
    /// ONI-style overlay UI.
    /// </summary>
    public sealed class SessionBridge : MonoBehaviour
    {
        private static readonly string[] SpeedLabels = { "Pause", "1x", "2x", "3x" };

        private SimSession _session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SessionBridge>() == null)
                new GameObject(nameof(SessionBridge)).AddComponent<SessionBridge>();
        }

        private void Awake()
        {
            _session = new SimSession();
        }

        private void Update()
        {
            _session.Update(Time.deltaTime);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 360, 120), GUI.skin.box);
            GUILayout.Label($"Sim time: {_session.SimTime:F1} s    Checkpoints: {_session.CheckpointIndex}");
            _session.SpeedIndex = GUILayout.Toolbar(_session.SpeedIndex, SpeedLabels);
            GUILayout.EndArea();
        }
    }
}
