# Step audio-A — Audio Lab, cue system, first sound batch (parallel lane)

You are the **audio lane** for Pocket Isle. Another Claude Code session (the **main lane**, a different account)
keeps editing the game in the main project folder at the same time. You work in a separate git worktree and a
second Unity window. Your job: build the tools and data for sound, and make the first full batch of sounds.
Hooking sounds into game scripts happens later on the main lane (Phase B), not here.

Read first: `CLAUDE.md`, `.claude-handoff.md` (skim for layout, naming and assembly setup),
`Assets/Docs/GameDesign.md` (skim). Mugesh tests by ear; **you cannot hear**, so every sound must be quick for him
to audition and tweak.

## Lane rules (do not break)
- Main project: `D:\UNITY\projects\Meta Hackathon` (branch `master`; don't touch it).
  Worktree: `D:\UNITY\projects\PocketIsle-audio`, branch `audio`.
- **Only add new files**, under `Assets/_Project/Audio/**` and `Assets/Docs/Audio/**`. Never edit an existing
  file: scripts, scenes, prefabs, asmdefs, ProjectSettings, `Packages/manifest.json`, `.gitignore`, `.gitattributes`.
  If something needs an existing file changed, write it down in `Assets/Docs/Audio/PhaseB-Hooks.md` instead.
- Reading existing files is fine (you need species ids, event names, assembly layout).
- No new packages.
- **No Play mode in the audio Unity window.** Two editors starting XR over Link crash (`XR_ERROR_LIMIT_REACHED`).
  All audition happens in Edit mode through the Audio Lab window.
- Commit locally on `audio` after each part: clear message, no AI attribution, never push, never merge into
  main. Mugesh (or the main lane) merges `audio` between main-lane steps.
- `*.wav` is already Git LFS in `.gitattributes`; `*.ogg` / `*.mp3` are not. Don't edit `.gitattributes`; if Lyria
  music lands as mp3/ogg, note the LFS rule in PhaseB-Hooks.md. Check `git lfs version` works before the first commit.

## Part 0 — Create the worktree (run from the main folder, then hand over)
Mugesh starts this session in the main folder (the main lane may be running there too; only touch git worktree
metadata). Do only this, then stop:
1. `git worktree list`. If `PocketIsle-audio` already exists, skip to step 4.
2. `git worktree add ../PocketIsle-audio -b audio` (from the current commit; the main lane's uncommitted work is
   not included, which is fine).
3. **Ask Mugesh** whether the main Unity editor is closed right now. Only if yes: copy `Library/` into the
   worktree to skip the long first import. If it's open, skip this (Unity locks Library files); Unity will import fresh.
4. Copy this prompt file unchanged to the same path inside the worktree (`Assets/Docs/Prompts/step-audio-a-lab.md`).
5. Tell Mugesh, in a short numbered list:
   (a) Unity Hub → Add → Add project from disk → `D:\UNITY\projects\PocketIsle-audio`, open it (same Unity 6.3).
   (b) End this session; start a new Claude Code session in `D:\UNITY\projects\PocketIsle-audio` and say:
   "Read Assets/Docs/Prompts/step-audio-a-lab.md and do Part 1 onward."

## Part 1 — Synth core + Audio Lab window
Folders: `Assets/_Project/Audio/{Runtime,Editor,Data,Patches,Clips/Synth,Clips/Kenney,Clips/Music}`.
The game uses asmdefs (`Diorama.Runtime`, `Diorama.Editor`, namespace `Diorama`). Give Audio its own:
`Diorama.Audio` (runtime, namespace `Diorama.Audio`, no Meta references) and `Diorama.Audio.Editor`. Phase B adds
the `Diorama.Audio` reference to `Diorama.Runtime.asmdef` (note it in PhaseB-Hooks.md; don't edit it now).

**SynthPatch** (ScriptableObject, one asset per sound, in `Patches/<group>/`):
- Duration, seed, variants count (bake N variants with seeded jitter), mono, 48 kHz, 16-bit.
- 1–4 **layers**, each with: wave (sine, triangle, soft square, soft saw, white/pink noise, buzz), base pitch
  (Hz or note name), pitch envelope (start → end + curve), amplitude envelope (attack, hold, decay, sustain,
  release), vibrato, FM (ratio, index, index decay; for bells, marimba, glass), filter (low/high/band-pass
  state-variable with cutoff envelope + resonance), **formant pair** (F1/F2 start → end, for vowel glides:
  Isling cries and Spirit blips), crackle (random impulse density + click length), gain, start offset, pan none.
- Master: gain, soft limiter, DC removal, 3 ms fade in/out (no clicks), normalise peak to −3 dBFS.
- **Loop mode** (ambience, hums): render extra tail and crossfade it into the head so the loop is seamless.
- **Sequence mode** (jingles): list of (note, start, length, layer preset). All tonal sounds and jingles use one
  scale: **D major pentatonic**, so chimes never clash.
- Bake writes `.wav` to `Clips/Synth/<group>/<id>[_vN].wav`.

**Import rules** (new AssetPostprocessor scoped to `Assets/_Project/Audio/Clips/`): SFX < 2 s → mono, ADPCM,
Decompress On Load; loops and ambience → Vorbis ~0.6, Compressed In Memory; `Clips/Music/` → Vorbis ~0.5,
Streaming. Verify these settings make sense for Quest (Android) in Unity 6.3; adjust and note why.

**Audio Lab** (EditorWindow, menu `Tools/Pocket Isle/Audio Lab`, matching the existing tools):
- Left: patch list grouped by folder, search, status dot (none / approved / redo).
- Right: the patch's fields with sliders; waveform preview; duration and peak readout.
- Buttons: **Play** (Edit mode preview; find the working preview API for Unity 6.3, e.g. `UnityEditor.AudioUtil`
  via reflection, else a hidden `HideAndDontSave` AudioSource; confirm it works without Play mode), **Play random
  variant**, **Auto-play on slider release** toggle, **Bake**, **Bake all**, **Revert to recipe defaults**.
- **Review tab:** every cue in one list with Play, Approve, Redo and a note field. Mugesh writes notes here
  ("too harsh", "longer tail"). Save notes on the patch asset. Next session reads Redo notes and fixes them.
- **Loop tool** (for Lyria music later): load a clip, set loop start/end, crossfade length, bake to a new looping clip.

Commit: "Audio Lab: synth core, patches, import rules".

## Part 2 — Cue data + runtime service
- **AudioCue** (ScriptableObject): `id` (string like `pod.throw`), clips[] (random pick, no immediate repeat),
  volume + jitter, pitch range, **bus** (Music, Ambience, SFX, UI, Voice), 3D or 2D, min/max distance (defaults
  0.1 / 1.5 m), loop, max simultaneous voices, min retrigger interval, priority, optional `pitchStepSemitones`
  (for rising sequences like XP ticks: `Play(id, step: n)`).
- **AudioCueTable**: all cues, lookup by id, editor validation (duplicate ids, missing clips, clips not
  referenced). Generate `AudioIds.cs` with const strings, so hooks are compile-checked.
- **AudioService** (runtime, created on first use, survives scene swaps):
  - Pooled AudioSources (about 16 × 3D, 6 × 2D; pool size configurable).
  - `Play(id, Vector3 pos)`, `Play(id, Transform follow)`, `Play2D(id)`, `PlayLoop(...)` returning a handle with
    `Stop(fade)` and `SetPitch/SetVolume` (shrine palm-fill tone, low-HP pulse, gate hum).
  - Music: `SetMusic(id, crossfade)`, `StopMusic(fade)`.
  - **Buses in code, no AudioMixer asset**: per-bus volume, saved in PlayerPrefs, getters/setters for the future
    Settings menu. `Duck(bool)` lowers Music by about 6 dB (dialogue, jingles).
  - 3D defaults: spatialBlend 1, Unity built-in panning (no spatializer plugin), doppler 0, logarithmic or custom
    rolloff. `AudioService.DistanceScale` multiplies min/max distance, so the main lane can feed island scale.
- **PhaseB-Hooks.md:** a table of event → cue id → the existing script and method where the call most likely
  goes (grep the main code to find them; read only). Include the procedural placeholder sounds to replace:
  `Onboarding.cs` (`MakeChime`), `Puzzles/PuzzleSounds.cs` and its users (`FallenLog`, `ForgeBellows`). Add notes for Project Settings → Audio checks on Quest
  (DSP buffer, 48 kHz output, voice count). This doc makes the Nov 4 hookup fast.

Commit: "Audio: cue table and AudioService".

## Part 3 — Sound batch 1 (synth)
Create a SynthPatch and an AudioCue for every id below, bake all, fill the cue table. Style: **cozy toy, small
and close.** Soft attacks (≥ 3 ms), little energy above ~9 kHz, no harsh square leads, short tails, wood /
glass / bell / kalimba / marimba / soft air. Loudness hierarchy: UI quietest, footsteps and rustles low,
moves medium, catch success and level-up loudest. Mark ids with ★ as **gap candidates** (organic sounds synth may
not nail; try anyway and flag them in Review).

| Group | Cue ids (recipe hints) |
|---|---|
| Island | `island.grab` (soft stone scrape: band-passed noise, short), `island.release`, `island.scale` (loop, wood creak), `island.recenter` (soft whoosh) |
| Walk | `walk.hop`, `walk.stop` (tiny tap), `walk.bump` (soft thud), `grass.rustle` (filtered noise flutter, 3 variants) |
| Encounter | `enc.rustle` (big rustle), `enc.sting` (two-note pluck, rising), `enc.rare` (gold sparkle sting: glass FM arpeggio), `pond.tap` ★, `pond.splash` ★ |
| Zoom/space | `zoom.in`, `zoom.out` (pitched whooshes), `clearing.rumble` (low noise, sinking), `pads.rise` (soft earth thump) |
| Pods | `pod.lead` (glow ping), `pod.grab` (glass tick), `pod.throw` (whoosh), `pod.open` (glass pop + sparkle), `pod.return` (rising zip) |
| Runes | `rune.hover` (very soft tick), `rune.select` (wood block + bell), `battle.run` (quick retreat whoosh) |
| Moves | `move.charge` (riser), `move.shoot.fire/water/wind/neutral` (launch), `move.impact.fire` (crackle burst), `.water` ★ (bubble splash), `.wind` (gust), `.neutral` (thud), `move.lunge` (whoosh), `move.slam` (thud + low ring), `move.shield.up` (glassy swell), `move.shield.pop` |
| Hits | `hit.normal`, `hit.super` (brighter, layered sparkle), `hit.weak` (dull, muted), `faint` (descending boop), `hp.low` (loop, soft pulse) |
| Catch | `catch.appear`, `catch.hit`, `catch.wobble` (wood click, 3 variants), `catch.success` (bright click + sparkle), `catch.shatter` (synth sparkle layer; Kenney glass added in Part 4), `catch.dazed` (loop, dizzy stars), `catch.toRim` (whoosh + clink), `catch.toStorage` (rising streak) |
| Progression | `xp.tick` (use pitchStep), `evo.rise` (shimmer riser, loop-able), `evo.burst` |
| Spirit | `spirit.appear` (chime), `spirit.blip` (6 formant variants, high and bell-like), `spirit.beam` (loop hum), `spirit.focus` (gold pulse) |
| Shrine/UI | `shrine.flame` (loop, crackle), `shrine.fill` (loop, steady tone; code raises pitch), `shrine.flare`, `shrine.heal` (warm chord swell), `ui.saved` (two-note chime) |
| Travel | `scene.sink`, `scene.rise` (rumbles), `gate.hum` (loop), `gate.enter`, `gate.sealed` (soft bonk) |
| Lodge | `lodge.podTick` |
| Realm | `lantern.sputter`, `ember.crackle` (loop), `steam.hiss` (loop), `restore.swell` (big warm swell, key of D) |
| Puzzles | `log.lift` (wood creak + thump), `bellows.pump` (air whoosh), `brazier.light` (whoomph), `forge.door` (stone rumble) |
| Keeper | `keeper.crown.shield`, `keeper.crown.crack`, `keeper.fury` (low rumble + crackle) |
| Opening | `shard.hum` (loop), `shard.crack`, `island.unfold` (rising earthy swell), `explorer.stretch` (tiny cute blip) |
| Ambience (loops, 30 s, 2D) | `amb.stillisle` (soft breeze + sparse synth birds ★), `amb.cinderwood` (wind in leaves, soft), `amb.cinderwood.restored` (adds warm crackle), `amb.lodge` (hearth), `amb.pond` ★ (lapping) |
| Jingles (sequence mode) | `jingle.catch`, `jingle.victory`, `jingle.levelup`, `jingle.evolve`, `jingle.keeper`, `jingle.shard` (marimba + bell, 1–3 s, D major pentatonic) |
| Music (slots only) | `music.title`, `music.hub`, `music.cinderwood`, `music.battle.wild`, `music.battle.keeper`, `music.lodge`: cue entries with no clips, marked "awaiting Lyria" |

Commit: "Audio: synth batch 1".

## Part 4 — Kenney CC0 imports
Mugesh pre-approved downloading Kenney's **Impact Sounds**, **Interface Sounds** and **RPG Audio** (all CC0) from
kenney.nl. Download and unzip to `D:\UNITY\projects\_audio-sources\kenney\` (outside both repos). If a
download is blocked, tell Mugesh the exact URLs and continue with the rest.
Pick by filename and copy only what's used into `Clips/Kenney/`:
- `step.grass`, `step.path` (concrete set), `step.stone`, `step.wood` (lodge): all variants of each footstep set;
  cue pitch jitter ±5%, low volume.
- `catch.shatter`: add a light glass impact layer to the synth sparkle (cue plays both, or bake a mix).
- `walk.bump`, `move.lunge`, `move.slam`: compare a soft impact against the synth version; keep both as cue
  variants and mark them for Review.
- `lodge.door` (door open), `lodge.page` (book flip), `island.scale` alternative (creak).
Write `Assets/Docs/Audio/AudioCredits.md`: every file → source pack → licence (CC0) → URL.

Commit: "Audio: Kenney CC0 imports + credits".

## Part 5 — Isling cries
Don't touch the species data (`Assets/_Project/Data/Creatures/`: `SpeciesRegistry.asset`, `Species/`). Read it
(read-only) to list every species id, including evolutions and Hearthstag.
Create a **CryProfile** asset per species in `Data/Cries/` (keyed by species id) plus a patch and cue
`cry.<speciesId>`:
- Body size → base pitch (tiny ~800 Hz, huge ~180 Hz). 1–3 syllables, each a formant glide (e.g. "mew",
  "brrp", "pwee").
- Element timbre layer: fire = warm buzz + crackle tail; water = bubbly pitch wobble; wind = breathy whistle;
  neutral = woody pluck.
- **Evolved form = the base form's voice, ~4 semitones lower, ~1.2× slower, a sub layer added.**
- Keeper: Hearthstag big and low with a crackle bed (it must still sound gentle, not scary).
- Runtime variants come from pitch: hurt = shorter and higher, faint = slowed and lower (document in PhaseB-Hooks.md).
- Speaker voices for dialogue blips: Spirit (high bell-like), Hearthstag (low warm), Heart (deep, slow).

Commit: "Audio: Isling cries".

## Part 6 — Report to Mugesh (short)
- What's built, the menu path, and how to review: **Tools → Pocket Isle → Audio Lab → Review tab, Play, Approve or
  Redo + note.** Suggest the order: starters' cries, pods and catch, battle, Spirit, the rest.
- The ★ gap list.
- Total size of `Clips/` on disk and the estimated build size after import compression.
- Anything blocked, and anything for PhaseB-Hooks.md.
