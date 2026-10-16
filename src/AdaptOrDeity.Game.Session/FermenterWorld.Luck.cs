using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Luck, the player's second character (adapting_plan.md, Luck): she acts on the environment, not on the creature. Her
    /// luck points build up over time (content luck.rate, to luck.max). She spends them sending a stream of current through
    /// the water along an arrow she aims (<see cref="DirectedCurrent"/>, anchored to the creature as the view is): paid
    /// when it is sent, in proportion to its strength (the arrow's length), it spins up (luck.spin_up), flows for
    /// luck.stream_time and coasts to a stop (luck.spin_down), carrying the creature, free organisms, loose food and prey.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _luckMax = GameParameters.Default.Value("luck.max");
        private readonly double _luckRate = GameParameters.Default.Value("luck.rate");
        private readonly double _luckStreamCost = GameParameters.Default.Value("luck.stream_cost");
        private readonly double _luckSpeed = GameParameters.Default.Value("luck.max_speed");
        private readonly double _luckFullLength = GameParameters.Default.Value("luck.full_length");
        private readonly double _luckStreamTime = GameParameters.Default.Value("luck.stream_time");
        private readonly double _luckSpinUp = GameParameters.Default.Value("luck.spin_up");
        private readonly double _luckSpinDown = GameParameters.Default.Value("luck.spin_down");
        private readonly DirectedCurrent _luckCurrent = new DirectedCurrent
            { HalfWidth = GameParameters.Default.Value("luck.half_width") };
        private double _luck = GameParameters.Default.Value("luck.start");
        private double _streamSpeed;     // the stream's speed while it flows, before spinning up or down [m/s]
        private double _streamLeft;      // time it still flows before coasting down [s]
        private double _streamLevel;     // its spin, 0-1: the flow is _streamSpeed x this

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

        /// <summary>Her stream, relative to the creature [m].</summary>
        public DirectedCurrent LuckCurrent => _luckCurrent;

        /// <summary>Her stream's flow now on its axis as a share of luck.max_speed, 0-1.</summary>
        public double LuckStrength => _luckCurrent.On ? _luckCurrent.Speed / _luckSpeed : 0.0;

        /// <summary>Points a stream along this arrow costs (relative to the creature [m]): luck.stream_cost times its
        /// strength, the arrow's length over luck.full_length (at most 1).</summary>
        public double LuckCost(double tailX, double tailY, double headX, double headY) =>
            _luckStreamCost * Strength(tailX, tailY, headX, headY);

        /// <summary>
        /// Sends a stream from the tail to the head, relative to the creature [m], replacing any that flows: its strength is
        /// the arrow's length over luck.full_length (at most 1), or what her points can pay for, paid now. Nothing is sent
        /// without points.
        /// </summary>
        public void LuckLaunch(double tailX, double tailY, double headX, double headY)
        {
            double strength = Strength(tailX, tailY, headX, headY);
            if (_luckStreamCost > 0.0)
                strength = Math.Min(strength, _luck / _luckStreamCost);
            if (strength <= 0.0)
                return;
            _luck = Math.Max(0.0, _luck - strength * _luckStreamCost);
            _streamSpeed = strength * _luckSpeed;
            _streamLeft = _luckStreamTime;
            _streamLevel = 0.0;
            _luckCurrent.Blow(tailX, tailY, headX, headY, 0.0);   // aimed, at rest: it spins up from here
        }

        /// <summary>Stops her stream at once.</summary>
        public void LuckStop()
        {
            _luckCurrent.Stop();
            _streamLeft = 0.0;
            _streamLevel = 0.0;
        }

        private double Strength(double tailX, double tailY, double headX, double headY)
        {
            double length = Math.Sqrt((headX - tailX) * (headX - tailX) + (headY - tailY) * (headY - tailY));
            return Math.Min(1.0, length / _luckFullLength);
        }

        // Points build up. A stream spins up towards its speed while it flows and coasts down after (first order, its
        // inertia), and ends once it has all but stopped.
        private void StepLuck(double dt)
        {
            _luck = Math.Min(_luckMax, _luck + _luckRate * dt);
            if (!_luckCurrent.On)
                return;
            double target = _streamLeft > 1e-9 ? 1.0 : 0.0;   // (a whole number of steps, to round-off)
            double tau = target > _streamLevel ? _luckSpinUp : _luckSpinDown;
            _streamLevel = target + (_streamLevel - target) * Math.Exp(-dt / Math.Max(tau, 1e-9));
            _streamLeft = Math.Max(0.0, _streamLeft - dt);
            if (_streamLeft <= 0.0 && _streamLevel < 0.01)
            {
                LuckStop();
                return;
            }
            _luckCurrent.SetSpeed(_streamSpeed * _streamLevel);
        }

        // Her stream where a body is, added to the water's (the creature's position anchors it)
        private void AddLuck(RigidBody2D body)
        {
            if (!_luckCurrent.On)
                return;
            var anchor = _body.Body;
            _luckCurrent.At(body.X - anchor.X, body.Y - anchor.Y, out double ux, out double uy);
            body.CurrentVx += ux;
            body.CurrentVy += uy;
        }
    }
}
