# Feature plan: many cells in parallel, and what goes on the GPU

Status: design (2026-10-11). Measurements are on the development laptop (i7-12700H: 6 performance + 8 efficiency
cores, 20 threads, AVX2; NVIDIA RTX A1000 Laptop, Ampere, 4 GB), .NET 8. Reproduce them with
`dotnet run -c Release --project src/AdaptOrDeity.Headless -- kernel-bench` and `tools/experiments/kernel_layout/`.

Target (M10): 5k cells and a 256² environment at 60 fps, at top speed. Later levels: tens of thousands of cells in
creatures and tissues.

## What we measured

Cost of one evaluation of the fermenter cell's rates (25 states, 278 inputs, 289 algebraic variables), per cell:

| layout | 1 thread | all threads |
|---|---|---|
| today: one batch per cell | 1,800–2,600 ns | — |
| one batch of all cells (65k) | 1,800 ns | 490–550 ns |
| **lean** batch (group parameters read once, algebraic variables kept in registers) | **310–370 ns** | **70–250 ns** |
| GPU, OpenCL, double (RTX A1000) | 100–150 ns | |
| GPU, OpenCL, float | 3.5 ns | |

- **The current layout is memory-bound.** Each cell carries ~5 KB (278 externals, 289 algebraic values, 25 states)
  and every evaluation streams all of it, although ~230 of the externals are parameters every cell of a group shares,
  and the algebraic values are only needed for display. The lean layout gives bit-identical rates, is 4–5× faster on one
  thread, stays flat as the cell count grows, and scales across threads (the spread in the threaded column is the
  efficiency cores and the thread pool).
- **Double precision on a consumer GPU does not beat the CPU.** Its 1:64 FP64 rate makes the GPU no faster than 20 CPU
  threads on the lean kernel. Gaming GPUs are 1:32–1:64 too, and Apple's Metal has no doubles at all.
- **Single precision on the GPU is ~30× faster than the CPU**, and the rates agree with double to 2×10⁻⁶ away from
  steady state. Near steady state, though, rates that should cancel to zero come out as ~10⁻⁸ of round-off, which an
  implicit solver for stiff physiology sees as real drift.
- **Reading results back from the GPU costs about as much as computing them** (1–7 ms for all rates of 65k–262k cells),
  so anything on the GPU must stay there for many steps.

## What others do

- **Games keep simulation on the CPU and the GPU for showing it.** Thrive (C#, the closest game to ours) diffuses its
  compound clouds in parallel .NET tasks over chunks, with AVX2 where available, in float, and uploads a texture each
  frame. Oxygen Not Included runs its gas, liquid and heat simulation in a native library off the main thread and
  sends the grid to the GPU as a texture. Noita runs its falling-sand world on CPU threads in chunks, updating only
  active regions and never two neighbouring chunks at once. From Dust and PixelJunk Shooter ran their fluids on the
  PS3's SIMD cores. Factorio made each connected fluid system a contiguous block of memory and updates the systems in
  parallel (6.5× faster).
- **GPU fluids exist where the fluid is the product.** Unity plugins (Zibra Liquids, Obi's compute backend) run fluids
  in compute shaders; Obi's own guidance is that its CPU backend wins for small simulations because the GPU has a high
  fixed cost. GPU Gems (ch. 38) and every Gray-Scott reaction-diffusion demo use ping-pong textures that never leave the
  GPU.
- **Many-cell physiology tools split the cell kinetics from the transport** (operator splitting): PhysiCell/BioFVM
  (OpenMP, 500k cells on a desktop; diffusion is the bottleneck), Myokit's OpenCL simulations (single precision by
  default, with a warning that it exposes problems double hides), openCARP's generated CPU/GPU ionic kernels, the Pele
  combustion codes (batched per-cell implicit solves, Jacobian factorised only when updated), DiffEqGPU (one solver
  per GPU thread; Rosenbrock methods suit it, Newton-iterated ones less). GPU cardiac solvers that run in float are
  reported unreliable for long chaotic runs.
- **Unity constraints.** Burst cannot compile our precompiled netstandard2.1 libraries (only code Unity compiles,
  written in its restricted subset). Compute shaders work on DX11/12, Vulkan, Metal and GL 4.3 but not WebGL.
  Reading data back from the GPU is asynchronous with frames of delay. `System.Numerics.Vector<T>` is only accelerated
  in recent Unity 6 builds, and `System.Runtime.Intrinsics` is not in netstandard2.1 until Unity's CoreCLR runtime
  (experimental in 6.7, Mono removed in 6.8). ILGPU needs runtime code generation, which IL2CPP forbids.

Sources are listed at the end.

## The plan

### 1. Generate the lean layout (largest gain, CPU, no new dependencies)

kernelgen emits, for each kernel:
- **Three kinds of input:** per-cell inputs (states, and the externals the world or the player sets per cell: the
  player's trait targets and controls, concentrations at the membrane, the motor), per-group parameters (everything
  else, one copy per group in a parameter block), and constants (already literals).
- **`ComputeRates` with algebraic variables as locals**, writing only rates. This is what the solver calls, many times
  per step.
- **`ComputeOutputs`** (not done yet) writing only the algebraic variables something reads (the panel, probes,
  coupling), selected by a list the world registers once; `ComputeVariables` still writes all 289 every game step.
- **Smaller methods** (not done yet): the generated methods are thousands of lines, which .NET optimises fully only
  after tiered recompilation; split them by submodule.
- **Analytic Jacobian in the same layout** ([symbolic-jacobians.md](symbolic-jacobians.md)): one forward-mode pass per
  colour instead of 26 rate evaluations.

The descriptor says which externals are per-cell (from the module config's port and input roles, plus a game list for
the player's parameters). Index lookups by name are resolved once at load (no string searches per frame).

Expected: ~4–5× per cell on one thread, before threads.

### 2. Integrate cells as batches, in parallel on the CPU

- **Group = contiguous block.** Each group (a creature's cell type, a tissue) owns one batch: structure-of-arrays,
  cells contiguous, its parameter block, and per-cell solver data (the 25×25 Jacobian's LU, 5 KB per cell in double;
  ~25 MB for 5k cells, a budget to watch beyond that). Like Factorio's fluid systems, groups are independent islands
  between coupling steps.
- **A batched integrator.** The backward Euler / modified Newton solver works on a range of cells: each cell keeps its
  own step size, Jacobian age and LU (adaptive steps differ between cells, which suits CPU threads, where each cell runs
  its own loop). Cells at steady state take long steps and cost almost nothing.
- **Threads with a fixed partition.** The batch is cut into fixed chunks (e.g. 64 cells) and run with `Parallel.For` on
  the simulation thread. Results never depend on the thread count (ADR 0005): every chunk's work is independent, and
  reductions (food withdrawn by each cell, the force of each flagellum on the body) are summed per chunk and then
  across chunks in chunk order. A test checks 1 thread against N threads bit for bit, and that a tick does not
  allocate.
- **Splitting.** Per macro step: mechanics → transport ½ → cell kinetics (parallel) → transport ½ (ADR 0002). The
  kinetics of different cells only meet through transport and mechanics, so the parallel part is the whole of the cell
  work.

Expected for 5k cells: ~0.1–0.3 µs per cell per rate evaluation across cores. With ~70 evaluations per cell per
simulated second while inputs are changing (the bench's busy scenario), 5k busy cells need 35–105 ms of compute per
simulated second: 10–30× real time, plus the linear solves. Cells at steady state cost far less (adaptive steps of tens
of seconds). Hence step 3.

### 3. Make top speed affordable: fewer cells to integrate, not faster cells

Top speed (10⁵–10⁶× real time) with thousands of busy cells is out of reach of any per-cell integration on a
desktop, CPU or GPU. The levers that remove work:
- **Levels of detail (ADR 0006).** A tissue of many alike cells is a homogenised element; individual cells run only
  where the player looks or where cells differ.
- **Clones.** Daughters start as copies; cells of a group with the same inputs evolve identically. Integrate one
  representative per set of identical cells and split the set when their inputs diverge (a food packet touches one).
- **Steady cells sleep.** A cell whose rates are below tolerance takes the longest step the input schedule allows
  (already partly true with adaptive steps); wake it on an input change.
- **Event-driven inputs.** The motor and food contact restart steps; group them so a run/pause switch does not restart
  every cell at once.

### 4. The environment field: large implicit steps, on the GPU once it is big

The GPU is used only for well-conditioned work: drawing and diffusion (user decision 2026-10-11). Measured
(tools/experiments/diffusion, single precision): one explicit 5-point step costs 26 µs on all CPU cores and 6 µs on the
GPU at 256², 532 µs and 57 µs at 1024²; an ADI step (any Δt) 125 µs and 5.8 ms on the CPU; coupling 5,000 cells to a
GPU field ~0.2 ms per exchange.

- **Method first.** An explicit step is stable only for Δt ≤ dx²/4D (2 ms at 4 µm spacing for oxygen, so ~556 steps
  per simulated second, ~9,000 per frame at 1,000× speed). Use steps tied to the cell exchange instead: an L-stable
  implicit scheme (damped ADI or locally one-dimensional backward Euler), or an exact spectral step (cosine transform,
  multiply by e^(−Dk²Δt)) where diffusivity is uniform. With one species at 256² that alone matches the GPU (~1.5× over
  the explicit CPU field for the whole simulation, because the cells dominate); on a fine grid (1024², 1 µm) implicit
  CPU steps are ~6× faster than explicit steps on the GPU.
- **Device second.** At 256² with few species, run the field on CPU cores beside the cells. With many species or
  larger grids, run it as a Unity compute shader (or fragment shaders over ping-pong render textures) kept on the GPU,
  four species per float4, reading back only each cell's concentrations through asynchronous readback a frame or more
  late, which the exchange interval absorbs. Expected 5–10× over the CPU field in those cases.
- **Cheaper fields.** Solve the quasi-steady field when a frame spans more than its relaxation time; represent food
  packets as diffusing Gaussians (closed form) rather than grid features; use a fine grid that moves with the body
  inside a coarse far field.

The simulation thread cannot touch the GPU, so this needs an `ITransportBackend` in the simulation with a Unity-side GPU
implementation fed by commands.

### 5. GPU cell kinetics: an experiment with gates, not the plan

Running the cells themselves on the GPU pays only in single precision, and single precision is a risk for stiff
physiology near steady state. Worth trying once 1–3 are done, if a level needs more than the CPU gives:
- kernelgen emits the lean kernel as HLSL (Unity compute shader) from the same AST, in float, and the analytic Jacobian;
  per GPU thread a Rosenbrock step (one LU per step, no Newton iteration count to diverge between threads), as DiffEqGPU
  does.
- **Mixed precision** as the likely compromise: Jacobian and its LU in float on the GPU (modified Newton already uses an
  approximate Jacobian), the residual in double on the CPU, with iterative refinement.
- **Gates:** goldens and the bench's accuracy column within tolerance in float over hours of game time, including
  steady states and slow drifts (ATP, glycogen, damage); a speed-up of at least 5× over the CPU path at the target cell
  count, including transfers; works on Metal (float only) and degrades to the CPU path where compute shaders are
  missing.

### 6. Unity runtime and determinism

- Measure the kernel bench inside a Unity IL2CPP player as well as .NET 8; Mono and IL2CPP code generation differ from
  RyuJIT, and so does `Vector<T>`.
- SIMD across cells (four cells per AVX2 vector) needs vector `exp`/`log`; revisit when Unity's CoreCLR brings
  `System.Runtime.Intrinsics` to our libraries. Burst stays out: it would mean a second copy of the simulation in
  Unity's restricted C#.
- Saves store state, never replay inputs: anything on a GPU differs between vendors in the last bits.

## Order of work and gates

| step | work | gate |
|---|---|---|
| 0 | `kernel-bench` in a Unity IL2CPP player; snapshot cost per cell (name lookups, the O(N²) trace index) | numbers recorded here |
| 1 | **done 2026-10-11**: group inputs read once per call (lock entry `per_cell_inputs`), `ComputeRates` keeps algebraic variables local and computes only those the rates need (218 of 289) | rates and variables bit-identical (4,096 perturbed cells); 5.3× one thread, 8.2× all threads, 3.5× for one-cell batches; one cell end to end 3,845× → 6,808× real time busy, 12,246× → 21,405× calm |
| 2 | analytic sparse Jacobian (symbolic-jacobians.md) | matches finite differences; goldens unchanged |
| 3 | batched integrator per group; fixed-chunk `Parallel.For`; ordered reductions | 1 vs N threads bit-identical; no allocation per tick; 5k busy cells ≥10× real time |
| 4 | clones, sleeping steady cells, levels of detail in play | top speed with 5k cells at 60 fps |
| 5 | environment field: implicit or spectral steps at the exchange interval, several species per pass, on CPU cores; GPU display | 256² field < 5% of the frame at top speed |
| 6 | GPU field (compute shader) when grids or species grow; GPU cell kinetics only as a gated experiment | measured gain over step 5 |

## Sources

Thrive `CompoundCloudPlane.cs` (github.com/Revolutionary-Games/Thrive); ONI patch notes on the simulation texture
race (oxygennotincluded.wiki.gg/wiki/Versions/CU-271819); Noita, GDC 2019 "Exploring the Tech and Design of Noita";
From Dust, GDC 2010 (gdcvault.com/play/1013667); PixelJunk Shooter, GDC 2010 (gdcvault.com/play/1012358); Factorio
FFF #271 and #416; Stam, "Real-Time Fluid Dynamics for Games" (GDC 2003); GPU Gems ch. 38; Obi backends
(obi.virtualmethodstudio.com/manual/7.0/backends.html); Zibra Liquids (80.lv); Unity manual: Burst type support and
floating-point determinism, compute shaders, scripting restrictions; Unity forum on AsyncGPUReadback latency and the
CoreCLR update (June 2026); Myokit SimulationOpenCL docs; openCARP limpet MLIR (CGO 2023); Chaste (PMC4285015);
Marcotte & Grigoriev, arXiv 1309.1720; DiffEqGPU (arXiv 2304.06835); SUNDIALS/MAGMA batched LU (LLNL CSE25 slides);
PhysiCell/BioFVM (PMC5841829); CellModeller (Rudge et al. 2012); Biocellion (PMC4609016).
