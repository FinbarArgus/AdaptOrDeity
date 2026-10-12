# Regulation by falling metabolites

Designer's request (2026-10-12): within level 1, once ATP drops below a certain level, unlock regulation that lets the
player turn protein expression or growth down when ATP is low; a cog beside each slider opens a popup choosing which
species' drop lowers it; the function is hardcoded for now, experts will choose it later.

## How it works
- **Upstream** (circulatory-autogen-modules homogenisation-checks ddb5c6e): cell/expression_regulation
  ArgusUNPUBLISHED_v01 gives f_s = x^4/(1 + x^4) per species: x = C/K for ATP, G6P and glucose inside, x = K_H/H_i for
  H+ (f falls as pH falls); K = half the resting level (H+: twice the resting H+). Every protein_pool's target and
  biosynthesis's growth are multiplied by R_reg = prod_s (1 - reg_on_s + reg_on_s f_s); the switches reg_on_s are 0 by
  default, so the cell is bitwise unchanged until the player turns one on. Only targets change, never a flux or a cost.
- **Game**: feature `regulation` (level 1, free, optional, on by default once unlocked), unlocked after the first gap
  without food (designer, 2026-10-13: "Unlock after the first gap of not contacting food"): the first time the creature
  goes `requires_food_gap_s` (60 s, content/timeline.json) without a living cell touching a packet, holding one, or with
  food in its vacuole (`TimeWithoutFood`, `FoodGapMet`, FermenterWorld.Regulation.cs). The gap counts from the start of
  play. A minute is about half the time a cell's ATP takes to run out in a full famine (~2 min, see below), so the
  player can act before the crash. Each
  group keeps a switch per regulated target (each trait, then growth) and species; ApplyControls pushes them to the
  group's cells as `reg_on_<species>_mod_<pool|bio>` (all 0 while the feature is locked or off). Daughters and released
  organisms copy their parent's kernel inputs, switches included. Command `SetRegulation(group, target, species, on)`;
  frames carry `Regulation[RegulationRow(target, species)][group]`; the module-array export includes the switches.
- **UI**: a cog (⚙, with the number of species on) beside every expression slider and the growth slider in the cell
  panel opens a popup with a toggle per species ("Dropping ATP lowers this", G6P, glucose inside, pH) and the function,
  read-only, with a note that experts will choose it. The HUD announces a feature the frame it unlocks.

## What it does (measured)
- Half food (0.05 mM) for an hour, then full food (0.1 mM): unregulated, ATP runs out (< 1e-3 mM) and stays out when
  food returns (the ATP trap: hexokinase needs ATP). With the pumps, exchangers, flagellum and hydrolase regulated by
  ATP, they fall to ~0.38, ATP holds at 4.2 mM and V at -66 mV, and the cell is back at rest within the hour of food
  (ATP 9.53 mM, V -105 mV). Upstream test and RegulationKernelTests.
- No food at all: regulation only delays the collapse (ATP < 0.1 mM at 160 s instead of 130 s with those pools; 220 s
  with every pool, the proteome included, which then cannot be rebuilt). The maintenance ATPase is not regulated.
- Level 1 as tuned (contact rule, field off, ocean 1 µM): a cell left alone eats its packets until day 2.2, then its ATP
  falls from rest to 0 within minutes. Regulation unlocks in that same minute (52.45 h), too late to save that cell, and
  with the pumps regulated from then on the outcome is the same (damage 0.49 by day 7 either way): between packets the
  water holds almost no glucose, which is the no-food case above. Regulation pays off where food is short but not gone.
- Regulating glycolysis or the glucose carriers in a famine removes them within a minute (10 s expression time
  constant), so the cell cannot use food when it returns: the popup and the explanation say so.

## Still to do
- Experts choose the function (n, K, or another form): a new version of cell/expression_regulation per function, and a
  game setting to pick it.
- Show each target's present factor (R_reg) in the popup, and the cell's f per species.
- Whether the maintenance ATPase should also fall in a famine (real cells' maintenance drops); see the plan's open
  question on starvation.
