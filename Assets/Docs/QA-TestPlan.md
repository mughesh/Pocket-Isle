# Pocket Isle: QA test plan (round 1, late Oct 2026)

Thank you! Pocket Isle is a cozy mixed-reality creature-collecting game for Meta Quest, played with **hands only**
while seated. A small floating island appears in your room; you guide a tiny explorer, catch creatures called
**Islings**, solve hand puzzles and calm a frightened guardian. Install and dev panel: see `QA-Guide.md`.

## Not in this build (please don't report)
No sound or music yet (the volume settings are saved but play nothing) · only the hub and the fire realm (water and
wind gates are sealed on purpose) · some art is placeholder.

## Session 1: first-time player (most important, ~30 min)
**Don't open the dev panel and don't read ahead.** Play from a new game as a real player would. Seated, hands only.
Write a note **every time** you:
- don't know what to do next, or what a hint means
- try a gesture and nothing happens (or the wrong thing happens)
- have to repeat a gesture more than twice
- feel uncomfortable (neck, arms, eyes) or something is hard to see or read
- see something visually broken (things clipping, floating, flickering)
- feel something is fun, cute or satisfying (yes, write these too!)

Goal: get as far as you can: the opening → pick a starter → the tutorial → first catch → through the Fire gate into
Cinderwood → the village shrine → the fallen log → the forge → the guardian battle → back home to the statue.
At the end, answer: where did you get stuck? what was your favourite moment? what would you change first?

## Session 2: targeted checks with the dev panel (~30–45 min)
| Area | What to check |
|---|---|
| Island | Move, turn and resize it with one and two hands; recenter (hold the Meta button) puts it back in front of you |
| Walking | Thumb swipe walks, the same swipe again runs, thumb tap stops; look at a tile and pinch to walk there |
| Battles | Throwing pods to send Islings out, switching, all 3 moves, running away, losing everything (you go back to a shrine) |
| Catching | The big pod above the rim: throw it at a weak (red) wild Isling; throw it too early (it should shatter) |
| Hearth Lodge | Palm over the shrine flame to enter; swap Islings between the room and your pods; leave by the door |
| Puzzles | Lift the log onto a glowing ledge; light the three braziers; the forge door opens |
| Guardian | The battle (shield, two phases), winning, the realm coming alive, placing the shard in the statue |
| Pause / resume | Take the headset off for 30 s and put it back on; quit and relaunch (you continue where you were) |
| Both hands | Do things with your left hand too |

## Session 3: Quick Journey run (~15 min, step 9g)
The judges' route. Start fresh: dev panel **Save > Quick: start** (or a fresh install, then pick **Quick journey** on
the title). Play it like a judge would, no dev panel, and **time it** (the dev readout shows a Quick Journey timer:
**Tools > Readouts**). Note:
- Did the title make sense (logo, Full adventure / Quick journey)?
- After the first catch: two friends arrive at the rim and the fire gate glows. Clear?
- Cinderwood: did you ever not know where to go (gold tiles show the way when you stand still for a few seconds)?
- How many wild battles on the way? Did the guardian fight feel fair with the gift team?
- Placing the shard in the statue: the end card (Keep exploring / Title). Total time from the shard to the end card.

## Session 4: menu and settings pass (~10 min, step 9g)
| Check | Expected |
|---|---|
| Left palm up + pinch mid-game | The game menu, everything stops (walking, battle, the Spirit) and the island dims |
| Same gesture again / Resume | Everything continues where it was |
| Menu during a battle | Battle runes don't react while it's open |
| Menu while holding the island | Opens when you let go |
| Settings: each volume row, Dominant hand, Spirit hints | The choice lights up; quit and relaunch: still set |
| Recenter island | The island comes back in front of you |
| Credits | A stone card; Back returns |
| Title | Saves, the island sinks and rises with the logo, Continue takes you back to the same spot |
| Relaunch | Continue names the slot you played last (Adventure / Quick journey) |
| New journey on a slot with a save | Needs a 2 s hold, then the opening plays from the shard |

## Session 5: fresh-install onboarding pass (~15 min, step 9h)
Uninstall first (`adb uninstall com.mugesh.pocketisle`), then install and start, so it is a true first run. No dev
panel. Note for each step whether you knew what to do **without** reading ahead:
| Check | Expected |
|---|---|
| Launch | **No room-scan prompt**, no permission pop-up; the island appears and floats |
| Every Spirit line | Short; stays up long enough to read; the next one never cuts it off |
| Pinch lesson (after "Hello, big one!") | A ghost hand pinches **beside your own hand**; each pinch sparkles gold; two pinches and it moves on |
| Each lesson's hint | After a few seconds it shrinks to a small chip with a hand ("Hold a corner", "Pinch!"); it stays until you do it; looking at it or the Spirit opens the full line |
| Island move | The ghost grabs the **near corner of the rim on your main-hand side** |
| Starter pick | All three pods have a **gold rim**; the one you look at turns white, grows and wiggles; the ghost grabs the pod you look at (or each in turn) and sets it on the rim |
| First battle | Your explorer faces the wild Isling; the Spirit is off to the side (not in front of the Islings); the ghost grabs your lead pod (gold) and lets go over the pad |
| Lodge | Standing by the door or resting a hand near it doesn't take you out; walking into the door does |
| Title (relaunch) | The new logo: a little piece of the island with amber "Pocket" and mossy green "Isle", a lantern over the I |

## How to report
Use **Tools > Report bug** in the dev panel right after something goes wrong (it saves a screenshot, the save file and
the log). Then add a line to a shared note: `time · what you did · what happened · what you expected`.
Severity: **Blocker** (can't continue) · **Major** (works but badly) · **Minor** (cosmetic).
