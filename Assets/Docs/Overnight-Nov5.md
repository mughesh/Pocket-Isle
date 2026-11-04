# Overnight Nov 5: status (cut short: usage limit reached ~01:00)

## Done
- **Git:** your notes committed and **master pushed** (`2f2bdd5`). Everything below is on branch `overnight-nov5` (not pushed, not merged).
- **Smoke run:** `Tools > Pocket Isle > QA > Smoke Run` (`SmokeRun.cs`). It backs up both saves, turns XR init off, plays the game flat and restores both afterwards. Report in `Logs/SmokeRun/report.md`.
  - Run 1: 67 beats ok, 5 failed. **No game exceptions or errors.**
  - The failures were the run's own: retrying the statue trip, palm over each lily bud, the lily pads hook hitting an inactive copy, walking into the tower after the door opens. Fixed in the run.
  - **Run 2 not finished:** I stopped it early.
- **Readiness report:** `Tools > Pocket Isle > Release > Readiness Report` writes `Assets/Docs/Release-Readiness.md` and `Assets/Docs/Audio/Regen-List.md`.
  - **To do:** keystore not set; app icon not set; 237 free clips in 79 cues; every music cue still awaiting a clip.
  - The last QA APK asks for RECORD_AUDIO and BLUETOOTH. Check where they come from before submitting.
- **Written and compiling, not looked at in Play yet:**
  - Video mode and Clean hints toggles (dev panel, Tools tab).
  - Ambient life (`AmbientLife.cs`, `Critter.shader`).
  - Battle feel: element hits, a gold ring and 40 ms hit-stop on super effective, catch stars (`BattleStage.cs`).
  - Restoration wave front: dots and lifting leaves (`RealmRestoration.cs`).
  - All three have switches in `Data/Resources/PolishConfig.asset`.
  - Icon and key art maker: `IconMaker.cs`, Play mode, not run yet.

## Tests
- 314/318 passed. The 4 failures are BattleTests and DevCheatsTests. The cheat flags (invincible / one-hit KO) were still on after I stopped the smoke run mid-Play. They should pass after a domain reload (a recompile or restarting Unity); I didn't get to rerun them. **Rerun the tests first.**

## Left
- Finish smoke run 2: Galeowl, the wind shard, the end card, See the ending, the finale.
- Make the icon, then assign it.
- Polish screenshots (`nov5_*`) and the perf numbers.
- The headset test list.

## Commands
- Keep it: `git checkout master && git merge overnight-nov5 && git push`
- Throw it away: `git checkout master && git branch -D overnight-nov5`
