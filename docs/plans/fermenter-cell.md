# Feature plan: the fermenter cell (stage-1 starting organism)

Implements "Game design → Starting organism" and "Physiology modelling" in
[`adapting_plan.md`](../../adapting_plan.md). Work happens upstream in circulatory-autogen-modules
(branch `homogenisation-checks`), then the submodule pin and kernels follow.

## The cell
A ~2 µm prokaryote-grade cell (disc, radius 1 µm, depth 2 µm) in seawater, without mitochondria:
- **Metabolism (bond graph):** homolactic fermentation (glucose + 2 ADP + 2 Pi ⇌ 2 lactate + 2 ATP) and a
  maintenance ATPase, common modular rate laws, species stores glucose, lactate, ATP, ADP, Pi; heat =
  enthalpy release; affinities and dissipation as outputs.
- **Membrane potential and ions (sympathetic-neuron maths):** membrane capacitor; Na/K concentrations and
  Nernst potentials; cytosolic Ca with a fixed buffer (no ER); Na, K and Ca leaks; Na/K pump using ATP
  (1 per cycle); Na/Ca exchanger. Na/K-ATPase and NCX are animal proteins used as functional analogues
  until protons and pH are modelled (bacteria use H⁺-coupled systems; they also rest near −105 mV, which
  needs proton pumping; without it the cell rests near −70 mV).
- **Membrane carriers:** Glf-type glucose facilitator (Km 4.1 mM) and a lactate carrier.
- **Rate:** total ATP turnover at steady state = Lynch & Marinov maintenance for the cell volume at 20 °C;
  the Na/K pump takes ~25% of it (Rolfe & Brown 1997, mammalian).

## Modules
| Module | Version | New or reused |
|---|---|---|
| `cell/energy_metabolism` | `fermenter_ArgusUNPUBLISHED_v01` | new (bond-graph cytosol) |
| `cell/energy_metabolism` | `mitochondrial_ArgusUNPUBLISHED_v01` | renamed from `ArgusUNPUBLISHED_v01` (later stage) |
| `cell/membrane_potential` | `fermenter_ArgusUNPUBLISHED_v01` | new (capacitor only) |
| `cell/ion_concentrations` | `SN_soma_Argus2026_v01` | reused, new instance `fermenter` |
| `cell/Ca_handling` | `fermenter_ArgusUNPUBLISHED_v01` | new (cytosol + buffer) |
| `cell/ion_channels/i_leak_Na`, `i_leak_K` | `Argus2026_v01` | reused, new instances |
| `cell/ion_channels/i_NaCa` | `Argus2026_v01` | reused, new instance |
| `cell/ion_channels/i_NaK` | `fermenter_ArgusUNPUBLISHED_v01` | new (SN maths + ATP use and ATP dependence) |
| `cell/ion_channels/i_leak_Ca` | `ArgusUNPUBLISHED_v01` | new |
| `transport/cell_membrane_face` | `fermenter_ArgusUNPUBLISHED_v01` | new (glucose + lactate carriers) |
| `cell/prokaryote` | `fermenter` (supermodule) | new: the whole lumped cell |
| `transport/cytosol_face`, `transport/medium_volume` | `ArgusUNPUBLISHED_v01` | new (finite-volume reference only) |

## Steps
1. ~~Write the modules and the supermodule; calibrate so the default point is an exact steady state.~~ Done
   (`tools/build_fermenter_cell.py`, `tools/module_writer.py`, `tools/cellml_writer.py` upstream; the earlier
   cell modules moved to `tools/build_cell_modules.py`). Rest: −70 mV; turnover 5.5e5 ATP/s; heat 9.1 kW/m³.
2. ~~Verification~~ Done: every new version and the whole cell pass cam_testing's full set (33 tests, 3 n/a);
   all ten submodules compile to game kernels.
3. ~~Review page~~ Done: https://claude.ai/artifact/9hj7fSRpauDueTco2w4ZwC (awaiting your decisions).
4. Then: finite-volume reference (rings) and the homogenisation test, cell-size sweep, η(Φ).
5. Then: submodule bump, kernels, goldens in AdaptOrDeity.

## Findings to resolve (from building it)
- Resolved: the SN pump law had no thermodynamic brake (ran against the second law at 40 °C); the fermenter
  pump now has f_thermo = 1 − exp(−(A_ATP − W_ion)/RT). i_NaK Argus2026_v01 has the same gap.
- The SN Na/Ca exchanger with alpha_NaCa = 1.2 is not exactly thermodynamically consistent (reversal shifted by
  RT/F ln 1.2 ≈ 4.6 mV).
- Only metabolism has a Q10; the pump and leaks do not, so cooling unbalances ATP.
- The Ca buffer (~1 µs) is the stiffest part; a rapid-buffer approximation is a candidate for the game.
- Without protons the cell rests at −70 mV (E. coli ≈ −105 mV).
- Era check (content/protein_timeline.json): Na⁺/K⁺-ATPase is eukaryotic, not present in a 2.0 Ga
  prokaryote; the lactate carrier uses MCT1 (animal) kinetics. Everything else has prokaryotic equivalents.
