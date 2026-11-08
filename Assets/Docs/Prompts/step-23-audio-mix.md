# Step 23: the audio mix pass + the paid ElevenLabs regeneration (step 15b folded in)

Triaged Nov 8, 11:45 PM (design chat). Source: Gemini's audio report on Mugesh's two Nov 8 headset captures
(`com.oculus.vrshell-20261108-203859-0.mp4`, `com.mugesh.pocketisle-20261108-204656-0.mp4`) + the design chat's own
measurements of the clips. Gemini rated the music design (silence until restored, the bloom, temple → Keeper battle
without a restart) 9.5/10: **don't change the music logic.** What's wrong is **balance** (some music too quiet, some
effects too loud) and **a few sounds that are obviously synthesized noise** (footsteps, lodge fire, the restored-wind
gust, the area-change whoosh, Galeowl's knots).

Design chat measurements (RMS, whole clip / energy above 2.5 kHz / below 300 Hz):
| Clip | Total | >2.5 kHz | <300 Hz | Reads as |
|---|---|---|---|---|
| `Synth/Walk/step.grass_v1` | −22.5 | −25.7 | −58.9 | almost all hiss: "velcro" |
| `Synth/Walk/step.path_v1` | −22.1 | −27.4 | −62.3 | same |
| `Synth/Ambience/amb.lodge` | −26.8 | −30.7 | −43.9 | hissy crackle |
| `Synth/Travel/scene.rise` | −17.6 | −24.9 | −29.2 | loudest effect, broadband |
| `Synth/Galewick/knot.pull_v1` | −17.8 | −56.4 | −28.7 | all low: "buzzer" |
| `Synth/Jingles/jingle.catch` | −13.0 | −37.9 | −42.5 | **already soft** (Gemini's "square wave" claim is wrong for the 3rd time) |
| `music.title_1` / `music.hub_1` (the First Light intro cut) | −17.3 LUFS | | | ~6 dB quieter than the full tracks (−11 to −12.5 LUFS), at the same cue volume |

## How to run
- Fresh session: orient (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-20.md` … `Run-22.md`, git log).
- Branch **`step-23`** from the current HEAD. Same rules as before: don't stage the 15a work in progress,
  `Assets/_Recovery/`, `_lanes/`; stage by path; commit per section "(headset test pending)"; no AI attribution; don't
  push or merge; unattended = no questions, flat bridge, tests, smoke run.
- **Part 1 (A–D) needs nothing from Mugesh: do it first.** Part 2 (E–G) needs `ELEVENLABS_API_KEY` on a paid plan; if
  it isn't set, stop after D and say so in the report.
- Report: `Assets/Docs/Run-23.md` (every changed level old → new, the cue source audit, generation list + credits used,
  an **A/B listening list** for Mugesh with the Dev panel buttons to flip each).

# Part 1 — balance (no ElevenLabs needed)

## A. Music levels and fades
1. Cue volumes: `music.title` 0.25 → **0.42**, `music.hub` 0.25 → **0.40**, `music.lodge` 0.18 → **0.35**,
   `music.galewick` 0.25 → **0.30**, `music.lilymere` 0.25 → **0.30**, `music.cinderwood` 0.25 → **0.28** (keep the
   +2 dB restored lift), `music.temple` 0.25 → **0.28**, `music.battle.wild` 0.25 (same), `music.finale` 0.40 → **0.38**.
2. The restoration bloom fades in over **1.8 s** (was 3). Leaving the lodge: music fades out over **1.8 s** (was 1.5).
3. While `music.finale` plays, the Still Isle ambience ducks **−6 dB** (birds were fighting the climax).
4. Keep the Dev panel music rows (one per cue) and show the new values.

## B. Ambience and effect levels
5. `amb.stillisle` **−3.5 dB**; on the title screen and while `music.title`/`music.hub` play it also gets a gentle
   low-pass (~1.2 kHz) so the piano sits on top (AudioLowPassFilter on the ambience source, or a softer baked variant).
6. `amb.lodge` **−4 dB**. Footsteps (`step.*`) **−6 dB** and ±8 % random pitch per step. The area-change sounds
   (`scene.rise`, `scene.sink`, `gate.enter`) **−6 dB**, and they must **fade out over ~300 ms after** the new area has
   appeared: never cut off by the scene swap (find where the swap stops them and let them finish on a source that
   survives the swap).
7. Jingles: leave the level (already −9 dB since step 16, peaks measured soft). Part 2 offers acoustic versions for an
   A/B.

## C. Which clips each cue really plays (audit)
8. Several cues mix very different sources (e.g. `step.grass` = 6 synth noise + 5 Kenney + 11 ElevenLabs free;
   `amb.lodge` = synth + one free clip). Write a table in the report: for each cue Gemini flagged (step.*, amb.stillisle,
   amb.lodge, amb.galewick(.restored), the restored "breathing" gust (find its cue: `wind.sweep` or the restore
   swell), scene.rise/sink, gate.enter, knot.pull, keeper.knot.snap, the jingles) list every clip, its source and
   **which ones actually play at runtime** (variant/tier/review rules).
9. Rule from now on: **one source family per cue**, so variants sound alike. For footsteps that means no synth noise
   steps mixed in: until Part 2 lands, play the Kenney + ElevenLabs ones only (low-passed ~2 kHz, short) and drop the
   synth variants from the cue.

## D. Quick fixes on the synth sounds that stay
10. `knot.pull` / `keeper.knot.snap`: re-voice in the Audio Lab synth recipe to a woody rope creak / soft pluck
    (resonant 400–900 Hz ticks, no low buzz), or use a Part 2 clip if better.
11. The Galewick "breathing again" gust (05:51 in part 2): slower swell (~1.5 s in, ~2.5 s out), band-limited
    (~200 Hz – 1.8 kHz), a little moving resonance, plus a soft leaf rustle or a wooden wind-chime note at its peak, so
    it reads as a meadow breeze, not a wave. Part 2 may replace it.

# Part 2 — ElevenLabs paid plan (= step 15b + replacements)

## E. Step 15b as written
12. Do `Assets/Docs/Prompts/step-15b-sfx-paid-regen.md` (confirm a paid tier via the API, regenerate the kept free
    clips with their original prompts, replace the `_FREE` twins, Release Check passes, `AudioCredits.md`). **Budget:
    check the balance first; 15b first, then F; stop and report if F would go past the plan's credits.**

## F. New natural sounds for the flagged ones (generate 3–4 takes each, pick with the contact sheets / AudioScope)
13. Footsteps, for a **tiny figure on a tabletop** (short, soft, close but small; 0.1–0.2 s each, slice from batches):
    - grass: "tiny soft footsteps on short grass, light pats, miniature, gentle, no scrape"
    - path: "tiny soft footsteps on packed dirt path, light taps, miniature"
    - stone: "tiny soft footsteps on smooth stone, small taps, light, indoor"
    - wood: "tiny soft footsteps on a wooden floor, small light knocks"
14. Ambience loops (30 s, seamless): Still Isle "calm sheltered island clearing, very soft breeze, distant gentle
    songbirds, peaceful, no surf, no waves"; Hearth Lodge "small cosy fireplace in a quiet wooden room, soft gentle
    crackle, warm, sparse pops, no rumble"; Galewick restored "breezy hilltop meadow, soft wind through tall grass,
    leaves rustling, a far windmill creak, no ocean".
15. One-shots: the area change "soft airy whoosh, gentle, short, magical, fades out smoothly" (rise and sink); the
    Galewick breathing gust "a single warm gust of meadow wind rising and settling, leaves rustling, 4 seconds";
    knots "soft rope creak and a small wooden pluck as a knot comes loose".
16. **Acoustic jingles for an A/B** (1–2 s, matching the warm acoustic soundtrack: felt piano, marimba, celesta, harp,
    no synth, no chiptune): catch "short happy marimba and celesta flourish, rising"; level up "short bright harp and
    celesta upward run"; victory "short warm acoustic victory flourish, plucked strings and marimba"; evolve "magical
    rising harp glissando with soft chimes"; shard "crystal bell shimmer, single warm chord"; keeper "gentle solemn
    resolution, warm woodwind and harp, 2 s". Keep the current synth jingles; add a Dev panel switch
    "Jingles: synth / acoustic" (default synth until Mugesh picks).
17. Import with Import Gen Batch into the same cues (paid, no `_FREE`), one source family per cue (C9), level copies as
    the gen lane does (−18 dB mid-band, peak ≤ −1 dBFS), then re-check the A/B levels so a new sound isn't louder than
    the one it replaces. New footsteps keep B6's −6 dB intent: judge by ear-level parity with the Kenney steps.

## G. Credits and the check
18. `AudioCredits.md`: ElevenLabs Sound Effects, paid plan, month; Kenney CC0; synth; music (Gemini/Lyria, by Mugesh,
    terms saved). In-app credit lines unchanged except what 15b already swaps.
19. Release Check passes (0 free clips), tests, smoke run. In the report: the A/B list in play order (title →
    lodge → Galewick walk → battle + catch jingle → tower knots → restore gust → area changes).

## Not doing (from Gemini's report)
- Changing the music state logic: it works.
- Jingles "8-bit square wave": measured soft; the A/B in F16 answers the real question (style), not a filter.
- Filter numbers as law (Q 2.5, 0.15 Hz LFO, exact Hz): use them as starting points, judge by the measurements.
