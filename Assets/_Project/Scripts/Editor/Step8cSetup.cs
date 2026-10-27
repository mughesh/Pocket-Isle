using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8c (first minutes polish): scene values on MR_Main (island at chest height, shard contact) and the
    /// lessons (re-written by Step 8b's setup, then wired). Safe to re-run.
    /// </summary>
    static class Step8cSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Setup/Early (5-8)/Set Up MR_Main (8c polish)", priority = 509)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8c] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            // The lessons, the script and the onboarding / ghost wiring (8b), with 8c's lesson changes.
            Step8bSetup.SetUp();
            var log = new StringBuilder("[Step8c] MR_Main set up:");

            var placer = Object.FindFirstObjectByType<IslandPlacer>(FindObjectsInactive.Include);
            if (placer != null)
            {
                Undo.RecordObject(placer, "Step 8c");
                placer.belowEye = 0.35f;
                placer.distance = 0.45f;
                EditorUtility.SetDirty(placer);
                log.Append($"\n  island spawn: {placer.belowEye * 100f:0} cm below the eyes, {placer.distance * 100f:0} cm ahead (chest)");
            }
            else log.Append("\n  NO IslandPlacer");

            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            if (opening != null)
            {
                Undo.RecordObject(opening, "Step 8c");
                opening.contactSlack = 0.01f;
                opening.autoTouchSeconds = 10f;
                EditorUtility.SetDirty(opening);
                log.Append($"\n  shard: contact (+{opening.contactSlack * 100f:0} cm), unfolds by itself after {opening.autoTouchSeconds:0} s");
            }

            RimZones(log);

            var guide = Object.FindFirstObjectByType<LodgeGuide>(FindObjectsInactive.Include);
            if (guide != null)
            {
                Undo.RecordObject(guide, "Step 8c");
                guide.ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
                guide.onboarding = Object.FindFirstObjectByType<Onboarding>(FindObjectsInactive.Include);
                EditorUtility.SetDirty(guide);
                log.Append($"\n  lodge guide: two chains (empty lodge: toss, inspect, take, exit), ghost {(guide.ghosts != null ? "linked" : "MISSING")}");
            }
            else log.Append("\n  NO LodgeGuide");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Rebuilds the island's rim grab boxes: back and both sides whole, the front only at its two corners. The strip
        /// with the six pod sockets has no grab box, so reaching for a pod can't start an island grab (and the rim's
        /// hover glow, which follows the boxes, stays off it).
        /// </summary>
        static void RimZones(StringBuilder log)
        {
            var handle = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            var rack = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            var rim = handle != null ? handle.transform.Find("Rim Grab") : null;
            if (handle == null || rack == null || rim == null || handle.island == null || handle.island.map == null)
            {
                log.Append("\n  rim zones: NO island handle / pod rack / 'Rim Grab'");
                return;
            }
            if (!rack.SocketBandLocal(out float bandMin, out float bandMax, out _))
            {
                log.Append("\n  rim zones: no socket row found");
                return;
            }
            var map = handle.island.map;
            float halfX = map.Columns * map.tileSize * 0.5f, halfZ = map.Rows * map.tileSize * 0.5f;
            float depth = Step2QuestSetup.RimInset + Step2QuestSetup.RimOutset;
            float height = Step2QuestSetup.RimTop - Step2QuestSetup.RimBottom, y = (Step2QuestSetup.RimTop + Step2QuestSetup.RimBottom) * 0.5f;
            float edge = (Step2QuestSetup.RimOutset - Step2QuestSetup.RimInset) * 0.5f;

            foreach (var old in rim.GetComponents<BoxCollider>()) Undo.DestroyObjectImmediate(old);
            var boxes = new System.Collections.Generic.List<Collider>();
            void Box(Vector3 center, Vector3 size)
            {
                var box = Undo.AddComponent<BoxCollider>(rim.gameObject);
                box.center = center;
                box.size = size;
                boxes.Add(box);
            }
            float outer = halfX + depth * 0.5f;
            Box(new Vector3(0f, y, -halfZ - edge), new Vector3(outer * 2f, height, depth));
            Box(new Vector3(halfX + edge, y, 0f), new Vector3(depth, height, halfZ * 2f + depth));
            Box(new Vector3(-halfX - edge, y, 0f), new Vector3(depth, height, halfZ * 2f + depth));
            // Front corners, at least 3 cm (island-local) each so there is always something to hold.
            const float minCorner = 0.03f;
            float leftIn = Mathf.Min(bandMax, outer - minCorner), rightIn = Mathf.Max(bandMin, -outer + minCorner);
            Box(new Vector3((leftIn + outer) * 0.5f, y, halfZ + edge), new Vector3(outer - leftIn, height, depth));
            Box(new Vector3((rightIn - outer) * 0.5f, y, halfZ + edge), new Vector3(rightIn + outer, height, depth));

            Undo.RecordObject(handle, "Step 8c rim zones");
            handle.rimColliders = boxes.ToArray();
            EditorUtility.SetDirty(handle);
            log.Append($"\n  rim grab: back + sides + front corners (front {(-outer):0.000}..{rightIn:0.000} and {leftIn:0.000}..{outer:0.000}); " +
                       $"no grab over the pod strip {bandMin:0.000}..{bandMax:0.000} (island-local x)");
        }
    }
}
