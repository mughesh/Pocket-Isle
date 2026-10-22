# Build plan

Revised Oct 22: island floats (no table), world is a big map seen through a scrolling island, full game with a story.

Goal before Goa (Oct 28): on Quest, place the floating island anywhere, walk the explorer across a map bigger
than the island, and have Emberpup jump out of tall grass. Capture and battle come after Goa.

| Step | Target date | What you see at the end |
|---|---|---|
| 1. Toon look in Unity | done Oct 21 | Island, wavy grass, trees and rocks, toon shaded, in the Editor |
| 2. On the headset | done Oct 22 | APK on Quest, passthrough, thumb swipes move a test cube |
| 2b. Floating island + continuous walk | Oct 22–23 | Island spawns in front; grab rim to move; two hands to turn/resize; tilts toward you on release; shadow on real surface; hold-to-keep-walking |
| 3. Creature baker | Oct 23–25 | Editor tool sculpts Emberpup into a mesh asset; it idles on the island |
| 4. Map window + walk + encounter | Oct 25–27 | Test map ~4× the island; island scrolls as the explorer walks; tiles rise/sink at edges; tall grass rustles, Emberpup jumps out. Tagged build before Goa |
| Story (chat, no code) | Oct 24–27 | Premise, 3 regions, creature list, beats, ending |
| Goa | Oct 28 – Nov 2 | No work |
| 5. Capture | Nov 3 | Flick to throw the pod, catch, creature on the shelf |
| 6. Battle | Nov 3–6 | Gaze + pinch move runes, 4 attack templates with effects |
| 7. World + story + creatures | Nov 6–10 | 3 regions built, story beats, ~6 creatures |
| 8. Polish | Nov 10–13 | Onboarding, sound, save + spatial anchor, softened island art, 72 fps |
| 9. Ship | Nov 14–16 | Demo video, Devpost write-up, APK to the Competition channel |

Cut list if late (first to go first): 3rd region, creatures beyond 4, spatial-anchor persistence, surface snap,
smooth scrolling → flip-screen, battle shields, sound polish.
In the Unity project: Assets/Docs/BuildPlan.md and Assets/Docs/Prompts/step-NN-*.md.
