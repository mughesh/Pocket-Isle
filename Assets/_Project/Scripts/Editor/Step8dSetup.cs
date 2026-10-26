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
    /// Step 8d (grab and touch fixes): runs the 8c setup first, then the island's palm-grab zone on the base skirt, the
    /// grab frame, the dev readout, and the 8d values (shard contact, palm over the flame, starter pods). Safe to re-run.
    /// </summary>
    static class Step8dSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string FrameMaterialPath = "Assets/_Project/Materials/Island/GrabFrame.mat";
        const string DevMaterialPath = "Assets/_Project/Materials/Onboarding/DevLines.mat";
        // The base skirt, island-local metres: from the cap's edge down to below the deepest edge column.
        const float SkirtTop = 0f, SkirtBottom = -0.13f;

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (8d grab and touch)", priority = 48)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step8d] Stop Play mode first.");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            Step8cSetup.SetUp();
            var log = new StringBuilder("[Step8d] MR_Main set up:");

            SkirtZone(log);
            GrabFrame(log);
            Readout(log);

            var opening = Object.FindFirstObjectByType<Opening>(FindObjectsInactive.Include);
            if (opening != null)
            {
                Undo.RecordObject(opening, "Step 8d");
                opening.contactSlack = 0.015f;
                opening.podScale = 2.4f;
                opening.podFocusGrow = 1.3f;
                opening.podArc = new Vector3(1.2f, 0.1f, 0.26f);
                opening.podArcBow = 0.06f;
                opening.podMinGap = 0.4f;
                EditorUtility.SetDirty(opening);
                log.Append($"\n  shard: contact within {opening.contactSlack * 100f:0.0} cm of its surface (convex collider); starter pods x{opening.podScale} on an arc " +
                           $"{opening.podArc.x:0.0}x the island wide, {opening.podArc.y * 100f:0} cm up, looked-at x{opening.podFocusGrow}");
            }

            var shrines = Object.FindFirstObjectByType<HearthShrines>(FindObjectsInactive.Include);
            if (shrines != null)
            {
                Undo.RecordObject(shrines, "Step 8d");
                shrines.palm = new PalmHover();
                EditorUtility.SetDirty(shrines);
            }
            var lodge = Object.FindFirstObjectByType<LodgeRoom>(FindObjectsInactive.Include);
            if (lodge != null)
            {
                Undo.RecordObject(lodge, "Step 8d");
                lodge.palm = new PalmHover();
                EditorUtility.SetDirty(lodge);
            }
            var p = new PalmHover();
            log.Append($"\n  palm over the flame / door arch: palm joint facing > {p.facing}, {p.height.x * 100f:0}-{p.height.y * 100f:0} cm above, " +
                       $"within {p.radius * 100f:0} cm (scaled with the island){(shrines == null || lodge == null ? " - SHRINES OR LODGE MISSING" : "")}");

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// The island's grab zone: the rocky base skirt all around (palm grab only). Back and sides from the cap's edge
        /// down to the bottom; the front only below the pod band, so reaching for a pod never meets it.
        /// </summary>
        static void SkirtZone(StringBuilder log)
        {
            var handle = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            var rack = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            var rim = handle != null ? handle.transform.Find("Rim Grab") : null;
            if (handle == null || rim == null || handle.island == null || handle.island.map == null)
            {
                log.Append("\n  grab zone: NO island handle / 'Rim Grab'");
                return;
            }
            var map = handle.island.map;
            float halfX = map.Columns * map.tileSize * 0.5f, halfZ = map.Rows * map.tileSize * 0.5f;
            float depth = Step2QuestSetup.RimInset + Step2QuestSetup.RimOutset;
            float edge = (Step2QuestSetup.RimOutset - Step2QuestSetup.RimInset) * 0.5f;
            float outer = halfX + depth * 0.5f;
            // The pod band's lower edge: the rim pods' centre minus a resting pod's radius and a little room.
            float frontTop = SkirtTop;
            if (rack != null && rack.podPrefab != null) frontTop = map.capHeight - rack.socketDepth - rack.podPrefab.radius - 0.005f;
            else log.Append("\n  grab zone: no pod rack / pod prefab, the front goes up to the cap");

            foreach (var old in rim.GetComponents<BoxCollider>()) Undo.DestroyObjectImmediate(old);
            var boxes = new List<Collider>();
            void Box(Vector3 center, Vector3 size)
            {
                var box = Undo.AddComponent<BoxCollider>(rim.gameObject);
                box.center = center;
                box.size = size;
                boxes.Add(box);
            }
            float h = SkirtTop - SkirtBottom, y = (SkirtTop + SkirtBottom) * 0.5f;
            Box(new Vector3(0f, y, -halfZ - edge), new Vector3(outer * 2f, h, depth));
            Box(new Vector3(halfX + edge, y, 0f), new Vector3(depth, h, halfZ * 2f + depth));
            Box(new Vector3(-halfX - edge, y, 0f), new Vector3(depth, h, halfZ * 2f + depth));
            float fh = frontTop - SkirtBottom;
            Box(new Vector3(0f, (frontTop + SkirtBottom) * 0.5f, halfZ + edge), new Vector3(outer * 2f, fh, depth));

            Undo.RecordObject(handle, "Step 8d grab zone");
            handle.rimColliders = boxes.ToArray();
            EditorUtility.SetDirty(handle);

            var grab = rim.GetComponent<HandGrabInteractable>();
            if (grab != null)
            {
                Undo.RecordObject(grab, "Step 8d palm grab");
                grab.InjectSupportedGrabTypes(GrabTypeFlags.Palm);
                EditorUtility.SetDirty(grab);
            }
            log.Append($"\n  grab zone: base skirt, y {SkirtTop:0.000}..{SkirtBottom:0.000} on the back and sides, front {frontTop:0.000}..{SkirtBottom:0.000} " +
                       $"(below the pod band); {(grab != null ? "PALM grab only (a pinch never moves the island)" : "NO HandGrabInteractable")}");
        }

        static void GrabFrame(StringBuilder log)
        {
            var handle = Object.FindFirstObjectByType<IslandHandle>(FindObjectsInactive.Include);
            if (handle == null) return;
            var frame = Object.FindFirstObjectByType<IslandGrabFrame>(FindObjectsInactive.Include);
            if (frame == null)
            {
                var go = new GameObject("Island Grab Frame");
                Undo.RegisterCreatedObjectUndo(go, "Island Grab Frame");
                frame = go.AddComponent<IslandGrabFrame>();
            }
            Undo.RecordObject(frame, "Step 8d");
            frame.island = handle.island;
            frame.handle = handle;
            frame.material = Material(FrameMaterialPath, "Diorama/Grab Frame");
            frame.height = 0f;
            frame.outset = 0.008f;
            frame.cornerRadius = 0.03f;
            EditorUtility.SetDirty(frame);
            log.Append($"\n  grab frame: rounded outline around the base, fades in within {frame.showDistance * 100f:0} cm of an open hand (replaces the rock brighten)");
        }

        static void Readout(StringBuilder log)
        {
            var readout = Object.FindFirstObjectByType<DevReadout>(FindObjectsInactive.Include);
            if (readout == null)
            {
                var go = new GameObject("Dev Readout");
                Undo.RegisterCreatedObjectUndo(go, "Dev Readout");
                readout = go.AddComponent<DevReadout>();
            }
            Undo.RecordObject(readout, "Step 8d");
            readout.lineMaterial = Material(DevMaterialPath, "Diorama/Dev Lines");
            EditorUtility.SetDirty(readout);
            log.Append("\n  dev readout (editor and development builds only): shard contact, palm over the flame / door; F1 or the dev panel toggles");
        }

        static Material Material(string path, string shaderName)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[Step8d] Shader {shaderName} not found.");
                return null;
            }
            m = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
