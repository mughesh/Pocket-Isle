# Step 12a: smart-step walking, teaching it, objective lines vs chatter, the way forward

Decided Oct 27, 9:45 PM (design chat) after Mugesh's playtests with other people: **the swipe walk is the most used
control and the hardest to control.** A swipe walks forever and fast; people overshoot, panic, and never learn the
stop tap. Also: non-gamers don't know what to do after the tutorial, and they can't tell objective lines from chatter.
Read `.claude-handoff.md`, `GestureTestPiece.cs` (AutoWalk / HoldToWalk / run), `ExplorerPath`, `Onboarding.cs`
(lessons, `goldPath`), `QuickJourneyDirector` (gold path to the next goal), `Dialogue*`, 11d's objective rule.

## A. Smart steps (replaces AutoWalk as the default; keep the old modes selectable in code for comparison)
1. **One swipe = a smart step:** the explorer walks in the swipe direction (mapped to the player's view as today) and
   **follows the path's turns** (on a path tile with exactly one way onward besides back, it turns with it; off-path
   it goes straight). It **stops at the first of:** a junction (more than one way onward), the tile before an object
   or piece it can interact with (a hand moment, a prop with a line, a door / gate / building entrance), the edge of a
   tall-grass patch (stops on the first grass tile, so walking into grass is a choice), a blocked tile, or **5 tiles**.
2. **Preview:** the moment a swipe is read, small **gold footprints** appear on the tiles to the stop tile and fade
   as it walks over them; the stop tile gets a soft ring. If the swipe can't move at all, the explorer bumps and turns.
3. **Two quick swipes (≤ 0.45 s apart, same direction) = keep walking:** the first swipe starts a smart step at once
   (no waiting), the second upgrades it to continuous: it follows the path and stops only at junctions, interactables,
   blockers or a **tap**. No more separate run: continuous is a brisk walk.
4. **Speed:** base walk ~25% slower than today; continuous ~ today's walk speed. Keep the bounce/step look matched.
5. A swipe in a new direction while walking = stop and re-plan in that direction. A tap = stop (as today). Look +
   pinch on a tile = A* to it (unchanged). Touch walking stays Off by default (11d).
6. Tests: stop rules on synthetic maps (junction, object, grass edge, 5 cap, blocked, follows an L-turn), double-swipe
   window, re-plan, footprints count.

## B. Teach it properly (onboarding, both slots)
7. Rewrite the walk lessons (keep their ids where possible so old saves resume):
   - `lesson.walk`: "Swipe your thumb along your finger to take a few steps." The gold footprints show; a gold target
     tile 3–4 tiles ahead on the hub path. Done when the explorer stops on it. Ghost = the thumb swipe demo.
   - `lesson.walk.turn`: a second gold tile around a corner: "The path turns. Swipe again!" (shows it follows turns
     and stops at the junction).
   - `lesson.walk.far` (new): "Swipe twice quickly to keep walking. Tap to stop." A long straight stretch with a gold
     tile at the end; done on arrival via continuous walk **or** after 3 smart steps (never blocks). Ghost = two quick
     swipes, then a tap.
   - `lesson.look` (look + pinch a far tile) stays as the alternative.
   Chips: "Swipe", "Swipe again", "Swipe twice". Remove/replace `lesson.stop` (the tap is taught inside walk.far).
8. **How to play card:** the game menu (and title Settings) gets a **Controls** rune: a stone card with 4 small
   illustrated rows (swipe = few steps; swipe twice = keep going, tap = stop; look + pinch = go there; pinch / grab
   things on the island). Static icons (simple drawn hand glyphs), no new animation system.

## C. Objective lines vs chatter (gold objective card)
9. **Objective lines** (lesson lines, task lines like `gw.kites.task`, the pinned chip): the bubble gets a **gold
   border**, a small **target icon** at its left, bold text; it stays pinned as the chip until done (as 11d).
   **Chatter** (story, praise, flavour, first-meet lines): a smaller plain cream bubble, normal weight, fades.
   Mark lines in the script data (`objective: true`) rather than guessing from ids; set it on every lesson, task and
   hand-moment line.
10. **Poke the Spirit** (fingertip) or look + pinch it = **repeat the current objective** (full line) and show the way
    (D). With no objective active, it says a short idle line and shows the way to the next goal.
11. **Lines keep up with the player:** when an objective completes, drop its remaining queued lines (don't read out
    "pump the bellows" after the door is open); on a scene change (interior, realm), flush lines tied to the previous
    place; story lines that matter replay later only if they're objectives.

## D. The way forward (non-gamers are lost after the tutorial)
12. Reuse the Quick Journey **gold path** for the full adventure: a **Spirit's trail** of glowing gold tiles from the
    explorer toward the **next goal** (next unsolved hand moment in this realm, else the Keeper door, else the return
    gate; in the hub: the next unopened realm gate, then the statue with a shard). It shows: after a lesson ends, when
    entering a realm, when the player stands still ~12 s with no objective done, and when the Spirit is poked. It
    fades after reaching the goal or 20 s.
13. **Make the gold read on every ground colour:** brighter, more saturated gold, a raised soft glow (slightly above
    the cap, additive), a travelling pulse along the trail toward the goal, and a thin dark outline so it never blends
    with sand, wheat or the cracked bed. Same look for lesson gold tiles and the footprints.

## Housekeeping
- EditMode tests green; commit per section (A, B, C, D) "(headset test pending)". Update `.claude-handoff.md`,
  GameDesign.md (Walking, Guidance), QA-Guide (what's new + how walking works now).
- Shots `Logs/BattleShots/12a_*` (footprints preview, objective vs chatter bubble side by side, Spirit's trail on
  sand / wheat / grass, Controls card).
