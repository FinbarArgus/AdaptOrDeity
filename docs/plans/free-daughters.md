# Free daughters

Designer request of 2026-10-12: "The daughter shouldn't disappear, it should go off on its own with dynamics the same
as ours when it was released." Built on branch feat/free-daughters (from m0-skeleton, after the prey and branch work in
[prey-and-branches.md](prey-and-branches.md)).

## Built

### A released daughter is a free organism (`FermenterWorld.Organisms.cs`, `FreeOrganism`)
- Without multicellularity a division releases the daughter as a **free organism**: its cell is the daughter exactly as
  divided (`FermenterCell.Divide`: states, trait targets, the group's effective energy controls, tolerance and
  `StopAtMacroSteps`), so it carries on from release. A daughter beside its parent in the same water, with the same
  controls, follows its parent's trajectory to the bit (`FreeOrganismTests`).
- Its body is a one-cell `CreatureBody` with the same rigid-body dynamics and the current, and its parent cell's
  flagella at their angles. It runs and tumbles by the creature's rules (`motility.*`) with its **own random numbers**,
  seeded from `motility.seed` and its id. It starts in a pause, at rest in the water, one cell spacing out from the
  colony's centre (behind the body for a single cell), turned as the body is.
- **Nobody controls it.** Its controls stay as released; whether it swims, engulfs and divides by itself is what the
  features were at release (`Swims`, `Engulfs`, `AutoDivides`). The player's later changes reach only the creature.
- Ids from 1, never reused; `Parent` (0 for the creature) and `Generation` (1 for the creature's daughters).

### Same world, same rules
- **Eating** (as a creature's cell, environment-field.md): it sees the free sugar of the packets it touches on the
  membrane facing them, and what it takes through that membrane leaves them. A packet several bodies touch is shared:
  drawn in turn, the creature's cells first, then organisms by id (`FoodField.Draw` never gives more than a packet's free
  sugar). Food stays with the body that met it
  first: `FoodPacket.Holder` (0 the creature, else an organism's id), moved with that body (`FoodField.MoveHeld`) and
  left lying where it is when the organism dies or goes (`FoodField.Release`).
- **Engulfing**, if it was on at release: the creature's rules (prey and packets smaller than the cell, one vacuole that
  must stay smaller than the cell, digestion and uptake). The vacuole code is shared (`Engulf`, `Digest`,
  `VacuoleOnMembrane`, `TakeUp` in `FermenterWorld.Engulfing.cs`).
- **Environment field**: inside the field it reads its membrane and exchanges as a creature's cell does (into
  `CellGlucoseExchanged`, so the field's balance holds to 1e-12); beyond the field it sees the open sea and exchanges
  nothing. The field's outermost grid covers the organisms' range each way (`NestedFieldSettings.Reach`; 136 µm across
  for one cell), so in play every simulated organism is inside it.
- **Its food earns the player nothing**: `Eaten` per organism and `OrganismsEaten` in all; `FoodEaten` and the food
  points are the creature's only. `Swallowed` likewise counts only the creature's prey.
- **Death**: at a lethal damage load. At the end of the step it dies in, it leaves `Organisms` and **its body becomes
  food where it lies** (designer request 2026-10-13, "dead daughters should not be removed, they should be able to be
  eaten"; `FermenterWorld.Die`, `FoodField.AddBody`, `FreeOrganism.Remains`). See "Dead bodies" below.
- **Division**: if it was released with growth and automatic division on, it divides by itself when its daughter is
  built, and the daughter is released the same way, behind it, with what it had. **Every daughter in the lineage is
  offspring** (designer request 2026-10-13, "we should get points from daughters daughters as well"): `Offspring`
  counts the creature's own daughters and every free organism's, any generation, and each earns
  `skills.points_per_offspring`; `OwnOffspring` is the creature's own (generation 1) among them. `Daughters` per
  organism; `OrganismsReleased` (equal to `Offspring`) in all.

### Dead bodies
- A dead organism's body is a food packet of kind `PacketKind.Body`: a sphere of the cell's radius (1 µm) at the
  packets' density (`food.*`: 2.29 M glucose equivalents, all of a cell's carbon), **9.6e-15 mol**, about twice a
  fresh packet (4.9e-15 mol), with the packets' free share (`food.free_fraction`, 3%), **plus what its vacuole held**
  (polymer and free sugar as they were: engulfed food is not lost at death). The cell's simulated pools (glucose,
  glycogen, ATP) are not added: the packet density already counts all of a cell's carbon, its small molecules
  included, so adding them would count them twice. `DeadCellAmount` is the cell's part.
- It smells of serine (lysed cells). It drifts with the water it died in (the current at its death); held by a cell
  it moves with that cell's body like other food, and let go (its holder died or left) it drifts again. Packets the
  organism held lie where they are.
- **It stays until it is eaten**: no timed removal (`organisms.dead_kept` is retired). Its radius goes as the cube root
  of what is left, from its own size (`FoodPacket.Amount`, `FullRadius`), and it is used up below
  `food.used_up_fraction` of its own amount.
- **Who eats it, by the packets' rules**: its free sugar leaks into the field; touching cells' hydrolase cuts its polymer
  into its free sugar and the membrane facing it takes that up (in turn: the creature's cells first, then organisms by
  id). Untouched, only its free part leaks: its polymer stays. **Engulfing** needs an item smaller than the cell: a fresh body is as large as a cell, so
  no cell (all are the same size) engulfs it whole. Eaten from outside to ~87% (radius ~0.96 of a cell), it fits in an
  empty vacuole with room left for a prey cell and an engulfing cell swallows the rest (`FreeOrganismTests`).
- **Range**: a body stays while it is within the organisms' range (`organisms.range_view_widths`, 2 view widths, 60 µm
  along either ground axis) of the creature, though other food leaves the field at the edge of the 30 µm window.
  Beyond it the body is no longer simulated (`FoodField.BodiesGone`); that is not a timed removal but it is a removal
  (open question). With the creature drifting in the same water a body it does not swim away from stays in range.
- At the cap a daughter replaces the farthest living organism: dead ones are food, not organisms. Frames carry only
  living organisms (`OrganismAlive` is gone) and mark bodies among the packets (`PacketBody`); Unity draws a body as a
  grey dead cell with a pale outline, shrinking as it is eaten; its tooltip says "Dead cell".

### Bounds and cost
- **Range**: an organism farther than `organisms.range_view_widths` = **2 view widths** (60 µm, the view being the
  30 µm food window) from the creature along either ground axis is no longer simulated (`OrganismsGone`). Beyond the
  window there are no packets, so out there it would only starve; the camera shows at most ~1.5 view widths.
- **Cap**: at most `organisms.max` = **32**. A new daughter beyond it replaces the first dead organism, else the one
  farthest from the creature (counted as gone).
- Measured (one thread unless said; 600 s simulated, field off, food 0.05 packets/s, after a 10 s warm-up):

  | Organisms | Calm (not swimming) | Swimming |
  |---|---|---|
  | 0 (the creature alone) | 47 µs per simulated s | 70 µs |
  | 8 | 260 µs | 410 µs (2 still in range after 40 s) |
  | 32 | 1,030 µs; 537 µs on 8 workers | 1,640 µs (13 still in range after 40 s) |

  About 30 µs per calm organism and ~80 µs per swimming one per simulated second, as the creature's cells. At the cap
  and 1000× a calm population costs ~1 s of one core per real second (~0.5 s on 8 workers): the game keeps up. Swimmers
  cost more but leave: a daughter with the starting flagellum swims ~110 µm in 2 minutes, so most leave the range
  within a minute.
- Each organism's motion and integration run on the workers; shared things (packets, prey, the field, counters) are
  settled in id order, so results are bit-identical for any number of threads (tested with 1, 4 and 8).

### Frames and Unity
- Frames carry the organisms in id order, growing like cells and packets (`organisms.initial_capacity` 8):
  `OrganismId`, `OrganismGeneration`, `OrganismX/Y` (relative to the creature, ground axes), `OrganismOrientation`,
  `OrganismAlive`, `OrganismMotorOn`, `OrganismFlagella` and `OrganismFlagellumAngles`, `OrganismVacuoleRadius`, every
  overlay channel (`OrganismChannels`), the counters, and the details of one inspected organism
  (`SimCommands.InspectOrganism(id)`, `OrganismDetails`). The codec's header has the organism capacity (magic "ASN5"; `PacketBody`, `OwnOffspring`).
- Unity: `OrganismView` draws each like the creature's cells but paler with a white outline (grey when dead), growing
  as it builds a daughter, coloured by the overlay shown, with its flagella beating while its motors run.
  Clicking one opens `OrganismPanel` (read-only, top left: its state, overlay values and every cell-panel detail by
  group); hovering shows what it is; the HUD's "Free n" counts them (tooltip: released, died, gone) and "Offspring n"'s
  tooltip splits the creature's own daughters from their descendants (as does the skill window's points line).

## Open questions
- A dead body beyond the organisms' range (60 µm) is dropped like an organism that leaves: should far bodies be kept
  (unsimulated) for when the creature comes back, or is dropping them out of sight acceptable?
- The lineage earns points without bound: a large free population of auto-dividing descendants may out-earn food.
  Diminishing points per generation, or a cap? (Not measured yet.)
- Free organisms cannot meet or engulf one another or the creature (no contact between bodies); should a larger
  organism engulf a smaller one later?
