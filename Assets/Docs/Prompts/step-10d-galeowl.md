# Step 10d: Galeowl (untangle, battle, restoration, wind shard)

Read CLAUDE.md, `.claude-handoff.md` (9c Hearthstag, 9e fury, 9f balance + Keeper sim), `RealmsDesign.md` (Galewick:
"Untangle the Keeper", "Keeper: Galeowl"), `02_keepers_turnaround.png` (Galeowl), the 10a/10b notes.
Mirror Hearthstag's flow wherever possible: reuse KeeperHall, KeeperRules, KeeperFury, the shard holder, statue
sockets and RealmRestoration. Only the untangle and the look are new.

1. **Tower hall** interior (like Forge Hall, round stone room, wooden beams, tangled kite strings hanging, a big
   window of still sky). Enter through the opened tower door (10b D); doorway back out.
2. **Galeowl** model: big round owl ~2.5x an Isling, slate-blue + cream, kite-tail feather tufts, wind-vane crest,
   rigid wings that open and flap, toon + outline, ≤ 10k tris. Frightened: grey storm aura with string scraps
   whipping round, ruffled (scaled-up feather tufts), red-gold eyes, trembling (KeeperFury palette); calmed: soft
   grey-gold, amber eyes, bows.
3. **Untangle** (before the battle): Galeowl wrapped in string loops with **3 glowing knots**. Each knot has two loose
   ends with gold dots. Pinch both ends (one per hand) and pull apart > 12 cm: twang, the loops fall away, the owl
   shudders. One hand works too (pinch one end, pull away > 15 cm). Look + pinch a knot = it loosens by itself in 2 s.
   Lesson `gw.keeper.knots`: "It's all tangled up! Pull the knots apart." chip "Pull apart!". All 3: it shakes free,
   rears, the fury aura flares, `gw.keeper.meet` lines, battle.
4. **Battle:** Lv 11 at 1 shard (scaled by shards held; Lv 9 at 0), HP ~2.75x, moves Gale Shot (wind), Talon Dive
   (neutral, every 3rd turn), **Gust Wall** shield (breaks to a Fire move or 3 hits, 2 open turns, returns), phase 2
   below 50%. No capture; Run works; losing = last shrine. Run the Keeper sim per starter with a Fire helper available
   in Galewick: target 60–75% (0 shards) and report.
5. **After the win:** calms and bows, the tower's vanes and the hall's window fill with wind, the wind shard floats to
   you into the holder's second cup; outside a ~5 s restoration wave from the tower (10a restored state: grass waves,
   mills spin, kites fly everywhere, seeds drift); return gate. Hub: the statue's wind socket.
6. Lines `gw.keeper.*` (meet, shield, shield.break, win: 3 short lines in Galeowl's voice: afraid the wind would blow
   the pieces apart, thanks the guardian), `gw.restored`.
7. Unseal: `GalewickConfig.open = true` for builds **only if** 10a–10d pass their tests; say so in the report.

Housekeeping: EditMode tests green, commit per section "(headset test pending)", don't push. Update
`.claude-handoff.md`, GameDesign.md, QA-Guide/TestPlan (a Galewick run).
