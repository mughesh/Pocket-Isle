# Step 2: on the headset (pulled forward from the plan)

Read CLAUDE.md and Assets/Docs/BuildPlan.md. Step 1 is done: the Diorama scene builds the island.
Goal now: the same island running on Quest 3 in passthrough, sitting on my real table, plus a quick
test of thumb microgestures. This is our biggest technical risk, so we check it before more art.
Confirm every Meta API against v207 with the Meta VR CLI or agentic skills before using it.

Build:
1. New scene `Assets/_Project/Scenes/MR_Main.unity`. Use Meta Building Blocks where they exist
   rather than hand-made rigs: camera rig, passthrough, hand tracking with the Interaction SDK rig,
   and MR Utility Kit. Hands only; controllers optional.
2. Project settings for this (list them and wait for my yes before applying): hand tracking on
   (high frequency), passthrough on, scene support for MRUK, anchors, Android permissions MRUK
   needs. Use the Project Setup Tool where possible.
3. `IslandPlacer`: after MRUK loads the room, find the best table surface near the user and place
   the island root centred on it, flat, slightly above the surface. Fallback when there is no table
   or no scene data: place it 45 cm in front of the user, about 30 cm below eye height, facing them.
   Expose island scale (start at 0.6 so it's about 42 cm wide on a desk) and height offset.
   Add a simple way to re-place it (for example, a two-hand pinch-and-hold, or a small button near
   the island that you poke).
4. Microgesture test: use the Interaction SDK / OVR microgestures (thumb swipe left, right,
   forward, back, and thumb tap on the index finger). Show the last gesture and which hand on a small
   world-space label next to the island, and move a placeholder cube one tile per swipe on the
   island grid so I can feel the mapping. Tap makes it hop.
5. Keep the Editor workflow working: the scene should also run in Meta XR Simulator, with keyboard
   arrows as a fallback for the swipes.

Done means: Build And Run to my Quest (I'll have it connected by USB). I see passthrough, the island
on my table or the fallback spot, grass swaying, and the cube moving with my thumb.
Tell me exactly what to do on the headset (permissions, space setup), what to test, then commit.
