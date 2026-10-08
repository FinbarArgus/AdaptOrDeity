using System;
using System.Linq;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session.Tests
{
    public class FoodFieldTests
    {
        [Test]
        public void PacketsEnterFromTheBoundaryCrossAndLeave()
        {
            var field = new FoodField(seed: 7) { MeanInterval = 50.0 };
            double edge = field.HalfSize + field.PacketRadius;
            int seen = 0;
            for (int i = 0; i < 20000; i++)
            {
                field.Step(1.0);
                foreach (var p in field.Packets.Where(p => p.Active))
                {
                    Assert.That(Math.Abs(p.X) <= edge * 1.0001 && Math.Abs(p.Y) <= edge * 1.0001, Is.True, "inside the field");
                    // heading inwards from the side it entered: velocity points towards the field
                    seen++;
                }
            }
            Assert.That(seen, Is.GreaterThan(0));
            Assert.That(field.ActiveCount, Is.LessThan(field.Packets.Count), "packets leave as well as arrive");
        }

        [Test]
        public void SameSeedSameField()
        {
            var a = new FoodField(seed: 3);
            var b = new FoodField(seed: 3);
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
            food.MeanInterval = 1e12;   // only the packet we place
            food.Step(0.0);
            var packet = food.Packets.First(p => p.Active);
            // Park it on the cell, still.
            typeof(FoodPacket).GetProperty("X")!.SetValue(packet, 0.0);
            typeof(FoodPacket).GetProperty("Y")!.SetValue(packet, 0.0);
            typeof(FoodPacket).GetProperty("Vx")!.SetValue(packet, 0.0);
            typeof(FoodPacket).GetProperty("Vy")!.SetValue(packet, 0.0);
            double g0 = packet.Glucose;
            double inCell0 = world.Cell(0).State("cytosol__C_glc") * world.Cell(0).Volume;

            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(600);
            Assert.That(world.Cell(0).External("C_glc_e_mod_membrane_face"),
                Is.EqualTo(food.BackgroundGlucose + packet.Glucose / food.PacketVolume).Within(0.01),
                "the cell saw the packet before this step's uptake was withdrawn");
            Assert.That(packet.Glucose, Is.LessThan(g0), "the cell takes glucose from the packet");
            Assert.That(world.Cell(0).Budget().AtpGlycolysis, Is.GreaterThan(world.Cell(0).RestingTurnover * 1.01),
                "more glucose, faster glycolysis");
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            Assert.That(frame.PacketCount, Is.EqualTo(1));
            int trace = loop.Schema.Details.ToList().FindIndex(d => d.Id == "trace_" + food.TraceSpecies[packet.MainTrace].Id);
            Assert.That(frame.Details[trace][0], Is.GreaterThan(0.0), "the packet's main trace is at the membrane");
        }
    }
}
