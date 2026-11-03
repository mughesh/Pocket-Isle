# Pocket Isle — script v1 (Oct 24)

Lantern Spirit = **Spirit**. The creatures are **Islings** (never "creature" in a line). Bubbles are short: one idea per line, ≤ ~70 characters.
Format: `trigger_id` · speaker · lines (shown in order). **once** = only the first time.
Optional `· → target` (before the lines): the Spirit flies to that anchor while saying them and it pulses
(`leadPod`, `cradle`, `runes`, `nearPad`, `wildPad`, `shrine`, `lodgeCenter`, `pedestal`, `lodgeIsling`, `rimSocket`, `rimPod`,
`lodgeDoor`, `tile:x,y`, step 8: `shard`, `rim`, `starterPods`, `landmark:name` (WorldMap landmarks: `statue`, `firstGrass`, `pond`), `gate:fire` / `gate:water` / `gate:wind` /
`gate:home`, `gate:last`, step 8b: `ghost` (beside the ghost hand above the island), `goldTile`, step 8c: `shardFront`, `nearestRune`, `islandRim`). No target = it stays home. The target glows gold and the Spirit shines its lantern on it (step 7c).
Optional `· while <condition>` (step 8b): the line only shows while it holds and closes the moment it stops
(`battle`, `swap`, `canWalk`, `encounters`, `tutorial`, `wildRed`, `battle.out`; `!` not, `&` and, `|` or). A battle or a
scene swap starting closes every hint line that has no `while` saying it still applies.
Names are placeholders. The step 5c dialogue system loads this (convert to data asset).

## Opening (Still Isle)
Step 9h: less text before the starter (one line each; the lesson lines stay up until the lesson is done).
- `intro.shard` · objective · once · → shard (only if the shard isn't touched within 3 s)
  - Spirit: Something's here. Touch it?
- `intro.arrive` · once
  - Spirit: Hello, big one! I'm a Lantern Spirit.
- `intro.world` · objective · once · → rim
  - Spirit: This little island is yours. Grab its rocky edge to move it.
- `intro.starter` · objective · once · → starterPods
  - Spirit: Pick an Isling friend!
- `starter.kilnpot` · Spirit: Kilnpot! Warm as a fresh-baked pot.
- `starter.pebbloop` · Spirit: Pebbloop! Bubbly, and very round.
- `starter.pufflet` · Spirit: Pufflet! Light as a cloud, twice as fluffy.
- `intro.statue` · once · while !tutorial · → landmark:statue (first time within ~3 tiles of the statue)
  - Spirit: That statue has three empty holes... The world broke into floating pieces.
  - Spirit: I feel like I know it.

## Tutorials: lessons (step 8b)
One lesson at a time (Data/Onboarding/Onboarding_Main). The first line comes when the lesson starts and stays up until
the lesson is done (step 9h: after ~6 s it shrinks to a hint chip; looking at the chip or the Spirit opens it again); the
`.again` line only comes if the line was closed (its condition paused it). Shard, move and starter use the Opening lines above; send out, pick a move and catch use
`pod.first`, `battle.first` and `capture.first` below.
- `lesson.pinch` · objective · while tutorial · → ghost (step 9h, after `intro.arrive`: the ghost pinches beside your own hand)
  - Spirit: Tap your thumb and finger together. Like this!
- `lesson.pinch.once` · objective · while tutorial · → ghost (step 14a: after the first of the two pinches; chip "Pinch again")
  - Spirit: Nice! Once more.
- `lesson.pinch.done` (step 14a: the pinch lesson's praise)
  - Spirit: Perfect!
- `lesson.move.again` · objective · → rim
  - Spirit: Grab the rocky edge and slide it.
- `lesson.starter.again` · objective · → starterPods
  - Spirit: Look at one and pinch, or grab it.
- `lesson.walk` · objective · while canWalk · → ghost (Nov 3: any direction, dots count 4 tiles walked)
  - Spirit: Swipe your thumb left, right or forward to walk.
- `lesson.walk.again` · objective · while canWalk · → ghost
  - Spirit: Swipe any way. Watch your explorer go!
- `lesson.walk.turn` · objective · while canWalk · → ghost (step 12a)
  - Spirit: The path turns. Swipe again!
- `lesson.walk.turn.again` · objective · while canWalk · → ghost
  - Spirit: Swipe, and I'll follow the path.
- `lesson.walk.far` · objective · while canWalk · → ghost (step 14f: done on a tap stop, the gold tile or two swipe walks)
  - Spirit: Swipe to walk, tap to stop.
- `lesson.walk.far.again` · objective · while canWalk · → ghost
  - Spirit: Swipe to go. Tap your thumb to stop.
- `lesson.walk.help` · objective · while canWalk · → ghost
  - Spirit: Or look at a spot and pinch.
- `lesson.stop` · while canWalk · → ghost
  - Spirit: Swipe to walk, then tap your thumb to stop.
- `lesson.stop.again` · while canWalk · → ghost
  - Spirit: Swipe, then tap to stop.
- `lesson.look` · objective · while canWalk · → goldTile
  - Spirit: Look at the glowing spot and pinch.
- `lesson.look.again` · objective · while canWalk · → goldTile
  - Spirit: The glowing spot. Look, then pinch!
- `lesson.grass` · objective · while canWalk · → landmark:firstGrass
  - Spirit: Islings hide in tall grass. Let's peek!
- `lesson.grass.again` · objective · while canWalk · → landmark:firstGrass
  - Spirit: Follow the gold tiles into the tall grass.
- `lesson.send.again` · objective · while battle · → leadPod
  - Spirit: Toss your friend's pod onto the island!
- `lesson.rune.again` · objective · while battle · → nearestRune
  - Spirit: Poke a rune!
- `lesson.catch.again` · objective · while battle · → cradle
  - Spirit: Grab the big pod and flick it!
- `lesson.twohands` · objective · while !battle · → islandRim (step 8c, once after the first battle)
  - Spirit: Use both hands to turn or resize the island.
- `lesson.hands` · while tutorial
  - Spirit: Show me your hands!
- `praise.nice` · Spirit: Nice!
- `praise.like` · Spirit: Just like that!
- `praise.perfect` · Spirit: Perfect!

## Shard Gates and realms (step 8)
- `gate.sealed` · → gate:last (walking into or pinching a sealed gate)
  - Spirit: This one's still asleep. Maybe later.
- `gate.fire.first` · once · → gate:fire (first time the fire gate comes into the window)
  - Spirit: That arch is warm. It leads somewhere.
- `realm.cinderwood.first` · once · → landmark:firstGrass (first arrival in Cinderwood; step 9a: the first ember grass by the gate)
  - Spirit: Cinderwood. The fire here has gone quiet.
  - Spirit: Let's find out why.
- `hub.return.first` · once · → landmark:statue (first time back in the hub from a realm)
  - Spirit: Home again. The statue looks... different?

## Tutorials
- `tut.grass` · once · while encounters: Spirit: Tall grass rustles when an Isling is hiding in it.
- `tut.pond` · once · while encounters & !battle (first time next to water): Spirit: Face the water and tap. Something might bite!
- `pod.first` · objective · once · → leadPod (encounter start)
  - Spirit: Grab your friend's pod from the rim and toss it in!
- `battle.first` · objective · once · → nearestRune
  - Spirit: Poke a rune to attack!
- `battle.types` · once (first super or not-very effective hit)
  - Spirit: Water beats Fire. Fire beats Wind. Wind beats Water.
- `battle.win.first` · once: Spirit: You did it! Your friend is getting stronger.
- `battle.lose.first` · once: Spirit: Everyone's tired. Let's rest at the shrine.
- `capture.first` · objective · once · → cradle (first time the wild HP bar is red)
  - Spirit: It's weak now. Grab the big pod and flick it!
- `capture.locked` · once · → cradle (a pod thrown above red shatters)
  - Spirit: Weaken it first!
- `capture.dazed.first` · once · → cradle (first dazed wild Isling)
  - Spirit: It's dazed. Quick, a pod!
- `capture.success.first` · once: Spirit: A new Isling! It's in your pods now.
- `party.full` · once: Spirit: Your pods are full. New friends will wait for you at the Hearth Lodge.
- `evolve.first` · once · → nearPad (first evolution)
  - Spirit: Look at that! They've grown into something new.
- `shrine.first` · once · while !tutorial · → shrine (first time a shrine comes into the window)
  - Spirit: A Hearth Shrine! Warm your hand over the flame.
- `shrine.near` · once · while !tutorial · → shrine (step 8c: first time the explorer stands next to one)
  - Spirit: Warm your hand over the flame.
- `shrine.later` · → shrine (Nov 3: the shrine is locked until the tutorial is over: pinching it or a palm over it)
  - Spirit: We'll rest there later. Let's find a friend first!
- `lodge.first` · once · → lodgeCenter (first time inside the Hearth Lodge; the lodge hint chain starts, step 7c)
  - Spirit: This is the Hearth Lodge. Islings you've caught rest here.
- `lodge.inspect` · once · → lodgeIsling (chain 2: waits for an inspect, comes back after 15 s)
  - Spirit: Look at one and pinch to see how it's doing.
- `lodge.take` · once · → pedestal (chain 3, after the first inspect; then the gold moves to a rim socket)
  - Spirit: Pick it up and set it in a pod on the rim to bring it along.
- `lodge.leave` · once · → rimPod (chain 4, after the first swap)
  - Spirit: To leave one here, toss its pod onto the floor.
- `lodge.exit` · once · → lodgeDoor (chain 5, after chain 4 or 30 s)
  - Spirit: The door takes you back outside.
- `lodge.first.empty` · once · → lodgeCenter (step 8c: first visit with nobody in the lodge; the empty-lodge chain starts)
  - Spirit: This is the Hearth Lodge. Islings you leave here rest by the fire.
- `lodge.toss` · once · → rimPod (empty chain 2: waits for a pod let go on the floor; ghost toss)
  - Spirit: Toss a pod onto the floor to let one rest here.
- `lodge.inspect.one` · once · → lodgeIsling (empty chain 3, after it pops out; then lodge.take, lodge.exit)
  - Spirit: Look at it and pinch to see how it's doing.
- `lodge.last` · → leadPod (the last rim pod refuses to leave)
  - Spirit: Keep at least one Isling with you!
- `gate.first` · once
  - Spirit: A Shard Gate. It leads to another piece of the world.
  - Spirit: Step in when you're ready.
- `rare.first` · once (first gold sparkle): Spirit: Ooh, sparkly! That one's rare.
- `xp.first` · once · → nearPad (step 9f: the first XP after a win)
  - Spirit: Your Islings learn from every battle. Watch them grow!
- `steambara.first` · once · → wildPad (step 9f: the first wild Steambara)
  - Spirit: A water Isling, visiting the springs! Water beats fire...
- `menu.first` · once (step 9g: a few calm seconds after the lessons end, never in a battle)
  - Spirit: Palm up and pinch with your left hand any time to pause.

## Cinderwood (fire)
- `cw.arrive` · once (step 14c: the arrival line; Nov 3: a thorny vine blocks the trail ahead)
  - Spirit: Cinderwood... it used to glow all night.
- `cw.vine` · objective · → puzzle:cw.arrive (Nov 3: lesson at the arrival plaza's ember vent, the vine across the trail; chip "Pick it up!")
  - Spirit: Thorns on the trail! Pick up that ember and drop it on them.
- `cw.vine.tip` · objective · → vine (Nov 3: the ember picked up; the Spirit points at the vine, chip "Drop it on the vine!")
  - Spirit: Now drop it on the vine!
- `cw.vine.done` · once (Nov 3: the ember dropped on the vine: it burns away)
  - Spirit: Whoosh! The way is open.
- `cinder.village.first` · once · → landmark:plaza (step 9a: first time the explorer comes near the village plaza)
  - Spirit: A kiln village. The kilns are cold... everyone's gone quiet.
- `cinder.springs.first` · once · → landmark:springs (first time near a pool)
  - Spirit: Hot springs! Well, warm-ish. Not much steam.
- `cinder.log.first` · once · → landmark:log (first time near the fallen log)
  - Spirit: A huge log blocks the path. Too heavy for the explorer...
- `cinder.temple.first` · once · → landmark:temple (first time near the forge door; past the log, so from 9b)
  - Spirit: A forge in the cliff. The fire came from here.
- `cinder.ruins.first` · once · → landmark:ruins (first time in the kiln ruins)
  - Spirit: Old kilns. Something glazed is moving in the grass...
- `cinder.log.hands` · objective · once · → puzzle:cinder.log (step 9b: right after cinder.log.first; the ghost shows the lift)
  - Spirit: Maybe your big hands could move it?
- `cinder.log.done` · once (step 9b: the log set down on a ledge, the trail open)
  - Spirit: You're strong! Let's go on.
- `cw.temple` · once (first time at the temple door)
  - Spirit: The forge temple. Something big is breathing in there.
- `cinder.bellows.first` · objective · once · → puzzle:cinder.bellows (step 9b: first time the bellows are in reach; the ghost pokes the pad)
  - Spirit: Bellows! Poke them to send heat to the braziers.
- `cinder.brazier.one` · Spirit: One!
- `cinder.brazier.two` · Spirit: Two!
- `cinder.brazier.three` · Spirit: Three!
- `cinder.forge.open` · once (step 9b: all three braziers lit, the door glows and opens)
  - Spirit: The forge is waking up!
- `cinder.hall.first` · once · → hallKeeper (step 9b: first time inside the Forge Hall; Hearthstag comes in 9c)
  - Spirit: Something huge sleeps here... not yet.
- `cinder.keeper.first` · once · → hearthstag (step 9c: first time in the Forge Hall; then the Keeper battle)
  - Spirit: It's scared... the fire is trapped inside it.
  - Spirit: Let's calm it down. Gently!
- `keeper.shield.first` · once (first blocked hit)
  - Spirit: Its crown shields it! Water breaks it, or keep chipping away.
- `keeper.shield.broken.first` · once: Spirit: The shield's down. Now!
- `keeper.phase2` · once (below half HP): Spirit: It's fighting the fear. Keep going!
- `cinder.keeper.win` · once · → hearthstag (it kneels and calms)
  - Spirit: It wasn't angry. It was scared, holding all the fire in.
- `cinder.shard.take` · once · → fireShard (the fire shard floats to the player)
  - Spirit: The fire shard! Take it.
- `cinder.restored` · once (the restoration wave outside)
  - Spirit: Cinderwood is warm again!

Step 10f, Cinderwood's new hand moments (lesson lines stay pinned as a chip until the moment works once):
- `cw.flame` · objective · → puzzle:cw.flame (lesson at the kiln village's shrine, its flame nearly out; chip "Cup it!")
  - Spirit: The village flame is fading! Keep it warm with your hands.
- `cw.flame.cold` · once · → shrine (trying to rest at the shrine before it's rekindled)
  - Spirit: It's too weak to rest by. Warm it up first!
- `cw.flame.done` · once (the flame rekindled: the warmth rolls over the plaza)
  - Spirit: It's burning! Look, the whole village is waking up.
- `cw.lanterns` · objective · → puzzle:cw.hollow (lesson at the smoky hollow, once the village flame is rekindled; chip "Lanterns n/6"; step 14c: short, the arrival taught it)
  - Spirit: Smoke! Light the lanterns again.
- `cw.lanterns.done` · once (every lantern lit: the smoke burns off the path)
  - Spirit: The smoke's burning away! The path is clear.
- `cw.ember` · objective · → puzzle:cw.ember (lesson at the forge's ember vent, before the bellows; chip "Pick it up!")
  - Spirit: Another ember! Drop it in the forge's mouth.
- `cw.ember.tip` · objective · → firebox (step 12b: the ember picked up; the Spirit points at the firebox, chip "Tip it in!")
  - Spirit: Now tip it into the forge's mouth!
- `cw.forge.cold` · (the bellows pumped while the firebox is cold; at most every 8 s)
  - Spirit: The forge is stone cold. It needs a spark first.
- `cw.ember.done` · once (the ember dropped into the firebox: it roars)
  - Spirit: It's roaring! Now the bellows can blow.

## Galewick (wind, step 10a)
- `gate.wind.first` · once · → gate:wind (first time the wind gate comes into the window, once it leads somewhere)
  - Spirit: Listen... no wind through this tunnel at all.
- `realm.galewick.first` · once · → landmark:firstGrass (first arrival in Galewick)
  - Spirit: Galewick... it's never this quiet.
  - Spirit: The grass isn't even swaying.
- `gw.arrive` · once (step 14a: a few tiles past the arrival, the cloud puffs on the path ahead)
  - Spirit: Galewick... the air isn't moving at all.
- `gw.arrive.clouds` · objective · → puzzle:gw.arrive (step 14a: the realm's signature gesture right after the portal; chip "Sweep", then "Clouds n/2")
  - Spirit: Clouds are stuck on the path. Sweep your hand to get the wind going!
- `gw.arrive.done` · once (step 14a: the last puff gone, a gust ripples across the meadow)
  - Spirit: There's the wind! Feel it run through the grass?
- `gw.village.first` · once · → landmark:plaza (first time near the village plaza)
  - Spirit: Windmills, and not one sail turning.
- `gw.mill.first` · once · → landmark:mill (first time by the big mill and the ditch)
  - Spirit: The bridge is up. That big mill must lower it.
- `gw.kites.first` · once · → landmark:kites (first time in the kite field)
  - Spirit: Kites, lying in the grass. Nobody to fly them.
- `gw.ridge.first` · once · → landmark:ridge (first time near the ridge path)
  - Spirit: A cloud sits right on the path. Stuck, like everything.
- `gw.tower.first` · once · → landmark:tower (first time near the wind tower)
  - Spirit: The wind tower. Its vanes should be spinning.
- `gw.rare.first` · once · → landmark:rare (first time in the rare grass)
  - Spirit: This grass feels different. Something odd floats here.

Step 10b, the hand moments (lesson lines stay pinned as a chip until the moment works once):
- `gw.mill` · objective · → puzzle:gw.mill (lesson at the big mill; chip "Flick!")
  - Spirit: The mill is stuck. Give its sails a flick!
- `gw.mill.done` · once (three flicks: the sails keep turning, the drawbridge comes down)
  - Spirit: There it goes! The bridge is coming down.
- `gw.kites` · → puzzle:gw.kites (lesson in the kite field; chip "Pull it up!")
  - Spirit: Kites! Lift one up into the sky.
- `gw.kites.task` · objective · → puzzle:gw.kites (step 11d: the kite field's lesson says the task; chip "Kites n/3")
  - Spirit: The tower needs kites. Let's fly all three!
  - Spirit: Pinch one and lift it up into the sky.
- `gw.kites.one` · Spirit: One!
- `gw.kites.two` · Spirit: Two!
- `gw.kites.three` · Spirit: All three!
- `gw.gust` · objective · → puzzle:gw.clouds (at the cloud bank; chip "Sweep!"; step 14a: short, the arrival taught it)
  - Spirit: More stuck clouds. Sweep them away!
- `gw.gust.done` · once (the last puff blown off the ridge path)
  - Spirit: The path is clear! Feel that breeze?
- `gw.tower.kites` · → landmark:tower (at the tower door with fewer than three kites)
  - Spirit: The tower needs all three kites.
- `gw.tower.open` · once (the kites tie onto the crown, the vanes turn)
  - Spirit: The kites caught the wind. The door is opening!
  - Spirit: The tower's open! Let's go in.
- `gw.meet.kitelet` · once (step 10c: the first wild Kitelet)
  - Spirit: A Kitelet! It wants to fly, but there's no wind.
- `gw.meet.zeppelope` · once (step 10c: the rare Zeppelope)
  - Spirit: A Zeppelope! It's drifting... it never lands!

Step 10d / 10e, the tower hall:
- `gw.keeper.knots` · objective · → galeowl (lesson in the tower hall; chip "Pull apart!")
  - Spirit: It's all tangled up! Pull the knots apart.
- `gw.keeper.meet` · once · → galeowl (all three knots undone: it shakes free and flares up)
  - Galeowl: Stay still! If the wind blows, the pieces blow away!
  - Spirit: It's scared. Let's calm it down!
- `gw.keeper.shield` · once · → galeowl (the Gust Wall goes up the first time)
  - Spirit: A wall of wind! Fire should break it.
- `gw.keeper.shield.break` · once · → galeowl
  - Spirit: The wall is down. Now!
- `gw.keeper.win` · once · → galeowl (it calms and bows)
  - Galeowl: I held the wind so nothing else would fly away.
  - Galeowl: But a still world is a stuck world.
  - Galeowl: Thank you, guardian. Let it blow.
- `gw.shard.take` · once · → windShard (step 10e: the wind shard floats out of the window to the player)
  - Spirit: The wind shard! Grab it.
- `gw.restored` · once (the restoration wave from the tower)
  - Spirit: Feel that? Galewick is breathing again!

## Lilymere (water, step 11a)
- `gate.water.first` · once · → gate:water (first time the well gate comes into the window, once it leads somewhere)
  - Spirit: Listen... the well is silent. Not even a drip.
- `realm.lilymere.first` · once · → landmark:firstGrass (first arrival in Lilymere)
  - Spirit: Lilymere... the lagoon used to reach right up to here.
  - Spirit: Half the water is gone, and nothing ripples.
- `lm.arrive.stream` · objective · → puzzle:lm.stream (step 14a: the empty stream by the arrival; chip "Palm down", then "Pads n/4")
  - Spirit: The stream's run dry, and the lily pads are stuck in the mud! Hold your hand over it like a little cloud.
- `lm.arrive.stream.done` · once (step 14a: the stream runs, the pads float into a bridge)
  - Spirit: Look, the pads are floating! A bridge, just for us.
- `lm.village.first` · once · → landmark:village (first time near the stilt village)
  - Spirit: Stilt huts, standing over almost nothing.
- `lm.channel.first` · once · → landmark:channel (first time at the lily channel)
  - Spirit: The lily pads have all drifted apart.
- `lm.garden.first` · once · → landmark:garden (first time at the lotus garden)
  - Spirit: A lotus garden, dried to cracks. The buds won't open.
- `lm.basin.first` · once · → landmark:basin (first time at the waterfall basin)
  - Spirit: The waterfall froze mid-fall. Look, the drops just hang there.
- `lm.temple.first` · once · → landmark:temple (first time near the lake temple)
  - Spirit: A temple, half under the lagoon. Someone is hiding in there.
- `lm.marsh.first` · once · → landmark:marsh (first time in the reed marsh)
  - Spirit: Reeds taller than you! Stay close.
- `lm.pond.first` · once · → landmark:pond (first time at the rare pond)
  - Spirit: This little pond feels deep. Something big could live in it.

Step 11b, the hand moments (lesson lines stay pinned as a chip until the moment works once):
- `lm.pads` · objective · → puzzle:lm.pads (lesson at the lily channel; chip "Push!")
  - Spirit: The channel's too wide. Nudge the lily pads together!
- `lm.pads.done` · once (all three pads locked into a path)
  - Spirit: A lily pad bridge! Hop across.
- `lm.rain` · objective · → puzzle:lm.rain (at the lotus garden; chip "Palm down!"; step 14a: short, the stream taught it)
  - Spirit: The buds are thirsty. Rain on them!
- `lm.rain.done` · once (every bud bloomed into a stepping stone)
  - Spirit: Look at them bloom! A path of lotus.
- `lm.scoop` · objective · → puzzle:lm.lagoon (lesson at the waterfall basin; chip "Scoop!")
  - Spirit: The falls are frozen. Bring them some water!
- `lm.scoop.more` · Spirit: One more!
- `lm.scoop.done` · once (the waterfall runs, the lagoon rises)
  - Spirit: It's flowing! The whole lagoon is filling up!
- `lm.meet.sailfold` · once (step 11c: the first wild Sailfold)
  - Spirit: A Sailfold! Folded from an old map, I bet. It sails anywhere.
- `lm.meet.bottlewhale` · once (step 11c: the rare Bottlewhale)
  - Spirit: A Bottlewhale! Look, there's a tiny ship inside it!
- `lm.temple.shut` (step 11c: at the lake temple's door before the lagoon rises)
  - Spirit: The temple's sunk. The water's too low to open it.
- `lm.temple.open` · once (the lagoon rises: the temple door surfaces and glows)
  - Spirit: The temple door is open! Let's go in.
- `lm.keeper.meet` · once · → tidemother (step 11c: in the lake temple, she clutches the hoarded water)
  - Tidemother: Stay back! This water is all that's left. I won't lose it again!
  - Spirit: She's so scared. Let's calm her down.
- `lm.keeper.shield` · once · → tidemother (the Lily Shell goes up the first time)
  - Spirit: She's hiding in her shell. Wind could lift it off!
- `lm.keeper.shield.break` · once · → tidemother
  - Spirit: The shell's open. Now!
- `lm.keeper.win` · once · → tidemother (she calms and lets the water go)
  - Tidemother: I watched it drain away once. I was so afraid of losing it again.
  - Tidemother: Thank you, guardian. Water is meant to flow.
- `lm.shard.take` · once · → waterShard (step 11c: the water shard floats out to the player)
  - Spirit: The water shard! Grab it.
- `lm.restored` · once (the restoration wave from the temple)
  - Spirit: Listen... the whole lagoon is singing again!

## Objectives (step 11d)
- `objective.more` · objective (step 11d: an objective's object is outside the island window; a gold arrow points to it; once per moment a session)
  - Spirit: One more over there!
- `spirit.poke.idle` (step 12a: the Spirit poked with no objective active; its gold trail shows the way)
  - Spirit: This way! Follow my gold trail.

## Statue and finale
- `hub.shard.first` · objective · once · → statueSocket (step 9c: in the hub holding a shard)
  - Spirit: Put the shard in the statue. Something's calling it.
- `hub.shard.placed` · once (the shard locks in): Spirit: I... feel warmer. Strange.
- `hub.shard.more` · once: Spirit: Two more shards are out there.
- `statue.two` · once (step 10e: the second shard locks into the statue)
  - Spirit: Two pieces home. It's almost awake...
- `statue.three` · once (step 11c: the third shard locks into the statue; the finale comes later)
  - Spirit: All three pieces home... Something is waking up!
- `statue.shard.1`
  - Spirit: The statue's warmer. And I feel... brighter?
- `statue.shard.2`
  - Spirit: Two shards. I can almost remember something.
- `statue.shard.3`
  - Spirit: Three! Something's waking up...
Step 13: each shard that locks in makes the statue whisper a memory in the Heart's voice (its own bubble: no tail, soft
gold-white, letters fading in), then the Spirit answers. The third starts the finale (FinaleDirector).
- `heart.dream.fire` · once (the fire shard locks in)
  - Heart: ...I remember warmth. Kilns glowing all night long...
- `heart.dream.wind` · once (the wind shard locks in)
  - Heart: ...I remember the wind. Kites so high they touched the clouds...
- `heart.dream.water` · once (the water shard locks in)
  - Heart: ...I remember rain. Every pond full of lotus...
- `spirit.dream.1` · once (after the first whisper)
  - Spirit: Did the statue just... speak?
- `spirit.dream.2` · once (after the second whisper)
  - Spirit: That voice... why does it feel like mine?

## Finale (step 13)
The third shard wakes the statue: it becomes the Heart of the World. Walking, wild battles and the menu's Title wait.
- `heart.wake` (the Heart lifts its head)
  - Heart: ...little light. You found your way home.
- `spirit.finale.1` (the Spirit floats before the Heart's empty forehead)
  - Spirit: I think... I'm a piece of it. Its light.
- `spirit.finale.2` · objective (pinned; chip "Hold out your hand")
  - Spirit: I have to go back. Will you take me?
- `spirit.finale.hand` · objective (the Spirit sits on your palm; chip "Lift it to the Heart")
  - Spirit: Lift me up to the Heart. Gently.
- `spirit.finale.wait` (30 s and nothing happened: it never forces it)
  - Spirit: It's okay. Whenever you're ready.
- `spirit.finale.3` (as it leaves your palm)
  - Spirit: Thank you, big one. For everything.
- `heart.thanks` (its eyes open, the hub answers)
  - Heart: Guardian. You carried the world in your hands.
- `heart.friends` (the Hall of Fame: your party comes out)
  - Heart: And these are the friends who carried it with you.
- `spark.hello` (a new spark hops out of your explorer's lantern)
  - Spirit: ...hello again. I'll always be in your light.

## Post-game (step 13)
- `heart.collection` (poke the Heart or look at it + pinch: the collection card shows)
  - Heart: Every friend you make warms the world a little more.
- `heart.collection.full` (every realm's roster complete)
  - Heart: Every Isling of every realm. The world is whole, guardian.
- `spark.rare.cinder` (the Spark's idle hint: Cinderwood still has Islings to meet)
  - Spirit: I heard something rare by the kiln ruins...
- `spark.rare.gale` (Galewick still has Islings to meet)
  - Spirit: Something's fluttering in the high grass of Galewick...
- `spark.rare.lily` (Lilymere still has Islings to meet)
  - Spirit: I saw ripples in the Lilymere reeds. Someone's hiding!
- `keeper.spar` · → hallKeeper (poke a calm Keeper: Spar? / Not now)
  - Spirit: It wants a friendly match! Spar?
- `keeper.spar.bow` (after the friendly match, win or lose)
  - Spirit: What a match! It bows to you.

## Quick Journey (step 9g, the quick save slot only; a Galewick journey since step 14c)
- `quick.gate` · once · → gate:wind (Nov 3: no gift; after the capture lesson, back in the hub: the Spirit points at the wind gate)
  - Spirit: Bring back the wind shard!
- `quick.end` · once (the wind shard locks in, the statue whispers; then the Keep exploring / See the ending / Title runes float in over the statue)
  - Spirit: You woke the first piece of the world!
  - Spirit: Two more realms are waiting.
  - Spirit: Keep exploring, or peek at how it all ends?
- `quick.ending.go` · once (Nov 3: See the ending picked; the fire and water shards appear and fly into the statue, then the finale)
  - Spirit: Let's dream ahead... all the way to the end.
  - Spirit: Imagine every shard found!

## Idle hints (random, rare, only when nothing is happening for ~40 s)
- Spirit: Your Islings get stronger every battle.
- Spirit: Every gate leads somewhere different. Just saying.
- Spirit: Psst. Rare ones hide in tall grass.
- Spirit: You can move the island anytime. Grab the edge.
