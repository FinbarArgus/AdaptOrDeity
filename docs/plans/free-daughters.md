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
- **Eating** (contact rule): it sees the packets it touches, and what it takes beyond the background leaves them. A
  packet several organisms touch is shared as colony cells share it: withdrawn in turn, the creature's cells first, then
  organisms by id (`FoodField.Withdraw` never gives more than a packet holds). Food stays with the body that met it
  first: `FoodPacket.Holder` (0 the creature, else an organism's id), moved with that body (`FoodField.MoveHeld`) and
  left lying where it is when the organism dies or goes (`FoodField.Release`).
- **Engulfing**, if it was on at release: the creature's rules (prey and packets smaller than the cell, one vacuole that
  must stay smaller than the cell, digestion and uptake). The vacuole code is shared (`Engulf`, `Digest`,
  `VacuoleOnMembrane`, `TakeUp` in `FermenterWorld.Engulfing.cs`).
- **Environment field**: inside the field it reads its membrane and exchanges as a creature's cell does (into
  `CellGlucoseExchanged`, so the field's balance holds to 1e-12); beyond the field it sees the open sea and exchanges
  nothing. With the game's grids the field (~380 µm across for one cell) reaches beyond the range, so in play every
  simulated organism is inside it.
- **Its food earns the player nothing**: `Eaten` per organism and `OrganismsEaten` in all; `FoodEaten` and the food
  points are the creature's only. `Swallowed` likewise counts only the creature's prey.
- **Death**: at a lethal damage load. A dead organism drifts with the water, drawn grey, for `organisms.dead_kept`
  (600 s), then is removed. It does not become food (open question).
- **Division**: if it was released with growth and automatic division on, it divides by itself when its daughter is
  built, and the daughter is released the same way, behind it, with what it had. **Its daughters are its offspring, not
  the player's**: `Offspring` and the reproduction points count only the creature's own daughters (`Daughters` per
  organism, `OrganismsReleased` in all). Counting the lineage is an open question.

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
  (`SimCommands.InspectOrganism(id)`, `OrganismDetails`). The codec's header has the organism capacity (magic "ASN3").
- Unity: `OrganismView` draws each like the creature's cells but paler with a white outline (grey when dead), growing
  as it builds a daughter, coloured by the overlay shown, with its flagella beating while its motors run.
  Clicking one opens `OrganismPanel` (read-only, top left: its state, overlay values and every cell-panel detail by
  group); hovering shows what it is; the HUD's "Free n" counts the living ones (tooltip: released, died, gone).

## Open questions
- Should a dead organism become a food packet (it is one: a dead cell)? Then death conserves its carbon.
- Should the lineage's daughters count for reproduction points (fitness is the lineage's), or only the creature's own?
- Free organisms cannot meet or engulf one another or the creature (no contact between bodies); should a larger
  organism engulf a smaller one later?
