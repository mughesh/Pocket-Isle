# Step 5e: island always faces the player, recenter fix, gaze offset, rune tilt

Do this after 5d is tested and committed. Feedback from Mugesh's headset test (Oct 24). These matter for every
later step: the rim pods, the Lantern Spirit and the runes all sit on the island's **front** (player) side.

## 1. Island front always faces the player
- Define the island's **front** = its near side (+Z or whichever side the Spirit/runes use now; keep one constant).
- **While grabbing (one hand):** position follows the hand as now; **yaw turns so the front faces the head**
  (yaw only, smoothed ~0.15 s). Stays level.
- **Two hands:** uniform scale only. Remove two-hand yaw (it would fight the auto-facing).
- **Not grabbing:** if the head is more than 25° off the island's front axis for over 1 s (player shifted or turned
  in the chair), ease the yaw to face the head over 0.6 s. **Never during battle or while a tile touch, rune poke
  or grab is active.**
- The map rotates with the island, so "forward" on the map stays "away from the player". Swipe directions stay
  relative to the player's view as now; check they still match after the island turns.

## 2. Recenter fix
- Quest recenter mostly puts the island to the player's **right**, not in front. Likely the head pose is read
  before the tracking space settles (MRUK world lock / TrackingSpace update after recenter).
- Fix: on recenter, wait until the centre-eye pose is stable (2–3 frames, or until it stops jumping), then place
  45 cm in front along the head's forward **projected on the horizontal plane**, 55 cm below the eyes, front facing
  the head. Log head pose, forward and placement. Test facing 4 different directions in the room.

## 3. Gaze aim offset
- The island sits below chin level, so head gaze needs too much neck tilt. Aim the gaze ray **below** head
  forward by a pitch offset: default 15°, exposed. Better: adaptive. Take the angle from the head to the island
  centre, and use an offset that covers most of it (e.g. 70% of that angle, clamped 0–30°), so looking roughly
  at the island lands on it. Applies to the reticle for runes, bubbles and look-to-walk tiles. The reticle shows
  where the gaze actually lands.

## 4. Rune panel tilt
- Tilt the rune row back (pitch around its X axis) so its face points up at the head, clamped 0–40°, set at battle
  start along with the yaw (yaw drift stays ±15°). Easier to read and poke from above.

## Housekeeping
- One compile batch. Don't commit until Mugesh tests. Update `.claude-handoff.md`.

## Test on the headset
1. Grab the island and carry it left and right: its front keeps turning toward you; pods/Spirit side always yours.
2. Shift in the chair or turn ~40°: after a second the island eases around to face you. Not during a battle.
3. Recenter while facing 4 different directions: the island lands straight in front of you every time.
4. Look roughly at the island without bending your neck much: the reticle lands on it.
5. Battle runes face up toward you and are easy to read and poke.
