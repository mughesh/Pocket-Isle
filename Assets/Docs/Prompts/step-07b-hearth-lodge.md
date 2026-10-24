# Step 7b: Hearth Lodge (shrine interior) + "Islings"

Read CLAUDE.md, step-07-progression-save.md, GameDesign.md and Script_v1.md.
Mugesh tested step 7 on Oct 24. Fixes-2 are good. **The shrine doesn't work as a core place**: you can't tell
it's a shrine, it only triggers from one exact tile, nothing is asked of your hands, and the 2D storage panel
feels wrong. Decided Oct 24: the shrine becomes a doorway into a small **interior scene that rises inside the
island's rim** (dollhouse cutaway). The same **scene swap** is reused later for temple boss arenas (step 9) and
realm gates (step 8), so build it generic. It also **replaces the Sanctuary island** (cut list #4).

## A. Scene swap (generic, reused for gates and temples)
1. `IslandBuilder` / `WorldWindow` can switch to another `WorldMap` at runtime:
   `SceneSwap.Enter(WorldMap interior, Vector2Int entryTile, float zoom)` and `SceneSwap.Return()` (back to the
   saved outdoor map, tile and facing).
2. Transition (about 1.2 s): outdoor tiles and props **sink into the base** (front to back ripple), the rocky
   base/rim tints toward the next map's theme, the next map's tiles **rise**. Reverse on return. Explorer fades or
   shrinks with its tile and pops up on the entry tile. Recycle the instanced pools; no hitch (check frame time).
3. Interior maps can be smaller than the window: they're centred, shown at their own `zoom` (lodge ~1.75×) so the
   room fills the island top and creatures look bigger than outside. Outside tiles stay hidden.
4. Theme per map (`RealmTheme`); interior lighting comes from its own theme (warm hearth light).
5. Save: `SaveData` stores whether you're inside and which interior; load puts you back inside.

## B. The shrine on the map: easy to find, a hand moment to enter
6. HearthShrine becomes **2×2 tiles**, taller, with a **flame beacon**: a soft additive flame column with a slow
   pulse, visible above the trees from across the window. Distinct silhouette: a stone hut with an arched doorway
   and the hearth flame in a bowl on top (mossy cap like the Spirit's).
7. Spirit line `shrine.first` fires the **first time a shrine enters the window**, anchored to it.
8. **Any of the tiles touching the shrine counts** (not one exact tile). Standing there: the flame leans toward the
   player and brightens, a small glow ring appears above the bowl.
9. **Enter = warm your hand over the flame:** palm within ~8 cm above the bowl, palm facing down (palm normal
   · down > 0.6), held **0.8 s** with a filling ring. Accessibility: **look + pinch on the shrine** does the same.
   Then: flame flares → heal-all wave over the rim pods → save → scene swap into the Lodge.
10. Losing everything returns you to the last shrine's front tile (outside), healed (unchanged).

## C. The Hearth Lodge (interior)
11. `Lodge` WorldMap, about **8×8** tiles, authored by the generator + hand edits like the test map. Wood/stone
    floor tiles. Dollhouse cutaway: **back wall and the back half of the side walls only**, no roof, no front wall.
12. New SDF props (prop kit, no modelling): **Hearth** (big, back centre, lit), **Back wall arch** segments,
    **Honeycomb pod wall** (left back, hex cells holding pods), **Rug**, **Cushions** (2–3), **Inspect pedestal**
    (front centre), **Door** tile (front edge, glowing threshold). Lodge theme: warm wood, cream, ember light.
13. **Stored Islings live in the room:** up to **12** shown as their baked prefabs (outlines on), wandering tile to
    tile slowly, idling, sleeping by the hearth, hopping. The rest sleep in pods on the honeycomb wall. Watch the
    budget (12 × ≤ 6k tris); if needed use a simplified mesh in the lodge.
14. **Inspect:** look + pinch (or poke) an Isling → it hops onto the pedestal and its card shows: name, Lv, element,
    HP, XP bar, its 3 moves (element colour + name), ATK / DEF / SPD. Pinch elsewhere or pick another to close.
    Honeycomb pod → that Isling hops out into the room (another goes to sleep in a pod if over 12).
15. **Party ↔ storage (the PC-box actions we need: view, to party, to storage):**
    - Grab an Isling in the room (ISDK grab, generous volume) → it curls into a pod in your hand → drop it on a rim
      socket: empty socket = joins the party; full socket = **swap** (the rim Isling pops out into the room).
    - Grab a rim pod and drop or toss it onto the floor → that Isling pops out into the room (storage).
    - Accessibility: with an Isling on the pedestal, look + pinch a rim pod (or empty socket) = same swap.
    - The party can never be empty (the last rim pod refuses with a wobble + Spirit hint).
16. **Leave:** walk onto the door tile, or look + pinch the door. Save on enter and on leave.
17. Remove the 2D `StorageShelf` panel.
18. Dialogue: `lodge.first` → anchored to the room centre: "This is the Hearth Lodge. Islings you've caught rest
    here." `lodge.swap.first` → anchored to the pedestal: "Pick one up and set it on the rim to bring it along."
    `shrine.first` new text: "A Hearth Shrine! Warm your hand over the flame." `party.full` → "Your pods are full.
    New friends will wait for you at the Hearth Lodge."

## D. "Islings" (the creatures' name, decided Oct 24)
19. Player-facing text: every Spirit line, card, toast and label says **Isling / Islings** (never "creature",
    never "Pokémon"). Update Script_v1.md and reimport. "Storage" → "Lodge".
20. Code: rename the **gameplay** types: `CreatureSpecies` → `IslingSpecies`, `CreatureInstance` →
    `IslingInstance` (and obvious members/variables). **Keep the art pipeline names** (SdfShape, CreatureRecipe,
    CreaturePart, the baker, Creature Lab), because they're tools shared with props.
    Rename the file and class together and keep the `.meta` GUID so assets don't break; `[FormerlySerializedAs]`
    for any renamed serialized field. After: no missing scripts, all species assets load, the save file still loads
    (add a save-version migration if the JSON keys change).
21. Do D as its **own commit at the end** so it's easy to revert.
22. Update GameDesign.md (Islings; Hearth Lodge replaces the Sanctuary island; shrine flow) and BuildPlan.md
    (cut list #4 done this way; scene swap built here, reused in step 8 gates and step 9 temple).

## Housekeeping
- One compile batch per section. EditMode tests green (add: swap rules, scene swap save/restore, rename sanity).
  Flat Editor Play smoke test: walk next to the shrine, look + pinch it, enter, inspect, swap, leave, save/reload.
- Commit locally per section with "(headset test pending)". No AI attribution lines. Don't push.
  Don't move `pre-goa` until Mugesh tests. Update `.claude-handoff.md`.
- Summary for Mugesh at the end: what changed, what to click, what to test, perf numbers (lodge + transition).

## Test on the headset
1. From across the window you can see the shrine's flame beacon; the Spirit points it out.
2. Stand on any side of it: the flame leans toward you. Hold your palm over it: ring fills, heal wave, the island
   sinks and the Lodge rises.
3. Your caught Islings wander the room. Look + pinch one: it hops onto the pedestal with its full card.
4. Pick one up and drop it on a rim pod: they swap. Toss a rim pod onto the floor: that Isling joins the room.
5. Walk out the door: the outdoor map rises back where you were.
6. Quit inside the Lodge, relaunch: you're still inside. No dips below 72 fps during the transition.
7. Every line and label says Isling(s).
