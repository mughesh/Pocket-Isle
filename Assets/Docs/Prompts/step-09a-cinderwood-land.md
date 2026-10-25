# Step 9a: Cinderwood, the land

Read CLAUDE.md, `.claude-handoff.md`, GameDesign.md ("Cinderwood"), BuildPlan.md, PolishBacklog.md, Script_v1.md.
Decided Oct 26 (design chat): **every realm is 60×60** (dense, not 100×100: the test map felt empty). Layout =
**one winding trail through five beats, with side pockets** for exploring. A "Quick Journey" menu option for judges
(follow the trail only) comes later, so the trail alone must be a complete, satisfying route.
9a = the walkable realm. **Not in 9a** (that's 9b): lifting the log, the temple interior, bellows/brazier puzzles,
Hearthstag, the restored-realm switch, the shard.

## A. Layout (x = west→east, y = south→north; south = the arrival gate, nearest the player)
| Zone | Tiles (x, y) | What's there |
|---|---|---|
| **Arrival** | 6–14, 2–8 | Return gate to the Still Isle; you arrive facing north; first ember grass in view |
| **Forest trail** | 4–22, 8–24 | Dense autumn trees on both sides, the trail winds north, ember grass patches beside it, leaf piles, a signpost |
| **Ember field** (pocket) | 26–50, 2–14 | Open meadow of tall ember grass with rock islands and a few lone trees; the classic "grind" field |
| **Lantern glade** (pocket) | 30–44, 16–26 | A ring of stone lanterns (unlit) around a clearing, quiet; Waxle country |
| **Kiln village** | 6–26, 26–40 | 4–5 clay huts with chimneys, 3 kilns, pot clusters, woodpiles, fences, lantern posts, a small plaza; **Hearth Shrine** at the village's east edge |
| **Berry grove** (pocket) | 46–56, 24–36 | Berry bushes in rows, a fence, grass between rows |
| **Hot springs** | 30–46, 34–46 | 3–4 round pools (H tiles) with stone rims, boulders, grass ring around them (water visitors later) |
| **Fallen log** | trail at 18–22, 44–46 | The trail narrows into a rocky gap; a big fallen log blocks it completely (closed blocker in 9a) |
| **Cliff forge temple** | 10–30, 48–58 | A cliff wall across the north edge; the **forge door** (big arched forge mouth, cold) at 18–22, 56; three unlit braziers in front |
| **Kiln ruins** (pocket) | 44–56, 46–58 | Broken kilns, glazed pot shards, cracked ground; reached by a narrow hidden path from the springs; **rare grass** (Glazegolem) |

Trail order: arrival → forest → village (shrine) → east to the springs → back west and north to the log → temple.
Rules for the layout:
1. The trail is 2 tiles wide (path tiles), always readable, with gentle bends (no straight run longer than 10 tiles).
2. **Something every ~8 tiles** along the trail: a prop cluster, a grass patch, a view of a landmark. Never more
   than 6 plain grass tiles in a row beside the trail.
3. Tall grass covers ~20% of the walkable land, in patches of 6–20 tiles, some touching the trail and some off it.
4. Pockets are reached by side paths (1 tile wide) or gaps between trees; the ruins path is narrow and half-hidden
   by trees (worth finding).
5. Map edges: dense trees, rocks or the cliff; never a bare edge.
6. Build it with the map generator (zones + rules above, fixed seed), then hand-place the beats (huts, shrine, log,
   door, braziers, pools). Save as the real `Cinderwood` map (replaces the test map; keep a copy of the old one
   in Data/Archive).

## B. Encounters per zone (data tables, one per zone)
| Zone | Table | Levels |
|---|---|---|
| Forest trail | Toastling 60 · Waxle 30 · Kettlewing 10 | 3–5 |
| Ember field | Toastling 50 · Kettlewing 30 · Waxle 20 | 4–6 |
| Lantern glade | Waxle 70 · Toastling 30 | 5–7 |
| Berry grove | Kettlewing 60 · Toastling 40 | 5–7 |
| Hot springs | Kettlewing 70 · Waxle 30 (water visitors replace this later) | 6–8 |
| Kiln ruins | Glazegolem **25%** · Toastling 40 · Waxle 35 | 7–9 |
- Elsewhere the ~5% rare surprise stays. Existing "uncaught 2×" and "New!" rules apply.
- Zone ids live on tiles (a zone grid like the props grid) so later realms reuse the same system.

## C. New props (prop kit, Cinderwood palette, unrestored + restored colours)
Max 10 new recipes. Reuse existing ones (Kiln, LanternPost, StoneLantern, Brazier, BerryBush, PotCluster, FallenLog,
HotSpringSteam, AutumnTree) wherever possible.
1. **ClayHut**: round clay hut, conical thatch/clay roof, little chimney, round wooden door, 2×2 tiles.
2. **Fence**: wooden posts + rail, straight and corner variants.
3. **Signpost**: a post with two arrow boards (no text).
4. **Woodpile** and **LeafPile** (small, cheap filler).
5. **CliffWall**: tall layered rock segments, 2 variants, tile-aligned, ~3× a tree's height, dark warm stone.
6. **ForgeDoor**: a big arched forge mouth set into the cliff, iron-banded, cold dark inside (9b lights it).
7. **RuinedKiln**: a broken kiln with a fallen chimney; **PotShards**: scattered glazed shards.
8. **EmberVent**: small glowing cracks in the ground; dim when unrestored, bright when restored.

**Tree fix** (from testing: canopies read as mushroom caps): rebuild `AutumnTree` canopies as 2–3 stacked, slightly
offset rounded blobs, wider in the middle, **no dark blob underneath**, a bit more trunk showing, leaf colour from
deep red-orange at the base to gold at the top. Add a second variant: **tall narrow tree**. Mix both.

## D. Dialogue (Script_v1.md, reimport; Islings wording)
- `cinder.village.first` → village plaza: "A kiln village. The kilns are cold... everyone's gone quiet."
- `cinder.springs.first` → a pool: "Hot springs! Well, warm-ish. Not much steam."
- `cinder.log.first` → the log: "A huge log blocks the path. Too heavy for the explorer..."
- `cinder.temple.first` → the forge door: "A forge in the cliff. The fire came from here."
- `cinder.ruins.first` → ruins: "Old kilns. Something glazed is moving in the grass..."
- Keep `realm.cinderwood.first`.

## E. Checks
- Perf: walk the whole trail in the Editor with the perf tour; then one device capture of the village (most props)
  and the springs (steam). Target 72 fps; report draw calls and tris.
- Walkability: an automated check that the trail connects arrival → shrine → springs → log tile, every pocket is
  reachable, and nothing walkable is cut off.
- Save: entering Cinderwood from the hub and the Lodge return still work on the new map.

## Housekeeping
- One compile batch per section; EditMode tests green. Test in Editor Play over Link.
- Commit per section with "(headset test pending)". No AI attribution. Don't push. Update `.claude-handoff.md`,
  GameDesign.md (Cinderwood as built, 60×60 rule for all realms), BuildPlan.md.
- Summary for Mugesh: a **top-down map screenshot with zone labels**, what to test, the perf numbers.

## Test
1. Fire gate → Cinderwood: you arrive by the gate, the forest trail leads north.
2. Walk the whole trail: something to look at every few steps; nothing feels empty.
3. Village: huts, kilns, the shrine; the springs: pools; the log blocks the way; the forge door sits in the cliff.
4. Find the ruins path off the springs; rare Glazegolem grass there.
5. Each zone's grass gives its own Islings at its own levels.
6. Trees look like trees now, in two shapes.
7. 72 fps in the village and at the springs.
