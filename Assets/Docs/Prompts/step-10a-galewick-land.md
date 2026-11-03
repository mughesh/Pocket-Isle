# Step 10a: Galewick land (map, look, props, gate)

Read CLAUDE.md, `.claude-handoff.md` (9a Cinderwood land, 8 gates, 9c restoration), **`Assets/Docs/RealmsDesign.md`
(Galewick)**, `ConceptArt/01_realms_diorama_sheet.png` (wind diorama), `Concepts_01_Summary.md` (wind palette and props).
Decided Oct 26 night. Same pipeline as 9a: data-driven map, props as SDF recipes, zones, landmarks.
Before Play over Link, ask Mugesh to restart Unity.

## A. Map (60 x 60, trail + pockets)
1. Generate and hand-place Galewick like Cinderwood: arrival meadow + return gate → windmill village → kite field
   loop → windy ridge → wind tower on the ridge top. Side pockets: rare grass patch, fire-visitor patch, a Hearth
   Shrine near the village. Rolling-hill feel through tile height steps (1–3 steps), dry-stone walls and fences as
   edges, a ditch in front of the big mill (the drawbridge spot, 10b), a narrow ridge path where the cloud bank sits
   (10b), the kite field as an open loop with 3 kite spots (10b).
2. Zones for encounter tables (village fields, kite field, ridge grass, rare pocket, fire-visitor patch). Tables can
   stay placeholder until 10c (Pufflet + fire visitors), but wire the zones now.
3. Landmarks for the dev panel Go to (arrival, village, big mill, kite field, ridge, tower, shrine).

## B. Look and props
4. Theme: wind grass (wheat `#E3C66F` tips, meadow green base, taller and thinner than Cinderwood's), palette from
   RealmsDesign. **Unrestored = nothing moves:** grass sway off (stiff, tiny idle only), mill sails stopped, kites lying
   in the grass, low clouds parked. Restored state (wired to `RealmRestoration` like Cinderwood, toggled from the dev
   panel): grass waves, sails spin, kites on posts fly, drifting seeds, clouds drift.
5. Props (recipes, ≤ 10 new, palette-swap where possible): windmill (stone tower + 4 white sails on a pivot: sails are
   a rigid part that spins), small mill, cottage, dry-stone wall, wooden fence, kite (diamond, tail of 4 bows, 3
   colour swaps), kite post, hay bale, signpost, low cloud puff (soft toon blob, slightly translucent edge), wind
   tower (tall round stone tower, wooden vanes at the crown, arched door).
6. Hub wind gate (the tunnel with the little windmill) **opens** to Galewick through the existing gate travel, with a
   Galewick return gate by the arrival. Base material on travel: pale "cloud stone".
   **QA/Release builds:** keep the wind gate sealed until `GalewickConfig.open` is true (default true in the Editor,
   false in builds until 10d), so the tester's Goa build stays on the finished content. Dev panel Go to always works.

## C. Levels across realms
7. Wild levels and Keepers scale with **shards held**: +3 wild levels, +2 Keeper levels per shard (config). Re-run the
   Keeper sim for Hearthstag at 1 shard (a player who did Galewick first) and report.

## Housekeeping
- Perf: Galewick at 72 fps in the Editor estimate, draw calls ≤ Cinderwood's. Shots `Logs/BattleShots/10a_*`
  (arrival, village, kite field, ridge, tower; unrestored and restored).
- EditMode tests green. Commit per section with "(headset test pending)", no AI attribution, don't push. Update
  `.claude-handoff.md`, GameDesign.md (Galewick as built), QA-Guide (Galewick via dev panel only).

## Test
1. Dev panel Go to Galewick: windmill highlands, bright but completely still.
2. Walk the trail: village → kite field → ridge → tower; pockets off to the sides.
3. Dev panel restored toggle: everything starts moving.
4. In the Editor the hub's wind gate takes you there and back.
