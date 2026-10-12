using System;
using System.Linq;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;
using static AdaptOrDeity.Game.Session.Tests.FermenterSessionTests;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Level 4 in the world: boundary and inner groups that follow the colony, the cells coupled to the space between
    /// them with exact bookkeeping, the matrix's cost, and results that do not depend on the number of threads.
    /// </summary>
    public class Level4ColonyTests
    {
        private static SimLoop Colony(int cells, int workers, out FermenterWorld world)
        {
            world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            world.Workers = workers;
            loop.Enqueue(SimCommands.SetLevel(4));
            Colonies.Multicellular(loop.Enqueue);
            loop.ApplyCommands();
            while (world.CellCount < cells)
            {
                int parent = world.CellCount - 1;
                FinishDaughter(world, parent);
                loop.Enqueue(SimCommands.Divide(parent));
                loop.ApplyCommands();
            }
            loop.StepExactly(1);   // the world takes in the new layout on its next step
            return loop;
        }

        [Test]
        public void CellsFallIntoBoundaryAndInnerGroupsAsTheColonyGrows()
        {
            var loop = Colony(7, 1, out var world);
            Assert.That(world.GroupCount, Is.EqualTo(2));
            Assert.That(world.CellGroup(0), Is.EqualTo(FermenterWorld.InnerGroup));
            for (int i = 1; i < 7; i++)
                Assert.That(world.CellGroup(i), Is.EqualTo(FermenterWorld.BoundaryGroup), $"cell {i}");
            Assert.That(world.Space!.Count, Is.EqualTo(6));

            while (world.CellCount < 19)
            {
                int parent = world.CellCount - 1;
                FinishDaughter(world, parent);
                loop.Enqueue(SimCommands.Divide(parent));
                loop.ApplyCommands();
            }
            loop.StepExactly(1);
            Assert.That(Enumerable.Range(0, 19).Count(i => world.CellGroup(i) == FermenterWorld.InnerGroup), Is.EqualTo(7));
            Assert.That(world.Space!.Count, Is.EqualTo(24));
            world.Dispose();
        }

        [Test]
        public void BeforeLevel4EveryCellFacesOpenWater()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(2));
            Colonies.Multicellular(loop.Enqueue);
            loop.ApplyCommands();
            for (int n = 1; n < 7; n++)
            {
                FinishDaughter(world, n - 1);
                loop.Enqueue(SimCommands.Divide(n - 1));
                loop.ApplyCommands();
            }
            loop.StepExactly(1);
            Assert.That(world.Space, Is.Null);
            Assert.That(world.GroupCount, Is.EqualTo(1));
            Assert.That(world.IsBoundaryCell(0), Is.True);
            world.Dispose();
        }

        [Test]
        public void AGroupsSlidersMoveOnlyItsCells()
        {
            var loop = Colony(7, 1, out var world);
            loop.Enqueue(SimCommands.SetControl(FermenterWorld.InnerGroup, FermenterWorld.MatrixControl, 0.5));
            loop.ApplyCommands();
            Assert.That(world.Cell(0).MatrixAllocation, Is.EqualTo(0.5));
            Assert.That(world.Cell(1).MatrixAllocation, Is.EqualTo(0.0));
            world.Dispose();
        }

        [Test]
        public void MatrixCostsThreeAtpAndAGlucosePerUnitAndFillsTheSpace()
        {
            var loop = Colony(7, 1, out var world);
            loop.Enqueue(SimCommands.SetControl(FermenterWorld.InnerGroup, FermenterWorld.MatrixControl, 1.0));
            loop.Enqueue(SimCommands.SetControl(FermenterWorld.BoundaryGroup, FermenterWorld.MatrixControl, 1.0));
            loop.ApplyCommands();
            loop.StepExactly(600);   // a minute
            var inner = world.Cell(0);
            double unit = inner.MatrixExport;
            Assert.That(unit, Is.GreaterThan(0.0));
            Assert.That(inner.Budget().AtpMatrix, Is.EqualTo(3.0 * unit).Within(1e-12 * unit));
            // the glucose the matrix takes leaves the cytosol: the module's own balance, read back
            Assert.That(inner.AlgebraicValue("cytosol__v_mx") * inner.Volume,
                Is.EqualTo(unit).Within(1e-12 * unit));
            Assert.That(Enumerable.Range(0, world.Space!.Count).All(v => world.Space.Matrix(v) > 0.0), Is.True);
            world.Dispose();
        }

        [Test]
        public void EverySpeciesAndTheCarbonAreBookedExactly()
        {
            var loop = Colony(19, 1, out var world);
            world.BathConcentration = 0.05;   // richer water outside, so the gaps carry glucose inwards
            loop.Enqueue(SimCommands.SetControl(FermenterWorld.InnerGroup, FermenterWorld.MatrixControl, 1.0));
            loop.ApplyCommands();
            loop.StepExactly(300);
            var space = world.Space!;
            for (int s = 0; s < ExtracellularSpace.Species.Length; s++)
            {
                double held = s == ExtracellularSpace.Glucose ? space.Held(s) + space.MatrixHeldAndLost() : space.Held(s);
                double booked = space.TotalFromOutside[s] + space.TotalFromCells[s] +
                                (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                double gross = Math.Abs(space.TotalFromOutside[s]) + Math.Abs(space.TotalFromCells[s]) + Math.Abs(held) +
                               (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                Assert.That(gross, Is.GreaterThan(0.0));
                Assert.That(held, Is.EqualTo(booked).Within(1e-12 * gross), ExtracellularSpace.Species[s]);
            }
            // the inner cells live on what the gaps let through: less glucose at their membranes than at the rim's
            Assert.That(world.Cell(0).MembraneGlucose, Is.LessThan(world.Cell(18).MembraneGlucose));
            world.Dispose();
        }

        [Test]
        public void ResultsAreBitIdenticalOnAnyNumberOfThreads()
        {
            double[]? reference = null;
            foreach (int workers in new[] { 1, 4 })
            {
                var loop = Colony(10, workers, out var world);
                loop.Enqueue(SimCommands.SetControl(FermenterWorld.BoundaryGroup, FermenterWorld.MatrixControl, 0.7));
                loop.ApplyCommands();
                loop.StepExactly(200);
                var space = world.Space!;
                var print = new double[space.Count * 5 + world.CellCount];
                for (int v = 0; v < space.Count; v++)
                {
                    for (int s = 0; s < 4; s++)
                        print[5 * v + s] = space.Concentration(v, s);
                    print[5 * v + 4] = space.Matrix(v);
                }
                for (int c = 0; c < world.CellCount; c++)
                    print[space.Count * 5 + c] = world.Cell(c).State("cytosol__C_ATP");
                world.Dispose();
                if (reference == null)
                {
                    reference = print;
                    continue;
                }
                Assert.That(print.Select(BitConverter.DoubleToInt64Bits), Is.EqualTo(reference.Select(BitConverter.DoubleToInt64Bits)));
            }
        }

        [Test]
        public void WithTheEnvironmentFieldTheGapsAndCellsExchangeWithItExactly()
        {
            // the open sectors of the cells and the open gaps exchange with the field; what the field gained is what the
            // packets released less what the cells' open sectors took and what came into the gaps, to 1e-12
            var loop = Colony(19, 1, out var world);
            loop.Enqueue(SimCommands.SetControl(FermenterWorld.InnerGroup, FermenterWorld.MatrixControl, 1.0));
            loop.ApplyCommands();
            var env = world.Environment!;
            var space = world.Space!;
            const int g = AdaptOrDeity.Sim.World.EnvironmentField.Glucose;
            double sourced0 = env.Field.Sourced(g), packets0 = env.PacketGlucose, cells0 = world.CellGlucoseExchanged;
            double gaps0 = space.TotalThroughOpenFaces[ExtracellularSpace.Glucose];
            loop.StepExactly(300);
            double sourced = env.Field.Sourced(g) - sourced0, packets = env.PacketGlucose - packets0;
            double cells = world.CellGlucoseExchanged - cells0;
            double gaps = space.TotalThroughOpenFaces[ExtracellularSpace.Glucose] - gaps0;
            Assert.That(Math.Abs(gaps), Is.GreaterThan(0.0), "glucose moved through the open gaps");
            Assert.That(sourced, Is.EqualTo(packets - cells - gaps).Within(1e-12 * (packets + Math.Abs(cells) + Math.Abs(gaps))));
            for (int s = 0; s < ExtracellularSpace.Species.Length; s++)
            {
                double held = s == ExtracellularSpace.Glucose ? space.Held(s) + space.MatrixHeldAndLost() : space.Held(s);
                double booked = space.TotalFromOutside[s] + space.TotalFromCells[s] +
                                (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                double gross = Math.Abs(space.TotalFromOutside[s]) + Math.Abs(space.TotalFromCells[s]) + Math.Abs(held) +
                               (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                Assert.That(held, Is.EqualTo(booked).Within(1e-12 * gross), ExtracellularSpace.Species[s]);
            }
            Assert.That(world.CellGroup(0), Is.EqualTo(FermenterWorld.InnerGroup));
            world.Dispose();
        }
    }
}
