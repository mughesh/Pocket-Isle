# Regression rules (from Mugesh's notes)

Every headset note that came back after a fix is now a rule. **Before changing a system, read its rows.** "Check" says what
catches a break: a test, a smoke-run rule (`Note "rule"` in the smoke report, watched the whole run) or only the headset.

| # | Rule | From | Check |
|---|---|---|---|
| R1 | No music and no island ambience while only the shard floats (first minute). Title music fades as the shard arrives. | Nov 6 notes; broken by step 20, fixed step 21 | smoke rule R1 |
| R2 | The pause menu pauses every sound (music, ambience, voices, effects); only its own UI taps play. | Nov 8 notes | smoke rule R2 |
| R3 | The menu never sits over another row: in a battle the move row and the battle line step aside while it's open. | Nov 8 (battle + menu shot) | smoke rule R3 (when a battle has the menu open), shot `21_menu_battle.png` |
| R4 | Grabbing a pod is never a move press: no rune pick (poke or gaze + pinch) while a hand reaches for, holds or just let go of a pod. | Nov 8 (log: "Grabbed Pod 2" then "Picked move 2") | headset |
| R5 | Run (and any rune) never ducks away from the finger: the row only steps aside for pods a hand can take, not the empty sockets. | Nov 8 | headset |
| R6 | A hand is never stuck on a button; a press releases at once. | Nov 6 notes | headset |
| R7 | Ghost hands play on their real target and end on it (a carry ends at its socket/spot), never cover it, never sit over the map's centre for thumb demos. | Nov 6, 7, 8 notes (statue shard ghost, 3rd time) | headset; log `[GhostHands] ... grabs at puzzle:<id>, lets go at puzzleTo:<id>` |
| R8 | The swipe ghost floats over the island's centre, upright, palm never to the face (Meta's system gesture). The walk lesson shows all four swipes, each with its arrow; the far lesson shows one swipe with its arrow, then the tap (the dot pulses). Each lesson keeps its own demo. Judge motion from a video or a stroke-by-stroke sequence with the camera at the seat, never from one frame. | Nov 6, 7, 8 notes + Nov 8 video (5th time); step 21 lost the tap, fixed step 22 | `Step22Tests` (each lesson's clip, no recording shadows it); `Step21Debug.SwipeGhost` (arg = clip) |
| R9 | Ending Islings: no two overlap, none in or behind a prop from the seat, none in tall grass; the explorer's tile stays free. | Nov 7, Nov 8 notes | `Step21Tests` (FreeSpot), `Step17Tests` (arc); shot `21_hall.png` |
| R10 | The ending is short and skippable: Skip from the Hall of Fame on; Islings pop one after another (not ~2 s each). | Nov 6, Nov 8 notes | headset |
| R11 | Credits rise from behind the island (like the step 17 banner), not out of its middle; lanterns about 14 cm, never island-sized. | Nov 8 notes | shots `21_lanterns_*` |
| R12 | Every speaker has a voice while its line types: the Spirit's blip, each Keeper's, the statue's bell. | Nov 8 notes (statue silent) | `Step21Debug.HeartVoice` (blips counted) |
| R13 | The explorer is locked while the statue speaks; the statue's offer starts only next to it. | Nov 6 notes | headset |
| R14 | A Keeper's shard floats high, never behind the hall's arch; a touch takes it. | Nov 6 notes | headset |
| R15 | Lines and their pills never cover the Isling or the pod you must act on. | Nov 6 notes | layout checker (step 14j) |
| R16 | A card opened by poking a rim pod while exploring closes on the next swipe, walk, battle or island grab. | Nov 8 notes | headset |
| R17 | Every rune row keeps its tablets apart **as seen in the room**: 1.2 cm at the island's real size (the check used 8 mm on the island, ~5 mm in the room at 0.6, so "almost touching" passed). Nothing overlaps the pods or the Journal tablet. | Nov 4, Nov 8 notes (2nd time) | smoke "menu layout" (`TabletLayout.MinRoomGap`), `Step22Tests`; `Step22Debug.MenuShot` |
| R18 | The menu never fails silently: it opens in the opening once the island is up (no Journal, no Title); while only the shard floats a soft sound says not now. | Nov 8 notes | headset; log `[GameMenu] Not now (...)` |

## How to use
- A new note that repeats one of these: say which rule broke and why the check missed it, then fix the check too.
- A new kind of note: add a row once it's fixed.
