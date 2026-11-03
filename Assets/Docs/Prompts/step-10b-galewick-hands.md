# Step 10b: Galewick hand moments (flick the windmill, kites into the room, fan the wind)

Read CLAUDE.md, `.claude-handoff.md` (9b puzzle kit, 9h ghost retarget + lesson chips), **`Assets/Docs/RealmsDesign.md`
("What makes a hand moment beautiful", "The four hand moments")**, the kit in `Runtime/Puzzles`.
Decided Oct 26 night. **These are the moments Mugesh wants the game to be remembered for: spend the effort on feel and
on how much of the world answers.** Before Play over Link, ask Mugesh to restart Unity.

Shared rules: each moment is a kit piece (island-local, map tiles, saved in `PuzzleState`, works only near the
explorer), has a look + pinch fallback, teaches itself once (Spirit line pinned as a chip until done, gold focus, a
ghost on the real object via `GhostRetarget`; use the closest recorded clip or a code clip until Mugesh records one),
and gives the explorer a cheer when solved. Write the `gw.*` lines (one short line each, ≤ 60 characters).

## A. Wheel piece → flick the windmill (village)
1. Turn the `IWheelPiece` stub into a real **Wheel** piece: angular velocity, drag, flick input (fingertip speed
   tangential to the wheel plane > 0.5 m/s within ~2 cm of the sails adds spin by speed), threshold (keep spinning
   above a speed for N seconds), checkpoint, look + pinch = one flick.
2. Big mill: 3 good flicks within 4 s → its gears turn (small gear prop by the door), the **drawbridge** lowers over
   the ditch with a creak and dust; saved. Small mills can be flicked for fun (no puzzle).
3. Lesson `gw.mill`: "The mill's stuck. Give its sails a flick!" chip "Flick!".

## B. Kites into your room (kite field)
4. **Kite piece:** 3 kites lie in the grass. Pinch (or fist) one: it lifts on its string. Raised past ~15 cm above the
   island top (island-scaled) and released → it **flies**: 30–50 cm above the island, out in the room, a lazy
   figure-eight (each kite its own phase/height), tail bows flutter, the string a soft sagging curve (line or thin
   tube mesh, toon-coloured) down to the **explorer's backpack**. Released lower → it drifts back down to its spot.
   Look + pinch a kite → it rises and flies by itself.
5. Flying kites follow the explorer as it walks and fan out so they never overlap; they stay out of the battle view
   (lift higher and back during battles); hidden in interiors; saved (a reload restores them flying). Max 3.
5b. **Keep it simple (de-risk):** the flight is **scripted motion, not physics**: once released above the threshold the
   kite eases to its slot (offset from the explorer, height by index) and plays a looping figure-eight + tail sway;
   the string is a 3-point Bezier (backpack → sag point → kite) rebuilt each frame as a thin tube or a camera-facing
   ribbon. While held it simply follows the pinch point with a little lag (no string tension sim). If pinch-and-raise
   feels unreliable on hands, the threshold drops to 8 cm and a quick upward toss also counts. Ship this version first;
   polish only after it works end to end.
6. Lesson `gw.kites`: "Kites! Lift one up into the sky." chip "Pull it up!". Spirit counts "One!" / "Two!" / "All
   three!" Draw-call budget: ≤ 2 per kite including the string.

## C. Gust → fan the wind (windy ridge)
7. **Gust detector:** an open, flat hand (fingers extended, not pinching, not grabbing the island) moving > 0.6 m/s
   over the island footprint within 30 cm above it = a gust with direction and strength from speed (cooldown 0.3 s).
   Works with either hand.
8. **World answers:** the grass shader gets a **moving gust front** (direction, front position, width, strength): a
   bright ripple band runs across all tall and short grass on the island and flattens it briefly; wind streak
   particles from the pool along the sweep; mill sails get a nudge; loose leaves/petals fly. This works everywhere in
   Galewick (restored or not) as a toy, and is the hero of the realm, so tune it until it feels great.
9. **Cloud bank:** 3 big low cloud puffs across the ridge path. A gust passing over a puff pushes it in the sweep
   direction and thins it (about 3 good gusts per puff); a gone puff opens its tiles; all gone = the path is open
   (saved). Look + pinch-hold a puff = it blows away slowly.
10. Lesson `gw.gust`: "This cloud is stuck. Wave your hand over the island!" chip "Wave!".

## D. Wind tower door (uses B)
11. The explorer reaching the tower with 3 kites: they detach from the backpack and tie onto the tower's crown, the
    vanes turn, the door opens (saved). Fewer kites: `gw.tower.kites` "The tower needs all three kites."

## Housekeeping
- Perf on device-like settings: ≤ 8 extra draw calls in the kite field with 3 kites flying; gust shader cost ≤ 0.3 ms.
- Shots `Logs/BattleShots/10b_*` (mill flick, kites flying over the island, gust ripple mid-sweep, cloud bank).
- EditMode tests: wheel flick math, kite release threshold, gust detection from synthetic hand tracks, cloud puff
  thinning, tower rule. Commit per section with "(headset test pending)", don't push. Update `.claude-handoff.md`,
  GameDesign.md, QA-Guide.

## Test
1. Village: flick the big mill's sails 3 times; it keeps turning and the drawbridge lowers.
2. Kite field: pull a kite up off the island and let go; it flies in your room on a string from the explorer; walk and
   it follows. Three kites fan out.
3. Ridge: sweep your open hand over the island; a ripple runs across all the grass; the cloud puffs drift away.
4. Tower: the kites tie onto it and the door opens.
