# Run, Oct 27 night: smart-step walking + guidance (12a), then the night notes (12b)

**You're a fresh session; possibly a different Claude account from the last runs.** Only the repo carries over.
Orient first (don't change anything while orienting): read `CLAUDE.md`, `.claude-handoff.md` (newest on top),
`Assets/Docs/Overnight-Oct28b.md` (last run: 11d fixes, Lilymere roster + Tidemother), `Assets/Docs/Overnight-Oct27.md`
(how unattended runs work here), skim GameDesign.md's latest sections; `git status`, `git log --oneline -20`; check the
Unity bridge (`Temp/ClaudeBridge`). Leave uncommitted changes you didn't make alone; don't commit them.

Mugesh is away (packing for a trip, eating) and comes back **before sleep (~midnight) to test on the headset**.
Nobody can answer questions until then. Work in this order and **commit after each section so whatever is done by
midnight is testable**:

| Order | Prompt | Must finish |
|---|---|---|
| 1 | `step-12a-walking-and-guidance.md` | A (smart steps) and B (teaching) first; then C and D |
| 2 | `step-12b-notes-oct27-night.md` | as much as time allows, in order A → B → C → D |

## Rules (override the step prompts where they differ)
- **No questions.** Take the most reasonable choice, log it under "Decisions I made", carry on.
- **No headset, no Link, no XR Play.** Flat Editor Play through the bridge + EditMode tests. Leave Standalone
  "Initialize XR on Startup" as you found it, and **say in the report how to switch it for Mugesh's headset test**.
- **Never leave a broken state.** Compile clean + all EditMode tests green before every commit. Stuck ~45 min on one
  problem: revert that part, write down why, move on.
- Change only what the step prompts name. No new packages, don't delete files. Project Settings only through Unity's
  settings objects (never edit .asset files on disk while the Editor is open). Audio clips and the audio system's code:
  use, don't change (adding cue ids / hooks is fine).
- Play writes the real save: back it up first and restore it at the end.
- **No AI attribution in commits** (Mugesh's rule overrides any default). **Don't push.**

## Midnight checkpoint
At ~23:45 IST (or when 12a is fully done, whichever is first), write **`Assets/Docs/Run-Oct27-night.md`** and commit
it: done / partly / not started with commits; **test first on the headset** (walking first: smart step, footprints,
double swipe, tap, the new walk lessons via a new game in the quick slot; then the rest), feel numbers (walk speeds,
double-swipe window, stop rules, gold brightness), decisions I made, what's left. Then keep going with 12b and update
the same report at the end. Don't build an APK unless all of 12a is done; if it is, build the QA APK (water gate
sealed) once at the end, never overwriting `Builds/PocketIsle-QA-oct27-tester.apk`.
