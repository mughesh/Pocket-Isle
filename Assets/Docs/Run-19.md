# Run 19: the credits become sky lanterns (Nov 8, overnight)

Branch `step-19` from `step-18`. Not pushed, not merged. Flat Play via the bridge (seat camera), tests, smoke run.
Headset not tested.

## Done / partly / not

| # | Item | Status | Notes |
|---|---|---|---|
| A1 | Toon paper lantern (tapered chamfered box, bamboo ribs + bottom ring, flame, glowing paper, outline, halo) | done | `SkyLantern.cs` + `Diorama/Sky Lantern` shader, built in code at runtime (no baked asset needed: 112 tris) |
| A2 | Text printed on the front, dark brown, bold, cap ≥ 1.2 cm | **partly** | 12 mm cap on 4 of 5 lanterns; "Sounds: elevenlabs.io (free plan)" auto-shrinks to 8.5 mm (too long for the panel) |
| A2 | Lantern ~10 cm, last one 1.6x | **changed** | 14 cm tall, 12 cm wide. At 12 mm cap, "Islings, realms and Keepers" can't fit a 10 cm lantern (it wraps to 4 lines as it is) |
| A3 | Budget | done | ≤ 5 lanterns (≤ 4 visible at once), 112 tris each (+ ~60 frame), no lights. About 4 draws a lantern (outline + paper, halo, text), estimated from the code, not profiled |
| B4 | `CreditsText.Lanterns`, in order, the 15b swap included; Settings card unchanged | done | |
| B5 | Framed painting on the big lantern from `Art/Credits/Mugesh.png` | done | Wooden frame, gold rim, head and shoulders (uv crop). Painted by the lantern shader itself (see "Fixes" below). No file = text only (`CreditsPaintingHook`) |
| C6 | Lifts from the forehead crystal with a gold spark, rises in front of the statue, left / centre / right arc | done | Path goes toward the player first, then up: never behind the statue |
| C7 | Faces the head, holds 3 s with bob + sway, rises and fades ~1 m up, next every 2 s | done | |
| C8 | Big one last, centre, closer, 5 s, card as it leaves | done | **Order change:** the Spark now returns *before* the credits, so the thank-you card can follow the big lantern directly (it used to come after the Spark) |
| C9 | ~15–18 s, Skip fades in 0.4 s, launch sound | done | 15.8 s to the card. Skip checked in flat Play: lanterns fade, card comes up. Sound: `credits.lantern` (synth: paper flutter + warm whoosh, volume 0.3) |
| C10 | Glow decal on the cap | not done | optional; skipped |
| D11 | Remove banner + standee | done | `CreditsBanner`, `CreditsStandee`, `CreditsStandeeHook` deleted. `CreditsPortraitTool` stays |
| E12 | Flat Play shots, Skip, smoke | done | below |

## Shots (flat Play, seat eye ~0.45 m from the lanterns)

Every lantern at reading height (`Run-19-img/`):

| | |
|---|---|
| ![](Run-19-img/19_read_0_islings.png) 1 Islings, realms and Keepers | ![](Run-19-img/19_read_1_music.png) 2 Music |
| ![](Run-19-img/19_read_2_sounds.png) 3 Sounds (8.5 mm cap) | ![](Run-19-img/19_read_3_font.png) 4 Font |
| ![](Run-19-img/19_read_4_pocket.png) 5 the big one | ![](Run-19-img/19_portrait.png) the painting, close |

Wide, everything in the air: ![](Run-19-img/19_wide_3_font.png) and as the big one arrives: ![](Run-19-img/19_wide_4_pocket.png)

Checked in the shots:
- Nothing hides behind the statue.
- No text is clipped.
- The lanterns being read never overlap.
- The leaving ones go up and fade.

## Timing (from the log)

| Lantern | Lets go | Reading | Height over cap | From the head | First line cap |
|---|---|---|---|---|---|
| Islings, realms and Keepers | 0 s | 1.6–4.6 s, left | 33 cm | 0.46 m | 12.0 mm |
| Music | 2 s | 3.6–6.6 s, centre | 34 cm | 0.43 m | 12.0 mm |
| Sounds: elevenlabs.io (free plan) | 4 s | 5.6–8.6 s, right | 33 cm | 0.46 m | 8.5 mm |
| Font: Liberation Sans | 6 s | 7.6–10.6 s, left | 33 cm | 0.46 m | 12.0 mm |
| Pocket Isle (big, painting) | 9.2 s | 10.8–15.8 s, centre | 32 cm | 0.40 m | 13.6 mm |
| Thank-you card | 15.8 s | | | | |

The big one waits an extra 1.2 s (`lanternBigDelay`). Without it, it arrived while the left lantern was still being read and touched it. That was the first shot; fixed.

## Fixes along the way

- **Flat Play camera:** the main camera sits at the world origin, under the island, so the lanterns turned toward the floor. `FinaleDirector.EyeWorld` now uses a seat in front of the island when the camera is below the cap. On the headset it's the real head.
- **Colours:**
  - Vertex colours were read as linear, so the bamboo looked tan and the paper pale. They're converted now.
  - The painting looked washed out through the separate `Diorama/Portrait` material. It's now painted by the lantern shader (opaque, in the frame's mesh), and its colours are right.
- **Ribs:** the first build put a rib down the middle of the front panel. They're on the corners now.
- **Scene value:** `lanternSpacing` 0.155 → 0.175 is set in the scene by `Step19Setup.Lanterns`, because scene values override code defaults.

## My mistake

My commit for section A also took the old-credits file deletions I had staged earlier, so A on its own didn't compile. I soft-reset the two local commits and redid them, so each one compiles now. I check `git diff --cached` before each commit from now on (saved to memory).

## Checks

- **Tests:** 352/352. New `Step19Tests`: order, big one last, neighbours never share a slot, 14–18 s, tris budget. The Step16 credits test now uses the lantern timing.
- **Smoke run:** 84 ok, 0 failed, 6.6 min. The finale to its end took 52 s. One known note: the opening's "no progress for 20 s", same as before.
- **Save and XR:** the save is restored byte-identical and XR init on start is back on.

## Headset test (step 19 part; the combined list in play order is at the top of Run-20.md)

1. Dev panel > See the ending (or play to the end). After the Hall of Fame the Spark returns, then the first lantern lifts off the Heart's forehead with a gold spark and a soft flutter.
2. Each lantern stops at about eye level in front of the statue and turns to you. Check that the text is readable without leaning in, including the small "Sounds" one.
3. Left / centre / right: two being read at once never touch.
4. The big lantern comes last, centre and closer, with your picture framed. Check the crop: head and shoulders.
5. As it rises away, the "Thank you for playing" card comes up.
6. In passthrough, check the leaving lanterns fade cleanly with no dark ghost; the flat shots show them greyish while fading.
7. Run it again and press Skip mid-credits: everything fades in 0.4 s and the card comes up.
8. Log: `[FinaleDirector] Lantern "..." reading: NN cm over the cap ... first line cap NN mm.`
