# Release readiness (read-only report)

Made by Tools > Pocket Isle > Release > Readiness Report on 2026-11-05 00:54. Nothing was changed.

| Check | Value | OK? |
|---|---|---|
| Package name | com.mugesh.pocketisle | yes |
| Version name | 0.1.0 | yes |
| Version code | 1 | yes |
| Min SDK | AndroidApiLevel32 | yes |
| Target SDK | AndroidApiLevel34 | yes |
| Architectures | ARM64 | yes |
| Scripting backend | IL2CPP | yes |
| Android defines | OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING;USE_INPUT_SYSTEM_POSE_CONTROL;USE_STICK_CONTROL_THUMBSTICKS | yes |
| POCKET_DEV in the release build | Build Release APK passes no extra defines; Build QA APK adds POCKET_DEV for that build only | yes |
| Keystore | not set (debug key) | **no** |
| App name | Pocket isle | yes |
| Company | Mugesh | yes |
| App icon | none | **no** |
| Unity splash | off | - |
| Hand tracking (Meta config) | controllers and hands | yes |
| Hand tracking frequency | high | - |
| Passthrough support | required | - |
| Scene support | required | - |
| Anchor support | on | - |
| System splash (Meta) | none (black) | - |

## Built APKs (Builds/)

- `PocketIsle-QA.apk`: 109.3 MB, Oct 28 11:18
- `PocketIsle-QA-oct27-tester.apk`: 101.3 MB, Oct 27 16:30
- `PocketIsle.apk`: 125.1 MB, Oct 25 23:06

Manifest of `PocketIsle-QA.apk` (the newest; a new build regenerates it):

- `package: name='com.mugesh.pocketisle' versionCode='1' versionName='0.1.0' platformBuildVersionName='14' platformBuildVersionCode='34' compileSdkVersion='34' compileSdkVersionCodename='14'`
- `sdkVersion:'32'`
- `targetSdkVersion:'34'`
- `application-label:'Pocket isle'`
- `native-code: 'arm64-v8a'`
- permission `android.permission.INTERNET`
- permission `android.permission.ACCESS_NETWORK_STATE`
- permission `android.permission.RECORD_AUDIO`
- permission `android.permission.MODIFY_AUDIO_SETTINGS`
- permission `android.permission.BLUETOOTH`
- permission `com.oculus.permission.HAND_TRACKING`
- permission `com.oculus.permission.USE_ANCHOR_API`
- permission `com.oculus.permission.USE_SCENE`
- permission `com.oculus.permission.BOUNDARY_VISIBILITY`
- permission `com.mugesh.pocketisle.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION`

Build size estimate: the newest release-style APK above (no APK built tonight). The QA build carries the dev panel and is a little larger.

## Audio

- **237 free-plan clips in 79 cues** (no commercial licence; the release build refuses them). Regenerate on a paid plan: `Assets/Docs/Audio/Regen-List.md` lists each cue, its kept variants, the prompt and the length.
- Music cues: `music.battle.keeper`: no clip yet (awaiting); `music.battle.wild`: no clip yet (awaiting); `music.cinderwood`: no clip yet (awaiting); `music.finale`: no clip yet (awaiting); `music.galewick`: no clip yet (awaiting); `music.hub`: no clip yet (awaiting); `music.lilymere`: no clip yet (awaiting); `music.lodge`: no clip yet (awaiting); `music.title`: no clip yet (awaiting).
- Music sources: `D:\UNITY\projects\_audio-sources\music\plan.txt` (music.hub_1.mp3, Gemini app (Lyria 3.5), licence to check before submission).

## To do before the store build

1. Keystore: Publishing Settings > Keystore Manager: create one, keep it and its passwords OUT of git (back it up; losing it = no updates)
1. App icon: Player Settings > Icon: Default Icon = Assets/_Project/Art/Icon/PocketIsle_Icon_1024.png (see Overnight-Nov5.md)
1. Audio: 237 free-plan clips in 79 cues: regenerate on a paid plan (Assets/Docs/Audio/Regen-List.md), re-import, Sync Cues.
