# Step 8e: right-hand bug, dominant hand, pinch grab back with context modes, starter pods inside the island

Read CLAUDE.md, `.claude-handoff.md`, step-08d-grab-touch-fixes.md. Mugesh tested 8d in Editor Play over Link
(Oct 25 night). **Test in Editor Play over Link for this step; no device build needed.**

## A. Right hand doesn't work in the opening and the lodge (bug)
Symptom: shard poke, palm over shrine/door, and pinching lodge Islings work only with the **left** hand. Everything
outside the opening and lodge works with both hands.
Suspect: `HandLookup.Find` returns the first non-synthetic `Hand` of that side that feeds nothing. The **ghost-hand
copies default to the right hand**; if a copy keeps any `Hand` (or IHand data source) component, it qualifies, and
every system whose first lookup happens after a ghost exists caches the ghost instead of the real right hand
(`Refresh` never re-finds while the cached one isn't null). That fits: ghosts show in the opening and the lodge.
1. **Confirm first:** log which `Hand` object (full hierarchy path) each system resolves for each side, in the
   opening and in the lodge. Put the result in the summary.
2. **Fix at the root:** one `HandRegistry` resolved once from the rig itself (the hands the rig's ISDK interactors
   use, found under the interaction rig root), never via a scene-wide `FindObjectsByType`. Every system
   (`Opening`, `PalmHover`, `LodgeRoom`, `PodRack`, `BattleRunes`, `DialogueBubble`, `IslandHandle`,
   `IslandGrabFrame`, `MicrogestureInput`, `Onboarding`, `GhostClipRecorder`) reads from it.
3. Ghost hands: make sure the copies carry **no** `Hand`, data source, interactor or collider components (mesh
   + our ghost driver only). Add an EditMode or Play test that fails if a ghost has any `IHand`.
4. If a resolved hand stops being valid (rig rebuilt), re-resolve from the rig, never from ghosts.

## B. Dominant hand from the first touch
5. Whichever hand touches the shard first becomes the **dominant hand** (saved in the save file/settings).
   Ghosts are shown as that hand by default (mirrored clips), and single-hand hints use it.
   Before the shard is touched, ghosts show the right hand.

## C. Island grab: pinch is back, with context modes (decided Oct 25)
Mugesh: pinch-grab is the Meta norm and feels better for a small island than a full-hand grab (also for two-hand
zoom). Rim pods only need to be grabbable in battles and in the lodge.

| Mode | When | Rim pods | Island grab (pinch **or** palm) | Grab frame |
|---|---|---|---|---|
| **Explore** | Default | **Not grabbable or pokeable.** Look + pinch = stats card only | **Whole base skirt** all around, including the front pod strip | Whole perimeter |
| **Pods active** | Battle (from encounter start to zoom-out) and inside the Lodge | Grab / toss / swap as now; pods pop out ~3 mm with brighter brass to signal "grabbable now" | Back, sides and the two **front corners** only (not the pod strip) | Only the grabbable parts light up |

6. Island `HandGrabInteractable`: support **Pinch and Palm** again. Two-hand pinch = yaw + scale, as before.
7. **Accidental grabs from look + pinch** (the original problem): tighten the grab volume to a thin shell around
   the base skirt (about 2.5 cm out from the rock surface, not over the top cap or the play area). A pinch counts as
   an island grab only when the **pinch point** is inside that shell. Otherwise it's a gaze select. If the pinch
   point is inside a shell **and** the gaze is on a selectable, the grab wins only if the hand had hovered there
   (frame lit) for ≥ 0.15 s before the pinch. Log every resolved pinch (grab vs select) in the dev readout.
8. The grab frame lights only when the pinch point / palm is near a grabbable zone, so the player sees a grab coming.
9. Mode switches are instant and safe mid-grab (a grab in progress continues; new grabs follow the new mode).
10. Ghosts: the move-island lesson and two-hands tip go back to **pinch** grabs on the base sides (Mugesh's
    `grab_rim_slide` recording fits again).

## D. Starter pods stay over the island (my 8d spec was wrong: 1.2× the width pushed them outside)
11. The three pods float **inside the island's footprint**: a shallow arc across ~0.7× the island's width, ~10 cm
    above the cap, over the middle of the island, slightly toward the player. Shrink their float size until the arc
    fits with gaps of ≥ 0.4 × pod diameter. Gaze grow 1.25×, with neighbours sliding apart but staying over the island.
12. Recompute from the island's current scale each frame (they must stay inside if the player resizes the island).

## Housekeeping
- One compile batch per section; EditMode tests green. Test in Editor Play over Link (headset on before Play).
- Commit per section with "(headset test pending)". No AI attribution. Don't push. Update `.claude-handoff.md`.
- Summary: what A's logging found (which object was resolved), what changed, what to test.

## Test (Editor Play over Link)
1. Right hand: poke the shard, palm over the shrine and the lodge door, pinch a lodge Isling: all work. Same with the left.
2. Touch the shard with your left hand: ghosts afterwards show a left hand.
3. Exploring: pinch the base anywhere (even under the pods) to move the island; two-hand pinch to turn/resize.
4. Look + pinch on tiles and pods while your hands are near the island never moves it.
5. Exploring: rim pods only show their card on look + pinch; reaching into them does nothing.
6. Battle and lodge: the pods pop out a little and can be grabbed; the island moves from the sides, back and front
   corners; the grab frame never lights over the pods.
7. Starter pods float over the island, never outside it, with clear gaps.
