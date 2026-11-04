# Overnight run, Nov 5 (from ~00:30 IST): safety net first, then checks, then polish

**You may be a fresh session.** Orient first, changing nothing: `CLAUDE.md`, `.claude-handoff.md` (newest on top),
`Run-14k.md`, `Assets/Docs/submission-checklist` notes in the handoff if any, `git status`, `git log --oneline -20`,
the Unity bridge. Mugesh is asleep until morning: **no questions**; take the reasonable choice and log it under
"Decisions I made". **No headset, no Link, no XR Play:** flat Editor Play through the bridge + EditMode/PlayMode tests.

## 0. Git (Mugesh asked for this exact order)
1. Commit any of **your** uncommitted work only if it belongs to step 14k (check `git status`; Mugesh's own edits to
   `Notes.md`, `Notes copy.md`, `step-14h-evolution.md` and the TMP fallback asset: commit `Notes.md` and the prompt
   as "Docs: Mugesh's notes" if they're just text, leave the TMP asset and `Notes copy.md` alone).
2. **Push master now** (Mugesh said "push" for this). This is the known-good state.
3. Create and switch to a branch **`overnight-nov5`**. Everything below is committed there. **Do not push the branch
   and do not merge into master**: Mugesh tests in the morning and merges + pushes himself.

## 1. Automated playthrough + bug sweep (most important)
4. Build a **flat smoke run** (a bridge command or PlayMode tests, rerunnable later as `Tools > Pocket Isle > QA >
   Smoke Run`) that drives the game with the dev tools and synthetic input through: new game (opening, every lesson),
   first battle + catch, shrine + lodge (poke, swap), the Quick Journey end to end (gift → Galewick → every moment →
   Galeowl → shard → statue → end card → See the ending), Cinderwood and Lilymere moments + Keepers via dev presets,
   an evolution in the palm (synthetic palm), the Journal, the finale, save/quit/continue, pause menu/settings.
   Log every exception, error, warning spam, stuck state (no progress for 20 s), missing cue id, line shown out of
   context, overlapping tablets, label inside a mesh.
5. **Fix what it finds** (real bugs only, with the log line as evidence; small, local fixes). Anything risky or
   design-level: don't change it, list it. Re-run until clean or only listed items remain.

## 2. Release readiness checker (no signing, no upload tonight)
6. `Tools > Pocket Isle > Release > Readiness Report`: package name, version name/code, min/target SDK, ARM64 + IL2CPP,
   `POCKET_DEV` off in the release profile, keystore configured (expected: **not yet**), manifest permissions and
   hand-tracking features, app name, **app icon present**, splash, build size estimate, the `_FREE` clips list
   **grouped by cue with the prompt each was made from** (so tomorrow's paid regeneration is quick: write
   `Assets/Docs/Audio/Regen-List.md` with cue, kept variants, prompt, duration), music files and their source.
   Don't change project settings; just report.
7. **App icon** (if missing): render the logo (island + letters) on a soft sky-teal gradient, square, to the sizes the
   Android/Quest build needs, assign it **only if** that's a Player Settings icon change Unity allows without touching
   other settings; else save the PNGs under `Assets/_Project/Art/Icon/` and list the step for Mugesh. Also export a
   **1920x1080 key art** (logo over a nice island shot) for the Devpost thumbnail to `Builds/Marketing/`.

## 3. Visual polish (additive, toggleable, perf-budgeted)
Only after 1 is clean. Each item: a config toggle, ≤ 2 extra draws, ≤ 0.2 ms, no change to gameplay or input.
8. **Ambient life per area** (was on the cut list; now there's time), restored state stronger than faded:
   hub butterflies (2–3, drift between flowers), Cinderwood fireflies + rising embers when restored, Galewick drifting
   dandelion seeds + a bird crossing now and then when restored, Lilymere dragonflies + water sparkles when restored.
   Island-local, inside the window, pooled.
9. **Battle feel:** element impact bursts readable (fire sparks, water splash rings, wind swirl, neutral dust),
   "Super effective!" with a gold flash ring and a short camera-free "impact pause" (40 ms hit-stop on the stage
   tweens only), the catch success: pod clicks shut, three gold stars pop, then it flies to the rim.
10. **Restoration waves:** add a soft colour ring sweeping across the island ahead of the wave + leaves/petals/sparkles
    lifting as it passes (per realm palette).
11. Shots before/after for each in `Logs/BattleShots/nov5_*`.

## 4. Video helper (QA builds only)
12. Dev panel Tools: **"Video mode"** toggle: hides debug readouts/labels, dev-only visuals and the FPS counter, keeps
    the game UI; and **"Clean hints"** toggle that suppresses idle hint chatter (objective chips stay). Nothing
    else changes.

## Rules
- Never leave a broken state: compile clean + all tests green before each commit on the branch. Stuck ~45 min on one
  problem: revert that part, write why, move on.
- Don't change: walking, island grab, lessons, the opening, Keeper balance, audio clips/system code (use, don't change),
  Project Settings (except the icon if allowed above), packages. Don't delete files. Play writes the real save: back it
  up first, restore it at the end. No AI attribution in commits.

## Morning report
Write and commit `Assets/Docs/Overnight-Nov5.md` (on the branch): what the smoke run covers and how to run it; bugs
found / fixed (with commits) / left; the readiness report summary; the visual items with shots and their toggles; perf
numbers; **headset test list in order** (most at-risk first); and the two commands Mugesh needs:
`git checkout master && git merge overnight-nov5 && git push` (after his test) or
`git checkout master && git branch -D overnight-nov5` (to throw it away).
