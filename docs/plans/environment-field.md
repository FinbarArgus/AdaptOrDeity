# The environment field

Chemicals around the creature on a grid that follows it: diffusion with implicit steps tied to the cells' exchange,
cells taking up and releasing species, food particles releasing into it, the ocean of the epoch at its edges.
Background: docs/plans/parallel-and-gpu.md section 4; research: docs/research/food-uptake-and-extracellular-space.md.

## Decided (2026-10-12)
- **Species (8):** glucose, lactate, H+ (pH), O2 and the four trace species (smells). Na+, K+ and Ca2+ stay at seawater
  values. Cells exchange glucose (`J_glc_e`), lactate (`J_lac_e`) and H+ (`J_H_lac`) now.
- **Size:** the field grows with the colony, multiplicatively: 60 times the colony's diameter (and at least the free
  organisms' range, 136 µm across for one cell).
- **Food:** a packet is a dissolving particle that releases glucose and its traces into the field until used up (not
  the earlier "spreading Gaussian"), pending the discussion below.
- **Arrivals:** what appears per second (food now; prey, enemies and friends later) is constant for each level and
  region, set in content/timeline.json to make food scarce or abundant. Done for food.

## Built
- `Sim.Transport.DiffusionField` (2026-10-12): species on a square grid in a thin water layer, implicit row-then-column
  steps (`DiffusionScheme`: locally one-dimensional backward Euler, or Douglas ADI), far-field or closed edges, point
  and bilinear sources, interpolated sampling, edge and inner-rectangle face fluxes per step, shifting by whole cells.
- `Sim.Transport.NestedField` (2026-10-12): the nested grids.
  - **Levels:** level 0 is 1 µm (`field.fine_spacing`) and covers the colony plus 8 µm (`field.fine_margin`) each side,
    rounded up to a multiple of 4 cells and at least 24; each further level has 4x the spacing and the same N cells a
    side (4x the extent), except the outermost: the fewest levels whose outermost spans 60x the colony diameter
    (`field.extent_multiple`) in at most 2N cells, with as many cells as that takes (so it spans the extent to one of
    its cells, not up to 4x beyond it), and at least the free organisms' range each way (`NestedFieldSettings.Reach`,
    2 view widths, plus two of its cells). One cell: 24 cells at 1 µm and 34 at 4 µm (136 µm; 30, 120 µm, without the
    reach); 64 cells: 44 x 4 levels, outer 24 at 64 µm; 512: 80 x 4, outer 58; 5,000 (~160 µm): 208 x 4, outer 179 (out
    to 11.5 mm). Each level's corner sits on the next level's grid lines, so a coarse cell under a finer grid (its
    shadow) holds 4x4 fine cells; the outermost recentres by its own cells.
  - **Step (coarse first):** every level takes a Douglas step (its steady state is exact at any step; locally
    one-dimensional steps at the 0.1 s exchange halve the gradient within ~8 µm of a cell, measured). A finer level's
    edges are closed except for what the coarser step carried into its shadow through each coarse face, which enters
    through the 4 fine edge cells along it; sources act on their finest level and, summed, on the coarser shadows; then
    each shadow takes the mean of the fine cells over it. The finer grid's boundary flux is therefore the coarse one:
    content over all levels changes only by sources and the outermost edge, to rounding (tests: 1e-12). An explicit
    reflux (fine flux corrected into the coarse ring, the first design) was unstable at λ ~ 300: the coarse-flux
    coupling has no such feedback. Price: the flux at the fine grid's edge is resolved at the coarse spacing (hence the
    margin; measured against one fine grid over the same extent: within 1% of the membrane's depletion 10 µm away).
  - **Fine substeps:** the finest level takes `field.fine_substeps` (4) Douglas steps per macro step, its sources and
    the coarse inflow held over them. Douglas damps a moving cell's near field slowly (its short modes), so with one
    step a cell drifting at 2 µm/s saw 7% too little of its own depletion; with 4, 0.7% (20 µm/s swimming: 16% and 4%).
  - **Smells** (the trace species) step every `field.trace_stride` (10) macro steps over the time since, with the mean
    of what packets released meanwhile (`NestedField.SetStride`; exact in content, same steady state): nothing reads
    them every step, and they were 4 of the 7 species stepped.
  - **Moving and growing:** levels recentre by whole cells of the next coarser level when the colony is more than one
    such cell off centre; the fine grid's N and the number of levels grow with the diameter (never shrink). Both refill
    each level from the old ones (copy where covered, else a minmod-limited linear prolongation that keeps each coarse
    cell's mean), so content is conserved to rounding apart from water entering or leaving at the outermost edge
    (`OceanInflow`).
  - **Membrane (2026-10-13):** the grids hold the layer's depth average, which is exactly 2D. A cell of radius a at
    mid-depth of a layer of depth d (closed floor and top: its images make the field 2D beyond ~d) sees at its membrane
    C2D(r*) + J/(4 pi D a), r* = 2 d e^-gamma (11.2 µm for the 10 µm layer, `field.layer_depth`). The grid's own value
    at a source is the 2D field at r_eff = 0.2 h (Peaceman) to 0.72 h, depending on where the source sits in its grid
    cell (the lattice Green's function of the bilinear deposit and sample, `DiffusionField.SelfCoupling`), so
    `AtMembrane` = C_grid + J [1/(4 pi D a) - (ln(4 sqrt 2 d/h) - 2 pi delta)/(2 pi D d)] (`MembraneResistance`): the same
    membrane value wherever the cell sits and on any spacing (tested to 1% against the analytic value, h = 1 and 0.5 µm,
    nested and single grids). The earlier C_grid + J/(4 pi D a) counted the 2D field below r* twice: 1.6-1.8x the
    depletion at the membrane, and ±9% as the cell crossed its grid cell. Not yet included: the 3D near field of
    neighbouring cells (in a colony each cell's six neighbours would add about 4x its own near-field correction), and a
    cell resting on the floor (~1.4x its own near field).
  - **Parallel:** each level's rows, then columns, in blocks of 32 lines per (species, block) item on the world's
    workers; results are bit-identical on any number of threads (tested). Species with no sources that sit at the far
    field (O2, the traces until food arrives) are not stepped. Douglas changes below 1e-200 are flushed to 0: a field
    settling towards a far field of 0 reached subnormal numbers, 14x slower. Recentring allocates nothing.
- `Sim.World.EnvironmentField` and the coupling in `FermenterWorld` (2026-10-12), always on since 2026-10-13 (the
  contact rule, `field.enabled` and the field-off code paths are gone).
  - **Species (8):** glucose, lactate, acid, O2, aspartate, serine, ribose, dimethyl sulfide; diffusivities with sources
    in content (`field.diffusivity.*`). Far field: the level's ocean glucose (timeline `ocean`; changing it makes a new
    sea), lactate and acid 0, O2 `environment.oxygen`, traces 0.
  - **pH:** the field carries acid (H+ added to the ocean, mol/m³), which the carbonate buffer carries (D of
    bicarbonate); free H+ follows from one buffer pair (`environment.carbonate_total` 3 mM, pK 5.9) in closed form, so
    pH never goes negative and the ocean's pH 7.3 (the kernel's H_o) is the zero.
  - **Each macro step:** the field fits the colony; the enzymes of the cells touching a packet cut its polymer into its
    free sugar (held back at `food.contact_glucose`, product inhibition, as in a vacuole), its free sugar leaks into the
    field (`food.free_leak_rate`, with its traces in proportion: contact trace per contact glucose) and what it offers
    touching membranes is set (implicit in what they drew last step, `FoodField.Predict`); each cell reads C_glc_e,
    C_lac_e, H_e and H_o from the field at its position with the membrane correction for its last exchange with the field,
    and the free sugar of each packet it touches on the cap of membrane facing it ((1 - cos t)/2, sin t = a/(R + a): ~5%
    for a fresh packet; `FoodField.Faces`); an input is passed only when it moved by more than `field.input_tolerance`
    (1%) of its departure from the open water (each change restarts the cell's integrator); cells step; each living
    cell's uptake through the faces comes out of those packets (`FoodField.Draw`, in cell order, then free organisms by
    id) and its other exchange (J_glc_e, J_lac_e, -J_H_lac) goes into the field; the field steps. Every mole leaving a
    packet or a cell enters the field or a cell (tests: 1e-12). Food eaten (a statistic) is all a cell takes from
    packets and vacuoles, and its field uptake's share above the ocean's glucose (sugar that leaked from packets).
  - **Contact (2026-10-13):** what leaks from a packet that living cells touch comes out at the contact, on their
    surface: it enters the field at each touching cell (equal shares) and each sees it in its membrane correction as its
    own release (`FermenterWorld.AtContact`, `ContactGlucose`); untouched packets leak where they lie.
  - **Why faces:** sugar hydrolysed into open water reaches a cell with probability k/(k + 4 pi D a), k its carriers'
    clearance: ~0.05% for Glf (k ~ 5e-18 m³/s against 4 pi D a = 8e-15). With release into the water only, a level-1
    cell starved within minutes of the start. Through the face, the packet's free sugar (~50-70 mM) reaches ~5% of the
    membrane: a resting cell eats at its demand (~5e-19 mol/s), about the hydrolase's rate on one packet.
  - **Can the face be wider? (designer, 2026-10-13, "Can we increase the 5% packet face?"; open question 19)** The
    proposal: sugar hydrolysed at the contact spreads around the cell in milliseconds (a²/D = 1.5 ms), so all of the
    membrane sees it. Worked through, the physics does not widen the face:
    - The mean over a sphere's surface of the field of a point source Q on that surface (the sphere reflecting) is
      exactly Q/(4 pi D a), the same as for a source at its centre (reciprocity: it equals the field at the source of a
      uniform release from the surface). That is how the contact source now enters the cell's membrane correction.
    - A membrane with uniform carriers of clearance k (uptake per concentration, linear at these concentrations) then
      takes up k/(k + 4 pi D a) of Q, wherever on the surface Q comes out. The capture of ~1 for product released at the
      surface (a/r, the Vetter et al. 1998 picture) holds for a perfectly absorbing cell, k >> 4 pi D a (Berg and Purcell:
      a few thousand diffusion-limited absorbers). The kernel's carriers are far from that: measured clearance at low
      glucose, Glf 4.5e-18 m³/s (k/(k + 4 pi D a) = 5e-4), Glf x4 1.7e-17 (0.2%), the H+ symporter at its largest share
      1.7e-17 (0.2%), ABC 1.2e-16 (1.4%), PTS at its largest share 7.9e-16 (9%); 4 pi D a = 8.4e-15 m³/s. Attached marine
      bacteria are indeed seen to release most of what they hydrolyse (Smith et al. 1992, hydrolysis-uptake uncoupling).
    - So the contact's near field adds a Glf cell about 0.05% of what leaks at the contact; the ~5% cap facing the packet
      (the sealed contact: the packet's free sugar, held at 50 mM, against that part of the membrane) stays what feeds a
      touching cell. Effective share now: the cap, (1 - cos t)/2 = 5.2% for a fresh packet, plus k/(k + 4 pi D a) of the
      leak (tested: ContactTests).
    - What would feed a cell more, physically: a larger sealed contact (the cell spreading on the particle, as cells on
      surfaces do), carriers with higher affinity (the PTS takes ~9% of the near field), a periplasm-like envelope that
      keeps the product near the membrane (sugar entering it at the contact would reach all the carriers; its loss is
      through the outer layer's permeability, not free diffusion), or engulfing (the vacuole lines most of the membrane).
      A design choice for the designer.
  - **Frames:** `FieldExtent`, `FieldGlucose`, `FieldLactate` and `FieldPh` (64x64 floats centred on the creature over
    max(food window, 3 colony diameters), ground axes; codec ASN6). Unity draws them as the water overlays (F7 glucose,
    F8 lactate, F9 pH; `FieldView`), under the cells, coloured over the range in view (the legend shows it).
  - **Cost** (`field-bench`, 3 active species, 1 / 8 workers, per 0.1 s step, other jobs running; 2026-10-12 -> 2026-10-13,
    with glucose's 4 fine substeps, the outermost grid at the extent and levels narrower than 40 cells on one thread):
    1 cell 0.04 / 0.05 -> 0.04-0.05 / 0.04-0.05 ms; 64 cells 0.15 / 0.09 -> 0.17 / 0.14 ms; 512 cells 0.50 / 0.28 ->
    0.62 / 0.45 ms; 5,000 cells 3.5 / 1.7 -> 4.5 / 2.4 ms, including the per-cell exchange. The cells cost ~3 µs per cell
    per step with changing inputs (15 ms single-threaded at 5,000), so the field is ~15-30% of them. A one-cell world
    (field, smells every 1 s, the cell) takes ~76 µs a step on one thread, ~95% of it the field: a simulated week in ~7
    minutes. colony-bench (512 cells, 20 s, 1 / 8 threads): 1.06 / 0.31 s -> 0.98 / 0.32 s (the field world is the only
    one now; the contact rule ran it in 0.61 s).
- Tests: tests/AdaptOrDeity.Sim.Core.Tests/NestedFieldTests.cs (geometry and the outermost grid's extent, closed-field
  conservation with sources, far field steady, Gaussian across grid edges, steady point sink vs the thin layer's ln r
  profile, the membrane against the analytic thin-layer near field wherever the cell sits on two spacings, a drifting
  cell's own depletion with fine substeps, strided and per-species substeps conserving, recentring and regridding
  conservation, determinism, idle species), DiffusionFieldTests (Douglas steady state and boundedness),
  tests/AdaptOrDeity.Game.Session.Tests/EnvironmentFieldTests.cs (the free part leaking, enzymes cutting polymer into
  free sugar, product inhibition, buffer, cell and food conservation, a colony depleting glucose and the water
  recovering when it dies, growth, determinism, frames, the game against the reference for one cell and 7 cells),
  FoodFieldTests (a touching cell sees a packet's free sugar on its face), ParallelCellsTests (stepping in the field
  allocates nothing).

## For the designer
- **Douglas steps** damp patterns a few cells long slowly at the exchange step (a cell's near field takes ~1-3 s to
  settle instead of one step) and can overshoot a little (~10% on a checkerboard at 10^4x the explicit step): not
  positivity-preserving. Membrane values are clamped at 0.
- **2D layer:** a colony's depletion falls off as ln r (weaker than 3D's 1/r), so depletion and lactate build-up are
  stronger and recover slowly (as 1/t at the centre) compared with open water. A 3D correction (the water above the
  layer) would change this; the 10 µm depth is a stand-in.
- **Other H+ fluxes** (the H+-ATPase, NhaA, the flagellar motor) are not put into the field yet: only H+ with lactate.
- **Packets** leak their free part whether or not a cell is near; their polymer is cut only where cells touch them, at
  each touching cell's surface hydrolase rate V_max S/(K_m_poly + S) (S: the packet's polymer over its sphere; saturated),
  into the packet's free sugar, which the touching membrane takes up on the ~5% of it facing the packet. Each packet a
  cell touches gets the cell's full hydrolase rate (several touching packets are digested in parallel): a choice to
  revisit (share the enzyme by the faces?).
- **A level-1 cell lives at its demand while it touches a packet** (~5e-19 mol/s), and its ATP runs out in the first
  gap between packets longer than its reserve, for good (starvation is absorbing, open question 18). Held food now stays
  against its cell as it shrinks (free-daughters.md, "Meals have mass"): before, a held packet lost contact as it shrank
  and the cell starved holding food, so a cell now eats all it holds and gaps come only when everything held is eaten.
  Calibration with the scripted players: docs/plans/scoring.md.
- **Growth needs a vacuole:** the face's ~5% of membrane cannot carry growth's uptake (growth 3 needs ~1.9e-18 mol/s), a
  vacuole lining most of the membrane can. A level-2 cell at growth 3 starves in the gaps between swallowed packets (see
  the calibration below).
- **1% input tolerance** of the departure from the sea trades accuracy of the cells' inputs for speed (+15% at 512 cells
  over the old rule); 0 (every change) costs ~60% more.
- **Neighbours' near field:** each cell's membrane sees its own 3D near field but its neighbours' only as resolved by the
  1 µm grid; in a dense colony that underestimates the depletion between cells (each of six neighbours at 2.2 µm would
  add about 0.6 of its J/(2 pi D d) more).

## Calibration with the field (2026-10-13)
Explicit probes (deleted after), one random sequence each; LevelTests hold the level-1 results.

| Run | Field (food 5e-3 packets/s, 1.2e-14 mol per point) | Contact rule (2026-10-12: 3e-3, 2e-14) |
|---|---|---|
| Level 1 left alone | 7.2e-14 mol, 6 points; ATP gone day 1.7, damage 0.55 by day 7 | 1.69e-13 mol, 8 points; day 3.9 |
| Level 1 kept on food (1.5e-2 packets/s) | 3.1e-13 mol, 26 points | 2.98e-13 mol (1e-2), 14 points |
| Level 2 after an untouched level 1 | eats nothing more; dies at the end of level 2 | the same (4 prey) |
| Level 2 fresh, untouched (prey 2e-4/s) | 3.0e-13 mol, 24 points, 4 prey | 2.98e-13 mol, 14 points, 7 prey |
| Level 2, level's food, growth 1, automatic division | 5.7e-13 mol, 47 points, 10 prey, 1 daughter | (not measured) |
| Level 2 kept on food (1.5e-2), growth 1 | 47 points, 10 prey, 1 daughter | |
| Level 2 kept on food (1.5e-2), growth 3 | 1.1e-12 mol, 93 points, 2 prey, 5 daughters | 5 daughters (1e-2) |

A level-1 cell's outcome hangs on the gaps between packets (see "For the designer"). At level 2 a fed cell eats from
its vacuole (engulfed packets), which lines most of its membrane: growth works there; the mitochondria path's 10 prey
are reachable in a week at growth 1. `skills.points_per_offspring` is unchanged (1).

## Decided 2026-10-12 (second round)
- Grid: nested grids (fine ~1 µm around the colony; coarser grids x4 spacing and extent out to 60x the colony
  diameter; conservative flux exchange between grids; per-cell correction at the membrane, the thin layer's near field
  since 2026-10-13).
- Level 4 (built 2026-10-12, docs/plans/level4-extracellular-space.md): extracellular space between cells as volumes,
  each a module; boundary and inner cell groups; matrix production costs carbon and ATP (researched value).
- Level 1 lasts one simulated week; skill prices calibrated by bots.

## Accuracy against the reference (FieldAccuracy.Reference)
The reference has none of the trade-offs: one 1 µm grid over the game grids' whole extent, 64 field steps per macro
step, every input change passed to the cells, cells at rtol 1e-8. The game against it (EnvironmentFieldTests, a 0.1 mM
sea, the creature drifting with the current; depletion = sea - value):

| | 2026-10-12 | 2026-10-13 | bound (test) |
|---|---|---|---|
| one cell: depletion at the membrane | +7% | -0.3% | 2% |
| one cell: depletion the cell sees (its input) | 0 (hidden by the 1% tolerance) | +0.4% | 2% |
| one cell: water 10 µm away, of the membrane's depletion | +11% (1.6x the reference's) | 0.0% | 2% (5-40 µm) |
| 7 cells (60 s): membrane, per cell | not measured | +0.4-0.5% (-1.2 to +1.4% without the fine substeps) | 2% |
| 7 cells: input, per cell | not measured | 0.1-0.5% | 2% |
| 7 cells: water 5-40 µm away | not measured | 0.5-0.6% | 2% |
| uptake | | < 0.01% | 0.1% |

What closed the gaps:
1. **Extent.** The reference covered 60 colony diameters (120 µm for one cell), the game's outermost grid up to 4x that
   (384 µm); in 2D the depletion goes as ln(R/r), so the game was 7% deeper at the membrane and 1.6x 10 µm away. Against a
   reference of the same extent the nested grids were already within 0.5% (steady sink, any position). Now the outermost
   grid spans the extent multiple to one of its cells, and the reference covers the game's extent.
2. **Membrane correction** (checked against the analytic thin-layer solution): see "Membrane" above. Both the game and
   the reference had counted the 2D field below r* twice (1.6-1.8x the true depletion at the membrane) and varied by
   ±9% with the cell's place in its grid cell.
3. **Time scheme:** a drifting cell's own near field lagged with one Douglas step per macro step (-7%); 4 fine substeps
   for glucose bring it to -0.7% (+0.1% at 8; swimming at 20 µm/s: -4% at 4).
4. **Input tolerance:** relative to the input's departure from the sea instead of its value, so a lone cell's own
   0.07% depletion reaches it. Cost (colony-bench, 512 cells, 20 s, 1 thread, field on): 1.06 s (old rule) -> 1.22 s
   (new rule and fine substeps); every change passed (tolerance 0) measured 1.36 s on 2026-10-12.
The fine grid's 8 µm margin needed no change: the coarse flux at its edge costs < 1% of the membrane's depletion.

## To discuss (earlier notes)
- **Food model after the research:** cells at 2 Ga absorb (external digestion), they do not engulf; packets as small
  polymer particles (0.5-2 µm, one dead-cell equivalent) eroded by cell-surface enzymes, the sugar halo diffusing out
  and captured with probability ~a/r. Smaller, faster-disappearing packets. Transporters as evolvable traits (PTS,
  ABC, H+ symport, facilitator; number vs affinity; surface vs secreted enzymes).
- **Grid for 60x the colony:** one cell is 2 µm (field 120 µm); 5,000 cells are ~150 µm across (field ~9 mm). A uniform
  1 µm grid would be ~9,000² at that size. Options: a fixed-size grid whose spacing grows with the colony (cells
  become sub-grid when it is large); or a fine grid around the colony (~1 µm) nested in a coarse one out to 60x.
- **Sublevels:** each level may have a few sublevels (scarcity, abundance, other stresses) before the creature evolves
  on their results; their content structure is not designed yet.
- **Extracellular space at level 4:** a biofilm-matrix colony (boundary and inner cell groups, unsealed shared space,
  slowed diffusion) fits ~1.8 Ga; a sealed regulated interstitial fluid fits animals.
