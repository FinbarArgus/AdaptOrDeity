# 0004 — New physiology modules are authored upstream first

**Status:** accepted (2026-10-06)

**Decision.** New modules (glucose uptake, energy metabolism, heat, damage, …) are written in
`circulatory-autogen-modules`, pass `cam_testing`, and only then are pinned via the submodule. During
development the submodule may temporarily pin a feature-branch commit; release builds require
`reviewed: true` modules on the default branch.

**Consequences.** The open-source modules stay the single source of physiological truth; the game repo
stays closed.
