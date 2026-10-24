# Step 5 fixes: battle stage + ISDK poke

Step 5 is committed (cd9feb7). Headset test (Oct 23): zoom is perfect, but the battle happens inside tall grass
and the grass hides the creatures. Poking the move runes does nothing. Decisions from Mugesh:
clearing + pads, Pokémon framing, ISDK poke.

## 1. Battle stage: clearing + pads
- **Placement.** Work out "toward the player": the head position projected into island-local XZ. Your pad goes
  on the encounter side nearest the player, the wild pad 2–3 tiles farther away on the same line (snap to
  tiles, stay inside the map, avoid water/trees/rocks for the pad tiles; fall back to the best free pair).
  Zoom pivot = midpoint between the pads; pick the zoom factor so both pads, the HP bars and a small margin fit
  inside the footprint (around ×2.5–3).
- **Framing (Pokémon style).** Your creature stands on the near pad with its back to the player, facing the wild
  one. The wild creature stands on the far pad facing the player and your creature. The explorer stands just
  behind your pad, to one side.
- **Clearing.** During the zoom, inside a soft ellipse covering both pads (+~1 tile): grass blades bend outward
  and shrink to ~20% height (extend the existing grass push to a "clearing" uniform: centre, radii, strength);
  tall-grass caps there look like short grass. Trees and rocks inside the clearing, or between the head and the
  pads, sink into the ground like edge tiles do. Everything returns after the battle.
- **Pads.** Two round earth discs (slightly domed, toon outline) rise from the ground over 0.4 s under each
  creature, ~0.8 tile wide, a touch above ground. Habitat tint: grass pad (earth + grass ring), water pad
  (sand + wet ring) for pond encounters. Make them a reusable prefab; sink after the battle.
- **Focus.** Content outside the clearing darkens/desaturates ~25% (one global shader value); the rocky base and
  rim stay normal. Fade in with the zoom, out with the zoom-out.
- Runes stay above the near rim and must never cover your creature or the wild one from the head's view; nudge
  the row down/closer if they overlap. HP bars: wild one above its head, yours above your creature, both facing
  the head.

## 2. Move runes: ISDK poke
- Replace the hand-rolled poke in `BattleRunes.cs` with Interaction SDK v207: each rune gets a
  `PokeInteractable` with a `PlaneSurface` + `ClippedPlaneSurface` (bounds = the plate), a press visual (plate
  moves in on press, springs back), and select → `Pick(i, "poke")`. Confirm the APIs/samples with the Meta
  tooling; follow ISDK's poke button sample setup.
- Check the Interactions Rig still has `HandPokeInteractor` on both hands, enabled (the locomotion disable step
  must not have removed them). Log poke hover/select once each so we can see it works.
- Runes keep facing the head; make sure the moving surface still works with ISDK poke. Plate at least 3 cm wide
  at the island's default scale.
- Keep gaze + pinch exactly as it is. Keep keyboard 1–4 in the Editor.

## Housekeeping
- One compile batch. Don't commit until Mugesh tests. Update `.claude-handoff.md`.

## Test on the headset
1. Walk into tall grass: on zoom, the grass parts in a circle, two pads rise, the rest of the island dims.
2. Your creature is near you, back to you; the wild one faces you. Nothing blocks the view.
3. Poke a rune: it presses in and the move plays. Gaze + pinch still works.
4. Battle ends: pads sink, grass and trees come back, dim fades, zoom out.
5. Pond encounter: sand pads, same flow.
