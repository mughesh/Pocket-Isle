# Step 28: lesson walking (the first swipes must have room to learn the tap)

Decided Nov 10, 1:30 AM (design chat + Mugesh). Mugesh watched first-time players: at the start the explorer stands
where the road is short, so a swipe stops by itself after ~3–4 tiles. That's far too little time for a first-timer to
notice and try the tap. The map shows why: start (15,22) sits in the 3-wide stub below the statue; swiping north hits
the statue (15,17) after 4 tiles; east/west ends after 1; south hits the T of row 23 after 1.

**Decision:** during the walk lessons and the step 27 tap practice only, the explorer **never stops by itself**: it
keeps walking until the tap, at a slower pace, and nothing it bumps into does anything. It still never leaves the paths
(walking onto grass would trigger the very things we're protecting against).

## How to run
- Orient (`CLAUDE.md`, `.claude-handoff.md`, `Run-27.md`, git log). Branch **`step-28`** from master. Usual rules (stage
  by path; no keys/keystores/`_Recovery`/`_lanes`; commit per section "(headset test pending)"; no AI attribution;
  flat bridge + tests + smoke run + Release Check; revert after ~45 min stuck). **Don't push**; fast-forward master
  locally if all pass. **Don't touch the release setup.**
- Report `Assets/Docs/Run-28.md`: a top-down shot of the Still Isle with the lesson route, the practice targets and the
  lesson zone drawn on it; old → new values; the headset test list.

## A. Lesson walking mode
1. A mode on the explorer's walk (`Onboarding` turns it on from the first swipe lesson to the end of the tap practice,
   then off for good; saved so a reload mid-tutorial resumes it):
   - **Speed ~65%** of normal (tune so a swipe-to-tap round takes a relaxed 2–4 s).
   - **No self-stops:** at a T-end or junction it turns onto the longer road (the one with the most tiles ahead); at a
     wall, a dead end or a blocked tile (the statue, a gate, the lodge, the shrine) it turns around and keeps going.
     Only a **thumb tap** stops it (and the next lesson step).
   - **Free to roam the whole Still Isle** on every tile the explorer can normally walk (Mugesh: it shows the player
     the explorer can go anywhere, and the window scrolling with it is the game's core idea). No lesson zone.
2. The step 27 tap-practice targets are placed **dynamically, ahead on the road the explorer is on**, with at least ~4
   tiles of run-up and never right after a turn (prefer long straight legs like row 23, 16 tiles). If the explorer
   turns away, the target moves ahead again.
2b. When the practice ends wherever the explorer is, the grass lesson's gold trail leads to the first grass as usual
   (check it works from any corner of the island).

## B. Everything else is inert until the lessons end
3. While lesson walking is on, nothing on the island reacts to the explorer: **tall grass (no encounters, no
   rustle-trigger), the wind gate/portal, the lodge door, the hearth shrine, the pond, the statue, gold trails to later
   objectives.** Bumping one just turns the explorer (a soft bump sound, no line). Check what `Onboarding`
   (`EncountersEnabled`) already blocks and close every other gap; list them all in the report.
4. Look + pinch on those things is ignored too (no walk-up), and their gaze hover (step 27 F) doesn't show, so nothing
   invites a detour.
5. When the practice ends: normal speed and stopping rules, everything live again, and the grass lesson takes over as
   now (the first battle). The Quick Journey uses the same mode.

## C. Check
6. Bridge sim: from the start tile, swipe in each of the four directions with no tap for 20 s: the explorer keeps
   moving the whole time, never triggers grass, a door, the gate or the lodge. Then end the practice in each corner of
   the island and check the gold trail reaches the first grass. Then the full tutorial flow (smoke run).
