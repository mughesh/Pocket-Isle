# Step 10e: Galeowl part two (keeper flow for any realm, tower hall, untangle, victory, wind shard)

Read CLAUDE.md, `.claude-handoff.md`, **`Assets/Docs/Overnight-Oct27.md`** ("Blocked": the plan for part two),
`step-10d-galeowl.md` (items 1, 3, 5, 6, 7), `RealmsDesign.md` (Untangle the Keeper), `KeeperHall.cs`,
`ShardHolder.cs`, `StatueSocket.cs`, `RealmRestoration.cs`, ForgeHallTools.
Mugesh is here today (last build day before Goa): he tests on the headset between sections, so commit each section
and tell him what to test. Before Play over Link, ask him to restart Unity.

## A. One keeper flow for every realm (do first, protect Cinderwood)
1. Generalise `KeeperHall`, `ShardHolder`, `StatueSocket` (and anything else hard-wired to `cinder.keeper` /
   `shard.fire` / the fire socket): keeper id, shard id, species, interior, socket and its colour become fields or a
   small `RealmKeeper` data asset; today's fire values are the defaults so Cinderwood's scene data doesn't change.
   `ShardHolder` holds any number of shards (circle the Spirit's lantern evenly, offer the one whose socket is near).
2. **Cinderwood regression gate:** before and after, run the flat bridge Hearthstag flow (meet → battle win → shard
   floats to the Spirit → restoration wave → statue socket) and its tests; must behave identically. Commit A alone.

## B. Tower hall
3. Interior like Forge Hall (round stone room, wooden beams, tangled kite strings hanging from the beams, a big round
   window of still grey sky, an empty perch). Tower door (10b) leads in; doorway leads back out in front of the tower.
   `WindTowerOpen` stops "leading nowhere".

## C. Untangle (before the battle)
4. Galeowl sits on the perch wrapped in string loops with **3 glowing knots** on its body; each knot has two loose
   ends with gold dots. Pinch both ends (one per hand) and pull apart > 12 cm: twang, loops fall away, the owl
   shudders. One hand also works (pinch one end, pull away > 15 cm). Look + pinch a knot = it loosens in 2 s.
   Fingertips near Galeowl never walk the explorer (`TileTouch.Block`, as 648e2b0 did for the mill).
5. Lesson `gw.keeper.knots`: "It's all tangled up! Pull the knots apart." chip "Pull apart!", pinned until the first
   knot snaps; ghost = two hands pinching and pulling apart (mirror a pinch clip on both hands if no recording).
6. All 3: Galeowl shakes free, flaps, the fury aura flares, `gw.keeper.meet` lines, battle (the 10d data). Lost or
   fled: the hall resets to "free but frightened" (no re-tangle), the battle starts on poke / walk-up like Hearthstag.

## D. Victory and restoration
7. Win: calms and bows (calmed palette), the window fills with moving clouds and light, the **wind shard** floats to
   the Spirit; `gw.keeper.win` lines; outside a ~5 s restoration wave from the tower (Galewick restored state from
   10a: grass waves, mills spin, kites on posts fly, seeds drift); return gate by the tower. Revisiting: Galeowl
   preens on its perch; poke it for a hoot.
8. Hub: the statue's **wind socket** (green/teal) takes the wind shard like the fire one (offer, grab, lock, glow).
   With both sockets lit, the Spirit says `statue.two`: "Two pieces home. It's almost awake..."
9. Dev panel: Story tab gets Galewick flags (mill, kites, clouds, tower, knots, keeper, shard) and Go to: Tower hall.

## E. Open the gate (Mugesh decides)
10. After A–D pass the flat run, **ask Mugesh** whether to set `GalewickConfig.open = true` for builds. If he says yes,
    the hub's wind gate opens in QA/Release builds.
11. **Build the QA APK** and install it (Mugesh smoke-tests it, then it goes to the QA tester before Goa). Update
    `QA-Guide.md` (what's new: Galewick) and `QA-TestPlan.md` (a Galewick session: mill, kites, gust, tower, knots,
    Galeowl, statue).

## Housekeeping
- EditMode tests green (keeper flow per realm, shard holder with 2 shards, untangle math from synthetic hand tracks).
  Commit per section "(headset test pending)", no AI attribution, don't push. Update `.claude-handoff.md`,
  GameDesign.md. Shots `Logs/BattleShots/10e_*` (hall, knots, calmed Galeowl, restored Galewick, statue two sockets).

## Test
1. Cinderwood's Keeper flow still works exactly as before.
2. Galewick tower: walk in, Galeowl is tangled; pull the three knots apart (both hands, then one hand); it breaks free.
3. Win: the wind shard comes to the Spirit, Galewick wakes up in a wave.
4. Hub: the wind shard locks into the statue; with both shards, the Spirit's line.
