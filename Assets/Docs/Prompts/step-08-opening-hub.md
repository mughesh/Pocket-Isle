# Step 8: opening, starter choice, Still Isle hub, Shard Gates

Read CLAUDE.md, BuildPlan.md, GameDesign.md ("Opening", "World structure", "Player and world") and Script_v1.md.
Step 7c passed on the device on Oct 25: build 95 MB, **stable 73 fps**.

## 0. Housekeeping first
1. Review the uncommitted files from the build (`.gitignore`, `PolishBacklog.md`, `Mobile_RPAsset.asset`,
   `DefaultVolumeProfile.asset`, `OculusRuntimeSettings.asset`). Commit the deliberate ones with a clear message
   ("Build settings from the 7c device check"); list anything you're unsure of and leave it.
2. **Move the `pre-goa` tag** to that commit (Mugesh approved: 7c tested on device). Don't push.

## A. Still Isle hub map (~30×30)
3. `StillIsle` WorldMap (generator + hand edits, like the test map). Layout:
   | Where | What |
   |---|---|
   | Front centre | **Start tile** (explorer wakes here) |
   | Near start | **Hearth Shrine** (with its Lodge) |
   | Centre | **Statue plaza**: the statue on a round stone plaza, 3 empty sockets facing the start |
   | Left edge | **Fire gate** → Cinderwood |
   | Back edge | **Water gate** → Lilymere (sealed for now) |
   | Right edge | **Wind gate** → Galewick (sealed for now) |
   | One side, close to start | **First tall-grass patch** (reachable in ~10 s of walking) |
   | Other side | A small **pond** for the fishing tutorial |
   Paths link everything; mossy boulders, stone lanterns and a few calm trees fill the gaps. Each gate sits at the
   end of a short path stub (a dead end), so walking into it is always on purpose.
4. **Still Isle theme** (RealmTheme): calm and neutral: soft moss green grass, pale warm stone, cream paths,
   quiet blue water, teal-green tree leaves, soft gold accents. No embers. Base rock: grey stone with moss.
5. New SDF props:
   - **Statue**: a seated, mossy stone creature (big, about 3×3 tiles). Cracks with a faint gold glow inside.
     Three round empty sockets on its chest (red / blue / white rims, dim). Must look ancient and kind, not
     scary, and nothing like any known character.
   - **Shard Gates**: Fire = an ember-stone arch; Water = a round stone well with a lily rim; Wind = a short stone
     tunnel with a little windmill on top. 2 tiles wide each. A shimmering portal surface inside each (simple
     shader). Sealed gates: portal dark, a soft stone seal over it.
   - Hub filler: mossy boulders, stone lantern (reuse with the new palette), calm tree (reuse the tree recipe with
     rounder canopies and the hub palette), stepping stones.
6. **Hub encounters:** there are no Neutral species yet, so the hub grass uses a small "visitors" table: Toastling,
   Waxle, Kettlewing at Lv 2–4. The **first encounter is scripted: Toastling Lv 3** (easy to beat and catch).
   The pond table: Pebbloop-like water visitors aren't wild (starters), so use Kettlewing for now.

## B. Shard Gates (reuse the 7b scene swap)
7. **Enter a gate** by walking into its portal tile, or look + pinch the gate (the explorer walks there, then
   enters). Transition = `SceneSwap` with the realm's base material (Cinderwood: warm lava-rock tint; hub: mossy
   grey). The scene swap must now support **map-to-map travel**, not only interiors (the destination becomes the
   new "outdoor" map that the save and the lodge return to).
8. Cinderwood (today's test map, renamed `Cinderwood` for now) gets a **return gate** at its start; you arrive
   next to it. Walking into it goes back to the hub's Fire gate.
9. **Sealed gates:** walking in or pinching = a soft bump and the Spirit says `gate.sealed`. Unseal comes in step 10.
10. First entry into Cinderwood: `realm.cinderwood.first` (anchored to the land ahead).
11. Save: current map, tile, facing; the Lodge returns to the map you came from; losing everything returns to the
    last shrine **on its own map**.

## C. The opening (new game only)
Goal: launch to walking in **under 60 s**, first battle within **~3 min**. No menus.
12. **Shard arrives:** passthrough room, nothing else. A small floating rock shard (mossy, gold light in its cracks,
    about 12 cm) drifts in from about 1.5 m away and settles where the island would spawn (waist height, in front),
    turning slowly and glowing.
13. **First touch:** the shard waits for your hand: poke it, pinch it or hold your palm near it (any of them).
    **Auto after 6 s** if nothing happens. It cracks open with light → the rocky base grows out of it → the Still
    Isle's tiles rise (reuse the rise animation, centre outward) → the explorer sits up and stretches.
14. **Spirit intro:** a spark leaves the statue (subtle hint) and becomes the Spirit by your hand. `intro.arrive`.
15. **Move the island:** `intro.world` → gold focus on the rim. Waits for a grab, or moves on after 12 s.
16. **Starter choice:** three glass pods rise from the statue plaza and float in a row above the island, in front
    of you, each with a tiny turning starter inside (Kilnpot, Pebbloop, Pufflet). `intro.starter`.
    - Look at one: it grows (like rim pods), the starter inside waves, and the Spirit says that starter's line.
    - **Choose = grab it and drop it on the rim** (it snaps into socket 1), or look + pinch it (it flies there).
      No confirm dialog; picking doesn't lock anything.
    - The other two pods sink back into the statue (they could be found again later; not in this step).
17. **Walk:** `intro.explorer` (rewrite the line, see D) → gold focus on a tile path toward the first grass.
18. First grass → scripted Toastling Lv 3 → the existing battle and catch tutorials take over.
19. `intro.statue` fires the first time the explorer is within ~3 tiles of the statue.
20. **Returning player** (save exists): skip the opening; the island rises where it was placed (map, tile and
    facing from the save) in about 2 s.
21. Debug: **Tools > Pocket Isle > New Game** (deletes the save, plays the opening in Editor Play), and an Editor
    key to skip the opening.

## D. Script updates (Script_v1.md, reimport)
22. Replace `intro.explorer`:
    - "See the little explorer? Swipe your thumb along your finger to walk."
    - "Tap your thumb to stop. Or just look at a spot and pinch."
23. Add:
    - `intro.shard` · once · → shard: "Oh! Something's here. Touch it?" (only if no touch after 3 s)
    - `gate.sealed` · → that gate: "This one's still asleep. Maybe later."
    - `gate.fire.first` · once · → fire gate (first time it enters the window): "That arch is warm. It leads somewhere."
    - `realm.cinderwood.first` · once: "Cinderwood. The fire here has gone quiet." / "Let's find out why."
    - `hub.return.first` · once (first time back in the hub from a realm): "Home again. The statue looks... different?"
      (The statue doesn't change yet; that's step 11. Keep it vague.)
    Islings wording everywhere.

## Housekeeping
- One compile batch per section; EditMode tests green (add: gate travel save/restore, lodge return map, new game
  vs returning player). Flat Editor smoke test: New Game → full opening → pick a starter → walk → first battle →
  catch → Fire gate → Cinderwood → back.
- Commit per section with "(headset test pending)". No AI attribution lines. Don't push. Update
  `.claude-handoff.md`, GameDesign.md (hub layout, gates, opening as built) and BuildPlan.md status.
- **Device build at the end** and a quick perf capture of: the opening unfold, the hub with the statue in view, a
  gate transition. Report the numbers (target 72 fps).
- Summary for Mugesh at the end: what changed, what to click, what to test.

## Test on the headset
1. Tools > Pocket Isle > New Game, build, launch: a glowing shard drifts in; touching it (or waiting) unfolds the
   Still Isle around the waking explorer.
2. The Spirit sparks out of the statue and says hello; moving the island is prompted, not forced.
3. Three starter pods float up; looking at one makes it wave; dropping it on the rim picks it.
4. Walk to the first grass: Toastling, battle, catch, all with the existing tutorials.
5. Walk into the Fire gate: Cinderwood rises. Its return gate takes you home.
6. Water and Wind gates are sealed with a Spirit line.
7. Quit and relaunch: no opening; you're where you left off.
8. Still 72 fps during the unfold and the gate swap.
