# Step 9f: level curve fix, visible XP, Steambara, the explorer, real fire evolutions

Read CLAUDE.md, `.claude-handoff.md` (9e notes: the simulator found the trail reaches the temple at ~Lv 6 but
Hearthstag needs ~Lv 9), `Assets/Docs/ConceptArt/Concepts_03_Summary.md`, sheets `07_fire_rest.png`,
`11_fire_evolutions_rest.png`, `12_explorer_options.png`. Decided Oct 26 (design chat).
Before Play over Link, ask Mugesh to restart Unity.

## A. Level curve (the trail must prepare you for the Keeper)
1. **XP ×1.6** for wild wins, and **catching gives XP** too (same as a win, to the Islings that fought).
2. Re-run the 9e **XP trail check** and the **Keeper simulator**. Targets: the shortest trail arrives at the temple at
   **Lv 8–9**; at that level, a party of 3 beats Hearthstag **60–75%** of the time **for each starter** (with
   Steambara available, see C). Tune XP, wild levels or the Keeper only within those targets. Report the tables.

## B. Make XP visible (Mugesh didn't notice it exists)
3. **Pod card** (rim and lodge): add an **XP bar** under the HP bar, with "Lv 6" and a small "→ 7" at the bar's end.
4. **After a win**, on the pad: a big "+34 XP" pops above each Isling that fought; its XP bar is shown **large**
   (twice today's size) and fills over ~1 s; a **level-up** gets a banner ("Lv 7!") with the gold ring burst and a hop;
   the pods of benched Islings that got XP glow and show "+34" too.
5. First time: `xp.first` → nearPad: "Your Islings learn from every battle. Watch them grow!"

## C. Steambara: a Water visitor at the hot springs
6. Build **Steambara → Onsenbara** from `07_fire_rest.png` (Water type: a capybara relaxing in a steam puff with a
   small fruit on its head; Onsenbara: bigger, a little hot-spring tub ring and a towel on its head). Rigid parts,
   ≤ 6k tris, 3 moves each (one Water attack that can break the Keeper's shield), evolves at Lv 14.
7. Add it to the **hot springs** table (~35%) and rarely to the berry grove (~10%). Spirit line on first meeting:
   `steambara.first` → wildPad: "A water Isling, visiting the springs! Water beats fire..."

## D. The explorer (concept Option A: teal hood)
8. Replace the orange cube with the **Option A** explorer from `12_explorer_options.png`: a gender-neutral kid,
   big head, pointed **teal hood** with a cream lining, teal tunic, brown boots, **brown backpack with a small
   glowing lantern** (lantern glow matches the Spirit's colour). Built from SDF parts, rigid pieces only: head+hood,
   body, two arm nubs, two leg nubs, backpack, lantern (swings on a pivot).
9. Size: about the same footprint as the cube today (~2 cm tall at island scale 1). It must read from above at 45°.
   Outline on. ≤ 3k tris.
10. Motion in code: walk = a little bounce per tile with arm/leg nub swing and a hood bob; run = faster, leaning;
    idle = look around, lantern sway; hop (tap) = squash and jump; cheer after puzzles = arms up; the existing
    "stretch" in the opening and "looks up at the log" keep working. Faces its walking direction.

## E. Rebuild the 5 scaled-up fire evolutions from sheet 11
11. **Forgehorn** (big beetle, anvil on its head, large glowing horn), **Emberdillo** (bigger leaf plates with glowing
    edges, leaf tail), **Cindermole** (miner's-lamp coal on its head, rounded digging mitts), **Cinderbloom** (the cap
    opens into a dark ember flower with glowing cracks, spores swirl), **Pyrecrane** (tall, red cap, flame crest, big
    fan tail of ember feathers). Full recipes (not modifiers on the base), same species data and moves.
12. Turnaround shots to `Logs/BattleShots/9f_<name>.png`.

## Housekeeping
- One compile batch per section; EditMode tests green (curve targets as tests). Commit per section with
  "(headset test pending)". No AI attribution. Don't push. Update `.claude-handoff.md`, GameDesign.md.
- Then **build the QA APK** (Tools > Pocket Isle > Build QA APK) and install it: Mugesh smoke-tests it, then gives it
  to the QA tester before Goa.
- Summary: the new tables (arrival level, win % per starter), what changed, what to test.

## Test
1. Win a wild battle: big "+XP", a large XP bar filling, a level-up banner; the pod card shows Lv and an XP bar.
2. Walk the trail normally: you reach the temple around Lv 8–9.
3. Steambara appears at the hot springs; its Water move breaks Hearthstag's shield.
4. The explorer is the teal-hooded kid with a backpack lantern, bouncing as it walks.
5. Forgehorn, Emberdillo, Cindermole, Cinderbloom and Pyrecrane match sheet 11.
