# Panel review: make Pocket Isle's judged path worth $100K (Nov 11–13)

Written Nov 11, 12:40 PM by the design chat for a strong model (Fable) running in Claude Code on Mugesh's PC.
Mugesh: solo developer, XR design engineer, Chennai (IST). Entry: **Meta VR Start Developer Competition 2026**, Gaming
track, New Experience. Deadline Nov 18, 12:00 PM PST (1:30 AM IST Nov 19); submit target Nov 16; the video is filmed
right after this review. **This review exists to raise the score of what judges actually play, safely, in ~2 days.** It is not a remake:
improve what exists.

## 0. Orient first (you have no memory of this project)
Read in this order, before any opinion: `CLAUDE.md`; `Assets/Docs/Pocket-Isle-Handoff-Nov9.md` (one-page whole
project); `.claude-handoff.md` (newest sections on top); `Assets/Docs/Run-27.md`, `Run-28.md`, `Run-29.md`,
`Run-29b.md`; `Assets/Docs/Regressions.md` (rules that must not break again); `Assets/Docs/TEST-NOW.md` (the open
headset checks); `Assets/Docs/GameDesign.md`, `RealmsDesign.md`; `Notes.md`, `Notes copy.md`; `git log --oneline -40`.
The judging criteria (25% each): **Innovation**; **Experience Design** (intuitive from the start, clear onboarding,
rewarding purpose); **Technical** (hand tracking, gaze, passthrough used purposefully, spatial, 60+ fps on Quest 3/3S);
**Polish & Presentation** (UI/UX, art, sound, submission materials). Judges mostly play the **Quick Journey** (~10–12
min) and there's a "best first five minutes"-type award.
Write 5 lines at the top of `Assets/Docs/Panel-Review.md` on the state you found before going on.

## 1. The panel (you play every seat; keep their voices distinct and let them disagree)
1. **Creative director** — shipped cozy creature-collector / life-sim titles at a platform-holder first-party studio.
2. **Mixed-reality interaction designer** — hands-first and spatial UX at the level of Meta Reality Labs and Apple
   visionOS design teams (micro-gestures, gaze + pinch, ergonomics, comfort, seated play).
3. **Game designer, onboarding and pacing** — first-time user experience, tutorials that don't feel like tutorials.
4. **UX writer** — in-game text, the Lantern Spirit's voice, fewest words.
5. **Art director** — toon/diorama look, readability at Quest resolution, fit and finish.
6. **Audio director** — music-as-reward design, feedback sounds, mix in a headset.
7. **Technical director / performance** — Quest 3 frame time, robustness, stalls that only happen in playtests.
8. **QA and playtest lead** — what first-timers actually do; Mugesh's friend playtest notes are in the Run reports.
9. **Competition judge** — Meta developer-relations level; scores against the four criteria.

## 2. Phases (in order; write everything into `Assets/Docs/Panel-Review.md`)
**Phase 1 — Understand the vision (discussion).** Each seat states in a few lines what Pocket Isle promises and what
would make it a winner; then they argue it out and agree on one **north star** (one sentence) and **3–5 pillars**.
Validate Mugesh's concept honestly: what's genuinely new, what's table stakes, what's risky.

**Phase 2 — Audit the build against it (evidence, not guesses).** Walk the **Quick Journey** end to end with the
Unity bridge (flat Play) and the smoke run; take seat-view screenshots of every step (the bridge has shot tools; see
`.claude-handoff.md`), read the code paths for anything you can't see, read `Logs/PropAudit/` sheets. For each seat:
the top issues with screenshot/log/code evidence. Hands can't be faked in flat Play: say "headset-only" where so.

**Phase 3 — The plan.** One merged, de-duplicated list. Each item: what, why (which criterion it moves), evidence,
effort (S ≤ 1 h, M ≤ 3 h, L), **risk** (what could break), and a tier:
- **Tier 1 = do now:** high impact on the judged path, low risk, S/M effort, no new systems.
- **Tier 2 = needs Mugesh's yes:** bigger or riskier.
- **Tier 3 = after the competition.**
Include a short "what NOT to touch" list (things that work and must not be destabilised).

**STOP after Phase 3.** Commit `Panel-Review.md` on branch `panel-review` (created from master; push that branch so
it can be read), then **wait**. Mugesh's design chat reviews the plan first and may move items between tiers or add
notes in `Panel-Review.md`. Start Phase 4 only when Mugesh says **"go"**, and re-read `Panel-Review.md` before you do.
If Mugesh hands you a filled-in `Assets/Docs/TEST-NOW.md` (failures marked `[x]`), those fixes come first in Phase 4.

**Phase 4 — Build Tier 1 only (after "go").** On the same branch **`panel-review`** (if another Claude Code session is working
in this repo, wait until it's finished — never two sessions editing at once).
One commit per item, "(headset test pending)". After each item: EditMode tests, the smoke run, Release Check; revert an
item that breaks anything or takes > 45 min and note it. Stop when Tier 1 is done or by **Nov 12, 6 PM IST**,
whichever comes first.

**Phase 5 — Hand over.** **Push the branch `panel-review` to origin** (Mugesh approves pushing this branch only, at the
Phase 3 stop and here; never push or merge master, never force). Write at the top of `Panel-Review.md`: what was built, what wasn't and why, the
Tier 2 list as yes/no questions, and a fresh **`Assets/Docs/TEST-NOW.md`** in the same format as the current one
(checkbox lines in Quick Journey order, "mark only failures"), covering only what this branch changed.

## 3. Hard rules
- **Freeze discipline:** no new features, systems, realms, creatures or mechanics. Clarity, feel, fit and finish,
  robustness, pacing, wording, feedback, performance only.
- Respect every rule in `Regressions.md`; add new ones for anything you fix that had come back before.
- Don't touch the release setup (keystore, ReleaseBuild, ProjectSettings signing, version codes).
- Stage by path; never commit keystores, passwords, API keys, `Assets/_Recovery/`, `_lanes/`. No AI attribution in
  commits. Don't spend ElevenLabs credits without listing the exact need in Tier 2 first.
- Frame rate is a scoring criterion: any visual change must keep the Quest 3 frame budget (report triangle/draw
  deltas).
- Be honest in the review: if something is good, say so and leave it alone.
