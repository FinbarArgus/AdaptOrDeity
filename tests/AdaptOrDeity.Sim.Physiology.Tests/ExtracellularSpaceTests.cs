using System;
using AdaptOrDeity.Sim.Modules;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>
    /// The space between cells (upstream extracellular_volume and extracellular_face): the face law against the module,
    /// exact conservation of every species and of carbon with the matrix, the backward Euler solution between two volumes,
    /// the steady state at an open face, and the matrix slowing diffusion.
    /// </summary>
    public class ExtracellularSpaceTests
    {
        private const double Area = 6e-13, Half = 6.64e-7;

        private static ExtracellularSpace NewSpace()
        {
            var space = new ExtracellularSpace(1e-8);
            var face = NativeModule.Get("extracellular_face", ExtracellularSpace.ModuleVersion).Descriptor.Instances["default"];
            for (int s = 0; s < ExtracellularSpace.Species.Length; s++)
                space.Diffusivity[s] = face["D_" + ExtracellularSpace.Species[s]];
            return space;
        }

        [Test]
        public void ConductanceIsTheFaceModulesLaw()
        {
            var module = NativeModule.Get("extracellular_face", ExtracellularSpace.ModuleVersion);
            var d = module.Descriptor;
            var batch = new KernelBatch(d, 1) { Count = 1 };
            batch.ApplyInstance("default", 0, 1);
            using var face = module.CreateInstance();
            var random = new Random(3);
            for (int trial = 0; trial < 20; trial++)
            {
                double area = 1e-13 * (1 + 9 * random.NextDouble()), du = 1e-7 + 1e-6 * random.NextDouble();
                double dd = trial % 4 == 0 ? 0.0 : 1e-7 + 1e-6 * random.NextDouble();
                double fu = 0.5 + 0.5 * random.NextDouble(), fd = 0.5 + 0.5 * random.NextDouble();
                batch.Externals[d.ExternalIndex("A_f")][0] = area;
                batch.Externals[d.ExternalIndex("d_up")][0] = du;
                batch.Externals[d.ExternalIndex("d_down")][0] = dd;
                batch.Externals[d.ExternalIndex("f_up")][0] = fu;
                batch.Externals[d.ExternalIndex("f_down")][0] = fd;
                face.SetInputs(batch);
                face.ComputeVariables(batch);
                foreach (var s in ExtracellularSpace.Species)
                {
                    double dif = batch.Externals[d.ExternalIndex("D_" + s)][0];
                    double expected = ExtracellularSpace.Conductance(area, dif, du, dd, fu, fd);
                    Assert.That(batch.Algebraic[d.AlgebraicIndex("G_" + s)][0], Is.EqualTo(expected).Within(1e-14).Percent, s);
                }
            }
        }

        [Test]
        public void TwoClosedVolumesFollowBackwardEuler()
        {
            using var space = NewSpace();
            double v1 = space.DefaultVolume, v2 = 1.5 * space.DefaultVolume;
            space.AddVolume(v1, new[] { 1.0, 0.5, 1e-4, 0.003 });
            space.AddVolume(v2, new[] { 0.2, 0.0, 5e-5, 0.0 });
            space.SetFaces(new[] { new ExtracellularSpace.Face(0, 1, Area, Half, Half) }, Array.Empty<ExtracellularSpace.OpenFace>());
            const double dt = 0.001;
            int ns = ExtracellularSpace.Species.Length;
            var diff0 = new double[ns];
            var total0 = new double[ns];
            for (int s = 0; s < ns; s++)
            {
                diff0[s] = space.Concentration(0, s) - space.Concentration(1, s);
                total0[s] = space.Capacity(0, s) * space.Concentration(0, s) + space.Capacity(1, s) * space.Concentration(1, s);
            }
            for (int step = 0; step < 5; step++)
                space.Step(dt);
            for (int s = 0; s < ns; s++)
            {
                double g = ExtracellularSpace.Conductance(Area, space.Diffusivity[s], Half, Half, 1.0, 1.0);
                double k1 = space.Capacity(0, s), k2 = space.Capacity(1, s);
                double factor = Math.Pow(1.0 + g * (1.0 / k1 + 1.0 / k2) * dt, -5);
                Assert.That(factor, Is.LessThan(0.9), "the test must see the volumes relax");
                double total = k1 * space.Concentration(0, s) + k2 * space.Concentration(1, s);
                Assert.That(total, Is.EqualTo(total0[s]).Within(1e-12 * Math.Abs(total0[s])), $"{ExtracellularSpace.Species[s]} conserved");
                Assert.That(space.Concentration(0, s) - space.Concentration(1, s),
                    Is.EqualTo(diff0[s] * factor).Within(1e-9 * Math.Abs(diff0[s])), ExtracellularSpace.Species[s]);
            }
        }

        [Test]
        public void SpeciesAndCarbonAreConservedWithCellsOpenWaterAndMatrix()
        {
            using var space = NewSpace();
            var water = space.OpenWater;
            for (int v = 0; v < 4; v++)
                space.AddVolume(space.DefaultVolume * (1 + 0.1 * v), water);
            space.SetFaces(new[]
            {
                new ExtracellularSpace.Face(0, 1, Area, Half, Half), new ExtracellularSpace.Face(1, 2, Area, Half, Half),
                new ExtracellularSpace.Face(2, 3, Area, Half, Half), new ExtracellularSpace.Face(0, 3, Area, Half, Half),
            }, new[] { new ExtracellularSpace.OpenFace(0, Area, Half), new ExtracellularSpace.OpenFace(2, Area, Half) });
            space.Outside[0] = 0.05;   // a food packet at the first open gap
            int n = space.Count;
            for (int step = 0; step < 200; step++)
            {
                for (int v = 0; v < n; v++)
                {
                    space.UptakeCoefficient[v] = 1e-17 * (v + 1);
                    space.Source[ExtracellularSpace.Lactate * n + v] = 2e-20;
                    space.Source[ExtracellularSpace.Proton * n + v] = 2e-20;
                    space.MatrixIn[v] = 5e-20 * (v % 2);
                }
                space.Step(0.1);
            }
            Assert.That(space.Matrix(1), Is.GreaterThan(0.0));
            for (int s = 0; s < ExtracellularSpace.Species.Length; s++)
            {
                double held = s == ExtracellularSpace.Glucose ? space.Held(s) + space.MatrixHeldAndLost() : space.Held(s);
                double booked = space.TotalFromOutside[s] + space.TotalFromCells[s] +
                                (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                // relative to the gross amounts moved (what is held is a small difference of large flows)
                double gross = Math.Abs(space.TotalFromOutside[s]) + Math.Abs(space.TotalFromCells[s]) + Math.Abs(held) +
                               (s == ExtracellularSpace.Glucose ? space.TotalMatrixDelivered : 0.0);
                Assert.That(held, Is.EqualTo(booked).Within(1e-12 * gross), ExtracellularSpace.Species[s]);
            }
        }

        [Test]
        public void OpenFaceSteadyStateBalancesTheCellsUptake()
        {
            using var space = NewSpace();
            space.AddVolume(space.DefaultVolume, space.OpenWater);
            space.SetFaces(Array.Empty<ExtracellularSpace.Face>(), new[] { new ExtracellularSpace.OpenFace(0, Area, Half) });
            const double outside = 0.01, a = 3e-16;
            space.Outside[ExtracellularSpace.Glucose] = outside;
            space.UptakeCoefficient[0] = a;
            for (int step = 0; step < 50; step++)
                space.Step(1.0);
            double g = ExtracellularSpace.Conductance(Area, space.Diffusivity[0], Half, 0.0, 1.0, 1.0);
            Assert.That(space.Concentration(0, ExtracellularSpace.Glucose), Is.EqualTo(g * outside / (g + a)).Within(1e-9).Percent);
            Assert.That(space.GlucoseTaken[0], Is.EqualTo(a * g * outside / (g + a) * 1.0).Within(1e-9).Percent);
        }

        [Test]
        public void MatrixSlowsDiffusionAndStaysBelowItsCeiling()
        {
            using var space = NewSpace();
            space.AddVolume(space.DefaultVolume, space.OpenWater);
            space.SetFaces(Array.Empty<ExtracellularSpace.Face>(), Array.Empty<ExtracellularSpace.OpenFace>());
            space.MatrixIn[0] = 1e-18;   // far more than the cells make: the gel fills in minutes
            for (int step = 0; step < 600; step++)
                space.Step(1.0);
            double f = space.Hindrance(0);
            Assert.That(f, Is.LessThan(0.95));
            Assert.That(f, Is.GreaterThan(0.8));
            Assert.That(space.Matrix(0), Is.LessThan(1.15 * space.MatrixCeiling));
            Assert.That(ExtracellularSpace.Conductance(Area, 1e-9, Half, Half, f, 1.0),
                Is.LessThan(ExtracellularSpace.Conductance(Area, 1e-9, Half, Half, 1.0, 1.0)));
        }
    }
}
