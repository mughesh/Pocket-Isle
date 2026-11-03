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
    /// C: Cinderwood's arrival lanterns (rewrites the Cinderwood map: 4 stone lanterns past the arrival, nothing else
    /// moves; "Arrival Lanterns", a LanternHollowPiece copied from the hollow's; the lesson and ghost move there).
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
            CinderArrival(log);
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

        /// <summary>C: the arrival lanterns (map + piece); the hollow keeps a counter and a short line, no ghost.</summary>
        static void CinderArrival(StringBuilder log)
        {
            CinderwoodTools.Write(log);
            var pieces = Object.FindObjectsByType<LanternHollowPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var hollow = System.Array.Find(pieces, p => p.id == Step10fSetup.HollowId);
            if (hollow == null)
            {
                log.Append(NL + "  arrival lanterns: no smoky hollow to copy (Set Up MR_Main (10f Cinderwood hands))");
                return;
            }
            var arrive = System.Array.Find(pieces, p => p.id == ArriveLanternsId);
            if (arrive == null)
            {
                var go = new GameObject("Arrival Lanterns");
                Undo.RegisterCreatedObjectUndo(go, "Step 14c");
                go.transform.SetParent(hollow.transform.parent, false);
                arrive = go.AddComponent<LanternHollowPiece>();
            }
            Undo.RecordObject(arrive, "Step 14c");
            EditorUtility.CopySerialized(hollow, arrive);
            // Every field that differs, set explicitly (a re-run copies whatever the hollow has now).
            arrive.id = ArriveLanternsId;
            arrive.reach = 4;
            arrive.lanterns = CinderwoodTools.ArriveLanterns;
            arrive.smoke = new Vector2Int[0];
            arrive.smokeRecipe = null;
            arrive.firstSlot = ArriveFirstSlot;
            arrive.spotRadius = 1.6f;
            arrive.waitFor = "";
            arrive.lessonLine = "cw.arrive.lanterns";
            arrive.lessonChip = "Touch";
            arrive.counterLabel = "Touch · Lanterns";
            arrive.ghostClip = "poke";
            arrive.ghostScale = 1f;
            arrive.doneLine = "cw.arrive.lanterns.done";
            arrive.raceOnTouch = true;
            arrive.embersPerLantern = 8;
            EditorUtility.SetDirty(arrive);

            Undo.RecordObject(hollow, "Step 14c");
            hollow.ghostClip = "";
            hollow.lessonChip = "";
            hollow.counterLabel = "Lanterns";
            hollow.raceOnTouch = false;
            hollow.embersPerLantern = 0;
            EditorUtility.SetDirty(hollow);
            log.Append(NL + $"  'Arrival Lanterns' ({ArriveLanternsId}): {arrive.lanterns.Length} lanterns from c{arrive.lanterns[0].x} r{arrive.lanterns[0].y}, " +
                       $"one touch races the line, warm slots {arrive.firstSlot}-{arrive.firstSlot + arrive.lanterns.Length - 1} (radius {arrive.spotRadius} tiles), " +
                       $"lesson {arrive.lessonLine} + ghost {arrive.ghostClip}, chip '{arrive.counterLabel} n/{arrive.lanterns.Length}'; hollow: no ghost, chip '{hollow.counterLabel}'");
        }

        public const string ArriveLanternsId = "cw.arrive";
        /// <summary>WarmSpots slots 8-11 (Cinderwood's village 0, hollow 1-6, forge 7).</summary>
        public const int ArriveFirstSlot = 8;

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
