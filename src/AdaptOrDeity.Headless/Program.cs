using System;
using System.Globalization;
using AdaptOrDeity.Game.Session;

namespace AdaptOrDeity.Headless
{
    internal static class Program
    {
        /// <summary>Runs a session without a front end. Usage: AdaptOrDeity.Headless [simSeconds]</summary>
        private static int Main(string[] args)
        {
            double duration = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 3600.0;
            const double realDt = 1.0 / 60.0;

            var session = new SimSession();
            while (session.SimTime < duration)
                session.Update(realDt);

            Console.WriteLine(FormattableString.Invariant(
                $"sim_time={session.SimTime:F3} s checkpoints={session.CheckpointIndex}"));
            return 0;
        }
    }
}
