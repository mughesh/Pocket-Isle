# Step 5d: movement rework + polish from the Oct 24 headset test

5b (props + Cinderwood theme) and 5c (Lantern Spirit + dialogue) passed on the headset: **commit them first and move
the `pre-goa` tag to that commit.** Then this step. Feedback from Mugesh:
- Hold-to-walk keeps stopping by accident when the thumb rests near the centre. Holding the hand up in view the
  whole time is tiring. Walking speed needs changing.
- Effects jitter **only while the explorer walks** (hot spring steam, flicker, everything); fine when standing.
- Move runes turn too far toward the head (~45° into the island); hard to poke.

## 1. Walking: three ways, one path system
1. **Swipe auto-walk is the default again** (the old AutoWalk): a swipe walks until a tap, an obstacle, the map
   edge or a new swipe; the same swipe again = run. **Remove thumb-hold tracking from the default** (keep the
   HoldToWalk mode selectable in the Inspector, off; turn off ThumbHoldProbe in MR_Main).
2. **Speed:** walk 0.4 s/tile, run 0.2 s/tile (exposed).
3. **Tile pathfinding:** `ExplorerPath.GoTo(tile)` = A* over walkable tiles (CanWalk), 4-direction steps, max ~40
   tiles; walks the path at walk speed (run if the target is > 8 tiles away). Any swipe or tap cancels it.
   The window scrolls as usual while following a path.
4. **Touch a tile to walk there (no colliders).** Every frame, convert each index fingertip to island-local space,
   then to a map tile through the window (inverse of `TileToLocal`, works zoomed or not).
   - Within ~3 cm above the surface: show a soft ring on the hovered tile (green = reachable, grey = not).
   - **Dip** the fingertip to the surface (≤ 5 mm above the cap) and lift = go to that tile.
   - Ignored when: in battle, the hand is grabbing or near the rim (grab guard), a bubble or rune is being poked,
     or the fingertip moves fast (> 0.6 m/s, it's a reach or a flick, not a touch).
5. **Look at a tile + pinch to walk there** (for reclined players, island far away): head-gaze ray onto the island
   top (same reticle as the runes), hovered-tile ring, pinch = GoTo. Priority: bubble > runes > tile. Not in battle.
   Make sure a pinch doesn't also fire a microgesture tap on the same hand.
6. A small target marker on the destination tile until the explorer arrives.

## 2. Jitter while walking
7. Find why the content jitters only while scrolling. Likely suspects: the scroll offset / window origin is set in a
   different update phase than the instance matrices or shader globals (one-frame lag between them), the steam and
   other particles simulating in world space while the map slides under them, or timing differences between
   `Update`, `LateUpdate`, the grab transformer and `beginCameraRendering`.
   Fix it so **all per-frame view state (window offset, zoom, clearing, matrices, globals) is computed once, in one
   place, right before rendering**, and particles simulate in the island's local space. Verify on device at 72 fps
   while walking, running and following a path, also while grabbing and moving the island.

## 3. Battle runes
8. Rune row faces the player at battle start (head direction projected on the island), then only drifts slowly,
   **clamped to ±15°** from that. No full look-at.

## 4. Clean-up
9. Hide the debug world-space labels (gesture log, thumb probe text, placement info) by default; a dev toggle
   (Inspector bool + 5 quick left-thumb taps in dev builds) shows them.
10. Keep the orange test cube as the explorer for now (the real explorer comes later), but make it smaller (~60% of a
    tile) so it doesn't hide creatures.

## Housekeeping
- One compile batch per section. Don't commit until Mugesh tests. Update `.claude-handoff.md`.

## Test on the headset
1. One swipe: the explorer walks until you tap or it hits something. Same swipe again: runs. Resting the thumb
   doesn't stop it.
2. Hover a fingertip over the island: a ring follows the tile under it. Dip and lift on a far tile: it walks there
   around obstacles. Reaching across the island to grab the rim doesn't make it walk.
3. Lean back, look at a tile and pinch: it walks there.
4. Steam, flames and grass don't jitter while walking, running or moving the island.
5. Battle: the runes stay put (little drift) and are easy to poke.
6. No debug text floating around.
