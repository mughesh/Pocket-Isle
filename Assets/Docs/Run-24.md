# Run 24 (Nov 9): step 24 A-D, then the paid audio

## Read this first
- **Paid audio ran: all five priorities done, 14,279 of 40,000 credits, 10 credits per second.** It needed a new API key with User: Read, so it ran after the first report. See `Audio/Regen-Report.md`.
- **Nobody has heard the new sounds.** Takes were picked by measurement.
- Flat Editor only: nothing heard or felt on a headset. A device build is still needed.

| Check | Result |
|---|---|
| Tests | 372/372 |
| Smoke run | 84 ok, 0 failed, 1 known note (opening wait) |
| Release Check | **PASS: 0 free clips** |

## Listening list, in play order
Dev panel > Tools > **Sound A/B** has the two switches. Both start on the old sound and are remembered on the device.

| # | Where | Listen for | Switch |
|---|---|---|---|
| 1 | Title, first island | Still Isle bed: **only one take passed, check it isn't too thin** | none |
| 2 | Walk on grass, path, stone | New tiny steps mixed with Kenney's; similar level | none |
| 3 | Lodge (wood floor) | Sparse fire pops; steps on wood | none |
| 4 | Area change | Airy swish instead of grinding soil | none |
| 5 | Galewick walk | Bed and wildlife | none |
| 6 | First battle and catch | Hits and move sounds; **`hit.weak` is synth only now**; catch jingle | **Jingles: synth / acoustic** |
| 7 | Level up, evolve | Jingles | **Jingles** |
| 8 | Tower knots | Rope creak and pluck (no synth now) | none |
| 9 | Tower Keeper's first line | Owl call, then hoots as it types | **Voices: blip / new** |
| 10 | Keeper beaten, shard | Keeper and shard jingles | **Jingles** |
| 11 | Galewick restored | One warm gust, then the new restored bed | none |
| 12 | Cinderwood, Lilymere, forge | Regenerated beds; `amb.cinderwood` first (more low end) | none |
| 13 | Hearthstag and Tidemother halls | Stag and whale calls and grains | **Voices** |
| 14 | The Heart | Bell-like call and murmurs, **no moo** (fewest grains) | **Voices** |

Log for the switches: none. The Spirit keeps its blip in both modes. An entrance call plays on a speaker's first line, or after 30 s without its lines.

The sound change from D: `water.pour` now starts when the stream starts, not when the pour counts.

## Shots from the seat
| | Before | After |
|---|---|---|
| Field | ![](Run-24-img/A_field_before.png) | ![](Run-24-img/A_field_after.png) |
| Keeper hall | ![](Run-24-img/A_keeper_before.png) | ![](Run-24-img/A_keeper_after.png) |

## What changed
| § | Commit | What |
|---|---|---|
| A | `76ec070` + the final place commit | Moves over the arena's front band. New fields `overArena`, `arenaInset` 0.04, `arenaHover` 0.03. Battle line goes to the row's end away from the player's Isling |
| B | `e2e8f8c` | Keeper label is the name only, rematches too. Wild and party keep their level |
| C | `7db68f7` | A bump re-asks at once (1 s debounce). `temple.warn` never goes stale. Refusals log `[TempleDoor] refused: ...` |
| D | `767cfab` | Water stays level and slides low. Stream starts about 20 degrees, full at 60. Hand drains, bowl rises. Both ghosts tip gradually |

Seat check (`Step24Debug.Seat`), both stages: **nothing covered, 3.9 cm from move to nearest rim pod.**

## Decisions to check
- **Row height is 1.8 cm over the cap, under the 2-3 cm you asked for.** At 2.8 cm it clipped 7% of the player's label in the hall. To change it: `BattleRunes.arenaHover`.
- **Pods are 3.9 cm away, a hair under about 4.** Further back starts to crowd the player's label.
- **Only the fingertip now counts as reaching for a pod.** Over the arena, the palm of a hand poking a move sits above the pods and would have ducked the row (R5).
- **Water only leaves over the bowl.** A tilted palm on the way there doesn't leak. Turning the hand right over away from the bowl still spills, as before.
- **The forge had a worse bug than the note.** It only listened to steps, so after "Not yet" you stood on the threshold and bumping did nothing. It now listens to bumps. A bump on its shut door says `cw.forge.cold` ("stone cold, needs a spark"), the closest existing line; say if you want a better one.
- A long battle line ("Toastling needs a rest...") still sits centred when no Isling is out.

## Not verified
- **The tilted pour's look was not seen, even in flat.** The logic has unit tests and the smoke run covers look + pinch, but the real-hand stream, level water and ghost timing are headset only.
- Explorer locked: no bump fires at all, so there is no door message to show. Left as is.

## Headset test list
1. Wild battle: moves sit over the arena, rim and pods fully visible in front of them.
   - Log: no `[BattleRunes] A hand reaches for a pod` while poking a move.
2. Reach for a rim pod during a battle: the row ducks, and a pod grab is never a move press.
   - Log: `[BattleRunes] ... ignored: a hand is on a pod`.
3. Keeper battle: label shows the name, no Lv.
4. Temple door with a small team: Not yet, then bump again: asks at once. Do it at the tower, the forge and the lake temple.
   - Log: `[TempleRules] ...: Go in / Not yet`.
5. Lilymere pour, one hand: tilt slowly over the bowl: dribble, then a thicker stream; tilt back and it stops. Pours 1/2 ticks partway through.
6. Same with both hands cupped.
7. Quest frame rate in a battle (the row is unchanged in cost, but check).

**A device build is still needed before calling this done.** Fields are new, so builds read the code defaults from the scene file.
