# Step 6: capture pods + party on the rim

Read CLAUDE.md, BuildPlan.md (v4), GameDesign.md ("Battle", "Capture", "Party and collection") and Script_v1.md.
5e passed (commit it if not done). Goal: the full **find → fight → catch** loop with your hands, and a party of up
to 6 living in glass pods on the island's front rim. The island's front always faces the player now (5e).

## Pods
1. Pod prefab (bake with the kit): a glass bubble with a brass band, ~3 cm at default island scale. Inside: a tiny
   copy of the creature (its baked mesh, scaled down, slowly turning). Empty socket = dim brass ring.
2. **Six sockets on the front rim**, in the rocky side just under the cap, spread across the right two-thirds
   (the Spirit is at the near-left corner). They must not sit under the battle runes from the player's view.
3. Outside battle: **poke or look + pinch a pod** → a small stats card in the bubble style (name, level, element,
   HP bar). Fainted creatures' pods look grey and cracked-light.

## Sending out
4. On an encounter, the lead pod glows and the Spirit can hint. **Grab the pod (ISDK grab) and throw or drop it onto
   the island** → it bursts on the near pad and the creature pops out. If nothing happens for 6 s, auto send-out
   (keeps it accessible). Look + pinch on a pod also sends it out.
5. **Switch in battle:** grab another pod and throw it in (uses your turn; the current creature returns to its pod).

## Catching
6. In battle, a **Catch** rune joins the runes. Picking it puts an empty pod in a cradle above the rim (or grab an
   empty pod from the cradle anytime in battle).
7. **Flick it at the wild creature.** ISDK grab + release velocity; aim assist: if the throw goes roughly toward the
   wild pad (within ~25°), it homes in. A miss bounces off and returns to the cradle (no turn lost). Look + pinch on
   the cradle pod = auto throw (accessibility).
8. Catch chance from HP % (lower = better), rarity (rare = harder) and level. The pod wobbles 1–3 times; success =
   click + sparkles; failure = the creature bursts out and it's the wild one's turn.
9. Success: the creature shrinks into the pod, the pod flies to the first empty socket. Party full → it streaks off
   the island as a light (storage list in `Party`; Sanctuary comes later).
10. Losing: your whole party faints → zoom out, explorer back to spawn, party healed (the shrine replaces spawn in
    step 7).

## Data
11. `Party`: 6 slots + storage list of `CreatureInstance`. Lead = first non-fainted. Start: the Kilnpot Lv 5 as now.
12. Dialogue hooks: `capture.first` (first time a creature is below 50% HP), `capture.success.first`, `party.full`.

## Small extra: realm theme swap (decided Oct 24)
13. The current Cinderwood look becomes the **unrestored** state (players spend most time there): warm orange but
    a bit muted, lanterns unlit, kilns dim, no embers. The **restored** state goes beyond today's look: brighter
    leaves with glowing tips, lanterns and kilns bright, stronger steam, drifting embers. For now only swap/adjust
    the two palettes in `Themes/Cinderwood` and keep the G / 3-tap toggle; restored-only extras (embers, blossoms,
    extra props) come in step 9.

## Housekeeping
- One compile batch per section. Don't commit until Mugesh tests. Update `.claude-handoff.md`.
- When it passes: commit and move the `pre-goa` tag.

## Test on the headset
1. Kilnpot sits in a glowing pod on the front rim; poking it shows its card.
2. Encounter: grab the lead pod, throw it on the island: Kilnpot pops out. Waiting 6 s also works.
3. Weaken the wild creature, pick Catch, flick the pod at it: wobble, click, it's yours; the pod flies to the rim.
4. A bad throw comes back to the cradle. Look + pinch on the cradle pod throws it for you.
5. Catch a few: the 7th streaks away as a light.
6. Swap creatures mid-battle by throwing another pod.
7. Lose everything: back at spawn, healed.
8. Default Cinderwood now looks a bit muted; G / 3 taps shows the brighter, livelier restored version.
