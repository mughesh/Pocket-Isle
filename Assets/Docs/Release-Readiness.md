# Release readiness (read-only report)

Made by Tools > Pocket Isle > Release > Readiness Report on 2026-11-13 02:19. Nothing was changed.

| Check | Value | OK? |
|---|---|---|
| Package name | com.mugesh.pocketisle | yes |
| Version name | 1.0.0 | yes |
| Version code | 12 | yes |
| Min SDK | AndroidApiLevel32 | yes |
| Target SDK | AndroidApiLevel34 | yes |
| Architectures | ARM64 | yes |
| Scripting backend | IL2CPP | yes |
| Android defines | OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING;USE_INPUT_SYSTEM_POSE_CONTROL;USE_STICK_CONTROL_THUMBSTICKS | yes |
| POCKET_DEV in the release build | Build Release APK passes no extra defines; Build QA APK adds POCKET_DEV for that build only | yes |
| Keystore | pocketisle-release.keystore alias pocketisle (used by Build Release APK) | yes |
| App name | Pocket isle | yes |
| Company | Mugesh | yes |
| App icon | set | yes |
| Unity splash | off | - |
| Hand tracking (Meta config) | controllers and hands | yes |
| Hand tracking frequency | high | - |
| Passthrough support | required | - |
| Scene support | required | - |
| Anchor support | on | - |
| System splash (Meta) | none (black) | - |

## Built APKs (Builds/)

- `PocketIsle-Release-1.0.0-12.apk`: 121.5 MB, Nov 12 15:53
- `PocketIsle-QA.apk`: 121.5 MB, Nov 12 13:36
- `PocketIsle-QA-nov10.apk`: 120.6 MB, Nov 12 13:29
- `PocketIsle-QA-nov6-tester.apk`: 110.8 MB, Nov 6 16:18
- `PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk`: 110.6 MB, Nov 5 18:29
- `PocketIsle-QA-oct27-tester.apk`: 101.3 MB, Oct 27 16:30
- `PocketIsle.apk`: 125.1 MB, Oct 25 23:06

Manifest of `PocketIsle-Release-1.0.0-12.apk` (the newest; a new build regenerates it):

- `package: name='com.mugesh.pocketisle' versionCode='12' versionName='1.0.0' platformBuildVersionName='14' platformBuildVersionCode='34' compileSdkVersion='34' compileSdkVersionCodename='14'`
- `sdkVersion:'32'`
- `targetSdkVersion:'34'`
- `application-label:'Pocket isle'`
- `native-code: 'arm64-v8a'`
- permission `android.permission.INTERNET`
- permission `android.permission.ACCESS_NETWORK_STATE`
- permission `com.oculus.permission.HAND_TRACKING`
- permission `com.oculus.permission.USE_ANCHOR_API`
- permission `com.oculus.permission.USE_SCENE`
- permission `com.oculus.permission.BOUNDARY_VISIBILITY`
- permission `com.mugesh.pocketisle.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`

Build size estimate: the newest release-style APK above (no APK built tonight). The QA build carries the dev panel and is a little larger.

## Audio

- **0 free-plan clips in 0 cues** (no commercial licence; the release build refuses them). Regenerate on a paid plan: `Assets/Docs/Audio/Regen-List.md` lists each cue, its kept variants, the prompt and the length.
- Music cues: `music.battle.keeper`: no clip yet (awaiting); `music.battle.wild`: music.battle.wild_1 (129 s); `music.cinderwood`: music.cinderwood_1 (119 s); `music.finale`: music.finale_1 (75 s); `music.galewick`: music.galewick_1 (120 s); `music.hub`: music.hub_1 (43 s); `music.lilymere`: music.lilymere_1 (120 s); `music.lodge`: music.lodge_1 (27 s); `music.temple`: music.temple_1 (123 s); `music.title`: music.title_1 (43 s).
- Music sources: `D:\UNITY\projects\_audio-sources\music\plan.txt` (music.hub_1.mp3, Gemini app (Lyria 3.5), licence to check before submission).

## To do before the store build

Nothing.
