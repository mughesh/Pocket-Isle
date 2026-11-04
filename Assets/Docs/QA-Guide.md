# Pocket Isle: QA guide

Thanks for testing! This build has a **dev panel**: a menu with shortcuts so you can jump anywhere, set up the party
and flip story flags instead of replaying everything.

## 1. Install (one line, on a PC with the Quest plugged in)

```
adb install -r -g PocketIsle-QA.apk
```

`adb` comes with Meta Quest Developer Hub. The headset needs developer mode on and the USB prompt accepted.
The app appears in the library under **Unknown Sources**.

## 2. Open the dev panel

- **Hands:** turn your **left palm toward you and pinch** (thumb + index), the same gesture as Meta's menu button.
  That opens the **game menu** (the game pauses); press its **Dev** rune. (Players see the same menu without Dev.)
- **Controllers:** the left controller's menu button, then **Dev**.
- **Unity Editor:** F2 opens the panel directly (Esc opens the game menu).

The panel opens above the island's far corner on your main hand's side, facing you, and follows only when the island
moves. **To move it**, pinch its edge, any side, like the island's rim (or point at the frame just outside it and pinch) and drag;
it opens at that spot from then on, until the app closes. Close it with the **X** in its top-right corner (or F2).
Long tabs have **Up / Down** buttons on the right.

Press a button by **poking it** with a fingertip, or by **pointing at it** (a thin line from your hand shows up when it
points at the panel) **and pinching**. Some buttons ask for a second press to confirm (new game, reset story, load,
clear storage): they turn red, press again within 3 seconds.

## 3. What each tab does

Each tab is split into groups under gold headings.

| Tab | Buttons |
|---|---|
| **Travel** (was Go to) | Teleports the explorer. Hub: start, shrine, statue, first grass, the three gates. Cinderwood (CW): arrival, village, shrine, springs, ruins, log, forge door. **Forge Hall** goes inside (the Keeper meets you there). Not during a battle. |
| **Team** (was Party) | Heal all. Lead level -1 / +1 / +5. All to Lv 5 / 10 / 15. **Team of 3** at Lv 8 or 10 (your starter + Steambara, the Water Isling, + a fire Isling; the others go to storage). Lv 8 is the Keeper fight as tuned (about 2 in 3 wins), Lv 10 is easy. Clear storage. Species picker: `< Species >`, `Lv -1 / +1`, **Add to party**. |
| **Battle** (was Encounter) | Same species picker, then **Fight now** for a wild battle on the spot. **One-hit KO** (your hits knock anything out). **Invincible** (your Islings take no damage). Wild battles on / off. Rare x10. |
| **Story** | **Finale: play from wake / from the goodbye** (all three shards put in the statue, the finale not done; it plays on the hub at once). Skip / reset the tutorial. **Replay opening** (a new game). Cinderwood restored on / off. Log placed / fallen. Forge + door lit / cold. Keeper calm / frightened. Fire shard held / none. Shard in the statue on / off. Reset all story. *Story buttons save, change the save and reload the game: wait 2-3 seconds.* |
| **Save** | **Save 1-3** keep a snapshot; **Load 1-3** go back to one (reloads). **Load auto** = the snapshot taken automatically before the last new game. **New game** (in the slot you are playing). Show save path. **Quick: start / at gift / at gate / at temple** jump into the Quick Journey slot: the opening, the hub right before the gift, the fire gate after the gift, the forge door with the log placed. The line at the bottom shows which slot you play and the Quick Journey timer. |
| **Tools** | FPS counter. Debug readouts (lines and numbers for us, off at start). Time x1 / x2 / x4. Walk x2. Debug labels. Record ghost (the next missing ghost-hand clip, for Mugesh). **Report bug**. (Close is now the X.) |

Cheats (one-hit KO, invincible, wild battles off, rare x10, time and walk speed) last until the app closes. They are
never saved.

## 4. What's new in this build (step 14h, Nov 4)

- The menu is shorter: Resume, **Journal**, Settings, the speaker. Controls and Title are inside Settings.
- **Journal:** every Isling you've met or caught; poke a portrait for its card. Also on the lodge pedestal.
- **Evolution:** a pod glowing gold is ready. Grab it and turn your hand palm up: it evolves on your palm.
  Dev > Team > "Make lead ready to evolve" to try it.
- Big evolved Islings shrink a little on the battle pad so you can always see the wild one.
- Temple doors no longer block a small team: the Spirit warns, then Go in / Not yet. Keepers match your team's strength.
- Finale: "Lift me up to the gold crystal on its forehead"; a dotted gold arc shows the way.

## Earlier (step 14f, Nov 3 night)

- Every button is a mossy stone tablet; all text is much bigger. Look at a tablet: it grows with a gold rim. Pinch or poke:
  it sinks with a stone tap.
- Game menu: **Resume** (big, gold) on top, then Controls, Settings, Title. The little speaker tablet turns music on and off.
- Battle moves are a 2 x 2 grid; the colour round each move is its element.
- The last walk lesson is "Swipe to walk, tap to stop" and can't trap you (it passes itself after 30 s).
- Untangling Galeowl never walks the explorer.
- Finale: a gold crystal on the Heart's forehead; the Spirit turns to light and flies into it.

## Earlier (step 14d, Nov 3 evening)

- Walking is faster. The walk lesson: swipe any way until the dots fill, then swipe twice and **tap your thumb to stop**.
- The shrine by the lessons stays shut until the tutorial is over.
- Quick Journey: no gift; the Spirit points you to the wind gate and Galeowl is easier.
- Walk up to a portal to go through (no need to walk into the arch). A portal you look at glows brightly.
- After every hand moment the gold trail shows where to go next. Name cards rise in the middle of the island with a sound.

## Earlier (step 14c, Nov 3: Galewick first)

- **Galewick is the first realm now.** After the tutorial the Spirit's trail points to the **wind gate** (the sign over
  it glows gold), then Cinderwood, then Lilymere. Every gate is open; you can still go anywhere.
- **Gate signs** over the three hub gates: the realm's name and emblem.
- **Name cards:** entering a realm a carved sign rises over the far side of the island with its name and a line, then
  sinks. Shorter (name only) on later visits.
- **Cinderwood:** four dark lanterns just past the arrival: touch one and the fire runs down all of them.
- **Quick Journey** goes to Galewick: two friends (Toastling + Whirlby), the ridge clouds are already gone, the end comes
  when the wind shard is in the statue: **Keep exploring / See the ending / Title**. See the ending plays the finale and
  goes back to the title without finishing your quick save.
- Things to watch: is any name card in the way of something you need to do? Are the gate signs readable from where you
  sit? Is Galeowl too hard or too easy as the first Keeper?

## Earlier (step 14a, Nov 3: the first minute)

- **Where the island appears:** in front of you, about 30 cm under your eyes, once your head is still. Before you touch
  the shard, look away or stand up / sit down: after a second it glides back in front of you. Meta's recenter (hold the
  Meta button) re-places the island, also over Link.
- **Grabbing the island (step 14i): pinch and hold a corner** (or the front lip under the pod row). A gold ring fills round
  your fingers in about half a second; full = you're holding it (a soft click, the bracket turns gold). Let go early and
  nothing happens; slide your hand away while it fills and it cancels. Your second hand joins faster (a third of a second)
  to turn or resize. A hand resting below the island, or a pinch while you look at something else, never grabs.
  Pinching near a pod gives you the pod, never the island.
- **Pinch lesson:** two dots under the line fill as you pinch; "Nice! Once more." after the first.
- **The Spirit** hovers right over the thing to do, with gold rays round it while it teaches.
- **Pods onto the rim:** bring a held pod near the rim: a ghost pod shows the socket it will go into; let go.
- **Galewick:** two clouds block the path just past the arrival: sweep your open hand over the island (tilted, like
  fanning air). The wind ripples on across the meadow for a second after your hand.
- **Lilymere:** an empty, muddy river just past the arrival with lily pads stuck in it: hold your palm down anywhere
  over it to rain; the water spreads, the pads float, then gather on the path as a bridge. The old lily channel is a
  plank bridge now.
- **Game menu:** Music On / Off. **Controls** shows each gesture with a ghost hand over the island: pick one.
- Things to watch: does the island ever appear off to the side, too high or too low? Did anything grab the island when
  you meant a pod? Is the Spirit ever in the way of what it points at?

## Earlier (step 13, Oct 28: the finale)

- **The statue talks:** each shard you put in makes it whisper a memory (a gold-white bubble with no tail), and it
  wakes a little: eyes glow after one, it breathes after two.
- **The finale** starts when the third shard goes in (or Dev > Story > **Finale: play from wake**). The statue wakes
  as the **Heart of the World**. When the Spirit asks "Will you take me?": **hold one open hand out, palm up**. The
  Spirit lands on it. **Lift your hand to the Heart's forehead** (the round gold setting): it floats in. (Or look at the
  Heart and pinch: it goes by itself.) Then: the Heart's eyes open, the hub glows, your Islings come out one by one
  with cards, the credits, and the Spirit comes back as a small Spark. About 3 minutes.
- **Finale: from the goodbye** skips straight to the palm part.
- **After the finale:** poke the Heart to see how many Islings you've met in each realm. Poke a calm Keeper in its
  hall for a friendly match ("Spar?").
- Things to watch: does the Spirit land on your palm easily? Does the forehead feel reachable? Is the Heart's look
  majestic and kind? Is anything hard to read (cards, credits)?

## Earlier (step 12c, Oct 28)

- Walking is a little faster (same speed as look + pinch). Footprints now point the way you walk.
- The gold trail follows the roads.
- In battle the Spirit's bubble keeps off the attack runes.
- Inside the Keeper halls the way out only glows once the Keeper is calm.
- Galewick: the ridge clouds sit on the road again; the road up to the tower is clear.
- Lilymere has its own colours (teal and blue).
- **Lilymere's waterfall:** the falls come from a bowl on top of a rocky cliff. Scoop water from the inlet with **one
  cupped hand**, carry it over the cliff and tip your hand: two pours melt the falls.
- The dev panel is reorganised (see above).

## Earlier (step 12a, Oct 27 night: walking and guidance)

- **Walking works differently now.** One thumb swipe walks a few tiles (up to 5) along the path and stops on its own:
  at a fork, near something to do, before a gate, at the edge of tall grass. Gold footprints show where it will go.
  **Swipe twice quickly** to keep walking; **tap** your thumb to stop. Look + pinch a tile still walks there.
- **New game**: three short walk lessons (a gold tile ahead, one round a corner, then swipe twice) replace the old stop lesson.
- **Gold bubbles** are things to do (they stay as a small chip); plain cream ones are just chat.
- **Lost?** Poke the Spirit (or look at it and pinch): it repeats what to do, or shows a gold trail to where to go next.
  Standing still for ~12 s also shows the trail.
- Game menu > **Controls**: a card with the four controls.
- Note anything that feels slow, stops too early or too late, or walks somewhere you didn't mean.
- Near any hand puzzle the island view now **settles** to show the whole puzzle.
- Kites, the ember, knot ends and the log glow white when your hand is close enough to grab (pinch or fist).
- Inside halls and the lodge, the **way out glows** with a gold arrow.
- Losing a battle puts you back at the shrine of the realm you're in.
- The wind shard is **green**. In Galewick the clouds float in the air and the ridge path is wider.
- Lilymere's dry areas are brown mud now (not white).

## Earlier (step 11d: fixes from Mugesh's Oct 27 notes)

- **Every task now shows itself**: gold rings on what you can act on, a counter in the Spirit's chip (Kites 1/3,
  Clouds 2/3, Flicks 1/3, Knots 1/3, Pads 1/3, Lotus 4/5, Pours 1/2), and a gold arrow at the island's edge when one is
  out of view. Tell us any task you still found by accident.
- **Settings > Touch tiles to walk** is **Off** now. Walk with the thumb swipe or by looking at a tile and pinching.
- **Open doors glow** (the forge door, the wind tower door): a warm swirl in the doorway and a gold ring in front.
- **Galewick**: the big mill's flick zone is bigger and slower, and the middle finger counts too; the cloud puffs are
  twice as big and float above the plants; the tower crown shows three empty hooks; after Galeowl a **return gate**
  stands beside the tower.
- **The shard after a Keeper win**: pinch or grab it, or just wait 4 s and it flies to the Spirit.
- **Lilymere**: pads float on the water and come back if they drift out of view; the finished bridge is just the pads;
  the lotus crossing is just the flowers; the rain cloud is palm-sized; at the garden the island shows all five buds;
  scoop with **both hands cupped together** (one hand still works), the basin fills half way after one pour.

- **Lilymere's Islings and Keeper** (step 11c): new water Islings in Lilymere's grass (Marimoss, Drizzcan, Lilyvole,
  Sailfold; rare Bottlewhale in the rare pond). After the basin (the lagoon rises) the **lake temple door glows**: walk
  in and calm **Tidemother**. Dev panel **Go to > LM: lake temple**; Story tab **Tidemother / Water shard**.

## Earlier (Oct 28: sound, and Lilymere for Mugesh)

- **Sound everywhere**: footsteps, battles, catching, the Cinderwood beats, Keeper, ambience per area. No music yet
  (no tracks). Note any moment that's too loud / too quiet / wrong / annoying.
- **Lilymere** (water realm; the well gate stays sealed in this build, use dev panel **Go to > LM: ...**):
  - **LM: channel**: push the drifting lily pads with a fingertip into the three gold rings across the channel.
  - **LM: garden**: hold one hand palm down over the dry garden, about a hand's width up: a little cloud rains. Wiggle
    your fingers for harder rain. The five buds on the path bloom into stepping stones.
  - **LM: basin**: cup a hand palm up, dip it in the little pool by the path, lift it out: water sits in your palm. Tip
    it over the stone basin. Twice: the waterfall breaks free and the lagoon fills.
  - Story tab: **LM pads / LM rain / LM basin + rise** and **Lilymere restored** toggle them.

## Earlier (step 10f, Oct 27: Cinderwood's new hand moments)

- **Village flame** (Go to > CW: village or CW: shrine): the shrine's flame is nearly out and you can't rest there yet.
  Cup it with both hands, palms toward it, about 2 s (one hand curled round it: 3 s; look + pinch-hold: 3 s). The whole
  plaza lights up; then the shrine heals as before.
- **Smoky hollow** (Go to > CW: hollow; after the village flame): dark smoke blocks the trail to the hot springs. Gold
  glows float over the stone lanterns: touch one with a fingertip; touch two in a row quickly and the fire races down the
  line. The smoke burns away.
- **Ember** (Go to > CW: forge door): the bellows do nothing yet. A gold ring marks the glowing vent: pinch (or grab)
  the ember, carry it to the gold glow at the stone mouth beside the bellows and let go. The forge roars; now the
  bellows light the braziers.
- A new game starts in the hub again (it had started in Cinderwood). The dev panel opens behind the island; point a
  hand ray at it and pinch.
- Story tab: **CW flame / CW hollow / CW firebox** toggle each one (edits the save, reloads).
- Tell us: did the cup, the lantern touch and the ember pick-up work first time? Anything you had to try twice?

## Earlier (steps 10a-10b, Oct 27: Galewick, the wind realm)

- **The hub's wind gate is open in builds now** (Oct 27); Go to > GW: arrival (or village, big mill, kite field, ridge,
  tower, shrine, rare grass, fire patch) also works.
- Windmill highlands, bright but **completely still** until restored (Story tab > **Galewick restored**: grass waves,
  sails turn, kites fly on posts, seeds drift).
- **Flick the big mill**: flick its sails with a fingertip (or look at them and pinch), 3 flicks within 4 s. The
  drawbridge comes down over the ditch.
- **Kites**: pinch a kite in the kite field and pull it up about a hand's height over the island, then let go. It flies
  above the island on a string to the explorer. Three kites follow you.
- **Fan the wind** (on the ridge): sweep an open flat hand fast over the island. The grass ripples and the clouds on
  the path blow away.
- **Wind tower**: arrive with all three kites; they tie onto the tower and the door opens. (Inside comes later.)
- Wild Islings and Keepers are stronger once you hold a shard (+3 / +2 levels per shard).
- **New wind Islings** in Galewick's grass (Kitelet, Whirlby, Samarling, Vanehare, rare Zeppelope) and their evolutions;
  Pufflet and Pebbloop now evolve at Lv 14. Encounter tab can spawn them all (and Galeowl's species exists, but its hall
  isn't built yet).

## Earlier (step 9h)

- **No room-scan prompt** on a new headset (the island just floats when the headset has no room scan).
- **Instructions stay** until you do them: after a few seconds the Spirit's bubble shrinks to a small chip with a hand
  ("Hold a corner", "Pinch!"); look at the chip or the Spirit to read the full line again. Lines stay up longer.
- **Shorter opening**, and a new **pinch lesson** right after the Spirit arrives (a ghost hand pinches beside your own
  hand; each pinch sparkles; two pinches and you move on).
- **Pods you can use now have a gold rim**; the one you look at or touch turns white and grows a little.
- **Ghost hands act on the real things**: the island-move ghost grabs the near rim's corner on your main-hand side, the
  starter ghost grabs the pod you look at (or each pod in turn) and sets it on the rim, the send-out ghost grabs your
  lead pod and lets go over the pad.
- **Lodge:** standing next to the door, or a hand resting near it, no longer takes you out; walk into the door (swipe
  toward it, or look at it and pinch).
- **Battles:** your explorer faces the wild Isling; the Spirit waits at the side of the island; its bubble never sinks
  into the island.
- **New logo** on the title (a little piece of the island with amber and mossy green letters).

## Earlier (step 9g)

- **Title screen:** a "Pocket Isle" logo over the island. New players pick **Full adventure** or **Quick journey**
  (about 10 min) after the island unfolds; returning players get **Continue / New journey / Settings**. Starting a
  slot over that already has a save needs a **2 s hold** (the rune fills gold).
- **Two save slots:** `save.json` (Full adventure) and `quick.json` (Quick Journey). Continue resumes the last one played.
- **Game menu:** left palm up + pinch pauses the game: **Resume, Settings, Title, Dev**.
- **Settings:** Music / Effects / Ambience (no sound yet, the setting is saved), Dominant hand, Spirit hints (Fewer =
  no idle hints), Recenter island, Credits.
- **Quick Journey:** after the first catch the Spirit gives two friends (Steambara, Toastling), the fire gate glows;
  in Cinderwood fewer wild battles, double XP, no rare ones, and gold tiles show the way when you stand still. Placing
  the fire shard in the statue ends it (Keep exploring / Title). Two lessons are skipped there (thumb tap to stop,
  two-hand turning).

## Earlier (step 9f)

- **XP shows now:** a gold XP bar with the next level ("→ 7") on the pod card and the lodge card; after a win a big
  "+XP", the XP bar grows and fills, "Lv 7!" with a gold ring and a hop; pods of Islings that fought glow with "+XP".
- **Faster levels:** walking the trail normally should get you to about Lv 8 by the temple.
- **Steambara**, a Water capybara, at the hot springs (and sometimes the berry grove). Water beats fire: the Keeper
  is tuned for a team with a Water Isling. Without one, Kilnpot and Pufflet teams will struggle; please note it if
  you reach the temple without meeting a Steambara.
- **The explorer** is a teal-hooded kid with a backpack lantern now (was an orange cube).
- **Forgehorn, Emberdillo, Cindermole, Cinderbloom, Pyrecrane** have new looks.

## 5. Report a bug

1. Get to the moment (or just after it).
2. Open the panel: **Tools > Report bug**.
3. The status line shows the folder name, like `bugreports/20261026-120940`. Note it, with one line about what you saw.

Each report holds a screenshot (if the headset allows it), the save, the last 300 log lines and a state summary
(map, tile, party, story flags, fps).

## 6. How Mugesh pulls the reports (one line)

```
adb pull /sdcard/Android/data/com.mugesh.pocketisle/files/bugreports ./bugreports
```

Snapshots live next to them in `.../files/snapshots`; the saves are `.../files/save.json` (Full adventure) and `.../files/quick.json` (Quick Journey).
