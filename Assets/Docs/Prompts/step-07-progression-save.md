# Step 7: XP, levels, evolution, Hearth Shrine, save / load

Read CLAUDE.md, BuildPlan.md (v4), GameDesign.md ("Battle", "Party and collection") and Script_v1.md.
Goal: creatures grow from fights, can evolve, the Hearth Shrine heals / saves / swaps, and the game survives
quitting and the headset going to sleep. Everything tunable from data.
**Not in this step:** spatial-anchor persistence (cut list #5; the island re-places in front of the player on
launch anyway), move learning (3 fixed moves per species), the Sanctuary island (storage stays a list + shelf).

## A. XP and levels
1. `ProgressionConfig` asset (Data/Battle): `xpToNext(level) = a·level + b` with **a = 6, b = 10**, level cap 30,
   XP share rules. Move/extend the existing `BattleMath.XpReward`.
2. After a win (wild faints or is caught) every creature that **fought in this battle** gets the full reward.
   Fainted participants get it too (cozy rule).
3. **Feedback on the pad before zoom-out:** a thin XP bar under your creature's HP bar fills; `+21 XP` floats up.
   Level up: golden ring burst at its feet, `Lv 6!` pop, a happy hop. Max HP rises and current HP rises by the same
   amount. Pods and cards show the new level.
4. Several level-ups in one go play quickly (about 0.4 s each), not one long sequence.

## B. Evolution
5. Uses `CreatureSpecies.evolvesTo` / `evolveLevel` (fill in levels: Kilnpot→Kilnhearth 14, Toastling→Crumbhog 12,
   Waxle→Lanternsnail 12, Glazegolem→Kilngolem 16; Kettlewing→Steamswan is waiting on art, so leave it empty).
6. Triggers after the XP sequence, still on the pad: the creature glows white, then swaps between the old and the
   new mesh at the same silhouette scale, faster and faster (3–4 swaps over about 2 s), then a light burst and the
   new form does a hop. No cancel. Keeps the HP ratio, level, XP and nickname.
7. Dialogue `evolve.first` ("Look at that! They've grown into something new.") and pulse on `nearPad`.

## C. Hearth Shrine
8. Prop recipe **HearthShrine** (prop kit, Cinderwood palette): a small stone hearth with a flame bowl and a mossy
   cap like the Spirit's (hint they're linked). Map char code: pick a free one. Place one near spawn and one near the
   test tall grass on the test map.
9. Explorer steps onto the shrine's front tile → flame flares, warm wave rolls over the rim pods (each pod's glow
   refills, fainted pods un-grey) = **heal all** and **save**. A small "Saved" puff by the Spirit. Dialogue
   `shrine.first` (already in Script_v1), anchor on the shrine tile.
10. **Swap while standing at the shrine:** a floating **storage shelf** appears beside the island's right side (two
    rows of 6 pods; more → small left/right arrows, poke or look + pinch). Grab a storage pod and drop it on a rim
    socket = swap (or onto an empty socket = add). Look + pinch a storage pod then a rim pod = same swap. Can't
    leave the party empty. Walking off the tile hides the shelf (shrink + fade).
11. Lose everything → explorer returns to the **last shrine used** (spawn if none), party healed (replaces the
    step 6 spawn rule).

## D. Save / load
12. One JSON file in `Application.persistentDataPath/save.json`, with a `version` field. Species by a stable id
    (a `SpeciesRegistry` asset listing all species; id = asset name). Contents: party and storage (species id,
    level, xp, hp, nickname), explorer world tile + facing, last shrine, realm restoration state per realm, dialogue
    said-ids, caught-species set (for the future "Caught 10 / 30" counter), walk mode setting.
13. **Autosave:** at a shrine, after a catch, after a battle, on `OnApplicationPause(true)` / focus lost (headset
    off) and on quit. Write to a temp file then rename (no half-written saves).
14. **Load on start:** save exists → restore. No save → new game (Kilnpot Lv 5 for now; starter choice is step 8).
    Unreadable or newer-version save → rename to `save.bad.json`, start new, log a warning.
15. **Pause / resume (a competition rule):** while paused, freeze battle timers (8 s dazed window, 6 s auto
    send-out), walking and dialogue. On resume, everything continues where it was.
16. Debug (Editor + BattleDebug): X = +50 XP to the lead, L = lead levels up, K = save now, Shift+K = delete save.
    Menu **Tools > Pocket Isle > Delete Save**.

## E. Tests (EditMode)
17. XP curve and cap, multi-level-up, HP increase on level-up, evolution trigger at the level (and not without
    `evolvesTo`), save round-trip (party, storage, said-ids, restoration), corrupt save → new game + `.bad` file,
    swap rules (party never empty).

## Housekeeping
- One compile batch per section. EditMode tests green. Flat Editor Play smoke test: win a battle (XP, level-up),
  force an evolution with L, step on a shrine (heal, save, shelf), quit and re-enter Play (state restored).
- Mugesh is out: **commit locally per section group once it compiles and passes the smoke test**, with
  "(headset test pending)" in the message. Don't move `pre-goa`. No AI attribution lines. Don't push.
- Update `.claude-handoff.md` and the "Status" in BuildPlan.md. Write a short summary for Mugesh: what changed, what
  to click, what to test, and anything you weren't sure about.

## Test on the headset (later)
1. Win a battle: XP bar fills, a level-up plays on the pad, the pod card shows the new level.
2. L (or a few battles) up to the evolve level: the evolution plays, and the new form is in the pod.
3. Walk onto the shrine: party heals, "Saved", the storage shelf appears; swap a creature in and out.
4. Take the headset off for a minute, put it back on: everything resumes, nothing lost.
5. Quit the app and relaunch: same party, same place, same restoration state.
6. Lose everything: back at the last shrine, healed.
