# TEST NOW — one headset pass (Nov 12)

Build: a QA build of branch **`panel-review`** (master `0cc5c7eb` + the panel's Tier 1 + the Nov 11 evening fix list). Nothing of it has been on the headset.
Your Nov 11 pass is kept as `TEST-NOW-nov11-filled.md`; everything you marked `[x]` there is in this build.
Play a **Quick Journey** from the start, top to bottom, then the Cinderwood / Lilymere / finale lines with the Dev panel jumps. For each line: do it, then mark it.
**Mark only failures:** change `[ ]` to `[x]` and write what you saw after the arrow. Leave passes alone.
When done, tell Claude Code: **"Fix every [x] in Assets/Docs/TEST-NOW.md."** Nothing else to remember.

## 1. Opening and lessons (Quick Journey)
- [ ] Move lesson ignored: it closes quietly after about **25 s**; the two-hand lesson after about **20 s**. Both still teach when you do them →
- [ ] Walk lesson: ignore the swipe for **12 s**: "Or look at a spot and pinch." takes the line's place →
- [ ] **Stop practice:** any thumb tap that stops the walk counts ("Nice stop!" + a small burst where you stopped); a stop on or next to the gold tile says **"Perfect!"** with the big burst; 2 stops pass it →
- [ ] Any Spirit / Isling bubble, head tilted or looking down at one off to the side: it stays **level** →
- [ ] A chip or bubble **never sits over the explorer** (it moves up); check at the first grass and at every door →

## 2. Walking (the rules changed; walk around deliberately)
- [ ] From grass toward a road: the walk **stops on the road** (first road tile), never across it into the grass beyond →
- [ ] Along a road: follows bends, stops at junctions, and at the road's end it **stops on the last road tile** (never wanders into the grass) →
- [ ] Standing on a road, swipe toward grass: it steps off (one swipe = off the road) →
- [ ] Nothing small (hay, boulders, reeds) catches you beside or in a road anywhere; the north lane east of Galewick's plaza is open (the wall gap) →
- [ ] **Never off the island**: in any held view (a hand moment, the lotus garden, the mill) the walk stops at the island's edge with a bump; the explorer never stands in the room →
- [ ] Look at a tile + pinch while a view is held: it walks only as far as the island shows →
- [ ] **Long walks still go on**: on open grass and along a long road, one swipe keeps walking past where the island's edge was when you swiped (it must not stop every ~7 tiles) →

## 3. First battle and catch
- [ ] Your turn: the Isling's **own element move glows and breathes** (grows and shrinks, ~1 s) until you pick; the others stay normal →
- [ ] Wild turn: nothing breathes; pressing a move only shakes it →
- [ ] Catch: "Caught!" sparkle and "Gotcha! X joined you!", then the "New Isling!" tag; the Spirit's own line comes **after the tag has gone** →

## 4. Galewick
- [ ] The dandelion's first showing: a small **poke ghost** over it; it fades when your hand comes near; the walk stops beside it →
- [ ] The **lodge (hearth shrine)** sits beside the road just before the mill and the ditch (east side, north of the mill), not in the old inner pocket; palm over its bowl: a **wide bright ring** fills with a rising tone and a chime when full, then in →
- [ ] **Windmill**: as you come near, the mill **and the raised bridge** are both in the window; flick the sails: the bridge lowers **in view**, the view holds a second, then returns to you →
- [ ] Tower door with your **2** Islings: straight in (Quick Journey); with 1: "Bring at least 2 Islings" →
- [ ] Tower hall: cream floor, slate walls, warmer light: better than before? →
- [ ] **Galeowl speaks: an owl call, then hoots** as it types (the new voices are now the default; Dev panel > Tools > "Sound A/B" > Voices switches back to the blip) →
- [ ] Galeowl fight: "Galeowl's turn...", "Galeowl used Gale Shot!", at the end "**Galeowl is calm!**"; never "Wild Galeowl" →
- [ ] Shard in the statue: no pod glows and no "X is glowing..." line through the end card; Keep exploring: now it may →
- [ ] See the ending: the **Skip** tablet sits beside the pods (your left), never on them →

## 5. Cinderwood (Dev panel jump)
- [ ] Touch the ember: fingertip burns with a **new whoomp** as it catches (the crackle after it is still the old one); poke the thorns: they **burn for ~2.5 s** (fire spreads along them, they darken, embers) before they crumble; the sound is a crackle-hiss-crumble →
- [ ] Wait 20 s with a burning finger: the flame goes back, walking still works →
- [ ] Village flame: cup it: the **ring under it is wide and bright** and fills; the sound rises; a chime when it catches →
- [ ] **Smoky hollow**: the path there is a straight 2-wide band (no zig-zag); the line says "Touch the village flame, then poke the lanterns": touch the flame: fingertip catches; poke each lantern: a whoosh, it lights, your finger stays lit; a cold finger does nothing; look + pinch still lights one →
- [ ] Lit lanterns: a quiet steady burning sound over the line (new; too loud, too quiet, or fine?) →
- [ ] Ember-lily bloom: poke ghost the first time, then poke: embers puff + heal wave →
- [ ] Lift the log, let go anywhere over the island: it glides into place; bellows fine →

## 6. Lilymere (Dev panel jump)
- [ ] Arrival over the pad bridge and the lotus path: the explorer **stays on the island** throughout (the Nov 11 bug) →
- [ ] "Walk in, or look and pinch" at the temple door: the chip sits above, **not over the explorer** →
- [ ] Lotus bloom: poke ghost the first time; pour: tilt slowly: a trickle, then a stream; tilt back: it stops →
- [ ] Basin rim, reeds and lily pads look clean; reeds no longer crowd the roads →

## 7. Finale and the Heart
- [ ] The Heart's first line: the deep synth **"moooo" with a new singing-bowl and choir tone on top**; Dev panel > Tools > "Sound A/B" > Voices: blip gives the plain moo + bell back. Which do you prefer? (Its typing murmurs are unchanged) →
- [ ] Sky-lantern credits read well; the credits say paid sounds (no "free plan") →

## 8. Whole run
- [ ] Frame rate (Dev panel FPS): tower fight, the ridge with kites, a heal wave, the hollow with all lanterns lit, the finale →
- [ ] Anything stalled or froze? Dev panel > Report bug right then, and write where →
- [ ] Colours on the headset: still dull after the hall change? (T2-8 in Panel-Review.md) →

## Known not done (don't test)
- Starter card element symbol; the swipe ghost re-recording (yours).
- Zig-zags: only the smoky hollow was straightened; the rest of the path audit's "jogs" are 2-wide corners and plaza edges (pictures in `Logs/WalkSim`).
