# Status and decisions — Meta VR Start 2026 entry

Last updated: Oct 20, 2026. Read this first when starting a new chat about the project.

## Competition facts
- Meta VR Start Developer Competition 2026. Deadline **Nov 18, 2026, 12:00 PM PT** (1:30 AM IST Nov 19). Internal target: **Nov 16**.
- Rules: https://start-developer-competition-26.devpost.com/rules
- Tracks: Entertainment, Gaming, Productivity; divisions New / Adapted. Special awards: Social, Agentic Interaction, Reason to Come Back, First Five Minutes, Accessibility, Boldest Original Concept, Judge's Choice.
- Judging 25% each: Innovation, Experience Design, Technical Implementation (60 fps+, hands, gaze ISDK v207+, passthrough, MRUK), Polish & Presentation (video < 3 min, real gameplay).
- Hard rule: fully usable with hands. No brands, no real people, original IP only. Entering solo; one entry per person.

## Mugesh's constraints
- Full-time job; Goa trip Oct 28 – Nov 2 (no work). Relies on Claude (chat) and Claude Code for nearly all code and assets; he tests in the headset.
- Uses his personal Claude account and own machine for this project (keeps IP clean from employer).

## The game (decided)
- Seated, hands-first MR creature-collector. A ~70 cm floating island anchored on a real table (MRUK).
- Loop: explore → tall-grass encounter → short battle → capture flick → collection shelf.
- Movement: thumb microgestures (hand resting on armrest; lap may be outside tracking, test day 1).
- Battle: 1v1, 2–3 moves, four attack templates (shoot, lunge, slam, shield) recoloured per element. Moves chosen through diegetic 3D runes (gaze + pinch), not floating 2D buttons.
- Capture pod: glass bubble with brass band. Not a red/white ball.
- Creatures: original designs only. Familiar elemental archetypes are fine; homages to specific Pokémon are not (e.g. avoid an electric rodent with a lightning tail).
- Names are placeholders: Emberpup (fire fox pup), Jellop (water jelly), Breezle (wind bird). App name TBD.

## Art pipeline (decided)
- Toon shaded, texture-free. Creatures are SDF recipes (smooth-union shapes) meshed with marching cubes at edit time.
- No bones or skinning. Bodies breathe/jiggle in the vertex shader; ears, tails, wings are rigid pieces on pivots.
- Shader-painted patterns (belly, socks, tips, spots), inverted-hull outlines, eye shader that looks at the camera.
- Grass: one instanced mesh with wind and push-away. Map is a data grid; props instanced.
- Island to be softened to match creatures (rounded tiles, puffy SDF foliage, outlines).
- Reference prototype: creature-diorama-v2.html (three.js) — encounter, battle, capture, shaders, recipes.

## Workflow
- Gemini: concept sheets (front/side views, palette, which parts are rigid) and naming ideas.
- Claude chat: design decisions and browser prototypes for new creature recipes and effects.
- Claude Code (in the Unity project, guided by CLAUDE.md): port to Unity — C# editor mesh bakers, URP shaders, Meta SDK wiring.
- Mugesh: headset testing 30–60 min/day, feel and cuts.
- Once a feature is in Unity, Unity is the source of truth; the browser prototype is only for new ideas.

## Setup
- Unity 6.3 LTS, Universal 3D template, Meta Quest build profile, OpenXR + Meta XR feature group, Meta XR SDK v207+ (Core, Interaction, MRUK, Simulator). Project Setup Tool: Fix All.
- Next step: Claude Code runs the audit prompt (no changes), Mugesh brings the fix list back.

## Open questions
- Scrolling "treadmill" map vs whole island visible at once (leaning: whole island first, scrolling only if the map grows).
- App name and final creature names.
- Gaming vs Productivity track: Gaming.
