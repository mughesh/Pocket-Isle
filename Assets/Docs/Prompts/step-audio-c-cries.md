# Step audio-C — Isling cries v2 (after step audio-B)

Audio lane, worktree `D:\UNITY\projects\PocketIsle-audio`, branch `audio`. Lane rules from `step-audio-a-lab.md`
apply. Needs AudioScope from step audio-B.

## Why
Batch 1 cries share one shape: almost the whole roster is Fire and timbre comes only from element, so every cry is two
short down-bent buzz blips plus a broadband crackle tail as loud as and longer than the voice. Pitch range is narrow,
and formants do nothing because F0 (600–900 Hz) sits above F1. Mugesh: "Almost all the creature cries are similar."
Generated (ElevenLabs) creature vocal one-shots may be used as an extra layer if step B's generator is set up, but
the three-axis design below decides each voice.

## Cries v2
Read every species (read-only: `Data/Creatures/SpeciesRegistry.asset`, `Species/`, any description or
concept text in `Assets/Docs/GameDesign.md` and `Assets/Docs/Story/`) to judge each creature's personality.
Then give every **base form** a unique voice from three independent axes. **Timbre comes from the archetype
(creature personality), not from element.**

**Archetype (source and body):**
| Archetype | Recipe | Good for |
|---|---|---|
| Squeak | high FM (ratio 1–2, fast index decay), F0 700–1400 Hz | tiny, nervous |
| Chirp/whistle | sine plus breath noise, fast up-sweeps, F0 900–2000 Hz | birds, flyers |
| Honk/bleat | buzz with formants, **F0 120–350 Hz** so vowels are audible | goofy, big-mouthed |
| Purr/growl | buzz with AM 20–40 Hz, F0 90–250 Hz, mid-band body | sturdy, armoured, sleepy |
| Coo/hoot | triangle/sine, soft vibrato, F0 250–600 Hz | gentle, round |
| Rattle/clack | pulse train of tuned wood or clay clicks (8–25/s) plus a short pitched tone | beetles, rocky, mechanical |

**Contour:** rise, fall, arch (rise-fall), dip (fall-rise), octave leap, stutter (same note repeated),
trill (fast alternation of 2 notes), wobble. Range **±5 to ±12 semitones**, not ±2–5.

**Rhythm:** one long (0.5–0.9 s) · two even · short-short-long · long-short · three fast · four-note trill.
Total length 0.35–1.4 s, varied across the roster.

- **Element is seasoning only**, at least 12 dB under the voice, ≤ 120 ms: fire = 2–5 sparks, water = 1–3 bubble
  blips, wind = a breath puff, neutral = nothing. Delete the long crackle tail.
- **Formants only when F0 < 350 Hz.** Above that, character comes from contour, FM and AM.
- **Pitch:** spread the roster across about 3 octaves (110 Hz–1.4 kHz start notes). Size sets the register, the archetype
  narrows it. Snap the start note to D major pentatonic; glides may pass between notes.
- **Evolved forms:** the same archetype, contour and rhythm as the base (clearly related), 4–7 semitones lower,
  1.2× slower, and a **growl layer in 150–600 Hz** (AM on a buzz). No sub-only weight.
- **Hearthstag (Keeper):** honk archetype at F0 ~110–140 Hz with real vowel formants ("hoo-ahh"), a slow
  arch contour, a warm, quiet crackle bed. Big and kind, not scary.
- **LanternSpirit** keeps a high, bell-like voice that matches `spirit.blip`.
- Write the assignment table (species → archetype, contour, rhythm, start note, length, element seasoning) to
  `Assets/Docs/Audio/CryTable.md` with a one-line reason per species. **No two base forms may share more
  than one of the three axes.**
- Extend CryProfile with these fields. The recipe builds from them, and old fields can go.
- Hurt and faint runtime pitch rules stay. Re-check that they still sound right with the new lengths.

**Distinctness check (AudioScope):** per cry, a feature vector of pitch contour (F0 track resampled to 16 points,
in semitones relative to the start), log start pitch, duration, syllable count (envelope peaks), spectral centroid,
and AM rate. Report each cry's nearest neighbour and the distance. **Fail** if any two base forms are closer than a
threshold you calibrate so that today's v1 cries would almost all fail. Also make `Tools/Pocket Isle/Audio/Cry
Lineup`: a contact sheet of every base cry in one PNG, plus a "Play all cries" button in the Lab with a 0.4 s gap
between cries.

Commit: "Audio: Isling cries v2 (archetypes, contours, rhythms)".

## Report
Lab → Play all cries; the Cry Lineup PNG; the nearest-neighbour table; anything that still fails.
