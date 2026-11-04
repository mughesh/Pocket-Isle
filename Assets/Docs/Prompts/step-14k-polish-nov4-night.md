# Step 14k: inward grab band, Journal text + ray/poke, lines in context, lodge without pedestal, ghost see-through, shrine trigger, labels at the feet

Decided Nov 4, 10:30 PM (design chat) from Mugesh's headset test of 14j. Orient first if you're a fresh session
(`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-14j.md`). Commit per section "(headset test pending)", no AI
attribution, don't push. Screenshots in `Assets/Docs/Notes-img/`: `grab-outside-line-nov4.png`,
`journal-small-text-nov4.png`, `seethrough-cutout-nov4.png`, `shrine-trigger-row-nov4.png`, `keeper-label-high-nov4.png`.

## A. Grab band: inward only (do first)
1. Today only the line itself seems to grab. The grab zone becomes a **band starting at the glowing line and going
   inward** over the rock/rim (about the width the zone has today, e.g. 3–4 cm deep, tune), **nothing outside the
   line** (that's where pinches for walking and selecting happen). Corners and the front lip as now; pods still win.
   The glow shows the band, not just a line. Tests: grab 1 cm inside, 3 cm inside, none 1 cm outside.

## B. Journal: readable, ray + poke only, hover pop
2. Text is too small (names, "Lv N", hints, the help line). Bigger fonts for every Journal and detail-card label: fewer
   portraits per page if needed (e.g. 4 x 3 with pages) rather than shrinking text. Hints may wrap to 2 lines.
3. **No look + pinch on the Journal** (Mugesh: gaze isn't needed here). Use **poke** and the **hand ray + pinch** (like
   the dev panel). Change the help line to "Poke a portrait, or point and pinch."
4. **Hover pop:** when a fingertip (or the ray) comes near a portrait or tab, it grows ~10%, lifts a little and gets the
   white hover rim (the game's focus language); it settles back when the finger leaves. Apply the same hover to every
   board tablet (menu, settings, Journal tabs) for consistency.
5. Also make the dev panel's text bigger where it's hard to read (QA only, lower priority).

## C. The Spirit talks about what's in front of you
6. Mugesh: in the lodge the Spirit first says filler like "Islings hide in the grass" and only after a few of those
   talks about the Hearth Lodge. Fix the rule, not just this case:
   - Every line gets a **context**: where it's valid (map / interior / realm, battle or not, lodge, hall) and its kind
     (objective, place intro, story, filler/idle). Tag every line in `Script_v1` (report the table).
   - A line only shows if its context matches **now**; queued lines whose context no longer matches are dropped (not
     delayed).
   - Entering a place: its **intro line goes first** (above queued filler); filler waits until the place's intro and
     any objective have shown.
   - Idle filler is picked only from lines valid in the current place (no grass hints in the lodge, no lodge hints in a
     realm, no realm hints in a hall).
   Run a flat pass through hub → lodge → each realm → a hall → battle and log every line shown with its context.

## D. Lodge: no pedestal
7. Remove the pedestal. **Poke an Isling** (or point/look + pinch): it hops in place and its **card** shows above it
   (the same card as today), facing you; poke again or poke another to switch; the card closes after 6 s or when you
   walk. Swaps keep working by grabbing (unchanged). Remove pedestal-only code paths and lines.

## E. See-through: a ghost, not a hole, and in grass too
8. The current effect cuts props away completely (looks odd). Change to a **ghost**: props between the head and the
   explorer fade to ~30–35% opacity with a soft edge-glow/outline so their shape stays visible; the explorer keeps its
   gold outline. Smooth fade in/out.
9. **Tall grass:** today nothing happens in grass. Grass blades within ~1 tile of the explorer, on the player's side,
   fade/flatten (shader parameter with the explorer position and radius) so the explorer is always visible in grass,
   in the overworld and in battle zoom.
10. Perf: still ≤ 0.3 ms total. Shots before/after from the screenshot spot and in tall grass.

## F. Shrine (and every walk-up trigger) fires from the right distance
11. At Cinderwood's shrine the guidance and ghost don't trigger from the row the explorer stands on; one row closer
    works. Make every "stand next to it" trigger (shrines, lodge door, temple doors, gates, hand-moment pieces with a
    reach) fire from **any tile within 1 tile including diagonals** of its footprint, and make sure the Spirit's gold
    trail ends **on** a tile that triggers it. Check all shrines and doors on every map; list any that were off.

## G. Battle labels at the creatures' feet
12. Names/HP bars are now too high and too small (Hearthstag's floats far above). Put every battle label (player,
    wild, Keeper) **at the bottom: just in front of each pad, at pad level, facing the player**, bigger (readable at the
    default distance), always drawn on top with the dark backing plate. The near Isling's label in front of its pad
    (toward the runes), the wild/Keeper label in front of its pad (between the pads, not overlapping the near Isling).
    Shots: wild battle with a tall near form, each Keeper fight.

## Housekeeping
- EditMode tests green; the overlap checker from 14j still passes. Update `.claude-handoff.md`, GameDesign.md (grab
  band, Journal input, line context rule, lodge cards, see-through, trigger rule, label rule), QA-Guide; `Run-14k.md`
  with the headset test list (grab first).
