# Project brief for Claude Code

## What we are building
A seated, hands-first mixed reality creature-collecting game for Meta Quest, built for the
**Meta VR Start Developer Competition 2026**. Working title and all creature names are placeholders.

The player sits or reclines. A small floating island (about 70 cm wide) floats in their real room.
**No table is needed.** They place it where they like (desk height, hip level, eye level from a
reclining chair) and can move, turn and resize it. The island is a window onto a much larger world map:
it shows only a patch of land that scrolls as a tiny explorer walks, like the top-down view in classic
creature-collecting games. Walking into tall grass triggers an encounter, a short battle weakens the
wild creature, and a flick of the hand throws a capture pod. Caught creatures live on a collection shelf
beside the island. The finished game has a short storyline across a few regions.

Core loop: **explore → tall-grass encounter → battle → capture flick → collection shelf**.

## Hard deadlines
- Internal submission target: **Nov 16, 2026**. Official deadline: Nov 18, 2026, 12:00 PM PT.
- The owner (Mugesh) is away **Oct 28 – Nov 2**. Leave the project in a working state before that.
- He has little time to code or model. You write almost all code and generate all assets in code.
  He tests on the headset and reports what feels wrong.

## Competition rules that affect code and content
- **Fully usable with hands, end to end.** Controllers optional. Never require a controller.
- Seated or reclined: every interaction works without standing or walking around the room.
- Fast cold start, clean pause/resume, something satisfying within 10 minutes.
- Keep key UI inside the central ~70° field of view (Meta VR Glasses are narrower than Quest 3).
- At least 60 fps on Quest (target 72). Must be stable and bug-free.
- Passthrough must be purposeful: the island floats in the real room, casts a soft shadow on the real
  surface below it (MRUK), and snaps onto a table if released just above one.
- **No brand logos, no branded products, no real people, no copyrighted characters.**
  Creatures must be original. Do not imitate Pokémon designs, names, or the red-and-white ball.
- English only. Must work offline. **No runtime calls to AI or web APIs.**

## Technical decisions (do not change without asking)
- Unity **6.3 LTS**, URP, Meta Quest build profile, OpenXR with the Meta XR feature group.
- Meta XR SDK **v207 or later**: Core SDK, Interaction SDK, MR Utility Kit (MRUK), Meta XR Simulator.
- Input: Interaction SDK hands. Movement uses **thumb microgestures**: a swipe (left, right, forward,
  back) starts walking; walking continues while the thumb stays off-centre (read from hand joint poses,
  because microgesture events fire once per swipe); thumb back to rest or a thumb tap stops. Steps are
  tile-by-tile. Capture is a flick/throw gesture. Gaze + pinch for UI.
- Island placement: grab the rocky rim/base to move (never the play surface); two-hand grab for yaw and
  uniform scale; the island always stays level (no tilt toward the head). Re-placing in front of the user is
  the standard Quest recenter. The island is parented to the rig's TrackingSpace so MRUK world lock moves it
  with the user. Microgestures are ignored on a hand that is grabbing.
- Art style: **toon-shaded, sculpted, procedural**. No hand modelling, no imported art except from the
  Meta Asset Library if Mugesh approves it.
  - Creature bodies are **signed distance field (SDF) recipes** melted together with smooth union,
    turned into meshes with marching cubes **at edit time** (Editor scripts), saved as `.asset` meshes.
  - **No bones, no skinning, no Animator rigs.** Bodies only breathe or jiggle in the vertex shader.
    Ears, tails, wings and similar are separate rigid meshes on pivots, animated in code.
  - Toon shader with banded lighting, rim light, inverted-hull outline, and shader-painted
    patterns (belly, socks, tips, spots). Eyes are a separate eye shader (iris, pupil, highlights)
    that can look at the camera.
  - Grass is one instanced mesh with a wind and push-away vertex shader.
  - The world is a large authored data grid (built with a generator, then story beats placed by hand;
    not random at runtime). The island renders a scrolling window of it: tiles, rocks and trees are
    instanced and recycled; entering tiles rise from the base, leaving tiles sink. The rocky base is fixed.
- Battle: 1 vs 1, 2–3 moves per creature, no menus. Four attack templates (shoot, lunge, slam,
  shield) recoloured per element. Effects are small shaders plus one pooled particle system.

## Reference implementation
`Assets/Docs/Reference/creature-diorama-v2.html` is a working three.js prototype of the art style,
creature recipes, toon/outline/eye/grass shaders, encounter, battle and capture flow.
Port from it. Keep the look and the recipes, translate GLSL to URP shaders (HLSL or Shader Graph),
and translate the JS SDF recipes and marching cubes to C# Editor tools.

## Performance budget (Quest 3)
- Creature body: aim ≤ 6k triangles after simplification. Whole scene: ≤ 150 draw calls.
- Prefer GPU instancing, shared materials, texture-free shaders.
- Particles: one pooled system, modest counts.
- Verify with Meta VR CLI performance captures, not guesses.

## Project layout
- `Assets/_Project/` — all our content
  - `Scripts/Runtime/`, `Scripts/Editor/`
  - `Shaders/`, `Materials/`, `Meshes/Generated/`, `Prefabs/`, `Scenes/`, `Data/`
- `Assets/Docs/` — reference prototype, build plan, step prompts, decisions log
- Do not edit files under `Packages/` caches or anything Meta/Unity ships.

## How to work with Mugesh
- Work in **small steps**. Each step ends with something he can see in the Editor or on the headset.
- Before installing packages, changing project settings, or deleting files, **list what you will do
  and wait for a yes**.
- After each step: say what changed, what he must click in Unity (exact menu paths), and what to test.
- Commit to git after every working step with a clear message. Never commit broken compile states.
- Unity recompiles on every script change, so batch related script edits together.
- If something can only be judged in the headset (feel, comfort, tracking), say so and ask him to test.
- Keep explanations short. He is an experienced XR design engineer, not a beginner.
- Use Meta's agentic tooling (Meta VR CLI, meta-quest agentic skills) to check docs and the device
  instead of guessing APIs. Meta SDK APIs change between versions; confirm against v207+.

## Overrides to my personal CLAUDE.md (this project only)
- **Hands:** use the Meta Interaction SDK (hands, microgestures, gaze + pinch). AutoHands does not
  apply here, and don't use XRI.
- **Island placement fallback is intended.** The island floats by default; MRUK only adds the shadow
  and table snap. When there is no scene data, skip those and keep playing. That is a UX feature, not a
  hidden config fallback. Log which path was taken.
- **Project settings are part of the work.** When a step deliberately changes ProjectSettings, XR
  settings, URP assets or OpenXR/Meta settings, commit those files with that step and list them.
  Still never commit UserSettings, Library, Temp, Logs or .slnx/.csproj.
