using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// The minimap's memory (FermenterWorld.Exploration.cs): every minimap.sample_interval the tiles whose centres are in the
    /// creature's view are explored, with the water seen there; frames carry the tiles around the creature's own.
    /// </summary>
    public class MinimapTests
    {
        private static SimLoop NewLoop(out FermenterWorld world)
        {
            world = new FermenterWorld();
            return new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
        }

        private static int InView(FermenterWorld world)
        {
            var b = world.Body.Body;
            double half = world.Food.HalfSize, size = world.TileSize;
            var (i0, j0) = world.TileOf(b.X - half, b.Y - half);
            var (i1, j1) = world.TileOf(b.X + half, b.Y + half);
            int n = 0;
            for (long j = j0; j <= j1; j++)
                for (long i = i0; i <= i1; i++)
                    if (Math.Abs((i + 0.5) * size - b.X) <= half && Math.Abs((j + 0.5) * size - b.Y) <= half)
                        n++;
            return n;
        }

        [Test]
        public void TheTilesInViewAreExploredWithTheWaterSeenThere()
        {
            var loop = NewLoop(out var world);
            Assert.That(world.ExploredTileCount, Is.EqualTo(0));
            loop.StepExactly(1);
            Assert.That(world.TileSize, Is.EqualTo(GameParameters.Default.Value("minimap.tile_size")));
            Assert.That(world.ExploredTileCount, Is.EqualTo(InView(world)), "the 30 um view: 6 x 6 tiles of 5 um");
            var b = world.Body.Body;
            var (ci, cj) = world.TileOf(b.X, b.Y);
            Assert.That(world.TryGetExplored(ci, cj, out var here), Is.True);
            double x = (ci + 0.5) * world.TileSize, y = (cj + 0.5) * world.TileSize;
            var env = world.Environment;
            Assert.That(here.Glucose, Is.EqualTo((float)env.Field.Sample(EnvironmentField.Glucose, x, y)));
            Assert.That(here.Lactate, Is.EqualTo((float)env.Field.Sample(EnvironmentField.Lactate, x, y)));
            Assert.That(here.Ph, Is.InRange(6.5f, 8.5f));
            Assert.That(here.Solid, Is.False, "the creature starts in open water");
            long far = (long)Math.Ceiling(2.0 * world.Food.HalfSize / world.TileSize);
            Assert.That(world.TryGetExplored(ci + far, cj, out _), Is.False, "beyond the view");
        }

        [Test]
        public void MovingOnExploresNewTilesAndRemembersTheOld()
        {
            var loop = NewLoop(out var world);
            loop.StepExactly(1);
            var b = world.Body.Body;
            var (ci, cj) = world.TileOf(b.X, b.Y);
            int first = world.ExploredTileCount;
            world.TryGetExplored(ci, cj, out var start);
            b.Place(b.X + 100e-6, b.Y, b.Angle);   // 100 um along the current: a fresh view
            loop.StepExactly(20);                 // two samples later
            Assert.That(world.ExploredTileCount, Is.GreaterThanOrEqualTo(first + InView(world)));
            Assert.That(world.TryGetExplored(ci, cj, out var then), Is.True, "remembered");
            Assert.That(then.SeenAt, Is.EqualTo(start.SeenAt), "not seen again since");

            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            int m = SnapshotFrame.MinimapSize;
            Assert.That(frame.MinimapTile, Is.EqualTo((float)world.TileSize));
            // the creature sits in the raster's centre tile
            double tx = -frame.MinimapOriginX / frame.MinimapTile, ty = -frame.MinimapOriginY / frame.MinimapTile;
            Assert.That(tx, Is.InRange(m / 2, m / 2 + 1));
            Assert.That(ty, Is.InRange(m / 2, m / 2 + 1));
            Assert.That(frame.MinimapExplored[m / 2 * m + m / 2], Is.True);
            Assert.That(frame.ExploredTiles, Is.EqualTo(world.ExploredTileCount));
            // the start, about 20 tiles back along x, is on the map with its age
            var (ni, nj) = world.TileOf(b.X, b.Y);
            int k = (int)((cj - (nj - m / 2)) * m + (ci - (ni - m / 2)));
            Assert.That(frame.MinimapExplored[k], Is.True);
            Assert.That(frame.MinimapGlucose[k], Is.EqualTo(start.Glucose));
            Assert.That(frame.MinimapAge[k], Is.EqualTo((float)(frame.SimTime - start.SeenAt)).Within(1e-3f));
            Assert.That(frame.MinimapAge[k], Is.GreaterThan(1f));
            Assert.That(frame.MinimapExplored[0], Is.False, "the corner, 160 um away, never seen");
            Assert.That(float.IsNaN(frame.MinimapGlucose[0]), Is.True);
        }

        [Test]
        public void TheGroundIsSolidOnTheMap()
        {
            var loop = NewLoop(out var world);
            var b = world.Body.Body;
            b.Place(b.X, world.Seabed.GroundY + 3e-6, b.Angle);   // resting just above the ground
            loop.StepExactly(1);
            var (ci, cj) = world.TileOf(b.X, world.Seabed.GroundY - 6e-6);
            Assert.That(world.TryGetExplored(ci, cj, out var below), Is.True);
            Assert.That(below.Solid, Is.True);
            Assert.That(float.IsNaN(below.Glucose), Is.True);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            int m = SnapshotFrame.MinimapSize, solid = 0;
            for (int k = 0; k < m * m; k++)
                if (frame.MinimapSolid[k])
                {
                    solid++;
                    Assert.That(frame.MinimapExplored[k], Is.True);
                }
            Assert.That(solid, Is.GreaterThan(0));
        }
    }
}
