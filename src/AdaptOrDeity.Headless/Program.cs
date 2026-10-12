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
        /// <c>AdaptOrDeity.Headless fermenter-trace simSeconds intervalSeconds [trait=multiplier|level=n|growth=x|storage=x|repair=x ...]</c>:
        /// CSV of the panel over time, with the settings applied after one interval.
        /// <c>AdaptOrDeity.Headless model directory [simSeconds] [level=n|trait=multiplier|growth=x|storage=x|repair=x ...]</c>:
        /// the creature's module array (creature_module_array.json, creature_parameters.csv) after the settings and the run.
        /// </summary>
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "fermenter")
                return Fermenter(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "fermenter-trace")
                return FermenterTrace(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "model")
                return Model(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "protocol")
                return ProtocolRun.Run(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "scale-bench")
                return ScaleBench.Run(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "colony-bench")
                return ColonyBench.Run(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "field-bench")
                return FieldBench.Run(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "bench")
                return Bench.Run(args.Skip(1).ToArray());
            if (args.Length > 0 && args[0] == "bot")
                return Bot(args.Skip(1).ToArray());

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

        // bot left|mediocre|great|NAME [level=n] [food=packets/s] [ocean=mol/m3] [key=value ...]: a scripted player through one level (the
        // named reference player, or NAME with the settings, ScriptedPlayer.Parse); prints its outcome.
        private static int Bot(string[] args)
        {
            string name = args.Length > 0 ? args[0] : "left";
            var rest = args.Skip(1).ToList();
            int level = 1;
            double food = double.NaN, ocean = double.NaN;
            foreach (var a in rest.ToList())
            {
                if (a.StartsWith("ocean=", StringComparison.Ordinal))
                {
                    ocean = double.Parse(a.Substring(6), CultureInfo.InvariantCulture);
                    rest.Remove(a);
                    continue;
                }
                if (a.StartsWith("level=", StringComparison.Ordinal))
                    level = int.Parse(a.Substring(6), CultureInfo.InvariantCulture);
                else if (a.StartsWith("food=", StringComparison.Ordinal))
                    food = double.Parse(a.Substring(5), CultureInfo.InvariantCulture);
                else
                    continue;
                rest.Remove(a);
            }
            var player = name switch
            {
                "left" => Bots.LeftAlone(),
                "mediocre" => Bots.Mediocre(),
                "great" => Bots.Great(),
                _ => ScriptedPlayer.Parse(name, rest),
            };
            using var world = new FermenterWorld { Workers = 1 };
            if (level != 1)
                world.Level = level;
            if (!double.IsNaN(food))
                world.Food.SetArrivals(food, world.Food.Settings.CarriedShare);
            if (!double.IsNaN(ocean))
                world.BathConcentration = ocean;
            bool trace = Environment.GetEnvironmentVariable("AOD_BOT_TRACE") == "1";
            Console.WriteLine(BotOutcome.Play(player, world, report: trace ? (w, t) => Console.WriteLine(FormattableString.Invariant(
                $"t {t / 3600:F0} h ATP {w.Cell(0).Atp:F2} X {w.Body.Body.X:R} gap {w.TimeWithoutFood:F0} eaten {w.FoodEaten:G4} survival {w.SurvivalMeter:F1} damage {w.DamageMeter:F1} reg {w.FoodGapMetAt:F0}"))
                : null));
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

        private static int FermenterTrace(string[] args)
        {
            double duration = double.Parse(args[0], CultureInfo.InvariantCulture);
            double interval = double.Parse(args[1], CultureInfo.InvariantCulture);
            var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var frame = loop.CreateFrame();
            var schema = loop.Schema;
            var traits = schema.Traits.ToList();
            long stepsPerInterval = (long)Math.Round(interval / world.MacroDt);
            Console.WriteLine("time_s," + string.Join(",", schema.Details.Select(d => d.Id)) + "," +
                              string.Join(",", traits.Select(t => "trait_" + t.Id)));
            for (long i = 0; i * interval <= duration + 1e-9; i++)
            {
                if (i == 1)
                    foreach (var arg in args.Skip(2))
                    {
                        // trait=multiplier, level=n, or a control: growth=, storage=, repair= (group 0)
                        var parts = arg.Split('=');
                        double v = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        int control = schema.Controls.ToList().FindIndex(c => c.Id == parts[0]);
                        if (parts[0] == "glucose")
                            world.BathConcentration = v;   // background glucose [mM], e.g. to stand for sitting on a packet
                        else if (parts[0] == "level")
                            loop.Enqueue(SimCommands.SetLevel((int)v));
                        else if (control >= 0)
                            loop.Enqueue(SimCommands.SetControl(0, control, v));
                        else
                            loop.Enqueue(SimCommands.SetTrait(0, traits.FindIndex(x => x.Id == parts[0]), v));
                    }
                loop.ApplyCommands();
                if (i > 0)
                    loop.StepExactly(stepsPerInterval);
                loop.WriteSnapshot(frame);
                Console.WriteLine(FormattableString.Invariant($"{frame.SimTime:R},") +
                                  string.Join(",", Enumerable.Range(0, schema.Details.Count).Select(d =>
                                      frame.Details[d][0].ToString("R", CultureInfo.InvariantCulture))) + "," +
                                  string.Join(",", Enumerable.Range(0, traits.Count).Select(t =>
                                      frame.TraitValues[t][0].ToString("R", CultureInfo.InvariantCulture))));
            }
            return 0;
        }

        private static int Model(string[] args)
        {
            string directory = args[0];
            double duration = args.Length > 1 ? double.Parse(args[1], CultureInfo.InvariantCulture) : 0.0;
            var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            var traits = loop.Schema.Traits.ToList();
            var controls = loop.Schema.Controls.ToList();
            foreach (var arg in args.Skip(2))
            {
                var parts = arg.Split('=');
                double v = double.Parse(parts[1], CultureInfo.InvariantCulture);
                int control = controls.FindIndex(c => c.Id == parts[0]);
                int trait = traits.FindIndex(t => t.Id == parts[0]);
                if (parts[0] == "level")
                    loop.Enqueue(SimCommands.SetLevel((int)v));
                else if (control >= 0)
                    loop.Enqueue(SimCommands.SetControl(0, control, v));
                else if (trait >= 0)
                    loop.Enqueue(SimCommands.SetTrait(0, trait, v));
                else
                {
                    Console.Error.WriteLine($"unknown setting {parts[0]}; traits: {string.Join(", ", traits.Select(t => t.Id))}; " +
                                            $"controls: {string.Join(", ", controls.Select(c => c.Id))}");
                    return 2;
                }
            }
            loop.ApplyCommands();
            loop.StepExactly((long)Math.Round(duration / world.MacroDt));
            loop.Enqueue(SimCommands.ExportModel(directory, "creature"));
            loop.ApplyCommands();
            int status = 1;
            while (loop.TryDequeueEvent(out var e))
                if (e is ModelExported exported)
                {
                    Console.WriteLine(exported.Path);
                    status = 0;
                }
                else if (e is CommandRejected rejected)
                    Console.Error.WriteLine($"rejected: {rejected.Reason}");
            return status;
        }

        private static void PrintRejections(SimSession session)
        {
            while (session.TryDequeueEvent(out var e))
                if (e is CommandRejected rejected)
                    Console.Error.WriteLine($"rejected: {rejected.Reason}");
        }
    }
}
