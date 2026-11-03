# Step 14c report: Galewick first, Quick Journey v2, Cinderwood arrival, name cards (Nov 3)

Headset test pending for all of it. Tests 257/257. No Play runs (Standalone XR init is on); no APK.
**One-time:** restart the Unity Editor once before Play: the prop shader's warm-spot array grew from 8 to 12, and the
Editor keeps the old array size until a restart (builds are fine).

Setup: **Tools > Pocket Isle > Setup > First Minute (14) > Set Up MR_Main (14c Galewick first)** (already run and saved).

## Commits
| Section | Commit | What |
|---|---|---|
| prompt | `e223e48` | the step prompt |
| A | `7830e18` | trail order wind > fire > water, well gate open in builds, Keeper levels, RealmOrderSim |
| B | `5474b78` | Quick Journey to Galewick: gift, dials, ridge pre-solved, end with 3 runes, dev presets |
| C | `06899d9` | Cinderwood arrival lanterns (map diff: 4 props only), WarmSpots 12 slots |
| D | `8979cae` | realm name cards, hub gate signs |

## A. Realm order sims (`RealmOrderSim.Report`, 1000 runs per starter, grass x1.5)
Party at each Keeper: the starter (fights every battle) + the realm's visitor that breaks the shield (Toastling,
Steambara, Kitelet; caught at its patch level) + one other trail catch half the time.

| Keeper | Shards | Lv | Starter arrives (p10 / mean / p90) | Kilnpot | Pebbloop | Pufflet |
|---|---|---|---|---|---|---|
| Galeowl | 0 | 8 (was 9) | 8 / 9.9 / 12 | 100% | 54% | 68% |
| Hearthstag | 1 | 14 (base 12, was 10) | 10 / 11.8 / 14 | 53% | 91% | 44% |
| Tidemother | 2 | 13 (unchanged) | 12 / 14.7 / 17 | 50% | 65% | 98% |

- Before tuning (old levels): Galeowl 84 / 6 / 11%, Hearthstag 96 / 100 / 96%. Galewick fire patch +1 level (Toastling 7-9).
- **Per-starter 60-75% can't be met by levels alone:** each Keeper's shield breaks to one element, so the starter of
  that element wins easily and the others depend on the visitor. Averages are in band (74 / 63 / 71%).
- Trade-off: a player who still goes to Cinderwood first meets Hearthstag Lv 12 (was 10).

## B. Quick Journey (Galewick)
Keeper sim, Galeowl Lv 8, gifts Toastling + Whirlby (Whirlby picked by `QuickJourneySim.SweepWind`), 1000 fights:

| Starter | Gift Lv | Raised to | Starter at Galeowl | Win |
|---|---|---|---|---|
| Kilnpot | 5 | 8 | 9.8 | 80% |
| Pebbloop | 8 | 9 | 10.6 | 74% |
| Pufflet | 8 | 7 | 9.0 | 76% |

### Dry run: **13:00, over the 12:00 target** (0.5 s per tile, 45 s per battle)
| Beat | Time | How |
|---|---|---|
| Opening + lessons | 3:32 | opening 75 s, lessons 70 s, tutorial battle 68 s |
| Gift | 0:15 | two pods, quick.gate |
| To the wind gate | 0:11 | 11 tiles |
| Arrival sweep | 0:30 | two puffs |
| Mill | 1:20 | 37 tiles, 0.3 battles, flick 40 s |
| Kites | 1:42 | 24 tiles, three kites 75 s |
| Tower | 1:11 | 41 tiles (ridge clouds gone), tie the kites 30 s |
| Untangle | 1:00 | Galeowl's knots |
| Galeowl | 2:30 | meeting, fight, shard |
| Home | 0:17 | 8 + 15 tiles |
| Statue + end | 0:30 | place, whisper, quick.end |

**Options for Mugesh (nothing cut yet):**
| Option | Saves | Cost |
|---|---|---|
| 2 kites instead of 3 in the quick slot | ~0:25 | one fewer pinch-and-fly |
| Pre-solve the mill in the quick slot | ~0:45 | judges never see the flick gesture |
| Untangle: 2 knots instead of 3 in the quick slot | ~0:20 | shorter Keeper moment |
| Accept ~13 min | 0 | the estimates are rough; the real timer is in the dev readout |
My pick: 2 kites + 2 knots (~0:45 saved, every gesture still taught).

## C. Cinderwood arrival lanterns
4 stone lanterns at (10,7), (13,8), (10,9), (12,11) (x, y), placed after every random draw: the Cinderwood map diff
vs before is those 4 props, nothing else. One touch or look + pinch races the fire down the line (0.15 s apart), warm
glow radius 1.6 tiles each (WarmSpots slots 8-11), embers drift up, `cw.arrive.lanterns.done`.

## D. Name cards and gate signs
Shots: `Logs/BattleShots/14c_cards.png`, `14c_gatesigns.png` (baked cards in a preview scene).
**Not shot** (need Play with XR init off): the cards in place over the island with their line, the hub with the signs,
the lanterns before / after, the quick end card.

## Headset test list (in order)
1. New quick game (Title > Quick Journey): opening and lessons as before. After the capture lesson: "The road to
   Galewick is windy..." and two pods (Toastling, Whirlby) land on the rim; the Spirit points at the wind gate; the
   wind gate's sign glows gold. Log: `[QuickJourney] Gift given`.
2. Through the wind gate: the Galewick card rises over the far side after the arrival line, with "The windmill
   highlands, where the wind stopped", then sinks. Log: `[RealmNameCard] galewick`.
3. Sweep the arrival clouds, mill, kites. At the ridge: no clouds. Log: `[CloudBankPiece] gw.clouds cleared already`.
4. Tower, untangle, Galeowl (is it fair with your starter?), home. The Still Isle card says "Home".
5. Wind shard into the statue: the whisper, "Did the statue just... speak?", then the end card with 3 runes. Try
   **See the ending** (the finale plays, then the title); Continue the quick save: it is back at the end, not finished.
   Log: `See the ending: from the wake (the quick save is suspended)`.
6. Full adventure: after the tutorial the trail goes to the wind gate (gold sign). Go to Cinderwood: card, then four
   dark lanterns; touch one: the fire runs down all four, embers rise. Log: `[LanternHollowPiece] cw.arrive`.
7. Are the gate signs readable from your seat? Is any card in your way?
