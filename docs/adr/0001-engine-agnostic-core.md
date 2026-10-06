# 0001 — Engine-agnostic simulation core shipped to Unity as DLLs

**Status:** accepted (2026-10-06)

**Context.** Unity changed its terms unilaterally in 2023 (Runtime Fee, later cancelled). The
simulation must also run headless for tests, forward predictions and research use.

**Decision.** All simulation and game-rule code is plain C# in `src/` (netstandard2.1 + net8.0) with no
Unity references. Unity consumes the built DLLs from `unity/Packages/com.adaptordeity.sim/Plugins`.

**Consequences.** The compiler and the architecture tests enforce the boundary. A port to another engine
(e.g. Godot C#) only replaces `unity/`. Unity-only acceleration (Burst) must sit behind an interface
(`ITransportBackend`) inside `unity/`.
