# Release how-to (Pocket Isle, Meta Horizon Store channel `Competition`)

## Keystore (losing it = the app can never be updated)
- File: `D:/UNITY/keystores/pocketisle-release.keystore`, alias `pocketisle`, **outside the repo**. Never commit it.
- Certificate: CN=Mugesh, O=Pocket Isle, SHA-256 `9384326c6a8deff5270b135d3952fcdbc04bbf519d54b6f234f7e800c47ce7ef`.
- Backups: a cloud drive and a USB stick (Nov 12). Password: in Mugesh's password manager only (never in a file, command or chat).
- Check the password and cert any time (type the password at the prompt):
  `"/d/UNITY/6000.3.25f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool" -list -v -keystore /d/UNITY/keystores/pocketisle-release.keystore -alias pocketisle`

## Build
1. Not in Play mode; File > Save (the save-scenes dialog blocks a build).
2. Tools > Pocket Isle > **Build Release APK**, type the password in the window (kept for this Editor session only).
3. Output: `Builds/PocketIsle-Release-1.0.0-<code>.apk`.
- **Version code** goes up by one on every release build (`ReleaseBuild.NextVersionCode`), written to
  `ProjectSettings/ProjectSettings.asset` (`AndroidBundleVersionCode`): commit that line. The store only accepts a higher code than the last upload.
- The build refuses: `POCKET_DEV` in the Android defines, not IL2CPP / ARM64, or any free-plan audio clip (Release Check).
  Tools > Pocket Isle > Release > Build Release APK (TEST...) allows free clips but names the APK TEST-NOT-FOR-SUBMISSION: never upload that.
- The release manifest hook (`ReleaseManifestHook`) removes RECORD_AUDIO, MODIFY_AUDIO_SETTINGS, BLUETOOTH and sets
  `excludeFromRecents="true"` on the launch activity (a Meta store manifest rule).

## Check the APK (optional, Section D of step 26)
`aapt dump badging <apk>` (package, versionCode, targetSdk 34, permissions, arm64-v8a) and
`apksigner verify --print-certs <apk>` (the SHA-256 above). Tools: `D:/UNITY/6000.3.25f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/`.

## Upload
1. Meta VR Developer Hub > App Distribution > Upload: pick the APK and the **`Competition`** channel (the app secret stays out of shell history).
2. Expected warning, not a blocker: "contains both 32-bit and 64-bit libraries": Meta SDK logging libraries (~1.6 MB in `assets/lib/armeabi-v7a`); the game's own code is 64-bit only.

## Install the way a judge does
1. Dashboard > Distribution > Release Channels > Competition > Users > **Email Invite Users** (the headset account), accept the email.
2. Headset: Library > Pocket Isle > Install. App Settings shows the channel and version.
3. For judges: Channel Settings > **Grant access to users by URL** gives an invite link; they join, then "Install on headset".

## Trap: debug vs release
A QA build (debug key) and a release build (release key) can't replace each other: `adb install -r` fails with
INSTALL_FAILED_UPDATE_INCOMPATIBLE. The fix is an uninstall, which **wipes the save**: first
`adb pull /sdcard/Android/data/com.mugesh.pocketisle/files/ <dated folder outside the repo>`.
adb lives in `/c/Program Files/Meta VR Developer Hub/resources/bin/`.

## Freeze
**No updates during judging (Nov 18 – ~Dec 11).** Last upload before Nov 18, 12:00 PM PT.
