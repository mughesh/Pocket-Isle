# Step audio-D: sounds for everything built since the audio merge (Galewick, Cinderwood moments, Lilymere, finale)

**Unattended run** (Mugesh is away on a trip until Nov 3). You're a fresh session, maybe on a different Claude
account. Orient first: `CLAUDE.md`, `.claude-handoff.md` (newest on top), **`Assets/Docs/Audio/PhaseB-Hooks.md`**
(sections 2–5 = how the audio system is wired; **"Missing sounds (next audio pass)" at the bottom = your work list**),
`Assets/Docs/Audio/SoundList.md`, `Assets/Docs/Audio/NightNotes.md`, `AudioCredits.md`, the Audio Lab tools
(`Assets/_Project/Audio/Editor/`), `git log --oneline -20`, the Unity bridge. Mugesh's verdict on the current sounds:
**"it fits so beautifully, I almost don't need changes"** — so match the existing style (synth recipes + levelled gen
clips, loudness tiers, cue volume 0.8) and don't re-do sounds that already have cues.

How the audio was made before (keep the same process):
- **Synth first** (Audio Lab models: modal, particles, bubbles, fire, wind, friction, breath; layer groups; bake;
  AudioScope must pass). Everything interactive or that follows the hand in real time is synth.
- **ElevenLabs Sound Effects API** only for what synth can't do well (beds, rain, running water, frogs, birds, big
  organic textures). Output goes to the **staging folder `D:\UNITY\projects\_audio-sources\elevenlabs\`** (never inside
  the repo), same tooling as before (`gen.py`, `genlist.json`, `scope.py`, `ledger.csv`, `manifest.json`, `level/`
  copies at −18 dB mid-band, peak ≤ −1 dBFS), then **Tools > Pocket Isle > Audio > Import Gen Batch** brings the picks
  into `Clips/Gen/<cue>/`. Read the gen lane's earlier `HANDOFF.md` / `NightNotes.md` in the staging folder first.
- Key: env var `ELEVENLABS_API_KEY` (never print, log or write it). **Free plan**: tag every clip `plan: free`
  (`_FREE`); the release check blocks them until Mugesh regenerates the picks on a paid month. One account per person.
- **Credits:** check the balance via the API first and before each group; always set `duration_seconds`; batch-and-
  slice short one-shots. **Spend at most what's left minus 500.** If the key is missing or credits run out, make those
  cues in synth (best effort) and list them as "gen later" in the report. Don't stop the run for it.

## Work list (from PhaseB-Hooks "Missing sounds"), with the source to use
| Area | Cues | Source |
|---|---|---|
| Galewick | `wind.sweep` (real-time, follows hand speed: synth wind model with speed → gain/filter), `mill.turn` (creak loop), `mill.stop`, `kite.tug`, `cloud.part`, `knot.pull`, `keeper.knot.snap` | synth |
| | `tower.door` (wind rush + stone), `voice.galeowl` (soft hoo-hoo) | gen, synth fallback |
| | `amb.galewick` / `amb.galewick.restored` (still air with rare gusts / breezy meadow, creaks, larks) | gen beds (30 s loop) |
| Cinderwood | `flame.cup` (crackle loop that swells), `flame.rekindle`, `lantern.light`, `smoke.clear`, `ember.pick`, `ember.drop` | synth |
| | `firebox.roar`, **`log.drop`** (solid wooden thud; Mugesh: today's is almost silent) | gen + synth layer |
| Lilymere | `lily.click`, `lotus.bloom`, `channel.ripple`, `water.slosh` (loop while held) | synth |
| | `rain.soft` / `rain.hard` (loops that follow the cloud), `frog.croak`, `frog.chorus`, `water.scoop`, `water.pour`, `water.spill`, `ice.crack`, `ice.shatter`, `waterfall.run` (loop), `lagoon.rise` | gen (rain/water/frogs/ice), synth layers where they help |
| | `amb.lilymere` / `amb.lilymere.restored` (dry still air, faint drips / water lapping, frogs, birds, reeds) | gen beds |
| Finale | `heart.beat` (slow warm lub-dub loop; body 80–300 Hz + soft click, never only sub), `spark.chime`, `credits.tablet`, `gates.bloom` (fire crackle + water sparkle + wind whoosh rising) | synth |
| | `heart.crack` (deep stone cracking, grit falling), `heart.wings` (big soft feathery-stone unfurl), Heart whisper hum (soft breathy, optional) | gen + synth layer |
| Battle / UI | `hp.low` (soft pulse, hook it: start under 25 % HP, stop on heal / end), `spirit.beam`, `spirit.focus` | synth |
| **Typing blip** (Mugesh: too frequent, too many beats, annoying) | `DialogueBubble.Blip`: one soft tick every ~6 characters at most, quieter, gentle random pitch; objective cards get one soft chime when they appear instead of ticks; the Heart's whispers stay silent | synth + code |
| Cries | `cry.<species>` for every new wind and water Isling + evolutions (**Make Cry Profiles (missing only)**, Create missing, Bake all, Sync cues), plus `cry.Galeowl`, `cry.Tidemother`, a gentle `voice.heart` breath for the Heart | synth (existing cry system) |
| Loops on landmarks (P1) | shrine idle flame, ember vents, hot springs, waterfall, mills: one 3D loop per landmark in view | hook existing cues |

Prompt style for gen (as before): material + action + size + distance + mood; one-shots end with "close-miked, loud,
dry, no reverb, no music, no voices, single sound"; **a tiny toy world seen up close** ("small, light, miniature");
**cozy, warm, magical, never scary**; beds `loop: true`, 30 s, "no sudden events, steady and calm".

## Then hook everything
- Add the cue ids to the cue table and `AudioIds`; replace every `PuzzleSounds` placeholder listed in the missing table
  with its cue (keep PuzzleSounds only as a fallback for a missing cue id, logged once).
- Ambience profiles **GalewickFaded / GalewickRestored / LilymereFaded / LilymereRestored** with the new beds and
  sweeteners (`sw.gust`, `sw.bird`, `sw.creak` / `sw.drip`, `frog.croak`), wired where the realm sets its area.
- Loops that follow things (rain under the palm cloud, water slosh while held, flame cup swell, wind sweep) start and
  stop with their piece and follow its position.
- `music.lilymere` and `music.finale` states already exist; leave their cues empty (Mugesh makes the music).

## Checks, commits, report
- AudioScope must pass for every new synth cue; gen clips that fail are listed for Mugesh, not dropped.
- Flat bridge Play: walk each realm's moments and the finale once; no `[Audio] Unknown cue id`, no stuck loops after a
  scene change or pause. EditMode tests green.
- Commits (no AI attribution, don't push): "Audio-D: synth cues for Galewick, Cinderwood, Lilymere, finale", "Audio-D:
  gen batch 2 import", "Audio-D: hooks and ambience", "Audio-D: cries for the wind and water rosters", "Audio-D: quieter
  typing". Update `PhaseB-Hooks.md` (mark rows done, what's left), `AudioCredits.md`, `.claude-handoff.md`.
- Build the QA APK at the end (water gate as it is), never overwrite `Builds/PocketIsle-QA-oct27-tester.apk`.
- Write **`Assets/Docs/Audio/AudioD-Report.md`**: per cue (source, variants, AudioScope result, "listen to this one
  first" picks), credits used and left, gen clips that need Mugesh's ear, cues still synth that should be gen later,
  and a **headset listening checklist** in play order.
