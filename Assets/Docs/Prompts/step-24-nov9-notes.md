# Step 24: Mugesh's Nov 9 notes (battle buttons, Keeper level, temple door, the pour, voices)

Triaged Nov 9, 3:50 AM (design chat), checked in code. Reference shots in `Assets/Docs/Prompts/step-24-img/`.
**Sections A–D need nothing external: do them now. Section E needs the paid ElevenLabs key: do it in the same run as
step 23 Part 2, not before.**

## How to run
- Fresh session: orient (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-23.md`, git log).
- Branch **`step-24`** from the current HEAD (`step-23`/master). Same rules: stage by path, never commit keystores /
  keys / `Assets/_Recovery/` / `_lanes/`; commit per section "(headset test pending)"; no AI attribution; unattended =
  no questions, flat bridge, tests, smoke run. Mugesh said "push" for these runs: when A–D pass tests + smoke run,
  `master` fast-forwards to `step-24` and is pushed (never force).
- Report `Assets/Docs/Run-24.md` with **seat-view shots before/after** for A (a wild battle on the field and a Keeper
  battle in a hall) and the headset test list.

## A. Battle moves sit over the arena, not in front of the rim (`01_…`)
Today the move tablets (Ember Puff · Tackle · Hunker Down · Run) float **in front of and below the front rim**, so the
rim and the pods sit behind them: they feel stuck to your lap and the island looks like it's behind a counter.
1. Move the row (`BattleRunes`) **back and up onto the arena's front band**: its front edge at or just inside the
   rim's inner edge, hovering ~2–3 cm over the cap, tilted toward the head, centred. Never in front of the rim.
2. Nothing may hide behind it from the seat: not the player's Isling, its HP label, the wild one, the caption pill, nor
   the rim pods. If the front band is too shallow, the caption pill ("What will Kilnpot do?") moves above the player
   Isling's label or to the side, and the player's pad may shift back a little. Use the layout checker from the seat
   pose (both the field battle stage and the Keeper hall stage).
3. Reach: the moves stay an easy poke, and at least ~4 cm clear of the rim pods so a poke for a move never grabs a pod
   (keep the step 17 duck-away when a hand goes for a pod).

## B. Keepers show no level (`01_…`)
4. A Keeper's battle label is its name only ("Galeowl", no "Lv 4": a Lv 12 Isling against a Lv 4 boss reads wrong).
   Same in Keeper rematches. Wild and party labels keep their level. (`BattleHud` label text.)

## C. The temple door that wouldn't open (silent block)
Found in code: walking into an open temple door with fewer Islings than the guardian expects asks
**Go in / Not yet** (`TempleRules.Gate`). After "Not yet", or if the board was missed, every bump for
`AskAgainSeconds` returns false **with no line and no board**: the door just doesn't work, then suddenly does.
5. Never a silent wall: bumping the door again re-asks at once (only skip while the board is already open; a ~1 s
   debounce at most). Same for every door that uses `TempleRules.Gate` (Galewick tower, forge, lake temple).
6. The warning line (`TempleRules.WarnLine`) is never dropped by the stale-line rule (like Keeper lines).
7. Every refused entry logs why (`[TempleDoor] refused: ...`). Check the other door types for any other silent refusal
   (door not open yet, `SceneSwap.IsBusy`, explorer locked, bump from a diagonal front tile) and make each one either
   work or say something.

## D. The pour follows the hand (`02_…`)
Today the water sits at one angle in the cupped hand and the stream only appears when the hand is fully tilted, so
tilting feels dead and then sudden.
8. The water sheet in the hand **stays level** (gravity) as the palm tilts and slides toward the low edge.
9. The stream starts at a small tilt (~20°) as a thin dribble; its flow and thickness grow with the tilt, full at
   ~60°; tilting back slows and stops it. The water in the hand drains in proportion. A pour counts ("Pours 1/2") once
   enough has landed in the bowl, not on a single angle threshold.
10. The ghost demo for the pour shows the same gradual tilt. Check the two-hand scoop (`PalmWater`) behaves the same.

## E. Voices for Keepers and the Heart (needs ElevenLabs paid: run with step 23 Part 2)
Today every Keeper and the Heart speak with the Spirit's blip re-pitched (step 18 take 2); the Heart, two octaves down,
sounds like a **moo**. Lines are mostly skimmed, so a speaker's sound is what tells you who is talking and that it
matters.
11. Keep the Lantern Spirit's robotic blip (Mugesh chose it).
12. Each Keeper and the Heart get two things, generated on the paid plan (same licence rules as step 23 E):
    - **An entrance call** when its first line in a scene starts (1–2 s): Galeowl "soft gentle owl hoot, warm, small";
      Hearthstag "deep warm stag call, soft, distant, gentle"; Tidemother "soft low whale song, gentle, short";
      the Heart "deep warm stone hum with a crystal singing-bowl ring, sacred, slow". If a Keeper already has its own
      cry cue, try that first.
    - **Voice grains** for typing (8–12 grains, 0.15–0.3 s, sliced from 2–3 batches): creature-like gibberish murmurs in
      the speaker's character, like Animal Crossing voices: Galeowl soft hooting coos; Hearthstag low breathy huffs;
      Tidemother bubbly low hums; the Heart low resonant chord murmurs (no moo).
13. Grains play like the Spirit's blip (every few characters, small pitch jitter), level-matched under the music.
    A Dev panel switch "Voices: blip / new" for the A/B (default blip until Mugesh picks). Credits as in step 23 G.

## Not doing
- Hiding the player's own level in Keeper battles (only the Keeper's goes).
