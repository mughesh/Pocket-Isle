# Galewick and Lilymere: realm design (Oct 26 night, design chat)

Decided with Mugesh. Placeholder names. Template = Cinderwood (60 x 60, trail + side pockets, zone encounter tables,
props baked from recipes, hand moments built on the puzzle kit, a Keeper temple, restoration wave, shard to the statue).
**Galewick is built first** (Oct 27 on), Lilymere after Goa.

## What makes a hand moment beautiful (the bar for every realm from now on)
The log and the bellows are chores with one yes/no result. A good moment has:
1. **Only a giant hand could do it** (scale contrast).
2. **The world answers everywhere at once** (grass waves, water runs, many things react, not one door).
3. **A gesture nobody needs to learn** (cup, sweep, pull up, flick).
4. **It leaves the island and enters the room** where it can.
Every moment also has a look + pinch fallback (same rule as the kit) and teaches itself once (line + gold focus + ghost).

## Unrestored = still, restored = moving (same rule as Cinderwood)
Wind makes this literal: unrestored Galewick is bright and pretty but **nothing moves** (grass stiff, windmill sails
stopped, kites lying in the grass, clouds parked low). Restored: grass waves, mills spin, kites fly, seeds drift.

---

# Galewick (wind): windmill highlands

**Look:** rolling green-gold hills, wheat-coloured tall grass (wind grass), white-sailed windmills on stone towers,
low dry-stone walls, wooden fences, red / orange / teal kites, a stone wind tower temple on a ridge. Palette: sky blue
`#9CC9E8`, meadow green `#9CCB6A`, wheat `#E3C66F`, sail white `#F4F1E6`, slate `#7E8A93`, kite red `#E0644A`,
kite teal `#3FA7A3`.

**Story:** the wind stopped when Galeowl, the Keeper, got tangled in every kite string in the highlands while trying to
hold the wind still (it was afraid the gusts would blow the pieces of the world further apart). Lines in the
`gw.*` set, written with each step.

## Route (trail + pockets, 60 x 60)
| Order | Place | Hand moment | Result |
|---|---|---|---|
| 1 | Arrival meadow by the return gate (hub's wind gate = the windmill tunnel) | — | `gw.arrive`: "Galewick... it's never this quiet." |
| 2 | **Windmill village** (4 cottages, 2 small mills, 1 big mill by a ditch) | **Flick the windmill** | The big mill's sails spin, its gears lower a **drawbridge** over the ditch |
| 3 | **Kite field** (3 kites lying in the grass, spread over a small loop) | **Kite into your room** | Each kite you pull up flies on a string tied to the explorer's backpack, above the island in your room; they follow the explorer for the rest of the realm |
| 4 | **Windy ridge** (a low cloud bank sits across the trail) | **Fan the wind** | Sweeping an open palm over the island blows the cloud bank off the path in big puffs; every sweep ripples all the grass and nudges the mills |
| 5 | **Wind tower** (temple on the ridge top) | (the kites) | The explorer arrives with 3 kites: they tie onto the tower's crown, the vanes turn, the door opens |
| 6 | **Tower hall** (interior like Forge Hall) | **Untangle the Keeper** | Galeowl wrapped in strings; pull 3 knots apart with two hands, it shakes free, fury, battle |
| 7 | Restoration | — | Wave from the tower: grass waves, every mill spins, kites rise everywhere, seeds drift; the wind shard |

Side pockets: rare grass patch (Zeppelope), a hidden orb spot (kept as a landmark only), a fire-visitor patch (fire
beats wind), a shrine near the village.

## The four hand moments
**Flick the windmill (village, teaches the realm's verb gently).** A fingertip moving fast across the sails (tangential
to their plane, > 0.5 m/s, within 2 cm) adds spin; one flick = a quarter turn that slows down; 3 flicks within 4 s keep
it turning and the drawbridge lowers (stays down, saved). Look + pinch the sails = one flick. Built on the kit's
`IWheelPiece` stub (now a real Wheel piece: angular velocity, drag, threshold, checkpoint).

**Kite into your room (kite field).** Pinch a kite (or close a fist near it): it lifts off the grass on its string.
Pull it up past ~15 cm above the island and let go: it catches the wind and flies **above the island, out in the room**,
30–50 cm over the island (scaled with it), in a lazy figure-eight with a little tail flutter, string drawn as a soft
curve down to the explorer's backpack. Let go lower: it drifts back down. Up to 3 kites; they fan out so they don't
overlap; they follow the explorer (scroll-proof). Look + pinch a kite = it rises and flies by itself. After the realm
is restored, kites fly from posts all over Galewick too.

**Fan the wind (windy ridge).** An open, flat hand (fingers extended, not pinching) sweeping over the island at > 0.6
m/s, within 30 cm above it, = a gust in the sweep direction with strength from speed. Every gust: a visible ripple
band runs across all the tall and short grass (grass shader gets a moving gust front: direction, front position,
width, strength), wind streak particles, mills twitch, loose leaves fly. The cloud bank on the trail is 3 big puffs:
each gust pushes the puffs it passes over away and thins them; gone = the path opens (saved). Look + pinch-hold a puff
= it blows away slowly. After restoration, gusts still ripple the grass anywhere in Galewick (a toy).

**Untangle the Keeper (tower hall, before the battle).** Galeowl sits wrapped in kite strings, 3 glowing knots on its
body. Pinch one knot's two loose ends, one hand each (each end shows a gold dot), and pull your hands apart > 12 cm:
the knot snaps with a twang and the strings fall away; one-handed works too (pinch one end, pull away > 15 cm) so it
never blocks. Look + pinch a knot = it loosens by itself after 2 s. All 3: Galeowl shakes free, flaps, the fury aura
flares, the battle starts.

## Keeper: Galeowl
Big round owl (2.5x an Isling), slate-blue and cream, feather tufts like kite tails, wind-vane crest. Frightened: grey
storm aura with string scraps whipping round, ruffled, wide red-gold eyes, trembling. Lv 11 at one shard (scales).
Moves: Gale Shot (wind), Talon Dive (neutral, every 3rd turn), **Gust Wall** shield (breaks to a Fire move or 3 hits,
2 open turns, comes back). Phase 2 below 50%. No capture. Calmed: soft grey-gold, eyes amber, bows its head.

## Islings in Galewick
Wind roster (sheets 15 / 16): **Kitelet** (→ Kitewyrm), **Whirlby** (→ Windwhirl), **Samarling** (→ Samaroak),
**Vanehare** (→ Compasshare), **Zeppelope** (rare, → Skyshiplope); Pufflet's evolution **Nimbulpaca**.
Tables: ~70% wind, ~20% fire visitors (Toastling, Coalbeetle), ~10% Steambara-style oddities skipped for now.
Zones: village fields, kite field, ridge grass, rare pocket.

## Levels across realms (region order is free)
Wild levels and Keepers scale with **shards held**: +3 wild levels and +2 Keeper levels per shard (tune with the sim).
Target: arriving at any Keeper with 1 shard ≈ the same win odds as Cinderwood with 0.

---

# Lilymere (water): lily lagoon (after Goa)

**Look:** a shallow lagoon with lily pads and lotus, stilt huts, little bridges, a frozen waterfall from a cliff,
a half-sunk lake temple. Unrestored: water still and glassy and half-dry (pale sand banks showing), the waterfall
frozen mid-fall, lotus closed. Restored: water ripples and flows, the waterfall runs, lagoon full, lotus open, mist.

| Order | Place | Hand moment | Result |
|---|---|---|---|
| 1 | Lagoon edge | **Lily pad bridge** | Nudge drifting pads with a fingertip; they click together into a path across a channel |
| 2 | Lotus garden (dry bed) | **Rain hand** | Palm down over the island + wiggle fingers: a tiny cloud forms under the hand and rains; buds bloom into big lotus stepping stones where it falls |
| 3 | Waterfall basin (by the temple) | **Scoop and pour** | Cup a hand in the lagoon, a wobbly water ball sits in the palm, pour it into the dry basin: the frozen waterfall cracks and runs, the lagoon rises and lifts the sunken temple's door above water |
| 4 | Lake temple | — | Tidemother (giant turtle, lily garden on its shell); shield breaks to a Wind move |
Roster: Marimoss (→ Marimound), Drizzcan (→ Rainurn), Lilyvole (→ Lotusvole), Sailfold (→ Galleyfold), Bottlewhale
(rare, → Lighthousewhale); Pebbloop → Riverloom. Steambara/Onsenbara already exist.

---

# Cinderwood upgrade (decided Oct 27)
Cinderwood felt undercooked next to Galewick. The log and the bellows stay; three moments are added (step 10f):
| Where | Hand moment | World answers |
|---|---|---|
| Kiln village shrine (route) | **Cup the flame**: both hands cupped round the dying village flame for 2 s | All kilns glow and smoke, hut windows and lantern posts light outward, plaza creatures cheer; the shrine starts working |
| Smoky hollow (new, route, village → springs) | **Light the lantern trail**: touch two lanterns in a row and fire races down the line | 6 lanterns catch in a chain, spark trail, the dark smoke burns off the path |
| Forge temple (before the bellows) | **Ember in your palm**: an ember from a vent sits glowing on your open palm; turn the hand over above the firebox | Firebox roars, chimney smokes, every vent along the cliff glows; then the bellows work |

---

## Cuts and notes
- **Golden Berry cut for good** (Oct 26). Berry bushes stay as scenery only, no gameplay.
- Cinderwood revisited Oct 27 (section above).
- Quick Journey content is decided after the realms exist (Mugesh).
