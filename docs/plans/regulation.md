# Regulation by falling metabolites

Designer's request (2026-10-12): within level 1, once ATP drops below a certain level, unlock regulation that lets the
player turn protein expression or growth down when ATP is low; a cog beside each slider opens a popup choosing which
species' drop lowers it; the function is hardcoded for now, experts will choose it later.

## How it works
- **Upstream** (circulatory-autogen-modules homogenisation-checks): cell/expression_regulation ArgusUNPUBLISHED_v01
  gives f_s = 1/(1 + u^4), u = max(0, 1 - x)/(1 - 1/2), x = C/C_rest for ATP, G6P and glucose inside and x = H_rest/H_i
  for H+: f = 1 exactly at and above the resting level (no effect at rest), 1/2 at half of it (H+: twice the resting H+,
  0.3 pH below), 1/17 at zero, smooth (C^3) at rest. Every protein_pool's target, biosynthesis's growth, and four parts
  of the maintenance are each multiplied by R = prod_s (1 - reg_on_s + reg_on_s f_s), each with switches of its own:
  protein synthesis (protein_expression: its capacity and every pool's replacement of degraded copies), RNA and lipid
  upkeep and repair (biosynthesis, `reg_on_<s>_upkeep`, `reg_on_<s>_repair`), and the maintenance ATPase (cytosol). All
  switches are 0 by default, so the cell is unchanged until the player turns one on.
- **Protein turnover is realistic** (2026-10-13): a protein is degraded at k_deg about 1%/h and replaced up to its target;
  it rises towards a higher target with its mRNA's mean lifetime (~7 min) within the ribosomes' capacity (~9x the
  resting turnover at 20 C, throttled as ATP falls). Copies above a lowered target, set by the player or by regulation,
  are removed by ATP-dependent proteolysis with a 5 min half-life, within the proteases' capacity (5%/h of the cell's
  protein). Their amino acids go to a recycled pool that later building draws on first. In a famine the proteolysis
  stops once ATP is spent. Regulating the maintenance acts at once.
- **Game**: feature `regulation` (level 1, free, optional, on by default once unlocked), unlocked after the first gap
  without food (designer, 2026-10-13: "Unlock after the first gap of not contacting food"): the first time the creature
  goes `requires_food_gap_s` (content/timeline.json) without a living cell touching a packet, holding one, or with food
  in its vacuole (`TimeWithoutFood`, `FoodGapMet`, FermenterWorld.Regulation.cs). The gap counts from the start of play;
  its value is set against the famine reserve (docs/plans/scoring.md). Each
  group keeps a switch per regulated target (each trait, growth, then the maintenance: `FermenterCell.MaintenanceRegulation`)
  and species; ApplyControls pushes them to the group's cells as `reg_on_<species><suffix>_mod_<submodule>` (all 0 while
  the feature is locked or off). Daughters and released organisms copy their parent's kernel inputs, switches included.
  Command `SetRegulation(group, target, species, on)`; frames carry `Regulation[RegulationRow(target, species)][group]`;
  the module-array export includes the switches.
- **UI**: a cog (⚙, with the number of species on) beside every expression slider, the growth slider and each part of a
  "Maintenance" block (texts reviewed: false) opens a popup with a toggle per species ("Dropping ATP lowers this", G6P,
  glucose inside, pH) and the function, read-only, with a note that experts will choose it. The HUD announces a feature
  the frame it unlocks.

## What it does (measured, 2026-10-13)
- Half food (0.05 mM) for an hour, then full food (0.1 mM): unregulated, ATP runs out (< 1e-3 mM) and stays out when
  food returns (the ATP trap: hexokinase needs ATP). Regulating the pumps, exchangers, flagellum and hydrolase by ATP no
  longer helps (their copies fall ~0.1% in the hour). With the maintenance regulated by ATP, ATP holds at 4.2 mM and V at
  -100 mV; when food returns the cell first rebuilds the ~0.5% of its proteome lost while synthesis was cut (throttled as
  ATP is low), so after an hour ATP is 4.7 mM and V -101 mV. Upstream test and RegulationKernelTests.
- No food at all: ATP < 0.1 mM at ~100 s unregulated, ~170 s with the maintenance (and pumps and growth) regulated.

## Still to do
- Experts choose the function (n, K, or another form): a new version of cell/expression_regulation per function, and a
  game setting to pick it.
- Show each target's present factor (R_reg) in the popup, and the cell's f per species.
- Cutting the RNA/lipid upkeep or the maintenance ATPase has no modelled downside (design question); starvation-raised
  degradation (~5%/h) and dilution by growth are not modelled.
