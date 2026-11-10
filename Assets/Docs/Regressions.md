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
| R19 | The battle moves sit over the arena, never in front of the rim; from the seat they hide no Isling, HP label or pod, and stay about 4 cm from the pods. | Nov 9 notes | `Step24Debug.Seat` in a field and a Keeper battle |
| R20 | A door never refuses silently: a bump always answers (line or Go in / Not yet, 1 s debounce at most) and the log says why. | Nov 9 notes | `Step24Tests`; log `[TempleDoor] refused:` |
| R21 | Every way into an open door works: walking in, look + pinch, a fingertip. The chip sits beside the door, never over it. | Nov 9 playtest | `Step27Tests`; smoke `RunOnly` "Step27 shots"; headset for real hands |
| R22 | The Spirit moves when its target changes, not per line: at least 5 s parked, glides 0.6 s, doesn't follow the head. | Nov 9 playtest | `Step27Tests` (SpiritPark); headset |
| R23 | Filler and idle hints: none in lessons, hand moments, battles or the first 3 minutes. | Nov 9 playtest | `Step27Tests` (ChatterQuiet) |
| R24 | No tall grass within 1.5 tiles of any door, or 2 rows toward the seat. | Nov 9 playtest | `Step27Tests` on the real maps |
| R25 | Don't import `Script_v1.md` over the dialogue asset: the asset has lines the file lacks. | Nov 9, step 27 | by hand |
| R26 | Anything drawn with no depth test (battle labels, pops) hides while the menu, Settings or the Journal is over the island. | Nov 10 playtest | SmokeRun `MenuOverBattle` |
| R27 | Every starter also lives in the wild, in its own element's realm. | Nov 10 playtest | `FireRosterTests.Starters_AlsoLiveInTheWild` |
| R28 | During the walk lessons and the tap practice the explorer never stops by itself, crosses road edges, walks at 65% speed, and nothing on the island reacts to it. | Nov 10 playtest, step 28 | `Step28Tests`; smoke "lesson walk" (4 directions, 20 s each); headset |
| R29 | The swipe-and-tap lesson passes only on a real thumb tap (or a 60 s wait). No gold tile that stops the explorer in that lesson. | Nov 10 playtest | `Step28Tests`, `Step14fTests` |
| R30 | Both tap lessons (far, practice) show the swipe and then the tap, every time. | Nov 10 playtest | `Step28Tests` |
| R31 | From any seated head position the moves, the battle line and the name labels never overlap. | Nov 10 playtest (2nd time after step 24 moved the row) | SmokeRun `BattleLayout` (6 heads) |
| R32 | The Spirit hovers over the active interaction, never on the island's floor. | Nov 10 playtest (step 16 had parked it beside the target) | headset; shots `layout_*` |
| R33 | While the menu is open a lesson's card is grey and off the tablets, its ghost hidden. The menu stops the game's clock: anything timed by `Time.time` freezes where it is. | Nov 10 playtest | SmokeRun `MenuOverLesson`; headset |
| R34 | Every road is at least 2 tiles wide, except the listed bridge and the road through tall grass; widening never opens a way round a blocker (log, vine, gate). | Nov 10 playtests, step 29 A0 | `Step29Tests.RealMaps_HaveNoOneWideRoadsLeft_ExceptTheListed`, `Step14eTests` (vine) |
| R35 | The swipe walk stops at real junctions (per corridor), beside doors, shrines and blooms, and at the first tall grass; never mid-corridor on a 2-wide road. A tap stops it anywhere. | Nov 10 playtests (2nd time walking was the top complaint) | `Step29Tests` corridor tests |
| R36 | Gold means "where to go next" only; the explorer's own walk marks and junction arrows are white / soft cyan. | Nov 10 playtests | headset |
| R37 | Off-turn the moves are sunk and grey, and a press only shakes: nothing is ever queued. | Nov 10 playtests | `Step29Tests`; log `pressed off-turn` |
| R38 | Every rim pod shows its HP ring, in battle or not; a hurt pod's card says where to heal. | Nov 10 playtests | smoke `RunOnly` "Step29" |
| R39 | Fishing and the pond line only from a bank, never a bridge; every realm with a river has a water table. | Nov 10 playtests | `Step29Tests.Fishing_NeverFromABridge_OnlyFromABank` |
| R40 | A hand moment never leaves the explorer or a hand locked: every lock has an unlock on every exit (done, timeout, hand lost), logged `[HandPuzzle] lock/unlock <id>`. | Nov 7 (statue), Nov 10 (ember carry): 2nd time | `Step29Tests.Fingertip_*`; log |
| R41 | A hand moment's ghost and chip show when the moment becomes usable, even if it was touched while blocked (the cold bellows). | Nov 6-8 ghost notes, Nov 10 bellows: came back | headset; log `lesson re-armed` |
| R42 | No bubble, chip, card or tablet comes nearer the head than 45 cm. | Nov 10 playtests | `Step29Tests.Text_NeverCloserThan45cm...`; headset lean-in |

## How to use
- A new note that repeats one of these: say which rule broke and why the check missed it, then fix the check too.
- A new kind of note: add a row once it's fixed.
