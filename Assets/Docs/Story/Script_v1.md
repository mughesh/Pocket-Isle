# Pocket Isle — script v1 (Oct 24)

Lantern Spirit = **Spirit**. Bubbles are short: one idea per line, ≤ ~70 characters.
Format: `trigger_id` · speaker · lines (shown in order). **once** = only the first time.
Optional `· → target` (before the lines): the Spirit flies to that anchor while saying them and it pulses
(`leadPod`, `cradle`, `catchRune`, `runes`, `nearPad`, `wildPad`, `tile:x,y`). No target = it stays home.
Names are placeholders. The step 5c dialogue system loads this (convert to data asset).

## Opening (Still Isle)
- `intro.arrive` · once
  - Spirit: Oh! You're awake. Hello, big one.
  - Spirit: I'm a Lantern Spirit. A small one. Very small.
- `intro.world` · once
  - Spirit: The world broke into floating pieces. This is one of them.
  - Spirit: Hold its rocky edge to move it. Put it wherever feels comfy.
- `intro.explorer` · once
  - Spirit: See the little explorer? Slide your thumb along your finger to walk.
  - Spirit: Keep your thumb there to keep walking. Tap to stop.
- `intro.starter` · once
  - Spirit: Every guardian needs a friend. Pick one!
- `starter.kilnpot` · Spirit: Kilnpot! Warm as a fresh-baked pot.
- `starter.pebbloop` · Spirit: Pebbloop! Bubbly, and very round.
- `starter.pufflet` · Spirit: Pufflet! Light as a cloud, twice as fluffy.
- `intro.statue` · once (first time near the statue)
  - Spirit: That statue... it has three empty holes.
  - Spirit: I feel like I know it. Strange.

## Tutorials
- `tut.grass` · once: Spirit: Tall grass rustles when something's hiding in it.
- `tut.pond` · once (first time next to water): Spirit: Face the water and tap. Something might bite!
- `pod.first` · once · → leadPod (encounter start)
  - Spirit: Grab your friend's pod from the rim and toss it in!
- `battle.first` · once · → runes
  - Spirit: Look at a rune and pinch. Or just poke it.
- `battle.types` · once (first super or not-very effective hit)
  - Spirit: Water beats Fire. Fire beats Wind. Wind beats Water.
- `battle.win.first` · once: Spirit: You did it! Your friend is getting stronger.
- `battle.lose.first` · once: Spirit: Everyone's tired. Let's rest at the shrine.
- `capture.first` · once · → cradle (first time the wild HP bar is red)
  - Spirit: It's weak now. Grab the big pod and flick it!
- `capture.locked` · → catchRune (Catch picked above red)
  - Spirit: Weaken it first!
- `capture.dazed.first` · once · → cradle (first dazed wild creature)
  - Spirit: It's dazed. Quick, a pod!
- `capture.success.first` · once: Spirit: A new friend! It's in your pods now.
- `party.full` · once: Spirit: Your pods are full. New friends go to the Sanctuary.
- `shrine.first` · once
  - Spirit: A Hearth Shrine. Rest here to heal everyone and save.
  - Spirit: You can swap friends here too.
- `gate.first` · once
  - Spirit: A Shard Gate. It leads to another piece of the world.
  - Spirit: Step in when you're ready.
- `rare.first` · once (first gold sparkle): Spirit: Ooh, sparkly! That one's rare.

## Cinderwood (fire)
- `cw.arrive` · once
  - Spirit: Cinderwood... it used to glow all year round.
  - Spirit: The leaves have gone grey. It's so cold.
- `cw.village` · once: Spirit: The kilns are cold. Nobody's baking. That's never happened.
- `cw.springs` · once: Spirit: Even the hot springs are only lukewarm.
- `cw.log` · once (blocked by the fallen log)
  - Spirit: A fallen log. Too heavy for the little one.
  - Spirit: But not for you. Reach in and lift it!
- `cw.log.done` · Spirit: Strong hands!
- `cw.temple` · once (first time at the temple door)
  - Spirit: The forge temple. Something big is breathing in there.
- `cw.bellows` · once
  - Spirit: These bellows feed the forge. Squeeze your hand, open, squeeze.
- `cw.brazier` · Spirit: One lit! (then: Two! / All three!)
- `cw.door` · Spirit: The door's opening...
- `cw.keeper.meet`
  - Hearthstag: Stay back! If I share the fire, it will go out forever.
  - Spirit: He's not mean. He's scared.
  - Spirit: Let's calm him down.
- `cw.keeper.shield` · once: Spirit: His crown blocks everything. Maybe water can crack it?
- `cw.keeper.shield.break` · Spirit: It cracked! Now's our chance!
- `cw.keeper.win`
  - Hearthstag: ...It's warm. The forest is warm again.
  - Hearthstag: I was so afraid of the dark. Thank you, guardian.
  - Hearthstag: Take this shard. Mend what broke.
- `cw.restored` · once
  - Spirit: Look! Cinderwood is glowing again!
- `cw.shard.dock` · once: Spirit: The island's getting bigger. Did you feel that?

## Statue and finale
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
- Spirit: Your friends get stronger every battle.
- Spirit: The other gates lead to water and wind. Just saying.
- Spirit: Psst. Rare ones hide in tall grass.
- Spirit: You can move the island anytime. Grab the edge.
