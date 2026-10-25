# Step 8c: polish the first 10 minutes (Mugesh's Notes.md, triaged)

Read CLAUDE.md, `.claude-handoff.md`, `Assets/Docs/Notes.md` (Mugesh's test notes, Oct 25) and step-08b-onboarding.md.
8b + the ghost recordings tested well. Judges play ~10 minutes, so everything in the opening, tutorial and first
lodge visit is worth fixing now. The rest goes to the backlog (section F). Decisions below are final unless a
technical blocker comes up (then ask).

## A. Opening
1. **Spawn higher:** island (and the shard) spawn at **chest height**: ~35 cm below the eyes (was 55), ~45 cm
   ahead. Same for Quest recenter. Check the adaptive gaze still lands well.
2. **Shard is poked, not proximity-triggered:** remove the proximity trigger. Use an ISDK poke interactable (or a
   fingertip-vs-sphere contact) on the shard. Real hands can't be stopped, so sell the contact: on touch the shard
   **gets pushed back a little and springs back** (damped spring), a light flash at the contact point, then it
   cracks. Palm touch and look + pinch still work.
3. **Shard motion glitch:** its spin restarts when it switches from "drifting in" to "hovering". Keep one
   continuous rotation (accumulated angle / angular velocity carried across states) and blend the bob in.
4. The **"touch it" line** shows ~1.5 s after the shard settles (it waits too long now).
5. **Shader warmup:** warm up the shard, fx orb, portal and pod variants (ShaderVariantCollection) while the shard
   drifts in, to remove the 97 ms hitches measured in the opening.
6. **Starter pods** stay big (current float size) while floating and while held; they shrink to rim size only as
   they snap into the socket (0.3 s ease).

## B. Ghost hands and hints
7. **Ghost size at targets: 75% of real hand size** (the above-island thumb demos stay as they are; Mugesh likes
   them). One tunable in the Ghost Clips settings.
8. **Ghost start/end points:** if it can be done safely, tighten each clip's key point onto the current target
   (pod centre, rim edge, rune face, cradle). If it risks breaking clips, leave it and add it to the backlog.
9. **Attack lesson = poke:** the line becomes "Poke a rune to attack!" and the ghost does a poke at the nearest
   rune. Look + pinch still works; it's just not what we teach first.
10. **Lantern beam:** the thin ray line looks bad. Replace it: no line. The Spirit's lantern brightens, and a soft
    gold **light pool / halo** sits on the target (on top of the gold outline). Optionally a very short, faint cone
    fading out right below the lantern. Nothing that crosses the scene.
11. **Two-hand island tip** (contextual, not in the opening chain): after the first battle is over, once, a short
    lesson: "Use both hands to turn or resize the island." with a both-hands ghost at the rim (pinch-apart / rotate).
    Done when the player does a two-hand grab, or skip after 15 s.

## C. Rim pods vs rim grab
12. The **pod card** follows its pod (parent it to the island), sits higher so it doesn't hide the pod, and
    **closes when the island is grabbed or moved**.
13. **Grab zones:** island grab colliders no longer cover the pod band. Grab works on the sides, back and the two
    front corners; the front strip with the 6 sockets is pods only. Keep the rim hover-brighten on the grab zones
    only. Make sure a hand reaching for a pod never starts an island grab.

## D. Shrine and Lodge
14. **Palm over the shrine works whenever the shrine is visible on the island**, not only when the explorer is next
    to it: palm over its flame, the ring fills, the explorer walks there by itself, then you go in.
15. **Walking up no longer auto-enters.** Remove "walk into the doorway = enter". Standing next to it makes the
    flame lean toward you and glow (and the first time, the Spirit says "Warm your hand over the flame."). Look + pinch
    on the shrine still works (accessibility).
16. **First lodge visit when the lodge is empty** (usual case: the caught Isling is in your rim pods): the chain
    adapts. No auto-dropping.
    1. "This is the Hearth Lodge. Islings you leave here rest by the fire." → room centre
    2. "Toss a pod onto the floor to let one rest here." → a rim pod, ghost `grab_toss` toward the floor
    3. after it pops out: inspect ("Look at it and pinch to see how it's doing.") → the Isling
    4. take back ("Pick it up and set it in a pod on the rim to bring it along.") → pedestal, then a socket
    5. exit → door arch
    If the lodge already has Islings, use the existing chain.
17. **Bug:** look + pinch on a wandering lodge Isling doesn't select it (only pinching directly on it works). Fix
    so the gaze cone picks lodge Islings like everything else.

## E. Housekeeping
- One compile batch per section; EditMode tests green; flat Editor smoke test with New Game.
- Commit per section with "(headset test pending)". No AI attribution lines. Don't push.
- In `Notes.md`, mark each handled note "→ done in 8c" (don't delete Mugesh's text). Update `.claude-handoff.md`.
- Device build at the end; quick perf check of the opening (confirm the hitches are gone).

## F. Backlog only (add to PolishBacklog.md, don't build)
- **Lock island** toggle (avoid accidental grabs): goes into the Spirit menu.
- Ghost start/end points per target, if not done in item 8.

## Test on the headset
1. Launch: the island and shard appear at chest height; the shard spins smoothly; "touch it" comes quickly.
2. Poke the shard: it gets nudged and springs back, flashes, cracks. Hovering a finger near it does nothing.
3. Starter pods stay big until they snap into the rim.
4. Ghost hands at targets are smaller; the attack lesson teaches poke.
5. Hints show a soft gold light pool, no ray line.
6. Reaching for a rim pod never grabs the island; the pod card stays on its pod and closes when you move the island.
7. From across the hub, palm over the shrine: the explorer walks there and you go in. Walking up just makes it glow.
8. First lodge visit with an empty lodge: the Spirit asks you to toss a pod in, then inspect, then take back.
9. Look + pinch on a lodge Isling selects it.
10. After the first battle: the two-hands tip shows once.
