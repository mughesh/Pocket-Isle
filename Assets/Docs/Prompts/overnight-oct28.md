# Overnight run, night of Oct 27 → 28: Lilymere (11a → 11b → 11c)

Mugesh is away (working remotely, then asleep, then on a trip until Nov 3). **Nobody can answer questions or wear the
headset.** The audio branch has just been merged (step-audio-hookup); use `AudioService` for new sounds.
Work through the step prompts in order, the way each says, and leave a clear report. Read CLAUDE.md,
`.claude-handoff.md`, `Overnight-Oct27.md` (how last night's run went and its rules) first.

| Order | Prompt | Must finish |
|---|---|---|
| 1 | `step-11a-lilymere-land.md` | yes |
| 2 | `step-11b-lilymere-hands.md` | yes (scoop and pour is the hero: spend effort on it) |
| 3 | `step-11c-lilymere-roster-keeper.md` | if time (A roster, then B Tidemother) |

## First
0. If `Builds/PocketIsle-QA-oct27-tester.apk` doesn't exist, copy `Builds/PocketIsle-QA.apk` to that name before
   anything else (it's the tester's build; your build at the end must not be the only copy).

## Overnight rules (override the step prompts where they differ)
- **No questions.** Take the most reasonable choice, log it under "Decisions I made", carry on. Never wait.
- **No headset, no Link, no XR Play.** Flat Editor Play through the bridge (`Temp/ClaudeBridge`, Standalone XR init
  off for the run, put back as it was at the end and say so) and EditMode tests. List every feel number.
- **Never leave a broken state.** Compile clean + all EditMode tests green before each commit. Stuck ~45 min on one
  problem: revert that part to the last good commit, write down what blocked it, move on.
- **Protect what works:** don't change the hub, the opening, onboarding, Cinderwood, Galewick, title/menu, audio
  files or the audio system's code (you may *use* AudioService and add profiles/states/cue ids). Shared code that must
  change (e.g. the keeper flow): only additive, defaults unchanged, and re-run Cinderwood's and Galewick's flat flows
  + tests after. No new packages, no ProjectSettings changes, don't delete files.
- **No AI attribution in commits** (Mugesh's rule overrides any default). Don't push.
- If everything finishes early: full EditMode suite, a flat bridge walk hub → well gate → Lilymere → temple →
  restored, fix what's obviously broken. **No new features.**

## At the end
1. **Build the QA APK** with `LilymereConfig.open` = false (water gate sealed). Don't install. Note path and size.
   This build is for Mugesh after his trip, not the tester (the tester already has the evening build).
2. Write and commit **`Assets/Docs/Overnight-Oct28.md`**, same sections as `Overnight-Oct27.md`: done / partly / not
   started with commits; test first (with dev panel paths); feel numbers to tune; decisions I made; blocked or
   reverted; shots; perf; APK path and size.
3. Update `.claude-handoff.md`.
