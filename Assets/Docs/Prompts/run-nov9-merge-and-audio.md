# Run, Nov 9: checkpoint + merge + push, then step 23 Part 1 (audio balance), then merge + push again

Mugesh said **"push"** for this run: pushing `master` (and tags) to origin is approved here. You may be a fresh
session: orient first (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-21.md`, `Run-22.md`, git log).
He may be asleep: no questions, log decisions, flat bridge + tests + smoke run only.

## 1. Checkpoint the working tree (on `step-22`)
Look at every uncommitted change (`git status`, `git diff`) and sort it:
a. **Docs** (`Assets/Docs/**`: prompts incl. `step-17-img/`, Notes, Run reports, their `.meta`) → commit
   "Docs: prompts, notes and reports up to Nov 8".
b. **Unfinished code from the last session** (seen: `GhostHands.cs`, `KeeperHall.cs`, `SpiritTrail.cs`,
   `OnboardingScript.cs`, `Step8bSetup.cs`, `AudioCueTools.cs`, `Onboarding_Main.asset`, `restore.swell.asset`,
   `RealmCard_Lilymere_EmblemShine.asset`): read the diffs against the handoff and Mugesh's notes. If they compile and the
   EditMode tests + smoke run pass, commit them with a message that says what they do. If something is clearly
   half-done or breaks a test, `git stash push -m "nov8-unfinished" -- <those paths>` and describe it in the report.
c. **Step 15a work in progress** (`ReleaseBuild.cs`, `Step15aTests.cs`, `DeviceBuild.cs`, `ReleaseReadiness.cs`, the
   icon meta, `ProjectSettings/*`, `DevAgentSettings.asset`, the TMP fallback font asset): commit as
   "Step 15a (WIP): release build setup" **only if** it compiles, `Step15aTests` pass, and the Quest quality level is
   unchanged (4x MSAA, render scale 1; explain the big `QualitySettings.asset` diff in the report). Otherwise leave it
   uncommitted and report why.
d. **Never commit** a keystore (`*.keystore`, `*.jks`), a password, an API key, `Assets/_Recovery/`, `_lanes/`.
   Check `.gitignore` covers keystores; add the patterns if not.

## 2. Merge and push
- `master` fast-forwards to `step-22` (verified: master is an ancestor; 35+ commits): `git checkout master`,
  `git merge --ff-only step-22`. If not a fast-forward any more, stop and report (no merge commits, no force).
- Tag `nov8-checkpoint`. `git push origin master` and `git push origin nov8-checkpoint`. Never `--force`.

## 3. Step 23 (audio)
- Branch `step-23` from `master`. Do **Part 1 only (sections A–D)** of `Assets/Docs/Prompts/step-23-audio-mix.md`.
- **Do not do Part 2 (E–G, ElevenLabs) in this run, even if a key is set.** Mugesh runs it separately after he buys
  the plan. Leave a short "Part 2 ready to run" note at the top of the report.

## 4. Merge and push again
- When step 23 Part 1 is done, tests and the smoke run pass: `git checkout master`,
  `git merge --ff-only step-23`, `git push origin master`. If anything fails, don't merge; report.

## 5. Report
- `Assets/Docs/Run-23.md` + a `.claude-handoff.md` entry. At the **top**: what got committed/stashed in step 1, the
  pushed commit hash, Part 1 status, then Mugesh's headset A/B listening list in play
  order.
