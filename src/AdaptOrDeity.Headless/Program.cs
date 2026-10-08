using System;
using System.Globalization;
using System.Linq;
using AdaptOrDeity.Game.Session;

namespace AdaptOrDeity.Headless
{
    internal static class Program
    {
        /// <summary>
        /// Runs a session without a front end.
        /// <c>AdaptOrDeity.Headless [simSeconds] [bathO2_mM]</c>: the M2 one-cell world.
        /// <c>AdaptOrDeity.Headless fermenter [simSeconds] [trait=multiplier ...]</c>: the fermenter cell and its panel.
        /// </summary>
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "fermenter")
                return Fermenter(args.Skip(1).ToArray());

            double duration = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 1.0;
            using var session = SimSession.CreateOneCell();
            if (args.Length > 1)
                session.Enqueue(SimCommands.SetBathConcentration(double.Parse(args[1], CultureInfo.InvariantCulture)));

            var frame = session.RunHeadless(duration);
            var o2 = session.Schema.Overlays[0];
            Console.WriteLine(FormattableString.Invariant(
                $"sim_time={frame.SimTime:F3} s  cell {o2.Name}={frame.Channels[0][0] * o2.DisplayMultiplier:F2} {o2.DisplayUnit}"));
            PrintRejections(session);
            return 0;
        }

        private static int Fermenter(string[] args)
        {
            double duration = args.Length > 0 ? double.Parse(args[0], CultureInfo.InvariantCulture) : 60.0;
            using var session = SimSession.CreateFermenter();
            var traits = session.Schema.Traits.ToList();
            foreach (var arg in args.Skip(1))
            {
                var parts = arg.Split('=');
                int t = traits.FindIndex(x => x.Id == parts[0]);
                if (t < 0)
                {
                    Console.Error.WriteLine($"unknown trait {parts[0]}; traits: {string.Join(", ", traits.Select(x => x.Id))}");
                    return 2;
                }
                session.Enqueue(SimCommands.SetTrait(0, t, double.Parse(parts[1], CultureInfo.InvariantCulture)));
            }
            var frame = session.RunHeadless(duration);
            Console.WriteLine(FormattableString.Invariant($"sim_time={frame.SimTime:F1} s"));
            string group = "";
            for (int d = 0; d < session.Schema.Details.Count; d++)
            {
                var info = session.Schema.Details[d];
                if (info.Group != group)
                    Console.WriteLine($"[{group = info.Group}]");
                Console.WriteLine(FormattableString.Invariant(
                    $"  {info.Name,-30} {frame.Details[d][0] * info.DisplayMultiplier,14:G5} {info.DisplayUnit}"));
            }
            Console.WriteLine("[Traits]");
            for (int t = 0; t < traits.Count; t++)
                Console.WriteLine(FormattableString.Invariant(
                    $"  {traits[t].Label,-45} x{frame.TraitValues[t][0]:F3} (target x{frame.TraitTargets[t][0]:F3})"));
            PrintRejections(session);
            return 0;
        }

        private static void PrintRejections(SimSession session)
        {
            while (session.TryDequeueEvent(out var e))
                if (e is CommandRejected rejected)
                    Console.Error.WriteLine($"rejected: {rejected.Reason}");
        }
    }
}
