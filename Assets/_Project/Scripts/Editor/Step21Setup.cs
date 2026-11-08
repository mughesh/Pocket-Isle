using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 21: scene values (the scene's serialized values win over code defaults).</summary>
    public static class Step21Setup
    {
        /// <summary>The credits lanterns from behind the island at step 19's size (Nov 8: 1.8 and 1.3 looked too big).</summary>
        [MenuItem("Tools/Pocket Isle/Setup/Step 21 lanterns")]
        public static void Lanterns()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[Step21Setup] Not in Play mode.");
                return;
            }
            var f = Object.FindFirstObjectByType<FinaleDirector>(FindObjectsInactive.Include);
            if (f == null)
            {
                Debug.LogError("[Step21Setup] No FinaleDirector in the open scene (open MR_Main).");
                return;
            }
            var so = new SerializedObject(f);
            // Nov 8 (Mugesh: still too big at 1.3, almost the island's size): step 19's size.
            so.FindProperty("lanternFarScale").floatValue = 1f;
            so.FindProperty("lanternBackHeight").floatValue = 0.3f;
            so.FindProperty("lanternBackDrop").floatValue = 0.16f;
            so.FindProperty("lanternBackOut").floatValue = 0.05f;
            // Nov 8 (Mugesh: ~2 s an Isling in the Hall of Fame): one after another.
            so.FindProperty("hallPopGap").floatValue = 0.15f;
            so.FindProperty("hallComeOut").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(f.gameObject.scene);
            EditorSceneManager.SaveScene(f.gameObject.scene);
            Debug.Log($"[Step21Setup] Lanterns: far scale {f.lanternFarScale}, back height {f.lanternBackHeight}, drop {f.lanternBackDrop}, out {f.lanternBackOut}; scene saved.");
        }
    }
}
