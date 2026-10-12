# Level 4: the space between cells

The designer decided on 2026-10-12 that level 4 (~1.8 Ga, a prokaryote biofilm) brings three things:
- boundary and inner cell groups;
- the space between cells, broken into volumes, each a module;
- a researched cost of making the matrix.

Research: docs/research/food-uptake-and-extracellular-space.md, sections "Extracellular space between cells" and "Cost of
making the matrix (EPS)".

## Built (2026-10-12)

### Upstream modules
These live in circulatory-autogen-modules, branch `feat/extracellular-matrix`, merged into `homogenisation-checks` (a12d84a).
- `transport/extracellular_volume/biofilm_ArgusUNPUBLISHED_v01` (`tools/build_extracellular_space.py`). One volume of the
  water between cells. It holds glucose, lactate, H+ and O2, and the matrix as mM of glucose units. Na+, K+ and Ca2+ stay
  at seawater values.
  - Each species: dC/dt = (J_in + J_out + j_cell)/V_e, where J_in and J_out are the summed faces and j_cell is the cells'
    exchange (a single port, so it stays a parameter for the game to set).
  - H+ is buffered by the seawater: capacity V_e (1 + b_H), with b_H = beta/(ln 10 H+) ≈ 2,600.
  - Matrix: dC_mx/dt = j_mx/V_e - k_mx C_mx - k_ovf C_mx (C_mx/C_mx_max)^8.
    - Breakdown at k_mx releases f_mx_glc of it as glucose.
    - Matrix above the densest gel (50 g/L, 309 mM) is squeezed out to the sea.
    - N_mx_lost counts what left the colony, so carbon can be booked.
  - Hindrance f_hind = ((1 - phi)/(1 + phi))², with phi = v_mx C_mx (Mackie and Meares).
- `transport/extracellular_face/biofilm_ArgusUNPUBLISHED_v01`. Diffusion through the gap between two cells:
  G = A D f_up f_down / (d_up f_down + d_down f_up), the two half-paths in series, each slowed by its volume's matrix. Open
  water at the mouth is d = 0 and f = 1.
  - D for glucose, lactate and O2 is its value in water at 20 °C.
  - For H+ it is the free proton plus the share the buffer carries, so H+ spreads at about the buffer's speed.
- The cell, `energy_metabolism adaptive_ArgusUNPUBLISHED_v01` (`tools/build_adaptive_cell.py`), makes matrix:
  glucose + n ATP -> one glucose unit of matrix outside + n ADP + n Pi.
  - n = 3 (2-4 by pathway accounting).
  - Capacity V_mx_max = 0.1 x the resting glycolytic flux, scaled by the game's `mx_alloc` (0-1), as glycogen storage is.
  - Export is irreversible: the thermodynamic factor is clipped at 0.
  - The matrix leaves on the `matrix_export` port as J_mx. The budget gains J_ATP_mx.
- Verified with cam_testing:
  - every species, the matrix and carbon balance;
  - the exact two-volume solution;
  - the second law across faces;
  - the ceiling;
  - definitions;
  - BC sweeps;
  - time-step convergence.

  The cell's carbon, first-law and second-law invariants include the matrix.

### In the game
The game holds copies of the upstream modules in `content/upstream/` (lock `root`), identical to homogenisation-checks
a12d84a: that branch is local, and the submodule must point at a pushed commit (CI checks it out). The cell is assembled
into `content/assembled/` from a12d84a, where matrix is made from glucose 6-phosphate (the split glycolysis).

**Tiling.** Cells sit on the triangular lattice of `ColonyLayout`, so the volumes are the lattice triangles whose three
slots all hold cells: the Delaunay triangles of the cell centres (`ColonyGaps`). On a lattice this needs no general
triangulation. Each volume is the triangle less the three 60° sectors of its cells: 1.44 µm³ for 2 µm cells 0.3 µm apart
and 2 µm deep, the upstream default instance. Why triangles rather than one volume per cell-cell gap:
- each enclosed pocket is one well-mixed volume;
- every gap (lattice link) becomes exactly one face, either between the two triangles either side of it or, on the rim,
  from a triangle to open water;
- every cell edge is six 60° sectors, each facing exactly one triangle.

  So cells, volumes and faces need no overlapping bookkeeping. Dead cells keep their slots and their gaps.

**Boundary and inner groups.** A cell is a boundary cell if any of its six sectors faces open water; the rest are inner
cells. From level 4 (`boundary_inner_groups`, not optional) the world keeps group 0 = boundary and group 1 = inner. The
inner group starts as a copy of the boundary group's targets and controls. A cell that changes group (as the colony
grows) takes its new group's targets.

**Coupling, per macro step (0.1 s).**
1. Each cell sees the mean of what its sectors face, either a volume or open water. Open water is the environment field
   at the cell (`ICellEnvironment.AtMembrane`, corrected for the open share's own exchange with it), with the free sugar
   of packets it touches on the membrane facing them. That gives the cell's membrane glucose, lactate and H+. Changes
   below `field.input_tolerance` of their departure from the sea are not passed on, because each change restarts the
   cell's integration.
2. The cells step.
3. Their exchange is shared out over their sectors. Glucose uptake becomes an implicit sink on each facing volume
   (J/C_membrane/6 x the volume's new concentration), so no volume is overdrawn. Lactate, acid and matrix released are
   sources. The open-water shares go into the field (`Exchange`), the share through the membrane facing packets comes
   out of them, and the field steps after the space.
4. Transport (`Sim.Physiology.ExtracellularSpace`) takes one backward Euler step per species across faces and open
   gaps, solved by conjugate gradients. The gaps relax in milliseconds, so an explicit step would be unstable. The new
   state is rebuilt from the solved fluxes, so the bookkeeping is exact. The mouth of an open gap sees the field
   there, and what flows in is taken from the field at the mouth (H+ as acid).
5. Chemistry, every 1 s: each volume's module advances its matrix (delivered, broken down, overflowing). Volumes with no
   matrix are skipped.

**The matrix control** ("Extracellular matrix", `extracellular_matrix`, level 4, per group, 0-1) sets `mx_alloc`. The
cell panel shows "Making matrix" (ATP/s), "Matrix exported" and "Edge on open water".

**Tests.**
- `ColonyGapsTests`: lattice, 1, 2, 3, 7, 9 and 19 cells, geometry against the upstream default.
- `ExtracellularSpaceTests`:
  - the face law against the module;
  - the backward Euler two-volume solution;
  - conservation of every species and of carbon to 1e-12 of the gross amounts moved;
  - the open-gap steady state;
  - the matrix ceiling and hindrance.
- `Level4ColonyTests`:
  - groups as the colony grows;
  - sliders per group;
  - matrix cost exactly 3 ATP and one glucose per unit;
  - exact bookkeeping in the world;
  - inner cells see less glucose;
  - the field gains exactly the packets' glucose less what the cells' open sectors and the gaps took (1e-12);
  - bit-identical results on 1 and 4 threads.
- kernelgen: goldens for a module without states.

**First numbers** (19 cells, 50 µM outside, matrix on, one hour):
- Glucose in the gaps sits ~2.5% below the outside value.
- Matrix builds at ~0.2 mM/h: glucose-limited, because the cell makes it from internal glucose (µM).
- Speed: 3.3 s per simulated hour, against 1.6 s for the same colony at level 2.

## Open questions
1. **Does the matrix matter?** At realistic gel densities (≤ 100 g/L, phi ≤ 0.06) Mackie-Meares slows small solutes by
   only ~10%. Biofilms' ~0.25 comes mostly from the cells and the tortuous path, which the gap geometry already has. Ways
   to give the matrix more effect:
   - Raise the ceiling.
   - Make the matrix bind or retard charged solutes (EPS is anionic: H+ and cations).
   - Let it swell and push cells apart.
   - Make it the colony's glue (with level 6 adhesion).
2. **What the matrix is for in play.** Keeping glucose the colony releases (Glf leak, broken-down matrix) near the cells;
   a reserve under famine; slowing acid loss. Its benefit needs designing, since the cost is real (2.5 glucose-equivalents
   per unit for a fermenter).
3. **Production rate.** 0.1 x the resting glycolytic flux is a placeholder; lactic acid bacteria divert 0.5-3%. Matrix
   synthesis is glucose-limited at low internal glucose: should it draw on glycogen?
4. **Inner cells starve at 1 µM ocean glucose**, which is correct but harsh. Level 4's ocean and arrivals need tuning with
   the gaps in place.
5. **Food packets** reach the gaps only where a packet touches a gap's mouth (radius half the gap). Should packets inside
   the colony's outline feed the volumes they cover?
6. **The field's buffer and the volumes' buffer differ.** The field turns acid into pH with the carbonate buffer; a
   volume uses a linear buffer (b_H) at the sea's pH. They agree near the sea's pH. Should the volume module take the
   carbonate buffer too?
7. **The creature as a module array** (`ToModuleArray`) does not yet include the volumes and faces. Each would be an
   extracellular_volume record and each gap an extracellular_face record.
8. **Speed.** Changing membrane inputs restart cells' integrations every step. The 0.1% input tolerance halves the
   cost. Soft restarts in BE_batched_c (docs/plans/libcuflynx-solver.md) would remove most of the rest.
9. **Groups beyond level 6.** How the automatic boundary and inner groups combine with player-made groups ("several cell
   groups") and with a fixed body size (counts per group change as cells move between groups).
10. **The upstream branch** is merged into homogenisation-checks (a12d84a, with the sugar carriers). Once that is pushed,
    point the submodule at it and drop `content/upstream/`.
