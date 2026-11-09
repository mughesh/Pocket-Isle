# Run 27 (Nov 9): fixes from the first outside playtest

## Read this first
- **Flat Editor only, nobody has worn it.** Doors, the stop practice and the pinch icon were only driven by the bridge. A real pinch, poke, swipe and tap need the headset.
- The Galewick tower had no tall grass within 1.5 tiles, so there was nothing to cut. See section D.

| Check | Result |
|---|---|
| Tests | 396/396 (19 new in `Step27Tests`) |
| Smoke run | 84 ok, 0 failed, 1 known note (opening wait) |
| Release Check | **PASS** |
| Door check | Look + pinch from 4 tiles away walked the explorer into the tower, forge and lake temple |

## A. Doors: done
- `DoorEntry` (tower, forge, lake temple): look + pinch or a fingertip on the door walks the explorer up and in, through the door's usual rules (Go in / Not yet, "not yet" lines). No way there = a line, never silence.
- The lodge door already had look + pinch; a fingertip on the arch now leaves too. Open halls were not changed.
- Signifier when the explorer is within 4 tiles, or the door's Keeper isn't calmed yet: portal 40% taller and brighter (intensity 1.5 to 2.7), gold running along the ground into the doorway, and a chip **beside** the door: "Walk in, or look at the door and pinch".
- Lines: "The tower is open! Walk in.", "The temple is open! Walk in.", and a new "The forge is open! Walk in." once the forge door has finished opening.
- **No ghost hand was attached to any door**, so none was removed. The nearest ones are the shrine's palm over the flame (a real gesture) and the hand moments near a door. If you still see a hand on a door, tell me which door and when.

| | Before | After |
|---|---|---|
| Tower | ![](Run-27-img/before_tower.png) | ![](Run-27-img/after_tower.png) |
| Forge | ![](Run-27-img/before_forge.png) | ![](Run-27-img/after_forge.png) |
| Lake temple | ![](Run-27-img/before_temple.png) | ![](Run-27-img/after_temple.png) |

## B. Journal: done
Opens on the realm you're in (a Keeper's hall counts as its realm). On the Still Isle or in the lodge: the tab with the most Islings found, and on a tie or none found, Wind.

## C. Stop practice: done
- New lesson `practice`, after the swipe-and-tap lesson: gold tiles 3, 4, then 5 along the path. A tap that stops on or next to the tile counts: burst, "Nice stop!", dots fill, the last says "Perfect!". Walking past puts a new one ahead. Passes after 3 stops or 60 s; Quick Journey asks for 2.
- Chip: "Swipe to walk · Tap to stop". Ghost: the `thumb_tap` clip, whose gold dot pulses on the tap.
- **Swipe ghost:** not redone. To record it: Tools > Pocket Isle > Ghost Hands > Record Ghost Clip, over Link. That code wasn't changed; I did not run a recording.

## D. Grass near doors: done, smaller than the prompt expected

| Map | Tiles changed |
|---|---|
| Galewick | c23 r8, c24 r8 (the gate to the Still Isle) |
| All others | none |

- **The tower, forge, lake temple and Still Isle gates had no tall grass or tall prop within 1.5 tiles.** Only Galewick's gate did, and those two tiles are gone. So I couldn't reproduce grass hiding the tower's portal in the map data: it may just have looked low and small from the seat, which the taller portal addresses. Please check it on the headset.
- I also tried clearing a wide wedge toward the seat (10 Galewick tiles). It dropped the shortest trail's arrival level below its tested range (7.63, needs 7.7), so I cut it back to 2 rows. Wheat patches at c13-15 r7 and c12-13 r8 near the tower stay.
- `Step27Setup.ClearDoorGrass` edits the saved map only. Re-running the Galewick generator would bring the tiles back.

| | Before | After |
|---|---|---|
| Tower from the side | ![](Run-27-img/before_tower_side.png) | ![](Run-27-img/after_tower_side.png) |
| Wind gate | ![](Run-27-img/before_gate_wind.png) | ![](Run-27-img/after_gate_wind.png) |

## E. Steadier text: done

| Value | Old | New |
|---|---|---|
| Spirit glide | 0.5 s | 0.6 s |
| Least time parked | none (moved per line) | 5 s |
| Parked spot follows the head | yes, every frame | no, only if the target moves 6 cm or more |
| Time a line is kept when a more important one waits | from when it started showing | 1.2 s + 0.05 s per character, after typing finishes |
| Idle hint gap | 120 s | 240 s |
| Filler | always | every second one, none in lessons, hand moments, battles or the first 3 minutes |

- Battle, lesson spots and the finale still move the Spirit at once.
- **Filler count in the Quick Journey smoke run: 0 before, 0 after.** That run skips through lines too fast for filler or idle hints to fire, so it proves nothing. The rule is covered by tests, and how it feels needs the headset.
- Idle hints now keep their reading time too (they were replaced instantly).

## F. Gaze: done
Everything using `FocusHighlight.Hover`: outline more than one-third wider (scale 1.45 to 2.0) and warm white, a soft glow, a pinch icon (two fingertips and a gold dot) above the target, and one quiet tick on arrival (once per target, at most every 0.35 s; `rune.hover` at 0.35 volume). The icon moves off the target and the explorer, and fades when a fingertip is within 8 cm. Gold is unchanged.

| Before | After |
|---|---|
| ![](Run-27-img/before_gaze_door.png) | ![](Run-27-img/after_gaze_door.png) |

## One thing to know
`Docs/Story/Script_v1.md` is out of date against the dialogue asset: it's missing lines added to the asset directly (for example `battle.lose.retry`). **Don't run Dialogue > Import Script_v1**, it would delete them. I added the new lines to both by hand.

## Headset test list (play order)
| # | Where | Check |
|---|---|---|
| 1 | Walk lesson, then the practice | Swipe, tap on the gold: burst, "Nice stop!", dots. Is 3 too many? Does the tap ghost read? |
| 2 | Walk past a gold tile | A new one appears ahead, no scolding |
| 3 | Any bubble | The Spirit stays put between lines; text comes up in the same place |
| 4 | Look at a pod, shrine, gate | Thicker warm outline, soft glow, pinch icon, one tick. Does the icon get in the way? |
| 5 | Open the Journal in Galewick, then on the Still Isle | Wind page; then the most-found page |
| 6 | Tower opens | "The tower is open! Walk in." Portal tall and bright, gold to the door, chip beside it |
| 7 | Look at the tower door and pinch, from a few tiles away | The explorer walks up and goes in |
| 8 | Poke the door with a fingertip | Same. **Does a hand near the island trigger it by accident?** |
| 9 | Forge and lake temple | Same as 6 to 8 |
| 10 | Lodge door | Poke the arch: leaves. Hands resting near it don't |
| 11 | A whole Quick Journey | Fewer Spirit interruptions. Too quiet? |
