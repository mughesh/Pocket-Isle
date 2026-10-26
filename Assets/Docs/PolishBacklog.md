# Polish backlog

Small, non-blocking improvements noticed during testing. **Showstoppers get fixed right away; everything else goes
here** and gets done in the polish days (Nov 13–14), most valuable first. Add a line whenever you notice something.

| Added | Item | Notes |
|---|---|---|
| Oct 26 | **Grab-lock jigsaw corners** (Mugesh's idea): the island's corners are puzzle-piece slots; a snapped-in piece = island locked, a visible gap = unlocked; the pieces double as handles for one/two-hand grab and zoom | Future grab-lock iteration; the 8e pinch/palm modes were reverted to the older grab |
| Oct 26 | **Glazegolem should look clearly rare** (Mugesh): richer glaze, gold kintsugi cracks, a sparkle or shimmer | Do with the character pass. Step 9b prompt |
| Oct 25 | **Lock island** toggle (no accidental grabs) | In the Spirit menu. Mugesh's note, step 8c |
| Oct 25 | Ghost start / end points per target: the grab ghosts (pods, cradle, rim) still land the palm on the target's centre | 8c did the shard (front surface) and the attack lesson (nearest rune). Needs a per-target contact offset |
| Oct 25 | **The Spirit as the menu**: poke or look + pinch the Spirit, a small ring of options round it (Replay tutorial, Recenter, Volumes, New game, Quit) | Design with Mugesh first |
| Oct 25 | ~~Ghost hand placeholders: thumb swipe / tap / pinch poses are rough~~ | Done: Mugesh's recordings |
| Oct 25 | ~~Shader warmup at launch (shard, fx orb, portal, pod variants)~~ | Done in 8c (drawn once before the shard). Confirm on device |
| Oct 25 | A dedicated palm-grab recording for the island ghosts (move, two hands) | 8d turns the rim-slide recording so its palm lies on the base side; a real side grab would read better |
| Oct 25 | Remove the 8c rim corner code (`Step8cSetup.RimZones`, `PodRack.SocketBandLocal`) once the 8d skirt zone is confirmed | 8d's setup replaces those boxes right after the 8c setup builds them |
| Oct 25 | Starter pods a bit small at the default island size | podScale 3.2, grows 1.45x on gaze |
| Oct 25 | Windmill blades on the Wind gate could spin | Static now (Sway only) |
| Oct 24 | Lantern Spirit size and position tuning | Fine for now |
| Oct 24 | Real explorer character | Replace the orange cube |
| Oct 23 | Ambient life: fireflies, birds, butterflies | Fireflies cheap (particles) |
| Oct 23 | Friendly creature villagers in the kiln village | One-line hints |
| Oct 22 | Movement feel and speed tuning on the real Cinderwood map | |
| Oct 22 | Golden Berry crafting | First to cut |

## From step 9h (Oct 26)

| Added | Item | Notes |
|---|---|---|
| Oct 26 | Rock-slab look for the title and menu runes | Runes stay like the battle runes for now |
| Oct 26 | **Guided ghost recorder** (Mugesh's idea) | Dev panel "Ghost" tab walks through each lesson: "Record this next: <lesson>", Ready, 3-2-1, records while he does the real action, stops when the lesson's done event fires, preview, Keep / Redo; clips saved with their source / target anchors so the 9h retarget applies. Nov 3, only for clips that still look wrong after 9h |
| Oct 26 | Quick Journey 10-minute design pass | Separate design session |

## Lodge life (do with scene filler props)

| Added | Item | Notes |
|---|---|---|
| Oct 25 | Poke the hearth to stoke it | Embers burst, Islings gather round |
| Oct 25 | Pet an Isling with an open hand | It leans in, little hearts |
| Oct 25 | A berry bowl to fill | |
| Oct 25 | A toy ball you toss that Islings chase | |
| Oct 25 | Wind chimes / hanging lanterns you can flick | |
