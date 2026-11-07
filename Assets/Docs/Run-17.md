# Run 17: fixes from the Nov 7 Quick Journey video

Branch `step-17` (from `overnight-nov6` HEAD `931c56c`; master was not fast-forwarded yet). Not pushed, not merged.
Every section is committed on its own, "(headset test pending)". Run unattended on Nov 7, 13:36 to ~14:40.

| | Section | State | Commit |
|---|---|---|---|
| A | First catch never feels broken | **done** | `03c53b7` |
| B | Rim rows anchored to the island | **done** | `00b4d26` |
| C | Rim pods stay in their sockets | **done** | `e30ff9f` |
| D | Kites leave the sky in battles | **done** | `9be2463` |
| E | Hall exit after the shard | **done** | `313db3f` |
| F | Ghost hands (swipe, evolve, lessons) | **done** | `d85872d` |
| H | Finale party arc + cards | **done** (arc tested in tests, not seen in a shot) | `aa458b0` |
| G | "Island tilts when I lean in" | **found + fixed** | `7d02042` |
| I | Shard bursts per element | **done** (fires, not caught in a shot) | `730d4c7` |
| J | Credits banner + standee | **done** (standee waits for `Mugesh.png`) | `60a5d74` |
| K | Dev panel auto-close | **done** | `7b05d6d` |

Tests: 339/339 edit mode. Smoke run: 84/84 (see the end).

## What changed (old → new)

**A. Catch**
- Root cause of the 15 s fight with the cradle pod: the island's "pod wins" check (`IslandHandle.NearPod`) only looked at the
  six rim pods, **not the cradle pod**, and the cradle sits on the near-right corner, which always grabs. So a pinch on the
  cradle pod grabbed the island (the grab frame lit up in `03a`).
- Fix: the cradle pod wins within 12 cm (`cradlePodClearance`), and **island grab is locked while a battle is up**
  (`IslandHandle.SetGrabLock`; the old pod-reach pause now uses the same lock; the grab frame hides while locked).
- Aim assist: `catchAimAngle` 25° → `catchAssistAngle` **70°**, plus speed toward the creature ≥ 0.15 m/s. Misses: a drop,
  backward or sideways. Bridge check: 110° off = miss, 55° off = caught.
- After a miss: the pod pulses in the cradle; "Missed! Grab the big pod and flick it again."
- In battle only battle lines play (battle, capture, xp, evolve, rare, pod, party, keeper, a landmark meeting, or a line whose
  context asks for `battle`); other lines wait and show after. Checked: `spirit.poke.idle` (the gold-trail line) is held.

**B. Rim rows**: `RuneBoard.rimLocked`: yaw fixed to the island's front, tilt **35°** fixed, no re-facing. On the game menu,
title, Quick Journey end card, finale and rematch rows and the temple door question. Journal and Settings keep face-once.

**C. Rim pods**: no more sliding aside. The focused pod's glass grows to at most **1.3x** (was 2x), never past its socket
spacing, pops forward and lifts a little. Its grab volume still grows (up to 2x) without overlapping a neighbour's.

**D. Room props**: `BattleVeil` shrinks the flying/tied kites, their strings, a carried ember and the water ball away in
0.3 s when a battle starts and brings them back after. (Cloud bank, gusts and the Keeper shard already hid.) The prop shader
is opaque, so "fade" = a quick shrink.

**E. Hall exit**: the exit glow, burst and arrow wait for the shard to be taken (`KeeperHall.ShardTaken`), not just the calm.
Also fixed a save gap: a save between the calm and the pickup used to lose the shard for good (calmed = no rise). Now the
shard rises again on entry or load.

**F. Ghosts**
- Swipe (and any `thumb_*` demo over the island): beside the front-right rim (left for left-handed), **1.6x**, never over
  the map, a glowing gold dot plus a short trail on the thumb tip.
- Evolve ghost: **my bug, older than this step.** The placeholder bake assumed the hand root's fingers point along +Z. On
  this rig they point along -Y, so "palm up" rendered as a vertical palm facing you. The two palm-up clips (`grab_palm_up`,
  finale `palm_lift`) are corrected. The evolve one now ends palm flat and up, cupped, about 10 cm above the rim's front
  edge. Other placeholder clips are unchanged (they were tuned by eye against the old mapping).
- Move and two-hands lessons: the Spirit and bubble park above the island's **back centre**.

**H. Hall of Fame**: a front arc round the statue (`FinaleLogic.HallArc`): centres ≥ 1.4x the widest Isling apart, ≥ 2 tiles
plus half a body from the statue, never more than 100° off the front. Cards are **1.8x** with lighter stone and darker ink,
and they grow upward so they clear the heads. A layout check raises a card that would overlap a neighbour.

**G. Tilt (found, then fixed)**
- New pose log: every island-root change over 2 mm or 0.3° in its parent, with the writer.
- Lean-in camera on the bridge: 28 cm out, 22 cm up from the front, shifted sideways.
- Result: 4–5 cm sideways did nothing. **9–10 cm sideways turned the island 10–12° (yaw only) about 1.5 s later**, from the
  idle facing turn (head more than 6° off the front for 1.5 s). Pitch and roll were 0.0 every time.
- So the "tilt" is that turn. `IslandHandle.idleReface` is now **off**. The island root moves only by a grab, a recenter or
  the first placement. It still faces you while held and after a release.
- Accidental grabs were not the cause, so hand puzzles keep island grab.

**I. Shard bursts**: touching a Keeper's shard and slotting one into the statue replay the opening's shard burst, tinted
per element, with one flourish: wind a swirling ring and leaves, fire rising embers, water a ripple ring and bubbles. About 70
particles, under 1.5 s, plus the `shard.touch` chime on the Keeper shard.

**J. Credits**: a wide slate banner behind the island's back edge (0.5 m at scale 1), the lines rolling up through it
(4 slots, bigger type, 13 s, Skip works).
- **Standee: only when `Assets/_Project/Art/Credits/Mugesh.png` exists.** That file isn't there. You dropped
  `character.png` (you waving, with a VR headset) and `Home_char.png`. I didn't rename your files. To use `character.png`,
  rename or copy it to `Mugesh.png`; the build and Play pick it up (`CreditsStandeeHook`).
- The preview below used `character.png`. Brand check: the headset in the picture has no visible logo, but it looks like a
  real headset model. Glance at it against the "no branded products" rule.

**K. Dev panel**: closes by itself on a battle start, a scene or realm swap, and the finale start.

## Shots (Logs/BattleShots)
- `17_menu.png`: pause row along the rim. `17_title.png`: title row.
- `17_pods.png`: pod 2 focused, neighbours in place.
- `17_kites.png` / `17_kites_battle.png`: kites before and during a battle.
- `17_swipe.png`: thumb ghost beside the rim with the gold dot.
- `17_evolve_end.png`: evolve ghost end pose, palm up.
- `17_credits_a.png` / `17_credits_b.png`: banner and standee preview.

## Headset test list (play order)
1. **Title**: the row sits along the rim, tilted back. Lean left and right: it stays put.
2. **Move / two-hands lessons**: the Spirit and bubble float above the back middle, not on a corner handle.
3. **Swipe lesson**: a bigger ghost hand beside the front-right rim with a gold dot on the thumb. Nothing over the map.
4. **First battle**: reach for the cradle pod. The grab frame stays dark and the pod comes to your hand.
   - Flick it roughly at the creature, even 45–60° off: it flies in.
   - Drop it on purpose: "Missed! Grab the big pod and flick it again." and the pod pulses.
   - Poke the Spirit mid-battle: no "Follow my gold trail".
5. **Rim pods**: look at a pod. It grows a little and pops forward; its neighbours don't move.
6. **Pause menu**: the row lies along the rim. Lean in and to the side: it doesn't shear.
7. **Dev panel**: open it, walk into grass. It closes when the battle starts.
8. **Galewick**: fly kites, then get into a battle. Kites and strings vanish quickly and come back after.
9. **Fine work** (mill flick, finale): lean in close and to the side. The island must not turn.
   - Log: no "[IslandHandle] Root moved ... idle facing turn".
10. **Keeper calmed**: no exit glow while it talks. Touch the shard: a tinted burst and chime, then the exit shows.
11. **Statue**: slot a shard. The tinted burst with its flourish plays.
12. **Evolution**: the ghost hand ends palm up, flat, just above the rim's front, not a vertical "stop" palm.
13. **Finale Hall of Fame**: the Islings stand in an arc with no overlaps, and the cards are readable from the seat.
14. **Credits**: a banner behind the island, lines rolling up. The standee shows only once `Mugesh.png` exists.

## Not done or open
- Nothing skipped.
- Not seen in shots: the Hall of Fame arc (covered by tests and the smoke run) and the shard bursts (log only).
- The swipe ghost's exact offset and the 35° rim tilt are guesses from flat shots. They are fields (`thumbRimOffset`,
  `thumbScale`, `rimTilt`) if the headset says otherwise.

## Smoke run (final state, 14:27)
**84/84 beats ok, 0 failed, 6.6 min.** Two notes, neither from this step:
- Tidemother's HP label sits inside its box: the same known note as Nov 5 and Nov 6 (huge Keeper box). Check on the headset.
- The opening took 51 s: the move lesson waits up to 45 s for a real island grab, and flat Play can't grab, so it times out.
Hall of Fame in the run: "2 Islings, widest 1.3 tiles, closest centres 1.9 tiles (need 1.9), radius 2.7 tiles".
Saves are restored and XR init is set back on.
