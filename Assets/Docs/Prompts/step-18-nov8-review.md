# Step 18: fixes from the Nov 8 long play (23:27 video, judge review) + the first music test

Triaged Nov 8, 2:30 AM (design chat). **The video did not reach the design chat this time**, so every claim was checked
against the code instead: what's below is confirmed in code, or marked **(verify first)** where only the video shows it.
The review's other points were rejected on purpose (see "Not doing"). The Nov 8 build = branch `step-17` at `1f7eff1`.

## How to run (works attended or unattended)
- Fresh session: orient first (`CLAUDE.md`, `.claude-handoff.md` newest on top incl. "Nov 8 (night) notes",
  `Assets/Docs/Run-17.md`, git log).
- Work on branch **`step-18`** from the current HEAD (Mugesh fast-forwards master to `step-17` first).
- **Do not stage or commit** the step 15a work in progress (`ReleaseBuild.cs`, `Step15aTests.cs`, `DeviceBuild.cs`,
  `ReleaseReadiness.cs`, `ProjectSettings/*`, the icon meta, `DevAgentSettings.asset`, the TMP fallback font asset),
  nor `Assets/_Recovery/`, `_lanes/`. Stage only the files you change, by path. Never `git add -A`. (You may commit
  `Assets/_Project/Art/Credits/Mugesh.png` + meta and the music file you move in G.)
- Commit per section, message ends "(headset test pending)", no AI attribution lines. Don't push, don't merge.
- If Mugesh isn't answering: no questions, flat Editor Play via the bridge + tests + the smoke run, revert what you
  can't fix after ~45 min, log decisions.
- Order: **A → G → B → C → D → E → F**. Today is feature-freeze day: A and G matter most.
- At the end: smoke run, `Assets/Docs/Run-18.md` (done / partly / not, old → new values, the battle sim table, the music
  levels, the headset test list in play order) and a `.claude-handoff.md` entry.

## A. Battles must not wipe a cozy player
Seen: in Galewick a Lv 5 wild Samarling knocked out four party Islings in a row (08:24–09:18), and earlier at
04:39–05:08. The party is never healed between wild battles (only by a loss, the lodge or the Dev panel), so chip
damage adds up.
1. **Evolving heals.** `Progression.Evolve` keeps the HP ratio and keeps a fainted creature at 0: Toastling evolved into
   Crumbhog on the palm with "Fainted · HP 0/85" (12:12) and came into a later battle empty. The new form comes out at
   **full HP, not fainted**. The pod's fainted/dim look on the rim clears at once. Add a test.
2. **A little rest after each win:** after a won wild battle (catch or knockout), every non-fainted party Isling gets back
   **25% of its max HP** (a soft heal sound + a small green "+" on the rim pods). Fainted ones stay fainted until the lodge
   or a loss. Keeper battles unchanged.
3. **No one-shots from weaker wild ones:** simulate (EditMode test) wild Samarling, Vanehare, Kitelet, Ashmole,
   Coalbeetle at Lv 5–8 against full-HP party Islings at the same level and +1/+2. Rule: a wild Isling never knocks out a
   **full-HP party Isling of its level or higher in fewer than 3 hits** (super effective may take 2). Report the table
   before/after; tune wild damage only (not Keepers, not the player's damage).
4. **After a faint, the new Isling acts first:** a forced switch is already free (`BattleSystem.Switch`), but the next
   turn can still let a faster wild one strike first, so it reads as "it hit me the moment I tossed in". The turn right
   after a forced switch-in, the player's creature moves first.
5. **The pod lights up a bit sooner:** `BattleMath.RedHpShare` 0.25 → **0.33**. Verify with the bridge that a wild one at
   14% HP has the glowing cradle pod and the throw works (the review says the pod stayed locked at a sliver, 04:58 and
   08:52: **verify first**; if it really was locked, find why and fix it).

## G. First music test: "First Light at the Summit" in Galewick
Mugesh made one track (`Assets/_Project/Audio/First_Light_at_the_Summit.mp3`, 116.6 s, stereo 44.1 kHz, about
**−11.8 LUFS**: loud). Measured shape: soft intro (−28 → −18 dB RMS over 0–40 s), full body 40–103 s, its own fade-out
103–116 s. It goes on **Galewick's overworld** (the name fits the windy summit, and Galewick is the Quick Journey's
realm, so it's the best test of how music feels over a long stretch).
6. Move it with the AssetDatabase to `Assets/_Project/Audio/Clips/Music/music.galewick_1.mp3` (keep the GUID). Import:
   **2D** (it's 3D now), Streaming, Vorbis ~0.5, keep stereo, no normalize. Add it to the `music.galewick` cue
   (`awaitingClips` off).
7. **Play it whole, then rest** (no loop cut; ear fatigue in MR is the risk): the track plays once, start to its own
   fade-out, then **~45 s of ambience only**, then again. Add this to the music path generally (a cue field like
   `restSeconds`; 0 = today's loop), only `music.galewick` uses it now. The first play on arriving in Galewick starts
   after the realm name card, fading in over ~3 s.
8. Battles: the wild/Keeper battle cues are still silent, so music fades out (0.6 s) for battles; afterwards it does
   **not** restart at once: it waits for its rest, then plays from the top. Inside halls (tower) it keeps going at −6 dB;
   it stops for the Keeper battle. Dialogue duck stays (−4 dB).
9. Level: sitting under the ambience and the Spirit's voice, never on top. Start around **cue volume 0.25** and report
   in-game loudness next to the ambience and the Spirit's blips (AudioScope or the step 16 measure tool). Make the cue
   volume a Dev panel slider for Mugesh's headset test.
10. Add the track to the licence/credits list as "music: Mugesh (source tool to confirm)", no brand names in the app.

## B. Statue and shards
11. **Socketed shards shake with the statue.** During the finale wake-up `StatueSocket.Shake` moves the statue
    (`StatueCenter + shake`), but socketed shards are drawn at the fixed `SocketWorld`, so they hang still and clip in and
    out of the shaking stone (21:20–21:26). Add the same offset to the socketed shards (and their glow/ring).
12. **Temple shard sunrays on the floor (verify first):** at 16:15 the sunrays reportedly sat on the hall floor ~1 s
    before jumping to the shard. `KeeperHall.DrawShardRing` draws at `_shardLocal`; don't draw until the shard's
    position is set (no frame at the default (0,0,0)).
13. **Statue palm ghost (verify first):** the review saw a big vertical open palm over the statue (18:51, 19:31, 21:11).
    The palm carry is the Nov 8 design, so keep it, but its `palm_lift` ghost must show a **flat palm facing up** under
    the shard (the evolve ghost's fixed pose), small, beside the statue (never over it), fading when a real hand is near.
    Shoot it with `Step17Debug GhostAt` and put the shot in the report.

## C. Tall grass doesn't hide the battle (verify first)
14. The review saw tall yellow grass at the front-left of the battle stage hiding the player's Isling and its HP
    (04:45, 08:35, 16:40). During a battle, tall-grass blades between the head and either pad or the HP labels flatten
    (y ~0.15, 0.3 s) and grow back after. Check with a battle in Galewick's ridge and kite fields.

## D. Poke a rim pod for its card while exploring
15. Rim pods are inert while exploring (step 8e: no grab, no poke), so the only way to see an Isling's HP is the lodge,
    gaze + pinch, or a battle (09:37, 12:54: he held a hand under the rim for seconds). While exploring, a **fingertip
    poke** on a pod shows its card for ~3.5 s with a soft glass tap. **No grab while exploring** (unchanged). A resting
    hand must not trigger it: needs the index tip, not the palm, and the hand moving toward the pod.

## E. The title screen shows no party pods
16. After the finale → Title (23:10) the six party pods sat on the rim under Continue / New journey / Settings. Hide the
    rim pods and the menu stone while the title is up; show them when play starts.

## F. Swipe → tap reads as one move
17. The swipe ghost already got the gold thumb dot, size and yaw (Nov 7–8). In `thumb_swipe_tap` (far lesson), cut the
    pause between the swipe and the tap to ~0.3 s so it reads "slide, glide, tap".

## Not doing (decided)
- **Samarling's element:** it is Wind (`element: 3`) and Fire beats it. The "Not very effective" at 08:33 was Samarling's
  wind move landing on Toastling (wind on fire), shown over Toastling.
- **Galewick "mostly Fire":** only the `GwFire` meadow is fire-heavy, on purpose (Fire breaks Galeowl's Gust Wall);
  arrival, kites, ridge, village and rare zones are mostly Wind.
- **1-HP mercy on wild creatures:** a knockout already leaves the wild one dazed for a sure catch.
- **New voices for Keepers and the statue:** Keepers already have their own voice cue (`VoiceKeeper`); not now.
- **+4 dB attack hits, tile-ripple marimba foley, a Keeper heartbeat pulse:** not measured, and the battle music isn't
  made yet; after the music tracks exist.
- The review's code snippets and numbers (`isFainted`, 0.28 m, 35%): guesses; use the real names and the rules above.
