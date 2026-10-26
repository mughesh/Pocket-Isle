# Step 9e: Dev/QA panel, Hearthstag's fury, Keeper balance, small fixes

Read CLAUDE.md, `.claude-handoff.md`, `Assets/Docs/Notes.md` (Mugesh's latest notes, Oct 26), step-09c and 9d prompts.
Decided Oct 26 (design chat). **Not in this step:** the title screen, main menu, settings and Quick Journey (they get
their own design session); but build the dev panel on a state API that Quick Journey will reuse.
Housekeeping first: delete the untracked `_lanes/` folder (approved). Ask Mugesh to restart Unity before Play over Link.

## A. Dev / QA panel
Mugesh tests a lot by replaying (walking back to spots, recatching, redoing the tutorial), and a QA tester friend
will test builds soon. Build a proper in-headset dev panel.
1. **Builds:** a scripting define `POCKET_DEV`. Menu: **Tools > Pocket Isle > Build QA APK** (dev panel on) and
   **Build Release APK** (dev panel compiled out). Editor always has it.
2. **Open:** the left hand's **menu gesture** (palm-up pinch, Meta's app "menu" button for hand tracking; read it the
   ISDK/OVR way confirmed in the v207 source). Editor: F2. Opens/closes a floating panel beside the island (left
   side, facing the player, inside the central ~70° FOV). Buttons are poke **and** look + pinch. A plain, readable 2D
   panel is fine (it's a tool).
3. **Built on a `GameState` API** (presets, teleport, story flags, party setup) that Quick Journey will reuse later.
4. Tabs:
   | Tab | Controls |
   |---|---|
   | **Go to** | Landmarks per map: hub (start, shrine, statue, first grass, each gate), Cinderwood (arrival, village, shrine, springs, ruins, log, forge door, Forge Hall). Teleports the explorer (scene swap if another map) |
   | **Party** | Heal all · lead level −1/+1/+5 · set all to Lv 5/10/15 · add any species at a chosen level · preset "Lv 11 team of 3" · clear storage · toggles: one-hit KO (yours), invincible |
   | **Encounter** | Force an encounter now (species + level) · wild encounters on/off · rare chance ×10 |
   | **Story** | Skip / reset tutorial · replay opening · Cinderwood restored on/off · log, bellows, door solved/unsolved · Keeper calmed/reset · give/remove fire shard · shard in statue on/off · reset all story |
   | **Save** | 3 named snapshot slots (save / load) · new game · show save path |
   | **Tools** | FPS overlay · debug readouts · time scale 1/2/4× · walk speed ×2 · **Report bug** |
5. **Report bug:** writes a folder `persistentDataPath/bugreports/<timestamp>/` with a screenshot (if possible), the
   save JSON, the last ~300 log lines and a state summary (map, tile, party, story flags, fps); shows the folder name.
6. `Assets/Docs/QA-Guide.md` for the QA friend: install the QA APK (one adb line), open the panel, what each tab
   does, how to report a bug and how Mugesh pulls the reports (one adb line).

## B. Hearthstag: frightened fury, then calm (Mugesh: "it looks like a cute deer")
Still cozy, never gory, but it must read as **overwhelmed and dangerous** before, and **at peace** after.
7. **Frightened:** a dark smoky aura around the body (soft billboards, grey-violet smoke with ember sparks), crown
   crystals flaring with **flame tongues**, constant **trembling**, eyes glowing **red-ember** (eye shader emission),
   **snorts of smoke** every few seconds, and a **stomp** now and then (front leg lifts, slams): glowing lava-crack
   lines spread on the pad, dust, a small island bounce.
8. **Battle intro:** it rears up and stomps; the Spirit flinches back. **Phase 2 (< 50%)**: aura thicker, flames
   bigger, trembling stronger.
9. **Calm transition (on victory):** the aura burns off into rising **gold motes**, flames shrink to a soft glow, eyes
   blink and soften to warm amber, trembling stops, it lowers its head; then the palette shift (already built).
10. Perf: use the pooled particle system and billboards; check fps in the Forge Hall during the fight (flat Play
    numbers plus one Link check).

## C. Keeper balance (Mugesh couldn't win with low-level Islings)
11. Target: **beatable at a normal pace**. Hearthstag **Lv 10**, HP ≈ 2.2× a normal Lv 10 (keep the shield rules).
12. Write a headless **battle simulator** (EditMode): 1,000 fights of a **Lv 9 party of 3** (starter + two fire
    commons, simple "best move" AI with switching when low) vs Hearthstag. Tune until the party wins **~60–75%**.
    Also estimate how many wild battles the normal trail gives before the temple and the party's level when
    arriving (XP curve check). Report both tables in the summary.

## D. Small fixes from the notes
13. **Spirit and bubble never inside the island:** clamp the Spirit's position outside the island's bounds (sides,
    and above the cap by a margin); the bubble's placement checks against the island bounds too.
14. **Gaze tile circle hidden by trees and grass:** draw the tile highlight **on top** (no depth test, soft alpha,
    plus a short thin light pillar on the tile) so it's always visible.
15. **Ghost grab/pinch positions:** each ghost's key point lands on a real anchor of its target (the rim edge for
    island grabs, the pod centre for pod pinches, the handle for the bellows, the log's middle). Check each lesson.
16. In `Notes.md`, mark each handled note "→ done in 9e".
17. **List which evolutions were made by scaling up a base form** because there was no concept art (Mugesh will get
    art made).

## Housekeeping
- One compile batch per section; EditMode tests green. Commit per section with "(headset test pending)".
  No AI attribution. Don't push. Update `.claude-handoff.md`, GameDesign.md.
- Build a **QA APK** at the end and install it (Mugesh tests the panel on the device).
- Summary: what changed, how to open the panel, the balance tables, the scaled-up evolutions list.

## Test
1. Left-hand menu pinch opens the dev panel; every tab works with poke and look + pinch.
2. Go to the Forge Hall, set a Lv 9 party of 3: the fight is winnable; Hearthstag looks overwhelmed and dangerous,
   then calms with gold motes.
3. Report bug writes a folder; the QA guide's adb line pulls it.
4. The Spirit never sinks into the island; the gaze tile circle shows through trees; ghosts point at the right spots.
