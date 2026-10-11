# Diffusion on CPU and GPU

Measurements behind the "cells on the CPU, diffusion on the GPU" section of
[docs/reports/engine-and-gpu.html](../../../docs/reports/engine-and-gpu.html). Not part of the build.

```bash
dotnet run -c Release                    # CPU, float: explicit 5-point step and ADI step, one thread and all threads
python3 -m venv .venv && .venv/bin/pip install pyopencl numpy
.venv/bin/python gpu_diffusion.py        # GPU explicit step (GPU timestamps), coupling 5,000 cells, CPU FFT exact step
```
