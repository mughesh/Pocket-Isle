# Overnight run, Oct 27: Galewick

Prompt: `Prompts/overnight-oct27.md`. Everything tested in flat Editor Play through the bridge and EditMode tests (155/155).
**Nothing tested with real hands or on the headset.** No pushes. The wind gate is **sealed in builds** (`GalewickConfig.open` = false).

## Done / partly / not started

| Step | Section | State | Commit |
|---|---|---|---|
| 10a | B look + props + theme | done | 40de659 |
| 10a | A map, zones, landmarks, gate, dev panel | done | 22015ef |
| 10a | C levels by shards held + sim | done | 70718ba |
| 10b | A mill flick (Wheel piece, drawbridge, gear, toy small mills) | done, flat-checked | 5ebe422, 80757c9 |
| 10b | B kites into the room | done, flat-checked | 5ebe422, 80757c9 |
| 10b | C gust + cloud bank | done, flat-checked (ripple not judged by eye, see below) | 5ebe422 |
| 10b | D tower door | done, flat-checked | 5ebe422, 80757c9 |
| 10c | wind roster (10) + Nimbulpaca + Riverloom, tables, first-meet lines | done (XP trail check not done) | e324d05 |
| 10d | part one: Galeowl model, species (wall breaks to Fire), moves, lines, Keeper sim | done | dbf1ad7 |
| 10d | part two: tower hall, untangle, battle hookup, victory, wind shard, restoration, statue socket | **not started** (see "Blocked") | |

## Test first in the morning (headset, over Link: restart Unity first, tick Standalone "Initialize XR on Startup")
1. **Dev panel > Go to > Hub: wind gate**, walk in: Galewick arrives, "it's never this quiet", nothing sways.
2. **GW: big mill**: flick the big mill's sails with a fingertip, 3 flicks within 4 s. They keep turning, the gear turns, the drawbridge swings down over the ditch. Also try look + pinch on the sails (one flick each). Small mills spin for fun.
3. **GW: kite field**: pinch a kite and pull it up about 15 cm above the island, let go: it flies above the island on a string to the explorer's backpack. Try one let go low (it drifts back). Fly all three, walk: they follow and fan out.
4. **GW: ridge**: sweep an open flat hand fast over the island. Look for the ripple through the grass, white streaks, mills twitching. The three puffs on the path thin and drift away (about 3 sweeps). Look + pinch-hold a puff also blows it away slowly.
5. **GW: tower** with three kites: the kites fly up onto the crown, the vanes spin, the door sinks open. With fewer kites: "The tower needs all three kites."
6. **Story tab > Galewick restored: on**: grass waves, every mill turns, kites fly on posts, seeds drift.
7. A battle in Galewick with kites flying: they lift higher and back out of the battle view.
8. **Encounter tab**: fight Kitelet, Whirlby, Samarling, Vanehare, Zeppelope and their evolutions; check their spinning
   pinwheels / rotors / propellers and Kitelet's bead tail on the headset. Evolve a Pufflet and a Pebbloop at Lv 14.

## Feel numbers to tune (Inspector, `Hand Puzzles/Galewick/...`)

| What | Value | Where |
|---|---|---|
| Flick speed that counts | 0.5 m/s | Big Mill > WheelPiece > Min Flick Speed |
| Flick zone: distance from the sails' plane | 2.2 cm (+1.5 cm radius slack) | Near Plane / Slack |
| One flick turns | a quarter turn, drag 1.5/s | Drag (WheelSpin.flickTurn in code) |
| Flicks to solve / window | 3 in 4 s | Flicks Needed / Flick Window |
| Mill speed once solved | 2.2 rad/s | Keep Speed |
| Kite lift to fly | 15 cm above the island top (island-scaled) | Kites > Lift Threshold (prompt fallback: 0.08) |
| Kite toss counts at | 0.6 m/s upward | Toss Speed |
| Kite grab radius | 1.1 tiles + 2 cm | Grab Radius / Grab Slack |
| Kite flying height / step / spread | 30 cm / +8 cm each / 15 cm apart | Fly Height / Fly Step / Fly Spread |
| Flying kite size | 2.4 tiles | Flying Scale |
| Kites in battle | +16 cm up, 14 cm back | Battle Offset |
| Gust speed | 0.6 m/s (full strength at 1.6) | Gust > Min Speed / Full Speed |
| Gust height above island | 0-30 cm | Max Height |
| Open hand: fingertips from palm | at least 7 cm | Open Reach |
| Gust cooldown | 0.3 s | Cooldown |
| Ripple crossing time / band width | 0.75 s / 7 cm | Cross Seconds / Band Width |
| Cloud thinning per gust | 0.36 (about 3 gusts) | Cloud Bank > Per Gust |
| Gust hits a puff within | 20 cm of the sweep line | Corridor |
| Look + pinch-hold thinning | 0.4/s (about 2.5 s) | Hold Rate |
| Unrestored stillness | 0.92 (8% idle sway) | Data/Props/Themes/Galewick > Still When Unrestored |
| Wind grass | blades x1.3 tall, x0.72 thin | Galewick theme > Blade Height / Width |

## Decisions I made (without asking)
- **No tile heights.** "Rolling hills" are hillocks: the Galewick theme's tree prop is a grassy dome with wheat tufts, plus a meadow tree (the hub's calm tree recoloured). Real height steps would touch walking, touch/gaze picking, battles and every puzzle.
- **Cloud puffs are opaque** toon blobs (no translucent edge; the prop shader is opaque).
- **Wind gate**: the hub's gate data links to Galewick; `ShardGates` keeps it sealed in builds through `GalewickConfig` (`open` false, `openInEditor` true). Re-running the 9a setup no longer re-seals it.
- **Drawbridge spot**: the ditch's 2-tile crossing is path under a blocking Drawbridge prop. While it's up, a strip of water is drawn over the crossing so it doesn't read as a way across.
- **Cloud bank**: the 1-wide ridge path with three puffs in a row. A gust swept along the path thins all three at once, so three good sweeps clear it. If that feels too easy, lower Corridor or Per Gust.
- **Ghost clips**: mill = `grab_flick`, kites = `grab_toss`, gust = `palm_hover` (no sweep recording exists; record a "palm_sweep" and set the Cloud Bank's Ghost Clip).
- **Lessons stay pinned** (chip) until the moment first works (first good gust on a puff, first kite flying, the mill solved), not just until first touch. `HandPuzzle.lessonChip` turns that on; Cinderwood's pieces don't use it (unchanged).
- **Shard scaling**: wild +3 per shard held (hub excluded), Keepers +2 per *other* shard. Hearthstag at 1 shard = Lv 12.
- **Flying kites** are the lying kite stood up and grown 2.4x (one prop per colour). Strings are one camera-facing ribbon mesh for all three (URP Unlit, KiteString.mat).
- Wind tower: the closed door is a wooden door now; once open the island draws `WindTowerOpen` (dark doorway) and the door leaf sinks. The doorway leads nowhere until 10d (logged once).

## Blocked or reverted
- Nothing reverted.
- **10d part two not started on purpose.** The Keeper hall, shard holder and statue socket are written around
  Hearthstag and the fire shard (`KeeperHall.KeeperId`, `ShardHolder.FireId`, one cup, the fire socket). Galeowl needs
  them generalised (per-Keeper ids, a second cup, the wind socket), which touches Cinderwood's Keeper flow, the one you
  already tested on the headset. Doing that unattended felt too risky. Plan for part two: (1) make KeeperHall / ShardHolder /
  StatueSocket take the keeper id, shard id and cup index as fields (defaults = today's fire values); (2) a Tower Hall
  interior (ForgeHallTools-style); (3) an Untangle piece (3 knots, two ends each, pull apart > 12 cm, one hand > 15 cm,
  look + pinch = 2 s); (4) wire TowerDoor.hall; (5) restoration wave from the tower; (6) then decide on `GalewickConfig.open`.
- **XP trail for Galewick (10c item 4) not run**: KeeperSim.Trail walks Cinderwood's beats only. With Galewick's tables
  at Cinderwood's levels (+2) and the shortest trail about as long, expect roughly the same (~Lv 8-9 at the tower, 0 shards).
- First flat mill test: three bridge-sent flicks landed 1.5 s+ apart (bridge latency), so only 2 fell inside the 4 s window. A one-call triple flick solved it. With real hands, 3 flicks in 4 s is easy, but watch for it.
- The grass ripple band was not visible in a still shot taken just after a gust (it crosses in 0.75 s). **Judge it in motion.** If it's weak, raise the brighten (Grass.shader, `float3(0.32, 0.3, 0.22)`) or the band width.

## 10c / 10d notes
- New Islings (all 8.6-10.2k tris incl. parts and face, like the fire roster): Kitelet -> Kitewyrm (Lv 16),
  Whirlby -> Windwhirl (16), Samarling -> Samaroak (16), Vanehare -> Compasshare (16), Zeppelope -> Skyshiplope (18, rare);
  Pufflet -> Nimbulpaca and Pebbloop -> Riverloom at Lv 14 (as Kilnpot -> Kilnhearth). Moves: Ribbon Lash, Pinwheel Burst,
  Seed Copter, Vane Dart (arc), Prop Wash (all Wind) + a Neutral move + a support. Starter evolutions keep their base's moves.
- Galewick tables: ~72% wind, ~28% fire visitors (the fire patch is mostly fire), Zeppelope 5% rare in the rare pocket.
  First-meet lines `gw.meet.kitelet` / `gw.meet.zeppelope`.
- Galeowl: Lv 9 (11 at one other shard), HP base 120, atk 9, def 10. Sim, party = starter + Toastling + a wind common:
  Lv 9 party wins 74% (Kilnpot 100% because Fire breaks the wall, Pebbloop 62%, Pufflet 59%); Lv 8 only 31% (a cliff like
  Hearthstag's). Fire helper matters: the fire patch is right by the village.
- Look notes: the taller wind grass (x1.3) looks very tall and wispy at battle zoom. If it bothers you, lower the Galewick
  theme's Blade Height to 1.15.

## Shots (`Logs/BattleShots/`)
- 10a: `10a_map.png` (labelled top-down), `10a_<arrival|village|mill|kites|ridge|tower|fire|rare>[_restored].png`, `10a_gallery*.png`, `10a_play_arrival.png`, `10a_play_tower_restored.png`, `10a_play_kites_restored.png`.
- 10c: `10c_<name>.png` (12 turnarounds), `10c_lineup.png` (all with Kilnpot / Kilnhearth for scale; the row reads
  right to left), `10c_battle_kitelet.png`. 10d: `10d_galeowl.png`.
- 10b: `10b_mill_lesson.png`, `10b_mill_bridge_down.png`, `10b_kites_flying.png` (the hero shot), `10b_kites_close.png`, `10b_clouds.png` (lesson chip + palm ghost), `10b_gust_mid.png`, `10b_tower_open.png` (kites tied on the crown).

## Perf (edit mode, island window, triangles incl. outline passes)
| Spot | Draws | Tris |
|---|---|---|
| arrival | 26 | 148k |
| village | 25 | 91k |
| big mill | 30 | 105k |
| kite field | 26 | 60k |
| ridge | 20 | 185k |
| tower | 20 | 223k |
| fire patch | 18 | 171k |
| rare pocket | 18 | 206k |

Cinderwood was 18-30 draws, 126-232k. The hillock budget was cut to 520 tris after the first numbers (the tower and rare spots will be a bit lower than shown). 10b extras near the moments: big mill +6 draws (mill, gear, bridge or water), kites +7 (3 kites x2 + 1 for all strings), cloud bank +6, tower +4 while opening. No device capture.

## QA APK
`Builds/PocketIsle-QA.apk`, rebuilt at the end of the run with 10a-10c and Galeowl's data; **wind gate sealed**
(`GalewickConfig.open` = false). Galewick is reachable through the dev panel only. **95.6 MB, built 03:51.**
The build stopped once on a "Scene(s) Have Been Modified" dialog (in-memory scene changes from my Play runs); I answered
Don't Save, so the build used the committed scene.
Install as in QA-Guide section 1 (not installed: the headset may be asleep).
