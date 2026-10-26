# Step 9g: title screen, game menu, settings, Quick Journey

Read CLAUDE.md, `.claude-handoff.md` (step 8 opening, 9e dev panel + `GameState`), `Opening.cs`, `GameSave.cs`,
`BattleRunes.cs`, `DevPanel.cs`, `Assets/Docs/ConceptArt/17_logo_options.png` (**Option 1**). Decided Oct 26 (design chat).
Before Play over Link, ask Mugesh to restart Unity. Goal: done and tested **Oct 27**; Mugesh leaves Oct 28, so the
project must end this step in a working state (if a part slips, stop at a clean commit and say so).

Names: *shard* = the mossy rock you poke at a new game · *logo* = "Pocket Isle" letters on a mini floating island ·
*runes* = the battle's stone tablets (poke or look + pinch, gold = focus). **No new interaction types: every button
in this step is a rune**, built on `BattleRunes` (factor out a shared `RuneButton` / rune row if needed).

## A. Title (new and returning launch)
1. **Logo** (Option 1 of sheet 17): chunky rounded 3D letters "POCKET" (smaller) over "ISLE" (big) standing on a mini
   floating island (grass cap, rocky underside, one tiny tree, optional tiny Lantern Spirit glow). Toon + outline, no
   textures, baked at edit time like props (font glyphs → SDF → marching cubes, or another edit-time route you pick;
   any font used must be OFL and only at edit time). About 30 cm wide at island scale 1, ≤ 8k tris, ≤ 6 draw calls.
   Gentle bob and slow sway; yaw faces the head. Sits above the island's far half, ~25 cm over the top surface, inside
   the central 70°.
2. **Save slots:** `save.json` = Full adventure, `quick.json` = Quick Journey (same `SaveData`). PlayerPrefs keeps
   the **last played slot**. `GameSave` loads/saves the active slot. Any slot existing = returning player.
3. **New player (no slot):** the opening as today: shard drifts in → touch → island unfolds, explorer stretches. Then,
   **before the Spirit appears**: the logo rises out of the statue area to its spot (≈1 s, little sparkle), and two
   runes rise above the near rim (battle rune spot): **Full adventure** and **Quick journey** (sub-label "about 10
   min"), plus a small **Settings** rune. Nothing is focused gold until the player looks at one (both are valid).
   Picking one: the logo floats up and fades (≈1.2 s), the slot is chosen, then the Spirit + starter flow continues.
4. **Returning player:** no shard. The island rises from the middle (existing `returnRiseSeconds` rise) with the logo
   already rising above it. Runes: **Continue** (gold, sub-label "Adventure" or "Quick journey" = last slot),
   **New journey**, **Settings**. Gameplay (walking, encounters, idle hints) is held until Continue.
   - New journey → runes: Full adventure / Quick journey / Back. If that slot already has a save, its rune needs a
     **hold to confirm** (look or poke and keep for 2 s; a ring fills; Spirit-free text "Hold to start over").
     Then delete that slot and play the opening from the shard (same scene reload `GameState` uses).
   - If both slots exist, Continue resumes the last played one; a small second line "or: <other slot>" rune is fine
     but optional.
5. Cold start: logo and runes are built/warmed during the existing `Opening.WarmUp`; no hitch over 50 ms. The title
   costs ≤ 2 ms per frame on device.

## B. Game menu (mid-game) and Settings
6. **Open:** the left-hand menu gesture (palm up, pinch: `OVRInput.GetDown(Button.Start, Controller.LHand)`), and
   **Esc** in the Editor. Today that gesture opens the dev panel: in all builds it now opens the **game menu**; in
   Editor/QA builds (`POCKET_DEV`) the game menu has a **Dev** rune that opens the dev panel, and **F2** still opens
   the dev panel directly. Not available during the opening before the starter is picked (Settings is on the title
   anyway), during a scene swap, or while the island is grabbed (queue it until release).
7. **Menu:** `GamePause` pauses gameplay (walking, battle tweens, encounters, Spirit idle, puzzles); the island dims
   like the battle space; runes over the near rim, facing the head: **Resume**, **Settings**, **Title**, (**Dev**).
   Resume or the same gesture closes it. **Title** saves, then shows the returning-launch view (island sinks and
   rises with the logo, or a scene reload into a title state, whichever is cleaner).
8. **Settings** (one stone board above the near rim, opened from title or menu; Back closes it). Rows, all runes or
   rune-pebbles:
   | Row | Control | Notes |
   |---|---|---|
   | Music, Effects, Ambience | 5 pebbles each (0–4 lit; poke or look + pinch a pebble) | Saved in PlayerPrefs under the bus names the audio lane uses (`Music`, `SFX`, `Ambience`; see `Assets/Docs/Audio/PhaseB-Hooks.md` if present, else a `AudioSettings` static with those keys). No sound hooks yet: Nov 4 wires them to `AudioService`. |
   | Dominant hand | Auto / Left / Right | Auto = today's rule (first shard touch). Overrides `DominantHand`. |
   | Spirit hints | On / Fewer | Fewer = no idle hints, lessons still run. |
   | Recenter island | one rune | Moves the island in front of the head at the default distance/height (existing placer). |
   | Credits | one rune | A scrolling stone card: Pocket Isle by Mugesh; Meta XR SDK; fonts (OFL); audio credits read from `AudioCredits.md` if it exists. |
9. Dialogue: `menu.first` · once → nearRim: "Palm up and pinch with your left hand any time to pause." Plays after
   the first walk lesson completes (also add it to the lessons' end if that's simpler), not during battle.

## C. Quick Journey (the judges' ~10 min route)
Same game, tuned dials, in its own slot. Keep the full opening, starter pick and lessons (they are the best hand
moments). Put every dial in a `QuickJourneyConfig` asset so Mugesh can tune without code.
10. **Lessons:** keep every lesson that teaches a hand verb (shard touch, move island, starter, walk, touch/look to
    walk, grass + first battle, capture). Cut repeats and anything not on the route (list what you cut in the handoff).
11. **The gift** (after the capture lesson, in the hub): `quick.gift` → nearSpirit:
    "The road to Cinderwood is long. Take two friends with you!" / "Bring back the fire shard!"
    Two pods fly to the rim: **Steambara** and **Toastling**, both Lv 8 (config); the starter and any caught Isling are
    raised to Lv 8. The Cinderwood gate gets the gold focus outline and the Spirit points at it.
12. **Dials in Cinderwood** (config defaults): encounter rate ×0.5, XP ×2 on top of the normal curve, no rare-only
    detours, the trail's gold path hint on. Re-run the 9e/9f **Keeper simulator** for the quick team: target **75–85%
    win** for each starter at the arrival level; tune the config (not the Full adventure numbers). Report the table.
13. **End:** when the fire shard locks into the statue: `quick.end` → nearSpirit: "You woke the first piece of the
    world!" / "Thank you for playing the Quick Journey." Then two runes: **Keep exploring** (free play continues in
    this slot) and **Title**.
14. **Timing check:** add a Quick Journey timer to the dev readout (opening start → shard in statue) and a
    `Tools > Pocket Isle > Quick Journey Dry Run` (or sim) that estimates the route time from tile counts, walk speed,
    encounter rate and average battle length. Target **≤ 12 min**. Report the estimate per segment (opening + lessons,
    gift → gate, trail, log, forge, Keeper, statue).
15. Dev panel: Save tab gets **Quick Journey** presets (start, at the gift, at the gate, at the temple) built on
    `GameState` (`SaveData` + reload into the quick slot).

## Housekeeping
- One compile batch per section; EditMode tests green (slots, last-slot Continue, hold-to-confirm, settings persist,
  quick config applied only in the quick slot). Commit per section with "(headset test pending)". No AI attribution.
  Don't push. Update `.claude-handoff.md`, `GameDesign.md` (Title, Menu, Settings, Quick Journey as built),
  `QA-Guide.md` + `QA-TestPlan.md` (a Quick Journey run and a menu/settings pass).
- Then **build the QA APK** and install it. Summary: what changed, the sim table, the route time estimate, what to test.

## Test
1. Fresh install: shard → island → logo + Full / Quick runes → pick Quick → Spirit, starter, lessons as before.
2. After the capture lesson: two friends arrive at Lv 8, the gate glows; Cinderwood to the Keeper and the shard in the
   statue in about 10–12 min; the end card shows.
3. Relaunch: island rises with the logo, Continue says "Quick journey"; New journey → Full adventure plays the shard
   opening; relaunch again: Continue says "Adventure".
4. Mid-game, left palm up + pinch: the menu pauses everything; Settings rows work and persist after relaunch;
   Recenter moves the island in front of you; Title goes back to the logo; QA build: Dev opens the dev panel.
5. 72 fps on the title and with the menu open.
