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
- `intro.shard` · once · → shard (only if the shard isn't touched within 3 s)
  - Spirit: Oh! Something's here. Touch it?
- `intro.arrive` · once
  - Spirit: Oh! You're awake. Hello, big one.
  - Spirit: I'm a Lantern Spirit. A small one. Very small.
- `intro.world` · once · → rim
  - Spirit: The world broke into floating pieces. This is one of them.
  - Spirit: Hold its rocky edge to move it. Put it wherever feels comfy.
- `intro.starter` · once · → starterPods
  - Spirit: Every guardian needs an Isling friend. Pick one!
- `starter.kilnpot` · Spirit: Kilnpot! Warm as a fresh-baked pot.
- `starter.pebbloop` · Spirit: Pebbloop! Bubbly, and very round.
- `starter.pufflet` · Spirit: Pufflet! Light as a cloud, twice as fluffy.
- `intro.statue` · once · while !tutorial · → landmark:statue (first time within ~3 tiles of the statue)
  - Spirit: That statue... it has three empty holes.
  - Spirit: I feel like I know it. Strange.

## Tutorials: lessons (step 8b)
One lesson at a time (Data/Onboarding/Onboarding_Main). The first line comes when the lesson starts; the `.again` line
every ~10 s while it waits. Shard, move and starter use the Opening lines above; send out, pick a move and catch use
`pod.first`, `battle.first` and `capture.first` below.
- `lesson.move.again` · → rim
  - Spirit: Grab the rocky edge and slide it.
- `lesson.starter.again` · → starterPods
  - Spirit: Look at one, then grab it or pinch.
- `lesson.walk` · while canWalk · → ghost
  - Spirit: Slide your thumb along your finger to walk.
- `lesson.walk.again` · while canWalk · → ghost
  - Spirit: Thumb along your finger. Like this!
- `lesson.walk.help` · while canWalk · → ghost
  - Spirit: Or look at a spot and pinch.
- `lesson.stop` · while canWalk · → ghost
  - Spirit: Swipe to walk, then tap your thumb to stop.
- `lesson.stop.again` · while canWalk · → ghost
  - Spirit: Swipe, then tap to stop.
- `lesson.look` · while canWalk · → goldTile
  - Spirit: Look at the glowing spot and pinch.
- `lesson.look.again` · while canWalk · → goldTile
  - Spirit: The glowing spot. Look, then pinch!
- `lesson.grass` · while canWalk · → landmark:firstGrass
  - Spirit: Islings hide in tall grass. Let's peek!
- `lesson.grass.again` · while canWalk · → landmark:firstGrass
  - Spirit: Follow the gold tiles into the tall grass.
- `lesson.send.again` · while battle · → leadPod
  - Spirit: Toss your friend's pod onto the island!
- `lesson.rune.again` · while battle · → nearestRune
  - Spirit: Poke a rune!
- `lesson.catch.again` · while battle · → cradle
  - Spirit: Grab the big pod and flick it!
- `lesson.twohands` · while !battle · → islandRim (step 8c, once after the first battle)
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
- `pod.first` · once · → leadPod (encounter start)
  - Spirit: Grab your friend's pod from the rim and toss it in!
- `battle.first` · once · → nearestRune
  - Spirit: Poke a rune to attack!
- `battle.types` · once (first super or not-very effective hit)
  - Spirit: Water beats Fire. Fire beats Wind. Wind beats Water.
- `battle.win.first` · once: Spirit: You did it! Your friend is getting stronger.
- `battle.lose.first` · once: Spirit: Everyone's tired. Let's rest at the shrine.
- `capture.first` · once · → cradle (first time the wild HP bar is red)
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
- `cw.arrive` · once
  - Spirit: Cinderwood... it used to glow all year round.
  - Spirit: The leaves have gone grey. It's so cold.
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
- `cinder.log.hands` · once · → puzzle:cinder.log (step 9b: right after cinder.log.first; the ghost shows the lift)
  - Spirit: Maybe your big hands could move it?
- `cinder.log.done` · once (step 9b: the log set down on a ledge, the trail open)
  - Spirit: You're strong! Let's go on.
- `cw.temple` · once (first time at the temple door)
  - Spirit: The forge temple. Something big is breathing in there.
- `cinder.bellows.first` · once · → puzzle:cinder.bellows (step 9b: first time the bellows are in reach; the ghost pokes the pad)
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

## Statue and finale
- `hub.shard.first` · once · → statueSocket (step 9c: in the hub holding a shard)
  - Spirit: Put the shard in the statue. Something's calling it.
- `hub.shard.placed` · once (the shard locks in): Spirit: I... feel warmer. Strange.
- `hub.shard.more` · once: Spirit: Two more shards are out there.
- `statue.shard.1`
  - Spirit: The statue's warmer. And I feel... brighter?
- `statue.shard.2`
  - Spirit: Two shards. I can almost remember something.
- `statue.shard.3`
  - Spirit: Three! Something's waking up...
- `heart.wake`
  - Heart: ...Little light. You found your way back.
  - Spirit: You... I know you. I'm part of you, aren't I?
  - Heart: The world broke, and so did I. Show me it can be whole.
- `heart.win`
  - Heart: You've grown strong, little light. And you, guardian.
  - Spirit: I'll miss you, big one. Come and visit, okay?
  - Spirit: I'll be right here. In the middle of everything.
- `end` · Spirit: (glows, slots into the Heart's forehead)

## Idle hints (random, rare, only when nothing is happening for ~40 s)
- Spirit: Your Islings get stronger every battle.
- Spirit: The other gates lead to water and wind. Just saying.
- Spirit: Psst. Rare ones hide in tall grass.
- Spirit: You can move the island anytime. Grab the edge.
