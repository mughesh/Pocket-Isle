# Overnight run, Oct 28 (from 2 AM): the finale (step 13)

Prompt: `Prompts/overnight-oct28-finale.md` (step `Prompts/step-13-finale.md`). Everything was checked in flat Editor
Play through the bridge (synthetic palm, look + pinch from the bridge) and EditMode tests (235/235).
**Nothing was tested with real hands or on the headset.** No pushes. The water gate is still sealed in builds.

## Done / partly / not started

| Section | What | State | Commit |
|---|---|---|---|
| A | The statue whispers a memory per shard (Heart's voice bubble), the Spirit answers; it wakes per shard (eyes glow, then breathing + swaying moss) | done, flat-checked | `ea2b750` |
| B | The Heart of the World (shrine guardian): model, rig, pose, 4 critique rounds | done, flat-checked | `06c4edb` |
| C | The finale: wake, understanding, palm goodbye, eyes open + the hub answers, Hall of Fame, credits, the spark returns, free play; dev panel entries; resume at the goodbye | done, flat-checked (3 full runs + 2 sped-up) | `a646e19` |
| D | Post-game: collection card on poking the Heart, the Spark's hints, roster complete (gate sparkle + crown halo), Keeper rematch | done, flat-checked | `4d7c52c` |
| E | Tests, docs (GameDesign finale section replaced, QA guide, PhaseB-Hooks missing sounds, handoff), this report, QA APK | done | (docs commit) |

## Test first on Nov 3 (headset)
Standalone **Initialize XR on Startup is ON** (as found): Play over Link works as before. (For flat bridge checks I turn
it off with `Step12aDebug.XrInit 0`, and I put it back on before the build.)

1. **Jump straight to the finale:** game menu (left palm toward you + pinch) > **Dev** > **Story** > **Finale: play
   from wake**. It puts all three shards in the statue, takes you to the hub and plays. *Your real save is changed:
   the finale counts as done afterwards; Save tab slots let you go back.*
2. Watch the wake (~20 s): the statue shakes, dust and gold, the Heart sits up and lifts its head. Is it **majestic and
   kind** from your seat? Is it too big or small on the island?
3. "Will you take me?": **hold one open hand out, palm up**, anywhere. The Spirit should land on it within half a second
   (a warm glow at the palm). **Lift it up to the round gold setting on the Heart's forehead**: within ~6 cm it floats in.
   Try: turning the palm over (it floats back to wait), doing nothing for 30 s (the wait line), looking at the Heart +
   pinching (it goes by itself after 1.5 s).
4. Eyes open, wings unfold, the gates' sparkles, the golden ring under the island, the grass waving.
5. **Hall of Fame**: your party comes out one by one with stone cards. Are the cards readable? Is "met in" right?
6. **Credits** tablets over the far side: readable? Too slow (5.2 s each)?
7. The Spark: does it read as the Spirit, smaller and warmer, without the acorn cap?
8. **Finale: from the goodbye** skips straight to step 3 (the quickest way to retry the palm).
9. After the finale: **poke the Heart** (fingertip on its head or chest) = the collection card. In a Keeper hall, **poke
   the calm Keeper** = Spar? / Not now (a friendly match, 5 levels up).
10. The statue before the finale: put a shard in (Story tab shard buttons): the whisper bubble (gold-white, no tail,
    letters fading in) and the eyes glowing.

## The Heart's critique rounds
Shots in `Logs/BattleShots/`. Final set: `13_heart_front` (asleep), `13_heart_34`, `13_heart_side` (awake: eyes open,
wings out), `13_heart_withExplorer` (the seat view), `13_heart_vsStatue` (the old statue, same camera).

| Round | Looked at | What was wrong | What changed |
|---|---|---|---|
| 1 | `13_heart_r1_*` | Reads as a lion-dog guardian (good). Crown invisible at island scale; collar and two gems buried in the chest; the mouth read as a frown; the tail a string of beads; the folded wing stuck out behind like a fin; veins dotted. Also 55k tris: tiny pieces (gems, socket) defeated the simplifier | Crown about 2x; the collar is now beads projected onto the body surface with gems in settings facing out; a smile with corners up; the tail as tapered round cones; thicker continuous veins; tiny pieces baked 3-5x larger and scaled back (`PropKit.Small`) and per-piece budgets |
| 2 | `13_heart_r2_*` | Crown, collar and smile read. Open wings too small and hidden behind the mane; folded wing still a fin (Euler order); the open eyes too small; face small inside a lumpy mane; flat grey | Wings 1.4x longer with bigger cloud feathers; the wing pose composed yaw, roll, pitch (folded drapes down the flank); bigger face and muzzle; mane rings wider, fewer bigger curls; more moss tufts and flowers |
| 3 | `13_heart_r3_*` | Wings frame the head well. The gold eyes were hidden: the bigger muzzle rose in front of them | Eyes and brows up, muzzle top lower and narrower |
| 4 | `13_heart_r4_*` | Gold eyes under heavy lids, kind smile, forehead setting, green cloud-wings, orange / blue / green gems: reads as a kind guardian. Mane still busy up close, fine at island scale | Curls carved (a groove ring + nub instead of a ring stuck on); wind pieces green (wind is green since 12b); budget trimmed to 24.2k |

Compared with the old statue (`13_heart_vsStatue`) it is the same footprint and spot, about 1.23x as tall (crown tip),
wider with the wings. Next to the Keepers (`11c_tidemother_calmed`, `9c_hearthstag`, `10d_galeowl`) it has less colour
(stone), so the finale leans on the gold (veins, eyes, crown blaze, motes) for its moment.
At the default seat view it is small at the back of the window, so the finale **centres the window on it and zooms
in 1.15x** for the sequence.

## Feel numbers (Inspector)

| What | Value | Where |
|---|---|---|
| Palm counts as up | palm normal · up ≥ 0.55, held 0.35 s | Finale > Handoff > Up Dot / Land Seconds |
| Hand-off distance to the forehead | 6 cm (x island scale) | Handoff > Socket Radius |
| Palm turned away / lost before it floats back | 0.6 s | Handoff > Lose Seconds |
| Wait line when idle | 30 s | Handoff > Wait Seconds |
| Look + pinch the Heart: it goes by itself after | 1.5 s | Handoff > Gaze Delay |
| Window during the finale | centred 1.2 tiles in front of the Heart, zoom 1.15 | Finale > Frame Ahead / Zoom |
| The Spirit waits in front of the forehead | 2.2 tiles | Wait In Front |
| In the setting: size / drop | 0.42 / 0.32 tiles | In Socket Size / Drop |
| Hall of Fame: gap between Islings, Lodge guests | 2.6 s, 2 | Hall Gap / Lodge Guests |
| Cards over the Islings | 1.8 / 2.9 tiles (alternating) | Card Height |
| Credits tablets | 5.2 s each, rising 0.1 to 0.27 m over the far rim | Tablet Seconds / Tablet Rise |
| Collection card | 7 s | Collection Seconds |
| The Spark's hint | 15 s after arriving in an incomplete realm, once a session | Hint After |
| Heart: breathing, blinks | 1.2 % over 4.2 s; every 3.5-6.5 s, 0.14 s | Heart of the World > Life |
| Heart: head turn / look up | 28° / 16° | Max Yaw / Max Pitch |
| Heart: heartbeat | 0.9 beats/s | Beat Rate |
| Wings folded / open | (0, 70, -62) / (0, 12, 28) (pitch, yaw back, roll up) | Wing Folded / Wing Open |
| The Heart's voice | 16 letters/s, 1.12x text, each letter fades over 7 letters | Dialogue Bubble > Heart |
| Statue breathing (two shards) | 1.4 % over 4.5 s | Statue Socket (fire) > Breathe |

Timings measured in flat Play: wake ~20 s, understanding ~6 s, the goodbye is the player's, eyes open + the hub answers
~10 s, Hall of Fame ~30 s (six Islings), credits ~36 s, the spark ~8 s: about 2 min plus the goodbye.

## Decisions I made (without asking)
- **The Heart is four baked props, not a creature recipe.** The creature baker makes one renderer per part (14+ draws);
  props merge pieces into one mesh with six colour slots and per-slot glow, so body / head / wing x2 = 8 draws. Eyes
  open / closed are two head props swapped (a blink is a swap), not a lid on a pivot; there is no separate jaw (the head
  nods). The tail's sway is the wind ribbon swaying in the shader.
- **Palm goodbye is new code** (`PalmHandoff`, pure and tested): the forge ember became pick and place on Oct 27, so
  there was no palm-up carry left to reuse. Either hand, whichever is most palm up. The ghost hand uses `palm_carry`.
- **"The player's hand gently lit"**: the ISDK hand keeps its own material, so it's a warm halo at the palm instead.
- **The statue to the Heart is a swap covered by a dust and gold burst**, not a mesh morph.
- **The gates bloom with element sparkles.** The gate props don't animate. When a gate is outside the island window, the
  sparks show at the window's edge toward it, not in mid-air.
- **The Spirit stays in the forehead and the guide is the Spark.** After the hand-off the Spirit's own model becomes the
  Spark (it hops out of the lantern); a warm light stays in the Heart's setting.
- **Met in:** saved at catch time from now on. In old saves, starters (Kilnpot / Pebbloop / Pufflet and their
  evolutions) count as the Still Isle, everything else as the first realm whose roster has it.
- **Rosters** = every species in a realm's grass, water and zone tables plus what they evolve into (no Keepers):
  Cinderwood 22, Galewick 20, Lilymere 20. The step's example numbers (12 / 11 / 11) were smaller; a species found in
  two realms counts in both.
- **Keeper rematch:** the shield mechanic stays (it is the Keeper fight) but softer (2 hits); a lost friendly match heals
  the party in place instead of the shrine. Only after the finale.
- **Resume:** quitting at any point after the wake restarts at the goodbye (the Heart already awake).
- **The menu's Title** is hidden during the finale (Resume, Settings, Controls stay).
- **Music:** `music.finale` is wired and silent (no track yet); the area music comes back after.
- **Lodge guests: 2** (three pushed the Hall of Fame's draw calls past 250).
- **The old finale lines** in Script_v1.md (`heart.wake` x3, `heart.win`, `end`) were replaced by the step's lines.

## Blocked or reverted
- Nothing reverted.
- **Over the draw-call budget at the Hall of Fame's peak** (see below). Not fixed: it needs a device capture to know if it
  matters. If it does: fewer Lodge guests (0), or hide the rim pods during the Hall of Fame.

## Perf at the finale's peak (Editor stats, flat Play; not a device capture)

| Moment | Batches | Set-pass | Tris |
|---|---|---|---|
| Hub after the finale (the Heart awake) | 134 | 104 | 195k |
| Hall of Fame, 3 out | 178 | 144 | 238k |
| Hall of Fame peak, 6 party + 3 Lodge guests (now 2), motes, cards | 257 | 220 | 343k |
| Same, the Heart hidden | 256 | 219 | 312k |

The Heart itself is 8 draws and 24.2k tris (body 9.4k, open head 9.5k, wings 2 x 2.6k). The peak comes from the
Islings (each 10-15 draws with outlines and eyes). Editor fps 110-200. **Needs a Meta VR CLI capture on the Quest.**

## Save, settings, build
- The real save was backed up before Play and put back after every run (`save.json`, `save.old.json`).
- Standalone Initialize XR on Startup: back to **on** (as found).
- QA APK: **`Builds/PocketIsle-QA.apk`, 109.0 MB** (04:44, release + POCKET_DEV, water gate sealed), not installed.
  `Builds/PocketIsle-QA-oct27-tester.apk` untouched.
