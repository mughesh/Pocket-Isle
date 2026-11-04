# Step 14h report: the growth loop (Journal, evolution in your palm, team-tuned Keepers, readable battles, finale crystal)

Headset test pending for all of it. Tests 294/294. No build (not asked). Nothing pushed.
Setup (already run, no scene changes needed): **Tools > Pocket Isle > Setup > First Minute (14) > Set Up MR_Main (14h growth loop)**
(reimports the script, bakes the Journal). Everything new is made at runtime: the scene had unsaved Editor changes I didn't make,
so I didn't save it.

## Commits

| Commit | What |
|---|---|
| `40c4bc7` | A + B: evolve in your palm; the first Keeper's thanks |
| `720f686` | C: readable battles (pad fit) |
| `cbea526` | D: the Isling Journal; the shorter game menu (your mid-step note) |
| `5c5c18a` | E: temples warn; Keepers tuned to your team |
| `d9b16ae` | F: carry the Spirit to the gold crystal |
| (docs) | this report, handoff, GameDesign, QA guide, PhaseB hooks |

A and B share one commit (B's thanks makes the starter "due", A's code).

## A. Evolution: ready, then in your palm

- **No more silent in-pod evolutions.** "Due" = level at or past the evolve level, any cause (benched XP, Dev panel, debug keys,
  a lodge Isling already past it). Never saved; recomputed every frame, so a loaded save shows it again.
- **Due pod:** glows gold and pulses; while exploring it alone can be grabbed (the pod strip switches to pod-grab mode, like the
  lodge). When calm (no battle, lesson, menu, Journal, tutorial, finale, scene change) the Spirit pins `evolve.ready`
  "{name} is glowing... it's ready to grow!", chip **Hold it**. It drops in a battle and comes back after.
- **Palm moment:** grab the glowing pod, turn the hand palm up (the finale's test: palm · up ≥ 0.55 for 0.35 s):
  1. the pod settles on the palm and opens;
  2. the Isling stands there at battle size;
  3. white flash, forms swap faster and faster, `evo.rise`, burst + `evo.burst`, `jingle.evolve` with the music duck, gold ring;
  4. the new form's cry, its name in gold, a hop, its card for 3 s;
  5. it curls back into the pod, which flies to its socket.
- **Fallbacks:** look + pinch the glowing pod = it floats over the island and evolves there. Held 6 s with no palm up = the show
  runs above the hand. Let go without either = it flies home, still glowing. Never forced.
- **One at a time** (pod order). Battle-pad evolution unchanged, plus the new form's cry.
- **Dev panel:** level buttons and "Add to party" re-check at once; new **Make lead ready to evolve** (Team tab).
- **Lines:** `evolve.ready`, `evolve.first` (first one), `evolve.done` "{name}! So strong now." Lines can now use `{name}`-style
  values (`Dialogue.SetVar`).

Flat checks (shots `Logs/BattleShots/`):
- `14h_glow.png`: the due pod.
- `14h_palm_1..7.png`: Marimoss to Marimound on a synthetic palm.
- `14h_pinch_1..7.png`: look + pinch fallback, Kilnpot to Kilnhearth over the island.
- `14h_above_1..7.png`: palm-down hold, the 6 s fallback over the hand (Pufflet to Nimbulpaca).

Flat Play has no head: the card and pop text face the floor camera in those shots (mirrored); in the headset they face you.

## B. The Keeper's thanks

After the **first** Keeper calmed (shard to the Spirit, wave, return gate, back on the realm map), if the starter's line hasn't
evolved: that Keeper's line `keeper.thanks.evolve.<wind|fire|water>` (its voice; Galeowl: "Your friend fought so bravely... let
the wind lift it higher.") and the starter's level rises to its evolve level, so its pod glows. Once per save (puzzle flag
`keeper.thanks`), both slots.
**Quick Journey dry run change:** after Galeowl the starter becomes due. That's one palm moment (~15 s, player-paced) between
the tower and the statue. No battle follows in the quick route, so the sim's win rates don't change.

## C. Readable battles

`PadFit`, battle pads only:
- **Sightline:** the near actor's top stays under the line from your eyes to the wild one's centre, minus 30% of the wild one's
  height.
- **Size cap:** no actor over 1.3× its opponent. Keepers are exempt: they're meant to tower.
- **Limits:** it only shrinks, never below 0.45×, and eases in.
- **Heights:** taken from the drawn mesh. The rig's nominal height missed lamps and crests: the first try didn't shrink Riverloom
  enough.

The palm, Journal, lodge and map keep true sizes.

| Near pad (Lv 30) vs Pufflet Lv 5, eyes 30 cm above / 50 cm in front | Fit |
|---|---|
| Riverloom | 0.65 |
| Skyshiplope | 0.74 |
| Lighthousewhale | 0.84 |
| Kilngolem | 0.70 |
| Nimbulpaca | 0.89 |
| Kitewyrm | 0.93 |

Shots: `14h_pad_<name>.png`. The whole wild Isling shows over the near one in each.

## D. The Isling Journal

- **Data:** the save's `caught` / `seen` sets, one source of truth (Journal, toast, the Heart's collection card).
  - Evolving into a form counts as caught.
  - Old saves: the pods and lodge count as caught.
  - The existing `seen` list (met in battle since 7c) is real data, so I kept it rather than clearing it.
- **Totals:** 46 Islings: Fire 20, Water 14, Wind 12 (checked at bake and in a test).
- **The book:** an opaque stone slab over the island.
  - Header: "Isling Journal", "Found N / 46".
  - Element tablets with counts, plus Close.
  - A 5 × 4 portrait grid:
    - caught: full colour, "Lv N" of your best one;
    - seen: dimmed, "Seen";
    - unseen: black silhouette, "???", plus where it lives ("Galewick, kite field"), "Rare", or "A starter" (Kilnpot);
    - evolved forms: "Evolves from X" once X is caught, else "???".
  - Poke a portrait (or look + pinch) for its card: element, where it lives or what it evolves from, moves, evolve level.
- **Portraits:** baked at edit time (front 3/4, transparent; rendered on black and on white for clean alpha) into one atlas.
  The grid is one mesh, one draw; the labels are one text per row (4).
- **Opens from:** the game menu (**Journal**), and a stone "Journal" tablet leaning on the lodge pedestal (poke it).
- **Toast:** every new entry (first catch, new evolved form) shows "New Isling! Kilnhearth · 9 / 46" with its portrait for
  2.5 s, with a chime. During an evolution it waits until the show ends. The first time ever, `journal.first`.
- **Menu (your note mid-step):** **Resume · Journal · Settings · speaker** (Dev apart, QA only). Controls was already in
  Settings; **Title** is now a small tablet beside the Settings heading (from the game menu only, not during the finale).

Shots: `14h_menu.png`, `14h_journal_fire.png`, `14h_journal_water.png` (a card open), `14h_toast_7.png`,
`14h_lodge_book.png`, `14h_lodge_journal.png`.

## E. Temples warn; Keepers tuned to your team

- **Warning, not a wall:** below the suggested team (Galewick 4, others 6) the Spirit says "{Keeper} is strong. Most guardians
  bring {n} friends." and **Go in** / **Not yet** tablets show (the explorer waits). Go in enters as usual. No warning in the
  Quick Journey. Shot `14h_temple_warn.png`.
- **The formula** (`KeeperScaling`, tuned by `KeeperTeamSim`, not by hand):
  `Keeper Lv = its shard-scaled level + clamp(round(0.75 × (team − level − offset)) − 1 × (3 − fighters), −6, +3)`
  - team = average level of your 3 strongest; fighters = how many of those 3 slots are filled;
  - offsets: Galeowl −1.5, Hearthstag −1.5, Tidemother −0.5;
  - the Quick Journey keeps its fixed Keeper; shard scaling still applies first.

Sim (600 fights per row; teams = a starter + that realm's route Islings; "Lv 7-10" etc. are relative to the Keeper's level, as
written for Galeowl Lv 8):

| Keeper (normal order) | 1 Isling, k−3 | 4 Islings, k−1..k+2 | 6 Islings, k+1..k+3 | Before (fixed Keeper): 1 / 4 / 6 |
|---|---|---|---|---|
| Galeowl (Lv 8) | 8% | 72% | 100% | 0% / 97% / 100% |
| Hearthstag (Lv 14) | 6% | 66% | 95% | 0% / 88% / 100% |
| Tidemother (Lv 13) | 2% | 68% | 97% | 0% / 78% / 100% |

- **Targets:**
  - 4 and 6 Isling teams land on target.
  - **A lone starter gets 2–8%, short of ~15%:** Keeper levels are whole steps, and the next easier step jumps to ~30% (sweep in
    the log).
  - Tidemother's lone starter barely wins at any level (water shield, heals).
- **Levelling up:** every whole team level always raises your edge over the Keeper (tested); the sim shows win chance rising,
  with ±5% sampling wobble.
  - One honest nuance: a single Isling's level-up moves the average by a third and can coincide with the Keeper's next whole-level
    step. That one step is a hair harder; the next ones pull ahead.

## F. Finale: carrying the Spirit to the crystal

- `spirit.finale.hand`: **"Lift me up to the gold crystal on its forehead."** (chip "To the crystal").
- While the Spirit sits on the palm:
  - it leans toward the crystal;
  - the crystal pulses with a gold ring plus the objective gold;
  - a dotted gold arc runs from the palm to it, the dots drifting up.
- **Ghost hand (new clip `palm_lift`):** retargeted, source = your palm (or where to hold it out, before the Spirit lands),
  target = just under the crystal. It holds the palm out, then lifts it palm up all the way to the crystal.
- Shots: `14h_finale_ghost_1..6.png` (waiting), `14h_finale_ghost_palm_1..6.png` (Spirit on the palm).

## Headset test list (in order)

1. **Menu:** Resume · Journal · Settings · speaker in one short row. Settings has Controls and a small Title tablet.
2. **Journal from the menu:**
   - readable at your seat? Counts right?
   - silhouettes black, seen ones dimmed;
   - poke a portrait: its card; Fire / Water / Wind tablets switch.
3. **Dev > Team > Make lead ready to evolve:**
   - the pod glows gold;
   - the Spirit: "{name} is glowing...", chip Hold it.
4. **Palm moment:** grab the glowing pod, turn your hand palm up.
   - It settles, opens, evolves on your palm: cry, gold name, card.
   - It curls back to its socket.
   - Then the toast "New Isling! ... N / 46".
5. **Fallbacks:** again with look + pinch on the glowing pod (over the island); and holding it palm down for 6 s (over the hand).
6. **A wild battle** with a big evolved Isling in front: can you always see the wild one?
7. **Lodge:** the Journal tablet on the pedestal opens the Journal.
8. **Temple door with fewer than 4 (Galewick) / 6:**
   - the warning, then Go in / Not yet;
   - Go in enters; Not yet stays.
   - The Keeper log line shows its tuned level.
9. **First Keeper calmed (full game, starter not evolved):** after the wave, the Keeper's thanks and the starter's pod glows.
10. **Finale goodbye** (Dev > Story > Finale from the goodbye):
    - the ghost lifts its palm to the crystal;
    - with the Spirit on your palm: the line about the gold crystal, the pulsing crystal and the dotted arc.

## Open items

- **Missing sounds** (in `Audio/PhaseB-Hooks.md`): Journal open / page, the toast chime, a glowing-pod loop. Placeholders:
  `menu.open` / `menu.close`, `lodge.page`, `spark.chime`; no loop yet.
- **Lone-starter Keeper win chance** is 2–8% (see E).
- **Journal labels:** 4 text objects (one per row) beside the 1-draw portrait mesh. One text total would need line-height tuning
  in TMP; not worth it now.
- **Pre-existing:** some words render slanted ("Lilyvole", "Riverloom" in the Journal); not chased.
