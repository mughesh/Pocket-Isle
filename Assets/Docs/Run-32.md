# Run 32 + 33 (Nov 13, night): one swipe-and-tap lesson, Lilymere's stops, Heart voice picker, friend-test fixes

Branch `step-32` (from master `d128d8d7`), not pushed. Tests 498 / 0; walk sim 0 failures; Release Check passed; smoke: see the end.

## Step 32
| Section | State | Notes |
|---|---|---|
| A. Swipe + tap in one lesson | Done (`8a8a6674`) | The four-swipe lesson is out. `practice`: 3 rounds. The ghost shows a swipe; once the explorer walks it turns into a tap and the chip reads "Tap to stop" (`Onboarding.PracticeGhost`). Rounds 1-2: any tap stop ("Nice stop! Swipe, then tap again."); after round 2: "Nice! Last one: stop on the gold!"; round 3 has the gold tile. Also 3 rounds in the Quick Journey. Help line after 12 s, 60 s pass timer kept. |
| A. Lesson walk area | Done | During the lesson the explorer stays within 6 tiles (square) of the statue; at the edge it turns toward the middle (`SmartStep.PlanRoam(toward)`, `Onboarding.LessonAreaBlocks`). Smoke: 4 x 20 s walks, 46 tiles each, all inside. |
| A. Time | Partly | Smoke can't time a real player (the lessons are pumped). Count of steps: before = 4 swipes + 3 tap stops (two lessons), after = 3 swipe-and-tap rounds (one lesson). Smoke beats: opening 51 s both; "walk lessons" 5 s -> 3 s; first battle 23 s -> 19 s. Headset timing needed. |
| B. Lilymere stops | Done, one cause found (`d235bfda`) | `Step32Walk.Lily`: main route + 100 landmark pairs: 2 unexplained stops (both the corner c33 r46 on wind -> stream, left as is: the swipe rules stay frozen); short stops twice in a row: 9, all one tile before something to use. The sim can't see held views: **the lotus garden and focus locks hold the island view and the walk was cut at its edge, re-centred, cut again** ("stops after 2 tiles, like a junction"). Now a walk leaving a puzzle's held view makes the puzzle let go (`WorldWindow.SetFocus(soft)`, `GestureTestPiece.LeavingHeldView`). No shots (needs Play in Lilymere). |
| C. Heart voice | Done (`71ecf076`) | 6 candidates, call + murmur grains each, 1,360 paid credits (ledger 18,989; picks in `_audio-sources/elevenlabs-paid/picks-s32.md`). Dev panel > Tools > Sound A/B: **Heart voice** (synth moo, 1-6; pressing plays the call) + **Play Heart call**. Default synth moo. Weak takes kept for the ear test: 1 (murmurs dark, 4 grains), 3 (murmurs slightly clipped), 4 and 6 (dark). |

## Step 33
| # | Item | State |
|---|---|---|
| 1 | Hand picture stuck on menu buttons | Done: `PokeRelease` (made at start-up): finger > 3 cm off the touch point for 0.2 s -> the ISDK poke limiter is reset and a stuck select let go. Log `[PokeRelease] visual freed`. Headset only. |
| 2 | Island grab in battles | Done: the battle lock holds only while a hand is within 12 cm of the cradle or a party pod (`IslandHandle.podReach`). |
| 3 | Gaze icon reads as "V" | Redrawn: a side-view pinch (small palm, index from above, thumb from below, tips at the gold dot). **No seat shot taken**: check on the headset. |
| 4 | Settings too small | Title: Settings and Back are now the same size as the other two (row symmetric). The game menu's Settings was already the same size. |
| 5 | Wind-blow ghost disappears | Cause: the arrival cloud lesson ended the moment a resting pinch looked at a cloud; the sweep ghost also faded under a nearby hand. Now a look + pinch must hold 0.4 s, the sweep ghost never fades for a hand, and every hide/fade is logged (`[CloudBankPiece] ... lesson over: ...`, `[GhostHands] palm_sweep: hidden`). |
| 6 | Portal cut off near the wind tower | **Not done**: size and offset look right on paper; needs a screenshot of what is cut. |
| 7 | Exit portal after a loss | Not reproduced in the Editor (no debug loss). Guard added: a Keeper's temple gate stays shut and hidden until that Keeper is calmed **and** its shard taken (`KeeperHall.GateLocked`, checked by `ShardGates`), whatever the map says; if the map had shown it, the log says `[ShardGates] ... kept shut`. |
| 8 | Vanes through the tower | Done: hub lower (3.25 -> 2.85) and out (0.72 -> 0.80) on an axle, blades 0.85 -> 0.62: the sweep stays under the walkway. Side render `Logs/PropAudit/some_00.png`. Tower 8.5k -> 9.8k tris. |
| 9 | Opening crackle | Confirmed fixed earlier tonight (`8c1c6cf1`): `shrine.flame` silent under the shard, half volume, no synth crackle. |

## Notes
- Commit `8a8a6674` (32 A) needs `d235bfda` to compile (GestureTestPiece got both changes); the branch head compiles.
- New bridge tools: `Step32Walk.Lily` (arg tag) -> `Logs/WalkSim/lily_<tag>.md`.
