# Run 29: Nov 10 friend playtests (two sessions) + follow-up

## State at the start (Nov 10, 22:00)
1. master = `0148c13` (step 28 + Nov 10 playtest notes), local only, not pushed; branch `step-29` made from it.
2. Run-28: tests 405/405, smoke 91 ok / 0 failed; Release Check not run; everything headset test pending (steps 22-28).
3. Rules to respect: Regressions R1-R33 (R28-R30 lesson walking, R31 battle layout, R21 doors, R24 no tall grass by doors, R25 don't import Script_v1).
4. Open from before: swipe ghost re-record (Run-27, needs Mugesh over Link), Run-24/25 headset lists (battle row, doors, pour, audio), device build pending.
5. Working tree had unrelated edits (step-23 prompt, TMP fallback font, `_Recovery`, `_lanes`): left alone, never staged.

## In one line
Everything in the prompt has code, tests and a smoke run behind it; **nothing has been on the headset**. Partly done: 9 (no other white path found), 19 (poke ghost, not open/close), 21 (no element symbol), 23 (lean-in not checked). Questions for you: the team-size number (still 4 / 6), and whether the bellows should become an open/close pump.

Commits on `step-29` (master fast-forwarded locally, not pushed): `93c44c2` A0, `8e9cffd` A, `d8ea3fe` B, `2f09acf` C + D17, `0c8210c` D18-D20 + E-I, then this report.

## Still open from before (so nothing disappears)
- **Swipe ghost re-record** (Run-27, Notes copy.md): needs you over Link (Tools > Pocket Isle > Ghost Hands > Record Ghost Clip). Not touched.
- **Run-24 headset list** (battle row over the arena, doors, the tilted pour) and **Run-25 audio listen**: still untested on the headset.
- **Device build**: none since the step-26 QA build; every step 22-29 change is flat Editor only.
- Notes.md (Oct 27) and Notes copy.md (Nov 6-8) items were all closed in steps 21-22 (R1-R18) except the swipe ghost above.

## Checks (flat Editor, no headset)
| Check | Result |
|---|---|
| Edit-mode tests | **425 passed, 0 failed** (20 new in `Step29Tests`; `PartyTests` and `Step14hTests` updated for the new odds and the finale line) |
| Smoke run (full) | **91 ok, 0 failed** (after A0; after A+B; after C+D17; after D18-I), only the known opening-wait note. One earlier run was thrown away (see below) |
| Smoke `RunOnly` "Step29" | 11 ok: rings on every pod, the hurt pod's card line, the tired warning at the tower, the bloom heals everyone, an off-turn press only shakes, seat shots. 1 note: the junction shot ended at the statue (see item 7) |
| Release Check | PASS |

**Two lessons from this run** (saved to memory): saving a .cs while a smoke run plays recompiles mid-Play and wrecks the run (one run was thrown away and re-run); editing the open MR_Main.unity on disk pops a modal that freezes the bridge (cleared by clicking Reload from PowerShell). Scene values now go through bridge methods only.

## Every item
| # | Item | Status | Evidence |
|---|---|---|---|
| 1 | A0 widen every 1-wide road | **done** | `Step29Paths` (audit + apply). 179 lane + 33 corner tiles: Still Isle 41, Galewick 84, Cinderwood 49, Lilymere 38. Shots below. Test `RealMaps_HaveNoOneWideRoadsLeft_ExceptTheListed` |
| 2 | Exceptions kept, listed | **done** | Lilymere lagoon crossing c22 r13-15 (bridge, water/bed both sides). Left one wide (both sides blocked): Cinderwood c48 r23 (trees next to a scene tile), c13-14 r50 (a landmark tile and a rock next to a blocker), Lilymere c46 r44-47 (road through tall grass: widening would eat the encounter patch). 9 lane gaps where the lane tile is a landmark/scene tile on grass (still 2 walkable wide). No lily-pad, drawbridge or frozen-falls road was 1 wide (they are props over water). Full list: `Run-29-img/A0_paths.md` |
| 3 | Path logic intact | **done** | No tile ever became unwalkable (saved positions safe). A tree/rock opens only if it joins one walkable area: the first try opened a way round Cinderwood's arrival vine, `Step14eTests.Cinderwood_VineIsTheOnlyWayOn` caught it, rule added. Door grass R24, gate fronts, landmarks, scene tiles locked. Cinderwood's tall-grass floor check 16% → 15% (the roads took ~25 trees/rocks). Smoke walks every realm's route: 91 ok |
| 4 | Top-down before/after per realm | **done** | `Run-29-img/A0_<realm>_before/after.png` (new lane magenta, corners violet, exception red frame, left-one-wide yellow) |
| 5 | Swipe walk follows bends, stops at junctions / before interactables / first grass / blocked | **done** | `SmartStep.PlanCorridor`, `GestureTestPiece.BuiltInStop` (doors: tower, forge, lake temple, hearth shrines, blooms; hand moments as before). "One tile before" = it stops on the tile beside the thing; the next swipe walks in. First grass: until a wild Isling is met (`EncounterDirector.Seen`). Lessons unchanged (R28) |
| 6 | Junctions per corridor | **done** | A side counts only where the road reaches 2+ tiles further that way than one step back. Tests: 2-wide straight, L (both lanes, keeps its lane), T, 3-wide plaza, crossroads |
| 7 | White junction chevrons | **done** (not seen) | `WalkPreview` chevrons on the open ways, 2 s or next swipe; `OpenWays` test. The flat shot `29_junction.png` caught a stop in front of the statue instead (the hub plaza widens one tile per row, which by design is not a junction; the walk stopped at the statue), so the chevrons themselves were not seen |
| 8 | Two colours | **done** | GoldMark mode 3 (chevron) + `GoldMarks.walkColours`; the walk preview is white/cyan chevrons. Trails, gold tiles, gold marks stay gold |
| 9 | First grass: one gold path | **partly** | The grass lesson already draws only the Spirit-style gold trail (step 28); the walk preview, the other trail-like mark, is now white chevrons. I found nothing else that draws a white tile path; check on the headset |
| 10 | Off-turn moves sink and grey; captions | **done** | `BattleRunes`: sink 1.6 cm + 0.8 cm back, 85% grey, no hover/sounds/gaze dot; "Wild Vanehare's turn..." for 0.7 s before the wild move (`BattleStage.theirTurnBeat`); "Choose your attack" + one gold rim pulse on rise. Shots `29_yourturn.png` (colour, "Choose your attack") and `29_offturn.png` (sunk, grey, the wild move's line). Test `Battle_TheirTurnLine_AndChooseLine` |
| 11 | Off-turn press = shake only | **done** | `OffTurnPress`, `Pick` guard (keys, poke, code); the smoke beat presses a move off-turn and checks it only shook; log `pressed off-turn ... nothing queued` |
| 12 | HP rings always, low blink + !, fainted grey + zz | **done** | `PodHpRings` + `Diorama/Pod Ring`; shot `Run-29-img/29_rings.png`; test `Rings_Colours_LowAndFaintedBadges` |
| 13 | Hurt pod card line | **done** | `PodCard.HealTip`; shot `Run-29-img/29_card.png` |
| 14 | Healing bloom in all three realms | **done** | `HealBloom` + PropKit recipes (2598 tris each). Tiles: Galewick c18 r11 (tower c17 r3), Cinderwood c19 r10 (forge c19 r3), Lilymere c24 r10 (temple c16 r10): on grass beside the road, ~6 steps before the door, no tall grass within a tile. Shots `29_bloom_open.png` / `29_bloom_healed.png`; smoke beat "the bloom heals everyone"; test `Bloom_Tiles_OnEveryRealm...` |
| 15 | Lodge heal felt | **done** (not seen) | `HearthShrines.LodgeHealMoment`: green + from the floor 2.2 s, rings refill one by one (0.3 s apart, chime each), "Your Islings are healed!"; only if someone was hurt |
| 16 | Tired warning at a Keeper door | **done** | `TempleRules.TiredCheck` (once per door until healed, bloom pulses gold 10 s, door still works); smoke beat; test `Tired_UnderHalf_OrFainted_AndCardLines`. Team line now "{keeper} is strong. Bring at least {n} Islings." **n stays 4 (Galewick) / 6** — your example said 3; say if the numbers should change |
| 17 | Ember → burning fingertip | **done** (not seen) | `EmberCarry.fingertip` (touch catches, poke lights, 20 s timeout, 1 s hand-lost), `FingerFlame` (core, body, two tongues, glow, embers, crackle that swells with motion, whoomp, hiss+flare); forge ember **and** the arrival vine. Lock/unlock: `[HandPuzzle] lock/unlock <id>` on catch, light, timeout, hand lost; the burning hand no longer marks itself held (that was what kept the walk off). Tests `Fingertip_*` |
| 18 | Log auto-drops | **done** (not seen) | `CarryPiece.autoDropOverIsland` (on for the fallen log): let go anywhere over the island after lifting = a 0.7 s glide into the nearest spot |
| 19 | Bellows ghost + chip | **partly** | Why it was missing: pokes while the firebox was cold used the lesson up. Now it re-arms when the forge wakes (`HandPuzzle.RearmLesson`), and the chip "Poke the bellows again and again" shows from the start. **The ghost is the poke ghost, not open/close:** the bellows are worked by poking the pad; an open/close hand pump would be a new input and needs a recorded ghost clip. Say if you want that |
| 20 | No fishing line on bridges; 2 Galewick banks | **done** (not seen) | `Fishing` (bridge = wet on both sides; tap and the pond line only on a bank). Galewick had **no water table at all** (why fishing never worked there): `Encounters_GwWater` (Lilymere's two water species, Lv 6-8). Banks c36 r30 and c25 r33 with ripples (`FishSpots`, shot `29_fishbank.png`). Test `Fishing_NeverFromABridge_OnlyFromABank` |
| 21 | Starters face the player, stat card on gaze | **done** (not seen) | `PodView.faceHead` (faces the head, small hop, no spin); `PodCard.ShowStarter`: name and line in the element colour, "Fire · strong vs Wind, weak vs Water", "Signature move: X". **No element symbol glyph** (colour only); test `Starter_TypeLine_AndSignature` |
| 22 | Encounter variety | **done** | Caught species x0.3 while the table has an uncaught common one; rare entries keep their own weight. Odds below; test `Encounters_CaughtWeighLess...` |
| F | Copy pass | **done** for objectives, chips and hints | 47 lines/chips, table below. Story lines kept. Verbs outside the list kept where the gesture has no listed verb: Sweep (clouds), Flick (sails, pod), Scoop/pour, Touch (the ember, as in your example) |
| 23 | Text ≥ 45 cm from the head | **done** (not checked lean-in) | `KeepTextAway` on the bubble, every rune board/menu, stone tablets (chips), pod cards, the battle line; slides back along the view ray and grows by the same ratio. Test `Text_NeverCloserThan45cm...`. The 25 cm lean-in needs the headset (flat Play has no head) |
| 24 | Heart's entrance call | **done** (not heard) | First Heart line: `spirit.blip` at pitch 0.25 (two octaves down) + `spark.chime` bell ping, once; the per-line `heart.hum` is gone; murmurs at 55% blip volume, always the old synth voice |
| 25 | Device log + 2 min in Report bug | **done** | Command below; `BugReport` keeps the last 120 s (cap 6000 lines); test `BugReport_KeepsTheLastTwoMinutes` |

## Encounter odds, Galewick (one species caught: each zone's first)
Before: uncaught x2 (rares too), caught x1. After: caught x0.3 while an uncaught common one exists, rare flat.

| Zone (table) | Odds before → after |
|---|---|
| village (Encounters_GwVillage) | Whirlby (caught) 18% → 6%; Kitelet 29% → 34%; Vanehare 24% → 27%; Toastling 18% → 20%; Coalbeetle 12% → 13% |
| arrival (Encounters_GwArrival) | Kitelet (caught) 21% → 7%; Whirlby 36% → 43%; Pufflet 18% → 21%; Toastling 24% → 28% |
| fire (Encounters_GwFire) | Toastling (caught) 18% → 6%; Coalbeetle 35% → 40%; Ashmole 35% → 40%; Whirlby 12% → 13% |
| kites (Encounters_GwKites) | Kitelet (caught) 25% → 9%; Vanehare 31% → 38%; Samarling 19% → 23%; Kettlewing 25% → 30% |
| ridge (Encounters_GwRidge) | Samarling (caught) 18% → 6%; Vanehare 29% → 34%; Whirlby 24% → 27%; Coalbeetle 18% → 20%; Pufflet 12% → 13% |
| rare (Encounters_GwRare) | Samarling (caught) 21% → 8%; Vanehare 36% → 44%; Kitelet 36% → 44%; Zeppelope (rare) 6% → 4% |
| tower (Encounters_Galewick) | Kitelet (caught) 25% → 9%; Whirlby 38% → 45%; Toastling 38% → 45% |
| map (Encounters_Galewick) | Kitelet (caught) 25% → 9%; Whirlby 38% → 45%; Toastling 38% → 45% |

## Copy pass (before → after)
| Where | Before | After |
|---|---|---|
| `intro.shard` | Something's here. Touch it? | Something's here! Poke it. |
| `lesson.move` | Grab a corner, then move me wherever is comfy. | Grab a corner. Move the island somewhere comfy. |
| `intro.starter` | Pick an Isling friend! | Pick a friend! Look at a pod and pinch. |
| `lesson.pinch` | Tap your thumb and finger together. Like this! | Pinch your thumb and finger. Like this! |
| `lesson.move.again` | Grab a corner, then move it. | Grab a corner and move the island. |
| `lesson.starter.again` | Look at one and pinch, or grab it. | Look at a pod and pinch. Or grab it. |
| `lesson.walk.far` | Swipe to go. Tap your thumb to stop. | Swipe to walk. Tap your thumb to stop. |
| `lesson.walk.far.again` | Your explorer walks until you tap your thumb. | Tap your thumb to stop walking. |
| `lesson.look.again` | The glowing spot. Look, then pinch! | Look at the glowing spot, then pinch! |
| `lesson.grass` | Islings hide in tall grass. Let's peek! | Islings hide in tall grass. Walk in! |
| `lesson.grass.again` | Follow the gold tiles into the tall grass. | Follow the gold path into the tall grass. |
| `lesson.send.again` | Toss your friend's pod onto the island! | Grab your friend's pod. Toss it onto the island! |
| `lesson.rune.again` | Poke a rune! | Choose your attack! Poke one. |
| `lesson.catch.again` | Grab the big pod and flick it! | Catch it! Flick the big pod at it. |
| `pod.first` | Grab your friend's pod from the rim and toss it in! | Grab your friend's pod. Toss it onto the island! |
| `battle.first` | Poke a rune to attack! | Choose your attack! Poke one. |
| `capture.first` | It's weak now. Grab the big pod and flick it! | It's weak now. Catch it! Flick the pod at it. |
| `evolve.ready` | {name} is glowing! Take its pod and hold it on your open palm. | {name} is glowing! Hold its pod on your open palm. |
| `cw.vine` | Thorns on the trail! Pick up that ember and drop it on them. | Thorns! Touch the ember, then poke the thorns. |
| `cw.vine.tip` | Now drop it on the vine! | Now poke the thorns! |
| `cinder.log.hands` | Maybe your big hands could move it? | Grab the log and lift it away. |
| `cinder.bellows.first` | Bellows! Poke them to send heat to the braziers. | Bellows! Poke them again and again. |
| `cw.flame` | The village flame is fading! Keep it warm with your hands. | The flame is fading! Hold your hands round it. |
| `cw.lanterns` | Smoke! Light the lanterns again. | Smoke! Poke the lanterns to light them. |
| `cw.ember` | Another ember! Drop it in the forge's mouth. | Touch the ember, then poke the fire. |
| `cw.ember.tip` | Now tip it into the forge's mouth! | Now poke the fire! |
| `gw.arrive.clouds` | Clouds are stuck on the path. Sweep your hand to get the wind going! | Clouds on the path! Sweep them away with your hand. |
| `gw.mill` | The mill is stuck. Give its sails a flick! | The mill is stuck! Flick its sails. |
| `gw.kites.task` | Pinch one and lift it up into the sky. | Pinch a kite and lift it up. |
| `gw.keeper.knots` | It's all tangled up! Pull the knots apart. | All tangled! Pinch a knot and pull. |
| `lm.pads` | The channel's too wide. Nudge the lily pads together! | Too wide! Poke the lily pads together. |
| `hub.shard.first` | Put the shard in the statue. Something's calling it. | Grab the shard. Put it in the statue. |
| `spirit.finale.hand` | Lift me up to the gold crystal on its forehead. | Hold me up to the crystal on its forehead. |
| `temple.warn` | {keeper} is strong. Most guardians bring {n} friends. | {keeper} is strong. Bring at least {n} Islings. |
| `lm.arrive.stream` | The stream's run dry, and the lily pads are stuck in the mud! Hold your hand over it like a little cloud. | The stream's dry! Hold your hand over it like a cloud. |
| `chip (lessons) x1` | Touch it | Poke the shard |
| `chip (lessons) x1` | Two hands | Grab with two hands |
| `chip (lessons) x1` | Into the grass | Walk into the grass |
| `chip (lessons) x1` | Toss the pod | Grab the pod, toss it |
| `chip (lessons) x1` | Poke a rune | Choose your attack |
| `chip (lessons) x1` | Flick the pod | Flick the pod at it |
| `chip (lessons) x2` | Swipe | Swipe to walk |
| `chip (starter, look) x2` | Look + pinch | Look at it, then pinch |
| `door chip` | Walk in, or look at the door and pinch | Walk in, or look and pinch |
| `forge ember chip (pinch mode)` | Tip it in! | Drop it in the fire |
| `vine ember chip (pinch mode)` | Drop it on the vine! | Drop it on the thorns |
| `vine fingertip chip` | Now poke the fire | Now poke the thorns |


Scene chips (`Step29Setup.Chips`): Push! → Poke the pads together; Palm down! → Turn your palm down; Sweep!/Sweep → Sweep the clouds away (counter "Clouds"); Cup it! → Hold your hands round it; Palm down → Hold your palm over it (counter "Pads"); Touch them! → Poke the lanterns; Pull it up! → Pinch a kite, lift it; Scoop! → Scoop water, pour it; Pull apart! → Pinch a knot, pull; Flick! → Flick the sails; Pick it up! (both embers) → Touch the ember. New lines: `heal.bloom` "Your Islings are healed!", `hall.tired` "Your Islings are tired. Heal them first." (asset and Script_v1.md both, by hand: R25).

## Pull the device log after a playtest (Git Bash, one line)
`adb logcat -d -s Unity > playtest.log`

In-game: Dev panel > Report bug writes `bugreports/<time>/log.txt` (last 2 min), save, state and screenshot; pull with `adb pull /sdcard/Android/data/<package>/files/bugreports .`

## Headset test list (Quick Journey play order)
1. **Starter pick**: the three face you and hop, no spinning; look at one: card with its colour, "Fire · strong vs Wind, weak vs Water", signature move.
2. **First grass** (full adventure): one gold path; swiping toward it stops at the grass edge once. Walk preview = white chevrons.
3. **Walking on 2-wide roads** (Still Isle): a swipe follows bends, stops at each real junction with white chevrons for 2 s, never mid-corridor. Tap still stops. Log `Smart step ... ends at junction` / `[WalkPreview] Junction at`.
4. **Battle**: during the wild turn moves sunk and grey, "Wild X's turn...", a press only shakes (log `pressed off-turn`); your turn they rise with a gold pulse, "Choose your attack".
5. **Rings**: every pod has a ring; low pod blinks red with "!", fainted grey "zz". Poke a hurt pod: card says "Tired! Heal at the lodge or a healing bloom."
6. **Galewick river**: the bridge never says "face the water"; the two rippling banks (east of the bridge north side, west south side) fish.
7. **Galewick bloom** (before the tower): walk onto / poke / look + pinch: green +, rings sweep, chime, "Your Islings are healed!", it closes 60 s. With a hurt team the tower says "tired" once and the bloom pulses gold; the door still works. Walk stops beside the tower door.
8. **Cinderwood vine**: touch the ember: fingertip burns (crackle, embers); poke the thorns: they burn. Wait 20 s instead: the flame floats back, walking works. Log `[HandPuzzle] lock/unlock cw.arrive`.
9. **Fallen log**: lift and let go anywhere over the island: it glides into its spot.
10. **Forge ember** the same as 8, then the **bellows**: poke ghost and chip "Poke the bellows again and again" from the start.
11. **Cinderwood + Lilymere blooms** heal the same way.
12. **Lodge** with a hurt team: green + from the floor, rings refill one by one, line. Full team: nothing.
13. **Text lean-in**: pull the island to ~25 cm: bubbles, chips, menus and cards stay ~45 cm away, same size in view.
14. **The Heart** (finale): one deep synth "moooo" with a bell ping at its first line; quiet murmurs after.
15. **Report bug** from the Dev panel, then pull it: the log covers about 2 minutes.

## Shots
A0 top-down (row 0 at the top): `Run-29-img/A0_StillIsle_before.png` / `_after.png`, same for Galewick, Cinderwood, Lilymere.
New lane magenta, corner fills violet, the bridge exception framed red, the spots left one wide framed yellow.
Seat view (flat, seat camera): `29_rings.png`, `29_card.png`, `29_bloom_open.png`, `29_bloom_healed.png`, `29_fishbank.png`,
`29_yourturn.png`, `29_offturn.png`, `29_junction.png`. No shot of the burning fingertip, the log drop or the starter card:
flat Play can't fake a hand, and the starter pick only runs in a new game's opening.
