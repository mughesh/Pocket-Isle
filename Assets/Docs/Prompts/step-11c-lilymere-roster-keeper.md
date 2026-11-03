# Step 11c: water roster + Tidemother

Read CLAUDE.md, `.claude-handoff.md` (10c wind roster, **10e one Keeper flow for every realm**, 10d/10e Galeowl),
`Concepts_03_Summary.md` (sheets 13, 14), `13_water_roster.png`, `14_water_evolutions.png`, `02_keepers_turnaround.png`
+ `Concepts_01_Summary.md` (Tidemother). Same rules as 10c and 10e.

## A. Water roster
1. Marimoss → Marimound, Drizzcan → Rainurn, Lilyvole → Lotusvole, Sailfold → Galleyfold, Bottlewhale (rare) →
   Lighthousewhale. Full recipes from the sheets, rigid pivot parts with their idle motions (Drizzcan's drops drip,
   Lilyvole's pad wobbles, Sailfold rocks like on waves, Bottlewhale's tiny ship inside, Lighthousewhale's lamp
   turns), ≤ the fire/wind rosters' tris, 3 moves each (a Water attack, a Neutral, a support), evolve at 16 (rare 18).
   Riverloom (Pebbloop) exists already.
2. Lilymere tables by zone: ~70% water, ~20% wind visitors (Kitelet, Samarling, Vanehare), rare pond Bottlewhale ~5%.
   Levels from the shard scaling. First-meet lines for 2 of them.

## B. Tidemother (uses 10e's keeper flow: no new keeper code beyond her data and looks)
3. Lake temple interior: a round flooded hall, shallow water floor with stepping stones, the hoarded water piled up in
   a glassy dome behind her, lily garden, a high window of still water-light. Door from 11b's surfaced temple.
4. **Tidemother:** enormous cozy sea-turtle (~2.5x an Isling, ≤ 10k tris): seafoam-green skin, jade dome shell with
   mini lily pads and a pink lotus on top, warm eyes, stubby flippers. Frightened (KeeperFury): she clutches the water
   dome, a churning grey-teal aura with spray, shell plates rattle, eyes worried red-teal; calmed: lets the dome go
   (it flows out of the hall as a wave), soft green, the lotus on her shell opens.
5. Battle: Lv 9 at 0 shards (+2 per other shard), HP ~2.75x, moves Tide Shot (water), Shell Slam (neutral, every 3rd
   turn), **Lily Shell** shield (breaks to a Wind move or 3 hits, 2 open turns, returns), phase 2 below 50%. No
   capture. Keeper sim per starter with a Wind helper available: target 60–75% (0 shards); report.
6. Win: she releases the water, the water shard floats to the Spirit, restoration wave from the temple (Lilymere
   restored state), return gate. Hub: the statue's **water socket** (blue). Lines `lm.keeper.*` (meet, shield,
   shield.break, win: afraid of losing the water again, thanks the guardian), `lm.restored`. With all three sockets
   lit the Spirit says `statue.three`: "All three pieces home..." (the finale itself is a later step).
7. Dev panel: Lilymere flags (pads, rain, basin, rise, keeper, shard) and Go to: Lake temple.

## Housekeeping
- Shots `Logs/BattleShots/11c_<name>.png`, lineup with the Kilnpot line, `11c_tidemother.png` (frightened, calmed).
- EditMode tests green; commit per section "(headset test pending)", no AI attribution, don't push. Update
  `.claude-handoff.md`, GameDesign.md, QA-Guide/TestPlan (a Lilymere session).
