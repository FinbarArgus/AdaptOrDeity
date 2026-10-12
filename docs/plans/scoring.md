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
| mediocre | regulation on when it unlocks: falling ATP lowers the H+-ATPase and all four parts of the maintenance |
| great | the mediocre regulation; swimming to gather a store (on while it holds less than two fresh packets' worth) |
| optimised | provisional: the great player's, Glf and the surface hydrolase x2, pumps, exchangers and flagella regulated too |

The four LevelOne fixtures are `[Explicit]` until the content is calibrated: the designer is changing protein expression
again (fast regulated synthesis and proteolysis), and the calibration waits for that cell.

## Calibration: provisional measurements (2026-10-13)
One deterministic random sequence per run (the level's seeds); outcomes hang on when the first long famine comes, so a
change anywhere moves them. Points use `scoring.survival_points` = `damage_points` = 5 (placeholders).

**Before the turnover merge** (old expression dynamics; held-food fix in): level 1 at 2e-3 / 3e-3 packets/s, ten
strategies each (pumps regulated, all pumps regulated, swimming never / only without food, lean expression x0.5, Glf and
hydrolase x2, repair 0.5, gathering): every one crashed at its first long famine, on day 0.24-0.52 at 2e-3 and day
0.5-2.6 at 3e-3. Regulation unlocked seconds before the crash and changed nothing: in a full famine (ocean 1 µM) a resting
cell's ATP falls below half at 57 s and to 0.1 mM at 126 s whatever is regulated.

**After the turnover merge** (realistic turnover, maintenance cog), ocean 1 µM:

| Food [packets/s] | left alone | mediocre | great (Glf, hydrolase x2, swim without food) | regulation + gathering |
|---|---|---|---|---|
| 2e-3 | starving from day 0.53, 2 points | same as left alone | day 0.52, 2 | day 0.53, 2 |
| 3e-3 | day 0.81, 3 | same | day 2.19, 5 | day 1.94, 3 |
| 4e-3 | day 1.63, 3 | same | day 2.02, 3 | 0.53 d starving, 10 |
| 5e-3 | day 2.68, 5 | same | day 2.53, 5 | never starved, 10 |

In a full famine regulating the maintenance only delays the crash (ATP to 0.1 mM at 190 s instead of 124 s), so with a
1 µM ocean a mediocre player is a cell left alone. On half food it avoids the ATP trap (regulation.md), so the open
water's glucose decides whether regulation pays:

| Ocean glucose, food | left alone | mediocre | regulation + gathering |
|---|---|---|---|
| 0.02 mM, 3e-3 | day 2.23, 5 | same as left alone | day 0.89, 3 |
| 0.02 mM, 5e-3 | day 3.40, 5 | same | never, 10 |
| 0.035 mM, 3e-3 | day 2.52, 5 | 0.29 d starving, survives: 10 | 0.34 d, 10 |
| 0.035 mM, 5e-3 | never, 10 | never, 10 | never, 10 |
| 0.05 mM, 3e-3 | day 0.81, 3 | 0.45 d starving, survives: 10 | 0.52 d, 10 |
| 0.05 mM, 5e-3 | never, 10 | never, 10 | 0.16 d, 10 |

**Provisional direction** for the calibration after the expression change: level 1 with ~0.05 mM of glucose in the open
water and ~3e-3 packets/s makes the left-alone cell starve (day 0.8) and the mediocre one survive; the points per meter
then need to reach 15 at full meters (three stars), and the survival meter to tell 0.5 d of starving from none
(`scoring.starvation_to_empty` well below 1, e.g. 0.25 of a level), so that great and optimised players earn more than
the mediocre one. The optimised player is to come from a search over the strategy (swimming policy and store,
expression, which targets regulation lowers, repair) once the cell is final. Level 2 (5e-3, prey 2e-4/s, from a fresh
level-2 world): left alone survives the week (10 points, 6 prey); growth 1 with engulfing leaves one daughter (11
points, 7 prey); growth 1 or 3 without engulfing starves (touching packets cannot feed growth through the ~5% face);
growth 3 with engulfing starves on day 0.4.

## Still to do
- The calibration above, on the next expression model; then the LevelOne fixtures lose `[Explicit]`.
- Level 2's reference players.
