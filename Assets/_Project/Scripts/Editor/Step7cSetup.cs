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
            room.maxInRoom = 8;
            EditorUtility.SetDirty(room);
            log.Append("\n  lodge door: 2-tile glowing archway; leave by walking onto it, look + pinch, or palm over it (ring); 8 Islings in the room, the rest in honeycomb pages");
            var director = Object.FindFirstObjectByType<EncounterDirector>();
            if (director != null)
            {
                director.uncaughtWeight = 2f;
                EditorUtility.SetDirty(director);
                log.Append("\n  encounters: species not caught yet count 2x, first meeting sparkles 'New!'");

                var tour = Object.FindFirstObjectByType<PerfTour>(FindObjectsInactive.Include);
                if (tour == null)
                {
                    var go = new GameObject("Perf Tour");
                    Undo.RegisterCreatedObjectUndo(go, "Perf Tour");
                    tour = go.AddComponent<PerfTour>();
                }
                tour.explorer = room.explorer;
                tour.encounters = director;
                tour.runes = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
                tour.swap = room.swap;
                tour.party = room.party;
                EditorUtility.SetDirty(tour);
                log.Append("\n  'Perf Tour' (device perf capture; runs only when perftour.txt is in the persistent data folder)");
            }
            Focus(log);
            DialogueTools.Import();
            var guide = room.GetComponent<LodgeGuide>();
            if (guide == null) guide = Undo.AddComponent<LodgeGuide>(room.gameObject);
            guide.room = room;
            guide.swap = room.swap;
            EditorUtility.SetDirty(guide);
            log.Append("\n  Script_v1 reimported; 'Lodge Guide' on the Lodge Room (first-visit hint chain: inspect, take, leave, exit)");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        // ---- B: one focus language ----

        /// <summary>FocusHighlight on the Dialogue object (gold target + lantern beam, white hover); the Spirit parks beside.</summary>
        static void Focus(System.Text.StringBuilder log)
        {
            var dialogue = Object.FindFirstObjectByType<Dialogue>(FindObjectsInactive.Include);
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (dialogue == null)
            {
                Debug.LogError("[Step7c] No Dialogue in MR_Main (run the dialogue setup first).");
                return;
            }
            var focus = dialogue.GetComponent<FocusHighlight>();
            if (focus == null) focus = Undo.AddComponent<FocusHighlight>(dialogue.gameObject);
            focus.beamMaterial = BeamMaterial();
            focus.island = island;
            EditorUtility.SetDirty(focus);
            var spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
            if (spirit != null)
            {
                spirit.targetOffset = new Vector3(0.065f, 0.012f, 0f);
                EditorUtility.SetDirty(spirit);
            }
            log.Append("\n  FocusHighlight on the Dialogue object (gold hint outline + lantern beam, white gaze hover); the Spirit parks beside targets");
        }

        static Material BeamMaterial()
        {
            const string path = "Assets/_Project/Materials/Props/LanternBeam.mat";
            var shader = Shader.Find("Diorama/Lantern Beam");
            if (shader == null) Debug.LogError("[Step7c] Shader Diorama/Lantern Beam not found.");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = "LanternBeam" };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (shader != null && m.shader != shader) m.shader = shader;
            m.renderQueue = 3012;
            EditorUtility.SetDirty(m);
            return m;
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
