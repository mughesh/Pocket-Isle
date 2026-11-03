using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14c (Nov 3): Galewick first. A: the realm order's Keeper levels (Galeowl Lv 8, Hearthstag Lv 12, Tidemother
    /// unchanged; RealmOrderSim), all three gates open in builds, the script reimported. B: the Quick Journey goes to
    /// Galewick (gifts Toastling + Whirlby, per-starter levels from QuickJourneySim.SweepWind, the director's realm).
    /// Safe to re-run.
    /// </summary>
    public static class Step14cSetup
    {
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14c Galewick first)", priority = 1002)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14c] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step14c] MR_Main set up:");
            DialogueTools.Import();
            log.Append(NL + "  Script_v1 reimported");
            Order(log);
            Quick(log);
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// B: the quick gifts and levels (QuickJourneySim.SweepWind, Galeowl Lv 8: Kilnpot gift 5 / raised 8, Pebbloop 8 / 9,
        /// Pufflet 8 / 7; Toastling leads: fire breaks the Gust Wall) and the director's quick realm.
        /// </summary>
        static void Quick(StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<QuickJourneyConfig>(QuickJourneySim.ConfigPath);
            if (config == null)
            {
                log.Append(NL + "  Quick Journey config MISSING (Step9gSetup.QuickOnly)");
                return;
            }
            IslingSpecies S(string n) => KeeperSim.Load(n);
            config.giftSpecies = new[] { S("Toastling"), S("Whirlby") };
            config.giftLevel = 8;
            config.raiseTo = 8;
            config.perStarter = new[]
            {
                new QuickJourneyConfig.StarterTuning { starter = S("Kilnpot"), giftLevel = 5, raiseTo = 8 },
                new QuickJourneyConfig.StarterTuning { starter = S("Pebbloop"), giftLevel = 8, raiseTo = 9 },
                new QuickJourneyConfig.StarterTuning { starter = S("Pufflet"), giftLevel = 8, raiseTo = 7 },
            };
            EditorUtility.SetDirty(config);
            var director = Object.FindFirstObjectByType<QuickJourneyDirector>(FindObjectsInactive.Include);
            if (director != null)
            {
                Undo.RecordObject(director, "Step 14c");
                director.galewick = AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);
                EditorUtility.SetDirty(director);
            }
            log.Append(NL + $"  Quick Journey: gifts {string.Join(" + ", System.Array.ConvertAll(config.giftSpecies, s => s != null ? s.name : "MISSING"))}, " +
                       $"Kilnpot 5/8, Pebbloop 8/9, Pufflet 8/7; director {(director != null && director.galewick != null ? "-> Galewick" : "MISSING")}");
        }

        /// <summary>A: Keeper levels for the Galewick-first order, every gate open in builds.</summary>
        static void Order(StringBuilder log)
        {
            foreach (var hall in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (hall.keeperSpecies == null) continue;
                int level = hall.keeperSpecies.name switch
                {
                    "Galeowl" => GaleowlKit.Level,
                    "Hearthstag" => KeeperKit.Level,
                    "Tidemother" => TidemotherKit.Level,
                    _ => hall.keeperLevel,
                };
                if (hall.keeperLevel == level) continue;
                Undo.RecordObject(hall, "Step 14c");
                log.Append(NL + $"  {hall.keeperSpecies.name}: Lv {hall.keeperLevel} -> {level}");
                hall.keeperLevel = level;
                EditorUtility.SetDirty(hall);
            }
            foreach (var path in new[] { GalewickTools.ConfigPath, LilymereTools.ConfigPath })
            {
                var c = AssetDatabase.LoadAssetAtPath<GalewickConfig>(path);
                if (c == null)
                {
                    log.Append(NL + $"  {path} MISSING");
                    continue;
                }
                if (!c.open)
                {
                    c.open = true;
                    EditorUtility.SetDirty(c);
                }
                log.Append(NL + $"  {c.name}: open in builds");
            }
        }
    }
}
