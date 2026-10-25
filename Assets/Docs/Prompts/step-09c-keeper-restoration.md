# Step 9c: Hearthstag, the restored realm, the first shard

Read CLAUDE.md, `.claude-handoff.md`, GameDesign.md ("Realms and story", "Cinderwood", "Battle"), step-09b-hand-puzzles.md,
Script_v1.md, and the concept sheet `Assets/Docs/ConceptArt/08_hearthstag_v2.png`. 9b is built (headset test pending).

## Mugesh is asleep: unattended run rules
- **No headset.** Don't use Play over Link and don't build to the device. Test in **flat Editor Play** (arrow keys,
  Space, debug keys, the Claude bridge). For this session you may untick *Initialize XR on Startup*; **tick it back
  at the end and never commit that change**.
- Commit per section with "(headset test pending)". No AI attribution. Don't push. Don't ask questions: take the
  most reasonable reading, note it in the summary.
- **Housekeeping first:** delete the untracked `_lanes/` folder at the project root (an abandoned idea; Mugesh
  approved). Don't touch any other untracked file.
- When done, continue with `step-09d-fire-roster.md`.

## A. Hearthstag model (the Keeper)
1. A great stag, **~2.5× a normal Isling's height** on its pad. Sturdy legs, broad chest, kind but tired face.
   **Chunky ember-crystal crown**: short blocky crystals, **not** big branching antlers (IP rule: nothing like any
   known legendary deer). Follow the v2 concept.
2. Rigid pivot parts: each crown crystal (own glow per crystal), head, ears, tail, plus a **shield shell** (a
   translucent faceted ember dome around it, off by default).
3. Two palettes on the same mesh: **frightened** (ash-grey fur, crystals flickering dull red) and **calmed**
   (warm chestnut fur, crystals bright gold).
4. Species: Fire, Keeper flag, not catchable, Lv 12, HP ≈ 2.5× a normal Lv 12. Moves: Ember Shot (shoot), Crown
   Charge (lunge), Crown Shield (shield template, special rule below). Budget: body ≤ 10k tris, parts ≤ 3k.

## B. The Forge Hall meeting
5. Entering the Forge Hall the first time: Hearthstag lies curled around the cold forge, **frightened palette**,
   crystals flickering. Spirit (`cinder.keeper.first`, → Hearthstag): "It's scared... the fire is trapped inside it."
   Then: "Let's calm it down. Gently!" Hearthstag rises, the crown flares, the battle starts (same zoom, clearing and
   pads, inside the hall).

## C. The Keeper battle
6. Team battle as usual (switch any time). **No capture** (cradle hidden). **Run** works: you're back at the hall
   door and Hearthstag resets.
7. **Crown shield:** it starts shielded (shell visible, crystals bright). While shielded, hits do ~10% damage and
   show "Blocked!". A **Water** move breaks the shield at once; otherwise **every hit cracks one crystal**, and
   **3 hits** break it. Broken: shell shatters, crystals go dim for **2 turns** of normal damage, then it
   re-raises the shield (crystals flare back).
8. Every 3rd turn it uses Crown Charge. **Below 50% HP:** phase 2: crystals burn brighter, Ember Shot hits harder;
   Spirit: "It's fighting the fear. Keep going!"
9. Losing everything: back to the village shrine, healed, nothing lost; Hearthstag resets.
10. Lines: `keeper.shield.first` → Hearthstag ("Its crown shields it! Water breaks it, or keep chipping away."),
    `keeper.shield.broken.first` ("The shield's down. Now!").

## D. Victory and restoration
11. Win: Hearthstag kneels; its palette fades from frightened to **calmed** over ~2 s; it nods. Spirit: "It wasn't
    angry. It was scared, holding all the fire in."
12. The forge behind it ignites. **The fire shard** (a glowing ember crystal, ~3 cm) rises from the forge and floats
    toward the player. Spirit: "The fire shard! Take it." **Grab it** (pinch) → it goes into a **shard holder** on
    the rim (a small brass cradle at the back-left corner, new; three slots). Look + pinch fallback. Ghost lesson
    with `grab_drop`.
13. Scene swap back outside, then the **restoration wave**: `RealmState.Restored` 0 → 1 rippling outward from the
    temple over ~5 s (embers rise, lanterns and kilns light, braziers bright, springs steam, ember vents glow,
    leaf tips glow). Spirit: "Cinderwood is warm again!"
14. A **return gate** appears in front of the temple (a shortcut home, also for Quick Journey later).
15. Saved: Keeper calmed, realm restored, shard held. Revisiting the Forge Hall shows Hearthstag calm and resting
    (no battle; poke it → a happy snort).

## E. The shard into the statue
16. In the hub, while you hold a shard, the statue's matching **socket glows** and the Spirit points at it
    (`hub.shard.first`, → statue socket: "Put the shard in the statue. Something's calling it.").
17. **Grab the shard from the rim holder and place it in the red socket** (Carry kit: the socket is the target
    spot; look + pinch fallback). It locks in with a light pulse; red cracks glow on the statue; some moss falls
    away; the Spirit flickers ("I... feel warmer. Strange."). Saved (shards placed: 1/3).
18. Then the Spirit: "Two more shards are out there." The water and wind gates stay sealed (Lilymere and Galewick
    come later); their sealed line stays.

## F. Debug and tests
19. Debug key / bridge command: jump to the Forge Hall with a Lv 14 party (for testing the fight quickly), and one
    to grant the fire shard in the hub.
20. EditMode tests: shield rules (Water breaks, 3 hits break, re-raise after 2 turns), phase 2 at 50%, no capture,
    restore/save round-trip (calmed, restored, shard held / placed).
21. Flat Editor smoke test of the full chain: enter hall → battle → win → shard → restoration wave → return gate →
    hub → shard into the statue. Update `.claude-handoff.md`, GameDesign.md (as built), Script_v1.md.

## Test on the headset (Mugesh, later)
1. Walk into the Forge Hall: Hearthstag is grey and scared; the Spirit explains; the battle starts.
2. Its crown shield blocks; 3 hits (or a Water move) break it; it comes back after 2 turns.
3. Win: it calms (grey → chestnut, gold crown), the forge lights, the shard floats to you; grab it.
4. Outside, Cinderwood comes alive in a wave from the temple. A return gate takes you home.
5. Put the shard in the statue's red socket with your hand: it glows, moss falls, the Spirit feels warmer.
