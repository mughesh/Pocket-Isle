using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Switches "Initialize XR on Startup" for the Windows (Editor Play mode) target.
    /// Off: Play runs flat (keyboard fallback). On: Play starts OpenXR, for Quest Link.
    /// With it on, Meta's PC runtime blocks Unity's main thread inside OpenXR startup until a headset is
    /// in Link mode, so only turn it on with Link already active. Android builds are not affected.
    /// </summary>
    static class PlayModeXrToggle
    {
        const string MenuPath = "Tools/Diorama/Play Mode XR (Quest Link)";

        static XRGeneralSettings StandaloneSettings =>
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            var s = StandaloneSettings;
            if (s == null) { Debug.LogWarning("[Diorama] No XR settings for Standalone."); return; }
            s.InitManagerOnStart = !s.InitManagerOnStart;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log(s.InitManagerOnStart
                ? "[Diorama] Play Mode XR ON: put the headset in Link mode before pressing Play."
                : "[Diorama] Play Mode XR OFF: Play runs flat with keyboard controls.");
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            var s = StandaloneSettings;
            Menu.SetChecked(MenuPath, s != null && s.InitManagerOnStart);
            return s != null && !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
