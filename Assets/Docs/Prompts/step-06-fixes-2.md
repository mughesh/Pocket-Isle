# Step 6 fixes 2: no Catch rune, pod always in hand, pods grow on gaze, gaze lower

Read CLAUDE.md, step-06-capture-party.md and step-06-fixes.md. Mugesh tested step-06-fixes on Oct 24. The card,
hints, theme and catch rule mostly work. Changes below.
**First, commit the current step-06-fixes work as it is** ("Step 6 fixes: ..."), then do these as a new commit.

## A. Runes only once your creature is out
1. At battle start, before your creature lands, show **no move runes**. Only the lead pod glows (plus the Spirit
   hint pointing at it) and a small **Run** rune.
2. When your creature lands on the near pad, the 3 move runes pop in, one after another (about 0.1 s apart).
3. Same on a switch: runes hide while the pod flies, and come back with the new creature's moves.

## B. Drop the Catch rune: the capture pod is just there
4. **Remove the Catch rune.** The row is 3 moves + Run.
5. The empty capture pod appears in the cradle **as soon as the wild creature is visible** and stays for the whole
   battle. It can always be grabbed and thrown. Look + pinch on it = auto throw (accessibility, unchanged).
6. The cradle pod shows the catch state, so the rune isn't needed:

| Wild HP | Cradle pod | Throw result |
|---|---|---|
| above red (> 25%) | Plain glass, no glow | Pod hits, wobbles once, **breaks**: creature bursts out. Spirit: `capture.locked` ("Weaken it first!") the first time. Uses your turn |
| red (≤ 25%) | Warm glow | Chance roll (`CatchChance`), 1–3 wobbles; on a fail it **breaks**. Uses your turn |
| dazed (fainted, 8 s window) | Strong pulse | Sure catch (rares 75%) |

7. **Pod break effect:** the glass cracks (quick white crack lines in the PodGlass shader), then shatters into
   shards (pooled particle burst, glass-tinted, plus a small puff in the creature's element colour). The brass band
   drops and fades. The creature pops out with a squash-and-stretch and an angry little shake, landing back on its
   pad. A fresh empty pod pops into the cradle about 1 s later.
8. A miss (didn't reach the creature) still just bounces back to the cradle, no turn used (unchanged).
9. Remove now-unused Catch-rune code and dialogue targets (`catchRune` → point `capture.locked` at `cradle`).
   Update `BattleTests`.

## C. Rim pods grow when you look at them
10. Party pods stay 3 cm. The pod under the **gaze ray** (or the nearest one when a hand comes within about 10 cm)
    grows to **2× (about 6 cm)** in 0.15 s, and its grab volume grows with it. It shrinks back 0.3 s after
    gaze/hand leaves. Only one pod is big at a time.
11. Neighbours slide aside a little (dock-style) so the big pod never overlaps them, and slide back after.
12. Stays big while it's grabbed or its card is showing. Empty sockets don't grow.
13. Same rule for the cradle pod? No, it's already big (6 cm). Leave it.

## D. Gaze a bit lower
14. `HeadGaze`: `DefaultOffset` 18° → **23°**, adaptive `maxOffset` 25° → **30°**, `share` 0.75 → **0.85**.
    Check that runes, tiles, pods and the cradle are still easy to hit in flat Play (Editor gaze debug line), and
    log the current offset once at battle start so Mugesh can tell us the number if it needs more.

## Housekeeping
- One compile batch per section. EditMode tests green. Quick flat Editor Play smoke test (arrow keys + Space,
  BattleDebug keys) before committing.
- Mugesh is out, so **commit locally once it compiles and the smoke test passes**, with
  "(headset test pending)" at the end of the message. That gives a rollback point. Don't move the `pre-goa` tag.
- No AI attribution lines. Don't push. Update `.claude-handoff.md`.
- Then continue with `step-07-progression-save.md`.

## Test on the headset (later)
1. Encounter: only the lead pod glows and Run shows; moves appear once Kilnpot lands.
2. No Catch rune. The cradle pod is there from the start; throw it at full HP: it wobbles once and shatters, the
   creature bursts out angry, a new pod appears.
3. In red the cradle glows; when dazed it pulses; catches work.
4. Look at a rim pod: it grows and its neighbours slide aside; easy to grab and poke.
5. Gaze feels lower and more natural for runes and tiles.
