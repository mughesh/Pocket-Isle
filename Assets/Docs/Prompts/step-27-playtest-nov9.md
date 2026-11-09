# Step 27: fixes from the first outside playtest (Nov 9 evening, friends who don't own a Quest)

Triaged Nov 9, 11 PM (design chat). First players who aren't Mugesh: what confused **them** is what will confuse judges.
These are clarity fixes, not features; keep each one small. Mugesh's raw notes are at the end.

## How to run (unattended tonight)
- Orient (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-25.md`, git log). Branch **`step-27`** from master
  (`dbd6e3c`, step 25 is in, not pushed yet). Usual rules: stage by path; no keys/keystores/`_Recovery`/`_lanes`;
  commit per section "(headset test pending)"; no AI attribution; no questions; flat bridge + tests + smoke run +
  Release Check; revert after ~45 min stuck. **Don't push.** If all pass, fast-forward master locally.
- **Don't touch the release setup** (step 26 runs tomorrow with Mugesh).
- Report `Assets/Docs/Run-27.md`: before/after seat-view shots for A, D, F; old → new values; the headset test list.

## A. Temple and tower doors: obvious how to go in
Players didn't know how to enter. A hand shape shown over the door suggested a hand action on the door itself, but
entering is walking the explorer in.
1. **Make every plausible action work:** walking into an open door (as now), **look + pinch on the door** (the explorer
   walks up and in, using the walk-up rule), and **poking the door** with a fingertip (same). Applies to the Galewick
   tower (`TowerDoor`), the forge door, the lake temple (`TempleDoor`), and the lodge door if it isn't already so.
2. **One clear signifier on an open door** when the explorer is within a few tiles or the door is the current
   objective: the portal glow brighter and taller, a gold trail on the ground into the doorway, and a chip
   "Walk in, or look at the door and pinch". Remove any ghost hand that hovers over a door suggesting a gesture on it.
3. Opening moment: when a door first opens, the Spirit's line says it plainly ("The tower is open! Walk in.").

## B. The Journal opens on the right page
4. The Journal opens on the tab of the **realm you're in** (Galewick → Wind, Cinderwood → Fire, Lilymere → Water). On
   the Still Isle / lodge: the tab with the most Islings found; tie or none found → **Wind** (the first realm). Remember
   the last tab while the board stays open only.

## C. Swipe to walk, tap to stop: practise it
Players didn't learn that a thumb tap stops the explorer.
5. After the walk lessons, a short **practice**: three gold target tiles, one after another, at increasing distances
   along the path. Swipe to start, **tap to stop on (or next to) the gold tile**. Each success: the small gold burst +
   "Nice stop!"; the third: "Perfect!". A miss (walked past) just puts the next try's target ahead again, no fail state.
   Progress dots like the pinch lesson (o o o → * * *). Passes after 3 stops or ~60 s (never a wall).
6. The chip during it: "Swipe to walk · Tap to stop". The tap ghost shows the tap clearly (the gold dot on the thumb
   tip, a press-and-release). **The swipe ghost's animation is waiting for Mugesh's re-recording**: don't redo it, just
   make sure the tool he records with still works and say in the report how to run it.
7. Quick Journey keeps the practice (it's the judges' path) but at 2 targets.

## D. The Galewick tower's portal is hidden in grass
8. Tall grass (and any tall prop) within ~1.5 tiles of every door's front and portal is cleared or flattened (all
   doors: tower, forge, lake temple, lodge, realm gates). The portal glow rises above grass height. Seat-view shots of
   each door before/after.

## E. Less text, steadier text
One player couldn't read because the text "keeps moving spots"; another wished for a voice-over (not now).
9. **The Spirit stops hopping:** it moves only when the objective's target changes, not per line; it stays put for at
   least ~5 s once parked; moves glide (~0.6 s), never jump. The bubble stays attached to the Spirit, so consecutive
   lines appear in the same place.
10. **Readable time:** a line stays at least ~1.2 s + 0.05 s per character after it finishes typing before the next
    replaces it (objective lines stay until done, as now).
11. **Less chatter:** idle and filler lines half as often; no filler at all during lessons, puzzles, battles and the
    first 3 minutes. Report how many filler/idle lines played in a Quick Journey smoke run before vs after.
12. Not doing: voice-over (possible after the competition with ElevenLabs text-to-speech; out of scope now).

## F. Looking at things must be noticed
The white gaze outline is almost always missed.
13. When the gaze is on anything a look + pinch would select (`FocusHighlight.Hover`): the outline thicker and warm
    white, a soft glow, a small **pinch icon** (two touching fingertips, the gold dot style) floating just above the
    target, and a quiet tick on gaze-enter (once per target, rate-limited). Doors, temples, pods, lodge Islings,
    shrines and the bubble all use it. Keep gold for "do this now" (no change).
14. Check the icon never covers the target or the explorer, and it fades if a hand is already near the target.

## Mugesh's raw notes (Nov 9)
- Going into the temple was a little hard (confused). The hand over it doesn't give a proper signifier that it opens
  the temple that way.
- The Journal can show the wind Islings page first: our flow starts with the wind realm.
- Not aware that moving and tapping stops the character; practise it a few times like other interactions. (Swipe
  ghost re-recording still pending: Mugesh.)
- The portal outside the wind tower is hidden behind grass.
- Text feels too much, hard to read as it keeps moving spots; voice-over preferred (one player).
- Gazing on the temple and other objects isn't prominent; the white border is almost always ignored.
