# Feature plan: exact sparse Jacobians generated ahead of time

Done, then superseded (2026-10-12): the differentiation moved to libcuflynx (`kernel_ir.py`), whose generated C the
game now runs; kernelgen no longer emits kernels (docs/plans/libcuflynx-solver.md).

Step 3 of the integration speed-ups (steps 1–2, Jacobian reuse and adaptive steps, are done: see the `bench` numbers in
`adapting_plan.md`). Status (2026-10-11): parts 1–2 done (structural dependence and forward-mode `ComputeJacobian`, in
`tools/kernelgen/aod_kernelgen/jacobian.py`; `JacobianTests` checks every kernel against central differences and fails
on a 0.1% error in one entry). The fermenter's Jacobian has 248 structural non-zeros of 625 and costs 3.9 µs against
23 µs for finite differences. The integrator falls back to finite differences where an exact entry is not finite (a
concentration underflowing in a denominator; 103 of 346 refreshes in the famine golden). Column colouring and ∂f/∂p
are not done; coupled multi-cell systems will need the pattern for a sparse LU.

## What circulatory_autogen does today (surveyed 2026-10-11)
- **No ahead-of-time derivatives.** libcellml generates Python (or C for the C++ path); derivatives come from
  re-executing that code with overloaded number types at run time: CasADi SX tracing (`casadi_python_solver_helper.py`
  155–219) or an AADC tape (`aadc_python_solver_helper.py`), rebuilt per process or run. The generated code is patched by
  regex and AST to make it traceable (`PythonGenerator.py` 27–156), which breaks when libcellml's output changes.
- **The state Jacobian is mostly not used.** The production solvers (Myokit CVODE, the C++ CVODE driver, scipy) build
  difference-quotient Jacobians; only AADC's `bdf_newton` and CasADi's own integrators use exact ones. Myokit's forward
  sensitivities use a difference-quotient right-hand side and lose accuracy below rtol 1e-9.
- **Integration is unrolled into the derivative graph** (CasADi `mapaccum`, whole-trajectory AADC tapes): memory and
  recording time grow with the number of steps, and a change of dt means re-recording.
- **Dense everywhere.** The 3-compartment model's Jacobian is ~10% non-zero (6 colours would cut finite differences
  4.5×), but nothing uses sparsity.
- Four backends carry the same semantics separately, and have diverged (zero gradients from computed constants, #389;
  a wrong BDF2 sensitivity formula).

## What we do instead
kernelgen already reads libcellml's analyser AST directly (no generated-code patching) and emits C# kernels. It will
also emit, at build time, from the same AST:

1. **The sparsity pattern** of ∂f/∂y: which states each rate depends on, by following the algebraic dependency chain
   structurally. Recorded in the kernel descriptor.
2. **A forward-mode derivative kernel** (`ComputeJacobian`): every assignment `a = g(b, c)` is emitted with its tangent
   `ȧ = ∂g/∂b ḃ + ∂g/∂c ċ`, a mechanical transformation of each equation: no expression swell, exact, compiled ahead of
   time like the rates. Piecewise expressions differentiate the active branch.
3. **Column colouring** of the pattern (greedy, structurally independent columns share a seed), so one pass of the
   derivative kernel fills several columns. For our cell (25 states) that is a handful of passes instead of 26 rate
   evaluations, and the Jacobian is exact rather than finite-difference.
4. **∂f/∂p for parameters** from the same transformation, for calibration and sensitivities later, so there is one
   source of derivatives for every consumer instead of four.

The integrator then takes the generated Jacobian when the kernel has one (finite differences remain the fallback) and,
for coupled multi-cell systems, the sparsity pattern for a sparse LU.

**Verification.** A kernelgen test compares the generated Jacobian with finite differences on every kernel at several
states (relative 1e-6); the C# goldens and the `bench` accuracy column must not change.

**Expected gain.** Jacobian refreshes are now rare (kept across steps), so the main win is exactness and robustness
after big input changes, plus the cheaper refresh; for coupled systems of many cells the sparsity is the larger win.

## If we generate C or C++ with libcellml
Measured 2026-10-11 on the fermenter cell replicated N times in one model, sharing one time variable (seconds;
`~/Documents/git_projects/libcellml/perf/bench/replicated_cell_bench.py fermenter_flat.cellml <label> 1 4 16 64`, with
the build's bindings first on `PYTHONPATH`):

| cells (variables) | 0.6.3 (pip, in use): analyse / generate C | `perf/generation-speed`: analyse / generate C | + `perf/generator-lookup`: parse / analyse / generate C |
|---|---|---|---|
| 1 (567) | 1.28 / 0.29 | 0.05 / 0.01 | 0.02 / 0.07 / 0.00 |
| 8 (4,536) | 182 / 70 | 0.43 / 1.68 | 0.15 / 0.43 / 0.05 |
| 16 (9,072) | — | 1.31 / 8.38 | 0.24 / 0.79 / 0.11 |
| 64 (36,288) | (hours, extrapolated) | — | 1.02 / 3.66 / 1.01 |

`perf/generator-lookup` (libcellml perf checkout, `perf/src-gen`, one commit on `perf/generation-speed`) removes four
whole-model scans found with callgrind: `AnalyserModel::analyserVariable()` (group → variable map),
`Generator::analyserVariableIndexString()` (indices cached per generation), `Parser::loadConnection()` (components
indexed by name) and the analyser's variable → equations gathering. Output is byte-identical (printed models and
generated C), and libcellml's 75 test suites pass. Validation (MathML element checks, ~30% of analysis) is the largest
remaining cost and is linear.

Next for libcellml: open PRs for `perf/generation-speed` and `perf/generator-lookup` upstream (after review), and a
regression benchmark in its CI on replicated models.
