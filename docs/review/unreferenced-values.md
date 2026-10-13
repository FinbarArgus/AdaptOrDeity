# Unreferenced and unverified values

Values the game uses that have no source, a placeholder source, or a source cited from memory and not yet checked. We go
through these together; when a value gets a checked source, update it where it lives and remove its row here.

## content/parameters.json (placeholder status or unreferenced source)

Generated 2026-10-12 from entries with status "placeholder", source "unreferenced_placeholder", or a note asking for a check.

| key | value | units | source | status | note |
|---|---|---|---|---|---|
| `food.packet_trace_minor_share` | 0.1 | 1 | unreferenced_placeholder | placeholder | other trace species relative to the main one |
| `food.free_fraction` | 0.03 | 1 | neidhardt1990physiology | placeholder | share of a packet's glucose equivalents already small molecules: a bacterium's pool of free metabolites is ~3-4% of its dry mass (E. coli), taken as t |
| `food.free_leak_rate` | 0.00166667 | 1/s | unreferenced_placeholder | placeholder | rate the free small molecules leak out of a packet (a lysed cell): first order, ~10 min time constant; no source yet. With the field always on (2026-10-13) it decides how much of a touched packet's sugar the cell gets rather than the water |
| `food.contact_glucose` | 50 | mol/m3 | unreferenced_placeholder | placeholder | free sugar in a packet (or a vacuole) at which enzymes stop cutting its polymer (product inhibition): the most the membrane facing it sees (2026-10-13) |
| `food.contact_trace` | 0.01 | mol/m3 | unreferenced_placeholder | placeholder | a packet's main trace per its free sugar (per food.contact_glucose): 2e-4 mol of trace with each mol of glucose it leaks |
| `food.packet_radius` | 8e-07 | m | unreferenced_placeholder | design | radius of a fresh packet (a dead cell or organic particle): smaller than the 1 um cell (user decision 2026-10-09), within the 0.5-2 um particles of do |
| `food.carrier_speed` | 1e-06 | m/s | unreferenced_placeholder | placeholder | speed of a creature carrying food (prey, or an ally bringing it) over the ground: 1 um/s, slower than a swimming bacterium (~20 um/s) so it can be met |
| `food.lying_density` | 3.52e+10 | 1/m2 | unreferenced_placeholder | placeholder | food lying on the ground per area when the field is first filled: about 32 packets in the 30 um view (32 / (30 um)^2), 8x the earlier 4 as packets hol |
| `controls.growth_max` | 50 | 1 | lynch2015bioenergetic | placeholder | most ATP a cell may put into growth, in multiples of its resting turnover: growing fermenters run glycolysis 10-40x their maintenance flux; the cell c |
| `controls.expression_min` | 0.25 | 1 | unreferenced_placeholder | design | lowest expression a player can choose (two octaves below the calibrated cell; user decision 2026-10-08) |
| `controls.expression_max` | 4 | 1 | unreferenced_placeholder | design | highest expression a player can choose (two octaves above) |
| `environment.current_speed` | 2e-06 | m/s | unreferenced_placeholder | placeholder | drift of the water over the ground, which carries the creature: a slow near-bed current, 2 um/s (a cell moves with its water; over the sea floor the f |
| `motility.mean_run` | 2 | s | unreferenced_placeholder | placeholder | mean length of a run with the motor on (random, exponential; E. coli runs ~1 s between tumbles) |
| `motility.mean_pause` | 4 | s | unreferenced_placeholder | placeholder | mean pause with the motor off between runs (random, exponential); each run sets off in a random direction |
| `scoring.survival_points` | 5 | 1 | unreferenced_placeholder | design | skill points the survival meter converts to at 100/100 when a level ends (designer 2026-10-13); calibrated with the scripted players (docs/plans/scoring.md) |
| `scoring.damage_points` | 5 | 1 | unreferenced_placeholder | design | skill points the damage meter converts to at 100/100 |
| `scoring.starving_atp_fraction` | 0.5 | 1 | unreferenced_placeholder | design | a living cell is starving while its ATP is below this fraction of its resting level |
| `scoring.starvation_to_empty` | 1 | 1 | unreferenced_placeholder | design | starvation of the whole creature that empties the survival meter, in level durations |
| `scoring.star_points_1` | 5 | 1 | unreferenced_placeholder | design | a level's points for one star (a mediocre player; set from the scripted players) |
| `scoring.star_points_2` | 10 | 1 | unreferenced_placeholder | design | points for two stars (a great player) |
| `scoring.star_points_3` | 15 | 1 | unreferenced_placeholder | design | points for three stars (the best strategy found) |
| `skills.points_per_offspring` | 1 | 1 | unreferenced_placeholder | design | reproduction points: skill points per daughter released in the creature's lineage (its own daughters at division without multicellularity, and its free organisms' daughters, any generation |
| `field.diffusivity.lactate` | 1e-09 | m2/s | unreferenced_placeholder | placeholder | lactate anion in water: ~1.0e-9 m2/s, typical of small organic anions; needs a measured value |
| `field.diffusivity.aspartate` | 8e-10 | m2/s | longsworth1953diffusion | placeholder | amino acids of aspartate's size diffuse at ~0.8-0.9e-9 m2/s at 25 C (glycine 1.06e-9, alanine 0.91e-9); aspartate's own value to check |
| `field.diffusivity.serine` | 8.5e-10 | m2/s | longsworth1953diffusion | placeholder | near alanine's 0.91e-9 m2/s at 25 C; serine's own value to check |
| `field.diffusivity.ribose` | 7.5e-10 | m2/s | longsworth1953diffusion | placeholder | a pentose, a little faster than glucose (6.73e-10 at 25 C); to check |
| `field.diffusivity.dimethylsulfide` | 1.1e-09 | m2/s | saltzman1993dimethylsulfide | placeholder | dimethyl sulfide in water at ~20 C; value to check against the paper |
| `environment.oxygen` | 0.00025 | mol/m3 | planavsky2014low | placeholder | O2 in the surface ocean at ~0.1% of present atmospheric O2 (mid-Proterozoic estimate); 0.25 mol/m3 today at 20 C. Nothing takes it up yet |
| `environment.carbonate_total` | 3 | mol/m3 | sheldon2006precambrian | placeholder | dissolved inorganic carbon of the 2 Ga ocean: ~10 PAL CO2 (paleosols ~2.2 Ga) gives ~0.15 mM CO2 and ~3 mM HCO3- at pH 7.3. Sets the buffer that turns |
| `environment.carbonate_pk1` | 5.9 | 1 | millero2010carbonate | placeholder | first carbonic acid dissociation constant (pK1*) in seawater at ~20 C, S 35; to check |
| `prey.radius` | 5e-07 | m | unreferenced_placeholder | design | radius of a prey cell: half the creature's 1 um cell, so a cell can engulf it (marine bacteria are often 0.2-1 um across). Its content is a sphere of  |
| `prey.run_speed` | 3e-06 | m/s | unreferenced_placeholder | design | swimming speed of prey over the ground during a run: 3 um/s, slower than swimming bacteria (20-30 um/s) so the creature can meet them |
| `prey.mean_run` | 2 | s | unreferenced_placeholder | placeholder | mean run before a tumble (exponential); each tumble sets a new heading at random. E. coli runs ~1 s |

## Cited from memory, to check (marked "sourced" but not looked up)

- Field diffusivities and ocean values added with the nested field (glucose Longsworth 1953, acid as bicarbonate Zeebe 2011,
  O2 Han & Bartels 1996, DMS Saltzman 1993, O2 far field Planavsky 2014, DIC Sheldon 2006, pK1 Millero 2010): the agent wrote
  them from memory.
- docs/research/food-uptake-and-extracellular-space.md: DOIs marked "check" (Parfrey 2011, Betts 2018, Boedeker 2017, Flores
  2006, Stewart 2003, Love 2009, dos Reis 2015, Sebé-Pedrós 2010, Michels 1979, Iversen & Ploug 2010, Leys & Riesgo 2012).

## content/timeline.json

- `levels[].ocean.glucose` = 1 µM: free glucose in open water is nM to low µM; no source yet.
- `levels[].duration_s` for levels 2-7 (a week each): placeholders; level 1's week is the designer's decision.
- `levels[].arrivals.food_per_s` (3e-3 packets/s since packets hold 4.9e-15 mol; 80% carried): calibrated for play (level 1
  starts to starve within the week), not sourced. The outcome is sensitive to it and to the random sequence: in probes 3e-3 to 3.5e-3
  ran out of ATP on days 1.5-4, 4e-3 never (gaps between packets decide, see the plan's decision log).
- `levels[].arrivals.prey_per_s` (0 at level 1, see the decision log for level 2's value): calibrated for play, not sourced.
- Feature costs: multicellularity and mitochondria (points), mitochondria's `requires_swallowed` 10 (designer's number).
- Regulation (`features[regulation]`): unlocked after the first gap of `requires_food_gap_s` (30 s) without food (design,
  designer's "after the first gap of not contacting food"; ATP falls below half at 57 s and to 0.1 mM at 126 s of a full
  famine);
  free (no points). Its explanation cites Potrykus & Cashel 2008 and
  Schneider, Gaal & Gourse 2002 (looked up, not read in full).

## Upstream (circulatory-autogen-modules homogenisation-checks a12d84a: feat/sugar-transporters and feat/extracellular-matrix merged; the game's cell since 2026-10-12)

- Carrier turnovers (PTS 200/s, ABC 20/s, GalP 50/s), footprints (12, 25, 12 nm²), K_m_ATP_carrier 0.1 mM: placeholders.
- Km values (PTS 5 µM, ABC 0.5 µM, GalP 0.1 mM) taken from the research notes, not checked against the papers.
- Split glycolysis (commit 670ddfa): dG0_hk -16.7 kJ/mol (textbook, to cite), h_hk 23.8 kJ/mol (to cite), K_ATP_hk 0.1 mM,
  lower_glycolysis_headroom 300: placeholders.
- G6P at rest (commit f0aedae): G6P_i 1 mM is a design operating point (E. coli chemostats hold ~3.5 mM, Chassagnole 2002);
  n_G6P_lg 4, the lower glycolysis's Hill coefficient, is PFK-1's cooperativity cited from memory (to check); K_G6P_lg, and
  the matrix's K_G6P_mx and dG0_mx, are derived from these placeholders (G6P_i, n_G6P_lg, the headroom), not measured.
- Flagellum's membrane share (commit f0aedae): footprint_Flagellum 1700 nm² (the motor's disc out to its stators; the MotA
  stator width behind it is unchecked).
- Surface hydrolase (commit 918fab5): kcat 1000 /s (alpha-amylases ~1e3 /s; the 2e3 /s B. licheniformis value uncited),
  K_m_poly 20 mM glucose units (from SusG's 3.1 mM maltoheptaoside, Shipman 1999, converted), footprint 20 nm² (a
  75-80 kDa globular protein is ~25 nm² in cross-section; estimate, commit 0db734f), hydrolysis_capacity 10x resting need
  (design): no published copy numbers for surface glycosidases found. Its length (700 aa) is now sourced from UniProt
  (SusG 692 aa; 0db734f).

- Expression regulation (homogenisation-checks, cell/expression_regulation, normalised 2026-10-13): f = 1/(1 + u^n),
  u = max(0, 1 - C/C_rest)/(1 - 0.5), n = 4 (`regulation_hill_n`), half-way at 0.5 x the resting level
  (`regulation_K_fraction`; for H+ the resting H+ / 0.5, pH 0.3 below rest): exactly 1 at and above rest, 1/17 at C = 0.
  The designer's hardcoded choice (design), not fitted to ppGpp or rrn promoter data; experts will choose others.
- Protein turnover (2026-10-13): k_deg 1%/h is sourced (Maurizi 1992, BNID 109924), but the same rate for every protein
  is a simplification (a short-lived fraction of 1-7% has half-lives of 13 min to 1 h; starvation raises bulk degradation
  to ~5%/h, Willetts 1967: not modelled), and dilution by growth, the main way growing bacteria lower a protein (Gupta et
  al. 2024), is not modelled. The rise's time constant tau = t_half_mRNA / ln 2 (433 s) is a modelling choice (the mRNA's
  mean lifetime), not a measured induction time.
- Protein synthesis capacity (2026-10-13): build_capacity 9.0 x the resting turnover is derived from k_elong 5 aa/s at 20 C
  (an Arrhenius extrapolation of Farewell & Neidhardt 1998's 7.5 aa/s at 28 C and 12.9 at 37 C: medium confidence),
  ribosome_active_fraction 0.8 (Dai et al. 2016, growing cells) and ribosomes_per_um3 7,000 (placeholder), with c_syn 24
  (placeholder). Replacement synthesis (about 2% of the capacity) is not counted against it.
- Maintenance regulation (2026-10-13): protein synthesis, RNA and lipid upkeep, repair and the maintenance ATPase each
  take the regulation switches (off by default). Cutting the upkeep or the maintenance ATPase has no modelled downside
  (the mRNA, ribosome and lipid counts are fixed; the maintenance ATPase is everything not itemised): a design question.
- Regulated proteolysis and amino-acid recycling (homogenisation-checks 4e84568, 2026-10-13):
  - `t_half_proteolysis` 5 min (tau_prot = 433 s) at 20 C: placeholder at the fast end of the measured range. At 37 C
    sigma32 lasts under 1 min (Herman 1995) and RpoS ~2 min (Zhou & Gottesman 1998); ssrA-tagged LacI lasts 8 min at 37 C
    and 38 min at 25 C (Purcell 2012). At 20 C that is ~3 min to over an hour. It applies to every protein the cell lowers,
    as if each carried a degron; bacteria mostly lower transporters by dilution in growth.
  - `proteolysis_capacity` 5%/h of the resting cell's protein (Q_prot_max 8.1e4 amino acids/s): Willetts 1967's bulk
    breakdown in starving cells, taken as the capacity of regulated proteolysis (placeholder). It makes halving the
    glycolytic enzymes take about an hour.
  - `aa_reuse_K` 1 mM: recycled amino acids at which half of a new protein's amino acids come from them. Estimated from
    feedback-inhibition constants (~0.1 mM each, from memory) over 20 amino acids; unreferenced.
  - `tau_aa_catabolism` 30 min: unreferenced.
  - `y_aa_catabolism` 0.5 ATP per amino acid catabolised: unreferenced. Arginine deiminase gives 1 ATP per arginine, and
    serine, threonine and alanine ~1 through acetate; most others give none without respiration (from memory).
  - `c_deg` 1 ATP per amino acid is now cited (Kenniston 2003: ~1 ATP per residue translocated by ClpXP), but the
    unfolding cost (10 to over 500 ATP per substrate) is not counted separately.

## Level 4: extracellular space (upstream homogenisation-checks a12d84a; copies in content/upstream while that branch is local)

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

- Survival and damage points per level, star thresholds, reproduction points per offspring, and skill costs: calibrated
  with the scripted players (docs/plans/scoring.md), design values.
- Achievements (content/timeline.json achievements[]): their texts are reviewed: false; the famine one uses the
  regulation gap.
- Prey (content prey.*): radius 0.5 µm, run speed 3 µm/s, mean run 2 s: design, chosen so prey can be met and engulfed.
- Engulfing: the vacuole's membrane share (r_v/R)², the cell's whole hydrolase acting in the vacuole as well as outside, and
  the vacuole kept at the packet density as it is digested: game conventions (docs/plans/prey-and-branches.md).
- Packet density counts all of a dead cell's carbon (protein, nucleic acid, lipid) as glucose equivalents digestible by the
  sugar hydrolase: a simplification (medium-low confidence).
- Membrane shares in the game (content/traits/fermenter.json): new carriers (PTS, ABC, H+ symport) start at 0 and their
  sliders run from 0 to the room the other proteins leave (share range 0-1 of the ceiling); expression multipliers stay
  x0.25-4 of the starting share: design.
- A food packet's polymer concentration for the surface hydrolase (FermenterWorld.PolymerConcentration): its polymer over
  a sphere of its radius, ~2.2 M at the packet density, so the hydrolase is saturated (surface-limited digestion).
- Field accuracy settings: input tolerance 1%, fine grid 1 µm, margin 8 µm, depth 10 µm, 60x extent: design.
