# Overnight Nov 6: step 16 (judge-review polish)

Branch `overnight-nov6` (not pushed, not merged). Master was pushed first with your notes and prompts (`Docs: Mugesh's notes`).
**Tests 336/336. Final smoke run 84/84 ok (6.5 min, was 7.0).** No headset tonight: flat Editor Play only.
Saves, XR-on-startup and the time scale are back as they were.

**Keep it:** `git checkout master && git merge overnight-nov6 && git push`
**Throw it away:** `git checkout master && git branch -D overnight-nov6`

Step 15a (release build code: `ReleaseBuild.cs`, `Step15aTests.cs`, icon/keystore path in ProjectSettings) is still
**uncommitted in the working tree**, untouched tonight. It waits for your TEST build + upload.

## Done / partly / not

| Section | Status | What changed |
|---|---|---|
| A ghost hands | done | Fade to 8% when your fingertip or palm is within 16 cm of the target. Finale lift, evolution, Keeper shard and statue ghosts are 0.6x and 10 cm to the side. |
| B jingles | done | Soft mallet + bell, low-pass 2.5 kHz, shorter tails, about -9 dB. Music ducks -4 dB under them (was -8). Numbers below. |
| C Spirit's bubble | done | The Spirit parks **beside** a line's target (your right first, then left), never above it. It and the bubble keep off the ghost's grab and drop spots and both battle creatures. Home moved to the near-**right** corner. Chatter waits while a focal card is up. Typing 60 letters/s; lines under 30 letters show at once. |
| D shard to statue | done | Keeper shard floats 0.2 m up, toward you; a **touch** takes it ("The fire shard! Touch it!"). Statue offer only when the explorer stands next to the statue. The view holds and the explorer is locked while it whispers. 12 cm snap for the socket and the forehead crystal. |
| E first minute | done | No island ambience or music while only the shard floats: new glassy shard hum, plus a shimmer that grows as your hand nears. Swipe ghost side-on, palm in/down (measured, shot below). No "tap to stop" in lessons. **Menu stone (≡)** on the rim, front-left; menu line names it. |
| J battle spectacle | done (Keeper flourish not shot) | Idle bob/sway/glance; the wild hops when yours arrives; wind-up on every move; bigger shots with element trails; lunge hop; slam dust + pad shake; shield pulse; 60 ms hit-stop on every hit; element-coloured damage numbers; HP ghost segment + low-HP pulse; faint spin + flop; winner hops; Keeper flourishes. |
| F small fixes | done | Stuck hand on menu buttons: the poke is released right after a press (my read of the cause: the paused menu stops ISDK's poke from letting go), plus a 1.2 s watchdog. Recenter glides 0.4 s. Tall grass: 16% per step (was 12.5%), 30% in clumps of 2-4 tiles; the grace counts every step. Two-hand lesson passes after 8 s. Galeowl knots: one hand already works (pinch an end, pull 15 cm) or look + pinch. |
| G readability | done | Battle line on a dark rounded pill, cream text, 1.2x. Quick Journey end card at the title's tablet size, in front of the statue. |
| H ending | done | Skip tablet from the Hall of Fame on; faster Hall of Fame; one slate plaque with gold trim over the Heart scrolls the credits (~10 s, was ~36 s); then "Thank you for playing Pocket Isle" with Keep exploring / Title. No Unity/Meta lines; the free-plan ElevenLabs line stays until 15b (flip `CreditsText.PaidSoundsDone`). |
| I looks | mostly | Lodge: round woven rug (terracotta/cream/sage), hearth stone tiles, floor glow at the hearth, bookshelf + fern + cushions, side table + lantern + chest (4 extra draws). Soft glow under the island in every scene. Grab line warm/translucent until hovered. MSAA confirmed 4x, render scale 1. **Not done: plank seams** on the lodge floor (needs a floor shader change). |

## Decisions I made
- **Spirit home** moved to the near-right corner (your "more to the right"); it goes left only when the right is in the objective's way.
- **Jingles** lowered by the full 9 dB. They now sit ~4 dB under the median one-shot. If they feel too quiet, raise `SynthBatch1.JingleVolume` (0.28) to 0.35.
- **"Touch it!"** lines: "The fire/wind/water shard! Touch it!"
- **Statue offer** needs the explorer next to the statue (walk-up rule, 1 tile). The trail already leads there.
- **Credits:** "Made by Mugesh", original designs, the free-plan line, Kenney (CC0), the font licence. "Music: to come" removed.
- **Menu stone** sits left of the Journal's spot, so the two never overlap (layout check: 1.6 cm to the Journal, 11.5 cm to the pods).
- **Slam** winds up half as long (its leap is a wind-up already), so it stays under +25% of the old pace.
- The scene file has an older uncommitted change; **nothing tonight touches the scene**. New tuning is in new fields with code defaults.

## Jingles before / after (clip mid level + cue volume; median of 138 other one-shot cues = -19.9 dB)

| Cue | Before: plays at | >3 kHz share | After: plays at | >3 kHz share | Length |
|---|---|---|---|---|---|
| catch | -14.9 dB (+5.0) | 0.20 | -24.1 dB (-4.1) | 0.01 | 1.41 -> 1.02 s |
| level up | -14.9 (+5.0) | 0.19 | -24.1 (-4.1) | 0.02 | 1.31 -> 0.90 s |
| evolve | -14.9 (+5.0) | 0.19 | -24.1 (-4.1) | 0.03 | 2.16 -> 1.80 s |
| shard | -14.9 (+5.0) | 0.28 | -24.1 (-4.1) | 0.09 | 1.48 -> 1.02 s |
| Keeper win | -14.9 (+5.0) | 0.12 | -24.1 (-4.1) | 0.01 | 2.92 -> 2.50 s |
| victory | -14.9 (+5.0) | 0.13 | -24.1 (-4.1) | 0.01 | 2.08 -> 1.74 s |

The video's "+15-16 dB over the game" includes quiet ambience; against other sound effects they were +5 dB and are now -4 dB.
AudioScope: all six pass.

## Battle pace (seconds per move, the move plus the 0.45 s pause)

| Template | Before | After | Change |
|---|---|---|---|
| shoot | 1.38 | 1.36-1.46 (measured) | up to +6% |
| lunge | 1.00 | 1.15 (measured) | +15% |
| slam | 1.24 | ~1.43 (1.55 measured before halving its wind-up) | +15% |
| shield | 0.95 | 1.16 (measured) | +22% |

## Shots (`Logs/BattleShots/`, not in git)
- `16_swipe_front.png` / `16_swipe_side.png` (before) and `16_swipe_after.png`: the swipe ghost now shows the back of the hand. Measured palm-to-head -0.31 (was +0.09; positive = palm toward you).
- `16_menu_stone.png`: the ≡ stone front-left of the rim.
- `16_battle_shoot_sheet.png`, `16_battle_slam_sheet.png`, `16_battle_shield_sheet.png` (+ single frames `16_battle_<move>_<n>.png`): wind-up, shot, impact and gold flash; slam dust and ring; shield bubble.
- `16_lodge.png` / `16_lodge_crop.png`: the dressed lodge, the hearth glow, the rug; under-glow beneath the island.
- **Not shot:** Keeper flourishes (the Forge Hall trip didn't start from tonight's save in flat Play). Check them on the headset.

## Smoke run (final branch state)
84/84 ok, 0 failed, 6.5 min. Notes, none a bug:
- "No progress for 20 s" in the opening (the starter wait, scripted).
- Galeowl's HP label inside its box. This is the same note as Tidemother on Nov 5: a huge Keeper's box covers the label at its feet, and the label draws on top. Check it on the headset.
- The finale's "no progress" note from Nov 5 is gone: the ending no longer has a 20 s still stretch.

## Headset test list (first minute first)
1. **New journey:** only the shard's soft glassy hum, no island sounds. Move a hand toward it: a brighter shimmer. Island sounds start as it unfolds.
2. **Swipe lesson:** the ghost shows the back of the hand, side-on, not a palm facing you. The line says "Swipe your thumb along your finger to walk."; no "tap to stop".
3. **Ghosts:** move your hand toward a pod or the target: the ghost fades almost away, and comes back when you pull away.
4. **Spirit:** during the pod pick/toss and the windmill/kites, the Spirit and its bubble sit beside the target (right side), not over your Isling or the drop spot.
5. **Two-hand lesson:** do nothing: it moves on after 8 s.
6. **Menu stone:** poke the ≡ stone on the rim (front-left): the menu opens. Poke Settings and other buttons: your hand never sticks.
7. **Battle:** both Islings bob and glance. Moves wind up; shots are bigger. Hits pause a moment with coloured numbers; HP drains with a pale trail. A faint spins. The battle line sits on a dark pill. Pace feels OK?
8. **Jingles:** catch, level up, evolve: softer, not piercing. Too quiet?
9. **Tall grass:** small clumps (2-3 tiles) now meet Islings sometimes.
10. **Meta recenter:** the island glides to its new place instead of blinking.
11. **Keeper win:** the shard floats high (not behind the arch); touch it. Keeper flourishes before its moves (crown flare / wing gust / wave).
12. **Statue:** the offer starts only when you stand next to the statue; the view holds; you can't walk off while it whispers; dropping the shard near the socket snaps it in.
13. **Quick Journey end card:** readable tablets in front of the statue.
14. **Ending (See the ending):** Skip appears at the Hall of Fame; the credits on one plaque; then "Thank you for playing" with Title.
15. **Lodge:** rug, hearth stones and glow, shelf + fern, table + lantern + chest; Islings still wander the middle. Grab line warm at rest, bright when hovered. Soft glow under the island.

## Tools
- `Tools > Pocket Isle > Lodge > Dress the Lodge (step 16)` re-bakes the lodge dressing.
- Bridge: `Step16Debug` (Ghost, Shot, Frames, Palm, Spirit); `Diorama.Audio.EditorTools.Step16Audio` (MeasureBefore/After, Bake, BakeOpening).
