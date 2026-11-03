# Step 9h: first minutes clarity (instructions, ghost hands, pinch, small fixes, logo v2)

Read CLAUDE.md, `.claude-handoff.md` (8b lessons + ghost hands, 9g title), `Onboarding.cs`, `LessonRunner.cs`,
`GhostHands.cs`, `GhostHandClip.cs`, `Dialogue*.cs`, `Data/Dialogue/Script_v1.asset`, `LodgeRoom.cs`.
Decided Oct 26 (design chat) from Mugesh's 9g test and the QA tester's first run (a non-gamer, years of XR QA).
Before Play over Link, ask Mugesh to restart Unity. **Last build day before Goa:** do the parts in order, commit
each, and stop at a clean commit if time runs out. End with a QA APK for the tester.

The tester's verdict, in short: **too much text, too fast, nobody reads it; instructions vanish before the action is
done; pinch is never really taught; the pod needs a highlight.** Judges are new users too. This step fixes that.

## A. Small fixes (fast, do first)
1. **No room scan on launch.** The tester got a room-scan prompt on a new device. MR_Main's MRUK has
   `LoadSceneOnStartup: 1`. Never trigger Space Setup / scene capture: turn startup loading off and load from device
   with scene capture **not** requested when there's no data (check the v207 MRUK API for the flag), after the opening
   places the island. No scene data = floating island with the soft fixed shadow (already the intended path). Log it.
2. **Lodge: no accidental exits.** Remove the palm-hover exit on the door arch. Walking onto a door tile exits only
   when the move was aimed out of the room (a swipe/tap toward the door), never because the explorer is standing next
   to it or a path ends there. Look + pinch on the arch still exits. Spawn tile inside stays one step away, facing in.
3. **Battle facing:** at battle start the explorer turns to face the wild Isling (it kept its walking direction).
4. **Spirit out of the battle view:** during a battle the Lantern Spirit moves to the side of the near rim on the
   non-dominant side, outside the line from the head to both pads (it sat in front of Pebbloop's face).
5. **Bubble never inside the island:** clamp the Spirit's bubble so its bottom stays above the island's top surface
   plus a margin (as seen from the head), also when the Spirit flies low over the island centre or the player sits far.

## B. Instructions stay until the action is done
6. **One instruction at a time, and it stays.** While a lesson is teaching: the Spirit parks beside the lesson's target
   and stays there; the lesson line stays up. After ~6 s the bubble shrinks to a **hint chip** (short verb + a small
   hand icon, e.g. "Grab the edge") that stays until the lesson completes. Looking at the Spirit or the chip opens the
   full line again. On done: praise word, chip pops, next lesson. No other line may replace a lesson line mid-lesson
   (story lines queue until the lesson is done; idle hints are off while a lesson teaches).
7. **Reading pace for every non-lesson line:** shown for at least 1.5 s + 0.06 s per character, and the next line
   waits for that. Multi-line beats show one line at a time.
8. **Less text before the starter.** New opening script (replace the lines in `Script_v1`):
   | id | lines |
   |---|---|
   | `intro.shard` | "Something's here. Touch it?" |
   | `intro.arrive` | "Hello, big one! I'm a Lantern Spirit." |
   | `intro.world` | "This little island is yours. Grab its rocky edge to move it." |
   | `intro.starter` | "Pick an Isling friend!" |
   | `intro.statue` (keep where it is) | "That statue has three empty holes... The world broke into floating pieces." / "I feel like I know it." |
   "The world broke into floating pieces" moves from the opening to the statue beat. Keep starter name lines (1 line).

## C. Teach pinch, highlight what to touch
9. **Pinch lesson** (new, right after `intro.arrive`, before the island grab): `lesson.pinch` → chip "Pinch!":
   "Tap your thumb and finger together. Like this!" The ghost hand demos a slow pinch **right beside the player's
   own dominant hand** (follows it, ~8 cm to the outside, at hand height), not at a far target. Each pinch makes a
   small gold sparkle at the fingertips. Done after 2 pinches. Then the starter lesson says "Look at one and pinch,
   or grab it." Cut in the Quick Journey? No: keep it in both.
10. **Pod highlights (focus language):** every pod the player can act on right now gets the gold outline (starter
    pods during the pick, the lead pod on the rim when sending out, the cradle pod when catching); white outline on
    gaze or hand hover; the pod also grows ~8% while hovered. Same rule for the starter pods: the looked-at one grows
    and wiggles (as today) **and** gets the white outline.

## D. Ghost hands in the right place
Most recorded clips play away from what they act on, because a clip replays the hand where Mugesh's hand was.
11. **Retarget clips to the real objects (no re-recording):** for clips with a grab or pinch (detect from
    `keyPinch` / thumb-index distance in the frames), find the **grab frame** and the **release frame**. At playback,
    warp the root path so the grab frame lands on the lesson's real **source** (the pod, the rim, the cradle pod) and
    the release frame lands on the real **target** (the rim socket, the island tile, the wild pad): before the grab,
    offset by the source delta; after the release, by the target delta; in between, blend the two by time. Clips
    with one contact point (poke, palm) map their key frame to the target only. Add per-lesson source/target hooks
    (Dialogue targets / transforms) in the lesson data; log any lesson that has no target.
12. **Specific fixes:**
    - Island move ghost: grabs the **rim at the near-right corner** (dominant side), not mid-island.
    - Starter pick ghost: demos on the pod the player is **looking at**; looking at none = it moves to the next pod
      each loop (left → middle → right). Never always the middle one.
    - Send-out ghost (pick the lead pod off the rim and toss it): grabs the real lead pod and releases over the near
      pad; place-on-rim ghosts end exactly on the empty rim socket.
13. Shots `Logs/BattleShots/9h_ghost_<lesson>.png` of each ghost at its key frame next to its target, for review.

## E. Logo v2 (the first thing judges see)
Mugesh: the logo looks like a kid's clay model, two flat colours, not like the island. Rebuild it from the island's
own art so it belongs to the game:
14. **Base:** a mini Still Isle chunk built from the island's real tile columns, caps and rocky base (`IslandBuilder`
    look: tile grid on top, earth columns, dark rocky underside with the same depth variation), a path tile strip,
    one hub tree, the small statue silhouette, tall-grass tufts, a lantern post. Reuse the existing meshes/materials.
15. **Letters:** chunky, rounded, slightly uneven heights (hand-made feel). "Pocket": warm amber with a lighter top
    band (bevel highlight) and a darker lower band, dark outline. "Isle": grass green body with a cream highlight band
    and **moss caps** dripping over the top edges, the dot of the i = a tiny glowing lantern (the Spirit's gold). Small
    tilt per letter, all toon + outline. A soft gold glow behind the words is fine.
16. Little life: the tiny lantern on the i pulses; two specks of ember/sparkle drift; the base bobs. ≤ 10k tris,
    ≤ 8 draw calls. Turntable shots `Logs/BattleShots/9h_logo_*.png` (front, 3/4) for Mugesh to judge.

## Not in this step (add to `PolishBacklog.md`)
- Rock-slab look for title/menu runes (runes stay consistent with battle for now).
- **Guided ghost recorder** (Mugesh's idea): a dev panel "Ghost" tab walks through each lesson, shows "Record this
  next: <lesson>", Ready → 3-2-1 → records while he does the real action → stops when the lesson's done event fires
  → preview, Keep / Redo; clips saved with their source/target anchors so D's retarget applies. Nov 3, only for
  clips that still look wrong after D.
- Quick Journey 10-minute design pass (separate design session).

## Housekeeping
- One compile batch per part; EditMode tests green (lesson line persistence and queueing, reading pace, retarget math
  with synthetic clips, lodge exit rule). Commit per part with "(headset test pending)". Don't push. Update
  `.claude-handoff.md`, `GameDesign.md` (onboarding rules: one instruction, stays until done, reading pace; ghost
  retarget), `QA-Guide.md` (what's new) and `QA-TestPlan.md` (a fresh-install onboarding pass).
- Then **build the QA APK** and install it. Summary: what changed, anything skipped, the shots to review.

## Test
1. Fresh install: no room-scan prompt; short lines, each readable; the pinch lesson's ghost sits by your own hand.
2. Every lesson's hint stays (bubble → chip) until you do it; nothing jumps ahead.
3. Starter ghost follows the pod you look at; target pods glow gold, hovered ones white + bigger.
4. Island move ghost grabs the near-right rim; send-out and rim ghosts start and end on the real pod and socket.
5. Lodge: standing near the door doesn't exit; walking into it does.
6. Battle: the explorer faces the wild Isling; the Spirit is off to the side; its bubble never sinks into the island.
7. Logo looks like a piece of the island with amber + mossy green letters and a lantern dot.
