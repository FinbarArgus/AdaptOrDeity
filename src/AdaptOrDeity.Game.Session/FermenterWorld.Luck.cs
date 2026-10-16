using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Luck, the player's second character (adapting_plan.md, Luck): she acts on the environment, not on the creature. Her
    /// luck points build up over time (content luck.rate, to luck.max). She spends them sending a stream of current (a
    /// <see cref="Jet"/>) into the view: aimed with an arrow from the view's edge, it arrives from beyond that edge and
    /// runs on along the arrow's line through the whole field, conserving the water's mass (divergence-free) and its
    /// momentum. Paid when sent, in proportion to its strength (the arrow's length), it spins up (luck.spin_up), is driven
    /// for luck.stream_time, then coasts, spreading at constant momentum (luck.spread_time) as friction takes it
    /// (luck.friction_time). It sits in the water, drifting with the tide, and carries the creature, free organisms, loose
    /// food and prey.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _luckMax = GameParameters.Default.Value("luck.max");
        private readonly double _luckRate = GameParameters.Default.Value("luck.rate");
        private readonly double _luckStreamCost = GameParameters.Default.Value("luck.stream_cost");
        private readonly double _luckSpeed = GameParameters.Default.Value("luck.max_speed");
        private readonly double _luckFullLength = GameParameters.Default.Value("luck.full_length");
        private readonly double _luckStreamTime = GameParameters.Default.Value("luck.stream_time");
        private readonly double _luckWidth = GameParameters.Default.Value("luck.jet_width");
        private readonly Jet _luckJet = NewJet();
        private double _luck = GameParameters.Default.Value("luck.start");

        private static Jet NewJet()
        {
            double width = GameParameters.Default.Value("luck.jet_width");
            return new Jet
            {
                SpinUp = GameParameters.Default.Value("luck.spin_up"),
                // the width doubles (sigma² x 4) in luck.spread_time once the drive stops
                Spread = 3.0 * width * width / (2.0 * GameParameters.Default.Value("luck.spread_time")),
                Friction = GameParameters.Default.Value("luck.friction_time"),
                FrontWidth = GameParameters.Default.Value("luck.front_width"),
            };
        }

        /// <summary>Luck's points now.</summary>
        public double Luck
        {
            get => _luck;
            set => _luck = Math.Max(0.0, Math.Min(_luckMax, value));
        }

        /// <summary>The most luck points she can hold.</summary>
        public double LuckMax => _luckMax;

        /// <summary>Points a stream costs at full strength (luck.stream_cost).</summary>
        public double LuckStreamCost => _luckStreamCost;

        /// <summary>The arrow length that gives full strength [m] (luck.full_length).</summary>
        public double LuckFullLength => _luckFullLength;

        /// <summary>A stream's flow at full strength [m/s] (luck.max_speed).</summary>
        public double LuckMaxSpeed => _luckSpeed;

        /// <summary>Her stream, in world coordinates [m].</summary>
        public Jet LuckJet => _luckJet;

        /// <summary>Her stream's flow now on its axis as a share of luck.max_speed, 0-1.</summary>
        public double LuckStrength => _luckJet.On ? _luckJet.Speed / _luckSpeed : 0.0;

        /// <summary>Points a stream along this arrow costs (relative to the creature [m]): luck.stream_cost times its
        /// strength, the arrow's length over luck.full_length (at most 1).</summary>
        public double LuckCost(double tailX, double tailY, double headX, double headY) =>
            _luckStreamCost * Strength(tailX, tailY, headX, headY);

        /// <summary>
        /// Sends a stream along the arrow from the tail (on the view's edge) towards the head, relative to the creature [m],
        /// replacing any that flows: it enters where the tail is, from beyond it, and runs on along the arrow's line. Its
        /// strength is the arrow's length over luck.full_length (at most 1), or what her points can pay for, paid now.
        /// Nothing is sent without points.
        /// </summary>
        public void LuckLaunch(double tailX, double tailY, double headX, double headY)
        {
            double strength = Strength(tailX, tailY, headX, headY);
            if (_luckStreamCost > 0.0)
                strength = Math.Min(strength, _luck / _luckStreamCost);
            if (strength <= 0.0)
                return;
            _luck = Math.Max(0.0, _luck - strength * _luckStreamCost);
            var b = _body.Body;
            _luckJet.Launch(b.X + tailX, b.Y + tailY, headX - tailX, headY - tailY, strength * _luckSpeed, _luckWidth,
                _luckStreamTime);
        }

        /// <summary>Stops her stream at once.</summary>
        public void LuckStop() => _luckJet.Stop();

        private double Strength(double tailX, double tailY, double headX, double headY)
        {
            double length = Math.Sqrt((headX - tailX) * (headX - tailX) + (headY - tailY) * (headY - tailY));
            return Math.Min(1.0, length / _luckFullLength);
        }

        // Points build up; the stream moves on in the water, drifting with the tide.
        private void StepLuck(double dt)
        {
            _luck = Math.Min(_luckMax, _luck + _luckRate * dt);
            _luckJet.Step(dt, _freeVx, _freeVy);
        }

        // Her stream where a body is, added to the water's
        private void AddLuck(RigidBody2D body)
        {
            if (!_luckJet.On)
                return;
            _luckJet.At(body.X, body.Y, out double ux, out double uy);
            body.CurrentVx += ux;
            body.CurrentVy += uy;
        }
    }
}
