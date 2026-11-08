# Step 20: the soundtrack goes in (music as the reward for restoring a realm)

Decided Nov 8, 4 AM (design chat). Mugesh made the tracks with Gemini/Lyria and Gemini wrote an audio spec; the design
chat measured every file and checked the spec against our audio code (`MusicDirector`, `AudioDirector`, `AudioService`
with step 18's `restSeconds`). What's below is the adapted plan. Mugesh hasn't headset-tested step 18 or step 19 yet.

## How to run
- Fresh session: orient (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Assets/Docs/Run-18.md` section G,
  `Assets/Docs/Run-19.md` if step 19 ran, git log).
- Branch **`step-20`** from the current HEAD (after `step-19` if it ran, else `step-18`). Same rules as before: don't
  stage the 15a work in progress, `Assets/_Recovery/`, `_lanes/`; stage by path; commit per section "(headset test
  pending)"; no AI attribution; don't push or merge; unattended = no questions, flat Play via the bridge, tests, smoke run.
- At the end: `Assets/Docs/Run-20.md` (what plays where, a state → cue table, levels, the headset test list in play
  order) and a `.claude-handoff.md` entry.

## The idea (why)
Wall-to-wall music in a seated MR game tires the ears within minutes, and our story is "the elements stopped flowing".
So: **an unrestored realm has no music, only its thin, still ambience. Music comes back when you restore the realm.**
Temples get a tension track; the finale gets the big swell. Every realm track plays whole, then rests (step 18's
`restSeconds`), never a hard loop. This also gives the Devpost a line: "music returns when you heal a realm".

## The tracks (all measured: ~2 min each, about −11 to −12.5 LUFS, soft intros, natural fade-outs)
| File (in `Assets/_Project/Audio/` unless noted) | Goes to | Cue |
|---|---|---|
| `Steps_Through_Tall_Grass.mp3` (120 s) | Galewick, **restored** | `music.galewick` |
| `Lanterns_by_the_Spring.mp3` (119 s; quieter kalimba-like tail from 1:32) | Cinderwood, **restored** | `music.cinderwood` |
| `Where_the_Lotus_Blooms.mp3` (120 s) | Lilymere, **restored** | `music.lilymere` |
| `Rite_of_the_Unbroken.mp3` (123 s; steady build) | Keeper halls **and** Keeper battles | new `music.temple` |
| `Clips/Music/music.battle.wild_1.mp3` (Tall Grass Duel, step 18) | wild battles (unchanged) | `music.battle.wild` |
| `Clips/Music/music.galewick_1.mp3` (First Light at the Summit, step 18) | **source only now**: cut into the three clips below | — |
| `Clips/Music/music.title_1.mp3` (cut 0:00–0:43, fades out from 0:38.5) | title screen | `music.title` |
| `Clips/Music/music.hub_1.mp3` (same cut, own copy: Sync Cues won't share a clip) | Still Isle, once per arrival | `music.hub` |
| `Clips/Music/music.finale_1.mp3` (cut from the swell at 0:41.5 to the end, 75 s) | the Heart wakes → credits | `music.finale` |
| `Clips/Music/music.lodge_1.mp3` (Lanterns tail from 1:32, 27 s, fades in) | Hearth Lodge | `music.lodge` |
The four cut clips were made by the design chat and are already in `Clips/Music/`. The mapping of the three realm
tracks is the design chat's reading of Gemini's spec (it named them Track_1 Lilymere, Track_2 Cinderwood,
Track_4 Galewick): if a track's character obviously doesn't fit its realm, say so in the report, don't swap on your own.

## A. Files and cues
1. Move the realm/temple tracks with the AssetDatabase (keep GUIDs) to `Clips/Music/` as `music.galewick_1.mp3`,
   `music.cinderwood_1.mp3`, `music.lilymere_1.mp3`, `music.temple_1.mp3`. The step 18 First Light file currently at
   `music.galewick_1.mp3` moves out of the project to `D:/UNITY/projects/_audio-sources/music/First_Light_at_the_Summit.mp3`
   (it's only a source now) before the Galewick one takes its name.
2. Import every music clip: 2D, Streaming, Vorbis ~0.5, stereo, no normalize. Run Sync Cues; `awaitingClips` off on
   every cue that now has a clip. Add the `music.temple` cue + `AudioIds.MusicTemple`.

## B. The rules (MusicDirector / AudioDirector)
3. **Unrestored realm = no music** (ambience only). **Restored realm = its track**, play whole, `restSeconds` 45.
   Put this behind one switch, `MusicDirector.RealmMusicBeforeRestore` (default **false**), with a Dev panel toggle
   ("Realm music: reward / always") so Mugesh can A/B it on the headset. With "always", restored or not, the track
   plays (today's step 18 behaviour).
4. **The restoration bloom:** the moment the shard is taken in the hall and the realm revives, the realm's track starts
   (fade in ~3 s) under the `.restored` ambience, and it's playing when you walk back out.
5. **Temples:** `music.temple` starts on entering a Keeper hall (puzzles included) and carries straight into the Keeper
   battle **without restarting** (`BattleKeeper` uses the same cue; same cue = no crossfade). On the Keeper being
   calmed: fade out over 2.5 s to silence, then the bloom (4) when the shard is taken. Loops (restSeconds 0) while in
   the hall: hall visits are short.
6. **Title:** `music.title` once (43 s, it fades itself); it doesn't restart while the opening flows into the
   tutorial. No music while only the shard floats (step 16 E rule stands).
7. **Still Isle:** `music.hub` once per arrival from a realm or a load (a "welcome home"), then ambience only until the
   next arrival (no rest cycle).
8. **Hearth Lodge:** `music.lodge` once on entering (27 s), then quiet; leaving fades it out.
9. **Finale:** `music.finale` starts the moment the Heart wakes (all three shards in) and plays through the Hall of
   Fame and the credits; it ends by itself. Skip fades it over 1.5 s. (`AudioDirector` already plays `MusicFinale`
   when it has clips.)
10. **Wild battles:** unchanged from step 18 (battle track in, afterwards the area's music waits for its rest). In an
    unrestored realm the battle track is the only music you hear: that contrast is fine.
11. Area crossfades 3 s (was 2); battle 0.6 s unchanged. Keep the dialogue (−4 dB) and jingle ducks.

## C. Levels
12. Start every cue at **0.25** except: finale **0.4**, lodge **0.18**, temple **0.25**. Report in-game loudness next to
    each area's ambience bed and the Spirit's blips (the step 18 method). Music sits under the ambience peaks and the
    voices; the finale may sit a bit above.
13. The Dev panel music tool (step 18 "Galewick −") becomes one row per music cue (− / + 0.05, shows the value), so
    Mugesh can tune each on the headset and tell the design chat the numbers.

## D. Credits and licences
14. `Assets/Docs/Audio/AudioCredits.md`: list every music file, its in-game cue and "made with Google Gemini (Lyria)
    by Mugesh; licence terms to be checked before submission". The in-app credit stays "Music: Mugesh" (no brand
    names in the app unless a licence requires it).

## E. Check
15. Flat Play through: title → tutorial → Galewick (silent, ambience only) → wild battle (battle track) → back (silence
    after) → tower (temple track) → Keeper battle (same track, no restart) → calm (fade) → shard (Galewick bloom) →
    walk out (Galewick track) → Still Isle (hub once) → lodge (lodge once) → See the ending (finale). Log every music
    change (`[Music] a -> b`) and paste the sequence in the report. Then the "always" toggle once. Smoke run passes.

## Not doing (from Gemini's spec)
- **Ducking music under every SFX (−11 dB):** constant pumping; the jingle and dialogue ducks are enough.
- **A high-shelf EQ on battle music:** not needed; impacts already read.
- **Exact volume numbers from the spec (0.20 ambience etc.):** ambience levels stay as they are; music levels per C.
