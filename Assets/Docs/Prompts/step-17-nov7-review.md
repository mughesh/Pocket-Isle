# Step 17: fixes from the Nov 7 Quick Journey video (judge review + Mugesh's notes)

Triaged Nov 7, 1 PM (design chat). The design chat checked every claim against frames of
`com.mugesh.pocketisle-20261107-114356-0.mp4` (15:14) and the code: what's below is confirmed or decided; the
review's other points were rejected on purpose (see "Not doing"). **Reference frames are in
`Assets/Docs/Prompts/step-17-img/` (file names = issue + video time). Read the listed images before each section.**

## How to run (works attended or unattended)
- Fresh session: orient first (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Assets/Docs/Overnight-Nov6.md`,
  `Assets/Docs/Notes copy.md`, git log).
- Work on branch **`step-17`** from the current HEAD (Mugesh fast-forwards master to `overnight-nov6` first).
- **Do not stage or commit** the step 15a work in progress (`ReleaseBuild.cs`, `Step15aTests.cs`, `DeviceBuild.cs`,
  `ReleaseReadiness.cs`, `ProjectSettings/*`, the icon meta, `DevAgentSettings.asset`, the TMP fallback font asset),
  nor `Assets/_Recovery/`, `_lanes/`. Stage only the files you change, by path. Never `git add -A`.
- Commit per section, message ends "(headset test pending)", no AI attribution lines. Don't push, don't merge.
- If Mugesh isn't answering: no questions, flat Editor Play via the bridge + tests + the smoke run
  (Tools > Pocket Isle > QA > Smoke Run), revert what you can't fix after ~45 min, log decisions.
- Order: **A → B → C → D → E → F → H → G → I → J → K** (A–F matter most; feature freeze is Nov 8).
- At the end: smoke run on the final state, then `Assets/Docs/Run-17.md` (done / partly / not, old → new values,
  shots, the headset test list in play order) and a `.claude-handoff.md` entry.

## A. The first catch must never feel broken (`03a_…`, `03b_…`)
What really happened (02:23–02:45): Toastling was in the red, the big cradle pod glowed (a red-zone catch is
allowed by design). Mugesh flicked it; `CatchThrow` judged the throw off-aim (`Aimed(act, target, catchAimAngle)`) →
"Missed! Flick it right at Toastling." He then reached for the cradle pod for ~15 s: **in 03a the island's grab frame
lights up as his hand reaches the cradle** (the cradle sits just off the island's front-right corner, inside the grab
band), so the island, not the pod, was taking his pinch. He only got through by attacking again.
1. **Aim assist:** any release with real forward speed whose direction is within ~70° (yaw) of the wild creature and
   not going backward counts as aimed; the pod then flies onto the wild creature (`FlyTo` already does). "Missed!" only
   for a pod dropped, thrown backward or clearly sideways off the island. Report old/new `catchAimAngle`.
2. **Cradle pod wins over the island grab:** while a battle is up, island grab is off (or at least within ~12 cm of the
   cradle and party pods). Log grabs while reproducing it with the bridge; find why re-grabbing the returned pod
   failed and fix that too.
3. After a miss: the pod is back in the cradle and **pulses**; caption "Missed! Grab the big pod and flick it again."
4. **No overworld chatter during battles:** at 02:29 the Spirit said "This way! Follow my gold trail." mid-battle.
   While a battle is up, only battle lines play (trail/idle/realm lines wait).

## B. The pause menu sits on the rim, it doesn't turn to the head (`04a_…`, `04b_…`)
The game menu row (Dev · Resume · Journal · Settings · speaker) is a `RuneBoard` that faces the head once and
re-faces after 30°/1 s (`BoardFacing`). On the rim that shears it across the stone when you lean (03:16–03:24).
5. Add a **rim-locked** mode to `RuneBoard`: yaw fixed to the island's front (island-relative 0), a fixed tilt that
   reads well from the seat, no re-facing. Use it for every row that sits on the rim: the game menu, the title menu
   (Full adventure / Quick journey / Settings) if it sits on the rim, the Quick Journey end card's row. Floating boards
   (Journal, Settings, Controls) keep face-once.

## C. Rim pods stay in their sockets (`05_…`)
6. `PodRack`: the gazed/focused party pod grows to `focusScale` 2 and **neighbours slide aside** (04:09: Pebbloop's pod
   pushed off its socket). Remove the slide. The focused pod grows only as far as fits its socket spacing (~1.3x) and
   lifts/pops slightly forward instead; its grab volume may stay larger than the visual. Same in battle switches.

## D. Kites leave the sky during battles (`06_…`)
7. Galewick's raised kites and strings (`KitePiece`, flying in the room over the island) cut straight through the battle
   (07:15–07:57). When a battle starts, fade them out (~0.3 s); fade them back when it ends. Check every other prop that
   lives in the room outside the island (Cinderwood ember, Lilymere rain/water, anything raised by a hand moment) and
   do the same.

## E. The temple's return gate appears after the shard (`07_…`)
8. `KeeperHall` shows the return gate (and its burst) when the Keeper is calmed; it glows while the Keeper is still
   talking (09:51–10:34) and pulls the eye from the shard. Show the gate + burst + exit arrow only **when the shard is
   taken** (touch/grab → Spirit). Save/load between calm and pickup stays consistent.

## F. Ghost hands that read at a glance
9. **Swipe ghost** (`02a_…`, `02b_…`): it's tiny and drawn over the map's centre. Show it **~1.6x** bigger beside the
   island's **front-right rim**, where the real right hand rests (`restingHand`), side-on (step 16 E.11 rule: palm never
   toward the face), with a **gold glowing dot + short trail on the thumb tip** during the slide. Never over the map.
10. **Evolution ghost** (`08_…`): the `grab_palm_up` clip ends in a vertical palm facing the player (reads as "stop").
    End pose: **palm flat, facing up**, fingers toward the island, the pod resting on it, ~10 cm above the rim's front
    edge beside the glowing pod. Check the finale's open-palm ghost (`FinaleDirector`, "where the ghost shows the open
    palm") uses the same correct pose.
11. **Move / resize lessons** (`01_…`): the bubble "Grab two corners and pull apart…" sits on the front-right corner,
    over the handle and the right ghost hand. During these two lessons the Spirit and bubble park **above the back
    centre** of the island, never near a corner handle.

## H. The finale party is readable (`09a_…`, `09b_…`)
12. Hall of Fame (`FinaleDirector`): the three Islings huddle in front of the statue, overlapping (Riverloom clips into
    Toastling) and the cards are tiny. Spread them in a **front arc** around the statue: no two footprints overlap
    (centre gap ≥ 1.4x the bigger creature's width), none hidden behind the statue, all inside the cap. Cards **~1.8x**
    bigger text, staggered heights, never intersecting the statue or each other (layout checker), facing the player
    once. Keep the stone-tablet style; raise the text contrast.

## G. "The island tilts when I lean in" (`11_…`): find it before fixing it
13. Mugesh: when he brings his head close for fine work (the mill flick at 05:24–05:30, the finale at 14:31–14:37) the
    island tilts a little. The island root has no head-facing code, so find what moves it: log every island-root pose
    change (> 2 mm or > 0.3°) with its caller during flat Play with synthetic hands near the rim, and read every writer
    (`IslandPlacer` re-place safety net, `IslandHandle` grab band while hands work near the rim, recenter glide,
    `HandPuzzle.KeepViewWhileLocked` / `FocusLock`, `WorldWindow`).
14. Rule to enforce: the island root moves **only** by a deliberate grab (pinch held in the grab band), Meta recenter or
    first placement. A one-hand grab moves and yaws, never pitches or rolls. If accidental grabs are the culprit: island
    grab is off while a hand puzzle is active (same as battles in A2). If you find nothing, say so with the log; don't
    guess-fix.

## I. Shard bursts per element
15. Touching a Keeper's shard (10:35) and slotting it in the statue (12:44) currently just vanish/slot. Reuse the
    **opening shard-touch burst** (`Opening`), tinted per element, plus one flourish: **wind** = a swirling ring +
    a few leaves; **fire** = rising embers; **water** = a ripple ring + bubbles. ≤ 1.5 s, small particle counts, 72 fps
    safe; the existing shard chime. It rhymes with the very first touch of the game: that's the point.

## J. Credits behind the island + the standee (`10_…`)
16. The credits plaque over the Heart is small (14:42). Make it a **wide slate banner behind the island** (back edge,
    above the cap, ~0.5 m wide at island scale 1, readable from the seat), the lines **rolling slowly upward** inside a
    masked window (3–4 lines visible, bigger type), still ~10–15 s, Skip still works.
17. **Standee:** if `Assets/_Project/Art/Credits/Mugesh.png` exists, show it beside the banner as a paper/acrylic
    cut-out (alpha-cutout quad, thin backing rim, soft shadow, gentle bob). If the file is absent, show nothing (no
    placeholder). Mugesh will drop the PNG in.

## K. Dev panel tidy (`12_…`)
18. The Dev button is already Editor/QA-only. The panel stayed open from 08:34 to the end of the video: auto-close it
    when a battle starts, on any scene/realm swap and when the finale starts.

## Not doing (decided)
- **Locking the pod until the creature is dazed** ("attack once more first"): the review misread the HP bar. The
  red-zone catch is the design; the real failure was aim + the island stealing the grab (A).
- **Jingles −8 dB again:** done Nov 6 (step 16 B: −9 dB, soft mallet/bell, low-pass). The review repeated an old claim.
- **Hearth-shrine ghost "re-triggering" (03:36):** that was the first visit in this run; it's the intended hint.
- **Rewording the pause line / dropping the left-palm pause:** kept on purpose (Nov 6 decision).
- The review's hard numbers (0.22 m arc, Euler(25,0,0), 0.18 m bubble offsets, `hasEnteredHearthLodgeOnce`):
  guesses from a video; use the real layout and the rules above.
