# Step 14i report: hold to grab, island lessons back, still boards, island facing, Journal card, straight paths

Headset test pending for all of it. Tests 308/308. No build (not asked). Nothing pushed. No scene changes (the scene had unsaved
Editor changes I didn't make): renamed fields (`reFaceAngle`, `moveLessonSeconds`, board `turnAwayAngle`) pick up the new
defaults instead.

## Commits

| Commit | What |
|---|---|
| `29264d3` | A + D: hold to grab the island; the island faces the head (both live in IslandHandle) |
| `bb05d93` | B: the move and two-hands lessons back |
| `0367212` | C: boards face you once, then stay |
| `594eff6` | E: Journal detail card, lodge Journal tablet on the rim |
| `31bccfb` | Fix found on the way: a new game counted the scene's placeholder Kilnpot as caught |
| `437a819` | F: straight paths (Cinderwood), path audit |

## A. Hold to grab
- A pinch or fist on a corner (or the front lip) **arms** the grab: a gold ring fills there over **0.6 s**; full = held (soft
  click, gold bracket). The second hand joins with **0.3 s**. Released early = nothing; > 4 cm away while filling = cancelled.
  Arming, cancelled and held pinches never walk or select.
- Guards: a gaze on something selectable away from the hand as the pinch starts = a look + pinch; a hand **below the island's
  base** never arms; battles and the lodge: corners only (as before).
- How: ISDK's own grab only fires on the frame the pinch starts, so the island's filter refuses it and a filled ring calls
  `HandGrabInteractor.ForceSelect(rim, allowManualRelease: true)`; opening the hand releases it. Logic in `GrabHold` (pure, tested).
- Ghost: `grab_rim_slide` pauses on the pinch while the same gold ring fills, then moves. Controls card: "**Pinch and hold** a corner".

## B. The island lessons: why they were "gone"
- They never left the Full adventure, but **they skipped themselves silently**: move after **12 s** (since step 8b, Oct 25),
  two hands after **15 s** (since 9h). Since **14a (Nov 3)** only corners and the lip grab, while the line still said "Grab its
  rocky edge": grabs on the edge did nothing, the timer ran out and the lessons vanished. The **Quick Journey cut the two-hands
  lesson** entirely (since 9g, Oct 26).
- Now: `lesson.move` "Pinch and hold a corner, then move me wherever is comfy." (chip **Hold a corner**), then
  `lesson.twoHands` "Hold two corners and pull apart to make it bigger." (chip **Two hands**); each waits **45 s**; both slots.
  Old saves are past the opening, so they don't replay.

## C. Boards stop turning
- Menu, Settings, Journal (+ card), Controls, battle moves, lodge card: face the head once when opened, then world-locked. They
  turn again only after the head is **> 30 deg away for 1 s with no hand within 15 cm** (`BoardFacing`). The old drift (up to
  15 deg, continuously) is gone.

## D. The island faces you
- Cause: the idle re-face only kicked in at **35 deg** off, so a small shift after placement (leaning, reclining, the shard's
  glide placed from the head's forward) left it visibly turned until you re-grabbed it.
- Now: placement turns the front along the **line from the island's centre to the head**; idle re-face at **> 6 deg for 1.5 s**
  (smooth; never while held, zoomed in a battle or while a lesson ghost shows). Logs "Facing after placement / release: N deg".

## E. Journal
- Lodge: the book in the middle of the room is gone; a small **Journal tablet on the rim, left of the pods** (lodge only).
- Poke a portrait: a **card beside the book**: turning 3D model (portrait if only seen, silhouette if unseen), name, element
  badge, strong / weak vs, where it lives, evolution line with levels ("???" for forms not met), its moves with elements, your
  best one's level and Caught xN, and one line per Isling (46 lines, `JournalLore`, max 70 characters). Unseen: silhouette,
  "???" and the hint only. Poke it again to close.
- Fix found while testing: a new game counted the scene's placeholder Kilnpot as caught before you picked a starter.

## F. Straight paths

| Map | Turns before / after | Jogs before / after | Notes |
|---|---|---|---|
| Cinderwood | 99 / 34 | 58 / 9 | Regenerated in straight legs; the 9 left: smoke hollow (kept on purpose), junctions |
| Galewick | 36 / 36 | 6 / 6 | Already straight (Oct 27); hits are junction corners and the ridge's narrow ends |
| Lilymere | 27 / 27 | 5 / 5 | Already straight; hits are side-path junctions |
| Still Isle | 22 / 22 | 6 / 6 | The hits are the round statue plaza's edge, not a path |
| Interiors (4) | 0-4 / same | 0 / 0 | Open floors, no paths to straighten |

- Cinderwood's layout check passes (connectivity, the log and the smoke still gate their parts, tall grass 16%). Its
  straight-run limit is now 20 tiles (was 10, written for the old winding trail).
- Swipes along a path that stop on a junction within 3 tiles: Cinderwood 74/361 (20%) before, 95/548 (17%) after. The rest
  stop at real corners, branches and plazas.
- Pictures: `Logs/BattleShots/14i_paths_<map>_before.png` / `_after.png`, springs close-up `14i_paths_springs_compare.png`,
  in game `14i_springs_seat.png`.
- An old save standing on a tile that changed is moved to the realm's start (existing guard).

## Headset test list (in order)
1. **Grab hold:** pinch a corner and keep holding: a gold ring fills (~0.6 s), then it moves. Let go early: nothing, and you
   don't walk. Rest your hand on the armrest and pinch: never grabs. Second hand on another corner: joins fast, resize works.
2. **Lessons (new game, either slot):** after the pinch lesson, "Pinch and hold a corner..." with a ghost that holds while
   the ring fills, then "Hold two corners and pull apart...". Each waits for you (45 s).
3. **Boards:** open the menu and touch the tablets: nothing turns. Turn your head well away for a second: it turns to you.
4. **Facing:** after the island appears, after a recenter and after you let go, its front faces you. Lean a little: within
   ~2 s it eases round. Log: "Facing after placement: N deg".
5. **Journal card:** Journal > poke a caught Isling: a card with its model turning, matchups, line, moves, description.
   Is the text big enough? In the lodge: the Journal tablet left of the pods.
6. **Paths:** Cinderwood, arrival to the springs and on to the log: straight legs, swipes walk whole legs.

## Gotchas
- Bridge: two `exec` commands within a second can drop the first (the bridge reads one cmd file). Space them by 2+ s.
- Bridge `exec` finds methods by name: an overload with the same name throws "Ambiguous match".
