using System;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Rules the cell panel uses to show hints and slider ranges, read from a snapshot frame (so the front end and tests
    /// share them).
    /// </summary>
    public static class CellHints
    {
        /// <summary>Detail ids the rules read.</summary>
        public const string GlfUptake = "glucose_glf", MembraneUsed = "membrane_used";

        /// <summary>
        /// True while glucose leaks out of the cell through the facilitator Glf (its own flux is negative: more sugar
        /// inside than outside), whatever the other carriers bring in.
        /// </summary>
        public static bool GlfLeaking(SessionSchema schema, SnapshotFrame frame, int cell)
        {
            int d = Find(schema, GlfUptake);
            return d >= 0 && frame.Details[d][cell] < 0f;
        }

        /// <summary>
        /// The largest share of the membrane ceiling trait <paramref name="trait"/> (a share trait) can take in the cell:
        /// the room the other membrane proteins leave now (the simulation clamps to the room their targets leave).
        /// </summary>
        public static double ShareRoom(SessionSchema schema, SnapshotFrame frame, int cell, int trait)
        {
            int d = Find(schema, MembraneUsed);
            if (d < 0)
                return schema.Traits[trait].Max;
            double others = frame.Details[d][cell] - frame.TraitValues[trait][cell];
            return Math.Max(0.0, Math.Min(schema.Traits[trait].Max, 1.0 - others));
        }

        private static int Find(SessionSchema schema, string id)
        {
            for (int d = 0; d < schema.Details.Count; d++)
                if (schema.Details[d].Id == id)
                    return d;
            return -1;
        }
    }
}
