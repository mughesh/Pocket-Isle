using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 7: the progression config (Data/Battle/Progression), evolution levels on the species, the GrowthDirector,
    /// and the dialogue script re-imported. Safe to re-run.
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

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
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
