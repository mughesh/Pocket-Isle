# Panel review: the judged path (Nov 11, 1 PM IST)

## Phase 4–5: what was built (Nov 11, 5:30 PM IST, branch `panel-review`)

Mugesh's "go" came at ~5 PM with the Tier 2 answers below. Everything is **headset test pending**; the checklist for it is
`Assets/Docs/TEST-NOW-panel.md` (his filled-in `TEST-NOW.md` was left untouched: the evening fix list reads from it).

**Built (one commit each):**

| # | What landed | Verified by |
|---|---|---|
| T1-1 | `QuickJourneyDirector.EndingUp` from the shard's lock-in to Keep exploring; `GrowthDirector.Available` honours it: no pod glow, no `evolve.ready` over the whisper, `quick.end` or the end card. The smoke run's new beat "end card owns the stage" notes any R48 break. | `PanelTests.QuickEnding_*`; smoke: all seven end lines and the card with no pin (`20_end_card_after_no_evolve_pin.png`, was `13–15_*`) |
| T1-2 | Walk lesson `helpAfter` 30 → 12 s ("Or look at a spot and pinch."). The far lesson has no help line, so it was left as is. | `PanelTests.WalkLesson_*` |
| T1-3 | Captions through `BattleRunes.WildName / MoveLine / FaintLine`: "Galeowl's turn...", "Galeowl used Gale Shot!", "**Galeowl is calm!**"; wild ones unchanged. | `PanelTests.Keeper_IsNeverWild_InCaptions` |
| T1-4 | `capture.success.first` is said after the pod flies home and the "New Isling!" toast has gone (`JournalToast.Showing`), not with "Caught!" / "Gotcha!". | `PanelTests.Catch_*`; `22_catch_after_one_voice.png` |
| T1-5 | `DialogueBubble` faces the head with `BattleHud.LabelUp` (both the placed and the parked bubble): R43 for bubbles. | `PanelTests.Bubbles_*`; the bubble tests and the smoke run unchanged |
| T1-6 | Finale Skip tablet anchored at the Journal tablet's rim spot, left of the pod band (`FinaleDirector.SkipSpot`); the rim's middle only if there are no pods. | `21_finale_skip_beside_pods_after.png` (was `16_*`) |
| T1-7 | `TowerHallTheme` restored palette: floor tiles are `path` in the hall, so the whole floor is now cream (0.68/0.63/0.55) against slate walls and pillars; bases/tips/dirt/rock a touch warmer; light 0.84/0.9/1.0 blue → 1.0/0.94/0.84 warm, 0.85 → 0.9. Asset values only. | `19_tower_hall_after.png` (was `09_*`); `PanelTests.TowerHall_*` |
| T1-8 | Regressions R48 (end card never interrupted), R49 (Keepers never wild), R50 (one voice at the catch); R46 amended for the quick slot. | this file, `Regressions.md` |
| T2-1 (yes) | `TempleRules.Needed`: **2 in the quick slot**, 3 everywhere else (`QuickIslings`). Three older tests updated. | `PanelTests.Temple_*`, `Nov11Tests`, `Nov4Tests`, `Step14hTests` |
| T2-2 (yes) | `QuickJourneyConfig.skipAfterOverrides` (move 25 s, two hands 20 s) through `QuickJourney.SkipAfter` and a `LessonRunner.SkipAfter` hook; the lessons' own 45 s stay for the full adventure; nothing is cut. | `PanelTests.QuickSlot_*`, `LessonRunner_UsesTheSkipAfterHook` |

**Not built:** T2-3 (realm music dial: no), T2-4 (warm the hall on the calm: no), T2-6 (realm card: no). T2-5: Mugesh measured **72 fps stable** on his pass; no capture needed. Nothing from Tier 1 was reverted.

**Runs after the last item:** EditMode 444 passed / 0 failed (11 new in `PanelTests`); smoke `New game, Lodge, Menus, Evolution, Save + continue, Quick Journey, Step29, BattleLayout, MenuOverBattle` with shots: **81 ok / 0 failed / 0 notes** (7.8 min); Readiness Report: 0 "no" rows, "To do: nothing" (the generated report files were not committed).

**Triangles / draws (Editor, seat camera, `shots_index_after.md` vs `shots_index.md`):** tower hall at the door 119 → 120 draws, 137k → 138k tris; hall fight peak 143 → 145 draws, 151k → 153k tris; end card 123 draws / 124k tris; finale credits peak 143 draws / 192k tris (unchanged beat). No item adds geometry or a material; the ±2 is kite and particle timing.

**Mugesh's headset note (5 PM): "colours look dull on the headset display."** Not acted on: a global colour change is outside Tier 1 and he suspects his monitor's contrast. If it holds after T1-7's hall, the honest fix is a small saturation/contrast lift in the toon shader's banding or the URP colour grading, tested on device first. Listed as a Tier 2 question below.

**Tier 2 as yes/no (open):**
- T2-8 **A saturation / contrast lift for the headset** (one URP post-process volume or a shader band tweak, Quest-tested)? Needs a device A/B; not before the fix list.
- T2-3 realm music before restoration in the quick slot: still no?
- T2-4 warm the hall on Galeowl's calm (restoration fade): still no after seeing T1-7?

**Next on this branch, on Mugesh's word:** the design chat's `Assets/Docs/Prompts/fix-list-nov11.md` (his `[x]` lines + `Notes copy.md`), same rules.

---

## 0. State found
1. **master = `0cc5c7eb` = step-29b, pushed** (steps 22–29b: Nov 10 playtest fixes, Nov 11 headset notes, prop rebake). Tests 433/433, full smoke 91 ok / 0 failed, Release Check PASS. Branch `panel-review` made from it.
2. **Nothing from steps 22–29b has been on the headset.** Mugesh has started the `TEST-NOW.md` pass on a QA build of `78a9b7d` (2 lines annotated so far: starter card works; the "surf pulsing" bed was never seen).
3. Last device numbers are from **Oct 25** (release APK, Quest 3): 71–72 fps, 35–62 draws, 64–172k tris; cold start 0.54 s. Since then: 46 Islings, three realms, the finale, paid audio, heal FX, rings, ~+9k prop triangles. **No frame measurement of today's build exists.**
4. Open from earlier runs: the swipe ghost re-recording (Mugesh, over Link), the Run-24/25 battle-row / pour / audio headset lists, junction chevrons never seen, no element symbol on starter cards.
5. Quick Journey as built: tutorial (shard, pinch, move, starter, walk ×4 directions, far, practice, look, grass, send, rune, catch) → hub send-off → Galewick (sweep, kites, ridge clouds, mill, tower) → untangle → Galeowl Lv 4 → shard → statue → end card. Dry run was 13:00 on Nov 3 (target 12:00); lessons grew since (practice, lesson walking at 65% speed).

---

## 1. The panel (Phase 1: the vision)

### What each seat thinks Pocket Isle promises

**Creative director.** It promises a *living diorama you are the god of*: a tiny world on your desk, a tiny explorer, and hands big enough to change the weather. The cozy creature-collector loop (grass → battle → catch → grow) is the skeleton, but the promise judges will remember is "I lifted a kite out of the island and it flew in my room." A winner leans on that promise every two minutes. Table stakes: the battle, the catch, the Journal. Risky: the structure (lessons, three realms, finale) is a *full game's* structure; in 10 minutes a judge sees the tutorial and one realm, so the tutorial *is* the game for scoring.

**MR interaction designer.** The promise is *hands-only, seated, no menus*: thumb microgesture to walk, look + pinch to choose, a fingertip to poke, a flick to catch, a palm to hold a creature. That is genuinely rare on Quest (most entries wrap a controller game). What's new: the microgesture-as-walk, the grab band on the rim, the two-hand resize, evolution on the open palm. Risky: microgestures are the least reliable hand input Meta ships (Mugesh's testers' top complaint was walking, twice). The game must never *depend* on a swipe landing: look + pinch must always be an obvious second door, not a fallback mentioned once in a line.

**Onboarding / pacing designer.** The promise to a first-timer: *something satisfying within 3 minutes, a story beat within 10.* Right now the opening has 13 lessons before free roam (shard, pinch, move, two hands, starter, walk, far, practice, look, grass, send, rune, catch). Each has a skip timer (45–60 s on the ones that can't be verified), so a judge who misses a gesture waits. The swipe lesson now runs four directions × 20 s in the smoke run; in a judge's hands it's "swipe until 4 tiles" then "tap to stop", then a 3-stop practice. That is three walking lessons in a row before the first grass. Table stakes done well: one instruction at a time, chip + ghost, praise. Risky: the lesson chain's *length*, and the fact that the judge's one guaranteed battle (the scripted Toastling Lv 3) is also a lesson.

**UX writer.** The Spirit's voice is good: short, warm, verb-first ("Thorns! Touch the ember, then poke the thorns."). The Nov 10 copy pass fixed the worst. What remains is *density*, not words: at the catch there are four texts at once (Caught!, "Gotcha! Toastling joined you!", the "New Isling! Toastling · 2/46" toast, the Spirit's "A new Isling! It's in your pods now."). Promise: one voice, one line, one idea.

**Art director.** The look is the strongest card: toon, texture-free, hand-sculpted props, a real art direction (wheat Galewick, teal hub, mossy stone UI). The island reads as an object in the room, not a UI panel. What's new for a Quest title: SDF-baked everything with an inverted hull outline at 72 fps. Risky: grey-on-grey interiors (the tower hall: slate floor, slate walls, slate pillars, a slate owl), and that a few baked props still show voxel steps up close. Small text on tablets (move names) is at the Quest legibility floor.

**Audio director.** The music-as-reward design is a genuine idea: no music in a realm until you restore it, then it blooms. Table stakes: footsteps, UI taps, jingles, the Spirit's blip. Risky: on the judged path the realm walk (roughly minutes 5–9) has ambience only; the temple track arrives at the tower and the bloom at the shard. That's deliberate and defensible, but a judge who quits at minute 7 scores "no music". Also: nobody has heard the 532 paid clips in a headset.

**Technical director.** Promise: 72 fps, instant cold start, no stalls, hands that never lock. The architecture is right (instanced tiles/grass/props, one particle pool, texture-free). Risks are *unmeasured*: the last device capture is 17 days old; the Keeper halls, the kites' strings, the heal wave and the finale were never profiled; the opening had 97 ms hitches (shader warm-up) on Oct 25 and nothing says they're gone. A single hitch during the shard unfold is the judge's first impression.

**QA / playtest lead.** Two friends played on Nov 10: top complaints were walking (stops mid-road, junctions), unclear turn order in battle, not knowing the team was hurt, hands not understood for the ember. All fixed in code since, none confirmed on a headset. The pattern in the Run reports: *fixes land in flat Play and get re-reported from the headset* (Regressions R8 five times, R17 twice, R40 twice). So the plan must weight "already built, needs confirmation" over "new work".

**Competition judge.** Against the four criteria: Innovation is strong (hands-only MR diorama, god-scale hand moments, music as reward). Experience Design is the swing criterion: onboarding is thorough but long; purpose is clear ("bring back the wind shard") once the hub send-off lands. Technical: hands, gaze, passthrough all used purposefully; fps unproven today. Polish & Presentation: art is excellent, audio unheard, submission materials (video, Devpost, key art) not started. **The grand-prize gap is not features; it's confidence: does every judged minute work the first time, on a judge's hands?**

### Where they disagree
- **Creative director vs onboarding designer:** CD wants the kite moment (the first "only a giant could do this") earlier; OD says the Still Isle tutorial must stay as the safe room: no new content, shorten what's there instead. **Agreed:** shorten, don't move.
- **MR designer vs CD on walking:** MR wants look + pinch promoted to a co-equal control (taught, not mentioned); CD worries two walking methods confuse. **Agreed:** keep swipe first, but the "Or look at a spot and pinch" help line must come sooner than 30 s and the gold tile practice should accept look + pinch as a stop too.
- **Audio director vs CD on silence:** AD would play a thin unrestored layer in realms; CD and Mugesh hold music-as-reward. **Agreed:** keep the design; put the "always" dial (it exists: `MusicDirector.RealmMusicBeforeRestore`) as a Tier 2 yes/no for Mugesh after he hears the realm in a headset.
- **Technical director vs everyone:** TD says no visual change should ship without a device capture. **Agreed:** every Tier 1 item reports its triangle/draw delta, and the plan's first item is a measurement, not a feature.

### North star (one sentence)
**A judge sits down, and within ten minutes their own hands have walked a tiny explorer, caught a creature, flown a kite into their room and calmed a frightened owl, without one moment of "what do I do?" or "did it see me?".**

### Pillars (what every Tier 1 item must serve)
1. **Hands that are understood.** Every gesture has a visible, immediate answer, and a second door (look + pinch, a poke) that is just as obvious.
2. **One voice, one line.** Never two texts for one event; the Spirit says the next verb, nothing else.
3. **Godscale moments on the clock.** A hand moment that leaves the island every ~2 minutes of the judged path (shard, kite, sweep, flick, untangle, palm evolution, shard to statue).
4. **Nothing stalls, nothing drops.** 72 fps, no hitch on the first minute, no lock-ups: measured, not assumed.
5. **Fit and finish you can see from the seat.** Readable at Quest resolution: labels, rings, halls with contrast.

### Honest validation of the concept
- **Genuinely new:** hands-only creature collecting on a desk-sized diorama; microgesture walking with gaze fallback; hand moments at god scale (kites into the room, ember on a fingertip, rain from a palm); evolution on the open palm; music that blooms when a realm is restored; sky-lantern credits in the room.
- **Table stakes (done, keep):** type triangle battles, catch with a throw, party pods, a Journal, Keepers with shields, save/continue, pause, settings.
- **Risky:** tutorial length vs the 10-minute judge; microgesture reliability on unfamiliar hands; an unmeasured frame budget after a month of additions; audio nobody has heard; the warning at the tower in the Quick Journey (a judge arrives with 2 Islings and is told to bring 3, see Phase 2).

---

## 2. Audit (Phase 2: evidence)

**How:** the smoke run with a new `shots` flag (`SmokeRun.RunOnly` arg `New game, Quick Journey, Step29, shots`): a seat-camera shot at the end of every beat and 0.9 s into every new line, with draw calls and triangles (`Logs/Panel/index.md`, 126 shots; the ones cited are in `Assets/Docs/Panel-Review-img/`, index copied as `shots_index.md`). Result today on this branch: **45 ok, 0 failed** (7.5 min), one known note (the Still Isle junction shot ends at a blocked tile, as in run 29). Flat Play: no real hands, no head roll, the seat camera stands in for the head. **Headset-only** marks what this can't see.

### Creative director
| # | Finding | Evidence |
|---|---|---|
| CD1 | **The Quick Journey's ending is stepped on by the evolution objective.** Galeowl's thanks makes the starter "due"; `evolve.ready` ("Kilnpot is glowing! Hold its pod on your open palm.") pins itself and its ghost hand right after "You woke the first piece of the world!", so `quick.end`'s next two lines ("Two more realms are waiting." / "Keep exploring, or peek at how it all ends?") never show, and it sits over the end card and over "Let's dream ahead..." in See the ending. The judge's last memory of the game is a chore prompt. | `13_quick_end_line1.png` → `14_end_card_evolve_pinned.png` → `15_see_the_ending_evolve_pinned.png`; index 093–099. Code: `GrowthDirector.Available/Calm` has no gate for the QJ ending (`QuickJourneyDirector._ending`, the end runes) or the ChoiceBoard |
| CD2 | The kites flying in the room over the ridge, the mill with the drawbridge, the shard to the statue: these are the game's best minutes and they work in order. **Leave alone.** | `07_kites_in_the_room.png`, `12_shard_to_statue.png` |
| CD3 | The tower hall is slate on slate: floor, walls, pillars and an unlit grey owl; only the window changes when it calms. The climax room is the dullest room. | `09_tower_hall_grey.png`, `11_galeowl_calmed.png`; `TowerHallTheme.asset`: `restored` and `unrestored` palettes identical (grass 0.46/0.49/0.51, path 0.50/0.53/0.55), `startRestored 1` |

### MR interaction designer
| # | Finding | Evidence |
|---|---|---|
| MR1 | **The second door for walking comes too late.** If a judge's swipe doesn't register, "Or look at a spot and pinch." only appears after 30 s of the walk lesson (`helpAfter 30`); the lesson itself never skips (`skipAfter 0`). Thirty silent seconds on the first real input is where a judge decides the hands don't work. | `Onboarding_Main.asset` walk: `help lesson.walk.help, helpAfter 30`; Nov 10 playtests: walking was the top complaint twice |
| MR2 | Speech bubbles still use world up (`Quaternion.LookRotation(…, Vector3.up)`), the thing Nov 11 fixed for battle labels (`BattleHud.LabelUp`). Looking down at a bubble at the side of the view, it reads slanted; in the flat shots every side bubble is visibly rotated. | `03_practice_nice_stop.png`, `05_catch_text_pileup.png`, `18_tired_line_slanted_bubble.png`; `DialogueBubble.cs:805, 998` |
| MR3 | Opening waits: move lesson 45 s, two-hands 45 s (both run in the Quick Journey: `cutLessons` is empty, the handoff's "two hands cut" is stale), far lesson 60 s. A judge who doesn't find the corner waits 90 s before the starters. | `Onboarding_Main.asset` skipAfter; `QuickJourney.asset` `cutLessons: []`; `Opening.cs:433` |
| MR4 | Headset-only: grab band vs pinch-to-walk, tap read as pinch (`pinchTapGuard 0.35`), the catch flick, the pod card tilt. Covered by `TEST-NOW.md`. | — |

### Onboarding / pacing designer
| # | Finding | Evidence |
|---|---|---|
| OB1 | **Every judge hits "Galeowl is strong. Bring at least 3 Islings." at the tower.** The Quick Journey party is the starter + the tutorial Toastling (2); encounters are halved and nothing says where a third comes from; Galeowl is Lv 4 there, tuned for exactly those two. The warning is true in the full game and false in the judged one. Mugesh asked for 3 everywhere on Nov 11 (R46), which is why the panel puts the change in Tier 2, with a recommendation. | `TempleRules.Needed(galewick, quick)` ignores `quick`; `QuickJourney.asset` `keeperLevelOffset -4`, `encounterRate 0.5`; Run-14d (Galeowl Lv 4 for 2 Islings) |
| OB2 | 13 lessons before free roam, three of them walking (walk, far, practice) back to back. Lines are good, pacing is long. Nothing here can be cut under the freeze; the timers can be. | `Onboarding_Main.asset`; smoke: tutorial over at beat 13 |
| OB3 | The stop practice reads well: gold tile ahead, "Nice stop!", "Perfect!". Keep. | `03_practice_nice_stop.png` |

### UX writer
| # | Finding | Evidence |
|---|---|---|
| UX1 | **Keepers are called wild.** The caption says "Wild Galeowl used Gale Shot!" and "Wild Galeowl fainted!"; the story says it calms. | `10_keeper_fight_wild_caption.png`; `BattleStage.cs:664, 674` (`Wild` prefix on `BattleSide.Wild`, Keepers sit on that side) |
| UX2 | **Four texts at the catch:** "Caught!" pop, "Gotcha! Toastling joined you!" caption, the "New Isling! Toastling · 2/46" toast with its portrait, and the Spirit's "A new Isling! It's in your pods now." All at once, three of them saying the same thing. | `05_catch_text_pileup.png`; `EncounterDirector.cs:950–956` |
| UX3 | The copy pass from Nov 10 holds: verb first, one action, short. Galeowl's three calm lines are the best writing in the game. Keep. | `11_galeowl_calmed.png` |
| UX4 | Minor: the Galewick name card's line ("Where the wind stopped") is hidden behind the arrival cloud and the sweep ghost. | `06_galewick_arrival_card.png` |

### Art director
| # | Finding | Evidence |
|---|---|---|
| AD1 | Pillars, windows, kites and the basin read clean after the Nov 11 rebake. Rings are slim and readable (red low, grey fainted). Keep. | `09_tower_hall_grey.png` (pillars), `112` in the index |
| AD2 | The tower hall needs one warm tone (see CD3). Asset values only, no triangles. | `TowerHallTheme.asset` |
| AD3 | Move tablet labels ("Ember Puff / Fire") and the "Not very effective" pop are at the legibility floor in the seat shot; the headset decides. | `04_first_battle.png`; headset-only |
| AD4 | The finale's **Skip tablet sits on the rim pods** (board at `Vector2.zero` on the near rim). | `16_finale_skip_over_pods.png`; `FinaleDirector.cs:424` |

### Audio director
| # | Finding | Evidence |
|---|---|---|
| AU1 | **Nobody has heard the 532 paid clips.** The Regen report's "listen first" list (`amb.stillisle`, `amb.lodge`, `amb.galewick.restored`, footsteps) is the right order. Can't be audited here. | `Assets/Docs/Audio/Regen-Report.md` |
| AU2 | The judged path's music: title → hub (tutorial) → **Galewick walk: ambience only (~minutes 5–9)** → temple track in the hall and the Keeper fight → silence after the calm → the Galewick bloom at the shard → hub. Deliberate (music as reward). The dial to hear a realm track before restoration exists (`MusicDirector.RealmMusicBeforeRestore`, Dev panel "Realm music: always/reward"). | `AudioDirector.cs:178, 189` |
| AU3 | The Keeper's thanks and the Heart's first call are the only two "voice" moments on the judged path; both untested. Headset-only. | Run-29 item 24 |

### Technical director
| # | Finding | Evidence |
|---|---|---|
| TD1 | **No device frame measurement since Oct 25** (71–72 fps at 35–62 draws). Today's Editor counts on the judged path: 53–143 draws, 67k–256k triangles. Peaks: **143 draws in the tower hall fight**, 137 at the mill, 256k tris on the ridge (trees + kites + clouds). The 150-draw budget is within 5% in the Keeper hall. Unknown on device: the heal wave, the kites' strings, the finale (140 draws at the Spark). | `shots_index.md` columns Draws / Tris |
| TD2 | Shader warm-up covers shard, fx, pod, portal, logo at the opening (`Opening.WarmUp`). Galewick's first kite / cloud / gust draws are not warmed; a hitch at the first kite lift is possible. Unmeasured. | `Opening.cs:740–786` |
| TD3 | Robustness today: smoke 45/45; tests 433; Release Check PASS; XR init restored after the run. The Still Isle junction shot still ends at a blocked tile (flat-only, R35 is covered by the corridor tests). | `Logs/SmokeRun/report.md` |
| TD4 | The panel-shot instrumentation (this branch's only code change so far) is a tool: `SmokeRun` arg `shots`. It runs only when asked. | `SmokeRun.cs` `PanelShot / PanelLines` |

### QA / playtest lead
| # | Finding | Evidence |
|---|---|---|
| QA1 | Everything from steps 22–29b is unconfirmed on a headset; Mugesh's pass has begun (2 of 36 lines annotated). Any Phase 4 must start from his `[x]` marks. | `TEST-NOW.md` |
| QA2 | Patterns that came back from the headset before (R8 swipe ghost ×5, R17 overlaps ×2, R40 locks ×2) are all "flat Play can't see it". The Tier 1 list below avoids anything whose check is headset-only unless the fix is trivially safe. | `Regressions.md` |
| QA3 | A first-timer's stall points on the judged path, in order: swipe not registering (MR1), the two 45 s waits (MR3), the tower warning (OB1), the ending prompt (CD1). | above |

### Competition judge (scores as it stands, out of 10)
| Criterion | Now | Why | What moves it |
|---|---|---|---|
| Innovation | 9 | Hands-only diorama, god-scale moments, music as reward | Nothing to add; don't break it |
| Experience Design | 6.5 | Clear purpose and good lines, but the first input can go silent for 30 s, two 45 s waits, a false warning at the climax, the ending stepped on | MR1, OB1, CD1, UX2 |
| Technical | 7 | Hands, gaze, passthrough all purposeful; fps unproven for a month | TD1 (a measurement), nothing visual that adds draws |
| Polish & Presentation | 7 | Art 9, writing 8, audio unheard, hall dull, "Wild Galeowl", submission materials not started | UX1, AD2, AD4, MR2; the video and Devpost are outside this review |

---

## 3. The plan (Phase 3)

Effort: S ≤ 1 h, M ≤ 3 h, L more. Every item: EditMode tests, the smoke run (`New game, Quick Journey, Step29`), Release Check; one commit each, "(headset test pending)". Triangle/draw delta stated per item; none below adds a draw call.

### Tier 1: do now (after "go"; TEST-NOW `[x]` fixes come first)
| # | What | Why (criterion) | Evidence | Effort | Risk |
|---|---|---|---|---|---|
| T1-1 | **Quick Journey ending owns the stage:** `GrowthDirector` is not calm while the QJ is ending, the end card is up, the dream-ahead runs or a ChoiceBoard is open; the due pod keeps glowing (the gold pod is enough) and `evolve.ready` pins after the player picks Keep exploring / Title. Test: QJ end with a due starter shows all three `quick.end` lines and no `evolve.ready` until the card is answered. | Experience, Polish: the last 60 s of the judged path (CD1) | `13–15_*.png` | S–M | Low: one more condition in `Available`; the full game is unchanged |
| T1-2 | **Walk help at 12 s, not 30:** `lesson.walk.help` ("Or look at a spot and pinch.") after 12 s of no swipe in the walk lesson; the far lesson the same. Asset values. | Experience: the second door shows before a judge gives up (MR1) | `Onboarding_Main.asset` | S | Low: data only; R28–R30 untouched |
| T1-3 | **Keepers aren't wild:** captions "Galeowl used Gale Shot!", "Galeowl is calm!" (or "...calms down!") instead of "Wild Galeowl ... fainted!". Test on `BattleStage` caption text for a Keeper. | Polish (UX1) | `10_keeper_fight_wild_caption.png` | S | Low: strings, Keeper side only |
| T1-4 | **One voice at the catch:** the Spirit's `capture.success.first` waits until the "New Isling!" toast has gone (toast seconds + 0.5), or is dropped when the toast is up. "Caught!" and the "Gotcha!" caption stay. | Polish, Experience (UX2) | `05_catch_text_pileup.png` | S | Low: a delay on one line |
| T1-5 | **Bubbles level with the head:** `DialogueBubble` uses `BattleHud.LabelUp` (head up minus roll) in its two `LookRotation`s; a test like `Nov11Tests.LabelUp_*` for the bubble. | Polish (MR2), the same rule as R43 | `18_tired_line_slanted_bubble.png` | S | Low–medium: it changes how every bubble faces; the smoke run and the existing bubble tests cover placement. Revert if any bubble test moves |
| T1-6 | **Finale Skip off the pods:** place the Skip tablet left of the pod band (the Journal tablet's rim spot rule, `LodgeJournalBook.RimSpot` / `PodRack.SocketBandLocal`), not at the rim centre. | Polish; R17 says nothing overlaps the pods (AD4) | `16_finale_skip_over_pods.png` | S | Low |
| T1-7 | **A warmer tower hall, no new behaviour:** `TowerHallTheme` restored palette: floor two tones (a cream-grey path band vs slate floor), pillars/rock a touch warmer, `lightColor` warm instead of blue; the window keeps its grey sky until the calm. Zero triangles, zero draws. Before/after seat shots. | Polish (CD3, AD2) | `09_tower_hall_grey.png` | S | Low: asset values; if it reads wrong in the shot, revert |
| T1-8 | **Regression rules** for what the above fixes: R48 (the QJ end card is never interrupted by an objective), R49 (Keepers are never "wild" in captions), R50 (one text per event at the catch). | Keeps them fixed | — | S | None |

Expected effect: Experience 6.5 → 7.5, Polish 7 → 7.5. Total Tier 1: about 5–7 h including runs.

### Tier 2: needs Mugesh's yes (reply yes/no per line)
| # | Question | Panel's recommendation | Effort | Risk |
|---|---|---|---|---|
| T2-1 | **Quick Journey tower warning: need 2 there instead of 3?** (`TempleRules.Needed` honours `quick`: 2 in the quick slot, 3 everywhere else.) Galeowl is tuned for the starter + Toastling at Lv 4; the warning tells a judge to do something the journey doesn't support. | **Yes.** It reverses your Nov 11 note only for the quick slot; R46 stays for the full game | S | Low |
| T2-2 | **Shorter opening waits in the Quick Journey only:** move 45 → 25 s, two-hands 45 → 20 s (or cut two-hands in the quick slot via `cutLessons`). | Yes to 25/20; **no** to cutting two-hands (resize is a judged interaction) | S | Low; you set 45 after lessons vanished too fast in 14i, so this is your call |
| T2-3 | **Realm music before restoration on the judged path** (`RealmMusicBeforeRestore` dial on for the quick slot only, at the current realm level 0.28–0.30)? | Decide after you hear Galewick in the headset. The panel splits 5–4 for keeping silence | S | Low (a dial), but it changes the design's signature |
| T2-4 | **Warm the hall when Galeowl calms** (hall `startRestored 0` with a warmer `restored` palette, `RealmRestoration.FadeTo(1)` from `KeeperHall.Restore`): the room answers the calm like the realm does. | Yes if T1-7 alone isn't enough on the headset | M | Medium: touches the restoration blend for interiors |
| T2-5 | **A device frame capture** on your TEST-NOW pass: Dev panel FPS in the tower hall fight, on the ridge with 3 kites, during the bloom heal wave, at the Spark. Or push `perftour8.txt` and read `[PerfTour]` from logcat. | **Yes, before any further visual change.** No build is needed if the current QA build has the Dev panel | 10 min of yours | None |
| T2-6 | Realm name card above the arrival clouds (UX4): raise the card or delay the arrival objective 2 s. | Only if it bothers you on the headset | S | Low |
| T2-7 | ElevenLabs: nothing needed. Zero credits planned. | — | — | — |

### Tier 3: after the competition
- Shader warm-up for Galewick's first kite / cloud / gust materials (TD2).
- Junction chevrons on the Still Isle hub plaza (the smoke shot still ends at a blocked tile).
- Element symbol on starter cards; the swipe ghost re-recording (yours).
- A second pass on text sizes at the Quest floor (AD3) once seen in the headset.

### What NOT to touch (works; destabilising it costs more than any gain)
- The lesson chain's order and content (R28–R30), the smart-step corridor rules (R34–R35), the grab band, the pod HP rings and bars (R38, R44), the battle layout (R19, R31).
- Hand moments as built: sweep, kites, mill, untangle, shard touch. They are the demo.
- The music-as-reward design and the mix levels from step 23 (unless T2-3 says otherwise).
- Prop recipes beyond the Nov 11 rebake (R47); every PropKit writer rewrites its realm's other recipes.
- The release setup, version codes, keystore. **Step 26 (the release build in the "Competition" channel) has still not run** (`Run-26.md` does not exist): that is the most urgent item in the project and it is outside this review.
- `Script_v1.md` import (R25).

### Stop
Phase 3 ends here. Branch `panel-review` holds this file, the evidence shots and the `shots` flag in `SmokeRun`. Waiting for **"go"** (and a filled-in `TEST-NOW.md` if there is one) before Phase 4.
