# Third-party notices

AdaptOrDeity includes or is derived from the following third-party material. Ship this file (and the
referenced licence texts) with every distributed build.

## circulatory-autogen-modules
- Source: https://github.com/physiomelinks/circulatory-autogen-modules (git submodule at
  `external/circulatory-autogen-modules`, pinned commit)
- Repository licence: Apache License 2.0 — full text in `external/circulatory-autogen-modules/LICENSE`
- Per-module licence: each module version declares a `licence` in its `_modules_config.json`. The build
  (`tools/kernelgen`) only accepts modules whose licence is on the allowlist (currently `CC0-1.0`).
- Use in this product: C# kernels in `src/AdaptOrDeity.Sim.Kernels.Generated/` are generated from these
  CellML modules at build time.

TODO before first release: the Apache-2.0 copyright line in the modules repo is still the unfilled
template; agree the copyright holder and any NOTICE text upstream.
