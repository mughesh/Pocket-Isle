# Step 9i: Hearth Lodge door on the back wall

Read CLAUDE.md, `.claude-handoff.md` (7b lodge, 9h `LodgeRoom.AimedOut`), `LodgeRoom.cs`, the lodge map + props setup.
Decided Oct 26 night. Small step: do it, test in the Editor, commit. Before Play over Link, ask Mugesh to restart Unity.

**Why:** the door arch sits on the near edge (row 7, `doorOutward` (0,1)), right where the hands go for the rim pods
and the island grab, so players leave the lodge by accident. Hands never go to the far wall.

1. Move the door to the **back wall** (row 0 side): two door tiles, `doorOutward` (0,-1), clear of the honeycomb
   (`honeycombCenter` ~ (1.5, 0)) and the hearth; pick the spot that looks right from the default view and say which.
   The arch now faces the player (better read). Close the near-edge gap with the same wall/rim style as the other sides.
2. Entering from outside: the explorer appears on the tile just inside the back door and walks one tile toward the
   player (facing them). Spawn rule otherwise unchanged (never on the door, pedestal, or a taken tile).
3. Exits unchanged otherwise: a move aimed out through the door (swipe toward it, tap or look + pinch on a door tile)
   or look + pinch on the arch. Update `LessonData`/ghost and Dialogue targets that point at the door (`lodgeDoor`),
   the lodge guide's toss target if it used the old door side, and anything in the hub that assumed the old facing.
4. Re-check the cozy tiles, the pedestal (4,5) and the Islings' wander area still work with the new wall; move cozy
   tiles that now block the walk to the door.
5. Old saves inside the lodge load fine (explorer on a valid tile).

Housekeeping: EditMode tests green (door tiles, AimedOut with the new outward, spawn inside the back door). Commit
"(headset test pending)", no AI attribution, don't push. Update `.claude-handoff.md`, GameDesign.md (lodge layout).
Shot `Logs/BattleShots/9i_lodge.png` from the default view.

## Test
1. Enter the lodge: the explorer comes in from the back and walks toward you.
2. Grab rim pods, grab the island, reach around the near edge: you never leave by accident.
3. Swipe toward the back door, or look + pinch the arch: you leave.
