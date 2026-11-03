using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 3 headset feedback on 14a. Re-runs the 14a setup (Lilymere's stream is a sunken mud bed now, the crossing
    /// too, four pads scattered in the mud, the palm hover ghost), then: Galewick's gusts cross slower (the wind lasted
    /// barely past the hand) and the Controls card gets the ghost hands. Safe to re-run.
    /// </summary>
    public static class Step14bSetup
    {
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14b Nov 3 feedback)", priority = 1001)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14b] Stop Play mode first.");
                return;
            }
            Step14aSetup.SetUp();
            var log = new StringBuilder("[Step14b] MR_Main set up:");
            var gusts = Object.FindFirstObjectByType<GustField>(FindObjectsInactive.Include);
            if (gusts != null)
            {
                Undo.RecordObject(gusts, "Step 14b");
                gusts.crossSeconds = 1.4f;
                gusts.bandWidth = 0.1f;
                gusts.trailEvery = 0.06f;
                EditorUtility.SetDirty(gusts);
                log.Append(NL + "  Gusts: cross in 1.4 s (was 0.75), band 0.1, streaks ride the band");
            }
            else log.Append(NL + "  Gusts: GustField MISSING");
            var ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            int boards = 0;
            foreach (var s in Object.FindObjectsByType<SettingsBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(s, "Step 14b");
                s.ghosts = ghosts;
                EditorUtility.SetDirty(s);
                boards++;
            }
            log.Append(NL + $"  Controls card: ghost hands {(ghosts != null ? "wired" : "MISSING")} on {boards} settings board(s)");
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(log.ToString());
        }
    }
}
