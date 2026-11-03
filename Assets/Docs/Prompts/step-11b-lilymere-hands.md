# Step 11b: Lilymere hand moments (lily pad bridge, rain hand, scoop and pour)

Read CLAUDE.md, `.claude-handoff.md` (10b Galewick hands, **10f ember in your palm** = the carry code to reuse, 10f
cup the flame = palm-pose detection), `RealmsDesign.md` (the bar + Lilymere), the kit in `Runtime/Puzzles`.
Shared rules as 10b/10f: kit pieces, island-local, saved, near the explorer only, look + pinch fallback, lesson pinned
as a chip until it first works, ghost on the real object, fingertips at a piece never walk the explorer
(`TileTouch.Block`), a cheer when solved, `lm.*` lines ≤ 60 characters. **The world answers widely.**

## A. Lily pad bridge (lily channel)
1. 3 lily pads drift slowly in the channel. A fingertip touching a pad's edge pushes it along the water in the push
   direction (speed from the finger, drag on water, bumps off banks and other pads with a little bounce). A pad that
   comes within ~0.6 tiles of an empty **bridge slot** (3 slots across the channel, faint rings) glides in and clicks
   (ripple ring, a little bob, the frog on it hops). All 3: the pads lock into a path, a frog chorus, small ripples
   spread across the whole channel; tiles open (saved). Look + pinch a pad = it drifts into the nearest free slot.
2. `lm.pads`: "The channel's too wide. Nudge the lily pads together!" chip "Push!".

## B. Rain hand (lotus garden)
3. **Rain:** a hand held **palm down** (palm normal within ~40° of straight down), 5–25 cm above the island over the
   garden, for 0.6 s forms a small grey-white toon cloud right under the palm; it rains (short streak particles,
   splash dots on the tiles below) as long as the palm stays down there; a **finger wiggle** (summed finger-joint
   speed over a threshold) makes it rain harder (more drops, faster growth). The cloud follows the hand.
4. 5 closed **lotus buds** in the dry bed: each gets wet under the rain (fill 0→1 over ~2 s of rain, ~1 s when
   wiggling); full = it blooms into a **big open lotus stepping stone** with a pop of petals and a little splash of
   water filling the cracks around it. All 5 = a lotus path across the garden (tiles open, saved). Rain also darkens
   the cracked sand under it (wet decal or tint, fades slowly) and any grass under it perks up: the world answers.
   Look + pinch-hold a bud = a tiny cloud rains on it by itself.
5. `lm.rain`: "So dry... Hold your hand over it like a little cloud." chip "Palm down!".

## C. Scoop and pour (waterfall basin, the hero moment)
6. **Scoop:** a hand cupped palm-up (reuse 10f's palm-up test) dipped into any lagoon Water tile near the basin
   (hand below the water surface + 1 cm) and lifted out: a **wobbly ball of water** sits in the palm (toon water
   material, a few bubbles inside, little drips falling off, a slight jelly wobble when the hand moves). It stays while
   the palm stays roughly up (10f ember carry rules); tipped elsewhere it splashes down and is gone.
7. **Pour:** tip the hand over above the stone basin: a stream pours in, the basin fills a step. **2 scoops** fill it.
   Full: the basin glows, cracks run up the **frozen waterfall**, the ice shatters into sparkles, the waterfall
   **runs** (running look + mist + splash fx), then the lagoon **rises**: the `riseTiles` dry bed turns to Water in a
   wave spreading out from the basin (~3 s), the half-sunk temple's door surfaces with a gush (11c), lily pads and
   lotus everywhere bob on the rising water. Saved. Look + pinch the water, then the basin = a ball floats over and
   pours by itself.
8. `lm.scoop`: "The falls are frozen. Bring them some water!" chip "Scoop!"; after the first pour `lm.scoop.more`
   "One more!".

## Housekeeping
- EditMode tests: pad push + slot snap, palm-down + wiggle detection from synthetic hand poses, bud fill timing,
  scoop (dip + lift) and pour (tip over basin) from synthetic tracks, rise tiles.
- Perf ≤ 6 extra draws per moment (rain cloud + drops ≤ 2). Shots `Logs/BattleShots/11b_*` (pads mid-push and
  locked, cloud raining with a synthetic hand, lotus path, water ball on a palm, waterfall frozen and running, lagoon
  mid-rise). Route sounds through the SFX bus if the audio hookup is merged; list missing cues in PhaseB-Hooks.md.
- Commit per section "(headset test pending)", no AI attribution, don't push. Update `.claude-handoff.md`,
  GameDesign.md, QA-Guide.

## Test
1. Push the three lily pads into the slots; the channel becomes a path.
2. Hold a palm down over the garden: a cloud rains; wiggle your fingers for harder rain; the buds bloom into stones.
3. Scoop water from the lagoon with a cupped hand, pour it into the basin twice: the waterfall breaks free and the
   lagoon rises; the temple door comes up.
