# Step 8d: island grab rework, shard touch, palm hover, starter pod spacing

Read CLAUDE.md, `.claude-handoff.md`, step-08c-first-minutes-polish.md. Mugesh tested 8c on Oct 25 night.
Four problems. **Measure, don't guess**: for B and C add a dev-only debug readout first, confirm the cause, then fix.

## A. Island grab: palm grab on the base, with a Meta-style grab frame (decided Oct 25)
Problems: a look + pinch near the island sometimes starts a corner grab; leaving the front strip ungrabbable feels
wrong; it's unclear where you can grab.
1. **Separate the gestures.** Island = **palm grab only** (whole-hand grab: fingers curl, ISDK palm grab rules).
   **Pinch never grabs the island.** Pinch stays for pods, runes, gaze select. Configure the island's
   `HandGrabInteractable` supported grab types = Palm only; confirm in the ISDK v207 source, not old docs.
2. **Grab zone = the rocky base skirt all around:** from just under the cap's edge down to the bottom of the base,
   all four sides. On the front, the zone covers the base **below the pod band** (the pods keep their own pinch
   volumes; a pod pinch always wins over the island). Remove the old corner-only boxes.
3. **Grab frame (affordance):** when an open hand comes within ~10 cm of the base skirt, a **glowing rounded-rectangle
   outline** fades in, tracing the island's base perimeter (like the grab frame on Meta's panels): thin, soft white,
   brighter on the side nearest the hand. Hover (hand inside the zone, ready to grab) = brighter and a little thicker.
   Grabbed = solid, slight gold. Two-hand grab = both nearest sides lit. Fades out 0.3 s after the hand leaves.
   Hide it during battles and scene swaps. It replaces the old rock-brighten.
4. **Ghost hands:** the "move the island" lesson ghost does a **palm grab on the side of the base** (use the
   `grab_rim_slide` recording with the key point moved to the base skirt; if it reads as a pinch, use a procedural
   curl-to-fist pose). The two-hands tip ghost = both palms on the left and right sides.
5. Two-hand grab = two palm grabs (yaw + uniform scale, as now). Gestures stay ignored on a grabbing hand.

## B. Shard touch: fingertip contact still doesn't register
Mugesh: poking does nothing; only putting the whole hand inside works. Likely the contact radius
(`_shardRadius` from mesh extents × `shardSize`) is smaller than the visible shard, or the wrong scale is applied.
6. Dev-only debug: draw the contact sphere (wire, in-headset, toggled by a debug key) and log the fingertip distance
   to the shard surface each touch attempt. Confirm the cause.
7. Fix with ground truth: a **convex MeshCollider** on the shard (trigger, no physics), and per frame
   `Physics.ClosestPoint` from each index fingertip / palm; contact when the distance ≤ **1.5 cm**. Or use an ISDK
   `PokeInteractable` if that's cleaner. Either way the visible surface is what counts.
8. Make sure the nudge + spring + flash still happen at the contact point.

## C. Palm over the shrine: right hand doesn't work, left only sometimes
Likely the palm normal (cross of knuckles, mirrored per hand in `PalmHover`) has the wrong sign for one hand, or
the thresholds are too strict.
9. Dev-only debug readout per hand: palm-facing dot, height above the flame, horizontal distance. Confirm.
10. Fix: use the **palm joint's own orientation** (OpenXR palm joint: the back of the hand is its +Y, so the palm
    faces −Y, the same for both hands; verify in the ISDK v207 source) instead of the cross product. Loosen:
    facing dot > 0.5, height 3–15 cm above the flame, horizontal radius 6 cm (scaled with the island). Test both
    hands. Same fix for the lodge door palm hold.

## D. Starter pods overlap
11. While floating, the three pods are spread on a **shallow arc** across about 1.2× the island's width, about
    10 cm above the cap, with gaps of at least 0.4× a pod's diameter. Base float size **25% smaller** than now. The
    looked-at pod grows 1.3× (not 1.45) and its neighbours slide apart (dock-style, like the rim pods), so they never
    touch. The speech bubble must not cover a pod (push it up/away, the 7c overlap rule).

## Housekeeping
- One compile batch per section; EditMode tests green.
- **Device build** (not just Link Play), then test A–D on the headset build. Remove nothing from the debug readouts;
  leave them behind a dev-build flag.
- Commit per section with "(headset test pending)". No AI attribution. Don't push. Update `.claude-handoff.md`.
  Leave `QualitySettings.asset` / `UnityConnectSettings.asset` changes uncommitted unless you made them on purpose
  (say which).

## Test on the headset
1. Look + pinch near the island never moves it. A palm grab on any side of the base, including the front under the
   pods, does.
2. Bring an open hand near the base: the rounded grab frame fades in, brightens on hover, goes solid when grabbed.
3. Pinching a rim pod never grabs the island.
4. Poke the shard with one fingertip: it nudges, flashes and cracks.
5. Palm over the shrine flame works with the right hand and the left hand, first try.
6. The three starter pods float apart with clear gaps; looking at one grows it without touching the others.
