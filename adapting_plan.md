# AdaptOrDeity — Adapting Plan

The living high-level plan for AdaptOrDeity: what we are building and what is still to do.
- **Remove items once they are complete.** Finished milestones and tasks leave this file; git history
  and the code keep the record. Decisions stay while they describe the design.
- Record new decisions in the **Decision log** (newest last) and anything undecided in **Open questions**.
- **Feature-level plans are separate files** in [`docs/plans/`](docs/plans/), one per feature. They
  never overwrite this file; when a feature changes the high-level plan, update the relevant section here.
- Design records for cross-cutting technical choices stay in [`docs/adr/`](docs/adr/).

## What the game is
A closed-source **physiology evolution game**. The player adapts an organism, starting as a single
fermenting cell in an early ocean, and must solve real physiological limits (diffusion distance → a
nutrient tube → airways/circulation → thermoregulation …) to colonise every region of a planet that
changes through geological time. Physiology is quantitative and teachable, built from **CellML modules
in the open-source sister repo `circulatory-autogen-modules`** (Apache-2.0 repo, modules CC0-1.0); the
game code stays closed. Built mostly solo by a physiology modeller; the team will grow, so module
boundaries and conventions are first-class.

## Status
| Milestone | State | Notes |
|---|---|---|
| M3 Cell network, lumped cells, shaders | In progress | Upstream: homogenisation pairs built, comparison test not written; fermenter cell modules (bond graph) being written |
| M4–M10 | Not started | |

Upstream work lives on branch `homogenisation-checks` of `circulatory-autogen-modules`, checked out as a
worktree at `~/Documents/git_projects/circulatory-autogen-modules-homogenisation` (uncommitted, awaiting
review). Review page for the first cell modules: https://claude.ai/artifact/UPtUGRQfVaSEzzDa2UXRMx

## Time: the evolutionary time bar
- An **evolutionary time bar** across the top of the screen runs from **−2.0 billion years** through the
  present day and on into the future.
- It advances in **discrete jumps, one per level**. Each level is an epoch: the planet's regions take
  that period's environment (O₂, temperature, nutrients, light, competitors).
- Within a level the organism is simulated **in real time or faster** for **a few typical days or weeks
  of that period** (day/night and tidal cycles where they matter), to see how well the creature copes
  with the new environment. That window produces the level's fitness and mutation budget.
- Speeds therefore span 1× up to ~10⁵–10⁶× (a week is 6 × 10⁵ s). The fast end needs implicit transport,
  stiff/implicit kinetics or quasi-steady-state for fast species, and steady-state initialisation.
- Two clocks: `EvolutionClock` (geological time, discrete per level) and `SimClock` (physiological time
  inside a level's evaluation window; the single source of truth for simulation time).
- Geology for the first levels (to be detailed in content): O₂ appeared with cyanobacteria and the Great
  Oxidation Event ~2.33 Ga, but at −2.0 Ga much of the ocean was still anoxic, so early regions differ
  (oxygenated surface oases vs anoxic deep water).

## Game design
| Area | Decision |
|---|---|
| Starting organism | A **~2 µm prokaryote-grade fermenter** (homolactic: glucose → 2 lactate + 2 ATP), no mitochondria. **Membrane respiration** is an evolvable trait; O₂ arriving is a planet event; **mitochondria (endosymbiosis)** are a later major milestone that lifts the membrane-area energy limit (Lane & Martin 2010; disputed by Lynch & Marinov 2017). |
| Basal metabolic rate | Maintenance ATP from **Lynch & Marinov (2015)**: C_M = 0.39·V^0.88 × 10⁹ ATP/cell/h at 20 °C (V in µm³), scaled by Q10. Growth cost C_G = 27·V^0.97 × 10⁹ ATP per cell when division arrives. Sanity range: resting rates across life 0.3–9 W/kg (Makarieva et al. 2008). |
| Space | 2D unstructured (Voronoi) body mesh with a per-cell depth (2.5D). All editing in the flat view; a later fold into 3D is for visualisation only. |
| Exchange | **In-plane edges only.** Into-screen and out-of-screen surfaces never exchange. Depth only scales volume and in-plane face area. |
| Environment | Structured 256² field around the body in the same plane, advection-diffusion, far-field reservoir boundaries. Creature velocity improves exchange through a boundary layer. |
| Species | Stage 1: glucose, lactate (waste), ATP/ADP/Pi, temperature; O₂ and CO₂ once respiration evolves. Q10 temperature dependence; pH/bicarbonate deferred. |
| Editing | Paint/erase cells (1 painted cell = 1 cell early; later 1 element = N cells, zoom for visuals only). Cell types: generic metabolic, barrier, contractile, sensory, storage. Cell size is a trait. |
| Cost | Real physiological cost (maintenance ATP inside the kernels) plus a mutation budget. |
| Mutation budget | From reproductive output: a population model of the player's fitness vs hard-coded historical competitor creatures per region and epoch, × a `reproductive_organ` multiplier. Asexual for now. |
| Fitness | Pluggable heuristics, global fixed weights, total ∈ [0,1] renormalised over active heuristics. Mass, extremity muscle mass, sensing, cell damage, famine resistance, environmental tolerance, reproductive output; each activates when its physiology exists. |
| Failure | Starving cells accumulate damage → necrosis on a Damage overlay; damage lowers fitness. |
| New cells | Start at their CellType's baseline steady state. |
| UX | Oxygen Not Included style: hotkeyed overlay tabs, legends, hover tooltips, category build menu, speed controls, warnings, level report; live plots on click; flux arrows; educational parameter inspector; stylised 2D art; the evolutionary time bar on top. |

## Physiology modelling
- **Lumped cells in the game, shaders for within-cell detail.** The game runs two levels on one
  finite-volume graph, chosen per region by the player: **level 1 lumped cells** (one well-mixed volume per
  cell; the network *between* cells resolves tissue-scale gradients) and **level 2 homogenised tissue**.
  There is no sub-cellular PDE at runtime: the dip inside a cell is under 1% up to ~15–20 µm radius
  (0.01% for the 2 µm starting cell), and sub-cell diffusion modes (µs–ms) would be the stiffest part of
  fast-forward. Interfaces between levels are ordinary conservative faces.
- **Within-cell visuals by shader:** render the Delaunay triangulation (dual of the Voronoi mesh) with cell
  values at the vertices so the GPU interpolates between cells, and add the analytic within-cell profile
  the lumped model implies, C(r) = C̄ + (q/4D)(R²/2 − r²) (mean-preserving; a per-cell shape function
  precomputed at mesh build replaces r² for Voronoi polygons). The inspector labels it as reconstructed.
- **Giant cells:** a live per-cell number Φ = qR²/(D·C). Below ~0.1 lumping is within 1%; above it the
  cell's rates are multiplied by the **effectiveness factor** η(Φ) (Thiele/Aris; cylinder, first order:
  η = 2I₁(Φ)/(Φ·I₀(Φ))), validated upstream against the finite-volume reference.
- **Level 0 lives upstream only**, as the finite-volume **reference model** (a cell meshed into rings) in the
  homogenisation checks. It validates the lumped bond-graph cell, d_eff, the shader profile and η(Φ)
  across a cell-size sweep.
- **Upscaling:** coarse parameters derived from the finer level where a formula exists, calibrated to it
  where not; both values recorded.
- **Homogenisation checks upstream** (fine vs coarse system-model pairs): steady state within 1%,
  transient NRMSE ≤ 5%, across parameter sweeps, plus mass conservation, energy (first law) and the
  second law in both models.
- **Heat = enthalpy release** (first law): viscous/Joule dissipation and reaction ΔH make heat; ideal
  diffusion and facilitated transport make none (their dissipation is entropic). Every element's
  dissipation ≥ 0 (second law).
- **Bond graphs for conservation, accepted rate laws for kinetics:** cell models are bond graphs
  (species stores with μ = μ° + RT ln(C/c°), junction structure = stoichiometry, so carbon, adenine and
  phosphate are conserved by construction). Each process uses the most widely accepted rate law, or an
  existing module from the repo: common modular rate law (Liebermeister et al. 2010) for lumped enzymes,
  symmetric carrier for facilitated transport, Fick for diffusion.
- **Diffusion (reference) vs lumped (game) version:** the same bond-graph modules build both: N rings of
  `cytosol_volume` joined by `cytosol_face` (the upstream reference) and one `cytosol_volume` (the lumped
  bond graph the game runs). Their comparison is the level 0 ↔ 1 homogenisation check, with a cell-size
  sweep from 2 µm (gradients negligible) to giant cells (where η(Φ) takes over).

## Verification and validation (every module)
Every module in the game must be **verified**, and ideally **validated**. This covers upstream CellML
modules and the game's native elements (transport solver, level interfaces, effectiveness factor η(Φ),
shader reconstruction).

**Verification (required before a module is used in a release build):**
- Upstream CellML modules pass cam_testing's full set: structure, run, invariants (exact solutions,
  conservation of mass, charge and energy, second law), parameter/boundary-condition sweeps over the
  ranges the game exposes, time-step convergence, and solver stability at the game's integrator and step.
- Generated C# kernels match their Myokit golden traces (NRMSE < 1e-6).
- Native C# elements match analytic solutions and conserve exactly (1e-12), and agree with an upstream
  reference model where one exists.
- Every pair of levels passes its homogenisation check (steady 1%, transient NRMSE 5%, sweeps, mass,
  energy, second law).

**Validation (ideally every module):**
- Compared with experimental data in cam_testing's validation tests (baseline against data with an NRMSE
  threshold; calibrate on one data set and predict held-out data), recording the data source.
- A module without validation data is marked **"verified, not validated"**. The game's inspector shows this
  status, and the module is listed below until it is validated.

**Modules still to validate:**
| Module | Candidate validation data |
|---|---|
| `energy_metabolism` (mitochondrial, later stage) | Simultaneous calorimetric heat flux and O₂ flux in cultured cells (Gnaiger & Kemp 1990); ATP turnover rates |
| fermenter cell (`cell/prokaryote/fermenter` and its new submodules) | Maintenance ATP vs cell volume (Lynch & Marinov 2015); lactate production of homolactic bacteria; bacterial membrane potential and K⁺/Na⁺ contents vs medium |
| `membrane_carrier_face` (GLUT1, MCT1; Glf for the fermenter) | Uptake-vs-concentration data (Mueckler & Thorens 2013; Weisser et al. 1995; Halestrap & Price 1999) |
| `thermal_volume`, `heat_conduction_face` | Tissue thermal property measurements (IT'IS database) |

## Architecture
- **Engine:** Unity 6 desktop front end; the simulation is a pure C# engine-agnostic library
  (netstandard2.1 + net8.0) shipped to Unity as DLLs (ADR 0001).
- **CellML:** build-time C# kernels from libcellml (`tools/kernelgen`, ADR 0003); a runtime assembler
  wires kernels per cell by libcuflynx's port rules and checks that both ends of each connection have the same
  units (module ports may be in mV, nA, pF, …; every variable's SI multiplier is in its descriptor); a native finite-volume solver owns spatial transport,
  kernels own local kinetics, coupled by Strang splitting (ADR 0002). Algebraic-only modules (faces,
  carriers) are supported.
- **Upstream-first:** new physiology is authored in circulatory-autogen-modules, passes cam_testing, then
  the submodule pin moves (ADR 0004).
- **Determinism:** bit-identical at any thread count (ADR 0005).
- **Threading:** one SimHost thread; commands in, triple-buffered snapshots out; zero allocation per tick.
- **Layout:** `src/AdaptOrDeity.{Sim.*, Game.*, Headless}`, `tests/`, `tools/kernelgen`, `unity/`,
  `content/`, `docs/`; layering enforced by `tests/AdaptOrDeity.ArchitectureTests`.

### Domain model
- **Planet** → **Region**s (environment per epoch, competitor table per epoch, colonisation record);
  `PlanetDynamics` applies each level's jump.
- **Organism**: BodyPlan (Voronoi seeds + CellType + depth + overrides, stable CellId), runtime state,
  aggregates. **CellType**: modules + instances + overrides, inspector whitelist, costs, tags.
- **Modules**: `KernelDescriptor`, `IKernel` (SoA), `KernelBatch`, `ModuleRegistry`, `Assembler`.
- **Transport**: `ITransportBackend` (implicit diffusion, advection, body↔environment coupling with a
  Sherwood boundary layer; flux law G = σA/(d_up+d_down)).
- **Rules**: `EvolutionClock`, `SimClock`, `Scheduler`, `IFitnessHeuristic`, `ForwardPredictor`,
  `PopulationModel`, `MutationBudget`, level report; `SteadyStateSolver` for initialisation, fast-forward
  and predictions.

### Per-step data flow (sim thread)
Commands → mechanics → advect/diffuse (dt/2) → local kinetics (dt) → diffuse/advect (dt/2) → damage and
alerts → fitness integrals and probes → clock tick (level-window end → level report) → publish snapshot.

## Stage-1 milestones
| # | Milestone | Verification |
|---|---|---|
| M3 | Upstream homogenisation checks; fermenter cell as bond graph (FV reference + lumped game version) with η(Φ); Voronoi mesh; paint/erase, depth and level selection (cell/tissue); native transport on the cell network; shader field rendering (Delaunay interpolation + within-cell profile); flux arrows; benchmark | Homogenisation, mass, energy, second-law checks green upstream incl. size sweep; analytic diffusion + conservation 1e-12; level interfaces conserve mass; shader profile mean equals the cell value; perf baseline |
| M4 | Full stage-1 species + temperature + Q10; stiff/implicit kinetics for fast-forward; all overlays | Carbon/adenine/phosphate and enthalpy balances close; dissipation ≥ 0; speeds to ~10⁵× stable |
| M5 | Damage, necrosis, Damage tab, warnings; save/load v1 | Starvation timeline matches the golden; bit-identical save round trip |
| M6 | Cell palette; build menu; educational inspector | Module goldens; inspector clamps to tested ranges |
| M7 | Evolutionary time bar + level windows (days/weeks); heuristics; population model; mutation budget; level report | Heuristic unit tests; ledger invariants; level window reproducible |
| M8 | ForwardPredictor (famine, tolerance) | Deterministic; frame time within 5% |
| M9 | Planet/regions per epoch (−2.0 Ga onward), colonisation, membrane-respiration trait and O₂ events; save v2 | Headless scenario tests; migration test |
| M10 | Performance and polish | 5k cells + 256² at 60 fps at top speed |

## Risks
- Stiffness and fast-forward to 10⁵–10⁶×: implicit transport, implicit or QSSA kinetics, steady-state solver.
- libcellml generator edge cases (pin < 0.7); port-semantics drift from libcuflynx (parity goldens).
- Unit mismatches between modules (build-time gate); Mono/IL2CPP performance (benchmark in the player).
- Multi-species wiring upstream: one port carrying every species with per-variable `multi_port`.
- Upstream-first is slower solo: temporary pins to feature branches allowed; releases need reviewed modules.
- cam_testing's stability test: libcuflynx's `solve_ivp` route disagrees with CVODE by 0.77 (normalised) for
  `energy_metabolism`, while an independent libcellml Python + scipy run matches CVODE to 5e-9. Likely the
  route ignores the test's run_parameters; investigate in libcuflynx before relying on that route.

## Decision log
- **2026-10-06** Unity 6 + engine-agnostic C# core; 2.5D Voronoi mesh; quantitative SI physiology from
  upstream CellML; ONI-style UX; fitness heuristics with global weights; population-based mutation budget.
- **2026-10-06** Build-time C# kernels; native transport + CellML kinetics; submodule; upstream-first.
- **2026-10-06** Fitness ∈ [0,1]; new cells at baseline steady state.
- **2026-10-06** In-plane exchange only (no into/out-of-screen diffusion).
- **2026-10-06** Three levels of detail, player-chosen per region; homogenisation checks upstream
  (1% steady, 5% transient, sweeps, mass, energy, second law); heat = enthalpy release (ADR 0006).
- **2026-10-07** Starting cell: ~2 µm fermenter without mitochondria; membrane respiration evolvable;
  mitochondria a later milestone; metabolic rate from Lynch & Marinov 2015.
- **2026-10-07** Cell models as bond graphs for conservation laws, with the most widely accepted rate law
  per process (or a repo module); diffusion and lumped versions built from the same modules.
- **2026-10-07** Evolutionary time bar from −2.0 Ga to the present and beyond, discrete jumps per level,
  each level simulated in real time or faster for a few days/weeks of that period. This replaces the
  earlier "EpochSteadyState over months–years" mode as the way time advances; the steady-state solver is
  kept for initialisation and fast-forward. *(Confirm.)*

- **2026-10-07** Lumped cells in the game (levels 1 and 2 only), within-cell gradients drawn by shader
  from the analytic profile; level-0 finite volumes kept upstream as the validation reference; giant cells
  corrected by the effectiveness factor η(Φ).

- **2026-10-07** Every module must be verified and ideally validated against data; unvalidated modules
  are flagged "verified, not validated" in the game and tracked in this plan.
- **2026-10-08** Proteins are unlocked by evolutionary time from
  [`content/protein_timeline.json`](content/protein_timeline.json) (unlock = the conservative, latest
  published date; family-level presence counts). The 2.0 Ga fermenter's Na⁺/K⁺-ATPase is a eukaryotic
  protein and an anachronism; its MCT1-based lactate carrier is an animal stand-in.
- **2026-10-08** The fermenter uses a prokaryotic ion set with protons and pH: an F1Fo H⁺-ATPase as its only
  primary pump, NhaA, Trk and KefC, buffered cytosolic H⁺, resting at −105 mV and pH 7.6. Ocean pH is an
  environment parameter per epoch (7.3 at 2 Ga, placeholder): above ~7.8 the cell cannot keep Na⁺ out
  without a primary Na⁺ pump, an evolvable trait.

## Open questions
1. Is an erased cell's content released to its neighbours (conserved) or lost?
2. Reproductive output / PopulationModel equation and the competitor tables per region and epoch.
3. Level sequence: which epochs (dates) are the levels, and what each changes in the environment.
4. Length of each level's window (days or weeks), and whether it includes day/night cycles from the start.
5. Homogenisation rules when an element = N cells.
6. Modding interpreter/JIT and sandboxing; distribution platform.
7. Placeholder parameters still needing sources (listed on the review page and in module CSVs).
8. Unfilled copyright line in `circulatory-autogen-modules/LICENSE`, and the NOTICE text.
9. Undated and placeholder entries in the protein timeline still need sources.
