# Overnight run, Oct 27 → 28: Lilymere

Prompt: `Prompts/overnight-oct28.md` (started from Mugesh's "work on the next big task, Lilymere" before the prompt
arrived; 11a was already under way and matched step 1). Everything tested in flat Editor Play through the bridge and
EditMode tests (197/197). **Nothing tested with real hands or on the headset.** No pushes. The well (water) gate is
**sealed in builds** (`LilymereConfig.open` = false); dev panel Go to reaches Lilymere either way.

Earlier the same evening (before this run): audio branch merged and hooked up (`07c3056`, `7941eb1`, `c6e9964`), the
tester's QA APK built from that and kept as `Builds/PocketIsle-QA-oct27-tester.apk`.

## Done / partly / not started

| Step | Section | State | Commit |
|---|---|---|---|
| 11a | A map, zones, landmarks, gate, dev panel spots | done, flat-checked | 80b67bc |
| 11a | B look (water ripples, dry bed tile, lagoon rise), props, theme | done, flat-checked | 80b67bc |
| 11a | ambience LilymereFaded / Restored, `music.lilymere` state | done (beds reuse `amb.pond`; no music track) | 80b67bc |
| 11b | A lily pad bridge | done, flat-checked (bridge pushes and look + pinch) | b0c3bbe |
| 11b | B rain hand | done, flat-checked (synthetic palm) | b0c3bbe |
| 11b | C scoop and pour + waterfall + lagoon rise | done, flat-checked (synthetic hand: dip, lift, pour, spill, pour) | b0c3bbe |
| 11b | docs, dev panel story flags | done | 67d0f22 |
| 11c | A water roster | **not started** (see "Decisions") | |
| 11c | B Tidemother, temple interior, water shard, statue socket | **not started** | |

## Test first (headset, over Link: restart Unity first, tick Standalone "Initialize XR on Startup")
1. **Dev panel > Go to > Hub: water gate**, walk in (Editor: open): Lilymere arrives, "the lagoon used to reach right up
   to here". Water is still and greyish, the lagoon half dry (pale sunk sand round it), nothing ripples.
2. **LM: channel**: three lily pads drift beside the crossing; the crossing is open water with three gold rings. Push a
   pad with a fingertip along the water toward a ring (a couple of pushes: drag stops it in ~3 tiles). It glides in
   with a splash. All three: ripples along the channel, "A lily pad bridge!", walk across. Also try look + pinch on a pad.
3. **LM: garden**: hold one hand palm down over the dry garden, 5-25 cm above the island, still for a moment: a little
   cloud forms under your palm and rains. Wiggle your fingers: harder rain. The five buds on the path bloom one by one
   into big lotus stones (2 s each, 1 s hard). Then the garden crossing opens. Look + pinch-hold a bud also rains on it.
4. **LM: basin**: cup a hand palm up, dip it into the little pool just south of the path (below the water surface), lift
   it out: a ball of water wobbles in your palm. Carry it to the stone basin and tip your hand over above it: it pours.
   Tip it anywhere else: splash, gone. Second pour: cracks run up the frozen waterfall, it shatters and runs, the lagoon
   floods its dry ring, a gush at the temple door. Look + pinch the pool, then the basin, also pours.
5. **Story tab > Lilymere restored: on**: everything solved, ripples and sparkles, lotus open, waterfall running.
6. **Story tab > LM pads / LM rain / LM basin + rise** toggle each beat (edits the save, reloads).

## Feel numbers to tune (Inspector, `Hand Puzzles/Lilymere/...`)

| What | Value | Where |
|---|---|---|
| Pad push: fingertip reach | 3.5 cm + half the pad, height band 3 cm | Lily Pad Bridge > Touch Radius / Touch Height |
| Pad push share / drag / bounce | 0.8 / 0.8 per s / 0.5 | Push Share / Drag / Bounce |
| Pad snap into a slot | 0.6 tiles | Snap Radius |
| Pad size drifting / locked | 1.5 / 2.1 x LilyPad | Pad Scale / Slot Scale |
| Rain: palm down | within 40 deg of straight down | Rain Garden > Max Angle |
| Rain: height band | 5-25 cm above the island | Min / Max Height |
| Rain: hold to form | 0.6 s (drops < 0.3 s keep it) | Form Seconds (RainCloud.grace in code) |
| Rain: wiggle | summed fingertip speed 0.6 m/s | Wiggle Speed |
| Rain reach | 1.3 tiles round the point under the palm | Rain Radius |
| Bud bloom | 2 s (1 s hard), look + pinch-hold 2.5 s | Soft / Hard / Look Seconds |
| Scoop: cupped | palm normal · up ≥ 0.55 | Scoop And Pour > Cup Up |
| Scoop: dip / lift | 1 cm under the surface / 2 cm over it | Dip Depth / Lift Height |
| Carry: keeps / tips | ≥ 0.1 / ≤ -0.35 | Keep Up / Tip Up |
| Over the basin | 1.2 tiles round its middle, up to 30 cm | Basin Radius / Basin Above |
| Pours to fill | 2 | Pours Needed |
| Ball in the palm | 3 cm across, 1.2 cm above the palm | Ball Size / Ball Lift |
| Ice cracking / lagoon wave | 1.2 s / 3 s | Crack Seconds / Rise Seconds |

## Decisions I made (without asking)
- **11c not started.** 11a and 11b took the night with flat checks of every moment. Ten new creature recipes plus a
  Keeper model are judged by eye on the sheets; doing them half-awake at the end of a long session risked a roster
  you'd have to redo, so I stopped at a clean, tested 11b and kept the night's work safe. Tables stay placeholders
  (Pebbloop, Steambara, wind visitors; Riverloom rare in the pond).
- **New tile D (dry bed)** instead of a separate `riseTiles` list: every D tile is the rise layer (`WorldMap.SetRisen`,
  runtime only, never serialized). `IslandMap.At` became virtual; `Raw` reads the layout as written. The lotus garden's
  bed is D too, so it floods with the lagoon (fine once restored).
- **Crossings are path under blocking covers** (ChannelGap water sheet, BedGap cracked sand) that the pieces hide and
  open, like Galewick's drawbridge. The map checks count them as gates.
- **Scoop inlet**: the lagoon's open water was 11 tiles from the basin (not both in the window), so a 3 x 2 inlet of
  water sits under the causeway 5 tiles from the basin, joined to the lagoon by dry bed that floods on the rise.
- **The rise floods every dry tile at once** (one island rebuild) with a wave of splashes spreading from the basin over
  3 s, rather than tile by tile (that would need per-tile rebuilds).
- `LilymereConfig` is a subclass of `GalewickConfig` (same open / openInEditor rule); `ShardGates.lilymere` seals the
  water gate. The hub's water gate now leads to Lilymere (`Step8Setup.WaterLink`); the hub test was updated for that.
- The lagoon temple's door "surfacing" is a gush only; opening it is 11c.
- Ambience beds reuse `amb.pond` (no Lilymere bed yet); rain uses `sw.drip`; pad clicks and pours use `pond.splash`;
  the ice uses `catch.shatter`; the rise uses `restore.swell`. Missing cues listed at the bottom of
  `Docs/Audio/PhaseB-Hooks.md`.
- No frog on the pads, no wet-sand darkening or grass perking under the rain (left for polish).
- Standalone "Initialize XR on Startup" was already off when the run started (Mugesh had unticked it); left off.

## Blocked or reverted
- Nothing reverted. Two shell heredocs broke on quotes (no changes made); scripts went to files instead.
- **Audio settings slipped (my mistake):** the evening's Project Settings > Audio edit was made on disk with the Editor
  open, and Unity wrote its own copy back (DSP 1024, Meta XR Audio spatializer). Set again through Unity's settings
  object (`AudioHookupDebug.ApplyProjectAudio`: requested DSP 256, 48 kHz, 32 / 64 voices, no spatializer), committed
  `37d054c`, and the overnight APK rebuilt with it. **The tester's evening APK still has DSP 1024** (AudioService doesn't
  use the spatializer, so only latency differs).
- Props over their triangle budget even after simplifying (the decimator stops early on these shapes): StiltHut 2960
  (2600), Lotus 1898 (1000), LakeTemple 4914 (3800), LotusStone 1016 (900), WaterBall 812 (700), BasinWater 494 (300).
  All well under the creature budget; the lotus is placed ~15 times on the map.

## Shots (`Logs/BattleShots/`)
`11a_village`, `11a_channel`, `11a_garden`, `11a_temple`, `11a_basin`, `11a_temple_restored`, `11a_garden_restored`,
`11a_hub_well_gate`, `11a_arrival`, `11a_temple_restored2`; `11b_pads_start`, `11b_pads_push`, `11b_pads_locked`,
`11b_covers_channel`, `11b_covers_garden`, `11b_rain`, `11b_lotus_path`, `11b_waterfall_frozen`, `11b_ball_on_palm`,
`11b_lagoon_rising`, `11b_waterfall_running`.

## Perf
- Not measured with a capture this run. Extra draws per moment by construction: pads 3 views + 3 rings/orbs; rain 5 buds
  + 5 stones (only one of each shows per bud) + 1 cloud + pooled fx; scoop 1 ball + 1 basin water + 1 running waterfall
  + pooled fx. The ripple bands are a uniform branch in the Toon shader on the water caps only.

## Save
- Play writes the real save. After the checks I reset lm.pads / lm.rain / lm.lagoon, put the explorer back at
  Cinderwood c25 r7 and removed the Lilymere lines the checks fired from `save.json`'s `said` list (backup in the
  session scratchpad).

## QA APK
- `Builds/PocketIsle-QA.apk` (well gate sealed), built at the end: 106.6 MB (rebuilt after the audio settings fix). Not installed. The tester's evening
  build is `Builds/PocketIsle-QA-oct27-tester.apk` (106 MB).
