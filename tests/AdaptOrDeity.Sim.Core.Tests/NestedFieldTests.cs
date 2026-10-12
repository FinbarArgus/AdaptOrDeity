using System;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Transport;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Core.Tests
{
    public class NestedFieldTests
    {
        private const double Dglc = 6.7e-10, Depth = 1e-5;

        private static NestedFieldSettings Settings() => new NestedFieldSettings
        {
            FineSpacing = 1e-6, Margin = 8e-6, ExtentMultiple = 60.0, Depth = Depth, MinCells = 24,
        };

        private static NestedField Field(double diameter, FieldEdge edge = FieldEdge.FarField, double far = 0.0) =>
            new NestedField(Settings(), new[] { Dglc, 2e-9 }, new[] { far, 0.0 }, 0.0, 0.0, diameter, edge);

        // Sets every level from c(x, y) at its cell centres, then makes each shadow the mean of the finer cells.
        private static void Fill(NestedField f, int s, Func<double, double, double> c)
        {
            for (int k = f.Levels - 1; k >= 0; k--)
            {
                var L = f.Level(k);
                for (int j = 0; j < L.Ny; j++)
                    for (int i = 0; i < L.Nx; i++)
                        L[s, i, j] = c(L.OriginX + (i + 0.5) * L.Spacing, L.OriginY + (j + 0.5) * L.Spacing);
            }
            f.Restrict();
        }

        [Test]
        public void LevelsGrowFourfoldOutToSixtyColonyDiameters()
        {
            var f = Field(2e-6);
            Assert.That(f.CellsPerSide, Is.EqualTo(24), "colony + 2 x 8 um margin, at least 24 cells");
            Assert.That(f.Levels, Is.EqualTo(2), "24 um at 1 um, then the outermost at 4 um");
            Assert.That(f.OuterCells, Is.EqualTo(30), "120 um: 60 diameters exactly");
            for (int k = 1; k < f.Levels; k++)
                Assert.That(f.Level(k).Spacing, Is.EqualTo(4 * f.Level(k - 1).Spacing).Within(1e-18));
            f.Size(150e-6, out int n, out int levels, out int outerCells);
            Assert.That(n, Is.EqualTo(168));
            double hOuter = 1e-6 * Math.Pow(4, levels - 1);
            Assert.That(outerCells * hOuter, Is.InRange(60 * 150e-6, 60 * 150e-6 + hOuter), "the extent, to one outer cell");
            Assert.That(outerCells, Is.InRange(n / 2, 2 * n), "the fewest levels with at most 2N outer cells");
            Assert.That(Math.Ceiling(60 * 150e-6 / (hOuter / 4)), Is.GreaterThan(2 * n), "no more levels than needed");
            var single = new NestedField(new NestedFieldSettings { SingleGrid = true }, new[] { Dglc }, new[] { 0.0 }, 0.0, 0.0, 2e-6);
            Assert.That(single.Levels, Is.EqualTo(1));
            Assert.That(single.CellsPerSide, Is.EqualTo(120), "the reference covers the nested grids' extent");
        }

        [Test]
        public void AClosedFieldKeepsItsContentAcrossLevelsAndSourcesAddExactly()
        {
            var f = Field(10e-6, FieldEdge.Closed);
            Fill(f, 0, (x, y) => 2.0 + Math.Exp(-(x * x + y * y) / (2 * 6e-6 * 6e-6)));
            Fill(f, 1, (x, y) => x > 3e-6 ? 1.0 : 0.0);
            double start0 = f.Total(0), start1 = f.Total(1);
            var rng = new Random(4);
            for (int n = 0; n < 300; n++)
            {
                // uptake and release inside the fine grid, and a source out on the coarse levels
                f.Deposit(0, (rng.NextDouble() - 0.5) * 20e-6, (rng.NextDouble() - 0.5) * 20e-6, -3e-18);
                f.Deposit(0, 40e-6, -70e-6, 5e-18);
                f.Deposit(1, (rng.NextDouble() - 0.5) * 10e-6, 1e-6, 1e-14);
                f.Step(n % 3 == 0 ? 0.5 : 0.1);
            }
            Assert.That(f.OceanInflow(0), Is.EqualTo(0.0));
            Assert.That(f.Total(0), Is.EqualTo(start0 + f.Sourced(0)).Within(1e-12 * start0));
            Assert.That(f.Total(1), Is.EqualTo(start1 + f.Sourced(1)).Within(1e-12 * start1));
            Assert.That(f.Sourced(1), Is.GreaterThan(0.01 * start1), "the sources were a sizeable share");
        }

        [Test]
        public void TheFarFieldIsASteadyStateOnEveryLevel()
        {
            var f = Field(30e-6, FieldEdge.FarField, 0.25);
            for (int n = 0; n < 20; n++)
                f.Step(1.0);
            for (int k = 0; k < f.Levels; k++)
                Assert.That(f.Level(k)[0, 5, 7], Is.EqualTo(0.25).Within(1e-13));
            Assert.That(f.OceanInflow(0), Is.EqualTo(0.0).Within(1e-25));
        }

        [Test]
        public void AGaussianSpreadsAsTheHeatEquationSaysAcrossTheGridEdges()
        {
            // A 5 um Gaussian spreads past the 26 um fine grid onto the coarse ones; its peak follows variance + 2Dt and
            // what left through the outermost edge is all that is missing.
            var f = Field(2e-6);
            double sigma0 = 4e-6, t = 0.2;
            Fill(f, 0, (x, y) => Math.Exp(-(x * x + y * y) / (2 * sigma0 * sigma0)));
            double start = f.Total(0);
            for (int k = 0; k < 40; k++)
                f.Step(t / 40);
            double s2 = sigma0 * sigma0 + 2 * Dglc * t, peak = sigma0 * sigma0 / s2;
            Assert.That(f.Sample(0, 0.0, 0.0), Is.EqualTo(peak).Within(0.03 * peak));
            Assert.That(f.Sample(0, 20e-6, 0.0), Is.EqualTo(peak * Math.Exp(-400e-12 / (2 * s2))).Within(0.1 * peak * Math.Exp(-400e-12 / (2 * s2))),
                "the coarse level carries the tail");
            Assert.That(f.Total(0) - f.OceanInflow(0), Is.EqualTo(start).Within(1e-12 * start));
        }

        [Test]
        public void ASteadyPointSinkMakesTheThinLayersLogarithmicProfile()
        {
            // At steady state a sink J in a layer of depth H draws C(r) = C_far - J/(2 pi D H) ln(R/r): between 4 um (fine
            // grid) and 40 um (the outermost level) the drop is J/(2 pi D H) ln 10.
            const double far = 1.0, J = -2e-17;
            var f = Field(2e-6, FieldEdge.FarField, far);
            for (int n = 0; n < 6000; n++)
            {
                f.Deposit(0, 0.0, 0.0, J);
                f.Step(0.1);
            }
            double outer = f.Level(f.Levels - 1).Nx * f.Level(f.Levels - 1).Spacing;
            double scale = -J / (2 * Math.PI * Dglc * Depth);
            double drop = f.Sample(0, 40e-6, 0.0) - f.Sample(0, 4e-6, 0.0);
            Assert.That(drop, Is.EqualTo(scale * Math.Log(10.0)).Within(0.04 * scale * Math.Log(10.0)));
            Assert.That(f.Sample(0, 0.0, 40e-6) - f.Sample(0, 0.0, 4e-6), Is.EqualTo(drop).Within(0.02 * drop), "isotropic");
            Assert.That(f.Total(0) - f.OceanInflow(0) - f.Sourced(0), Is.EqualTo(far * Math.Pow(outer, 2) * Depth).Within(1e-12 * far * Math.Pow(outer, 2) * Depth),
                "the ocean resupplies exactly what the sink takes");
        }

        [TestCase(0.0, 0.0)]
        [TestCase(0.5e-6, 0.5e-6)]
        [TestCase(0.25e-6, -0.1e-6)]
        public void TheMembraneSeesTheThinLayersNearFieldWhereverTheCellSits(double x0, double y0)
        {
            // A sphere of radius a at mid-depth in a layer of depth d, with closed floor and top: its images make the
            // depth-averaged field 2D beyond ~d, and at its membrane C = C2D(r*) + J/(4 pi D a), r* = 2 d e^-gamma (11.2 um).
            // The grid's own value at the source depends on the spacing and where in its cell the source sits (by ~1.3
            // ln-units between a cell's centre and its corner); the membrane value must not, on any grid.
            const double far = 0.1, J = -2e-18, a = 1e-6, gamma = 0.5772156649015329;
            double scale = -J / (2 * Math.PI * Dglc * Depth), rStar = 2 * Depth * Math.Exp(-gamma);
            foreach (double h in new[] { 1e-6, 0.5e-6 })
                foreach (bool single in new[] { false, true })
                {
                    var settings = Settings();
                    settings.FineSpacing = h;
                    settings.MinCells = (int)Math.Round(24e-6 / h);
                    settings.SingleGrid = single;
                    var f = new NestedField(settings, new[] { Dglc }, new[] { far }, 0.0, 0.0, 2e-6);
                    for (int n = 0; n < 600; n++)
                    {
                        f.Deposit(0, x0, y0, J);
                        f.Step(0.1);
                    }
                    // the 2D field at r*, from the grid along four directions (well resolved there)
                    double c2d = 0.25 * (f.Sample(0, x0 + rStar, y0) + f.Sample(0, x0 - rStar, y0) + f.Sample(0, x0, y0 + rStar)
                                         + f.Sample(0, x0, y0 - rStar));
                    double expected = c2d + J / (4 * Math.PI * Dglc * a);
                    Assert.That(f.AtMembrane(0, x0, y0, a, J), Is.EqualTo(expected).Within(0.01 * (far - expected)),
                        $"h {h * 1e6} um, {(single ? "single grid" : "nested")}");
                    Assert.That((far - f.AtMembrane(0, x0, y0, a, J)) / scale, Is.EqualTo(6.76).Within(0.06),
                        "the same depletion on every grid (6.76 J/(2 pi D d) for a 120 um field)");
                    Assert.That(f.AtMembrane(0, x0, y0, a, 0.0), Is.EqualTo(f.Sample(0, x0, y0)), "no exchange, no correction");
                }
        }

        [Test]
        public void RecentringFollowsTheColonyAndConservesContent()
        {
            var f = Field(4e-6);
            Fill(f, 0, (x, y) => Math.Exp(-((x - 2e-6) * (x - 2e-6) + y * y) / (2 * 3e-6 * 3e-6)));
            double start = f.Total(0), atSpot = f.Sample(0, 6e-6, -2e-6);
            double x0 = f.Level(0).OriginX;
            for (int n = 1; n <= 5; n++)
                f.Follow(n * 1.5e-6, -n * 0.7e-6, 4e-6);
            Assert.That(f.Level(0).OriginX, Is.Not.EqualTo(x0), "the fine grid moved");
            Assert.That((f.Level(0).OriginX - x0) / 4e-6, Is.EqualTo(Math.Round((f.Level(0).OriginX - x0) / 4e-6)).Within(1e-9),
                "by whole coarse cells");
            Assert.That(f.Total(0), Is.EqualTo(start).Within(1e-12 * start));
            Assert.That(f.OceanInflow(0), Is.EqualTo(0.0).Within(1e-12 * start));
            Assert.That(f.Sample(0, 6e-6, -2e-6), Is.EqualTo(atSpot).Within(1e-12), "the same water over the ground");
            for (int n = 6; n <= 40; n++)
                f.Follow(n * 1.5e-6, -n * 0.7e-6, 4e-6);
            Assert.That(f.Total(0), Is.EqualTo(start).Within(1e-12 * start), "after moving 60 um");
        }

        [Test]
        public void RegriddingAsTheColonyGrowsConservesContent()
        {
            var f = Field(2e-6);
            Fill(f, 0, (x, y) => Math.Exp(-(x * x + y * y) / (2 * 5e-6 * 5e-6)));
            Fill(f, 1, (x, y) => 1.0 / (1.0 + (x * x + y * y) / 1e-10));
            double start0 = f.Total(0), start1 = f.Total(1);
            int levels = f.Levels, cells = f.CellsPerSide;
            f.Follow(3e-6, 1e-6, 40e-6);
            Assert.That(f.CellsPerSide, Is.GreaterThan(cells));
            Assert.That(f.Levels, Is.GreaterThan(levels));
            Assert.That(f.Total(0), Is.EqualTo(start0).Within(1e-12 * start0));
            Assert.That(f.Total(1), Is.EqualTo(start1).Within(1e-12 * start1));
            for (int n = 0; n < 10; n++)
                f.Step(0.1);
            Assert.That(f.Total(0) - f.OceanInflow(0), Is.EqualTo(start0).Within(1e-12 * start0), "and steps on conservatively");
        }

        [Test]
        public void ResultsAreTheSameOnAnyNumberOfWorkers()
        {
            double[] Run(int workers)
            {
                using var pool = new ParallelWork(workers);
                var f = Field(20e-6, FieldEdge.FarField, 0.5);
                for (int n = 0; n < 30; n++)
                {
                    f.Follow(n * 0.9e-6, 0.0, 20e-6 + n * 1e-6);
                    f.Deposit(0, n * 0.9e-6, 1e-6, -1e-18);
                    f.Deposit(1, n * 0.9e-6, -2e-6, 1e-18);
                    f.Step(0.1, pool);
                }
                var L = f.Level(0);
                var all = new double[2 * L.Nx * L.Ny + 2];
                for (int s = 0; s < 2; s++)
                    for (int j = 0; j < L.Ny; j++)
                        for (int i = 0; i < L.Nx; i++)
                            all[s * L.Nx * L.Ny + j * L.Nx + i] = L[s, i, j];
                all[all.Length - 2] = f.Total(0);
                all[all.Length - 1] = f.Total(1);
                return all;
            }
            Assert.That(Run(4), Is.EqualTo(Run(1)), "bit for bit");
        }

        [Test]
        public void AnIdleSpeciesIsNotSteppedUntilASourceArrives()
        {
            var f = Field(2e-6);
            f.SetActive(1, false);
            f.Step(0.1);
            Assert.That(f.IsActive(1), Is.False);
            f.Deposit(1, 0.0, 0.0, 1e-18);
            Assert.That(f.IsActive(1), Is.True);
            f.Step(0.1);
            Assert.That(f.Total(1) - f.OceanInflow(1), Is.EqualTo(1e-19).Within(1e-31));
        }
    }
}
