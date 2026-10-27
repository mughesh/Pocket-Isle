# Audio gen lane — ElevenLabs batch (third Claude account)

You are the **gen lane** for Pocket Isle, a cozy hands-first mixed reality creature game for Meta Quest (a ~70 cm
floating toy island in the player's real room; tiny explorer, glass capture pods, a fire forest "Cinderwood", a
frightened fire Keeper stag "Hearthstag", a Lantern Spirit). Two other Claude Code sessions are running: the **main lane**
(game code, `D:\UNITY\projects\Meta Hackathon`) and the **audio lane** (Unity audio tools, worktree
`D:\UNITY\projects\PocketIsle-audio`, branch `audio`, now running `step-audio-b-fixes.md`: synth models, AudioScope,
importer). Your job: **generate sound effects and ambience with the ElevenLabs Sound Effects API into a staging
folder outside both repos.** The audio lane imports your picks into Unity.

Read first (read-only): `D:\UNITY\projects\PocketIsle-audio\Assets\Docs\Audio\SoundList.md` (what the game needs and
why) and this file.

## Rules
- Work only in **`D:\UNITY\projects\_audio-sources\elevenlabs\`** (create it). **Never write inside either repo** and never run
  git there. Never touch Unity.
- API key: environment variable `ELEVENLABS_API_KEY`. **Never print it, log it or write it to a file.** If it's missing,
  stop and tell Mugesh: `setx ELEVENLABS_API_KEY "<key>"` in a terminal, then restart Claude Code.
- **Plan tag.** Ask Mugesh once whether the account is on **Free** or **Starter (paid)**, and write it into every sidecar and
  the manifest (`"plan": "free"` or `"paid"`). Free-plan output has no commercial licence and needs attribution; the audio
  lane uses it for listening only, and the release check blocks it. If Mugesh upgrades mid-run, record the switch and
  set the plan per file.
- **One free account per person** is ElevenLabs' rule. Don't suggest or use extra accounts.
- Check the **current API reference** (docs → Text to Sound Effects) before coding: endpoint (expected
  `POST https://api.elevenlabs.io/v1/sound-generation`, header `xi-api-key`), fields (expected `text`, `duration_seconds`,
  `prompt_influence`, `loop`, `model_id`), and `output_format` (prefer 48 kHz WAV/PCM; loops may only come as MP3). One request =
  one clip.
- **Credits:** with a set duration the API charges about **20 credits per second**; auto-length costs about 100 per clip
  (verify in the docs). **Always set `duration_seconds`.** Keep `ledger.csv` (time, cue, seconds, credits estimate, plan)
  and check the account balance via the API before each group. **Budget: 9,000 credits** for the whole list below. Stop
  and ask before going past it.

## Tooling (use what's on the PC: Python preferred; check for ffmpeg)
- `gen.py`: reads `genlist.json`, calls the API with retries and backoff, saves `<cue>/<cue>_<n>.<ext>` + `<cue>_<n>.json`
  sidecar (prompt, duration, influence, loop, model, output format, plan, date, credits). Flags: `--group`, `--cue`,
  `--dry-run` (prints the credit estimate only).
- **Batch-and-slice for short one-shots** (5× cheaper): ask for a sequence in one clip ("eight separate tiny footsteps
  with short gaps"), then slice by onset detection into variants (`<cue>_s<n>.wav`), trim silence, 5 ms fades.
- `scope.py`: per group, a contact-sheet PNG (spectrogram 0–8 kHz + waveform per clip, labelled) and `report.md`
  (duration, peak dBFS, mid-band 250 Hz–6 kHz level, low share < 250 Hz, silence share). **Open the PNGs and read them
  yourself.** You can't hear, but you can see. Regenerate (with a tweaked prompt) anything that's mostly silence, mostly sub
  (low share > 0.45), clipped, musical when it shouldn't be, or obviously wrong. Max 2 retries per cue.
- **Level:** don't normalise the originals. Write a `level/` copy trimmed and normalised to −18 dB mid-band level, peak ≤ −1 dBFS
  (generated one-shots come out quiet: Mugesh's stag stomp test peaked at −19 dBFS with 2 s of near-silence).
- `manifest.json`: every kept clip → cue id, file (level copy), original, plan, prompt, your note (`good` / `ok` /
  `weak` + one line why). The audio lane imports from this.

## Prompt style
Material + action + size + distance + mood. One-shots end with *"close-miked, loud, dry, no reverb, no music, no
voices, single sound"* (sequences: "…separate sounds with short gaps"). Scale: **a tiny toy world seen up close**, so ask
for "small, light, miniature" versions of real sounds. Mood: **cozy, warm, magical, never scary, violent or sci-fi**.
`prompt_influence` 0.5–0.7. Beds: set `loop: true`, 30 s, and *"no sudden events, steady and calm"*.

## Gen list (P0 first; candidates = clips to request)
| Group | Cue | Prompt direction | Len s | Cands |
|---|---|---|---|---|
| Ambience beds | `amb.stillisle` | calm sunny meadow, soft breeze through grass, a few distant small birds, peaceful | 30 loop | 1 |
| | `amb.cinderwood` | hushed warm autumn forest under a sleeping spell, dry leaves in a low breeze, far-off ember crackle, almost no birds | 30 loop | 1 |
| | `amb.cinderwood.restored` | the same forest come alive, warm birdsong, cosy crackle, gentle wind in glowing leaves | 30 loop | 1 |
| | `amb.forgehall` | large quiet stone hall, low warm hum of embers, distant soft drips | 30 loop | 1 |
| | `amb.lodge` | small cosy room with a hearth, soft fire crackle, occasional wood creak | 30 loop | 1 |
| Sweeteners (sequences → slice) | `sw.bird` | ten different tiny songbird chirps with gaps | 8 | 1 |
| | `sw.leaves` | a few dry leaves fluttering down, four separate times | 6 | 1 |
| | `sw.creak` | small wooden creaks, five separate | 5 | 1 |
| | `sw.crackle` | single ember pops and crackles, eight separate | 4 | 1 |
| | `sw.drip` | single water drips into a small pond, eight separate | 4 | 1 |
| | `sw.gust` | soft short wind gust through grass | 2.5 | 3 |
| | `sw.insect` | a brief distant cricket chirp, four separate | 5 | 1 |
| Walk | `step.grass` / `step.path` / `step.stone` | eight tiny light footsteps of a small creature on soft grass / packed dirt path / stone | 4 each | 1 each |
| | `grass.rustle` | small creature brushing through tall grass, six short rustles | 4 | 1 |
| | `walk.bump` | small creature bumps softly into a rock | 0.6 | 3 |
| Encounter | `enc.rustle` | something small hiding in tall grass shakes it, sudden rustle | 1.2 | 3 |
| | `pond.splash` / `pond.tap` | tiny creature hops into a pond / fingertip taps still water | 1.0 / 0.6 | 3 / 3 |
| Island | `island.grab` / `island.release` | small mossy rock base picked up, soft stone scrape / set down, soft settle | 0.7 | 3 / 3 |
| | `island.unfold` | soil and pebbles rising out of the ground and settling, gentle, earthy, a little magical | 2.5 | 3 |
| | `scene.sink` / `scene.rise` | small tiles of earth sinking into the ground / rising out of it, soft rumble and pebbles | 1.5 | 2 / 2 |
| | `clearing.rumble` / `pads.rise` | grass bending flat and small rocks sinking / two round earth pads rising, soft thump | 1.4 / 0.7 | 2 / 3 |
| Capture | `pod.throw` / `catch.toRim` | small glass marble whooshing through the air / then landing with a light clink | 0.6 / 0.8 | 3 / 3 |
| | `catch.shatter` | small glass bauble cracks and shatters lightly, sparkly | 0.8 | 3 |
| Battle | `move.charge` | small magical energy gathering, soft rising whoosh | 1.0 | 3 |
| | `move.shoot.fire` / `.water` / `.wind` / `.neutral` | small cute fireball launched / small water jet / small gust of wind / small energy pellet: toy-like, playful | 0.7 | 3 each |
| | `move.impact.fire` / `.water` / `.wind` / `.neutral` | the same, hitting a small creature softly: crackle burst / bubbly splash / gust puff / soft thump | 0.8 | 3 each |
| | `move.lunge` / `move.slam` | small creature dashes forward / jumps and slams the ground, soft thud with dust | 0.5 / 1.0 | 3 / 3 |
| | `hit.normal` / `hit.super` / `hit.weak` | soft cartoon bonk on a small creature / bigger bonk with a sparkle / dull muffled tap | 0.4 / 0.6 / 0.4 | 3 each |
| | `faint` | small creature flops down softly | 1.0 | 3 |
| Cinderwood | `log.lift` / `log.drop` | small wooden log lifted with wood strain and creak, leaves and dust falling / dropped onto a rock ledge, solid thud, leaves and dust | 1.5 / 1.2 | 3 / 3 |
| | `bellows.pump` | small leather bellows squeezed once, soft air puff | 0.8 | 3 |
| | `brazier.warm` / `brazier.light` | coals warming with a soft hiss / small brazier bursts into flame, warm whoomph | 1.0 / 1.2 | 2 / 3 |
| | `forge.door` | heavy stone door sinking into the ground, grinding, a hiss of steam and a few sparks | 3.0 | 2 |
| | `keeper.stomp` / `keeper.snort` / `keeper.fury` | large gentle stag stomps packed earth, loud close thump, pebbles rattle / big deer snorts, breathy, a puff of smoke / fire flares up around it, whoosh and crackle (not scary) | 1.2 / 1.0 / 1.6 | 3 each |
| | `restore.swell` | warm magical wave spreading through a forest, rising shimmer, leaves rustling alive | 4.0 | 2 |
| | `lantern.sputter` | small lantern flame sputtering | 0.8 | 3 |
| Loops | `ember.crackle` / `steam.hiss` / `shrine.flame` | small ember bed crackling / small hot spring steaming / small calm flame in a stone bowl | 10 loop | 1 each |

Estimate: about 7,000 credits before retries (the beds are 3,000 of it); retries bring it to about 8,500. Generate in this order: **beds → Cinderwood → battle → walk → island → the
rest**, so the most valuable sounds land first if credits run out.

## Hand-off
- After each group: update `manifest.json` and tell Mugesh in 2–3 lines (group done, credits used / left, any `weak`).
- At the end, write `HANDOFF.md`: credits used, the manifest summary (good / ok / weak counts), contact sheet paths, and
  the cues that should stay synth. Mugesh passes it to the audio lane, which imports with its importer.
