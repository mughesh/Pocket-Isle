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
- **Explorer:** tiny character on the tiles, moved with thumb microgestures.
- **Guide:** the **Lantern Spirit**, a small floating creature near your hand. Short text bubbles, hints, story. No voice.
- **Story hook:** the world shattered into floating pieces, one per element. Each Shard Keeper (region boss)
  was frightened or corrupted by the shattering. They aren't evil. Beat one and it calms, giving up its shard.

## World structure
- **The Still Isle (hub, neutral).** Where the game starts. A statue in the centre with 3 empty sockets,
  never explained. Each shard you bring changes it a little (cracks glow, moss falls). Third shard wakes it.
- **3 elemental realms:** Fire, Water, Wind (type triangle). Push for a 4th (Earth/Plant) if ahead.
  Each realm is a large map to explore; the island shows only a window of it, scrolling as the explorer walks.
- **Shard Gates** (well, tunnel, arch) travel between realms. Transition: tiles sink into the base, the rocky
  base changes material (lava rock, coral, cloud stone), new realm's tiles rise. Built once, reused everywhere.
- **Region order is free.** All 3 gates open from the hub. Wild levels and Keepers scale with shards held.
- Each realm has: tall-grass areas, berry bushes, a Hearth Shrine or two, a **Temple** with its Keeper, hidden orbs.

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

## Encounters and creature spread (decided Oct 22)
- **Visible wanderers:** common creatures roam the tiles; walk into one to battle.
- **Tall grass:** random encounters, mostly commons, plus ~5% **rare/powerful surprise** (gold sparkle at zoom start). The hook.
- **Spread per realm:** ~70% home element, ~20% visitors of the type that beats that realm's Keeper, ~10% Neutral.
  No Keeper is a hard wall: Keeper shields also break after 3 hits of any type; the counter type is just faster.

## Cinderwood (fire realm, the vertical slice) — decided Oct 22
- **Size 100×100 tiles** (~1 min walk edge to edge at 0.6 s/tile; island window is 14×14). Something every ~15 tiles.
- **Layout: main trail + side pockets.** Gate → forest trail → kiln village → hot springs → fallen log (hand moment)
  → cliff forge temple. Side pockets hide rare grass patches, berry bushes and an orb.
- **Habitat:** ember tall grass (red-gold). Water visitors live in the tall grass around the hot springs.
- **Residents (later, if cheap):** a few friendly creature villagers in the kiln village with one-line hints.
- **Temple:** pump the forge bellows (open/close hand) → heat flows through pipes → lights 3 braziers → Keeper door opens.
- **Hearthstag fight:** ember-crown shield blocks damage until hit by a Water move (or 3 hits of anything), then open for 2 turns; repeats.

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

## Core loop
explore → tall grass encounter → battle → capture → party / Sanctuary → Temple Keeper → shard → statue

## Creatures
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
- **Flow:** grab a pod from the rim → toss it onto the island → your creature pops out facing the wild one →
  move runes float just above the near rim → weaken → flick a pod to capture → zoom out.
- **1v1, turn-based, switch any time** from the party of 6 (switching uses your turn). Flee from wild battles.
- **3 moves per creature:** one element attack, one basic (neutral) attack, one support (shield / buff / heal).
  Built from four animation templates (shoot, lunge, slam, shield) recoloured per element.
- **Pick moves with gaze + pinch** on 3D runes above the near rim. Works reclined, no reaching.
- **Types:** Water > Fire > Wind > Water, plus **Neutral** (hub creatures; no strengths or weaknesses).
  Strong ×1.5, weak ×0.67 (tune later).
- **Levels:** XP to creatures that fought; level cap ~30; each species evolves at a set level (~12–16).
- **Keepers:** temple hand puzzle, then a battle with a special phase (e.g. shielded until hit by its weakness).
- **Losing everything:** explorer returns to the last Hearth Shrine, team healed, nothing lost.

## Capture
- Weaken, then flick a glass pod (brass band). Lower HP = better chance.
- **Golden Berry** (rare): craft from berries (bushes regrow on real-time timers) + orbs (one per temple,
  plus hidden spots). Befriends a wild creature without battle. Rare creatures and Keepers refuse it.
  First feature to cut if late.

## Party and collection
- **Party:** 6 glass pods in sockets on the island's rocky rim. Pinch one for stats; pull one to send it out.
- **7th catch and beyond:** flies to the Sanctuary automatically (light streak off the island).
- **Hearth Shrine:** rest to heal the party and save; the only place to swap party and Sanctuary creatures.
- **Sanctuary island:** a second small island you summon any time. Caught creatures roam it, one corner per element.
  Pick any up to inspect it in your hand. Counter "Caught 10 / 30". Uncaught species are not shown.

## Opening (first 5 minutes)
1. A broken shard drifts into your room and unfolds into the Still Isle. The explorer wakes.
2. The Lantern Spirit appears by your hand and greets you.
3. Pick 1 of 3 starters (one per element). Doesn't lock region order.
4. Walk with thumb gestures to the first tall grass; first battle and first catch within ~3 minutes.
5. The statue is visible, unexplained.

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
