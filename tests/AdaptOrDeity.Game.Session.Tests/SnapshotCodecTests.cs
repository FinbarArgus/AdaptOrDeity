using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Frames survive the bytes the simulation library hands across its C entry points: every public property, found by
    /// reflection, so a property added to <see cref="SnapshotFrame"/> but not to <see cref="SnapshotCodec"/> fails here.
    /// </summary>
    public class SnapshotCodecTests
    {
        private static SnapshotFrame Shaped() => new SnapshotFrame(3, 7, 2, 5, 4, 3, 2, 3, 2, 6);

        [Test]
        public void EveryPropertySurvivesTheRoundTrip()
        {
            var random = new Random(7);
            var original = Shaped();
            Fill(original, random);
            var bytes = new byte[SnapshotCodec.Size(original)];
            Assert.That(SnapshotCodec.Write(original, bytes), Is.EqualTo(bytes.Length));
            var copy = Shaped();
            SnapshotCodec.Read(bytes, copy);
            AssertSame(original, copy, nameof(SnapshotFrame));
        }

        [Test]
        public void FramesOfDifferentShapesAreRefused()
        {
            var original = Shaped();
            var bytes = new byte[SnapshotCodec.Size(original)];
            SnapshotCodec.Write(original, bytes);
            Assert.Throws<FormatException>(() => SnapshotCodec.Read(bytes, new SnapshotFrame(3, 8, 2, 5, 4, 3, 2, 3, 2, 6)));
        }

        [Test]
        public void ARealFrameRoundTrips()
        {
            var world = new FermenterWorld();
            var loop = new SimLoop(world, new SimLoopOptions { SpeedPresets = FermenterWorld.SpeedPresets });
            loop.StepExactly(50);
            var frame = loop.CreateFrame();
            loop.WriteSnapshot(frame);
            var bytes = new byte[SnapshotCodec.Size(frame)];
            SnapshotCodec.Write(frame, bytes);
            var copy = loop.CreateFrame();
            SnapshotCodec.Read(bytes, copy);
            AssertSame(frame, copy, nameof(SnapshotFrame));
            TestContext.WriteLine($"a fermenter frame is {bytes.Length:N0} bytes");
        }

        private static void Fill(object target, Random random)
        {
            foreach (var p in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = p.GetValue(target);
                if (value is Array array)
                    FillArray(array, random);
                else if (p.SetMethod != null)
                    p.SetValue(target, RandomValue(p.PropertyType, random));
                else if (value != null && !p.PropertyType.IsPrimitive)
                    Fill(value, random);
            }
        }

        private static void FillArray(Array array, Random random)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var element = array.GetValue(i);
                var type = array.GetType().GetElementType()!;
                if (element is Array inner)
                    FillArray(inner, random);
                else if (type.IsPrimitive)
                    array.SetValue(RandomValue(type, random), i);
                else if (element != null)
                    Fill(element, random);
            }
        }

        private static object RandomValue(Type type, Random random)
        {
            if (type == typeof(int)) return random.Next(1, 1000);
            if (type == typeof(long)) return (long)random.Next(1, 1000000);
            if (type == typeof(double)) return random.NextDouble() * 100;
            if (type == typeof(float)) return (float)(random.NextDouble() * 100);
            if (type == typeof(bool)) return true;   // frames start false: true shows it was carried
            throw new NotSupportedException(type.Name);
        }

        private static void AssertSame(object expected, object actual, string path)
        {
            foreach (var p in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object? e = p.GetValue(expected), a = p.GetValue(actual);
                string at = $"{path}.{p.Name}";
                if (e is IList list)
                {
                    var other = (IList)a!;
                    Assert.That(other.Count, Is.EqualTo(list.Count), at);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] is IList || (list[i] != null && !list[i]!.GetType().IsPrimitive))
                            AssertSame(list[i]!, other[i]!, $"{at}[{i}]");
                        else
                            Assert.That(other[i], Is.EqualTo(list[i]), $"{at}[{i}]");
                    }
                }
                else if (e != null && !p.PropertyType.IsPrimitive)
                    AssertSame(e, a!, at);
                else
                    Assert.That(a, Is.EqualTo(e), at);
            }
        }
    }
}
