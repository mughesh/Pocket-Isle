# Step 14j report: the old grab back (tighter), nothing overlaps, labels on top, see the explorer

Headset test pending for all of it. Tests 312/312. No build (not asked). Nothing pushed. No scene changes: new defaults come
from renamed fields (`grabOutside` etc., `BattleRunes.runeStep`) and runtime-made components (`ExplorerSeeThrough`).

## Commits

| Commit | What |
|---|---|
| `017c279` | A: hold to grab reverted; plain grab with a tight zone (`GrabZone`); lessons say "Grab a corner" |
| `86ace21` | B: tablet layout rule + check on every board, Journal tabs, Controls row, halo, lodge tablet, flat silhouettes |
| `f2dae72` | C: battle labels above full bounds, on top, dark plate, in view; near label beside the pad; stricter pad fit |
| `f71af67` | D: see the explorer: dithered cut-out round it + gold outline while hidden |

## A. Grab zone, old vs new (island scale 0.6)

| Handle | Outside the glowing line | Inside (over the rock) | Height band |
|---|---|---|---|
| Corner, old | ~4.9 cm (collider shell + 3 cm reach); **~10 cm** in practice: the palm counted during a pinch | ~5 cm | +0.7 cm .. -7.8 cm round the cap, +-3 cm reach |
| Lip, old | ~2.9 cm | 3 cm | under the pods |
| **Corner, new** | **1.2 cm** (world, any island size) | **2.1 cm** | +1.2 cm .. -7.2 cm |
| **Lip, new** | **1.2 cm** | **2.1 cm** | 2 cm under the pods .. -7.2 cm |

- A pinch is judged by its **pinch point only**; the palm counts only for a palm grab (index pinch strength under 0.4).
- What you see is what grabs: the handle lights bright only while the hand is in the zone (`IslandHandle.InGrabZone`).
- Kept: pods win, battles and the lodge corners only, the gaze rule on the lip, the 14i lessons (now "Grab a corner" /
  "Grab two corners and pull apart...", 45 s each, both slots). Gone: the gold ring, hold timers, `GrabHold`.
- Tests: grab on / 1 cm outside the line, nothing at 1.5 / 3 / 10 cm, inward over the rock, never over the play area, the band,
  lip only below the pods and only exploring, two corners at once, the same reach at scale 0.3 / 0.6 / 1.2, pods win.

## B. Nothing overlaps
- **Why the Journal tabs looked overlapped:** their slabs were 1.2 cm apart, but each tablet's halo reached 8 mm (+1.6 mm drop),
  so two halos filled the gap. Halo now 3.5 mm.
- Rule (`TabletLayout`): 8 mm apart at rest, 2 mm while one grows on hover. Every board checks itself in the Editor / dev builds.
- Check run (title, slot choice, menu, Settings, Controls, Credits, Journal + card, battle runes, lodge tablet): **1 hit**,
  the Controls row (5 mm apart): fixed. Battle runes: a step wider (9.7 cm, a looked-at move grows 20%). Now 0 problems.
- Lodge Journal tablet: left of the whole socket band (which already includes a grown pod's reach) + 1 cm.
- Unseen Journal entries: one flat dark slate colour, shape only (`Diorama/Portrait` silhouette mode).

## C. Labels never inside a mesh
- Label top = the actor's drawn bounds now (`StageActor.TryGetVisualBounds`: body, parts, smoke aura, flames, a raised shield).
- Drawn on top (no depth test, late queue) on a dark plate; kept under 86% of the view's height.
- The near Isling's label: beside its pad on the near side, away from the explorer, clear of a wide body (Tidemother's fins).
- **Item 8:** the pad fit only ran once at spawn, before the battle zoom settled. It now re-checks every 0.3 s. A fresh
  measure jittered 20% with the idle animation (the fit pumped 0.47..0.58), so each actor is measured once per spawn.
  Stricter sightline: the wild one shows from 5% of its height up (was its centre less 30%).

| Near form (Lv 15) vs Kettlewing | On-pad scale | Why |
|---|---|---|
| Tidemother | x0.45 (the floor) | 1.3x rule |
| Riverloom | x0.58 | 1.3x rule |
| Skyshiplope | x0.66 | 1.3x rule |
| Samaroak | x0.88 | 1.3x rule |
| Steamswan | x0.98 | 1.3x rule |
| Onsenbara, Zeppelope, Marimoss | x1.00 | fits |
| Pyrecrane | not measured | the bridge battle didn't start (the previous one was still closing) |

- Shots `Logs/BattleShots/14j_labels_riverloom.png`, `_tidemother`, `_keeper_cinderwood`, `_keeper_galewick`,
  `_keeper_lilymere` (low seated head, like your screenshots).

## D. Always see the explorer
- Each frame, the line from the head to the explorer's chest is checked over the map tiles (trees, props, buildings;
  `IslandBuilder.ObstacleTop`). While something is on it, island content between the head and the explorer dissolves in a
  soft dithered circle about 2 tiles across round the explorer (0.2 s in / out), never at or below its feet. The explorer's
  outline turns gold meanwhile.
- Cheap: it lives in the shared window clip every island shader already calls (no extra pass, a few ALU). CPU check
  **0.004 ms**. GPU cost not measured on the device yet (Editor only).
- Shots `14j_explorer_seen_before.png` / `_after.png` (Cinderwood forest), `_close.png` (gold outline).

## Headset test list (grab first)
1. **Grab:** pinch a corner right on the glowing line: it grabs at once. It lights bright when your fingers are in the zone.
   Pinch 3 cm outside the line: nothing. Two corners: resize. Pinch next to a pod: you get the pod.
2. **Lessons (new game):** "Grab a corner, then move me..." then "Grab two corners and pull apart...".
3. **Boards:** menu, Settings > Controls, Journal tabs: clear gaps, nothing touching. Journal unseen = flat dark shapes. Lodge:
   the Journal tablet left of the pods with a gap, also with a full party.
4. **Battle labels:** Riverloom / Tidemother vs a wild one: the wild name and bar above its head on a dark plate; yours beside
   your pad. Keeper fight: its name and bar above the flames / shield, readable.
5. **See-through:** walk into Cinderwood's forest: trees in front of the explorer fade in a circle, gold outline. Walk out:
   they come back.
6. **Perf:** Cinderwood forest at 72 fps with the see-through on (OVR Metrics).

## Gotchas
- The bridge's shot camera is 55 deg; the flat Play camera is wider, so labels clamped to the view sit outside a shot unless the
  hold camera matches (`Step14jDebug.HoldLow` sets 55).
- `GameState.SetPuzzle` reloads the save and brings the title back: the Keeper bridge command unsolves in memory instead.
