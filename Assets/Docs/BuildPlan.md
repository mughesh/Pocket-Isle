# Build plan — Pocket Isle

Revised Oct 22 (v3) for the full game in `GameDesign.md`. Systems first, then one complete realm, then more content.
Prompts for Claude Code: `Assets/Docs/Prompts/step-NN-*.md`. Design: `Assets/Docs/GameDesign.md`.

## Status (Oct 25, Claude Code)
- Steps 1–7c: passed on the headset. Step 7c was the first release device build: 95 MB, **stable 73 fps**
  (`pre-goa` moved to the Oct 25 build-settings commit).
- Ahead of plan: step 7 (XP / levels / evolution, Hearth Shrine, save / load + pause), step 7b (scene swap, 2 x 2
  shrine, **Hearth Lodge instead of the Sanctuary island**), the **Islings rename** (done), step 7c (lodge door,
  gold focus outline + Spirit lantern beam, first-visit hint chain, 8 Islings walk the lodge, the rest sleep in pods).
- The island always stays level: **tilt toward the head on release was dropped**. Spatial anchors not done (cut #5).
- **Step 8 built Oct 25 (11 days ahead of plan), headset test pending:** Still Isle hub with the statue, Shard Gates
  (fire open to Cinderwood, water and wind sealed), map-to-map travel, the opening (shard, unfold, Spirit, starter
  pods), scripted first encounter. Flat Editor smoke test passed (new game to catch to Cinderwood and back).
- **Step 8b built Oct 25 (headset test pending):** one lesson at a time (Onboarding_Main), ghost hands show each
  gesture (placeholders until Mugesh records the ten clips), no wild battles until the tutorial grass, hint lines
  expire on battle / scene swap. The ghost-hand player is ready for step 9's hand puzzles.
- Steps 8c-8e (first-minutes polish, grab / touch fixes, hands and modes) built Oct 25-26; 8e tested over Link.
- **Step 9a built Oct 26 (headset test pending): Cinderwood, the land.** 60 x 60 (the size for every realm), one
  winding trail through five beats (arrival gate, forest, kiln village + shrine, hot springs, fallen log, cliff forge
  temple) with four side pockets (ember field, lantern glade, berry grove, hidden kiln ruins), per-zone encounter
  tables, 11 new props + the tree fix, first-visit zone lines, automated layout checks, a Cinderwood perf tour.
  Device capture owed (village and springs).
- **Step 9b built Oct 26 (headset test pending): guardian-hand puzzles.** Reusable kit (Carry, Pump, Wheel stub; saved
  state; self-teaching ghost lessons), the fallen log (lift it onto a ledge), the forge (bellows → pipe → 3 braziers →
  the door opens), the Forge Hall placeholder interior. After the first Link test: bigger log grab, the bellows are poked.
- **Step 9c built Oct 26 (headset test pending): Hearthstag and the first shard.** Keeper model + battle rules (crown
  shield, phase 2), the Forge Hall meeting and fight, victory and calming, the fire shard into a rim holder, the
  restoration wave, the temple return gate, the shard into the hub statue.
- **Step 9d built Oct 26 (headset test pending): the rest of the fire roster.** Steamswan, Coalbeetle/Forgehorn,
  Mapadillo/Emberdillo, Ashmole/Cindermole, Cindercap/Cinderbloom, Embercrane/Pyrecrane in the Cinderwood tables;
  Glazegolem and Kilngolem look rare (gold kintsugi).
- **Step 9e built Oct 26 (headset test pending): dev panel, Hearthstag's fury, Keeper balance, small fixes.** Dev / QA
  panel on a GameState API, QA APK + QA guide; Hearthstag frightened fury and burn-off calm; Keeper Lv 10 (headless
  battle sim: Lv 9 team of 3 wins ~69%); Spirit and bubble stay out of the island; gaze ring shows through trees;
  pinch-grab ghosts land the pinch point on the target.
- Not in 9e: title screen, main menu, settings, Quick Journey (own design session).
- Next: headset test of 9b-9e, then the next realm.

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
| 2b-2. Island handling | Oct 22–23 | Grab rim to move; two hands to turn and resize; stays level (no tilt); soft shadow on real surface (MRUK) |
| 3. World window | Oct 23–25 | Test map ~4× the island; island shows a scrolling window; tiles rise/sink at edges, clipped at rim; explorer walks the map; **window can zoom in/out around a tile** (battle stage) |
| 4. Creature kit + baker | Oct 24–26 | Editor tool bakes SDF part recipes (ScriptableObject) into meshes; Kilnpot, Pebbloop, Pufflet on the island with idle breathing, eyes, rigid ears/tails |
| 5. Encounter + battle core | Oct 26–27 | Tall grass → zoom in → your creature out → 3 moves via gaze + pinch runes → HP, types, damage → win / flee → zoom out. Species/move/type data assets. **Tagged build `pre-goa`** |

In chat (no code), same days: Cinderwood map layout, fire creature list (names + concepts), move list,
Keeper fight phases, opening script. Concept images via Antigravity in batches.

## Goa (Oct 28 – Nov 2): no work

## After Goa (Nov 3–16)

| Step | Dates | Done when |
|---|---|---|
| 6. Capture + party | Nov 3–4 | Flick pod to capture (HP-based chance); 6 rim pods (look + pinch for stats, throw a pod to send out); 7th catch goes to the Hearth Lodge; Hearth Shrine heals, saves, swaps |
| 7. Progression + save | Nov 4–5 | XP, levels, evolution at level; save/load; spatial anchor restores island position |
| 8. Opening + hub | Nov 5–6 | Shard drifts in and unfolds; Lantern Spirit; starter choice; Still Isle with statue + 3 Shard Gates; gate transition (base material swap) |
| 9. Cinderwood complete | Nov 6–9 | Full map, fire creatures (min 4), 2 hand moments (log, bellows), temple, Hearthstag fight, restoration, shard into statue |
| **Gate: Nov 9 evening** | | Slice playable start to shard #1? If not, Nov 10–12 go to finishing the slice, not new realms |
| 10. Lilymere + Galewick | Nov 9–12 | Same template: map, min 4 creatures each, hand moments, Keeper, restoration |
| 11. Finale | Nov 12–13 | Statue wakes into the Heart; final battle; Spirit rejoins; caught counter in the Hearth Lodge |
| 12. Polish | Nov 13–14 | Audio (credits list), onboarding hints, softened island art, 72 fps, bug pass |
| 13. Ship | Nov 15–16 | Demo video < 3 min, Devpost write-up, APK to the Competition channel |

## Cut list (first to go first)
1. Golden Berry crafting
2. Creatures beyond 4 per element (10 stays the stretch)
3. Sanctuary island → **done this way (step 7b): the Hearth Lodge interior behind each shrine** (scene swap built
   there, reused for step 8 gates and the step 9 temple arena)
4. Table snap (keep the shadow)
5. Spatial-anchor persistence
6. Galewick, then Lilymere: finale wakes on the shards that exist (shard count is data)
7. Smooth scrolling → flip-screen transitions

## Risks to watch
- Zooming + clipping the window must stay cheap: measure draw calls in step 3.
- Creature baker quality vs time: if the kit is slow, bake fewer species with more palette variants.
- Gaze + pinch runes vs microgesture conflicts: pinches on the walking hand during battle.
