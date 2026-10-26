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
- **Walk, three ways, one path system:** (1) thumb swipe = auto-walk until tap / obstacle / new swipe, same swipe
  again = run; (2) **touch a tile** with a fingertip (dip and lift) = walk there; (3) **look at a tile + pinch** =
  walk there (reclined players). Thumb-hold walking dropped (accidental stops, tiring). Walk 0.4 s/tile, run 0.2.
- Tap: stop while walking, hop when standing, fish when facing water.
- Battle runes: poke or look + pinch; the panel faces you at battle start, drifts at most ±15°.

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
  Left-hand menu pinch (palm up) or F2 opens a 2D panel beside the island's left side; poke or look + pinch.
- Tabs: Go to (hub and Cinderwood landmarks, the Forge Hall), Party (heal, levels, team of 3 presets, add any species,
  one-hit KO, invincible), Encounter (fight any species now, wild on/off, rare x10), Story (tutorial, opening, realm
  restored, log, forge, Keeper, shard flags), Save (3 snapshots + auto, new game), Tools (fps, readouts, time x1/2/4,
  walk x2, report bug). QA guide: `Assets/Docs/QA-Guide.md`.
- Built on `GameState`: live changes apply at once; story changes edit the save and reload the scene. **Quick Journey**
  (the judges' short flow, own design session) will reuse it: a preset = a SaveData + a reload.

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
  half side walls, hearth, honeycomb pod wall, rug, cushions, pedestal, door). Up to 12 stored Islings wander, doze by
  the hearth and hop; the rest sleep in the honeycomb pods. Look + pinch (or poke) one: it hops onto the pedestal with
  its full card (Lv, element, HP, XP, moves, ATK / DEF / SPD). Grab one and drop it on a rim socket to bring it along
  (swap if full); toss a rim pod onto the floor to leave it in the lodge (never the last one). Walk out the door.
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

## Finale
- All 3 shards in the statue. It wakes: the **Heart of the World**, a huge ancient creature.
- Final battle with your team. On victory the shards rejoin and the world is whole.

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
