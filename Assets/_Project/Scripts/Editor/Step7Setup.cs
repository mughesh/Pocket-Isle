using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7: the progression config (Data/Battle/Progression), evolution levels on the species, the GrowthDirector;
    /// the Hearth Shrines (<see cref="ShrineTools"/>) with their scene object and the storage shelf; the species
    /// registry and the Game Save object (save / load, pause); and the dialogue script re-imported. Safe to re-run.
    /// </summary>
    static class Step7Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string ProgressionPath = "Assets/_Project/Data/Battle/Progression.asset";
        const string SpeciesDir = "Assets/_Project/Data/Creatures/Species";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (7 progression)", priority = 44)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step7] Stop Play mode first.");
                return;
            }
            var log = new System.Text.StringBuilder("[Step7] MR_Main set up:");
            var progression = ProgressionAsset();
            log.Append($"\n  {ProgressionPath} (xp to next = {progression.a} x level + {progression.b}, cap {progression.levelCap})");
            log.Append("\n  evolutions: " + Evolutions());
            DialogueTools.Import();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var director = Object.FindFirstObjectByType<EncounterDirector>();
            var stage = Object.FindFirstObjectByType<BattleStage>();
            if (director == null || stage == null || director.party == null || director.pods == null)
            {
                Debug.LogError("[Step7] MR_Main needs the battle and capture setup first (Set Up MR_Main (6 capture)).");
                return;
            }
            Undo.RecordObject(director, "Step 7");
            director.progression = progression;
            EditorUtility.SetDirty(director);
            Undo.RecordObject(stage, "Step 7");
            stage.progression = progression;
            EditorUtility.SetDirty(stage);

            if (!director.TryGetComponent(out GrowthDirector growth)) growth = Undo.AddComponent<GrowthDirector>(director.gameObject);
            growth.party = director.party;
            growth.pods = director.pods;
            growth.encounters = director;
            growth.progression = progression;
            EditorUtility.SetDirty(growth);
            log.Append($"\n  GrowthDirector on {director.name}; progression wired to the director and the stage");

            // ---- C: Hearth Shrines and the storage shelf ----
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var rack = director.pods;
            if (island != null && island.world != null)
            {
                log.Append("\n  " + ShrineTools.SetUp(island.world));
                island.Rebuild();
            }
            else Debug.LogError("[Step7] The island has no world map; no shrines placed.");

            var shelf = Find<StorageShelf>("Storage Shelf");
            shelf.island = island;
            shelf.party = director.party;
            shelf.pods = rack;
            shelf.podPrefab = rack.podPrefab;
            shelf.hudMaterial = rack.hudMaterial;
            shelf.fx = rack.fx;
            shelf.grabGuard = rack.handle;
            shelf.offset = new Vector3(-0.04f, 0.15f, 0.06f);
            EditorUtility.SetDirty(shelf);

            var shrines = Find<HearthShrines>("Hearth Shrines");
            shrines.island = island;
            shrines.explorer = director.explorer;
            shrines.party = director.party;
            shrines.pods = rack;
            shrines.shelf = shelf;
            shrines.fx = rack.fx;
            EditorUtility.SetDirty(shrines);
            director.shrines = shrines;
            EditorUtility.SetDirty(director);
            log.Append("\n  'Hearth Shrines' and 'Storage Shelf' objects wired; losing returns to the last shrine");

            // ---- D: save / load, pause ----
            var registry = Registry();
            var save = Find<GameSave>("Game Save");
            save.party = director.party;
            save.explorer = director.explorer;
            save.shrines = shrines;
            save.encounters = director;
            save.realm = island != null ? island.GetComponent<RealmRestoration>() : null;
            save.registry = registry;
            save.hud = Object.FindFirstObjectByType<BattleHud>(FindObjectsInactive.Include);
            var spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
            save.spirit = spirit != null ? spirit.transform : null;
            EditorUtility.SetDirty(save);
            if (!save.TryGetComponent(out GamePause _)) Undo.AddComponent<GamePause>(save.gameObject);
            log.Append($"\n  'Game Save' (GameSave + GamePause), registry with {registry.species.Count} species" +
                       $"{(save.realm == null ? "; WARNING no RealmRestoration on the island" : "")}{(save.spirit == null ? "; WARNING no Lantern Spirit for the Saved puff" : "")}");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        [MenuItem("Tools/Pocket Isle/Delete Save", priority = 60)]
        static void DeleteSave()
        {
            SaveGame.Delete(GameSave.SavePath);
            Debug.Log($"[Step7] Deleted {GameSave.SavePath}. Next Play starts a new game (stopping Play autosaves again).");
        }

        /// <summary>Data/Creatures/SpeciesRegistry with every species asset (ids = asset names).</summary>
        static SpeciesRegistry Registry()
        {
            const string path = "Assets/_Project/Data/Creatures/SpeciesRegistry.asset";
            var r = AssetDatabase.LoadAssetAtPath<SpeciesRegistry>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<SpeciesRegistry>();
                AssetDatabase.CreateAsset(r, path);
            }
            r.species.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:CreatureSpecies", new[] { SpeciesDir }))
                r.species.Add(AssetDatabase.LoadAssetAtPath<CreatureSpecies>(AssetDatabase.GUIDToAssetPath(guid)));
            r.species.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            EditorUtility.SetDirty(r);
            return r;
        }

        /// <summary>A scene object with this component (made on the first run, found after).</summary>
        static T Find<T>(string name) where T : Component
        {
            var c = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (c != null) return c;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, name);
            return go.AddComponent<T>();
        }

        static ProgressionConfig ProgressionAsset()
        {
            var p = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ProgressionPath);
            if (p != null) return p;
            p = ScriptableObject.CreateInstance<ProgressionConfig>();
            AssetDatabase.CreateAsset(p, ProgressionPath);
            return p;
        }

        static string Evolutions()
        {
            var sb = new System.Text.StringBuilder();
            void Set(string from, string to, int level)
            {
                var a = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"{SpeciesDir}/{from}.asset");
                var b = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"{SpeciesDir}/{to}.asset");
                if (a == null || b == null)
                {
                    Debug.LogError($"[Step7] Species {from} or {to} is missing in {SpeciesDir}.");
                    return;
                }
                a.evolvesTo = b;
                a.evolveLevel = level;
                EditorUtility.SetDirty(a);
                sb.Append($"{from} -> {to} at {level}, ");
            }
            Set("Kilnpot", "Kilnhearth", 14);
            Set("Toastling", "Crumbhog", 12);
            Set("Waxle", "Lanternsnail", 12);
            Set("Glazegolem", "Kilngolem", 16);
            sb.Append("Kettlewing waits for its art");
            return sb.ToString();
        }
    }
}
