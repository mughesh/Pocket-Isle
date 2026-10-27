using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11d: fixes from Mugesh's Oct 27 headset notes. A: every hand moment gets the objective rule (gold rings on
    /// what you can act on, a counter in the chip, arrows to what's out of view), the kite field says its task, open
    /// doors glow (forge, wind tower). B: the pinch ghosts and the ghost spark. C: Galewick's map (low rocks by the cloud
    /// bank, the return gate by the tower) and the tower gate's link home; bigger raised puffs. D: Lilymere's pads, lotus,
    /// rain cloud and the two-hand scoop. Safe to re-run.
    /// </summary>
    public static class Step11dSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        /// <summary>Counter labels per objective (id → chip label).</summary>
        public static readonly (string id, string label)[] Counters =
        {
            ("gw.kites", "Kites"), ("gw.clouds", "Clouds"), ("gw.mill", "Flicks"), ("gw.knots", "Knots"),
            ("lm.pads", "Pads"), ("lm.rain", "Lotus"), ("lm.lagoon", "Pours"),
        };

        [MenuItem("Tools/Pocket Isle/Setup/Lilymere (11)/Set Up MR_Main (11d Oct 27 notes)", priority = 804)]
        public static void All()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step11d] Stop Play mode first.");
                return;
            }
            var log = new StringBuilder("[Step11d] MR_Main set up:");
            if (!GalewickMap(log)) return;
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            Pieces(log);
            Doors(log);
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (gw.kites.task, gw.tower.open, objective.more)");
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            Debug.Log(log.ToString());
        }

        /// <summary>C: Galewick's map rewritten (same seed) with the tower's return gate and low rocks by the cloud bank; its gates linked.</summary>
        internal static bool GalewickMap(StringBuilder log)
        {
            var map = GalewickTools.Write(log);
            if (map == null) return false;
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            var problems = GalewickTools.Check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step11d] Galewick layout check: {p}");
            log.Append(problems.Count == 0 ? "\n  Galewick layout checks: all passed (the tower's return gate front is reachable)" : $"\n  Galewick layout checks: {problems.Count} problem(s)");
            var hub = AssetDatabase.LoadAssetAtPath<WorldMap>(StillIsleTools.MapPath);
            if (hub == null)
            {
                Debug.LogError($"[Step11d] No hub map at {StillIsleTools.MapPath}.");
                return false;
            }
            Step8Setup.LinkGalewick(hub, map, log);
            var gate = GalewickTools.Tile(GalewickTools.TowerGateXY);
            log.Append($"\n  tower return gate c{gate.x} r{gate.y}: prop {(map.TryGetProp(map.PropAt(gate.y, gate.x), out _) ? "placed" : "MISSING")}, linked home");
            return true;
        }

        static void Pieces(StringBuilder log)
        {
            var beacon = Step7bSetup.BeaconMaterial();
            var pieces = Object.FindObjectsByType<HandPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int marked = 0;
            foreach (var p in pieces)
            {
                Undo.RecordObject(p, "Step 11d");
                p.focusMaterial = beacon;
                p.counterLabel = null;
                foreach (var (id, label) in Counters)
                    if (p.id == id) p.counterLabel = label;
                switch (p.id)
                {
                    case "gw.kites":
                        p.lessonLine = "gw.kites.task";
                        p.ghostClip = "pinch_lift";
                        break;
                    case "cinder.log":
                    case "cw.ember":
                        p.ghostClip = "pinch_drop";
                        break;
                    case "lm.lagoon":
                        p.ghostClip = "cup_scoop";
                        break;
                }
                if (p is WheelPiece wheel)
                {
                    // C10: a thicker flick zone, a lower speed (index and middle fingertips both count in code).
                    wheel.nearPlane = 0.04f;
                    wheel.slack = 0.025f;
                    wheel.minFlickSpeed = 0.35f;
                }
                if (p is CloudBankPiece bank)
                {
                    bank.puffScale = 2.7f;
                    bank.puffHeight = 0.6f;
                }
                if (p is RainGarden rain) rain.settleReach = 7f;
                if (p is LilyPadBridge pads && pads.slots.Length > 0)
                {
                    // The pads stay in the part of the channel the window shows round the crossing.
                    float cx = pads.slots[0].x - 0.5f;
                    pads.channel = Rect.MinMaxRect(cx - 5.5f, pads.channel.yMin, cx + 6.5f, pads.channel.yMax);
                    pads.floatHeight = 0.03f;
                    pads.respawnAfter = 3f;
                }
                EditorUtility.SetDirty(p);
                marked++;
                log.Append($"\n  '{p.name}' ({p.id}): gold marks {(beacon != null ? "on" : "MISSING")}{(string.IsNullOrEmpty(p.counterLabel) ? "" : $", counter '{p.counterLabel}'")}, ghost {p.ghostClip}");
            }
            var ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            var fx = Object.FindFirstObjectByType<BattleFx>(FindObjectsInactive.Include);
            if (ghosts != null)
            {
                Undo.RecordObject(ghosts, "Step 11d");
                ghosts.fx = fx;
                EditorUtility.SetDirty(ghosts);
            }
            log.Append($"\n  {marked} hand moments; ghost pinch spark {(ghosts != null && fx != null ? "on" : "MISSING")}");
        }

        /// <summary>A4: the forge door and the wind tower door glow the same way once open; the tower's crown hooks.</summary>
        static void Doors(StringBuilder log)
        {
            var beacon = Step7bSetup.BeaconMaterial();
            foreach (var forge in Object.FindObjectsByType<ForgeBellows>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(forge, "Step 11d");
                forge.doorGlowMaterial = beacon;
                EditorUtility.SetDirty(forge);
                log.Append($"\n  '{forge.name}': open door glow {(beacon != null ? "on" : "MISSING")}");
            }
            foreach (var tower in Object.FindObjectsByType<TowerDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(tower, "Step 11d");
                tower.glowMaterial = beacon;
                EditorUtility.SetDirty(tower);
                log.Append($"\n  '{tower.name}': open door glow + crown hooks {(beacon != null ? "on" : "MISSING")}");
            }
        }
    }
}
