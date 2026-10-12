using System;
using System.Buffers.Binary;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A <see cref="SnapshotFrame"/> as bytes: how the simulation library (NativeAOT) hands frames to the front end
    /// through its C entry points. A header gives the frame's dimensions (both sides must build frames from the same
    /// schema); then every field in a fixed order, arrays whole. No allocation.
    /// </summary>
    public static class SnapshotCodec
    {
        private const int Magic = 0x41534E34;   // "ASN4"

        // Bytes before the frame's fields: the magic number and the shape (HeaderFields integers).
        private const int HeaderFields = 12, HeaderBytes = 4 + 4 * HeaderFields;

        /// <summary>Bytes <see cref="Write"/> needs for frames shaped like <paramref name="frame"/>.</summary>
        public static unsafe int Size(SnapshotFrame frame)
        {
            var c = new Cursor(null, 0, Mode.Measure);
            Visit(frame, ref c);
            return c.Position;
        }

        /// <summary>Writes <paramref name="frame"/> into <paramref name="buffer"/>; returns the bytes written.</summary>
        public static unsafe int Write(SnapshotFrame frame, Span<byte> buffer)
        {
            fixed (byte* p = buffer)
            {
                var c = new Cursor(p, buffer.Length, Mode.Write);
                Visit(frame, ref c);
                return c.Position;
            }
        }

        /// <summary>Fills <paramref name="frame"/> (shaped like the writer's) from <paramref name="bytes"/>.</summary>
        public static unsafe void Read(ReadOnlySpan<byte> bytes, SnapshotFrame frame)
        {
            fixed (byte* p = bytes)
            {
                var c = new Cursor(p, bytes.Length, Mode.Read);
                Visit(frame, ref c);
            }
        }

        /// <summary>A frame shaped as the one in <paramref name="bytes"/> (its header): for a reader whose frames no longer fit.</summary>
        public static SnapshotFrame NewFrameFor(ReadOnlySpan<byte> bytes)
        {
            CheckMagic(bytes);
            return new SnapshotFrame(H(bytes, 0), H(bytes, 1), H(bytes, 2), H(bytes, 3), H(bytes, 4), H(bytes, 5), H(bytes, 6),
                H(bytes, 7), H(bytes, 8), H(bytes, 9), H(bytes, 10), H(bytes, 11));
        }

        /// <summary>Whether <paramref name="frame"/> has the shape of the frame in <paramref name="bytes"/> (no allocation).</summary>
        public static bool Fits(ReadOnlySpan<byte> bytes, SnapshotFrame frame)
        {
            CheckMagic(bytes);
            return H(bytes, 0) == frame.Channels.Length && H(bytes, 1) == frame.CellX.Length && H(bytes, 2) == frame.Probes.Length
                && H(bytes, 3) == (frame.Probes.Length > 0 ? frame.Probes[0].Times.Length : 0) && H(bytes, 4) == frame.Details.Length
                && H(bytes, 5) == frame.TraitValues.Length && H(bytes, 6) == frame.Controls.Length
                && H(bytes, 7) == (frame.Controls.Length > 0 ? frame.Controls[0].Length : 0)
                && H(bytes, 8) == frame.FeatureUnlocked.Length && H(bytes, 9) == frame.PacketX.Length
                && H(bytes, 10) == frame.PreyX.Length && H(bytes, 11) == frame.OrganismX.Length;
        }

        private static void CheckMagic(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < HeaderBytes || BinaryPrimitives.ReadInt32LittleEndian(bytes) != Magic)
                throw new FormatException("not a snapshot frame");
        }

        // Header field i: the frame's dimensions after the magic number.
        private static int H(ReadOnlySpan<byte> bytes, int i) => BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4 + 4 * i));

        // One description of the layout, walked to measure, write and read.
        private static void Visit(SnapshotFrame f, ref Cursor io)
        {
            int magic = Magic;
            io.Value(ref magic);
            if (magic != Magic)
                throw new FormatException("not a snapshot frame");
            Shape(f.Channels.Length, ref io);
            Shape(f.CellX.Length, ref io);
            Shape(f.Probes.Length, ref io);
            Shape(f.Probes.Length > 0 ? f.Probes[0].Times.Length : 0, ref io);
            Shape(f.Details.Length, ref io);
            Shape(f.TraitValues.Length, ref io);
            Shape(f.Controls.Length, ref io);
            Shape(f.Controls.Length > 0 ? f.Controls[0].Length : 0, ref io);
            Shape(f.FeatureUnlocked.Length, ref io);
            Shape(f.PacketX.Length, ref io);
            Shape(f.PreyX.Length, ref io);
            Shape(f.OrganismX.Length, ref io);

            long sequence = f.Sequence; io.Value(ref sequence); f.Sequence = sequence;
            double simTime = f.SimTime; io.Value(ref simTime); f.SimTime = simTime;
            long checkpoint = f.CheckpointIndex; io.Value(ref checkpoint); f.CheckpointIndex = checkpoint;
            int speed = f.SpeedIndex; io.Value(ref speed); f.SpeedIndex = speed;
            bool lagging = f.Lagging; io.Value(ref lagging); f.Lagging = lagging;
            int cellCount = f.CellCount; io.Value(ref cellCount); f.CellCount = cellCount;
            int probeCount = f.ProbeCount; io.Value(ref probeCount); f.ProbeCount = probeCount;
            int groupCount = f.GroupCount; io.Value(ref groupCount); f.GroupCount = groupCount;
            int level = f.Level; io.Value(ref level); f.Level = level;
            double eaten = f.FoodEaten; io.Value(ref eaten); f.FoodEaten = eaten;
            int points = f.SkillPoints; io.Value(ref points); f.SkillPoints = points;
            double left = f.LevelTimeLeft; io.Value(ref left); f.LevelTimeLeft = left;
            double cx = f.CreatureX; io.Value(ref cx); f.CreatureX = cx;
            double cy = f.CreatureY; io.Value(ref cy); f.CreatureY = cy;
            bool motor = f.MotorOn; io.Value(ref motor); f.MotorOn = motor;
            float orientation = f.Orientation; io.Value(ref orientation); f.Orientation = orientation;
            float spin = f.Spin; io.Value(ref spin); f.Spin = spin;
            float motorPower = f.MotorPower; io.Value(ref motorPower); f.MotorPower = motorPower;
            float mechanical = f.MechanicalPower; io.Value(ref mechanical); f.MechanicalPower = mechanical;
            double heat = f.HeatToWater; io.Value(ref heat); f.HeatToWater = heat;
            float vx = f.CreatureVx; io.Value(ref vx); f.CreatureVx = vx;
            float vy = f.CreatureVy; io.Value(ref vy); f.CreatureVy = vy;
            int packets = f.PacketCount; io.Value(ref packets); f.PacketCount = packets;
            float fieldExtent = f.FieldExtent; io.Value(ref fieldExtent); f.FieldExtent = fieldExtent;
            int prey = f.PreyCount; io.Value(ref prey); f.PreyCount = prey;
            int swallowed = f.Swallowed; io.Value(ref swallowed); f.Swallowed = swallowed;
            int offspring = f.Offspring; io.Value(ref offspring); f.Offspring = offspring;
            int own = f.OwnOffspring; io.Value(ref own); f.OwnOffspring = own;
            int foodPoints = f.FoodPoints; io.Value(ref foodPoints); f.FoodPoints = foodPoints;
            int reproduction = f.ReproductionPoints; io.Value(ref reproduction); f.ReproductionPoints = reproduction;
            int organisms = f.OrganismCount; io.Value(ref organisms); f.OrganismCount = organisms;
            int inspected = f.InspectedOrganism; io.Value(ref inspected); f.InspectedOrganism = inspected;
            int released = f.OrganismsReleased; io.Value(ref released); f.OrganismsReleased = released;
            int died = f.OrganismsDied; io.Value(ref died); f.OrganismsDied = died;
            int gone = f.OrganismsGone; io.Value(ref gone); f.OrganismsGone = gone;

            Blocks(f.Channels, ref io);
            Blocks(f.Details, ref io);
            Blocks(f.TraitValues, ref io);
            Blocks(f.TraitTargets, ref io);
            Blocks(f.Controls, ref io);
            Blocks(f.FlagellumAngles, ref io);
            Blocks(f.TraitArea, ref io);
            io.Block(f.CellGroup);
            io.Block(f.CellAlive);
            io.Block(f.CellCanDivide);
            io.Block(f.FeatureUnlocked);
            io.Block(f.FeatureEnabled);
            io.Block(f.CellSurface);
            io.Block(f.CellX);
            io.Block(f.CellY);
            io.Block(f.PacketX);
            io.Block(f.PacketY);
            io.Block(f.PacketRadius);
            io.Block(f.PacketFill);
            io.Block(f.PacketTrace);
            io.Block(f.PacketCarried);
            io.Block(f.PacketBody);
            io.Block(f.FieldGlucose);
            io.Block(f.PreyX);
            io.Block(f.PreyY);
            io.Block(f.PreyRadius);
            io.Block(f.PreyHeading);
            io.Block(f.VacuoleRadius);
            io.Block(f.VacuoleX);
            io.Block(f.VacuoleY);
            io.Block(f.VacuolePrey);
            io.Block(f.OrganismId);
            io.Block(f.OrganismGeneration);
            io.Block(f.OrganismX);
            io.Block(f.OrganismY);
            io.Block(f.OrganismOrientation);
            io.Block(f.OrganismMotorOn);
            io.Block(f.OrganismFlagella);
            io.Block(f.OrganismVacuoleRadius);
            io.Block(f.OrganismDetails);
            Blocks(f.OrganismFlagellumAngles, ref io);
            Blocks(f.OrganismChannels, ref io);
            foreach (var probe in f.Probes)
            {
                int cell = probe.Cell; io.Value(ref cell); probe.Cell = cell;
                int channel = probe.Channel; io.Value(ref channel); probe.Channel = channel;
                int count = probe.Count; io.Value(ref count); probe.Count = count;
                io.Block(probe.Times);
                io.Block(probe.Values);
            }
        }

        private static void Shape(int expected, ref Cursor io)
        {
            int value = expected;
            io.Value(ref value);
            if (value != expected)
                throw new FormatException($"snapshot frame shapes differ ({value} against {expected}): both sides need the same schema");
        }

        private static void Blocks<T>(T[][] arrays, ref Cursor io) where T : unmanaged
        {
            foreach (var a in arrays)
                io.Block(a);
        }

        private enum Mode { Measure, Write, Read }

        /// <summary>Moves values between fields and a byte buffer, or only counts their bytes.</summary>
        private unsafe struct Cursor
        {
            private readonly byte* _p;
            private readonly int _length;
            private readonly Mode _mode;

            public Cursor(byte* p, int length, Mode mode)
            {
                _p = p;
                _length = length;
                _mode = mode;
                Position = 0;
            }

            public int Position { get; private set; }

            public void Value<T>(ref T value) where T : unmanaged
            {
                fixed (T* v = &value)
                    Move((byte*)v, sizeof(T));
            }

            public void Block<T>(T[] values) where T : unmanaged
            {
                fixed (T* v = values)
                    Move((byte*)v, values.Length * sizeof(T));
            }

            private void Move(byte* field, int bytes)
            {
                if (_mode != Mode.Measure)
                {
                    if (Position + bytes > _length)
                        throw new ArgumentException("snapshot buffer too small");
                    if (_mode == Mode.Write)
                        Buffer.MemoryCopy(field, _p + Position, bytes, bytes);
                    else
                        Buffer.MemoryCopy(_p + Position, field, bytes, bytes);
                }
                Position += bytes;
            }
        }
    }
}
