using System;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// Backward (implicit) Euler over a kernel batch, for stiff kernels such as a whole cell whose Ca buffer relaxes in
    /// microseconds while its ion contents change over hours. Each instance solves y₁ = y₀ + dt·f(y₁) by Newton's method
    /// with a dense LU solve. L-stable: large steps damp fast modes instead of blowing up, and a converged step at steady
    /// state stays there exactly. First order; externals are held constant over the step. Allocation-free once warm.
    /// <para>
    /// Modified Newton, as stiff solvers such as CVODE do it: the Jacobian of the rates (df/dy, by finite differences)
    /// and the LU factors of I - dt·df/dy are kept across iterations and steps, and only the residual is evaluated each
    /// iteration (one rate evaluation). The factors are redone when dt changes (cheap) and the Jacobian is recomputed only
    /// when Newton converges slowly or fails, or after <see cref="MaxJacobianAge"/> steps. The answer is the same to the
    /// Newton tolerance; the cost per step falls from ~(n + 1) rate evaluations per iteration to ~1-3 per step.
    /// </para>
    /// </summary>
    public sealed class BackwardEulerIntegrator
    {
        private readonly int _maxIterations;
        private readonly double _tolerance;
        private double[] _y0 = Array.Empty<double>();
        private double[] _f = Array.Empty<double>();
        private double[] _g = Array.Empty<double>();
        private double[] _dx = Array.Empty<double>();
        private double[] _scale = Array.Empty<double>();
        private double[,] _jf = new double[0, 0];    // df/dy at the last refresh
        private double[,] _lu = new double[0, 0];    // LU factors of I - h df/dy
        private int[] _piv = Array.Empty<int>();
        private int _jacobianFor = -1;               // batch index the cached Jacobian belongs to (-1: none)
        private double _luStep = double.NaN;         // step the factors were made for
        private int _jacobianAge;

        /// <param name="maxIterations">Newton iterations per step.</param>
        /// <param name="tolerance">Convergence on the residual of the step, relative to |y| + atol.</param>
        public BackwardEulerIntegrator(int maxIterations = 10, double tolerance = 1e-8)
        {
            _maxIterations = maxIterations;
            _tolerance = tolerance;
        }

        /// <summary>Keep the Jacobian across iterations and steps (modified Newton). False: rebuild it every iteration.</summary>
        public bool ReuseJacobian { get; set; } = true;

        /// <summary>Steps after which the Jacobian is recomputed even if Newton converges well.</summary>
        public int MaxJacobianAge { get; set; } = 50;

        /// <summary>Rate evaluations so far (diagnostics: the cost of the integration).</summary>
        public long RateEvaluations { get; private set; }

        /// <summary>Jacobian evaluations so far (diagnostics).</summary>
        public long JacobianEvaluations { get; private set; }

        /// <summary>Forget the cached Jacobian (e.g. after the state was set from outside).</summary>
        public void Invalidate() => _jacobianFor = -1;

        /// <summary>Newton iterations taken by the last instance of the last step (diagnostics).</summary>
        public int LastIterations { get; private set; }

        /// <summary>True if every instance of the last step converged.</summary>
        public bool LastConverged { get; private set; }

        /// <summary>
        /// Steps by <paramref name="dt"/>, retrying with halved substeps (down to dt/2^maxHalvings) for any instance whose
        /// Newton iteration did not converge or produced a non-finite state. Returns the number of halvings used.
        /// </summary>
        public int StepRobust(IKernel kernel, KernelBatch batch, double t, double dt, int start, int end, int maxHalvings = 12)
        {
            int n = batch.States.Length;
            if (_save.Length < n)
                _save = new double[n];
            int worst = 0;
            for (int k = start; k < end; k++)
            {
                for (int i = 0; i < n; i++)
                    _save[i] = batch.States[i][k];
                int halvings = 0;
                while (true)
                {
                    int sub = 1 << halvings;
                    double h = dt / sub;
                    bool ok = true;
                    for (int j = 0; j < sub && ok; j++)
                    {
                        Step(kernel, batch, t + j * h, h, k, k + 1);
                        ok = LastConverged && Finite(batch, k, n);
                    }
                    if (ok || halvings >= maxHalvings)
                        break;
                    for (int i = 0; i < n; i++)
                        batch.States[i][k] = _save[i];
                    halvings++;
                }
                worst = Math.Max(worst, halvings);
            }
            kernel.ComputeVariables(t + dt, batch, start, end);
            return worst;
        }

        private static bool Finite(KernelBatch batch, int k, int n)
        {
            for (int i = 0; i < n; i++)
                if (double.IsNaN(batch.States[i][k]) || double.IsInfinity(batch.States[i][k]))
                    return false;
            return true;
        }

        private double[] _save = Array.Empty<double>();

        public void Step(IKernel kernel, KernelBatch batch, double t, double dt, int start, int end)
        {
            int n = batch.States.Length;
            Ensure(n);
            double[][] y = batch.States;
            double t1 = t + dt;
            LastConverged = true;
            for (int k = start; k < end; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    _y0[i] = y[i][k];
                    _scale[i] = Math.Abs(_y0[i]) + 1e-12;
                }
                bool fresh = !ReuseJacobian || _jacobianFor != k || _jacobianAge >= MaxJacobianAge;
                bool converged = Newton(kernel, batch, k, n, t1, dt, fresh, out int it);
                if (!converged && !fresh)
                {
                    // the kept Jacobian was too stale: start again from y0 with a new one
                    for (int i = 0; i < n; i++)
                        y[i][k] = _y0[i];
                    converged = Newton(kernel, batch, k, n, t1, dt, true, out int it2);
                    it += it2;
                }
                _jacobianAge++;
                LastIterations = it;
                LastConverged &= converged;
            }
            kernel.ComputeVariables(t1, batch, start, end);
        }

        /// <summary>
        /// Newton on G(y) = y - y0 - dt f(y) for instance k; true when the residual G itself is below tolerance (relative
        /// to each state's size). Testing the residual, not the size of the update, matters with a kept Jacobian: the
        /// iteration then converges linearly, and a small update can still be far from the solution.
        /// </summary>
        private bool Newton(IKernel kernel, KernelBatch batch, int k, int n, double t1, double dt, bool fresh, out int it)
        {
            double[][] y = batch.States;
            double[][] r = batch.Rates;
            double previous = double.MaxValue;
            for (it = 0; it < _maxIterations; it++)
            {
                kernel.ComputeRates(t1, batch, k, k + 1);
                RateEvaluations++;
                double residual = 0.0;
                for (int i = 0; i < n; i++)
                {
                    _f[i] = r[i][k];
                    _g[i] = y[i][k] - _y0[i] - dt * _f[i];
                    residual = Math.Max(residual, Math.Abs(_g[i]) / (Math.Abs(y[i][k]) + _scale[i]));
                }
                if (double.IsNaN(residual))
                    return false;
                if (residual < _tolerance)
                    return true;   // y solves the step (it counts the evaluations made)
                if (fresh || !ReuseJacobian)
                {
                    Jacobian(kernel, batch, k, n, t1);
                    fresh = false;
                    _jacobianFor = k;
                    _jacobianAge = 0;
                    _luStep = double.NaN;
                }
                if (_luStep != dt && !Factor(n, dt))
                    return false;
                for (int i = 0; i < n; i++)
                    _dx[i] = -_g[i];
                BackSolve(n);
                double norm = 0.0;
                for (int i = 0; i < n; i++)
                {
                    y[i][k] += _dx[i];
                    norm = Math.Max(norm, Math.Abs(_dx[i]) / (Math.Abs(y[i][k]) + _scale[i]));
                }
                if (double.IsNaN(norm))
                    return false;
                // a kept Jacobian that no longer contracts the iteration fast enough is refreshed
                if (ReuseJacobian && it > 0 && norm > 0.5 * previous)
                    fresh = true;
                previous = norm;
            }
            return false;
        }

        /// <summary>df/dy at the current state by forward differences (the rates there are in _f).</summary>
        private void Jacobian(IKernel kernel, KernelBatch batch, int k, int n, double t1)
        {
            double[][] y = batch.States;
            double[][] r = batch.Rates;
            for (int jc = 0; jc < n; jc++)
            {
                double yj = y[jc][k];
                double h = 1e-7 * (Math.Abs(yj) + _scale[jc]);
                y[jc][k] = yj + h;
                kernel.ComputeRates(t1, batch, k, k + 1);
                RateEvaluations++;
                for (int i = 0; i < n; i++)
                    _jf[i, jc] = (r[i][k] - _f[i]) / h;
                y[jc][k] = yj;
            }
            JacobianEvaluations++;
        }

        /// <summary>LU factors (partial pivoting) of I - dt df/dy into _lu/_piv; false if singular.</summary>
        private bool Factor(int n, double dt)
        {
            for (int i = 0; i < n; i++)
                for (int jc = 0; jc < n; jc++)
                    _lu[i, jc] = (i == jc ? 1.0 : 0.0) - dt * _jf[i, jc];
            for (int c = 0; c < n; c++)
            {
                int p = c;
                double best = Math.Abs(_lu[c, c]);
                for (int i = c + 1; i < n; i++)
                    if (Math.Abs(_lu[i, c]) > best)
                    {
                        best = Math.Abs(_lu[i, c]);
                        p = i;
                    }
                if (best == 0.0 || double.IsNaN(best))
                {
                    _luStep = double.NaN;
                    return false;
                }
                _piv[c] = p;
                if (p != c)
                    for (int jc = 0; jc < n; jc++)
                    {
                        double tmp = _lu[c, jc];
                        _lu[c, jc] = _lu[p, jc];
                        _lu[p, jc] = tmp;
                    }
                for (int i = c + 1; i < n; i++)
                {
                    double factor = _lu[i, c] / _lu[c, c];
                    _lu[i, c] = factor;
                    if (factor == 0.0)
                        continue;
                    for (int jc = c + 1; jc < n; jc++)
                        _lu[i, jc] -= factor * _lu[c, jc];
                }
            }
            _luStep = dt;
            return true;
        }

        /// <summary>Solves (I - dt df/dy) x = _dx in place with the kept factors.</summary>
        private void BackSolve(int n)
        {
            for (int c = 0; c < n; c++)
            {
                int p = _piv[c];
                if (p != c)
                {
                    double tb = _dx[c];
                    _dx[c] = _dx[p];
                    _dx[p] = tb;
                }
                for (int i = c + 1; i < n; i++)
                    _dx[i] -= _lu[i, c] * _dx[c];
            }
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = _dx[i];
                for (int jc = i + 1; jc < n; jc++)
                    sum -= _lu[i, jc] * _dx[jc];
                _dx[i] = sum / _lu[i, i];
            }
        }

        private void Ensure(int n)
        {
            if (_y0.Length == n)
                return;
            _y0 = new double[n];
            _f = new double[n];
            _g = new double[n];
            _dx = new double[n];
            _scale = new double[n];
            _jf = new double[n, n];
            _lu = new double[n, n];
            _piv = new int[n];
            _jacobianFor = -1;
        }
    }
}
