# Prey, engulfing and the level-2 branch

Designer requests of 2026-10-12: packets with a real density, the surface hydrolase as a slider, prey cells and
engulfing at level 2, and a branch between multicellularity and mitochondria (with reproduction points for daughters
that leave). Built on branch feat/prey-branch.

## Built

### Packets at a real density
- A packet is a dead cell or organic particle. Its amount is (glucose-equivalent density) × (sphere volume of its
  radius): density = cell density × dry share × carbon share of the dry mass / (carbon's molar mass × 6)
  = 1100 × 0.30 × 0.50 / (0.012011 × 6) ≈ **2,290 mol/m³ (2.3 M)** (`GameParameters.PacketDensity`; content
  `body.cell_density` (BioNumbers), `food.dry_mass_fraction` and `food.dry_carbon_fraction` (Neidhardt 1990), medium
  confidence: the game counts all of a dead cell's carbon as glucose).
- Convention: the amount is that of a **sphere** of the packet's radius; contact with a cell uses the **disc** of that
  radius in the plane (the sphere's equator). The radius stays 0.8 µm, so a fresh packet holds 4.9e-15 mol (was 4e-14,
  ~8x less). A packet keeps its density as it is eaten: its radius goes as the cube root of what is left.
- The hydrolase's substrate S (`PolymerConcentration`) is the packet's polymer over its sphere, ~2.2 M, far above
  K_m_poly (20 mM): the enzyme is **saturated** and digestion is limited by the enzyme on the surface, not the
  substrate. That is realistic for a dense particle (surface-limited erosion).
- `food.free_fraction` (3%) is now read as a share of the carbon: ~70 mM of free sugar inside a packet, above the
  50 mM (`food.contact_glucose`) at which hydrolysis stops.

### Surface hydrolase slider
- Trait `Hydrolase_expression` (content/traits/fermenter.json; pool `pool_Hydrolase`, protein `glycoside_hydrolase_gh13`
  added to the timeline), ×0.25–4 of its starting share of the membrane ceiling, like the transporters.
- Cost: what the pool charges (build when expression rises, turnover always) for a 700 aa protein. Length checked
  against UniProt: SusG 692 aa (Q8A1G3, surface lipoprotein), AmyE 659 (P00691, secreted), PulA 1,090 (P07206,
  surface lipoprotein), B. licheniformis amylase 512 (P06278). The upstream value (700) is unchanged and now sourced
  (homogenisation-checks 0db734f, notes only; the game's cell is unchanged). Footprint 20 nm² kept (a 75-80 kDa globular
  protein is ~25 nm² in cross-section).
- Where it acts: on packets the cell touches (into the packet's free sugar, which the membrane facing it takes up;
  environment-field.md) and in vacuoles.

### Prey (`Sim.World.PreyField`)
- Small cells (radius `prey.radius` 0.5 µm; content at the packet density, 1.2e-15 mol) arriving at the window's edge at
  `levels[].arrivals.prey_per_s` (0 at level 1), heading inwards. They swim by run and tumble (`prey.run_speed` 3 µm/s,
  `prey.mean_run` 2 s, a random new heading at each tumble), drift with the current like the creature, and leave at
  the window's edge. Their own random numbers (`prey.seed`), so food is unchanged by them.
- Frames: `PreyCount`, `PreyX/Y` (relative to the creature, ground axes), `PreyRadius`, `PreyHeading`; frames grow
  with the prey in view. Unity: `PreyView` (a violet cell with a dark core and a beating tail behind it).

### Engulfing (feature `engulfing`, level 2, free, optional, on by default; `FermenterWorld.Engulfing.cs`)
- A living cell touching a prey cell or packet smaller than itself takes it into its **vacuole**. A cell has one
  vacuole; what it swallows merges into it, and the vacuole (a sphere at the packet density) must stay smaller than
  the cell. A packet is taken only if room for a prey cell is left beside it, so food never shuts prey out.
- The vacuole is a compartment outside the cytosol: each step the cell's hydrolase digests its polymer
  (V_max S/(K_m + S), S its polymer over its lumen), held back while its free sugar is at `food.contact_glucose` (50 mM,
  product inhibition: the carriers never see more than a packet outside offers), and the cell's own carriers take up its
  free glucose. Its lumen keeps the largest volume its items filled; it is drawn at the size of what it still holds. The share
  of the membrane lining the vacuole, (r_v/R)² as spheres, sees the vacuole's concentration and the rest sees the
  outside; the carriers' uptake splits in the same proportion (as the level-4 sectors do). The vacuole's
  concentration is stepped implicitly in the uptake (its tiny volume would otherwise oscillate). What the cell takes
  from it is food eaten (skill points). Nothing leaves a vacuole to the water; a vacuole emptied below 1% of a prey cell
  is removed (the remainder counted as egested).
- Bookkeeping, tested to 1e-12: engulfed = taken up + egested + still held.
- `Swallowed` counts prey cells only (packets are not organisms). Frames carry it and each cell's vacuole
  (`VacuoleRadius`, `VacuoleX/Y` in the body frame, `VacuolePrey`); Unity draws the vacuole inside the cell (violet
  with prey in it, amber with food only), shrinking as it is digested.
- Science (in the feature's explanation, reviewed: false): phagocytosis is a eukaryotic trait, not documented at
  ~1.9 Ga; the Asgard archaeon makes protrusions but has not been seen to engulf (Imachi 2020); a planctomycete engulfs
  (Shiratori 2019); "phagocytosis early" is debated. The game lets the player explore that route.

### The branch: multicellularity or mitochondria
- `multicellularity` (level 2, cost in content) and `mitochondria` (level 2, cost in content, `requires_swallowed` 10)
  exclude each other (`excludes` in content/timeline.json, mirrored in `GameFeatures`, checked by
  `FeatureTableMatchesTheTimeline`). `FermenterWorld.WhyNotForSale` says why a feature cannot be bought.
- Without multicellularity a daughter leaves at division: it is not added to the creature, `Offspring` counts it, and
  it earns **reproduction points** (`skills.points_per_offspring`), on top of food points. It lives on as a free
  organism in the same water ([free-daughters.md](free-daughters.md)); its daughters, any generation, count and earn
  the same. With multicellularity the
  daughter stays (the colony) and earns none. Frames carry `FoodPoints`, `ReproductionPoints`, `Offspring`.
- Later colony features need multicellularity (`requires`): boundary and inner cells, the extracellular matrix,
  several groups, a fixed body size.
- **Mitochondria do nothing yet**: buying them closes multicellularity and is recorded; respiration inside the cell is
  not modelled ("Planned" in the effect text).
- Unity: the skill window shows what each entry still needs (Swallowed n/10, a required feature), which path it
  closes, and asks before buying an exclusive skill; the points line splits food and reproduction. The HUD shows
  Swallowed and Offspring from level 2, and two development buttons (+5 points, a prey cell swimming at the creature).

- A cell holding a vacuole sees inputs that change every macro step, so it uses the soft restarts of the environment
  field's cells (`StopAtMacroSteps`); with hard restarts libcuflynx's integrator failed a few seconds after a division.

## Calibration (2026-10-12, contact rule, field off; superseded 2026-10-13 by the field's, environment-field.md)
Measured with explicit probes (deleted after); numbers in the content notes and the decision log.

| Run | Result |
|---|---|
| Level 1 left alone, food 3e-3 packets/s | ate 1.69e-13 mol, 8 points, ATP gone on day 4, damage 0.32 by day 7 |
| Level 1 kept on food (0.01 packets/s) | 2.98e-13 mol, 14 points |
| Level 1, other arrival rates (probe, redrawn arrivals) | 1e-4 to 8e-4: ATP gone on day 1; 3e-3: 9.45e-14 mol, day 2.4; 3.5e-3: day 1.5; 4e-3 and more: never |
| Level 2 untouched after an untouched level 1, prey 2e-4/s | 4 swallowed; the cell dies at the end of level 2 (ATP gone since level 1) |
| Level 2 starting fed, untouched, prey 2e-4/s | 7 swallowed (probes with other sequences: 11; with 3 flagella 6) |
| Level 2 kept on food, growth 3, no multicellularity | 5 daughters left in the week |
| Level 2 at the level's food, growth 2, 3 or 10 | ATP gone within a day, no daughter |

Choices: food 3e-3 packets/s and 32 lying packets per view (packets hold ~8x less), `skills.glucose_per_point` 2e-14 mol
(unchanged), prey 2e-4/s, multicellularity and mitochondria 5 points each, 5 reproduction points per daughter that leaves.
At level 2 a player has the level-1 points (8 untouched, 14 fed, less any transporter bought) plus the food eaten there
(14 for a fed cell): both paths are affordable; mitochondria also need the 10 prey.

## Planned
- **Mitochondria's physiology**: an endosymbiont pool inside the cell (its protein, its membranes), aerobic respiration
  where O₂ is available (heme-copper oxidases; the level-4 membrane respiration modules), ATP export to the host, its
  cost, and the membrane-area argument (Lane & Martin 2010; Lynch & Marinov 2017). Upstream first (CellML).
- Prey that flee, die and become packets; prey of several sizes; prey that sense the creature (later levels).
- The vacuole's hydrolase share (now the cell's whole capacity acts both outside and in the vacuole).
