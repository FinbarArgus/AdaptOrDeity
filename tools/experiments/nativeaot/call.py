"""Load the NativeAOT library from Python (ctypes) and run its measurements: a stand-in for any C caller."""
import ctypes, sys
lib = ctypes.CDLL(sys.argv[1])
lib.aod_kernel_rates_ns.restype = ctypes.c_double; lib.aod_kernel_rates_ns.argtypes = [ctypes.c_int]
lib.aod_colony_seconds.restype = ctypes.c_double; lib.aod_colony_seconds.argtypes = [ctypes.c_int, ctypes.c_double, ctypes.c_int]
print('ping', lib.aod_ping())
for n in (256, 4096, 65536):
    print(f'kernel rates, {n} cells, 1 thread: {lib.aod_kernel_rates_ns(n):.0f} ns per cell')
lib.aod_colony_seconds(64, 120.0, 1)   # warm (one-off costs)
for w in (1, 2, 4, 8):
    s = lib.aod_colony_seconds(64, 60.0, w)
    print(f'colony 64 cells, 60 s, {w} workers: {s:.3f} s ({60 / s:.0f}x real time)')
