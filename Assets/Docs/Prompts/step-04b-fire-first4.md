# Step 4b: bake the first 4 fire creatures

Step 4 (creature kit) is committed and Mugesh is happy with the 3 starters. Now make the first 4 wild fire
creatures with the same kit, so step 5 (battle) has real opponents.

Read:
- `Assets/Docs/ConceptArt/05_fire_first4.png` and `06_fire_first4_evolutions.png` (look at them).
- `Assets/Docs/ConceptArt/Concepts_02_Summary.md`: body shapes, rigid parts, pattern zones, palettes, idle ideas.
- `Assets/Docs/GameDesign.md`: "Fire roster" table.

Make:
1. Recipes + species for **Toastling, Waxle, Kettlewing, Glazegolem** (base forms), and their evolutions
   **Crumbhog, Lanternsnail, Kilngolem** using `baseRecipe` + modifiers where it works, a full recipe where it
   doesn't. Skip Steamswan for now (its concept is being redrawn).
2. New kit parts where needed and **reusable** (name them generically): chunky spines, candle shell with drips,
   kettle lid, spout beak, pot rim, glowing crack lines (pattern zone), small flame (reuse FlamePuff).
3. Glazegolem is the rare surprise: slightly bigger, glowing cracks that pulse, heavier idle (slow sway).
4. Element on each species: all Fire.
5. Add them to the Creature Lab cycle. Save turnaround shots and compare with the concept art like last time;
   list differences.
6. Put Toastling, Waxle and Kettlewing on the test world as wanderers (MapCreature), not near spawn.

Budget: same as starters (body ≤ 6k tris). One compile batch. Don't commit until Mugesh has seen the shots.
