using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// Shows the creature's motion over the ground: a fading wake through the positions it has passed (fixed to the
    /// ground, so it streams out behind) and an arrow along its velocity. The speed itself is shown in the HUD.
    /// </summary>
    public sealed class MotionView : MonoBehaviour
    {
        private const int WakePoints = 80;
        private const float WakeSpacing = 0.01f;     // world units between wake samples (0.1 µm)
        private const float ArrowLength = 0.18f;     // world units (1.8 µm), beyond the cell's edge

        private SessionBridge _bridge;
        private LineRenderer _wake, _shaft, _head;
        private readonly double[] _wx = new double[WakePoints], _wy = new double[WakePoints];
        private int _count, _next;

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            var material = new Material(Shader.Find("Sprites/Default"));
            _wake = Line("Wake", material, -5, new Color(0.75f, 0.92f, 1f, 0.0f), new Color(0.75f, 0.92f, 1f, 0.35f), 0.002f, 0.05f);
            _shaft = Line("Velocity", material, 5, new Color(1f, 0.95f, 0.55f, 0.9f), new Color(1f, 0.95f, 0.55f, 0.9f), 0.008f, 0.008f);
            _head = Line("Velocity head", material, 5, new Color(1f, 0.95f, 0.55f, 0.9f), new Color(1f, 0.95f, 0.55f, 0.9f), 0.008f, 0.008f);
        }

        private LineRenderer Line(string name, Material material, int order, Color start, Color end, float w0, float w1)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<LineRenderer>();
            l.material = material;
            l.useWorldSpace = false;
            l.sortingOrder = order;
            l.startColor = start;
            l.endColor = end;
            l.startWidth = w0;
            l.endWidth = w1;
            l.positionCount = 0;
            l.numCapVertices = 2;
            return l;
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null)
                return;
            double cx = _bridge.DisplayX, cy = _bridge.DisplayY;   // smooth between snapshots
            // sample the path every WakeSpacing of travel
            int last = (_next - 1 + WakePoints) % WakePoints;
            if (_count == 0 || System.Math.Abs(cx - _wx[last]) + System.Math.Abs(cy - _wy[last]) >= WakeSpacing)
            {
                _wx[_next] = cx;
                _wy[_next] = cy;
                _next = (_next + 1) % WakePoints;
                _count = Mathf.Min(_count + 1, WakePoints);
            }
            _wake.positionCount = _count;
            int start = (_next - _count + WakePoints) % WakePoints;
            for (int i = 0; i < _count; i++)
            {
                int j = (start + i) % WakePoints;   // oldest first: the line fades in from its old end
                _wake.SetPosition(i, new Vector3((float)(_wx[j] - cx), (float)(_wy[j] - cy), 0.5f));
            }

            float vx = f.CreatureVx, vy = f.CreatureVy, speed = Mathf.Sqrt(vx * vx + vy * vy);
            if (speed <= 0f)
            {
                _shaft.positionCount = 0;
                _head.positionCount = 0;
                return;
            }
            var dir = new Vector3(vx / speed, vy / speed, 0f);
            var normal = new Vector3(-dir.y, dir.x, 0f);
            var tip = dir * ArrowLength;
            _shaft.positionCount = 2;
            _shaft.SetPosition(0, dir * 0.06f);
            _shaft.SetPosition(1, tip);
            _head.positionCount = 3;
            _head.SetPosition(0, tip - dir * 0.04f + normal * 0.025f);
            _head.SetPosition(1, tip);
            _head.SetPosition(2, tip - dir * 0.04f - normal * 0.025f);
        }
    }
}
