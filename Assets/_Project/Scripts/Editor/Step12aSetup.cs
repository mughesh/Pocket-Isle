using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 12a: smart-step walking and guidance. Writes the GoldMark material, puts the footprint preview
    /// (<see cref="WalkPreview"/>) next to the explorer and switches it to smart steps. Safe to re-run.
    /// </summary>
    public static class Step12aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MaterialPath = "Assets/_Project/Materials/GoldMark.mat";

        [MenuItem("Tools/Pocket Isle/Setup/Walking + Guidance (12)/Set Up MR_Main (12a walking)", priority = 900)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step12a] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step12a] MR_Main set up:");
            var mat = GoldMaterial();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer == null)
            {
                Debug.LogError("[Step12a] No explorer (GestureTestPiece) in MR_Main.");
                return;
            }
            Undo.RecordObject(explorer, "Step 12a");
            explorer.moveMode = MoveMode.SmartStep;
            explorer.smartSeconds = 0.53f;
            explorer.continuousSeconds = 0.4f;
            explorer.smartTiles = SmartStep.SmartTiles;
            explorer.doubleSwipeSeconds = 0.45f;
            var preview = explorer.GetComponent<WalkPreview>();
            if (preview == null) preview = Undo.AddComponent<WalkPreview>(explorer.gameObject);
            preview.explorer = explorer;
            preview.island = explorer.island;
            preview.material = mat;
            preview.footTiles = 0.9f;
            preview.ringTiles = 1.05f;
            log.Append($"\n  explorer: smart steps ({explorer.smartSeconds} s a tile, walk on {explorer.continuousSeconds} s, double swipe {explorer.doubleSwipeSeconds} s), footprints on");
            Extra(log, mat);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>Later parts of 12a (lessons, Spirit's trail) wire their pieces here.</summary>
        static void Extra(StringBuilder log, Material gold)
        {
        }

        internal static Material GoldMaterial()
        {
            var shader = Shader.Find("Diorama/GoldMark");
            if (shader == null) Debug.LogError("[Step12a] Shader Diorama/GoldMark not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (m == null)
            {
                m = new Material(shader) { name = "GoldMark" };
                AssetDatabase.CreateAsset(m, MaterialPath);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            m.SetColor("_Color", new Color(1f, 0.74f, 0.12f));
            m.SetColor("_Core", new Color(1f, 0.95f, 0.62f));
            m.SetColor("_Outline", new Color(0.16f, 0.08f, 0f));
            m.renderQueue = 3009;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
