# Step audio-hookup — merge the audio branch, wire sounds, ambience and music into the game

Main project `D:\UNITY\projects\Meta Hackathon`, branch `master`. Run this **after the current step (10f) is committed**
(clean tree). Mugesh wants to **hear the sounds in play** on the headset before judging them, and the game needs a
**looping background music bed** per area (Alto's Adventure-like: subtle, warm, always there, keeps the game from
feeling sleepy). His procedural whooshes from steps 10x stay for now.

Read first: `Assets/Docs/Audio/PhaseB-Hooks.md`, `Assets/Docs/Audio/SoundList.md`, `Assets/Docs/Audio/NightNotes.md`
(all arrive with the merge).

## Part 1 — Merge
1. **Ask Mugesh to close the `PocketIsle-audio` Unity window** (one editor at a time from here on). The worktree stays
   on disk; don't delete it.
2. `git lfs version` must work (`.wav` is LFS). Then `git merge --no-ff audio -m "Merge audio branch (Audio Lab, cues,
   synth models, gen batch, ambience)"`. The audio branch only added files under `Assets/_Project/Audio/`,
   `Assets/Docs/Audio/` and `Assets/Docs/Prompts/`, so conflicts are not expected. If one shows up, stop and show it.
3. Let Unity import; the console must be clean. `Tools/Pocket Isle/Audio/Analyse All` must still pass for the synth cues.

## Part 2 — Wire the audio system into the game
- `Diorama.Runtime.asmdef` references `Diorama.Audio`.
- **Volumes:** map the Settings board's existing `Music`, `SFX`, `Ambience` pebbles (`VolumeSettings` / `GameSettings`)
  onto AudioService's buses (UI and Voice follow SFX). Remove the old "nothing plays through them until the audio
  step" note.
- **Project Settings → Audio (pre-approved by Mugesh for this step only):** 48 kHz, DSP buffer Best latency, real voices 32 /
  virtual 64, spatializer none (Unity built-in 3D). List the changed files in the commit.
- **Hook every P0 row of SoundList.md**, then P1, using `AudioIds` constants and the hook table in PhaseB-Hooks.md. New code
  since the audio branch was cut (steps 10a–10f: Galewick, wind roster, Galeowl, knots, mills, cloud bank, kites, Cinderwood
  hands) has no cues yet. Keep its procedural sounds, but route their AudioSources through the SFX bus volume. Put
  **missing sounds for those interactions** in a list at the bottom of PhaseB-Hooks.md for the next audio pass.
- Replace `Onboarding.MakeChime` with `ui.lesson`, and `PuzzleSounds` users with their cues (keep PuzzleSounds as a fallback only if a
  cue id is missing, and log it once).
- 3D sounds sit at the event's world position on the island; `AudioService.DistanceScale` = island scale.
- **Release check vs builds:** QA APKs must build with the free-plan ElevenLabs clips in them (they're for testing).
  Only **Build Release APK** runs `ReleaseCheck` and refuses on failure.

## Part 3 — Ambience
- AmbienceDirector profiles per area: Still Isle, Cinderwood faded and restored, Forge Hall, Lodge, Battle. **Add Galewick faded and
  restored** (synth wind bed + gusts, creaks, and sw.bird when restored; still air with rare gusts when faded).
- `SetArea` on every map or interior change and on restoration; `IslandBounds` from the island transform and scale;
  `Duck` in battle (bed −6 dB, sweeteners paused) and in dialogue (−3 dB).

## Part 4 — Music
- **MusicDirector** (in the Audio runtime, uses `AudioService.SetMusic`): one cue per state, 2 s crossfades.
  | State | Cue |
  |---|---|
  | Title, Still Isle, Lodge | `music.hub` |
  | Cinderwood (faded and restored; restored = +2 dB) | `music.cinderwood` |
  | Galewick | `music.galewick` |
  | Wild battle | `music.battle.wild` |
  | Keeper battle | `music.battle.keeper` |
  Battle start: crossfade to the battle cue (0.6 s). Win: the victory jingle ducks the music, then back to the area cue
  (2 s). Dialogue: music −4 dB. Pause menu: music −8 dB. Scene swap: keep playing through.
- **Mix:** music sits **under** the SFX: Music bus default at 0.55, ambience under the music. Tune by ear later.
- **Music files** come from Mugesh: `D:\UNITY\projects\_audio-sources\music\<cue>_*.mp3|wav` (Eleven Music or Lyria
  downloads). For each: use the Lab's **Loop tool** to find a clean loop. Cut at bar lines (detect the tempo from onsets; Mugesh's
  prompts ask for steady tempos), 2 s equal-power crossfade, then bake to `Clips/Music/<cue>.wav`: stereo, Vorbis
  ~0.5, Streaming. Tag `_FREE` when the source was made on a free plan (ask Mugesh, or read a `plan.txt` beside the
  files). Check the loop seam with AudioScope (no level jump, no click).
- **If a music file isn't there yet:** wire the state anyway and leave the cue empty (silent). Never fake music with synth
  loops.

## Part 5 — Test and report
- Editor: play the Quick Journey flow with the dev panel. All cues fire, no errors, no "missing cue" spam.
- **Device QA build** (audio timing and voices differ on device): 72 fps holds in battle and in the restoration wave; no
  crackles.
- Report to Mugesh: a short headset checklist in play order (opening → walk → first battle and catch → Cinderwood log,
  bellows, forge, Keeper → shard → statue → Galewick), and how to note bad sounds: say the moment and "too loud / too quiet /
  wrong / annoying". The audio pass after his trip fixes them in the Audio Lab.

Commits: "Merge audio branch…", "Audio hookup: buses, settings, P0 hooks", "Audio hookup: ambience", "Audio hookup:
music director + loops". No AI attribution, never push.
