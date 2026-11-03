using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14a: first-minute polish and the realm arrival moments. Reimports Script_v1 (the pinch lesson's "Nice! Once
    /// more." / "Perfect!", gw.arrive*, lm.arrive.stream*, the shorter ridge and garden lines), rewrites the Galewick map
    /// (two cloud puffs on the trail past the arrival) and the Lilymere map (the arrival stream; the channel crossing is a
    /// plank bridge) with their checks, and wires MR_Main: the grab frame shows the handles (6 cm, gold held), the
    /// Spirit's sunrays, Galewick's "Arrival Clouds" (CloudBankPiece gw.arrive, the sweep lesson moves here; the ridge
    /// keeps its counter, no ghost), Lilymere's "Arrival Stream" (RainGarden in stream mode, lm.stream; old saves with
    /// lm.pads solved count), the lily pad bridge off, the garden's rain lesson without its ghost. Safe to re-run.
    /// </summary>
    public static class Step14aSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string NL = "\n";

        [MenuItem("Tools/Pocket Isle/Setup/First Minute (14)/Set Up MR_Main (14a first-minute polish)", priority = 1000)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step14a] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var log = new StringBuilder("[Step14a] MR_Main set up:");
            var script = DialogueTools.Import();
            log.Append(NL + $"  Script_v1 reimported ({(script != null ? "ok" : "FAILED")}: lesson.pinch.once / .done, gw.arrive*, lm.arrive.stream*)");
            Maps(log);
            GrabFrame(log);
            Sunrays(log);
            ArrivalClouds(log);
            ArrivalStream(log);
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        static void Maps(StringBuilder log)
        {
            var gw = GalewickTools.Write(log);
            if (gw != null)
            {
                var problems = GalewickTools.Check(gw, log);
                log.Append(NL + (problems.Count == 0 ? "  Galewick map: checks pass" : "  Galewick map PROBLEMS: " + string.Join("; ", problems)));
            }
            var lm = LilymereTools.Write(log);
            if (lm != null)
            {
                var problems = LilymereTools.Check(lm, log);
                log.Append(NL + (problems.Count == 0 ? "  Lilymere map: checks pass" : "  Lilymere map PROBLEMS: " + string.Join("; ", problems)));
            }
        }

        static void GrabFrame(StringBuilder log)
        {
            var frame = Object.FindFirstObjectByType<IslandGrabFrame>(FindObjectsInactive.Include);
            if (frame == null)
            {
                log.Append(NL + "  grab frame: MISSING");
                return;
            }
            Undo.RecordObject(frame, "Step 14a");
            frame.showDistance = 0.06f;
            frame.grabbedColor = new Color(1f, 0.78f, 0.3f);
            frame.grabbedTint = 0.85f;
            EditorUtility.SetDirty(frame);
            log.Append(NL + $"  grab frame: corner brackets (idle {frame.idleLevel:0.00}) + front lip, lit within {frame.showDistance * 100f:0} cm, gold held");
        }

        static void Sunrays(StringBuilder log)
        {
            var spirit = Object.FindFirstObjectByType<LanternSpirit>(FindObjectsInactive.Include);
            var frame = Object.FindFirstObjectByType<IslandGrabFrame>(FindObjectsInactive.Include);
            if (spirit == null || frame == null || frame.material == null)
            {
                log.Append(NL + "  sunrays: MISSING the Spirit or the Grab Frame material");
                return;
            }
            var rays = spirit.GetComponent<SpiritSunrays>();
            if (rays == null) rays = Undo.AddComponent<SpiritSunrays>(spirit.gameObject);
            rays.spirit = spirit;
            rays.material = frame.material;
            EditorUtility.SetDirty(rays);
            log.Append(NL + "  sunrays on the Lantern Spirit (objectives only, one draw)");
        }

        static void ArrivalClouds(StringBuilder log)
        {
            var banks = Object.FindObjectsByType<CloudBankPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ridge = System.Array.Find(banks, b => b.id == "gw.clouds");
            if (ridge == null)
            {
                log.Append(NL + "  arrival clouds: no ridge cloud bank to copy (Set Up MR_Main (10b))");
                return;
            }
            var arrive = System.Array.Find(banks, b => b.id == "gw.arrive");
            if (arrive == null)
            {
                var go = new GameObject("Arrival Clouds");
                Undo.RegisterCreatedObjectUndo(go, "Step 14a");
                go.transform.SetParent(ridge.transform.parent, false);
                arrive = go.AddComponent<CloudBankPiece>();
            }
            Undo.RecordObject(arrive, "Step 14a");
            EditorUtility.CopySerialized(ridge, arrive);
            arrive.id = "gw.arrive";
            arrive.reach = 4;
            arrive.puffs = System.Array.ConvertAll(GalewickTools.ArriveCloudXY, c => GalewickTools.Tile(c));
            arrive.puffTwins = System.Array.ConvertAll(GalewickTools.ArriveCloudTwinXY, c => GalewickTools.Tile(c));
            arrive.lessonLine = "gw.arrive.clouds";
            arrive.lessonChip = "Sweep";
            arrive.counterLabel = "Sweep · Clouds";
            arrive.ghostClip = string.IsNullOrEmpty(ridge.ghostClip) ? "palm_sweep" : ridge.ghostClip;
            arrive.doneLine = "gw.arrive.done";
            arrive.rippleOnClear = true;
            EditorUtility.SetDirty(arrive);

            Undo.RecordObject(ridge, "Step 14a");
            // The arrival taught the sweep: the ridge keeps its counter and a short line, no ghost.
            ridge.ghostClip = "";
            ridge.rippleOnClear = false;
            if (string.IsNullOrEmpty(ridge.counterLabel)) ridge.counterLabel = "Clouds";
            EditorUtility.SetDirty(ridge);
            log.Append(NL + $"  'Arrival Clouds' (gw.arrive): 2 puffs at c{arrive.puffs[0].x} r{arrive.puffs[0].y} / r{arrive.puffs[1].y} (2 wide), lesson gw.arrive.clouds + ghost {arrive.ghostClip}, chip '{arrive.counterLabel} n/2', ripple on clear; ridge bank: no ghost, counter '{ridge.counterLabel}'");
        }

        static void ArrivalStream(StringBuilder log)
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(LilymereTools.MapPath);
            var gardens = Object.FindObjectsByType<RainGarden>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var garden = System.Array.Find(gardens, g => g.id == "lm.rain");
            if (garden == null || map == null)
            {
                log.Append(NL + "  arrival stream: no lotus garden to copy (Set Up MR_Main (11b)) or no Lilymere map");
                return;
            }
            var stream = System.Array.Find(gardens, g => g.id == "lm.stream");
            if (stream == null)
            {
                var go = new GameObject("Arrival Stream");
                Undo.RegisterCreatedObjectUndo(go, "Step 14a");
                go.transform.SetParent(garden.transform.parent, false);
                stream = go.AddComponent<RainGarden>();
            }
            Undo.RecordObject(stream, "Step 14a");
            EditorUtility.CopySerialized(garden, stream);
            stream.id = "lm.stream";
            stream.reach = 4;
            stream.buds = System.Array.ConvertAll(LilymereTools.StreamPadsXY, p => LilymereTools.MapPoint(p));
            stream.bridgeSlots = System.Array.ConvertAll(LilymereTools.StreamBridgeXY, p => LilymereTools.MapPoint(p));
            var bed = new List<Vector2Int>();
            var crossing = new List<Vector2Int>();
            for (int x = LilymereTools.StreamX0; x <= LilymereTools.StreamX1; x++)
                for (int y = LilymereTools.StreamY0; y <= LilymereTools.StreamY1; y++)
                {
                    var t = LilymereTools.Tile(x, y);
                    if (LilymereTools.IsStreamGate(x, y)) crossing.Add(t);
                    else if (map.Raw(t.y, t.x) == IslandMap.DryBed) bed.Add(t);
                }
            stream.streamTiles = bed.ToArray();
            stream.crossingTiles = crossing.ToArray();
            stream.gapTiles = crossing.ToArray();
            var mid = LilymereTools.MapPoint(new Vector2((LilymereTools.StreamX0 + LilymereTools.StreamX1) * 0.5f, (LilymereTools.StreamY0 + LilymereTools.StreamY1) * 0.5f));
            stream.gardenCenter = mid;
            stream.gardenRadius = new Vector2(8f, 3f);
            stream.budRecipe = stream.stoneRecipe = PropKit.Load("LilyPad");
            stream.budScale = 1.3f;
            stream.stoneScale = 1.7f;
            stream.lessonLine = "lm.arrive.stream";
            stream.lessonChip = "Palm down";
            stream.counterLabel = "Palm down · Pads";
            stream.doneLine = "lm.arrive.stream.done";
            stream.legacySolvedId = "lm.pads";
            stream.settleReach = 6f;
            EditorUtility.SetDirty(stream);

            Undo.RecordObject(garden, "Step 14a");
            // The stream taught the rain: the garden keeps its counter and a short line, no ghost.
            garden.ghostClip = "";
            EditorUtility.SetDirty(garden);

            var pads = Object.FindFirstObjectByType<LilyPadBridge>(FindObjectsInactive.Include);
            if (pads != null)
            {
                Undo.RecordObject(pads.gameObject, "Step 14a");
                pads.gameObject.SetActive(false);
            }
            log.Append(NL + $"  'Arrival Stream' (lm.stream): {bed.Count} stream tiles, {crossing.Count} crossing tiles, 3 pads on the south bank, " +
                       $"lesson lm.arrive.stream + ghost {stream.ghostClip}, chip '{stream.counterLabel} n/3', old lm.pads counts; " +
                       $"lily pad bridge {(pads != null ? "off (the channel is planks)" : "not found")}; garden: no ghost");
        }
    }
}
