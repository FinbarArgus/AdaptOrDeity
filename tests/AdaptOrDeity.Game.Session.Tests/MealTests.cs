using System;
using System.Linq;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Meals have mass (FermenterWorld.Meals.cs): food a body holds adds its mass and, outside the cells, its Stokes drag,
    /// so the flagella push the body more slowly; eating it restores the speed.
    /// </summary>
    public class MealTests
    {
        [Test]
        public void ALoadsDragDividesTheSpeedAndItsMassOnlyTheTimeToReachIt()
        {
            const double r = 1e-6, eta = 1.08e-3, mass = 5e-15, force = 1e-13;
            var free = new CreatureBody(r, eta, new RigidBody2D());
            var loaded = new CreatureBody(r, eta, new RigidBody2D());
            free.AddCell(0.0, 0.0, mass, CreatureBody.DefaultAngles(0.5));
            loaded.AddCell(0.0, 0.0, mass, CreatureBody.DefaultAngles(0.5));
            double xi = 6.0 * Math.PI * eta * r, xiMeal = 6.0 * Math.PI * eta * 0.8e-6;
            loaded.SetLoad(3e-15, 1e-27, xiMeal, 1e-20);
            Assert.That((loaded.Body.Mass, loaded.Body.Drag), Is.EqualTo((mass + 3e-15, xi + xiMeal)));
            free.Step(0.1, force, 0.0, 0.0);
            loaded.Step(0.1, force, 0.0, 0.0);
            Assert.That(free.Body.Vx, Is.EqualTo(force / xi).Within(1e-12 * force / xi));
            Assert.That(loaded.Body.Vx / free.Body.Vx, Is.EqualTo(xi / (xi + xiMeal)).Within(1e-12), "1/1.8 for a 0.8 um meal on a 1 um cell");
            loaded.SetLoad(0.0, 0.0, 0.0, 0.0);
            Assert.That((loaded.Body.Mass, loaded.Body.Drag), Is.EqualTo((free.Body.Mass, free.Body.Drag)), "unloaded");
            // a cell joining keeps the load
            loaded.SetLoad(3e-15, 1e-27, xiMeal, 1e-20);
            loaded.AddCell(2.2e-6, 0.0, mass, CreatureBody.DefaultAngles(0.5));
            Assert.That(loaded.Body.Drag, Is.EqualTo(2.0 * xi + xiMeal).Within(1e-15 * xi));
        }

        // A world without food arriving, the creature swimming, one fresh packet stuck to it.
        private static (FermenterWorld World, FoodPacket Meal) HoldingAMeal()
        {
            var world = new FermenterWorld { Workers = 1 };
            world.Food.SetArrivals(0.0, 0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            world.CellPosition(0, out double x, out double y);
            var s = world.Food.Settings;
            var meal = world.Food.AddBody(x + world.CellRadius + s.PacketRadius, y, 0.0, 0.0, s.PacketRadius, s.PacketAmount,
                s.FreeFraction * s.PacketAmount, 0);
            world.Step(0.0, world.MacroDt);
            Assert.That((meal.Stuck, meal.Holder), Is.EqualTo((true, 0)));
            return (world, meal);
        }

        [Test]
        public void AHeldMealSlowsTheCreatureAndEatingItRestoresItsSpeed()
        {
            var (world, meal) = HoldingAMeal();
            using var _ = world;
            var body = world.Body.Body;
            double eta = world.Body.Eta, xiCell = 6.0 * Math.PI * eta * world.CellRadius;
            double density = GameParameters.Default.Value("body.cell_density");
            world.Step(0.1, world.MacroDt);
            // (the load is set before the step's eating: the meal is a hair smaller now)
            Assert.That(world.Body.LoadDrag, Is.EqualTo(6.0 * Math.PI * eta * meal.Radius).Within(1e-4 * xiCell));
            Assert.That(world.Body.LoadMass, Is.EqualTo(density * FoodField.Volume(meal)).Within(1e-3 * world.Body.LoadMass));
            Assert.That(body.Drag, Is.EqualTo(xiCell + world.Body.LoadDrag).Within(1e-12 * xiCell));
            // the speed through the water is the flagella's force over the drag, meal included: R/(R + r) of the cell's alone
            Assert.That(Math.Sqrt(body.Vx * body.Vx + body.Vy * body.Vy) * body.Drag,
                Is.EqualTo(Math.Sqrt(world.Body.ForceX * world.Body.ForceX + world.Body.ForceY * world.Body.ForceY))
                    .Within(1e-6 * Math.Sqrt(world.Body.ForceX * world.Body.ForceX + world.Body.ForceY * world.Body.ForceY) + 1e-30));
            double r = world.Food.Settings.PacketRadius;
            Assert.That(xiCell / body.Drag, Is.EqualTo(world.CellRadius / (world.CellRadius + r)).Within(1e-3), "a fresh packet against the 1 µm cell");
            // eaten (here: taken away), the load goes and the speed is the cell's again
            world.Food.Remove(meal);
            world.Step(0.2, world.MacroDt);
            Assert.That((world.Body.LoadMass, world.Body.LoadDrag), Is.EqualTo((0.0, 0.0)));
            Assert.That(body.Drag, Is.EqualTo(xiCell).Within(1e-12 * xiCell));
        }

        [Test]
        public void EngulfedFoodAddsMassButNoDrag()
        {
            var (world, meal) = HoldingAMeal();
            using var _ = world;
            world.Level = 2;   // engulfing is on by default from level 2
            world.Step(0.1, world.MacroDt);
            world.Step(0.2, world.MacroDt);
            var v = world.VacuoleOf(0);
            Assert.That(v, Is.Not.Null);
            Assert.That(meal.Active, Is.False, "swallowed");
            double density = GameParameters.Default.Value("body.cell_density");
            // (the load is set before the step's digestion: a 0.5 um meal is digested ~0.1% a step)
            Assert.That(world.Body.LoadMass, Is.EqualTo(density * v!.Glucose / GameParameters.Default.PacketDensity).Within(3e-3 * world.Body.LoadMass));
            Assert.That(world.Body.LoadDrag, Is.EqualTo(0.0), "inside the cell");
        }

        [Test]
        public void AFreeOrganismCarriesItsMealsToo()
        {
            var world = new FermenterWorld { Workers = 1 };
            using var _ = world;
            world.Level = 2;
            world.SetFeature(GameFeatures.IndexOf(GameFeatures.Engulfing), false);
            world.Food.SetArrivals(0.0, 0.0);
            foreach (var p in world.Food.Packets.Where(p => p.Active).ToList())
                world.Food.Remove(p);
            FermenterSessionTests.FinishDaughter(world, 0);
            world.Divide(0);
            var o = world.Organisms[0];
            // on the organism's far side from the creature
            var b = world.Body.Body;
            double dx = o.X - b.X, dy = o.Y - b.Y, d = Math.Sqrt(dx * dx + dy * dy);
            var meal = world.Food.AddBody(o.X + 1.8 * world.CellRadius * dx / d, o.Y + 1.8 * world.CellRadius * dy / d, 0.0, 0.0,
                world.CellRadius, 1e-15, 0.0, 0);
            world.Step(0.0, world.MacroDt);
            world.Step(0.1, world.MacroDt);
            Assert.That((meal.Stuck, meal.Holder), Is.EqualTo((true, o.Id)));
            Assert.That(o.Body.LoadDrag, Is.EqualTo(6.0 * Math.PI * o.Body.Eta * meal.Radius).Within(1e-4 * o.Body.Body.Drag));
            Assert.That(world.Body.LoadDrag, Is.EqualTo(0.0), "not the creature's");
        }

        [Test]
        public void HeldFoodStaysAgainstItsCellAsItShrinks()
        {
            var (world, meal) = HoldingAMeal();
            using var _ = world;
            double radius = meal.Radius;
            typeof(FoodPacket).GetProperty("Glucose")!.SetValue(meal, 0.3 * meal.Amount);   // most of it eaten
            typeof(FoodPacket).GetProperty("Free")!.SetValue(meal, 0.0);
            world.Step(0.1, world.MacroDt);
            world.Step(0.2, world.MacroDt);
            Assert.That(meal.Radius, Is.LessThan(0.7 * radius));
            world.CellPosition(0, out double x, out double y);
            Assert.That(world.Food.Touches(meal, x, y, world.CellRadius), Is.True, "still held against the cell");
            Assert.That(world.TimeWithoutFood, Is.EqualTo(0.0));
        }
    }
}
