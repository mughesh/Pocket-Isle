# Step 2b part 2: island handling

Read CLAUDE.md, Assets/Docs/BuildPlan.md (v3) and Assets/Docs/GameDesign.md first. The game is now Pocket
Isle: a big world seen through the floating island, battles happen on the island zoomed in. This step only
does how the player places and holds the island. Use Interaction SDK v207 grab APIs (confirm with the Meta
tooling); no XRI.

## Build
1. **Rim grab to move.** One hand grabbing the rocky rim/base (not the play surface) moves the island with
   that hand, keeping the grab offset. Hover feedback: the rim brightens slightly when a hand is close.
   The top surface must never start a grab, so reaching or flicking over the island doesn't move it.
2. **Two-hand turn and resize.** Both hands on the rim: yaw only (no roll), uniform scale clamped
   0.5×–2×, scaling around the midpoint between the hands.
3. **Tilt toward the head on release.** Ease (~0.4 s) so the island's top faces the head, pitch clamped
   0–40°. Recomputed on every release, not continuously.
4. **Soft shadow on the real surface below** using MRUK: raycast down from the island, draw a soft blob
   shadow decal on the surface it hits (floor, table, bed). No scene data → no shadow, log it, keep playing.
   Table snap is NOT in this step.
5. **Gesture guard.** Microgestures are ignored on a hand that is grabbing or within ~5 cm of the rim.
6. **Replace** the old two-hand-pinch-for-1-s re-place with: a palm-up hand held still for 1 s shows a small
   "bring island here" button above the palm (pinch to confirm). Keep R in the Editor.
7. Keep `HoldToWalk` as the default movement mode; leave AutoWalk switchable.

## Housekeeping
- Delete `Assets/_Recovery/` and `Scripts/Editor/SceneDump.cs` (approved by Mugesh).
- One compile batch. Don't commit until Mugesh tests. Then commit this step plus BuildPlan.md, GameDesign.md,
  Prompts/gemini-01-concepts.md, Concepts/gemini-01-output.txt and this prompt. Update `.claude-handoff.md`.

## Test on the headset
1. Grab the rim with one hand: island follows, keeps its offset. Release: it tilts toward your head.
2. Reach over the grass and flick: island does not move.
3. Both hands on the rim: turn and resize; can't flip it upside down or go below 0.5×.
4. Lean back in a chair, place the island at eye level: the top faces you.
5. Shadow appears on the floor/table under the island and follows it.
6. Palm up for 1 s, pinch the button: island comes back in front of you.
7. While holding the rim, thumb swipes on that hand don't move the explorer.
