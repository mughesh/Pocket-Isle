# Step 4: creature kit + baker

Read CLAUDE.md, Assets/Docs/BuildPlan.md and Assets/Docs/GameDesign.md first. Step 3 (world window) is done.
Goal: a reusable, data-driven way to make all ~60 creatures. A creature is a **recipe asset**, not code.
This step proves it with the 3 starters standing on the island.

References:
- `Assets/Docs/Reference/creature-diorama-v2.html`: SDF helpers (`sdSphere`, `sdEll`, `sdRC`, `smin`, `smax`),
  `sculpt()` marching cubes, `surface()` for eye/mouth placement, toon/outline/eye shaders, body jiggle.
- `Assets/Docs/ConceptArt/03_starters_turnaround.png`: look at it. Kilnpot, Pebbloop, Pufflet must match.
- `Assets/Docs/Concepts/gemini-01-output.txt`: body recipes for the 3 starters (`kilnpotBody`, `pebbloopBody`,
  `puffletBody`, same helper names as the prototype) and palettes. Treat as a starting point.
- Mirror X when porting, as IslandMap.cs does.

## Data (ScriptableObjects)
1. `SdfShape`: a list of primitives (sphere, ellipsoid, rounded cone/capsule), each with params, blend
   (smooth-union k, or smooth-subtract), and a **mirror X** flag (most limbs are pairs).
2. `CreaturePart` (the **kit**, reusable): an SdfShape + pivot + idle motion (none, flick, wag, spin, bob, flap)
   + which pattern zones it gets. Build a starter set: round ear, bear ear, droopy ear, wool tuft, wing tuft,
   chimney tail, paddle tail, rotor (3 blades), flame puff, bubble ring.
3. `CreatureRecipe`: body SdfShape, placed parts (part + position/rotation/scale, mirror), eye size/spacing and
   mouth placement (found by ray onto the surface like `surface()`), palette (base, belly, accent/tips, socks,
   glow, eyes), pattern zone params, overall height, idle style (breathe speed/amount, hover for floaty ones).
4. `CreatureSpecies` (minimal now, battle uses it in step 5): name, element (Fire/Water/Wind/Neutral), recipe,
   evolution (species + level), base stats placeholder, 3 move slots (empty for now).
   An evolution can reuse the base recipe with **modifiers** (scale, extra parts, palette shift); support that.

## Baker (Editor)
5. **Tools > Pocket Isle > Creatures > Bake Selected / Bake All**: SDF → marching cubes (C# port) → weld vertices
   → smooth normals → simplify to **≤ 6k triangles** for the body → save meshes under
   `Assets/_Project/Meshes/Generated/<Species>/` and a prefab under `Prefabs/Creatures/`.
   Deterministic; re-baking overwrites cleanly.
6. Creature shader (extend Toon): banded lighting, rim, inverted-hull outline, **shader-painted pattern zones**
   from object-space position (belly, tips, socks, spots), optional glow, vertex **breathe/jiggle**. Palette via
   material property blocks so all creatures share materials.
7. Eye shader as in the prototype (iris, pupil, two highlights, look-at target), plus blinking.
8. **Creature Lab scene** (`Scenes/CreatureLab.unity`): turntable, neutral light, one creature at a time, a
   key to cycle species, and an Inspector button "Rebake & reload" so I can tweak a recipe in seconds.

## Runtime
9. `CreatureView`: idle loop (breathe, blink, part motions, look at the player's head when close), a `Hop()` and
   a `Faint()` for later. Scales with the world window, including when it's zoomed in.
10. Put Kilnpot, Pebbloop and Pufflet on tiles near the explorer's spawn in MR_Main, about 1 tile tall
    (explorer-sized), walking in tiny idle hops occasionally. Parent them to the world window so they scroll.

## Performance
11. Report triangles per creature and draw calls with the 3 on the island, ×1 and zoomed ×3.

## Housekeeping
- Commit the uncommitted docs first as their own commit (GameDesign.md, step-03 prompt, antigravity-02 prompt,
  ConceptArt.meta). Leave OculusRuntimeSettings / XRGeneralSettings out unless they're intended; ask me.
- Batch compiles. Don't commit step 4 until I've seen it. Update `.claude-handoff.md`.

## Check with me
- In the Creature Lab, after baking: screenshot each starter front and side next to the concept art and tell me
  where it differs. I'll judge, then we tune recipes before moving on.
- On the headset: the 3 starters on the island, breathing, blinking, looking at me, scrolling with the map.
