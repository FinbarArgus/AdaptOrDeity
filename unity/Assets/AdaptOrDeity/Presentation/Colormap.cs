using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>Overlay colour scale (perceptually ordered, viridis-like), as in ONI's overlay tabs.</summary>
    public static class Colormap
    {
        private static readonly Color[] Stops =
        {
            new Color(0.267f, 0.005f, 0.329f),
            new Color(0.231f, 0.322f, 0.545f),
            new Color(0.129f, 0.569f, 0.549f),
            new Color(0.369f, 0.788f, 0.384f),
            new Color(0.992f, 0.906f, 0.145f),
        };

        /// <summary>Colour for <paramref name="t"/> in [0, 1] (clamped).</summary>
        public static Color Evaluate(float t)
        {
            t = Mathf.Clamp01(t) * (Stops.Length - 1);
            int i = Mathf.Min((int)t, Stops.Length - 2);
            return Color.Lerp(Stops[i], Stops[i + 1], t - i);
        }
    }
}
