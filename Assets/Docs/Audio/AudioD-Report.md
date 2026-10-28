# Step audio-D report (Oct 28, unattended run)

Sounds for everything built since the audio merge: Galewick, the Cinderwood hand moments, Lilymere, the finale, cries
for the wind and water rosters, the quieter typing. **Headset test pending** (flat Editor Play only).

## Summary
| | |
|---|---|
| Commits | `c8c1fb4` synth cues, `eeef7cb` gen batch 2 import, `81b2149` hooks and ambience, `3607775` quieter typing, `7e4cde5` cries, `3f860aa` play-test fixes, then docs. Not pushed. |
| New cues | 37 synth cues + 4 synth beds + `music.finale` slot + 24 cries. Every cue has a synth version; 18 also hold gen clips to compare. |
| AudioScope | **All 69 new synth clips pass.** 15 gen clips fail (listed below, kept for your ear). Cries: see "Cries" (same as the 26 before them). |
| Gen | 36 clips imported (all `_FREE`, release build blocked as before). 2,936 credits this run. |
| Tests | EditMode 235/235, audio 4/4. |
| Flat Play | Each realm's moments and the finale walked once: no `[Audio] Unknown cue id`, no `[GameAudio]` placeholder fallbacks, no stuck loops after realm changes, pause and the finale. |
| QA APK | `Builds/PocketIsle-QA.apk` 114.6 MB (Oct 28 11:18), not installed; `PocketIsle-QA-oct27-tester.apk` untouched. |

## How picking works (same as step B)
A cue with both synth and gen clips plays **all of them at random** until you pick in **Audio Lab > Review**. The pick note
on each gen cue starts "Gen vs synth: pick." with the gen lane's verdicts. "Listen first" below is my guess from the
spectrograms; I can't hear.

## Per cue
S = synth variants, G = gen clips (`_FREE`). AudioScope: pass unless noted.

### Galewick
| Cue | Moment | S | G | Listen first | Notes |
|---|---|---|---|---|---|
| `wind.sweep` | hand sweep gust (GustField) | 1 loop | - | synth | One loop per gust: louder and higher for a faster sweep, fades as the gust band crosses. |
| `mill.turn` | drawbridge mill turning; tower vanes; mills in view once restored | 1 loop | - | synth | Creak with two soft knocks per turn. |
| `mill.stop` | drawbridge lands | 1 | - | synth | |
| `kite.tug` | kite grabbed; kite launched (pitched up) | 3 | - | synth | |
| `cloud.part` | a cloud puff blows away | 2 | - | synth | Airy sigh + small sparkle. |
| `knot.pull` | an end of a Galeowl knot pinched | 3 | - | synth | |
| `keeper.knot.snap` | knot snaps (pitch rises per knot) | 2 | - | synth | Rope twang + whoosh. |
| `tower.door` | tower door grinds open | 1 | 2 | **synth** | Gen takes are a steady rush without the stone thunk at the end. |
| `voice.galeowl` | poke the calm owl | 2 | 1 | **gen _2** | Two soft clean hoots. |
| `amb.galewick` | Galewick faded bed | 1 | 1 | gen _2 | Gen: steady hilltop air (high-passed). |
| `amb.galewick.restored` | Galewick restored bed | 1 | 1 | compare | Gen is mostly low wind; larks barely there (the sweeteners add birds and creaks). |

### Cinderwood
| Cue | Moment | S | G | Listen first | Notes |
|---|---|---|---|---|---|
| `flame.cup` | cupping the village flame | 1 loop | - | synth | Swells with the cup's progress (volume 0.35 -> 1, pitch +15 %). |
| `flame.rekindle` | flame relit | 1 | - | synth | |
| `lantern.light` | each hollow lantern lit; each catcher round the plaza | 3 | - | synth | Glass tick + small whoomph. |
| `smoke.clear` | hollow smoke burns off | 2 | - | synth | |
| `ember.pick` | ember grabbed or raised by look + pinch | 2 | - | synth | |
| `ember.drop` + `firebox.roar` | ember in the firebox | 1 + 1 | 0 + 1 | synth | Gen roar is breathy with a late whoomph. |
| `log.drop` | **the log lands (Mugesh: almost silent)** | 1 | 6 | **gen _4** | Remade heavier and moved one loudness tier up (Moves). Gen takes 4-6 are new; 1-3 re-levelled to the louder tier. |

### Lilymere
| Cue | Moment | S | G | Listen first | Notes |
|---|---|---|---|---|---|
| `lily.click` | pad clicks into a slot (a frog croaks 0.35 s later) | 3 | - | synth | Wooden tok + small water slap. |
| `frog.croak` | the pad's frog; Lilymere restored sweetener | 4 | 5 | **gen** | Gen: 5 clean small ribbits. |
| `frog.chorus` + `channel.ripple` | all pads lock | 1 + 1 | 2 + 0 | gen _1 | |
| `lotus.bloom` | bud blooms | 3 | - | synth | Petal pop + sparkle run. |
| `rain.soft` / `rain.hard` | under the palm cloud, follows it | 1 + 1 loops | 1 + 1 | **gen** | Switches soft / hard with the cloud; stops when it stops. Gen soft is a quiet original (+21 dB). |
| `water.scoop` | the cupped hand dips | 2 | 2 | gen _1 | |
| `water.slosh` | water held in the palm | 1 loop | - | synth | Follows the water, louder while it wobbles. |
| `water.pour` | pouring into the bowl | 1 | 2 | gen _1 | |
| `water.spill` | tipped away from the bowl | 2 | 2 | gen _1 | |
| `ice.crack` | 3 cracks race down the frozen fall | 3 | 2 | gen _1 | |
| `ice.shatter` | the fall breaks free | 1 | 2 | gen _2 | |
| `waterfall.run` | the running fall (one 3D loop while it runs) | 1 loop | 1 | **gen** | |
| `lagoon.rise` | the lagoon floods | 1 | 2 | gen _1 | Replaces the 2D restore.swell here (now 3D at the basin). |
| `amb.lilymere` | Lilymere faded bed | 1 | 1 | gen _2 | Dry reed rustle; quiet original. |
| `amb.lilymere.restored` | Lilymere restored bed | 1 | 1 | compare | Gen has lapping + faint frogs and birds but is a very quiet original (+28 dB): listen for hiss. |

### Finale
| Cue | Moment | S | G | Listen first | Notes |
|---|---|---|---|---|---|
| `heart.beat` | loop at the Heart's chest from the wake to the end of the Hall of Fame | 1 loop | - | synth | Lub-dub at 40 bpm, body 150 / 300 Hz + a soft clay knock (not only sub). |
| `heart.crack` | statue cracks while it wakes | 2 | 2 | **synth** | Gen takes are sparse small cracks. |
| `heart.wings` | wings unfold | 1 | - | synth | **Gen came back sub-only 4 times (2 prompts); stays synth.** |
| `heart.hum` | once when a whisper of the Heart starts | 1 | 1 | compare | Gen: breathy warm hum, maybe too tonal. New cue (was "voice.heart" in the plan; `voice.heart` stays the dialogue voice, unused). |
| `spark.chime` | Spirit on the palm, into the forehead, the spark leaving the eye | 2 | - | synth | |
| `credits.tablet` | each credits tablet rises | 3 | - | synth | Soft stone slide + set-down. |
| `gates.bloom` | the hub answers | 1 | - | synth | Crackle + water sparkle + rising whoosh + shimmer run. |
| `music.finale` | finale music | - | - | - | Empty slot (yours). |

### Battle / UI / typing
| Cue | Moment | Notes |
|---|---|---|
| `hp.low` | loops while your Isling is under 25 % HP; stops on heal, end, run | Existing cue, now hooked (BattleStage reads the bar). |
| `cry.<species>` | wild entrance (+0.45 s), send-out (+0.3 s), faint (0.8 pitch) | Hooked for all 50 species. |
| `spirit.focus` | when the lantern turns to a new hint target; also the objective card chime (0.6) | Existing cue, now hooked. |
| `spirit.beam` | faint hum on the target while the lantern shines | Existing cue, now hooked at 0.5 x shine. **Judge on the headset: it can be on for a while.** |
| typing | Spirit / Keeper: one tick per 6 characters at most, never closer than 0.2 s, 0.55 volume, pitch 0.94-1.06. Objective cards: no ticks, one soft chime. Heart: silent typing, one `heart.hum`. | Was a blip every 3 characters. |

### Landmark loops (P1)
`LandmarkLoops` (on [AudioDirector]): the nearest of each kind inside the island window gets one 3D loop: lit Hearth
Shrine (`shrine.flame`), ember vent (`ember.crackle`), hot spring tiles (`steam.hiss`), mills (`mill.turn`, Galewick
restored only). Off inside halls / lodge and in battles. The waterfall's loop lives in ScoopPour.

### Ambience profiles
GalewickFaded / GalewickRestored / LilymereFaded / LilymereRestored now use their own beds (`amb.galewick*`,
`amb.lilymere*`). Lilymere restored adds `frog.croak` as a sweetener; Galewick faded adds a rare far `sw.creak`.

## Cries
24 new: Kitelet, Kitewyrm, Vanehare, Compasshare, Whirlby, Windwhirl, Samarling, Samaroak, Zeppelope, Skyshiplope,
Nimbulpaca, Bottlewhale, Lighthousewhale, Drizzcan, Rainurn, Lilyvole, Lotusvole, Marimoss, Marimound, Sailfold,
Galleyfold, Riverloom, **Galeowl** (coo-coo), **Tidemother** (wah-hum). Base forms got their own syllables; evolutions
take the base voice lower and slower (the cry system's rule).
**AudioScope flags every cry, old and new** (tier mid level, gaps between syllables, and the Keepers' low share: Galeowl
0.8, Tidemother 0.8, Hearthstag 0.97). That's because cries were never moved to tier levelling ("until step C"). I kept
the new ones on the same peak levelling so they match the 26 you've heard. Moving all 50 to a tier is one decision for
you, not done.

## Gen clips that need your ear (AudioScope fails, kept)
| Clip | Why it fails | My read |
|---|---|---|
| `log.drop_1..6_FREE` | mid level 2-8 dB under the new louder tier; some tails > 0.25 | Peaky knocks: the peak cap wins. Still much louder than before. |
| `heart.crack_1/2_FREE` | mid -22 / -25 vs -18 | Sparse cracks; synth has the weight. |
| `ice.crack_1/2_FREE`, `ice.shatter_1/2_FREE` | mid -22 to -24 vs -18 | Crisp but peaky. |
| `water.scoop_1_FREE` / `_2_FREE` | mid low / silence 0.52 | |
| `water.spill_2_FREE` | silence 0.37 | |
Left out as weak (not imported): `amb.lilymere_1` (sparse ticks), `rain.soft_1` (near silent), `voice.galeowl_1`
(4 clipped buzzy hoots), `firebox.roar_1` (sub burst). Dropped: `amb.galewick_1` (6-24 s silent), `heart.wings_1..4` (sub).

## Credits
| | |
|---|---|
| Used this run | **2,936** (first pass 2,176 for 36 clips, retries 760) |
| Ledger total | 7,664 (free plan, all `_FREE`) |
| Left | **Can't read it**: the key has no `user_read` (401). If this account's month started at 10,000 and nothing else spent, about 2,336 are left. I stayed well inside "left minus 500". |

## Synth now, worth a gen take later (paid month)
`heart.wings` (needs a mid-range feather prompt that doesn't go sub), `tower.door` (a stone thunk at the end),
`firebox.roar`, `wind.sweep` (a real airy whoosh bed for the loop), `smoke.clear`, `cloud.part`, `mill.turn` (real wooden
creak loop), `credits.tablet`. All synth ones work as they are.

## Headset listening checklist (play order)
Start with the realms' sounds turned up in Settings. For each line: what you do, what you should hear.
1. **Typing:** any Spirit line: soft ticks, clearly fewer than before. An objective card (gold border): one soft chime, no ticks.
2. **Hub:** stand near the Hearth Shrine: its flame crackles softly (3D, louder as it's centred in the window).
3. **Cinderwood village:** cup the flame: a warm crackle that swells while you hold, then the rekindle whoomph + sparkle, small ticks round the plaza as the lanterns catch.
4. **The log:** lift and drop it on the ledge: **a solid wooden knock now** (was almost silent). Tell me which variant you like (Review tab).
5. **The hollow:** each lantern: glass tick + small flame; all lit: smoke rolls away (a long sigh).
6. **Forge ember:** pick it: a crackly tick; drop it in: sizzle + roar. Near the ember vents and hot springs: a soft crackle / steam hiss loop.
7. **Galewick:** sweep a hand: a whoosh that's louder for a faster sweep and fades as the gust crosses. Faded bed: still air.
8. **Big mill:** three flicks: a wooden creak loop while the drawbridge lowers, a thunk when it lands.
9. **Kites:** grab = small rope tug; let go high = a brighter tug. **Clouds:** a puff blown away = an airy sigh + sparkle.
10. **Tower:** the vanes creak as they spin up, then the door grinds open.
11. **Galeowl:** pinch a knot end (a rope creak), pull: a twang that rises per knot. After calming it, poke it: two soft hoots.
12. **Galewick restored:** breezier bed, creaks and birds. Mills in view creak softly.
13. **Lilymere faded:** dry reed air. **Pads:** a wooden tok + water slap, a frog answers; all in: a ripple runs + a little frog chorus.
14. **Rain garden:** palm down: soft rain under the cloud, it follows your hand; wiggle: heavier rain. A bud blooms: pop + sparkle.
15. **Scoop:** dip = splash; holding water = a slosh that grows when you move; pour = a stream; tip away = a spill.
16. **The fall:** three ice cracks down the fall, a shatter, then a running waterfall loop; the lagoon fills with a rising gurgle.
17. **Lilymere restored:** lapping water, frogs now and then, birds.
18. **A wind or water battle:** the wild cries as it appears, yours as it comes out of the pod, a lower cry when one faints. Get low on HP: a soft pulse, it stops when you heal or the battle ends.
19. **Finale:** cracks as the statue wakes, then **a slow heartbeat** that keeps going under the goodbye and the Hall of Fame; the Heart's whispers start with a breathy hum; wings = a big soft whoosh; the gates bloom = crackle + sparkle + rising whoosh; each credits tablet = a stone slide; the spark = small glassy chimes.
20. **The lantern's beam hum:** while the Spirit points at something there's a faint hum. Tell me if it's annoying (one line to turn off).
