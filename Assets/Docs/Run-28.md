# Run 28: lesson walking and the Nov 10 playtest notes

Flat Editor only. Everything below still needs the headset.

## Checks
- Tests: 405 passed, 0 failed (8 new in `Step28Tests`).
- Full smoke run: 91 ok, 0 failed. Notes: the usual opening wait, and the two "label inside a Keeper's box" notes that come and go (Run-20).
- New smoke beats: lesson walk in four directions for 20 s with no tap (45 to 46 tiles each, never stopped, nothing reacted), a tap stops it, the lesson card greys behind the menu and comes back.
- `SmokeRun.RunOnly` arg `BattleLayout`: moves, battle line and name labels clear of each other from six head positions.
- Not run: Release Check. No top-down route shot: the route is no longer fixed (free roam, targets placed ahead of the walk).

## What changed
| Note | Before | Now |
|---|---|---|
| Lesson walk speed | 0.28 s per tile | 0.43 s per tile (65%) during walk, far and practice |
| Lesson walk stops | road edge, junction, gold tile | only a thumb tap; straight on over any walkable tile, turns at walls and gates |
| What reacts during those lessons | gates, doors, shrine, tile walking, gaze hover, grass lines | nothing (`OnLessonStep` replaces `OnStep`, only the lessons listen) |
| Far lesson passes on | tap, standing on gold, two finished walks, 30 s | tap, or 60 s |
| Far lesson gold tile | yes, and it stopped the walk | none |
| Practice ghost | tap only | swipe then tap, same as far |
| Practice target | 3 to 5 tiles along the path | 4 to 6 tiles straight ahead, moves if the walk turns away |
| Trail to the grass | faint tile flashes | the Spirit's gold trail |
| Lantern | beside the target, floor height for ground targets | hovers above the target, at least 9 cm over the island |
| Battle line | at the end of the move row, hidden by props | above the wild Isling, drawn on top |
| Near name label | in front of the pad, on the moves | beside its Isling, away from the explorer |
| Explorer in battle | in front of the Run tablet | a little behind its Isling |
| Menu over a lesson | card frozen bright over the tablets, ghost frozen | card grey over the island's back, ghost hidden |
| Menu | no way to the title | Main menu tablet (saves, then the title; New journey restarts) |

Inert list, each closed by: gates (`GateTile` in the plan, `ShardGates.Pick` and stop-to-enter), doors (`DoorEntry` hover, chip, `Pick`), hearth shrine and lodge (`usable`, `Pick`, first line), tall grass (no `OnTallGrassStep`, no rustle), pond (no fishing tap), statue and landmark lines (no `OnStep`), tile walking (`GoTo`, `TileGaze`, `TileTouch`), gaze hover (`FocusHighlight.Hover`).

## Headset test list
1. From the start swipe each way, no tap: the explorer keeps walking, slower, crosses road edges, turns at walls. Log: `Lesson walk ... walks until a thumb tap`.
2. Swipe-and-tap lesson: passes only on your tap. Ghost shows swipe then tap in both lessons.
3. Practice: the gold tile appears ahead of the walk with time to tap; turning moves it.
4. After the practice: normal speed; gold trail to the grass is bright and reaches it from anywhere.
5. Battle: name, battle line and moves apart, nothing cut by props.
6. Lantern hovers over what you must do, never on the floor.
7. Menu during a lesson: card grey and off the buttons, no ghost hand; closes back to normal.
8. Menu > Main menu: title with Continue and New journey.
