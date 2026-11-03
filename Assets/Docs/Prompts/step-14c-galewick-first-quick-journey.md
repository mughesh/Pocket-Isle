# Step 14c: Galewick first, the new Quick Journey, Cinderwood's arrival moment, realm name cards

Decided Nov 3 (design chat). Orient first if you're a fresh session (`CLAUDE.md`, `.claude-handoff.md`, `Run-14a.md`,
GameDesign "Quick Journey as built", `QuickJourneyConfig`, `QuickJourneyDirector`, `QuickJourneySim`, the Spirit's trail
in `Onboarding`/guidance). Mugesh is around: commit per section "(headset test pending)", tell him what to test. No AI
attribution, don't push. Builds only if he asks.

## A. Galewick is the first realm (full adventure and Quick Journey)
1. After the tutorial, the Spirit's trail, the hub's guidance lines and gate glow point to the **wind gate first**, then
   (after the wind shard) **Cinderwood**, then **Lilymere**. The player can still walk into any open gate (all three
   gates open in builds now: set `LilymereConfig.open` = true; the realms are all built). Update the hub's "next goal"
   order, any line that names the fire gate as the first stop, and the Quick Journey's `quick.gate` line.
2. Re-check levels for this order with the sims: wild levels and Keepers already scale with shards held; run the XP
   trail + Keeper sim for **Galeowl at 0 shards** (target 60–75% per starter for a normal player who caught 1–2 Islings
   on the way), **Hearthstag at 1 shard**, **Tidemother at 2 shards**. Tune only levels/XP within those targets; report
   the tables.

## B. The Quick Journey becomes a Galewick journey
3. **Opening and lessons: unchanged, all of them** (Mugesh: judges must learn every interaction). The quick slot keeps
   its existing cuts only where they're off the route; don't cut more.
4. **The gift** (after the capture lesson, as today): two friends for Galewick: **Toastling** (fire beats wind) + a wind
   Isling (pick from the Galewick roster by sim). Line: "The road to Galewick is windy. Take two friends with you!",
   then "Bring back the wind shard!" and the Spirit goes to the **wind gate** (gold).
5. **Galewick in the quick slot:** keep the arrival sweep, the mill flick, the kites, the tower, the untangle and
   Galeowl. **Pre-solve the ridge cloud bank** (the sweep was taught at the arrival). Dials as Cinderwood's were:
   encounters x0.5, XP x2, no rares, gold tiles after 4 s standing still. Keeper sim for the quick team: 75–85% per starter.
6. **Timing:** update `QuickJourneySim.DryRun` for the new route (opening + lessons, gift, gate, arrival, mill, kites,
   tower, untangle, Galeowl, home, statue). Target **≤ 12 min**. If it's over, report the per-beat table with options
   (e.g. pre-solve the mill, 2 kites instead of 3 in the quick slot) and **ask Mugesh** before cutting anything else.
7. **The end:** the wind shard locks into the statue → the statue whispers its wind memory (`heart.dream.wind`) → the
   Spirit: "Did the statue just... speak?" → `quick.end`: "You woke the first piece of the world! Two more realms are
   waiting." with three runes: **Keep exploring** (all gates open, quick dials off outside Galewick, the trail points to
   Cinderwood next), **See the ending** (plays the finale from the wake, as the dev panel's "Finale: play from wake"
   does, on a temporary copy so the quick save isn't marked finished; afterwards back to the title), **Title**.
8. Dev panel Save tab: the Quick presets (start, gift, gate, temple) now point to Galewick (gate = wind gate, temple =
   tower hall door).

## C. Cinderwood's arrival "aha" (every realm teaches its gesture right after the portal)
9. Two to four tiles past Cinderwood's arrival, the trail into the forest is dark: **four dead stone lanterns** line it
   (the hollow's lantern prop and piece, a second short instance). Arrival line `cw.arrive`: "Cinderwood... it used to
   glow all night." then the objective `cw.arrive.lanterns`: "The lanterns went out. Touch one!" chip "Touch" +
   lantern count. Touch one (or two in a row): fire races down the four, the path ahead brightens and a few embers
   drift up (the aha). The lantern lesson + ghost move here; the Smoky hollow later keeps its counter, short line only.
   No blocking needed (the path is walkable either way), but the trail/guidance waits for it once.

## D. Realm name cards and gate signs
10. **On entering a realm** (every time, shorter after the first): a **carved title card** rises over the island's far
    half for ~3 s, then sinks: realm name in the logo's chunky lettering style, an element emblem, and one line under
    it. First visit: name + line; later visits: name only (1.5 s). Lines:
    | Realm | Line |
    |---|---|
    | Still Isle | "Home" (only when coming back from a realm the first time) |
    | Galewick | "The windmill highlands, where the wind stopped" |
    | Cinderwood | "The autumn forest, where the fire went cold" |
    | Lilymere | "The lily lagoon, where the water froze" |
    Restored realms: the line becomes "...where the wind blows again" etc. Style: a wooden plank sign for Galewick,
    a charred-wood plank with ember edges for Cinderwood, a mossy stone tablet for Lilymere, toon + outline,
    readable from the default view, never over the explorer's path view during an objective (it waits until the
    arrival line has shown).
11. **Gate signs on the hub:** a small sign above each gate arch with the realm's name in its colour (green wind,
    red-orange fire, blue water) and its emblem, readable from the default view; it glows gold on the gate the trail
    points to.

## Housekeeping
- EditMode tests green (guidance order, quick route with pre-solved ridge, end card runes, See the ending doesn't
  mark the quick save finished, name card timing). Update `.claude-handoff.md`, GameDesign.md (realm order, Quick
  Journey as built v2, name cards), QA-Guide ("what's new"), and the QA brief's Galewick-first order in
  `QA-MetaChecks.md` if it names the order.
- Shots `Logs/BattleShots/14c_*`: hub gate signs, each realm's name card, Cinderwood arrival lanterns before/after,
  the quick end card.
- Report `Run-14c.md`: commits, sim tables (Galeowl / Hearthstag / Tidemother per order; quick team), the dry-run
  table, headset test list in order.
