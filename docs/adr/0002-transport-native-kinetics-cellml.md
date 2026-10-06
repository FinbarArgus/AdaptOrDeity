# 0002 — Native spatial transport, CellML local kinetics (operator splitting)

**Status:** accepted (2026-10-06)

**Context.** The body topology changes every time the player paints a cell, and the environment is a
256² advection-diffusion field. Representing every face as a CellML module instance would be too slow.

**Decision.** A native C# finite-volume solver owns diffusion, advection and heat conduction for body and
environment, using the same flux law as `tissue_diffusion_face` (G = σA/(d_up+d_down)). CellML kernels
own the local per-cell kinetics. They are coupled by Strang splitting.

**Consequences.** The transport solver needs analytic and parity tests against libcuflynx-assembled
face/volume models. Splitting error must be checked against unsplit references.
