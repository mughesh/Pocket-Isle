using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 15 (Mugesh: "I want that lily pads thing back"): the channel's fingertip lily pad bridge (11b A, lm.pads) is
    /// back. Step 14a had turned it off and laid planks over the crossing (the map asset now has open water covers there
    /// until the pads lock in). This checks the map, turns the piece back on with the crossing's covers, stops the arrival stream
    /// counting an lm.pads solve. Safe to re-run.
    /// Bridge: exec:Diorama.EditorTools.Nov15PadsSetup.SetUp.
    /// </summary>
    public static class Nov15PadsSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Setup/Polish/Lily Pads Back At The Channel (Nov 15)", priority = 1000)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Nov15Pads] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Nov15Pads] Lily pads back at the channel:");
            // The map asset itself was patched by hand (the crossing's 6 planks to ChannelGap covers): rewriting it from
            // LilymereTools would undo the roads widened by hand since Nov 12.
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            if (map == null)
            {
                Debug.LogError("[Nov15Pads] No Lilymere map at " + LilymereTools.MapPath);
                return;
            }
            var problems = LilymereTools.Check(map, log);
            log.Append("\n  Lilymere map: " + (problems.Count == 0 ? "checks pass" : "PROBLEMS: " + string.Join("; ", problems)));

            var pads = Object.FindFirstObjectByType<LilyPadBridge>(FindObjectsInactive.Include);
            if (pads == null)
            {
                Debug.LogError("[Nov15Pads] No Lily Pad Bridge in MR_Main (Set Up MR_Main (11b Lilymere hands)).\n" + log);
                return;
            }
            Undo.RecordObject(pads.gameObject, "Nov 15 pads");
            Undo.RecordObject(pads, "Nov 15 pads");
            pads.gameObject.SetActive(true);
            pads.map = map;
            pads.gapTiles = LilymereTools.GateTiles(false).Select(xy => LilymereTools.Tile(xy.x, xy.y)).ToArray();
            EditorUtility.SetDirty(pads);
            log.Append($"\n  'Lily Pad Bridge' (lm.pads) on: {pads.gapTiles.Length} covered tiles, {pads.slots.Length} slots, ghost '{pads.ghostClip}', lesson {pads.lessonLine}");

            var stream = Object.FindObjectsByType<RainGarden>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g => g.id == "lm.stream");
            if (stream != null)
            {
                Undo.RecordObject(stream, "Nov 15 pads");
                stream.legacySolvedId = "";
                EditorUtility.SetDirty(stream);
                log.Append("\n  arrival stream: an lm.pads solve no longer counts for it");
            }

            // No Script_v1 reimport: the asset holds lines the .md lacks (Nov 12 edits); lm.channel.first is set in both.
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            if (problems.Count == 0) Debug.Log(log.ToString());
            else Debug.LogError(log.ToString());
        }
    }
}
