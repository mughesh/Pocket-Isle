# Step audio-B — AudioScope, better synth models, core sounds; ElevenLabs only for a short list

Audio lane, worktree `D:\UNITY\projects\PocketIsle-audio`, branch `audio`. **All lane rules from
`step-audio-a-lab.md` still apply** (new files only outside `Assets/_Project/Audio/**` and `Assets/Docs/Audio/**`,
no Play mode, no new packages, commit per part, no AI attribution, never push or merge).
**Work list:** `Assets/Docs/Audio/SoundList.md` (priorities P0 → P1 → P2). **No Isling cries** in this step (step C).

## Why (Mugesh, Oct 26)
"Most of the audios seem to be just some small noise… a lot of other sounds for the in game that is bland… a good
ambient sound is definitely needed." Then: **make as much as possible in Unity; only what synth can't do confidently
comes from ElevenLabs.**
Batch 1 analysis: 29 of 124 clips are mostly sub-bass (< 250 Hz, inaudible on small speakers); peak-normalising to −3 dBFS
makes bassy and noisy clips quiet; rustles and whooshes are plain hiss; impacts are single synth layers. Tonal sounds (chimes,
runes, jingles, pod ping, Spirit, xp) look clean; keep them and only re-level.

## Part 1 — AudioScope: let Claude see what it makes
You can't hear, but you can read images. Build this first, then use it on every change.
- `Editor/AudioScope.cs`: pure C# STFT (Hann 1024, hop 256) → log-magnitude spectrogram (0–8 kHz, 60 dB range) +
  waveform strip + label. **Contact sheet** PNG per group in `Temp/audio-analysis/<group>.png` (git-ignored).
- `Temp/audio-analysis/report.md` per clip: duration, peak dBFS, **midLevel** (RMS of a 250 Hz–6 kHz band-pass, dB),
  **low share** (energy < 250 Hz ÷ total), **tail share** (energy after the main body ÷ total), **silence share**
  (frames 40 dB under the peak). Failures at the top as `FAIL id: reason`.
- Spectrogram in the Audio Lab's right pane under the waveform. Menu `Tools/Pocket Isle/Audio/Analyse All` and
  `Analyse Group`.
- **Workflow from now on:** after every bake or import, analyse, **open the PNGs and read them yourself**, fix,
  re-run. Don't hand Mugesh anything that fails.

Rules: **speaker-safe** (low share ≤ 0.45, except music); **level tier** (midLevel ±1.5 dB of its tier); **tail** ≤ 0.25 for
hits; **silence** ≤ 0.3 for one-shots (trim leading and trailing silence on import).
**Loudness:** replace peak-normalise with **mid-level normalise to a tier**, peak ceiling −1 dBFS (soft limiter), for
synth bakes and imports alike (imports: bake a trimmed, normalised copy; never edit originals). Starting tiers (tune by ear):
UI −20, steps and rustles −22, island and travel −18, moves and hits −16, catch success, level-up and jingles −13, ambience
beds −28, sweeteners −24, hum loops −24.

Commit: "Audio: AudioScope analysis + loudness tiers".

## Part 2 — New synth models (port the algorithms, not code)
Add these as new layer types in `SynthRenderer` with fields in SynthPatch, Lab sliders and presets. Write them from the
published algorithms. They are well documented and short.
| Model | Based on | Use it for |
|---|---|---|
| **Modal resonator bank** (4–12 damped sine modes excited by a click, mallet or noise burst; per-mode freq ratio, decay, gain; strike position) | Modal synthesis; STK `ModalBar` | **Material presets: wood, stone, glass, clay pot, brass, crystal.** Knocks, clinks, pod glass, wobble clicks, hits, log thud body, crown shield clang, statue socket lock |
| **Particle collisions** (stochastic sum of tiny decaying resonances: density, system decay, resonance freqs, gain per grain) | Cook's PhISEM; STK `Shakers` presets (Crunch, Little/Big Rocks, Sticks, Sand Paper, Water Drops) | **Footsteps on grass, path and stone, pebbles, gravel, debris, dust falling, grass rustle, leaves** |
| **Bubble** (sine with exponential decay and rising pitch; size → pitch and decay; random clouds) | van den Doel's bubble model | **Drips, splashes, springs, water moves, steam pops** |
| **Fire** (crackle = sparse sharp impulses through a band-pass + hiss + low lapping noise with slow AM) | Farnell, *Designing Sound*, fire chapter | **Ember crackle, brazier light, shrine flame, fire moves, keeper fury** |
| **Wind** (noise through moving band-passes driven by slow random gusts; whistle partials at high gusts) | Farnell, wind chapter | **Breeze beds, gusts, wind moves, whooshes** (with the resonant sweep) |
| **Friction / creak** (stick-slip pulses driving a modal body; rate and pressure curves) | Farnell, creaking door | **Wood creak, log strain, rope and chain creak, leather bellows squeak** |
| **Air puff / breath** (noise burst through a formant or band-pass with a fast pitch-like sweep) | Farnell, breath | **Bellows puff, snort (try it), pod whoosh tail** |
Also: **Layer groups** (sum several layers with their own offsets, so one patch = transient + body + tail + sparkle)
and a **Random scatter** of a layer N times over a window (debris, sparks, raindrop-style events).
Unit-test that each model renders deterministically per seed and stays within −1 dBFS.

Commit: "Audio: modal, particle, bubble, fire, wind, friction, breath models".

## Part 3 — Remake the P0 sounds with synth
Go through SoundList P0 (then P1) group by group. Each sound = layers: **transient (2–5 kHz) + body (300–1500 Hz) + tail /
texture + optional sparkle**. Sub only as a quiet extra. Use the new models. AudioScope every group and read the PNGs.
- Footsteps: Kenney steps stay as variants; add synth particle steps (grass, path, stone) at a **toy scale** (lighter,
  higher, shorter). Keep both in the cue for Mugesh to compare.
- The magic set (shard touch, crack, logo rise, gold path, shard dock, statue socket, keeper calm, restore swell) =
  crystal/glass modal + shimmer (detuned FM partials, D major pentatonic) + soft air. These should feel **magical, warm, never
  sci-fi**.
- New cues from SoundList (shard.touch, logo.rise, rune.hold, menu.*, path.gold, log.drop, pipe.glow, brazier.warm,
  keeper.stomp, keeper.snort, keeper.calm, keeper.crown.break, shard.float, shard.dock, statue.socket, gift.pods, ui.lesson,
  pod.wiggle): create them, and add each to PhaseB-Hooks.md with the event that fires it.
- **Confidence tag** per cue in Review: `synth-good`, `synth-ok`, `synth-weak`. Be honest: after you look at the spectrogram,
  anything that reads thin, buzzy or noisy goes `synth-weak`.

Commit per group: "Audio: synth remake — <group>".

## Part 4 — Import the ElevenLabs batch (generated by a separate gen lane)
A third Claude account runs `Assets/Docs/Prompts/step-audio-gen-elevenlabs.md`. It generates outside both repos into
`D:\UNITY\projects\_audio-sources\elevenlabs\` and writes `manifest.json` (cue, levelled file, original, plan free or paid,
prompt, note good / ok / weak). **Don't build an API tab and don't call ElevenLabs yourself.**
- **Importer** (`Tools/Pocket Isle/Audio/Import Gen Batch`, plus a button in the Lab): reads the manifest, copies each levelled file into
  `Clips/Gen/<cueId>/` (keep the name, and add `_FREE` when plan = free), runs it through the same trim and tier levelling,
  and adds it to the cue **as extra variants next to the synth ones** (never deletes synth clips). Re-running is safe
  (skips files it already has, picks up new ones).
- Cues get a Review note "Gen vs synth — pick" so Mugesh compares in the Lab.
- **Licence check:** free-plan clips (`_FREE`) are fine for listening in the Editor, but `AudioLabChecks` must **fail the
  release check** while any cue still holds a free-plan clip, listing them. Mugesh decides later whether to regenerate them on a
  paid month or drop them.
- AudioCredits.md: paid clips = "Generated with ElevenLabs Sound Effects, paid plan (commercial licence)".
- `.mp3` isn't in Git LFS; note it in PhaseB-Hooks.md, and don't edit `.gitattributes`.
- If the manifest doesn't exist yet, build the importer, test it on a fake manifest in Temp, and move on.

Commit: "Audio: Gen batch importer + licence check".

## Part 5 — Ambience: beds + a living layer
The player is in their real room (passthrough), so the island should sound alive **up close** without filling the room.
- **Beds** (2D loops, −28 tier): from the gen batch when imported. Until then, **synth placeholders** from the wind and fire models
  (StillIsle breeze, Cinderwood breeze + far crackle, Forge Hall low ember hum + drips, Battle wind only).
- **AmbienceDirector** (new runtime file): `AmbienceProfile` assets (bed cue, sweetener sets: cue, min/max interval,
  volume and pitch jitter, max at once). `SetArea(profile, crossfade)`, `Duck(amount, fade)` (battle and dialogue: bed −6 dB,
  sweeteners paused in battle). **Sweeteners play as 3D one-shots at random points on the island surface**
  (`IslandBounds`: centre, radius, height; fed by the main lane in Phase B; default a 0.35 m disc), every 3–15 s, never
  the same clip twice in a row.
- **Synth sweeteners now:** `sw.crackle` (ember pops), `sw.drip`, `sw.gust`, `sw.creak`, `sw.pebble`. Birds, leaves and insects come
  from the gen batch.
- Profiles: StillIsle, Cinderwood faded, Cinderwood restored, Forge Hall, Lodge, Battle.
- **Ambience tab** in the Lab: pick a profile → Play runs the bed + sweeteners in Edit mode (EditorApplication.update, 2D
  with pan), so Mugesh can judge a minute of it without Play mode.
- PhaseB-Hooks.md: where to call `SetArea`, `Duck`, and how to feed `IslandBounds`.

Commit: "Audio: AmbienceDirector + synth beds and sweeteners".

## Part 6 — Report to Mugesh (short)
- Confidence counts (good / ok / weak), and which cues now have gen variants to compare.
- **Review order:** (1) Ambience tab, a minute per profile; (2) Review tab: opening and magic, walk, battle and catch,
  Cinderwood beats; (3) the rest. Listen on **headphones and on laptop speakers**.
- AudioScope before/after counts, contact sheet paths, anything that still fails.
