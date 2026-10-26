# Audio Phase B: hooking sounds into the game

Written by the audio lane (branch `audio`). The audio lane only added files; everything below needs an existing file
changed, so it happens on the main lane after `audio` is merged. Target: Nov 4.

## 1. Setup (one-time)
| What | Change |
|---|---|
| Assembly | Add `"Diorama.Audio"` to `references` in `Assets/_Project/Scripts/Runtime/Diorama.Runtime.asmdef` (and to `Diorama.Editor.asmdef` if editor tools need cue ids). `Diorama.Audio` has no Meta references. |
| Namespace | `using Diorama.Audio;` then `AudioService.Play(AudioIds.PodThrow, pos)`. Ids are constants in `Assets/_Project/Audio/Runtime/AudioIds.cs` (generated; rerun **Tools > Pocket Isle > Audio > Sync Cues** after adding cues). |
| Table | `Assets/_Project/Audio/Data/Resources/AudioCueTable.asset` loads on first use. Nothing to put in a scene. `AudioService.SetTable(t)` overrides it. |
| Island scale | Each frame or on scale change: `AudioService.DistanceScale = island.transform.lossyScale.x / defaultScale;` (IslandHandle.UpdateTransform / IslandPlacer.ApplyScale). |
| Pause | Quest pause: Unity pauses all AudioSources itself. GamePause can also set `AudioListener.pause = paused`. |
| LFS | `*.wav` is LFS. If Lyria music arrives as `.mp3` / `.ogg`, add `*.mp3` / `*.ogg` LFS lines to `.gitattributes` first (the audio lane may not edit it). |

### API cheatsheet
| Call | Use |
|---|---|
| `AudioService.Play(id, Vector3 pos, step = 0)` | 3D one-shot at a point |
| `AudioService.Play(id, Transform follow, step = 0)` | 3D one-shot that follows (a pod in flight) |
| `AudioService.Play2D(id, step = 0)` | UI, jingles, head-locked |
| `var h = AudioService.PlayLoop(id, transform)` / `PlayLoop2D(id)` | loop; keep `h`, later `h.Stop(0.3f)`, `h.SetPitch(1 + fill)`, `h.SetVolume(v)` |
| `AudioService.SetMusic(id, crossfade)` / `StopMusic(fade)` | music |
| `AudioService.Duck(true/false)` | music -6 dB during dialogue and jingles |
| `AudioService.GetBusVolume / SetBusVolume(AudioBus.SFX, v)` | Settings menu (saved in PlayerPrefs `audio.bus.<Bus>`) |
| `step` | rising sequences: `Play2D(AudioIds.XpTick, step: n)` raises by the cue's `pitchStepSemitones` per step |

A handle goes invalid by itself when the sound ends or the voice is reused, so storing one is safe.
**Fake-null:** the service checks follow targets with `!= null`; a destroyed follow target ends its loop.

## 2. Event -> cue -> where the call goes
Script paths are under `Assets/_Project/Scripts/Runtime/`. "Most likely" = found by reading; confirm the exact line.

### Island, walking
| Event | Cue | Script / method |
|---|---|---|
| Rim grab starts | `island.grab` | `IslandHandle.BeginTransform` (or the `GrabStarted` event) |
| Rim released | `island.release` | `IslandHandle.EndTransform` |
| Two-hand scale | `island.scale` (loop; Kenney creak is an alternative) | start in `BeginTransform` when two hands; `SetPitch` / `SetVolume` from the scale speed in `UpdateTransform`; stop in `EndTransform` |
| Recenter / re-place | `island.recenter` | `IslandPlacer.OnRecentered` / `Place(reason)` |
| Each step lands on a tile | `step.grass` / `step.path` / `step.stone` / `step.wood` by tile, or `walk.hop` | `GestureTestPiece.Update` where `OnStep?.Invoke(_tile)` fires (pick by `Map.At(tile)`: `G` grass, `T` tall grass, `P` path, lodge = wood, halls = stone) |
| Tall grass stepped | `grass.rustle` | same place, `OnTallGrassStep` |
| Walk stops | `walk.stop` | `GestureTestPiece.Stop` |
| Bumped into a blocker | `walk.bump` | `GestureTestPiece.TryStep` (`OnBump`) |
| Hop on tap | `walk.hop` | `GestureTestPiece.Hop` |
| Fishing tap on water | `pond.tap` | `GestureTestPiece.TapWhileStanding` (`OnFishTap`) |

### Encounter, zoom, battle
| Event | Cue | Script / method |
|---|---|---|
| Grass rustles before a wild | `enc.rustle` | `BattleStage.Rustle` |
| Wild appears | `enc.sting` (or `enc.rare` when rare) | `BattleStage.WildAppears(rare, ..)` |
| Water ripple / splash-out | `pond.splash` | `BattleStage.Ripple` / `WildAppears` with `waterTile` |
| Camera zooms in / out | `zoom.in` / `zoom.out` | `WorldWindow.ZoomTo` / `ZoomOut` (or `ZoomedIn` / `ZoomedOut` events) |
| Clearing sinks | `clearing.rumble` | `EncounterDirector.Encounter` (stage fade / clearing) and `BattleStage.RisePads(false)` |
| Pads rise | `pads.rise` | `BattleStage.RisePads(true)` |
| Battle music | `music.battle.wild` / `music.battle.keeper` | `EncounterDirector.Encounter` start; back to the map's music in `Finish` |
| Move rune hover | `rune.hover` | `BattleRunes.OnPokeState` (hover) and the gaze focus in `Update` |
| Rune chosen | `rune.select` | `BattleRunes.Pick` |
| Run chosen | `battle.run` | `BattleRunes.Pick` when it's the run rune |
| Move charge | `move.charge` | `BattleStage.PlayMove` start |
| Shoot launch | `move.shoot.<element>` | `BattleStage.Shoot` (element from `ElementStyle` / `MoveData`) |
| Shoot impact | `move.impact.<element>` | `BattleStage.Impact` |
| Lunge | `move.lunge` | `BattleStage.Lunge` |
| Slam | `move.slam` | `BattleStage.Slam` (on the impact action) |
| Shield up / pop | `move.shield.up` / `move.shield.pop` | `BattleStage.RaiseShield` / `PopShield` |
| Hit | `hit.normal` / `hit.super` / `hit.weak` | `BattleStage.Hit` (effectiveness on the `BattleEvent`) |
| Faint | `faint` + `cry.<species>` faint variant (see 4) | `BattleStage.Play` on the faint event / `FadeWild` |
| Low HP | `hp.low` (loop) | `BattleStage.ShowPlayerBar`: start below ~25% HP, stop on heal / end |
| Win | `jingle.victory` (duck music) | `EncounterDirector.Reward` |
| XP ticks | `xp.tick` with `step` | `BattleStage.PlayXp` (one per tick of the filling bar) |
| Level up | `jingle.levelup` | `BattleStage.PlayXp` on each `LevelUp` |

### Pods, catching
| Event | Cue | Script / method |
|---|---|---|
| Lead pod glows | `pod.lead` | `PodRack.PresentCradle` / `WarmWave` |
| Pod grabbed | `pod.grab` | `PodRack.OnGrabbed` |
| Pod thrown | `pod.throw` (follow the pod) | `PodRack.OnReleased` / `Thrown` event |
| Pod opens, Isling out | `pod.open` | `BattleStage.PodBurst` |
| Recall into pod | `pod.return` | `BattleStage.RecallPlayer` |
| Capture pod appears | `catch.appear` | `PodRack.PresentCradle` (catch phase) |
| Pod hits the wild | `catch.hit` | `EncounterDirector.CatchThrow` (hit callback) / `BattleStage.SuckIntoPod` |
| Wobble | `catch.wobble` | `PodRack.Wobble` |
| Caught | `catch.success` + `jingle.catch` | `EncounterDirector.PlayCatch` success / `Caught` event |
| Breaks free | `catch.shatter` | `PodRack.Shatter` / `BattleStage.BreakFree` |
| Dazed stars | `catch.dazed` (loop) | `EncounterDirector.Dazed` / `BattleStage.DizzyStars`; stop when it ends |
| Pod flies to the rim | `catch.toRim` | `PodRack.FlyHome` |
| Pod streaks to storage (party full) | `catch.toStorage` | `PodRack.StreakAway` |
| Missed throw | `pod.throw` already playing; optional `walk.bump` on landing | `PodRack.Miss` |

### Growth, Spirit, shrines, travel
| Event | Cue | Script / method |
|---|---|---|
| Evolution glow | `evo.rise` (loop while it rises) then `evo.burst` + `jingle.evolve` | `PodRack.EvolveInPod` / `GrowthDirector.EvolveAll` |
| Spirit appears | `spirit.appear` | `LanternSpirit.Start` / first `Place` |
| Spirit speaks | `spirit.blip` every 2-3 visible characters | `DialogueBubble` typewriter (`_text.maxVisibleCharacters` update) when the speaker is Spirit; `voice.keeper` / `voice.heart` for the others (see 4); `AudioService.Duck(true)` while a line shows |
| Spirit beam / point | `spirit.beam` (loop) | `Dialogue.SetTarget` / `UpdateTarget` (the focus beam) |
| Focus pulse | `spirit.focus` | `FocusHighlight` pulse |
| Shrine flame | `shrine.flame` (loop, 3D at each shrine) | `HearthShrines.Scan` per shrine; stop in `OnDestroy` |
| Palm fills the ring | `shrine.fill` (loop) + `SetPitch(1 + fill)` | `HearthShrines.UpdateShrine` (`warming`) |
| Flame flares | `shrine.flare` | `HearthShrines.Flare` |
| Healed | `shrine.heal` | `HearthShrines.Enter` (`Rested`) |
| Saved | `ui.saved` | `GameSave.Save(reason, puff: true)` / `Puff` |
| Scene sinks / rises | `scene.sink` / `scene.rise` | `SceneSwap.Swap` (before / after `SwapAtBottom`) |
| Gate hum | `gate.hum` (loop, 3D per portal) | `ShardGates.Scan` / `UpdatePortal` |
| Enter a gate | `gate.enter` | `ShardGates.Enter` |
| Gate sealed | `gate.sealed` | `ShardGates.OnBump` / `Use` when locked |
| Lodge pod tick | `lodge.podTick` | `LodgeRoom.OnRimPicked` / `Wake` |
| Lodge door, page | `lodge.door` / `lodge.page` | `LodgeRoom.Leave` + `OnEntered` / `NextPage` |

### Realm, puzzles, Keeper, opening
| Event | Cue | Script / method |
|---|---|---|
| Lantern flicker | `lantern.sputter` | lantern props (Cinderwood) when they dim |
| Ember bed | `ember.crackle` (loop) | `RealmDrift` kind Embers (one 3D loop at the island, not per particle) |
| Hot spring | `steam.hiss` (loop) | hot spring tiles (`H`); one loop near the springs |
| Realm restored | `restore.swell` + `amb.cinderwood.restored` | `RealmRestoration.Wave` / `FadeTo(1)` |
| Fallen log lift / land | `log.lift` | `FallenLog.OnLifted` / `OnLanded` (replaces `PuzzleSounds.Thud`) |
| Bellows stroke | `bellows.pump` | `ForgeBellows.OnStroked` (replaces `PuzzleSounds.Puff`) |
| Brazier lights | `brazier.light` | `ForgeBellows.Arrive` (replaces `PuzzleSounds.Whoosh`) |
| Forge door | `forge.door` | `ForgeBellows.OpenDoor` (replaces `PuzzleSounds.Rumble`) |
| Keeper crown shield | `keeper.crown.shield` | `KeeperHall.Meet` (crown flares) and `OnPresented` when the shield rises again |
| Crown cracks | `keeper.crown.crack` | `KeeperHall.OnPresented` (shield broken) |
| Fury | `keeper.fury` | `KeeperFury.Stomp` / `Snort` / phase 2 start |
| Keeper win | `jingle.keeper` | `KeeperHall.Victory` |
| Shard hum | `shard.hum` (loop) | `Opening.MakeShard` (until it cracks); `KeeperHall.ShardRise` |
| Shard cracks | `shard.crack` | `Opening.Spark` / `SetShardGlow` crack |
| Shard collected | `jingle.shard` | `KeeperHall.CollectShard` |
| Island unfolds | `island.unfold` | `Opening.NewGame` (the island rise) |
| Explorer stretches | `explorer.stretch` | `Opening` after the island lands / `ExplorerView.Cheer` |

### Ambience and music (2D)
| Where | Cue | Script / method |
|---|---|---|
| Still Isle (hub) | `amb.stillisle` + `music.hub` | on map load / `SceneSwap.TravelFinished` |
| Cinderwood | `amb.cinderwood` (or `.restored` after the Keeper) + `music.cinderwood` | same |
| Lodge | `amb.lodge` + `music.lodge` | `LodgeRoom.OnEntered`; back in `OnReturned` |
| Pond nearby | `amb.pond` (3D loop at the pond, quiet) | `WorldWindow.Follow` when water is in view |
| Title | `music.title` | `Opening.Start` |

Ambience loops play with `PlayLoop2D(id, 1.5f)`; keep the handle per map and `Stop(1.5f)` on travel.

## 3. Procedural placeholders to remove
| File | What | Replace with |
|---|---|---|
| `Onboarding/Onboarding.cs` | `MakeChime()` (line ~457) feeds `_audio.clip` | `AudioService.Play2D(AudioIds.UiSaved)` or a lesson chime cue; delete `MakeChime` / `Note` and the AudioSource |
| `Puzzles/PuzzleSounds.cs` | `Thud`, `Puff`, `Whoosh`, `Rumble` | `log.lift`, `bellows.pump`, `brazier.light`, `forge.door`; delete the file once nothing uses it |
| `Puzzles/FallenLog.cs` | uses `PuzzleSounds.Thud` | `AudioService.Play(AudioIds.LogLift, logTransform)` |
| `Puzzles/ForgeBellows.cs` | uses `PuzzleSounds.Puff / Whoosh / Rumble` | the three cues above; delete its AudioSource |

## 4. Cries and voices
- `cry.<speciesId>` per species: `AudioIds.Cry(SpeciesRegistry.IdOf(species))`. The id is the species **asset name**,
  case-sensitive (`cry.Kilnpot`, `cry.Hearthstag`). All 26 registry species have one, `LanternSpirit` included.
  Battle: on `WildAppears`, on send-out (`PodBurst`), at faint. Lodge: `LodgeRoom.Wake` / `Inspect`.
- New species later: run **Tools > Pocket Isle > Audio > Make Cry Profiles (missing only)**, then Audio Lab **Create
  missing**, **Bake all**, **Sync cues**. An evolution gets its base form's voice automatically.
- Variants come from pitch, not extra clips: grab the handle and set pitch after play:
  - **hurt:** `Play(AudioIds.Cry(id), actor)` then `h.SetPitch(1.25f)` and stop early `h.Stop(0.15f)` after ~60% of the clip.
  - **faint:** `h.SetPitch(0.8f)` (slower and lower in one; Unity pitch changes both).
- Dialogue blips: `spirit.blip` (Spirit), `voice.keeper` (Hearthstag), `voice.heart` (Heart), one every 2-3 characters while typing.

## 5. Project Settings > Audio on Quest (main lane; ProjectSettings is off limits for the audio lane)
Current `ProjectSettings/AudioManager.asset`: DSP buffer 1024 (`m_DSPBufferSize`), output 48000 Hz, real voices 32,
virtual 512, spatializer plugin **Meta XR Audio**.
- **DSP buffer:** "Best latency" (256) or "Good latency" (512) for snappier taps; 1024 adds ~21 ms. Try 512 on device and
  listen for crackle under load.
- **Sample rate:** 48 kHz output matches our clips (no resampling). Keep it.
- **Real voices:** 32 covers our pool (16 3D + 6 2D + 2 music = 24). Don't go lower.
- **Spatializer:** AudioService sets `spatialize = false` on its sources, so the Meta XR Audio plugin isn't used (plain
  built-in panning, as the brief asks). If the team later wants HRTF, set `spatialize = true` in `AudioService.MakeVoice`.
- **Import:** Clips/ import rules are automatic (`AudioImportRules`); check that no Android override is set by hand.

## 6. Check on device after hookup
1. Cold start: no `[Audio] Unknown cue id` errors in the log; one `[Audio] AudioService up: 16 3D + 6 2D voices, N cues.`
2. Walk 20 tiles on grass, path and tall grass: steps never cut out, no repeats that sound machine-gunned.
3. A battle with 3 moves each: music ducks under the victory jingle, comes back.
4. Pause the app (Quest button) and resume: loops continue, no stuck loops.
5. fps stays at 72 in a battle with all loops running (shrine flame + gate hum + ambience).

## 3. Step audio-B: new cues (Oct 27)
All are in the cue table and `AudioIds`. "?" = the script isn't on the audio branch or the exact spot needs a look.

| Event | Cue | Script / method |
|---|---|---|
| Finger pokes the floating shard | `shard.touch` (3D, 3 variants) | `Opening.Contact(at, push, how)` |
| Logo rises out of the statue | `logo.rise` | `Opening` where the logo starts rising (`WarmUp` / `Spark`?) |
| Looked-at starter pod grows and wiggles | `pod.wiggle` | `Opening.UpdateStarters` when the focused pod starts its wiggle |
| Lesson done ("Nice!") | `ui.lesson` (2D) | `Onboarding.End(l, done: true)`; replaces `MakeChime` |
| Hold-rune fill starts / released early | `rune.hold` (2.4 s rise that ends in a ting at 2 s) | hold-rune script (main lane, ?): `var h = AudioService.Play(AudioIds.RuneHold, pos);` on hold start, `h.Stop(0.1f)` if released before it fills (`Play` returns a handle) |
| Game menu opens / closes | `menu.open`, `menu.close` (2D) | `GamePause.SetPaused(true/false)` or the menu script (?) |
| Gold path tiles appear | `path.gold` (3D at the first gold tile) | `Onboarding.GoldPath` / `PickGoldTile` |
| Spirit gifts two pods | `gift.pods` | the quick-gift beat (Spirit / `PodRack`?) |
| Log lands on the ledge | `log.drop` | `FallenLog.OnLanded` |
| Glow runs along the pipe | `pipe.glow` | `ForgeBellows.Pulse(i, strokes)` start |
| Brazier coals warm | `brazier.warm` | `ForgeBellows.OnStroked` (first strokes) / `DrawBrazier` warming |
| Keeper stomps | `keeper.stomp` | `KeeperFury.Stomp()` / `KeeperHall.OnBattleStomp(feet)` |
| Keeper smoke snort | `keeper.snort` | `KeeperFury.Snort()` |
| Crown crystal breaks | `keeper.crown.break` | `KeeperRules.Break()` |
| Keeper calms, aura burns off | `keeper.calm` | `KeeperHall.Victory()` start |
| Shard floats over (loop) | `shard.float` (loop) | `KeeperHall.ShardRise()` start, stop in `CollectShard` |
| Shard docks in the rim holder | `shard.dock` | `KeeperHall.ShardToHolder()` end |
| Shard locks into the statue | `statue.socket`, then `jingle.shard` | the end beat (main lane, ?) |
| Settings pebble tick | `ui.pebble` | settings menu (P1, ?) |

Also new in step B:
- **Synth footsteps:** `step.grass`, `step.path`, `step.stone`, `step.wood` now hold 6 synth variants **and** the Kenney ones. Mugesh picks in the Review tab; until then the cue plays both at random.
- **Loudness:** every cue plays at volume 0.8. The loud / quiet order is now baked into the clips (loudness tiers). Don't trim cue volumes to fix the balance; tell the audio lane instead.
- **Imported clips** (Kenney, later ElevenLabs) are played through levelled copies in `Clips/Levelled/`; the source files aren't referenced and stay out of the build.

### Gen batch (ElevenLabs) and the release guard
- `Tools > Pocket Isle > Audio > Import Gen Batch` (or **Import gen** in the Lab toolbar) copies new clips from the gen lane's manifest into `Clips/Gen/<cueId>/` and adds them to the cues next to the synth variants. Safe to re-run.
- **Free-plan clips (`_FREE`) have no commercial licence.** `Tools > Pocket Isle > Audio > Release Check` lists them, and `ReleaseBuildGuard` (an `IPreprocessBuildWithReport`) **fails non-development builds** while any cue holds one. Development builds still go through for headset tests.
- `.mp3` is not in Git LFS. This batch is all `.wav` (LFS). If a later batch brings `.mp3`, add `*.mp3 filter=lfs diff=lfs merge=lfs -text` to `.gitattributes` first (the audio lane doesn't edit it).

## 4. Ambience (step audio-B)
`AmbienceDirector` (runtime, created on first use) plays a 2D bed per area and scatters sweeteners as 3D one-shots on the
island surface. Profiles: `Assets/_Project/Audio/Data/Ambience/` (StillIsle, CinderwoodFaded, CinderwoodRestored,
ForgeHall, Lodge, Battle). Reference them from the scene / area scripts (add an `AmbienceProfile` field).

| When | Call | Likely spot |
|---|---|---|
| Game start on the Still Isle, and back from a realm | `AmbienceDirector.SetArea(stillIsle, 2f)` | `Opening.Finish` / `ShardGates.OnTravelFinished(to)` |
| Enter Cinderwood | `SetArea(restored ? cinderwoodRestored : cinderwoodFaded, 2f)` | `ShardGates.OnTravelFinished(to)` (pick by the realm's saved restored flag) |
| Cinderwood restores | `SetArea(cinderwoodRestored, 4f)` (long crossfade with `restore.swell`) | `KeeperHall.Restore()` |
| Enter / leave the Forge Hall | `SetArea(forgeHall, 1.5f)` / back to the Cinderwood one | `KeeperHall.OnSwapFinished(inside)` |
| Lodge open / close | `SetArea(lodge, 1f)` / back | `LodgeRoom` open, `LodgeRoom.Close` |
| Battle starts / ends | `AmbienceDirector.Duck(-6f, 0.5f, pauseSweeteners: true)` / `Duck(0f, 1f)`; for wind only use `SetArea(battle)` and restore the area after | `BattleStage` start / `OnBattleEnded` |
| Dialogue line shows / ends | `Duck(-6f, 0.3f)` / `Duck(0f, 0.6f)` (sweeteners keep playing) | `Dialogue` show / hide |
| Island moved or scaled | `AmbienceDirector.Bounds = new IslandBounds(playSurfaceCentreWorld, 0.35f * islandScale, 0.05f * islandScale);` | `IslandHandle.EndTransform`, `IslandPlacer.Place`, scale change (same place as `AudioService.DistanceScale`) |

Until `Bounds` is set, sweeteners use a 0.35 m disc 0.5 m in front of the listener and log one warning.
Sweetener cues: `sw.crackle`, `sw.drip`, `sw.gust`, `sw.creak`, `sw.pebble` (synth); `sw.bird`, `sw.leaves`, `sw.insect` wait
for the gen batch (skipped until they have clips). Beds `amb.*` hold the synth placeholder and the gen bed (pick in Review).
Audition without Play mode: **Audio Lab > Ambience tab**, pick a profile, Play (a minute, panned), Next take for another.
