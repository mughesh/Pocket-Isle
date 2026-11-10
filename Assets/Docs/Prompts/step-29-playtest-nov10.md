# Step 29: fixes from the Nov 10 friend playtests (two sessions) + Mugesh's follow-up

Triaged Nov 10, 9:45 PM (design chat). First-time players struggled with walking, battle turns, healing before a
boss, a few hand moments and the wording. **Clarity and robustness fixes, done properly.** Mugesh noticed that some
earlier asks were silently skipped in past runs: **this run must not skip anything** (see "Done means" at the end).
Order = priority; if time runs out, stop at a section boundary and report honestly. Steps 27–28 are in (`0148c13`,
master, not pushed).

## How to run (unattended, ~3 h, Mugesh is out)
- Orient (`CLAUDE.md`, `.claude-handoff.md` newest on top incl. the "Nov 10 playtest" commit, `Run-28.md`,
  `Assets/Docs/Regressions.md`, git log). Branch **`step-29`** from master. Usual rules: stage by path; no
  keys/keystores/`_Recovery`/`_lanes`; commit per section "(headset test pending)"; no AI attribution; no questions;
  flat bridge + tests + smoke run + Release Check after each section; revert a section after ~45 min stuck and say so.
  **Don't push.** If all pass, fast-forward master locally. **Don't touch the release setup.**
- Report `Assets/Docs/Run-29.md`: per item done/partly/not **with evidence**, old → new values, top-down map shots for
  A0 (before/after per realm), seat-view shots for A, B, C, D, and the headset test list **in Quick Journey play order**.

## A0. No one-tile paths anywhere (all realms) — do this first, carefully
Mugesh's call: every walkable path must be **at least 2 tiles wide**. Turns on 1-tile paths are too sharp to swipe.
1. In every realm map (Still Isle, Galewick, Cinderwood, Lilymere, and the hall maps if they have paths), widen each
   1-tile-wide path segment to 2 by turning the neighbouring tile on **one consistent side** into path. Prefer plain
   grass `G` neighbours; never overwrite a prop's footprint, water, a door's front tiles, a gate, a puzzle tile or a
   landmark; if both sides are blocked, report the spot instead of forcing it.
2. **Exceptions (keep as designed, list them):** bridges and drawbridges, lily-pad and lotus stepping stones, the
   frozen-falls run and any puzzle path whose width is the puzzle. Everything else gets widened.
3. **Don't break the path logic:** keep step 14k's "straight legs ≥ 3 tiles", the walk-up rule (door/shrine/hand-moment
   triggers next to footprints), gold trails, `ExitDoorGuide`, the tall-grass zones and encounter tiles, Spirit
   targets, saved positions (an explorer saved on a changed tile must still be on a walkable tile). Re-run every map
   test and the smoke run; walk every realm's main route with the bridge.
4. A top-down before/after shot per realm in the report, with every widened segment and every exception marked.

## A. Walking: stop where a choice or an action is
Players couldn't stop at corners and junctions and got lost. The swipe walk currently runs **straight past side
roads** (`SmartStep.Plan(..., throughBranches: true)` since Nov 6).
5. After the lessons (step 28's lesson mode unchanged), a swipe walks along the path, **follows simple bends by
   itself**, and **stops by itself**: at every **real junction** (a choice; the next swipe picks the branch), **one
   tile before any interactable or hand moment** (doors, gates, shrines, the lodge, the heal bloom, puzzle props, Keeper
   halls), at the tall-grass edge the first time only, at blocked tiles. A tap still stops it anywhere.
6. **Junctions on 2-wide paths:** with A0, a 2-wide corridor has a path tile beside the explorer on every step, so the
   current per-tile `Branch()` test would call every tile a junction. Define junctions **per corridor**, not per tile
   (e.g. treat the path mask's lanes/skeleton: a junction is where the corridor splits into separate routes), and keep
   the explorer in its lane on a 2-wide path. Unit tests: a 2-wide straight, a 2-wide L-bend, a 2-wide T, a 3-wide
   plaza, a crossroads.
7. At a junction stop, small **white** chevrons on the open directions (fade after ~2 s or on the next swipe).
8. **Two colours:** gold = "where to go next" (objective trail, gold marks) only. The explorer's walk preview/direction
   icon and the junction chevrons become **white/soft cyan chevrons**, never gold.
9. **First grass:** one golden path only; the white tile path into the first grass goes (or becomes the gold trail).

## B. Battle: it's obvious whose turn it is
Players kept pressing moves during the wild Isling's turn.
10. Off-turn, the move tablets **sink and grey out** (desaturated, lower, no hover, no press sound, no gaze icon), and
    the caption shows "Wild Vanehare's turn…". On the player's turn they rise with one soft gold rim pulse; caption
    **"Choose your attack"**.
11. A press off-turn does nothing but a tiny shake; never queues a move.

## C. Health: always visible, easy to fix
12. **HP rings on every rim pod, always** (not only in battle): green → yellow → red; fainted = grey.
    **Low HP (≤ 25%) pulses red** (a slow blink + a small red "!" badge) so it can't be missed; fainted shows a grey
    ring with a small "zz".
13. **Poking (or look + pinch) a hurt pod** shows its card with the HP meter **and** a line: "Tired! Heal at the lodge
    or a healing bloom." (Fainted: "Fainted. Heal at the lodge or a healing bloom.")
14. **Healing bloom (not a building):** Mugesh doesn't want a lodge or shrine crammed outside the tower. Instead a small
    **one-tile healing plant of the realm** beside the main path **in all three realms**, placed **a little before the
    temple door** (on the approach, a few tiles back, not right at the door), on a tile that isn't in the way:
    Galewick a glowing dandelion-puff flower, Cinderwood a glowing ember-lily, Lilymere a glowing lotus bud. (Only
    Cinderwood's lodge sits on the path; in Galewick and Lilymere the lodge is out of reach from the path, so the
    bloom is the dependable heal before every Keeper.) Walking
    the explorer onto it or poking it: **a burst of green "+" rising around the explorer**, every party Isling fully
    healed, the rim rings refill with a sweep, a soft heal chime, the Spirit line **"Your Islings are healed!"**; the
    bloom closes and reopens after ~60 s. Small SDF prop, flat, matches the realm's art; check it isn't hidden in grass.
15. **Healing in the lodge must be felt:** on entering the lodge, if any Isling was hurt: **green "+" symbols flow up
    out of the lodge floor** (reuse the step 18 heal "+" effect, bigger and longer, ~2 s), the rim rings refill one
    by one with a soft chime, and the Spirit says **"Your Islings are healed!"**. If nobody was hurt: no effect, no line.
16. Before entering a Keeper hall with any Isling under ~50% or fainted: the Spirit says once **"Your Islings are
    tired. Heal them first."** and the healing bloom pulses gold; the door still works (never a wall). Keep the
    team-size warning, worded per F: "Bring at least 3 Islings."

## D. Hand moments that stalled
17. **Ember → burning fingertip (Cinderwood).** The palm carry got stuck once, never reached the forge, and movement
    stayed disabled. New: **touching the ember sets the index fingertip on fire**, and **poking the firebox with that
    finger lights it**. Same for the Cinderwood bush/brush clearing if it used the palm ember.
    - **The fire must look really good:** a layered toon flame on the fingertip (bright core + flickering outer tongues,
      a few rising embers, a warm glow on the finger and hand), lively and clearly burning, sized to read from the seat;
      a fire crackle loop that swells a little when the hand moves and a soft "whoomp" when it catches; a hiss/flare
      when it lights the firebox. Keep it cheap (72 fps).
    - Never stuck: if nothing is lit within ~20 s, the flame floats back to the ember. **Audit every lock/unlock pair**
      of the moment and log `[HandPuzzle] lock/unlock <owner>`; the explorer is unlocked on every exit path.
18. **Log lift:** releasing the log anywhere over the island **auto-drops it into its target spot** (a short arc).
19. **Bellows:** the ghost hand is missing: show the open/close ghost and a chip "Open and close your hand" from the
    start; it fades when the hand is near.
20. **Galewick bridge:** the "face the water" fishing line fires on the bridge and fishing doesn't work there. Never on
    bridges; only on a real fishable bank tile facing water. Mark 2 fishable bank spots near the Galewick river with
    soft ripples so players can catch more before the tower.

## E. Starter pods and variety
21. **Starter pick:** the three starters **face the player** (no spinning; a small idle hop). Gazing one shows a stat
    card: name, element colour and symbol, "Fire · strong vs Wind, weak vs Water", its signature move. Reuse the
    Journal/pod card look with element colours.
22. **Encounter variety:** species the player already has get **x0.3 weight** in grass and water tables while an
    uncaught species of that table exists; rare stays rare. Report before/after odds for Galewick's zones.

## F. Fewer, plainer words (a copy pass on every instruction line)
"Poke a rune to attack" confused players. Rewrite every objective line, chip and hint (`Script_v1` / dialogue assets /
code strings); keep story lines' voice, just shorter:
- **One action per line, verb first; ≤ ~6 words for chips/objectives, ≤ ~12 for Spirit lines.**
- Fixed verbs only: **Look, Pinch, Poke, Grab, Swipe, Tap, Hold, Turn your palm**. No "rune", "tile", "socket",
  "interactable": name the thing ("the door", "the pod", "the button").
- The goal, not the mechanism: "Catch it! Flick the pod." not "Throw the capture pod at the wild Isling".
- Examples: "Choose your attack" · "Flick the pod at it" · "Swipe to walk · Tap to stop" · "Walk into the tower" ·
  "Heal at the lodge" · "Bring at least 3 Islings" · "Touch the ember… now poke the fire".
- A before → after table of every changed line in the report.

## G. Text never in your face
23. Every bubble, chip and tablet keeps **≥ ~45 cm from the head**: if the island is pulled closer, the text slides
    along the view ray to 45 cm (scaled to keep its angular size), never through the face. Check with a 25 cm lean-in.

## H. The Heart's voice (interim)
24. The new Heart call ("critter" sound) is worse than the old deep synth "moooo" Mugesh liked. Make the Heart's
    entrance call the old synth version (the Spirit blip two octaves down with its bell ping), **once**, when the Heart
    first speaks; keep its murmurs subtle. A proper Heart voice comes later as its own step.

## I. Logs from playtests
25. In the report: the one-line Git Bash command to pull the device log after a playtest
    (`adb logcat -d -s Unity > playtest.log`). Make the in-game "Report bug" save the last ~2 min of log on the device.

## Done means (no silent skips)
- Before writing the report, **re-read this whole prompt** and give every numbered item a status: done / partly / not,
  with the evidence (test name, shot, log line, or why not).
- Also re-read Mugesh's open notes (`Assets/Docs/Notes.md`, `Notes copy.md`) and the "not done / partly" items of
  Run-24 … Run-28; list any still open at the top of the report so nothing disappears.
- Add each fixed item that has come back before to `Assets/Docs/Regressions.md`.
