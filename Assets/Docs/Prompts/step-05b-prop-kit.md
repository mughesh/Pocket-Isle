# Step 5b: prop kit + realm theme

Read CLAUDE.md, Assets/Docs/BuildPlan.md (v4) and Assets/Docs/GameDesign.md ("Cinderwood"). Steps 1–5 and the
battle-stage fixes are done (commit the fixes first if they aren't committed yet).
Goal: environment props made **the same way as creatures** (SDF recipes → baker), placed on the map from data,
drawn instanced through the world window. Plus a per-realm colour theme, so the test world can look like
Cinderwood. References: `Assets/Docs/ConceptArt/01_realms_diorama_sheet.png` (Cinderwood panel) and
`Concepts_01_Summary.md` (Cinderwood props and palette).

## Data
1. `PropRecipe` (ScriptableObject): one or more SdfShapes, each with a palette slot (e.g. clay, wood, stone, glow),
   optional rigid pieces (reuse CreaturePart where it fits), footprint in tiles (1×1 default, up to 2×2),
   blocks walking yes/no, random yaw / scale jitter, triangle budget (default ≤ 1.5k).
   Glow and simple motion live in the **shader** (flicker, steam wobble, sway) so props stay instanced.
2. **Map props layer.** `WorldMap` gets a second text grid of the same size for props, plus a legend
   (char → PropRecipe). Terrain stays in the first grid. Hand-editable like the terrain.
3. `RealmTheme` (ScriptableObject): colours for grass caps, tall grass (base → tip), path, water, dirt columns,
   underside rock, tree trunk/leaves (low/high), outline tint, and the **unrestored** variants (ash grey leaves,
   cold kilns). A global `_RealmRestored` (0..1) blends between them and scales glows. `WorldMap` references a theme.
   Make **Cinderwood** (restored + unrestored) from the concept palette and **Test** (current colours).
4. New terrain tile `H` = hot spring water: mineral teal, stone rim where it meets land, soft steam (one shared
   particle system or a shader billboard).

## Baker + rendering
5. Extend the baker: **Tools > Pocket Isle > Props > Bake Selected / Bake All**, meshes to
   `Meshes/Generated/Props/`, shared materials, palette via property blocks or per-instance colour.
6. Props render through `IslandBuilder` like trees and rocks: instanced, clipped and sinking at the window edge,
   scaling with zoom, sinking inside the battle clearing when they block the view. Blocking props stop walking.
7. Props show in the Creature Lab too (cycle them), with "Rebake & reload".

## First Cinderwood props (look at the concept panel)
8. Pottery kiln (dome + chimney + glowing mouth; cold when unrestored), paper lantern post (post + glowing lantern),
   brazier (tripod bowl; lit / unlit state for the temple puzzle), berry bush (3 blobs + berries),
   autumn tree (rounded canopy, glowing leaf tips; grey when unrestored), fallen log (2×1, blocks the path; the
   hand-puzzle step will make it liftable), stone lantern for paths, small pot cluster.

## Test
9. **Tools > Pocket Isle > Props > Dress Test World**: switch the test world to the Cinderwood theme, put a few of
   each prop near spawn, a hot spring, and the fallen log across a path. Editor key **G** toggles
   `_RealmRestored` 0 ↔ 1 (animated over 2 s) so we can see the restoration.
10. Report draw calls at ×1 and in battle zoom, and on device fps.

## Housekeeping
- One compile batch per sub-step. Don't commit until Mugesh tests. Update `.claude-handoff.md`.

## Test on the headset
1. The island looks like Cinderwood: red-gold grass, glowing trees, kilns, lanterns, a steaming hot spring.
2. Props scroll, sink at the rim, scale with zoom, and part for battles.
3. The fallen log blocks the path. Kilns and trees block walking.
4. Press G in the Editor (or a debug key on device): the realm fades from grey to glowing.
