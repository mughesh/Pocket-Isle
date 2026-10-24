# Step 5e: island always faces the player, gaze offset, rune tilt, recenter fix

5d passed (commit it if not done). Headset feedback, Oct 24. These are showstoppers for step 6 (capture pods sit on
the front rim, the Spirit is at the front), so fix them before moving on. Small step, one compile batch.

## 1. The island's front always faces the player
Define the island's **front** = the rim side where the Spirit, runes and (soon) the pods live.
- **One-hand grab:** position follows the hand as now, and the island **yaws so its front faces the head**
  (head position projected on the horizontal plane), smoothly (~0.25 s). Stays level.
- **Two-hand grab:** scale only (0.5–2× as now) + the same auto-facing; no manual yaw.
- **On release:** finish easing to face the head.
- **Idle:** if the head ends up more than 35° off the front for over 1.5 s (player shifted in the chair), ease back
  to face them over ~1 s. Not during battle zoom. Expose the angle/time and an on/off toggle.
- Check that swipe directions stay correct relative to the player's view after the island turns (swipe left =
  the explorer moves to the player's left), and that the battle layout's "toward the player" still works.

## 2. Gaze aims lower
The island usually sits below the chin, so aiming the head straight at it is a strain.
- The gaze ray (tile walking, runes, bubbles) = head forward **pitched down by an offset**. Default 18°, exposed.
- Better if cheap: make the offset adaptive, e.g. most of the angle between head forward and the direction to the
  island centre, clamped to 0–25°, smoothed, so it feels right at waist height and at eye level.
- The reticle shows exactly where the offset ray lands.

## 3. Rune panel tilts toward the eyes
- Pitch the rune panel back so its face points at the head (rotate on its X axis), clamped 0–40°; the yaw rule
  from 5d (±15° drift) stays.

## 4. Recenter puts the island to the side (bug)
- After a Quest recenter the island lands to the player's right instead of in front. Likely the placement reads the
  head pose before the recenter (or the MRUK world lock / TrackingSpace move) has applied.
- Fix: on recenter, wait until the head pose and TrackingSpace have settled (a couple of frames, or until the head
  pose changes), then place 45 cm in front along the **flattened head forward**, 55 cm below the eyes, front facing
  the head. Log head pose before/after so we can see it. Test recentering 5 times from different seat angles.

## Housekeeping
- Don't commit until Mugesh tests. Update `.claude-handoff.md`.

## Test on the headset
1. Grab the island and drag it left/right around you: its front keeps facing you.
2. Turn your chair 45° and wait: the island eases round to face you.
3. Look at a tile below your chin with a comfortable head tilt: the reticle lands on it.
4. Battle runes face your eyes, easy to read and poke.
5. Recenter 5 times from different angles: the island always comes back in front of you.
6. After the island turns, a left swipe still moves the explorer to your left.
