# Pocket Isle — game design (v0.2, Oct 22, 2026)

Title: **Pocket Isle** (kept by Mugesh; "Pocket Isles" exists on itch.io and "Pocket Island" on iOS/Windows,
nothing found on the Quest store). Realm and creature names are placeholders. Open items at the end.

## Names and concepts (from Gemini round 1, Oct 22; source: Assets/Docs/Concepts/gemini-01-output.txt)
- Realms: **Still Isle** (hub), **Cinderwood** (fire), **Lilymere** (water), **Galewick** (wind). Placeholders.
- Starters: **Kilnpot** (fire clay-pot tanuki cub, cork-chimney tail puffing ember rings),
  **Pebbloop** (water river-hippo calf, floating bubble-ring collar, paddle tail),
  **Pufflet** (wind cloud-alpaca lamb with a spinning dandelion rotor on its head).
- IP fixes: Hearthstag antlers = short chunky ember-crystal crown, not big branching antlers (too close to Xerneas).
  Pufflet's silhouette led by the rotor and alpaca neck, not a round wool ball (too close to Wooloo).
- Lantern Spirit: palm-size glass-acorn lantern with a crooked mossy stone cap (clue it came from the statue),
  two orbiting ear-wisps.
- Heart of the World: stone body from the hub's mossy rock, cracked to show a golden core, forehead socket the
  Lantern Spirit slots into; fire crown, water lotus shell and ribbon tail, wind cloud-wings.
- Realm palettes and prop lists: see the Gemini output file.
- Music: Gemini's Lyria prompts, with any studio or artist names removed.

## Pitch
A cozy creature-collecting adventure on a floating island in your room. You are a giant guardian
guiding a tiny explorer across a shattered world. Catch creatures, beat the three Shard Keepers,
and restore the world. Each restored region docks onto your island.

## Pillars
1. **Cozy and whimsical.** Gentle stakes. Creatures faint, they never get hurt. No punishment beyond walking back.
2. **Your hands are the guardian's hands.** Grab pods off the rim, throw them, lift the island, inspect creatures in your palm.
3. **The world lives in your room.** The island floats where you put it; battles leap out to toy size in front of it.
4. **Familiar loop, new body.** Pokémon-style structure (party of 6, types, levels, gyms) told through MR.

## Player and world
- **Player:** a giant guardian spirit. Explains why real hands can reach in, throw pods and move the island.
- **Explorer:** tiny character on the tiles: thumb swipe auto-walks, or touch a tile, or look at a tile + pinch.
- **Guide:** the **Lantern Spirit**, a small floating creature near your hand. Short text bubbles, hints, story. No voice.
- **Story hook:** the world shattered into floating pieces, one per element. Each Shard Keeper (region boss)
  was frightened or corrupted by the shattering. They aren't evil. Beat one and it calms, giving up its shard.

## Controls (decided Oct 24)
- **Walk, three ways, one path system:** (1) thumb swipe = **smart step** (step 12a, Oct 27 night; below); (2) **touch
  a tile** with a fingertip (dip and lift) = walk there (Off by default since 11d); (3) **look at a tile + pinch** =
  walk there (reclined players). Thumb-hold walking dropped (accidental stops, tiring).
- **Smart steps (12a, replaces auto-walk):** one swipe walks the swipe way and follows the path's turns, stopping at the
  first of: a junction, the tile that brings an unsolved hand moment within reach, the tile before a gate, the first
  tall-grass tile, a blocked tile, or 5 tiles. Gold footprints show the walk the moment the swipe is read (they fade as
  it is walked, a ring on the stop tile); a swipe that can't move bumps and turns. **Two quick swipes** (same way, within
  0.45 s) = walk on, stopping only at junctions, interactables, blockers or a **tap**. No run. Smart step 0.53 s a tile,
  walk on 0.4 s. A new direction while walking re-plans. AutoWalk / HoldToWalk stay selectable in code (Inspector).
- **Guidance (12a):** objective lines (lessons, tasks, hand moments; `· objective` in the script) are bold in a
  gold-bordered bubble behind a target icon and stay pinned as the chip; chatter is smaller, plain cream, and fades.
  Poke the Spirit (or look + pinch it) = the current objective again, else "This way!" and the **Spirit's trail**: gold
  tiles toward the next goal (next unsolved hand moment in the realm, then the Keeper door, then the return gate; in the
  hub the statue with a shard, else the next realm gate). The trail also shows after the tutorial, on entering a realm
  and after 12 s standing still with no objective; 20 s, or until arrival. A scene change drops queued chatter.
  Controls card (game menu and Settings): swipe, swipe twice + tap, look + pinch, pinch / grab.
- Tap: stop while walking, hop when standing, fish when facing water.
- Battle runes: poke or look + pinch; the panel faces you at battle start, drifts at most ±15°. Step 14f: a 2 x 2 grid
  (moves 1-2 on top, move 3 and Run below), the move's element on the tablet's inlay.
- **Walk lessons (14f):** the last one is "Swipe to walk, tap to stop", done on any tap that stops a walk, arriving on
  the gold tile ahead, or two finished swipe walks; it passes itself after 30 s. Swiping twice still walks on (the
  Controls card shows it); no lesson teaches it (players saw no difference and could get stuck).
- **Hand moments own their pinches (14f):** while Galeowl's untangle runs the explorer is held still; anywhere, a pinch
  that starts within 8 cm of an active piece, or while one is held, never walks.
- **Buttons (14f):** every button is a chunky carved stone tablet: rounded slab, moss on the top edge, the label carved
  in and filled with warm cream, a dark rim and a soft shadow + faint warm halo so it never blends into a white wall or
  a dark room. Looked at: grows, gold rim, label brightens. Pressed: sinks with a stone tap. Disabled: dim. The primary
  action (Resume, Continue) is larger with a gold inlay. One shared mesh and material (instanced).
- **The game menu (14f):** Resume (large, gold, first) · Controls · Settings · Title, a small speaker tablet in the
  corner for music on / off, Dev only in QA builds, set apart. Title: Continue (its journey under it) · New journey ·
  Settings.
- **The text rule (14f):** every piece of world-space text has a cap height of at least 1.4 cm with the island at its
  default size (about 1.5° at the default viewing distance) and grows with the island; high contrast (labels on tablets
  6.4:1), a dark outline over the scene. Where a line can't fit, it is shortened or dropped (one knob:
  `TextRule.MinCapWorld`).

## World structure
- **The Still Isle (hub, neutral).** Where the game starts. A statue in the centre with 3 empty sockets,
  never explained. Each shard you bring changes it a little (cracks glow, moss falls). Third shard wakes it.
- **3 elemental realms:** Fire, Water, Wind (type triangle). Push for a 4th (Earth/Plant) if ahead.
  Each realm is a large map to explore; the island shows only a window of it, scrolling as the explorer walks.
- **Shard Gates** (well, tunnel, arch) travel between realms. Transition: tiles sink into the base, the rocky
  base changes material (lava rock, coral, cloud stone), new realm's tiles rise. Built once, reused everywhere.
- **Region order is free.** All 3 gates open from the hub. Wild levels and Keepers scale with shards held.
- Each realm has: tall-grass areas, berry bushes, a Hearth Shrine or two, a **Temple** with its Keeper, hidden orbs.

### Still Isle as built (step 8, Oct 25)
30 x 30 map (`WorldMap_StillIsle`), row 0 = back, column 0 = the player's left. The window (14 tiles) shows the statue
from the start.

| Where | What |
|---|---|
| c15 r22 | Start tile (front centre, small cream clearing) |
| c14-16 r16-18 | Statue (3 x 3) on a round stone plaza: a seated mossy stone hound with long drooping ears, closed smiling eyes, faint gold cracks, three empty chest sockets (red / blue / white rims, dim). Faces the start. |
| c10 r20 | Hearth Shrine (2 x 2, door to the front), with its Lodge |
| c19-25 r19-22 | First tall grass, right of the start (~7 tiles of walking). First encounter of a new game: Toastling Lv 3 |
| c3-7 r19-21 | Pond (fishing spot c6 r22), stepping stones beside it |
| c2 r15 / c14 r2 / c26 r15 | Fire / Water / Wind gates (2 x 1) at the end of path stubs on the left, back and right edges; walk in from the tile in front (+row) |

Calm theme: moss green grass, cream paths, pale stone, quiet blue water, teal round trees, grey mossy base rock, no
embers or falling leaves, an open front edge (no border trees facing the player). Encounters: "visitors" table
(Toastling, Waxle, Kettlewing Lv 2-4; the pond has Kettlewing) until Neutral species exist.

### Shard Gates as built (step 8)
- Fire = ember-stone arch, Water = round-cornered stone well with lilies under a wooden crossbar, Wind = short stone
  tunnel with a little windmill. Each has a portal surface (shader `Diorama/Portal`): open = a slow swirl in the gate
  colour, sealed = a dark stone seal with a faint seam.
- Use: walk into it from its front tiles, or look + pinch it (the explorer walks there first). Open gates travel with
  the scene swap (content sinks, the base tints to the next realm, the new map rises); you arrive in front of the
  other map's return gate. Sealed gates (Water, Wind until step 10): a soft bump, the seal flashes, `gate.sealed`.
- Cinderwood (60 x 60 since step 9a; the old test map is in Data/Archive) has a mossy return gate by its arrival.
- Each realm keeps its own restoration; the save keeps the map you're on; the Lodge returns you to the map you came
  from; losing everything returns you to the last shrine on its own map.

## Realms and story (decided Oct 22)
Pattern: in each realm the element has stopped flowing because its frightened Keeper hoards it in the temple.
Beat the Keeper → the element flows again, the realm visibly revives, its shard docks onto your island.

| Realm | On arrival | Guardian-hand moments | Keeper | Restored |
|---|---|---|---|---|
| Fire: ember autumn forest | Glowing leaves going grey, kilns cold | Lift a fallen log off the path; pump forge bellows (open/close hand) | Hearthstag (great stag, ember antlers) | Leaves glow, hot springs steam |
| Water: lily lagoon | Waterfalls frozen mid-fall, lagoon drying | Push lily pads into a bridge; turn the sluice wheel | Tidemother (giant turtle, lily garden on shell) | Waterfalls run, lagoon fills |
| Wind: windmill highlands | No wind, windmills still, kites grounded | Spin a windmill with a finger to open a gate; untangle kite strings | Galeowl (huge owl tangled in kite strings) | Windmills spin, kites rise |
| Still Isle: hub | Statue, 3 empty sockets | Place each shard by hand | Heart of the World wakes | World rejoins |

- 1–2 guardian-hand moments per realm: your real hand changes the world.
- **Keeper fights:** temple hand puzzle to reach the Keeper, then a team battle with a special phase
  (e.g. Keeper shields itself until you use the right type).
- **Twist:** the Lantern Spirit is a fragment of the Heart. It goes quiet near the statue, glows as shards
  are placed, and rejoins the Heart at the end.
- Temple types: Fire = forge in a cliff; Water = half-sunk lake temple; Wind = tall wind tower.

## Encounters and creature spread (revised Oct 23)
- **Wild creatures are hidden.** Nothing battle-able roams the map (replaces the Oct 22 "visible wanderers").
- **Tall grass:** random encounters, mostly commons, plus ~5% **rare/powerful surprise** (gold sparkle at zoom start). The hook.
- **Ponds / water edges:** stand next to water facing it and **thumb tap** → ripples → a water-edge creature leaps out.
  Tap is context-sensitive: facing water = fish, otherwise standing still = hop, while walking = stop.
- **Ambient life (if cheap, else polish backlog):** non-battle fireflies, birds, butterflies so the island feels alive.
- **Spread per realm:** ~70% home element, ~20% visitors of the type that beats that realm's Keeper, ~10% Neutral.
  No Keeper is a hard wall: Keeper shields also break after 3 hits of any type; the counter type is just faster.

## Cinderwood (fire realm, the vertical slice) — decided Oct 22
- ~~Size 100×100~~ **Every realm is 60×60** (decided Oct 26: the 100×100 test map felt empty; dense beats sparse).
  Something to look at every ~8 tiles along the trail.
- **Layout: main trail + side pockets.** Gate → forest trail → kiln village → hot springs → fallen log (hand moment)
  → cliff forge temple. Side pockets hide rare grass patches, berry bushes and an orb.
- **Habitat:** ember tall grass (red-gold). Water visitors live in the tall grass around the hot springs.
- **Residents (later, if cheap):** a few friendly creature villagers in the kiln village with one-line hints.
- **Temple:** pump the forge bellows (~~open/close hand~~ decided Oct 26: **pinch the handle and push it down**; after the first Link test, Oct 26: **poke the pad**, one poke = one pump) → heat
  flows through pipes → lights 3 braziers → Keeper door opens. The log is **grabbed, lifted and set aside** (Oct 26).

### Cinderwood as built (step 9a, Oct 26)
Written by `CinderwoodTools` (Tools > Pocket Isle > Set Up MR_Main (9a Cinderwood)), seed 909. Coordinates (x west→east,
y south→north; south = the arrival, nearest the player); the map's (column, row) = (x, 59 − y). Top-down shot:
`Logs/BattleShots/9a_map.png` (Step9aDebug.MapShot).

| Beat / pocket | Tiles (x, y) | As built |
|---|---|---|
| Arrival | 6–14, 2–8 | Return gate at (7, 6); you arrive at (10, 5) beside its front, facing north; first ember grass at (15, 7); a signpost |
| Forest trail | 4–22, 8–24 | 2-wide trail winding north, ember grass patches on both sides, leaf piles, lantern posts, signpost at the field fork |
| Ember field (pocket) | 26–50, 2–14 | 1-wide side path from (13, 9); five tall grass patches, rock islands, ember vents, a few lone trees |
| Lantern glade (pocket) | 30–44, 16–26 | Path north from the field; ring of 8 unlit stone lanterns round a clearing, two grass patches |
| Kiln village | 6–26, 26–40 | Round plaza, 5 clay huts, 3 kilns, pots, woodpiles, lantern posts, fences, signposts; **Hearth Shrine at (24, 33)**; the three map creatures live on the plaza |
| Berry grove (pocket) | 46–56, 24–36 | Path from the springs; fenced square, 3 rows of berry bushes, tall grass between rows |
| Hot springs | 30–46, 34–46 | 4 round pools (H) with stone rims, boulders, tall grass rings |
| Fallen log | 18–22, 44–46 | Trail back west from the springs narrows between rocks; the log at (20, 45) blocks it completely |
| Forge temple | 10–30, 48–58 | Behind the log: cliff wall along y 56, the 3-wide forge door at (19, 56); 9b: bellows at (18, 55), pipe along the cliff foot, 3 braziers in a row at (16 / 14 / 12, 55); ember vents |
| Kiln ruins (pocket) | 44–56, 46–58 | Narrow path from the springs through the trees; 4 ruined kilns, pot shards, vents, rare grass |

- The forest between places is a wall (trees, rocks between them): only the trail, side paths and pockets are
  walkable. Behind the first row of trees, ~45% of trees are rocks (perf; they sit behind the tree walls).
- Zones (a grid like the props grid) carry their own tall grass tables: forest Toastling 60 / Waxle 30 / Kettlewing 10
  Lv 3–5; field 50/30/20 Lv 4–6; glade Waxle 70 / Toastling 30 Lv 5–7; grove Kettlewing 60 / Toastling 40 Lv 5–7;
  springs Kettlewing 70 / Waxle 30 Lv 6–8 (water visitors later); ruins **Glazegolem 25% (rare)** / Toastling 40 /
  Waxle 35 Lv 7–9. Elsewhere (arrival, village) the map table with the ~5% rare Glazegolem.
- First-visit lines (`LandmarkLines`): plaza 4 tiles, springs pool 5, log 3, forge door 4 (from 9b), ruins 5.
- Checks (setup + EditMode test): trail connects arrival → shrine → springs → log; every pocket reachable; nothing
  walkable cut off (the temple is reachable only with the log lifted); tall grass 17% of walkable land in 21
  patches of 5–20; longest straight trail 9 tiles; never more than ~2 trail tiles without something to look at.
- New props: ClayHut, Fence + FenceCorner (turned by the legend's yaw), Signpost, Woodpile, LeafPile, CliffWall
  (its second look = turned 180), ForgeDoor (cold), RuinedKiln, PotShards, EmberVent: 11 recipes (the cap was 10;
  the fence needs its corner piece). Trees: AutumnTree rebuilt (stacked blobs, red-orange to gold), TallTree mixed in
  (35%).
- **Hearthstag fight:** ember-crown shield blocks damage until hit by a Water move (or 3 hits of anything), then open for 2 turns; repeats.

### Hand puzzles as built (step 9b, Oct 26)
Tools > Pocket Isle > Set Up MR_Main (9b puzzles). Scene objects under **Hand Puzzles**.

**The kit** (`Runtime/Puzzles`, reusable for Lilymere and Galewick). Every piece is island-local and positioned in map
tiles (works at any island scale, while the map scrolls), draws its own prop (the island stops drawing the map's copy:
runtime-only, never written into the map asset), works only while its map shows, it's in the window and the explorer
is within `reach` tiles, saves its state (`PuzzleState` in the save: solved + a stage), and teaches itself the first
time it's usable in a session (line + gold focus + ghost hand at `puzzle:<id>`, until first touched).
| Piece | How | Settings |
|---|---|---|
| **Carry** | Pinch or close a fist anywhere near it (capsule 0.85 tiles round its whole length, +3 cm). Heavy: only rises until lifted 0.6 tiles, then follows with 0.22 s lag and a damped wobble. Released within 1.3 tiles of a target spot: drops onto it (solved). Anywhere else: falls back home. Look + pinch it (floats up), look + pinch a spot (floats there). | weight (lag, max speed, wobble), lift before it moves, snap radius, spots |
| **Pump** | A press pad with a spring return: a fingertip coming down onto it from above pushes it (the first version's lever read badly). Stroke = past 70% of the travel and back above 30%: one poke, one pump. Look + pinch the pad = one stroke. | pad height, travel, radius, spring, strokes needed, checkpoint, cooling (after 4 s, a step every 2 s, never below a checkpoint) |
| Wheel | `IWheelPiece` stub only (sluice wheel, windmill) | |

**The fallen log** (Carry `cinder.log`): reach 3. Lesson: `cinder.log.first` (landmark) then `cinder.log.hands` "Maybe
your big hands could move it?" + the recorded `grab_drop` ghost on the log. Two glowing rings on the rock ledges either
side of the gap (18 / 22, 45). Lifting: leaves and dust fall, the explorer hops. On a ledge: thud, dust, the island's
tiles bounce (the base stays put), the log's tiles open, the explorer hops, "You're strong! Let's go on." Elsewhere: a
small thud, it lies across the gap again.

**The forge** (Pump `cinder.bellows`): reach 4. Bellows beside the door with a round leather press pad on top. Lesson:
"Bellows! Poke them to send heat to the braziers." + the recorded `poke` ghost on the pad. 12 pokes, 4 per brazier: each pump squashes
the bellows with a puff and sends a glow along the pipe to the next unlit brazier (nearest first), whose coals warm a
step; the 4th lights it (flame burst, "One!" / "Two!" / "Three!", saved). All three: the door glows from inside, its
iron bands heat, it sinks into the ground with steam and sparks, "The forge is waking up!", the doorway opens (saved).

**Forge Hall** (placeholder until 9c): 10 x 10 interior like the Lodge (stone floor, pillars, the big cold forge, anvil,
chains, dim ember crystals). Walk onto the open doorway to go in; the Keeper's spot is an empty glowing ring ("Something
huge sleeps here... not yet."); the door tiles lead back out in front of the forge, facing away.

### Cinderwood hand moments as built (step 10f, Oct 27)
Three moments added on top of the log and the bellows (RealmsDesign "Cinderwood upgrade"). Rekindled places light the
island's own props through **warm spots** (a circle in which glows burn, smoke rises and colour comes back; no extra draws).

| Where | Moment | World answers |
|---|---|---|
| Kiln village shrine | The flame is nearly out (a blue sputtering wisp) and the shrine can't be rested at. **Cup it**: both palms turned toward it within ~10 cm for 2 s, or one hand curled round it 3 s, or look + pinch-hold 3 s. Let go and it shrinks back. Chip "Cup it!" | It flares; a warm pulse rolls from the shrine over the plaza: kilns glow and smoke, windows and lantern posts catch one after another, the plaza Islings hop; the shrine heals and saves from then on |
| Smoky hollow (trail village -> springs, x28-31 y33-36) | Dark smoke on 8 path tiles between rocks, 6 dead stone lanterns, each with a gold glow over it (after the village flame). **Touch a lantern's top** to light it; two in a row within 1.5 s and the fire races down the line (0.15 s apart, a spark trail). Look + pinch lights one and the next follows. Chip "Touch them!" | The smoke rolls away from the lit end and the path opens; the lanterns keep burning |
| Forge temple, before the bellows | The firebox (a stone mouth in the cliff beside the bellows) is cold; pumping only puffs grey. **Pick the ember up** (pinch or grab it in its vent, marked by a gold ring) and let go over the firebox's mouth (a gold glow; a spark trail points there while carried). Look + pinch the vent, then the firebox. Chip "Pick it up!" | The firebox roars, its chimney smokes, every vent along the cliff glows, the pipe warms; then the bellows work |

- The village's north edge is forest now (it opened onto the trail back from the springs), so the way on runs through the
  hollow. Trail 111 tiles; a Lv 5 starter reaches the temple at ~Lv 8.4.
- All three are saved (cw.flame, cw.hollow, cw.ember), count as done once the realm is restored, and stay lit (no
  flicker) through the restoration wave. Quick Journey keeps them.
- Virtual light only: the ember's glow is on the ember and its halo, not on the real room or the hand.

- **Arrival lanterns (step 14c):** four dead stone lanterns line the trail 2-6 tiles past the arrival. `cw.arrive`
  "Cinderwood... it used to glow all night.", then "The lanterns went out. Touch one!" (chip Touch, Lanterns n/4, poke
  ghost). One touch (or look + pinch) sends the fire down all four, the glow spreads round them, embers drift up. The
  smoky hollow keeps a Lanterns counter and a short line, no ghost.

### Keeper and shard as built (step 9c, Oct 26)
- **Hearthstag**: Fire Keeper, **Lv 10, base HP 110 (HP 154 = 2.75x a normal Lv 10), atk 10, def 11, spd 8** (step 9f
  balance, `KeeperSim`: a Lv 8 team of 3 with Steambara, Water leading, wins ~80% with Kilnpot, ~62% with Pufflet, ~93%
  with Pebbloop; **without a Water Isling Kilnpot and Pufflet can't win at Lv 8-10**. Was HP 88 / atk 8 / def 9 for a
  Lv 9 team of two fire commons in 9e); Ember Shot (36), Crown Charge (48, every
  3rd turn), Crown Shield. ~2.5x an Isling; frightened = ash fur + flickering dull red crystals, calmed = chestnut + gold.
- **Fury (step 9e)**: frightened it reads overwhelmed and dangerous, never gory: grey-violet smoke aura with ember sparks,
  flame tongues on the crown, constant trembling, red-ember glowing eyes, the smile turned to a frown, smoke snorts every
  few seconds, a stomp now and then (rears, slams: lava cracks on the ground, dust, the tiles bounce). The battle opens
  with a rear-up stomp (the Spirit flinches back); phase 2 = thicker aura, bigger flames, harder trembling. Won: the
  aura burns off into rising gold motes, flames shrink, eyes blink to warm amber, trembling stops, it lowers its head,
  then the palette shift.
- **Crown shield**: starts up; hits do 10% ("Blocked!") and crack a crystal; a Water move or 3 hits break it; 2 open
  turns, then it raises it again. Below 50% HP: phase 2 (shot x1.5, crown brighter). No capture; Run always works (back
  at the hall door); losing = the last shrine, healed, nothing lost. The fight resets until won.
- **After the win**: kneels, calms, nods; forge ignites; the fire shard floats to you, grab it (or look + pinch) into the
  brass holder on the rim (back-left, three cups). Outside: the realm restores in a ~5 s wave from the temple; a return
  gate appears in front of the temple (24, 52). Revisiting: Hearthstag rests by the burning forge; poke it for a snort.
- **Hub**: holding a shard, the statue's matching socket glows and the Spirit points; carry the shard from the holder to
  the socket (or look + pinch): it locks in, the statue's rims and cracks glow, moss falls, the Spirit feels warmer.

### Dev / QA panel (step 9e, Oct 26)
- Editor and QA builds only (scripting define `POCKET_DEV`; **Tools > Pocket Isle > Build QA APK / Build Release APK**).
  The game menu's Dev rune (step 9g; before: the left-hand menu pinch) or F2 opens a 2D panel; poke or point + pinch.
- Tabs: Go to (hub and Cinderwood landmarks, the Forge Hall), Party (heal, levels, team of 3 presets, add any species,
  one-hit KO, invincible), Encounter (fight any species now, wild on/off, rare x10), Story (tutorial, opening, realm
  restored, log, forge, Keeper, shard flags), Save (3 snapshots + auto, new game), Tools (fps, readouts, time x1/2/4,
  walk x2, report bug). QA guide: `Assets/Docs/QA-Guide.md`.
- Built on `GameState`: live changes apply at once; story changes edit the save and reload the scene. **Quick Journey**
  (the judges' short flow, own design session) will reuse it: a preset = a SaveData + a reload.
- Step 9g: the left-hand menu gesture opens the **game menu** now; its **Dev** rune (Editor / QA builds) or F2 opens
  the panel. Save tab: **Quick: start / at gift / at gate / at temple** (presets into the quick slot).

### Title, game menu, settings (step 9g, Oct 26)
- **Every button is a rune** (`RuneBoard`: the battle runes' look and input; poke, or look + pinch either hand; the
  looked-at rune grows with a gold border; a few runes need a 2 s hold, their plate fills gold). Boards sit over the
  near rim and face the head.
- **Logo** (sheet 17 option 1, `PropKit.Logo`, baked like a prop, 7.6k tris, one mesh): puffy orange "Pocket" over
  green "Isle" on a mini floating island (grass, brown rock underside, grey rocks, a tiny tree, a lantern). No font:
  letters are capsule strokes. About 30 cm wide at island scale 1, ~25 cm over the island's far half, bobs, sways a
  little and turns to the head.
- **Save slots:** `save.json` = Full adventure, `quick.json` = Quick Journey (same SaveData). PlayerPrefs keeps the last
  played slot. Any slot = a returning player.
- **New player:** the opening as before; after the explorer stretches and before the Spirit, the logo rises out of the
  statue and the runes offer **Full adventure / Quick journey** ("about 10 min") / **Settings** (nothing gold until
  looked at). The pick floats the logo away and the opening goes on in that slot.
- **Returning player:** no shard; the island rises with the logo. **Continue** (gold, "Adventure" or "Quick journey"),
  **New journey**, **Settings**, and "or: <other slot>" when both have a save. Gameplay waits until Continue. New
  journey: Full / Quick / Back; a slot that has a save needs the 2 s hold ("Hold to start over"), then it is deleted
  (copy kept as `<slot>.old.json`) and the opening plays from the shard without the slot question.
- **Game menu** (`GameMenu`): left palm up + pinch (Meta's menu gesture; Esc in the Editor). Pauses the game
  (`GamePause`, time stops; runes run on unscaled time), dims the island like the battle space. **Resume**,
  **Settings**, **Title** (saves, reloads into the returning title), **Dev** (QA builds). Not during the opening before
  the starter, on the title or during a scene swap; while the island is held it opens on release. The row sits a little
  above the battle runes so they never overlap; battle runes ignore picks while paused. `menu.first` ("Palm up and
  pinch with your left hand any time to pause.") a few calm seconds after the lessons end, never in battle.
- **Settings** (`SettingsBoard`, one stone board): Music / Effects / Ambience (five pebbles: off, 1-4; PlayerPrefs under
  the bus names `Music`, `SFX`, `Ambience`, read via `VolumeSettings`; nothing plays through them until the audio step),
  Dominant hand Auto / Left / Right (overrides the first-shard-touch rule), Spirit hints On / Fewer (Fewer = no idle
  hints), Recenter island (the placer's re-place in front of the head), Credits (a stone card; scrolls once it is longer
  than 7 lines; `AudioCredits.md` is appended by the setup when it exists), Back.

**Logo v2 (step 9h):** built from the island's own art: a 6 x 3 mini Still Isle chunk (grass caps, a path strip, grey
tile columns, a rocky underside of uneven depth, tall-grass tufts, the hub's tree, statue and lantern shapes), "Pocket"
amber with a light top band and a dark lower band, "Isle" grass green with a cream band and dripping moss caps, the I's
dot a small glowing lantern. Hand-made: uneven heights, a small tilt per letter. The lantern pulses, two specks drift,
the base bobs. 9.8k triangles, 4 draws.

### Realm order (step 14c, Nov 3): Galewick first
- The Spirit's trail and the hub's guidance point to the **wind gate first**, then Cinderwood, then Lilymere
  (`SpiritTrail.HubOrder`); any open gate still works, and all three are open in builds.
- Keeper levels for that order (`RealmOrderSim`: starter + the realm's shield-breaking visitor + one trail catch,
  grass x1.5): Galeowl Lv 8 at 0 shards (was 9; the Galewick fire patch one level up, Toastling 7-9), Hearthstag Lv 12
  (was 10, so Lv 14 at the usual one shard), Tidemother Lv 9 (13 at two shards, unchanged). Win %: Galeowl 100 / 54 / 68,
  Hearthstag 53 / 91 / 44, Tidemother 50 / 65 / 98 (Kilnpot / Pebbloop / Pufflet). The spread per starter is the element
  matchup (the starter that breaks the shield wins easily), not levels. A Cinderwood-first player now meets Hearthstag Lv 12.

### Quick Journey as built v2 (step 14c, Nov 3: a Galewick journey; v1 was Cinderwood, step 9g)
- Same game, its own slot, dials in `Data/QuickJourney.asset` (`QuickJourneyConfig`), applied only in the quick slot.
- **Opening and lessons unchanged** (judges learn every interaction). Cut: `twoHands` only.
- **The gift:** after the capture lesson, in the hub: `quick.gift` ("The road to Galewick is windy. Take two friends
  with you!"), Toastling + Whirlby fly to the rim, the starter is raised; `quick.gate` ("Bring back the wind shard!")
  sends the Spirit to the wind gate. Per starter (QuickJourneySim, Galeowl Lv 8): Kilnpot gifts Lv 5 / raised 8 (80%),
  Pebbloop 8 / 9 (74%), Pufflet 8 / 7 (76%). Toastling leads the fight: fire breaks the Gust Wall.
- **Galewick:** arrival sweep, mill flick, kites, tower, untangle, Galeowl. The ridge cloud bank is cleared already
  (`CloudBankPiece.Presolve`: the sweep was taught at the arrival). Dials in Galewick only: encounters x0.5, XP x2, no
  rares, gold tiles after 4 s standing still (wind gate, mill, kites, tower, home gate, statue).
- **End:** the wind shard locks in, the statue whispers (`heart.dream.wind`), the Spirit: "Did the statue just...
  speak?", then `quick.end` ("You woke the first piece of the world!" / "Two more realms are waiting.") and three
  runes: **Keep exploring** (the full adventure's trail takes over, Cinderwood next), **See the ending** (the finale
  from the wake with the quick save written once and then suspended, so it is never marked finished; back to the title
  after), **Title**.
- **Dry run** (Tools > Pocket Isle > Quick Journey > Dry Run): **13:00, over the 12:00 target**; per-beat table and
  options in `Run-14c.md` (waiting on Mugesh before anything else is cut).

### Nov 3 evening changes (step 14d)
- Walk 0.28 s a tile, run 0.15. Walk lessons: swipe any way until 4 tiles are really walked, then walk on + tap to stop
  (the turn lesson went). The hub shrine is locked through the tutorial.
- **Quick Journey v2 has no gift** (it gave Islings you had or from other realms): the Spirit sends you to the wind
  gate; Galeowl is Lv 4 in the quick slot (offset -4: Kilnpot 100%, Pebbloop 64%, Pufflet 90% with the tutorial Toastling).
- The Spirit's trail shows after every solved hand moment, in both slots. Portals: walking onto a front tile heading in
  or stopping there goes through; gazed portals glow brighter. Name cards centred higher, with `card.rise` / `card.sink`.

### Realm name cards and gate signs (step 14c)
- Entering a realm (or coming home) a carved card rises over the island's far half, after the arrival line: the name in
  the logo's chunky strokes, the element emblem, one line under it (first visit, 3 s); later visits the name only
  (1.5 s); the first visit after the shard is taken shows the restored line once. Galewick: wooden plank, "The windmill
  highlands, where the wind stopped"; Cinderwood: charred plank with ember edges, "The autumn forest, where the fire went
  cold"; Lilymere: mossy stone tablet, "The lily lagoon, where the water froze"; Still Isle: warm plank, "Home".
  `RealmNameCard`, baked cards `Data/Props/RealmCard_*` (PropKit.RealmCards, 6.5-8.2k tris each).
- Hub gate signs (`GateSigns`): the same card, small, over each gate arch; gold on the gate the trail points to.

## The objective rule (step 11d, Oct 27 headset notes)
Mugesh kept finding tasks by accident. One rule now covers every hand moment in all three realms:
- **Gold on what you can act on now**: a pulsing gold ring with a soft glow on each object of the current objective
  (kites lying in the grass, cloud puffs, the mill's sails, Galeowl's knots, lily pads, lotus buds, the scoop pool then
  the basin, the log, the bellows pad, the village flame; the lanterns, ember vent and firebox keep their 10f rings).
  Off once that object is done. Same language as the 9h focus: gold = act now, white = hover.
- **A counter in the chip** when an objective needs N: Kites 1/3, Clouds 2/3, Flicks 1/3, Knots 1/3, Pads 1/3,
  Lotus 4/5, Pours 1/2 (the lanterns race, no counter). The lesson line stays pinned until the objective is done and
  pins again when you come back. Count lines ("One!") pass over the pinned line and give it back.
- **Say the task**: the kite field opens with "The tower needs kites. Let's fly all three!"; the tower's crown shows
  three empty glowing hooks until the kites are tied.
- **Open doors glow** (forge, wind tower, the lake temple later): a warm portal swirl in the doorway and a gold ring on
  the threshold, the same component (`DoorGlow`) for every enterable door.
- **Out of view**: when an objective's object is outside the island window, a small gold arrow lies at the window's
  edge pointing to it, and the Spirit says once "One more over there!".
- **Touch tiles to walk is off by default** (Settings row, saved). Walking: thumb swipe and look + pinch.
- **Pick-ups read as a pinch**: the ghosts for the kites, log, ember and Keeper shard pinch with thumb and index (the
  other fingers relaxed) with a tiny gold spark when it closes; every pick-up also takes a whole-hand grab.
- **The Keeper's shard** after a win: pinch or grab within 9 cm, gold ring, look + pinch, and after 4 s it floats to
  the Spirit by itself.

## Lilymere as built (steps 11a-11b, Oct 27 night)
- 60 x 60 (`LilymereTools`, seed 1131), straight legs only: arrival + return gate, the stilt village round shallows (a
  plank bridge over them, 5 stilt huts, Hearth Shrine), the lily channel (3 rows of water across the map, one 2-wide
  crossing), the lotus garden (a dry cracked pond bed the trail crosses), the waterfall basin under the cliff (the frozen
  waterfall on its face), the lake temple half-sunk on the lagoon's north shore, reached by a causeway. Pockets: reed
  marsh, rare pond, wind-visitor meadow, a lookout jetty. Woods walls make the two puzzles the only ways on.
- New tile **D (dry bed)**: pale, sunk like water, not walkable. Unrestored the lagoon is half dry (its outer ring is D);
  `WorldMap.SetRisen` turns every D into water at runtime (never saved into the asset), driven by `LilymereLagoon`
  (lm.lagoon solved or the realm restored).
- Look: unrestored water still and a little grey; restored water ripples and sparkles (Toon `_Ripple` on the island's
  water caps only), lotus open (the Lotus prop's flower only shows once restored).
- Hub **well (water) gate** <-> Lilymere; builds keep it sealed (`LilymereConfig.open` = false). Dev panel LM: spots.
- **Arrival stream (step 14a, replaces the lily pad bridge):** 3 tiles past the arrival a thin trickle (dry bed) crosses
  the trail from woods to woods, three lily pads stuck in the mud on its south bank. Palm down over it = rain: the stream
  swells to water in ~2 s, each pad rained on lifts and floats ("Pads n/3"), and all three drift into a pad bridge over
  the trail. It teaches the rain gesture right after the portal ("The stream's almost dry... Hold your hand over it like
  a little cloud."). The channel crossing is a plain plank bridge now; old saves with the pads solved count it solved.
- Hand moments (11b): ~~lily pad bridge~~ (off since 14a), **rain hand** (palm down within 40 deg, 5-25 cm over
  the garden for 0.6 s = a cloud under the palm that rains; wiggle the fingers to rain harder; five buds bloom into lotus
  stepping stones in 2 s / 1 s hard; look + pinch-hold rains on one bud), **scoop and pour** (cup a hand palm up, dip it
  in the lagoon's inlet by the causeway, lift: a wobbly ball of water; tip it over the stone basin to pour, anywhere else
  it spills; 2 pours: the waterfall cracks and runs, the lagoon rises; look + pinch the water, then the basin, pours by
  itself). Each teaches itself once with a pinned chip (Push! / Palm down! / Scoop!) and a ghost.
- **Water roster (11c)**: Marimoss -> Marimound (moss ball / grassy hill with flowers), Drizzcan -> Rainurn (watering-can
  elephant calf / banded urn), Lilyvole -> Lotusvole (lily-pad hat / lotus hat), Sailfold -> Galleyfold (paper boat /
  three sails), Bottlewhale -> Lighthousewhale (rare; a ship painted inside the bottle / a lighthouse with a glowing
  lamp). Evolve at 16 (rare 18). Moves Moss Splash, Spout Spray, Pad Swipe, Wake Ram, Cork Pop + a Neutral + a support.
  Tables ~70% water, ~20% wind visitors (Kitelet, Samarling, Vanehare), Bottlewhale 5% (rare) in the pond.
- **Tidemother (11c)**: a huge cozy sea turtle, jade shell with lily pads and a lotus. The lake temple's door glows open
  once the lagoon rises; inside, a flooded hall (shallow water floor, stepping stones, lily pools, a high water-light
  window) with the hoarded water piled in a glassy dome behind her. Battle Lv 9 (+2 per other shard), HP 132, Tide Shot,
  Shell Slam, Lily Shell (breaks to Wind). Win: she lets the water go (the dome flows out), the water shard floats out
  (it flies to the Spirit by itself after 4 s), restoration wave from the temple, a return gate beside the temple. The
  hub statue's blue socket takes it; with all three placed the Spirit says "All three pieces home..." (finale later).
  Sim (starter + Kitelet + a water common, equal levels): 64% at 0 shards, 69% at 2 shards (Pufflet ~97%, wind beats her).

## Galewick as built (steps 10a-10b, Oct 27)
- 60 x 60 (`GalewickTools`): arrival meadow + return gate, windmill village (4 cottages, 2 small mills, big mill),
  shrine, a ditch crossed only at the drawbridge, the kite field loop (3 kites), a hill band crossed only by the
  1-wide ridge path (cloud bank), the wind tower on the ridge top; pockets: fire visitors (scorched patch), rare grass.
  Rolling hills = hillocks (the theme's tree prop) instead of tile heights. Zones with placeholder tables (Pufflet + fire
  visitors) until 10c.
- Still while unrestored (`RealmTheme.stillWhenUnrestored` 0.92: grass and sway damped), restored = sails turning
  (prop motion Spin), kites on posts (motion Fly), seeds drifting, grass waving.
- Hub wind gate <-> Galewick; builds keep it sealed (`GalewickConfig.open`) until 10d is done.
- Levels across realms: wild +3 per shard held (not in the hub), Keepers +2 per other shard (`ShardScaling`).
- Hand moments (10b): **mill** (Wheel piece: a fingertip flick > 0.5 m/s across the sails adds spin; 3 flicks in 4 s
  keep it turning, the gear turns, the drawbridge lowers), **kites** (pinch/fist a kite, lift past 15 cm and let go or
  toss it up: it flies 30-46 cm over the island on a string to the backpack, figure-eight, follows the explorer, out of
  the battle view, saved), **gust** (open flat hand sweeping > 0.6 m/s within 30 cm over the island: a ripple runs
  across all the grass, streaks, mills twitch; works anywhere in Galewick), **cloud bank** (3 puffs on the ridge path,
  about 3 gusts each, look + pinch-hold also works), **tower** (arrive with 3 kites: they tie onto the crown, vanes
  turn, the door opens). Each teaches itself once with a pinned chip (Flick! / Pull it up! / Wave!) and a ghost.
- **Arrival clouds (step 14a):** two cloud puffs sit on the trail 3-4 tiles past the arrival. "Galewick... the air isn't
  moving at all." then "Clouds are stuck on the path. Sweep your hand to get the wind going!" (chip "Sweep · Clouds n/2",
  the sweep ghost). Clearing them sends a big gust rippling across the whole meadow in view. The ridge's cloud bank
  keeps its counter and a short line, no ghost (the player knows the sweep by then).
- Wind roster (10c): Kitelet -> Kitewyrm, Whirlby -> Windwhirl, Samarling -> Samaroak, Vanehare -> Compasshare (Lv 16),
  Zeppelope -> Skyshiplope (rare, Lv 18); Pufflet -> Nimbulpaca, Pebbloop -> Riverloom (Lv 14). Tables ~70% wind, ~30% fire
  visitors, Zeppelope 5% in the rare pocket.
- Galeowl (10d part one, 10e): Wind Keeper, Gust Wall breaks to Fire, Lv 9 (11 at one other shard). Tower hall (10e):
  round cloud-stone room, tangled kite strings on the beams, a round window of still grey sky beside its perch. It sits
  wrapped in strings with 3 glowing knots: pinch both loose ends and pull apart > 12 cm (one hand: > 15 cm away; look +
  pinch: loosens in 2 s), twang, loops fall. Freed, it flares up and fights. Won: it calms and bows, the window clears
  (sky, light, drifting clouds), the teal wind shard floats out to you and rides with the Spirit, Galewick restores in a
  wave from the tower, the tower's return gate appears. Calmed it preens on its perch; poke it for a hoot.
- One Keeper flow for every realm (10e): `KeeperHall` per Keeper (ids, names, look as fields), the shard holder carries
  any number of shards, one statue socket per shard (the wind socket is teal; two placed = `statue.two`).

### Look of the two states (decided Oct 24)
The element "not flowing" shows as things being **still and unlit**, not as colour loss. The bright palette is the
default; restoring the realm makes things **move and glow**.

| | Unrestored (default, most play time) | Restored |
|---|---|---|
| Palette | The bright Cinderwood palette | Same, about 6% warmer and brighter, leaf tips glow |
| Lanterns, lantern posts, kilns, braziers | Dim, irregular sputtering flicker (drops out, catches again) | Bright, steady warm glow, gentle flicker, soft halo |
| Embers | None | Drifting embers rising through the trees and over tall grass |
| Hot springs | Still water, no steam | Steam puffs |
| Leaves | Static colour | Slight glow pulse on the tips |

Debug toggle: G (Editor) or three quick left-hand thumb taps (dev builds), 2 s fade.

### Fire roster (placeholders; evolutions at a set level)
| Creature | Concept | Evolves to | Rarity |
|---|---|---|---|
| Kilnpot | Clay-pot raccoon cub, chimney tail | Kilnhearth | Starter |
| **Toastling** | Bread-loaf hedgehog, toasted spines | Crumbhog | Common · **first 4** |
| **Waxle** | Snail with a melting-candle shell | Lanternsnail | Common · **first 4** |
| **Kettlewing** | Kettle-shaped duckling, steam from its beak | Steamswan | Common · **first 4** |
| Coalbeetle | Round beetle, glowing coal horn | Forgehorn | Common |
| Mapadillo | Armadillo with maple-leaf plates | Emberdillo | Common |
| Ashmole | Mole with a lit coal on its nose | Cindermole | Uncommon |
| Cindercap | Glowing mushroom critter, ember spores | Cinderbloom | Uncommon |
| **Glazegolem** | Stack of glazed pots, glowing cracks | Kilngolem | **Rare surprise · first 4** |
| Embercrane | Crane with ember-tipped feathers | Pyrecrane | Rare surprise |
| Steambara (Water visitor) | Capybara soaking in steam, fruit on head | Onsenbara | Common near hot springs (counts toward the water roster) |
| Hearthstag | Keeper: stag, chunky ember-crystal crown, sturdy legs | — | Temple |
Avoided for originality: lizards, foxes, cats, pigs, moths, pinecones (all used by Pokémon fire types).

**Fire roster as built (step 9d, Oct 26):** all 10 fire lines exist (Steambara is the water roster's). Step 9f: the
five evolutions (Forgehorn, Emberdillo, Cindermole, Cinderbloom, Pyrecrane) are full recipes from sheet 11 now.
**Steambara -> Onsenbara (step 9f):** Water, evolves at 14; capybara in a steam cloud with a fruit (Onsenbara: bigger,
wooden tub ring, towel). Spring Splash (Water shoot 45, breaks the Keeper's shield) / Tackle / Warm Nap; Onsenbara Hot
Soak (Water slam 60) / Spring Splash / Warm Nap. Hot springs ~35%, berry grove ~10%; `steambara.first` on the first one.
**The explorer (step 9f):** concept 12 Option A, a teal-hooded kid with a backpack lantern (Spirit gold), ~2 cm, 3k
tris; walk bounce with arm / leg swing and hood bob, run leans, idle glances and lantern sway, tap hop, cheer (arms up)
when the log or the forge door is solved.
| Line | Evolves at | Where (Cinderwood) | Moves (base) |
|---|---|---|---|
| Kettlewing -> **Steamswan** (teapot swan) | 14 | forest, field, grove, springs | Steam Jet / Tackle / Hunker Down |
| **Coalbeetle -> Forgehorn** | 16 | forest 15, field 17 | Horn Ram / Tackle / Hunker Down |
| **Mapadillo -> Emberdillo** | 15 | forest 10, field 16, grove 10 | Roll Out / Ember Puff / Hunker Down |
| **Ashmole -> Cindermole** | 16 | village / arrival 15, ruins 25 | Dig Strike / Ember Puff / Warm Nap |
| **Cindercap -> Cinderbloom** (lava-cracked cap) | 16 | lantern glade 30 | Spore Drift / Tackle / Warm Nap |
| **Embercrane -> Pyrecrane** (rare) | 16 | field 5, springs 5 (rare) | Flame Wing / Ember Puff / Puff Up |
- Glazegolem / Kilngolem read as rare: deep teal glaze, gold kintsugi cracks that glow and pulse.

## Core loop
explore → tall grass encounter → battle → capture → party / Hearth Lodge → Temple Keeper → shard → statue

## Islings (the creatures)
- **Name (decided Oct 24): Islings.** Every line, card, toast and label says Isling / Islings, never "creature"
  or "Pokémon". (Code: gameplay types are `IslingSpecies` / `IslingInstance`; the art pipeline keeps its Creature* names.)
- **Target 10 species per element** (~30 base + ~30 evolutions + 3 Keepers + the Heart creature).
- Ship in tiers: **4 per element guaranteed, 7 if on track, 10 stretch.** Region 1 gets its full set first.
- **1 evolution per species** as the base rule; a few rare 3-stage lines are optional later.
- **Parts kit:** bodies, heads, ears, tails, wings, horns, patterns as SDF pieces plus palettes.
  A species = data file (parts + colours + stats + moves). An evolution = the same file with modifiers.
- Concepts from Gemini in batches; Claude turns them into recipes. Original designs only.

## Battle (decided Oct 22)
- **Stage = the island itself, zoomed in.** Grass rustles → in ~0.6 s the map content scales ~3× around the
  encounter tile inside the fixed rocky base/rim; tiles beyond the rim are clipped (same clipping as the
  scrolling window). Creatures end up hand-sized on real tiles. Zoom back out after. No second stage to place.
- **Battle space (Oct 23):** a clearing opens (grass bends flat, trees/rocks in the way sink), two round earth pads
  rise under the creatures (sand pads at ponds), the rest of the island dims. **Pokémon framing:** your creature
  on the near pad, back to you; the wild one on the far pad, facing you. Runes use ISDK poke + gaze/pinch.
- **Flow:** grab a pod from the rim → toss it onto the island → your creature pops out facing the wild one →
  move runes float just above the near rim → weaken → flick a pod to capture → zoom out.
- **1v1, turn-based, switch any time** from the party of 6 (switching uses your turn). Flee from wild battles.
- **3 moves per creature:** one element attack, one basic (neutral) attack, one support (shield / buff / heal).
  Built from four animation templates (shoot, lunge, slam, shield) recoloured per element.
- **Pick moves with gaze + pinch** on 3D runes above the near rim. Works reclined, no reaching.
- **Types:** Water > Fire > Wind > Water, plus **Neutral** (hub creatures; no strengths or weaknesses).
  Strong ×1.5, weak ×0.67 (tune later).
- **Levels:** XP to creatures that fought; level cap ~30; each species evolves at a set level (~12–16).
  Step 9f: a win or a catch pays (5 + 4 x wild level) **x 3**; Cinderwood's wild Islings are 2 levels up. The shortest
  trail reaches the temple at **~Lv 8** (p10 7, p90 10; ~9.5 if you walk each grass twice). XP is visible: an XP bar and
  "→ next level" on the pod card and the lodge card; after a win a big "+XP" over the pad, the pad's XP bar at twice its
  size filling over ~1 s, "Lv N!" with a gold ring and a hop per level; benched Islings that fought: their pods glow
  with "+XP". First time: `xp.first` ("Your Islings learn from every battle. Watch them grow!").
- **Keepers:** temple hand puzzle, then a battle with a special phase (e.g. shielded until hit by its weakness).
- **Losing everything:** explorer returns to the last Hearth Shrine, team healed, nothing lost.

## Capture
- Weaken, then flick a glass pod (brass band). Lower HP = better chance.
- **Catch rule (Oct 24, fixes 2):** no Catch rune. The big empty pod (6 cm) waits in the cradle from the moment the
  wild Isling appears and can always be thrown. Above red HP it wobbles once and shatters (turn used, "Weaken it
  first!" the first time); in the red (≤ 25%) it glows and has a real chance; a fainted wild Isling is **dazed for
  8 s** (the pod pulses): a pod catches it for sure (rares 75%); no throw = it fades out, battle won, XP as usual.
- **Golden Berry** (rare): craft from berries (bushes regrow on real-time timers) + orbs (one per temple,
  plus hidden spots). Befriends a wild creature without battle. Rare creatures and Keepers refuse it.
  First feature to cut if late.

## Party and collection
- **Party:** 6 glass pods in sockets on the island's rocky rim. Look at one (it grows) for its card; **throw the
  pod** onto the battle pad to send the Isling out (or look + pinch it).
- **7th catch and beyond:** goes to the Hearth Lodge automatically (light streak off the island).
- **Hearth Shrine (decided Oct 24, step 7b):** a 2 x 2 stone hut with a flame beacon, visible from across the window.
  Stand on any tile touching it (its flame leans toward you), then **warm your hand over the flame** (palm down over
  the bowl, 0.8 s, a ring fills) or look + pinch it: flame flares, a warm wave heals the rim pods, the game saves, and
  the island **swaps into the Hearth Lodge** (content sinks, the base tints, the room rises). Losing everything
  returns you outside the last shrine used.
- **Hearth Lodge (replaces the Sanctuary island, cut list #3):** a small dollhouse room inside the rim (back wall and
  half side walls, honeycomb pod wall (back left), hearth (back centre), door arch (back right, facing the player, step 9i:
  on the near edge players walked out by accident while reaching for the rim), rug, cushions, pedestal; the near edge is
  open). The explorer comes in at the back door and walks one tile toward the player. Up to 12 stored Islings wander, doze by
  the hearth and hop; the rest sleep in the honeycomb pods. Look + pinch (or poke) one: it hops onto the pedestal with
  its full card (Lv, element, HP, XP, moves, ATK / DEF / SPD). Grab one and drop it on a rim socket to bring it along
  (swap if full); toss a rim pod onto the floor to leave it in the lodge (never the last one). Walk out the back door (swipe
  toward it, tap or look + pinch a door tile, or look + pinch the arch).
  The same scene swap opens realm gates (step 8) and temple arenas (step 9). Counter "Caught 10 / 30" later.

## Opening (first 5 minutes)
1. A broken shard drifts into your room and unfolds into the Still Isle. The explorer wakes.
2. The Lantern Spirit appears by your hand and greets you.
3. Pick 1 of 3 starters (one per element). Doesn't lock region order.
4. Walk with thumb gestures to the first tall grass; first battle and first catch within ~3 minutes.
5. The statue is visible, unexplained.

**As built (step 8, `Opening`):** new game only (no save). Passthrough room, a 12 cm mossy shard with gold cracks
drifts in from ~1.5 m and settles where the island will be. Touch it (fingertip, palm, or look + pinch); after 3 s the
Spirit's `intro.shard`, after 6 s it unfolds by itself. It cracks open with light, the base grows out of it, the tiles
rise from the middle, the explorer pops up and hops twice. A spark leaves the statue and becomes the Spirit
(`intro.arrive`). `intro.world` makes the rim glow and waits for a grab or 12 s. Three glass pods rise from the statue
and float over the front half (Kilnpot left, Pebbloop middle, Pufflet right, as the player sees them): looking at one
makes it grow and wiggle and the Spirit names it; drop it on the rim or look + pinch to pick (no confirm), the others
sink back into the statue. Then `intro.explorer` and a gold run of tiles toward the first grass. Nothing saves before the
pick (quitting replays the opening). Returning players: the island rises from the middle in 2 s where it was placed.
Flat Editor: launch to walking about 45 s (10 s of it waits for a head pose that never comes), on the device about 35 s.

**Lessons (step 8b, `Onboarding`, data `Data/Onboarding/Onboarding_Main`):** one at a time; each waits for its moment,
says its line (a shorter reminder every 10 s), shows a ghost hand doing the gesture, and on done plays a soft chime and
the Spirit's praise (Nice! / Just like that! / Perfect!). Order: touch the shard (hint after 3 s, opens by itself at 6 s),
move the island (rim, skips after 12 s), pick a starter (look, grab, drop on the rim), walk 3 tiles (thumb swipe demo
above the island, the Spirit beside it; after 30 s "Or look at a spot and pinch."), stop (thumb tap), look + pinch the gold
tile, follow the gold tiles to the tutorial grass (always the scripted Toastling Lv 3), toss the pod in (auto send-out
waits 20 s in the tutorial and doesn't earn praise), pick a move, flick the cradle pod once the wild one is in the red,
then free roam: the first win or catch turns wild battles and fishing on. Until then other tall grass only rustles and
the statue, shrine, gate, grass and pond hints wait. A lost or fled tutorial battle goes back to the grass lesson.
Doing something early (tapping to stop during the walk lesson) completes that lesson when it comes up. The current
lesson is saved; older saves count the tutorial as done. Hint lines expire: a battle or scene swap closes them, and a
line with a `while` condition closes as soon as it stops holding (control hints never show in battle).
**Ghost hands:** a copy of the player's own ISDK hand mesh, translucent pale blue with a bright rim (not gold), drawn
over the island, never grabbable. Real size at the shard, rim, pods, runes and cradle; 60% above the island for the
thumb demos. It shows the hand used last (mirrored), slows to 0.6x and grows 20% after 3 loops, and with no hands
tracked for 3 s the Spirit says "Show me your hands!" while both ghost hands rise in front of the island.

**First minutes clarity (step 9h, Oct 26; from the QA tester's first run: too much text, too fast, instructions gone
before the action is done, pinch never taught):**
- **One instruction at a time, and it stays.** While a lesson teaches, the Spirit parks beside its target and the
  lesson line stays up (`Dialogue.Pin`). After ~6 s the bubble shrinks to a **hint chip** (a short verb + a small hand:
  "Grab the edge", "Pinch!", "Look + pinch", "Toss the pod"...) that stays until the lesson is done; looking at the chip
  or the Spirit opens the full line again. On done: the chime and the praise, the chip goes, the next lesson. No other
  line replaces a lesson line: story lines wait until it is done; idle hints are off during the tutorial.
- **Reading pace:** every line stays at least 1.5 s + 0.06 s a character after it is typed in; a more important line
  waits for that minimum instead of cutting it off. The first poke or pinch on a line still typing shows it whole.
- **Less text before the starter:** one line each: "Something's here. Touch it?", "Hello, big one! I'm a Lantern
  Spirit.", "This little island is yours. Grab its rocky edge to move it.", "Pick an Isling friend!" then "Look at one
  and pinch, or grab it.". "The world broke into floating pieces" moved to the statue beat.
- **Pinch is taught** right after the Spirit arrives (both slots): "Tap your thumb and finger together. Like this!", the
  ghost pinches right beside the player's own dominant hand (~8 cm outside it, following it), each pinch sparkles gold
  at the fingertips, done after 2 (the opening moves on after 45 s without them).
- **Focus language on pods:** every pod the player can act on now has a gold rim (starter pods while picking, the lead
  pod while sending out, the armed cradle pod); a looked-at or touched pod turns white and grows ~8%.
- **Ghosts on the real objects (retarget, no re-recording):** a grab clip's grab and release frames come from its
  thumb-index gap; the grab lands on the lesson's real source and the release on its real target, blended over the carry.
  Island move: the near rim's corner on the dominant side. Starter: the looked-at pod, else the next one each loop (left,
  middle, right), set down on the rim socket. Send out: the real lead pod, let go over the near pad. Catch: the cradle pod.
- **Small fixes:** no room scan or scene-permission prompt ever (room data is used only if the headset already has it and
  the permission was already given: shadow + table snap; otherwise the island floats). Lodge: no palm-over-the-arch exit;
  a door tile exits only when the step was aimed out (a swipe toward the door, or a tap / look + pinch on it). Battles:
  the explorer turns to face the wild Isling; the Spirit waits beside the near rim on the non-dominant side; its bubble
  never sinks into the island.

**First-minute polish (step 14a, Nov 3; from Mugesh's notes after his trip):**
- **Spawn and recenter:** every placement (the shard, the island, the menu's Recenter, Meta's recenter) waits for a
  settled head (0.5 s within 1 cm / 1.5 deg, TrackingSpace still) and uses one rule: yaw-only forward, 45 cm out, the
  island's top 30 cm under the eyes (never above 15 cm under them, never below 55 cm over a known floor). Before it is
  touched the shard follows the player (looked > 35 deg away or sat down / stood up > 15 cm, for 1 s = it glides over);
  the island unfolds exactly where the shard is. Recenter = Meta's event, the OpenXR reference space change, or a
  TrackingSpace / head jump; a safety net re-places once if the island ends > 40 deg off or > 0.8 m away. Nothing else
  moves the island on its own. Every placement is logged and goes into bug reports.
- **Island handles:** grab only at the four corners (a faint bracket each, white when a hand is within 6 cm, gold held)
  or the front lip under the pods (exploring only). Pods always win near them; elsewhere on the rim pinches go to pods
  and objects. Two hands = any two handles.
- **Pinch lesson:** two progress dots under the line; the first pinch = a bigger gold burst, a chime, "Nice! Once
  more." (chip "Pinch again"); the second = "Perfect!".
- **The Spirit teaches from the spot:** while a lesson or objective teaches it parks just above the interaction point
  (over the ghost hand or the thing to touch, a little toward the player) with soft gold sunrays round it; plain chatter
  has no rays.
- **Pods onto the rim:** a held pod within 12 cm of the rim previews the socket it will fly into (a ghost pod).
- **Music On / Off** in the game menu (saved).

## Finale (step 13: the Heart wakes; no final battle)
Everything happens on the hub at the game's normal scale. Modelled on how creature games end (a hall of fame, credits,
the world stays open), with our own heart: the Lantern Spirit was a piece of the Heart all along, and the player gives
it back with their own hand.
- **The statue means something from the first shard.** Each shard that locks in makes it whisper a memory in the
  Heart's voice (`heart.dream.fire / wind / water`: its own bubble from the statue's head, no tail, soft gold-white,
  larger, letters fading in, no typing ticks), and the Spirit answers (`spirit.dream.1`: "Did the statue just...
  speak?", `.2`: "That voice... why does it feel like mine?"). It wakes a little with each: one = its closed eyes glow
  faintly, two = it breathes and its moss sways, three = the finale.
- **The Heart of the World** = the statue come alive (a shrine guardian, the sitting temple lion-dog kind, our own):
  on its haunches on the statue's plinth, at most ~1.3x the statue's height; mossy grey-green stone with gold veins
  from a core in its chest that pulse like a heartbeat; a mane of carved stone curls (three rings) with moss and tiny
  flowers; a curled tail ending in a ribbon of wind; a crown of the three elements (ember crest, lotus, cloud-wing
  plumes); cloud-wings that unfold; an empty round setting in its forehead for the Spirit; kind heavy-lidded eyes that
  open gold; a collar with the three shard gems. Four rigid props (body, head closed / open, a mirrored wing): 8 draws,
  24k tris.
- **The finale** (~2.5-3 min; walking, wild battles and the menu's Title wait):
  1. Wake: the sockets flare, light runs up the statue, stone flakes and moss fall, a burst of dust and gold, it is the
     Heart; it breathes and lifts its head: "...little light. You found your way home."
  2. The Spirit floats before its empty forehead: "I think... I'm a piece of it. Its light." "I have to go back. Will
     you take me?" (chip: Hold out your hand).
  3. **The goodbye in your palm:** an open hand palm up and the Spirit lands on it, glowing (chip: Lift it to the
     Heart). Within ~6 cm of the forehead it floats off into the setting with a soft chime ("Thank you, big one. For
     everything."). Look + pinch the Heart = it goes by itself. 30 s with nothing: "It's okay. Whenever you're ready."
     It never forces it. **Step 14f:** the forehead holds a **gold crystal** set in the stone, dull until now. As the
     Spirit lifts off the palm it turns into light (the lantern dissolves into a bright gold mote, ~0.3 s), flies into
     the crystal, and the crystal ignites (glow, a soft ring of light, the heartbeat). No lantern is ever seen in the
     forehead. The Spark later comes out of the crystal.
  4. Its eyes open gold, the wings unfold, the crown blazes; the hub answers (the gates bloom with their element, the
     grass waves, warm motes rise into the room, a golden ring of light under the island): "Guardian. You carried the
     world in your hands."
  5. **Hall of Fame:** "And these are the friends who carried it with you." The party comes out of the rim pods onto
     the path, each with a stone card (name · Lv · met in <realm>); two Lodge Islings wander out; everyone cheers. A
     Hall of Fame record (party + date) goes into the save.
  6. **Credits:** seven stone tablets rise one at a time over the island's far side.
  7. **The spark returns:** a tiny spark slips from the Heart's eye into the explorer's lantern and hops out as the guide,
     the Spark (the Spirit's look, smaller and warmer, no acorn cap); the Spirit's light stays in the Heart's forehead:
     "...hello again. I'll always be in your light." Free play.
- Quitting in the middle restarts at the goodbye. Dev panel Story > Finale: play from wake / from the goodbye.
- **Post-game:** poke the Heart: the Isling collection per realm (caught of each realm's roster incl. evolutions) and a
  warm line. The Spark hints at realms with Islings still to meet. A completed roster: its gate sparkles gold and the
  matching crown piece wears a halo. **Keeper rematch:** poke a calm Keeper in its hall: Spar? / Not now; a friendly
  match 5 levels over the last fight (no fury, a softer shield, XP, no shard); win or lose, it bows.

## Length and scope
- Full playthrough 1–2 hours. Judges play ~10 minutes, so the hub + first region must be excellent.
- Everything data-driven (creatures, moves, maps, temples, dialogue) so content is cheap to add.
- Polish region 1 fully before starting region 2.

## Audio
- Music: Gemini (Lyria). SFX and ambience: CC0 packs (Kenney, Sonniss GDC bundle, Freesound CC0).
- Keep `Assets/Docs/AudioCredits.md` listing every file and its licence.

## Open questions
- Game name, realm names, Keeper and Heart creature designs.
- Type chart beyond the triangle; whether to add a 4th element.
- XP curve, damage numbers, move list per species.
- Region map sizes (tiles) and how many screens per realm.
- Story beats per realm (what you find, who you meet).
