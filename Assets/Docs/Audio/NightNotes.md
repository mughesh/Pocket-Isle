# Audio lane night notes (step audio-B)

Progress log so a cut-off session can pick up. Newest at the bottom.

- Oct 27 01:25: Part 1 done (`efcf20a`). Every non-cry patch re-baked to tiers; Kenney via `Clips/Levelled/`; cue volumes 0.8. AudioScope 165 -> 71 failing.
- Oct 27 01:37: Part 2 done (`b87235e`). Models: modal, particles, bubbles, fire, wind, friction, breath; layer groups; scatter; Lab preset row; tests 4/4. Gallery: `Temp/audio-analysis/Models.png`.
- Oct 27 ~02:00: Part 3 remakes in `Editor/SynthRemakes.cs` (overlay on batch 1). Mallet exciter changed to a no-null soft pulse (a half-sine nulled 300-400 Hz bodies). Imports mostly sub get a 180 Hz high-pass in their levelled copy. AudioScope 230 clips, 19 failing: all synth P0/P1 pass; left = amb.lodge / amb.pond (Part 5) and bassy Kenney sources (walk.bump, move.slam, step.wood).
- Bridge entry points: `exec:Diorama.Audio.EditorTools.StepBTools.Remake` (create, re-apply, bake, sync, analyse), `.ModelGallery`, `exec:Diorama.Audio.Tests.AudioTestRun.Direct`.
