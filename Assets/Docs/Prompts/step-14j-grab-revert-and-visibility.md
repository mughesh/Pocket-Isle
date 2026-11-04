# Step 14j: the old grab back (tighter), nothing overlaps, labels never inside meshes, see the explorer through things

Decided Nov 4, 8 PM (design chat) from Mugesh's headset test of 14i. Orient first if you're a fresh session
(`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-14i.md`). Commit per section "(headset test pending)", no AI
attribution, don't push. Mugesh tests between sections. Screenshots in `Assets/Docs/Notes-img/`:
`journal-overlap-nov4.png`, `lodge-journal-overlap-nov4.png`, `battle-name-inside-nov4.png`,
`explorer-hidden-nov4.png`, `keeper-hp-hidden-nov4.png`.

## A. Grab: revert the hold, keep the old grab with a tighter zone (do first)
Mugesh: "the grab we made now ... I am unable to grab or do anything." **Revert step 14i A** (hold-to-grab, its guards
and its lesson/ghost/Controls wording) back to the **pre-14i grab** (14a B: corner handles + front lip, pinch or fist,
pods win, battle/lodge corners only). Keep 14i B (the move + two-hands lessons restored) but with the old "grab"
wording and ghosts.
1. **Tighter zone:** today a pinch ~10 cm outside the glowing frame line still grabs. The grab zone must sit **inward
   of the line** (over the rock), extending **at most ~1–1.5 cm outside** the drawn line, with a sensible height band.
   Report the old and new zone numbers (per handle: outside reach, inside depth, height band). The glowing frame must
   match the real zone (what you see is what grabs).
2. Tests: grab inside the zone, no grab 3 cm outside the line, pods still win, two-hand grab.

## B. Nothing overlaps (buttons, tablets, pods)
3. **Journal tabs** (Fire / Water / Wind / Close) overlap each other: lay out every row of tablets with fixed gaps
   (≥ 8 mm between tablets at island scale 1), sized to fit the row; never overlap. Check every board (game menu,
   settings, Journal, detail card, end cards, title, lodge, battle runes).
4. **Lodge Journal tablet** touches the first pod: place it left of the pod row with a clear gap; it must never cover a
   pod or a socket (also when the party is full).
5. Add an automated check (Editor test or bridge command) that lays out each board and reports any overlapping
   tablet rects; run it and fix all hits.
6. Journal silhouettes are **dark-coloured, not black**: unseen portraits must be a solid flat silhouette (one dark
   colour, no shading or details), with "???" and the hint readable.

## C. Labels never inside a mesh (names, HP bars, Keeper bars)
7. In battle the near Isling's name and HP bar sit **inside its head** (Riverloom, Marimoss's sprout) and the Keeper's
   name + HP bar are **hidden inside its fury aura / shield sphere / flames** (Hearthstag).
   Rule for every battle label (player, wild, Keeper): place it **above the actor's full visual bounds** (body + parts
   + aura/shield/flames/fx), with a small gap; draw it **on top** (always visible, never occluded by models or fx, a
   subtle dark backing plate for contrast); keep it inside the view (clamp under the top of the battle view).
   For the near actor, which you look at from behind: put its label **beside its pad on the near side** (toward the
   runes) rather than over its head, so it never hides the wild one either.
8. Re-check 14h C (no Isling blocks the view): Riverloom at Lv 15 still covers a lot of Kettlewing in the shot. Make
   the rule stricter or verify it's active for evolved forms in wild battles; report each tall species' on-pad scale.
9. Shots of a wild battle with the tallest near forms and a Keeper fight per realm (`14j_labels_*.png`).

## D. Always see the explorer
10. When the explorer is hidden behind something (trees, buildings, props, cliffs) from the player's view, show it:
    props **between the head and the explorer fade out in a soft circle around the explorer** (dithered/transparent
    cut-out, about 2 tiles across, smooth in/out over 0.2 s), and the explorer gets a thin **gold outline** while it
    was occluded. Applies on every map and interior; only props on that line of sight fade, nothing else changes.
    Cheap on Quest (no extra pass over the whole island; per-prop fade via a shader property or a screen-space mask).
11. Shot from the screenshot's spot in Cinderwood, before/after (`14j_explorer_seen_*.png`).

## Housekeeping
- EditMode tests green; perf check that D costs ≤ 0.3 ms. Update `.claude-handoff.md`, GameDesign.md (grab zone,
  layout rule, label rule, see-through), QA-Guide; `Run-14j.md` with the headset test list (grab first).
