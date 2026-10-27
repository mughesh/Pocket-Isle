# Overnight run, Oct 27 → 28 (second run): fixes from the headset notes, then Lilymere's roster + Tidemother

Prompt: `Prompts/overnight-oct28b.md`. Everything was tested in flat Editor Play through the bridge and in EditMode tests
(208/208). **Nothing was tested with real hands or on the headset.** No pushes. The well (water) gate stays **sealed in
builds** (`LilymereConfig.open` = false). The wind gate stays open (Mugesh's call, Oct 27).

## Done / partly / not started

| Step | Section | State | Commit |
|---|---|---|---|
| 11d | A: the objective rule (gold marks, chip counters, pinned until done, arrows, kite task, crown hooks, door glow) | done, flat-checked | 98c566e |
| 11d | B: touch walking off (Settings row), easy Keeper shard, pinch ghosts + spark, fist grabs | done, flat-checked (no real hands) | 98c566e |
| 11d | C: mill flicker, mill flick zone, big raised clouds, return gate after Galeowl | done, flat-checked | 98c566e |
| 11d | D: pads, lotus crossing, rain cloud, garden view, two-hand scoop | done, flat-checked (synthetic hands) | 98c566e |
| 11d | G: sound notes appended to `Audio/PhaseB-Hooks.md` | done | 98c566e |
| 11c | A: water roster (10 Islings), tables, first-meet lines | done | 2e00e11 |
| 11c | B: Tidemother, lake temple, water shard, statue socket, statue.three, dev panel | done, flat-checked end to end | 93bd7ca |

11d went in as **one commit** instead of one per section. The sections share files (HandPuzzle, KeeperHall, the pieces,
the setup, the tests). Splitting them by hunk wasn't possible here, and intermediate commits might not have compiled.

## Test first on the headset (in order; over Link restart Unity first and tick Standalone "Initialize XR on Startup")
1. **Settings > Touch tiles to walk** shows **Off**. Fingertips on the grass don't walk. Thumb swipe and look + pinch still do.
2. **Go to > GW: kite field**: "The tower needs kites. Let's fly all three!" with the chip **Kites 0/3**. Every lying
   kite has a gold ring. The ghost **pinches** (thumb + index, other fingers loose) with a tiny gold spark. Fly one: "One!",
   then the chip reads Kites 1/3 and the lesson line comes back.
3. **GW: big mill**: flick the sails with the index **or middle** fingertip. Try from a little further in front (zone
   4 cm, was 2.2) and a slower flick (0.35 m/s, was 0.5). Each flick jolts the sails and counts **Flicks n/3**.
   Walk past the mill: **the sails must not flicker any more**.
4. **GW: ridge**: the puffs are twice as big and float clear of the plants, each with a gold ring. Chip **Clouds n/3**.
5. **GW: tower**: before the kites, three glowing hooks on the crown. Story tab **GW tower** on: the door glows (a warm
   swirl, a gold ring in front), the same as the forge door (**CW: forge door** with the forge open).
6. **Galeowl** (Go to > GW: tower hall): chip **Knots n/3**, closing a fist on a knot's end also grabs it. After the
   win, **don't touch the shard**: after 4 s it flies to the Spirit. Back outside, a **return gate beside the tower**
   takes you home.
7. **LM: channel**: pads float on the water with gold rings; push one far left or right and it stops at the edge
   (the channel's pad area is now ±6 tiles round the crossing). Finished bridge: **just the three pads** on the water,
   the explorer steps up onto them and hops across.
8. **LM: garden**: the island glides so **all five buds** show (chip **Lotus n/5**). The rain cloud is palm-sized
   (11 cm). The lotus crossing is stones on the cracked bed (no road).
9. **LM: basin**: cup **both hands together**, palms up, dip and lift: a bigger ball. Tip both over the basin:
   **Pours 1/2**. One cupped hand still works. Gold ring on the pool, then on the basin while carrying.
10. **Story tab > LM basin + rise** on, then **LM: temple**: the temple door glows. Walk in: **Tidemother** (grey and
    frightened, red-teal eyes, a churning aura). Win (wind breaks her Lily Shell). The dome of water behind her flows
    out, the water shard floats out, the wave runs from the temple and a return gate stands west of the temple.
11. Hub: with fire and wind already placed, set the water shard: **"All three pieces home... Something is waking up!"**
12. Encounter tab: fight Marimoss, Drizzcan, Lilyvole, Sailfold, Bottlewhale and their evolutions. Look at Drizzcan's
    drops dripping, the lily hat wobbling, the boats rocking, the bottle cork popping and the lighthouse lamp.

## Feel numbers to tune

| What | Value (was) | Where |
|---|---|---|
| Gold mark brightness | 1.3 | every hand moment > Mark Intensity |
| Mill flick zone / slack / speed | 4 cm / 2.5 cm / 0.35 m/s (2.2 / 1.5 / 0.5) | Big Mill > Near Plane / Slack / Min Flick Speed |
| Cloud puff size / height | x2.7 / 0.6 tiles over the path (x1.35 / on the path) | Cloud Bank > Puff Scale / Puff Height |
| Keeper shard grab reach / auto | 9 cm, pinch or fist / 4 s | Keeper Hall > Shard Grab Reach / Shard Auto Seconds |
| Pad float above water | 3 mm (0.03 tiles) under-side gap | Lily Pad Bridge > Float Height |
| Pad respawn when out of view | 3 s | Lily Pad Bridge > Respawn After |
| Pad channel area | ±6 tiles round the crossing (was ±10) | Lily Pad Bridge > Channel |
| Rain cloud width | 11 cm in the room (was ~3 cm) | Rain Garden > Cloud Metres |
| Garden window settle | explorer within 7 tiles of the buds | Rain Garden > Settle Reach |
| Two-hand cup | palms within 8 cm (x1.6 while carrying), ball x1.8 | Scoop And Pour > Pair Gap / Pair Slack / Pair Ball |
| Tidemother | Lv 9 (+2 per other shard), HP 132, atk 10, def 11 | TidemotherKit |

## Decisions I made (without asking)
- **Mill flicker cause**: puzzle pieces placed their props before the explorer scrolled the window in its own Update.
  A turning mill's spin phase was hashed from that one-frame-old map position, so it jumped every frame while
  walking. Fix: props placed on the map re-sync at render time (`PropView.Sync`), and pieces with their own angle no
  longer add the hash.
- **Return gate after Galeowl was missing** because the Galewick map never had a gate prop at the tower: KeeperHall
  unhid an empty tile (my 10e flat check only read the flag). Fixed: prop + link + a test for Galewick and Cinderwood.
- **Lily pads looked submerged** because the LilyPad prop's disc hangs below its origin, more when drawn bigger. Pads
  now sit on the water / the crossing's water sheet by their underside. The finished bridge was invisible before
  (under the sheet) and now shows. The explorer gets a small additive **ground lift** on the bridge (a step up, a hop
  between pads). This is the only explorer change.
- **Ember ghost**: the prompt asked for an open palm-up ghost, but the ember is pick-and-place since the Oct 27 notes.
  A palm-up ghost would teach the old gesture, so it uses the new pinch ghost.
- **Pads keep the poke ghost**: they are pushed with a fingertip, not picked up.
- **Garden view**: instead of only a stand-here tile, the window settles over the buds and the explorer whenever they fit.
  The gold stand-here tile shows only when they can't fit.
- **Count lines pass over the pinned lesson**: count lines ("One!") show over it, then the lesson line comes back.
  Only one objective holds the bubble at a time.
- **The flick counter is "in the last 4 s"**: it can drop back to 0/3 if you stop. That is the real rule.
- **Tidemother's shell** is a painted jade zone on one body plus lily-pad and lotus parts. Her lotus doesn't open on
  calming yet (the calmed palette and the aura change carry it).
- **The temple return gate** is west of the temple (x13 y49), its front on the grass by the causeway.
- The roster's tris are 8.5k-11.8k each (body capped at 6k like the others; Rainurn's bands and drops make it the
  highest at 11.8k, 15 draw calls).
- **Audio spatializer slipped again** (Unity or the Meta SDK set Meta XR Audio during the run). I re-applied it through
  Unity before the build (`AudioHookupDebug.ApplyProjectAudio`). Nothing to commit: it matches the committed settings.

## Blocked or reverted
- Nothing reverted. The 4 s auto-take is very short if the player is reading. Raise Shard Auto Seconds if it snatches.
- **Possibly earlier, not verified:** `GalewickTools.Write` logs "Signpost at x28 y47: tile taken; skipped" (a sign
  by the ridge). I did not check whether it happened before tonight's map changes.
- `Builds/PocketIsle-QA-oct27-tester.apk` isn't in `Builds/`, so there was nothing to protect. The build writes
  `Builds/PocketIsle-QA.apk` only.

## Shots (`Logs/BattleShots/`)
11d: `11d_kites`, `11d_kites_chip`, `11d_kites_one`, `11d_clouds`, `11d_clouds_wide`, `11d_mill`, `11d_tower_hooks`,
`11d_tower_door`, `11d_tower_gate`, `11d_pads`, `11d_pads_float`, `11d_pad_bridge`, `11d_pad_bridge_walk`,
`11d_lotus_view`, `11d_rain_cloud`, `11d_lotus_crossing`, `11d_two_hand_ghost`, `11d_two_hand_ball`, `11d_basin_half`.
11c: `11c_<name>` for the 10 water Islings, `11c_lineup`, `11c_tidemother`, `11c_temple_door`, `11c_hall`,
`11c_tidemother_frightened`, `11c_battle`, `11c_dome_flowing`, `11c_tidemother_calmed`, `11c_wave`, `11c_restored`,
`11c_statue_three`.

## Perf
- Not measured with a capture. New draws by construction:
  - Gold marks: one quad per object plus a pooled orb, only while an objective is active.
  - Arrows: one quad each, only when off-window.
  - DoorGlow: 2 quads.
  - Tidemother: 14 draw calls; her hall adds the dome view.
- `PropView.Sync` is one loop over the placed puzzle props per camera render.

## Save
- Play writes the real save. It was backed up before the run (session scratchpad `saves11d`) and restored at the end.

## QA APK
- `Builds/PocketIsle-QA.apk`, **108.0 MB**, built 19:50 from `93bd7ca` (release, POCKET_DEV, water gate sealed, wind gate
  open). Not installed. The audio settings were re-applied through Unity first (no spatializer, DSP 256).
