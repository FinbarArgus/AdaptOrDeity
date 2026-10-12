# Scoring: survival, damage and reproduction points; stars; the level-end window

Designer requests of 2026-10-13:
- "I don't want there to be food points, there will be survival points and reproduction points. Adding more food allows
  more proteins but it's the survival that helps the creature evolve. Have a tracking 100/100 which decreases when the
  creature is starving, then convert the end score to points. Do a similar thing for damage."
- "Aim for the cell to starve if left alone without maintenance, enough points to unlock one thing if mediocre player
  adds regulation, 2 if a great player plays, and 3 if we optimised everything with perfect control."
- Stars per level (1 a mediocre player, 2 a great one, 3 perfect control), and a level-end window.

## Built

### Meters and points (FermenterWorld.Scoring.cs)
- **Survival meter** (per level, starts at 100): falls at 100 / (`scoring.starvation_to_empty` level durations) per
  second times the share of the creature's living cells that are **starving**: ATP below `scoring.starving_atp_fraction`
  (0.5) of the cell's resting level. With no living cell the whole creature counts as starving. With the default 1, the
  meter at the end is the share of the level not spent starving.
- **Damage meter** (per level, starts at 100): 100 x (1 - the worst mean damage load of the creature's cells in the level),
  a dead cell counting as a lethal load. Repair does not refill it. A cell at rest carries ~1.1% of a lethal load (its
  repair holds it there), so a healthy level ends at ~98.9.
- **Conversion**: when the level's time runs out each meter converts to points in proportion, rounded half away from
  zero: `scoring.survival_points` and `scoring.damage_points` at 100. **Reproduction points** are unchanged
  (`skills.points_per_offspring` per daughter in the lineage). Setting the level directly (development, tests) scores
  nothing; `DevEndLevel` (the HUD's End button) ends a level as if its time ran out.
- `skills.glucose_per_point` and the food points are gone. `FoodEaten` stays as a statistic (the skill window shows it).

### Stars, achievements, the seen record (FermenterWorld.Achievements.cs)
- **Stars** (0-3): how many of `scoring.star_points_1..3` the level's points (its meters' and the reproduction points
  earned in it) reach. `StarsNow`/`LevelPointsNow` give the running value (the HUD shows it).
- **Achievements** (content/timeline.json `achievements`, reviewed: false), each earned once and kept with its level:
  first meal (first packet touched or held, or food in a vacuole), survived a famine (a gap without food as long as the
  regulation unlock's, then food again while a cell is not starving), never starved (a level ended with a full survival
  meter), first daughter (a creature's cell divides), grandmother (a daughter's daughter), first prey, ten prey.
- **Seen**: per level, the individuals of each kind that came into view (the food window around the creature), each
  counted once per level: food packets, dead cells, prey cells, the creature's daughters, their descendants.
- **The finished level's record** (`LastScore`): meters, points, reproduction points, stars, what was seen, what was
  unlocked during it. Frames carry it all (codec ASN7).

### Unity
- Top bar: Survival and Damage meters (number, coloured bar, tooltip with the points they are worth now, the starving
  share and the time since food), the stars so far, a development End button.
- **Level-end window** (`LevelEndWindow`), opened when a level is scored, before the skill window: the stars, each meter
  with its bar and points, the reproduction points, the achievements earned in the level, what was unlocked during it and
  what the next level brings, and what was seen with a small drawing of each kind and its count.

### The held-food fix found while calibrating
Held packets kept their place in the body frame as they shrank and lost contact with their cell: the cell stopped
digesting food it still held (a level-1 cell starved on day 0.2 holding five packets). Held food now stays against its
cell (FoodField.KeepAgainstCell), so a cell eats all it holds.

## Calibration with scripted players (headless bots)
`ScriptedPlayer` applies a strategy each step (trait targets and controls at the start, regulation switched on when it
unlocks, a swimming policy); `Bots` holds the reference players; `BotOutcome.Play` plays one level;
`dotnet run -c Release --project src/AdaptOrDeity.Headless -- bot left|mediocre|great|NAME key=value ... [food=x]
[level=n]` runs one (AOD_BOT_TRACE=1 prints every simulated hour). One level-1 week takes ~8-12 minutes on one core.
The LevelTests fixtures (LevelOneLeftAlone, LevelOneMediocre, LevelOneGreat, LevelOneOptimised) play each once,
in parallel, each under 10 minutes, and check the stars.

| Player | Strategy |
|---|---|
| left alone | nothing |
| mediocre | regulation on when it unlocks: falling ATP lowers the H+-ATPase and maintenance |
| great | the mediocre regulation; Glf and the surface hydrolase x2; swimming only while without food |
| optimised | the best found by the search below |

### Measurements
(See the tables below; each run is one deterministic random sequence.)
