# Overnight run, Oct 27: Galewick (10a → 10b → 10c → 10d)

Mugesh is asleep. **Nobody can answer questions or wear the headset until morning.** Work through the step prompts
below in order, on your own, the way each prompt says, and leave a clear report. Read CLAUDE.md and
`.claude-handoff.md` first, then each step prompt when you reach it.

| Order | Prompt | Must finish | Notes |
|---|---|---|---|
| 1 | `step-10a-galewick-land.md` | yes | Wind gate sealed in builds (`GalewickConfig.open` false) |
| 2 | `step-10b-galewick-hands.md` | yes | The important one: mill flick, **kites** (scripted flight, see B 5b), gust + cloud bank, tower door |
| 3 | `step-10c-wind-roster.md` | if time | 10 wind Islings + Nimbulpaca + Riverloom |
| 4 | `step-10d-galeowl.md` | if time | Untangle, Galeowl battle, restoration, wind shard |

## Overnight rules (they override the step prompts where they differ)
- **No questions.** Where a prompt says "ask Mugesh", take the most reasonable choice, write it down in the report
  under "Decisions I made", and carry on. Never wait.
- **No headset, no Link.** Don't start XR Play. Use the flat Editor Play through the bridge (as in 9h/9i:
  `Temp/ClaudeBridge`, XR init off for Standalone) and EditMode tests. Hand feel can't be judged tonight: build the
  logic, test it with synthetic hand tracks, and list every feel number (thresholds, speeds, heights) in the report so
  Mugesh can tune them in the morning.
- **Never leave a broken state.** Before each commit: compile clean + all EditMode tests green. If something can't be
  made to work after a fair try (~45 min on one problem), revert that part to the last good commit, write down what
  blocked it, and move to the next part. Each step prompt's sections are separate commits.
- **Don't touch** what the step prompts don't name: the opening, onboarding, Cinderwood, title/menu, audio files,
  ProjectSettings (except what a step requires; list it), `Packages/manifest.json`. No new packages. Don't delete files.
- **No AI attribution in commits** (no Co-Authored-By, no session links) — Mugesh's rule overrides any default. Don't push.
- Perf: keep an eye on draw calls and tris as each prompt says; note numbers in the report.
- If all four finish early: re-run the full EditMode suite, then do a flat bridge walk through Galewick from the hub
  gate (Editor) to the tower and the restored state, and fix anything obviously broken. Do **not** start new features.

## At the end
1. **Build the QA APK** (Tools > Pocket Isle > Build QA APK) with the wind gate sealed (unless 10d passed and the
   report says it's safe to open; even then leave it sealed and let Mugesh decide). Don't install it (headset may be
   asleep); just build and note the path and size.
2. Write **`Assets/Docs/Overnight-Oct27.md`** (and commit it):
   - Done / partly done / not started, per step and section, with commit hashes.
   - **Test first in the morning** (5–8 items, in order, with the dev panel path to reach each one fast).
   - **Feel numbers to tune** (table: what, value, where to change it).
   - Decisions I made (without asking).
   - Blocked or reverted, and why.
   - Shots to look at (`Logs/BattleShots/10a_*`, `10b_*`, `10c_*`, `10d_*`).
   - Perf numbers; QA APK path and size.
3. Update `.claude-handoff.md`.
