using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9i: the Hearth Lodge door on the back wall (bridge: exec:Diorama.EditorTools.Step9iSetup.All). The lodge map
    /// and props rewritten and baked (LodgeTools), the Lodge Room's door, outward step and cushions set to match. Safe to
    /// re-run.
    /// </summary>
    public static class Step9iSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (9i lodge back door)", priority = 62)]
        public static void All()
        {
            var log = new StringBuilder("[Step9i] Set up:");
            LodgeTools.WriteLodge(log);
            var room = Object.FindFirstObjectByType<LodgeRoom>(FindObjectsInactive.Include);
            if (room == null) log.Append("\n  MISSING Lodge Room (Set Up MR_Main (7b lodge) first).");
            else
            {
                Undo.RecordObject(room, "Step 9i");
                room.doorTiles = (Vector2Int[])LodgeTools.DoorTiles.Clone();
                room.doorOutward = new Vector2Int(0, -1);
                room.cozyTiles = (Vector2Int[])LodgeTools.CushionTiles.Clone();
                EditorUtility.SetDirty(room);
                log.Append($"\n  'Lodge Room': door c{LodgeTools.DoorTiles[0].x}-{LodgeTools.DoorTiles[1].x} r0 (back wall), outward (0,-1), " +
                           $"cushions {string.Join(" ", System.Array.ConvertAll(LodgeTools.CushionTiles, t => $"c{t.x}r{t.y}"))}");
            }
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append("\n  Scene saved.");
            }
            else log.Append($"\n  Scene NOT saved (active: {scene.path}, playing {EditorApplication.isPlaying}).");
            Debug.Log(log.ToString());
        }
    }
}
