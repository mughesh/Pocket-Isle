# Step 19: the credits become sky lanterns

Decided Nov 8, 3:30 AM (design chat, Mugesh picked it from options). Today's credits (step 17 J banner + round badge)
look bad (screenshot from Mugesh): a flat dark UI slab with debug-like grid lines and tiny text, the statue's head
covering the third line, a floating disc portrait. None of it belongs to the toon stone-and-moss world.
**New idea: the Heart releases one glowing paper sky lantern per credit; they rise off the island into the room, pause
at eye level to be read, then drift up and fade. The last and biggest lantern carries Mugesh's portrait as a small framed
painting.** It rhymes with the Lantern Spirit, uses real room space (purposeful MR) and is a hero shot for the video.

## How to run
- Fresh session: orient first (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Assets/Docs/Run-18.md`, git log).
- Branch **`step-19`** from the current HEAD (`step-18`). Same rules as step 18: don't stage the 15a work in progress,
  `Assets/_Recovery/`, `_lanes/`; stage your files by path; commit per section "(headset test pending)"; no AI
  attribution; don't push or merge; unattended = no questions, flat Play via the bridge, tests, smoke run.
- At the end: `Assets/Docs/Run-19.md` with **shots of every lantern at reading height and one wide shot of all of them
  in the air** (the bridge's shot tools, game-view at the seat position), timing, tri/draw counts, the headset test list.

## A. The lantern
1. A toon paper sky lantern built like the other props (baked SDF/procedural, flat toon, inverted-hull outline): a
   slightly tapered, rounded box (wider at the top), warm cream paper, thin dark bamboo ribs at the corners and a bamboo
   ring at the open bottom with a tiny flame inside. **The paper glows from inside** (warm emissive, a little brighter at
   the bottom, a soft flicker), plus a cheap additive halo sprite.
2. Text is **printed on the front panel** in dark warm brown ink (high contrast on the glowing paper), bold, centred,
   1–2 lines. Readable from the seat: cap height ≥ 1.2 cm at island scale 1. Normal lantern ~10 cm tall; the last one
   ~1.6x.
3. Budget: ≤ 6 lanterns alive, a few hundred tris each, no realtime lights, 72 fps safe.

## B. The credits on lanterns (from `CreditsText`, so the step 15b swap still works)
4. Add a grouping to `CreditsText` (e.g. `Lanterns`, one entry per lantern), in this order:
   1. "Islings, realms and Keepers" / "original designs"
   2. "Music" / "Mugesh"
   3. the sound lines (today: "Sounds: elevenlabs.io (free plan)" / "Some sounds: Kenney (CC0)"; after 15b its own lines)
   4. "Font: Liberation Sans" / "SIL Open Font License"
   5. **the big one:** the framed portrait, then "Pocket Isle" / "Made by Mugesh"
   The Settings card keeps `CreditsText.Block` as it is.
5. **Framed painting:** a small rectangular wooden frame with a thin gold inner rim, hung on the big lantern's front
   above its text. The picture is Mugesh's cartoon from `Assets/_Project/Art/Credits/Mugesh.png`, cropped head and
   shoulders like `CreditsPortrait.png`, toon-friendly (unlit, no shine). If the file is missing, the big lantern shows
   only its text.

## C. The motion
6. Each lantern lifts from the Heart's forehead crystal (a small gold spark as it lets go), rises in front of the
   statue, never behind it or overlapping it, and stops at **reading height: roughly eye level, ~30–40 cm above the cap**,
   fanned in a gentle left-centre-right arc so the readable ones never overlap each other.
7. It faces the head once when it stops (yaw only, like boards), holds **~3 s** with a slow bob and sway, then keeps
   rising into the room and fades out ~1 m above the island. Next lantern every ~2 s (one-in, one-reading, one-leaving).
8. The big portrait lantern goes last, stops dead centre a bit closer, holds **~5 s**, then rises slowly; the existing
   "Thank you for playing Pocket Isle" card comes up as it leaves.
9. Total ~15–18 s. **Skip** still works (lanterns fade in 0.4 s). A soft paper-flutter / warm whoosh per launch from
   the synth or Kenney set, low; no new music.
10. Optional only if cheap: each lantern throws a faint warm glow on the island cap as it passes (a soft decal).

## D. Remove the old credits
11. Remove the step 17 J slate banner (`CreditsBanner`) and the round badge (`CreditsStandee` / `CreditsPortrait`
    use) from the finale. Keep the files only if a test or the Settings card needs them; otherwise delete them and their
    tests. `CreditsPortraitTool` can stay.

## E. Check
12. Flat Play the ending (Step17Debug Credits or See the ending): shots at reading height for every lantern, the wide
    shot, the portrait close-up. Nothing hides behind the statue, no text clipped, no overlap, Skip works, the
    Thank-you card follows. Smoke run passes.
