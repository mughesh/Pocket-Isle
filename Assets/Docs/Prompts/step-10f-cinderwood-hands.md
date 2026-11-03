# Step 10f: Cinderwood hand moments (cup the flame, light the lantern trail, ember in your palm)

Read CLAUDE.md, `.claude-handoff.md`, `RealmsDesign.md` ("What makes a hand moment beautiful" + the new
"Cinderwood upgrade" section), GameDesign "Cinderwood as built" + "Hand puzzles as built", the kit in
`Runtime/Puzzles`, 10b's Galewick pieces (CloudBank blocking tiles, `lessonChip`, `TileTouch.Block`, ghosts).
Decided Oct 27 with Mugesh: Cinderwood felt undercooked next to Galewick. **Add three moments; the log and the
bellows stay.** Mugesh is here: commit per section and say what to test. Before Play over Link, ask him to restart Unity.

Shared rules (same as Galewick): kit pieces, island-local, saved in `PuzzleState`, work only near the explorer,
look + pinch fallback, lesson pinned as a chip until it first works, ghost on the real object, fingertips at the
piece never walk the explorer, a cheer when solved, a short line each (`cw.*`, ≤ 60 characters). **The world
answers widely**: every moment lights or wakes many things, not one.

## A. Cup the flame (Kiln village shrine, on the route)
1. On the first visit the village's Hearth Shrine flame is **nearly out**: a tiny sputtering blue-orange wisp, the
   plaza's kilns cold, huts dark. `cw.flame`: "The village flame is fading! Keep it warm with your hands." chip "Cup it!".
2. **Cup:** both hands within ~10 cm of the flame with palms turned toward it (palm normal within ~60° of the flame)
   for 2 s → it grows (scale + brighter + warm particles rising between the fingers while held). One hand curled round
   it for 3 s also works. Look + pinch-hold 3 s = the same.
3. Rekindled: a warm pulse rolls out over the plaza: all kilns glow and puff smoke, hut windows light, lantern posts
   catch one after another outward, the three plaza creatures cheer/hop. The shrine works from then on (heal + save,
   palm hover as before). Saved. Before it's rekindled the shrine doesn't heal (the Spirit says so once).

## B. Light the lantern trail (new: the Smoky hollow, on the route between the village and the hot springs)
4. Map: the trail from the village to the springs passes a **Smoky hollow**: 6–8 tiles of trail under thick dark
   smoke (dark grey-violet puffs, like the cloud bank but dark, with ember specks) that block walking, with **6 dead
   stone lanterns** lining it (glade lantern prop). Update `CinderwoodTools` (seed stays), the trail checks and the
   landmark (`hollow`), and the dev panel Go to. Old saves with the explorer past it stay valid.
5. **Light:** a fingertip touching a lantern's top (≤ 2.5 cm) lights it. Touch two in order within 1.5 s (a sweep)
   and the fire **races** down the rest of the line by itself: each lantern catches 0.15 s after the last, a spark
   trail jumps between them along the path, and the smoke burns off from the lit end in rolling puffs. Look + pinch a
   lantern = it lights, and the next one lights itself 0.5 s later. `cw.lanterns`: "It's too dark to pass. Light the
   lanterns!" chip "Touch them!".
6. The smoke tiles open as it burns away (saved). The lit hollow stays a nice place: lanterns flicker warm.

## C. Ember in your palm (forge temple, before the bellows)
7. The forge's firebox (a stone mouth beside the door, under the pipe) is cold, so the bellows do nothing yet
   (pumping shows a weak grey puff, `cw.forge.cold`: "The forge is stone cold. It needs a spark first.").
8. An ember vent near the forge glows faintly. **Hold an open hand palm-up just under/beside the vent** (or pinch the
   ember): a live ember hops up and **sits on your palm**, glowing (warm halo sprite, sparks drifting up, a soft
   orange tint on the virtual hand if it's drawn). It stays as long as the palm stays roughly up; turning the hand over
   **above the firebox** tips it in. Tipped elsewhere or dropped: it falls back to its vent. Look + pinch the vent,
   then the firebox = it flies in by itself. `cw.ember`: "Carry an ember to the forge. Gently!" chip "Palm up!".
   (Be honest in the code comments: virtual light can't light the real room; the glow is on the ember and hand.)
9. Ember in: the firebox roars, the chimney on the cliff starts smoking, every ember vent along the cliff glows, the
   pipe warms a step. Now the bellows work as built in 9b (pumps carry the heat to the braziers → door).

## D. Hook-ups
10. Restoration wave unchanged, but the rekindled village, the lit hollow and the roaring firebox are already lit
    before it, so the restored state looks fuller; make sure nothing double-lights or flickers back.
11. Quick Journey: none of these are cut (config can skip later). Dev panel Story tab: flame / hollow / ember flags.
12. Re-run the XP trail check (the hollow adds ~8 tiles) and report the arrival level at the temple.

## Housekeeping
- EditMode tests: cup detection from synthetic hand poses (two hands, one hand, not cupped), lantern chain timing,
  palm-up carry + tip, blocking tiles, trail connectivity with the hollow. Commit per section "(headset test
  pending)", no AI attribution, don't push. Update `.claude-handoff.md`, GameDesign.md (Cinderwood), QA-Guide.
- Shots `Logs/BattleShots/10f_*` (flame before/after with the plaza, hollow before/mid-chain/after, ember on the
  palm with a synthetic hand, firebox roaring). Perf: ≤ 6 extra draws per moment.
- Then rebuild the QA APK (wind gate as Mugesh decided in 10e) and install it.

## Test
1. Village: the shrine flame is dying; cup it with both hands; the whole plaza lights up and the shrine heals.
2. Hollow: touch two lanterns in a row; the fire races down the line and the smoke burns away.
3. Forge: bellows do nothing; hold your palm by the vent, an ember sits on your hand; turn your hand over above the
   firebox; it roars; now the bellows light the braziers and the door opens.
