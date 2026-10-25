# Step 9b: guardian-hand puzzles: lift the log, pump the bellows, open the forge

Read CLAUDE.md, `.claude-handoff.md`, GameDesign.md ("Realms and story", "Cinderwood"), step-09a-cinderwood-land.md,
Script_v1.md. 9a passed (Oct 26). Decided Oct 26 (design chat): the log is **grabbed, lifted and set aside**; the
bellows are **pumped by pinching the handle and pushing it down**. Before Play over Link: ask Mugesh to restart
Unity (the XR limit crash).

## A. Hand puzzle kit (reusable; Lilymere and Galewick use it later)
Build generic, data-driven pieces (island-local, so they work at any island scale and while the map scrolls):
1. **Carry**: a world object you grab (pinch or palm, generous volume) and place on one of N **target spots**.
   Settings: weight (follow lag + wobble), lift height needed before it moves, snap radius, what happens on a bad
   release (falls back home). Accessibility: look + pinch the object, then look + pinch a spot = it floats there.
2. **Pump** (lever): a handle constrained to one axis with a spring return. A stroke = pushed past ~80% of its
   travel and back past 30%. Settings: travel, spring strength, strokes needed, decay (progress cools if you stop).
   Accessibility: look + pinch the handle = one stroke.
3. Each piece reports progress and done events, saves its solved state, and accepts a ghost-hand lesson
   (contextual lesson system from 8b) the first time it's in view and reachable.
4. Leave a stub interface for a third piece, **Wheel** (rotate around an axis: sluice wheel, windmill), not built yet.

## B. The fallen log (Carry)
5. When the explorer reaches the log: `cinder.log.first` (already in) then the new `cinder.log.hands`: "Maybe your
   big hands could move it?" Gold focus on the log; ghost: pinch, lift, move aside (Mugesh's `grab_drop` clip with
   the key point on the log; if it reads badly, a procedural lift).
6. Two glowing drop spots on the rocky ledges beside the gap. Grab the log: it lifts with a **heavy lag and a small
   wobble**, leaves and dust fall off as it lifts, the explorer looks up at it. Released on a spot: thud, dust puff,
   a small camera-free screen-shake of the island's tiles (a quick bounce), the log settles. Released anywhere else:
   it drops back across the gap.
7. Solved: the trail is open (path tiles replace the blocker), saved. The explorer cheers (hop). Spirit: "You're
   strong! Let's go on."

## C. The forge: bellows → pipes → braziers → door (Pump)
8. Beside the forge door: a **Bellows** prop (new SDF recipe: leather-and-wood bellows with a long iron handle on top)
   and **iron pipes** running along the cliff from the bellows to the three braziers (pipe segments as a prop chain).
9. Pinch the handle and push it down, let it spring up = one pump. Each pump: the bellows squash and puff, a **glow
   pulse travels along the pipes** to the next unlit brazier, its coals brighten a step. **4 pumps per brazier**; heat
   cools slowly if you stop for more than 4 s (one step back every 2 s, never below a lit brazier).
10. A brazier lights with a flame burst. All three lit: the **forge door glows from inside, the iron bands heat up,
    it swings open** (or slides up) with steam and sparks. Spirit: "The forge is waking up!"
11. Ghost lesson: `pump_handle` (Mugesh records it: pinch the handle, push down, release, 2 strokes). Until then, a
    procedural clip. Add it to the recording checklist and the Ghost Clips window.
12. Lines: `cinder.bellows.first` → the handle: "Bellows! Pump them to send heat to the braziers."; on each lit
    brazier a short cheer (rotating "One!", "Two!", "Three!").

## D. Into the temple (placeholder until 9c)
13. Walking into the open door → scene swap to a **Forge Hall** interior (same system as the Lodge): ~10×10,
    dollhouse cutaway, stone floor, pillars, a big cold forge at the back, an anvil, chains, dim ember crystals.
    For now the Keeper's spot is an empty glowing circle; Spirit: "Something huge sleeps here... not yet." Door tile
    leads back out. (9c adds Hearthstag, the battle and the restoration.)

## E. Backlog (add to PolishBacklog.md, don't build)
- **Glazegolem should look clearly rare** (Mugesh, Oct 26): richer glaze, gold kintsugi cracks, a sparkle or
  shimmer. Do with the character pass.

## Housekeeping
- One compile batch per section; EditMode tests (pump stroke counting, decay, carry snap / fall-back, solved state
  saved and restored). Test in Editor Play over Link (restart Unity first).
- Commit per section with "(headset test pending)". No AI attribution. Don't push. Update `.claude-handoff.md`,
  GameDesign.md (puzzle kit, log, bellows as built).
- Summary for Mugesh: what to test, and the **new recording** (`pump_handle`) with how to do it.

## Test
1. Reach the log: the Spirit suggests your hands; a ghost shows the lift.
2. Grab the log: it feels heavy (lags, wobbles). Drop it on a glowing spot: thud and dust; the trail is open.
   Drop it elsewhere: it falls back.
3. At the forge: pinch the bellows handle and pump; glow pulses run along the pipes; braziers light one by one.
4. Stop pumping for a while: the heat cools a bit, never relights a brazier backwards.
5. Three braziers: the forge door opens with steam and sparks. Walk in: the Forge Hall rises; walk out again.
6. Look + pinch works for both puzzles (log + spot, handle = one pump).
7. Quit and relaunch after solving: the log stays moved, the door stays open.
