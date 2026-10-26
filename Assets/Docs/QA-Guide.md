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

The panel opens about an arm's length in front of you, a little toward your main hand. It stays where it is; turn well
away and it comes back in front. **To move it**, pinch its edge, any side, like the island's rim (or point at the frame just outside it and pinch) and drag;
it opens at that spot from then on, until the app closes. Close it with **Tools > Close** (or F2).

Press a button by **poking it** with a fingertip, or by **pointing at it** (a thin line from your hand shows up when it
points at the panel) **and pinching**. Some buttons ask for a second press to confirm (new game, reset story, load,
clear storage): they turn red, press again within 3 seconds.

## 3. What each tab does

| Tab | Buttons |
|---|---|
| **Go to** | Teleports the explorer. Hub: start, shrine, statue, first grass, the three gates. Cinderwood (CW): arrival, village, shrine, springs, ruins, log, forge door. **Forge Hall** goes inside (the Keeper meets you there). Not during a battle. |
| **Party** | Heal all. Lead level -1 / +1 / +5. All to Lv 5 / 10 / 15. **Team of 3** at Lv 8 or 10 (your starter + Steambara, the Water Isling, + a fire Isling; the others go to storage). Lv 8 is the Keeper fight as tuned (about 2 in 3 wins), Lv 10 is easy. Clear storage. **One-hit KO** (your hits knock anything out). **Invincible** (your Islings take no damage). Species picker: `< Species >`, `Lv -1 / +1`, **Add to party**. |
| **Encounter** | Same species picker, then **Fight now** for a wild battle on the spot. Wild battles on / off. Rare x10. |
| **Story** | Skip / reset the tutorial. **Replay opening** (a new game). Cinderwood restored on / off. Log placed / fallen. Forge + door lit / cold. Keeper calm / frightened. Fire shard held / none. Shard in the statue on / off. Reset all story. *Story buttons save, change the save and reload the game: wait 2-3 seconds.* |
| **Save** | **Save 1-3** keep a snapshot; **Load 1-3** go back to one (reloads). **Load auto** = the snapshot taken automatically before the last new game. **New game** (in the slot you are playing). Show save path. **Quick: start / at gift / at gate / at temple** jump into the Quick Journey slot: the opening, the hub right before the gift, the fire gate after the gift, the forge door with the log placed. The line at the bottom shows which slot you play and the Quick Journey timer. |
| **Tools** | FPS counter. Debug readouts (lines and numbers for us, off at start). Time x1 / x2 / x4. Walk x2. Debug labels. Record ghost (the next missing ghost-hand clip, for Mugesh). **Report bug**. Close. |

Cheats (one-hit KO, invincible, wild battles off, rare x10, time and walk speed) last until the app closes. They are
never saved.

## 4. What's new in this build (step 9h)

- **No room-scan prompt** on a new headset (the island just floats when the headset has no room scan).
- **Instructions stay** until you do them: after a few seconds the Spirit's bubble shrinks to a small chip with a hand
  ("Grab the edge", "Pinch!"); look at the chip or the Spirit to read the full line again. Lines stay up longer.
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
