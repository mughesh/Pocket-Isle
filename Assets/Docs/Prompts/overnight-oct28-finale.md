# Overnight run, Oct 28 (from 2 AM): the finale (step 13)

**You're a fresh session; possibly a different Claude account from the last runs.** Only the repo carries over.
Orient first (don't change anything while orienting): read `CLAUDE.md`, `.claude-handoff.md` (newest on top),
`Assets/Docs/Run-Oct27-night.md` (the last run's report incl. step 12c), `Assets/Docs/Overnight-Oct27.md` (how
unattended runs work here), skim GameDesign.md; `git status`, `git log --oneline -20`; check the Unity bridge. Leave
uncommitted changes you didn't make alone; don't commit them.

Mugesh is asleep and then **away on a trip until Nov 3**. Nobody can answer questions or wear the headset. Do
**`step-13-finale.md`**, sections in order A → B → C → D. **B (the Heart's model) deserves real time**: it's the most
important model in the game and the centre of the video; do the critique rounds properly. If time runs short, finish
C before D (D can wait for Nov 3).

## Rules (override the step prompt where they differ)
- **No questions.** Take the most reasonable choice, log it under "Decisions I made", carry on.
- **No headset, no Link, no XR Play.** Flat Editor Play through the bridge + EditMode tests. Leave Standalone
  "Initialize XR on Startup" as you found it; say in the report how to switch it for the headset test.
- **Never leave a broken state.** Compile clean + all EditMode tests green before every commit. Stuck ~45 min on one
  problem: revert that part, write down why, move on.
- Change only what the step names. Don't touch walking, onboarding, the realms' hand moments, title/menu, audio clips
  or the audio system's code (use AudioService, add cue ids). No new packages, don't delete files. Project Settings
  only through Unity's settings objects.
- Play writes the real save: back it up first and restore it at the end.
- **No AI attribution in commits** (Mugesh's rule overrides any default). **Don't push.**
- Finished early: full EditMode suite; flat walk through the whole finale twice (once from each dev panel entry); fix
  what's obviously broken. No new features.

## At the end
1. Build the QA APK (water gate sealed as now), don't install, never overwrite `Builds/PocketIsle-QA-oct27-tester.apk`.
2. Write and commit **`Assets/Docs/Overnight-Oct28-finale.md`**: done / partly / not started with commits; **test
   first** on Nov 3 (how to jump straight to the finale from the dev panel); the Heart's critique rounds (what changed,
   shots); feel numbers (palm handoff distance, timings); decisions I made; blocked or reverted; perf at the finale's
   peak; APK path and size.
3. Update `.claude-handoff.md` (newest on top).
