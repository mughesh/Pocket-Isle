# Run 20: the soundtrack goes in (Nov 8, overnight; steps 19 and 20)

Branches:
- `step-19` comes off `step-18`; its report is `Run-19.md`.
- `step-20` comes off `step-19`; this report.

Neither is pushed or merged. Testing was flat Play via the bridge, tests and smoke runs. Nothing has been tested on the headset.

## Headset test, steps 19 + 20 in play order

| # | Do | Expect | Log line |
|---|---|---|---|
| 1 | Start the game (shard first) | No music while only the shard floats | `[AudioDirector] Only the shard` |
| 2 | Title shows | **Title track** fades in (43 s, fades itself) | `[Music] None -> Title`, `music.title starts` |
| 3 | New journey / Continue | Title track **keeps playing** to its end (no cut, no restart); the opening/tutorial on the Still Isle is otherwise quiet | `Title -> None (-, music.title plays on to its end)` |
| 4 | First battle (tutorial) | Battle track in (0.6 s); after it, ambience only | `Hub -> BattleWild`, `BattleWild -> Hub (3 s, welcome already played)` |
| 5 | Enter the Hearth Lodge | **Lodge track** once (27 s, soft). Check you can hear it: 0.18 measures ~6 dB under the lodge's ambience | `None -> Lodge`, `music.lodge starts` |
| 6 | Leave the lodge | It fades out (1.5 s); Still Isle quiet | `Lodge -> None` |
| 7 | Walk into Galewick (unrestored) | **No music**, the thin ambience only | no `[Music]` line, or `-> None` |
| 8 | Wild battle there, win or run | Battle track during; silence after | `None -> BattleWild`, `BattleWild -> None (3 s)` |
| 9 | Into the tower | **Temple track** (Rite of the Unbroken) fades in | `None -> Temple (3 s)` |
| 10 | Keeper battle | The temple track **carries on, no restart, no crossfade** | `Temple -> BattleKeeper (0.6 s)` with no `music.temple starts` after it |
| 11 | Keeper calmed | Fades to silence over 2.5 s | `-> None (2.5 s)` |
| 12 | Take the shard (the bloom) | **Galewick's track** fades in (3 s) under the restored ambience | `None -> Galewick (3 s)` |
| 13 | Walk out of the tower | Same track still playing | (no change) |
| 14 | Home to the Still Isle | **Still Isle welcome** once (43 s), then ambience only | `Home from a realm...`, `None -> Hub`, `music.hub starts` |
| 15 | Dev panel > Tools > Music > "Realm music: always", walk into an unrestored realm | Its track plays (step 18 behaviour). Switch back to "reward" | `Realm music: always` |
| 16 | Place the last shard (or Dev > See the ending) | **Finale track** from the wake | `music.finale starts` |
| 17 | After the Hall of Fame the Spark returns, then the credits | First **sky lantern** lifts off the forehead with a gold spark and a soft flutter | `Credits: 5 sky lanterns` |
| 18 | Each lantern | Stops at ~eye level in front of the statue, turns to you, readable without leaning in (the "Sounds" one is the smallest). Two at once never touch | `Lantern "..." reading: NN cm over the cap ... first line cap NN mm` |
| 19 | The big lantern | Last, centre, closer, your picture framed (head and shoulders) | |
| 20 | It rises away | "Thank you for playing" card comes up; the finale track plays on to its end | `music.finale plays on to its end` |
| 21 | In passthrough | Leaving lanterns fade cleanly (no dark ghost) | |
| 22 | Run the ending again, press Skip in the Hall of Fame or credits | Lanterns fade in 0.4 s, finale track fades over 1.5 s, the card comes up | `Finale skipped: music.finale fades` |
| 23 | Levels, everywhere | Music under the ambience peaks and voices; tune per cue in **Dev panel > Tools > Music** (- / + 0.05, remembered) and send me the numbers | |

## Done / partly / not (step 20)

| # | Item | Status | Notes |
|---|---|---|---|
| 1 | Move the tracks (GUIDs kept); First Light out of the project | done | `Step20Audio.MusicIn`. First Light now at `D:/UNITY/projects/_audio-sources/music/First_Light_at_the_Summit.mp3` |
| 2 | Import: 2D, Streaming, Vorbis 0.5, stereo, no normalize; Sync; `music.temple` + `AudioIds.MusicTemple` | done | All 8 music cues have their clip. `music.battle.keeper` is still awaiting a clip; nothing plays it now |
| 3 | Unrestored realm = no music; `RealmMusicBeforeRestore` + Dev toggle | done | Default **reward**; the toggle is remembered on the device |
| 4 | The bloom | done | Starts when the realm's shard is taken (`PuzzleState` stage ≥ 1), not when restoration hits 99 %, so it's playing when you walk out (the ambience still reads "Faded" for a moment) |
| 5 | Temple in halls, through the Keeper battle without a restart; 2.5 s fade on calm | done | Fixed on the way: the Keeper reference drops before the battle ends, which briefly switched to the wild battle track. The battle now stays a Keeper's to its end |
| 6 | Title once, no restart into the tutorial; no music while only the shard floats | done | Fixed on the way: the Still Isle track flashed on between the shard and the title. The opening is now quiet on the Still Isle |
| 7 | Still Isle once per arrival | done | The title counts as the welcome after a load or Continue. Returning from a realm plays it again |
| 8 | Lodge once on entering, fades out on leaving | done | |
| 9 | Finale from the wake, plays to its end, Skip fades 1.5 s | done | The finale and title tracks are never cut by silence: they play on to their end |
| 10 | Wild battles unchanged | done | |
| 11 | Area crossfades 3 s; battle 0.6 s; dialogue / jingle ducks kept | done | |
| 12 | Levels 0.25 / finale 0.4 / lodge 0.18, measured | done | Table below |
| 13 | Dev panel: a row per cue | done | Title, Still Isle, Lodge, Galewick, Cinderwood, Lilymere, Temple, Battle, Finale |
| 14 | AudioCredits.md | done | "Made with Google Gemini (Lyria) by Mugesh; licence terms to be checked before submission". App credit unchanged: "Music: Mugesh" |
| 15 | Flat Play sequence + "always" once + smoke | done | Logs below |

**Track-to-realm mapping:** I kept the design chat's mapping (Steps to Galewick, Lanterns to Cinderwood, Lotus to Lilymere). I can't judge by ear here whether each track fits its realm; please listen on the headset.

## State → cue

| Where / what | State | Cue | Mode | Level |
|---|---|---|---|---|
| Shard only (opening) | None | - | - | - |
| Title screen | Title | `music.title` | once (43 s) | 0.25 |
| Opening / tutorial on the Still Isle | None (title plays on) | - | - | - |
| Still Isle, arriving from a realm | Hub | `music.hub` | once per arrival | 0.25 |
| Hearth Lodge | Lodge | `music.lodge` | once per entry | 0.18 |
| Realm, unrestored ("reward") | None | - | ambience only | - |
| Realm, shard taken / restored (or "always") | Galewick / Cinderwood / Lilymere | `music.<realm>` | whole, then 45 s rest | 0.25 |
| Keeper hall, Keeper not calmed | Temple | `music.temple` | loops | 0.25 |
| Keeper battle | BattleKeeper | `music.temple` (same: no restart) | loops | 0.25 |
| Keeper calmed, shard not taken | None | - | 2.5 s fade | - |
| Wild battle | BattleWild | `music.battle.wild` | loops; after it the area's rule (rest first) | 0.25 |
| Finale (wake to credits) | (AudioDirector) | `music.finale` | once (75 s), Skip fades 1.5 s | 0.4 |

## Levels (BS.1770 integrated, file + cue volume, music bus 1)

| Music | File | Cue | In game | | Ambience bed | In game |
|---|---|---|---|---|---|---|
| title / hub | -17.3 LUFS | 0.25 | **-29.4** | | Still Isle | -23.7 |
| lodge | -18.3 | 0.18 | **-33.2** | | Lodge | -27.2 |
| galewick | -11.7 | 0.25 | -23.8 | | Galewick restored | -25.5 |
| cinderwood | -11.6 | 0.25 (+2 dB restored) | -23.6 (-21.6) | | Cinderwood restored | -26.0 |
| lilymere | -11.3 | 0.25 | -23.3 | | Lilymere restored | -28.3 |
| temple | -12.6 | 0.25 | -24.7 | | Forge hall | -28.3 |
| battle.wild | -13.1 | 0.25 | -25.2 | | (battle ducks ambience 6 dB) | |
| finale | -10.8 | 0.4 | **-18.7** | | Still Isle | -23.7 |

Spirit blip: -17.1 LUFS file × 0.8 × bubble 0.75 ≈ -21.6 (short bursts), so above every music cue except the finale.

What this means (my reading; please check by ear):
- **Realm and temple tracks** come out 2–5 dB *over* their restored beds. The prompt wants them under the ambience peaks. The sweeteners peak well above the bed, so this may be fine, but **if a realm track sits on top, step it down to ~0.15–0.2**.
- **Title and Still Isle** (the quiet intro cut) are ~6 dB under the Still Isle bed: they may be hard to hear. Try 0.4.
- **Lodge** at 0.18 is ~6 dB under the lodge bed: probably inaudible. Try 0.35.
- **Finale** at 0.4 is ~5 dB over the bed; the prompt allows "a bit above".

## Music sequence (final smoke run, step 20 end state)

```
[AudioDirector] Only the shard: no island ambience or music yet.
-- New game / opening: the shard, then the title
[Music] None -> Title (3 s).                                  music.title starts (43 s, once)
-- opening lessons
[Music] Title -> None (-, music.title plays on to its end).   music.title ended (once)
[Music] None -> Hub (3 s, welcome already played).
-- first battle
[Music] Hub -> BattleWild (0.6 s).                            music.battle.wild starts
[Music] BattleWild -> Hub (3 s, welcome already played).
-- into the lodge / leave
[Music] Hub -> None (1.5 s).  [Music] None -> Lodge (3 s).    music.lodge starts (27 s, once)
[Music] Lodge -> None (1.5 s).  [Music] None -> Hub (3 s, welcome already played).
-- quit and Continue
[Music] Hub -> Title (3 s).                                   music.title starts
[Music] Title -> None (-, music.title plays on to its end).
-- Cinderwood: moments (unrestored: no music line at all), then the Keeper
[Music] None -> Temple (3 s).                                 music.temple starts
[Music] Temple -> BattleKeeper (0.6 s).                       (no restart)
[Music] BattleKeeper -> Temple (3 s).                         (same cue: no restart)
[Music] Temple -> None (2.5 s).                               calmed
[Music] None -> Cinderwood (3 s).                             the bloom: music.cinderwood (then 45 s rest)
-- home with the fire shard
[Music] Home from a realm: the Still Isle's welcome may play again.
[Music] Cinderwood -> None (1.5 s).  [Music] None -> Hub (3 s).   music.hub starts (once)
-- Lilymere (already restored in the smoke save)
[Music] Hub -> None (3 s).  [Music] None -> Lilymere (3 s).   music.lilymere starts
[Music] Lilymere -> BattleKeeper (0.6 s).                     music.temple starts
[Music] BattleKeeper -> Lilymere (1.5 s, after 45 s of rest).
[Music] Home from a realm...  Lilymere -> None (1.5 s).  None -> Hub (3 s).   music.hub starts
-- Quick Journey (reload, Continue)
[Music] Hub -> Title (3 s).  Title -> None (-, plays on).  None -> Hub (-, welcome already played, title plays on).
[Music] Hub -> None (-, music.title plays on to its end).     Galewick, unrestored
[Music] None -> Temple (3 s).  Temple -> BattleKeeper (0.6 s).  BattleKeeper -> Temple (3 s).  Temple -> None (2.5 s).
[Music] None -> Galewick (3 s).                               the bloom: music.galewick
[Music] Home from a realm...  Galewick -> None (1.5 s).  None -> Hub (3 s).   music.hub starts
-- See the ending
[Music] Hub -> None (2 s).                                    music.finale starts (75 s, once)
[Music] None -> Hub (-, welcome already played, music.finale plays on to its end).
```

The **"always" toggle** was checked once in a focused flat run (Quick Journey at the tower door, unrestored Galewick):
- On arrival: `area GalewickFaded`, no music.
- Wild battle: `None -> BattleWild (0.6 s)`, then `BattleWild -> None (3 s)`.
- "Realm music: always": `None -> Galewick (3 s)`, `music.galewick starts`.
- Back to "reward": `Galewick -> None (3 s)`.

The title doesn't restart on a reload while it's still playing.

## Checks

- **Tests:** 354/354. New `Step20Tests`: every music cue's clip, level and mode, and the state-to-cue map. The test assembly now references `Diorama.Audio`.
- **Smoke run:** 84 ok, 0 failed, 6.6 min, with 2 known notes:
  - The opening's "no progress" wait, which happens every run.
  - "HP label inside Tidemother's box": a huge Keeper's bounds cover its label, already noted Nov 5/6; it comes and goes between runs.
- **Save and XR:** the save is restored byte-identical and XR init on start is on.

## Mistakes and decisions to know about

- **Hall duck:** step 18's `HallDb` (-6 dB for a realm's track inside its hall) is no longer applied. Halls now have their own rule (the temple track), and the bloom should be heard at full level.
- **Battle Keeper slot:** `music.battle.keeper` stays an awaiting slot; Keeper battles use `music.temple`.
- **Step 19's commit mistake:** see Run-19.md. Section A swept in staged deletions, so I redid both commits so each one compiles.
