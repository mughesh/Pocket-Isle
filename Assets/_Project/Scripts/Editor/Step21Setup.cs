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

        /// <summary>
        /// The swipe ghost (Mugesh, 4th time: tilted): the hand's measured fingers straight away from the player (a little
        /// up), the back of the hand up and out (palm in and down, never toward the face); no Thumb Yaw on top (it was 55,
        /// a correction stacked on a lean in Wrist Fingers).
        /// </summary>
        [MenuItem("Tools/Pocket Isle/Setup/Step 21 swipe ghost")]
        public static void SwipeGhost()
        {
            if (Application.isPlaying) return;
            var g = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            if (g == null)
            {
                Debug.LogError("[Step21Setup] No GhostHands in the open scene.");
                return;
            }
            var so = new SerializedObject(g);
            so.FindProperty("measuredWrist").boolValue = true;
            so.FindProperty("wristFingers").vector3Value = new Vector3(0f, 0.2f, 1f);
            so.FindProperty("wristBack").vector3Value = new Vector3(0.75f, 0.6f, -0.15f);
            so.FindProperty("thumbYaw").floatValue = 0f;
            so.FindProperty("thumbRoll").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
            EditorSceneManager.SaveScene(g.gameObject.scene);
            Debug.Log($"[Step21Setup] Swipe ghost: measured wrist, fingers {g.wristFingers}, back {g.wristBack}, yaw {g.thumbYaw}; scene saved.");
        }
    }
}
