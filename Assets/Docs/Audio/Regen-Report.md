# Regen report (Nov 9 night)

**Not run. 0 generations, 0 credits, cost per call not measured.**

| Check | Result |
|---|---|
| `ELEVENLABS_API_KEY` set | yes |
| API reports a paid tier | **no: HTTP 401, the key lacks `user_read`** |
| Write access to `_audio-sources` | yes |

Fix: ElevenLabs > API keys > this key > User: Read. Then rerun `Assets/Docs/Prompts/run-nov9-night.md`.

## Still to do, in priority order (none started)
1. 15b regen (`Regen-List.md`). Release Check is failing on 235 free-plan clips in cues.
2. Footsteps + ambience beds (step 23 F13-14)
3. One-shots (F15)
4. Step 24 E: Keeper and Heart voices
5. Acoustic jingles (F16)
