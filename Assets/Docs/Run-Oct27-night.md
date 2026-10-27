# Run, Oct 27 night: smart-step walking + guidance (12a), then the night notes (12b)

Prompt: `Prompts/run-oct27-night.md`. Flat Editor Play through the bridge plus EditMode tests (222/222). **Nothing tested
with real hands or on the headset.** No pushes. **No APK** (Mugesh, mid-run: "dont make a build").

## Update, Oct 28 after midnight: step 12c (Mugesh's midnight test notes + the 12b left-overs)
Notes: `Assets/Docs/Notes copy.md` + 7 screenshots. Flat Editor Play + synthetic hands only; tests 222/222. No APK.

| Commit | What | State |
|---|---|---|
| `3a8f4ac` | Smart steps at the pinch-walk speed (0.4 s a tile); footprints redrawn (heel behind, toes ahead); the Spirit's trail keeps to roads; battle runes keep the bubble off them, the bubble only takes gaze on the panel; objective border thinner / rounder; hall exit glow + arrow only once the Keeper is calmed; ridge clouds back on the road (0.6 tiles, bob 0.1) and hidden in battles; the road north of the ridge open again (two trees on it); Lilymere wetland palette (teal grass + willows, blue-grey stones, deeper blue water) | done, flat-checked |
| `cfe7470` | Dev panel rework: higher, beside the island (dominant hand), X to close, tabs Travel / Team / Battle / Story / Save / Tools with gold headings, full names, Up / Down scroll | done, flat-checked |
| `2370a1b` | 12b D11-D13: cliff with a carved bowl, the frozen fall from its lip into the stone basin (now the catch pool), pours fill the bowl (half / full), spill, cracks race down; water sheet across the palms + pouring stream; ghost `cup_carry` carries from the inlet to the bowl | done, flat-checked |
| `210d80f` | After Mugesh's test ("two hands bad, one hand kind of worked"): **one hand only**, bigger palm pool, easier tip (-0.25), pouring anywhere over the bowl / fall / pool counts; one-hand ghost. Two hands stay behind **Two Hands** on Scoop And Pour | done, flat-checked |
| | D10 drought details (wet tint, puddles, salt rims, garden shallow water) | **skipped** (Mugesh) |

Decisions:
- "Keep the door on the right" read as "glow only at the right time" (after the Keeper). If it meant moving the hall
  door to the right side, that's still open.
- The footprint "reversed" note was the shape: the old sole + one pad read as a heel in front. New prints point +v.
- Trail: A* with off-road steps costing 4, so it keeps to roads but still cuts across when there's no road.
- The bubble in battle wasn't visible in my flat shots, so its keep-off-the-runes move is **untested**.

Headset test (12c):
1. Walking speed; footprints point the way you walk.
2. Gold trail follows the road.
3. Battle: the Spirit's bubble never covers a rune; pokes on runes always land.
4. Forge Hall / tower hall: no bright arch on arrival; the exit glows once the Keeper is calm.
5. Galewick ridge: clouds sit on the road and drift; the road north to the tower is clear.
6. Lilymere: new colours; at the basin, scoop with **one** cupped hand, tip anywhere near the cliff: two pours run the falls.
7. Dev panel: opens above the island's right corner, X closes, Up / Down scroll a long tab.

New feel numbers: Scoop And Pour > Tip Up -0.25, Basin Radius 1.8, Basin Above 0.35, Palm Pool 0.036 m, Sheet Dome
0.012; Dev Panel > Beside Height 0.08, Beside Gap -0.12, Beside Back 0.08, size 42 x 36 cm; Cloud Bank > Puff Height
0.6, Bob 0.1; Explorer > Smart Seconds 0.4.

## Done / partly / not started (12a + 12b, before the update)

| Step | Section | State | Commit |
|---|---|---|---|
| 12a | A smart steps, footprints, double swipe, speeds | done, flat-checked | 342f263 |
| 12a | B walk lessons (walk / turn / far), Controls card | done, flat-checked (new game, all three lessons) | 4508fab |
| 12a | C objective vs chatter bubbles, poke the Spirit, line flushing | done, flat-checked | 713e68c |
| 12a | D Spirit's trail, gold that reads on every ground | done, flat-checked (hub, Galewick, Lilymere) | fce4984 |
| 12b | A1 focus lock for every hand moment | done, flat-checked (kites, mill, basin) | 2977431 |
| 12b | A2 grab assist (8 cm, pinch or fist) + white hover | done (kites, ember, knot ends, log; no real hands) | 0a5e693 |
| 12b | A3 bubble over props + the wavering fix | done, flat-checked | edd16d0 |
| 12b | A4 exit doors (glow + floor arrow) | done, flat-checked (tower hall) | 037f0d9 |
| 12b | A5 respawn at the current realm's shrine | done (tests; not lost a battle on purpose) | edd16d0 |
| 12b | A6 wind shard green | done | edd16d0 |
| 12b | A7 gift bug | done + test | edd16d0 |
| 12b | 8b dev panel rework | **not started** | |
| 12b | B firebox: chip, the Spirit points, bubble over the forge | done | 93fa481 |
| 12b | C clouds in the air (shadows, motion), ridge 2 wide, bush gone | done, flat-checked | b8a5339 |
| 12b | D10 drought mud colour | **partly**: colour only (no wet tint, puddles, salt rims, garden flooding look) | af39102 |
| 12b | D11 cliff bowl waterfall, D12 two-palm water sheet, D13 two-hand ghost | **not started** | |

## Headset: switch XR back on first
Standalone **Initialize XR on Startup** was **on** when I started. Flat Play needs it off (the XR guard stops Play after
20 s without a headset), so I turned it off through Unity's settings object while working and **turned it back on at
23:15** (the asset matches git again). To check: Edit > Project Settings > XR Plug-in Management > PC tab > Initialize
XR on Startup ticked. Unity may want a restart before Link takes the session, as usual.

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

Then the night notes (12b):
10. Walk up to any hand moment (Go to GW: kite field, GW: big mill, CW: forge door, LM: basin...): the island glides to
    frame all its pieces and holds while you're there; walking out lets go.
11. Kites, the ember, Galeowl's knot ends, the log: bring a hand close (within ~8 cm): the object turns white; a pinch
    or a fist there grabs it.
12. Forge: pick up the ember: chip "Tip it in!", the Spirit says so pointing at the firebox (gold ring brightens). The
    bubble is never inside the forge or the cliff now; it stays close to the Spirit and doesn't wobble.
13. Keeper halls / lodge: the way out glows (swirl in the arch, ring on the step) with a gold arrow on the floor.
14. Lose a battle in Galewick: you're back at Galewick's shrine (or its arrival), not the hub's.
15. Wind shard is green (holder, Spirit's circle, statue rim).
16. Quick slot: the two-Isling gift comes only in the hub right after the tutorial; going into a realm first skips it.
17. GW: ridge: the clouds float up in the air with soft shadows, bob and sway; the path is two tiles wide, no bush.
18. LM: garden / the lagoon's dry ring: ochre drought mud, not white.

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
| Grab assist | 8 cm (real world) | each hand moment > Grab Assist |
| Focus lock | 2.5 tiles margin, moves after 0.25 tiles | Explorer > Focus Lock |
| Bubble | max 14 cm from the Spirit, 6 mm deadband, 1.5 cm toward you | Dialogue Bubble |
| Clouds | 2.5 tiles up, bob 0.18, sway 0.3, spin 9 deg/s, billow 7%, shadow 0.32 | Cloud Bank |

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

## Decisions I made in 12b
- **Gift bug**: the gift only ever checks the quick slot, so the Islings came from the **quick slot**, late. If you
  leave the hub before it lands (wind gate first), it used to arrive whenever you came back. Now: quick slot only,
  once, and going into any realm first skips it for good (puzzle `quick.gift` stage 2). Test added.
- **Respawn**: the shrine used last **on the map you're on**, else that map's arrival (start tile); no more travelling
  to another realm's shrine. Shrines are saved per map (old saves: their one shrine counts for its map).
- Focus lock skips a moment whose pieces + the explorer don't fit one window; the lotus garden keeps its own settle.
- Grab assist: white hover on kites, the ember and knot ends; the log and the Keeper shard only got the reach (the
  shard already had 9 cm + hover).
- Ridge: 2 wide; each puff blocks both rows (three extra blocking tiles), the near side is low rocks only.
- The bubble draws over everything on the island (depth test Always on its own material copies). If it ever shows
  through something it shouldn't (a menu), say so.

## Left / known
- **Not done from 12b**: the dev panel rework (8b); D10 beyond the colour (wet tint under the rain, puddles, salt rims,
  dried reeds, shallow water between the lotus stones after the rise); D11 the cliff with the carved bowl and the
  waterfall falling from its lip; D12 the water sheet across both palms with a pouring stream; D13 the two-hand ghost
  that animates to the drop point. These need modelling and a new interaction each; I stopped at a clean state for
  your midnight test instead of rushing them.
- Quick Journey's own gold path still uses the old tile flash, not the new gold mark.
- Shots: `12a_footprints` (an early, small-footprint frame; sizes were raised after), `12a_lesson_walk`,
  `12a_lesson_turn`, `12a_controls_card`, `12a_bubble_poke`, `12a_bubble_chatter`, `12a_trail_hub`,
  `12a_trail_galewick`, `12a_trail_lilymere` in `Logs/BattleShots/`.
- `ProjectSettings/AudioManager.asset` shows the spatializer slipped back to "Meta XR Audio" again (not my edit; left
  uncommitted, as before). The 37d054c values are right; re-apply with `AudioHookupDebug.ApplyProjectAudio` before a build.

## Save
- Checks ran a new game in the adventure slot, which also deleted the quick slot's save. **Put back**: `quick.json`
  restored byte for byte from my backup, the test adventure save moved out (there was none before), the last played
  slot set back to Quick (the title's Continue). `mruk_config.dat` is rewritten by every Play (Editor MRUK config;
  can't be restored, regenerates).
