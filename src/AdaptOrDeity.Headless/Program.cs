using System;
using System.Globalization;
using AdaptOrDeity.Game.Session;

namespace AdaptOrDeity.Headless
{
    internal static class Program
    {
        /// <summary>Runs the one-cell session without a front end. Usage: AdaptOrDeity.Headless [simSeconds] [bathO2_mM]</summary>
        private static int Main(string[] args)
        {
            double duration = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 1.0;
            using var session = SimSession.CreateOneCell();
            if (args.Length > 1)
                session.Enqueue(SimCommands.SetBathConcentration(double.Parse(args[1], CultureInfo.InvariantCulture)));

            var frame = session.RunHeadless(duration);
            var o2 = session.Schema.Overlays[0];
            Console.WriteLine(FormattableString.Invariant(
                $"sim_time={frame.SimTime:F3} s  cell {o2.Name}={frame.Channels[0][0] * o2.DisplayMultiplier:F2} {o2.DisplayUnit}"));
            while (session.TryDequeueEvent(out var e))
                if (e is CommandRejected rejected)
                    Console.Error.WriteLine($"rejected: {rejected.Reason}");
            return 0;
        }
    }
}
