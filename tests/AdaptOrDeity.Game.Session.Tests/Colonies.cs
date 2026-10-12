using System;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Test worlds whose daughters stay: multicellularity is a skill (level 2, points), and without it a daughter leaves the
    /// creature at division. These buy it with granted points (FermenterWorld.DevGrantPoints).
    /// </summary>
    internal static class Colonies
    {
        private static int Index => GameFeatures.IndexOf(GameFeatures.Multicellularity);

        private static int Cost => GameFeatures.All[Index].Cost;

        /// <summary>Buys multicellularity in a world at level 2 or later.</summary>
        public static void Multicellular(FermenterWorld world)
        {
            world.DevGrantPoints(Cost);
            world.BuyFeature(Index);
        }

        /// <summary>Sends the commands that buy multicellularity (after a level of 2 or later is set).</summary>
        public static void Multicellular(Action<SimCommand> send)
        {
            send(SimCommands.DevGrantPoints(Cost));
            send(SimCommands.BuyFeature(Index));
        }
    }
}
