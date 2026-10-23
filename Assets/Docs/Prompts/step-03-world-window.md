# Step 3: world window (scrolling + zoom)

Read CLAUDE.md, Assets/Docs/BuildPlan.md (v3) and Assets/Docs/GameDesign.md first.
Goal: the island stops being the whole world and becomes a **window** onto a bigger map. The explorer walks
the map; the island shows the patch around them. The same window must **zoom in around a tile**, because
battles happen on the island zoomed in (step 5). This is the most important system in the game, so keep it
clean, data-driven and cheap to draw.

## Data
1. `WorldMap` ScriptableObject: a large tile grid (same char codes as IslandMap: G T P W F R, plus `X` = blocked
   void for map borders). Tile size stays 0.05 m. Keep it a plain text grid so I can hand-edit it.
2. Editor menu **Tools > Diorama > Step 3 > Generate Test World**: seeded generator that writes a 56×56 test map
   (~4× the island): winding paths, grass, 4–6 tall-grass patches, a pond, tree clusters, rocks, X border.
   Deterministic from the seed. Later realms will use the same generator, then hand-placed story tiles.
3. The island **footprint** (rocky base, rim colliders, shadow, grab) stays as it is now and never moves with
   the map. Keep the current 14×14 rounded shape as the window shape.

## Window (scrolling)
4. A `WorldWindow` (or refactor IslandBuilder into it) draws only the tiles inside the window, offset so the
   explorer's tile sits at the window centre. Instanced as now; recycle buffers, no per-step allocations.
5. **Smooth scroll:** while the explorer steps from tile to tile, the map content slides under it over the
   step duration, so the explorer stays centred. Expose `deadZoneTiles` (0 = always centred, Pokémon-style;
   >0 = explorer moves freely in the middle and the map only scrolls near the edges). Default 0; I'll compare.
6. **Edges look clean:** tiles in the outermost ring of the window sink into the base as they leave and rise as
   they enter (driven by distance to the window edge, so it's continuous while sliding). Anything outside the
   footprint is clipped in the shaders (island-local XZ test), including grass blades, trees and outlines.
7. **World borders:** clamp the window to the map bounds; near a border the explorer walks off-centre instead of
   showing empty space.
8. Collisions: can't walk into W, F, R or X. Walking/running/hold-to-walk modes stay exactly as they are.
9. Entering a T tile raises an event `OnTallGrassStep(Vector2Int tile)`; for now just flash the tile and log it.

## Zoom (battle stage)
10. `ZoomTo(Vector2Int tile, float factor, float seconds)` and `ZoomOut(float seconds)`: the map content scales
    around that tile inside the same fixed footprint (default ×3, 0.6 s, eased). Fewer tiles visible, same
    clipping and edge rise/sink. Grass, trees, rocks and the explorer scale with it.
    Walking is disabled while zoomed. Editor test key: **Z** toggles zoom on the explorer's tile.
    On the headset: a thumb **tap while standing still** toggles zoom (temporary, for testing).

## Performance
11. Report draw calls and instance counts in the window at ×1 and ×3, in the Editor Stats and on device
    (Meta VR CLI capture if available). Target: island + grass ≤ 60 draw calls, 72 fps on Quest 3.

## Housekeeping
- Remove `PalmSummon.cs` (retired). Keep the Diorama scene working or say what you changed there.
- One compile batch per sub-step; don't commit until I test. Then commit, and update `.claude-handoff.md`.

## Test on the headset
1. Walk in one direction for 20+ tiles: the map keeps scrolling smoothly, the explorer stays centred.
2. Edges: tiles sink/rise at the rim, nothing pokes past the rocky base.
3. Walk to a world border: the map stops, the explorer walks to the edge.
4. Water, trees and rocks block you.
5. Step into tall grass: the tile flashes.
6. Tap while standing: the island zooms ×3 on the explorer in 0.6 s; tap again to zoom out. No pop, no gaps.
7. Grab and move the island while zoomed and while walking: still works.
