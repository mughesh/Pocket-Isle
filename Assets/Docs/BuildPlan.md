# Build plan — Pocket Isle

Revised Oct 22 (v3) for the full game in `GameDesign.md`. Systems first, then one complete realm, then more content.
Prompts for Claude Code: `Assets/Docs/Prompts/step-NN-*.md`. Design: `Assets/Docs/GameDesign.md`.

## Principle
- **Every system data-driven** (ScriptableObjects): species, moves, type chart, maps, temples, dialogue.
  Content after Nov 10 should be data entry, not new code.
- **Vertical slice = Still Isle hub + Cinderwood (fire) fully polished.** Judges play ~10 minutes.
- Placeholder art is fine until the system works on the headset.

## Before Goa (Oct 22–27): every core system running on Quest with placeholder content

| Step | Dates | Done when (on the headset) |
|---|---|---|
| 1. Toon look | done Oct 21 | Island, grass, trees, toon shaders |
| 2. On the headset | done Oct 22 | Passthrough, microgestures move a test cube |
| 2b-1. Walk / run / hold | done Oct 22 | Swipe walks, same swipe runs, tap stops; HoldToWalk mode via thumb offset |
| 2b-2. Island handling | Oct 22–23 | Grab rim to move; two hands to turn and resize; tilts toward head on release; soft shadow on real surface (MRUK) |
| 3. World window | Oct 23–25 | Test map ~4× the island; island shows a scrolling window; tiles rise/sink at edges, clipped at rim; explorer walks the map; **window can zoom in/out around a tile** (battle stage) |
| 4. Creature kit + baker | Oct 24–26 | Editor tool bakes SDF part recipes (ScriptableObject) into meshes; Kilnpot, Pebbloop, Pufflet on the island with idle breathing, eyes, rigid ears/tails |
| 5. Encounter + battle core | Oct 26–27 | Tall grass → zoom in → your creature out → 3 moves via gaze + pinch runes → HP, types, damage → win / flee → zoom out. Species/move/type data assets. **Tagged build `pre-goa`** |

In chat (no code), same days: Cinderwood map layout, fire creature list (names + concepts), move list,
Keeper fight phases, opening script. Concept images via Antigravity in batches.

## Goa (Oct 28 – Nov 2): no work

## After Goa (Nov 3–16)

| Step | Dates | Done when |
|---|---|---|
| 6. Capture + party | Nov 3–4 | Flick pod to capture (HP-based chance); 6 rim pods (pinch for stats, pull to send out); 7th catch flies to Sanctuary; Hearth Shrine heals, saves, swaps |
| 7. Progression + save | Nov 4–5 | XP, levels, evolution at level; save/load; spatial anchor restores island position |
| 8. Opening + hub | Nov 5–6 | Shard drifts in and unfolds; Lantern Spirit; starter choice; Still Isle with statue + 3 Shard Gates; gate transition (base material swap) |
| 9. Cinderwood complete | Nov 6–9 | Full map, fire creatures (min 4), 2 hand moments (log, bellows), temple, Hearthstag fight, restoration, shard into statue |
| **Gate: Nov 9 evening** | | Slice playable start to shard #1? If not, Nov 10–12 go to finishing the slice, not new realms |
| 10. Lilymere + Galewick | Nov 9–12 | Same template: map, min 4 creatures each, hand moments, Keeper, restoration |
| 11. Finale + Sanctuary | Nov 12–13 | Statue wakes into the Heart; final battle; Spirit rejoins; Sanctuary island with counter |
| 12. Polish | Nov 13–14 | Audio (credits list), onboarding hints, softened island art, 72 fps, bug pass |
| 13. Ship | Nov 15–16 | Demo video < 3 min, Devpost write-up, APK to the Competition channel |

## Cut list (first to go first)
1. Golden Berry crafting
2. Creatures beyond 4 per element (10 stays the stretch)
3. Sanctuary island → simple counter + shelf at a Hearth Shrine
4. Table snap (keep the shadow)
5. Spatial-anchor persistence
6. Galewick, then Lilymere: finale wakes on the shards that exist (shard count is data)
7. Smooth scrolling → flip-screen transitions

## Risks to watch
- Zooming + clipping the window must stay cheap: measure draw calls in step 3.
- Creature baker quality vs time: if the kit is slow, bake fewer species with more palette variants.
- Gaze + pinch runes vs microgesture conflicts: pinches on the walking hand during battle.
