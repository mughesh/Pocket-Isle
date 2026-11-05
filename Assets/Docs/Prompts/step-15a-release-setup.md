# Step 15a: release setup (keystore, icon, permissions, Release APK, upload to "Competition")

Mugesh is present (he must type the keystore password and do the dashboard part). Orient first if you're a fresh
session (`CLAUDE.md`, `.claude-handoff.md`, `Overnight-Nov5.md`, `Assets/Docs/Release-Readiness.md`). No AI
attribution, don't push unless Mugesh says "push".

## 0. Branch
1. If `overnight-nov5` isn't merged yet and Mugesh says his test passed: `git checkout master`, merge it, push
   (he approves the push for this). Work on master from here.

## A. Keystore (production signing; Meta rejects debug-signed APKs)
2. Create a keystore **outside the repo**: `D:\UNITY\keystores\pocketisle-release.keystore`, alias `pocketisle`,
   RSA 2048, validity 30 years, using the JDK that ships with Unity (`keytool`). **Ask Mugesh to type the passwords
   himself in the terminal** (never write them in a file, a commit, a log or a prompt). Then ask him to copy the
   keystore to a second safe place (cloud drive / USB) and to store the password in his password manager. Explain in
   one line why: every future upload of this app must be signed with this same key.
3. Point Unity at it (Player Settings > Publishing Settings: custom keystore, path, alias) **through Unity's settings
   objects** (not by editing ProjectSettings.asset on disk). Passwords: Unity doesn't keep them between sessions; make
   the release build script read them from environment variables `POCKETISLE_KS_PASS` / `POCKETISLE_KEY_PASS` if set,
   else prompt in the Editor. Never store them.

## B. Release build settings
4. **App icon:** run `Tools > Pocket Isle > Release > Assign App Icon` (overnight-nov5 made the PNGs).
5. **Permissions:** the QA APK asks for `RECORD_AUDIO`, `BLUETOOTH`, `INTERNET`. The game uses none of the mic /
   Bluetooth. Remove `RECORD_AUDIO` and `BLUETOOTH` from the **release** manifest (find the source: Voice SDK /
   Immersive Debugger / a plugin; disable that feature for release builds or use a manifest `tools:node="remove"`);
   keep INTERNET only if something needs it (say what). Hand tracking: `oculus.software.handtracking` declared,
   controllers optional. Verify the merged manifest of the built APK (`aapt dump badging` / `apkanalyzer`).
6. **Version:** version name `1.0.0`, version code **= a number that increases with every upload** (start at 10; the
   build script bumps it and prints it).
7. **Release build** (`Tools > Pocket Isle > Build Release APK`): no `POCKET_DEV`, no dev panel, IL2CPP ARM64.
   The `ReleaseBuildGuard` will refuse while `_FREE` clips remain. **For this first test upload only**, allow a
   one-off override flag `POCKETISLE_ALLOW_FREE_FOR_TEST=1` that builds anyway and names the file
   `PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk`; the guard stays on by default. (The real submission build comes
   after step 15b replaces the free clips.)
8. Verify the APK: signed with the release key (`apksigner verify --print-certs`, not the Android debug cert),
   package `com.mugesh.pocketisle`, target SDK 34, ARM64 only, size. Install it on the headset (`adb install -r`)
   and ask Mugesh for a 2-minute smoke test (launch, title, Continue, menu, quit).

## C. Upload to the "Competition" release channel (Mugesh + you)
9. Mugesh, on the Meta Developer Dashboard: create the app (Pocket Isle, Meta Quest) if not done, create a **new
   release channel named exactly `Competition`**, note the **App ID** (and, for the CLI, an **App Secret / user token**
   from the dashboard's API page). Ask him for the App ID only; the secret he types into the terminal himself.
10. Upload with Meta's `ovr-platform-util` (download it if missing, with his OK) or Meta Quest Developer Hub's
    upload screen (his choice):
    `ovr-platform-util upload-quest-build --app-id <APP_ID> --app-secret <SECRET> --apk <path> --channel Competition --notes "Test upload"`
    Report the result. If the dashboard rejects it, show the exact error and fix what's on our side.
11. Write `Assets/Docs/Release-Howto.md`: keystore location (not the password), how to bump the version, the build
    menu, the upload command with placeholders, and the rule "no updates during judging (Nov 18 – Dec 11)".

Commit (no secrets anywhere): "Release setup: keystore (outside the repo), icon, release permissions, version, upload howto".
