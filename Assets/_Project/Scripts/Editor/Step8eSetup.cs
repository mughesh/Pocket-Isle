using System.Collections.Generic;
using System.Text;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8e (hands and modes): runs the 8d setup first, then the island's grab shell (pinch or palm), the Explore /
    /// Pods active wiring, the ghost side values and the 8e starter pod values. Safe to re-run.
    /// </summary>
    static class Step8eSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        // The grab shell, island-local metres: from 1 cm inside the rock face to 2.5 cm out, cap edge down to the bottom.
        const float ShellIn = 0.01f, ShellOut = 0.025f, ShellTop = 0f, ShellBottom = -0.13f;
        const float MinCorner = 0.03f;

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (8e hands and modes)", priority = 49)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8e] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            Step8dSetup.SetUp();
            var log = new StringBuilder("[Step8e] MR_Main set up:");

            Shell(log);

            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            if (opening != null)
            {
                Undo.RecordObject(opening, "Step 8e");
                opening.podScale = 2.4f;
                opening.podFocusGrow = 1.25f;
                opening.podArc = new Vector3(0.7f, 0.1f, 0.25f);
                opening.podArcBow = 0.06f;
                opening.podMinGap = 0.4f;
                EditorUtility.SetDirty(opening);
                log.Append($"\n  starter pods: over the island on an arc {opening.podArc.x:0.0}x its width, {opening.podArc.y * 100f:0} cm up, " +
                           $"at most x{opening.podScale} (smaller if they don't fit), looked-at x{opening.podFocusGrow}");
            }

            var ghosts = Object.FindFirstObjectByType<GhostHands>(FindObjectsInactive.Include);
            if (ghosts != null)
            {
                Undo.RecordObject(ghosts, "Step 8e");
                ghosts.sideDrop = 0.04f;
                ghosts.sideOut = 0.08f;
                EditorUtility.SetDirty(ghosts);
                log.Append("\n  ghosts: the move lesson and the two-hands tip pinch the base's sides (the recording turned a quarter toward each side)");
            }

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void Shell(StringBuilder log)
        {
            var handle = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            var rack = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            var rim = handle != null ? handle.transform.Find("Rim Grab") : null;
            if (handle == null || rack == null || rim == null || handle.island == null || handle.island.map == null)
            {
                log.Append("\n  grab shell: NO island handle / pod rack / 'Rim Grab'");
                return;
            }
            if (!rack.SocketBandLocal(out float bandMin, out float bandMax, out _))
            {
                log.Append("\n  grab shell: no socket row found");
                return;
            }
            var map = handle.island.map;
            float halfX = map.Columns * map.tileSize * 0.5f, halfZ = map.Rows * map.tileSize * 0.5f;
            float depth = ShellIn + ShellOut, mid = (ShellOut - ShellIn) * 0.5f;
            float h = ShellTop - ShellBottom, y = (ShellTop + ShellBottom) * 0.5f;
            float outer = halfX + ShellOut;

            foreach (var old in rim.GetComponents<BoxCollider>()) Undo.DestroyObjectImmediate(old);
            var boxes = new List<Collider>();
            BoxCollider Box(Vector3 center, Vector3 size)
            {
                var box = Undo.AddComponent<BoxCollider>(rim.gameObject);
                box.isTrigger = true;
                box.center = center;
                box.size = size;
                boxes.Add(box);
                return box;
            }
            Box(new Vector3(0f, y, -halfZ - mid), new Vector3(outer * 2f, h, depth));
            Box(new Vector3(halfX + mid, y, 0f), new Vector3(depth, h, halfZ * 2f + ShellOut * 2f));
            Box(new Vector3(-halfX - mid, y, 0f), new Vector3(depth, h, halfZ * 2f + ShellOut * 2f));
            // Front: two corners (always) and the pod strip between them (exploring only).
            float leftIn = Mathf.Min(bandMax, outer - MinCorner), rightIn = Mathf.Max(bandMin, -outer + MinCorner);
            Box(new Vector3((leftIn + outer) * 0.5f, y, halfZ + mid), new Vector3(outer - leftIn, h, depth));
            Box(new Vector3((rightIn - outer) * 0.5f, y, halfZ + mid), new Vector3(rightIn + outer, h, depth));
            var strip = Box(new Vector3((leftIn + rightIn) * 0.5f, y, halfZ + mid), new Vector3(leftIn - rightIn, h, depth));

            Undo.RecordObject(handle, "Step 8e grab shell");
            handle.rimColliders = boxes.ToArray();
            handle.podStripColliders = new Collider[] { strip };
            handle.pods = rack;
            handle.encounters = Object.FindFirstObjectByType<EncounterDirector>(FindObjectsInactive.Include);
            EditorUtility.SetDirty(handle);

            var grab = rim.GetComponent<HandGrabInteractable>();
            if (grab != null)
            {
                Undo.RecordObject(grab, "Step 8e pinch and palm");
                grab.InjectSupportedGrabTypes(GrabTypeFlags.Pinch | GrabTypeFlags.Palm);
                EditorUtility.SetDirty(grab);
            }
            log.Append($"\n  grab shell: {ShellIn * 100f:0} cm inside to {ShellOut * 100f:0.0} cm outside the base, y {ShellTop:0.00}..{ShellBottom:0.00}; back, sides, front corners " +
                       $"(x beyond {rightIn:0.000} / {leftIn:0.000}), the pod strip between them only while exploring; " +
                       $"{(grab != null ? "PINCH or PALM grab" : "NO HandGrabInteractable")}; modes from {(handle.encounters != null ? "the encounters" : "NO ENCOUNTERS")} and the pod rack");
        }
    }
}
