# Step 8b: onboarding lessons + ghost hands

Read CLAUDE.md, `.claude-handoff.md`, GameDesign.md ("Opening"), Script_v1.md and `Opening.cs`.
Step 8 works on the device (Oct 25, Mugesh: "marvelous"). Problems found in the opening: hints go stale (the Spirit
keeps explaining controls after a battle has started), and a wild battle can trigger in the first seconds before
the player knows anything. Decided Oct 25 (design chat): **one lesson at a time, ghost hands show each gesture,
no wild battles until the grass lesson.** The ghost-hand player is a reusable system: step 9's hand puzzles
(bellows pump, log lift, braziers) will use it too.

## A. Lesson system (`Onboarding`)
1. A lesson = id, Spirit line(s), optional gold focus target, optional ghost-hand clip + placement, a **done
   condition** (an event or predicate), and an optional **skip-after** time. Lessons run in order from a data asset
   (`Onboarding_Main`), one at a time.
2. While a lesson waits: the ghost loops its gesture; the Spirit line repeats about every 10 s (shorter second
   wording if available). On done: soft success chime (placeholder until the audio step) + the Spirit says a short
   praise ("Nice!" / "Just like that!" / "Perfect!", rotating) and the ghost fades out.
3. **Lines expire:** every Dialogue line can carry a `validWhile` condition. When a battle starts, a scene swap
   starts, or the condition stops being true, the bubble closes at once and the queue drops lines that no longer
   apply. Control hints never show during a battle.
4. **Wild battles are off** (`Onboarding.EncountersEnabled = false`) until lesson 6. Other grass only rustles.
   Pond fishing is off too until then.
5. Progress (current lesson) is saved, so quitting mid-tutorial resumes at that lesson. Returning players who
   finished it never see it again.
6. If the player does something ahead of time (e.g. walks before the walk lesson), that lesson counts as done
   when it comes up: praise and move on.

## B. The lessons (opening, new game)
| # | Lesson | Ghost hand | Where the ghost shows | Done when |
|---|---|---|---|---|
| 0 | Touch the shard | Poke | At the shard | Touched (auto after 6 s, as now) |
| 1 | Move the island | Grab the rim, slide it | At the rim, front-right | Grabbed, or skip after 12 s |
| 2 | Pick a starter | Look (gaze dot) → grab → drop on the rim | At the starter pods, then the rim socket | Starter in socket 1 |
| 3 | Walk | **Thumb swipe forward** along the index finger | **Above the island** (see C) | Walked 3 tiles |
| 4 | Stop | Swipe, then **thumb tap** | Above the island | Stopped while walking |
| 5 | Walk by looking | Gaze dot on a gold tile, then **pinch** | Above that tile | Arrived at the tile |
| 6 | Tall grass | none; the Spirit leads (gold path) to the **tutorial patch** next to the first grass | — | Stepped into it → **guaranteed** encounter (Toastling Lv 3) |
| 7a | Send out | Grab the lead pod, toss it onto the island | At the lead pod | Isling out |
| 7b | Pick a move | Gaze dot + pinch on a rune | At the runes | First move used |
| 7c | Catch | Grab the cradle pod, flick toward the wild one | At the cradle | Pod thrown (keep the existing capture.* lines) |
| 8 | Free roam | — | — | First catch or win → encounters on everywhere; statue and shrine first-sight hints run as now |

Contextual later (not part of the opening chain): fishing (first time standing next to water facing it: ghost thumb
tap above the island) and the shrine (ghost palm hovering over the flame). Same lesson system, triggered by context.

## C. Ghost hands
7. **Look:** a translucent hand, soft pale-blue fill with a bright rim and a faint glow so it reads against passthrough
   (not gold, gold means "this is the target"). Drawn slightly on top of the island (reduced depth test or a
   second x-ray pass at low alpha) so the island never hides it. Not grabbable, never a gaze or ray target.
8. **Size:** real hand scale when shown at a target near the player's hands (pods, rim, shard); for the
   above-island demos, about **60% scale**, floating ~12 cm above the island's front centre, angled so the thumb
   and index face the player. The **Spirit floats right beside it** during that lesson (Mugesh's call: the player's
   own hands may be out of view or in an awkward pose, so the demo can't rely on being next to them).
9. **Handedness:** show the hand the player used last (from tracking); default right. Mirror the recorded clip for
   the left hand.
10. **Hands not tracked** for more than 3 s during a lesson: Spirit "Show me your hands!" and the ghost shows both
    hands raised in front of the island.
11. **Struggle helper:** if a lesson isn't done after 3 loops, the ghost slows down to 0.6× and gets 20% bigger.
    For walk lessons, after 30 s the Spirit also offers the alternative: "Or look at a spot and pinch."

## D. Recording the gestures (Mugesh does this once, ~1 minute)
12. **Hand clip recorder:** Editor/headset tool (Tools > Pocket Isle > Record Ghost Clip, plus a debug key on the
    device). It records ISDK hand joint poses (OpenXR joint set, v207+) at 30 Hz for N seconds after a 3-2-1
    countdown, relative to a chosen anchor (the island for grabs/pokes, the wrist for thumb gestures), and saves a
    `GhostHandClip` asset (joints, root pose, duration, loop flag, which hand). Trim start/end in the inspector.
13. Clips to record (list them in the summary for Mugesh, with how to stand/sit for each): `poke`, `grab_rim_slide`,
    `grab_drop` (pick a pod, set it on the rim), `thumb_swipe_forward`, `thumb_tap`, `pinch`, `grab_toss` (pod
    onto the island), `grab_flick` (cradle pod), `palm_hover`, `hands_up`.
14. **Until he records them**, make procedural placeholder clips (keyframed finger curls) so everything works and
    can be tested. Recorded clips replace them by name.
15. Playback: a skinned ghost using the ISDK/OVR hand mesh driven by the clip's joints, interpolated, looped with a
    short fade at the loop point. One draw call per ghost hand.

## E. Script (Script_v1.md, reimport)
16. Lines per lesson (short, ≤ ~70 chars), e.g.: walk "Slide your thumb along your finger to walk.", stop "Tap your
    thumb to stop.", look-walk "Look at the glowing spot and pinch.", grass "Islings hide in tall grass. Let's peek!",
    hands "Show me your hands!", plus the praise set. Rewrite the existing intro lines to fit the lessons; remove
    any that no longer apply. Islings wording everywhere.

## Notes for later (don't build now)
17. Add to PolishBacklog.md: **the Spirit as the menu**: poke or look + pinch the Spirit → a small ring of options
    around it (Replay tutorial, Recenter, Volumes, New game, Quit). To be designed later with Mugesh.

## Housekeeping
- One compile batch per section; EditMode tests (lesson order, early-done skip, expiry on battle start, encounters
  gated, progress save/resume). Flat Editor smoke test with New Game and the debug skip keys.
- Commit per section with "(headset test pending)". No AI attribution lines. Don't push. Leave `Assets/Docs/Notes.md`
  and `UnityConnectSettings.asset` alone unless Mugesh says otherwise. Update `.claude-handoff.md`, GameDesign.md
  ("Opening" as built) and BuildPlan.md.
- Device build at the end + quick perf check of the opening with a ghost hand showing.
- Summary for Mugesh: what changed, what to click, what to test, and the **recording checklist**.

## Test on the headset
1. New Game: each lesson waits for you; a ghost hand shows the gesture at the right place; praise on success.
2. Walk lessons: the ghost thumb demo floats above the island with the Spirit beside it.
3. Walking around before the grass lesson never starts a battle.
4. Start a battle: any control hint bubble closes immediately.
5. Hide your hands: "Show me your hands!" with the both-hands ghost.
6. Quit at lesson 4 and relaunch: you resume at lesson 4.
7. Record one clip (e.g. `thumb_swipe_forward`) and see the ghost use it.
