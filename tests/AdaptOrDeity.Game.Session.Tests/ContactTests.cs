using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// What leaks from a packet a cell touches comes out at the contact, on the cell's surface (FermenterWorld.AtContact):
    /// it enters the field at the cell and the cell sees it in its membrane correction, so its carriers take up their share
    /// of the near field, k / (k + 4 pi D a), and the rest spreads into the water. Everything is conserved.
    /// </summary>
    public class ContactTests
    {
        // A world with one packet touching cell 0 and nothing else, not swimming; with a carrier, the cell has it at the
        // largest share the membrane takes (bought at level 2). A fast leak (leakRate) makes the near field dominate.
        private static FermenterWorld Touching(bool atContact, string? carrier = null, double leakRate = 0.0)
        {
            var world = new FermenterWorld { Workers = 1, LeakAtContact = atContact };
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Swimming), false);
            world.Food.SetArrivals(0.0, 0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            if (carrier != null)
            {
                world.Level = 2;
                world.SetFeature(GameFeatures.IndexOf(GameFeatures.Engulfing), false);
                world.DevGrantPoints(10);
                world.BuyFeature(GameFeatures.IndexOf(carrier == "PTS_share" ? GameFeatures.PtsTransport : GameFeatures.AbcTransport));
                world.SetTrait(0, world.Traits.ToList().FindIndex(x => x.Id == carrier), 1.0);
                for (int k = 0; k < 6000; k++)
                    world.Step(k * world.MacroDt, world.MacroDt);
            }
            world.CellPosition(0, out double x, out double y);
            var s = world.Food.Settings;
            double free = s.FreeFraction * s.PacketAmount;
            if (leakRate > 0.0)
            {
                s.FreeLeakRate = leakRate;
                free = s.PacketAmount;
                world.HydrolysisCapacity = _ => 0.0;
            }
            world.Food.AddBody(x + world.CellRadius + s.PacketRadius, y, 0.0, 0.0, s.PacketRadius, s.PacketAmount, free, 0);
            return world;
        }

        // Steps on; returns the glucose the cell took up meanwhile [mol].
        private static double Run(FermenterWorld world, double seconds)
        {
            double t0 = 1e6, taken = 0.0;   // after any carrier build-up above
            int steps = (int)Math.Round(seconds / world.MacroDt);
            for (int k = 0; k < steps; k++)
            {
                world.Step(t0 + k * world.MacroDt, world.MacroDt);
                taken += world.Cell(0).GlucoseUptake * world.MacroDt;
            }
            return taken;
        }

        [Test]
        public void ALeakAtTheContactEntersAtTheCellAndIsConserved()
        {
            using var world = Touching(atContact: true);
            var env = world.Environment;
            const int g = EnvironmentField.Glucose;
            double sourced0 = env.Field.Sourced(g), packets0 = env.PacketGlucose, cells0 = world.CellGlucoseExchanged;
            Run(world, 60.0);
            double packets = env.PacketGlucose - packets0, cells = world.CellGlucoseExchanged - cells0;
            Assert.That(packets, Is.GreaterThan(0.0));
            Assert.That(world.ContactGlucose, Is.EqualTo(packets).Within(1e-12 * packets), "all it leaked came out at the cell");
            Assert.That(env.Field.Sourced(g) - sourced0, Is.EqualTo(packets - cells).Within(1e-12 * (packets + Math.Abs(cells))),
                "what entered the field is what the packet leaked less what the cell took");
        }

        [Test]
        public void TheTouchingCellSeesTheContactLeakAsItsOwnRelease()
        {
            // two worlds alike but for where the leak enters; every input change passed, so the first step shows the near
            // field exactly: the membrane mean rises by the leak times the membrane resistance (1/(4 pi D a) less what the
            // grid resolves), on the share of the membrane not facing the packet
            using var contact = Touching(true, null, 0.05);
            using var apart = Touching(false, null, 0.05);
            contact.InputTolerance = apart.InputTolerance = 0.0;
            contact.Step(0.0, contact.MacroDt);
            apart.Step(0.0, apart.MacroDt);
            double q = contact.ContactGlucose / contact.MacroDt;
            contact.CellPosition(0, out double x, out double y);
            var env = contact.Environment;
            double resistance = env.Field.MembraneResistance(EnvironmentField.Glucose, x, y, contact.CellRadius);
            contact.Food.Faces(x, y, contact.CellRadius, out double faced, out _);
            Assert.That(q, Is.GreaterThan(0.0));
            Assert.That(contact.Cell(0).MembraneGlucose - apart.Cell(0).MembraneGlucose,
                Is.EqualTo((1.0 - faced) * q * resistance).Within(1e-6 * (1.0 - faced) * q * resistance));
            // a source on a sphere's surface raises the mean over it by Q / (4 pi D a), as one at its centre
            double d = GameParameters.Default.Value("field.diffusivity.glucose");
            Assert.That(resistance, Is.LessThan(1.0 / (4.0 * Math.PI * d * contact.CellRadius)).And.GreaterThan(0.0));
        }
    }
}
