# Cells on libcuflynx's generated C and solver

Decision (2026-10-12): the game's cells move from AdaptOrDeity's own C# kernels (`tools/kernelgen`) and integrator
(`BackwardEulerIntegrator`, `FermenterCell`'s adaptive stepping) to libcuflynx's `BE_batched_c`: kernels generated as
C from libCellML's analysis (`generators/kernel_ir.py`, `generators/cpp/batched.py`) and its runtime (adaptive backward
Euler or TR-BDF2, modified Newton with the exact Jacobian). Report: [docs/reports/batched-c-solver.html](../reports/batched-c-solver.html).

Why: on the colony protocol of `tools/interop` it is ~19x faster than the game's C# on one thread at rtol 1e-4 and
more accurate (0.20% from CVODES with TR-BDF2 against 1.19%), its rates take 258 ns per cell against 400-650 ns, and
its Jacobian 0.74 us against 3.9 us. One generator then serves libcuflynx, PhLynx and the game (issue #573).

## What the game needs that the runtime does not have yet

The runtime integrates the blocks of a fixed module array, all at once, on OpenMP threads (`bk_run`). The game:

- adds and removes cells as the colony grows and dies;
- advances each cell by one world step (0.1 s) from its own worker threads (`ParallelWork`), so results stay
  bit-identical for any number of workers and food and forces can be settled between steps;
- changes a cell's parameters at any time (player traits and controls, food at the membrane, the motor) and reads a
  few algebraic variables after every step (uptake, thrust, growth progress, pH; `outputs` in modules.lock.json),
  the rest only for the inspected cell.

## Steps

1. **libcuflynx: an instance API** in the runtime, next to `bk_run`. One instance is one block of one kernel with its
   own one-array `variables` and `states`:
   `bk_instance_create(kernel)`, `bk_instance_variables/states` (pointers the caller writes),
   `bk_instance_advance(inst, t1)` (restarts when an input it reads changed, else continues its step),
   `bk_instance_computed_constants(inst)` (libCellML's `computeComputedConstants` after a parameter change),
   `bk_instance_outputs(inst)` (an outputs program from `kernel_ir`: listed algebraic variables and what they need)
   and `bk_instance_variables_all(inst)` (libCellML's `computeVariables`, on demand), `bk_instance_destroy`.
   No OpenMP in this path. Tests in libcuflynx: an instance gives the same result as `bk_run` on a one-block model;
   bit-identical when instances are stepped from several threads.
2. **libcuflynx: build a one-cell library for the game** from `content/assembled/<type>/<version>` (flat CellML), with
   a descriptor JSON: states, variables (name, units, kind, one-array index, initial value), the kernel's inputs, the
   outputs list. A command for AdaptOrDeity's build: `libcuflynx-batched-build <flat.cellml> <out dir> --outputs ...`.
3. **AdaptOrDeity: a cell on the C library.** `Sim.Physiology` gains a cell backend that P/Invokes the library
   (works from the NativeAOT library and from Unity's Mono); `FermenterCell` keeps its API (traits, controls,
   budget, outputs on demand) over either backend. The world keeps stepping cells from `ParallelWork`.
   Tests: the C backend agrees with the C# one within the integrator tolerance on the single-cell and colony tests;
   results are bit-identical on 1, 4 and 8 workers; `tools/interop/test_solver_agreement.py` against CVODES.
4. **Packaging.** `tools/scripts/build_native_sim.sh` and `build_sim_for_unity.sh` copy the cell library next to
   `aod_sim`; Linux first, then Windows and macOS (the build is CMake already).
5. **Retire the C# path** once the game runs on the C backend in Unity: `tools/kernelgen`'s C# emitter, `jacobian.py`
   and `BackwardEulerIntegrator` go; the golden traces stay as the regression reference.

## Open

- Whether computed constants should be computed inside the kernel (no libCellML call after a parameter change) and
  constants written in as literals: measure after step 3.
- Group inputs (`per_cell_inputs`): the kernel already reads inputs once per call; sharing them across a group's
  cells matters only for a structure-of-arrays layout, which the instance API does not use.
