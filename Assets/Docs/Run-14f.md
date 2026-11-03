# Step 14f report: stone-tablet buttons, the simple menu, readable text, four fixes

Headset test pending for all of it. Tests 273/273. No build (not asked).
Setup already run and saved: **Tools > Pocket Isle > Setup > First Minute (14) > Set Up MR_Main (14f menus and fixes)**.

## Commits

| Commit | What |
|---|---|
| `29f4c03` | A: walk lesson, pinches on hand moments, the Heart's forehead crystal |
| `d242ea8` | B + C + A2: stone tablets, the simple menu, the readable text rule |
| (docs) | this report, handoff, GameDesign, QA guide, the rule's constant set a hair lower |

B, C and A2 went in one commit: they touch the same files, and the tablets are sized by the text rule.

## A. Four fixes

| Fix | What changed |
|---|---|
| 1. "Swipe twice" lesson could trap players | The last walk lesson is **"Swipe to walk, tap to stop."** (chip "Swipe, tap"; ghost = one swipe, then the tap). Done by any of: a tap that stops a walk, walking onto the gold tile ahead, or two finished swipe walks. Passes itself after 30 s. Swiping twice still walks on and stays on the Controls card. Old saves in that lesson resume into the new one (same id). |
| 2. Tiny text | See the text rule below |
| 3. Untangling Galeowl walked the explorer | While the untangle runs, the explorer is **held still** (like a battle, but hand moments still work). Everywhere: a look + pinch that starts within **8 cm** of an active piece, or while any piece is held, never walks (`HandPuzzle.ClaimsPinch`, one rule for every realm's pieces: their home, focus point, gold-marked parts, knot ends) |
| 4. Spirit into the Heart looked like overlapping meshes | A **gold crystal** in the forehead, dull until the Spirit comes. The lantern dissolves into a bright gold mote (0.3 s), which flies into the crystal. The crystal ignites: glow, a ring of light, a heartbeat. No lantern mesh is ever at the forehead. The Spark later comes out of the crystal. |

Flat checks (all passed):
- Lesson: a tap stop passes it. The gold tile passes it. Two finished walks pass it (a blocked swipe doesn't count). 30 s with nothing passes it. Two quick swipes still walk on.
- Untangle: the explorer is held, and released when the knots are done. A pinch 3 cm from a knot is claimed; 20 cm away walks.
- Finale: the shots below.

## A2. The text rule

**One rule (`TextRule`):** cap height ≥ **14 mm with the island at its default scale (0.6)**, about 1.5° at the default viewing distance. Text grows with the island. To shrink every board at once, change one constant: `TextRule.MinCapWorld`.

What it means in practice:
- Under the island: TMP font ≥ 0.34.
- In world space: ≥ 0.21.
- Baked letters (realm cards, gate signs): capitals ≥ 23 mm island-local.

**Interpretation call:** I read the rule literally: 14 mm in the world at the default island size. That made boards about 3× bigger, so most boards were re-laid out. If they feel too big on the headset, lower `MinCapWorld` (12 mm ≈ 1.3°).

### Size table: cap height in the world, island at default scale (mm)

| Element | Before | After | How |
|---|---|---|---|
| Title runes (Continue, New journey, Settings) | 3.4-4.7 | 14 (Continue 15.7) | tablets fit their labels; Continue on top, its journey under it |
| Rune sub-labels ("Quick journey" under Continue) | 2.9 | 14 | full size second line |
| Game menu runes | 3.8-4.3 | 14 (Resume 15.7) | new layout (C) |
| Settings row names | 3.8 | 14 | shortened (below) |
| Settings choices, pebble "off", bottom row | 1.9-3.4 | 14 | |
| Settings title | 5.0 | 14 | |
| Controls card runes / line | 3.8 / 3.7 | 14 / 14 | 3 runes over 2; line wraps to two lines |
| Credits card (settings) | 4.7 | 14 | five lines at once (scrolls as before) |
| Quick Journey end runes | 3.4 | 14 | a column over the statue |
| Keeper rematch (Spar? / Not now) | 3.4 | 14 | |
| Battle move runes / Run / caption | 3.2 / 4.5 / 4.9 | 14 | 2 x 2 grid |
| Battle HP labels | 3.6 | 14 | level only ("Lv 8") |
| Damage / effect pops | 5.4-7.4 | 14 | effect line one line higher |
| Bubble: objective line | 9.6 | 14 | wrap width grows with it |
| Bubble: chips, counters, chatter | 8.2-8.3 | 14 | |
| Pod card (name / info, HP / next) | 8.3 / 5.2 / 4.4 | 14 / 14 / dropped | card drawn 1.7x |
| Lodge Isling card | 2.5-4.1 | 14 | restacked: name, level, HP, XP bar, 3 moves, stats |
| Lodge page arrow "1/3 >" | 5.0 | 14 | dark outline |
| Finale Hall of Fame cards | 2.9 | 14 | one at a time |
| Finale credits tablets | 9.1 | 14 | |
| Finale collection card | 6.2 | 14 | |
| Realm name card: name (baked) | ~13-16 | ≥ 14 | scale floored at the rule |
| Realm name card: line | 1.9-4.1 (auto-shrunk) | 14 | moved under the card, dark outline |
| Hub gate signs (baked) | ~7-9 (estimate) | ≥ 14 | scale floored at the rule |
| Dev panel, dev readouts, gesture test labels | unchanged | | dev-only, exempt |

Measured with `Step14fDebug.TextAudit` (every visible TextMeshPro, smallest cap) and the shots. The "after" tablet labels measure 23.2 mm island-local (13.9 mm world). Afterwards I set the rule's constant a hair lower so they land at ≥ 14.0.

### Shortened or dropped to fit

- **Settings rows:** Dominant hand → **Hand**. Spirit hints → **Hints**. Touch tiles to walk → **Touch walk**. Recenter island → **Recenter**.
- **Title:** "Hold to start over" → **"Hold to restart"**.
- **Controls card lines:**
  - Walk: "Swipe your thumb along your finger".
  - Keep going: "Swipe twice: keep walking. Tap: stop".
  - Go there: "Look at a spot and pinch".
  - Pick up: "Pinch things to pick them up".
  - Move isle: "Grab a corner: move. Two hands: turn, size".
- **Realm card lines:**
  - Galewick: "Where the wind stopped" / "The wind blows again".
  - Cinderwood: "Where the fire went cold" / "The fire burns again".
  - Lilymere: "Where the water froze" / "The water flows again".
  - Still Isle: "Home" (unchanged).
- **Dropped:**
  - the battle rune's second line ("Water", "Support"). The element now shows as the tablet's inlay colour.
  - the creature name over the HP bars (the caption and lines name them).
  - the "→ next level" label on the pod card and the Lodge card (the XP bar stays).
- **Hall of Fame:** the cards show one at a time (the newest Isling's), not all six at once (they'd overlap at this size).

## B. The tablets

- **Look:** one shared procedural mesh (`TabletKit`):
  - rounded slab with a bevel;
  - moss lumps on the top edge;
  - an inlay groove round the face (gold on the primary);
  - a dark rim behind it (the outline, gold when looked at);
  - a halo: soft dark shadow plus a faint warm glow.
- **Drawing:** `Diorama/Tablet` stretches the mesh to each tablet's size, so corners and moss never distort. It uses its own toon light, so tablets look the same in any room.
- **Instanced:** all tablets on a board share one material, one draw. Labels are TMP as before.
- **States:**

  | State | Look |
  |---|---|
  | Looked at | grows 10%, gold rim, label brightens |
  | Pressed | sinks 4 mm, stone tap (`step.stone`) |
  | Disabled | grey and dim, can't be picked |
  | Primary | larger, gold inlay |
  | Lit toggle / volume pebble | coloured inlay |

- **Battle runes:** the move's element colour on the inlay.

### Contrast (measured from the shots)

| Board | Label on slab | Slab edge vs what's behind (bright / mid / dark) |
|---|---|---|
| Title | 6.4:1 | 3.9 / 3.9 / 3.9 |
| Game menu | 6.4:1 | 3.9 / 2.6* / 3.9 |
| Settings | 6.4:1 | 2.7 / 2.6 / 2.6 (inside the dark settings card) |
| Battle | 6.4:1 | 4.0 / 3.2 / 3.6 |

- **Labels:** 6.4:1 everywhere (target 4.5:1).
- **Edges:** the strongest feature within 10 px of the slab (dark rim or warm halo) against what lies just beyond. Target 3:1 (the WCAG non-text guideline).
- **Below 3:1:**
  - \* the QA-only Dev tablet over grass;
  - tablets on the dark settings card (that card's light border separates it from the room).

Shots are in `Logs/BattleShots/`:
- `14f_buttons_<bright|mid|dark>_<title|menu|settings|battle>.png`, each with a `.txt` of tablet rectangles;
- `14f_buttons_mid_controls.png`;
- the finale: `14f_crystal_dark.png`, `14f_crystal_dark_close.png`, `14f_mote_dissolve.png`, `14f_mote_flight.png`, `14f_crystal_ignite.png`, `14f_crystal_lit_close.png`, `14f_spark_out.png`.

## C. The simple menu

**Game menu:**

| Position | Tablet |
|---|---|
| Top middle | **Resume** (large, gold inlay) |
| Bottom row | Controls · Settings · Title |
| Top right corner | small speaker tablet (music on / off; replaces the Music On / Off rune) |
| Top left corner | Dev (Editor and QA builds only) |

**Title:** Continue (large, gold, its journey under it) on top. New journey · Settings below. "or: other journey" under those when both saves exist.

## Headset test list (in order)

1. Title: tablets readable from your seat? Mossy stone, gold rim on the one you look at. Pinch: it sinks with a tap.
2. Settings and Controls from the title: everything readable? Is the board too big or too tall?
3. Continue, then open the menu. Resume is big and gold. The speaker toggles music. Controls, Settings, Title work.
4. A wild battle: the 2 x 2 move grid. Readable? Does it cover your creature?
5. The bubble's text is bigger. Still comfortable, not too wide?
6. Pod card (look at a pod), and the Lodge card if you have sleepers.
7. New game (or the Dev panel lessons): "Swipe to walk, tap to stop" passes on a tap, the gold tile, or two walks.
8. Galewick tower: untangle Galeowl with pinches. The explorer never moves.
9. Finale (Dev > Story > Finale from the goodbye):
   - The crystal is dull, then lights when the Spirit flies in as a gold mote.
   - The lantern is never in the forehead.
   - The Spark leaves from the crystal.
10. Realm name card (enter a realm): the line under the card. Hub gate signs: readable, not overlapping?

## Open items

- **Pre-existing:** some words render slanted, like italics ("Body *Slam*", "*Title*"; also the finale's "For *everything*" in step 13). Not from a font file (the fallback font is Liberation Sans Regular). It hits whole words, not letters. I didn't chase it this step.
- Gate signs and the realm name card weren't shot (no gate in view at the saved spot). Their sizes come from the rule.
- The Lodge card wasn't shown (no sleepers in the save).
- `RuneBoard.fontSize` and `BattleRunes.runeSize / spacing / fontSize / captionFontSize` are unused now (kept as serialized fields, with tooltips saying so).
