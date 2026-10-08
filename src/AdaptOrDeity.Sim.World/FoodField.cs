using System;
using System.Collections.Generic;

namespace AdaptOrDeity.Sim.World
{
    /// <summary>A trace species a food packet carries: something a cell could smell once it has the receptor.</summary>
    public sealed class TraceSpecies
    {
        public TraceSpecies(string id, string name, string source)
        {
            Id = id;
            Name = name;
            Source = source;
        }

        public string Id { get; }

        public string Name { get; }

        /// <summary>What releases it (what the smell tells the cell).</summary>
        public string Source { get; }
    }

    /// <summary>A drifting patch of food: glucose plus traces of other species, in a disc of the 2D sheet.</summary>
    public sealed class FoodPacket
    {
        internal FoodPacket(int traceCount) => Traces = new double[traceCount];

        /// <summary>Centre [m].</summary>
        public double X { get; internal set; }

        public double Y { get; internal set; }

        /// <summary>Drift velocity [m/s].</summary>
        public double Vx { get; internal set; }

        public double Vy { get; internal set; }

        /// <summary>Radius [m].</summary>
        public double Radius { get; internal set; }

        /// <summary>Glucose left [mol].</summary>
        public double Glucose { get; internal set; }

        /// <summary>Trace species left [mol], in <see cref="FoodField.TraceSpecies"/> order.</summary>
        public double[] Traces { get; }

        /// <summary>The trace species this packet is richest in.</summary>
        public int MainTrace { get; internal set; }

        public bool Active { get; internal set; }
    }

    /// <summary>
    /// Food in the medium around the organism: packets of glucose, each with traces of other species, that enter from
    /// any boundary of a square field, drift across it and leave. A cell in contact with a packet sees the packet's
    /// glucose concentration (on top of the background) at its membrane, and takes glucose up through its own carriers;
    /// the packet loses what the cells take. Traces are recorded at the cell surface for sensing later; nothing takes
    /// them up yet. Deterministic for a given seed. This stands in for the environment field of the transport graph
    /// (adapting_plan.md: structured field, advection-diffusion), which replaces it.
    /// </summary>
    public sealed class FoodField
    {
        public static readonly IReadOnlyList<TraceSpecies> DefaultTraces = new[]
        {
            new TraceSpecies("aspartate", "Aspartate", "lysed cells and protein-rich debris"),
            new TraceSpecies("serine", "Serine", "lysed cells"),
            new TraceSpecies("ribose", "Ribose", "nucleic acid from dead cells"),
            new TraceSpecies("dimethylsulfide", "Dimethyl sulfide", "algal blooms (later epochs)"),
        };

        private readonly Random _random;
        private readonly FoodPacket[] _packets;
        private double _untilNext;

        /// <param name="seed">Random seed (determinism).</param>
        /// <param name="halfSize">Half the side of the square field [m].</param>
        /// <param name="capacity">Most packets in the field at once.</param>
        public FoodField(int seed = 1, double halfSize = 15e-6, int capacity = 24)
        {
            _random = new Random(seed);
            HalfSize = halfSize;
            TraceSpecies = DefaultTraces;
            _packets = new FoodPacket[capacity];
            for (int i = 0; i < capacity; i++)
                _packets[i] = new FoodPacket(TraceSpecies.Count);
            _untilNext = 0.0;
        }

        public IReadOnlyList<TraceSpecies> TraceSpecies { get; }

        /// <summary>Packet slots; only <see cref="FoodPacket.Active"/> ones are in the field.</summary>
        public IReadOnlyList<FoodPacket> Packets => _packets;

        public double HalfSize { get; }

        /// <summary>Glucose between packets [mol/m³].</summary>
        public double BackgroundGlucose { get; set; } = 1.0;

        /// <summary>Glucose concentration of a new packet [mol/m³] (placeholder: a rich patch, 10 mM).</summary>
        public double PacketGlucose { get; set; } = 10.0;

        /// <summary>Trace concentration of a new packet's main species [mol/m³] (others at a tenth).</summary>
        public double PacketTrace { get; set; } = 0.01;

        /// <summary>Packet radius [m].</summary>
        public double PacketRadius { get; set; } = 3e-6;

        /// <summary>Sheet depth, for packet volume [m] (the cell's depth).</summary>
        public double Depth { get; set; } = 2e-6;

        /// <summary>Drift speed [m/s] (placeholder: a slow current past a cell).</summary>
        public double DriftSpeed { get; set; } = 0.05e-6;

        /// <summary>Mean time between new packets [s].</summary>
        public double MeanInterval { get; set; } = 300.0;

        /// <summary>Packet volume [m³].</summary>
        public double PacketVolume => Math.PI * PacketRadius * PacketRadius * Depth;

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var p in _packets)
                    if (p.Active)
                        n++;
                return n;
            }
        }

        /// <summary>Moves packets, retires those that left the field or are spent, and releases new ones at the boundary.</summary>
        public void Step(double dt)
        {
            double margin = PacketRadius;
            foreach (var p in _packets)
            {
                if (!p.Active)
                    continue;
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
                if (Math.Abs(p.X) > HalfSize + margin || Math.Abs(p.Y) > HalfSize + margin || p.Glucose <= 0.0)
                    p.Active = false;
            }
            _untilNext -= dt;
            while (_untilNext <= 0.0)
            {
                Spawn();
                _untilNext += -Math.Log(1.0 - _random.NextDouble()) * MeanInterval;   // exponential intervals
            }
        }

        /// <summary>Glucose [mol/m³] a cell of radius <paramref name="r"/> at (x, y) sees at its membrane.</summary>
        public double GlucoseAt(double x, double y, double r)
        {
            double c = BackgroundGlucose;
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    c += p.Glucose / PacketVolume;
            return c;
        }

        /// <summary>Trace concentration [mol/m³] of species <paramref name="trace"/> at a cell's membrane.</summary>
        public double TraceAt(int trace, double x, double y, double r)
        {
            double c = 0.0;
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    c += p.Traces[trace] / PacketVolume;
            return c;
        }

        /// <summary>
        /// Takes <paramref name="mol"/> of glucose that a cell at (x, y) took up beyond the background, from the packets it
        /// touches in proportion to what each holds.
        /// </summary>
        public void Withdraw(double x, double y, double r, double mol)
        {
            if (mol <= 0.0)
                return;
            double total = 0.0;
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    total += p.Glucose;
            if (total <= 0.0)
                return;
            double share = Math.Min(1.0, mol / total);
            foreach (var p in _packets)
                if (p.Active && Touches(p, x, y, r))
                    p.Glucose -= p.Glucose * share;
        }

        public bool Touches(FoodPacket p, double x, double y, double r)
        {
            double dx = p.X - x, dy = p.Y - y, d = p.Radius + r;
            return dx * dx + dy * dy <= d * d;
        }

        private void Spawn()
        {
            FoodPacket? slot = null;
            foreach (var p in _packets)
                if (!p.Active)
                {
                    slot = p;
                    break;
                }
            if (slot == null)
                return;
            // Enter from a random point on a random side, heading inwards within ±45° of the normal.
            int side = _random.Next(4);
            double along = (2.0 * _random.NextDouble() - 1.0) * HalfSize;
            double edge = HalfSize + PacketRadius;
            double angle = (_random.NextDouble() - 0.5) * Math.PI / 2;
            double nx = 0, ny = 0;
            switch (side)
            {
                case 0: slot.X = -edge; slot.Y = along; nx = 1; break;
                case 1: slot.X = edge; slot.Y = along; nx = -1; break;
                case 2: slot.X = along; slot.Y = -edge; ny = 1; break;
                default: slot.X = along; slot.Y = edge; ny = -1; break;
            }
            double c = Math.Cos(angle), s = Math.Sin(angle);
            slot.Vx = DriftSpeed * (nx * c - ny * s);
            slot.Vy = DriftSpeed * (nx * s + ny * c);
            slot.Radius = PacketRadius;
            slot.Glucose = PacketGlucose * PacketVolume;
            slot.MainTrace = _random.Next(TraceSpecies.Count);
            for (int i = 0; i < slot.Traces.Length; i++)
                slot.Traces[i] = PacketTrace * PacketVolume * (i == slot.MainTrace ? 1.0 : 0.1);
            slot.Active = true;
        }
    }
}
