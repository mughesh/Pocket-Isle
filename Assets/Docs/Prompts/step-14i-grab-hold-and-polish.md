# Step 14i: hold-to-grab island, the grab lessons back, board stability, island facing, Journal details, straight paths

Decided Nov 4 evening (design chat) from Mugesh's headset notes. Orient first if you're a fresh session (`CLAUDE.md`,
`.claude-handoff.md` newest on top, `Run-14h.md`, commit `9986441`). Commit per section "(headset test pending)", no AI
attribution, don't push. Mugesh tests between sections. Screenshot: `Assets/Docs/Notes-img/crooked-paths-nov4.png`.

## A. Island grab: short hold + guards (the resting-hand problem)
Mugesh rests his hand on the armrest, pinches (to walk, to select) and the hand is near a corner, so the island grabs.
1. **Hold to grab:** a pinch (or fist) at a corner handle or the front lip **arms** the grab: a gold ring fills around
   that handle over **0.6 s** (tunable `grabHoldSeconds`); full = the island is held (a soft click + the frame lights).
   Released before it fills = nothing happens to the island (that pinch goes nowhere; it must not also walk).
   Moving the hand > 4 cm away while arming cancels it.
2. **Two hands:** the second hand joins with a **0.3 s** hold on another handle while the first is held (resize/turn).
3. **Guards:** (a) if the gaze is on a selectable thing (tile, pod, piece, rune) when the pinch starts, it's a look +
   pinch for that thing, never an arm; (b) a hand **below the island's base bottom** (resting low, on an armrest) never
   arms a handle; (c) during battles and the lodge only the corners arm (as today).
4. Every place that teaches or shows grabbing the island (lessons, chips, Controls card, QA guide) now says **"Pinch
   and hold a corner"**; the ghost hand pinches, **holds while a gold ring fills**, then moves the island.
5. Tests: arm/cancel/grab timing, the guards, two-hand join, no walk on a cancelled arm.

## B. Bring back the island lessons
6. Mugesh: the tutorial for moving and resizing the island seems gone. Find **why and since when** (log it in the
   handoff), then restore in **both slots**: `lesson.move` ("Pinch and hold a corner, then move me wherever is comfy.",
   chip "Hold a corner") right after the pinch lesson, and `lesson.twoHands` ("Hold two corners and pull apart to make
   it bigger.", chip "Two hands") right after it. Ghosts use A's hold. Old saves past these lessons don't replay them.

## C. Boards stop turning while you touch them
7. Game menu, Settings, Journal, Controls and any other board/card: they **face the head once when they open** (yaw
   only) and then stay **world-locked**. They re-face only when the head has turned > 30° away from them for 1 s **and**
   no hand is within 15 cm of the board. No continuous billboarding.

## D. The island faces you
8. Mugesh: sometimes the island is slightly turned (yaw) until he grabs and re-places it. Find the cause (placement
   using head forward instead of head position, the idle re-face threshold, the pivot not at the island's centre…).
   Rule: the island's front faces the **line from the island centre to the head** (yaw only) after every placement,
   recenter and release, and the idle re-face kicks in at > 6° for 1.5 s (smooth, never during a grab, battle zoom or
   a lesson ghost). Log yaw error on placement.

## E. Journal: more on the detail card, button in the right place
9. **Lodge:** remove the Journal book from the middle of the room; show a small **Journal tablet left of the pods on the
   rim**, only while in the lodge.
10. **Detail card** (poke a caught or seen portrait): a proper card, not one line: a slowly turning **3D model** of the
    Isling (or the portrait if seen only), name, element badge, **strong vs / weak vs** (from the type chart), **where it
    lives** (realm + spot), **evolution chain** with levels (unknown forms as silhouettes "???"), its **3 moves** with
    element, your best one's level and "Caught ×N", and a **one-line description** (write one short, cozy, original line
    per species from the concept summaries, ≤ 70 characters). Unseen portraits: silhouette, "???" and the hint only.
    Card sits next to the Journal at the old board-size rules, world-locked (C).

## F. Straight paths
11. Several paths still zig-zag one tile at a time (screenshot: Cinderwood near the hot springs). Rule for **every map**
    (hub, Cinderwood, Galewick, Lilymere, interiors): paths are **straight legs** with right-angle turns, each leg **≥ 3
    tiles**, no one-tile jogs, 2 tiles wide where two paths meet or near a hand moment. Straighten without moving the
    hand-moment spots, gates, shrines or landmarks; keep the map checks passing (connectivity, blocked-until-solved
    crossings, tall-grass share). Top-down shots before/after per map (`14i_paths_<map>_before/after.png`). Smart
    steps should rarely stop on a jog now; report the count of path turns per map before/after.

## Housekeeping
- EditMode tests green. Update `.claude-handoff.md`, GameDesign.md (grab rule, board rule, path rule), QA-Guide,
  `Run-14i.md` (headset test list in order: grab hold, lessons, boards, facing, Journal card, paths).
