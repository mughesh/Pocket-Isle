# Step 13: the finale (the Heart wakes) and the post-game

Decided Oct 28, 2 AM (design chat). No final battle, no giant world reassembly: **everything happens on the hub, at
the game's normal scale.** Modelled on how Pokémon ends (Hall of Fame → credits → the world stays open), with our own
heart: the Lantern Spirit has been a piece of the Heart all along, and the player gives it back with their own hand.
Read `.claude-handoff.md`, GameDesign (Heart of the World, statue, `StatueSocket`, shard flow from 10e/11c,
`statue.three`), `LanternSpirit`, 10f's ember palm carry (`EmberPalm` or similar: reuse it for the goodbye), the
statue prop recipe (`PropKit.Hub` Statue), the Lodge (Islings in storage), `GameSave` (caught species).

## A. The statue means something from the first shard
1. When a shard locks into the statue, the statue **whispers** one memory line in the **Heart's voice**: a distinct
   bubble style rising from the statue (no tail, soft gold-white glow, letters fade in slowly, slightly larger, no
   typing ticks). Then the Spirit reacts. Lines (objective: false):
   | Shard | Heart whisper (`heart.dream.<element>`) |
   |---|---|
   | fire | "...I remember warmth. Kilns glowing all night long..." |
   | wind | "...I remember the wind. Kites so high they touched the clouds..." |
   | water | "...I remember rain. Every pond full of lotus..." |
   | Shards placed | Spirit reaction (`spirit.dream.<n>`) |
   | 1 | "Did the statue just... speak?" |
   | 2 | "That voice... why does it feel like mine?" |
   | 3 | (the finale starts) |
2. Each placed shard also wakes a little more of the statue: 1 = its closed eyes glow faintly; 2 = slow breathing
   (a subtle scale pulse), moss sways; 3 = it wakes.

## B. The Heart of the World (the most majestic model in the game)
3. Mugesh: the statue reads like a dog. Lean into it on purpose: a **shrine guardian** (the sitting temple lion-dog
   kind), made original and noble. The Heart = **the statue come alive**: same spot and footprint, **at most ~1.3x the
   statue's height** (it sits up proud; it must not take over the island).
   - Body: sitting on its haunches, chest high, head raised; mossy grey-green stone with **golden veins/cracks** that
     glow from a core in its chest (emissive, pulsing slowly like a heartbeat).
   - **Mane** of chunky carved stone curls (layered, 2–3 rings) with moss tufts and tiny flowers; curled stone tail
     ending in a **ribbon of wind** (soft green, sways).
   - **Crown of the three elements** on its head: a small ember crest (fire, red-orange), a lotus (water, pink-blue),
     cloud-wing plumes (wind, green-white); each lights as its shard is placed.
   - Folded **cloud-wings** on its back that unfold at the end.
   - **Forehead socket** (an empty round setting) for the Spirit; kind heavy-lidded eyes that open gold.
   - Paws with stone claws, a carved collar with three shard settings (the shards visible there).
4. Budget: the best detail in the game, but still Quest-cheap: **≤ 25k tris, ≤ 8 draws**, toon + outline, rigid pivot
   parts only (head, jaw, eyelids, wings, tail segments, crown pieces). Motions: breathe, blink, head turn toward the
   player, wings unfold, tail sway, a slow nod.
5. **Look critique loop:** turnaround shots `Logs/BattleShots/13_heart_<front|34|side|withExplorer|vsStatue>.png`;
   compare with the old statue and the Keepers; **iterate at least 3 times** on silhouette, proportions and detail
   before moving on (log what changed each round). It must read majestic and kind at island scale from the default view.

## C. The finale (when the third shard locks in, in any realm order)
Lock walking, encounters and the menu's Title for the sequence; Settings and Resume stay. Lines with `objective`
only where the player must act. Duration ~2.5–3 min. A `music.finale` state (cue empty if no file).
6. **Wake (≈20 s):** all three sockets flare; light runs through the cracks; stone flakes and moss fall; the statue
   becomes the Heart (crossfade/morph between the statue prop and the Heart, plus dust and gold motes). It breathes,
   lifts its head. `heart.wake`: "...little light. You found your way home."
7. **Understanding (≈20 s):** the Spirit floats before the Heart's empty forehead. `spirit.finale.1`: "I think... I'm a
   piece of it. Its light." `spirit.finale.2`: "I have to go back. Will you take me?" (objective, chip "Hold out your
   hand").
8. **The goodbye in your palm (player acts):** the Spirit lands on the player's **open palm, palm up** (reuse the
   ember palm-up carry: it sits there glowing, warm halo, the player's hand gently lit). Chip "Lift it to the Heart".
   Bring it near the forehead socket (≤ ~6 cm): it floats off the palm and settles in with a soft chime. Look + pinch
   the Heart = the Spirit goes by itself after a moment. If the player does nothing for 30 s, the Spirit says
   `spirit.finale.wait` "It's okay. Whenever you're ready." (never forces it). As it leaves the palm:
   `spirit.finale.3` "Thank you, big one. For everything."
9. **The Heart opens its eyes (≈20 s):** gold eyes open, wings unfold, the crown's three pieces blaze. **The hub
   answers:** the three gates bloom with their element (fire gate flames up, the well overflows with sparkles, the wind
   tunnel's windmill spins), all hub grass waves, warm motes rise off the island into the room, a soft golden ring of
   light under the island. `heart.thanks`: "Guardian. You carried the world in your hands."
10. **Hall of Fame (≈40–60 s):** `heart.friends`: "And these are the friends who carried it with you." The party (up
    to 6) comes out of the rim pods one by one onto the hub path in front of the Heart: each hops, a small stone card
    shows **name · Lv · "met in <realm>"** (store where each Isling was caught if not stored yet: default "the Still
    Isle" for starters, else the map name at catch; old saves: best guess by species element). Then a few of the
    Lodge's Islings wander out too and everyone cheers. Save a `HallOfFame` record (party snapshot + date) in the save.
11. **Credits (≈40 s):** small stone tablets rise one at a time over the island's far side, readable, then sink:
    "Pocket Isle" / "Made by Mugesh (Heisenberg: The XR Guy)" / "Islings, realms and Keepers: original designs" /
    "Built with Unity and the Meta XR SDK" / "Sounds: ElevenLabs and synthesised (see AudioCredits.md)" / "Music: <to
    come>" / "Thank you for playing." Read real credits from `AudioCredits.md` if present; keep it to ≤ 8 tablets.
12. **The spark returns (≈15 s):** a tiny new spark slips from the Heart's eye down into the explorer's lantern, then
    hops out as the guide: the Lantern Spirit's look, smaller and warmer (no stone cap, a gold glow). `spark.hello`:
    "...hello again. I'll always be in your light." Unlock everything: free play.
13. Make the finale **replayable for the video**: dev panel Story > "Finale: play from wake" and "Finale: from the
    goodbye" (edit the save to 3 shards placed, finale not done, reload). Saved flag `finale.done`.

## D. Post-game (cheap, reuses what exists)
14. **Poke the Heart:** it shows the **Isling collection** per realm, e.g. "Cinderwood 9/12 · Galewick 8/11 ·
    Lilymere 5/11" (caught species vs that realm's roster incl. evolutions) and one warm line. The Spark's idle hint
    after the finale points at missing ones ("I heard something rare by the kiln ruins...").
15. **Complete a realm's roster:** its gate sparkles gold on the hub and the Heart's matching crown piece gets a halo.
16. **Keeper rematch:** in a restored realm's Keeper hall, poke the calm Keeper: runes "Spar? / Not now". A friendly
    battle at +5 levels over the last fight, no shield fury aura (playful), XP reward, no shard. Win or lose, it bows.

## E. Housekeeping
- EditMode tests: whisper per element + reaction by count, finale state machine (steps, saves, resume after quitting
  mid-finale: restart at the goodbye), palm handoff (synthetic palm), Hall of Fame data incl. old saves, collection
  counts, rematch levels.
- Perf: 72 fps budget in the finale (Heart + party out + motes): note draws/tris at peak.
- Commit per section (A, B, C, D) "(headset test pending)", no AI attribution, don't push. Update `.claude-handoff.md`,
  GameDesign.md (replace the old "Finale: final battle" section), QA-Guide. Add missing finale sounds to
  `Audio/PhaseB-Hooks.md` (stone cracking, heartbeat, wings, spark chime, gates blooming, credits).
- Shots `Logs/BattleShots/13_*`: the B turnarounds, each finale step, the palm goodbye (synthetic hand), Hall of Fame
  cards, a credits tablet, the spark, the collection card.
