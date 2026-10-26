using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9f: the level curve, visible XP, Steambara, the explorer and the rebuilt fire evolutions. One method per
    /// section (bridge: exec:Diorama.EditorTools.Step9fSetup.X). Safe to re-run.
    /// </summary>
    public static class Step9fSetup
    {
        const string ProgressionPath = "Assets/_Project/Data/Battle/Progression.asset";

        /// <summary>A: XP x3 on the progression asset (catches already pay the win reward); Cinderwood wild +2 is in its tables.</summary>
        public static void Curve()
        {
            var config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ProgressionPath);
            if (config == null)
            {
                Debug.LogError($"[Step9f] {ProgressionPath} missing.");
                return;
            }
            config.rewardMultiplier = 3f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step9f] Curve: reward x{config.rewardMultiplier} (a Lv 5 win pays {config.Reward(5, false)} XP, Lv 5 -> 6 needs {config.XpToNext(5)}).");
        }

        /// <summary>B: the pod rack's stats card gets the XP curve (its XP bar) in MR_Main.</summary>
        public static void Xp()
        {
            var rack = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            var config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ProgressionPath);
            if (rack == null || config == null)
            {
                Debug.LogError($"[Step9f] Xp: {(rack == null ? "no PodRack in the open scene" : $"{ProgressionPath} missing")}.");
                return;
            }
            Undo.RecordObject(rack, "Step 9f");
            rack.progression = config;
            EditorUtility.SetDirty(rack);
            var state = Object.FindFirstObjectByType<GameState>(FindObjectsInactive.Include);
            if (state != null)
            {
                Undo.RecordObject(state, "Step 9f");
                state.presetCommons = new[] { "Steambara", "Kettlewing" };
                EditorUtility.SetDirty(state);
            }
            SaveScene();
            Debug.Log("[Step9f] Xp: pod card XP bar wired (PodRack.progression).");
        }

        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        static void SaveScene()
        {
            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath && !EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(active);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(active);
            }
        }

        /// <summary>C: Steambara / Onsenbara (bake, registry, turnaround shots), the springs and grove tables, the Spirit's lines.</summary>
        public static void Steambara()
        {
            var s = Step9fKit.Steambara();
            Build("Steambara -> Onsenbara", s, s != null ? s.evolvesTo : null);
            Step9aSetup.MapOnly();
            DialogueTools.Import();
        }

        /// <summary>
        /// D: writes and bakes the explorer, lifts its rig by the leg length (the baker stands the body, not the legs, on
        /// the floor), adds <see cref="ExplorerView"/>, gives it to the explorer in MR_Main and to the puzzles that cheer.
        /// Shots to Logs/BattleShots/9f_explorer.png.
        /// </summary>
        public static void Explorer()
        {
            var recipe = Step9fKit.Explorer();
            var report = CreatureBaker.Bake(recipe);
            string path = recipe.bakedPrefab != null ? AssetDatabase.GetAssetPath(recipe.bakedPrefab) : null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Step9f] Explorer: bake produced no prefab.");
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            var view = root.GetComponent<CreatureView>();
            view.rigPosition = new Vector3(0f, Step9fKit.ExplorerLegDrop * view.rigScale, 0f);
            view.rig.localPosition = view.rigPosition;
            view.height += view.rigPosition.y;
            if (root.GetComponent<ExplorerView>() == null) root.AddComponent<ExplorerView>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            var prefab = AssetDatabase.LoadAssetAtPath<CreatureView>(path);

            var explorer = Object.FindFirstObjectByType<GestureTestPiece>(FindObjectsInactive.Include);
            if (explorer != null)
            {
                Undo.RecordObject(explorer, "Step 9f");
                explorer.explorerPrefab = prefab;
                EditorUtility.SetDirty(explorer);
            }
            var log = Object.FindFirstObjectByType<FallenLog>(FindObjectsInactive.Include);
            var bellows = Object.FindFirstObjectByType<ForgeBellows>(FindObjectsInactive.Include);
            if (bellows != null)
            {
                Undo.RecordObject(bellows, "Step 9f");
                bellows.explorer = explorer;
                EditorUtility.SetDirty(bellows);
            }
            SaveScene();
            Shot(prefab, $"{Step9cSetup.ShotDir}/9f_explorer.png");
            Debug.Log($"[Step9f] Explorer: {report}; rig lifted {view.rigPosition.y * 1000f:0.0} mm, full height {Measure(prefab) * 100f:0.00} cm; " +
                      $"explorer {(explorer != null ? "wired" : "MISSING")}, log {(log != null && log.explorer != null ? "cheers" : "MISSING")}, " +
                      $"bellows {(bellows != null ? "cheers" : "MISSING")}.");
        }

        /// <summary>Height of the whole baked figure (hood tip to boots), metres at scale 1.</summary>
        static float Measure(CreatureView prefab)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            Object.DestroyImmediate(go);
            return b.size.y;
        }

        /// <summary>Front / side / three-quarter views of the explorer, like the creature turnarounds.</summary>
        static void Shot(CreatureView prefab, string path)
        {
            var species = ScriptableObject.CreateInstance<IslingSpecies>();
            species.name = "Explorer";
            species.displayName = "Explorer";
            species.recipe = AssetDatabase.LoadAssetAtPath<CreatureRecipe>("Assets/_Project/Data/Creatures/Recipes/Explorer.asset");
            IslingShots.Turnaround(species, path);
            Object.DestroyImmediate(species);
        }

        /// <summary>E: the five fire evolutions as full recipes from sheet 11 (bake, registry, shots 9f_&lt;name&gt;.png).</summary>
        public static void Evolutions() => Build("Fire evolutions (sheet 11)", Step9fKit.Forgehorn(), Step9fKit.Emberdillo(), Step9fKit.Cindermole(),
            Step9fKit.Cinderbloom(), Step9fKit.Pyrecrane());

        public static void Forgehorn() => Build("Forgehorn", Step9fKit.Forgehorn());
        public static void Emberdillo() => Build("Emberdillo", Step9fKit.Emberdillo());
        public static void Cindermole() => Build("Cindermole", Step9fKit.Cindermole());
        public static void Cinderbloom() => Build("Cinderbloom", Step9fKit.Cinderbloom());
        public static void Pyrecrane() => Build("Pyrecrane", Step9fKit.Pyrecrane());

        internal static void Build(string what, params IslingSpecies[] species)
        {
            var log = new StringBuilder($"[Step9f] {what}:");
            foreach (var s in species)
            {
                if (s == null || s.recipe == null)
                {
                    log.Append("\n  MISSING species");
                    continue;
                }
                var r = CreatureBaker.Bake(s.recipe);
                log.Append($"\n  {s.name}: {r}");
                IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/9f_{s.name.ToLowerInvariant()}.png");
            }
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
