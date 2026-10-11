import re, sys, time
import numpy as np, pyopencl as cl
src = open(sys.argv[1]).read()
body = src[src.index('for (int k = start; k < end; k++)'):]
body = body[body.index('{') + 1: body.rindex('}')]
body = body[:body.rindex('}')]; body = body[:body.rindex('}')]   # closes of the method and the for loop
states, exts, rates_ref = [np.array(l.split(), dtype=np.float64) for l in open(sys.argv[2]).read().splitlines()]
NS, PER_CELL = len(states), 20

def kernel_source(real):
    b = body
    b = re.sub(r'\bs(\d+)\[k\]', r'S[\1*n+k]', b)
    b = re.sub(r'\be(\d+)\[k\]', r'E[\1*n+k]', b)
    b = re.sub(r'\bp(\d+)\b', r'P[\1]', b)
    b = re.sub(r'\br(\d+)\[k\]', r'R[\1*n+k]', b)
    b = b.replace('Math.Exp', 'exp').replace('Math.Log', 'log').replace('Math.Pow', 'pow').replace('Math.Abs', 'fabs')
    b = b.replace('Math.Sqrt', 'sqrt').replace('Math.PI', '3.141592653589793')
    if real == 'float':
        b = b.replace('double ', 'float ')
        b = re.sub(r'(?<![\w.])(\d+\.\d*(?:[eE][-+]?\d+)?|\d+[eE][-+]?\d+)(?![\w.])', r'\1f', b)
    pre = '#pragma OPENCL EXTENSION cl_khr_fp64 : enable\n' if real == 'double' else ''
    return pre + f'''__kernel void rates(const int n, __global const {real}* S, __global const {real}* E,
                    __global const {real}* P, __global {real}* R) {{
    const int k = get_global_id(0);
    if (k >= n) return;
    const double t = 1.0;
{b}
}}'''

ctx = cl.create_some_context(interactive=False); q = cl.CommandQueue(ctx)
mf = cl.mem_flags
for real, dt in (('double', np.float64), ('float', np.float32)):
    prog = cl.Program(ctx, kernel_source(real)).build(options=['-cl-mad-enable'] if real == 'float' else [])
    for n in (4096, 65536, 262144):
        S = np.repeat(states[:, None], n, axis=1).astype(dt).ravel()
        E = np.repeat(exts[:PER_CELL, None], n, axis=1).astype(dt).ravel()
        P = exts.astype(dt); R = np.empty(NS * n, dtype=dt)
        bS = cl.Buffer(ctx, mf.READ_ONLY | mf.COPY_HOST_PTR, hostbuf=S); bE = cl.Buffer(ctx, mf.READ_ONLY | mf.COPY_HOST_PTR, hostbuf=E)
        bP = cl.Buffer(ctx, mf.READ_ONLY | mf.COPY_HOST_PTR, hostbuf=P); bR = cl.Buffer(ctx, mf.WRITE_ONLY, R.nbytes)
        k = prog.rates
        k(q, (n,), None, np.int32(n), bS, bE, bP, bR); q.finish()
        reps = 50; t0 = time.perf_counter()
        for _ in range(reps): k(q, (n,), None, np.int32(n), bS, bE, bP, bR)
        q.finish(); dtm = (time.perf_counter() - t0) / reps
        cl.enqueue_copy(q, R, bR); q.finish()
        got = R.reshape(NS, n)[:, 0].astype(np.float64)
        scale = np.maximum(np.abs(rates_ref), 1e-9 * np.max(np.abs(rates_ref)))
        rel = np.max(np.abs(got - rates_ref) / scale)
        if n == 4096:
            for i in np.argsort(-np.abs(got - rates_ref) / scale)[:4]:
                print(f'   rate {i}: C# {rates_ref[i]:.6e}  {real} GPU {got[i]:.6e}')
        t1 = time.perf_counter(); cl.enqueue_copy(q, R, bR); q.finish(); readback = time.perf_counter() - t1
        print(f'{real:6} {n:7} cells: {dtm*1e9/n:7.1f} ns per cell per evaluation ({dtm*1e3:6.2f} ms per call); '
              f'max relative error in rates {rel:.1e}; reading all rates back {readback*1e3:.2f} ms')
