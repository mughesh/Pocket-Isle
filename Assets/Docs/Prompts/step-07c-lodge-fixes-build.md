# Step 7c: lodge fixes, one focus language, then a device build + perf gate

Read CLAUDE.md, step-07b-hearth-lodge.md, PolishBacklog.md. Mugesh tested 7b on Oct 24 night: palm-over-flame
entry and the lodge pod swap feel good ("cute"). Fixes below, then **the first real device build and perf check**.

## A. Getting in and out
1. **Door:** make it a **2-tile-wide glowing archway** on the lodge's front edge (the edge nearest the player),
   tall enough to read (about 6 cm at island scale 1). Its gaze/poke volume covers the whole arch plus a margin.
2. **Leave three ways:** walk onto either door tile; **look + pinch the arch** (explorer walks there and exits);
   **hold the palm over the arch 0.8 s** (same gesture and filling ring as entering, so it's symmetric).
3. **Enter three ways:** palm over the flame (keep), look + pinch the shrine (keep), and **walk into the hut's
   doorway tile** (new; the way players expect buildings to work). The doorway faces the trail.
4. **Gaze picking, everywhere:** replace the thin ray with a **narrow cone** (about 2° half-angle, a sphere cast
   sized by distance); among hits, pick the one nearest the ray centre, with a small stickiness bonus for the
   current target (no flicker between neighbours). Every selectable gets a gaze volume of at least about 4 cm at
   island scale 1. Applies to tiles, pods, runes, cradle, door, shrine, lodge Islings.

## B. One focus language: "the Spirit shines its lantern on it"
Problem: the Spirit sometimes hovers over the target and its big bubble covers it, so you can't tell what to pick.
5. **Gold = do this now.** When a line has a target, the target gets a **gold pulsing outline** (the outline
   shader's colour and width through a property block) and the Spirit **shines a soft lantern beam** on it: a thin
   additive cone from the Spirit's lantern to the target, warm gold, gentle shimmer. One shared `FocusHighlight`
   used by every hint.
6. **White = what you're looking at.** Gaze hover on any selectable = a thin white outline/rim (keep the existing
   rim brighten for the island). Never gold for hover, so the two can't be confused.
7. **The Spirit never blocks the target:** it parks **beside** the target (left or right, whichever side is more
   open from the player's view), never above or in front of it. The bubble goes on the **far side of the Spirit
   from the target**, smaller (max 2 lines, about 60% of today's width), with its tail pointing at the Spirit.
   Check overlap in the player's view: if the bubble's screen rect touches the target's bounds, push it away.
8. After the line ends, the gold stays on the target until it's used or 6 s pass (then a softer pulse every
   few seconds while the step is still pending).

## C. Lodge guidance (first visit, step by step)
9. A short **hint chain**: each line waits for its action, re-prompts after about 15 s idle, never repeats once
   done. Anchors use the gold focus from B.
   1. `lodge.first` → room: "This is the Hearth Lodge. Islings you've caught rest here."
   2. `lodge.inspect` → nearest wandering Isling: "Look at one and pinch to see how it's doing."
   3. `lodge.take` (after the first inspect) → the Isling on the pedestal, then the first empty or swappable rim
      socket: "Pick it up and set it in a pod on the rim to bring it along."
   4. `lodge.leave` (after the first swap) → a rim pod: "To leave one here, toss its pod onto the floor."
   5. `lodge.exit` (after step 4 or 30 s) → door arch: "The door takes you back outside."
   Add these to Script_v1.md (Islings wording) and reimport.

## D. Lodge capacity and wild spawns (cheap versions)
10. `Lodge Room > maxInRoom` = **8**. The rest sleep in the honeycomb (show up to 24 cells; beyond that, a small
    page arrow on the honeycomb, poke or look + pinch). Pinch a honeycomb pod = that Isling hops out and the one
    that's been wandering longest goes to sleep.
11. **Duplicates are fine** (as in the classic games): caught species keep spawning. One cheap tweak: in encounter
    rolls, **species not yet caught get 2× weight**, and the first time you meet an uncaught species a small
    "New!" sparkle shows at zoom start. Uses the saved caught-species set.

## E. The "more materials than submeshes" warning
12. It's the inverted-hull outline (the second material redraws the mesh), not a bug. Clean it up anyway: move the
    outline into `Diorama/Toon` as an extra pass with `LightMode = SRPDefaultUnlit` so each renderer has **one
    material**. Keep the per-renderer outline toggle (property block or keyword). Rebake/refresh prefabs and
    props; check the look is identical (outline width and colour, eyes, pods). Same draw count, but SRP Batcher
    friendly and no warning.

## F. Device build + perf gate (do this last)
13. Build the APK (Meta Quest build profile, IL2CPP, ARM64; development build off for the perf numbers), install
    over USB, launch. Use the **Meta VR CLI / meta-quest agentic tools** to capture, about 30 s each:
    | Scene | What to do |
    |---|---|
    | Outdoor walk | Walk and run across the test map, unrestored |
    | Restored | Same with G / 3 taps (embers, steam, lit lanterns) |
    | Battle | Zoom in, send out, 3 moves, a pod shatter, a catch |
    | Swap | Enter the lodge and leave again (transition frames) |
    | Lodge | 8 Islings wandering (use debug to fill storage) |
14. Report a table: avg / 1% low fps, frame time (CPU / GPU), draw calls, triangles, memory, for each scene. Target
    **72 fps**, never under 60. If anything misses, fix the biggest cause first (likely lodge Islings or embers)
    and capture again. Don't guess; measure.
15. Also check: cold start to playable (seconds), pause/resume by taking the headset off for 30 s, quit and
    relaunch restores the save, `adb logcat` for errors and exceptions during all of the above (list any).
16. Ignore the Editor crash with "Initialize XR on Startup" and no headset (known, in the handoff).

## G. Backlog only (don't build now)
17. Add to PolishBacklog.md under "Lodge life (do with scene filler props)": poke the hearth to stoke it (embers
    burst, Islings gather round); pet an Isling with an open hand (it leans in, little hearts); a berry bowl to
    fill; a toy ball you toss that Islings chase; wind chimes / hanging lanterns you can flick.

## Housekeeping
- One compile batch per section; EditMode tests green; commit per section with "(headset test pending)".
- When the device build is stable and Mugesh has tested, he'll say so and you **move `pre-goa`** to that commit.
- No AI attribution lines. Don't push. Update `.claude-handoff.md`. End with a short summary + the perf table.

## Test on the headset
1. Leave the lodge by walking to the big arch, by look + pinch on it, or by palm over it. All easy.
2. Walk into the shrine's doorway: you go in.
3. First lodge visit: the Spirit walks you through inspect → take → leave → exit, one step at a time.
4. Every hint: the target glows gold, the Spirit's lantern beam points at it, the bubble never covers it.
   Looking at anything gives a thin white outline instead.
5. Gaze picks tiles and pods more easily than before.
6. Uncaught species show up more and get a "New!" sparkle.
7. Islings look exactly the same as before (outline merged into one material).
8. The perf table shows 72 fps everywhere.
