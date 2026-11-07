# Run 18: Nov 8 long play fixes + the first music test

Branch `step-18` from `step-17` `1f7eff1` (master and origin were fast-forwarded to `1f7eff1` first, as Mugesh asked).
Not pushed, not merged. Every section committed on its own, "(headset test pending)". Run unattended on Nov 8, 02:30 to ~03:20.

Mugesh's own notes on top of the prompt (they win where they differ):
- Statue: **no palm ghost**, the shard is pinched or grabbed and placed (prompt B13 wanted a palm-up ghost).
- **Battle music**: `Tall_Grass_Duel.mp3` on battles (the prompt kept battles silent).
- **New voices** for the tower creatures and a special one for the statue (the prompt's "Not doing" list had it).
- Evolve heals, poke the rim pods, swipe/tap gap: the same as the prompt's A1, D, F.

| | Section | State | Commit |
|---|---|---|---|
| A | Battles don't wipe a cozy player | **done** (sim + tests; the rest heal seen in flat Play) | `9be3e2a` |
| G | First music (Galewick + battles) | **done** (start, battle switch, rest and restart seen in flat Play) | `f29f7ed`, `3dce30d`, `37f1de8` |
| B | Statue and shards | **done** (code; not seen in a shot) | `c1e1452` |
| C | Tall grass in battles | **done** (one battle shot) | `5f7030a` |
| D | Poke a rim pod for its card | **done** (code; needs real hands) | `fdc3ff1` |
| E | No party pods under the title | **done** (seen in the log) | `fdc3ff1` |
| F | Swipe → tap as one move | **done** (clip rebuilt and measured) | `8b445b5` |
| + | Voices: Keepers and the Heart (variations of the Lantern's blip) | **done** (baked; not heard in Play) | `f794eab`, `7ddf031` |
| + | Big bodies faint by slumping | **done** (code; not seen in a shot) | `488552c` |
| + | Finale pace | **done** (smoke run) | `17200e2` |
| + | Smoke run places the dreamed shards | **done** | `8733504` |

Tests: **348/348** edit mode. Smoke run: see the end.

## A. Battles (old → new)
- **Evolving heals:** `Progression.Evolve` kept the HP ratio and kept a fainted creature at 0 → the new form comes out at
  **full HP**, never fainted (test `Evolving_ComesOutAtFullHp_EvenFromFainted`; the old ratio test now expects full HP).
  The rim pod's fainted look reads `IsFainted` every frame, so it clears at once.
- **A little rest after a win:** after a won or caught **wild** battle, every non-fainted pod gets back **25% of max HP**
  (`Party.RestAfterWin`, `Party.RestShare`), a green "+" over its pod and the soft `shrine.heal` sound. Fainted stay
  fainted. Keeper battles unchanged. Flat Play: Kitelet 27/55 → 50/62 (with its level-up), Toastling 20/40 → 30/40.
- **Wild damage x0.75** (`BattleMath.WildDamageShare`, wild only, not Keepers, not the player). The rule: a wild Isling
  never knocks out a full-HP party Isling of its level or higher in fewer than 3 hits (2 if super effective). Worst case =
  its hardest attack at the top roll (1.1), against every non-Keeper species in the registry.
  - Sweep of the share (cases breaking the rule): 1.00: **36**, 0.95: 19, 0.90: 10, 0.85: 7, 0.80: 1, **0.75: 0**.
  - Fewest hits, worst defender (`*` = super effective):

| Wild | Before (x1.0) +0 / +1 / +2 | After (x0.75) +0 / +1 / +2 |
|---|---|---|
| Samarling Lv 5 | 2* / 2* / 2* | 2* / 3 / 3 |
| Samarling Lv 8 | 2* / 2* / 2* | 2* / 2* / 3 |
| Vanehare Lv 5 | 2* / 2* / 2* | 2* / 2* / 3 |
| Vanehare Lv 7 | 2 (neutral, Kettlewing) / 2* / 2* | 2* / 2* / 3 |
| Kitelet Lv 5 | 2* / 2* / 2* | 2* / 3 / 3 |
| Ashmole Lv 5 | **2 neutral** (Kettlewing, Kilnhearth, Kilnpot, Lilyvole, Pebbloop, Sailfold, Toastling) | 2* / 3 / 3 |
| Ashmole Lv 8 | 2 (Cindercap) / 2 / 2* | 2* / 2* / 3 |
| Coalbeetle Lv 7 | 2 (Kettlewing) / 2* / 2* | 2* / 2* / 3 |
| Coalbeetle Lv 8 | 2* / 2* / 2* | 2* / 2* / 2* |

  Before, many neutral matchups were 2 hits at the top roll (that's how four Islings went down in a row). After, every 2 is
  super effective. The full 5 x 4 x 3 table prints from `Step18bTests.WildIslings_NeverOneOrTwoShotAPartyIslingOfTheirLevel`.
- **Forced switch-in moves first** on the next turn (`BattleSystem._freshSwitchIn`; test with a slow Samarling vs a fast
  Vanehare).
- **Pod lights sooner:** `RedHpShare` 0.25 → **0.33**. The "locked at a sliver" report: in code the cradle glow follows
  `CatchUnlocked` after every turn, and 14% unlocks (test). Not reproduced; with 33% it unlocks earlier anyway.

## G. Music
- Files moved (GUIDs kept): `Clips/Music/music.galewick_1.mp3` (First Light at the Summit) and
  `Clips/Music/music.battle.wild_1.mp3` (Tall Grass Duel). Import: 2D (was 3D), Streaming, Vorbis 0.5, stereo, no normalize.
- **Galewick:** cue `music.galewick`, volume **0.25**, new `restSeconds` **45**: plays once to its own fade-out, 45 s of
  ambience only, again from the top. The first play waits for the island rise, the title and the realm name card, then
  fades in over 3 s (it first started under the title; fixed in `37f1de8`). Inside the tower it keeps going at **-6 dB**
  (`MusicDirector.HallDb`).
- **Battles:** `music.battle.wild` (0.25, loops), 0.6 s crossfade in. Keeper battles fall back to it in code until a Keeper
  track exists (`music.battle.keeper` awaits clips; Sync Cues clears a clip shared across cue names, so no sharing).
  After a battle the Galewick track does **not** snap back: 45 s rest, then from the top.
- Flat Play log: `None -> Galewick (3 s)` after Continue; `Galewick -> BattleWild (0.6 s)`; `BattleWild -> Galewick (1.5 s,
  after 45 s of rest)`; then `music.galewick starts`.
- **Levels (file loudness + cue volume, music bus at 1):**

| Sound | File | Cue | Estimate in game |
|---|---|---|---|
| Galewick music | -11.8 LUFS | 0.25 (-12 dB) | about **-23.8 LUFS** |
| Battle music | -13.1 LUFS | 0.25 | about -25.1 LUFS |
| Galewick ambience bed (`amb.galewick.wav`) | -27.9 LUFS | 0.8 | about -29.8 LUFS |
| Spirit blip | peak -1 dBFS | 0.8 x bubble 0.75 | peaks about -6 dBFS (short) |

  So at 0.25 the music is about **6 dB over the ambience bed** and well under the Spirit's blips. The prompt wants it under
  the ambience: if it feels on top on the headset, step it down in **Dev panel > Tools > Music** ("Galewick -", 0.05 a
  step; about 0.12 matches the bed). The setting is remembered on the device; tell me the number and I'll put it in the cue.
- Credits: "Music: Mugesh" in the app; `Assets/Docs/Audio/AudioCredits.md` lists both tracks, source tool **to confirm**.

## B. Statue and shards
- Socketed shards now ride the finale's wake-up shake (`StatueSocket.ShakenSocketWorld`, same offset as the statue).
- Temple sunrays on the floor: **confirmed in code**. `UpdateShardPickup` ran every frame from the start of the rise and drew
  the shard and its sunrays at `_shardHome` (still (0,0,0) = the hall floor) until the rise ended. It now waits for
  `_shardRisen`.
- Statue ghost: back to **grab_drop** (Mugesh's note), set in MR_Main (`Step18Debug.StatueGhost`, scene grep: 3 sockets).
  The palm carry from Nov 8 stays as a second way.

## C. Tall grass
- The grass shader's battle flattening now covers the clearing **plus a corridor toward the player as wide as the clearing
  is long** (`DioramaGrassClear`), so blades between the head and both pads and their labels lie down to **15%** (was 20%,
  clearing only). It fades in and out with the battle zoom. One battle shot at the tower door: the front-left tall grass
  lies flat. Not checked in the ridge or kite fields.

## D. Poke a rim pod
- Exploring, a fingertip entering a rim pod shows its card for **3.5 s** with a soft glass tap (`lodge.podTick`). No grab
  (unchanged), no hover growth. A resting hand can't: the tip must be moving toward the pod at ≥ **0.12 m/s** as it enters
  (`PodRack.pokeApproachSpeed`), and not from a hand that is grabbing the island. A slow entry logs
  "A fingertip in Pod n but not moving toward it". **Needs real hands.**

## E. Title
- Rim pods and their sockets hide while `TitleScreen.Showing` (log `[PodRack] Rim pods hidden under the title` / `back`).
  The menu stone already hid under the title.

## F. Swipe → tap
- The old clip: the swipe sped up, a **0.8 s hold**, then the tap's ~0.15 s idle lead-in and a **56° pose snap**. That's the
  "two separate actions".
- Now: the swipe at its recorded speed (clearer) with its settling tail cut, a **0.3 s glide** into the tap's first moving
  frame, then the tap. 75 → 58 frames (1.93 s). Size and yaw unchanged (Nov 8).

## Voices (Mugesh's ask, take 2: "a variation of the Lantern's robotic sound")
- Every voice is `spirit.blip`'s two layers (a formant buzz blip + an FM sine ping), re-pitched and re-voiced. Each Keeper
  types with its own (`voice.keeper.<name>`, else `voice.keeper`):
  - **Galeowl** (tower): a fourth down, round "oo" formants, a falling hoot glide.
  - **Hearthstag**: an octave down, darker, a few ember crackles.
  - **Tidemother**: a third down, a rising bubbly glide, a watery ping.
- **The Heart (statue)** was silent while typing. Now: the blip two octaves down with a slow bell ping and a shimmer, a
  longer tail, slower ticks (every 7 characters, at least 0.3 s apart; `DialogueBubble.heartBlipEvery / heartBlipGap`).
- Take 1 (owl hoot, ember hum, bell vowel, `f794eab`) was replaced by take 2 (`7ddf031`). Baked with `Step18Audio.Voices`.
  Not heard in Play: listen on the headset.

## Big bodies faint (late note: "the water tower monster flipped like a plane stuck in the ground")
- The small Islings' faint is a 360° spin with a hop, then a 90° tip onto their side. Tidemother is wider lying than
  standing (side 7.7 cm vs centre 5 cm), so on its side it stood on edge in the pad.
- Now a body **5.5 cm or taller, or wider lying than standing** (the three Keepers, Tidemother, Galleyfold, Sailfold, the
  big evolved forms) **shudders** (no spin, no hop), then **slumps**: sinks 20% of its centre height, squashes to 84%,
  leans 16°. `CreatureView.SlumpFaint / slumpFaintHeight`. Small Islings unchanged.

## Finale pace (late note: "the statue's ending takes too long")
| Where | Old | New |
|---|---|---|
| The Heart's lines type at | 16 chars/s (a 50-char line: 3.1 s + 4.5 s hold) | **28** (1.8 s + hold) |
| Wake: socket flares | 0.5 s apart | 0.3 s |
| Wake: shake | 6.5 s | **4.5 s** |
| Wake: head lift / eyes open | 3 s / 3 s | 2 s / 2 s |
| Before the Spirit's first line | 1.5 s | 0.8 s |
| After "thank you" | 2 s | 0.8 s |
| Hall of Fame: before / after / gap | 2 / 2 / 1.6 s | 1.2 / 1.2 / 1.2 s |
| Credit steps | 2.6 s | 2.2 s |
Scene values (MR_Main serializes the typing speed, the gap and the credit step) set with `Step18Debug.FinalePace` and checked
in the .unity file. Reading holds (1.5 s + 0.06 s a character) unchanged: they're shared with every line.

## Headset test, in play order
1. Title (Continue / New journey): **no party pods on the rim**, no music under it.
2. Continue into Galewick: the realm card, then the music fades in over 3 s. Is it under the ambience and the Spirit? If
   not, Dev panel > Tools > Music > "Galewick -" until it is; tell me the number.
3. Swipe lesson ghost: slide, glide, tap as one move.
4. Exploring: poke a rim pod with a fingertip → its card for 3.5 s and a tap sound. Rest a hand on the rim → nothing.
5. Wild battle: battle music comes in (0.6 s). Tall grass in front lies down. A wild Isling of your level should need 3
   hits on a full-HP Isling (2 only if it's strong against it).
6. Let one faint: the next one moves first on its first turn.
7. Bring a wild one to about a third of its HP: the cradle pod glows.
8. Win: green "+" on the hurt pods, soft heal sound, the log `A little rest: n Isling(s) got 25% HP back`. Music: about
   45 s of ambience, then Galewick from the top.
9. Evolve a fainted Isling: it comes out at full HP.
10. Tower: Galeowl's lines have their own blip (a lower, rounder Lantern voice); music keeps going quieter inside; the Keeper battle plays the battle track.
11. Temple shard rise: no sunrays on the floor first.
12. Statue: grab_drop ghost (pinch/grab and place); the Heart's lines have the deep blip with a bell.
13. Finale wake-up: socketed shards shake with the statue. The whole ending feels shorter (shake, eyes, Hall of Fame).
14. Beat a Keeper (Tidemother especially): it shudders and slumps instead of flipping onto its side.

## Smoke run
**84/84 beats ok, 0 failed, 0 errors, 6.6 min** (03:20, after every change above).
- The first run (03:09) failed "See the ending": since Nov 7 the dreamed fire and water shards wait 45 s each for the
  player, and the smoke script didn't place them. Not a game bug; the script now places them (`8733504`).
- The three Keeper battles (all slump now) and the finale ran clean; the finale took 50 s in the run (from the wake to
  the thank-you card, with the bridge's palm).
- The usual note: the opening waits for a real island grab in flat Play (no progress for 20 s), then times out by design.

Saves restored byte-identical after each Play check, XR init back on.
