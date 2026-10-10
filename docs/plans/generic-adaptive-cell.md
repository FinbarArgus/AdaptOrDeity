# Feature plan: generic adaptive cell, parameters.json, math into CellML

Implements the user's direction of 2026-10-08: every physiological parameter lives in a parameters.json with its
source (nothing hardcoded), the math that can be CellML is CellML, and cells are built from a hierarchy of supermodules
in circulatory-autogen-modules. Part of M3 in [`adapting_plan.md`](../../adapting_plan.md).

## Decisions (user, 2026-10-08)
- **Two parameter files.** Upstream (open): `modules/cell/generic_adaptive_cell/parameters.json` holds every
  physiological parameter of the cell modules (value, units, source, status, note); `tools/build_adaptive_cell.py`
  reads it and writes the module and supermodule instance CSVs. Game (closed): `content/parameters.json` holds the
  game-side values (food packets, colony, control ranges, display), also sourced; `Game.Content` embeds and loads it.
- **A hierarchy of supermodules, one per cell type.** Module type `cell/generic_adaptive_cell`, versions:
  - `empty_membrane`: the base with every general mechanism and no transport proteins.
  - base cells adding the proteins a whole class shares (later: `animal_cell_base` with the Na⁺/K⁺-ATPase, NCX, …).
  - cell types (`fermenter` now): a base plus their own proteins.
  A version extends another by adding submodules and links; the builder merges them into a flat supermodule
  (libcuflynx has no nested supermodules), so every level is a verified supermodule in its own right.

## Design (as built)
- **Parameters.** The builder reads every input with `par(key)`; a CSV row holding a JSON value takes its source from
  the JSON, a value derived from JSON entries (`der(...)`) names its rule and inputs, and the build reports any row that
  is neither (none). Supermodule rows are generated from the submodules' own CSVs (`tools/supermodule_writer.py`: every
  constant no port connection supplies), plus the overrides a cell sets for library modules.
- **Proteins as pools** (`cell/protein_pool ArgusUNPUBLISHED_v01`, one instance per protein): expression m approaches
  m_target with τ; upkeep n L (c_poly + c_deg + c_recycle) k_deg f_ATP / N_A; a rise asks r_up n₀ L (c_syn + c_poly) / N_A
  and is served the share s_build; falling needs no ATP; M_build is the daughter share; A_protein = n₀ footprint m.
  Ports: `protein_expression` (m, to a transporter), `enzyme_expression` (m, to the cytosol's V_gly),
  `protein_synthesis` (to protein_expression), `daughter_share` (M_build, summed in biosynthesis).
- **Protein synthesis** (`protein_expression adaptive`): sums the pools (as `*_in`, named as totals), serves
  s_build = min(1, J_cap/J_demand) with J_cap = build_capacity J_ref throttle, throttle = clamp((ATP − floor)/(ATP_ref −
  floor)), takes their ATP from the cytosol, and gives the protein area limit (area_max_fraction of the surface).
- **Transporters scale with expression**: i_H_ATPase, i_NaH, i_KH and the membrane face (Glf, LacT) gain m inputs;
  `i_leak_K` and `i_NaCa` get `adaptive_ArgusUNPUBLISHED_v01` copies of the library versions with density × m.
- **Cytosol** outputs its ATP budget by reaction and the glycogen's ATP (3 per unit, from its stoichiometry).
- **Biosynthesis** throttles growth itself (growth_level × J_ref × throttle), sums the daughter cost and outputs
  growth and glycogen progress.
- **What stays in C#:** events (division with glycogen top-up, death), feature gating, the targets and controls the
  player sets (clamped to the membrane room the kernel reports), the food field, numerics, display.

### libcuflynx port rules learned
- Ports match by type between a sender's exit/general ports and a receiver's entrance/general ports; two linked modules
  that share a port type are wired together. Hence `enzyme_expression` for the cytosol (the membrane face, linked to
  it, has `protein_expression`) and `daughter_share` straight from pools to biosynthesis (synthesis and biosynthesis
  both take ATP and membrane area, so they are not linked).
- An unconnected entrance-port variable gets no parameter value, and a variable that a multi-port passes on is treated
  as connected: so expression and ATP inputs are general ports, and the synthesis harness includes a cytosol.
- An open sum port is 0, not its CSV value. A component name must be unique across the library (libcuflynx finds
  components by name), so the adaptive copies are renamed.
- Variables summed by libcuflynx live in `multiport_sum_*` components: a receiving module names them (`J_keep_total =
  J_keep_in`) so outputs and kernel names stay readable.

## Steps
1. Upstream `parameters.json` for the current cell; builder reads it; game `content/parameters.json` and a loader. ✅
2. `protein_pool` module, adaptive transporter inputs, cytosol flux outputs, biosynthesis sums, synthesis. ✅
3. `generic_adaptive_cell/empty_membrane` and `generic_adaptive_cell/fermenter` (extends), replacing
   `cell/prokaryote/fermenter`; full cam_testing verification of every version. ✅ (all 17 versions pass)
4. Re-assemble the kernel; `FermenterCell` reduced to targets, controls, events and reads; tests; Unity. ✅
5. Update the hardcoded-math review page: what moved, what remains and why. ✅
   ([`docs/reviews/2026-10-08-hardcoded-math.html`](../reviews/2026-10-08-hardcoded-math.html))

Next: an `animal_cell_base` extending `empty_membrane`; sources for the 80 placeholders in parameters.json.
