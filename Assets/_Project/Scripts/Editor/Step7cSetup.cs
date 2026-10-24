using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7c: the lodge's door archway (props rewritten and baked), the room's palm-over-the-arch exit, and the rest of
    /// the 7c wiring as it lands. Run after Set Up MR_Main (7b lodge). Safe to re-run.
    /// </summary>
    static class Step7cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (7c)", priority = 46)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step7c] Stop Play mode first.");
                return;
            }
            var log = new System.Text.StringBuilder("[Step7c] MR_Main set up:");
            LodgeTools.WriteLodge(log);
            MergeOutlines(log);

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var room = Object.FindFirstObjectByType<LodgeRoom>(FindObjectsInactive.Include);
            if (room == null)
            {
                Debug.LogError("[Step7c] No Lodge Room (run Set Up MR_Main (7b lodge) first).");
                return;
            }
            room.beaconMaterial = Step7bSetup.BeaconMaterial();
            room.archHeight = 0.76f;
            EditorUtility.SetDirty(room);
            log.Append("\n  lodge door: 2-tile glowing archway; leave by walking onto it, look + pinch, or palm over it (ring)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        // ---- E: one material per renderer ----

        /// <summary>
        /// The inverted-hull outline used to be a second material on single-submesh renderers ("more materials than
        /// submeshes"). Now it is Diorama/Toon's own Outline pass: Creature has it on, Creature_Plain off, every other
        /// Toon material off (island content draws its outlines with its own Toon Outline batches). Prefab renderers
        /// with [Creature, Creature_Outline] become [Creature]; creature pieces without an outline become
        /// Creature_Plain (by their baked style, so re-running is safe). Pods get Creature_Plain for the Isling inside.
        /// </summary>
        static void MergeOutlines(System.Text.StringBuilder log)
        {
            var mats = CreatureBaker.Materials();
            var oldOutline = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Creatures/Creature_Outline.mat");
            var toonShader = Shader.Find("Diorama/Toon");
            int offs = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.shader != toonShader || m == mats.toon) continue;
                if (m.GetShaderPassEnabled("SRPDefaultUnlit")) offs++;
                CreatureBaker.SetOutlinePass(m, false);
            }

            int fixedRenderers = 0, prefabs = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                int before = fixedRenderers;
                var outlined = new System.Collections.Generic.Dictionary<Renderer, bool>();
                foreach (var view in root.GetComponentsInChildren<CreatureView>(true))
                    foreach (var p in view.pieces)
                        if (p.renderer != null) outlined[p.renderer] = p.style.outlineWidth > 0f;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    var current = r.sharedMaterials;
                    if (current.Length == 0 || current[0] == null || current[0].shader != toonShader) continue;
                    Material want = current[0];
                    if (outlined.TryGetValue(r, out bool on)) want = on ? mats.toon : mats.plain;
                    if (current.Length == 1 && current[0] == want) continue;
                    if (current.Length > 2 || current.Length == 2 && current[1] != oldOutline) continue;
                    r.sharedMaterials = new[] { want };
                    fixedRenderers++;
                }
                bool changed = fixedRenderers > before;
                foreach (var pod in root.GetComponentsInChildren<PodView>(true))
                    if (pod.miniMaterial != mats.plain)
                    {
                        pod.miniMaterial = mats.plain;
                        changed = true;
                    }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabs++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            log.Append($"\n  outlines merged into Diorama/Toon: {fixedRenderers} renderer(s) in {prefabs} prefab(s) now one material, " +
                       $"outline pass turned off on {offs} other Toon material(s), pods use Creature_Plain inside");
        }
    }
}
