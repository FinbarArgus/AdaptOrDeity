# 0003 — Build-time C# kernels generated from CellML

**Status:** accepted (2026-10-06)

**Context.** No Python at runtime; the topology is dynamic, so libcuflynx whole-model generation does
not fit.

**Decision.** `tools/kernelgen` uses libcellml (pinned `>=0.6.3,<0.7`, as libcuflynx) to generate one C#
kernel class per module version, with port and boundary-condition inputs as external variables. A runtime
assembler reimplements libcuflynx port matching to wire kernels per cell. Generated code is committed;
CI regenerates and diffs.

**Consequences.** Each kernel is checked against golden traces from Myokit. The module registry parses
the upstream folder layout itself, so external (mod) folders can be supported later via an interpreter.
