# AdaptOrDeity

A physiology evolution game: Unity 6 front end over an engine-agnostic C# simulation whose physiology
comes from CellML modules in `external/circulatory-autogen-modules`.

**The plan is [`adapting_plan.md`](adapting_plan.md).** Read it before starting work. It holds only what
is still to do: remove items from it once complete, and add new decisions to its decision log.
Feature-level plans go in [`docs/plans/`](docs/plans/), one file per feature, and must never overwrite
`adapting_plan.md` (nor be copied over it from a plan-mode file).

## Commands
```bash
dotnet build -c Release && dotnet test -c Release        # needs ~/.dotnet on PATH
dotnet run --project src/AdaptOrDeity.Headless -- 1.0   # headless one-cell run
tools/scripts/build_sim_for_unity.sh                     # copy sim DLLs into unity/
<libcuflynx venv>/bin/python tools/scripts/build_cell_library.py   # cells as libcuflynx C (aod_cell), before:
CLANG=<clang> tools/scripts/build_native_sim.sh         # the NativeAOT simulation library (aod_sim) for Unity
tools/kernelgen/.venv/bin/aod-kernelgen [--goldens|--check|--update-lock]
tools/kernelgen/.venv/bin/aod-kernelgen-flatten <flat.cellml> content/assembled <type> <version> \
    --modules-root <modules> --supermodule-config <config.json>   # whole cell -> one kernel (ADR 0007)
dotnet run --project src/AdaptOrDeity.Headless -- fermenter 600 H_ATPase_expression=2   # cell panel, headless
dotnet run -c Release --project src/AdaptOrDeity.Headless -- bench 600   # integrator speed and accuracy
dotnet run -c Release --project src/AdaptOrDeity.Headless -- kernel-bench  # kernel cost per cell, batches, threads
dotnet run -c Release --project src/AdaptOrDeity.Headless -- colony-bench 64 60  # a colony on 1..8 worker threads
tools/kernelgen/.venv/bin/pytest tools/kernelgen/tests
dotnet run --project src/AdaptOrDeity.Headless -- model <dir> 10 H_ATPase_expression=2   # creature as a module array
<libcuflynx venv>/bin/python -m pytest tools/interop   # libcuflynx, libcellml, PhLynx read it (see the test's docstring)
<libcuflynx venv>/bin/python -m pytest -s tools/interop/test_solver_agreement.py   # ours vs CVODES: results, timings
```

## Rules
- Layering is enforced by `tests/AdaptOrDeity.ArchitectureTests`: Sim never references Game; nothing
  outside `unity/` references UnityEngine. See [`docs/conventions.md`](docs/conventions.md).
- Physiology equations live upstream in CellML (authored in circulatory-autogen-modules, tested with
  cam_testing); generated kernels in `src/AdaptOrDeity.Sim.Kernels.Generated/Kernels/` are never edited.
- Every module must be **verified** (cam_testing's full test set upstream; golden traces for kernels;
  analytic and conservation tests for native code; homogenisation checks between levels) and ideally
  **validated** against data. See "Verification and validation" in `adapting_plan.md`.
- Canonical SI in the simulation; display units only in the UI.
- libcellml is our combined build (`~/Documents/git_projects/libcellml/perf/build-combined`: upstream main + our
  performance and units PRs), installed into every venv here; never the PyPI release.
- Design records for cross-cutting choices: [`docs/adr/`](docs/adr/).
