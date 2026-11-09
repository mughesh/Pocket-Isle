# Step 25: fix-ups after the paid audio (design chat cross-check, Nov 9 1 PM)

The design chat checked Run-24, Regen-Report, the paid sidecars (290, all `eleven_text_to_sound_v2`, dated Nov 9,
plan paid) and measured a sample of the new clips. Most of it is right (Release Check passes, licence trail exists,
the acoustic jingles are warmer than the synth ones, the step 24 A shots look good). These need fixing.

## How to run
- Orient (`CLAUDE.md`, `.claude-handoff.md`, `Run-24.md`, `Audio/Regen-Report.md`). Branch **`step-25`** from master.
  Usual rules (stage by path; no keys/keystores/`_Recovery`/`_lanes`; commit per section "(headset test pending)";
  no AI attribution; tests + smoke run + Release Check). **Don't push** (Mugesh pushes). If all pass, fast-forward
  master locally.
- `ELEVENLABS_API_KEY` is set; ~25k credits are left. **Cap for this step: 2,000 credits.**

## A. The in-app credits still say "free plan"
1. `CreditsText.PaidSoundsDone` is still `false`, so the lanterns and the Settings card say "Sounds made with
   elevenlabs.io (free plan)". Set it to `true` (or derive it from Release Check), check both places show the
   after-regen lines, update the test.

## B. The new footsteps are the "velcro" again
Measured: paid `step.grass_21_s1` / `_22_s5` are 0.25 s with almost all their energy above 2.5 kHz (RMS −15 total,
−15.3 above 2.5 kHz) and ~5 dB louder than the Kenney steps they play beside (−21 RMS, −45 above 2.5 kHz). Picking by
measurement missed brightness.
2. For every paid `step.*` slice: low-pass ~2.2 kHz, trim to the first ~0.12–0.15 s with a 20 ms fade, level-match
   to the Kenney takes in the same cue (RMS and brightness). Re-measure; any slice still more than 10 dB brighter
   (above 2.5 kHz relative to total) than the Kenney average is dropped from the cue. If fewer than 4 paid slices
   survive in a cue, the cue plays Kenney only. Report before/after numbers per cue.

## C. The Still Isle bed pulses like surf
Measured: `amb.stillisle_22` (the only take that passed) swings between about −46 and −14 dB RMS from second to
second and is almost all hiss above 2.5 kHz: exactly the "waves on a beach" Gemini heard, and it's the first sound of
the game.
3. Generate 4 new takes (30 s, loop): "very quiet steady air tone in a sheltered garden clearing, soft continuous
   breeze, distant gentle songbirds, calm, even volume, no gusts, no waves, no surf, no water, no music, no voices".
   Pick by: smallest second-to-second RMS swing (aim under 8 dB), least energy above 2.5 kHz, no gaps. Low-pass
   ~4 kHz after levelling if still bright. If none passes, fall back to the synth `amb.stillisle` with a ~1.2 kHz
   low-pass (credits-free). Report the swing and brightness of the chosen bed vs `_22`.
4. Same swing/brightness check on every other paid ambience bed (`amb.*`, `restore.breeze`); list the numbers and fix
   any bed swinging more than ~12 dB the same way (lodge pops are allowed to be sparse: judge its bed, not the pops).

## D. Tidy
5. `Clips/Gen/restore.swell/*_FREE.wav` and `Clips/Levelled/Gen/restore.swell/*_FREE.wav` are still outside the
   archive (no cue reads them): move them into `_free_archive`. Make Release Check also fail on any `_FREE` file
   outside the archive, not only on cue references.
6. `elevenlabs-paid/ledger.csv` sums to 1,428 credits while the report says 14,279 (the balance difference): fix the
   ledger column (seconds vs credits?) so the licence trail adds up, and note the invoice date in `AudioCredits.md`.
7. Commit the untracked prompt files in `Assets/Docs/Prompts/` (step 24 notes + images, run files).

## E. Report
`Assets/Docs/Run-25.md`: A–D results with the numbers, and the **listening list** for Mugesh's device test merged
with Run-24's (first sounds first).
