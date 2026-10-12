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
| M3 Cell network, lumped cells, shaders | In progress | Fermenter cell verified upstream and running in the game as one assembled kernel, with traits and a cell panel; homogenisation comparison test, cell network, shaders still to do |
| M4–M10 | Not started | |

Upstream work lives on branch `homogenisation-checks` of `circulatory-autogen-modules`, checked out as a
worktree at `~/Documents/git_projects/circulatory-autogen-modules-homogenisation` (uncommitted, awaiting
review). Review page for the first cell modules: https://claude.ai/artifact/UPtUGRQfVaSEzzDa2UXRMx

## Next: consolidate before new features (designer, 2026-10-12)
After the prey, engulfing and multicellularity/mitochondria features land and the designer has tested them in Unity,
no new features until:
1. **Solidify:** fix what testing finds; retire stand-ins (content/upstream copies once upstream is pushed).
2. **Check every module:** each upstream module passes cam_testing's full set (every test under 10 minutes); each
   game-side native element has its analytic and conservation tests; goldens current; every value without a checked
   source is in docs/review/unreferenced-values.md, to go through with the designer.
3. **Update the plans:** this file, docs/plans/*, and the milestone table, so they hold only what is still to do.

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
- **Levels and features** are listed in [`content/timeline.json`](content/timeline.json) (draft): level 1 (2.0 Ga)
  the fermenter with expression, repair and one flagellum that runs at random; level 2 (1.9 Ga) growth, division,
  glycogen, prey and engulfing, the choice between multicellularity and mitochondria, and processing to decide when to
  swim (planned); level 3 (1.85 Ga) smell (chemoreceptors), to swim towards
  food (bacterial chemotaxis predates 2 Ga; placed here for pacing); level 4 respiration; level 5 the eukaryotic cell (cytoskeleton; mitochondria are the level-2 path); level 6 several cell groups;
  level 7 animals with an optional fixed body size. Every
  feature, once unlocked, is the player's choice to use; some (fixed size) make the organism easier to maintain.
  The same file lists when each protein existed.
- Geology for the first levels (to be detailed in content): O₂ appeared with cyanobacteria and the Great
  Oxidation Event ~2.33 Ga, but at −2.0 Ga much of the ocean was still anoxic, so early regions differ
  (oxygenated surface oases vs anoxic deep water).

## Game design
| Area | Decision |
|---|---|
| Starting organism | A **~2 µm prokaryote-grade fermenter** (homolactic: glucose → 2 lactate + 2 ATP), no mitochondria. **Membrane respiration** is an evolvable trait; O₂ arriving is a planet event; **mitochondria (endosymbiosis)** lift the membrane-area energy limit (Lane & Martin 2010; disputed by Lynch & Marinov 2017); at level 2 they are one of two exclusive paths (the other multicellularity), available after swallowing 10 prey cells (docs/plans/prey-and-branches.md; their physiology is planned). |
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

### Organelles visible in the cell (designer, 2026-10-13; not started)
Once organelles are in the cell model (mitochondria first, from the level-2 branch; later others such as a nucleus,
vacuoles beyond today's food vacuole, and a cytoskeleton), the player sees them inside each cell in Unity: drawn
from the model's own state (count, size or volume share, activity), not decoration, and moving or changing with it
(e.g. mitochondria multiplying with their protein pool, dimming as respiration slows). Each organelle gets its overlay
or detail rows in the cell panel, like the membrane carriers' arcs today.

### Environment, threats and behaviour (user notes, 2026-10-09; not started)
- **Visible motion** (done 2026-10-09). The creature drifts with a slow near-bed current
  (`environment.current_*`, 2 µm/s placeholder) and swims on its flagella (`Sim.World.CreatureMotion`). The view follows it
  (a floating origin: every position in a frame is relative to the creature), so the creature stays put on screen and
  the world slides past:
  - world-fixed sediment specks, plus a fainter deeper layer at lower parallax (`BackgroundView`);
  - a fading wake through the path it has travelled and a velocity arrow (`MotionView`); speed in the top bar;
  - food lying on the ground streams past; food carried by another creature moves on its own path and shows its
    carrier as a dark body.
  Cells grow visibly as they build a daughter (drawn up to double area; the kernel keeps the starting geometry) and draw
  their flagella, beating while the motors run. Still to add: landmarks (dark patches and occluders, which hiding
  needs) and a minimap (with memory, later).
- **Swimming (level 1, 2026-10-09).** Propulsion comes only from flagella. The cell starts with one; a whole-number
  trait (1–8, `pool_Flagellum`) adds more, each costing its ~1×10⁷ amino acids to build (~3×10⁸ ATP, needs food).
  The motor is CellML (`cell/ion_channels/i_H_flagellum` MotAB): rotation proportional to the proton motive force
  (~100 Hz at rest), ~1,200 H⁺ per turn (Meister et al. 1987), swimming ~15 µm/s with one flagellum, saturating with
  more. At level 1 the motors run at random (runs ~2 s, pauses ~4 s; game rule, `motility.*`); level 2 adds
  processing to decide when to run (`motility_control`, planned). Running one flagellum costs ~1.2×10⁵ H⁺/s, which the
  H⁺-ATPase must pump back out.
- **The creature is one rigid body in 2D** (2026-10-09, ADR 0008). Each flagellum sits at a fixed angle on its cell's
  membrane (the player drags it to move it; daughters inherit the places) and pushes straight away from its base with
  the kernel's thrust (`i_flagellum` F_one), applied at its base, off by a normal random angle per run (σ = 10°,
  `motility.thrust_angle_sd`), so the body turns and runs curve. Thrust rises and falls with the motors and bundle
  (`tau_bundle`, 0.3 s placeholder, in CellML), since a cell this small has almost no inertia (it stops within ~0.3 µs
  of the thrust ending); Unity draws the body's pose carried forward between the simulation's 0.1 s snapshots, so
  motion is smooth. A test checks one cell slows to a stop after one push (exactly, and through the real kernel). Newton's laws for translation and rotation against Stokes drag (6πηR per cell, 8πηR³ plus
  6πηR r² for rotation; no hydrodynamic interaction between cells), integrated exactly per step: momentum and energy
  balance to round-off. The motors' proton-motive power far exceeds the thrust's mechanical power (~0.2%, as flagella
  are ~0.1–2% efficient); the rest, and all the drag's work, is heat in the water. Food a cell finds moves rigidly with
  the body.
- **Membrane proteins are drawn** as arcs of each cell's rim in their legend colour, each as long as the share of the
  surface its copies take, so expressing more of a protein visibly takes more membrane (capped by the area limit).
- **Enemies and allies** share the environment with the creature.
  - **Enemies** attack and cause damage *where they hit* (local damage on the struck cells, not a whole-cell average).
  - **Allies** give food packets.
  - **Telling them apart gets harder over time.** Early on allies and enemies are easy to detect; in later levels
    they look and smell alike, and telling them apart needs neural development (sensing cells, then integrating
    cells/a nervous system). Friends and enemies will sometimes look like the player's own creature.
  - **Dead enemies and dead prey turn into food** (a food packet, or a richer carcass, where they died).
- **Responses to an enemy**, each a capability the creature must have:
  - **Run**: needs muscle (contractile) cells whose usage can be raised, costing ATP.
  - **Hide**: in dark areas, or out of the enemy's line of sight or smell (so enemies sense by sight and/or smell, and
    the environment has light/dark regions and occluders).
  - **Protect**: develop a protective shell.
  - **Fight back**: produce something sharp that damages the enemy, either on the outside (a claw, spine) or released
    (a deterrent, like a skunk's smell, then poison). Contact damage is multiplied by the relative velocity of the hit.
- **Proteins unlocked by experience**, not only by evolutionary time: an activity repeated and survived unlocks the
  protein for the matching defence, e.g.
  - hit by enemies a few times and survived → shell proteins (secrete a shell, or turn cells into shell-like material);
  - hidden successfully a few times → pigment proteins to choose the creature's colour, and later to change colour
    quickly (camouflage).
  - hit by another creature → a protein for producing something sharp (outer, e.g. a claw; or released, e.g. a
    deterrent smell);
  - scared off enough enemies with a released substance → poison proteins;
  - killed more enemies with sharpness → successively sharper proteins that do more damage;
  - e.g. hit by 10 enemies → a protein for protective connective tissue.
  Activity counters per creature go in the save.
- **`content/timeline.json` carries both** when each protein is *potentially available* (its existing
  `available_from_ga`, the historical date) and an **unlock criterion** saying how it is actually unlocked, e.g.
  `"unlock": {"activity": "hit_by_enemy", "count": 10}` for protective connective tissue; proteins with no criterion
  unlock with their date as now. Activity counts always accumulate, whatever the date (user, 2026-10-09). Dates and
  the levels for features such as memory and communication are decided level by level as each is fleshed out; until
  then the file only needs an approximate order of availability. The game checks the criteria against the activity counters, and a test keeps every
  criterion's activity in the list the game records.
- **Communication and memory (later levels).** The creature evolves communication with creatures like itself, signals
  that help with the environment: "danger", "food", "store food". It also eventually evolves a memory (e.g. of where
  food and danger were, and of which look-alikes were friends).

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
- **Energy budget (ATP is the currency).** Costs and the budget are in mol ATP/s (shown as ATP/s): biochemistry is
  stoichiometric in ATP and the cost literature is in ATP equivalents. An ATP is not a fixed energy (its free energy,
  48.6 kJ/mol at rest, moves with ATP/ADP/Pi), so the panel also shows the free-energy use (ATP flux × ATP free
  energy, W) and the heat (enthalpy, W). Glycolysis makes the ATP (and glycogen, 3 per glucose unit); it is spent on:
  | Use | Model | At rest |
  |---|---|---|
  | H⁺-ATPase pumping | `i_H_ATPase` | 25% |
  | Protein turnover, all proteins incl. ribosomal/nucleoid ones | a `protein_pool` per protein: copies × length × 5.5 ~P/aa × 1%/h | 16% |
  | mRNA turnover (half-life ~5 min) and ribosome upkeep | `biosynthesis` | 7% |
  | Membrane lipids, scaling with membrane area | `biosynthesis`: 2 × surface / 0.5 nm² lipids × 100 ~P × 1%/h | 4% |
  | Repair, at the group's repair level | `biosynthesis`: damage 1 = lethal, 10 days unrepaired | 4% |
  | Other maintenance (not yet itemised; Lynch & Marinov total less the above) | `energy_metabolism` ATPase | 44% |
  | Building new protein when expression rises | `protein_pool` J_build, served by `protein_expression` | 0 |
  | Growth: building a daughter (1.8×10¹¹ ATP; Lynch & Marinov 1.6×10¹¹) | `biosynthesis` J_growth, P_growth | 0 |
  | Storing glycogen (2 ATP per glucose, 3 back) | `energy_metabolism` | 0 |

  Every ATP hydrolysed releases one H⁺ (accounted in the cytosol). Synthesis slows as ATP runs out, so famine runs
  ATP to zero, never below. Keeping a transporter is cheap, building it is not (all 2,850 H⁺-ATPases: ~200 ATP/s to
  keep, ~4×10⁸ ATP to build); a daughter's proteins are 90% of its cost. Glycogen at its ceiling (10% of dry weight)
  holds ~1% of a daughter: growth is the real store towards division, glycogen tops it up and buffers famine.
- **Energy uses still to add:** motility (flagella on the proton motive force), osmoregulation (compatible solutes),
  stress proteins (chaperones; heat damage), sensing and signalling, DNA replication timing within growth, and damage
  effects short of death (damage currently only kills at a lethal load).
- **Traits and controls (from the cell panel, per cell group):** protein expression ×0.25–4 for the 7 transporters
  and the glycolytic enzymes ([`content/traits/fermenter.json`](content/traits/fermenter.json)): each trait is the
  target of a `protein_pool` in the kernel, whose expression approaches it with a 10 s time constant; building is
  limited to 5× the resting ATP use and, like growth, throttled as ATP falls below rest (stopping at 4 mM, the
  stringent response), all in CellML, so large changes take minutes. Membrane proteins take shares of a ceiling of 15%
  of the surface that scales with membrane area (`s_target`, a share of it): at rest they use 36.8% of it (5.5% of the
  surface), the surface hydrolase and the flagellum included; a multiplier sets the share to that multiple of the
  starting share. The carriers bought as skills (PTS, ABC, H+ symport) start at 0 and their sliders set a share
  directly, from 0 to the room left. The game keeps the shares' sum within the ceiling. Kinetic midpoints are
  fixed (±10% changed almost nothing). Energy controls: growth (0–20× resting ATP use), glycogen storage (0–1), repair
  (0–1; below ~0.1 damage outruns repair). The whole-cell upstream sweep covers every trait and control over its
  range, temperature 0–40 °C and glucose 0–2 mM.
- **Where values live.** Every physiological parameter is in upstream
  `modules/cell/generic_adaptive_cell/parameters.json` with its source and status, read by `tools/build_adaptive_cell.py`
  (which reports any CSV value that is neither from the file nor derived from it by a stated rule); the game reads
  them from the kernel. Game-side values (food, colony, control ranges, display) are in
  [`content/parameters.json`](content/parameters.json), also sourced, embedded in `Game.Content`. Cells are versions
  of `cell/generic_adaptive_cell`: `empty_membrane` (every general mechanism) and `fermenter` (extends it with its
  proteins); later bases (an animal cell) extend `empty_membrane` the same way. See
  [`docs/plans/generic-adaptive-cell.md`](docs/plans/generic-adaptive-cell.md).
- **Food packets.** Dead cells or organic particles, smaller than a cell (0.8 µm radius fresh), at a cell's density of
  glucose equivalents (2.3 M: 1.1 g/mL × 30% dry × 50% carbon, 6 C per glucose; a sphere of its radius, 4.9×10⁻¹⁵ mol,
  about 3% of a daughter's glucose), with traces of other species (aspartate, serine, ribose, dimethyl sulfide);
  a packet keeps its density as it is eaten (radius as the cube root of what is left) and is removed below 1%. Its free
  sugar leaks into the environment field; a touching cell's surface hydrolase cuts its polymer into that free sugar (held
  at 50 mM), and the cap of the cell's membrane facing the packet sees it and takes it up through the cell's own carriers
  (Glf, and the PTS, ABC and H+ symport once bought); the packet then travels with the creature
  (docs/plans/environment-field.md). Traces leak with the sugar into the field, for smell (level 3); nothing takes them
  up yet.
  Since 2026-10-09 a packet either **lies on the ground** (still; a dead creature will be one too) or is **carried by
  another creature** (prey, or an ally bringing food); nothing drifts freely. Lying food has a density per area
  (`food.lying_density`) and comes into view ahead of the moving creature; carriers arrive at random times. The food
  window (30 µm) follows the creature, and food a cell touches travels with the creature until it is eaten.
- **Division, groups and fixed size.** A cell divides when its daughter is built (or glycogen can finish it), by
  clicking Divide or automatically; both halves keep the state, traits and controls. Cells belong to groups; sliders
  change every cell in a group (organs later use several groups); a group has no maximum size. Cells share the medium
  but do not yet exchange through the transport graph.
- **Fixed body size (a later level; to rework, user 2026-10-09).** The cell-type proportions within a volume of
  tissue become fixed and replication towards more cells is turned off; growth energy then goes to replacing damaged
  and dead cells (turnover). Growth and repair are both set as **ATP usage** (mol ATP/s, in multiples of the resting
  turnover), so energy moves from one to the other directly. Today growth is ATP (growth_level × J_ref) but repair is a
  0–1 level scaling a capacity; `biosynthesis` should take repair as an ATP rate too (repair rate = ATP / cost per
  unit of damage, limited by the damage present), and the C# rule that adds the growth share onto the repair level
  (with a per-group cell count) goes.
- **Diffusion (reference) vs lumped (game) version:** the same bond-graph modules build both: N rings of
  `cytosol_volume` joined by `cytosol_face` (the upstream reference) and one `cytosol_volume` (the lumped
  bond graph the game runs). Their comparison is the level 0 ↔ 1 homogenisation check, with a cell-size
  sweep from 2 µm (gradients negligible) to giant cells (where η(Φ) takes over).

## Descriptions and educational content (human review)
Every text the player reads that explains something (features' effects and explanations, level summaries, protein and
event notes, tooltips that teach) must be **reviewed, corrected where needed, and ticked off by a human**:
- Each such text is stored in content with a `reviewed` flag, `false` when written or changed (by Claude or anyone).
- A human reads it, edits it if needed and sets `reviewed: true`. Claude never sets the flag.
- Changing a reviewed text sets its flag back to `false`.
- Tests fail while any text is unreviewed (`tests/AdaptOrDeity.Game.Session.Tests/ContentReviewTests.cs`, for
  content/timeline.json features and levels). New kinds of educational content get a flag and a test when added.

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
| fermenter cell (`cell/generic_adaptive_cell/fermenter` and its new submodules) | Maintenance ATP vs cell volume (Lynch & Marinov 2015); lactate production of homolactic bacteria; bacterial membrane potential and K⁺/Na⁺ contents vs medium |
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
- libcellml generator edge cases (our combined 0.7.1 build; libcuflynx #572/#520/#574 unmerged); port-semantics drift
  from libcuflynx (parity goldens).
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
  [`content/timeline.json`](content/timeline.json) (unlock = the conservative, latest
  published date; family-level presence counts). The 2.0 Ga fermenter's Na⁺/K⁺-ATPase is a eukaryotic
  protein and an anachronism; its MCT1-based lactate carrier is an animal stand-in.
- **2026-10-08** The fermenter uses a prokaryotic ion set with protons and pH: an F1Fo H⁺-ATPase as its only
  primary pump, NhaA, Trk and KefC, buffered cytosolic H⁺, resting at −105 mV and pH 7.6. Ocean pH is an
  environment parameter per epoch (7.3 at 2 Ga, placeholder): above ~7.8 the cell cannot keep Na⁺ out
  without a primary Na⁺ pump, an evolvable trait.
- **2026-10-08** Protein expression costs ATP (turnover always, construction when expression rises), modelled
  upstream in `cell/protein_expression`; the player adapts expression (×0.25–4) and kinetic midpoints (±10%) in a
  right-hand cell panel that shows the metabolic rate and ATP budget. Whole cells are assembled at build time into one
  kernel stepped by backward Euler (ADR 0007).
- **2026-10-09** `content/timeline.json` gives each protein both its potential availability (historical date) and
  an unlock criterion (an activity count, e.g. hit by 10 enemies → protective connective tissue). Defences grow from
  use: sharp outer or released weapons after being hit, poison after deterring enough enemies with a release, sharper
  weapons with more kills; damage scales with velocity; the dead become food. Look-alike friends and foes;
  communication ("danger", "food", "store food") and memory come at later levels.
- **2026-10-09** The environment has moving food, enemies (local damage where they strike) and allies (give food);
  telling them apart becomes harder by level and needs neural development. Defences are running (muscle cells),
  hiding (darkness, out of sight or smell) and shells. Some proteins unlock through experience (surviving hits →
  shell; hiding → colour choice, then fast colour change), not only by evolutionary time.
- **2026-10-08** No physiological value is hardcoded: upstream `parameters.json` for the cell modules (the builder
  reads it and generates the CSVs) and game `content/parameters.json` for game-side values, each entry with its source.
  Math that can be CellML is CellML: protein expression, build capacity and the ATP throttle (`protein_pool`,
  `protein_expression`), growth throttling and the daughter's cost (`biosynthesis`), the ATP budget by reaction
  (`energy_metabolism`). Cells are a hierarchy of supermodules, `cell/generic_adaptive_cell`: `empty_membrane` as the
  base, a supermodule per cell type, and later bases with the proteins a class shares (an animal cell).
- **2026-10-11** The player's creature is a module array (`<prefix>_module_array.json` + `_parameters.csv`, the format
  libcuflynx, PhLynx and cam_testing share): each living cell an instance of the assembled cell in `content/assembled`
  (`cell_<i>`), with the parameters the player set. `content/assembled` is a module library in the upstream layout
  (CellML 1.1, units in their own file), so those tools build models from it. `tools/interop` tests that libcuflynx,
  libcellml and PhLynx accept what the game writes. Saves will store the creature this way.
- **2026-10-11** Kernels stay generated by our own emitter (`aod_kernelgen`) from libcellml's analysed equations, not
  by libcellml's code generator or circulatory_autogen's C++ templates (#520): our batched, stateless kernels with
  external arrays don't fit their single-model layout, and the planned exact sparse Jacobians come from the same
  equations. libcellml (our combined build) parses, validates and analyses; libcuflynx assembles supermodules. Later:
  read module configs through libcuflynx's loader once its instance work is released.
- **2026-10-11** Platforms: desktop only (Windows, macOS, Linux). Engine: stay on Unity for drawing and UI; the
  simulation is to become a NativeAOT-compiled .NET library loaded by Unity (modern runtime and SIMD), keeping the
  managed build as a fallback. GPU: drawing and the environment field; cell kinetics on it only behind the gates in
  docs/plans/parallel-and-gpu.md (no doubles on macOS GPUs). Next: lean kernel layout and parallel cells (steps 1-3).
- **2026-10-11** Parallel cells (docs/plans/parallel-and-gpu.md): batched multi-threaded cells first, then the
  analytic Jacobian, then the NativeAOT library prototype. A batch is one of the player's cell groups (trait targets
  and controls stay per cell). Results stay bit-identical at any thread count (ADR 0005). Environment: 8+ chemicals,
  so the field goes on the GPU, four species per pass, with implicit (L-stable) steps tied to the cell exchange, which
  also allow obstacles and variable diffusivity; an exact spectral step only as a later optimisation for open water.
  Top speed: cells at steady state sleep, and tissue is homogenised (ADR 0006); no clone sharing and no quasi-steady
  field. Food packets become spreading Gaussians (closed form), not grid features.
- **2026-10-11** Scale (docs/plans/parallel-and-gpu.md, "Scaling to 5,000 cells"): no cap on cells. Cells in a group
  differ (food and resources reach each differently); representative volume elements for groups come later. Frames
  carry details for the inspected cell only, and cell positions on the body (the front end turns them with the body).
  Target: 500x real time, as far as it can be reached. Sleeping steady cells is optional (FermenterCell.SleepTolerance,
  error measured: 0.3% at 1e-3), off by default; growing, swimming cells never qualify. Code generation may move to
  libcuflynx (#573): analysis and differentiation shared, thin C and C# emitters.
- **2026-10-12** Cells move to libcuflynx's generated C and its `BE_batched_c` runtime (docs/plans/libcuflynx-solver.md),
  replacing the game's C# kernels and integrator: ~19x faster per thread on the colony protocol and more accurate.
- **2026-10-12** The C# simulation path is retired: every module runs from one library of libcuflynx's generated C
  (`aod_modules`), which every build and test now needs (CMake and a C compiler; CI builds it on all platforms).

- **2026-10-12** Food arrives at a constant rate per level and region (content/timeline.json `arrivals`), whatever
  the field holds; packet slots are unlimited. Sublevels (scarcity, abundance, stresses) to design. Cells at 2 Ga
  absorb food (external digestion), they do not engulf (docs/research/food-uptake-and-extracellular-space.md); food
  packets become small polymer particles (docs/plans/environment-field.md).
- **2026-10-12** Skill points and the skill window: food eaten earns points (content parameters
  `skills.glucose_per_point`); features with a cost are bought with them once their level is reached, in a window that
  opens when the level goes up. The first are the sugar transporters (PTS, ABC, H+ symport) after level 1, priced so a
  normal level 1 buys one and a very good one two. Their CellML modules are drafted upstream (branch
  feat/sugar-transporters of circulatory-autogen-modules) and are not yet wired to the features.
- **2026-10-12** Descriptions and educational content are reviewed by a human (section above): `reviewed` flags in
  content and tests that fail until each is ticked off.

- **2026-10-12** The game keeps libcuflynx's BE_batched_c: inputs will change every macro step (food from the field),
  where CVODES would restart each step and fall 5-6x behind; at equal accuracy and steady inputs the two are level
  (docs/plans/libcuflynx-solver.md). libcuflynx gets a multithreaded CVODES C option for other users; reusing the
  Newton matrix factorisation in BE_batched_c is being measured.

- **2026-10-12** Designer decisions (docs/plans/environment-field.md): (1) the PTS delivers glucose 6-phosphate:
  glycolysis gets a G6P pool (hexokinase step + the rest) upstream; (2) carrier expression is a share of the membrane's
  transporter ceiling; (3) Glf leaking glucose out is shown with a hint; regulation (catabolite repression) becomes a
  skill to buy; (4) level 1 lasts one simulated week (enough to start starving), skill prices calibrated by bots;
  (5) the field is nested grids: a fine grid around the colony inside coarser ones out to 60x its diameter, with a
  per-cell diffusion-limit correction; (6) level 4 brings extracellular space now: boundary and inner cell groups,
  the space between cells as volumes (each a module), and a researched cost of making the matrix.
- **2026-10-12** Level 4's space between cells (docs/plans/level4-extracellular-space.md):
  - Volumes are the lattice (Delaunay) triangles of the cell centres. Each volume is an instance of upstream
    `extracellular_volume`, joined through the cell-cell gaps by the `extracellular_face` law. The rim gaps open to the
    sea.
  - Cells see and feed their six 60° sectors.
  - Transport is the game's backward Euler step, with exact bookkeeping. The matrix chemistry runs in the modules.
  - The automatic groups are boundary and inner.
  - Matrix costs 1 glucose + 3 ATP per glucose unit (pathway accounting, 2-4), with an allocation slider per group.
  - The upstream modules sit on branch feat/extracellular-matrix, merged into homogenisation-checks; copied into
    `content/upstream/` until that branch is pushed.

- **2026-10-12** The environment field is built (docs/plans/environment-field.md "Built"), off by default
  (content `field.enabled`): nested grids stepped coarse first with Douglas ADI (exact steady state at the exchange
  step), finer grids taking the coarse flux through their edges (conservative by construction; explicit refluxing was
  unstable), pH from buffered acid, packets dissolving over `food.packet_lifetime`, cell inputs passed on a 1% change.

- **2026-10-12** The game's cell is upstream homogenisation-checks a12d84a (feat/sugar-transporters and
  feat/extracellular-matrix merged): glycolysis split at a 1 mM G6P pool, the PTS delivering G6P, the ABC and H+ symport
  carriers, membrane-share protein pools (the flagellum's included) and the surface hydrolase.
  - Membrane traits set `s_target`: the expression sliders stay multipliers (×0.25–4) of the starting share, so play and
    saves keep their meaning; the PTS, ABC and H+ symport carriers become share sliders (0 to the room left) once their
    skill is bought. Every membrane pool counts towards the ceiling (shares sum ≤ 1).
  - The medium glucose goes to all four carriers; uptake, food eaten, the field exchange and the space between cells use
    their sum (the PTS's glucose arrives as G6P); the budget lists the carriers' ATP (a PEP per PTS glucose, 2 ATP per
    ABC glucose). The Glf leak hint reads Glf's own flux.
  - With the field on, packets are digested by touching cells' hydrolase, V_max S/(K_m_poly + S), S the packet's polymer
    over a disc of its radius as deep as the cells (always saturated at the placeholder packet density).
  - Level 4's matrix control sets `mx_alloc` on this cell (matrix now made from G6P); the extracellular modules stay
    copied in `content/upstream/` (identical to upstream) until homogenisation-checks is pushed and the submodule follows.
  - Level 1 re-measured (contact rule, field off): left alone a cell eats 1.55e-13 mol (7 points, one transporter) and
    starves from day 4 (damage 0.35 by day 7); kept on food 2.98e-13 mol (14 points, two). No content value changed.
  - The famine golden moved by design (realistic G6P pool): against the old one V NRMSE 0.12, ATP 0.05, Na+ 0.03.

- **2026-10-12** Prey, engulfing and the level-2 branch (designer requests; docs/plans/prey-and-branches.md):
  - Packets have a cell's density: 2.3 M glucose equivalents (1.1 g/mL × 30% dry × 50% carbon / (12 g/mol × 6)), a sphere of
    the 0.8 µm radius: 4.9e-15 mol (was 4e-14). They keep their density as they are eaten (radius as the cube root of what
    is left). The hydrolase's substrate is the packet's polymer over its sphere (~2.2 M): saturated, surface-limited.
  - The surface hydrolase is a trait (×0.25–4 of its membrane share; 700 aa, now sourced from UniProt SusG 692 aa, upstream
    0db734f, values unchanged). It acts in the environment field and in vacuoles; the contact rule ignores it (open question).
  - Level 2 brings prey (0.5 µm cells at the packet density, run and tumble at 3 µm/s, `prey_per_s` 2e-4) and engulfing
    (free, optional): a touching prey cell or packet smaller than the cell goes into the cell's one vacuole (it must stay
    smaller than the cell; packets leave room for a prey cell). The vacuole is outside the cytosol: the cell's hydrolase
    digests it (held back while its free sugar is at the 50 mM a packet offers, product inhibition), and the cell's
    carriers take the sugar up on the share of membrane lining it, (r_v/R)². Engulfing is not documented this early (a
    eukaryotic trait); the game lets the player explore the phagocytosis-early route, and its text says so.
  - Multicellularity (5 points) and mitochondria (5 points, after 10 prey swallowed) are exclusive paths at level 2.
    Without multicellularity a daughter leaves at division and earns reproduction points (`skills.points_per_offspring`
    5); later colony features require multicellularity. Mitochondria's physiology is planned (buying them changes nothing
    in the cell yet).
  - Calibration (field off, contact rule; content notes have the details): food arrivals 3e-3 packets/s (was 1e-4) and
    lying food 32 per view (was 4), as packets hold ~8x less; `skills.glucose_per_point` stays 2e-14 mol. Left alone at
    level 1 a cell eats 1.69e-13 mol (8 points), its ATP runs out on day 4 (damage 0.32 by day 7); kept on food 2.98e-13
    mol (14 points). Level 2, prey 2e-4/s: the untouched creature, starving since level 1, swallows 4 and dies at the end of
    the level; a fed cell starting level 2 swallows 7 (11 and 6 with 3 flagella in probes with other random sequences). A
    cell kept on food at growth 3 left 5 daughters in the week (10 reproduction points at 2, now 25 at 5); at the level's
    own food, growth 2-10 runs ATP out within a day and no daughter is made.
  - A cell with a vacuole has inputs that change every macro step: it uses the soft restarts (StopAtMacroSteps) the
    field's cells use. With hard restarts the integrator failed a few seconds after a division (deterministically, at any
    glucose).
- **2026-10-12** Released daughters live on (designer request; docs/plans/free-daughters.md): without multicellularity
  a daughter becomes a free organism in the same water, its cell exactly as divided and its own one-cell body that runs
  and tumbles by the creature's rules with its own random numbers. It keeps its controls (nobody controls it), eats and
  engulfs by the creature's rules in competition with it (packets withdrawn in turn: the creature's cells, then
  organisms by id), exchanges with the field where the field covers it, divides by itself if released with growth and
  automatic division on (its daughters earn the player nothing), dies at a lethal damage load and is removed 600 s
  later. At most 32 are simulated, none beyond 2 view widths (60 µm) from the creature; a calm organism costs ~30 µs and
  a swimming one ~80 µs per simulated second, and swimmers leave the range within a minute or two.
- **2026-10-13** Regulation by falling metabolites (designer request; docs/plans/regulation.md). Upstream
  cell/expression_regulation (homogenisation-checks ddb5c6e): every protein pool's target and growth times
  prod_s (1 - on_s + on_s f_s), f = x^4/(1 + x^4), x = C/(half the resting level) for ATP, G6P and glucose inside, x =
  (twice the resting H+)/H+ for pH; switches off by default, the cell bitwise unchanged (goldens identical). In the game a
  free level-1 feature unlocked when a cell's ATP first falls below half its resting level; per group, a cog beside each
  expression slider and the growth slider opens the species toggles. Measured: on half food regulating the pumps,
  exchangers, flagellum and hydrolase by ATP avoids the ATP trap (ATP 4.2 mM, back to rest when food returns; unregulated
  ATP runs out for good); with no food it only delays the collapse, and at level 1 as tuned the unlocking dip is the
  crash itself.
- **2026-10-13** Dead organisms are food and the lineage earns points (designer requests; docs/plans/free-daughters.md
  "Dead bodies"): a free organism that dies becomes a food packet where it lies (a sphere of its cell at the packet
  density, 9.6e-15 mol, with the packets' free share, plus its vacuole's content; its simulated pools are not added, as
  the density counts all of a cell's carbon), drifting with its water, eaten by the packets' rules (contact by the
  creature and organisms; engulfed only once eaten smaller than the cell), kept until eaten while within the organisms'
  range; `organisms.dead_kept` is retired. Every daughter released in the lineage (any generation) counts as offspring
  and earns `skills.points_per_offspring`; frames split the creature's own (`OwnOffspring`).

- **2026-10-13** The environment field is always on (designer: "start the process to make field always on";
  docs/plans/environment-field.md):
  - It meets its reference (one fine grid over the same extent, 64 steps, every input passed) within 0.6% for one cell
    and a 7-cell colony; tests bound it at 2%. Fixes: the outermost grid spans 60 colony diameters to one of its cells
    (it reached up to 4x beyond, and the reference covered only 60x); the membrane sees the thin layer's analytic near
    field less what the grid resolves (the old J/(4 pi D a) counted the 2D field twice: 1.6-1.8x the depletion);
    glucose takes 4 fine-grid substeps per macro step (a drifting cell saw 7% too little of its own depletion); inputs
    move on 1% of their departure from the sea (a lone cell's own depletion was hidden).
  - The contact rule and `field.enabled` are gone. A touching cell's hydrolase cuts a packet's polymer into the packet's
    free sugar (held at 50 mM); the cap of membrane facing the packet (~5%) takes it up, the rest leaks into the water.
    Sugar released into open water reaches a Glf cell with ~0.05% probability: with that alone a level-1 cell starved
    within minutes.
  - Recalibrated: food 5e-3 packets/s at levels 1 and 2 (was 3e-3), `skills.glucose_per_point` 1.2e-14 mol (was 2e-14).
    Level 1 left alone: 6 points, ATP gone at day 1.7; kept on food: 26 points. Level 2 untouched: 24 points, 4 prey;
    growth 1 with automatic division: 10 prey, 1 daughter.
  - The field covers the free organisms' range; smells step every 1 s; frames carry lactate and pH, shown in Unity as
    water overlays (F7-F9).
- **2026-10-13** Realistic protein turnover and maintenance regulation (designer; upstream homogenisation-checks,
  docs/plans/regulation.md):
  - Proteins are degraded at ~1%/h and replaced up to their target; they rise with their mRNA's mean lifetime (~7 min)
    within the ribosomes' capacity (~9x the resting turnover at 20 C), so sliders take minutes to hours to act and a
    lowered target empties a pool over days.
  - Regulation is normalised: f = 1 exactly at and above the resting level, so switching a species on has no effect at
    rest.
  - The maintenance (protein synthesis, RNA and lipid upkeep, repair, the maintenance ATPase) takes regulation switches of
    its own, off by default, offered as a "Maintenance" block with cogs; starving bacteria cut maintenance far below its
    resting value (Hoehler & Jorgensen 2013). Levels are recalibrated around it in a later step.

## Open questions
1. Is an erased cell's content released to its neighbours (conserved) or lost?
2. Reproductive output / PopulationModel equation and the competitor tables per region and epoch.
3. Level sequence: the draft in `content/timeline.json` (levels 1–6) needs confirming, and what each level changes in
   the environment.
4. Length of each level's window (days or weeks), and whether it includes day/night cycles from the start.
5. Homogenisation rules when an element = N cells.
6. Modding interpreter/JIT and sandboxing; distribution platform.
7. Placeholder parameters still needing sources (listed on the review page and in module CSVs).
8. Unfilled copyright line in `circulatory-autogen-modules/LICENSE`, and the NOTICE text.
9. Undated and placeholder entries in the protein timeline still need sources.
10. Expression range (×0.25–4), the build capacity (5× resting ATP use), the build ATP floor (4 mM) and the membrane
    ceiling (15% of the surface, protein footprints) are placeholders; transporter copy numbers rest on placeholder turnover
    numbers (100–1,000/s).
12. Growth pace (recalibrated 2026-10-09): the cell rests on a lean background (0.1 mM glucose, placeholder) where
    its uptake just meets maintenance; touching food (50 mM at the membrane) its carriers give ~40× and glycolysis
    ~52× the resting flux, as growing fermenters do (10–40×). At growth 50× it builds ~41% of a daughter per simulated
    hour (~2.5 h per division, E. coli-like at 20 °C). Confirm the feel.
    Integration speed (2026-10-11, `AdaptOrDeity.Headless bench`): modified Newton (Jacobian and LU kept across
    iterations and steps, convergence on the true residual) and adaptive error-controlled steps (tolerance 1%, steps up
    to 60 s, the world reading states interpolated within a step; a changed input restarts the step). One cell, one
    core: ~350× → ~3,800× real time with the motors switching every 2–4 s, ~12,000× with steady inputs, and more
    accurate than the old fixed 0.1 s steps (1–3% vs 10–18% against a 5 ms reference). Two hours of play headless:
    11.4 s → 3.5 s. Lean kernel layout (2026-10-11): 3,845× → 6,808× busy, 12,246× → 21,405× calm, identical
    results. Next: see docs/plans/parallel-and-gpu.md (lean kernel layout, analytic Jacobian, batched
    parallel cells; the GPU for display, cell kinetics on it only as a gated experiment).
13. Damage model placeholders: 10-day unrepaired lifetime, repair capacity 10× accrual, repair costing 10% of a
    daughter per lethal load, Q10 2; damage does nothing until lethal. Daughters inherit the parent's damage.
11. libcuflynx relabels some conversion-factor literals with the inverse or a dimensionless unit when flattening
    (`m3_per_um3` → `um3_per_m3`, `milliV_per_kiloV` → `dim`); values are right, units are not. Fix upstream?
14. Thresholds for each unlock criterion ("a few times"), and whether meeting a criterion before the protein's
    date unlocks it at once or when the date arrives (counts accumulate either way). Decided per level.
15. Enemies and allies: what they are per level (other cells, then animals), how they sense the creature (smell
    range, line of sight, light), how damage is localised on a lumped cell vs a multicellular body, and what an ally's
    food gift costs or requires.
16. Motion: the creature drifts with the current before it can swim and the view follows it (2026-10-09). Should the
    current's speed and direction vary (tides, eddies), and should the player be able to free the camera?
18. Starvation is absorbing: once ATP reaches 0 the cell cannot restart glycolysis (hexokinase and the PTS need ATP or
    PEP), so a gap between food packets longer than its reserve kills it slowly even when food returns. With packets of a
    real size (2.7 h of eating each) level 1 and 2's outcomes hang on the gaps, and growth at the level's food rate runs
    ATP out within a day. Keep a small ATP floor, a restart path (glycogen, basal uptake), or richer food? (2026-10-12)
    With the field (2026-10-13) a fed cell has no reserve beyond its demand, so this decides level 1: on the same random
    draws, food at 3e-3 to 7e-3 packets/s gave 1 to 21 points, not in order of the rate.
19. In the field each packet a cell touches gets the cell's whole hydrolase rate (several touching packets are digested
    in parallel); share the enzyme over the faces? And the face share (the cap the packet covers, ~5%) is geometry with
    no source for how close a cell and a particle sit. (2026-10-13)
    Regulation (2026-10-13) avoids the trap only while some food remains (half the resting glucose): the unregulated
    maintenance ATPase still drains ATP to 0 in a full famine. Should maintenance fall in a famine too, and should
    regulation unlock before the first crash?
20. libcuflynx's runtime failed after a division with hard restarts on inputs changing every step (vacuole cells): soft
    restarts avoid it; worth a minimal reproduction upstream. (2026-10-12)
21. Free organisms (docs/plans/free-daughters.md): a dead body beyond the organisms' range (60 µm) is dropped; keep far
    bodies? And should lineage points diminish by generation, since descendants may out-earn food? (2026-10-13)
17. Communication and memory: what a signal costs, what memory stores (places, individuals, threats) and how it
    is modelled (neural cells, a game abstraction, or both). Levels and dates decided when we get there.
