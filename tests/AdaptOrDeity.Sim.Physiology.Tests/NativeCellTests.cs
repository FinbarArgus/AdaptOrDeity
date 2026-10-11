using System;
using System.IO;
using System.Runtime.InteropServices;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Kernels.Generated;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Tests;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>
    /// A cell integrated by libcuflynx's generated C agrees with the same cell on the C# integrator run tightly, through
    /// rest, a meal and starvation, with the motor switching; daughters and set states restart it.
    /// </summary>
    public class NativeCellTests
    {
        [OneTimeSetUp]
        public void Library()
        {
            if (!CellLibrary.Resolve())
                Assert.Ignore("the cell library is not built (tools/scripts/build_cell_library.py)");
        }

        private static FermenterCell NewCell(bool native)
        {
            FermenterCell.NativeIntegration = native;
            try
            {
                var cell = new FermenterCell(new GenericAdaptiveCell_fermenter(), TraitCatalog.Fermenter);
                Assert.That(cell.IsNative, Is.EqualTo(native));
                return cell;
            }
            finally
            {
                FermenterCell.NativeIntegration = false;
            }
        }

        [Test]
        public void AgreesWithTheCSharpIntegratorRunTightly()
        {
            var native = NewCell(true);
            var reference = NewCell(false);
            reference.RelativeTolerance = 1e-6;
            int n = native.Batch.States.Length;
            var scale = new double[n];
            var worst = new double[n];
            for (int s = 0; s < 6000; s++)
            {
                double t = s * 0.1;
                double glucose = t >= 120 && t < 300 ? 50.0 : 0.1;
                double motor = (int)(t / 7) % 2;
                foreach (var c in new[] { native, reference })
                {
                    c.MembraneGlucose = glucose;
                    c.MotorActivation = motor;
                    c.Step(0.1);
                }
                for (int i = 0; i < n; i++)
                {
                    scale[i] = Math.Max(scale[i], Math.Abs(reference.Batch.States[i][0]));
                    worst[i] = Math.Max(worst[i], Math.Abs(native.Batch.States[i][0] - reference.Batch.States[i][0]));
                }
            }
            for (int i = 0; i < n; i++)
            {
                TestContext.WriteLine($"{native.Batch.Descriptor.States[i].Name,-24} {worst[i] / Math.Max(scale[i], 1e-30):E1}");
                Assert.That(worst[i], Is.LessThanOrEqualTo(1e-2 * scale[i] + 1e-15), native.Batch.Descriptor.States[i].Name);
            }
        }

        [Test]
        public void ASetStateIsWhereTheNativeCellContinuesFrom()
        {
            var cell = NewCell(true);
            cell.MembraneGlucose = 20.0;
            for (int s = 0; s < 100; s++)
                cell.Step(0.1);
            cell.SetState("bio__P_growth", 0.0);
            cell.Step(0.1);
            Assert.That(cell.GrowthInvested, Is.LessThan(1e-3 * cell.DaughterCost), "growth starts again from nothing");
        }
    }
}
