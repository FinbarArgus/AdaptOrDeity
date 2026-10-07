# Feature plan: the fermenter cell (stage-1 starting organism)

Implements "Game design → Starting organism" and "Physiology modelling" in
[`adapting_plan.md`](../../adapting_plan.md). Work happens upstream in circulatory-autogen-modules
(branch `homogenisation-checks`), then the submodule pin and kernels follow.

## The cell
A ~2 µm prokaryote-grade cell (disc, radius 1 µm, depth 2 µm) in a Proterozoic sea (seawater ions, pH 7.3),
without mitochondria:
- **Metabolism (bond graph):** homolactic fermentation (glucose + 2 ADP + 2 Pi ⇌ 2 lactate + 2 ATP) and a
  maintenance ATPase, common modular rate laws, species stores glucose, lactate, ATP, ADP, Pi; heat =
  enthalpy release; affinities and dissipation as outputs.
- **Membrane potential and ions (prokaryotic set):** membrane capacitor; Na/K concentrations and Nernst
  potentials (SN maths); cytosolic Ca with a fixed buffer (no ER); buffered cytosolic H⁺ (50 mM/pH,
  Slonczewski 1982) with pH and E_H. The F1Fo H⁺-ATPase is the only primary pump (10/3 H⁺ per ATP,
  Jiang 2001; Kobayashi 1985). NhaA (2 H⁺ in per Na⁺ out, Taglicht 1993) exports Na⁺; Trk (channel-like,
  i_leak_K maths) brings K⁺ in and KefC (K⁺/H⁺) exports it, so V sits below E_K; H⁺, Na⁺ and Ca²⁺ leaks;
  Na/Ca exchanger (SN maths; archaeal NCX_Mj is the equivalent). Rest −105 mV, pH 7.6.
- **Membrane carriers:** Glf-type glucose facilitator (Km 4.1 mM) and a lactate⁻/H⁺ symporter, which
  removes the 2 H⁺ per glucose that ATP hydrolysis releases.
- **Rate:** total ATP turnover at steady state = Lynch & Marinov maintenance for the cell volume at 20 °C;
  the H⁺-ATPase takes 25% of it (placeholder). Of the pumped H⁺, 30% return by NhaA, 20% by KefC and
  50% by the leak (placeholders).

## Modules
| Module | Version | New or reused |
|---|---|---|
| `cell/energy_metabolism` | `fermenter_ArgusUNPUBLISHED_v01` | new (bond-graph cytosol) |
| `cell/energy_metabolism` | `mitochondrial_ArgusUNPUBLISHED_v01` | renamed from `ArgusUNPUBLISHED_v01` (later stage) |
| `cell/membrane_potential` | `fermenter_ArgusUNPUBLISHED_v01` | new (capacitor only) |
| `cell/ion_concentrations` | `SN_soma_Argus2026_v01` | reused, new instance `fermenter` |
| `cell/Ca_handling` | `fermenter_ArgusUNPUBLISHED_v01` | new (cytosol + buffer) |
| `cell/H_handling` | `fermenter_ArgusUNPUBLISHED_v01` | new (buffered H⁺, pH, E_H) |
| `cell/ion_channels/i_H_ATPase` | `FoF1_ArgusUNPUBLISHED_v01` | new (F1Fo in hydrolysis mode, thermodynamic brake) |
| `cell/ion_channels/i_NaH` | `NhaA_ArgusUNPUBLISHED_v01` | new (Na⁺/H⁺ antiporter) |
| `cell/ion_channels/i_KH` | `KefC_ArgusUNPUBLISHED_v01` | new (K⁺/H⁺ exchanger) |
| `cell/ion_channels/i_leak_H` | `ArgusUNPUBLISHED_v01` | new |
| `cell/ion_channels/i_leak_Na`, `i_leak_K` | `Argus2026_v01` | reused (i_leak_K as the Trk channel) |
| `cell/ion_channels/i_NaCa` | `Argus2026_v01` | reused, new instance |
| `cell/ion_channels/i_NaK` | `ATP_ArgusUNPUBLISHED_v01` | renamed from `fermenter_…`; eukaryotic, not in this cell |
| `cell/ion_channels/i_leak_Ca` | `ArgusUNPUBLISHED_v01` | new |
| `transport/cell_membrane_face` | `fermenter_ArgusUNPUBLISHED_v01` | new (glucose facilitator + lactate⁻/H⁺ symporter) |
| `cell/prokaryote` | `fermenter` (supermodule) | new: the whole lumped cell |
| `transport/cytosol_face`, `transport/medium_volume` | `ArgusUNPUBLISHED_v01` | new (finite-volume reference only) |

## Steps
1. ~~Write the modules and the supermodule; calibrate so the default point is an exact steady state.~~ Done
   (`tools/build_fermenter_cell.py`, `tools/module_writer.py`, `tools/cellml_writer.py` upstream; the earlier
   cell modules moved to `tools/build_cell_modules.py`). Rest: −105 mV, pH 7.6; turnover 5.5e5 ATP/s;
   heat 9.1 kW/m³; 2,850 H⁺-ATPases per cell.
2. ~~Verification~~ Done: every new version and the whole cell pass cam_testing's full set; all fourteen
   submodules compile to game kernels. The whole cell conserves charge across modules (including H⁺) and
   obeys the second law in every transporter; at sea pH 8.1 it loads Na⁺ (20 → 116 mM in 6 h) and
   depolarises to −80 mV, as the NhaA thermodynamics predict.
3. ~~Review page~~ Done: https://claude.ai/artifact/9hj7fSRpauDueTco2w4ZwC (shows the earlier Na/K-pump cell).
3b. ~~Prokaryotic ion set with protons and pH~~ Done (2026-10-08).
4. Then: finite-volume reference (rings) and the homogenisation test, cell-size sweep, η(Φ).
5. Then: submodule bump, kernels, goldens in AdaptOrDeity.

## Findings to resolve (from building it)
- Resolved: the SN pump law had no thermodynamic brake (ran against the second law at 40 °C); i_NaK
  ATP_ArgusUNPUBLISHED_v01 and the H⁺-ATPase have f_thermo = 1 − exp(−(A − W)/RT). i_NaK Argus2026_v01 has
  the same gap.
- Resolved: the Na/K-ATPase anachronism and the −70 mV rest (now the prokaryotic set at −105 mV).
- Ocean pH matters: above about 7.8 NhaA cannot export Na⁺ at −105 mV (modern seawater is 8.1), so the
  cell loads with Na⁺ in today's ocean unless it evolves a primary Na⁺ pump. The 2 Ga value (7.3) is a
  placeholder between Halevy & Bachan's Archean and Phanerozoic ranges.
- NhaA, KefC and the H⁺ leak use linear flux-force laws (placeholder kinetics, exact thermodynamics);
  NhaA's alkaline activation and KefC's glutathione gating are not modelled. The H⁺-ATPase pH activation
  is a placeholder. The ATP and H⁺ budget shares are placeholders.
- The lactate exporter's kinetics have no prokaryotic source yet.
- The SN Na/Ca exchanger with alpha_NaCa = 1.2 is not exactly thermodynamically consistent (reversal shifted by
  RT/F ln 1.2 ≈ 4.6 mV).
- Only metabolism has a Q10; the transporters and leaks do not, so cooling unbalances ATP.
- The Ca buffer (~1 µs) is the stiffest part; a rapid-buffer approximation is a candidate for the game.
