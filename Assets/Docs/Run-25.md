# Run 25 (Nov 9): fix-ups after the paid audio

## Read this first
- **Nobody has heard any of this.** Everything was judged by measurement, in the flat Editor. A device build and the listening list below are next.
- **The Still Isle bed is now the synth bed.** The 4 new paid takes were effectively silent, so the step's fallback applied.
- 1,200 of the 2,000-credit cap spent. Account: 15,479 of 40,000.

| Check | Result |
|---|---|
| Tests | 377/377 |
| Smoke run | 84 ok, 0 failed, 1 known note (opening wait, same as Run-24) |
| Release Check | **PASS**: 0 free clips in cues, 0 free files outside the archive |

## A. Credits: done
`CreditsText.PaidSoundsDone` is on. The sky lanterns and the Settings card show the after-regen line, and no line mentions the free plan.

## B. Footsteps: done
Every paid slice: low-pass at 2.2 kHz, cut to 0.14 s from its onset, 20 ms fade out, re-levelled, then a per-cue gain to meet Kenney's perceived level. "Hi" is the energy above 2.5 kHz relative to the total. A slice more than 10 dB brighter than the Kenney average was retired.

| Cue | Hi before | Hi after | Kenney hi | Paid slices kept | Perceived level vs Kenney |
|---|---|---|---|---|---|
| `step.grass` | -0.5 dB | -14.5 dB | -19.9 dB | 35 of 39 | -0.8 dB |
| `step.path` | -8.8 dB | -24.1 dB | -28.8 dB | 28 of 37 | 0.0 dB |
| `step.stone` | -7.8 dB | -22.3 dB | -17.9 dB | 30 of 31 | +0.3 dB |
| `step.wood` | -32.9 dB | -48.5 dB | -36.8 dB | 24 of 29 | 0.0 dB |

- Every cue keeps well over the minimum of 4, so none fell back to Kenney only.
- **The step prompt's level claim didn't reproduce.** It measured paid steps 5 dB over Kenney. After levelling, their mid band matched and their plain RMS was 3-6 dB *under* Kenney, because Kenney has more low thump. I matched A-weighted level (close to what the ear hears) rather than plain RMS, which would have made the paid steps louder.
- The per-cue gains live only in the levelled files. A forced re-level of every cue would undo them, by 3 dB at most.

## C. Beds: done, with the fallback
**Still Isle:** the 4 new takes passed on swing (5-7 dB) but are not usable: all their energy sits below 250 Hz, and above it they are about -84 dB, too quiet to level up.

| Bed | Second-to-second swing | Above 2.5 kHz |
|---|---|---|
| `amb.stillisle_22` (old, surging) | 32.1 dB | -0.1 dB |
| Synth `amb.stillisle` with 1.2 kHz low-pass (now) | 10.9 dB | -24.9 dB |

- **The synth bed misses the 8 dB aim** (10.9 dB). Its soft gusts are the swing. It is still a third of the old take's swing.
- The low-pass is the cue's `lowPassHz`. I also made a cue's low-pass a ceiling in `AudioService`: without that, the title's temporary filter would have opened the bed up the moment the piano ended.
- Measured about 2 dB under the bed tier after the low-pass. Raise the cue's volume if it sounds thin.

**Other beds** (over about 12 dB, fixed without credits):

| Bed | Before | After | How |
|---|---|---|---|
| `amb.galewick` | 14.2 dB | 4.6 dB | slow gain ride |
| `amb.lilymere.restored` | 17.4 dB | 8.8 dB | slow gain ride |
| `amb.galewick.restored` | 15.3 dB (take 21) | 11.6 dB | take 21 retired, take 22 stays |

The gain ride differs from the step's "regenerate or synth": the last regeneration returned silence, and this keeps the paid sound at no cost. Its gain curve wraps around the file, so the loops stay seamless.

All other beds were under 8 dB. Brightness is left as is (only the Still Isle bed had a brightness brief).

## D. Tidy: done
- 4 `restore.swell` `_FREE` files moved to `_free_archive`. Release Check now fails on a free file outside the archive.
- **The ledger was right.** Its credits column summed to 14,279; 1,428 was the seconds column. Now 15,479, matching the account. `AudioCredits.md` has the plan date (Nov 9). The invoice itself wasn't checked.
- The prompt files are committed.

Retired clips (21) are moved to `_step25_dropped` folders, not deleted, and removed from the paid `manifest.json` so a re-import can't bring them back.

## Listening list, first sounds first
Dev panel > Tools > **Sound A/B** has the two switches.

| # | Where | Listen for | Switch |
|---|---|---|---|
| 1 | Title, first island | **Still Isle bed: steady, soft, no surf.** Is it too thin or dull? | none |
| 2 | Walk on grass, path, stone | **Steps: no velcro hiss**, paid and Kenney takes sound like one family | none |
| 3 | Lodge (wood floor) | Sparse fire pops; steps on wood | none |
| 4 | Area change | Airy swish instead of grinding soil | none |
| 5 | Galewick walk | Bed steady (it was ridden), no pumping; wildlife | none |
| 6 | First battle and catch | Hits and move sounds; `hit.weak` is synth only; catch jingle | **Jingles** |
| 7 | Level up, evolve | Jingles | **Jingles** |
| 8 | Tower knots | Rope creak and pluck | none |
| 9 | Tower Keeper's first line | Owl call, then hoots as it types | **Voices** |
| 10 | Keeper beaten, shard | Keeper and shard jingles | **Jingles** |
| 11 | Galewick restored | One warm gust, then the restored bed | none |
| 12 | Cinderwood, Lilymere, forge | Beds; `amb.cinderwood` first; Lilymere restored for pumping (ridden) | none |
| 13 | Hearthstag and Tidemother halls | Stag and whale calls and grains | **Voices** |
| 14 | The Heart | Bell-like call and murmurs, no moo | **Voices** |
| 15 | Credits | Lantern line reads "Sounds: synthesised and generated for Pocket Isle" | none |

Run-24's headset test list (battle row, doors, pour) is still open too.

**A device build is still needed before calling this done.**
