# Step 26: the release build and the "Competition" channel

**Run after step 25 is merged to master. Mugesh is present** (he types the keystore password and does the dashboard).
This replaces `step-15a-release-setup.md`, which is out of date.

Why now: an APK alone doesn't count. The entry is a release-signed build in a channel named exactly `Competition`, and
that path has never been proven end to end. It doesn't need the audio to be final: a later build with a higher version
code replaces this one any time before Nov 18.

## How to run
- Orient (`CLAUDE.md`, `.claude-handoff.md` newest on top, `Run-25.md`, git log). Branch **`step-26`** from master.
- Stage by path, never `git add -A`. Never commit keystores, passwords, API keys, app secrets, `Assets/_Recovery/`,
  `_lanes/`, or the TMP fallback asset. Commit per section. No AI attribution lines. **Don't push** (Mugesh pushes).
- **No game code or content changes.** Feature freeze. If something outside the release path is wrong, report it.
- Passwords and the app secret are typed by Mugesh only. Never put them in a command, file, log or this chat.
- Ask before uninstalling anything from the headset. Never overwrite `Builds/PocketIsle-QA-oct27-tester.apk`.

## What's already done (checked Nov 9 on `Builds/PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk`, built Nov 5)
| Item | State |
|---|---|
| Keystore | `D:/UNITY/keystores/pocketisle-release.keystore`, alias `pocketisle`, outside the repo |
| Signer | CN=Mugesh, O=Pocket Isle, SHA-256 `9384326c6a8deff5270b135d3952fcdbc04bbf519d54b6f234f7e800c47ce7ef` |
| Permissions | RECORD_AUDIO, MODIFY_AUDIO_SETTINGS, BLUETOOTH stripped by `ReleaseManifestHook` |
| Version | 1.0.0, code 11, bumped by `ReleaseBuild.Build` |
| Target | min SDK 32, target 34, arm64-v8a only |

**Don't redo these.** That APK has old audio and must not be uploaded. Tools are in
`D:/UNITY/6000.3.25f1/Editor/Data/PlaybackEngines/AndroidPlayer/` (`SDK/build-tools/*`, set `JAVA_HOME` to `OpenJDK`).
`Assets/Docs/Release-Howto.md` was never written, though the build's error message points to it (section F).

## A. Pre-flight (read-only)
1. Step 25 is on master; Release Check passes with 0 free clips.
2. `POCKET_DEV` is not in the Android defines. `POCKETISLE_ALLOW_FREE_FOR_TEST` is not set: if in doubt, run
   Tools > Pocket Isle > Release > Clear Test Flag. A build with that flag is named TEST and must not be uploaded.
3. Against current Meta docs (metavr docs, `hz-store-submit` skill), check the store's minimum target SDK and manifest
   rules. Report only what differs from the table above. Don't rely on memory for this.

## B. Keystore safety (losing it means this app can never be updated)
4. Print `sha256sum` of the keystore.
5. Mugesh copies it to **two places outside this PC's D: drive** (cloud drive + USB) and confirms. Compare hashes for any
   copy reachable from this PC.
6. Mugesh proves he has the password: he runs `keytool -list -v -keystore <path> -alias pocketisle` in his own terminal
   and types it at the prompt. The certificate SHA-256 must match the table. He stores it in his password manager.

## C. Build (Mugesh clicks)
7. Save scenes first (the save-scenes dialog blocks a build). Not in Play mode.
8. Mugesh runs Tools > Pocket Isle > **Build Release APK** and types the password in the window.
9. Output is `Builds/PocketIsle-Release-1.0.0-<code>.apk`, code above 11. Commit only the version lines of
   `ProjectSettings/ProjectSettings.asset` if that's all that changed; `androidUseCustomKeystore` must be back to 0.

## D. Verify the APK (aapt, apksigner)
10. Report each as pass/fail with the real value:
    - Package `com.mugesh.pocketisle`, version 1.0.0, code above 11, target SDK per step 3, arm64-v8a only, size.
    - Signer SHA-256 equals the table (not the Android debug cert).
    - Permissions are exactly INTERNET, ACCESS_NETWORK_STATE, HAND_TRACKING, USE_ANCHOR_API, USE_SCENE,
      BOUNDARY_VISIBILITY and the app's own DYNAMIC_RECEIVER one. Say what adds INTERNET and ACCESS_NETWORK_STATE.
    - `oculus.software.handtracking` declared with required=false (controllers optional, hands enough).
    - The dev panel is compiled out: its main class name isn't in `assets/bin/Data/Managed/Metadata/global-metadata.dat`.

## E. Headset and upload
**Trap: the headset has a debug-signed QA build. A release-signed APK can't install over it (`adb install -r` fails with
INSTALL_FAILED_UPDATE_INCOMPATIBLE). The fix is an uninstall, which wipes the save.**
11. Back up the save first: `adb pull` the app's `/sdcard/Android/data/com.mugesh.pocketisle/files/` to a dated folder
    outside the repo. Then ask Mugesh before uninstalling.
12. Mugesh, on the Meta Developer Dashboard: create the app if needed, then a **new release channel named exactly
    `Competition`**, and add himself as a tester.
13. Upload with **Meta Quest Developer Hub's upload screen** (recommended: the app secret stays out of shell history).
    Report the result; on a rejection, show the exact error and fix only what's on our side.
14. Install **from the channel**, the way a judge gets it. Mugesh checks, in order:
    1. Fresh launch: note every system dialog in the first minute (a permission popup is something judges will see).
    2. Shard, title, one lesson, hands only.
    3. Sound plays (MODIFY_AUDIO_SETTINGS was removed).
    4. Credits say the paid-plan line, no dev panel anywhere.
    5. Pause via headset menu and resume; quit and Continue.
    6. Wi-Fi off, launch again: the game still works.

## F. Report
15. `Assets/Docs/Release-Howto.md`: keystore location (no password), backup places (general, e.g. "cloud + USB"), build
    menu, how the version code works, the upload steps, the debug/release install trap, and "no updates during judging
    (Nov 18 – ~Dec 11)".
16. Re-run Tools > Pocket Isle > Release > Readiness Report (the committed one is from Nov 5).
17. Commit the leftover doc `.meta` files outside Prompts (`Audio/Regen-Report.md.meta`, `Run-23.md.meta`,
    `Run-24.md.meta`) and `Assets/Docs/Pocket-Isle-Handoff-Nov9.md`.
18. `Assets/Docs/Run-26.md`: done/partly/not, the section D table, the upload result, and the final build recipe.
    Add a `.claude-handoff.md` entry. If all pass, fast-forward master locally.
