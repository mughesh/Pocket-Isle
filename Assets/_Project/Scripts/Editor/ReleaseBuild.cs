using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 15a: the store build (Tools > Pocket Isle > Build Release APK).
    /// <list type="bullet">
    /// <item>Signed with the release keystore outside the repo (<see cref="KeystorePath"/>, alias <see cref="Alias"/>).
    /// Passwords come from the environment (POCKETISLE_KS_PASS / POCKETISLE_KEY_PASS) or a password window; Unity keeps
    /// them for this Editor session only, they are never written anywhere.</item>
    /// <item>Version 1.0.0; the version code goes up by one every build (at least 10) and is logged.</item>
    /// <item>No POCKET_DEV (dev panel compiled out), IL2CPP ARM64; the audio release guard is armed (no free clips).
    /// POCKETISLE_ALLOW_FREE_FOR_TEST=1 builds anyway, only as Builds/PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk.</item>
    /// <item>The Meta SDK's Voice / mic code makes Unity add RECORD_AUDIO, MODIFY_AUDIO_SETTINGS and BLUETOOTH; the game
    /// uses none of them, so the release manifest removes them (<see cref="StripPermissions"/>).</item>
    /// </list>
    /// </summary>
    public static class ReleaseBuild
    {
        public const string KeystorePath = "D:/UNITY/keystores/pocketisle-release.keystore";
        public const string Alias = "pocketisle";
        public const string VersionName = "1.0.0";
        public const int FirstVersionCode = 10;
        public const string ApkDir = "Builds";
        public const string TestApk = "Builds/PocketIsle-Release-TEST-NOT-FOR-SUBMISSION.apk";

        /// <summary>Set only while the release build runs: the gradle hook strips the permissions then.</summary>
        internal const string ReleaseKey = "PocketIsle.ReleaseManifest";

        public static readonly string[] RemovedPermissions =
        {
            "android.permission.RECORD_AUDIO",
            "android.permission.MODIFY_AUDIO_SETTINGS",
            "android.permission.BLUETOOTH",
        };

        public static string ReleaseApk(int code) => $"{ApkDir}/PocketIsle-Release-{VersionName}-{code}.apk";

        /// <summary>Next version code: one above the current, never below <see cref="FirstVersionCode"/> (pure).</summary>
        public static int NextVersionCode(int current) => Math.Max(FirstVersionCode, current + 1);

        [MenuItem("Tools/Pocket Isle/Build Release APK", priority = 71)]
        public static void Menu()
        {
            if (!File.Exists(KeystorePath))
            {
                Debug.LogError($"[ReleaseBuild] No keystore at {KeystorePath}. Create it first (Assets/Docs/Release-Howto.md).");
                return;
            }
            string ks = Environment.GetEnvironmentVariable("POCKETISLE_KS_PASS");
            string key = Environment.GetEnvironmentVariable("POCKETISLE_KEY_PASS");
            if (!string.IsNullOrEmpty(ks)) Build(ks, string.IsNullOrEmpty(key) ? ks : key);
            else if (!string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) && PlayerSettings.Android.keystoreName == KeystorePath)
                Build(PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasPass);
            else PasswordWindow.Open();
        }

        /// <summary>
        /// Step 15a, first test upload only: sets POCKETISLE_ALLOW_FREE_FOR_TEST=1 in this Editor process (Unity can't easily
        /// be started with it from the Hub), then builds the TEST-NOT-FOR-SUBMISSION APK. Restart the Editor (or run
        /// Release: Clear Test Flag) to arm the guard again.
        /// </summary>
        [MenuItem("Tools/Pocket Isle/Release/Build Release APK (TEST, free clips allowed)", priority = 72)]
        public static void MenuTest()
        {
            Environment.SetEnvironmentVariable("POCKETISLE_ALLOW_FREE_FOR_TEST", "1");
            Debug.LogWarning("[ReleaseBuild] POCKETISLE_ALLOW_FREE_FOR_TEST=1 for this Editor session: the next release builds are TEST, not for submission.");
            Menu();
        }

        [MenuItem("Tools/Pocket Isle/Release/Clear Test Flag", priority = 73)]
        public static void ClearTest()
        {
            Environment.SetEnvironmentVariable("POCKETISLE_ALLOW_FREE_FOR_TEST", null);
            Debug.Log("[ReleaseBuild] Test flag cleared: Build Release APK refuses free clips again.");
        }

        internal static void Build(string storePass, string keyPass)
        {
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            string defines = PlayerSettings.GetScriptingDefineSymbols(android);
            if (defines.Split(';').Any(d => d.Trim() == "POCKET_DEV"))
            {
                Debug.LogError("[ReleaseBuild] POCKET_DEV is in the Android scripting defines; remove it (the QA build adds it per build).");
                return;
            }
            if (PlayerSettings.GetScriptingBackend(android) != ScriptingImplementation.IL2CPP || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                Debug.LogError("[ReleaseBuild] Needs IL2CPP and ARM64 only (Player Settings > Other Settings).");
                return;
            }
            bool test = Environment.GetEnvironmentVariable("POCKETISLE_ALLOW_FREE_FOR_TEST") == "1";

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            PlayerSettings.Android.keyaliasName = Alias;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasPass = keyPass;
            PlayerSettings.bundleVersion = VersionName;
            int code = NextVersionCode(PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.bundleVersionCode = code;
            AssetDatabase.SaveAssets();
            string path = test ? TestApk : ReleaseApk(code);
            Debug.Log($"[ReleaseBuild] Version {VersionName} (code {code}), signed with {KeystorePath} ({Alias}){(test ? ", TEST: free clips allowed, not for submission" : "")} -> {path}.");

            SessionState.SetBool(ReleaseKey, true);
            SessionState.SetBool(DeviceBuild.ReleaseGuardKey, !test);
            try
            {
                DeviceBuild.Build(false, path);
            }
            finally
            {
                SessionState.SetBool(ReleaseKey, false);
                SessionState.SetBool(DeviceBuild.ReleaseGuardKey, false);
                // Keystore path and alias stay in Publishing Settings; the custom keystore is switched on only for this
                // build so QA / perf builds keep the debug key and never ask for the password.
                PlayerSettings.Android.useCustomKeystore = false;
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Removes <see cref="RemovedPermissions"/> from a manifest (tools:node="remove" so no library adds them back).</summary>
        public static int StripPermissions(string manifestPath)
        {
            var doc = new XmlDocument();
            doc.Load(manifestPath);
            const string androidNs = "http://schemas.android.com/apk/res/android", toolsNs = "http://schemas.android.com/tools";
            var root = doc.DocumentElement;
            if (root == null) return 0;
            if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", toolsNs);
            int n = 0;
            foreach (string perm in RemovedPermissions)
            {
                bool marked = false;
                foreach (var el in root.SelectNodes("uses-permission").Cast<XmlElement>().ToList())
                {
                    if (el.GetAttribute("name", androidNs) != perm) continue;
                    if (el.GetAttribute("node", toolsNs) == "remove") marked = true;
                    else
                    {
                        root.RemoveChild(el);
                        n++;
                    }
                }
                if (marked) continue;
                var rm = doc.CreateElement("uses-permission");
                rm.SetAttribute("name", androidNs, perm);
                rm.SetAttribute("node", toolsNs, "remove");
                root.AppendChild(rm);
            }
            doc.Save(manifestPath);
            return n;
        }

        class PasswordWindow : EditorWindow
        {
            string _store = "", _key = "";
            bool _same = true;

            public static void Open()
            {
                var w = GetWindow<PasswordWindow>(true, "Release signing", true);
                w.minSize = w.maxSize = new Vector2(380, 150);
            }

            void OnGUI()
            {
                EditorGUILayout.LabelField($"Keystore: {KeystorePath}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Kept for this Editor session only, never saved.", EditorStyles.miniLabel);
                _store = EditorGUILayout.PasswordField("Keystore password", _store);
                _same = EditorGUILayout.Toggle("Key password is the same", _same);
                using (new EditorGUI.DisabledScope(_same)) _key = EditorGUILayout.PasswordField("Key password", _same ? _store : _key);
                GUILayout.Space(6);
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_store)))
                    if (GUILayout.Button("Build Release APK", GUILayout.Height(26)))
                    {
                        string s = _store, k = _same ? _store : _key;
                        _store = _key = "";
                        Close();
                        EditorApplication.delayCall += () => Build(s, k);
                    }
            }
        }
    }

    /// <summary>Step 15a: strips the unused permissions from the generated gradle project, release builds only.</summary>
    class ReleaseManifestHook : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!SessionState.GetBool(ReleaseBuild.ReleaseKey, false)) return;
            // path is unityLibrary; the launcher manifest is its sibling.
            foreach (string m in new[] { Path.Combine(path, "src/main/AndroidManifest.xml"), Path.Combine(path, "../launcher/src/main/AndroidManifest.xml") })
                if (File.Exists(m))
                    Debug.Log($"[ReleaseBuild] {Path.GetFullPath(m)}: removed {ReleaseBuild.StripPermissions(m)} permission line(s), added tools:node=\"remove\" for {string.Join(", ", ReleaseBuild.RemovedPermissions)}.");
        }
    }
}
