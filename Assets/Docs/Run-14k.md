# Step 14k report: inward grab band, Journal text + ray/poke, lines in context, lodge without pedestal, ghost see-through, walk-up triggers, labels at the feet

Headset test pending for all of it. Tests 318/318. No build (not asked). Nothing pushed. No scene changes: new defaults come
from new fields (`IslandHandle.grabInward` etc., `BattleHud.labelScale`) and runtime code. Data changed: `Script_v1.md` +
asset (contexts), `Lodge.asset` (pedestal removed).

## Commits

| Commit | What |
|---|---|
| `de16aee` | A: grab band from the line inward, nothing outside |
| `f8adc84` | B: Journal 4 x 3, bigger text, poke + hand ray, hover pop on every tablet, dev panel text |
| `bcf5857` | C: every Spirit line has a context and a kind; intros first, filler last, out-of-place dropped |
| `97e4af1` | D: lodge without the pedestal: poke an Isling, its card shows above it |
| `1e788a8` | E: see-through ghost (not a hole), grass lies down round the explorer |
| `7dcdfff` | F: one walk-up rule (within 1 tile of what you see, diagonals too); audit of every map |
| `6a642f5` | G: battle labels at the creatures' feet |

## A. Grab band (inward only)
- **Why only the line grabbed:** 14j cut everything above the cap ("the play area"), so at the rim's height the zone was just
  the 5 mm between the line and the tiles, plus 1.2 cm outside.
- Now: from the line **3.5 cm in** (world, any island size), at the cap's height too, up to **2.5 cm over the cap** and down
  the rock face; **outside the line only 3 mm** of tracking slack. Corners and the front lip as before; pods still win.
- The ISDK shell boxes (step 8e: 4 cm in, 1.2 cm over the cap) are widened at Start to hold the band at the smallest island
  size; the filter keeps only the band.
- The frame shows the band: a soft strip lying on the rim from the line inward at the corners, lit with the line.

| Probe (island scale 0.6) | Result |
|---|---|
| 3 cm / 1 cm outside the line | none / none |
| on the line | corner |
| 1 cm / 3 cm inside, 1 cm over the cap | corner / corner |
| 5 cm inside | none |
| 3 cm inside on the rock face | corner |
| 1 cm inside, 4 cm over the cap | none |

Shot `Logs/BattleShots/14k_band.png` (a fake palm 2 cm inside the corner).

## B. Journal
- **4 x 3 portraits a page** (Fire 20 = 2 pages, the Page tablet beside the help line). One text per cell, so a hint wraps to 2
  lines ("Galewick, the warm / field").
- Sizes (x the board font): names 1.0 (was 0.66), hints 0.8 (~0.53), help 0.95 (0.72), header 1.2 (1.05), card title 1.15
  (1.0), card body up to 1.0 (0.9, min 0.75), tabs 20% taller.
- **Input:** poke, or the **hand ray + pinch** (thin line + dot like the dev panel, gold over a portrait or tablet). No look +
  pinch (the board's gaze reticle is off). Help line "Poke a portrait, or point and pinch."
- **Hover pop:** a fingertip within 4 cm in front of a portrait (or the ray on it) grows it 10%, lifts it 4 mm and shows a
  white rim; it eases back. Every board tablet (menu, Settings, Journal tabs, battle runes) does the same from ISDK poke
  hover, and the tablet rim is now **white on hover** (the press glow stays gold).
- Dev panel: button labels 1.2x (shrink to fit), status 0.95x, title 1.0x.
- Layout check (`Step14jDebug.Layouts`): title, Journal, battle runes, lodge tablet: **0 problems**.
- Shots `14k_journal.png`, `14k_journal_hover.png`, `14k_journal_card.png`.

## C. The Spirit talks about what's in front of you
- **Why the lodge got grass lines first:** a lesson pinned for the map (the grass lesson) held every other line back
  ("waits until the lesson is done") and its own lines kept their place in the queue, so the lodge's welcome waited.
- Rule: every trigger has `context` (hub, realm, cinderwood, galewick, lilymere, lodge, hall, outside, inside, battle; `& | !`)
  and `kind` (objective, intro, story, filler), written in `Script_v1.md` as `· in <context> · <kind>`, idle hints as
  `[in <context>]` (`Places` reads where you are). Out of place: refused (a "once" line stays unsaid), queued ones dropped.
  An intro goes ahead of story and filler (and before a lesson's standing line, which comes back after it); filler waits for
  intros and objectives; a pinned lesson that doesn't apply here holds nothing up; idle hints only from those valid here and
  not in the first 8 s after arriving. Every line shown logs `[Dialogue] Show <id> (<kind>, in <context>) at <place>`.
- Tags: 207 rows (table at the end). any 32, hub 28, cinderwood 28, galewick 22, lilymere 20, battle 21, outside 12,
  outside & !battle 18, hall 14, lodge 9, realm 1, !battle 1 (idle).

Flat pass (`Step14kDebug.Pass`: hub -> lodge -> Cinderwood -> Forge Hall -> a wild battle, intros forgotten first):

| Where | What showed / happened |
|---|---|
| hub | `hub.shard.first` (objective); idle hint "You can move the island anytime..." (4 hints valid) |
| hub, fired lodge / Cinderwood lines | `lodge.first`: not here; `cw.arrive`: not here |
| lodge | **`lodge.first` (intro) first**, then `lodge.inspect`; `tut.grass`, `hub.return.first`: not here; idle: 1 valid hint |
| Cinderwood | idle "Your Islings get stronger every battle." (4 valid); `lodge.last`, `hub.return.first`: not here |
| Forge Hall | idle: 1 valid hint; `cinder.village.first`: not here |
| wild battle (Cinderwood) | `pod.first`, `battle.first` (objectives, in battle); `menu.first` (outside & !battle): not here; `lodge.last`: not here |

## D. Lodge without the pedestal
- The pedestal prop is gone from the lodge map (and its generator). Poke an Isling (or look + pinch): it **hops in place, turns
  to you, its card shows above it**; poke it again to close, another to switch; closes after **6 s** or when you walk.
- Gone: the pedestal tile rules, "look + pinch a rim pod with an Isling on the pedestal" swap, the `pedestal` target
  (`lodge.take` now points at the inspected Isling: `lodgeInspected`). Swaps by grabbing only (unchanged).
- Shot `14k_lodge_card.png`.

## E. See-through: a ghost, and in grass too
- Props between the head and the explorer keep **a third of their surface on an even 4 x 4 screen pattern**, with a pale edge
  glow on their silhouette and a pale outline: the shape stays. Creatures and eyes (no pixel position in their shaders) thin
  the same way on a 3D hash. The explorer keeps its gold outline. **Off in battles** (the near Isling ghosted when the explorer
  stood behind it).
- **Tall grass:** blades within ~1.1 tiles of the explorer on the player's side (and right at its feet) lie down to a fifth of
  their height, leaning away. Always on (map and battle zoom).
- Perf: CPU check **0.012 ms**; GPU a few ALU in the shared clip (props: ordered dither + glow) and in the grass vertex
  shader, no extra pass or draw. Not measured on the device yet (Editor only).
- Shots `14k_ghost_before.png` / `_after.png` / `_close.png` (Cinderwood log), `14k_grass_before.png` / `_after.png` (hub grass).

## F. Walk-up triggers (`Step14kDebug.WalkUpAudit`)
- **Mugesh's Cinderwood shrine:** the shrine's front steps cover the row in front of its 2 x 2 tiles. Standing on the path in
  front of the steps (r29) was 2 rows from the tiles, so nothing fired; r28 (on the steps) did. The shrine now counts its
  steps (2 x 3) and the step row itself.
- Rule: any tile within one of what you see, diagonals too. Gates and the tower / temple doors (walked into from the front)
  take their front row and its two diagonals. Hand moments use their reach (2-7 tiles, already diagonal).
- The trail end for a hand moment is now a tile in its reach next to it, nearest the explorer (it used to be the nearest
  walkable tile to its centre, which for the village flame was **behind** the shrine, c23 r25).

| Map | Was off (now fixed) |
|---|---|
| Still Isle | shrine c10 r20: 4 tiles (path row in front of its steps); fire gate: 1 front diagonal; water gate: 2; wind gate: 1 |
| Cinderwood | village shrine c24 r26: 4 tiles; home gates: 1 + 2 front diagonals; `cw.flame` trail end was behind the shrine |
| Galewick | shrine c14 r40: 4 tiles; home gates: 2 + 2; wind tower door: 2 front diagonals |
| Lilymere | shrine c14 r40: 4 tiles; home gates: 2 + 2; lake temple door: 2 front diagonals |

- Every trail end checked triggers: the hub gates' (front tile), the tower and temple doors' landmarks, every hand moment's.
  The forge door is walked into (the doorway tiles), unchanged.

## G. Battle labels at the feet
- Every name + HP bar: **just in front of its pad, its plate's bottom at pad level**, facing you, **35% bigger**, on top on the
  dark plate. The near Isling's in front of its pad, toward the runes (lifted over the caption if they'd meet); the wild /
  Keeper's between the pads, sliding sideways only as far as needed to clear the near Isling on screen (checked every 0.25 s).
- Shots `14k_labels_tidemother.png`, `_hearthstag.png`, `_riverloom.png` (Lv 15 near forms vs Kettlewing),
  `14k_keeper_cinderwood.png`, `_galewick.png`, `_lilymere.png`.

## Headset test list (grab first)
1. **Grab:** pinch a corner on the line, then 1 cm and 3 cm in over the rim: all grab; the corner shows a soft strip. Pinch
   1 cm outside the line: nothing (it walks / selects). Two corners: resize. Next to a pod: the pod.
2. **Journal:** open it from the menu: 12 portraits, bigger names, hints on 2 lines. Point your hand: a ray and dot; pinch on a
   portrait opens its card. Poke one: same. Bring a fingertip close: the portrait grows with a white rim. Looking + pinching
   does nothing. Menu and Settings tablets pop the same way under a finger.
3. **Lodge:** enter: the Spirit's first line is the lodge welcome (no grass tips). Poke an Isling: it hops, its card shows
   above it; poke again closes; it closes by itself after 6 s or when you walk. No pedestal.
4. **Lines:** in a realm and a Keeper hall, idle chatter fits the place (no lodge or grass tips in a hall).
5. **See-through:** Cinderwood forest: trees in front of the explorer turn pale and see-through, shape kept. Tall grass: the
   blades round the explorer on your side lie down.
6. **Shrines and gates:** walk to Cinderwood's village shrine along the path: the ring / Spirit react from the path in front of
   its steps. Stop on a gate's front diagonal: it goes through.
7. **Battle:** names and bars at the creatures' feet in front of the pads, readable from where you sit; a Keeper fight too.
8. **Perf:** Cinderwood forest and tall grass at 72 fps (OVR Metrics).

## Gotchas
- **My mistake (fixed):** a bridge call that opened the menu while Play wasn't running set `Time.timeScale = 0` in the
  Editor, and saving assets wrote it into `ProjectSettings/TimeManager.asset` (Play then froze on the rise). Reverted the file
  and reset the scale; check Play is running before menu / board bridge calls.
- Flat Play needs Standalone "Initialize XR on Startup" off (`Step12aDebug.XrInit` 0); it's back on (1), not committed.
- Editor Play writes `quick.json`: I backed it up before the passes and restored it after each (teleports solve puzzles).

## Appendix: line contexts (Script_v1)

| Section | Trigger | Context | Kind |
|---|---|---|---|
| Opening | `intro.shard` | hub | objective |
| Opening | `intro.arrive` | hub | intro |
| Opening | `lesson.move` | outside | objective |
| Opening | `intro.starter` | hub | objective |
| Opening | `starter.kilnpot` | hub | story |
| Opening | `starter.pebbloop` | hub | story |
| Opening | `starter.pufflet` | hub | story |
| Opening | `intro.statue` | hub | story |
| Tutorials: lessons | `lesson.pinch` | outside | objective |
| Tutorials: lessons | `lesson.pinch.once` | outside | objective |
| Tutorials: lessons | `lesson.pinch.done` | outside | story |
| Tutorials: lessons | `lesson.move.again` | outside | objective |
| Tutorials: lessons | `lesson.starter.again` | hub | objective |
| Tutorials: lessons | `lesson.walk` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.again` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.turn` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.turn.again` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.far` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.far.again` | outside & !battle | objective |
| Tutorials: lessons | `lesson.walk.help` | outside & !battle | objective |
| Tutorials: lessons | `lesson.stop` | outside & !battle | story |
| Tutorials: lessons | `lesson.stop.again` | outside & !battle | story |
| Tutorials: lessons | `lesson.look` | outside & !battle | objective |
| Tutorials: lessons | `lesson.look.again` | outside & !battle | objective |
| Tutorials: lessons | `lesson.grass` | outside & !battle | objective |
| Tutorials: lessons | `lesson.grass.again` | outside & !battle | objective |
| Tutorials: lessons | `lesson.send.again` | battle | objective |
| Tutorials: lessons | `lesson.rune.again` | battle | objective |
| Tutorials: lessons | `lesson.catch.again` | battle | objective |
| Tutorials: lessons | `lesson.twoHands` | outside | objective |
| Tutorials: lessons | `lesson.hands` | any | story |
| Tutorials: lessons | `praise.nice` | any | story |
| Tutorials: lessons | `praise.like` | any | story |
| Tutorials: lessons | `praise.perfect` | any | story |
| Shard Gates and realms | `gate.sealed` | outside | story |
| Shard Gates and realms | `gate.fire.first` | hub | story |
| Shard Gates and realms | `realm.cinderwood.first` | cinderwood | intro |
| Shard Gates and realms | `hub.return.first` | hub | intro |
| Tutorials | `tut.grass` | outside | story |
| Tutorials | `tut.pond` | outside | story |
| Tutorials | `pod.first` | battle | objective |
| Tutorials | `battle.first` | battle | objective |
| Tutorials | `battle.types` | battle | story |
| Tutorials | `battle.win.first` | any | story |
| Tutorials | `battle.lose.first` | any | story |
| Tutorials | `capture.first` | battle | objective |
| Tutorials | `capture.locked` | battle | story |
| Tutorials | `capture.dazed.first` | battle | story |
| Tutorials | `capture.success.first` | any | story |
| Tutorials | `party.full` | any | story |
| Tutorials | `evolve.first` | any | story |
| Tutorials | `evolve.ready` | any | objective |
| Tutorials | `evolve.done` | any | story |
| Tutorials | `journal.first` | any | story |
| Tutorials | `shrine.first` | outside | story |
| Tutorials | `shrine.near` | outside | story |
| Tutorials | `shrine.later` | outside | story |
| Tutorials | `lodge.first` | lodge | intro |
| Tutorials | `lodge.inspect` | lodge | story |
| Tutorials | `lodge.take` | lodge | story |
| Tutorials | `lodge.leave` | lodge | story |
| Tutorials | `lodge.exit` | lodge | story |
| Tutorials | `lodge.first.empty` | lodge | intro |
| Tutorials | `lodge.toss` | lodge | story |
| Tutorials | `lodge.inspect.one` | lodge | story |
| Tutorials | `lodge.last` | lodge | story |
| Tutorials | `gate.first` | outside | story |
| Tutorials | `rare.first` | any | story |
| Tutorials | `xp.first` | any | story |
| Tutorials | `steambara.first` | battle | story |
| Tutorials | `menu.first` | outside & !battle | story |
| Cinderwood | `cw.arrive` | cinderwood | intro |
| Cinderwood | `cw.vine` | cinderwood | objective |
| Cinderwood | `cw.vine.tip` | cinderwood | objective |
| Cinderwood | `cw.vine.done` | cinderwood | story |
| Cinderwood | `cinder.village.first` | cinderwood | story |
| Cinderwood | `cinder.springs.first` | cinderwood | story |
| Cinderwood | `cinder.log.first` | cinderwood | story |
| Cinderwood | `cinder.temple.first` | cinderwood | story |
| Cinderwood | `cinder.ruins.first` | cinderwood | story |
| Cinderwood | `cinder.log.hands` | cinderwood | objective |
| Cinderwood | `cinder.log.done` | cinderwood | story |
| Cinderwood | `cw.temple` | cinderwood | story |
| Cinderwood | `cinder.bellows.first` | cinderwood | objective |
| Cinderwood | `cinder.brazier.one` | cinderwood | story |
| Cinderwood | `cinder.brazier.two` | cinderwood | story |
| Cinderwood | `cinder.brazier.three` | cinderwood | story |
| Cinderwood | `cinder.forge.open` | cinderwood | story |
| Cinderwood | `cinder.hall.first` | hall | intro |
| Cinderwood | `cinder.keeper.first` | hall | story |
| Cinderwood | `keeper.shield.first` | battle | story |
| Cinderwood | `keeper.shield.broken.first` | battle | story |
| Cinderwood | `keeper.phase2` | battle | story |
| Cinderwood | `cinder.keeper.win` | hall | story |
| Cinderwood | `cinder.shard.take` | hall | story |
| Cinderwood | `cinder.restored` | cinderwood | story |
| Cinderwood | `cw.flame` | cinderwood | objective |
| Cinderwood | `cw.flame.cold` | cinderwood | story |
| Cinderwood | `cw.flame.done` | cinderwood | story |
| Cinderwood | `cw.lanterns` | cinderwood | objective |
| Cinderwood | `cw.lanterns.done` | cinderwood | story |
| Cinderwood | `cw.ember` | cinderwood | objective |
| Cinderwood | `cw.ember.tip` | cinderwood | objective |
| Cinderwood | `cw.forge.cold` | cinderwood | story |
| Cinderwood | `cw.ember.done` | cinderwood | story |
| Galewick | `gate.wind.first` | hub | story |
| Galewick | `realm.galewick.first` | galewick | intro |
| Galewick | `gw.arrive` | galewick | intro |
| Galewick | `gw.arrive.clouds` | galewick | objective |
| Galewick | `gw.arrive.done` | galewick | story |
| Galewick | `gw.village.first` | galewick | story |
| Galewick | `gw.mill.first` | galewick | story |
| Galewick | `gw.kites.first` | galewick | story |
| Galewick | `gw.ridge.first` | galewick | story |
| Galewick | `gw.tower.first` | galewick | story |
| Galewick | `gw.rare.first` | galewick | story |
| Galewick | `gw.mill` | galewick | objective |
| Galewick | `gw.mill.done` | galewick | story |
| Galewick | `gw.kites` | galewick | story |
| Galewick | `gw.kites.task` | galewick | objective |
| Galewick | `gw.kites.one` | galewick | story |
| Galewick | `gw.kites.two` | galewick | story |
| Galewick | `gw.kites.three` | galewick | story |
| Galewick | `gw.gust` | galewick | objective |
| Galewick | `gw.gust.done` | galewick | story |
| Galewick | `gw.tower.kites` | galewick | story |
| Galewick | `gw.tower.open` | galewick | story |
| Galewick | `gw.meet.kitelet` | battle | story |
| Galewick | `gw.meet.zeppelope` | battle | story |
| Galewick | `gw.keeper.knots` | hall | objective |
| Galewick | `gw.keeper.meet` | hall | story |
| Galewick | `gw.keeper.shield` | battle | story |
| Galewick | `gw.keeper.shield.break` | battle | story |
| Galewick | `gw.keeper.win` | hall | story |
| Galewick | `gw.shard.take` | hall | story |
| Galewick | `gw.restored` | galewick | story |
| Galewick | `keeper.thanks.evolve.wind` | hall | story |
| Galewick | `keeper.thanks.evolve.fire` | hall | story |
| Galewick | `keeper.thanks.evolve.water` | hall | story |
| Lilymere | `gate.water.first` | hub | story |
| Lilymere | `realm.lilymere.first` | lilymere | intro |
| Lilymere | `lm.arrive.stream` | lilymere | objective |
| Lilymere | `lm.arrive.stream.done` | lilymere | story |
| Lilymere | `lm.village.first` | lilymere | story |
| Lilymere | `lm.channel.first` | lilymere | story |
| Lilymere | `lm.garden.first` | lilymere | story |
| Lilymere | `lm.basin.first` | lilymere | story |
| Lilymere | `lm.temple.first` | lilymere | story |
| Lilymere | `lm.marsh.first` | lilymere | story |
| Lilymere | `lm.pond.first` | lilymere | story |
| Lilymere | `lm.pads` | lilymere | objective |
| Lilymere | `lm.pads.done` | lilymere | story |
| Lilymere | `lm.rain` | lilymere | objective |
| Lilymere | `lm.rain.done` | lilymere | story |
| Lilymere | `lm.scoop` | lilymere | objective |
| Lilymere | `lm.scoop.more` | lilymere | story |
| Lilymere | `lm.scoop.done` | lilymere | story |
| Lilymere | `lm.meet.sailfold` | battle | story |
| Lilymere | `lm.meet.bottlewhale` | battle | story |
| Lilymere | `temple.warn` | realm | story |
| Lilymere | `lm.temple.shut` | lilymere | story |
| Lilymere | `lm.temple.open` | lilymere | story |
| Lilymere | `lm.keeper.meet` | hall | story |
| Lilymere | `lm.keeper.shield` | battle | story |
| Lilymere | `lm.keeper.shield.break` | battle | story |
| Lilymere | `lm.keeper.win` | hall | story |
| Lilymere | `lm.shard.take` | hall | story |
| Lilymere | `lm.restored` | lilymere | story |
| Objectives | `objective.more` | any | objective |
| Objectives | `spirit.poke.idle` | any | filler |
| Statue and finale | `hub.shard.first` | hub | objective |
| Statue and finale | `hub.shard.placed` | hub | story |
| Statue and finale | `hub.shard.more` | hub | story |
| Statue and finale | `statue.two` | hub | story |
| Statue and finale | `statue.three` | hub | story |
| Statue and finale | `statue.shard.1` | hub | story |
| Statue and finale | `statue.shard.2` | hub | story |
| Statue and finale | `statue.shard.3` | hub | story |
| Statue and finale | `heart.dream.fire` | hub | story |
| Statue and finale | `heart.dream.wind` | hub | story |
| Statue and finale | `heart.dream.water` | hub | story |
| Statue and finale | `spirit.dream.1` | hub | story |
| Statue and finale | `spirit.dream.2` | hub | story |
| Finale | `heart.wake` | any | story |
| Finale | `spirit.finale.1` | any | story |
| Finale | `spirit.finale.2` | any | objective |
| Finale | `spirit.finale.hand` | any | objective |
| Finale | `spirit.finale.wait` | any | story |
| Finale | `spirit.finale.3` | any | story |
| Finale | `heart.thanks` | any | story |
| Finale | `heart.friends` | any | story |
| Finale | `spark.hello` | any | story |
| Post-game | `heart.collection` | any | story |
| Post-game | `heart.collection.full` | any | story |
| Post-game | `spark.rare.cinder` | any | filler |
| Post-game | `spark.rare.gale` | any | filler |
| Post-game | `spark.rare.lily` | any | filler |
| Post-game | `keeper.spar` | any | story |
| Post-game | `keeper.spar.bow` | any | story |
| Quick Journey | `quick.gate` | hub | story |
| Quick Journey | `quick.end` | hub | story |
| Quick Journey | `quick.ending.go` | hub | story |
| Idle hints | `Your Islings get stronger every battle.` | outside & !battle | filler |
| Idle hints | `Every gate leads somewhere different. Just saying.` | outside & !battle | filler |
| Idle hints | `Psst. Rare ones hide in tall grass.` | outside & !battle | filler |
| Idle hints | `You can move the island anytime. Grab a corner.` | !battle | filler |
