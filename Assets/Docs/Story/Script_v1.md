# Pocket Isle — script v1 (Oct 24)

Lantern Spirit = **Spirit**. The creatures are **Islings** (never "creature" in a line). Bubbles are short: one idea per line, ≤ ~70 characters.
Format: `trigger_id` · speaker · lines (shown in order). **once** = only the first time.
Optional `· → target` (before the lines): the Spirit flies to that anchor while saying them and it pulses
(`leadPod`, `cradle`, `runes`, `nearPad`, `wildPad`, `shrine`, `lodgeCenter`, `lodgeInspected` (14k, was `pedestal`), `lodgeIsling`, `rimSocket`, `rimPod`,
`lodgeDoor`, `tile:x,y`, step 8: `shard`, `rim`, `starterPods`, `landmark:name` (WorldMap landmarks: `statue`, `firstGrass`, `pond`), `gate:fire` / `gate:water` / `gate:wind` /
`gate:home`, `gate:last`, step 8b: `ghost` (beside the ghost hand above the island), `goldTile`, step 8c: `shardFront`, `nearestRune`, `islandRim`). No target = it stays home. The target glows gold and the Spirit shines its lantern on it (step 7c).
Optional `· while <condition>` (step 8b): the line only shows while it holds and closes the moment it stops
(`battle`, `swap`, `canWalk`, `encounters`, `tutorial`, `wildRed`, `battle.out`; `!` not, `&` and, `|` or). A battle or a
scene swap starting closes every hint line that has no `while` saying it still applies.
Names are placeholders. The step 5c dialogue system loads this (convert to data asset).

## Opening (Still Isle)
Step 9h: less text before the starter (one line each; the lesson lines stay up until the lesson is done).
- `intro.shard` · in hub · objective · once · → shard (only if the shard isn't touched within 3 s)
  - Spirit: Something's here. Touch it?
- `intro.arrive` · in hub · intro · once
  - Spirit: Hello, big one! I'm a Lantern Spirit.
- `lesson.move` · in outside · objective · once · → rim (step 14i: the move lesson's line, was `intro.world`; chip "Grab a corner" in 14j; the ghost pinches a corner and moves it)
  - Spirit: Grab a corner, then move me wherever is comfy.
- `intro.starter` · in hub · objective · once · → starterPods
  - Spirit: Pick an Isling friend!
- `starter.kilnpot` · in hub · story · Spirit: Kilnpot! Warm as a fresh-baked pot.
- `starter.pebbloop` · in hub · story · Spirit: Pebbloop! Bubbly, and very round.
- `starter.pufflet` · in hub · story · Spirit: Pufflet! Light as a cloud, twice as fluffy.
- `intro.statue` · in hub · story · once · while !tutorial · → landmark:statue (first time within ~3 tiles of the statue)
  - Spirit: That statue has three empty holes... The world broke into floating pieces.
  - Spirit: I feel like I know it.

## Tutorials: lessons (step 8b)
One lesson at a time (Data/Onboarding/Onboarding_Main). The first line comes when the lesson starts and stays up until
the lesson is done (step 9h: after ~6 s it shrinks to a hint chip; looking at the chip or the Spirit opens it again); the
`.again` line only comes if the line was closed (its condition paused it). Shard, move and starter use the Opening lines above; send out, pick a move and catch use
`pod.first`, `battle.first` and `capture.first` below.
- `lesson.pinch` · in outside · objective · while tutorial · → ghost (step 9h, after `intro.arrive`: the ghost pinches beside your own hand)
  - Spirit: Tap your thumb and finger together. Like this!
- `lesson.pinch.once` · in outside · objective · while tutorial · → ghost (step 14a: after the first of the two pinches; chip "Pinch again")
  - Spirit: Nice! Once more.
- `lesson.pinch.done` · in outside · story (step 14a: the pinch lesson's praise)
  - Spirit: Perfect!
- `lesson.move.again` · in outside · objective · → rim
  - Spirit: Grab a corner, then move it.
- `lesson.starter.again` · in hub · objective · → starterPods
  - Spirit: Look at one and pinch, or grab it.
- `lesson.walk` · in outside & !battle · objective · while canWalk · → ghost (Nov 3: any direction, dots count 4 tiles walked)
  - Spirit: Swipe your thumb left, right or forward to walk.
- `lesson.walk.again` · in outside & !battle · objective · while canWalk · → ghost
  - Spirit: Swipe any way. Watch your explorer go!
- `lesson.walk.turn` · in outside & !battle · objective · while canWalk · → ghost (step 12a)
  - Spirit: The path turns. Swipe again!
- `lesson.walk.turn.again` · in outside & !battle · objective · while canWalk · → ghost
  - Spirit: Swipe, and I'll follow the path.
- `lesson.walk.far` · in outside & !battle · objective · while canWalk · → ghost (step 14f: done on a tap stop, the gold tile or two swipe walks)
  - Spirit: Swipe to walk, tap to stop.
- `lesson.walk.far.again` · in outside & !battle · objective · while canWalk · → ghost
  - Spirit: Swipe to go. Tap your thumb to stop.
- `lesson.walk.help` · in outside & !battle · objective · while canWalk · → ghost
  - Spirit: Or look at a spot and pinch.
- `lesson.stop` · in outside & !battle · story · while canWalk · → ghost
  - Spirit: Swipe to walk, then tap your thumb to stop.
- `lesson.stop.again` · in outside & !battle · story · while canWalk · → ghost
  - Spirit: Swipe, then tap to stop.
- `lesson.look` · in outside & !battle · objective · while canWalk · → goldTile
  - Spirit: Look at the glowing spot and pinch.
- `lesson.look.again` · in outside & !battle · objective · while canWalk · → goldTile
  - Spirit: The glowing spot. Look, then pinch!
- `lesson.grass` · in outside & !battle · objective · while canWalk · → landmark:firstGrass
  - Spirit: Islings hide in tall grass. Let's peek!
- `lesson.grass.again` · in outside & !battle · objective · while canWalk · → landmark:firstGrass
  - Spirit: Follow the gold tiles into the tall grass.
- `lesson.send.again` · in battle · objective · while battle · → leadPod
  - Spirit: Toss your friend's pod onto the island!
- `lesson.rune.again` · in battle · objective · while battle · → nearestRune
  - Spirit: Poke a rune!
- `lesson.catch.again` · in battle · objective · while battle · → cradle
  - Spirit: Grab the big pod and flick it!
- `lesson.twoHands` · in outside · objective · while !battle · → islandRim (step 14i: right after the move lesson, both slots; chip "Two hands")
  - Spirit: Grab two corners and pull apart to make it bigger.
- `lesson.hands` · in any · story · while tutorial
  - Spirit: Show me your hands!
- `praise.nice` · in any · story · Spirit: Nice!
- `praise.like` · in any · story · Spirit: Just like that!
- `praise.perfect` · in any · story · Spirit: Perfect!

## Shard Gates and realms (step 8)
- `gate.sealed` · in outside · story · → gate:last (walking into or pinching a sealed gate)
  - Spirit: This one's still asleep. Maybe later.
- `gate.fire.first` · in hub · story · once · → gate:fire (first time the fire gate comes into the window)
  - Spirit: That arch is warm. It leads somewhere.
- `realm.cinderwood.first` · in cinderwood · intro · once · → landmark:firstGrass (first arrival in Cinderwood; step 9a: the first ember grass by the gate)
  - Spirit: Cinderwood. The fire here has gone quiet.
  - Spirit: Let's find out why.
- `hub.return.first` · in hub · intro · once · → landmark:statue (first time back in the hub from a realm)
  - Spirit: Home again. The statue looks... different?

## Tutorials
- `tut.grass` · in outside · story · once · while encounters: Spirit: Tall grass rustles when an Isling is hiding in it.
- `tut.pond` · in outside · story · once · while encounters & !battle (first time next to water): Spirit: Face the water and tap. Something might bite!
- `pod.first` · in battle · objective · once · → leadPod (encounter start)
  - Spirit: Grab your friend's pod from the rim and toss it in!
- `battle.first` · in battle · objective · once · → nearestRune
  - Spirit: Poke a rune to attack!
- `battle.types` · in battle · story · once (first super or not-very effective hit)
  - Spirit: Water beats Fire. Fire beats Wind. Wind beats Water.
- `battle.win.first` · in any · story · once: Spirit: You did it! Your friend is getting stronger.
- `battle.lose.first` · in any · story · once: Spirit: Everyone's tired. Let's rest at the shrine.
- `capture.first` · in battle · objective · once · → cradle (first time the wild HP bar is red)
  - Spirit: It's weak now. Grab the big pod and flick it!
- `capture.locked` · in battle · story · once · → cradle (a pod thrown above red shatters)
  - Spirit: Weaken it first!
- `capture.dazed.first` · in battle · story · once · → cradle (first dazed wild Isling)
  - Spirit: It's dazed. Quick, a pod!
- `capture.success.first` · in any · story · once: Spirit: A new Isling! It's in your pods now.
- `party.full` · in any · story · once: Spirit: Your pods are full. New friends will wait for you at the Hearth Lodge.
- `evolve.first` · in any · story · once · → nearPad (first evolution)
  - Spirit: Look at that! They've grown into something new.
- `evolve.ready` · in any · objective · → evolvePod (step 14h: a pod Isling is ready to evolve and it's calm; pinned until it evolves, chip "Pod on your palm", "Turn your palm up" once held; Nov 4: a ghost hand grabs the pod and turns palm up; {name} = its name)
  - Spirit: {name} is glowing! Take its pod and hold it on your open palm.
- `evolve.done` · in any · story (step 14h: after an evolution in the palm; {name} = the new form)
  - Spirit: {name}! So strong now.
- `journal.first` · in any · story · once · while !tutorial (step 14h: the first "New Isling!" toast)
  - Spirit: Every Isling you meet goes in your Journal. Open the menu to see it!
- `shrine.first` · in outside · story · once · while !tutorial · → shrine (first time a shrine comes into the window)
  - Spirit: A Hearth Shrine! Warm your hand over the flame.
- `shrine.near` · in outside · story · once · while !tutorial · → shrine (step 8c: first time the explorer stands next to one)
  - Spirit: Warm your hand over the flame.
- `shrine.later` · in outside · story · → shrine (Nov 3: the shrine is locked until the tutorial is over: pinching it or a palm over it)
  - Spirit: We'll rest there later. Let's find a friend first!
- `lodge.first` · in lodge · intro · once · → lodgeCenter (first time inside the Hearth Lodge; the lodge hint chain starts, step 7c)
  - Spirit: This is the Hearth Lodge. Islings you've caught rest here.
- `lodge.inspect` · in lodge · story · once · → lodgeIsling (chain 2: waits for an inspect, comes back after 15 s)
  - Spirit: Look at one and pinch to see how it's doing.
- `lodge.take` · in lodge · story · once · → lodgeInspected (chain 3, after the first inspect; then the gold moves to a rim socket)
  - Spirit: Pick it up and set it in a pod on the rim to bring it along.
- `lodge.leave` · in lodge · story · once · → rimPod (chain 4, after the first swap)
  - Spirit: To leave one here, toss its pod onto the floor.
- `lodge.exit` · in lodge · story · once · → lodgeDoor (chain 5, after chain 4 or 30 s)
  - Spirit: The door takes you back outside.
- `lodge.first.empty` · in lodge · intro · once · → lodgeCenter (step 8c: first visit with nobody in the lodge; the empty-lodge chain starts)
  - Spirit: This is the Hearth Lodge. Islings you leave here rest by the fire.
- `lodge.toss` · in lodge · story · once · → rimPod (empty chain 2: waits for a pod let go on the floor; ghost toss)
  - Spirit: Toss a pod onto the floor to let one rest here.
- `lodge.inspect.one` · in lodge · story · once · → lodgeIsling (empty chain 3, after it pops out; then lodge.take, lodge.exit)
  - Spirit: Look at it and pinch to see how it's doing.
- `lodge.last` · in lodge · story · → leadPod (the last rim pod refuses to leave)
  - Spirit: Keep at least one Isling with you!
- `gate.first` · in outside · story · once
  - Spirit: A Shard Gate. It leads to another piece of the world.
  - Spirit: Step in when you're ready.
- `rare.first` · in any · story · once (first gold sparkle): Spirit: Ooh, sparkly! That one's rare.
- `xp.first` · in any · story · once · → nearPad (step 9f: the first XP after a win)
  - Spirit: Your Islings learn from every battle. Watch them grow!
- `steambara.first` · in battle · story · once · → wildPad (step 9f: the first wild Steambara)
  - Spirit: A water Isling, visiting the springs! Water beats fire...
- `menu.first` · in outside & !battle · story · once (step 9g: a few calm seconds after the lessons end, never in a battle)
  - Spirit: Palm up and pinch with your left hand any time to pause.

## Cinderwood (fire)
- `cw.arrive` · in cinderwood · intro · once (step 14c: the arrival line; Nov 3: a thorny vine blocks the trail ahead)
  - Spirit: Cinderwood... it used to glow all night.
- `cw.vine` · in cinderwood · objective · → puzzle:cw.arrive (Nov 3: lesson at the arrival plaza's ember vent, the vine across the trail; chip "Pick it up!")
  - Spirit: Thorns on the trail! Pick up that ember and drop it on them.
- `cw.vine.tip` · in cinderwood · objective · → vine (Nov 3: the ember picked up; the Spirit points at the vine, chip "Drop it on the vine!")
  - Spirit: Now drop it on the vine!
- `cw.vine.done` · in cinderwood · story · once (Nov 3: the ember dropped on the vine: it burns away)
  - Spirit: Whoosh! The way is open.
- `cinder.village.first` · in cinderwood · story · once · → landmark:plaza (step 9a: first time the explorer comes near the village plaza)
  - Spirit: A kiln village. The kilns are cold... everyone's gone quiet.
- `cinder.springs.first` · in cinderwood · story · once · → landmark:springs (first time near a pool)
  - Spirit: Hot springs! Well, warm-ish. Not much steam.
- `cinder.log.first` · in cinderwood · story · once · → landmark:log (first time near the fallen log)
  - Spirit: A huge log blocks the path. Too heavy for the explorer...
- `cinder.temple.first` · in cinderwood · story · once · → landmark:temple (first time near the forge door; past the log, so from 9b)
  - Spirit: A forge in the cliff. The fire came from here.
- `cinder.ruins.first` · in cinderwood · story · once · → landmark:ruins (first time in the kiln ruins)
  - Spirit: Old kilns. Something glazed is moving in the grass...
- `cinder.log.hands` · in cinderwood · objective · once · → puzzle:cinder.log (step 9b: right after cinder.log.first; the ghost shows the lift)
  - Spirit: Maybe your big hands could move it?
- `cinder.log.done` · in cinderwood · story · once (step 9b: the log set down on a ledge, the trail open)
  - Spirit: You're strong! Let's go on.
- `cw.temple` · in cinderwood · story · once (first time at the temple door)
  - Spirit: The forge temple. Something big is breathing in there.
- `cinder.bellows.first` · in cinderwood · objective · once · → puzzle:cinder.bellows (step 9b: first time the bellows are in reach; the ghost pokes the pad)
  - Spirit: Bellows! Poke them to send heat to the braziers.
- `cinder.brazier.one` · in cinderwood · story · Spirit: One!
- `cinder.brazier.two` · in cinderwood · story · Spirit: Two!
- `cinder.brazier.three` · in cinderwood · story · Spirit: Three!
- `cinder.forge.open` · in cinderwood · story · once (step 9b: all three braziers lit, the door glows and opens)
  - Spirit: The forge is waking up!
- `cinder.hall.first` · in hall · intro · once · → hallKeeper (step 9b: first time inside the Forge Hall; Hearthstag comes in 9c)
  - Spirit: Something huge sleeps here... not yet.
- `cinder.keeper.first` · in hall · story · once · → hearthstag (step 9c: first time in the Forge Hall; then the Keeper battle)
  - Spirit: It's scared... the fire is trapped inside it.
  - Spirit: Let's calm it down. Gently!
- `keeper.shield.first` · in battle · story · once (first blocked hit)
  - Spirit: Its crown shields it! Water breaks it, or keep chipping away.
- `keeper.shield.broken.first` · in battle · story · once: Spirit: The shield's down. Now!
- `keeper.phase2` · in battle · story · once (below half HP): Spirit: It's fighting the fear. Keep going!
- `cinder.keeper.win` · in hall · story · once · → hearthstag (it kneels and calms)
  - Spirit: It wasn't angry. It was scared, holding all the fire in.
- `cinder.shard.take` · in hall · story · once · → fireShard (the fire shard floats to the player)
  - Spirit: The fire shard! Touch it!
- `cinder.restored` · in cinderwood · story · once (the restoration wave outside)
  - Spirit: Cinderwood is warm again!

Step 10f, Cinderwood's new hand moments (lesson lines stay pinned as a chip until the moment works once):
- `cw.flame` · in cinderwood · objective · → puzzle:cw.flame (lesson at the kiln village's shrine, its flame nearly out; chip "Cup it!")
  - Spirit: The village flame is fading! Keep it warm with your hands.
- `cw.flame.cold` · in cinderwood · story · once · → shrine (trying to rest at the shrine before it's rekindled)
  - Spirit: It's too weak to rest by. Warm it up first!
- `cw.flame.done` · in cinderwood · story · once (the flame rekindled: the warmth rolls over the plaza)
  - Spirit: It's burning! Look, the whole village is waking up.
- `cw.lanterns` · in cinderwood · objective · → puzzle:cw.hollow (lesson at the smoky hollow, once the village flame is rekindled; chip "Lanterns n/6"; step 14c: short, the arrival taught it)
  - Spirit: Smoke! Light the lanterns again.
- `cw.lanterns.done` · in cinderwood · story · once (every lantern lit: the smoke burns off the path)
  - Spirit: The smoke's burning away! The path is clear.
- `cw.ember` · in cinderwood · objective · → puzzle:cw.ember (lesson at the forge's ember vent, before the bellows; chip "Pick it up!")
  - Spirit: Another ember! Drop it in the forge's mouth.
- `cw.ember.tip` · in cinderwood · objective · → firebox (step 12b: the ember picked up; the Spirit points at the firebox, chip "Tip it in!")
  - Spirit: Now tip it into the forge's mouth!
- `cw.forge.cold` · in cinderwood · story · (the bellows pumped while the firebox is cold; at most every 8 s)
  - Spirit: The forge is stone cold. It needs a spark first.
- `cw.ember.done` · in cinderwood · story · once (the ember dropped into the firebox: it roars)
  - Spirit: It's roaring! Now the bellows can blow.

## Galewick (wind, step 10a)
- `gate.wind.first` · in hub · story · once · → gate:wind (first time the wind gate comes into the window, once it leads somewhere)
  - Spirit: Listen... no wind through this tunnel at all.
- `realm.galewick.first` · in galewick · intro · once · → landmark:firstGrass (first arrival in Galewick)
  - Spirit: Galewick... it's never this quiet.
  - Spirit: The grass isn't even swaying.
- `gw.arrive` · in galewick · intro · once (step 14a: a few tiles past the arrival, the cloud puffs on the path ahead)
  - Spirit: Galewick... the air isn't moving at all.
- `gw.arrive.clouds` · in galewick · objective · → puzzle:gw.arrive (step 14a: the realm's signature gesture right after the portal; chip "Sweep", then "Clouds n/2")
  - Spirit: Clouds are stuck on the path. Sweep your hand to get the wind going!
- `gw.arrive.done` · in galewick · story · once (step 14a: the last puff gone, a gust ripples across the meadow)
  - Spirit: There's the wind! Feel it run through the grass?
- `gw.village.first` · in galewick · story · once · → landmark:plaza (first time near the village plaza)
  - Spirit: Windmills, and not one sail turning.
- `gw.mill.first` · in galewick · story · once · → landmark:mill (first time by the big mill and the ditch)
  - Spirit: The bridge is up. That big mill must lower it.
- `gw.kites.first` · in galewick · story · once · → landmark:kites (first time in the kite field)
  - Spirit: Kites, lying in the grass. Nobody to fly them.
- `gw.ridge.first` · in galewick · story · once · → landmark:ridge (first time near the ridge path)
  - Spirit: A cloud sits right on the path. Stuck, like everything.
- `gw.tower.first` · in galewick · story · once · → landmark:tower (first time near the wind tower)
  - Spirit: The wind tower. Its vanes should be spinning.
- `gw.rare.first` · in galewick · story · once · → landmark:rare (first time in the rare grass)
  - Spirit: This grass feels different. Something odd floats here.

Step 10b, the hand moments (lesson lines stay pinned as a chip until the moment works once):
- `gw.mill` · in galewick · objective · → puzzle:gw.mill (lesson at the big mill; chip "Flick!")
  - Spirit: The mill is stuck. Give its sails a flick!
- `gw.mill.done` · in galewick · story · once (three flicks: the sails keep turning, the drawbridge comes down)
  - Spirit: There it goes! The bridge is coming down.
- `gw.kites` · in galewick · story · → puzzle:gw.kites (lesson in the kite field; chip "Pull it up!")
  - Spirit: Kites! Lift one up into the sky.
- `gw.kites.task` · in galewick · objective · → puzzle:gw.kites (step 11d: the kite field's lesson says the task; chip "Kites n/3")
  - Spirit: The tower needs kites. Let's fly all three!
  - Spirit: Pinch one and lift it up into the sky.
- `gw.kites.one` · in galewick · story · Spirit: One!
- `gw.kites.two` · in galewick · story · Spirit: Two!
- `gw.kites.three` · in galewick · story · Spirit: All three!
- `gw.gust` · in galewick · objective · → puzzle:gw.clouds (at the cloud bank; chip "Sweep!"; step 14a: short, the arrival taught it)
  - Spirit: More stuck clouds. Sweep them away!
- `gw.gust.done` · in galewick · story · once (the last puff blown off the ridge path)
  - Spirit: The path is clear! Feel that breeze?
- `gw.tower.kites` · in galewick · story · → landmark:tower (at the tower door with fewer than three kites)
  - Spirit: The tower needs all three kites.
- `gw.tower.open` · in galewick · story · once (the kites tie onto the crown, the vanes turn)
  - Spirit: The kites caught the wind. The door is opening!
  - Spirit: The tower's open! Let's go in.
- `gw.meet.kitelet` · in battle · story · once (step 10c: the first wild Kitelet)
  - Spirit: A Kitelet! It wants to fly, but there's no wind.
- `gw.meet.zeppelope` · in battle · story · once (step 10c: the rare Zeppelope)
  - Spirit: A Zeppelope! It's drifting... it never lands!

Step 10d / 10e, the tower hall:
- `gw.keeper.knots` · in hall · objective · → galeowl (lesson in the tower hall; chip "Pull apart!")
  - Spirit: It's all tangled up! Pull the knots apart.
- `gw.keeper.meet` · in hall · story · once · → galeowl (all three knots undone: it shakes free and flares up)
  - Galeowl: Stay still! If the wind blows, the pieces blow away!
  - Spirit: It's scared. Let's calm it down!
- `gw.keeper.shield` · in battle · story · once · → galeowl (the Gust Wall goes up the first time)
  - Spirit: A wall of wind! Fire should break it.
- `gw.keeper.shield.break` · in battle · story · once · → galeowl
  - Spirit: The wall is down. Now!
- `gw.keeper.win` · in hall · story · once · → galeowl (it calms and bows)
  - Galeowl: I held the wind so nothing else would fly away.
  - Galeowl: But a still world is a stuck world.
  - Galeowl: Thank you, guardian. Let it blow.
- `gw.shard.take` · in hall · story · once · → windShard (step 10e: the wind shard floats out of the window to the player)
  - Spirit: The wind shard! Touch it!
- `gw.restored` · in galewick · story · once (the restoration wave from the tower)
  - Spirit: Feel that? Galewick is breathing again!

Step 14h, the first Keeper's thanks (once per save: the first Keeper calmed, back on the realm map after the wave; only
if the starter hasn't evolved; the starter becomes ready to evolve, `{name}` = the starter):
- `keeper.thanks.evolve.wind` · in hall · story (Galeowl)
  - Galeowl: Your friend fought so bravely... let the wind lift it higher.
- `keeper.thanks.evolve.fire` · in hall · story (Hearthstag)
  - Hearthstag: {name} has a fierce little spark... let my fire help it grow.
- `keeper.thanks.evolve.water` · in hall · story (Tidemother)
  - Tidemother: {name} never gave up... let the tide carry it higher.

## Lilymere (water, step 11a)
- `gate.water.first` · in hub · story · once · → gate:water (first time the well gate comes into the window, once it leads somewhere)
  - Spirit: Listen... the well is silent. Not even a drip.
- `realm.lilymere.first` · in lilymere · intro · once · → landmark:firstGrass (first arrival in Lilymere)
  - Spirit: Lilymere... the lagoon used to reach right up to here.
  - Spirit: Half the water is gone, and nothing ripples.
- `lm.arrive.stream` · in lilymere · objective · → puzzle:lm.stream (step 14a: the empty stream by the arrival; chip "Palm down", then "Pads n/4")
  - Spirit: The stream's run dry, and the lily pads are stuck in the mud! Hold your hand over it like a little cloud.
- `lm.arrive.stream.done` · in lilymere · story · once (step 14a: the stream runs, the pads float into a bridge)
  - Spirit: Look, the pads are floating! A bridge, just for us.
- `lm.village.first` · in lilymere · story · once · → landmark:village (first time near the stilt village)
  - Spirit: Stilt huts, standing over almost nothing.
- `lm.channel.first` · in lilymere · story · once · → landmark:channel (first time at the lily channel)
  - Spirit: The lily pads have all drifted apart.
- `lm.garden.first` · in lilymere · story · once · → landmark:garden (first time at the lotus garden)
  - Spirit: A lotus garden, dried to cracks. The buds won't open.
- `lm.basin.first` · in lilymere · story · once · → landmark:basin (first time at the waterfall basin)
  - Spirit: The waterfall froze mid-fall. Look, the drops just hang there.
- `lm.temple.first` · in lilymere · story · once · → landmark:temple (first time near the lake temple)
  - Spirit: A temple, half under the lagoon. Someone is hiding in there.
- `lm.marsh.first` · in lilymere · story · once · → landmark:marsh (first time in the reed marsh)
  - Spirit: Reeds taller than you! Stay close.
- `lm.pond.first` · in lilymere · story · once · → landmark:pond (first time at the rare pond)
  - Spirit: This little pond feels deep. Something big could live in it.

Step 11b, the hand moments (lesson lines stay pinned as a chip until the moment works once):
- `lm.pads` · in lilymere · objective · → puzzle:lm.pads (lesson at the lily channel; chip "Push!")
  - Spirit: The channel's too wide. Nudge the lily pads together!
- `lm.pads.done` · in lilymere · story · once (all three pads locked into a path)
  - Spirit: A lily pad bridge! Hop across.
- `lm.rain` · in lilymere · objective · → puzzle:lm.rain (at the lotus garden; chip "Palm down!"; step 14a: short, the stream taught it)
  - Spirit: The buds are thirsty. Rain on them!
- `lm.rain.done` · in lilymere · story · once (every bud bloomed into a stepping stone)
  - Spirit: Look at them bloom! A path of lotus.
- `lm.scoop` · in lilymere · objective · → puzzle:lm.lagoon (lesson at the waterfall basin; chip "Scoop!")
  - Spirit: The falls are frozen. Bring them some water!
- `lm.scoop.more` · in lilymere · story · Spirit: One more!
- `lm.scoop.done` · in lilymere · story · once (the waterfall runs, the lagoon rises)
  - Spirit: It's flowing! The whole lagoon is filling up!
- `lm.meet.sailfold` · in battle · story · once (step 11c: the first wild Sailfold)
  - Spirit: A Sailfold! Folded from an old map, I bet. It sails anywhere.
- `lm.meet.bottlewhale` · in battle · story · once (step 11c: the rare Bottlewhale)
  - Spirit: A Bottlewhale! Look, there's a tiny ship inside it!
- `temple.warn` · in realm · story (step 14h: at a temple door with fewer Islings in the pods than suggested: Galewick 4, the others 6; Go in / Not yet show; not in the Quick Journey)
  - Spirit: {keeper} is strong. Most guardians bring {n} friends.
- `lm.temple.shut` · in lilymere · story (step 11c: at the lake temple's door before the lagoon rises)
  - Spirit: The temple's sunk. The water's too low to open it.
- `lm.temple.open` · in lilymere · story · once (the lagoon rises: the temple door surfaces and glows)
  - Spirit: The temple door is open! Let's go in.
- `lm.keeper.meet` · in hall · story · once · → tidemother (step 11c: in the lake temple, she clutches the hoarded water)
  - Tidemother: Stay back! This water is all that's left. I won't lose it again!
  - Spirit: She's so scared. Let's calm her down.
- `lm.keeper.shield` · in battle · story · once · → tidemother (the Lily Shell goes up the first time)
  - Spirit: She's hiding in her shell. Wind could lift it off!
- `lm.keeper.shield.break` · in battle · story · once · → tidemother
  - Spirit: The shell's open. Now!
- `lm.keeper.win` · in hall · story · once · → tidemother (she calms and lets the water go)
  - Tidemother: I watched it drain away once. I was so afraid of losing it again.
  - Tidemother: Thank you, guardian. Water is meant to flow.
- `lm.shard.take` · in hall · story · once · → waterShard (step 11c: the water shard floats out to the player)
  - Spirit: The water shard! Touch it!
- `lm.restored` · in lilymere · story · once (the restoration wave from the temple)
  - Spirit: Listen... the whole lagoon is singing again!

## Objectives (step 11d)
- `objective.more` · in any · objective (step 11d: an objective's object is outside the island window; a gold arrow points to it; once per moment a session)
  - Spirit: One more over there!
- `spirit.poke.idle` · in any · filler (step 12a: the Spirit poked with no objective active; its gold trail shows the way)
  - Spirit: This way! Follow my gold trail.

## Statue and finale
- `hub.shard.first` · in hub · objective · once · → statueSocket (step 9c: in the hub holding a shard)
  - Spirit: Put the shard in the statue. Something's calling it.
- `hub.shard.placed` · in hub · story · once (the shard locks in): Spirit: I... feel warmer. Strange.
- `hub.shard.more` · in hub · story · once: Spirit: Two more shards are out there.
- `statue.two` · in hub · story · once (step 10e: the second shard locks into the statue)
  - Spirit: Two pieces home. It's almost awake...
- `statue.three` · in hub · story · once (step 11c: the third shard locks into the statue; the finale comes later)
  - Spirit: All three pieces home... Something is waking up!
- `statue.shard.1` · in hub · story
  - Spirit: The statue's warmer. And I feel... brighter?
- `statue.shard.2` · in hub · story
  - Spirit: Two shards. I can almost remember something.
- `statue.shard.3` · in hub · story
  - Spirit: Three! Something's waking up...
Step 13: each shard that locks in makes the statue whisper a memory in the Heart's voice (its own bubble: no tail, soft
gold-white, letters fading in), then the Spirit answers. The third starts the finale (FinaleDirector).
- `heart.dream.fire` · in hub · story · once (the fire shard locks in)
  - Heart: ...I remember warmth. Kilns glowing all night long...
- `heart.dream.wind` · in hub · story · once (the wind shard locks in)
  - Heart: ...I remember the wind. Kites so high they touched the clouds...
- `heart.dream.water` · in hub · story · once (the water shard locks in)
  - Heart: ...I remember rain. Every pond full of lotus...
- `spirit.dream.1` · in hub · story · once (after the first whisper)
  - Spirit: Did the statue just... speak?
- `spirit.dream.2` · in hub · story · once (after the second whisper)
  - Spirit: That voice... why does it feel like mine?

## Finale (step 13)
The third shard wakes the statue: it becomes the Heart of the World. Walking, wild battles and the menu's Title wait.
- `heart.wake` · in any · story (the Heart lifts its head)
  - Heart: ...little light. You found your way home.
- `spirit.finale.1` · in any · story (the Spirit floats before the Heart's empty forehead)
  - Spirit: I think... I'm a piece of it. Its light.
- `spirit.finale.2` · in any · objective (pinned; chip "Hold out your hand")
  - Spirit: I have to go back. Will you take me?
- `spirit.finale.hand` · in any · objective (the Spirit sits on your palm; chip "To the crystal"; step 14h: Mugesh read "the Heart" as his own heart)
  - Spirit: Lift me up to the gold crystal on its forehead.
- `spirit.finale.wait` · in any · story (30 s and nothing happened: it never forces it)
  - Spirit: It's okay. Whenever you're ready.
- `spirit.finale.3` · in any · story (as it leaves your palm)
  - Spirit: Thank you, big one. For everything.
- `heart.thanks` · in any · story (its eyes open, the hub answers)
  - Heart: Guardian. You carried the world in your hands.
- `heart.friends` · in any · story (the Hall of Fame: your party comes out)
  - Heart: And these are the friends who carried it with you.
- `spark.hello` · in any · story (a new spark hops out of your explorer's lantern)
  - Spirit: ...hello again. I'll always be in your light.

## Post-game (step 13)
- `heart.collection` · in any · story (poke the Heart or look at it + pinch: the collection card shows)
  - Heart: Every friend you make warms the world a little more.
- `heart.collection.full` · in any · story (every realm's roster complete)
  - Heart: Every Isling of every realm. The world is whole, guardian.
- `spark.rare.cinder` · in any · filler (the Spark's idle hint: Cinderwood still has Islings to meet)
  - Spirit: I heard something rare by the kiln ruins...
- `spark.rare.gale` · in any · filler (Galewick still has Islings to meet)
  - Spirit: Something's fluttering in the high grass of Galewick...
- `spark.rare.lily` · in any · filler (Lilymere still has Islings to meet)
  - Spirit: I saw ripples in the Lilymere reeds. Someone's hiding!
- `keeper.spar` · in any · story · → hallKeeper (poke a calm Keeper: Spar? / Not now)
  - Spirit: It wants a friendly match! Spar?
- `keeper.spar.bow` · in any · story (after the friendly match, win or lose)
  - Spirit: What a match! It bows to you.

## Quick Journey (step 9g, the quick save slot only; a Galewick journey since step 14c)
- `quick.gate` · in hub · story · once · → gate:wind (Nov 3: no gift; after the capture lesson, back in the hub: the Spirit points at the wind gate)
  - Spirit: Bring back the wind shard!
- `quick.end` · in hub · story · once (the wind shard locks in, the statue whispers; then the Keep exploring / See the ending / Title runes float in over the statue)
  - Spirit: You woke the first piece of the world!
  - Spirit: Two more realms are waiting.
  - Spirit: Keep exploring, or peek at how it all ends?
- `quick.ending.go` · in hub · story · once (Nov 3: See the ending picked; the fire and water shards appear and fly into the statue, then the finale)
  - Spirit: Let's dream ahead... all the way to the end.
  - Spirit: Imagine every shard found!

## Idle hints (random, rare, only when nothing is happening for ~40 s)
- [in outside & !battle] Spirit: Your Islings get stronger every battle.
- [in outside & !battle] Spirit: Every gate leads somewhere different. Just saying.
- [in outside & !battle] Spirit: Psst. Rare ones hide in tall grass.
- [in !battle] Spirit: You can move the island anytime. Grab a corner.
