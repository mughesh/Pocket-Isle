# Step 6 fixes: catch rule, pod size, hints on the target, Cinderwood living vs still

Read CLAUDE.md and step-06-capture-party.md. Step 6 passed the headset test (Oct 24) apart from the items below.
**First, commit step 6 as it is now** (it works), then do these fixes as a separate commit.

## A. Pod card text (PodCard.cs)
Long names ("Kettlewing") overflow the card and the second line overlaps the HP bar.
1. Layout, top to bottom, no overlaps: **name** (bold) / **Lv 5 · Fire** (smaller) / HP bar / `HP 38/38`.
   Fainted: append ` · Fainted` to line 2.
2. Name uses TMP auto-size (min about 60% of the current size, max = current) inside a fixed text box with side
   padding. If it still doesn't fit at the minimum size, the panel widens (up to 1.4×) instead of clipping.
3. Check the longest names in the roster (Glazegolem, Lanternsnail, Kettlewing, Kilnhearth) in the Editor.

## B. Catch rule (decided Oct 24)
| Wild HP | Catch rune | Result of a throw |
|---|---|---|
| above red (> 25%) | Locked: dim, no glow. Picking it gives a short Spirit hint ("Weaken it first!"), no turn used | — |
| red (≤ 25%, same threshold as the HUD's red bar) | Unlocks with a small pop + glow | Chance roll as now (`BattleMath.CatchChance`), wobbles |
| fainted | — | **Dazed window**: the wild creature slumps on its pad (dizzy stars or a slow sway, no fade) for **8 s**. A throw = sure catch (rares 75%). No throw in 8 s → it fades out, battle won, XP as usual |

4. While the window is open, the cradle pod glows and pulses. Look + pinch on the cradle still auto-throws.
5. The cradle and the Catch rune only appear once the wild creature is red or dazed. Grabbing an empty pod earlier
   isn't possible any more.
6. Tests in `BattleTests`: locked above 25%, unlocked at ≤ 25%, dazed = 1.0 (rare 0.75), timeout = win.
7. Dialogue: `capture.first` now fires the first time the wild creature reaches red (not 50%).
   Add `capture.locked` ("Weaken it first!") and `capture.dazed.first` ("It's dazed. Quick, a pod!").

## C. Bigger catch pod
8. Cradle pod: **2× the party pod** (about 6 cm across at island scale 1), sitting higher so the hand doesn't hit
   the rim. Grab volume (`HandGrabInteractable` collider) **8 cm**. Hover = brighten + 10% grow.
9. Party pods keep their look (3 cm) but get a **5 cm** grab volume. Neighbouring volumes must not overlap; if they
   do, shrink them to fit.
10. After a catch the pod shrinks back to party size as it flies to its socket.

## D. Hints point at the target (Spirit flies to it)
Hints currently pop above the Spirit at the near-left corner, far from the thing you need to touch.
11. Dialogue lines can name a **target anchor**: `leadPod`, `cradle`, `catchRune`, `nearPad`, `wildPad`, `tile:x,y`,
    or none (stays home). Systems register anchors by id, like speakers. Add the field to the Script_v1 import
    (an optional column / key); old lines without one behave as now.
12. When a line has a target: the Spirit flies there in about 0.5 s (ease, small arc), hovers just beside and above it
    (never covering it, offset toward the player), the bubble shows next to it, and the **target pulses**
    (shared rim-highlight / `_Flash`). After the line, plus about 1.5 s, it drifts home.
13. Keep it in the central ~70° FOV. If the target is outside that, the Spirit stops at the edge of that cone and
    leans toward it.
14. If the target is hidden or destroyed mid-line, the Spirit goes home and the line finishes there.
15. Point these at targets: lead-pod hint at encounter start → `leadPod`; `capture.first`, `capture.dazed.first`
    → `cradle`; `capture.locked` → `catchRune`; `party.full` → home. Grep Script_v1 for other lines that tell the
    player to touch something and give them targets too (list them in your summary).

## E. Cinderwood: still vs flowing (replaces the step 6 palette swap, decided Oct 24)
Mugesh's call: the muted/grey palette looks bad. **The bright look (today's restored palette) is now the default,
unrestored state.** "Element not flowing" is shown by things being *still and unlit*, not by colour loss.
Restored = the fire is flowing again, so things *move and glow*.

| | Unrestored (default, most play time) | Restored |
|---|---|---|
| Palette | Today's bright Cinderwood palette | Same, +5–10% warmth/brightness, leaf tips glow (`leafGlow`) |
| Lanterns, lantern posts, kilns, braziers | Dim, **irregular sputtering flicker** (drops out, catches again) | Bright, steady warm glow, gentle flicker, small soft halo billboard |
| Embers | None | **Drifting embers** rising through the trees and over tall grass |
| Hot springs | Still water, no steam | Steam puffs (the `HotSpringSteam` prop, only when restored) |
| Leaves | Static colour | Slight glow pulse on tips |

16. Theme: copy the current `restored` colours into `unrestored` in `Themes/Cinderwood`, then raise `restored`
    slightly. Revert the step 6 muted palette.
17. **Embers, cheap:** one instanced billboard mesh with a vertex shader (no CPU per ember, no `ParticleSystem`).
    Positions are hashed per **world** tile so they scroll with the map and don't pop, clipped by the same window
    clip as tiles. More embers over tree and tall-grass tiles and near kilns. Each one rises about 15–25 cm (island
    scale 1) with a sideways wobble, fades in and out, additive orange-gold, tiny (3–5 mm). Count fades in with
    `RealmState.Restored` (0 → about 120 visible). One draw call.
18. Lantern/kiln flicker: drive the `Glow` slot by `RealmState.Restored`: sputter pattern at 0, steady at 1.
    Use the existing `_GlowA/_GlowB` block; no new materials.
19. Steam: hot-spring steam alpha × `RealmState.Restored`.
20. Keep G / 3 taps as the toggle with the 2 s fade between states.
21. Budget: check draw calls and frame time in both states (Meta VR CLI capture); report the numbers.
22. Update `Assets/Docs/GameDesign.md` with a short "Look of the two states" section matching the table above.

## Housekeeping
- One compile batch per section. Don't commit fixes until Mugesh tests. Update `.claude-handoff.md`.
- When it passes: commit and move the `pre-goa` tag. No AI attribution lines. Don't push.

## Test on the headset
1. The pod card fits "Kettlewing" and "Lanternsnail" cleanly.
2. Full-HP wild creature: the Catch rune is dim; picking it gives "Weaken it first!" and no turn is lost.
3. Weaken it to red: the rune unlocks and the cradle pod appears, big and easy to grab.
4. KO a wild creature: it's dazed for 8 s; a throw catches it for sure. Wait instead: it fades and you win.
5. Hints: the Spirit flies to the cradle / lead pod / Catch rune, the target pulses, then it drifts back.
6. Default Cinderwood is bright, but lanterns sputter and nothing glows or rises. G / 3 taps: embers drift
   through the trees, lanterns light up, the springs steam. Still 72 fps.
