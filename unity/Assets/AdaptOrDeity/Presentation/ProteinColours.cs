using UnityEngine;

namespace AdaptOrDeity.Presentation
{
    /// <summary>One colour per adaptable protein (trait order), shared by the membrane drawing and the cell panel's legend.</summary>
    public static class ProteinColours
    {
        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.85f, 0.45f),   // flagella (drawn as tails)
            new Color(0.95f, 0.45f, 0.35f),   // H+-ATPase
            new Color(0.40f, 0.70f, 0.98f),   // NhaA
            new Color(0.75f, 0.55f, 0.95f),   // KefC
            new Color(0.55f, 0.90f, 0.75f),   // Trk
            new Color(0.98f, 0.65f, 0.85f),   // NCX
            new Color(0.98f, 0.75f, 0.30f),   // Glf
            new Color(0.70f, 0.85f, 0.40f),   // lactate exporter
            new Color(0.80f, 0.80f, 0.80f),   // glycolytic enzymes (cytosolic)
            new Color(0.35f, 0.55f, 0.95f),   // PTS
            new Color(0.95f, 0.30f, 0.65f),   // ABC importer
            new Color(0.45f, 0.95f, 0.45f),   // H+ glucose symporter
        };

        public static Color For(int trait) => Palette[trait % Palette.Length];
    }
}
