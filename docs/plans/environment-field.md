# The environment field

Chemicals around the creature on a grid that follows it: diffusion with implicit steps tied to the cells' exchange,
cells taking up and releasing species, food particles releasing into it, the ocean of the epoch at its edges.
Background: docs/plans/parallel-and-gpu.md section 4; research: docs/research/food-uptake-and-extracellular-space.md.

## Decided (2026-10-12)
- **Species (8):** glucose, lactate, H+ (pH), O2 and the four trace species (smells). Na+, K+ and Ca2+ stay at seawater
  values. Cells exchange glucose (`J_glc_e`), lactate (`J_lac_e`) and H+ (`J_H_lac`) now.
- **Size:** the field grows with the colony, multiplicatively: 60 times the colony's diameter.
- **Food:** a packet is a dissolving particle that releases glucose and its traces into the field until used up (not
  the earlier "spreading Gaussian"), pending the discussion below.
- **Arrivals:** what appears per second (food now; prey, enemies and friends later) is constant for each level and
  region, set in content/timeline.json to make food scarce or abundant. Done for food.

## To discuss
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
