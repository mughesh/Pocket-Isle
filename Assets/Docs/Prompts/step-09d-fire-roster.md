# Step 9d: the rest of the fire roster

Read CLAUDE.md, `.claude-handoff.md`, GameDesign.md ("Fire roster"), the concept sheets
`Assets/Docs/ConceptArt/07_fire_rest.png`, `10_fire_redraws.png` and their summaries, and how the existing fire
Islings are built (Toastling, Waxle, Kettlewing, Glazegolem: recipes, species, moves). Work in `CreatureLab.unity`.
**Same unattended rules as step 9c** (no headset, flat Editor Play only, XR checkbox restored at the end and never
committed, commit each Isling with "(headset test pending)", no AI attribution, don't push, don't ask: decide and note).

## Order (one at a time, commit each)
1. **Steamswan** (Kettlewing's evolution, Lv 14): from the redraw; it must keep the **kettle look** (spout neck, lid
   crown, steam puffs). Set `Kettlewing.evolvesTo`.
2. **Coalbeetle → Forgehorn**
3. **Mapadillo → Emberdillo**
4. **Ashmole → Cindermole**
5. **Cindercap → Cinderbloom**: follow the redraw; must **not** look like a classic mushroom person.
6. **Embercrane → Pyrecrane**: follow the redraw; a rare surprise, elegant and special.
7. **Glazegolem rare look** (if time): richer glaze, **gold kintsugi cracks** that glow softly, a faint shimmer, so it
   reads as rare at a glance. Same for Kilngolem.

## For each Isling
- SDF recipe (rigid pivot parts, no bones, ≤ 6k tris per body), baked mesh + prefab, species (Fire, stats, rarity,
  evolution level 12–16), 3 moves (reuse existing move data where it fits; new ones from the 4 templates recoloured).
  Original designs only: nothing that resembles a known franchise creature.
- Turnaround shots (front, side, 3/4) to `Logs/BattleShots/9d_<name>.png`.
- Add it to the **Cinderwood zone encounter tables** where it fits (suggested: Coalbeetle + Mapadillo in the forest
  and ember field; Ashmole near the village and ruins; Cindercap in the lantern glade; Embercrane as a rare in the
  ember field and springs, ~5%). Keep each table's existing Islings; rebalance weights.
- Register in `SpeciesRegistry`; check saves still load.

## Done when
- All items built, in the tables, EditMode tests green, flat Play: each one appears in its zone (bridge/debug
  encounter), battles, can be caught, evolves (debug level-up).
- Update `.claude-handoff.md`, GameDesign.md (roster as built).
- **Morning summary for Mugesh** (short, scannable): what 9b/9c/9d changed, a gallery list of the shot files to look
  at, what to test on the headset first (9b puzzles → 9c Keeper → new Islings), the `pump_handle` recording reminder,
  and anything you decided on your own.
