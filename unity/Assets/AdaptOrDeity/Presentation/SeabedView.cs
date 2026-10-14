using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>
    /// The seabed at the bottom of the world: the ground as a sandy band below its line, and the rocks around the creature
    /// (boulders, stacks and arches, from the simulation's frames) as grey discs with a darker rim. Placed relative to the
    /// creature like the food, so it scrolls as the creature moves.
    /// </summary>
    public sealed class SeabedView : MonoBehaviour
    {
        private static readonly Color Sand = new Color(0.55f, 0.47f, 0.33f, 1f);
        private static readonly Color SandTop = new Color(0.68f, 0.60f, 0.43f, 1f);
        private static readonly Color Stone = new Color(0.42f, 0.44f, 0.47f, 1f);
        private static readonly Color StoneRim = new Color(0.28f, 0.29f, 0.31f, 1f);
        private const float GroundWidth = 400f, GroundDepth = 200f, TopBand = 0.08f;   // world units

        private SessionBridge _bridge;
        private SpriteRenderer _ground, _groundTop;
        private readonly SpriteRenderer[] _rocks = new SpriteRenderer[AdaptOrDeity.Game.Session.SnapshotFrame.MaxRocks];
        private readonly SpriteRenderer[] _rims = new SpriteRenderer[AdaptOrDeity.Game.Session.SnapshotFrame.MaxRocks];

        public void Initialise(SessionBridge bridge)
        {
            _bridge = bridge;
            var square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            _ground = Part("Ground", square, Sand, -20);
            _groundTop = Part("Ground surface", square, SandTop, -19);
            var circle = CellView.CircleSprite();
            for (int i = 0; i < _rocks.Length; i++)
            {
                _rims[i] = Part($"Rock {i} rim", circle, StoneRim, -18);
                _rocks[i] = Part($"Rock {i}", circle, Stone, -17);
            }
        }

        private SpriteRenderer Part(string name, Sprite sprite, Color colour, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = colour;
            r.sortingOrder = order;   // under the food, prey and cells, over the water overlay
            r.enabled = false;
            return r;
        }

        private void LateUpdate()
        {
            var f = _bridge.Frame;
            if (f == null)
                return;
            float u = CellView.UnitsPerMetre;
            var shift = _bridge.Shift;
            float top = f.GroundY * u + shift.y;
            _ground.enabled = _groundTop.enabled = true;
            _ground.transform.localPosition = new Vector3(shift.x, top - 0.5f * GroundDepth, 0.3f);
            _ground.transform.localScale = new Vector3(GroundWidth, GroundDepth, 1f);
            _groundTop.transform.localPosition = new Vector3(shift.x, top - 0.5f * TopBand, 0.29f);
            _groundTop.transform.localScale = new Vector3(GroundWidth, TopBand, 1f);
            for (int i = 0; i < _rocks.Length; i++)
            {
                bool shown = i < f.RockCount;
                _rocks[i].enabled = _rims[i].enabled = shown;
                if (!shown)
                    continue;
                var p = new Vector3(f.RockX[i] * u + shift.x, f.RockY[i] * u + shift.y, 0.25f);
                float d = 2f * f.RockRadius[i] * u;
                _rims[i].transform.localPosition = p;
                _rims[i].transform.localScale = new Vector3(d * 1.08f, d * 1.08f, 1f);
                _rocks[i].transform.localPosition = p + new Vector3(0f, 0f, -0.01f);
                _rocks[i].transform.localScale = new Vector3(d, d, 1f);
            }
        }
    }
}
