# Run 26: the release build and the `Competition` channel (Nov 12–13)

Built from `panel-review`, not master (master was 32 commits behind); everything then fast-forwarded to master and pushed (Mugesh).

| Section | State |
|---|---|
| A. Pre-flight | Done. Release Check passed, no POCKET_DEV, test flag cleared. Meta docs (Oct 15): target SDK 34 required for apps made after Mar 1 2026, min 32 recommended. **Found: no `excludeFromRecents` on the launch activity** (a release manifest rule); the hook now sets it (`9a6654dc`). `supportedDevices` says `stanley` (the SDK's name for the glasses), docs say `vrglasses`: left as the SDK writes it, the upload took it. |
| B. Keystore | Done. File SHA-256 `a0c12b30…fe92ad`; copies on a cloud drive and USB (Mugesh); password checked, cert SHA-256 matches. |
| C. Build | Done. `Builds/PocketIsle-Release-1.0.0-12.apk`, 6.7 min; version line committed (`9cc4d83f`), custom keystore back off. |
| D. Verify | Done, all pass (table below). |
| E. Headset + upload | Done. Save pulled to `D:/UNITY/device-save-backup/pocketisle-20261112-1608/`, QA build uninstalled (OK'd), uploaded with the Hub to `Competition`, installed from the channel. Mugesh: works, FPS stable, no permission popup seen. |
| F. Report | This file, `Release-Howto.md`, Readiness Report re-run (0 free clips; Regen-List now empty). |

## Section D (`PocketIsle-Release-1.0.0-12.apk`)
| Check | Value | |
|---|---|---|
| Package / version | com.mugesh.pocketisle 1.0.0 (12) | pass |
| SDK | min 32, target 34, compile 34 | pass |
| ABI | arm64-v8a only (the game); Meta SDK logging libs also carry 32-bit copies in `assets/lib` (~1.6 MB) | pass |
| Size | 127 MB | - |
| Signer | CN=Mugesh, O=Pocket Isle, SHA-256 `9384326c…7ce7ef` (v2 scheme) | pass |
| Permissions | INTERNET, ACCESS_NETWORK_STATE (Unity's own unityLibrary manifest), HAND_TRACKING, USE_ANCHOR_API, USE_SCENE, BOUNDARY_VISIBILITY, DYNAMIC_RECEIVER | pass |
| Hand tracking | `oculus.software.handtracking` required=false | pass |
| excludeFromRecents | true | pass |
| Dev panel | its class shell stays (scene serialization), body compiled out, `Awake` destroys it; the menu's Dev rune is compiled out | pass |

## Upload result
Accepted, one warning: "contains both 32-bit (armeabi-v7a) and 64-bit libraries" (the Meta SDK logging libs above). Not a blocker.

## Open
- The readiness report still says the music (Gemini / Lyria) licence is "to check before submission".
- Next upload needs code 13+ (Build Release APK bumps it).

## Final build recipe
Not in Play, File > Save, Tools > Pocket Isle > Build Release APK, password in the window, upload with the Hub to `Competition`. Details in `Release-Howto.md`.
