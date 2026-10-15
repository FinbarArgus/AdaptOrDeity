using System;
using System.Collections.Generic;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Core;
using AdaptOrDeity.Sim.Physiology;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// A daughter released at division without multicellularity: a free-living one-cell organism in the creature's water.
    /// Its cell is the daughter as divided (its parent's physiology, trait targets, controls and solver settings at
    /// release); its body is a one-cell <see cref="CreatureBody"/> that swims by the creature's run-and-tumble motility
    /// with its own flagella and random numbers, and drifts with the current. Nobody controls it: its controls, and
    /// whether it swims, engulfs and divides by itself, stay as they were when it was released. SI.
    /// </summary>
    public sealed class FreeOrganism
    {
        internal FreeOrganism(int id, int parent, int generation, FermenterCell cell, CreatureBody body, Random random,
            bool swims, bool engulfs, bool autoDivides, double releasedAt, int traceCount)
        {
            Id = id;
            Parent = parent;
            Generation = generation;
            Cell = cell;
            Body = body;
            Random = random;
            Swims = swims;
            Engulfs = engulfs;
            AutoDivides = autoDivides;
            ReleasedAt = releasedAt;
            Traces = new double[traceCount];
        }

        /// <summary>Its number in the world (from 1, never reused).</summary>
        public int Id { get; }

        /// <summary>Id of the organism it divided from; 0 for a daughter of the player's creature.</summary>
        public int Parent { get; }

        /// <summary>1 for the creature's daughters, 2 for their daughters, and so on.</summary>
        public int Generation { get; }

        public FermenterCell Cell { get; }

        /// <summary>Its body: one cell at the centre of mass, with the flagella its parent cell had.</summary>
        public CreatureBody Body { get; }

        /// <summary>True until it dies; at its death it leaves <see cref="FermenterWorld.Organisms"/> and its body becomes
        /// food (<see cref="Remains"/>).</summary>
        public bool Alive => Cell.Alive;

        /// <summary>Where its cell is over the ground [m].</summary>
        public double X => Body.Body.X;

        public double Y => Body.Body.Y;

        /// <summary>Simulated time it was released [s], and when it died (NaN while alive).</summary>
        public double ReleasedAt { get; }

        public double DiedAt { get; internal set; } = double.NaN;

        /// <summary>
        /// True while it is out of range and frozen (<see cref="FermenterWorld.FrozenOrganisms"/>): kept exactly as it was,
        /// not moved or integrated, until the creature comes back.
        /// </summary>
        public bool Frozen { get; internal set; }

        /// <summary>
        /// The food packet its body became when it died (<see cref="PacketKind.Body"/>), or null while it lives. The slot is
        /// reused for other food once the body is eaten or gone (inactive).
        /// </summary>
        public FoodPacket? Remains { get; internal set; }

        /// <summary>True while its flagellar motors run (a run; false in a pause).</summary>
        public bool MotorOn { get; internal set; }

        /// <summary>Whether swimming, engulfing and automatic division were on when it was released (they stay so).</summary>
        public bool Swims { get; }

        public bool Engulfs { get; }

        public bool AutoDivides { get; }

        /// <summary>What it has engulfed, or null.</summary>
        public Vacuole? Vacuole { get; internal set; }

        /// <summary>Glucose it has eaten from food since its release [mol]: from packets outside and from its vacuole.</summary>
        public double Eaten { get; internal set; }

        /// <summary>Glucose equivalents it has engulfed, taken up from its vacuole and egested [mol].</summary>
        public double Engulfed { get; internal set; }

        public double EatenFromVacuole { get; internal set; }

        public double Egested { get; internal set; }

        /// <summary>Prey cells it has engulfed, and its daughters (released the same way).</summary>
        public int Swallowed { get; internal set; }

        public int Daughters { get; internal set; }

        internal Random Random { get; }

        internal double UntilSwitch { get; set; }

        // this step: touching a packet, inside the environment field, the shares of its uptake from its vacuole and from
        // the packets it touches, the field's glucose at its membrane, the trace species at its membrane; and the glucose
        // it exchanged with the field in the last step (for the membrane correction)
        internal bool Touching { get; set; }

        internal bool InField { get; set; }

        internal double VacuoleShare { get; set; }

        internal double PacketShare { get; set; }

        internal double OutsideGlucose { get; set; }

        internal double FieldRelease { get; set; }

        // the last level the creature saw it in
        internal int SeenLevel { get; set; }

        // glucose leaking from the packets it touches this step, entering the field at it [mol/s]
        internal double ContactRelease { get; set; }

        internal double[] Traces { get; }
    }

    /// <summary>
    /// Free-living organisms: daughters released at division without multicellularity, and their offspring. Each lives in
    /// the creature's water by the creature's rules: it eats the packets it touches (in competition with the creature: what
    /// several take from one packet in a step comes out in turn, the creature's cells first, then the organisms by id, as
    /// colony cells share it), engulfs prey and packets if engulfing was on when it was released, exchanges with the
    /// environment field where the field covers it, divides by itself if it was released with growth and automatic
    /// division on (its daughters are the creature's lineage: each earns reproduction points), and dies at a lethal damage
    /// load. At its death its body becomes food where it lies (<see cref="PacketKind.Body"/>, eaten by the packets' rules)
    /// and it leaves the list. One farther than organisms.range_view_widths view widths from the creature is frozen: kept
    /// with its whole state (cell, body, random numbers, vacuole, the bodies it holds) but not moved or integrated, until the
    /// creature comes back within range; dead bodies out of range freeze the same way (<see cref="FoodPacket.Frozen"/>). At
    /// most organisms.max organisms are simulated (a new daughter freezes the farthest) and organisms.frozen_max are kept
    /// frozen (beyond it the farthest is forgotten). Each organism's motion and integration run on the workers, and what
    /// organisms share (packets, the field) is settled afterwards in id order, so results do not depend on the number of
    /// workers.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly List<FreeOrganism> _organisms = new List<FreeOrganism>();
        private readonly List<(FreeOrganism Parent, FermenterCell Daughter)> _released =
            new List<(FreeOrganism, FermenterCell)>();
        private int _nextOrganism = 1;
        private int _bodyTrace = -1;
        private readonly int _motilitySeed = GameParameters.Default.Integer("motility.seed");
        private readonly int _maxOrganisms = GameParameters.Default.Integer("organisms.max");
        private int _maxFrozen = GameParameters.Default.Integer("organisms.frozen_max");
        private readonly List<FreeOrganism> _frozen = new List<FreeOrganism>();   // in id order
        private readonly double _rangeWidths = GameParameters.Default.Value("organisms.range_view_widths");
        private Action<int, int>? _organismMotion, _organismPass;

        /// <summary>Free organisms simulated now, in id order (all living: a dead one is food).</summary>
        public IReadOnlyList<FreeOrganism> Organisms => _organisms;

        /// <summary>Free organisms frozen out of range, in id order: kept as they were until the creature comes back.</summary>
        public IReadOnlyList<FreeOrganism> FrozenOrganisms => _frozen;

        /// <summary>Most free organisms kept frozen (content organisms.frozen_max; settable for tests).</summary>
        public int MaxFrozenOrganisms
        {
            get => _maxFrozen;
            set => _maxFrozen = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>The free organism with this id, or null if it is not simulated (never released, frozen, or forgotten).</summary>
        public FreeOrganism? Organism(int id)
        {
            foreach (var o in _organisms)
                if (o.Id == id)
                    return o;
            return null;
        }

        /// <summary>Most free organisms simulated at once (content organisms.max).</summary>
        public int MaxOrganisms => _maxOrganisms;

        /// <summary>
        /// Distance from the creature along either ground axis beyond which a free organism is no longer simulated [m]:
        /// organisms.range_view_widths view widths (the view is the food window, 2 x its half size).
        /// </summary>
        public double OrganismRange => _rangeWidths * 2.0 * _food.HalfSize;

        /// <summary>Free organisms released so far (the creature's daughters and theirs), that died (their bodies became
        /// food), and that were forgotten (frozen beyond organisms.frozen_max).</summary>
        public int OrganismsReleased { get; private set; }

        public int OrganismsDied { get; private set; }

        public int OrganismsGone { get; private set; }

        /// <summary>Glucose all free organisms have eaten from food so far [mol] (not the creature's: no skill points).</summary>
        public double OrganismsEaten { get; private set; }

        /// <summary>Glucose equivalents free organisms have engulfed, taken up from their vacuoles, and egested [mol].</summary>
        public double OrganismsEngulfed { get; private set; }

        public double OrganismsEatenFromVacuoles { get; private set; }

        public double OrganismsEgested { get; private set; }

        /// <summary>Overlay channel <paramref name="channel"/> of a free organism's cell (SI), as <see cref="Read"/>.</summary>
        public double ReadOrganism(FreeOrganism o, int channel) => ReadChannel(o.Cell, channel);

        /// <summary>Flagella a free organism's cell has built (a fraction is a part-built one).</summary>
        public double FlagellaOf(FreeOrganism o) => _flagellaTrait >= 0 ? o.Cell.TraitValue(_flagellaTrait) : 0.0;

        /// <summary>Cell-panel detail <paramref name="detail"/> of a free organism's cell (SI), as <see cref="ReadDetail"/>.</summary>
        public double ReadOrganismDetail(FreeOrganism o, int detail)
        {
            int trace = _detailTrace[detail];
            if (trace >= 0)
                return o.Traces[trace];
            return _readCell[detail](o.Cell);
        }

        // The creature's cell divided without multicellularity: the daughter leaves, a cell's spacing out from the colony's
        // centre (behind the body for a single cell), turned as the body is, with its parent cell's flagella.
        private void ReleaseFromCreature(int cell, FermenterCell daughter)
        {
            var b = _body.Body;
            _body.WorldPosition(cell, out double x, out double y);
            double dx = x - b.X, dy = y - b.Y, d = Math.Sqrt(dx * dx + dy * dy);
            if (d < 0.5 * CellRadius)
            {
                dx = -Math.Cos(b.Angle);
                dy = -Math.Sin(b.Angle);
                d = 1.0;
            }
            bool autoDivides = Active(GameFeatures.AutoDivide) && Active(GameFeatures.GrowthAndDivision);
            Release(daughter, x + _spacing * dx / d, y + _spacing * dy / d, b.Angle, _body.FlagellumAngles(cell), 0, 1,
                Active(GameFeatures.Swimming), Active(GameFeatures.Engulfing), autoDivides);
        }

        // A daughter of a free organism: released the same way, behind its parent, with what its parent had. It is the
        // creature's lineage: offspring that earns reproduction points.
        private void ReleaseFromOrganism(FreeOrganism parent, FermenterCell daughter)
        {
            parent.Daughters++;
            Offspring++;
            double angle = parent.Body.Body.Angle;
            Release(daughter, parent.X - _spacing * Math.Cos(angle), parent.Y - _spacing * Math.Sin(angle), angle,
                parent.Body.FlagellumAngles(0), parent.Id, parent.Generation + 1, parent.Swims, parent.Engulfs,
                parent.AutoDivides);
        }

        private void Release(FermenterCell daughter, double x, double y, double angle, IReadOnlyList<double> flagella,
            int parent, int generation, bool swims, bool engulfs, bool autoDivides)
        {
            if (_organisms.Count >= _maxOrganisms)
                MakeRoom();
            int id = _nextOrganism++;
            var creature = _body.Body;
            var rigid = new RigidBody2D { CurrentVx = creature.CurrentVx, CurrentVy = creature.CurrentVy };
            var body = new CreatureBody(CellRadius, _body.Eta, rigid);
            body.AddCell(0.0, 0.0, _cellMass, flagella);
            rigid.Place(x, y, angle);
            // its own random numbers, from the world's seed and its id (determinism)
            var random = new Random(unchecked(_motilitySeed * 1_000_003 + id * 7_919));
            var o = new FreeOrganism(id, parent, generation, daughter, body, random, swims, engulfs, autoDivides, _time,
                _food.TraceSpecies.Count);
            o.UntilSwitch = -Math.Log(1.0 - random.NextDouble()) * _meanPause;   // starts in a pause, as the creature
            _organisms.Add(o);
            OrganismsReleased++;
        }

        // At the cap: one that died this step becomes food now; else the organism farthest from the creature is frozen (the
        // first of equals).
        private void MakeRoom()
        {
            for (int k = 0; k < _organisms.Count; k++)
                if (!_organisms[k].Alive)
                {
                    Die(_organisms[k]);
                    _organisms.RemoveAt(k);
                    return;
                }
            int far = Farthest(_organisms);
            var o = _organisms[far];
            _organisms.RemoveAt(far);
            Freeze(o);
        }

        // The index of the organism farthest from the creature (the first of equals).
        private int Farthest(List<FreeOrganism> list)
        {
            int index = -1;
            var b = _body.Body;
            double far = -1.0;
            for (int k = 0; k < list.Count; k++)
            {
                var o = list[k];
                double d2 = (o.X - b.X) * (o.X - b.X) + (o.Y - b.Y) * (o.Y - b.Y);
                if (d2 > far)
                {
                    far = d2;
                    index = k;
                }
            }
            return index;
        }

        // An organism out of range (or pushed out at the cap) is frozen as it is, with the bodies it holds; beyond the frozen
        // cap the farthest frozen one is forgotten.
        private void Freeze(FreeOrganism o)
        {
            o.Frozen = true;
            _food.Freeze(o.Id);
            InsertById(_frozen, o);
            if (_frozen.Count > _maxFrozen)
            {
                int far = Farthest(_frozen);
                var gone = _frozen[far];
                _frozen.RemoveAt(far);
                Forget(gone);
                OrganismsGone++;
            }
        }

        private static void InsertById(List<FreeOrganism> list, FreeOrganism o)
        {
            int k = list.Count;
            while (k > 0 && list[k - 1].Id > o.Id)
                k--;
            list.Insert(k, o);
        }

        // Frozen organisms back within range resume where they were, in id order, while there is room under the cap.
        private void Thaw()
        {
            if (_frozen.Count == 0)
                return;
            var b = _body.Body;
            double range = OrganismRange;
            for (int k = 0; k < _frozen.Count && _organisms.Count < _maxOrganisms; k++)
            {
                var o = _frozen[k];
                if (Math.Abs(o.X - b.X) > range || Math.Abs(o.Y - b.Y) > range)
                    continue;
                _frozen.RemoveAt(k--);
                o.Frozen = false;
                _food.Thaw(o.Id);
                InsertById(_organisms, o);
            }
        }

        // A frozen organism is forgotten: the bodies it held stay frozen where they are, its vacuole's content goes with it.
        private void Forget(FreeOrganism o)
        {
            _food.Release(o.Id);
            if (o.Vacuole != null)
            {
                o.Egested += o.Vacuole.Glucose;
                OrganismsEgested += o.Vacuole.Glucose;
                o.Vacuole = null;
            }
        }

        /// <summary>Runs a pass over the free organisms, on the worker threads when there are enough.</summary>
        private void ForEachOrganism(Action<int, int> pass)
        {
            int n = _organisms.Count;
            if (n == 0)
                return;
            if (n < ParallelFrom || _workerCount == 1)
            {
                pass(0, n);
                return;
            }
            _workers ??= new ParallelWork(_workerCount);
            _workers.Run(n, CellChunk, pass);
        }

        // Each organism's runs and pauses (its own random numbers), its flagella's force from its cell's last state, and
        // its body's step; then the packets it holds move with it.
        private void MoveOrganisms()
        {
            _organismMotion ??= OrganismMotion;
            ForEachOrganism(_organismMotion);
            foreach (var o in _organisms)
                _food.MoveHeld(o.Id, o.X, o.Y, o.Body.Body.Angle);
        }

        private void OrganismMotion(int start, int end)
        {
            double dt = _stepDt;
            for (int k = start; k < end; k++)
            {
                var o = _organisms[k];
                var c = o.Cell;
                o.UntilSwitch -= dt;
                while (o.UntilSwitch <= 0.0)
                {
                    o.MotorOn = !o.MotorOn;
                    if (o.MotorOn)
                        o.Body.NewRun(o.Random, _thrustAngleSd);
                    o.UntilSwitch += -Math.Log(1.0 - o.Random.NextDouble()) * (o.MotorOn ? _meanRun : _meanPause);
                }
                c.MotorActivation = o.Swims && o.MotorOn && c.Alive ? 1.0 : 0.0;
                double flagella = _flagellaTrait >= 0 ? c.TraitValue(_flagellaTrait) : 0.0;
                double thrust = c.Alive ? Math.Max(0.0, c.FlagellumThrust) : 0.0;
                o.Body.CellForce(0, flagella, thrust, out double fx, out double fy, out double torque);
                LoadOrganism(o);
                CurrentAt(o.Body.Body);
                AddLuck(o.Body.Body);
                o.Body.Step(dt, fx, fy, torque);
                o.Body.WorldPosition(0, out double ox, out double oy);
                Seabed.CollideOne(o.Body.Body, ox, oy, CellRadius);
            }
        }

        // After the creature's cells: each living organism engulfs what it touches (if it engulfs), in id order, and its
        // vacuole is digested.
        private void EngulfAndDigestOrganisms(double dt)
        {
            foreach (var o in _organisms)
            {
                var c = o.Cell;
                if (!c.Alive)
                    continue;
                if (o.Engulfs)
                {
                    double angle = o.Body.Body.Angle;
                    o.Vacuole = Engulf(o.Vacuole, c, o.X, o.Y, Math.Cos(angle), Math.Sin(angle), out int prey,
                        out double glucose);
                    o.Swallowed += prey;
                    o.Engulfed += glucose;
                    OrganismsEngulfed += glucose;
                }
                if (o.Vacuole != null)
                    Digest(c, o.Vacuole, dt);
            }
        }

        // Each organism's membrane inputs (the packets it touches, or the environment field where it covers the organism,
        // and its vacuole) and its own integration (workers).
        private void StepOrganismCells()
        {
            _organismPass ??= OrganismPass;
            ForEachOrganism(_organismPass);
        }

        private void OrganismPass(int start, int end)
        {
            for (int k = start; k < end; k++)
            {
                var o = _organisms[k];
                var c = o.Cell;
                double x = o.X, y = o.Y;
                o.Touching = _food.TouchesAny(x, y, CellRadius);
                ReadEnvironment(o, x, y);
                o.VacuoleShare = VacuoleOnMembrane(c, o.Vacuole);
                o.PacketShare *= 1.0 - o.VacuoleShare;
                c.Step(_stepDt);
            }
        }

        // As a creature's cell where the field covers the organism (with the membrane correction for its last exchange with
        // it), beyond it the open sea; the packets it touches on the membrane facing them.
        private void ReadEnvironment(FreeOrganism o, double x, double y)
        {
            var env = _environment;
            var c = o.Cell;
            double r = CellRadius, glucose, lactate, protons;
            o.InField = env.Field.LevelAt(x, y) >= 0;
            if (o.InField)
            {
                glucose = env.AtMembrane(EnvironmentField.Glucose, x, y, r, c.Alive ? o.FieldRelease + o.ContactRelease : 0.0);
                lactate = env.AtMembrane(EnvironmentField.Lactate, x, y, r, c.Alive ? c.LactateRelease : 0.0);
                protons = env.Protons(env.AtMembrane(EnvironmentField.Acid, x, y, r, c.Alive ? c.ProtonRelease : 0.0));
            }
            else
            {
                glucose = Math.Max(0.0, env.Field.Sample(EnvironmentField.Glucose, x, y));
                lactate = Math.Max(0.0, env.Field.Sample(EnvironmentField.Lactate, x, y));
                protons = env.Protons(env.Field.Sample(EnvironmentField.Acid, x, y));
            }
            o.OutsideGlucose = glucose;
            _food.Faces(x, y, r, out double faced, out double offered);
            double membrane = (1.0 - faced) * glucose + offered;
            if (Moved(c.MembraneGlucose, membrane, _food.BackgroundGlucose))
                c.MembraneGlucose = membrane;
            o.PacketShare = c.MembraneGlucose > 0.0 ? Math.Min(1.0, offered / c.MembraneGlucose) : 0.0;
            if (Moved(c.MembraneLactate, lactate, 0.0))
                c.MembraneLactate = lactate;
            if (Moved(c.MembraneProtons, protons, _oceanProtons))
                c.MembraneProtons = protons;
            for (int t = 0; t < o.Traces.Length; t++)
                o.Traces[t] = env.Field.Sample(EnvironmentField.FirstTrace + t, x, y);
        }

        // Food stays with the organism that found it (after the creature's cells took theirs).
        private void OrganismsStick()
        {
            foreach (var o in _organisms)
                if (o.Touching && o.Alive)
                    _food.StickTo(o.X, o.Y, CellRadius, o.Id, o.X, o.Y, o.Body.Body.Angle);
        }

        // Each living organism inside the field exchanges with it as a creature's cell does (before the field steps), its
        // uptake through the membrane facing packets comes out of them, in id order after the creature's cells; food eaten is
        // that and its field uptake's share above the open water's glucose.
        private void OrganismsExchange(EnvironmentField env, double dt)
        {
            double ocean = env.OceanGlucose;
            foreach (var o in _organisms)
            {
                var c = o.Cell;
                o.FieldRelease = 0.0;
                if (!c.Alive)
                    continue;
                double j = c.GlucoseRelease;
                if (o.InField)
                {
                    double glucose = j * (j < 0.0 ? 1.0 - o.VacuoleShare - o.PacketShare : 1.0);
                    env.Exchange(EnvironmentField.Glucose, o.X, o.Y, glucose);
                    env.Exchange(EnvironmentField.Lactate, o.X, o.Y, c.LactateRelease);
                    env.Exchange(EnvironmentField.Acid, o.X, o.Y, c.ProtonRelease);
                    CellGlucoseExchanged -= glucose * dt;
                    o.FieldRelease = glucose;
                    double m = o.OutsideGlucose;
                    if (glucose < 0.0 && m > ocean)
                        Eat(o, -glucose * dt * (m - ocean) / m);
                }
                if (o.PacketShare > 0.0 && c.GlucoseUptake > 0.0)
                {
                    double drawn = _food.Draw(o.X, o.Y, CellRadius, c.GlucoseUptake * o.PacketShare * dt, dt);
                    Eat(o, drawn);
                    OrganismsEatenFromPackets += drawn;
                }
            }
        }

        /// <summary>Glucose free organisms have taken up from the packets they touch so far [mol].</summary>
        public double OrganismsEatenFromPackets { get; private set; }

        // What each organism took from its vacuole.
        private void OrganismsEat(double dt)
        {
            double usedUp = Settings.UsedUpFraction * _prey.Content;
            foreach (var o in _organisms)
            {
                var c = o.Cell;
                if (!c.Alive)
                    continue;
                var v = o.Vacuole;
                if (v == null)
                    continue;
                double taken = TakeUp(c, v, o.VacuoleShare, dt);
                Eat(o, taken);
                o.EatenFromVacuole += taken;
                OrganismsEatenFromVacuoles += taken;
                if (v.Glucose < usedUp)
                {
                    o.Egested += v.Glucose;
                    OrganismsEgested += v.Glucose;
                    o.Vacuole = null;
                }
            }
        }

        private void Eat(FreeOrganism o, double mol)
        {
            o.Eaten += mol;
            OrganismsEaten += mol;
        }

        /// <summary>
        /// Glucose equivalents in a dead cell's body [mol]: a sphere of the cell's radius at the packet density
        /// (food.* density: all of a cell's carbon, its small molecules included, as glucose), with food.free_fraction of
        /// it free. A dead organism's body holds this plus what its vacuole held.
        /// </summary>
        public double DeadCellAmount => Settings.SphereAmount(CellRadius);

        // A dead organism's body becomes food where it lies, drifting with its water: its cell at the packet density plus
        // its vacuole's content (polymer and free sugar as they were), smelling of serine (lysed cells). The packets it
        // held lie where they are.
        private void Die(FreeOrganism o)
        {
            o.DiedAt = _time;
            OrganismsDied++;
            _food.Release(o.Id);
            double cell = DeadCellAmount, glucose = cell, free = Settings.FreeFraction * cell;
            if (o.Vacuole != null)
            {
                glucose += o.Vacuole.Glucose;
                free += o.Vacuole.Free;
                o.Vacuole = null;
            }
            var water = o.Body.Body;
            o.Remains = _food.AddBody(o.X, o.Y, water.CurrentVx, water.CurrentVy, CellRadius, glucose, free, _bodyTrace);
        }

        private int BodyTrace()
        {
            for (int t = 0; t < _food.TraceSpecies.Count; t++)
                if (_food.TraceSpecies[t].Id == "serine")
                    return t;
            return 0;
        }

        // At the start of a step, once the creature has moved: organisms out of range freeze and frozen ones back in range
        // thaw, so every organism simulated in the step is within range (and the field) as it starts.
        private void FreezeAndThaw()
        {
            var b = _body.Body;
            double range = OrganismRange;
            int kept = 0;
            for (int k = 0; k < _organisms.Count; k++)
            {
                var o = _organisms[k];
                if (o.Alive && (Math.Abs(o.X - b.X) > range || Math.Abs(o.Y - b.Y) > range))
                    Freeze(o);
                else
                    _organisms[kept++] = o;
            }
            _organisms.RemoveRange(kept, _organisms.Count - kept);
            Thaw();
        }

        // Deaths (the body becomes food) and divisions.
        private void OrganismLives()
        {
            _released.Clear();
            int kept = 0;
            for (int k = 0; k < _organisms.Count; k++)
            {
                var o = _organisms[k];
                var c = o.Cell;
                if (!c.Alive)
                {
                    Die(o);
                    continue;
                }
                if (o.AutoDivides && c.GrowthProgress >= 1.0)
                {
                    var daughter = c.Divide();
                    if (daughter != null)
                        _released.Add((o, daughter));
                }
                _organisms[kept++] = o;
            }
            _organisms.RemoveRange(kept, _organisms.Count - kept);
            foreach (var (parent, daughter) in _released)
                ReleaseFromOrganism(parent, daughter);
        }
    }
}
