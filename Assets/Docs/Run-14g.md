# Step 14g report: menus back to the old size, realm card fixes

Headset test pending. Tests 270/270 (14f's text-size tests went with the rule). No build.
Commits: `423057e` (C realm cards), `f0ece22` (A, B, D menus and sizes).

## What changed

| | Change |
|---|---|
| Sizes (D) | Every board, card, bubble and HUD label is back to its pre-14f size, spacing and layout. The stone tablets stay, lighter. Labels only shrink to fit (down to 60%); boards never grow for text. |
| Game menu (A) | One row over the near rim: Resume (larger, gold inlay) · Controls · Settings · Title · speaker (small square). Dev is QA-only, small, on the left. |
| Tablets (A2) | Slab 6.5 → 3.5 mm thick. Bevel 1.8 → 1 mm. Corner 7 → 4.5 mm. Moss and halo about half. |
| Quiet menus (A3, B6) | While the menu or Settings is open, the line showing goes back to the front of the queue and idle hints wait. Everything shows after close (`Dialogue.Held`). |
| Settings (B) | Old board size. Selected options and lit volume pebbles are filled gold with a dark label: the level reads at a glance, and "off" fills gold when chosen. "Touch tiles to walk" → "Touch to walk". |
| Realm cards (C) | Line at about 55% of the name's size, right under the plaque. Plaques padded and rebaked. Lilymere: "Where the water ran dry" / "Where the water flows again". Galewick (stuck clouds) and Cinderwood (forge fire out) kept. |
| "Lilyme" (C10) | Not a short text entry. The line is "Lilymere... the lagoon used to reach right up to here." Typing runs on game time, and the game pauses (timeScale 0) when the headset loses focus (a capture, the system menu), so it froze mid-word. The bubble now types whole words. |

## Sizes before / after (board-local, island scale 1)

| Element | 14f | 14g (= pre-14f) |
|---|---|---|
| Menu rune | auto (~0.19 x 0.062), 2 rows | 0.072 x 0.034 (Resume 0.08 x 0.038), 1 row |
| Settings board | ~0.66 x 0.62 | 0.34 x 0.32 |
| Label font | 0.34 (14f) / 0.22 (337f5de) | 0.11 x tablet height (shrinks to fit) |
| Realm card line | font 0.22-0.27, far under the card | 55% of the name's caps, under the plaque |

Shots: `Logs/BattleShots/14f_buttons_<bright|dark>_g_menu.png`, `..._g_settings.png`, `14g_card_<realm>.png`, `14c_cards.png`.

## Headset checks

1. The menu is a single small row; the island stays visible. Resume is gold. The speaker toggles music.
2. With the menu open, the Spirit says nothing. After closing, the waiting line shows.
3. Settings at the old size. The volume level shows as gold pebbles, the chosen Hand / Hints / Touch option gold. No hint bubble over it.
4. Enter each realm: the line sits right under the plaque, about half the name's size; nothing cut off.
5. Typing never shows half a word, even if you open the system menu mid-line.

## Readability to watch (not scaled up, per the brief)

The pre-14f sizes are back, so the small items Mugesh first flagged are small again:
- settings row names;
- battle rune element lines;
- pod-card info lines.

Shorten text rather than grow the boards if any reads poorly.

Open: the slanted-word glitch from 14f is still not chased. "Credits looked disabled" in the Nov 4 shot is probably the passthrough hand in front of it: nothing in code dims it.
