# 0005 — Deterministic simulation

**Status:** accepted (2026-10-06)

**Decision.** Simulation results must be bit-identical for the same inputs regardless of thread count:
fixed chunk partitioning, reductions in chunk order, seeded RNG stored in saves.

**Consequences.** Saves can be continued exactly, forward predictions are reproducible, and goldens are
stable. Tested by comparing 1-thread and N-thread runs.
