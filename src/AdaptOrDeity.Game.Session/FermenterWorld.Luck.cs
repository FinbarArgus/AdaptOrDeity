using System;
using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.World;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// Luck, the player's second character (adapting_plan.md, Luck): she acts on the environment, not on the creature. Her
    /// luck points build up over time (content luck.rate, to luck.max); she spends them blowing a current through the
    /// water along an arrow she aims (<see cref="DirectedCurrent"/>, anchored to the creature as the view is), at a cost per
    /// second that grows with its strength. The current carries the creature, free organisms, loose food and prey.
    /// </summary>
    public sealed partial class FermenterWorld
    {
        private readonly double _luckMax = GameParameters.Default.Value("luck.max");
        private readonly double _luckRate = GameParameters.Default.Value("luck.rate");
        private readonly double _luckCost = GameParameters.Default.Value("luck.cost_rate");
        private readonly double _luckSpeed = GameParameters.Default.Value("luck.max_speed");
        private readonly double _luckFullLength = GameParameters.Default.Value("luck.full_length");
        private readonly DirectedCurrent _luckCurrent = new DirectedCurrent
            { HalfWidth = GameParameters.Default.Value("luck.half_width") };
        private double _luck = GameParameters.Default.Value("luck.start");

        /// <summary>Luck's points now.</summary>
        public double Luck
        {
            get => _luck;
            set => _luck = Math.Max(0.0, Math.Min(_luckMax, value));
        }

        /// <summary>The most luck points she can hold.</summary>
        public double LuckMax => _luckMax;

        /// <summary>Her current, relative to the creature [m].</summary>
        public DirectedCurrent LuckCurrent => _luckCurrent;

        /// <summary>Her current's strength, 0-1: the arrow's length over luck.full_length.</summary>
        public double LuckStrength => _luckCurrent.On ? _luckCurrent.Speed / _luckSpeed : 0.0;

        /// <summary>
        /// Blows (or re-aims) her current from the tail to the head, relative to the creature [m]: at luck.max_speed times
        /// the arrow's length over luck.full_length (at most 1). Nothing happens without luck points.
        /// </summary>
        public void LuckBlow(double tailX, double tailY, double headX, double headY)
        {
            if (_luck <= 0.0)
            {
                _luckCurrent.Stop();
                return;
            }
            double length = Math.Sqrt((headX - tailX) * (headX - tailX) + (headY - tailY) * (headY - tailY));
            _luckCurrent.Blow(tailX, tailY, headX, headY, Math.Min(1.0, length / _luckFullLength) * _luckSpeed);
        }

        /// <summary>Stops her current.</summary>
        public void LuckStop() => _luckCurrent.Stop();

        // Points build up; while she blows, they drain at luck.cost_rate times the strength, and the current stops when
        // they run out.
        private void StepLuck(double dt)
        {
            _luck = Math.Min(_luckMax, _luck + _luckRate * dt);
            if (!_luckCurrent.On)
                return;
            double cost = _luckCost * LuckStrength * dt;
            if (cost >= _luck)
            {
                _luck = 0.0;
                _luckCurrent.Stop();
            }
            else
                _luck -= cost;
        }

        // Her current where a body is, added to the water's (the creature's position anchors it)
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
