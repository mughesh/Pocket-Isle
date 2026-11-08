# Run 21: the Nov 8 notes (and the repeats)

Branch `step-21` off `step-20` (`bbffd71`). Not pushed, not merged. Every section committed on its own, "(headset test pending)".
Flat Editor Play, tests and the smoke run only. Tests **360/360**. Smoke run **84 ok, 0 failed** (1 known note: the opening
"no progress"; no rule broke). Save restored byte-identical, XR init back on.

**Of the 11 notes, 5 were repeats or my own regressions.** The rules are now in `Assets/Docs/Regressions.md` (R1-R17), and the
smoke run watches the ones it can (R1 quiet shard, R2 menu pauses sound, R3 menu over the battle row).

| # | Note | Repeat? | Status | Cause / fix | Commit |
|---|---|---|---|---|---|
| 1 | Song plays while poking the shard | **my step 20 regression** | done | step 20 let the title track play to its end; it now fades (2 s) as the shard arrives. Smoke rule R1 | `3e3ceeb` |
| 2 | Swipe ghost tilted, wrong motion | **4th time** | partly | the ghost added a 55° yaw on top of a leaning finger direction. Now: the pose comes from the hand's own measured fingers / back of the hand, set to fingers straight away, back of the hand up and out, no yaw. The sweep motion itself is unchanged | `554a8b8` |
| 3 | Poked pod card stays up | new | done | it closes on the next swipe, walk, battle or island grab | `3e3ceeb` |
| 4 | Pause doesn't mute | new | done | the menu paused only the music (-8 dB). Now every sound pauses; the menu's own UI taps still play | `3e3ceeb` |
| 5 | Switching pods presses a move | new | done | your log: "Grabbed Pod 2" then "Picked move 2 by gaze + pinch". The grab's pinch picked the rune you looked at. No rune pick while a hand reaches for, holds or just let go (0.5 s) of a pod | `3e3ceeb` |
| 6 | Menu in battle overlaps (image) | **my step 17 regression** | done | the move row and battle line step aside while the menu is open. Smoke rule R3 | `3e3ceeb` |
| 7 | Run doesn't work after a move | new | done (needs your hands) | your log has no Run press after a move. The row ducked away whenever a finger came near the **empty** pod sockets, which sit right under Run. Now only pods you can take duck it | `3e3ceeb` |
| 8 | Shard ghost places it low, not on the statue | **3rd time** | done | the statue's ghost had no drop point, so "grab and put down" played in place at the floating shard. Now it grabs the shard and lets go at the socket (smoke log: "grabs at puzzle:shard.fire, lets go at puzzleTo:shard.fire") | `554a8b8` |
| 9 | Statue is silent | **my step 18 bug** | done | step 18 gave the statue a bell voice, but its letter fade hid every letter count from the voice, so it never played. Now it blips (4 on "heart.thanks") | `554a8b8` |
| 10 | Ending Islings inside objects (image) | **2nd time** | done | step 17 only kept them apart from each other. Now each spot needs clear ground under its whole footprint (no lantern, lodge, statue or tall grass) and nothing in front of it from the seat. Lodge guests too | `3473027` |
| 11 | Lanterns from the island's middle, too close | new | done | they rise from below and behind the back edge, where the old banner stood, and read over that edge. Size back to step 19's after your two notes (1.8x, then 1.3x, were too big) | `3473027` |
| + | Islings pop ~2 s each (your note while I worked) | new | done | 0.35 s flight, 0.15 s between (was ~1.9 s each) | `3473027` |

## Shots (flat Editor, seat camera)
- `Run-21-img/21_battle.png` / `21_menu_battle.png`: a battle, then the menu open over it (the move row and line step aside).
- `21_hall_final.png`: Hall of Fame, 4 Islings, none in or behind a prop or tall grass (two moved: log "Isling 3 moved (18.77, 19.80) -> (19.08, 18.85)").
- `21_lanterns_read.png` / `21_lanterns_wide.png`: lanterns over the back edge at step 19's size.
- `21_swipe_1_close.png`: the swipe ghost mid-sweep from the seat.

## Not done / for you
- **Swipe ghost: I can't prove "not tilted" from flat shots.** If it's still wrong, record your own swipe: Dev panel > Tools > Record
  ghost (it records the clips not yet recorded; to redo `thumb_swipe_forward`, delete its asset first or tell me and I'll add a
  "re-record" button). Then the ghost plays your real hand.
- Lantern text is smaller to the eye than step 19's (they're ~0.73 m away, was ~0.58). If it's hard to read, say so and I'll
  bring them closer, not bigger.

## Headset test (play order)
1. **New Quick Journey**: the title music fades as the shard arrives; poking the shard is quiet.
2. Swipe lesson: the ghost hand is upright, back of the hand toward you, fingers away. Not tilted?
3. Exploring: poke a rim pod (card shows), then swipe: the card closes.
4. **Battle**: grab another pod and throw it: it switches, no move fires. Log: `Poke/Pinch on rune N ignored: a hand is on a pod`.
5. Battle, after a move: press **Run** (poke): it works.
6. Battle: open the menu: the moves and the line vanish, all sound pauses (menu taps still click); Resume brings both back.
7. Statue with a shard: the ghost carries the shard into the socket. The statue's lines have a soft bell voice.
8. Ending: Islings pop out quickly, none inside a lantern, the lodge or tall grass. Lanterns rise from behind the island, not huge.
