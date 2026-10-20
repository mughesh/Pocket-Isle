# Step 1: toon look in Unity

Read CLAUDE.md, Assets/Docs/BuildPlan.md and Assets/Docs/Reference/creature-diorama-v2.html first.
Port the island, grass and toon shading from the prototype. No XR, no creatures yet.

First, the git baseline: confirm .gitignore covers Library/, Temp/, Logs/, UserSettings/, *.csproj, *.slnx;
add a .gitattributes with Git LFS for common binary types; make the initial commit.

Build:
1. `Assets/_Project/Shaders/Toon.shader` (URP, HLSL): banded lighting from the main light, rim light,
   shader-painted belly / sock / tip / spot masks in object space, emissive, a flash amount, and a
   vertex breathe/jiggle. Plus `ToonOutline.shader`: inverted hull pushed along the normal, front-face
   culled. Same parameters as the prototype's `toon()` function. Must work with GPU instancing and
   SRP Batcher.
2. `Assets/_Project/Shaders/Grass.shader`: instanced blades, wind sway, push-away from a world point,
   base-to-tip colour, short vs tall colours (prototype `grassMat`).
3. A small `IslandMap` ScriptableObject holding the 14 × 14 text grid from the prototype
   (G grass, T tall grass, P path, W water, F tree, R rock, . empty), tile size 0.05 m.
4. `IslandBuilder` (runtime MonoBehaviour): builds the island from the map with
   Graphics.RenderMeshInstanced or instanced MeshRenderers: dirt columns, coloured caps, the hanging
   rocky underside, trees, rocks and grass blades. Seeded random so it is identical every time.
   Keep everything under one root transform so the whole island can be placed and scaled later.
5. Toon materials for tiles, trees and rocks (to match the creatures later), with outlines on trees
   and rocks.
6. Scene `Assets/_Project/Scenes/Diorama.unity` with the island at the origin, a main light, and an
   Editor camera framing it like the prototype. Leave SampleScene alone.

Done means: open Diorama, press Play, see the island with swaying grass in the Game view. Report the
draw-call count from the Stats panel and any console warnings. Then tell me what to look at and commit.
