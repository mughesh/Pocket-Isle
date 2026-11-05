# Step 15b: regenerate the free-plan sounds on a paid ElevenLabs plan

Prereq (Mugesh): an ElevenLabs **paid** plan for this month (Starter is enough), and the API key of that account in
`ELEVENLABS_API_KEY` (never print, log or write it). Orient first if you're a fresh session. Start Claude Code with
the staging folder allowed (`--add-dir "D:\UNITY\projects\_audio-sources"`). No AI attribution, don't push.

1. **Confirm the plan** via the API (subscription endpoint): it must report a paid tier. If it's free, stop and say so.
   Check the credit balance. The list is `Assets/Docs/Audio/Regen-List.md`: **237 kept clips in 79 cues, ~474 s**.
   At ~20 credits per second that's ~9.5k credits plus retries; budget **20k**, stop and ask past that.
2. Regenerate **only the kept clips**, each with **its original prompt, duration and loop flag** (from the list /
   sidecars), using the existing gen lane tools in `D:\UNITY\projects\_audio-sources\elevenlabs\` (`gen.py`, scope,
   level copies at −18 dB mid-band, peak ≤ −1 dBFS). Sidecars and manifest: `"plan": "paid"`. Batch-and-slice where the
   original was sliced. Read the contact sheets; regenerate anything clearly worse than the free clip it replaces (max
   2 retries), keep a note.
3. Import with **Import Gen Batch**: each paid clip **replaces** its `_FREE` twin in the same cue (same variant slot,
   same volume/tier), no `_FREE` suffix. Remove the `_FREE` clips from the cues; move the free files out of the build
   (e.g. `Assets/_Project/Audio/Clips/Gen/_free_archive/` excluded from the build, or delete only with Mugesh's OK).
4. `Tools > Pocket Isle > Audio > Release Check` must pass (0 free clips); `ReleaseBuildGuard` builds without the
   override. Update `AudioCredits.md` ("Generated with ElevenLabs Sound Effects, paid plan (commercial licence)",
   month and plan), and list anything still synth or Kenney (CC0).
5. Flat bridge listen-pass is impossible; instead run AudioScope on every replaced cue and report any cue whose level
   or length changed a lot vs the free version (Mugesh listens to those first). Write `Assets/Docs/Audio/Regen-Report.md`.
