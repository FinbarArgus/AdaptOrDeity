using System;
using AdaptOrDeity.Game.Content;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A request from the front end, applied on the simulation thread at the start of the next tick.
    /// The front end never touches simulation state directly.
    /// </summary>
    public interface ISimCommand
    {
        void Apply(SimLoop loop);
    }

    /// <summary>What a <see cref="SimCommand"/> does.</summary>
    public enum SimCommandKind
    {
        SetSpeed = 1,
        AddProbe = 2,
        ClearProbes = 3,
        SetBathConcentration = 4,
        SetTrait = 5,
        SetControl = 6,
        ExportModel = 7,
        Divide = 8,
        SetFeature = 9,
        SetLevel = 10,
        DevCompleteDaughter = 11,
        SetFlagellumAngle = 12,
        Inspect = 13,
        BuyFeature = 14,
        DevGrantPoints = 15,
        DevSpawnPrey = 16,
        InspectOrganism = 17,
        SetRegulation = 18,
        DevEndLevel = 19,
        ReseedSeabed = 20,
        LuckBlow = 21,
        LuckStop = 22,
    }

    /// <summary>
    /// A command as plain data (a kind, three integers, a number and two strings), so it can cross into the simulation
    /// library through its C entry points as well as be applied in process.
    /// </summary>
    public sealed class SimCommand : ISimCommand
    {
        public SimCommand(SimCommandKind kind, int a = 0, int b = 0, int c = 0, double x = 0.0, string? text1 = null,
            string? text2 = null)
        {
            Kind = kind;
            A = a;
            B = b;
            C = c;
            X = x;
            Text1 = text1;
            Text2 = text2;
        }

        public SimCommandKind Kind { get; }

        public int A { get; }

        public int B { get; }

        public int C { get; }

        public double X { get; }

        public string? Text1 { get; }

        public string? Text2 { get; }

        public void Apply(SimLoop loop)
        {
            switch (Kind)
            {
                case SimCommandKind.SetSpeed:
                    loop.SpeedIndex = A;
                    return;
                case SimCommandKind.AddProbe:
                    loop.AddProbe(A, B);
                    return;
                case SimCommandKind.ClearProbes:
                    loop.ClearProbes();
                    return;
                case SimCommandKind.SetBathConcentration:
                    if (!(loop.World is IBathWorld bath))
                        throw new InvalidOperationException("this world has no bath");
                    bath.BathConcentration = X;
                    return;
                case SimCommandKind.SetTrait:
                    Inspectable(loop).SetTrait(A, B, X);
                    return;
                case SimCommandKind.SetControl:
                    Inspectable(loop).SetControl(A, B, X);
                    return;
                case SimCommandKind.SetRegulation:
                    Inspectable(loop).SetRegulation(A, B, C, X != 0.0);
                    return;
                case SimCommandKind.ExportModel:
                    ExportModel(loop, Text1 ?? "", Text2 ?? "creature");
                    return;
                case SimCommandKind.Divide:
                    Inspectable(loop).Divide(A);
                    return;
                case SimCommandKind.SetFeature:
                    Inspectable(loop).SetFeature(A, B != 0);
                    return;
                case SimCommandKind.BuyFeature:
                    Inspectable(loop).BuyFeature(A);
                    return;
                case SimCommandKind.ReseedSeabed:
                    Fermenter(loop, "this world has no seabed").ReseedSeabed(A);
                    break;
                case SimCommandKind.LuckBlow:
                {
                    // the arrow as text, "tailX tailY headX headY" in metres relative to the creature (round-trip format)
                    var parts = (Text1 ?? "").Split(' ');
                    if (parts.Length != 4)
                        throw new FormatException($"LuckBlow wants four numbers, got '{Text1}'");
                    double Number(int i) => double.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
                    Fermenter(loop, "this world has no Luck").LuckBlow(Number(0), Number(1), Number(2), Number(3));
                    break;
                }
                case SimCommandKind.LuckStop:
                    Fermenter(loop, "this world has no Luck").LuckStop();
                    break;
                case SimCommandKind.SetLevel:
                    if (A < 1)
                        throw new ArgumentOutOfRangeException("level", "levels start at 1");
                    Inspectable(loop).Level = A;
                    return;
                case SimCommandKind.DevCompleteDaughter:
                    Fermenter(loop, "this world has no growing cells").DevCompleteDaughter(A);
                    return;
                case SimCommandKind.SetFlagellumAngle:
                    Fermenter(loop, "this world has no flagella").SetFlagellumAngle(A, B, X);
                    return;
                case SimCommandKind.Inspect:
                    loop.InspectedCell = A;
                    return;
                case SimCommandKind.InspectOrganism:
                    loop.InspectedOrganism = A;
                    return;
                case SimCommandKind.DevGrantPoints:
                    Fermenter(loop, "this world has no skill points").DevGrantPoints(A);
                    return;
                case SimCommandKind.DevEndLevel:
                    Fermenter(loop, "this world has no levels to score").DevEndLevel();
                    return;
                case SimCommandKind.DevSpawnPrey:
                    Fermenter(loop, "this world has no prey").DevSpawnPrey();
                    return;
                default:
                    throw new ArgumentException($"unknown command {(int)Kind}");
            }
        }

        private static IInspectableWorld Inspectable(SimLoop loop) =>
            loop.World as IInspectableWorld ?? throw new InvalidOperationException("this world has no adaptable cells");

        private static FermenterWorld Fermenter(SimLoop loop, string otherwise) =>
            loop.World as FermenterWorld ?? throw new InvalidOperationException(otherwise);

        private static void ExportModel(SimLoop loop, string directory, string prefix)
        {
            var world = Fermenter(loop, "this world has no creature model");
            try
            {
                world.ToModuleArray().Write(directory, prefix);
            }
            catch (Exception e) when (e is System.IO.IOException || e is UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"could not write the model to {directory}: {e.Message}", e);
            }
            loop.Post(new ModelExported(loop.Clock.SimTime, System.IO.Path.Combine(directory, ModuleArray.ArrayFile(prefix))));
        }
    }

    /// <summary>Factory for the commands the front end may send.</summary>
    public static class SimCommands
    {
        public static SimCommand SetSpeed(int speedIndex) => new SimCommand(SimCommandKind.SetSpeed, speedIndex);

        public static SimCommand AddProbe(int cell, int channel) => new SimCommand(SimCommandKind.AddProbe, cell, channel);

        public static SimCommand ClearProbes() => new SimCommand(SimCommandKind.ClearProbes);

        public static SimCommand SetBathConcentration(double concentration) =>
            new SimCommand(SimCommandKind.SetBathConcentration, x: concentration);

        /// <summary>Sets the target of trait <paramref name="trait"/> (a multiplier, count or membrane share) for every cell of
        /// <paramref name="group"/>.</summary>
        public static SimCommand SetTrait(int group, int trait, double value) =>
            new SimCommand(SimCommandKind.SetTrait, group, trait, x: value);

        /// <summary>Sets an energy control (growth, storage, repair) for every cell of <paramref name="group"/>.</summary>
        public static SimCommand SetControl(int group, int control, double value) =>
            new SimCommand(SimCommandKind.SetControl, group, control, x: value);

        /// <summary>
        /// Sets whether a fall of <paramref name="species"/> (<see cref="SessionSchema.RegulationSpecies"/>) lowers
        /// <paramref name="target"/> (a trait's index, <see cref="SessionSchema.GrowthRegulation"/> for growth, or
        /// <see cref="SessionSchema.MaintenanceTarget"/> for a part of the maintenance) in every
        /// cell of <paramref name="group"/>. Needs the Regulation feature.
        /// </summary>
        public static SimCommand SetRegulation(int group, int target, int species, bool on) =>
            new SimCommand(SimCommandKind.SetRegulation, group, target, species, on ? 1.0 : 0.0);

        /// <summary>
        /// Writes the creature as a module array (&lt;prefix&gt;_module_array.json and &lt;prefix&gt;_parameters.csv in
        /// <paramref name="directory"/>; see <see cref="FermenterWorld.ToModuleArray"/>), then reports
        /// <see cref="ModelExported"/>.
        /// </summary>
        public static SimCommand ExportModel(string directory, string prefix) =>
            new SimCommand(SimCommandKind.ExportModel, text1: directory, text2: prefix);

        /// <summary>Divides <paramref name="cell"/>, spending glycogen to finish the daughter if needed.</summary>
        public static SimCommand Divide(int cell) => new SimCommand(SimCommandKind.Divide, cell);

        /// <summary>Turns an unlocked optional feature on or off.</summary>
        public static SimCommand SetFeature(int feature, bool enabled) =>
            new SimCommand(SimCommandKind.SetFeature, feature, enabled ? 1 : 0);

        /// <summary>Buys a feature with skill points (see <see cref="FeatureInfo.Cost"/>).</summary>
        public static SimCommand BuyFeature(int feature) => new SimCommand(SimCommandKind.BuyFeature, feature);

        /// <summary>Sets the level being played (unlocks features); a development control until the time bar drives it.</summary>
        public static SimCommand SetLevel(int level) => new SimCommand(SimCommandKind.SetLevel, level);

        /// <summary>A new seabed from <paramref name="seed"/>: a new game's random floor.</summary>
        public static SimCommand ReseedSeabed(int seed) => new SimCommand(SimCommandKind.ReseedSeabed, seed);

        /// <summary>Luck blows (or re-aims) her current from the tail to the head, relative to the creature [m].</summary>
        public static SimCommand LuckBlow(double tailX, double tailY, double headX, double headY) =>
            new SimCommand(SimCommandKind.LuckBlow, text1: FormattableString.Invariant($"{tailX:R} {tailY:R} {headX:R} {headY:R}"));

        /// <summary>Luck stops her current.</summary>
        public static SimCommand LuckStop() => new SimCommand(SimCommandKind.LuckStop);

        /// <summary>Development aid: completes <paramref name="cell"/>'s daughter without charging its ATP (not for play).</summary>
        public static SimCommand DevCompleteDaughter(int cell) => new SimCommand(SimCommandKind.DevCompleteDaughter, cell);

        /// <summary>Moves flagellum <paramref name="k"/> of a cell to a base angle on its membrane (body frame) [rad].</summary>
        public static SimCommand SetFlagellumAngle(int cell, int k, double angle) =>
            new SimCommand(SimCommandKind.SetFlagellumAngle, cell, k, x: angle);

        /// <summary>The cell whose details frames carry (-1: none).</summary>
        public static SimCommand Inspect(int cell) => new SimCommand(SimCommandKind.Inspect, cell);

        /// <summary>Development aid: adds skill points (not for play).</summary>
        public static SimCommand DevGrantPoints(int points) => new SimCommand(SimCommandKind.DevGrantPoints, points);

        /// <summary>Development aid: ends the level now, scoring it as if its time had run out (not for play).</summary>
        public static SimCommand DevEndLevel() => new SimCommand(SimCommandKind.DevEndLevel);

        /// <summary>Development aid: a prey cell swimming straight at the creature (not for play).</summary>
        public static SimCommand DevSpawnPrey() => new SimCommand(SimCommandKind.DevSpawnPrey);

        /// <summary>Frames carry the details of the free organism with this id (-1: none). It cannot be controlled.</summary>
        public static SimCommand InspectOrganism(int id) => new SimCommand(SimCommandKind.InspectOrganism, id);
    }

    /// <summary>Something that happened in the simulation, delivered to the front end in order.</summary>
    public abstract class SimEvent
    {
        protected SimEvent(double simTime) => SimTime = simTime;

        /// <summary>When it happened [s].</summary>
        public double SimTime { get; }
    }

    public sealed class CheckpointReached : SimEvent
    {
        public CheckpointReached(double simTime, long index) : base(simTime) => Index = index;

        public long Index { get; }
    }

    /// <summary>The creature's model was written as a module array (<see cref="SimCommands.ExportModel"/>).</summary>
    public sealed class ModelExported : SimEvent
    {
        public ModelExported(double simTime, string path) : base(simTime) => Path = path;

        /// <summary>The module array file; its parameters file is next to it.</summary>
        public string Path { get; }
    }

    /// <summary>A command was rejected (invalid arguments); the simulation carried on.</summary>
    public sealed class CommandRejected : SimEvent
    {
        public CommandRejected(double simTime, string reason) : base(simTime) => Reason = reason;

        public string Reason { get; }
    }
}
