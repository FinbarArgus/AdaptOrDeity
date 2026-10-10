using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class FoodFieldTests
    {
        private static FoodField NewField(int seed)
        {
            var settings = GameParameters.Default.Food(0.1);
            settings.Seed = seed;
            return new FoodField(settings);
        }

        [Test]
        public void LyingFoodStaysOnTheGroundAndCarriedFoodMoves()
        {
            // The creature drifts in +x; the window follows it. Lying food never moves over the ground and enters the
            // view ahead of the creature; carried food moves with its carrier. Everything in view stays in the window.
            var field = NewField(7);
            const double v = 2e-6;
            field.Populate(0.0, 0.0);
            double x = 0.0;
            int lyingAhead = 0, lyingBehind = 0;
            var seen = new System.Collections.Generic.Dictionary<FoodPacket, (double X, double Y)>();
            foreach (var p in field.Packets.Where(p => p.Active))
                seen[p] = (p.X, p.Y);   // the food scattered at the start
            for (int i = 0; i < 2000; i++)
            {
                x += v * 1.0;
                field.Step(1.0, x, 0.0, v, 0.0);
                foreach (var p in field.Packets.Where(p => p.Active))
                {
                    double edge = field.HalfSize + field.PacketRadius;
                    Assert.That(Math.Abs(p.X - x) <= edge * 1.0001 && Math.Abs(p.Y) <= edge * 1.0001, Is.True, "in the window");
                    if (p.Kind == PacketKind.Lying)
                    {
                        if (seen.TryGetValue(p, out var at) && at.X == p.X)
                            Assert.That(p.Y, Is.EqualTo(at.Y), "lying food stays put");
                        else if (p.X > x)
                            lyingAhead++;
                        else
                            lyingBehind++;
                    }
                    else
                        Assert.That(p.Vx * p.Vx + p.Vy * p.Vy, Is.EqualTo(1e-12).Within(1e-15), "carried at the carrier's speed");
                    seen[p] = (p.X, p.Y);
                }
            }
            Assert.That(lyingAhead, Is.GreaterThan(0));
            Assert.That(lyingBehind, Is.EqualTo(0), "new lying food comes into view ahead, never behind");
            Assert.That(field.Packets.Any(p => p.Active && p.Kind == PacketKind.Carried) ||
                        seen.Keys.Any(p => p.Kind == PacketKind.Carried), Is.True, "carried food arrives too");
        }

        [Test]
        public void LyingFoodKeepsItsDensityInView()
        {
            // A moving creature keeps about lying_density x the area where food is in view: the window's width times its
            // length plus a packet radius at each end (food is in view while any of it is).
            var field = NewField(5);
            field.Populate(0.0, 0.0);
            double h = field.HalfSize, r = field.PacketRadius;
            double expected = field.Settings.LyingDensity * 2 * h * (2 * h + 2 * r), sum = 0.0;
            double x = 0.0;
            const int steps = 20000;
            for (int i = 0; i < steps; i++)
            {
                x += 2e-6;
                field.Step(1.0, x, 0.0, 2e-6, 0.0);
                sum += field.Packets.Count(p => p.Active && p.Kind == PacketKind.Lying);
            }
            Assert.That(sum / steps, Is.EqualTo(expected).Within(0.25 * expected));
        }

        [Test]
        public void SameSeedSameField()
        {
            var a = NewField(3);
            var b = NewField(3);
            for (int i = 0; i < 5000; i++)
            {
                a.Step(0.5);
                b.Step(0.5);
            }
            Assert.That(a.Packets.Select(p => (p.Active, p.X, p.Y)), Is.EqualTo(b.Packets.Select(p => (p.Active, p.X, p.Y))));
        }

        [Test]
        public void ACellTouchingAPacketSeesItsGlucoseAndDrainsIt()
        {
            var world = new FermenterWorld();
            var food = world.Food;
            food.Settings.MeanInterval = 1e12;   // no carriers
            food.Settings.LyingDensity = 0.0;   // and no more lying food: only the packet we place
            foreach (var p in food.Packets.Skip(1))
                typeof(FoodPacket).GetProperty("Active")!.SetValue(p, false);
            var packet = food.Packets[0];
            Assert.That(packet.Active, Is.True, "food lies around the creature from the start");
            // Park it on the cell, still.
            world.CellPosition(0, out double cx, out double cy);
            typeof(FoodPacket).GetProperty("X")!.SetValue(packet, cx);
            typeof(FoodPacket).GetProperty("Y")!.SetValue(packet, cy);
            typeof(FoodPacket).GetProperty("Vx")!.SetValue(packet, 0.0);
            typeof(FoodPacket).GetProperty("Vy")!.SetValue(packet, 0.0);
            double g0 = packet.Glucose, r0 = packet.Radius;

            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(1);
            double Dist(double ax, double ay) { world.CellPosition(0, out double qx, out double qy); return Math.Sqrt((ax - qx) * (ax - qx) + (ay - qy) * (ay - qy)); }
            double offset = Dist(packet.X, packet.Y);   // stuck where it touched
            loop.StepExactly(599);
            Assert.That(world.Cell(0).External("C_glc_e_mod_membrane_face"),
                Is.EqualTo(food.Settings.ContactGlucose).Within(1e-12), "a touching cell sees the contact concentration");
            world.CellPosition(0, out double cx1, out double cy1);
            Assert.That(Math.Abs(cx1 - cx) + Math.Abs(cy1 - cy), Is.GreaterThan(1e-6), "the creature moved over the ground");
            Assert.That(Dist(packet.X, packet.Y), Is.EqualTo(offset).Within(1e-15), "the food it found moves rigidly with it");
            Assert.That(packet.Glucose, Is.LessThan(g0), "the cell takes glucose from the packet");
            Assert.That(packet.Radius, Is.EqualTo(r0 * Math.Sqrt(packet.Glucose / g0)).Within(1e-3 * r0), "and it shrinks");
            Assert.That(r0, Is.LessThan(world.CellRadius), "food is smaller than the cell");
            Assert.That(world.Cell(0).Budget().AtpGlycolysis, Is.GreaterThan(world.Cell(0).RestingTurnover * 1.01),
                "more glucose, faster glycolysis");
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.PacketCount, Is.EqualTo(1));
            int trace = loop.Schema.Details.ToList().FindIndex(d => d.Id == "trace_" + food.TraceSpecies[packet.MainTrace].Id);
            Assert.That(frame.Details[trace][0], Is.GreaterThan(0.0), "the packet's main trace is at the membrane");
        }

        [Test]
        public void EveryGameParameterIsSourced()
        {
            // content/parameters.json: every value carries its units, a source and a status.
            var p = GameParameters.Default;
            Assert.That(p.Entries.Count, Is.GreaterThan(10));
            foreach (var e in p.Entries)
            {
                Assert.That(e.Source, Is.Not.Empty, e.Key);
                Assert.That(e.Note, Is.Not.Empty, e.Key);
                Assert.That(new[] { "sourced", "placeholder", "design", "definitional" }, Does.Contain(e.Status), e.Key);
            }
            Assert.That(p.TraceSpecies.Count, Is.EqualTo(4));
        }
    
        [Test]
        public void TheCreatureRunsAndPausesAtRandomOnItsFlagellum()
        {
            // Level 1: one flagellum at the back whose motor runs at random. During a run the body swims at ~15 um/s
            // (the kernel's thrust against Stokes drag) and turns a little (the thrust's random angle); in a pause only the
            // current carries it. Its mechanical power is a small share of the motor's proton-motive power; the rest and
            // the drag's work end as heat. With swimming turned off the motors stay off.
            var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            int on = 0, steps = 3000, runsStarted = 0;
            bool last = false;
            double maxSwim = 0.0, maxEfficiency = 0.0;
            var b = world.Body.Body;
            for (int i = 0; i < steps; i++)
            {
                loop.StepExactly(1);
                if (world.MotorOn)
                    on++;
                if (world.MotorOn && !last)
                    runsStarted++;
                last = world.MotorOn;
                maxSwim = Math.Max(maxSwim, Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy));
                if (world.MotorPower > 0.0)
                    maxEfficiency = Math.Max(maxEfficiency, b.Power / world.MotorPower);
            }
            double share = (double)on / steps;
            Assert.That(share, Is.InRange(0.15, 0.55), "runs 2 s, pauses 4 s on average: on about a third of the time");
            Assert.That(runsStarted, Is.GreaterThan(20));
            Assert.That(maxSwim, Is.InRange(10e-6, 20e-6), "one flagellum at the resting PMF swims ~15 um/s");
            Assert.That(maxEfficiency, Is.InRange(1e-4, 0.02), "flagellar propulsion is ~0.1-2% efficient");
            Assert.That(Math.Abs(b.Angle), Is.GreaterThan(1e-3), "the random thrust angle turns the body");
            Assert.That(b.Work, Is.EqualTo(b.KineticEnergy + b.Dissipated).Within(1e-9 * b.Work), "energy is conserved");

            loop.Enqueue(SimCommands.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false));
            loop.ApplyCommands();
            loop.StepExactly(100);
            Assert.That(b.Vx * b.Vx + b.Vy * b.Vy, Is.LessThan(1e-24), "no thrust: it only drifts with the water");
            Assert.That(world.Cell(0).External("a_motor_mod_i_flagellum"), Is.EqualTo(0.0));
        }

        [Test]
        public void OneCellCoastsSmoothlyToAStopAfterOneFlagellarPush()
        {
            // The real cell (kernel) on the rigid body: the motor runs for 1 s, then stops. Thrust rises and falls with the
            // motors and bundle (tau_bundle), so the speed changes smoothly at the game's 0.1 s step, then decays to a stop;
            // the force only ever points straight away from the flagellum's base.
            var cell = new AdaptOrDeity.Sim.Physiology.FermenterCell(new AdaptOrDeity.Sim.Kernels.Generated.GenericAdaptiveCell_fermenter(),
                TraitCatalog.Fermenter);
            var body = new CreatureBody(cell.External("R_cell_mod_membrane") * 1e-6, cell.External("eta_mod_i_flagellum"),
                new RigidBody2D());
            body.AddCell(0.0, 0.0, 1100.0 * cell.AlgebraicValue("membrane__Vol"), CreatureBody.DefaultAngles(0.5));
            double tau = cell.External("tau_bundle_mod_i_flagellum");
            var speeds = new System.Collections.Generic.List<double>();
            for (int i = 0; i < 200; i++)
            {
                cell.SetExternal("a_motor_mod_i_flagellum", i < 10 ? 1.0 : 0.0);   // one push: 1 s of running
                cell.Step(0.1);
                body.Step(0.1, _ => 1.0, _ => cell.AlgebraicValue("i_flagellum__F_one"));
                Assert.That(body.ForceX, Is.GreaterThanOrEqualTo(0.0), "pushed away from its base at the back (+x)");
                Assert.That(Math.Abs(body.ForceY), Is.LessThanOrEqualTo(1e-12 * body.ForceX + 1e-40),
                    "no sideways force without the angle error (to round-off: sin(pi) is not exactly 0)");
                speeds.Add(body.Body.Vx);
            }
            double peak = 0.0;
            foreach (var v in speeds)
                peak = Math.Max(peak, v);
            Assert.That(peak, Is.InRange(5e-6, 20e-6));
            for (int i = 1; i < speeds.Count; i++)
                Assert.That(Math.Abs(speeds[i] - speeds[i - 1]), Is.LessThan(0.35 * peak), $"smooth at step {i}");
            Assert.That(speeds[10 + (int)(3 * tau / 0.1)], Is.LessThan(0.1 * peak), "slowing after the motor stops");
            Assert.That(speeds[^1], Is.LessThan(1e-12 * peak), "stopped");
            Assert.That(body.Body.Dissipated, Is.EqualTo(body.Body.Work).Within(1e-6 * body.Body.Work), "all ends as heat");
        }

        [Test]
        public void FlagellaCanBeMovedAndDaughtersInheritTheirPlaces()
        {
            var world = new FermenterWorld();
            world.SetFlagellumAngle(0, 0, 0.5 * Math.PI);
            Assert.That(world.Body.FlagellumAngle(0, 0), Is.EqualTo(0.5 * Math.PI).Within(1e-12));
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            world.DevCompleteDaughter(0);
            loop.Enqueue(SimCommands.Divide(0));
            loop.ApplyCommands();
            Assert.That(world.CellCount, Is.EqualTo(2));
            Assert.That(world.Body.FlagellumAngle(1, 0), Is.EqualTo(0.5 * Math.PI).Within(1e-12));
        }
    }
}
