# Run, Nov 9 night: step 24 A–D, then the paid ElevenLabs work, then merge + push

Mugesh is asleep: no questions, log decisions, flat bridge + tests + smoke run only. Mugesh said **"push"** for this
run (fast-forward `master` and push; never force). Orient first (`CLAUDE.md`, `.claude-handoff.md` newest on top,
`Run-23.md`, git log). Same rules as every run: stage by path; never commit keystores, passwords, API keys,
`Assets/_Recovery/`, `_lanes/`; commit per section "(headset test pending)"; no AI attribution; revert after ~45 min
stuck; never leave the project broken.

1. **Step 24 A–D** (`Assets/Docs/Prompts/step-24-nov9-notes.md`) on branch `step-24` from master. Tests + smoke run.
2. **Check the paid plan:** `ELEVENLABS_API_KEY` set, the API reports a paid tier (Starter), write access to
   `D:/UNITY/projects/_audio-sources`. If any is missing: skip 3, go to 4, and say so at the top of the report.
3. **The ElevenLabs work, in the priority order of `step-23-audio-mix.md` E12** (15b regen → footsteps + beds →
   one-shots → step 24 E voices → acoustic jingles), on the same branch. Measure the real cost per call; keep ~10% of
   the quota in reserve and stop there, importing what's done.
4. Tests + smoke run + Release Check. If all pass: `git checkout master`, `git merge --ff-only step-24`,
   `git push origin master`. If anything fails: don't merge, report.
5. Report: `Assets/Docs/Run-24.md` (A–D shots, what ran, quota used and the measured cost per call, what's left
   undone if the quota ran out, what failed), `Regen-Report.md`, and
   at the top **one listening list in play order** with the Dev panel A/B switches (jingles, voices, footsteps…).
