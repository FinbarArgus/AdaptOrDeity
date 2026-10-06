# AdaptOrDeity — Architecture & Stage-1 Plan (single cell → multicellular)

## Context
AdaptOrDeity is a new, closed-source **physiology evolution game**. The player adapts an organism, starting as a single cell living by diffusion, and must solve real physiological limits (diffusion distance → nutrient tube → airways/circulation → thermoregulation …) in order to colonise every region of an adapting planet. The repo is empty (README only). This plan fixes the **whole-project architecture** and details **Stage 1: single cell → multicellular**.

The project is built mostly solo by a physiology modeller, but the team will grow, so module boundaries and conventions are first-class. Physiology must come from **CellML modules in the open-source sister repo `circulatory-autogen-modules`** (Apache-2.0 repo, per-module licence CC0-1.0). The game itself stays closed.

## Fixed decisions (from discussion)
| Area | Decision |
|---|---|
| Engine | **Unity 6**, desktop (Win/Mac/Linux). The sim core is a **pure C# engine-independent library** shipped to Unity as DLLs. This contains the licence risk and allows a later Godot port. |
| Fidelity | Quantitative and teachable: SI units, real equations. Extensible to research grade. |
| Space | **2D unstructured (Voronoi) body mesh with a per-cell depth (2.5D)**. All editing happens in the flat view. A later fold into 3D is for visualisation only. |
| Environment | Structured 256² field around the body in the same plane (environment cells covered by the body are inactive), with advection-diffusion and **far-field reservoir** boundaries. Creature velocity improves exchange through a boundary-layer model. |
| Exchange | **In-plane edges only.** The into-screen and out-of-screen surfaces never exchange (no diffusion, no heat loss). Depth only scales cell volume and the area of in-plane faces. The diffusion crisis comes from in-plane distance to the organism's perimeter, which is what pushes the player towards a nutrient tube. |
| Species | O2, CO2, glucose, metabolic waste (transported); ATP and glycogen (local states); temperature (transported heat with metabolic production and surface loss). **Q10** temperature dependence; pH/bicarbonate deferred. |
| Time | **Two switchable modes**: RealTime (continuous multi-rate sim) and EpochSteadyState (steps of months to years while the environment adapts, with the organism solved to steady state each epoch). **No growth or division**: the organism is always in its adult state. Edits apply live. **Checkpoints at a fixed sim-time interval or once per epoch.** |
| Editing | Paint/erase cells (1 painted cell = 1 biological cell early; later 1 element = N cells, with zoom for visuals only). Cell types: generic metabolic, barrier/epidermal, contractile (proto-muscle), sensory, storage. |
| Cost | Real physiological cost (maintenance ATP inside the kernels) plus a **mutation budget**. |
| Mutation budget | Comes from **reproductive output**: a population model of the player's fitness vs **hard-coded historical competitor creatures per region**, × a `reproductive_organ` multiplier. Asexual for now; sexual reproduction is a future decision. |
| Fitness | Pluggable heuristics with **global fixed weights**. The region only changes the forward-prediction environment. Heuristics: mass, extremity muscle mass, sensing ability, cell damage, **famine resistance** (forward sims with decreasing energy availability → survival time), **environmental/homeostatic tolerance** (region-dependent forward sims), reproductive output. Each switches on only when its prerequisite physiology exists. |
| Sensing | Feeds the sensing heuristic only (no chemotaxis in stage 1). |
| Failure | Starving cells accumulate damage → necrosis, shown on a **Damage overlay tab**. Damage lowers fitness at the next checkpoint. |
| UX | **Oxygen Not Included style**: hotkeyed overlay tabs that recolour the same mesh (O2, CO2, Glucose, Waste, Temperature, ATP, Damage) with legends, hover tooltips, a category build/paint menu, pause/1×/2×/3×, corner warnings, and a cycle report at checkpoints. Also: CUFLynx-style live plots on click, flux arrows/particles, and a parameter inspector limited to **educational mechanistic parameters**. Art: stylised 2D. |
| Performance | ~5k body cells + 256² env at 60 fps with time acceleration, in pure multithreaded C#. The transport backend can be swapped (Burst/GPU) later. |
| CellML | **Build-time C# kernels**: a Python libcellml codegen produces one kernel per module version. A runtime C# assembler wires kernels across the mesh as the player edits. A **native finite-volume solver** owns all spatial transport, and CellML owns local kinetics (operator splitting). The modules repo is a **git submodule**, and new modules are authored **upstream** and validated with `cam_testing`. **Modding** (external module folders) comes later, so the registry is designed for it now. |

## Repository layout
```
AdaptOrDeity/
  AdaptOrDeity.sln, Directory.Build.props (netstandard2.1 + net8.0), .editorconfig, LICENSE (proprietary), THIRD_PARTY_NOTICES.md
  external/circulatory-autogen-modules/     git submodule (pinned commit)
  content/   modules.lock.json (module/version/instance + sha256), species.json, celltypes/*.json,
             regions/*.json (env spec + competitor creatures), planets/*.json, fitness.json (global weights),
             display_units.json, schemas/*.schema.json
  src/
    AdaptOrDeity.Sim.Core/        clock, ids, SoA buffers, SI constants, job pool, deterministic reductions
    AdaptOrDeity.Sim.Mesh/        Voronoi/Delaunay body mesh (port of MIT Delaunator), geometry, depth, CSR face graph
    AdaptOrDeity.Sim.Modules/     ModuleDescriptor, ModuleRegistry, IKernel, KernelBatch, Assembler, integrators
    AdaptOrDeity.Sim.Kernels.Generated/   GENERATED, committed, never hand-edited
    AdaptOrDeity.Sim.Transport/   ITransportBackend + CPU FV backend (body graph + env grid + coupling + advection)
    AdaptOrDeity.Sim.Physiology/  Organism, BodyPlan, CellType, damage/necrosis, mechanics/velocity
    AdaptOrDeity.Sim.World/       Planet, Region, EnvironmentSpec, EnvironmentField, PlanetDynamics
    AdaptOrDeity.Game.Rules/      IFitnessHeuristic, Checkpoint, ForwardPredictor, PopulationModel, MutationBudget, Stages
    AdaptOrDeity.Game.Content/    JSON loading + schema validation + unit conversion
    AdaptOrDeity.Game.Persistence/ save container + migrators
    AdaptOrDeity.Game.Session/    ISimSession facade: SimHost thread, command queue, snapshots, events, UI schema
    AdaptOrDeity.Headless/        console: scenarios, trace dumps, benchmarks
  tests/      NUnit per project, ArchitectureTests, Benchmarks, golden/<module>/<version>/<instance>.csv
  tools/kernelgen/   Python (libcellml >=0.6.3,<0.7 pinned like libcuflynx; myokit) → C# kernels + goldens
  unity/     Unity 6 project; Packages/com.adaptordeity.sim/Plugins (built DLLs);
             Assets/AdaptOrDeity/{Presentation, UI, Backends(later Burst), Editor, Tests}
  docs/      architecture.md, conventions.md, units.md, module-authoring.md, adr/NNNN-*.md
```
**Dependency rule** (enforced by ArchitectureTests): `Unity → Game.Session → Game.* → Sim.*`. Sim never references Game. Nothing outside `unity/` references UnityEngine.

## Core domain model
- **Planet** → **Region**s (EnvironmentSpec: bath composition, temperature, flow generator, sources, extent/dx, boundary values, **competitor creature table by epoch**). `PlanetDynamics.Advance(epoch)` runs at checkpoints. Colonisation record per region.
- **Organism**: BodyPlan (cells = Voronoi seeds + CellTypeId + depth + parameter overrides, with a stable CellId), runtime state, velocity, and aggregates (mass, extremity muscle mass, sensor count, damage).
- **CellType** (content JSON): list of `{module_type, version, instance, overrides}`, inspector whitelist, face/permeability properties per species, mutation cost, tags (`contractile`, `sensory`, `storage`, `reproductive_organ` later), damage parameters.
- **Body mesh geometry**: V = area·depth. In-plane face area = edge length × min(depth). **Only in-plane faces exchange**: cell↔cell faces inside the body, and perimeter faces coupling the body to the adjacent environment cells (conductance σA/d in series with a boundary layer). The top and bottom surfaces are insulated for species and heat, so each cell is well mixed through its depth and no depth sublayers are needed.
- **Modules**: `KernelDescriptor` (generated: names, units, SI multipliers, ports + multi_port, defaults, `bc_sweep` tested ranges, references, licence, source hashes); `IKernel` (SoA `ComputeRates` over a slot range); `ModuleRegistry` (providers: Generated now, ExternalFolder plus interpreter/JIT later, parsing the upstream folder layout itself).
- **Assembler**: BodyPlan + CellTypes + Registry → CompiledTopology. It reimplements libcuflynx port matching: exit ↔ entrance/general of the same `port_type`, positional variable pairing, `multi_port` True/sum/Multiply, following `circulatory_autogen/src/libcuflynx/generators/{CVSCellMLGenerator.py ~L681–960, multi_port.py}`. Faces between cells are compiled into transport conductances, not kernels. Full rebuild per edit batch (~ms), with state remapped by `(CellId, module, variable)`.
- **ITransportBackend** (CPU): implicit diffusion (PCG on the body graph Laplacian; ADI/CG on the env grid), upwind/semi-Lagrangian advection with CFL substeps, and body↔env coupling `A/(d/σ + δ_BL/σ_env)` with δ_BL from a Sherwood correlation (velocity ⇒ better exchange). The flux law is identical to `tissue_diffusion_face` (G = σA/(d_up+d_down)).
- **Game rules**: `SimClock` (single source of truth for time); multi-rate `Scheduler`; `IFitnessHeuristic {IsActive, Requests, Evaluate → [0,1] + explanation}`; `ForwardPredictor` (immutable snapshot + region env + scenario, i.e. famine ladder or tolerance sweep, run on a background pool, deterministic); `PopulationModel` (player fitness vs regional competitors × reproductive_organ multiplier → reproductive output → budget); `MutationBudget` ledger; `Checkpoint` → `CycleReport`.

## Per-step data flow (sim thread, Strang split)
1. Drain the command queue (Paint/Erase/SetDepth/SetParam/Speed/Probe/Region) → validate against the budget → rebuild the mesh and topology if dirty.
2. Mechanics: contractile ATP use → thrust; Stokes drag → velocity → boundary layer δ.
3. Advect env (dt/2) → implicit diffuse body + env + coupling for species and heat (dt/2).
4. Local kinetics (dt): per kernel batch in topological order, chunk-parallel: gather externals → integrate (explicit RK, or backward Euler for kernels flagged stiff) → scatter. The cell pool is integrated together with its kernels, so the split equals the CellML volume module.
5. Diffuse (dt/2) → advect (dt/2).
6. Slow tasks: damage → necrosis (kernels off, faces inert), alerts.
7. Accumulate fitness integrals and probe ring buffers.
8. Clock tick. When a checkpoint is crossed: snapshot → async forward predictions → CycleReport event. The live sim keeps running.
9. Publish a snapshot frame.

**Threading/Unity boundary:**
- One persistent SimHost thread (CUFLynx live-worker pattern) plus a fixed-chunk worker pool, giving **bit-identical results at any thread count**.
- Unity → sim: `ConcurrentQueue<ISimCommand>` only.
- Sim → Unity: triple-buffered pooled float32 `SnapshotFrame`s and an event queue, with no per-frame allocations.
- **Schema-driven UI**: overlays (id, hotkey, unit, range, colormap), cell-type catalogue and inspector entries come from `ISimSession.Schema`.

## CellML pipeline (`tools/kernelgen`)
For each `modules.lock.json` entry:
1. **Licence gate** (allowlist; build fails otherwise).
2. Wrap the component and mark port/boundary-condition inputs as libcellml external variables, then run the Analyser.
3. **Units gate**: port variables must be canonical SI (mol/m³, mol/s, m, m², m³, K, W) or have a declared conversion.
4. Emit C# through a custom GeneratorProfile, rewritten to SoA. Fallback: emit per equation via `Generator.equationCode`.
5. Emit the descriptor.
6. Golden traces via Myokit/CVODE.

CI regenerates and diffs, so C# CI needs no Python.

Reference: libcuflynx's C-profile use in `circulatory_autogen/src/libcuflynx/generators/CVSCppGenerator.py` (~L1190–1530).

**Upstream-first workflow:** author the module in `circulatory-autogen-modules` → cam_testing green → bump the submodule pin → regenerate kernels and goldens → update content.

**New upstream modules for stage 1:**
- `tissue_diffusion_volume` vN with a **summing `species_exchange_port`**
- `tissue_diffusion_face` v02 (sigma per side + membrane permeability, for barrier cells)
- `tissue_heat_volume`
- `glucose_uptake` (MM)
- `energy_metabolism_lumped` (aerobic/anaerobic ATP, O2/CO2 stoichiometry, waste, heat, Q10)
- `atp_maintenance_demand`
- `glycogen_storage`
- `cell_damage`
- `contractile_unit`
- `chemosensor`

Existing modules to reuse: `modules/transport/tissue_diffusion_volume` and `tissue_diffusion_face` (Fang2008_v01), and `modules/respiratory/tissue_GE_simple`.

## Units, saves, conventions
- **Units:** canonical SI doubles in hot loops. Units are checked at build time and at port binding. `display_units.json` (µm, µM, mmHg, °C) is used only in the UI.
- **Saves:** zip `.aodsave` containing a manifest (versions, content + module-lock hashes, RNG, clock), planet/bodyplan/ledger/history JSON, and `state.bin` keyed by `cell:<id>/<module>@<version>/<var>`. Includes an `ISaveMigrator` chain.
- **Conventions:**
  - ADRs for cross-cutting choices.
  - Physiology equations live upstream in CellML only. Native C# equations need an ADR and a reference test.
  - Hot-path rules: no allocation, no LINQ, SoA.
  - Rules, weights, cell types, regions and competitors are content-driven.
  - CellML variable names are kept in descriptors and saves.
  - CODEOWNERS per `src/` project once the team grows.
  - A PR template with a tests/goldens/units/ADR checklist.

## Stage-1 milestones (each ends green in CI)
| # | Milestone | Verification |
|---|---|---|
| M0 | Skeleton: sln, props, submodule, schemas, CI (python + dotnet matrix + GameCI), Unity loads the stub DLL | CI green on 3 OSes; architecture test; Unity PlayMode smoke |
| M1 | kernelgen → C# kernel for `tissue_diffusion_volume/Fang2008_v01`; headless single cell in a well-mixed bath | Golden NRMSE < 1e-6 vs Myokit; analytic steady state matches |
| M2 | **Vertical slice in Unity**: SimHost, clock, commands, snapshots, one cell, O2 overlay + legend, tooltip, click → live plot, speed controls | No per-frame GC; sim rate ±1% of preset; deterministic pause |
| M3 | Voronoi body mesh + paint/erase + depth brush; native transport (body graph + 256² env + advection + perimeter-edge coupling); flux arrows; benchmark | Analytic diffusion + conservation (1e-12); parity vs a libcuflynx face/volume model; perf baseline in the Unity player |
| M4 | Upstream metabolism set → all species + temperature + Q10; stiff integrator; all overlays | cam_testing green upstream; goldens; carbon/energy balance closes; hypoxic core appears at the analytic critical radius (in-plane, r ≈ √(4DC₀/M)) |
| M5 | Damage + necrosis + Damage tab + warnings; save/load v1 | Starvation timeline matches the golden; save round trip is bit-identical |
| M6 | Cell palette (barrier, storage, sensory, contractile → velocity → Sherwood); build menu; educational inspector with tested ranges | Module goldens; motility raises uptake as Sh predicts; inspector clamps to bc_sweep ranges |
| M7 | Checkpoints, non-predictive heuristics, PopulationModel + competitor tables, MutationBudget, cycle report | Heuristic unit tests on synthetic organisms; ledger invariants; report snapshot tests |
| M8 | ForwardPredictor: famine resistance and tolerance on a background pool | Deterministic; live frame time within 5%; famine survival matches the reserve/demand estimate |
| M9 | Planet/regions, region selection, colonisation tracking, planet drift; stage-1 completion; save v2 + migrator | Headless scenario tests; v1→v2 migration test |
| M10 | Performance and polish (IL2CPP profiling, env multi-rate, optional Burst backend), ONI-style UX polish | 5k cells + 256² at 60 fps at top speed on the reference machine |

## Key risks
- **Stiffness** (ms ATP kinetics vs ~10³ s diffusion equilibria). Mitigations: implicit transport, implicit or QSSA kinetics, and an upstream stability declaration at the game's dt.
- **libcellml C# profile edge cases.** Use the per-equation fallback; pin libcellml <0.7.
- **Port semantics drift from libcuflynx.** Mitigation: assembler parity goldens and a documented supported subset.
- **Unit mismatches between modules** (dimensionless vs mM). Mitigation: the build-time gate.
- **Mono/IL2CPP performance.** Benchmark in Unity from M3 onwards; the backend is swappable.
- **Gameplay vs physics:** at µm scale, body temperature ≈ bath temperature, so heat matters more in later stages. With in-plane exchange only, a flat body's depth does not protect it; the 2D layout alone decides which cells starve.
- **Unstructured mesh editing cost.** Use incremental Delaunay plus a full rebuild fallback; this is benchmarked in M3.
- **Process:** upstream-first is slower for a solo developer, so allow a temporary pin to a feature branch. Release builds need modules marked `reviewed: true`.

## Resolved after review
1. **Two time modes (`SimMode`)**, switchable by the player at any time:
   - **RealTime**: the continuous Strang-split sim described above, with 1×/2×/3× speeds.
   - **EpochSteadyState**: time advances in steps of months to years. Each epoch: `PlanetDynamics.Advance(Δt_epoch)` updates the region environments, then the organism + local environment are solved to **physiological steady state**. The solver is pseudo-transient continuation with the implicit transport and stiff kinetics, finished by a Newton polish. Then damage, fitness and the budget are integrated over the epoch.
   - A checkpoint fires each epoch.
   - The steady-state solver is shared with `ForwardPredictor` and new-cell initialisation. It lives in `Sim.Physiology` as `SteadyStateSolver`.
   - Milestone: add steady-state mode to M8–M9, with verification that the steady state equals the long-time limit of RealTime to within tolerance.
2. **Fitness ∈ [0,1].** Each heuristic score is in [0,1]. Total = Σ wᵢsᵢ / Σ_active wᵢ, using fixed global weights, renormalised over the heuristics currently active so the total always stays in [0,1].
3. **New cells start at their CellType's physiological baseline steady state.** It is precomputed per CellType from instance defaults in the reference environment, by `SteadyStateSolver` on an isolated cell, and cached by (CellType, overrides hash).

## Open questions (deferred — figure out later)
1. Is an erased cell's content released to its neighbours (conserved) or lost?
2. Exact reproductive output / PopulationModel equation, and the first competitor-creature tables per region and epoch.
3. Single-cell start: one cell only? Flagellar motility before contractile cells?
4. Homogenisation rules when an element = N cells.
5. Modding interpreter/JIT and sandboxing; distribution platform (Steam?).
6. Sim-seconds per real second at each RealTime speed; RealTime checkpoint interval; epoch length options.
7. Fix the unfilled copyright line in `circulatory-autogen-modules/LICENSE`, and decide the NOTICE text.
