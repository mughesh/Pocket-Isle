using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Mugesh's Galewick notes, Oct 27 (Assets/Docs/Notes.md). Scene side: the big mill's lesson shows a finger flick
    /// ghost (was the pod grab-and-flick), the cloud bank's a palm sweep (was a palm hover) with the chip "Sweep!".
    /// Props and map: <see cref="All"/> also rewrites and bakes the Galewick props (the new diamond kites) and the map
    /// (straight paths, the square kite loop) and re-imports the script. Safe to re-run.
    /// Bridge: exec:Diorama.EditorTools.FeedbackOct27Setup.All
    /// </summary>
    public static class FeedbackOct27Setup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";

        [MenuItem("Tools/Pocket Isle/Apply Oct 27 Galewick Feedback", priority = 64)]
        public static void All()
        {
            Step10aSetup.PropsOnly();
            Step10aSetup.MapOnly();
            DialogueTools.Import();
            Scene();
        }

        /// <summary>The three kites stood up as they fly (2.4 tiles), close, over the island: Logs/BattleShots/oct27_kites.png.</summary>
        public static void KiteShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.map == null) return;
            var views = new System.Collections.Generic.List<PropView>();
            try
            {
                float tileM = island.map.tileSize;
                for (int i = 0; i < PropKit.KiteNames.Length; i++)
                {
                    var recipe = AssetDatabase.LoadAssetAtPath<PropRecipe>($"Assets/_Project/Data/Props/{PropKit.KiteNames[i]}.asset");
                    if (recipe == null) continue;
                    var view = new PropView($"Kite shot {i}", island, recipe) { restoredSelf = 1f };
                    var rot = Quaternion.Euler(0f, 0f, (i - 1) * 12f) * Quaternion.Euler(75f, 0f, 0f);
                    view.PlaceLocal(new Vector3((1 - i) * 0.14f, island.CapTopLocal + 0.2f + (i == 1 ? 0.04f : 0f), 0.1f), rot, tileM * 2.4f, true);
                    views.Add(view);
                }
                var t = island.transform;
                var look = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.2f, 0.1f));
                var eye = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.24f, 0.55f));
                BattleDebug.Render("Logs/BattleShots/oct27_kites.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            }
            finally
            {
                // Edit mode: PropView.Destroy (Object.Destroy) would leave them in the scene.
                foreach (var v in views)
                    if (v.transform != null) Object.DestroyImmediate(v.transform.gameObject);
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (t != null && t.name.StartsWith("Kite shot ")) Object.DestroyImmediate(t.gameObject);
            }
            Debug.Log("[FeedbackOct27] Logs/BattleShots/oct27_kites.png");
        }

        /// <summary>Play: the two new ghost clips frozen at their start and end, Logs/BattleShots/oct27_ghost_{clip}_{u}.png.</summary>
        public static void GhostShots()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (g == null || island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0.04f, 0.3f, 0.45f);
            var look = t.position + t.rotation * new Vector3(0f, 0.1f, 0f);
            foreach (var (clip, us) in new[] { ("finger_flick", new[] { 0.3f, 0.42f }), ("palm_sweep", new[] { 0.22f, 0.45f }) })
            {
                g.Show(clip, GhostPlace.AboveIsland, null, 1f, false, false);
                foreach (float u in us)
                {
                    g.debugFreeze = u;
                    g.DebugPose();
                    BattleDebug.Render($"Logs/BattleShots/oct27_ghost_{clip}_{u:0.00}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                }
            }
            g.debugFreeze = -1f;
            Debug.Log("[FeedbackOct27] Ghost shots in Logs/BattleShots/oct27_ghost_*.png");
        }

        /// <summary>Play: every visible renderer whose name has "Kite" in it, with its island-local position (debug).</summary>
        public static void ListKites()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var sb = new StringBuilder("[FeedbackOct27] Kite renderers:");
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.name.IndexOf("Kite", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var local = island != null ? island.transform.InverseTransformPoint(r.transform.position) : r.transform.position;
                sb.Append($"\n  {r.name} ({r.GetType().Name}) local {local}");
            }
            Debug.Log(sb.ToString());
        }

        public static void Scene()
        {
            var log = new StringBuilder("[FeedbackOct27] Scene:");
            int mills = 0;
            foreach (var wheel in Object.FindObjectsByType<WheelPiece>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (wheel.flicksNeeded <= 0) continue;
                Undo.RecordObject(wheel, "Oct 27 feedback");
                wheel.ghostClip = "finger_flick";
                EditorUtility.SetDirty(wheel);
                mills++;
            }
            log.Append($"\n  {mills} puzzle mill(s): ghost finger_flick.");
            var bank = Object.FindFirstObjectByType<CloudBankPiece>(FindObjectsInactive.Include);
            if (bank != null)
            {
                Undo.RecordObject(bank, "Oct 27 feedback");
                bank.ghostClip = "palm_sweep";
                bank.lessonChip = "Sweep!";
                EditorUtility.SetDirty(bank);
                log.Append("\n  Cloud bank: ghost palm_sweep, chip \"Sweep!\".");
            }
            else log.Append("\n  MISSING CloudBankPiece (Set Up MR_Main (10b Galewick hands)).");

            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path == ScenePath && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.Append("\n  Scene saved.");
            }
            else log.Append($"\n  Scene NOT saved (active: {scene.path}, playing {EditorApplication.isPlaying}).");
            Debug.Log(log.ToString());
        }
    }
}
