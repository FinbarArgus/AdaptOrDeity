using System;

namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// Backward (implicit) Euler over a kernel batch, for stiff kernels such as a whole cell whose Ca buffer relaxes in
    /// microseconds while its ion contents change over hours. Each instance solves y₁ = y₀ + dt·f(y₁) by Newton's method
    /// with a finite-difference Jacobian and a dense LU solve. L-stable: large steps damp fast modes instead of
    /// blowing up, and a converged step at steady state stays there exactly. First order, so it is for macro steps where
    /// the fast modes are in quasi-equilibrium; externals are held constant over the step. Allocation-free once warm.
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
        private double[,] _j = new double[0, 0];

        /// <param name="maxIterations">Newton iterations per step.</param>
        /// <param name="tolerance">Convergence on the scaled Newton update (relative to |y| + atol).</param>
        public BackwardEulerIntegrator(int maxIterations = 8, double tolerance = 1e-10)
        {
            _maxIterations = maxIterations;
            _tolerance = tolerance;
        }

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
            double[][] r = batch.Rates;
            double t1 = t + dt;
            LastConverged = true;

            for (int k = start; k < end; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    _y0[i] = y[i][k];
                    _scale[i] = Math.Abs(_y0[i]) + 1e-12;
                }
                bool converged = false;
                int it = 0;
                for (; it < _maxIterations && !converged; it++)
                {
                    // Residual G(y) = y - y0 - dt f(y); Jacobian dG/dy = I - dt df/dy by forward differences.
                    kernel.ComputeRates(t1, batch, k, k + 1);
                    for (int i = 0; i < n; i++)
                    {
                        _f[i] = r[i][k];
                        _g[i] = y[i][k] - _y0[i] - dt * _f[i];
                    }
                    for (int jc = 0; jc < n; jc++)
                    {
                        double yj = y[jc][k];
                        double h = 1e-7 * (Math.Abs(yj) + _scale[jc]);
                        y[jc][k] = yj + h;
                        kernel.ComputeRates(t1, batch, k, k + 1);
                        for (int i = 0; i < n; i++)
                            _j[i, jc] = (i == jc ? 1.0 : 0.0) - dt * (r[i][k] - _f[i]) / h;
                        y[jc][k] = yj;
                    }
                    for (int i = 0; i < n; i++)
                        _dx[i] = -_g[i];
                    if (!Solve(n))
                        break;
                    double norm = 0.0;
                    for (int i = 0; i < n; i++)
                    {
                        y[i][k] += _dx[i];
                        norm = Math.Max(norm, Math.Abs(_dx[i]) / (Math.Abs(y[i][k]) + _scale[i]));
                    }
                    converged = norm < _tolerance;
                }
                LastIterations = it;
                LastConverged &= converged;
            }
            kernel.ComputeVariables(t1, batch, start, end);
        }

        /// <summary>Solves J dx = dx in place by LU with partial pivoting; false if singular.</summary>
        private bool Solve(int n)
        {
            for (int c = 0; c < n; c++)
            {
                int p = c;
                double best = Math.Abs(_j[c, c]);
                for (int i = c + 1; i < n; i++)
                    if (Math.Abs(_j[i, c]) > best)
                    {
                        best = Math.Abs(_j[i, c]);
                        p = i;
                    }
                if (best == 0.0 || double.IsNaN(best))
                    return false;
                if (p != c)
                {
                    for (int jc = 0; jc < n; jc++)
                    {
                        double tmp = _j[c, jc];
                        _j[c, jc] = _j[p, jc];
                        _j[p, jc] = tmp;
                    }
                    double tb = _dx[c];
                    _dx[c] = _dx[p];
                    _dx[p] = tb;
                }
                for (int i = c + 1; i < n; i++)
                {
                    double factor = _j[i, c] / _j[c, c];
                    if (factor == 0.0)
                        continue;
                    for (int jc = c; jc < n; jc++)
                        _j[i, jc] -= factor * _j[c, jc];
                    _dx[i] -= factor * _dx[c];
                }
            }
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = _dx[i];
                for (int jc = i + 1; jc < n; jc++)
                    sum -= _j[i, jc] * _dx[jc];
                _dx[i] = sum / _j[i, i];
            }
            return true;
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
            _j = new double[n, n];
        }
    }
}
