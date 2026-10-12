using System;
using System.Linq;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>The space between cells on simple layouts: volumes, faces, and boundary and inner cells.</summary>
    public class ColonyGapsTests
    {
        private const double Spacing = 2.3e-6;

        [Test]
        public void LatticeCoordinatesAreTheLayoutsSlots()
        {
            for (int i = 0; i < 200; i++)
            {
                var (a, b) = ColonyGaps.Lattice(i);
                ColonyLayout.Slot(i, Spacing, out double x, out double y);
                Assert.That((a + 0.5 * b) * Spacing, Is.EqualTo(x).Within(1e-18), $"slot {i} x");
                Assert.That(b * Math.Sqrt(3.0) / 2.0 * Spacing, Is.EqualTo(y).Within(1e-18), $"slot {i} y");
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void OneOrTwoCellsEncloseNoWater(int cells)
        {
            var gaps = new ColonyGaps(cells, Spacing);
            Assert.That(gaps.Volumes, Is.Empty);
            Assert.That(gaps.Faces, Is.Empty);
            for (int i = 0; i < cells; i++)
            {
                Assert.That(gaps.IsBoundary(i), Is.True);
                Assert.That(gaps.OpenShare(i), Is.EqualTo(1.0));
            }
        }

        [Test]
        public void ThreeCellsEncloseOneVolumeOpenOnThreeSides()
        {
            var gaps = new ColonyGaps(3, Spacing);
            Assert.That(gaps.Volumes.Count, Is.EqualTo(1));
            Assert.That(gaps.Faces.Count, Is.EqualTo(3));
            Assert.That(gaps.Faces.All(f => f.Open && f.VolumeA == 0), Is.True);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(gaps.IsBoundary(i), Is.True);
                Assert.That(gaps.OpenShare(i), Is.EqualTo(5.0 / 6.0));
            }
        }

        [Test]
        public void AFullRingMakesTheCentreAnInnerCell()
        {
            var gaps = new ColonyGaps(7, Spacing);
            Assert.That(gaps.Volumes.Count, Is.EqualTo(6));
            Assert.That(gaps.Faces.Count(f => !f.Open), Is.EqualTo(6), "the six spokes join the volumes in a ring");
            Assert.That(gaps.Faces.Count(f => f.Open), Is.EqualTo(6), "the rim gaps open to the sea");
            Assert.That(gaps.IsBoundary(0), Is.False);
            Assert.That(gaps.OpenShare(0), Is.EqualTo(0.0));
            for (int i = 1; i < 7; i++)
            {
                Assert.That(gaps.IsBoundary(i), Is.True);
                Assert.That(gaps.OpenShare(i), Is.EqualTo(4.0 / 6.0));
            }
            // every volume is faced by its three cells' sectors
            var faced = new int[gaps.Volumes.Count];
            for (int i = 0; i < 7; i++)
                for (int k = 0; k < 6; k++)
                    if (gaps.SectorVolume(i, k) >= 0)
                        faced[gaps.SectorVolume(i, k)]++;
            Assert.That(faced, Is.All.EqualTo(3));
        }

        [Test]
        public void TwoRingsMakeSevenInnerCells()
        {
            var gaps = new ColonyGaps(19, Spacing);
            Assert.That(gaps.Volumes.Count, Is.EqualTo(24));
            Assert.That(Enumerable.Range(0, 19).Count(i => !gaps.IsBoundary(i)), Is.EqualTo(7));
            Assert.That(gaps.Faces.Count(f => f.Open), Is.EqualTo(12));
        }

        [Test]
        public void APartialRingClosesGapsOneAtATime()
        {
            // the second ring fills one slot at a time; each new slot closes the gaps it completes
            int before = new ColonyGaps(7, Spacing).Volumes.Count;
            var gaps = new ColonyGaps(9, Spacing);
            Assert.That(gaps.Volumes.Count, Is.GreaterThan(before));
            Assert.That(gaps.IsBoundary(0), Is.False);
            // a face is open exactly when one of its two sides is not a volume
            foreach (var f in gaps.Faces)
                Assert.That(f.Open, Is.EqualTo(f.VolumeB < 0));
        }

        [Test]
        public void VolumeGeometryIsTheUpstreamModulesDefault()
        {
            // the upstream default instance: cells of radius 1 µm, 0.3 µm apart, 2 µm deep
            using var space = new ExtracellularSpace();
            double volume = ColonyGaps.VolumeArea(2.3e-6, 1e-6) * 2e-6;
            Assert.That(volume, Is.EqualTo(space.DefaultVolume).Within(1e-9).Percent);
            var face = AdaptOrDeity.Sim.Modules.NativeModule.Get("extracellular_face", ExtracellularSpace.ModuleVersion)
                .Descriptor.Instances["default"];
            Assert.That(ColonyGaps.CentreToGap(2.3e-6), Is.EqualTo(face["d_up"]).Within(1e-9).Percent);
            Assert.That(face["A_f"], Is.EqualTo(0.3e-6 * 2e-6).Within(1e-9).Percent);
        }
    }
}
