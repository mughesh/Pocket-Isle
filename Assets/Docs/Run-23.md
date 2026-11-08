# Run 23 (Nov 9): checkpoint, merge, step 23 Part 1 (audio balance)

**Part 2 (ElevenLabs, sections E-G) is ready to run and was not started.** Run it after buying the plan.

**Not pushed.** The permission system refused the push, so `master` and the tag are local only. To publish, in Git Bash:

`git push origin master && git push origin nov8-checkpoint`

Flat Editor Play, tests and smoke run only. Nothing here has been heard on a headset.

## Step 1 checkpoint (on `step-22`, nothing stashed)
| Commit | What |
|---|---|
| `2b6f894` | Docs up to Nov 8 |
| `603d3f0` | Step 22 F: the Nov 8 notes (trail, Keeper bubble, rune and shard ghosts, restore.swell, "ran off" caption, healed moment after a loss) |
| `336b1f6` | Step 15a (WIP): release build setup |

- Master fast-forwarded to `336b1f6`, tagged `nov8-checkpoint`. Tests 365/365, smoke 84 ok, 0 failed.
- `.gitignore` now covers `*.keystore` and `*.jks`. The keystore lives outside the repo; no passwords are in the commits.
- `QualitySettings.asset`: the big diff is the unused PC tier and the per-platform table being removed. The Quest tier stays at 4x MSAA, render scale 1 (`Mobile_RPAsset`).
- Other settings in that commit: Meta XR Audio spatializer set, Unity Connect on, version 1.0.0 / code 11, icons, keystore path.

## Part 1 status: done (tests 365/365, smoke 84 ok, 0 failed; one "no progress" note on the opening wait, known)
One commit rather than one per section: A and B share `AudioDirector` hunks.

## Headset A/B list, in play order
| # | Where | Listen for | Old -> new |
|---|---|---|---|
| 1 | Title screen | Piano clearly over the ambience | title 0.25 -> 0.42; amb.stillisle -3.5 dB, 1.2 kHz low-pass while title/hub play |
| 2 | Still Isle welcome | Same | hub 0.25 -> 0.40 |
| 3 | Lodge | Music audible, fire not hissy | lodge 0.18 -> 0.35; amb.lodge -4 dB, natural take only; 1.8 s fade leaving (was 1.5, or 3 into silence) |
| 4 | Galewick walk | Steps soft, tiny, no "velcro" | step.* -6 dB, pitch +/-8 %, 2 kHz low-pass, cut at 0.3 s, no synth takes |
| 5 | Wild battle + catch | Unchanged on purpose | battle.wild 0.25; jingles untouched |
| 6 | Tower knots | Woody ticks and a soft pluck, no buzz | knot.pull, keeper.knot.snap re-voiced |
| 7 | Shard taken | Track blooms in faster | 3 s -> 1.8 s, no 1.2 s wait |
| 8 | Leaving Galewick's tower | **A breeze with a wooden chime, not a wave** | new `restore.breeze`, Galewick only; other realms keep the chord swell |
| 9 | Area changes | Quieter, no abrupt end | scene.rise/sink, gate.enter -6 dB; natural takes; rise and sink fade 0.3 s after the area is up |
| 10 | Restored realm, temple | Music sits with the bed | galewick, lilymere 0.25 -> 0.30; cinderwood, temple 0.25 -> 0.28 |
| 11 | Finale | Birds behind the climax | finale 0.40 -> 0.38; Still Isle bed and birds -6 dB while it plays |

Dev panel > Tools > Music rows flip the music levels (use the old numbers above). The other changes have no flip button. Music rows use a new saved key, so values set on the headset before this don't hide the new levels.

## Decisions to check
- **Item 8 reintroduces filtered noise**, as section D asked, after your wave complaint. If it still reads as a wave, delete the Galewick branch in `KeeperHall.Restore` and the chord plays.
- Nothing in the code cut off the area-change sounds: the swap never stopped them. The fade is new, the cut-off itself wasn't found.
- Sweeteners now duck with the bed everywhere (also -3 dB in dialogue, -6 dB under the finale); the finale's birds needed it.

## Cue audit (C): every clip can play, picked at random, never the same twice in a row; review status doesn't filter
| Cue | Before | Now plays |
|---|---|---|
| step.grass | 6 synth, 5 Kenney, 11 EL free | 5 Kenney, 11 EL free |
| step.path | 6 synth, 5 Kenney, 12 EL free | 5 Kenney, 12 EL free |
| step.stone | 6 synth, 10 Kenney, 14 EL free | 10 Kenney, 14 EL free |
| step.wood | 6 synth, 5 Kenney | 5 Kenney |
| amb.lodge | synth, 1 EL free | 1 EL free |
| scene.rise / scene.sink | synth, 2 EL free each | 2 EL free each |
| amb.stillisle, amb.galewick, amb.galewick.restored | synth, 1 EL free each | **unchanged: still mixed**; Part 2 replaces them |
| restore.swell | synth, 2 EL free | synth (step 22) |
| wind.sweep, gate.enter, jingles (6) | 1 synth each | same |
| knot.pull / keeper.knot.snap | 3 / 2 synth | same counts, re-voiced |

Removing the synth takes doesn't lower the free-clip count, so the Release Check still needs Part 2.

Knots, measured (dB): knot.pull below 300 Hz -28.7 -> -33.5, with 400-900 Hz now the main band at -18 to -20 of -18 total.
