# Step 2b (part 1): walk + run, thumb-hold experiment

CLAUDE.md and Assets/Docs/BuildPlan.md were replaced (island floats, no table; MRUK only adds shadow +
table snap; big map seen through a scrolling island). Read both again first.

## 1. Walk + run
Keep the current behaviour: swipe sets direction and keeps walking, tap stops, a new direction turns at
the next tile, the island edge stops.
Add: a second swipe in the SAME direction while walking switches to run.
- Walk 0.3 s/tile, run 0.15 s/tile, both exposed on the component.
- Tap stops from either state.
- Label shows Walk/Run + direction.

## 2. Thumb-hold experiment (debug only, does not drive movement yet)
Each frame, read the ISDK hand joint poses for the hand that does microgestures. Compute the thumb tip's
offset from its rest point, projected onto the index finger's axis (along the finger) and its side
axis, in mm. Show both numbers and a small bar on a world-space debug label near the hand, plus the
last microgesture fired. Confirm the joint API against v207 with the Meta tooling; don't guess.

## Housekeeping
- Batch both changes in one compile.
- Yes to deleting `Assets/_Recovery/` and `SceneDump.cs` if the scene opens cleanly without them.
- Don't commit; Mugesh tests first. When he says it works, commit the walk/run work, the new CLAUDE.md
  and BuildPlan.md, and this prompt file.

## Test on the headset
1. Swipe right once: walks. Swipe right again: runs. Tap: stops.
2. After a swipe, hold the thumb there 2 s, then return it to rest. Watch the numbers.
   If held vs rest stays clearly apart (about 8 mm or more, steady, no flicker), hold-to-walk is viable.
   If it flickers, keep auto-walk + double-swipe run.
