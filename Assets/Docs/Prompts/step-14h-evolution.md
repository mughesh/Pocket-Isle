# Step 14h: evolution you can see (in your palm), and the Keeper's thanks

Decided Nov 4 (design chat). Orient first if you're a fresh session. Commit per section "(headset test pending)", no AI
attribution, don't push. Read `Progression.cs`, `GrowthDirector.cs`, `BattleStage.Evolve`, `PodRack.EvolveInPod`,
`EncounterDirector` (reward flow), `GameState.LeadLevel/AllLevels`, the ember palm carry (10f) and the finale's palm
handoff (13) for palm-up detection, `KeeperHall` victory flow, `AudioIds` (`evo.rise`, `evo.burst`, `jingle.evolve`, cries).

**Today:** the Isling on the battle pad evolves there with the full show (keep it). Benched Islings that reach their
level evolve silently inside their rim pod after the battle (tiny flicker, no sound, easy to miss). Dev panel level
changes run no check at all. Starters evolve at Lv 14, others 16–18, so a 10-minute Quick Journey never shows one.

## A. "Ready to evolve" and evolution in your palm
1. Replace the automatic in-pod evolution (`GrowthDirector.EvolveAll` → `PodRack.EvolveInPod`). When a party Isling
   is due (any cause: battle XP while benched, dev panel levels, debug keys), its rim pod **glows gold and pulses**
   (the objective gold), and once things are calm (not in battle, no lesson teaching, no menu) the Spirit says
   `evolve.ready` (objective): "{name} is glowing... it's ready to grow!" chip "Hold it" — pinned until done or the
   player walks into a battle (then it re-pins after).
2. **The palm moment:** grab the glowing pod off the rim and turn the hand **palm up** (reuse the ember/finale palm-up
   test): the pod settles on the palm, opens, and the Isling stands on the palm at a readable size (about a real
   Isling's battle size). The full evolution show runs there (white flash, old/new forms swapping faster and faster,
   `evo.rise` loop, burst, `evo.burst`, `jingle.evolve` + music duck, gold ring, **the new form's cry**, its new name
   popped in gold, a hop on the palm, its new card for 3 s). Then it curls back into its pod, which returns to its
   socket (or the player puts it back; the 12 cm socket preview applies).
   Fallbacks: look + pinch the glowing pod = it floats up above the island and evolves there with the same show; if
   the palm isn't detected within 6 s of holding, the show runs above the hand anyway. Never forced: the player can
   ignore it; it stays ready.
3. Several due at once: one at a time, each pod glows in turn.
4. The battle-pad evolution stays as is, but add the new form's **cry** after the burst (it has none today).
5. Dev panel Team: level changes and "add species" run the due check immediately (the pod starts glowing). Add a Dev
   button "Make lead ready to evolve" for testing.
6. Lodge Islings never become due (no XP there); an Isling moved from the lodge to the rim with a level already past
   its evolve level becomes due on the rim.
7. Lines: `evolve.ready` (above), `evolve.first` kept for the very first one ("What? It's evolving!"), `evolve.done`
   chatter "{new name}! So strong now." Save: an Isling's "due" state isn't stored; it's recomputed on load.

## B. The Keeper's thanks (judges see an evolution)
8. After a Keeper is calmed and the shard has flown to the Spirit, **if the player's starter (or its line) hasn't
   evolved yet**, the Keeper's shard light reaches the starter's pod: the Keeper line `keeper.thanks.evolve` (in that
   Keeper's voice, e.g. Galeowl: "Your friend fought so bravely... let the wind lift it higher.") and the starter
   becomes **ready to evolve** (A's glowing pod + `evolve.ready`) with its level raised to its evolve level if below.
   Only once per save, from whichever Keeper is first; the full game too. Make sure the restoration wave, the return
   gate and the shard's hand-off still play in their order (the evolution prompt waits until they're done and the
   player is back on the realm's map).
9. Quick Journey: this happens after Galeowl; the dry run gains ~30 s; report the new estimate.

## Housekeeping
- EditMode tests: due detection from every cause, one-at-a-time queue, palm-up trigger with synthetic hands, the
  Keeper thanks once per save, no evolution during battles or lessons.
- Shots `Logs/BattleShots/14h_*` (glowing pod, Isling on a synthetic palm mid-show, the new card). Missing sounds go
  to `Audio/PhaseB-Hooks.md`.
- Update `.claude-handoff.md`, GameDesign.md (Evolution), QA-Guide; `Run-14h.md` with the headset test list.
