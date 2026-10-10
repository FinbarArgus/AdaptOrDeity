# 0008 — The creature moves as one rigid body, in native code

**Status:** accepted (2026-10-09)

**Context.** The user asked for the creature to move through its environment as one solid body in 2D, propelled only by
flagella fixed on its membrane, with momentum and energy conserved and realistic losses. The flagellar motor (proton
flux, rotation, thrust) is cell physiology; moving a body of many cells through water is mechanics of the whole
organism in its environment, like the transport graph and the food field (ADR 0002: native environment code).

**Decision.**
- The motor is CellML (`cell/ion_channels/i_H_flagellum` MotAB): proton flux and power from the proton motive force,
  thrust per flagellum `F_one` against the Stokes drag of a single cell, with an invariant that the mechanical power
  never exceeds the motor's.
- The body is native (`Sim.World.RigidBody2D`, `CreatureBody`): cells at fixed places in the body frame, flagella at
  fixed angles on each cell, forces and torques from the kernels' thrust, mass from the cells' volume and density, linear
  Stokes drag. Each step is the exact solution for forces held over the step, so the momentum balance
  (M dv = F dt − ξ dx) and the energy balance (thrust work = kinetic energy + heat dissipated) hold to round-off,
  although the velocity relaxes in ~10⁻⁷ s (Reynolds number ~10⁻⁵). Tests check both.
- Randomness (when the motors run, each run's thrust-angle error) is a seeded game rule.

**Consequences.** Swimming couples to physiology through one number per cell per step (thrust from the last step's
state). Hydrodynamic interaction between cells, wall effects and Brownian motion are neglected; the drag of a colony is
the sum of its cells'. Muscle cells (later) add forces to the same body.
