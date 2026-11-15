# Run 34 (Nov 14): a hand moment wakes each Keeper

Branch `step-34` (from master `d9120f92`), not pushed. Tests **516 / 0**; smoke `New game, Cinderwood, Lilymere, Quick Journey` **69 ok / 1 failed**, then after the fix `New game, Cinderwood` **32 ok / 0 failed**; audio Release Check passed. Headset test pending for all of it.

| Part | State | Commit | Notes |
|---|---|---|---|
| A. Gate takes any hand moment | Done | `eff91fd7` | `KeeperHall.wake` (a `HandPuzzle`; was `untangle`, `FormerlySerializedAs` keeps Galeowl's link). Unsolved: no meeting, the Keeper waits. Solved: a `KeeperWakePiece` plays its own waking beat, then the old meeting and battle. Galeowl's untangle untouched (its tests green; Quick Journey smoke green). |
| B. Tidemother: knock on the shell | Done | `a814d098` | `ShellKnockPiece` (`lm.shell`, stage = knocks). Before body `Tidemother_Shell` (a second bake: dome + plastron, flippers small, no tail, no eyes/face at runtime), breathing, bubbles. A fingertip entering the shell or look + pinch = a knock: hollow knock (`log.drop` pitched up each time), wobble, ripple ring, a gold dot of 3. Third knock: splash, the standing turtle swells out, whale call, rear, meeting. Chip "Knock on its shell", poke ghost. |
| C. Hearthstag: kindle the crown | Done | `68f2d98f` | `CrownKindlePiece` (`cw.kindle`). **The kneeling bake worked** (`Hearthstag_Kneel`: legs folded, hooves tucked, body on the floor, head low and forward; no fallback needed); frown, ash colours, dark crystals, a sputtering wisp. Cup it: `CupMeter` / `CupRules` with the village flame's numbers (2 palms ~2 s, one curled hand ~3 s, look + pinch-hold 3 s), wide hold ring (band 0.24), rising `flame.cup` tone, sparks; let go and it shrinks back. Full: chime, the fire roars, the standing stag rises under the flames, bellow, stomp. Fire 1.3x in the fight; calm = a small steady flame (also in a rematch). Chip "Cup the fire", cup ghost. |
| D. Rules | Done | `bdbcf4e9` | Saved per piece; a calmed Keeper never shows its before pose (old saves, rematches); a lost / fled battle doesn't reset it. The moment holds the explorer still (`HeldStill`), blocks tile touch near the body and logs `[HandPuzzle] lock / unlock lm.shell` / `cw.kindle` on every exit. 20 s of nothing: the Spirit repeats the hint. Quick Journey unchanged. |
| D. Dev panel filming jumps | Done | `bdbcf4e9` | **Dev panel > Travel > Filming: "Tidemother: shell", "Hearthstag: kneeling"**. Each resets that Keeper, its moment and its shard in the save, gives a preset team (Lv 14) if under 3, reloads, then walks into the hall. Not tried in Play (needs a save). |
| D. Tests and shots | Done | | `Step34Tests` (14). Seat shots `Logs/BattleShots/s34_<keeper>_before / after / calm.png`; bodies alone `Logs/Step34/*_seat.png`, `*_side.png`. |

## Smoke findings
- The one failure: **"speech bubble covers Hearthstag"** after the fight. The crown-flame mesh had a fixed bounds box 3x the Keeper's height; with the calm flame now lit, the layout check counted that box as the body. Fixed (bounds fit the flames each frame); re-run green.
- The cup ghost at full size hid the whole kneeling stag from the seat (R7). Now 0.55, on the crown only. Not re-shot.
- Two "no progress" notes in the run were there before: the opening's starter beat, and the hall waiting for Kilnpot to evolve.

## Nov 14 pm: first headset notes
- **Isling through the attack buttons (lake temple only):** its party pad was row 7, the other halls row 6. Set in step 11c, before step 24 put the move row over the arena, so the Isling stood through the plates. Now row 6 (scene set by `Step34Setup.Scene`, `Step11cSetup` matches). New smoke check: a body through the move row is a layout failure (`BattleRunes.BodyThroughRow`).
- **Knock:** lower pitch (1.0 to 1.24, was 1.35 to 1.75) with the Keeper's stomp under it pitched up; volume rises per knock (strength 0.55 / 0.78 / 1). The wobble is stronger each knock too.
- **Blue wave:** the lodge's heal shockwave, split out as `HealFx.Ripple` (any colours), in blue from under the shell, wider and brighter each knock.
- **Battle under the title (Mugesh quit mid-battle in the forge hall):** a save from inside a hall loads inside, and the hall started its meeting at once, under the title (the pod fell, the moves worked before Continue). Now the meeting, the walk-up restart and the calm poke all wait while the title or the returning rise is up (`KeeperHall.TitleUp`), then 0.6 s. The wake moments were already safe (the explorer is locked under the title). Kept: you resume where you saved, with the hall behind the title. Not replayed in the Editor (the save was on the headset).
- **Still happening (second try):** the guard read the title flag in the hall's `Start`, which can run before `Opening.Start` sets it (start order is undefined). `Meet` now waits one frame first. Replayed his Editor save (Quick journey, inside ForgeHall): "the meeting waits for Continue", then after Continue the meeting and battle.
- **Riverloom far too big:** the pad fit only compared heights; Riverloom is half Hearthstag's height but 2.4 tiles wide (0.282 vs the stag's 0.273). New width rule in `PadFit`: the near Isling at most 1.6 tiles wide, and at most 0.6 of a Keeper's width. On his save: Riverloom x0.58 "(too wide)", the Keeper unchanged.
- **The Lantern talked over the title** ("Pod on your palm", from a save after a level-up): the title and the returning rise now hold every line, like the game menu does (`Dialogue.Held`); they show after Continue. Checked on his save.
- **Power icon (game menu):** the ring started at 130 degrees instead of 50, so its gap was on the left and the bar crossed solid ring. Now the gap is at the top.
- Checks: tests 519 / 0; smoke `New game, Lilymere` 28 ok / 0 failed (the new through-the-row check did not fire).
- **Ghost hands worse in the build than in Editor Play (Nov 15):** `GhostClipLibrary` sat inside `GhostHandClip.cs`, so Unity made no script asset for it and `GhostClips.asset` saved `m_Script: 0`. The Editor still found the class by name; a build loaded no library, so every ghost used the code-made placeholder clips. Moved to its own `GhostClipLibrary.cs` and linked the asset. New `ScriptLinkTests`: no asset under `_Project` may have an unlinked script. Tests 524 / 0. Needs a build to confirm.
- **Waking water:** a splash all round the shell, a spray column and the biggest wave. When it rears, the flippers slap down a second, smaller splash and wave.

## Cost
- Triangles / draws: ~0. The before body replaces the standing model (the hall shows one at a time). Kneel 14.5k tris / 14 draws = the standing stag's budgets; shell 10.6k / 13 (one part fewer). While cupping: one ring quad; the knock dots are pooled fx orbs.

## Not done / to check
- Nobody has seen any of it move: fingertip knock radius, the cup round a small crown, and whether the kneel reads as **hurt** (not just resting) need the headset.
- The knock uses the log-drop sound pitched up (no new paid audio, as the timebox asked). If it doesn't read as a shell, it needs a recorded or generated knock.
- The finale shows no Keepers, so the calm flame has nowhere to show there.
