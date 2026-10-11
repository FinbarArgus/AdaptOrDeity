using System;
using NUnit.Framework;
using static AdaptOrDeity.Game.Session.Tests.FermenterSessionTests;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>Cells stepped on worker threads give the same results as on one thread, and stepping does not allocate.</summary>
    public class ParallelCellsTests
    {
        private static SimLoop Colony(int cells, int workers, out FermenterWorld world)
        {
            var loop = NewLoop(out world);
            world.Workers = workers;
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            while (world.CellCount < cells)
            {
                int parent = world.CellCount - 1;
                FinishDaughter(world, parent);
                loop.Enqueue(SimCommands.Divide(parent));
                loop.ApplyCommands();
            }
            return loop;
        }

        private static double[] Fingerprint(FermenterWorld world)
        {
            int n = world.Cell(0).Batch.States.Length;
            var values = new double[world.CellCount * (n + 1) + 3];
            for (int c = 0; c < world.CellCount; c++)
            {
                for (int i = 0; i < n; i++)
                    values[c * (n + 1) + i] = world.Cell(c).Batch.States[i][0];
                values[c * (n + 1) + n] = world.Cell(c).External("C_glc_e_mod_membrane_face");
            }
            values[values.Length - 3] = world.Body.Body.X;
            values[values.Length - 2] = world.Body.Body.Y;
            values[values.Length - 1] = world.Food.Packets.Count;
            return values;
        }

        [Test]
        public void ResultsAreBitIdenticalOnAnyNumberOfThreads()
        {
            double[]? reference = null;
            foreach (int workers in new[] { 1, 4, 8 })
            {
                var loop = Colony(16, workers, out var world);
                world.BathConcentration = 20.0;
                loop.StepExactly(600);   // a minute: motor runs and pauses, food contact, growth
                var print = Fingerprint(world);
                world.Dispose();
                if (reference == null)
                {
                    reference = print;
                    continue;
                }
                for (int i = 0; i < print.Length; i++)
                    Assert.That(BitConverter.DoubleToInt64Bits(print[i]), Is.EqualTo(BitConverter.DoubleToInt64Bits(reference[i])),
                        $"value {i} differs with {workers} workers");
            }
        }

        [TestCase(1)]
        [TestCase(4)]
        public void SteppingManyCellsDoesNotAllocateOnceWarm(int workers)
        {
            var loop = Colony(16, workers, out var world);
            loop.StepExactly(3000);
            // Allocation is counted over all threads (the workers'), so the test runner's own threads add noise: take
            // the quietest of several windows. A real allocation per step shows in every window.
            long allocated = long.MaxValue;
            for (int window = 0; window < 5; window++)
            {
                long before = GC.GetTotalAllocatedBytes(true);
                loop.StepExactly(200);
                allocated = Math.Min(allocated, GC.GetTotalAllocatedBytes(true) - before);
            }
            world.Dispose();
            Assert.That(allocated, Is.EqualTo(0), $"bytes allocated by 200 steps of 16 cells on {workers} workers");
        }
    }
}
