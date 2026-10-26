# Pocket Isle — sound list by priority (Oct 26, 2026)

No Isling cries for now (step audio-C, later). Music is separate (Lyria).
**P0** = the judges hear it in the Quick Journey (about 10 min), often or at a key moment. **P1** = heard less often, or supporting.
**P2** = full adventure or polish.
Source: **S** = synth (batch 1 is decent: chimes, runes, jingles, pod ping, Spirit), **G** = generate (organic, magic,
ambience), **S+G** = generated body + synth sparkle layer, **K** = Kenney (have).

## P0 — Opening and magic (first 60 seconds: sets the tone)
| Moment | Sound | Cue | Src |
|---|---|---|---|
| Shard drifts into the room | soft magical hum, slowly swelling | `shard.hum` (loop) | S+G |
| **Poke / touch the shard** | glassy shimmer + twinkle, answers the finger | `shard.touch` (new) | S+G |
| Shard cracks open | crack + bright light burst | `shard.crack` | S+G |
| Island grows out of it, tiles rise | earth and pebbles rising, warm swell | `island.unfold` | G |
| Explorer pops up, hops | tiny cute pop and hop | `explorer.stretch`, `walk.hop` | S |
| Spark becomes the Spirit | chime + soft flutter | `spirit.appear` | S |
| Spirit talks | wordless speech blips | `spirit.blip` | S |
| Logo rises out of the statue | magical rise, stone + shimmer | `logo.rise` (new) | S+G |
| Starter pods rise, look at one (grows, wiggles) | glass hum up, small wiggle tick | `pod.lead`, `pod.wiggle` (new) | S |
| Lesson done ("Nice!") | soft chime | `ui.lesson` (new, replaces Onboarding's built-in chime) | S |

## P0 — Hands, island, runes (constant)
| Moment | Sound | Cue | Src |
|---|---|---|---|
| Grab / release the rim | soft stone scrape, settle | `island.grab`, `island.release` | G |
| Rune looked at / poked / picked | very soft tick / wood + bell | `rune.hover`, `rune.select` | S |
| Hold-rune fills gold (2 s) | rising fill tone | `rune.hold` (new) | S |
| Game menu open / close (pause) | soft "whoomp" down / up | `menu.open`, `menu.close` (new) | S+G |
| Gold path tiles appear | light sparkle trail | `path.gold` (new) | S |

## P0 — Walk and encounter (constant)
| Moment | Sound | Cue | Src |
|---|---|---|---|
| Footsteps (walk and run) | tiny steps: grass, path, stone | `step.grass`, `step.path`, `step.stone` (8 variants) | G (K now) |
| Walking through tall grass | short soft rustle per step | `grass.rustle` | G |
| Encounter | big rustle + small rising sting | `enc.rustle`, `enc.sting` | G + S |
| Zoom in / out | soft whoosh | `zoom.in`, `zoom.out` | G |
| Clearing opens, pads rise | grass bending, small earth thumps | `clearing.rumble`, `pads.rise` | G |

## P0 — Battle and catch (every battle)
| Moment | Sound | Cue | Src |
|---|---|---|---|
| Grab pod / throw / it opens, Isling pops out / back to pod | glass tick, whoosh, glass pop + sparkle, rising zip | `pod.grab`, `pod.throw`, `pod.open`, `pod.return` | G + S |
| Moves (Fire, Water, Neutral first; Wind P1) | charge, shoot, impact per element | `move.charge`, `move.shoot.*`, `move.impact.*` | G |
| Lunge / slam / shield up / shield pop | whoosh, ground thud, glassy swell, pop | `move.lunge`, `move.slam`, `move.shield.*` | G (S for shield) |
| Hits | normal / super (brighter) / weak (dull) | `hit.normal`, `hit.super`, `hit.weak` | G + S |
| Faint | soft falling "boop" + flop | `faint` | S+G |
| Cradle pod appears, throw, hit | glass hum, whoosh, clink | `catch.appear`, `pod.throw`, `catch.hit` | S+G |
| Wobble ×1–3, success | wood clicks, bright click + sparkle | `catch.wobble`, `catch.success` | G + S |
| Shatter ("Weaken it first!") | small glass break | `catch.shatter` | K + S |
| Dazed loop (8 s) | dizzy twinkle stars | `catch.dazed` | S |
| Pod flies to the rim | whoosh + clink | `catch.toRim` | S+G |
| +XP bar fill, level up | ticking rise, chime + ring | `xp.tick`, `jingle.levelup` | S |
| Catch / win jingles | short marimba/bell | `jingle.catch`, `jingle.victory` | S |

## P0 — Cinderwood story beats (one each, key moments)
| Moment | Sound | Cue | Src |
|---|---|---|---|
| Quick gift: two pods fly from the Spirit | sparkle whooshes + clinks | `gift.pods` (new) | S+G |
| Gate travel: tiles sink, base tints, map rises | rumble down, magical swap, rumble up | `scene.sink`, `gate.enter`, `scene.rise` | G |
| Gate idle | portal swirl hum | `gate.hum` | S+G |
| Lift the fallen log | wood strain, leaves and dust falling | `log.lift` | G |
| Log drops on the ledge (tiles bounce) | heavy wood thud + rattle | `log.drop` (new) | G |
| Bellows pump (12 pokes) | leather squash + air puff | `bellows.pump` | G |
| Glow runs along the pipe | soft rising whoosh | `pipe.glow` (new) | S+G |
| Brazier coals warm / lights ("One!") | ember hiss / fire whoomph | `brazier.warm` (new), `brazier.light` | G |
| Forge door sinks | stone grind, steam hiss, sparks | `forge.door` | G |
| **Keeper:** opening rear-up stomp, stomps | deep earthy thump, rocks rattle | `keeper.stomp` (new) | G |
| Keeper smoke snorts | breathy snort + smoke puff | `keeper.snort` (new) | G |
| Crown shield blocks ("Blocked!") / crystal cracks / breaks | glassy clang / crack / shatter | `keeper.crown.shield`, `keeper.crown.crack`, `keeper.crown.break` (new) | S+G |
| Phase 2 flare | fire surge | `keeper.fury` | G |
| Calms: aura burns off into gold motes | warm magical release | `keeper.calm` (new) | S+G |
| Forge ignites, realm restores (~5 s wave) | big warm swell + crackle | `restore.swell` | S+G |
| Shard floats over, grab it, docks in the rim holder | hum, shimmer, satisfying lock | `shard.float`, `shard.dock` (new) | S+G |
| Shard locks into the statue (end) | deep lock + glow swell + moss falling | `statue.socket` (new), `jingle.shard` | S+G |

## P0 — Ambience (beds + island sweeteners)
| Area | Bed | Sweeteners on the island |
|---|---|---|
| Still Isle | calm meadow breeze, faint birds | small birds, leaf flutter, soft gusts, pond drips near the pond |
| Cinderwood (faded) | hushed warm forest, dry leaves, far crackle | ember pops near vents, creaks, rare bird |
| Cinderwood (restored) | brighter, birds back, warm crackle | birds, crackles, steam near the springs |
| Forge Hall | big quiet stone room, low ember glow hum | single ember pops, chain creak |
| Battle | wind only, quiet (under music) | none |

## P1
| Moment | Cue | Src |
|---|---|---|
| Stop tap, hop, bump into an obstacle | `walk.stop`, `walk.hop`, `walk.bump` | S / G |
| Two-hand turn / resize | `island.scale` | K |
| Recenter | `island.recenter` | G |
| Wind moves (no Wind Islings in the Quick Journey) | `move.*.wind` | G |
| Low HP pulse, Run | `hp.low`, `battle.run` | S |
| Hearth Shrine: idle flame, palm fill, flare, heal wave, "Saved" | `shrine.*`, `ui.saved` | G + S |
| Gate sealed bump | `gate.sealed` | S |
| Pond tap / fishing, splash | `pond.tap`, `pond.splash` | G |
| Evolution rise + burst | `evo.rise`, `evo.burst` | S |
| Lanterns sputter, ember vents, hot springs (3D loops on tiles) | `lantern.sputter`, `ember.crackle`, `steam.hiss` | G |
| Rare encounter sting | `enc.rare` | S |
| Settings pebble tick | `ui.pebble` (new) | S |
| Spirit lantern beam hum, gold focus pulse | `spirit.beam`, `spirit.focus` | S |

## P2
Lodge (door, page arrow, pod tick, hearth bed), revisit-Hearthstag poke snort, Credits scroll, kiln ruins
ambience, Water and Wind realm sounds.
