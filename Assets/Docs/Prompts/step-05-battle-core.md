# Step 5: encounters + battle core

Read CLAUDE.md, Assets/Docs/BuildPlan.md and Assets/Docs/GameDesign.md (sections "Encounters", "Battle").
Steps 3 (world window with ZoomTo/ZoomOut) and 4/4b (creatures, CreatureView with Hop/Faint) are done.
Goal: walk into tall grass or a wandering creature → the island zooms in → a 1v1 turn-based battle with 3 moves
picked by gaze + pinch → win or flee → zoom out and keep walking. **Logic is pure data + C#, presentation is
separate**, so later steps (capture, party, Keepers) plug in without rewrites.

## Data (ScriptableObjects)
1. `TypeChart`: Water > Fire > Wind > Water, Neutral has none. Strong ×1.5, weak ×0.67 (editable).
2. `MoveData`: name, element, kind (Attack / Support), power, accuracy, animation template
   (Shoot / Lunge / Slam / Shield), support effect (shield halves next hit, raise attack one stage, heal %).
3. Add to `CreatureSpecies`: base HP / Attack / Defense / Speed, and 3 move slots. Every species gets:
   one element attack, one basic Neutral attack, one support. Write placeholder moves for all baked species
   (names can be silly placeholders, e.g. "Ember Puff", "Tackle", "Hunker Down").
4. `CreatureInstance` (runtime, serializable for the save later): species, level, current HP, XP, nickname.
5. `EncounterTable` per region: entries (species, level range, weight, rare flag). Rare ≈ 5% total.
   Test world grass table: Toastling, Waxle, Kettlewing common; Glazegolem rare. Water table: Pebbloop for now.
6. A temporary `Party` holding one Kilnpot Lv 5 (party UI and pods are step 6).

## Battle logic (no MonoBehaviour)
7. `BattleState` / `BattleSystem`: turn order by Speed, damage =
   level factor × power × Attack/Defense × type multiplier × random 0.9–1.1, accuracy rolls, support effects,
   faint at 0 HP. Wild AI: random weighted move. Flee: chance from speed ratio. Seedable RNG.
   Emits events (MoveUsed, Damage(amount, effectiveness), Fainted, BattleEnded(result)).
8. Edit-mode unit tests for damage, type chart, turn order, flee, support effects.

## Encounter flow
9. **Design change (Oct 23): wild creatures are hidden; nothing battle-able wanders the map.** Remove the
   wandering MapCreatures from step 4b (keep the starters near spawn only as test props, or remove them).
   - Tall grass: each step onto T has a chance (start 1 in 8, exposed) to roll the region's grass table.
   - Ponds: standing next to W facing it, a thumb **tap** rolls the region's water table (ripple first, ~1 s,
     then the creature leaps out). Tap is context-sensitive: facing water = fish, standing still elsewhere = hop,
     walking = stop. `EncounterTable` gets a habitat (Grass / Water).
10. Start: grass rustle on the tile; if rare, a gold sparkle. `WorldWindow.ZoomTo(tile, 3, 0.6)`.
    The wild creature hops out onto the tile in front of the explorer; your Kilnpot pops out beside the explorer
    with a quick pod-burst effect (the real grab-and-throw comes in step 6). Walking is disabled during battle.

## Presentation
11. **Move runes:** 3 runes + a Flee rune float just above the near edge of the island rim, facing the head,
    within the central ~70° view. Each shows the move name and element colour.
    **Gaze = head gaze** (Quest 3 has no eye tracking): a soft reticle from the centre eye; the rune under it
    highlights; **pinch** (either hand) confirms. Also allow poking a rune directly. Use ISDK where it fits.
12. Animation templates on CreatureView, recoloured by element: Shoot (projectile), Lunge (dash + return),
    Slam (hop + ground ring), Shield (bubble). One pooled particle system. Hit flash and small knockback.
13. Small HP bar floating above each creature (name + level), and damage numbers with
    "super effective" / "not very effective" text. Port the look from creature-diorama-v2.html.
14. End: winner hops, loser Faint(); short "+XP" text (XP numbers are stored, levelling comes in step 7).
    Zoom out (0.6 s), wild creature removed, walking re-enabled.
    If Kilnpot faints: end the battle and zoom out (full losing rules come in step 6).

## Housekeeping
- Commit 4b first if Mugesh approved it. Batch compiles. Don't commit step 5 until he tests on the headset.
- Update `.claude-handoff.md`. Tag the commit `pre-goa` once he confirms on the headset.

## Test on the headset
1. Walk through tall grass: a battle starts within a few steps; the island zooms in smoothly.
2. Look at a rune, pinch: the move plays, HP drops, damage number shows.
3. Use the Fire move on a fire creature: "not very effective".
4. Win: the wild one faints, +XP, zoom out, walk again.
5. Flee works sometimes. Facing the pond and tapping: ripples, then a creature leaps out. Tapping away from
   water still hops.
6. Pinching to pick moves doesn't trigger walking, and grabbing the rim still works.
7. 72 fps during the battle effects.
