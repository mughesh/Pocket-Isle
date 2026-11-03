using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14f (Nov 3 night, design chat): A the last walk lesson becomes "Swipe to walk, tap to stop" (lessons rewritten,
    /// the swipe + tap ghost built, the script reimported), the Heart's forehead crystal (baked, wired). Code-only parts
    /// need no setup: pinches on hand moments never walk, the untangle holds the explorer still. Safe to re-run.
    /// </summary>
    public static class Step14fSetup
    {
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14f menus and fixes)", priority = 1005)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14f] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14f] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (lesson.walk.far: Swipe to walk, tap to stop)");
            var clip = Step12aSetup.BuildSwipeTap();
            log.Append(NL + (clip != null ? $"  ghost {Step12aSetup.SwipeTapClip}: {clip.frames.Count} frames" : $"  ghost {Step12aSetup.SwipeTapClip}: MISSING (no recorded swipe / tap clip)"));
            var lessons = Step8bSetup.WriteLessons();
            log.Append(NL + $"  lessons rewritten ({lessons.lessons.Count}): far = swipe + tap, done on a tap stop, the gold tile or two swipe walks, passes after 30 s");
            Crystal(log);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void Crystal(StringBuilder log)
        {
            var recipe = PropKit.WriteHeartCrystal();
            var heart = Object.FindFirstObjectByType<HeartOfWorld>(FindObjectsInactive.Include);
            if (heart == null)
            {
                log.Append(NL + "  crystal baked, but no Heart of the World in the scene (Set Up MR_Main (13 finale)) (MISSING)");
                return;
            }
            Undo.RecordObject(heart, "Step 14f");
            heart.crystalRecipe = recipe;
            EditorUtility.SetDirty(heart);
            log.Append(NL + $"  forehead crystal baked and on the Heart ({recipe.name})");
        }
    }
}
