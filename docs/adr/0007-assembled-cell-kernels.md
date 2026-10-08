# 0007 — Whole cells assembled at build time into one stiff kernel

**Status:** accepted (2026-10-08)

**Context.** The fermenter cell is a supermodule of 15 upstream modules joined through ports. ADR 0003 compiles
modules to C# kernels, and the plan foresees a runtime assembler for player-built networks. A cell's internal
wiring, however, is fixed by its supermodule, and its Ca buffer relaxes in about 1 µs while its ion contents change
over hours, so explicit fixed steps would need microsecond macro steps.

**Decision.**
- A supermodule is assembled at build time: libcuflynx (the same tool cam_testing verifies it with) writes the flat
  model; `aod-kernelgen-flatten` merges it into one component (each connected variable set becomes one variable,
  parameters keep their `<variable>_mod_<submodule>` names, literal units are restored from the source modules
  because libcuflynx relabels some conversion factors) and writes it under `content/assembled/` in the library's
  layout. The lock entry carries `root: content/assembled`; kernels and goldens follow as for any module.
- Stiff kernels are stepped by backward Euler with Newton iterations and a finite-difference Jacobian
  (`BackwardEulerIntegrator`): L-stable at 0.1 s macro steps, exact at steady states. Goldens record how to replay
  them (`replay`: integrator, dt, NRMSE tolerance, states compared).
- Player traits change kernel parameters at runtime (externals are per-instance arrays); the runtime assembler is
  still for connecting cells and tissues to the transport graph.

**Consequences.** A cell is one kernel with ~100 parameters and 11 states; regenerate `content/assembled/` whenever
its upstream modules change (it records its source). Backward Euler is first order: at the game step it tracks CVODE
to NRMSE ~1e-4 through famine (tests), and fast electrical transients (ms) are seen only in quasi-steady form.
