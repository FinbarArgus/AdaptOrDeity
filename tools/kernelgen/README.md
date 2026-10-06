# aod-kernelgen

Generates C# kernels (`src/AdaptOrDeity.Sim.Kernels.Generated/Kernels/*.g.cs`) and golden traces
(`tests/golden/`) from the CellML modules listed in `content/modules.lock.json`. See
`docs/adr/0003-build-time-kernels.md`.

```bash
python3 -m venv tools/kernelgen/.venv
tools/kernelgen/.venv/bin/pip install -e 'tools/kernelgen[test]'

tools/kernelgen/.venv/bin/aod-kernelgen              # regenerate kernels
tools/kernelgen/.venv/bin/aod-kernelgen --goldens    # ... and golden traces (Myokit/CVODE)
tools/kernelgen/.venv/bin/aod-kernelgen --check      # CI: fail if committed kernels are stale
tools/kernelgen/.venv/bin/aod-kernelgen --update-lock  # accept changed module files (after review)
tools/kernelgen/.venv/bin/pytest tools/kernelgen/tests
```

## What it does, per module version
1. **Licence gate**: the module config's `licence` must be in `licence_allowlist`.
2. **Analysis**: parses the CellML 1.1 component with libcellml (`<0.7`). Every `public_interface="in"`
   variable (except the variable of integration) and every constant/boundary condition becomes an
   *external* the runtime fills; states initialised from another variable copy that external at
   `Initialise`.
3. **Units**: the variable of integration must be seconds. Every variable's SI multiplier is recorded in
   the descriptor (port variables may be in mV, nA, pF, …, as in the library's electrophysiology modules);
   the runtime assembler checks that both ends of a connection agree.
4. **Emit**: one structure-of-arrays C# class per module version, walking libcellml's equation ASTs.
   Algebraic equations run before the ODEs that use them.
5. **Goldens**: the module runs standalone in Myokit (CVODE, rtol 1e-10) with inputs held at instance
   values and unconnected port inputs at 0; the C# tests must match within NRMSE 1e-6.

Not yet supported (the generator fails loudly): supermodules (multiple components), non-linear
algebraic systems, implicit equations.
