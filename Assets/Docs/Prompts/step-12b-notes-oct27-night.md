# Step 12b: fixes from Mugesh's Oct 27 night notes

Source: `Assets/Docs/Notes.md` (the whole current file). Triaged in the design chat; build these as decided. Items
about walking, guidance and line styling are in 12a, not here. Read `.claude-handoff.md`, `Overnight-Oct28b.md`.

## A. Shared
1. **Focus lock for every hand moment** (Mugesh loved the lotus garden's view settle): when the explorer comes within
   reach of any hand moment in any realm (mill, kites, clouds, knots, log, bellows/vent/firebox, lanterns, village
   flame, pads, garden, basin), the island window glides to frame all of that moment's pieces and **holds** while
   it's active (the explorer can still walk inside it); it releases when the moment is done or the explorer walks out
   of reach. Generalise 11d's garden recentre into this; one component, per-piece framing bounds.
2. **Grab assist for every pick-up** (kites and shards still miss often): while a hand is near a gold-focused
   pick-up, pinch **or** fist anywhere within ~8 cm grabs the nearest one (today it's tighter); the object shows white
   hover when a hand is in range so the player knows a grab will catch. Apply to kites, Keeper shards, knot ends, log,
   ember/water source, lily pads (push stays a touch).
3. **Bubble never hidden by geometry** (forge): the Spirit's bubble picks a spot on the player's side of its anchor
   and is drawn over the island's props (ZTest always or a depth offset for the bubble only), never inside the forge
   or cliff. Fold in the backlog "bubble wavers" fix (PolishBacklog "Fix on Nov 3"): one resolver, a max distance from
   the Spirit, hysteresis so it doesn't oscillate.
4. **Exit doors read as doors** (Keeper halls, lodge, any interior): the 11d door glow on the way out too, plus a small
   hanging sign or floor arrow with an exit glyph, gold focus when the interior's objective is done.
5. **Respawn after losing:** at the **Hearth Shrine of the realm you're in** (the last one used there; none used yet =
   that realm's arrival), not the last shrine anywhere. Healed, nothing lost.
6. **Shard colours:** fire red-orange, **wind green** (was blue), water blue. Shards, sockets, the statue glow, the
   Spirit's circling shards, the realm gate tints if they use the shard colour.
7. **Gift bug:** after doing Galewick first and coming back to the hub, the Full adventure got the Quick Journey gift
   (2 Islings). The gift must only ever fire in the **quick slot**, once. Find the cause, fix, add a test.

8b. **Dev panel rework** (QA builds only, low priority: do it last in A): open it **higher and to the right of the
    island** (dominant-hand side), add a **close (X) rune** in its corner, and reorganise: fewer top-level tabs with
    clear names (Travel, Team, Battle, Story, Save, Tools), inside each a short heading per group (e.g. Travel >
    Hub / Cinderwood / Galewick / Lilymere / Interiors) with readable labels ("Cinderwood: forge door", not "CW: forge
    door"); a scroll (drag or up/down runes) when a tab has more rows than fit. Same functions as today.

## B. Cinderwood forge
8. After taking the ember, the **firebox** must be obvious: gold focus on it while the ember is on the palm, the
   chip "Tip it in!", the Spirit points at it, and the bubble isn't hidden (A3).

## C. Galewick ridge
9. Clouds: they must look like **clouds in the air**, not things sitting on the road: float 2–3 tiles higher with a
   soft round shadow below, drift side to side and billow (slow bob, rotation, puffs scaling), thin wisps trail when
   gusted. The ridge path becomes **2 tiles wide** along the cloud bank. **Remove the corner bush** that hides half the
   path (and any tall prop that overlaps the ridge path in the default view).

## D. Lilymere
10. **Drought look instead of white:** the dry bed tile (D) and the dry garden become **cracked dry mud**: warm tan to
    ochre-brown with darker crack lines, a few pale salt rims and dried reeds, never near-white. The rain cloud must
    stand out against it (cloud stays cream-white with a grey underside). Rain darkens the mud under it (wet tint that
    fades slowly) and leaves small puddles; once a bud blooms, a little pool of water spreads around its stone. After
    the lagoon rises, the garden fills with shallow water between the lotus stones (no more white).
11. **Waterfall redesign (pour into the mountain):** replace the stone basin with a **rocky cliff/mountain** whose top
    has a carved **empty bowl** (a dent). The **frozen waterfall falls from the bowl's lip** down the cliff face into
    the lagoon. Pouring into the bowl fills it (half after the first pour, full after the second, with a visible
    water level); full: water spills over the lip, cracks race **down** the frozen fall, it shatters and runs, then
    the lagoon rises as built. Gold focus on the bowl while carrying water; chip "Pours n/2". Keep the bowl within
    hand reach and inside the focus-locked view (A1). Rebuild the map spot if needed; keep the scoop inlet near.
12. **Water in both hands:** replace the small ball with a **wobbly sheet of water pooled across both cupped palms**:
    a soft mesh spanning the two palm centres (its edges follow each palm), a little meniscus, light caustic sparkle,
    drips falling from the fingertips. Tipping both hands pours a **stream** (ribbon mesh + droplets) from the lower
    side; the sheet thins as it pours. One cupped hand (fallback) holds a smaller pool in that palm.
13. **Two-hand ghost:** a proper two-hand clip (both hands cup together, dip into the inlet, lift, carry, tip over the
    bowl), retargeted with source = the inlet and target = the bowl so it **animates all the way to the drop point**.
    If no recording exists, build it in code from the cup pose (mirrored) with a path from the inlet to the bowl.

## E. Housekeeping
- EditMode tests green; commit per section (A, B+C, D) "(headset test pending)". Update `.claude-handoff.md`,
  GameDesign.md, QA-Guide. Shots `Logs/BattleShots/12b_*` (focus lock at the mill / kites / basin, forge bubble +
  firebox focus, ridge clouds in the air with shadows, drought mud vs rain, the new cliff bowl frozen / half / running,
  two-palm water, two-hand ghost frames, wind shard green, exit sign).
