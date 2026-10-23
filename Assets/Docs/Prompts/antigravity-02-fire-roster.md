# Antigravity task 02: fire creature concepts (Pocket Isle)

You are helping with **Pocket Isle**, a cozy mixed-reality creature-collecting game for Meta Quest (solo entry,
Meta VR Start competition 2026). This repo is the Unity project. **Only add files under
`Assets/Docs/ConceptArt/`. Don't change code, scenes, settings or other docs, and don't commit.**

## Read first
- `Assets/Docs/GameDesign.md`: sections "Cinderwood" and "Fire roster" (names, concepts, rarity).
- `Assets/Docs/ConceptArt/03_starters_turnaround.png`: **the style we liked. Match it exactly.**
- `Assets/Docs/ConceptArt/Concepts_01_Summary.md`: format of the previous summary.

## Why the art rules matter
The 3D creatures are not modelled. Code sculpts them from simple rounded shapes (spheres, ellipsoids,
rounded capsules/cones) melted together, then adds separate rigid pieces that rotate on a pivot (ears, tails,
wings, horns, shells, crowns). There are no bones, so limbs don't bend. Patterns are painted by the shader as
simple zones. **Anything you draw must be buildable that way.**

## Style rules
- Soft vinyl toys: smooth blobby melted shapes, flat 2–3 tone colour bands, dark outlines.
- **No** textures, fur strands, scales, fine detail, thin limbs or thin spikes. Spines/feathers = a few chunky rounded cones.
- Short stubby legs. Big head, big expressive eyes. Readable silhouette at 5 cm tall.
- Patterns limited to: belly patch, coloured tips (ears/tail/feet), socks, a few spots or stripes.
- Front view + side view per creature, plain light-grey background, name label under each.
- **100% original.** Must not resemble any Pokémon, Digimon, Palworld or other existing creature.
  Avoid for fire: lizards, foxes, cats, pigs, moths, pinecones, legendary deer with big branching antlers.

## Sheets (one PNG each)
1. `05_fire_first4.png`: Toastling (bread-loaf hedgehog, a few chunky toasted spines), Waxle (snail whose shell
   is a melting candle with a small flame on top), Kettlewing (round kettle-shaped duckling, steam puff from its
   spout-like beak), Glazegolem (rare and powerful: stack of 3 glazed pots with glowing cracks, stubby pot arms).
2. `06_fire_first4_evolutions.png`: Crumbhog, Lanternsnail, Steamswan, Kilngolem. Same families, bigger and
   grander: extra pieces, longer tails, a crown or chimney. Clearly related to sheet 1.
3. `07_fire_rest.png`: Coalbeetle (round beetle, glowing coal horn), Mapadillo (armadillo with maple-leaf
   plates), Ashmole (mole with a lit coal on its nose), Cindercap (mushroom critter puffing ember spores),
   Embercrane (rare: crane with ember-tipped feathers, keep the neck thick), Steambara (WATER type visitor:
   capybara relaxing in a steam puff, small fruit on its head).
4. `08_hearthstag_v2.png`: redo of the fire Keeper. Big sturdy stag, thick stubby legs, chunky toy body (not a
   realistic deer). Instead of branching antlers: a short chunky crown of glowing ember crystals.
5. `09_kilnpot_family.png`: Kilnpot (existing starter, keep its look) next to its evolution Kilnhearth, a big
   round bear with a clay-kiln back and a chimney puffing embers.

## Summary file: `Assets/Docs/ConceptArt/Concepts_02_Summary.md`
For every creature, a short block (this is what the code-side sculpting uses):
- **Body:** the 3–6 rounded shapes it's made of (e.g. "egg body, round head merged on top, short snout").
- **Rigid parts:** each separate piece and where it pivots (e.g. "2 round ears on top of head, flick").
- **Pattern zones:** which of belly / tips / socks / spots / stripes, and where.
- **Palette:** 4–5 hex colours (base, belly, accent, glow if any, eyes).
- **Idle motion idea:** one line (e.g. "spines wobble when it breathes").
Then a one-line note for anything you think may be hard to build from rounded shapes.
