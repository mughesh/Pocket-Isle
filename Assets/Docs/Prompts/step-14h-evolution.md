# Step 14h: the growth loop: Journal, evolution in your palm, team-scaled Keepers, readable battles, finale crystal

Decided Nov 4 (design chat, rewritten at noon with Mugesh's new notes). Orient first if you're a fresh session
(`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-14g.md`, the Nov 4 notes commit `bacc20e`). Commit per section
"(headset test pending)", no AI attribution, don't push. Mugesh tests between sections.

**The idea that ties it together:** catch Islings → the **Journal** fills ("12 / 46") → they **grow and evolve in your
palm** (each new form is a new Journal entry) → a bigger team beats a **Keeper tuned to your team**. Every part rewards
the others. The game has **46 Islings** (20 fire, 14 water, 12 wind; the Lantern Spirit and the three Keepers don't
count).

Read: `Progression.cs`, `GrowthDirector.cs`, `BattleStage.Evolve`, `PodRack.EvolveInPod`, `EncounterDirector` (reward
flow), `GameState.LeadLevel/AllLevels`, the ember palm carry (10f) and the finale palm handoff (13), `KeeperHall`,
`ShardScaling`, `TempleRules` (bacc20e), `FinaleDirector` (collection card), `LodgeRoom`, `AudioIds` (`evo.rise`,
`evo.burst`, `jingle.evolve`, cries), the Keeper sims.

## A. Evolution: ready, then in your palm
Today: the Isling on the battle pad evolves there with the full show (keep). Benched Islings that reach their level
evolve silently in their rim pod after a battle. Dev panel level changes run no check (so it looked "inconsistent").
1. Replace the automatic in-pod evolution. When a party Isling is **due** (any cause: battle XP while benched, dev
   panel levels, debug keys, moved from the lodge already past its level), its rim pod **glows gold and pulses**; when
   calm (no battle, lesson, menu) the Spirit says `evolve.ready` (objective): "{name} is glowing... it's ready to
   grow!" chip "Hold it", pinned until done (re-pinned after a battle).
2. **The palm moment:** grab the glowing pod and turn the hand **palm up** (reuse the ember/finale palm-up test): the pod
   settles on the palm, opens, the Isling stands on the palm (about its battle size). Full show: white flash, forms
   swapping faster and faster, `evo.rise`, burst + `evo.burst`, `jingle.evolve` with a music duck, gold ring, **the new
   form's cry**, the new name in gold, a hop, its card for 3 s; then it curls back into its pod, which returns to its
   socket (12 cm preview applies). Fallbacks: look + pinch the glowing pod = it floats above the island and evolves
   there; palm not detected within 6 s of holding = the show runs above the hand. Never forced.
3. Several due: one at a time. Battle-pad evolution unchanged except it adds the new form's **cry**.
4. Dev panel: level changes and "add species" run the due check at once; add "Make lead ready to evolve".
5. Lines: `evolve.ready`, `evolve.first` kept for the first one, `evolve.done` chatter "{new name}! So strong now."
   "Due" isn't saved; it's recomputed on load.

## B. The Keeper's thanks (judges see an evolution)
6. After the first Keeper is calmed, the shard has gone to the Spirit, the restoration wave and return gate have played
   and the player is back on the realm map: if the starter's line hasn't evolved, the Keeper's line
   `keeper.thanks.evolve` (that Keeper's voice; Galeowl: "Your friend fought so bravely... let the wind lift it
   higher.") and the starter becomes **ready to evolve** (level raised to its evolve level if below). Once per save, the
   full game too. Report the Quick Journey dry-run change.

## C. Readable battles: no Isling blocks your view
7. Mugesh: an evolved Pebbloop on the near pad hid the wild Isling completely (he looks from behind his Isling). Rule
   for **every species and form** (and the wild side too): on the battle pads, scale an actor down if needed so the
   near actor's top stays below the line from the player's eyes to the wild actor's centre (with margin), and no
   actor is taller than ~1.3x its opponent's height. Only on the pads (palm, Journal, lodge and map keep the true size).
   Shots of the tallest forms (Riverloom, Skyshiplope, Lighthousewhale, Kilngolem, Nimbulpaca, Kitewyrm…) on the near
   pad vs a small wild one: `14h_pad_<name>.png`.

## D. The Isling Journal
8. **Data:** per species: unseen / seen (met in battle) / caught (caught or evolved into). Saved. Old saves: party +
   lodge species count as caught, nothing as seen.
9. **The Journal:** a stone book that opens **floating over the island** (like the settings board, old-size rules, island
   still visible around it): title "Isling Journal", a big count **"Found 12 / 46"**, three element columns or tabs
   (Fire 20, Water 14, Wind 12, each with its own count), a grid of small portraits:
   - caught: full-colour portrait, name, "Lv N" of your best one;
   - seen: full-colour but dimmed, name, "Seen";
   - unseen: **black silhouette**, "???", and a hint: realm + spot from its encounter zone ("Galewick, kite field") or
     "Rare" for rares; evolved forms show "Evolves from {base}" once the base is caught, else "???".
   Poke a portrait (or look + pinch) for a small card (element, moves, where it lives, evolution). Pages/scroll as
   needed. Portraits: render every species once at edit time into a texture atlas (front 3/4 view, transparent
   background); silhouettes = the same atlas tinted black. ≤ 2 draws for the grid.
10. **Opening it:** a **Journal** tablet in the game menu (the menu becomes Resume · Journal · Controls · Settings ·
    Title + the speaker, still one row at the old size), and a stone book on the Lodge pedestal (poke it).
11. **Catch / evolve toast:** every first catch or new evolved form: a small stone tag pops over the island for 2.5 s:
    "New Isling! Toastling · 13 / 46" (the Isling's portrait beside it), with a soft chime. First time ever: the
    Spirit `journal.first` "Every Isling you meet goes in your Journal. Open the menu to see it!"
12. The Heart's post-game collection card reads from the same data (one source of truth).

## E. Temples: warn, don't block; Keepers tuned to your team
13. Replace the hard door minimum (bacc20e `TempleRules`) with a **warning**: at the door with fewer than the
    suggested Islings (Galewick 4, others 6), the Spirit: "{Keeper} is strong. Most guardians bring {n} friends." and
    two tablets **Go in** / **Not yet**. Go in = enter normally. No warning in the Quick Journey.
14. **Keeper level from your team**, tuned by sim targets, not a hand-picked formula: with the realm's normal route
    (catches available on the way):
    | Team at the door | Win chance target |
    |---|---|
    | 1 Isling, Lv 5 | ~15% |
    | 4 Islings, Lv 7–10 | ~70% |
    | 6 Islings, well levelled | ≥ 85% |
    The Keeper follows the team only up to a cap (realm base + a few levels), so **levelling up always raises the win
    chance**. Shard-count scaling still applies. Quick Journey keeps its fixed, sim-tuned team and Keeper. Report the
    formula and the sim tables per Keeper.

## F. Finale: carrying the Spirit to the crystal
15. Mugesh read "Lift me up to the Heart" as **his own heart**. New line `spirit.finale.hand`: **"Lift me up to the
    gold crystal on its forehead."** While it waits: the crystal pulses with the objective gold ring, a **dotted gold
    arc** runs from the player's palm to the crystal (updates as the hand moves), and the Spirit leans toward it.
16. The ghost hand must **rise palm-up from the player's hand position all the way to the crystal** (retarget: source =
    the player's palm area, target = the crystal); check it in a shot sequence `14h_finale_ghost_*.png`.

## Housekeeping
- EditMode tests: due detection from every cause, the one-at-a-time queue, palm-up trigger (synthetic hands), Keeper
  thanks once per save, pad scaling rule, Journal states + counts + old-save migration, temple warning, Keeper scaling
  monotonic in team level.
- Missing sounds to `Audio/PhaseB-Hooks.md` (journal open/page, toast chime, glowing pod loop).
- Update `.claude-handoff.md`, GameDesign.md (Growth loop: Journal, Evolution, Keepers), QA-Guide; `Run-14h.md` with
  shots and the headset test list in order.
