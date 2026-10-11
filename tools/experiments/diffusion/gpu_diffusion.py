"""2D diffusion in float on the GPU (OpenCL): microseconds per explicit 5-point step with many steps queued, and the
cost of coupling 5,000 cells per exchange (gather the field at each cell and read it back; send each cell's uptake and
add it in). Also the CPU cost of an exact spectral step (FFT, multiply by exp(-D k² dt), inverse FFT) with numpy.
For docs/reports/engine-and-gpu.html."""
import time
import numpy as np
import pyopencl as cl

SRC = '''
__kernel void step(const int n, const float r, __global const float* u, __global float* v) {
    const int x = get_global_id(0), y = get_global_id(1);
    if (x == 0 || y == 0 || x >= n - 1 || y >= n - 1) return;
    const int i = y * n + x;
    v[i] = u[i] + r * (u[i - 1] + u[i + 1] + u[i - n] + u[i + n] - 4.0f * u[i]);
}
__kernel void gather(const int n, __global const float* u, __global const int* at, __global float* out) {
    const int k = get_global_id(0); out[k] = u[at[k]];
}
__kernel void deposit(__global float* u, __global const int* at, __global const float* amount) {
    const int k = get_global_id(0); u[at[k]] -= amount[k];
}'''
ctx = cl.create_some_context(interactive=False); mf = cl.mem_flags
q = cl.CommandQueue(ctx, properties=cl.command_queue_properties.PROFILING_ENABLE)
prg = cl.Program(ctx, SRC).build()
CELLS = 5000
print('   N   GPU step on GPU (us)  issued from Python (us)  CPU FFT exact step (us)  coupling 5k cells: gather+readback, upload+deposit (us)')
for n in (256, 512, 1024, 2048):
    a = np.random.rand(n * n).astype(np.float32)
    ua = cl.Buffer(ctx, mf.READ_WRITE | mf.COPY_HOST_PTR, hostbuf=a); ub = cl.Buffer(ctx, mf.READ_WRITE, a.nbytes)
    steps = 2000 if n <= 512 else 200
    prg.step(q, (n, n), None, np.int32(n), np.float32(0.2), ua, ub); q.finish()
    t0 = time.perf_counter(); events = []
    for s in range(steps):
        src, dst = (ua, ub) if s % 2 == 0 else (ub, ua)
        events.append(prg.step(q, (n, n), None, np.int32(n), np.float32(0.2), src, dst))
    q.finish(); issued = (time.perf_counter() - t0) / steps * 1e6
    gpu_step = np.median([(e.profile.end - e.profile.start) * 1e-3 for e in events])   # microseconds on the GPU

    at = np.random.randint(0, n * n, CELLS).astype(np.int32); amount = np.full(CELLS, 1e-6, np.float32)
    bat = cl.Buffer(ctx, mf.READ_ONLY | mf.COPY_HOST_PTR, hostbuf=at); bout = cl.Buffer(ctx, mf.WRITE_ONLY, 4 * CELLS)
    bam = cl.Buffer(ctx, mf.READ_ONLY, 4 * CELLS); out = np.empty(CELLS, np.float32)
    best_g = best_d = 1e9
    for _ in range(50):
        t0 = time.perf_counter(); prg.gather(q, (CELLS,), None, np.int32(n), ua, bat, bout); cl.enqueue_copy(q, out, bout); q.finish()
        best_g = min(best_g, time.perf_counter() - t0)
        t0 = time.perf_counter(); cl.enqueue_copy(q, bam, amount); prg.deposit(q, (CELLS,), None, ua, bat, bam); q.finish()
        best_d = min(best_d, time.perf_counter() - t0)

    f = a.reshape(n, n).astype(np.float32)
    k = 2 * np.pi * np.fft.fftfreq(n); k2 = (k[:, None] ** 2 + k[None, : n // 2 + 1] ** 2).astype(np.float32)
    decay = np.exp(-0.2 * k2 * 4000).astype(np.complex64)      # any step length costs the same
    best_f = 1e9
    for _ in range(5 if n >= 1024 else 20):
        t0 = time.perf_counter(); f = np.fft.irfft2(np.fft.rfft2(f) * decay, s=(n, n)).astype(np.float32); best_f = min(best_f, time.perf_counter() - t0)
    print(f'{n:5} {gpu_step:21.1f} {issued:24.1f} {best_f * 1e6:24.0f} {best_g * 1e6:20.0f} {best_d * 1e6:14.0f}')
