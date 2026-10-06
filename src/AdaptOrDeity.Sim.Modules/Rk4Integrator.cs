namespace AdaptOrDeity.Sim.Modules
{
    /// <summary>
    /// Classic fourth-order Runge-Kutta over a kernel batch, externals held constant during the step.
    /// Owns its scratch buffers so stepping does not allocate once warmed up.
    /// </summary>
    public sealed class Rk4Integrator
    {
        private double[][] _y0 = new double[0][];
        private double[][] _acc = new double[0][];

        public void Step(IKernel kernel, KernelBatch batch, double t, double dt, int start, int end)
        {
            int n = batch.States.Length;
            EnsureScratch(n, batch.Capacity);
            double[][] y = batch.States;
            double[][] r = batch.Rates;

            for (int i = 0; i < n; i++)
                for (int k = start; k < end; k++)
                {
                    _y0[i][k] = y[i][k];
                    _acc[i][k] = 0.0;
                }

            // Stage weights 1, 2, 2, 1 and the time offsets of the following stage.
            Stage(kernel, batch, t, start, end, n, weight: 1.0, nextOffset: 0.5 * dt);
            Stage(kernel, batch, t + 0.5 * dt, start, end, n, weight: 2.0, nextOffset: 0.5 * dt);
            Stage(kernel, batch, t + 0.5 * dt, start, end, n, weight: 2.0, nextOffset: dt);

            kernel.ComputeRates(t + dt, batch, start, end);
            for (int i = 0; i < n; i++)
                for (int k = start; k < end; k++)
                    y[i][k] = _y0[i][k] + dt / 6.0 * (_acc[i][k] + r[i][k]);

            kernel.ComputeVariables(t + dt, batch, start, end);
        }

        private void Stage(IKernel kernel, KernelBatch batch, double t, int start, int end, int n, double weight, double nextOffset)
        {
            kernel.ComputeRates(t, batch, start, end);
            double[][] y = batch.States;
            double[][] r = batch.Rates;
            for (int i = 0; i < n; i++)
                for (int k = start; k < end; k++)
                {
                    _acc[i][k] += weight * r[i][k];
                    y[i][k] = _y0[i][k] + nextOffset * r[i][k];
                }
        }

        private void EnsureScratch(int variables, int capacity)
        {
            if (_y0.Length == variables && (variables == 0 || _y0[0].Length >= capacity))
                return;
            _y0 = new double[variables][];
            _acc = new double[variables][];
            for (int i = 0; i < variables; i++)
            {
                _y0[i] = new double[capacity];
                _acc[i] = new double[capacity];
            }
        }
    }
}
