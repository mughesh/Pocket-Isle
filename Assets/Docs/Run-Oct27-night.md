# Run, Oct 27 night: smart-step walking + guidance (12a), then the night notes (12b)

Prompt: `Prompts/run-oct27-night.md`. Flat Editor Play through the bridge plus EditMode tests (221/221). **Nothing tested
with real hands or on the headset.** No pushes. **No APK** (Mugesh, mid-run: "dont make a build").

## Done / partly / not started

| Step | Section | State | Commit |
|---|---|---|---|
| 12a | A smart steps, footprints, double swipe, speeds | done, flat-checked | 342f263 |
| 12a | B walk lessons (walk / turn / far), Controls card | done, flat-checked (new game, all three lessons) | 4508fab |
| 12a | C objective vs chatter bubbles, poke the Spirit, line flushing | done, flat-checked | 713e68c |
| 12a | D Spirit's trail, gold that reads on every ground | done, flat-checked (hub, Galewick, Lilymere) | fce4984 |
| 12b | A-D | see the update at the end | |

## Headset: switch XR back on first
Standalone **Initialize XR on Startup** was **on** when I started. Flat Play needs it off (the XR guard stops Play after
20 s without a headset), so I turned it off through Unity's settings object while working and **turn it back on at
the end** (check: Edit > Project Settings > XR Plug-in Management > PC tab > Initialize XR on Startup ticked). If it's
off when you sit down, tick it there; `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` then shows no change in git.

## Test first on the headset (walking first)
New game in the **quick slot** (title > New journey > Quick journey), then:
1. **Walk lesson**: "Swipe your thumb along your finger to take a few steps." A gold tile glows 3-4 tiles ahead on the
   path. Swipe once toward it: gold footprints appear at once on the tiles it will walk, a ring on the last, and the
   explorer walks there (slower than before) and stops **on** the gold tile. Footprints fade as it walks over them.
2. **Turn lesson**: "The path turns. Swipe again!" A gold tile round a corner. One swipe: the explorer follows the
   bend by itself and stops on it.
3. **Far lesson**: "Swipe twice quickly to keep walking. Tap to stop." The ghost does two quick swipes and a tap. Two
   quick swipes (same way): it keeps walking along the path (brisker) until a fork, something to do, a wall or your
   tap. A tap stops it.
4. Then look + pinch (unchanged) and the grass lesson.
5. Smart-step stops anywhere: it stops at forks, on the tile where a hand moment comes in reach (e.g. walking up to
   the forge, the mill, the pads), before a gate, on the first tall-grass tile (so grass is a choice), at walls, after 5.
   A swipe into a wall: the explorer turns and bumps.
6. **Bubbles**: lesson / task lines are bold with a gold border and a gold target icon, and stay as the chip. Story and
   praise lines are smaller, plain cream.
7. **Poke the Spirit** with a fingertip (or look at it + pinch): during a lesson the full line opens again; with nothing
   to do it says "This way! Follow my gold trail." and gold tiles run toward the next goal.
8. Stand still ~12 s after the tutorial with nothing to do: the Spirit's trail shows (fire gate first in the hub).
   Enter a realm: the trail points at the nearest unsolved hand moment. Check it reads on sand, wheat, grass.
9. Game menu (Pause) > **Controls**: a stone card with four drawn rows. Also in title Settings > Controls.

## Feel numbers to tune (Inspector)

| What | Value | Where |
|---|---|---|
| Smart step speed | 0.53 s a tile (was 0.4; 25% slower) | Explorer (GestureTestPiece) > Smart Seconds |
| Walk-on speed | 0.4 s a tile (old walk speed) | Continuous Seconds |
| Tiles per swipe | 5 | Smart Tiles |
| Double-swipe window | 0.45 s, same direction | Double Swipe Seconds |
| Stop rules | junction, moment in reach (its Reach), before a gate, first tall grass, blocked, 5 | `SmartStep.Plan` |
| Footprints | 0.9 tiles, fade 0.35 s, 6 ahead when walking on | Explorer > Walk Preview |
| Gold brightness | 1.4 footprints, 1.5 trail | Walk Preview / Spirit Trail > Intensity; colours in `Materials/GoldMark` |
| Trail | after 12 s still, up 20 s, 14 tiles shown | Explorer > Spirit Trail |
| Chatter text size | 0.86 of objectives | Dialogue Bubble > Chatter Scale |
| Spirit poke | 2.5 cm fingertip, 1.2 s cooldown | Lantern Spirit > Spirit Poke |

## Decisions I made (without asking)
- **SmartStep took AutoWalk's enum slot (0)**, so the scene and every save move to smart steps without a migration;
  a save that says "AutoWalk" (the old default) also loads as SmartStep. AutoWalk / HoldToWalk are still in the enum.
- **Junction** = a path tile with a side road whose diagonals aren't path. A 2-wide road or a plaza doesn't count; a
  2-wide side road isn't noticed as a fork (rare on our maps). Two turns in a row stop the walk (no U-turn at a wide
  road's end). Crossing a road off the path stops on it (both sides are path).
- **"Within reach of a hand moment"** uses each moment's own Reach (4 tiles mostly), so the walk stops about where the
  moment becomes usable. Props with a line and landmarks don't stop the walk (too many stops); doors stop it naturally
  (they block), gates stop it on the tile before.
- Walk-on stops at junctions / interactables / blockers only, not at tall grass (as the prompt says).
- Lesson gold tiles come from an actual smart-step plan, so the stop lands on them; the smart step also stops on the
  lesson's gold tile. Each walk lesson also finishes after 3-4 smart steps, so it never blocks.
- The lesson ids are `walk`, `turn`, `far` (`stop` is gone; a save at `stop` resumes at `far`). The quick slot used to
  cut `stop`; it now teaches all three.
- The two-swipes-and-tap ghost is **built from the recorded clips** (two sped-up swipes + the tap), no new recording.
- Objective lines are marked in `Script_v1.md` with `· objective` by the 12a setup (38 lines: every lesson line, every
  hand moment's lesson line, `*.task`, `objective.more`). Count lines ("One!") stay chatter.
- A scene change drops queued **non-objective** lines; objectives come back from their pieces when they apply.
- The Spirit's trail is for the **full adventure**; the quick slot keeps its own gold path (still the old tile flash).
- Gold marks are a new premultiplied shader (`Diorama/GoldMark`) so the thin dark outline can darken pale ground.

## Left / known
- Quick Journey's own gold path still uses the old tile flash, not the new gold mark.
- Shots: `12a_footprints` (an early, small-footprint frame; sizes were raised after), `12a_lesson_walk`,
  `12a_lesson_turn`, `12a_controls_card`, `12a_bubble_poke`, `12a_bubble_chatter`, `12a_trail_hub`,
  `12a_trail_galewick`, `12a_trail_lilymere` in `Logs/BattleShots/`.
- `ProjectSettings/AudioManager.asset` shows the spatializer slipped back to "Meta XR Audio" again (not my edit; left
  uncommitted, as before). The 37d054c values are right; re-apply with `AudioHookupDebug.ApplyProjectAudio` before a build.

## Save
- The full-adventure slot had no save; the quick slot's `quick.json` is backed up. Checks ran a new game in the
  adventure slot. At the end both go back as they were (report updated then).
