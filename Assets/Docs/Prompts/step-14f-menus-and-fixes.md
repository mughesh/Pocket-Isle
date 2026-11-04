# Step 14f: stone-tablet buttons, the simple menu, readable text, and four fixes

**You may be a session that hasn't seen this project for a while (Mugesh's office account).** A lot changed. Orient
first, changing nothing: read `CLAUDE.md`, `.claude-handoff.md` (newest sections on top: steps 12–14e), the latest
reports (`Run-14a.md` ... `Run-14e.md`), GameDesign.md's latest sections (realm order Galewick first, Quick Journey v2,
the finale, the objective rule), `git status`, `git log --oneline -25`, and check the Unity bridge. Leave uncommitted
changes you didn't make alone. Rules: commit per section "(headset test pending)", **no AI attribution lines** in
commits, **never push** unless Mugesh says "push", ask before installing packages or changing Project Settings.
Mugesh is around: tell him what to test after each section. Decided Nov 3 night (design chat).

## A. Four fixes from Mugesh's Nov 3 notes
1. **The "swipe twice" lesson can trap players.** Players see no difference, so stop teaching it. The last walk lesson
   becomes "Swipe to walk, tap to stop." It is **done** when any of these happens: the player taps to stop a walk, the
   explorer arrives on the gold tile, or after 2 completed smart steps. It must be impossible to get stuck (also a
   30 s fallback that passes it). Keep "swipe twice = keep walking" working and on the Controls card; no lesson teaches it.
   Old saves sitting in that lesson resume into the new one. Test it.
2. **Tiny text on boards is unreadable.** Apply one rule to every piece of world-space text (realm name cards and
   their sub-line, hub gate signs, rune sub-labels like "Adventure" under Continue, chips, counters, cards, credits):
   **cap height ≥ 1.4 cm at the default viewing distance (≈ 1.5° of view), scaled with the island**, high contrast
   with its background, a dark outline when it sits over the scene. Where a line can't fit at that size, shorten it
   or drop it (list what you dropped). Report every text element and its size before/after.
3. **Untangling Galeowl walks the explorer.** Pinches used on the knots are read as look + pinch walking. While the
   untangle stage runs, the explorer can't walk at all (lock it like during battles), and generally: a pinch that
   starts within ~8 cm of an active hand-moment piece, or while a piece is held, never walks (check every realm's
   pieces with the same rule).
4. **The Spirit going into the Heart looks like overlapping meshes.** In the finale, the Heart's forehead gets a
   **gold crystal**, set in the stone, unlit until then (faint, no glow). When the Spirit lifts off the palm toward
   the forehead, it turns into light (the lantern mesh dissolves into a bright gold mote ~0.3 s) and flies into the
   crystal; the crystal ignites (glow + a soft ring of light + the heartbeat), and no lantern mesh is ever visible in
   or overlapping the forehead. The spark that returns later comes out of the crystal. Update the finale shots.

## B. Mossy stone tablet buttons (every button in the game)
5. Replace the plain transparent rune look (title, game menu, settings, end cards, lodge, **battle runes too**) with
   **chunky carved stone tablets**: rounded slab, a little moss on the top edge, the label **carved in and filled with a
   light cream/gold**, toon + outline, soft shadow under it. States: idle, looked-at (grows, gold rim glow, label
   brightens), pressed (sinks a few mm + a soft stone tap sound), disabled (dim). Primary action (Resume, Continue) =
   a slightly larger tablet with a gold inlay border. One shared mesh + material (instanced), ≤ 1 draw per board.
6. **Visibility check (Mugesh asked for it):** the tablets float over passthrough, so the background can be a white
   wall or a dark room. Make sure they never blend: text vs slab contrast ≥ 4.5:1, slab separated from any background
   by its outline + shadow/halo. Take shots of the title, game menu, settings and a battle over **a bright, a mid and a
   dark background** (`Logs/BattleShots/14f_buttons_<bright|mid|dark>_<board>.png`) and check them; also check
   readability per A2. Report the measured contrasts.

## C. The simple game menu
7. Game menu = **Resume** (large, gold, first) · **Controls** · **Settings** · **Title**, plus a **small speaker icon**
   tablet in its corner for music on/off (replaces the Music On / Off rune). **Dev** only in QA builds, set apart.
   Title screen keeps Continue / New journey / Settings on the new tablets.

## Housekeeping
- EditMode tests green. Update `.claude-handoff.md`, GameDesign.md (buttons, menu, text rule, finale crystal),
  QA-Guide "what's new". Report `Run-14f.md`: commits, the text size table, the contrast numbers and shots, headset
  test list in order. No build unless Mugesh asks.
