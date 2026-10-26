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
- **Controllers:** the left controller's menu button.
- **Unity Editor:** F2.

The panel opens about an arm's length in front of you, a little toward your main hand. It stays where it is; turn well
away and it comes back in front. **To move it**, pinch the small bar under it (or point at the bar and pinch) and drag;
it opens at that spot from then on, until the app closes. The same gesture closes it (or **Tools > Close**).

Press a button by **poking it** with a fingertip, or by **pointing at it** (a thin line from your hand shows up when it
points at the panel) **and pinching**. Some buttons ask for a second press to confirm (new game, reset story, load,
clear storage): they turn red, press again within 3 seconds.

## 3. What each tab does

| Tab | Buttons |
|---|---|
| **Go to** | Teleports the explorer. Hub: start, shrine, statue, first grass, the three gates. Cinderwood (CW): arrival, village, shrine, springs, ruins, log, forge door. **Forge Hall** goes inside (the Keeper meets you there). Not during a battle. |
| **Party** | Heal all. Lead level -1 / +1 / +5. All to Lv 5 / 10 / 15. **Team of 3** at Lv 9 or 11 (your starter + two fire Islings; the others go to storage). Clear storage. **One-hit KO** (your hits knock anything out). **Invincible** (your Islings take no damage). Species picker: `< Species >`, `Lv -1 / +1`, **Add to party**. |
| **Encounter** | Same species picker, then **Fight now** for a wild battle on the spot. Wild battles on / off. Rare x10. |
| **Story** | Skip / reset the tutorial. **Replay opening** (a new game). Cinderwood restored on / off. Log placed / fallen. Forge + door lit / cold. Keeper calm / frightened. Fire shard held / none. Shard in the statue on / off. Reset all story. *Story buttons save, change the save and reload the game: wait 2-3 seconds.* |
| **Save** | **Save 1-3** keep a snapshot; **Load 1-3** go back to one (reloads). **Load auto** = the snapshot taken automatically before the last new game. **New game**. Show save path. |
| **Tools** | FPS counter. Debug readouts (lines and numbers for us, off at start). Time x1 / x2 / x4. Walk x2. Debug labels. Record ghost (the next missing ghost-hand clip, for Mugesh). **Report bug**. Close. |

Cheats (one-hit KO, invincible, wild battles off, rare x10, time and walk speed) last until the app closes. They are
never saved.

## 4. Report a bug

1. Get to the moment (or just after it).
2. Open the panel: **Tools > Report bug**.
3. The status line shows the folder name, like `bugreports/20261026-120940`. Note it, with one line about what you saw.

Each report holds a screenshot (if the headset allows it), the save, the last 300 log lines and a state summary
(map, tile, party, story flags, fps).

## 5. How Mugesh pulls the reports (one line)

```
adb pull /sdcard/Android/data/com.mugesh.pocketisle/files/bugreports ./bugreports
```

Snapshots live next to them in `.../files/snapshots`, and the save is `.../files/save.json`.
