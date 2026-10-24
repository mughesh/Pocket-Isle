using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7b: the scene swap (SceneSwap object), the Hearth Lodge interior map and its theme (LodgeTools), and the
    /// save wired to the swap. Safe to re-run.
    /// </summary>
    static class Step7bSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (7b lodge)", priority = 45)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step7b] Stop Play mode first.");
                return;
            }
            var log = new System.Text.StringBuilder("[Step7b] MR_Main set up:");
            var lodge = LodgeTools.WriteLodge(log);
            DialogueTools.Import();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var window = Object.FindFirstObjectByType<WorldWindow>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            var save = Object.FindFirstObjectByType<GameSave>();
            if (island == null || window == null || explorer == null || save == null)
            {
                Debug.LogError("[Step7b] MR_Main needs the island, window, explorer and Game Save (run Set Up MR_Main (7 progression) first).");
                return;
            }

            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap == null)
            {
                var go = new GameObject("Scene Swap");
                Undo.RegisterCreatedObjectUndo(go, "Scene Swap");
                swap = go.AddComponent<SceneSwap>();
            }
            swap.island = island;
            swap.window = window;
            swap.explorer = explorer;
            if (swap.sun == null) swap.sun = RenderSettings.sun != null ? RenderSettings.sun : FindDirectional();
            swap.interiors.RemoveAll(m => m == null);
            if (!swap.interiors.Contains(lodge)) swap.interiors.Add(lodge);
            EditorUtility.SetDirty(swap);
            save.swap = swap;
            EditorUtility.SetDirty(save);
            log.Append($"\n  'Scene Swap' wired (light: {(swap.sun != null ? swap.sun.name : "NONE")}), Game Save knows it");

            Extra(island, explorer, swap, lodge, log);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>The shrines (2 x 2, beacon, lead into the lodge) and, later, the lodge room.</summary>
        static void Extra(IslandBuilder island, GestureTestPiece explorer, SceneSwap swap, WorldMap lodge, System.Text.StringBuilder log)
        {
            // The step 7 storage panel is gone (the lodge replaces it).
            var oldShelf = GameObject.Find("Storage Shelf");
            if (oldShelf != null)
            {
                Undo.DestroyObjectImmediate(oldShelf);
                log.Append("\n  removed the old 'Storage Shelf' object");
            }

            if (island.world != null && !(SceneSwap.IsInside))
            {
                log.Append("\n  " + ShrineTools.SetUp(island.world));
                island.Rebuild();
            }
            var shrines = Object.FindFirstObjectByType<HearthShrines>(FindObjectsInactive.Include);
            var director = Object.FindFirstObjectByType<EncounterDirector>();
            if (shrines == null || director == null)
            {
                Debug.LogError("[Step7b] No Hearth Shrines object (run Set Up MR_Main (7 progression) first).");
                return;
            }
            shrines.window = Object.FindFirstObjectByType<WorldWindow>();
            shrines.swap = swap;
            shrines.lodge = lodge;
            shrines.beaconMaterial = BeaconMaterial();
            shrines.grabGuard = director.pods != null ? director.pods.handle : null;
            shrines.beaconHeight = 0.22f;
            shrines.beaconWidth = 0.04f;
            EditorUtility.SetDirty(shrines);
            log.Append("\n  shrines lead into the lodge (palm over the flame or look + pinch), beacon material Materials/Props/Beacon.mat");
        }

        static Material BeaconMaterial()
        {
            const string path = "Assets/_Project/Materials/Props/Beacon.mat";
            var shader = Shader.Find("Diorama/Beacon");
            if (shader == null) Debug.LogError("[Step7b] Shader Diorama/Beacon not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = "Beacon" };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            m.SetColor("_Color", new Color(1f, 0.5f, 0.18f));
            m.SetColor("_Core", new Color(1f, 0.9f, 0.62f));
            m.renderQueue = 3010;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Light FindDirectional()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return l;
            return null;
        }
    }
}
