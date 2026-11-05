# Step 16: polish from the judge-panel review of the Nov 5 Quick Journey video + Mugesh's Nov 5 notes

Triaged Nov 6, 1 AM (design chat). The design chat checked the review against the video (frames + audio measured):
what's below is confirmed or decided; the review's other points were rejected on purpose (see "Not doing").
Orient first if you're a fresh session (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Overnight-Nov5.md`,
`Assets/Docs/Notes.md` Nov 5 section). Commit per section "(headset test pending)", no AI attribution.

## A. Ghost hands never hide what you're reaching for
1. Every lesson/hint ghost hand fades to ~8% alpha when either real hand (index tip or palm) comes within **~16 cm** of
   the lesson's target, and back when the hand leaves (0.2 s). Seen hiding the starter pods (0:46–0:53), the pod toss
   (1:21) and the statue during the finale lift (10:05: two huge ghosts over the statue).
2. The finale lift ghost and the evolution/shard ghosts: smaller, offset to the side of the target, never between the
   head and the target.

## B. Jingles: quieter and warmer (measured)
3. In the video the catch / level-up / Keeper win / evolve / shard jingles peak **15–16 dB above the game's median
   loudness** and carry about half their energy above 3 kHz (ambience is ~0.3). Lower `jingle.catch`, `jingle.levelup`,
   `jingle.evolve`, `jingle.shard` (and any victory jingle) by **~9 dB**, soften them (warm sine/triangle or mallet
   body, low-pass ~2.5 kHz, shorter tails) via the Audio Lab synth recipes, re-bake, AudioScope pass. Music ducks under
   them less. Report before/after levels.

## C. The Spirit's bubble stays out of the way
4. Mugesh: when a pod must be picked and tossed, the line appeared right over his own Isling and hid the drop spot.
   Video: bubbles crossing the windmill (2:30) and kites (3:02); the evolution card colliding with a bubble (8:46).
   Rule: the bubble (and the Spirit) avoid the **current objective's area** (target + drop zone + the hand's path) and
   the battle pads; prefer the **right side** of the island (Mugesh), then left; never inside props.
5. While a focal card is up (evolution card, realm name card, Journal, Settings, end card, credits) the Spirit's
   chatter waits; objective chips stay.
6. Typing: lines often show half-typed in captures ("This is", "The bridge is"); type faster (~60 characters/s) and
   show lines under 30 characters instantly.

## D. Shard to the statue (Mugesh's notes)
7. After a Keeper win the shard floats **higher**, never behind the hall's entry arch; it goes to the Spirit by
   **touch** (fingertip or palm within ~6 cm) as well as grab; new line "Touch the shard!".
8. Back on the Still Isle, the "put the shard in the statue" offer only starts when the explorer is **near the statue**
   (walk-up rule); until then the Spirit's trail leads there. The island view **focus-locks** on the statue for it (the
   hand-moment focus lock), and the **explorer is locked** while the statue whispers and the Spirit talks (no walking
   into the lodge mid-scene).
9. Forgiving placement: releasing the shard within ~12 cm of the socket snaps it in; the Spirit lift to the forehead
   crystal: within ~12 cm snaps. (Targeted, not a blanket collider increase.)

## E. The first minute
10. While only the shard floats in the room, **no island ambience or music** (today you hear the island before it
    exists). Instead a soft, curious **shard hum** (a quiet glassy shimmer with a slow pulse that gets a little brighter
    as a hand approaches), then the island's sound starts as it unfolds. Synth recipe in the Audio Lab.
11. Swipe lesson: show the swipe ghost hand **side-on, palm facing inward/down** (not a palm facing the headset: in the
    video a palm toward the face shows the Meta system button, and the system menu opened at 0:58). Line: "Swipe your
    thumb along your finger to walk." Remove "tap to stop" from the lessons (the walk stops by itself; tap still works
    and stays on the Controls card). Add a swipe ghost if missing.
12. Pause: keep the left-hand menu gesture (that's the gesture Meta gives apps), and **also** add a small **menu tablet
    on the rim** (front-left, a gear/"≡" icon, never overlapping pods or the lodge Journal tablet; layout checker). The
    menu line becomes "Poke the stone on the rim, or palm up and pinch with your left hand, to pause."

## F. Small fixes from Mugesh's notes
13. **Hand stuck on a menu button** until the menu closes: find and fix (a poke/select that never releases).
14. **Meta recenter:** the island blinks out and back; instead glide it to the new spot (~0.4 s) after the settle.
15. **Tall grass:** many small clumps never have Islings. Every tall-grass clump of ≥ 2 tiles can spawn; raise the
    encounter chance per step in grass moderately (report old/new); keep the Quick Journey's x0.5 on top.
16. Two-hand resize lesson passes after 8 s without two hands (one-handed players never get stuck). Galeowl's knots
    already allow one hand: verify.

## G. Readability
17. Battle line ("What will Pebbloop do?", "X used Y!"): a dark rounded backing pill with cream text, a bit larger.
18. Quick Journey end card runes (Keep exploring / See the ending / Title) were tiny above the statue: old tablet size
    rules, readable, in front of the statue.

## H. The ending: shorter and graceful
19. Credits: **one plaque** (dark slate, gold trim, readable) above the statue showing the credits as a short scroll
    (≤ 20 s total), not speech bubbles. After the spark returns, a final card "Thank you for playing Pocket Isle" with
    **Keep exploring** / **Title**; the scene never ends on a static island. A **Skip** tablet appears from the Hall of
    Fame onward. Total ending ≤ ~2 min.
20. Credits text: no brand names except what a licence requires. Until the paid sound regeneration, keep the required
    "Sounds made with elevenlabs.io (free plan)" line; after step 15b it becomes "Sounds: synthesised and generated
    for Pocket Isle; some sounds by Kenney (CC0)". Remove "Unity" / "Meta XR SDK" lines.

## I. Looks
21. **Hearth Lodge dressing** (it reads like a prototype): a round woven rug (cream/terracotta/sage, toon + outline) in
    the middle, plank seams on the floor, warm stone tiles round the hearth, a warm glow disc on the floor from the
    hearth, low props along the side walls (small bookshelf, cushion stack, potted fern, side table with a lantern,
    chest) leaving the centre free for Islings. Prop recipes, palette-swapped, ≤ 4 extra draws.
22. **Grounding:** a soft permanent glow/shadow disc under the island's rocky underside (like the finale's ring, much
    subtler) in every scene. Soften the bright white grab-frame line when not hovered (warm, translucent).
23. Confirm 4x MSAA on the Quest URP asset (report; don't raise render scale).
24. Battle feel: see section J (moved up in priority).

## J. Battle spectacle (battles are ~40% of play time; judges felt creatures "barely move")
Do this right after E (before F). Each move should read clearly from the player's seat, ~1.2–1.8 s per move, never
slower than today's battle pace by more than ~25%. All island-scaled, pooled particles, ≤ 0.5 ms, no new draw-heavy
systems. Use the four existing move templates (shoot, lunge, slam, shield) and the element colours.
25. **Alive on the pad:** idle breathing/bob, a little sway, blink, and a glance at the opponent every few seconds;
    the wild one reacts when your Isling arrives (hop or flinch). Keepers keep their fury idle.
26. **Wind-up → action → impact → recover** for every move:
    - wind-up 0.25 s: crouch + squash, element glow building on the body, a small charge sound (`move.charge`);
    - shoot: the projectile is bigger and readable (fire ember ball with a trail, water bubble that wobbles, wind
      crescent that spins, neutral star pellet), travels in ~0.35 s with a slight arc;
    - lunge / slam: a real hop toward the target (~0.1 m) with stretch, contact, bounce back; slam lands with dust and
      the pad shakes;
    - shield/support: a visible bubble/ring that pulses on the user, stat-up arrows for buffs;
    - impact: hit-stop (60 ms), white flash 0.1 s, knockback wobble on the defender, element burst ~1.5x today,
      floating damage number in the element colour ("Super effective!" in gold, "Not very effective" small grey).
27. **HP bars** drain smoothly with a lagging "ghost" segment; low HP (< 25%) pulses red softly.
28. **Faint:** a little spin and flop, sparkles, then it fades; the winner does a happy hop.
29. **Keeper moves** get one signature flourish each (Hearthstag crown flare, Galeowl wing gust that ripples the pads'
    grass, Tidemother wave that splashes across the arena).
30. Shots: a frame sequence of one move per template + one Keeper move per realm (`16_battle_<move>_<n>.png`), and the
    battle pace measured (seconds per move before/after).

## Not doing (decided)
- No blanket "2x every collider" (Mugesh: the windmill was easy; doubling small, close objects makes them steal grabs).
- No change to the left-hand pause gesture itself (it's the app menu gesture Meta passes to apps).
- No render-scale increase, no MRUK table snap, no island-scale buttons.

## Housekeeping
- EditMode tests green; the smoke run passes; the layout overlap checker passes. Update `.claude-handoff.md`,
  GameDesign.md, QA-Guide; `Run-16.md` with shots and the headset test list in order (first minute first).
