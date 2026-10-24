# Step 5c: Lantern Spirit + dialogue system

Read CLAUDE.md, BuildPlan.md (v4), GameDesign.md and **`Assets/Docs/Story/Script_v1.md`** (all the lines and
trigger ids). Goal: the Lantern Spirit companion and a small, data-driven dialogue system that every later step
uses by firing trigger ids. **No voice, no choices, no branching.** Keep it small.

## Lantern Spirit
1. Bake it with the creature kit from `ConceptArt/04_lantern_and_heart.png` (left side): glass-acorn body that glows
   warm amber, a crooked mossy stone cap (rigid part), two little ear-wisps orbiting beside it, simple dot eyes and
   a small mouth. Palm-sized (~6 cm at default island scale). Emissive body, gentle bob.
2. Where it lives: hovers just above the **near-left corner of the island rim** (player side), facing the head.
   Follows the island when it's moved or scaled. When it speaks, it bobs up a little and brightens.
   If the player brings an open palm close to it (< 10 cm), it hops onto the palm and rides there until the hand
   moves away fast (nice-to-have; skip if it costs more than an hour).

## Dialogue system
3. `DialogueLine` data: id, speaker (Spirit / Hearthstag / Heart / other), text, `once` flag, priority.
   Import `Script_v1.md` into a `DialogueScript` asset with an Editor menu (**Tools > Pocket Isle > Dialogue >
   Import Script_v1**) so I can edit the markdown and re-import. Format is in the file (trigger · flags · lines).
4. `Dialogue.Say(triggerId)`: queues all lines for that trigger. `once` triggers are remembered (in memory now,
   saved in step 7). Higher priority interrupts idle hints. Never more than one bubble at a time.
5. **Bubble:** world-space speech bubble above the speaker (Spirit by default; Keeper lines appear above the Keeper),
   rounded, warm cream with a dark outline, short tail, text typed in quickly (~45 chars/s), stays for
   `1.2 s + 0.05 s × characters`, then the next line. Readable at ~0.6–0.8 m. Stays inside the central ~70° view.
   **Poke the bubble or pinch while looking at it** to skip to the next line.
6. Speaker portrait not needed. Keeper/Heart bubbles use a slightly different tint.

## Hooks now (others get wired in their own steps)
7. Fire triggers from existing systems: `tut.grass` (first tall-grass step), `tut.pond` (first time next to water),
   `battle.first`, `battle.types`, `battle.win.first`, `battle.lose.first`, `rare.first`, idle hints
   (~40 s without input, max one per 2 min).
8. A debug list in the Inspector to fire any trigger by id, and an Editor key **D** that cycles through all ids.

## Housekeeping
- One compile batch. Don't commit until Mugesh tests. Update `.claude-handoff.md`.
- When he confirms, commit and **tag `pre-goa`**.

## Test on the headset
1. The Spirit floats by the near corner of the island, glowing, bobbing, looking at you.
2. Walk into tall grass the first time: it says the grass line. First battle: the rune hint.
3. Bubbles are readable, never block the battle, and a poke skips a line.
4. Move/scale the island: the Spirit follows.
