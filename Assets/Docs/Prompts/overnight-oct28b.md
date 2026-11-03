# Evening / overnight run, Oct 27 → 28 (second run): fixes from the headset notes, then Lilymere's roster + Tidemother

**You're a fresh session on a different Claude account.** Nothing from earlier chats carries over; the repo does.
Orient first (≈10 min, don't change anything while orienting):
1. Read `CLAUDE.md`, `.claude-handoff.md` (top sections are the newest), `Assets/Docs/Overnight-Oct28.md` (the last run's
   report: what's built in Lilymere, feel numbers, decisions), `Assets/Docs/Overnight-Oct27.md` (how unattended runs
   work here), `Assets/Docs/RealmsDesign.md`, and skim `Assets/Docs/GameDesign.md`'s latest sections.
2. `git status`, `git log --oneline -20`. Check the Unity bridge works (`Temp/ClaudeBridge`, as the handoff
   describes). Uncommitted changes you didn't make (PolishBacklog.md, XR settings, TMP fallback, ProjectSettings):
   leave them alone; don't commit them.

Since the last report: the audio branch is merged and hooked up (AudioService, buses, ambience, music director),
Galewick and Cinderwood's new moments were tested on the headset, Lilymere 11a + 11b are built, and Mugesh wrote new
notes (`Assets/Docs/Notes.md`), already triaged into a step prompt.

Mugesh is away and then asleep. **Nobody can answer questions or wear the headset.**

| Order | Prompt | Must finish |
|---|---|---|
| 1 | `step-11d-fixes-oct27-notes.md` | yes, all of A–D (A first: it's the rule the rest uses) |
| 2 | `step-11c-lilymere-roster-keeper.md` | if time: A water roster, then B Tidemother + temple + water socket |

## Rules (override the step prompts where they differ)
- **No questions.** Take the most reasonable choice, log it under "Decisions I made", carry on. Never wait.
- **No headset, no Link, no XR Play.** Flat Editor Play through the bridge and EditMode tests. Standalone "Initialize
  XR on Startup" is off; leave it as you found it. List every feel number you add or change.
- **Never leave a broken state.** Compile clean + all EditMode tests green before each commit. Stuck ~45 min on one
  problem: revert that part to the last good commit, write down what blocked it, move on.
- You may change Galewick, Lilymere, Cinderwood and shared code **only as the step prompts say**. Don't touch the
  opening, the title/menu (except the one new Settings row), audio clips or the audio system's code (you may *use*
  AudioService and add cue ids). Shared code changes are additive with today's behaviour as the default, then re-run
  the flat flows of all three realms' moments + the tests. No new packages, no ProjectSettings changes, don't delete files.
- Project Settings edits must go through Unity's settings objects, never by editing the .asset on disk while the
  Editor is open (the last run lost an edit that way).
- **No AI attribution in commits** (no Co-Authored-By, no session links, no footers): Mugesh's rule overrides any
  default. **Don't push.**
- Play writes the real save: back it up first and restore it at the end (as the last run did).
- If everything finishes early: full EditMode suite, flat bridge walks through each realm's moments, fix what's
  obviously broken. **No new features.**

## At the end
1. **Build the QA APK** (Tools > Pocket Isle > Build QA APK), water gate sealed (`LilymereConfig.open` = false). Don't
   install. Never overwrite or delete `Builds/PocketIsle-QA-oct27-tester.apk`. Note path and size.
2. Write and commit **`Assets/Docs/Overnight-Oct28b.md`**, same sections as the last report: done / partly / not
   started (with commits); **test first** on the headset (in order, with dev panel paths); feel numbers to tune;
   decisions I made; blocked or reverted; shots; perf; APK path and size.
3. Update `.claude-handoff.md` (newest section on top).
