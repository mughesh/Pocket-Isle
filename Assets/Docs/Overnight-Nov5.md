# Overnight Nov 5: morning report

Branch `overnight-nov5` (not pushed, not merged). Master was pushed first at `2f2bdd5` (your notes). Tests **318/318**.
No headset or build tonight: everything was checked in flat Editor Play. Saves, XR-init-on-startup and the time scale are back
as they were.

**Keep it:** `git checkout master && git merge overnight-nov5 && git push`
**Throw it away:** `git checkout master && git branch -D overnight-nov5`

## 1. Smoke run (`Tools > Pocket Isle > QA > Smoke Run`)
- **What it does:**
  - Sets both saves aside and turns XR-on-startup off for the run.
  - Plays 84 beats with the dev tools and synthetic input, then puts everything back when Play stops.
  - Report: `Logs/SmokeRun/report.md`, plus `run.log`. Stop it early with `Tools > Pocket Isle > QA > Smoke Run: Stop`.
- **Covers:**
  - New adventure: the title, the opening, every lesson, the first battle and catch.
  - Shrine and lodge: poke an Isling, swap a pod, the rim Journal tablet.
  - Pause menu, Settings, the Journal (pages, a card, the layout check).
  - Evolution in a synthetic palm.
  - Save, quit, Continue (same place and party).
  - Every Cinderwood and Lilymere moment, Hearthstag and Tidemother, both shards to the statue.
  - Quick Journey from the gift: the send-off, the wind gate, every Galewick moment, the tower, Galeowl, the wind shard, the end card, See the ending, the finale to its end.
- **Flags:**
  - exceptions and errors (unknown audio cue ids are errors)
  - the same warning 15+ times
  - nothing changing for 20 s
  - a line shown out of its context
  - tablet overlaps (the 14j check)
  - a battle label inside a creature's body
- **Last run (with all the polish on): 84/84 ok, 0 exceptions, 0 errors, 0 out-of-context lines, 0 tablet overlaps.** 7 minutes.
- Notes left in the report, none a bug:
  - "No progress for 20 s" in the opening (the starter wait) and during the finale: both are scripted waits.
  - Tidemother's label sits in the core of its drawn box. The label draws on top and Tidemother is huge, so check it on the headset.

### Bugs found / fixed / left
- The first runs failed on my own driving script, not the game. Four were fixed in the run, commit `e0daf98`: retrying the statue trip, palm over each lily bud, skipping an inactive copy of the lily pad piece, walking into the tower once it opens.
- **The smoke run's cheats leaked into the EditMode tests** (4 failures) when I stopped a run mid-Play. Fixed in `bd39359`: they're reset when a run ends.
- **No game bugs found.** Nothing is left open from the run.

## 2. Release readiness (`Tools > Pocket Isle > Release > Readiness Report`)
Writes `Assets/Docs/Release-Readiness.md` and **`Assets/Docs/Audio/Regen-List.md`** (each cue, the clips it keeps, the prompt, the length).

| | |
|---|---|
| OK | package `com.mugesh.pocketisle`, 0.1.0 (code 1), min SDK 32 / target 34, ARM64, IL2CPP, no POCKET_DEV in the Android defines (QA adds it per build), hands + controllers, app name |
| **To do** | **keystore not set**; **app icon not assigned** (made, see below); **237 free-plan clips in 79 cues** to regenerate |
| Check | the last QA APK asks for `RECORD_AUDIO`, `BLUETOOTH` and `INTERNET`. RECORD_AUDIO comes from Meta's Immersive Debugger AI assistant (only if it's on) or the Voice SDK; review before the store build |
| Music | every music cue is still waiting for a clip; the only source is `music.hub_1.mp3` (Gemini / Lyria, licence to check) |

## 3. Icon and key art
- `Tools > Pocket Isle > Release > Make Icon + Key Art (Play mode)` renders the game's own logo over a sky-teal gradient.
  - Icons: `Assets/_Project/Art/Icon/PocketIsle_Icon_{1024..36}.png`.
  - Key art: `Builds/Marketing/PocketIsle_KeyArt_1920x1080.png` (Cinderwood with the logo over it; re-run on a nicer spot if you like).
- **Not assigned:** ProjectSettings.asset already has an unrelated uncommitted change, so assigning it would mix the two. One click: **Tools > Pocket Isle > Release > Assign App Icon** (sets Default Icon only).

## 4. Visual polish (switches in `Data/Resources/PolishConfig.asset`)
| Item | What | Switch |
|---|---|---|
| Ambient life | butterflies (hub), fireflies (Cinderwood), a bird crossing now and then (Galewick, restored), dragonflies and water sparkles (Lilymere, restored); fewer when faded; never inside or in battle | `ambientLife` |
| Battle feel | element hits (fire sparks rise, water splash ring, wind swirl, neutral dust); super effective: gold ring + 40 ms hit-stop on the stage's tweens; catch: a white "click" ring, three gold stars, then the existing flight to the rim | `battleFeel`, `hitStop` |
| Restoration wave | a soft ring of the realm's colour just ahead of the wave, leaves / petals / sparkles lifting behind it, only inside the window | `restoreWave`, `waveLifts` |

- **Draws:** ambient life adds 2 (one instanced critter mesh, one particle system). Battle feel and the wave use the existing pooled battle particles (0 new).
- **Perf:** CPU not measured precisely yet (`AmbientLife.LastMs` holds the last frame's cost); no device capture.
- **Shots:** `Logs/BattleShots/nov5_cw_faded.png`, `nov5_cw_restored.png`, `nov5_hub_butterflies.png`, `nov5_lm_restored.png`.
  - The butterflies were too small in the shot, so I scaled the critters up after it (not re-shot).
  - No shots of the battle effects or the wave (hard to time in flat Play).

## 5. Video helper (dev panel > Tools > Video)
- **Video mode:** FPS counter, dev readouts and debug labels stay off while it's on. The game UI is unchanged. The menu's small Dev button stays so you can turn it off again.
- **Clean hints:** no idle hints and no filler chatter. Objectives, intros, story lines and lesson chips stay.
- Neither is saved.

## Headset test list (most at risk first)
1. **Battles:**
   - A super-effective hit: a gold ring, a short freeze; the battle must not stall afterwards.
   - A catch: the click ring and three gold stars, then the pod flies to the rim.
   - Fire / water / wind / neutral hits look different.
2. **Ambient life:**
   - Cinderwood fireflies, hub butterflies, Lilymere dragonflies: visible but small, and they never show outside the island's edge.
   - Hold 72 fps in Cinderwood (OVR Metrics).
3. **Restoration wave** (calm a Keeper): a coloured ring sweeps ahead with leaves lifting; nothing outside the island.
4. **Video mode / Clean hints:** toggle both in the dev panel; idle chatter stops, objective chips stay.
5. Everything from step 14k's list is still pending too (grab first).
