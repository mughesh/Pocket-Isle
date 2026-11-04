using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Mugesh's Nov 4 notes: reimports the script (the temple doors' <c>temple.few4</c> / <c>temple.few6</c>) and moves the
    /// island's spawn to 55 cm ahead (45 sat at the belly). Code-only parts need no setup: the Isling minimum at the
    /// temple doors (<see cref="TempleRules"/>), the radiating forehead crystal, the roomier settings board. Safe to re-run.
    /// </summary>
    public static class Nov4Setup
    {
        const string NL = "\n";
        public const float Distance = 0.55f;

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (Nov 4 notes)", priority = 1007)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Nov4] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Nov4] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported (temple.few4, temple.few6)");
            int placers = 0;
            foreach (var p in Object.FindObjectsByType<IslandPlacer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(p, "Island distance");
                p.distance = Distance;
                EditorUtility.SetDirty(p);
                placers++;
            }
            log.Append(NL + (placers > 0 ? $"  island spawn {Distance * 100f:0} cm ahead ({placers} placer)" : "  no IslandPlacer in the scene"));
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
