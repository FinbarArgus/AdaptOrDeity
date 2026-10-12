using System;
using AdaptOrDeity.Sim.Transport;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Core.Tests
{
    public class DiffusionFieldTests
    {
        private const double Dx = 1e-6, Depth = 1e-6, Dglc = 6.7e-10;

        [Test]
        public void AClosedBoxKeepsItsContentAndSourcesAddExactly()
        {
            var f = new DiffusionField(40, 30, Dx, Depth, new[] { Dglc, 2e-9 }, new[] { 0.0, 0.0 }, FieldEdge.Closed);
            f[0, 5, 7] = 3.0;
            f[1, 20, 20] = 1.0;
            double before0 = f.Total(0), before1 = f.Total(1);
            for (int n = 0; n < 50; n++)
                f.Step(0.01);
            Assert.That(f.Total(0), Is.EqualTo(before0).Within(1e-12 * before0));
            Assert.That(f.Total(1), Is.EqualTo(before1).Within(1e-12 * before1));
            f.AddSource(0, 10.5e-6, 10.5e-6, 2e-18);
            f.Step(0.1);
            Assert.That(f.Total(0), Is.EqualTo(before0 + 2e-19).Within(1e-12 * before0));
        }

        [Test]
        public void AGaussianSpreadsAsTheHeatEquationSays()
        {
            // Variance grows by 2Dt; LOD backward Euler is first order, so halving the step about halves the error.
            double Error(int steps)
            {
                const int n = 201;
                double sigma0 = 5e-6, t = 0.05, c0 = (n / 2 + 0.5) * Dx;
                var f = new DiffusionField(n, n, Dx, Depth, new[] { Dglc }, new[] { 0.0 });
                for (int j = 0; j < n; j++)
                    for (int i = 0; i < n; i++)
                    {
                        double x = (i + 0.5) * Dx - c0, y = (j + 0.5) * Dx - c0;
                        f[0, i, j] = Math.Exp(-(x * x + y * y) / (2 * sigma0 * sigma0));
                    }
                for (int k = 0; k < steps; k++)
                    f.Step(t / steps);
                double s2 = sigma0 * sigma0 + 2 * Dglc * t, peak = sigma0 * sigma0 / s2;
                return Math.Abs(f[0, n / 2, n / 2] - peak) / peak;
            }
            double coarse = Error(10), fine = Error(20);
            Assert.That(coarse, Is.LessThan(0.05));
            Assert.That(fine / coarse, Is.EqualTo(0.5).Within(0.1), "first order in time");
        }

        [Test]
        public void HugeStepsStayStableAndPositive()
        {
            // 10^4 times the explicit limit dx²/4D: no oscillation, no negative values, bounded by the start and far field.
            var f = new DiffusionField(32, 32, Dx, Depth, new[] { Dglc }, new[] { 1.0 });
            for (int j = 0; j < 32; j++)
                for (int i = 0; i < 32; i++)
                    f[0, i, j] = (i + j) % 2 == 0 ? 0.0 : 5.0;
            double dt = 1e4 * Dx * Dx / (4 * Dglc);
            for (int k = 0; k < 20; k++)
            {
                f.Step(dt);
                for (int j = 0; j < 32; j++)
                    for (int i = 0; i < 32; i++)
                        Assert.That(f[0, i, j], Is.InRange(0.0, 5.0));
            }
            Assert.That(f[0, 16, 16], Is.EqualTo(1.0).Within(1e-6), "relaxes to the far field");
        }

        [Test]
        public void TheFarFieldIsASteadyState()
        {
            var f = new DiffusionField(20, 10, Dx, Depth, new[] { Dglc, 2e-9 }, new[] { 0.1, 7.3e-5 });
            for (int k = 0; k < 10; k++)
                f.Step(1.0);
            Assert.That(f[0, 3, 4], Is.EqualTo(0.1).Within(1e-15));
            Assert.That(f[1, 19, 9], Is.EqualTo(7.3e-5).Within(1e-19));
        }

        [Test]
        public void AnUptakeDrawsTheFieldDownNearItsCell()
        {
            var f = new DiffusionField(41, 41, Dx, Depth, new[] { Dglc }, new[] { 1.0 });
            double x = 20.5e-6, y = 20.5e-6;
            for (int k = 0; k < 200; k++)
            {
                f.AddSource(0, x, y, -1e-19);
                f.Step(0.01);
            }
            Assert.That(f.Sample(0, x, y), Is.LessThan(f.Sample(0, x + 5e-6, y)));
            Assert.That(f.Sample(0, x + 5e-6, y), Is.LessThan(f.Sample(0, x + 15e-6, y)));
            Assert.That(f.Sample(0, x + 5e-6, y), Is.EqualTo(f.Sample(0, x, y + 5e-6)).Within(1e-12), "symmetric");
        }

        [Test]
        public void SamplingInterpolatesBetweenCentres()
        {
            var f = new DiffusionField(4, 4, Dx, Depth, new[] { Dglc }, new[] { 0.0 }) { OriginX = -2e-6, OriginY = 1e-6 };
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 4; i++)
                    f[0, i, j] = 3.0 * i + 5.0 * j;   // linear: interpolation is exact inside
            double x = -2e-6 + 1.8e-6, y = 1e-6 + 2.3e-6;
            Assert.That(f.Sample(0, x, y), Is.EqualTo(3.0 * 1.3 + 5.0 * 1.8).Within(1e-9));
        }

        [Test]
        public void ShiftingKeepsWhatStaysInViewOverTheGround()
        {
            var f = new DiffusionField(10, 10, Dx, Depth, new[] { Dglc }, new[] { 0.5 });
            f[0, 6, 4] = 9.0;
            double x = (6.5) * Dx, y = 4.5 * Dx;
            f.Shift(3, -2);
            Assert.That(f[0, 3, 6], Is.EqualTo(9.0));
            Assert.That(f.Sample(0, x, y), Is.EqualTo(9.0).Within(1e-12), "the same place over the ground");
            Assert.That(f[0, 9, 5], Is.EqualTo(0.5), "new cells take the far field");
            Assert.That(f[0, 2, 0], Is.EqualTo(0.5));
        }
    }
}
