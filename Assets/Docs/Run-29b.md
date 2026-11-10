# Run 29b: Nov 11 headset notes (overnight)

Branch `step-29b` (from master after step 29). **Not pushed.** Headset test pending for everything.

## Checks

| Check | Result |
|---|---|
| Edit-mode tests | 433 passed, 0 failed (8 new in `Nov11Tests`) |
| Full smoke | 91 ok, 0 failed (after each of the three parts) |
| Smoke `RunOnly` Step29 | 11 ok (rings, bloom heal with close shots) |
| Release Check | PASS (both parts) |

Smoke notes (not failures): the usual opening wait. One run noted "HP Tidemother label inside the Tidemother" (Lilymere Keeper); the next full run (after the label fix) didn't, so it's flaky placement. Watch it on the headset.

## Your notes, status

| # | Note | Status | Evidence |
|---|---|---|---|
| 1 | Text pushed back from the head is overdone: buttons go into the island | **Done.** Only the speech bubble keeps 45 cm. Move tablets, caption, pod cards, chips and stone tablets stay put | `Nov11Tests.KeepTextAway_OnlyOnTheSpeechBubble` |
| 2 | Pod HP ring blocky, thick, primitive; overlaps the next pod | **Done.** A slim bevelled bronze channel round the glass (like the pod rim), a rounded shaded fill with round ends and a light streak, a thin dark hairline. 1.42x the pod (was 2.1x), so it never reaches the next pod | `Run-29b-img/nov11_rings.png` (red low pod, grey fainted pod with zz) |
| 3 | Ring drops when you choose your move, before the hit lands | **Done.** In battle the ring follows the HP bar (`BattleStage.ShownHp`), which only moves when the hit plays | headset (flat Play can't show the timing well) |
| 4 | Lodge floor heal could look better | **Done.** The new heal wave across the floor: a flash, green then gold shockwaves, a rising halo, green and gold motes and + welling up all over the floor for 2.5 s | headset (lodge) |
| 5 | Dandelion: no signifier, walk doesn't stop by it, bland, needs a hand moment and a bigger heal | **Done.** While open: green motes rise from it, a + floats up every few seconds, a soft green halo breathes on the ground. A fingertip within 8 cm makes it swell, glow green and quicken its motes. The walk stops at its closest tile when you pass within ~2 tiles (first time per realm, or whenever someone is hurt and it's open), and the Spirit says "A healing dandelion! Poke it to heal your Islings." (ember-lily / lotus in the other realms; the bubble points at it). A poke squashes it, then a realm puff: Galewick white seeds on the wind, Cinderwood orange embers, Lilymere pink petals and cyan drops. Then the heal wave over the explorer: flash, two shockwaves, rising halo, sparkle column, a ring of +, the pod rings refill one by one | `nov11_bloom_idle/puff/wave.png`; `Nov11Tests.Bloom_*` |
| 6 | No warning at the tower with fewer than 3 Islings | **Done.** It was switched off in the Quick Journey (that's why you never saw it). Now 3 at every door, every mode: "Galeowl is strong. Bring at least 3 Islings." with Go in / Not yet. Never a wall | `Nov11Tests.Temple_ThreeIslings_WarnsEverywhere_NeverAWall` |
| 7 | Name plates tilt with the head | **Done.** Labels and the caption use the head's up with the roll taken out (`BattleHud.LabelUp`), for turning and placement. My first try used plain world up, which slanted the side labels when looking down (caught in the flat shot), so it now keeps them level across the view | `Nov11Tests.LabelUp_IgnoresAHeadRoll`, `battle_labels_level.png` |
| 8 | Assets look broken; tower pillars jagged | **Done for what I found** (next section) | before/after sheets |
| 9 | Team size 3 | Done (see 6) | |
| 10 | Bellows | No change (as you said) | |

## Asset audit

New tool `Tools: PropAudit.All` (bridge `exec:Diorama.EditorTools.PropAudit.All`). It renders all 137 baked props alone and close, 12 per sheet, in `Logs/PropAudit/` with a table of triangles, resolution and voxel size. `PropAudit.Some` (arg `pitch=55;A,B`) renders a few, bigger.

What was broken, and why:

| Prop | What looked wrong | Why | Fix | Tris before → after |
|---|---|---|---|---|
| TowerPillar, HallPillar, TemplePillar | Jagged, wavy columns; HallPillar's base chunky | Baked at 40 (≈5 cm voxels for a 17 cm column) with bumps, cut down to 800 triangles | Resolution 72, bumps a third, budget ~1800 | 800 → 1800 / 2086 / 1898 |
| TowerWindow, TempleWindow | Round hole's rim ragged | Subtracted hole in a bumpy wall at 60 | 84, softer wall bumps | 2000 → 3400 |
| WindTower (+ open) | Crown top shredded when seen from the seat | Crown wall only 14 cm thick round the hollow | Wall 22 cm | 7494 / 7290 (same) |
| StoneBasin | Inner lip spiky | Sharp lip and a sand floor touching the wall (its outline poked through) | Rounder lip, floor a little smaller, calm moss, 72 | 1800 → 2598 |
| ReedClump | Blades broken into bits | Tips thinner than a voxel | Thicker tips, 56 | 900 → 1400 |
| TangledKites | Strings broken | 1 cm tubes at 56 | 1.6 cm, 72 | 1574 → 2228 |
| LilyPad | Octagon ring with black spikes | Thin vein ring half sunk in the pad | Vein removed, 64 | 532 → 910 |
| WaterBall, BasinWater | Faceted | 32 resolution, low budget | 48 / 56 | 812 → 1300, 494 → 700 |

Shots: `props_before_*.png` vs `props_after_*.png`, and the three halls in place (`nov11_hall_*_close.png`). The pillars read clean now.

Looked at and left alone: the stone walls' gentle waviness (it's the hand-made style, not broken). Brazier and bellows rims show slight polygon steps up close. HeartCrystal's outline is heavy at prop scale but fine in the finale. Kite cloth edges are a bit zig-zag, but they're tangled scraps anyway. Say if any of these bother you on the headset.

Extra cost: about +9k triangles over all the fixed props, most of them in the halls (4 pillars each, about 4k). Within budget.

## Open items

- The "Tidemother HP label inside the Tidemother" smoke note, seen once (see Checks).
- Junction arrows in flat Play (from run 29): still not seen.
- From run 29: 9 (other white path), 21 (element symbol on starter cards), 23 (25 cm lean-in) are unchanged.

## Headset test list (Quick Journey order)

1. **Starter / first walk:** labels, cards and tablets stay where they are when you lean in. Only Isling / Spirit bubbles back off.
2. **First battle:** the name plates stay upright when you tilt your head. Choose a move. **Your pod's ring must not drop until the wild's hit lands** on screen. The ring should be slim and bronze round the glass, not touching the next pod.
3. **Galewick road to the tower:** walk past the dandelion. The walk stops beside it and the Spirit names it. Green motes rise and a + floats now and then. Bring a finger near (it swells, glows). Poke: you get a seed puff, then the heal wave, and the rings refill one by one.
4. **Tower door with 1–2 Islings:** "Galeowl is strong. Bring at least 3 Islings." plus Go in / Not yet. Go in works.
5. **Tower hall:** the pillars should be smooth, not jagged.
6. **Lose a fight or rest at a lodge with a hurt team:** the floor heal (waves, motes, +) and the rings refill.
7. **Cinderwood ember-lily and Lilymere lotus:** the stop and line, then embers / petals on a poke.
8. **Lilymere:** the stone basin rim, reeds and lily pads look clean.

Log lines to look for: `[HealBloom] ... the walk stopped beside the dandelion`, `[HealBloom] ... healed (poke); big puff, heal wave`, `[TempleRules] ...: 1 Isling(s) in the pods, most guardians bring 3: Go in / Not yet.`
