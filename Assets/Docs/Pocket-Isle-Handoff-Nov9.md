# Pocket Isle — design-chat handoff (Nov 9, 2026, 1:30 PM IST)

You are taking over as **Mugesh's design partner** for Pocket Isle, his solo entry to the **Meta VR Start Developer
Competition 2026**. This file is the whole context; the repo on his PC is more current than anything here.

## 1. How to work with Mugesh
- XR design engineer, Chennai (IST). Tests on a Quest 3. Perfectionist on the first minutes; wants honest triage:
  fix what judges will feel, push back on bad suggestions (including Gemini's), keep the feature freeze.
- **Format:** short and scannable (tables, bullets), bold the one must-not-miss thing, the why (1–2 lines) before the
  how, no narration, no re-explaining settled points. He skims long text; recap names/terms when they come up.
- Design questions: selectable options with the recommended one first.
- Commands: one line, Git Bash on Windows, forward slashes (`/d/UNITY/...`).
- Admit mistakes plainly. Zero audio background: explain audio terms in plain words.
- **Your job:** triage his notes, screenshots, videos and Gemini reviews; verify claims (frames with ffmpeg, the code,
  measurements) before acting; then **write step prompts straight into `Assets/Docs/Prompts/` on his PC** (via the
  linked-PC tools if this chat has them; otherwise give him the file). Keep a handoff doc current.
- **Claude Code** (fresh sessions, his other account) runs one prompt at a time. Rules every prompt repeats: orient
  first (`CLAUDE.md`, `.claude-handoff.md` newest on top, latest `Assets/Docs/Run-NN.md`, git log); work on a branch
  `step-NN`; stage by path, never `git add -A`; never commit keystores, passwords, API keys, `Assets/_Recovery/`,
  `_lanes/`; commit per section ending "(headset test pending)"; **no AI attribution lines**; **don't push unless
  Mugesh says "push"** (Claude Code's own push was refused once; Mugesh usually pushes himself); unattended runs = no
  questions, flat Editor Play via the Unity bridge, EditMode tests, the smoke run (Tools > Pocket Isle > QA > Smoke
  Run), revert after ~45 min stuck; end with `Assets/Docs/Run-NN.md` (done/partly/not, numbers, shots, a headset test
  list in play order) + a `.claude-handoff.md` entry.

## 2. Paths
- Repo: `D:\UNITY\projects\Meta Hackathon` (Unity 6, Meta XR SDK / ISDK v207, URP, toon, texture-free, SDF props baked
  at edit time). GitHub remote `origin`, branch **master**.
- Prompts: `Assets/Docs/Prompts/` (`step-NN-*.md`, reference images in `step-NN-img/`). Notes: `Assets/Docs/Notes.md`,
  `Notes copy.md`, `Notes-img/`. Reports: `Assets/Docs/Run-NN.md`. Regressions: `Assets/Docs/Regressions.md`.
- Audio: `Assets/_Project/Audio/` (cues in `Data/Cues/*`, clips in `Clips/{Synth,Levelled/Gen,Levelled/Kenney,Music}`,
  `MusicDirector`, `AudioDirector`, Audio Lab tools). Generation lanes outside the repo:
  `D:\UNITY\projects\_audio-sources\elevenlabs` (old free run) and `\elevenlabs-paid` (Nov 9 paid run: sidecars,
  ledger, picks). Music sources: `_audio-sources\music`.
- Credits portrait: `Assets/_Project/Art/Credits/Mugesh.png`.

## 3. Competition (verified from the official rules/FAQ/judging pages, Nov 3)
- Gaming track, **New Experience** division. **Deadline Nov 18, 12:00 PM PST = 1:30 AM IST Nov 19.** Our target: submit
  Nov 16. No updates during judging (Nov 18 – ~Dec 11); keep the build free and available until winners.
- **Build = a release-signed APK in a NEW release channel named exactly "Competition"** on the Meta Developer Dashboard
  (an APK alone is not considered; debug-signed is rejected).
- Video < 3 min, public YouTube/Vimeo, real Quest footage, AI video must not carry it. Devpost text: inspiration, how
  built, future plans, **target launch date**. English. VR Start membership ("Start Member" forum flair) required.
- Third-party content must be licensed (Meta may ask for proof). No corporate logos/brand names in the app.
- Judging 25% each: Innovation, Experience Design, Technical (60 fps min on Quest 3/3S, hands, gaze, passthrough/MRUK,
  anchors), Polish & Presentation. Mugesh's goal: the $100K grand prize (has been runner-up/finalist in 12+ hackathons).

## 4. The game
Cozy hands-first MR creature collector. You're a giant guardian; a ~70 cm floating island in your room is a window
onto a scrolling world; a tiny explorer walks it (thumb microgesture swipe walks until a thumb tap; look + pinch). Catch
**Islings** (46: fire, water, wind), 1v1 turn battles (Water > Fire > Wind > Water), calm three **Keepers**
(Galeowl wind, Hearthstag fire, Tidemother water), bring three **shards** to the Still Isle statue, which wakes as the
**Heart of the World**.
- Opening: a shard floats in the room (curious hum) → touch → island unfolds → title (Full adventure / Quick journey /
  Settings) → Lantern Spirit lessons (pinch, move, two-hand resize, starter pick, walk) → first battle + catch.
- Realms in order Galewick → Cinderwood → Lilymere, each with arrival hand moments and puzzles (kites pulled into the
  room, ember in your palm, rain/pour water from your palm), temples with Keeper battles; evolution happens on your
  open palm; Journal (Found n/46); Hearth Lodge (party swaps).
- **Quick Journey** (for judges, ~10–12 min): tutorial → Galewick → Galeowl → shard → end card (Keep exploring / See
  the ending / Title).
- Finale: Heart wakes, Spirit lifted to its forehead crystal, Hall of Fame arc, **sky-lantern credits** (one lantern
  per credit rises into the room; the last carries Mugesh's framed portrait), Thank-you card.
- UI: mossy stone tablets; rim menu stone (≡) + left-palm pinch pause; battle moves over the arena's front band; rim
  party pods.
- **Music design (works, Gemini rated it 9.5/10):** unrestored realm = no music, only its still ambience; taking the
  shard = the realm's track "blooms"; temple track carries into the Keeper battle; tracks play whole then rest 45 s;
  First Light intro on title / Still Isle arrival; finale swell. Tracks made by Mugesh with Gemini (Lyria).

## 5. State right now (Nov 9, 1:30 PM)
- **master = `d263812`, pushed.** Contains steps up to 24 + the paid ElevenLabs audio (tests 372/372, smoke run OK,
  Release Check passes: 0 free clips in cues). Untracked: some prompt files; `Assets/_Recovery/` (Unity crash backups,
  never commit).
- **Step 25 written, NOT run yet:** `Assets/Docs/Prompts/step-25-audio-fixups.md`. Fixes found by measuring the paid
  clips: (A) `CreditsText.PaidSoundsDone` still false → the app's credits still say "free plan"; (B) new paid
  footsteps are bright/hissy and ~5 dB over the Kenney steps → low-pass, trim, level-match or drop; (C) the new Still
  Isle bed (`amb.stillisle_22`) swings −46…−14 dB per second like surf → regenerate steadier (cap 2,000 credits) or
  fall back to synth + low-pass; (D) tidy leftover `_FREE` files, fix the paid ledger (sums 1,428 vs 14,279 real).
- **Nobody has heard the new audio.** After step 25: device build + Mugesh's listening pass (list in Run-24/Run-25;
  Dev panel > Tools > Sound A/B: Jingles synth/acoustic, Voices blip/new).
- ElevenLabs: **Starter plan, bought Nov 9 (monthly)**; 14,279 of 40,000 credits used (10 credits/s via the API).
  Licence: paid-plan content is commercially usable forever after cancelling, except Beta services (verified on
  ElevenLabs' help center); model used `eleven_text_to_sound_v2` (not beta). Keep the invoice. Key is in the env var
  `ELEVENLABS_API_KEY` (the first key was pasted in a chat: it should be rotated if it wasn't).

## 6. Recent history (one line each)
- 16 (Nov 6): judge-review polish (ghost fade, softer jingles, Spirit parking, battle spectacle, rim menu stone).
- 17 (Nov 7): first catch aim assist + battle grab lock, rim-locked menus, pods stay in sockets, kites hide in battle,
  temple gate after the shard, ghost hands, finale party arc, island no longer turns when leaning in, shard bursts.
- 18 (Nov 8): evolving heals, +25% HP after wild wins, no one-shots, first music test.
- 19: sky-lantern credits. 20: the soundtrack (reward model). 21–22: Nov 8 bugs, ghost/swipe lesson, statue voice,
  crack effects (feature creep: freeze slipped).
- 23 Part 1: mix balance (music title 0.42, hub 0.40, lodge 0.35, realms 0.28–0.30, temple 0.28, finale 0.38; bloom
  1.8 s; finale ducks ambience −6 dB; Still Isle bed −3.5 dB; lodge fire −4; footsteps −6; whoosh −6 + tail).
- 24 (Nov 9): battle moves moved onto the arena's front band, no level on Keeper labels, temple door silent-block bug
  fixed (`TempleRules.Gate` refused silently after "Not yet"), pour follows the hand tilt; paid audio: 15b regen,
  natural footsteps/beds/one-shots, Keeper + Heart voices (entrance call + murmur grains), acoustic jingles A/B.

## 7. Open items / blockers (in order)
1. Step 25 ran Nov 9 (`Run-25.md`, local on `step-25` / master until pushed). Next: a device build and Mugesh's listening pass; fix by ear.
2. **Release: prompt is `step-26-release.md` (written Nov 9, replaces step 15a), runs after step 25.** The Nov 5 test APK
   already has the release signature, the mic/Bluetooth permissions stripped, and version 1.0.0 (code 11). Still
   unconfirmed: **keystore backed up in 2 places**, upload to a new "Competition" channel. Debug-signed QA and
   release-signed builds can't install over each other: switching means an uninstall, which wipes the save. **Most urgent.**
3. Confirm the "Start Member" forum flair (5 min).
4. Full device playthrough with OVR Metrics (72 fps target, 60 minimum) → **hard feature freeze** (only bug fixes).
5. **Video** (< 3 min; not started; budget Nov 11–14): lead with the giant-hands story; hero shots: kites into the room,
   ember on the palm, rain/pour, evolution on the palm, the bloom, sky-lantern credits. Record with Dev panel Video mode.
6. **Devpost** text (inspiration, how built, future plans, target launch date), screenshots, key art
   (`Builds/Marketing/PocketIsle_KeyArt_1920x1080.png`).
7. Music licence: Google says it doesn't claim ownership of Gemini/Lyria output but the app terms are vague: save the
   terms pages as proof (not legal advice).
8. Nice-to-have only: spatial anchor; grab-lock button on the rim.
Timeline: Nov 9 audio + release build → Nov 10 device playthrough + freeze → Nov 11–14 video → Nov 15 Devpost → Nov 16 submit.

## 8. Settled — don't reopen without a new reason
- Gemini reviews misread things: it claimed "8-bit square-wave jingles" three times (measured soft); Samarling is Wind
  (a "not very effective" was its wind move on Toastling); Galewick's fire Islings are a deliberate meadow (Fire breaks
  Galeowl's Gust Wall); a KO already leaves a wild Isling dazed for a sure catch. Always verify Gemini before acting.
- Kept on purpose: left-palm pause; red-zone chance catch (pod glows at 33% HP); passthrough via the floating island
  (no MRUK table snap); no music before a realm is restored; no ducking music under every SFX.
- Earlier: hold-to-grab reverted; boards face you once then stay world-locked; menus at the old tablet sizes.

## 9. Your first reply
Confirm in a few bullets what state you understand we're in (master, step 25 pending, the release blocker, the
timeline), then ask Mugesh what he's bringing (a run report, notes, a video, or "do the release next").
