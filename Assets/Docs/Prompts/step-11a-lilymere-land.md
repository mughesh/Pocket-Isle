# Step 11a: Lilymere land (map, water looks, props, gate)

Read CLAUDE.md, `.claude-handoff.md` (10a Galewick land = the template, 9a Cinderwood), **`Assets/Docs/RealmsDesign.md`
(Lilymere)**, `ConceptArt/01_realms_diorama_sheet.png` (water diorama), `Concepts_01_Summary.md` (water palette,
props, Tidemother). Same pipeline as 10a: `LilymereTools` like `GalewickTools`, seeded, data-driven map, props as SDF
recipes, zones, landmarks, theme with unrestored / restored looks wired to `RealmRestoration`.

**Story:** Tidemother, a giant gentle sea-turtle with a lily garden on her shell, watched the lagoon drain when the
world broke. Terrified of losing what's left, she pulled the water into her half-sunken temple and froze the waterfall
so nothing could flow away.

## A. Map (60 x 60, trail + pockets)
1. Route: arrival (return gate, by the hub's **well** gate) → **stilt village** (huts on posts over shallows, little
   plank bridges, Hearth Shrine) → **lily channel** (a 2–3 tile wide water channel across the trail, 11b A) →
   **lotus garden** (a dry, cracked pond bed the trail must cross, 11b B) → **waterfall basin** under a cliff (11b C) →
   **lake temple** (half-sunk in the lagoon; its door is under water until the lagoon rises, 11c).
   Side pockets: reed marsh (tall reeds = tall grass), rare pond (Bottlewhale), wind-visitor meadow, a lookout jetty.
   **Paths in straight legs** (Mugesh: zig-zags are hard to walk), no tile heights (as 10a decided).
2. **Water tiles:** the lagoon and shallows use the existing Water tile. Unrestored = the lagoon is **half dry**: a
   ring of "dry bed" tiles (pale cracked sand, walkable only where the trail is) that become Water when restored or
   when the lagoon rises (11b C). Make that a map layer (`riseTiles`) so the switch is one call, saved.
3. Zones: village shallows, channel reeds, lotus garden edge, marsh, rare pond, wind meadow. Landmarks for the dev
   panel Go to (arrival, village, shrine, channel, garden, basin, temple, marsh, rare pond).

## B. Look and props
4. Palette: water teal `#5FB8C4` → deep `#2F7F95`, lily green `#5AA868`, lotus pink `#F2A7B8`, reed `#B7B26A`,
   sand `#E8D8B0`, stilt wood `#8B6A4A`, temple stone `#A9B8B0` with moss.
   **Unrestored = still and glassy:** water flat, no ripples, a little desaturated; lotus closed buds; the waterfall
   **frozen mid-fall** (a pale glassy sheet with stopped droplets, faint cracks). **Restored:** water ripples and
   sparkles (cheap: scrolling normal-ish highlight bands in the toon water shader), lotus open, waterfall runs (11b C
   prop states), mist puffs at its foot, dragonflies (2–3 tiny fx specks).
5. Props (≤ 10 recipes, palette swaps where possible): stilt hut, plank bridge, jetty, reed clump, lily pad (flat
   disc with a notch; 3 sizes), lotus bud / lotus open (two states), cattail, frozen waterfall + running waterfall
   (one prop, two looks), stone basin (round, dry / full), lake temple (half-sunk stone shrine with a moss roof, a
   round door), stepping stone.
6. Hub **well** gate opens to Lilymere through gate travel, return gate by the arrival. **Builds:** sealed until
   `LilymereConfig.open` (false in builds, true in the Editor), like `GalewickConfig`.

## Housekeeping
- Perf: draws ≤ Galewick's per spot; shots `Logs/BattleShots/11a_*` (map, each place unrestored and restored).
- Audio: if the audio hookup is merged (`AudioService` exists), route any new procedural sounds through the SFX bus
  and add missing cues to the list in `Assets/Docs/Audio/PhaseB-Hooks.md`; add Lilymere faded/restored ambience
  profiles (water lapping, frogs and birds when restored; still air when faded) and a `music.lilymere` state (cue
  empty if no file yet).
- EditMode tests green; commit per section "(headset test pending)", no AI attribution, don't push. Update
  `.claude-handoff.md`, GameDesign.md (Lilymere as built), QA-Guide.
