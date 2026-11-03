# Step 14d report: Mugesh's Nov 3 evening notes on 14c

Headset test pending for all of it. Tests 259/259. No Play runs (XR init on). Setup already run and saved:
**Tools > Pocket Isle > Setup > First Minute (14) > Set Up MR_Main (14d Nov 3 evening notes)**.
If you didn't restart Unity after 14c: restart once before Play (warm-spot shader array 8 -> 12).

| Note | What changed | Commit |
|---|---|---|
| Character faster | 0.28 s a tile (was 0.4), running 0.15 (was 0.2) | `b68fb59` |
| Gold path not showing | Every realm's arrival moment sits by the portal, so the trail showed nothing on arrival and only came back after 12 s standing still. It now shows 2.5 s after any hand moment is solved, and the quick slot uses the same trail (its old tile flash, which needed the gift, is gone) | `b68fb59` |
| Shrine too near the lessons | Locked until the tutorial is over: "We'll rest there later. Let's find a friend first!" | `b68fb59` |
| No gift | The Spirit just sends you to the wind gate (`quick.gate`). Galeowl is 4 levels easier in the quick slot only (Lv 4); you still have the tutorial's Toastling | `b68fb59` |
| Portal signifier / friction | A gazed portal glows ~2x brighter and pulses. Walking up onto a gate's front tile heading in, or stopping on it, goes through (not on the tile you arrive on) | `b68fb59` |
| Name card | Centred over the island, 25 cm up (it sat low at the back: behind Cinderwood's trees). It waited for the pinned objective (10 s late in Cinderwood / Lilymere); now only for the arrival line. New sounds `card.rise` / `card.sink` (synth, baked) | `b68fb59` |
| Boards | Gate signs sit on top of each gate prop's own mesh (the Galewick sign was over its arch). Cinderwood: charred planks, fire in the seams, burnt corners, flame tongues | `3e8047b` |
| First swipe lesson | No gold tile: swipe any way until the explorer has really walked 4 tiles (dots). Then "Swipe twice quickly to keep walking. Then tap your thumb to stop!" (done on a tap stop, or 3 walk-ons). The turn lesson is gone | `7482b9c` |

## Quick Journey without the gift (`QuickJourneySim.Keeper`, 1000 fights)
Party: starter + the tutorial Toastling (Lv 3) + the first Galewick catch half the time.

| Offset | Galeowl Lv | Kilnpot | Pebbloop | Pufflet |
|---|---|---|---|---|
| 0 | 8 | 51% | 1% | 4% |
| -2 | 6 | 98% | 11% | 22% |
| **-4 (set)** | **4** | **100%** | **64%** | **90%** |

Pebbloop (water vs wind) stays the hardest; a loss respawns at Galewick's shrine.

## Headset test list
1. New quick game: the walk lesson: swipe left / right, dots fill as the explorer walks; then swipe twice and tap: it stops and the lesson ends.
2. Look + pinch near the shrine during the lessons: the Spirit says "rest later", you stay outside.
3. After the catch lesson: "Bring back the wind shard!", the trail goes to the wind gate. Walk up to the gate: through, no need to bump.
4. Galewick: the card rises in the middle, with a sound. After each moment (clouds, mill, kites) the gold trail shows the next.
5. Galeowl at Lv 4 in the quick slot.
6. Hub gate signs: above the arches, not over them. Cinderwood's card.
7. Walking speed feels right?

Note: CLAUDE.md still says walk 0.4 s / run 0.2: update it if you keep the new speed.
