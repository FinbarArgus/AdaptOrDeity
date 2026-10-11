using System; using System.Diagnostics; using System.Threading.Tasks;

// 2D diffusion on an N x N grid in float, on the CPU: one explicit 5-point step (stable only for D dt/dx² <= 1/4) and
// one ADI step (Peaceman-Rachford: two tridiagonal sweeps, stable for any step), on one thread and on all threads
// (rows split into fixed chunks). Microseconds per step. For docs/reports/engine-and-gpu.html.
Console.WriteLine($"{Environment.ProcessorCount} threads; microseconds per step");
Console.WriteLine("   N   explicit 1T  explicit all   ADI 1T   ADI all");
foreach (int n in new[] { 256, 512, 1024, 2048 })
{
    var a = new float[n * n]; var b = new float[n * n];
    for (int i = 0; i < a.Length; i++) a[i] = (float)Math.Sin(i * 0.001);
    float r = 0.2f;
    double e1 = T(() => Explicit(a, b, n, r, 0, n));
    double eP = T(() => Parallel.For(0, 20, c => Explicit(a, b, n, r, n * c / 20, n * (c + 1) / 20)));
    var adi = new Adi(n, 5.0f);
    double i1 = T(() => adi.Step(a, b, false));
    double iP = T(() => adi.Step(a, b, true));
    Console.WriteLine($"{n,5} {e1,12:F0} {eP,13:F0} {i1,9:F0} {iP,9:F0}");
}

static void Explicit(float[] u, float[] v, int n, float r, int y0, int y1)
{
    for (int y = Math.Max(1, y0); y < Math.Min(n - 1, y1); y++)
    {
        int row = y * n;
        for (int x = 1; x < n - 1; x++)
        {
            int i = row + x;
            v[i] = u[i] + r * (u[i - 1] + u[i + 1] + u[i - n] + u[i + n] - 4f * u[i]);
        }
    }
}

static double T(Action step)
{
    step(); int reps = 1; var sw = Stopwatch.StartNew();
    while (sw.Elapsed.TotalSeconds < 0.05) { step(); reps++; }
    reps = Math.Max(1, (int)(reps * 0.3 / sw.Elapsed.TotalSeconds)); double best = double.MaxValue;
    for (int k = 0; k < 3; k++) { sw.Restart(); for (int i = 0; i < reps; i++) step(); best = Math.Min(best, sw.Elapsed.TotalSeconds * 1e6 / reps); }
    return best;
}

// Peaceman-Rachford ADI with r = D dt / dx² per half step: implicit in x then in y. The tridiagonal matrix is the same
// for every line, so its LU factors are computed once; each line is one forward and one backward sweep.
sealed class Adi
{
    readonly int n; readonly float r; readonly float[] mid, cp, inv;
    [ThreadStatic] static float[]? dp;
    public Adi(int n, float r)
    {
        this.n = n; this.r = r; mid = new float[n * n]; cp = new float[n]; inv = new float[n];
        float a = -0.5f * r, b = 1f + r, c = -0.5f * r;
        for (int i = 0; i < n; i++) { float m = i == 0 ? b : b - a * cp[i - 1]; inv[i] = 1f / m; cp[i] = c * inv[i]; }
    }

    public void Step(float[] u, float[] v, bool parallel)
    {
        if (parallel) { Parallel.For(0, 20, c => Rows(u, mid, n * c / 20, n * (c + 1) / 20)); Parallel.For(0, 20, c => Cols(mid, v, n * c / 20, n * (c + 1) / 20)); }
        else { Rows(u, mid, 0, n); Cols(mid, v, 0, n); }
    }

    float[] Scratch() => dp is { } d && d.Length == n ? d : (dp = new float[n]);

    // implicit along x, explicit along y
    void Rows(float[] u, float[] w, int y0, int y1)
    {
        var d = Scratch(); float a = -0.5f * r, h = 0.5f * r;
        for (int y = y0; y < y1; y++)
        {
            int row = y * n, upRow = y > 0 ? row - n : row, dnRow = y < n - 1 ? row + n : row;
            for (int x = 0; x < n; x++)
            {
                float rhs = u[row + x] + h * (u[upRow + x] + u[dnRow + x] - 2f * u[row + x]);
                d[x] = (rhs - (x > 0 ? a * d[x - 1] : 0f)) * inv[x];
            }
            float s = d[n - 1]; w[row + n - 1] = s;
            for (int x = n - 2; x >= 0; x--) { s = d[x] - cp[x] * s; w[row + x] = s; }
        }
    }

    // implicit along y, explicit along x (columns, strided)
    void Cols(float[] u, float[] w, int x0, int x1)
    {
        var d = Scratch(); float a = -0.5f * r, h = 0.5f * r;
        for (int x = x0; x < x1; x++)
        {
            int l = x > 0 ? -1 : 0, rr = x < n - 1 ? 1 : 0;
            for (int y = 0; y < n; y++)
            {
                int i = y * n + x;
                float rhs = u[i] + h * (u[i + l] + u[i + rr] - 2f * u[i]);
                d[y] = (rhs - (y > 0 ? a * d[y - 1] : 0f)) * inv[y];
            }
            float s = d[n - 1]; w[(n - 1) * n + x] = s;
            for (int y = n - 2; y >= 0; y--) { s = d[y] - cp[y] * s; w[y * n + x] = s; }
        }
    }
}
