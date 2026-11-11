# Run 30: the Nov 11 evening fix list

Prompt: `Assets/Docs/Prompts/fix-list-nov11.md`. Branch `panel-review` (on top of the panel's Tier 1 and Tier 2). **Not pushed. Nothing here has been on the headset.**
Headset checklist: `Assets/Docs/TEST-NOW.md` (fresh; your Nov 11 pass is kept as `TEST-NOW-nov11-filled.md`).

## Checks

| Check | Result |
|---|---|
| Edit-mode tests | **468 passed, 0 failed** (new: `Nov11FixTests`, `Nov11WalkTests`) |
| Smoke run with seat shots | **115 ok, 0 failed, 0 skipped** (9.7 min; shots in `Logs/Panel`) |
| Walking simulation | **0 failures** in 35,736 plans (table below) |
| Audio Release Check | passed (0 cues without clips, 0 free clips in cues) |
| Readiness Report | no "no" rows |
| Peak in the smoke run (Editor, flat) | 145 draws, 260k triangles |

Smoke notes, not failures: the usual opening wait; the Step29 "junction shot" on the Still Isle ends "blocked" at c15 r15 (the statue), as it did in the panel run before this list.

**One thing I broke and fixed on the way:** the first smoke run of this list failed 4 beats. The "never off the island" rule cut every walk plan at the edge of the view it started in, also when the view follows the explorer, so every long walk stopped after about 7 tiles. Now the plan is cut only while the view is held (a hand moment, the lotus garden, the mill); each single step is still checked against the window either way. Walk around a lot on the headset: this is the change most likely to feel wrong.

## The list, status

| # | Item | Status | What changed | Evidence |
|---|---|---|---|---|
| A1 | Never off the island | **Done** | A step is only ever onto a tile the island shows now (`WorldWindow.Shows`); under a held view the plan is cut at the view's edge (R51) | `Nov11WalkTests.InWindow_*`, `Explorer_GuardsEveryStep_WithTheWindow`; headset |
| A2 | Path entry stops on the path | **Done** | A walk coming onto a road stops on its first road tile; a walk along a road stops on its last tile; only the swipe's first two steps may leave a road, so you can still cross the other lane or step off (R52) | `Nov11WalkTests.Walk_*` (4) |
| A3 | Walking simulation, every realm | **Done** | `WalkSim` + bridge `Nov11Walk.Sim`; also runs as a test, so a map or rule change that breaks walking fails the tests | table below; `WalkSim_EveryRealm_IsClean` |
| A4 | Small stones beside roads | **Done** | 19 props moved or removed, list below | `SmallClutter_IsOffTheRoads_InEveryRealm` |
| A5 | Zig-zag paths | **Partly** | The smoky hollow is a straight 2-wide band. 13 grass tiles inside roads filled. The other "jogs" the audit finds are 2-wide corners and plaza edges, left alone | `Hollow_IsAStraightBand_*`, `NoGrassHoles_InsideRoads`; `Logs/WalkSim/*_before/after.png` |
| B6 | Show which move to press | **Done** | On your turn the Isling's own element move breathes 1.0 to 1.08 over 1.2 s with a rim glow until you pick | `BattleRunes_TheIslingsOwnElementMove_Breathes`; headset |
| C7 | Stop practice | **Done** | Any tap stop counts ("Nice stop!"); on or next to the gold tile: "Perfect!" | `Practice_AnyTapStopCounts_GoldIsPerfect` |
| D8 | Thorns burn, then go | **Done** | 1.3 s of fire spreading from the touched tile, then the 1.6 s crumble, with the new burn sound | `Thorns_BurnBeforeTheyCrumble`; headset |
| D9 | Hollow lanterns with the fire finger | **Done** | Touch the village flame or a lit lantern: the fingertip catches; poke each lantern. Look + pinch still works | `HollowLanterns_LightWithTheFireFinger`; smoke `cw.hollow`; headset |
| D10 | Hold rings too thin | **Done** | Lodge palm ring and the dying flame's ring: wider band, brighter, with a chime when full | `HoldRings_AreWiderBrighter_WithAChime`; headset |
| D11 | Poke ghost over each healing bloom | **Done** | Once per realm, 12 s, fades when a hand is near | `HealBloom_ShowsAPokeGhost_Once`; headset |
| E12 | Move Galewick's lodge | **Done** | c14 r40 to c34 r33: roadside, north-east of the mill, south of the ditch. The stub road to the old spot is grass again | `GalewickLodge_SitsRoadsideByTheMill`; `Logs/WalkSim/WorldMap_Galewick_before/after.png` |
| E13 | Windmill shows the bridge | **Done** | The mill's focus frames mill + bridge; while the bridge lowers the view holds on it, 1.2 s more, then returns | `Mill_FramesAndHoldsTheBridge`; headset |
| F14 | Text over the explorer | **Done** | The explorer is a keep-clear zone for every bubble and chip (R53) | `Explorer_KeepsBubblesOffItself`; shot `Logs/Panel/122_lilymere_*` |
| G15 | Fingertip fire sounds | **Partly** | New catch "whoomp" (3 takes). **The crackle loop is still the old one**: 6 new takes all came back as sparse pops with silence between | table below |
| G16 | Thorns burning | **Done** | 2 takes | |
| G17 | Lantern lighting + burn loop | **Done** | 3 new whoosh takes, 1 burn loop over the lit lanterns | |
| G18 | Hold tone + chime | **Done** | 1 new fill take, 2 chimes | |
| G19 | Keeper voices default | **Done** | New voices are the default. The switch: **Dev panel > Tools tab > "Sound A/B" > Voices** | `NewVoices_AreTheDefault` |
| G20 | The Heart's voice | **Partly** | First line: the old synth "moooo" you liked stays as the low hum, with a new singing-bowl and choir call on top (one take passed). Voices: blip gives the old moo + bell back. **Typing murmurs unchanged**: 6 new takes failed | headset: tell me which you prefer |

## Walking simulation

Every walkable tile x 4 swipes, walked on until something stops it. Checked: stays on walkable tiles inside the map, never crosses a road into grass, never leaves a road's end, never ends on a blocked or prop tile, ends.

| Map | Start tiles | Plans | Failed |
|---|---|---|---|
| Still Isle | 582 | 2,328 | 0 |
| Cinderwood | 1,248 | 4,992 | 0 |
| Galewick | 1,213 | 4,852 | 0 |
| Lilymere | 1,156 | 4,624 | 0 |
| Lodge, Forge Hall, Lake Temple Hall, Tower Hall | 268 | 1,072 | 0 |

Run twice (before and after the first wild Isling, since the first tall grass stops a walk): 35,736 plans, 0 failed. Full table with how walks end: `Logs/WalkSim/report.md`. Not simulated: the scene's hand moments, and the island window (that is R51, checked per step at runtime).

## Props moved (A4)

| Realm | Change |
|---|---|
| Still Isle | MossBoulder c13 r5 to c12 r5; c10 r15 to c10 r14 |
| Cinderwood | Woodpile c13 r29 to c12 r29; c24 r31 to c24 r32 |
| Galewick | HayBale c33 r12 removed; c49 r19 to c49 r18; c38 r26 to c38 r24; c22 r37 to c21 r37; c30 r38 removed; c35 r40 to c35 r38; c33 r42 to c33 r43. StoneWall c37 r40 removed (it closed a lane). Lane tiles c38 r26, c30 r38, c35 r40 are road now |
| Lilymere | ReedClump c23 r10, c27 r10, c31 r47, c10 r57 removed; c8 r42 to c8 r43; c29 r44 to c29 r43; c19 r47 to c19 r48 |

Lanterns, fences, braziers and hand-moment props beside roads were left (listed in `Logs/WalkSim/stones.md`).

## Sounds (G)

Two rounds. The first prompts said soft / gentle / quiet / deep and most takes came back near silent or all sub-bass, so the six failing cues got a second round with brighter prompts.

| Cue | Takes | Passed | In the game |
|---|---|---|---|
| finger.catch | 8 | 4 | 3 |
| ember.crackle (finger loop) | 6 | 0 | old take kept |
| thorn.burn | 3 | 3 | 2 |
| lantern.light | 8 | 3 | 3 added to the cue |
| lantern.burn | 6 | 3 | 1 |
| shrine.fill | 3 | 2 | 1 added |
| hold.done | 4 | 4 | 2 |
| voice.call.heart | 8 | 1 | 1 (the two older calls set aside in `Clips/Gen/_retired`) |
| voice.grain.heart | 6 | 0 | old murmurs kept |

- **Credits: 2,150 of the 3,000 cap** (1,210 + 940). Ledger total 17,629 of 40,000.
- Picked by measurement (level above 250 Hz, sub share, clipping, gaps). **Nobody has heard them.**
- The four new cues hold generated clips only; with no clip the game plays its old fallback.
- Lane: `pick_nov11.py` adds only these cues to the manifest. `pick.py` was not used for this: a re-run rebuilds the whole manifest and would undo step 25's hand-cut steps and beds.

## Not done

- Fingertip crackle loop and the Heart's typing murmurs (above). 850 credits of the cap are left if you want a third try.
- Zig-zags beyond the smoky hollow (your call after you see the pictures).
- No device build made.
