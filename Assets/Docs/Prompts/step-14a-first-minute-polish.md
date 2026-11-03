# Step 14a: first-minute polish + realm arrival moments

Decided Nov 3 (design chat) from Mugesh's notes after his trip. He's being "extra extra perfect" on the first minute:
that's what judges see first. **You may be a fresh session on another Claude account:** orient first (`CLAUDE.md`,
`.claude-handoff.md` newest on top, `Assets/Docs/Overnight-Oct28-finale.md`, `Assets/Docs/Audio/AudioD-Report.md`,
`git log --oneline -15`, the Unity bridge). Leave uncommitted changes you didn't make alone.
If Mugesh is around, ask him to restart Unity before Play over Link and to test between sections. If he doesn't answer,
take the reasonable choice, log it, carry on. Commit per section "(headset test pending)", no AI attribution, don't push.

## A. Spawn and recenter (Mugesh saw: off to the side, too high/low, wrong after a recenter or standing up, and
## sometimes Meta's recenter doesn't re-place properly)
Read `IslandPlacer` and `Opening` first and **find the real causes** (log them in the handoff), then make it robust:
1. **Place from a settled head pose:** wait until the head pose has been stable for 0.5 s (< 1 cm, < 1.5°) and the
   TrackingSpace has stopped moving (MRUK world lock). Yaw-only forward from the head, **0.45 m** out; island top
   **0.30 m below eye height** (clamped so it never sits above 0.15 m below the eyes or under 0.55 m above the floor
   when the floor is known). Same rule for the shard, the island, the menu's Recenter and Meta's recenter.
2. **The shard follows the player until touched:** before it's touched, if the head yaw is > 35° away from it, or the
   eye height changed > 15 cm (sat down / stood up), for 1 s, it glides (≈0.8 s) to the new placement spot. The island
   unfolds exactly where the shard is when touched.
3. **Meta recenter that "doesn't take":** subscribe to every recenter signal available (OVRManager display recentered
   event, TrackingSpace pose jumps > 5 cm / 5°), then wait for the pose to settle (max 2 s) and re-place. Safety net:
   if after any recenter the island ends > 40° off the head's yaw or > 0.8 m away, re-place once more after 1 s.
   **Never** auto-move the island during normal play otherwise (only on recenter, the menu's Recenter, or the shard
   stage).
4. Every placement logs head pose, TrackingSpace pose, floor height (if known), reason and result to the log and the
   bug report's state summary, so Mugesh's next "odd spawn" report shows why.
5. EditMode tests for the placement math (seated, standing, looking down/up, recenter shifts).

## B. Island grab: corner handles + front lip (no more conflict with pinches)
6. The island can only be grabbed at: the **4 corners** (Mugesh's jigsaw-corner idea: a notched corner piece on each
   corner of the rocky base, subtle until a hand is within ~6 cm, then it glows white; gold while held) and the
   **front lip**: the near face of the rocky base **below the pod row** (a pinch point or palm at least 2 cm below
   the pods' centre line). Pinch or whole-hand grab, as today. Everywhere else on the rim, pinches go to pods and
   objects and never move the island.
7. Pods always win inside their own grab radius; in battles and the lodge (pods active) the front lip is off and only
   the corners grab (keep `IslandHandle`'s modes). Two-hand grab = any two handles (two corners, or corner + lip).
8. Update the island-move lesson and its ghost to grab the near corner on the dominant side (it already does), the
   two-hands ghost to the two near corners, and `IslandGrabFrame` to show the handles instead of the whole shell.

## C. First pinch lesson: a signifier for the first pinch
9. Keep: the lesson needs two pinches, the ghost stays until done. Add: two **progress dots** under the chip (○ ○ →
   ● ○ → ● ●); first pinch = a bigger gold sparkle burst at the fingertips + a soft chime, the line changes to
   **"Nice! Once more."** (chip "Pinch again"); second = "Perfect!" and the lesson ends. Same dots for any lesson
   that needs a count (two-hand turn, walk lessons if they count).

## D. The Spirit at the point of interaction, with a sunray glow
10. While a lesson or objective teaches, the Spirit parks **just above the interaction point** (above the ghost hand
    for pinch / grab / two hands; above the target for starters, send-out, catch, shrine, gates, hand moments),
    slightly toward the player, never covering the target or the ghost; the bubble sits above the Spirit (the 12b
    bubble resolver keeps it out of geometry). Remove the old fixed spots (e.g. the right edge centre for the island
    grab). Check every lesson and every hand moment in all three realms; list any you couldn't place.
11. **Sunray glow** while an objective is active: soft radial rays around the Spirit (additive billboard, warm gold,
    slow rotation, gentle pulse, ~2x the Spirit's size), off for chatter. One draw call.

## E. Pods onto the rim: more forgiving
12. Wherever a pod is dropped onto the rim (starter pick, lodge swaps, any put-back): while a held pod is within
    **12 cm** of the rim's pod row, the nearest empty socket glows and a ghost pod previews in it; release = it flies
    into that socket. Released further away: as today. Battle tosses onto the island are unchanged.

## F. Realm arrival "aha" moments (teach the realm's signature gesture right after the portal)
13. **Galewick:** 2–3 tiles past the arrival, 2 cloud puffs sit on the path (a second `CloudBank`). Arrival line
    `gw.arrive`: "Galewick... the air isn't moving at all." then the objective `gw.arrive.clouds`: "Clouds are stuck on
    the path. Sweep your hand to get the wind going!" chip "Sweep" + "Clouds n/2". The sweep lesson + ghost move here;
    the ridge's cloud bank later keeps its counter but no full lesson (the player already knows). On clearing, a gust
    ripples across the whole visible meadow (the aha).
14. **Lilymere:** near the arrival, a thin **trickle stream** crosses the trail (2 tiles wide, very little water,
    dry mud banks), with the 3 lily pads **stuck on one bank** in the mud. Rain over the stream (reuse `RainCloud`):
    it swells (its tiles go from trickle to full water over ~2 s of rain), the pads lift, float and drift into a pad
    bridge across the trail (reuse the pad bridge's slots and look). Objective `lm.arrive.stream`: "The stream's
    almost dry... Hold your hand over it like a little cloud." chip "Palm down" + "Pads n/3" (pads afloat). The rain
    lesson + ghost move here; the lotus garden later keeps its counter, short line only.
    **This replaces the push-the-pads channel:** the old channel crossing becomes a simple plank bridge (no puzzle).
    Update the map checks, landmarks, dev panel Travel (Lilymere: stream) and Story flags (LM stream replaces LM pads;
    old saves with pads solved count as stream solved).
15. Cinderwood is fine as is (Mugesh).

## G. Music toggle (small)
16. The game menu gets a **Music On / Off** rune (off = the Music bus at 0, on = the last level; saved). The full menu
    redesign waits for the Quick Journey design.

## Housekeeping
- EditMode tests green; flat bridge runs of the opening, both arrival moments and a lodge swap. Update
  `.claude-handoff.md`, GameDesign.md, QA-Guide ("what's new"). Shots `Logs/BattleShots/14a_*` (shard placement
  seated/standing, corner handles glowing, pinch dots, Spirit + sunrays over the ghost hand, rim socket preview,
  Galewick arrival clouds, Lilymere stream before/after).
- Build the QA APK at the end (water gate as it is), never overwrite `Builds/PocketIsle-QA-oct27-tester.apk`.
- Report in `Assets/Docs/Run-14a.md`: what changed, the spawn causes found, headset test list in order, feel numbers.
