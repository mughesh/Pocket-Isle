# Regen report (Nov 9)

**Done: Release Check passes, 0 free clips in cues.** Plan Starter, model `eleven_text_to_sound_v2`, 14,279 of 40,000 credits used (36%). **Measured cost: 10 credits per second**, the `character-cost` header and the balance agree (the docs page says 40). 291 calls, 1,428 s.

| Group | Takes | Credits |
|---|---|---|
| Measuring call | 1 | 5 |
| 15b regen | 139 | 4,268 |
| Footsteps | 12 | 720 |
| Ambience beds | 9 | 2,700 |
| One-shots | 16 | 312 |
| Step 24 E voices | 24 | 939 |
| Acoustic jingles | 24 | 432 |
| Retries and new prompts | 66 | 4,903 |

Nothing left undone. Imported: 532 clips. Archived: 235 free files.

**Nobody has heard these.** Takes were picked by measurement only (clipping, low-frequency share, level, gaps, slice count, against the free twin where there is one). Sheets and reports: `../_audio-sources/elevenlabs-paid/scope/`, `qc.md`, `picks.md`.

## Listen to these first
| Cue | Why |
|---|---|
| `amb.stillisle` | **One take passed of six.** Five were nearly silent. Take 22 still has gaps (65% below its peak's range) |
| `amb.lodge` | Takes 22 and 23: sparse pops with long quiet between them, as asked. The gap check is off for this cue |
| `amb.galewick.restored` | First prompt came back as pure rumble; rewritten around rustling grass, three passed |
| `restore.breeze` | Same: rewritten without "wind", four passed |
| `amb.cinderwood` take 101 | Still far more low end than the free take after 3 tries |
| `move.impact.wind` take 6, `move.slam` takes 1 and 203 | Still worse than free by the measures after 3 tries |
| `hit.weak` | **Paid takes dropped**, near silent after 3 tries each. The cue now plays only its synth takes |
| `voice.grain.heart` | One batch passed of six. Fewest grains |
| `voice.call.tidemother` | Two passed of six (rumble) |

Retries swapped "loud" for "moderate volume" in the prompt (clipping lesson from the free run); every take's real prompt is in its sidecar.

## Decisions to check
- **Licence:** I couldn't open ElevenLabs' help article on commercial use (403). Their API docs mark this model deprecated (removed March 1, 2027), not beta. The clips are already generated, so the removal doesn't affect them. **Worth a quick look at the licence page before submitting.**
- Keeper entrance calls are newly generated. The note said try the Keeper's cry first, but I can't judge that without ears, so the cries weren't tried.
- Steps keep their Kenney takes next to the new ones, as step 23 left them.
- `amb.stillisle`, `amb.galewick`, `amb.galewick.restored`, `restore.breeze` and `knot.pull` no longer play synth takes (one source family per cue).
- The in-game credits line is unchanged.
