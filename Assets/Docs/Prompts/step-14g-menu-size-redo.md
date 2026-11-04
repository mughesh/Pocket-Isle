# Step 14g: menus back to the old size (keep the stone look), realm card fixes

Decided Nov 4 (design chat). Orient first if you're a fresh session (`CLAUDE.md`, `.claude-handoff.md`, `Run-14f.md`).
Commit per section "(headset test pending)", no AI attribution, don't push. Mugesh tests between sections.
Screenshots of the problems: `Assets/Docs/Notes-img/menu-too-big-nov4.png`, `settings-too-big-nov4.png`,
`realm-card-nov4.png`.

**What went wrong:** step 14f applied a "minimum text size" rule by scaling the boards up. The game menu now covers the
whole island and the settings board is a wall. Mugesh: "the older buttons were the correct size." The **stone tablet
look stays**; the **size, spacing and layout go back to the pre-14f runes**. My (design chat's) rule was wrong; text
readability must never be solved by making the whole board bigger.

## A. Game menu
1. Tablets at the **pre-14f rune size and spacing**, in **one row** above the near rim like the old menu: Resume
   (primary, a little larger, gold inlay) · Controls · Settings · Title, the speaker icon as a small square tablet at
   the end, Dev (QA only) small and set apart. The island must stay clearly visible behind/under it (no more than the
   old menu covered).
2. Lighter tablets: thinner slab, smaller moss strip, label font sized to the tablet (the pre-14f label size is fine).
3. While the menu is open: no Spirit chatter or hints (the "Palm up and pinch..." line showed while the menu was
   already open), queued lines wait until it closes.

## B. Settings board
4. Back to the **pre-14f board size** (roughly half today's width and height). Volume rows: smaller pebbles, and the
   **current level must be visible** (today every pebble looks the same: lit pebbles filled gold up to the level, the
   rest dark; "off" clearly marked when chosen). Selected option in Hand / Hints / Touch walk: gold fill, others plain.
5. Bottom row (Recenter, Controls, Credits, Back) the same smaller tablets; Credits must not look disabled.
6. No Spirit hints while Settings is open (a "Face the water and tap" hint sat on top of the board).

## C. Realm name card
7. The sub-line is too big: make it **~55% of the name's size**, no glow halo behind it, sitting right under the
   plaque (not floating far below it).
8. The name overflows its plaque ("Lilymere" is cut at the left edge): size the plaque to the text with padding, or
   fit the text inside; check all four names (Still Isle, Galewick, Cinderwood, Lilymere) and both line variants.
9. **Lilymere's line is wrong:** nothing looks frozen when you arrive. Use "Where the water ran dry" (restored: "Where
   the water flows again"). Re-check Galewick's and Cinderwood's lines against what the player sees on arrival.
10. Bug: on entering Lilymere the Spirit's bubble showed only "Lilyme" (a cut-off line). Find why (a line interrupted
    mid-typing by the card, or a short text entry) and fix it so the Spirit never shows a partial word.

## D. Text sizes (the right way)
11. Revert 14f's size changes everywhere they made a board bigger. Keep a readability rule only as: labels use the
    largest font that fits the element at its pre-14f size; if a label doesn't fit, shorten it. Report any element
    whose text is still hard to read at the default distance instead of scaling it.

## Housekeeping
- Shots: game menu and settings over the island (bright/dark background), each realm card. Compare with the pre-14f
  shots in `Logs/BattleShots` and the problem screenshots above. Update `.claude-handoff.md`, `Run-14g.md` (short:
  what changed, sizes before/after, headset checks). EditMode tests green.
