# Unreferenced and unverified values

Values the game uses that have no source, a placeholder source, or a source cited from memory and not yet checked. We go
through these together; when a value gets a checked source, update it where it lives and remove its row here.

## content/parameters.json (placeholder status or unreferenced source)

Generated 2026-10-12 from entries with status "placeholder", source "unreferenced_placeholder", or a note asking for a check.

| key | value | units | source | status | note |
|---|---|---|---|---|---|
| `food.packet_trace_minor_share` | 0.1 | 1 | unreferenced_placeholder | placeholder | other trace species relative to the main one |
| `food.free_fraction` | 0.03 | 1 | neidhardt1990physiology | placeholder | share of a packet already small molecules: free metabolite pool ~3-4% of E. coli dry mass, taken as glucose equivalents (to check) |
| `food.free_leak_rate` | 0.00167 | 1/s | unreferenced_placeholder | placeholder | first-order leak of a packet's free small molecules (~10 min); no source |
| `food.packet_amount` | 4e-14 | mol | unreferenced_placeholder | placeholder | glucose in a fresh food packet: about a quarter of what a 2 um fermenter needs per daughter (1.5e-13 mol) |
| `food.contact_glucose` | 50.0 | mol/m3 | unreferenced_placeholder | placeholder | glucose a cell touching a food packet sees at its membrane (the particle dissolving into the boundary layer), while the packet lasts |
| `food.contact_trace` | 0.01 | mol/m3 | unreferenced_placeholder | placeholder | concentration of a packet's main trace species at a touching membrane |
| `food.packet_radius` | 8e-07 | m | unreferenced_placeholder | design | radius of a fresh packet: smaller than the 1 um cell (user decision 2026-10-09); it shrinks as it is eaten (area in proportion to what is left) |
| `food.carrier_speed` | 1e-06 | m/s | unreferenced_placeholder | placeholder | speed of a creature carrying food (prey, or an ally bringing it) over the ground: 1 um/s, slower than a swimming bacterium (~20 um/s) so it can be met |
| `food.lying_density` | 4400000000.0 | 1/m2 | unreferenced_placeholder | placeholder | food lying on the ground per area: about four packets in the 30 um view (4 / (30 um)^2); a moving creature meets it at density x speed x view width |
| `controls.growth_max` | 50.0 | 1 | lynch2015bioenergetic | placeholder | most ATP a cell may put into growth, in multiples of its resting turnover: growing fermenters run glycolysis 10-40x their maintenance flux; the cell c |
| `controls.expression_min` | 0.25 | 1 | unreferenced_placeholder | design | lowest expression a player can choose (two octaves below the calibrated cell; user decision 2026-10-08) |
| `controls.expression_max` | 4.0 | 1 | unreferenced_placeholder | design | highest expression a player can choose (two octaves above) |
| `environment.current_speed` | 2e-06 | m/s | unreferenced_placeholder | placeholder | drift of the water over the ground, which carries the creature: a slow near-bed current, 2 um/s (a cell moves with its water; over the sea floor the f |
| `motility.mean_run` | 2.0 | s | unreferenced_placeholder | placeholder | mean length of a run with the motor on (random, exponential; E. coli runs ~1 s between tumbles) |
| `motility.mean_pause` | 4.0 | s | unreferenced_placeholder | placeholder | mean pause with the motor off between runs (random, exponential); each run sets off in a random direction |
| `skills.glucose_per_point` | 2e-14 | mol | unreferenced_placeholder | design | glucose eaten from food packets per skill point: calibrated so a level-1 cell left alone for the week earns about 6 points (it ate 1.37e-13 mol) (one  |
| `field.diffusivity.lactate` | 1e-09 | m2/s | unreferenced_placeholder | placeholder | lactate anion in water: ~1.0e-9 m2/s, typical of small organic anions; needs a measured value |
| `field.diffusivity.aspartate` | 8e-10 | m2/s | longsworth1953diffusion | placeholder | amino acids of aspartate's size diffuse at ~0.8-0.9e-9 m2/s at 25 C (glycine 1.06e-9, alanine 0.91e-9); aspartate's own value to check |
| `field.diffusivity.serine` | 8.5e-10 | m2/s | longsworth1953diffusion | placeholder | near alanine's 0.91e-9 m2/s at 25 C; serine's own value to check |
| `field.diffusivity.ribose` | 7.5e-10 | m2/s | longsworth1953diffusion | placeholder | a pentose, a little faster than glucose (6.73e-10 at 25 C); to check |
| `field.diffusivity.dimethylsulfide` | 1.1e-09 | m2/s | saltzman1993dimethylsulfide | placeholder | dimethyl sulfide in water at ~20 C; value to check against the paper |
| `environment.oxygen` | 0.00025 | mol/m3 | planavsky2014low | placeholder | O2 in the surface ocean at ~0.1% of present atmospheric O2 (mid-Proterozoic estimate); 0.25 mol/m3 today at 20 C. Nothing takes it up yet |
| `environment.carbonate_total` | 3.0 | mol/m3 | sheldon2006precambrian | placeholder | dissolved inorganic carbon of the 2 Ga ocean: ~10 PAL CO2 (paleosols ~2.2 Ga) gives ~0.15 mM CO2 and ~3 mM HCO3- at pH 7.3. Sets the buffer that turns |
| `environment.carbonate_pk1` | 5.9 | 1 | millero2010carbonate | placeholder | first carbonic acid dissociation constant (pK1*) in seawater at ~20 C, S 35; to check |
| `field.reference_substeps` | 64 | dimensionless | definitional | design | field steps per macro step in the reference environment (FieldAccuracy.Reference): lambda = D dt / dx^2 about 1.6 for the acid at 1 um and 0.1/64 s; t |

## Cited from memory, to check (marked "sourced" but not looked up)

- Field diffusivities and ocean values added with the nested field (glucose Longsworth 1953, acid as bicarbonate Zeebe 2011,
  O2 Han & Bartels 1996, DMS Saltzman 1993, O2 far field Planavsky 2014, DIC Sheldon 2006, pK1 Millero 2010): the agent wrote
  them from memory.
- docs/research/food-uptake-and-extracellular-space.md: DOIs marked "check" (Parfrey 2011, Betts 2018, Boedeker 2017, Flores
  2006, Stewart 2003, Love 2009, dos Reis 2015, Sebé-Pedrós 2010, Michels 1979, Iversen & Ploug 2010, Leys & Riesgo 2012).

## content/timeline.json

- `levels[].ocean.glucose` = 1 µM: free glucose in open water is nM to low µM; no source yet.
- `levels[].duration_s` for levels 2-7 (a week each): placeholders; level 1's week is the designer's decision.
- `levels[].arrivals` (1e-4 packets/s, 80% carried): calibrated for play (level 1 starts to starve within the week), not sourced.

## Upstream (circulatory-autogen-modules, branch feat/sugar-transporters)

- Carrier turnovers (PTS 200/s, ABC 20/s, GalP 50/s), footprints (12, 25, 12 nm²), K_m_ATP_carrier 0.1 mM: placeholders.
- Km values (PTS 5 µM, ABC 0.5 µM, GalP 0.1 mM) taken from the research notes, not checked against the papers.
- Split glycolysis (commit 670ddfa): dG0_hk -16.7 kJ/mol (textbook, to cite), h_hk 23.8 kJ/mol (to cite), K_ATP_hk 0.1 mM,
  K_G6P_lg 0.1 mM, lower_glycolysis_headroom 300 (gives G6P 0.33 µM at rest; real cells hold mM): placeholders.
- Surface hydrolase (commit 918fab5): kcat 1000 /s (alpha-amylases ~1e3 /s; the 2e3 /s B. licheniformis value uncited),
  K_m_poly 20 mM glucose units (from SusG's 3.1 mM maltoheptaoside, Shipman 1999, converted), length 700 aa, footprint
  20 nm², hydrolysis_capacity 10x resting need (design): no published copy numbers for surface glycosidases found.

## Level 4: extracellular space (upstream branch feat/extracellular-matrix; copies in content/upstream)

- Matrix cost 3 ATP per glucose unit (range 2-4): accounted from the pathway (sugar activation, carrier recycling,
  export); no measured cost per gram of EPS found.
- Maximum matrix production 0.1 x the resting glycolytic flux: placeholder (lactic acid bacteria divert 0.5-3% of sugar).
- Matrix breakdown 1e-6 /s (half-life ~8 days), half returned as glucose: placeholder.
- Densest gel 50 g/L (309 mM glucose units): biofilm EPS 10-100 g/L, low-medium confidence.
- Polymer volume 9.7e-5 m3 per mol glucose units (Mackie-Meares hindrance): medium.
- Diffusivities in the gaps at 20 C (glucose 5.9e-10, lactate 9e-10, free H+ 8e-9, O2 1.75e-9 m2/s): Stewart 2003 scaled,
  medium; they differ from the field's values (see the field rows above): to reconcile.
- Seawater buffer in the volumes 0.3 mM per pH unit (linear): placeholder; the field uses the carbonate buffer.

## Game design values without a physical source

- Skill points per glucose (2.0e-14 mol) and skill costs: calibrated by tests, design values.
- Field accuracy settings: input tolerance 1%, fine grid 1 µm, margin 8 µm, depth 10 µm, 60x extent: design.
