# Step 11d: fixes from Mugesh's Oct 27 headset notes (Galewick, Lilymere, general)

Source: `Assets/Docs/Notes.md` (lines 1–17; the two sound notes go to the audio pass, see G). Triaged in the design
chat on Oct 27 evening: every item below is decided, just build it. Read `.claude-handoff.md` (10b, 10e, 10f, 11a,
11b), `Overnight-Oct28.md` (Lilymere as built, feel numbers), GameDesign's focus language (gold = act now, white =
hover) and the 9h lesson chip.

## A. One rule for every objective: signify it (Notes 6, 7, 8, 17, 15)
Mugesh keeps finding tasks by accident. Build this once and apply it to every hand moment in all three realms:
1. **Gold focus** (outline or rim glow, the 9h language) on every object you can act on **right now** for the current
   objective: kites lying in the grass, cloud puffs, mill sails, lily pads, lotus buds, the scoop pool, the basin, the
   log, the bellows pad, the vent, the firebox, the lanterns, Galeowl's knots. Off once that object is done.
2. **Counter in the chip** whenever an objective needs N: "Kites 1/3", "Clouds 2/3", "Pads 1/3", "Lotus 4/5",
   "Pours 1/2", "Knots 1/3", "Lanterns" (no counter, it races). The chip stays pinned until the objective is done
   (re-pin on approach if the player walked off and came back). Add the counter to `HandPuzzle` so every piece gets it.
3. **Kites: say the task.** On entering the kite field: `gw.kites.task` "The tower needs kites. Let's fly all three!"
   pinned with "Kites 0/3"; the tower's crown shows three empty glowing hooks from afar.
4. **Open doors glow like the forge door.** When a door becomes enterable (wind tower, lake temple later, any
   interior): warm light from inside, a soft portal swirl in the doorway, gold focus, and a Spirit line
   (`gw.tower.open` "The tower's open! Let's go in."). Use the same component for the forge door so they match.
5. **Out-of-view targets** (Note 15): when an objective's objects aren't all inside the island window, a small gold
   arrow at the window edge points to the missing one(s), and the Spirit says once "One more over there!".

## B. General fixes
6. **Tile poke walking off by default** (Note 1): it fights scene interactions. Settings gets "Touch tiles to walk:
   On / Off" (default **Off**, saved). Walking stays: thumb swipe, look + pinch. Any lesson or hint that teaches
   touching a tile to walk uses look + pinch instead. Keep `TileTouch.Block` for when it's On.
7. **Shard grab after a Keeper win is too hard** (Note 2): the offered shard gets a bigger grab volume (≥ 6 cm round
   it, pinch **or** whole-hand grab), gold focus, and if not taken in 4 s it floats to the Spirit by itself (look +
   pinch also works). Same for every Keeper (shared flow from 10e).
8. **Ghost hands for pick-ups must read as a pinch** (Note 5): for pinch pieces (kites, knots, log, shard, pads) the
   ghost shows a clear thumb + index pinch (other fingers relaxed, the pinch point on the object, a tiny gold spark
   when it closes). The ember ghost shows an open palm-up hand under the vent (not a grab). And **every pick-up piece
   also accepts a whole-hand grab** (fist near it), because people will do that anyway.

## C. Galewick fixes
9. **Windmill sails flicker while the explorer walks** (Note 3, fine when idle): the spin angle must be continuous
   across map scroll/rebuilds (accumulate the angle per mill id, don't recompute from time or reset when the prop view
   is re-made). Check the small mills and the restored mills too.
10. **Mill flick is hard to hit** (Note 4): flick zone near plane 2.2 → 4 cm with more slack, min speed 0.5 → 0.35
    m/s, count index **and** middle fingertips, and any fingertip moving through the sail disc with a tangential
    part counts. Each counted flick gives visible feedback (sails jolt + a puff + the counter "Flicks 1/3").
11. **Cloud puffs too small and hidden** (Note 7): puffs ~2x bigger, raised so they sit clearly above the plants,
    no tall props on or right next to the ridge path under them, gold rim while the objective is active.
12. **No return gate after the wind tower** (Note 9): after Galeowl's win and the restoration wave, a return gate must
    appear outside the tower (like Cinderwood's in front of the temple). Find why it's missing and fix it; also check
    Cinderwood's still appears. Add a test.

## D. Lilymere fixes
13. **Lily pads** (Notes 10, 11, 12): pads stay inside the channel's water tiles (bounce off its edges, never leave the
    channel); a pad that still ends up out of reach or out of the window for 3 s fades and respawns at its start, so
    there are always 3. Gold focus on every free pad. Pads **float on top**: surface + a few mm with a slow bob, never
    submerged. **The finished bridge is just the three pads** touching edge to edge: no path tiles drawn over the
    crossing (the walkable tiles stay but render as water under the pads; the explorer hops pad to pad).
14. **Lotus crossing** (Note 13): same, the crossing is the lotus stones only, no road under them (bed/water shows).
15. **Rain cloud too small** (Note 14): the cloud ~2.5x (about a real palm's size, 10–12 cm), more and bigger drops,
    clearly visible from the default view.
16. **Lotus buds out of view** (Note 15): when the explorer reaches the garden, the island window recentres so all 5
    buds are in view (a gentle scroll, like a camera settle), and a gold "stand here" tile is offered if the window
    can't fit them from where the explorer stands. Plus A2's "Lotus n/5" and A5's arrow. Move buds closer together
    if the window still can't hold all five.
17. **Scoop and pour with two hands** (Notes 16, 17): primary gesture = **both hands cupped together** (palms up, side
    by side, within ~8 cm) dipped into the pool and lifted: a **bigger** water ball sits between them; tip both hands
    over the basin to pour. One cupped hand still works as a fallback (no lesson for it). Ghost: two hands cupping,
    lifting, tipping. Gold focus on the pool while scooping and on the basin while carrying; counter "Pours 1/2"; the
    basin's water level visibly half full after the first pour.

## E. Housekeeping for this step
- EditMode tests for the new rules (counter, re-pin, respawned pads stay 3, two-hand cup detection from synthetic
  poses, continuous mill angle, return gate after the wind win, tile-walk setting default off).
- Commit per section (A, B, C, D) with "(headset test pending)". Update `.claude-handoff.md`, GameDesign.md (the
  objective rule A), QA-Guide (what's new). Shots `Logs/BattleShots/11d_*` (kite field with the task chip + gold
  kites, ridge clouds, open tower door glow, pads floating + finished pad bridge, lotus crossing, big rain cloud,
  two-hand ghost, basin half full).

## G. Sound notes (don't fix now)
Append to the missing/bad sounds list at the bottom of `Assets/Docs/Audio/PhaseB-Hooks.md` for the next audio pass:
- The Spirit's text typing sound plays too often / too many beats; annoying. Find an alternative (fewer, softer ticks
  or one soft chime per line).
- The log's drop thud is weak, almost silent.
