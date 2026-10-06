# 0006 — Three levels of detail on one finite-volume graph; heat from enthalpy release

**Status:** accepted (2026-10-06); amended 2026-10-07 (see below)

**Context.** A single cell needs gradients *through* the cell; large organisms can't afford that. The
player should choose where detail matters. Coarse models must stay consistent with the finer ones, and
total energy must be conserved once temperature is simulated.

**Decision.**
- Volumes on the transport graph sit at level 0 (sub-cellular), 1 (lumped cell CellML module) or 2
  (homogenised tissue CellML module), chosen per region by the player. Interfaces are ordinary
  conservative faces.
- Coarse parameters are derived from the finer level where a formula exists, calibrated otherwise.
- Each level pair has upstream homogenisation checks in circulatory-autogen-modules: steady state 1%,
  transient NRMSE 5%, across parameter sweeps, plus mass, energy (first law) and second-law checks.
- Heat = enthalpy release: viscous/Joule dissipation and reaction ΔH produce heat; ideal diffusion of
  dilute solutes produces none (its dissipation is entropic). Every element's dissipation is ≥ 0.

**Consequences.** Level switching is a topology rebuild with state remapped conservatively (coarse value
= volume-weighted mean of fine; fine values initialised from the coarse value). The homogenisation
error of each level is known and reported, so the game can show what accuracy a chosen level costs.

**Amendment (2026-10-07).** The game runs only levels 1 (lumped cells) and 2 (tissue). Within-cell
gradients are under 1% up to ~15–20 µm radius and sub-cell diffusion modes would dominate stiffness during
fast-forward, so level 0 is not simulated at runtime: shaders draw within-cell detail from the analytic
profile of the lumped model, and cells beyond validity (Φ = qR²/(D·C) > ~0.1) use the effectiveness factor
η(Φ). Level-0 finite-volume models remain upstream as the reference in the homogenisation checks.
