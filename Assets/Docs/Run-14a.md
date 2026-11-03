# Run 14a: first-minute polish + realm arrival moments (Nov 3)

Prompt: `Docs/Prompts/step-14a-first-minute-polish.md`. Everything is **headset test pending**. Flat bridge runs only.
No QA APK this run (Mugesh: "No need for builds").

## Commits

| Section | Commit | What |
|---|---|---|
| A | `912c672` | Spawn and recenter (settled head, one placement rule, the shard follows you, recenter signals, logging) |
| B | `68423a7` | Island grabs only at the 4 corners + the front lip; pods win; grab frame shows only the handles |
| C | `2bd1b4d` | Pinch lesson: progress dots, "Nice! Once more." / "Perfect!", bigger burst + chime |
| D | `7f511e9` | Spirit parks just above the interaction point; sunrays while an objective teaches |
| E | `52547f1` | Rim socket preview (ghost pod) within 12 cm; forgiving lodge put-back |
| G | `a4ec6dc` | Game menu Music On / Off |
| F | `e60c230` | Galewick arrival clouds, Lilymere arrival stream, channel = planks, setup, maps, scene |
| fixes | `fa18881` | Fixes found in the flat runs (see below), bridge helpers, tests |

Setup to re-run after a pull: **Tools > Pocket Isle > Setup > First Minute (14) > Set Up MR_Main (14a first-minute polish)**.
Tests: **246/246**.

## A. Why the spawn went wrong (the real causes)

1. **Placed too early.** Startup placed 5 frames after the first tracked head pose, often while the headset was still
   being put on. It never checked again, so the eye height of that moment stuck: stand up or sit down after, and the
   island was too high or too low.
2. **Shard and island lived in different spaces.** The shard was moved in world space every frame, from a point taken
   once. The island is a child of TrackingSpace. Any TrackingSpace move (MRUK world lock, a recenter) split them, so the
   island unfolded off to the side of where the shard was.
3. **A recenter during the shard stage** moved the hidden island but not the shard (same cause).
4. **Meta's recenter "didn't take".** The only signal used was `OVRDisplay.RecenteredPose`. Meta's code raises it only on
   a standalone Quest (from the local-space recenter count) and **never over Link** (its headset-type check excludes
   Link). So over Link a recenter never re-placed the island.
5. **Settling was too short.** "Settled" meant 4 still frames (~55 ms) or 1.5 s. The 1-second check after only logged.
6. **Looking down or up** flattened a near-vertical forward vector: noisy yaw, so the island could land to one side.
7. **No height limits.** The pivot sat 35 cm under the eyes, with no clamp against the eyes or the floor.

**Fixes:** wait for a settled head (0.5 s within 1 cm / 1.5 deg, TrackingSpace still; startup max 3 s, recenter max 2 s).
One rule (`IslandPlacement`, pure, tested): yaw-only forward (the head's up vector when looking straight down / up), 45 cm
out, the island's top 30 cm under the eyes, never above 15 cm under them, never below 55 cm over a known floor (a
FloorLevel / Stage origin). The shard rides on the hidden island and follows you until touched. Recenter signals:
`RecenteredPose` + `OVRManager.TrackingOriginChangePending` + a TrackingSpace jump (5 cm / 5 deg) + a head jump in
tracking space (20 cm / 30 deg, live headset only, at most every 2 s). Then the settle wait, then a safety net:
1 s later, if the island is > 40 deg off your yaw or > 0.8 m away, it is placed once more. Every placement logs head
pose, TrackingSpace pose, floor, reason and result (`[IslandPlacer] Placed: ...`), and the last one goes into the bug
report's `state.txt` ("Island placement: ...").

## Found and fixed during the flat runs

- **Runaway placement in flat Play:** the seat camera moved the "head", the head-jump trigger read that as a recenter,
  re-placed the island, the camera followed... the island climbed to 26 m. Head jumps now count only with a live XR device
  and at most every 2 s. On the headset tracking doesn't follow the island, but a tracking glitch could still trigger one
  re-place (logged as "head jump").
- "Nice! Once more." was replaced at once by the lesson's first line (the reminder thought the line had gone away).
- The pinch lines had no target, so the Spirit stayed home: they point at the ghost now.
- The stream's stuck pads were sunk out of sight and too far apart for one palm: on the bank now, a tile apart.
- Re-running the setup copied the garden's emptied ghost onto the stream: the setup sets it itself.
- Galewick's map re-rolled when I moved its sign (the generator's random sequence shifted, and a 31-tile tall grass patch
  failed the checks): the sign stays where it was. Galewick's map is unchanged except the 4 puff tiles. Lilymere's map
  did re-roll (it had to change: the stream, the moved first grass patch). Its checks pass.

## Spirit placement (D.10): every lesson and hand moment

Parks above: shard, rim corner (move), starter pods, ghost (pinch, walk, stop), gold tile (look), first grass, lead pod
(send out), nearest rune, cradle (catch), island front middle (two hands, between the two corner ghosts), every hand
moment in all three realms (`puzzle:<id>`: Cinderwood log / pump / wheel / flame / hollow / ember, Galewick mill / kites /
arrival clouds / ridge clouds / tower door / Galeowl's knots, Lilymere stream / garden / scoop / Tidemother), shrines,
gates.
**Couldn't place (no single point):** `lesson.hands` ("Show me your hands!": both ghosts rise in front of the island),
`tut.pond` (any water tile), chatter lines without a target (they keep the Spirit home, no rays).
**Flat only, judge on the headset:** the pinch ghost floats beside your real hand. The Spirit flies there (off the
island), clamped to your 35-deg view cone.

## Headset test list (in this order)

Restart Unity before Play over Link. Turn Standalone **Initialize XR on Startup** back on (it's off for my flat runs).
New game: Settings > New journey, or delete `save.json`.

1. **Spawn seated:** launch, sit still: the shard drifts in ~45 cm ahead, its middle ~30 cm under your eyes. Log:
   `[IslandPlacer] startup: head settled after 0.5x s` then `Placed: ... startup (CenterEyeAnchor)`.
2. **Shard follows:** before touching it, turn your head ~60 deg and hold 1 s: it glides in front of you. Stand up (or
   sit back down): it follows the new height. Log: `[Opening] Shard follows the player (looked 6x deg away)`.
3. Touch it: the island unfolds exactly where the shard was.
4. **Pinch lesson:** two dots under the line. First pinch: a bigger gold burst, a chime, "Nice! Once more." with one dot
   gold. Second: "Perfect!". The Spirit hovers over the ghost hand beside your hand, with gold rays.
5. **Move lesson:** the Spirit sits over the near corner on your dominant side (not the right edge's middle). Bring a
   hand near that corner: its bracket lights white; grab: gold. Grab the front lip under the pods: it moves too.
   Pinch at the side middle or the back: nothing moves.
6. **Two hands:** grab any two handles (two corners, or corner + lip): turn / resize.
7. **Starter:** grab a floating pod, bring it to the rim: within ~12 cm a ghost pod glows in socket 1; let go: it flies in.
8. **Meta recenter:** hold the Meta button to recenter, facing another way: the island comes in front of you within
   ~2 s. Log: `Recenter signal: ...` (which signal fired, especially over Link), `Placed: ... recenter ...`,
   `Recenter +1 s: island 0.4x m away, x deg off`.
9. Menu > Recenter: same rule.
10. **Game menu:** Music Off / On (silent until tracks land; the rune label and the volume setting should flip).
11. **Galewick (Dev > Travel > GW: arrival, tutorial done):** "Galewick... the air isn't moving at all.", two clouds on
    the path ahead, chip "Sweep · Clouds 0/2", sweep ghost. Sweep: each puff thins and goes. The last one: a big gust
    ripples across the whole meadow, "There's the wind!". The ridge's clouds later: counter, short line, no ghost.
12. **Lilymere (LM: stream):** chip "Palm down · Pads 0/3", the stream is dry mud, three pads stuck on the near bank.
    Palm down over it: rain, the stream runs with water spreading from under your hand, each pad you rain on lifts and
    floats; all three: they drift into a bridge over the path. Walk across. The lily channel further on is planks.
13. **Lodge:** carry a pod near the rim: the ghost socket shows; put a rim pod back near its socket: it flies home.

## Feel numbers

| What | Value |
|---|---|
| Settle | 0.5 s within 1 cm and 1.5 deg (startup max 3 s, recenter max 2 s, menu max 1 s) |
| Placement | 45 cm out, island top 30 cm under the eyes, clamp 15 cm under the eyes / 55 cm over the floor |
| Shard follow | > 35 deg or eyes moved > 15 cm, for 1 s; glide 0.8 s |
| Recenter triggers | TrackingSpace 5 cm / 5 deg in a frame; head 20 cm / 30 deg (live XR, every 2 s max) |
| Safety net | 1 s after: > 40 deg off or > 0.8 m away = placed once more |
| Corner handle | 10 cm along each edge (island-local), hand within 3 cm of the shell |
| Front lip | at least 2 cm under the pods' centre line, the near 5 cm (island-local) of the base, Explore only |
| Pods win | pinch point or palm within a pod's radius + 1 cm |
| Grab frame | lights within 6 cm; idle brackets 0.18; held = gold (tint 0.85) |
| Progress dots | 11 mm, 6 mm apart, under the bubble |
| Spirit over a target | target radius + 6 cm at the placed size (0.1 island-scale), 0.9 cm toward you |
| Sunrays | 12 rays (alternating long / short), 0.3-1.05 x the Spirit's height, 0.04 turns/s, 1.3 Hz pulse +-18%, fade 0.4 s |
| Rim preview | 12 cm from a socket |
| Galewick arrival | 2 puffs (2 wide), 3-4 tiles past the arrival, ~3 gusts each; ripple 0.5 s after, a softer one 0.9 s later |
| Lilymere stream | rows y 8-9, x 5-18 (24 tiles + 4 crossing tiles), swells in 2 s of rain (hard 1.6x faster), pads fill as the garden's buds (2 s, 1 s hard), drift into the bridge in 1.6 s |

## Not done / open

- **No QA APK** (Mugesh: no builds this time). `Builds/PocketIsle-QA-oct27-tester.apk` untouched.
- The corner handle is a glowing bracket on the grab frame, not a modelled notched corner piece. Judge on the headset;
  a mesh piece can follow.
- Only the pinch lesson counts (dots): the walk lessons end on a gold tile, the two-hands lesson on one grab.
- The arrival puffs don't wall off the meadow: you can walk around them through the tall grass (encounters). The stream does
  gate the trail (woods at both ends).
- The trickle shows as dry mud (no thin water line); it swells to full water.
- Lilymere's map re-rolled (random layout), so the woods / tall grass around the village are new.
- The `palm_sweep` ghost is reused for the stream's rain (as the garden had); a palm-down hover clip would read better.

## Shots (`Logs/BattleShots/`)

`14a_shard_seated`, `14a_pinch_dots_zero` / `_one`, `14a_corner_handle_glow`, `14a_front_lip_glow`,
`14a_rim_socket_preview`, `14a_lodge_rim_preview`, `14a_galewick_arrival_clouds`, `14a_lilymere_stream_before` /
`_raining` / `_bridging` / `_after`. The Spirit + sunrays over the ghost hand show in `14a_pinch_dots_one`. Standing
vs seated spawn can't be shot flat (there's no head pose; it uses the seated default).
