# Project brief for Claude Code

## What we are building
A seated, hands-first mixed reality creature-collecting game for Meta Quest, built for the
**Meta VR Start Developer Competition 2026**. Working title and all creature names are placeholders.

The player sits back. A small floating island (about 70 cm wide) is anchored on their real desk or
coffee table. A tiny explorer walks around it. Walking into tall grass triggers an encounter, a short
battle weakens the wild creature, and a flick of the hand throws a capture pod. Caught creatures live
on a collection shelf beside the island.

Core loop: **explore → tall-grass encounter → battle → capture flick → collection shelf**.

## Hard deadlines
- Internal submission target: **Nov 16, 2026**. Official deadline: Nov 18, 2026, 12:00 PM PT.
- The owner (Mugesh) is away **Oct 28 – Nov 2**. Leave the project in a working state before that.
- He has little time to code or model. You write almost all code and generate all assets in code.
  He tests on the headset and reports what feels wrong.

## Competition rules that affect code and content
- **Fully usable with hands, end to end.** Controllers optional. Never require a controller.
- Seated: every interaction works within roughly a 2 ft radius. No walking around the room.
- Fast cold start, clean pause/resume, something satisfying within 10 minutes.
- Keep key UI inside the central ~70° field of view (Meta VR Glasses are narrower than Quest 3).
- At least 60 fps on Quest (target 72). Must be stable and bug-free.
- Passthrough must be purposeful: the island sits on a real surface found with MRUK.
- **No brand logos, no branded products, no real people, no copyrighted characters.**
  Creatures must be original. Do not imitate Pokémon designs, names, or the red-and-white ball.
- English only. Must work offline. **No runtime calls to AI or web APIs.**

## Technical decisions (do not change without asking)
- Unity **6.3 LTS**, URP, Meta Quest build profile, OpenXR with the Meta XR feature group.
- Meta XR SDK **v207 or later**: Core SDK, Interaction SDK, MR Utility Kit (MRUK), Meta XR Simulator.
- Input: Interaction SDK hands. Movement uses **thumb microgestures** (thumb swipe left, right,
  forward, back and tap on the index finger). Capture is a flick/throw gesture. Gaze + pinch for UI.
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
  - The island map is a text/data grid; tiles, rocks and trees are instanced.
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
