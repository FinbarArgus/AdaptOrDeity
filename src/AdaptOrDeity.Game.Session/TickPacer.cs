using System;
using System.Diagnostics;
using System.Threading;

namespace AdaptOrDeity.Game.Session
{
    /// <summary>
    /// The simulation thread's wait between ticks. It sleeps 1 ms, unless sleeping is found to oversleep while the
    /// simulation runs: macOS coalesces timers, and a 1 ms sleep there can take tens of milliseconds, so each tick at
    /// 1000x would jump tens of simulated seconds and frames would come in big, uneven steps. Then it yields instead,
    /// trying a sleep again every <see cref="RecheckSeconds"/>. Paused, it always sleeps.
    /// </summary>
    public sealed class TickPacer
    {
        /// <summary>A 1 ms sleep that takes longer than this oversleeps [ms].</summary>
        public const double OversleepMs = 4.0;

        public const double RecheckSeconds = 1.0;

        private readonly Func<double> _clock;
        private readonly Action _sleep, _yield;
        private double _yieldingSince = double.NaN;

        /// <param name="clock">Seconds; real time by default.</param>
        /// <param name="sleep">The 1 ms sleep; Thread.Sleep(1) by default.</param>
        /// <param name="yield">Giving the processor to other threads; Thread.Yield by default.</param>
        public TickPacer(Func<double>? clock = null, Action? sleep = null, Action? yield = null)
        {
            var watch = Stopwatch.StartNew();
            _clock = clock ?? (() => watch.Elapsed.TotalSeconds);
            _sleep = sleep ?? (() => Thread.Sleep(1));
            _yield = yield ?? (() => Thread.Yield());
        }

        /// <summary>Whether it yields rather than sleeps (a sleep overslept less than a recheck ago).</summary>
        public bool Yielding => !double.IsNaN(_yieldingSince);

        /// <summary>Waits before the next tick; <paramref name="running"/>: the simulation advances (not paused).</summary>
        public void Wait(bool running)
        {
            if (running && Yielding && _clock() - _yieldingSince < RecheckSeconds)
            {
                _yield();
                return;
            }
            double before = _clock();
            _sleep();
            double after = _clock();
            _yieldingSince = running && (after - before) * 1e3 > OversleepMs ? after : double.NaN;
        }
    }
}
