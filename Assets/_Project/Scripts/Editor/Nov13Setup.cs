using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 13 headset notes (Mugesh, first release build): the statue stood half a tile right of the road (its 3 x 3
    /// footprint's middle is a tile middle, the 2-wide road's is a tile edge): the Still Isle legend draws it half a tile
    /// left (what blocks walking stays). The title logo's rock hid the statue's head from a seated eye: it floats higher.
    /// Tools > Pocket Isle > Fixes > Nov 13 (or the bridge).
    /// </summary>
    public static class Nov13Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MapPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";
        public static readonly Vector2 StatueNudge = new Vector2(-0.5f, 0f);
        public const float LogoHeight = 0.32f;

        [MenuItem("Tools/Pocket Isle/Fixes/Nov 13")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Nov13] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Nov13]");
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(MapPath);
            if (map == null)
            {
                Debug.LogError($"[Nov13] No map at {MapPath}.");
                return;
            }
            Undo.RecordObject(map, "Nov 13 statue nudge");
            int n = 0;
            for (int i = 0; i < map.propLegend.Count; i++)
            {
                var e = map.propLegend[i];
                if (e.Key != 'S') continue;
                e.nudge = StatueNudge;
                map.propLegend[i] = e;
                n++;
            }
            if (n == 0) Debug.LogError("[Nov13] No 'S' (statue) in the Still Isle legend.");
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            log.Append($"\n  Still Isle statue drawn {StatueNudge.x} tile across ({n} legend entry).");

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            int logos = 0;
            foreach (var l in Object.FindObjectsByType<TitleLogo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(l, "Nov 13 logo height");
                l.height = LogoHeight;
                EditorUtility.SetDirty(l);
                logos++;
            }
            log.Append($"\n  Title logo {LogoHeight} m over the island ({logos}).");
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log(log.ToString());
        }
    }
}
