using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 4 scene tools: the Creature Lab scene, turnaround shots of every species, the 3 starters and the wild
    /// creatures on the island in MR_Main, and a draw call / triangle report.
    /// </summary>
    public static class CreatureTools
    {
        const string LabPath = "Assets/_Project/Scenes/CreatureLab.unity";
        const string MainPath = "Assets/_Project/Scenes/MR_Main.unity";
        const string ShotDir = "Logs/CreatureShots"; // Temp/ is wiped when Unity quits; Logs/ is not in git
        static readonly string[] Starters = { "Kilnpot", "Pebbloop", "Pufflet" };
        // Lab cycle and shot order: starters, then each line base -> evolution. Species not listed come last.
        static readonly string[] LabOrder =
            { "Kilnpot", "Pebbloop", "Pufflet", "Kilnhearth", "Toastling", "Crumbhog", "Waxle", "Lanternsnail", "Kettlewing", "Glazegolem", "Kilngolem" };
        // Wild wanderers on the test world: species and home tile (column, row), well away from the start tile (c28 r27).
        static readonly (string name, Vector2Int tile)[] Wild =
            { ("Toastling", new Vector2Int(19, 16)), ("Waxle", new Vector2Int(41, 45)), ("Kettlewing", new Vector2Int(44, 30)) };
        static readonly string[] FireRecipes = { "Toastling", "Crumbhog", "Waxle", "Lanternsnail", "Kettlewing", "Glazegolem", "Kilngolem" };

        // ---- Creature Lab ----

        [MenuItem("Tools/Pocket Isle/Creatures/Build Creature Lab Scene", priority = 40)]
        public static void BuildLab()
        {
            if (!SaveOrAbort()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.8f, 0.82f, 0.83f);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.05f;
            camGo.transform.position = new Vector3(0f, 0.3f, 1.45f);
            camGo.transform.LookAt(new Vector3(0f, 0.19f, 0f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);

            var lab = new GameObject("Creature Lab").AddComponent<CreatureLab>();
            var turntable = new GameObject("Turntable").transform;
            turntable.SetParent(lab.transform, false);
            lab.turntable = turntable;
            lab.species = AllSpecies();

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plinth.name = "Plinth";
            Object.DestroyImmediate(plinth.GetComponent<Collider>());
            plinth.transform.SetParent(turntable, false);
            plinth.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            plinth.transform.localScale = new Vector3(0.6f, 0.01f, 0.6f);
            plinth.GetComponent<MeshRenderer>().sharedMaterial = DioramaSceneSetup.ToonMaterial("Lab_Plinth", "#A9B3A1");

            DioramaSceneSetup.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, LabPath);
            Debug.Log($"[CreatureTools] Creature Lab saved to {LabPath} with {lab.species.Count} species. Press Play; Space cycles.");
        }

        static List<CreatureSpecies> AllSpecies() =>
            AssetDatabase.FindAssets("t:CreatureSpecies")
                .Select(g => AssetDatabase.LoadAssetAtPath<CreatureSpecies>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => System.Array.IndexOf(LabOrder, s.name) is int i && i >= 0 ? i : 99)
                .ThenBy(s => s.name)
                .ToList();

        /// <summary>Points the saved Creature Lab at every species (opened additively, so the open scene stays put).</summary>
        public static void RefreshLabSpecies()
        {
            var lab = EditorSceneManager.GetSceneByPath(LabPath);
            bool opened = !lab.isLoaded;
            if (opened) lab = EditorSceneManager.OpenScene(LabPath, OpenSceneMode.Additive);
            foreach (var root in lab.GetRootGameObjects())
                foreach (var l in root.GetComponentsInChildren<CreatureLab>(true))
                {
                    l.species = AllSpecies();
                    EditorUtility.SetDirty(l);
                    Debug.Log($"[CreatureTools] Creature Lab now cycles {l.species.Count} species.");
                }
            EditorSceneManager.MarkSceneDirty(lab);
            EditorSceneManager.SaveScene(lab);
            if (opened) EditorSceneManager.CloseScene(lab, true);
        }

        // ---- shots ----

        [MenuItem("Tools/Pocket Isle/Creatures/Save Turnaround Shots", priority = 41)]
        /// <summary>
        /// Front, side and three-quarter shots of every species to Logs/CreatureShots, rendered in an isolated preview
        /// scene (the open scene is left alone). Same camera and light as the Creature Lab.
        /// </summary>
        public static void CaptureShots()
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var camGo = new GameObject("Shot Camera");
                SceneManagerMove(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                // A camera with its own preview scene renders only that scene (the island skips it too).
                cam.scene = preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.8f, 0.82f, 0.83f);
                cam.fieldOfView = 24f;
                cam.nearClipPlane = 0.05f;
                camGo.transform.position = new Vector3(0f, 0.3f, 1.45f);
                camGo.transform.LookAt(new Vector3(0f, 0.19f, 0f));

                var lightGo = new GameObject("Shot Light");
                SceneManagerMove(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);

                var turntable = new GameObject("Turntable").transform;
                SceneManagerMove(turntable.gameObject, preview);

                Directory.CreateDirectory(ShotDir);
                var saved = new List<string>();
                foreach (var s in AllSpecies())
                {
                    if (s.Prefab == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(s.Prefab, preview);
                    go.transform.SetParent(turntable, false);
                    go.transform.localScale = Vector3.one * 8f;
                    go.GetComponent<CreatureView>().ApplyStyles();
                    Frame(cam, go);
                    foreach (var (view, yaw) in new[] { ("front", 0f), ("side", -90f), ("threequarter", -35f) })
                    {
                        turntable.localRotation = Quaternion.Euler(0f, yaw, 0f);
                        string path = $"{ShotDir}/{s.name}_{view}.png";
                        Render(cam, path, 900, 900);
                        saved.Add(path);
                    }
                    Object.DestroyImmediate(go);
                }
                Debug.Log($"[CreatureTools] Saved {saved.Count} shots to {Path.GetFullPath(ShotDir)}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // Same view direction as the Creature Lab camera; backs off so tall or long creatures fit at any yaw.
        static void Frame(Camera cam, GameObject go)
        {
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var rend in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(rend.bounds);
            float radius = new Vector2(Mathf.Max(b.extents.x, b.extents.z), b.extents.y).magnitude;
            float fit = radius * 1.1f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 target = new Vector3(0f, Mathf.Max(0.19f, b.center.y), 0f);
            Vector3 dir = new Vector3(0f, 0.11f, 1.45f).normalized;
            cam.transform.position = target + dir * Mathf.Max(1.46f, fit);
            cam.transform.LookAt(target);
        }

        static void SceneManagerMove(GameObject go, UnityEngine.SceneManagement.Scene scene) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        public static void Render(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
        }

        // ---- MR_Main ----

        [MenuItem("Tools/Pocket Isle/Creatures/Add Starters to MR_Main", priority = 42)]
        public static void AddStartersToMain()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainPath)
            {
                if (!SaveOrAbort()) return;
                scene = EditorSceneManager.OpenScene(MainPath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (island == null || island.world == null)
            {
                Debug.LogError("[CreatureTools] MR_Main needs the island with a world map (Step 3 menus).");
                return;
            }

            var world = island.world;
            // Ahead of the explorer (forward = row - 1 = away from the player), spread left to right.
            Vector2Int[] wanted = { new Vector2Int(2, -2), new Vector2Int(0, -3), new Vector2Int(-2, -2) };
            var taken = new HashSet<Vector2Int> { world.startTile };
            var report = new StringBuilder("[CreatureTools] Starters in MR_Main:");
            for (int i = 0; i < Starters.Length; i++)
            {
                var species = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"Assets/_Project/Data/Creatures/Species/{Starters[i]}.asset");
                if (species == null || species.Prefab == null)
                {
                    Debug.LogError($"[CreatureTools] {Starters[i]} is not baked yet (Bake All first).");
                    return;
                }
                var old = island.transform.Find(Starters[i]);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var tile = NearestWalkable(world, world.startTile + wanted[i], taken);
                taken.Add(tile);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(species.Prefab, island.transform);
                go.name = Starters[i];
                var mc = go.AddComponent<MapCreature>();
                mc.island = island;
                mc.explorer = explorer;
                mc.homeTile = tile;
                mc.SnapToMap();
                report.Append($"\n  {Starters[i]} on c{tile.x} r{tile.y} ('{world.At(tile.y, tile.x)}')");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(report.ToString());
        }

        /// <summary>Puts the wild wanderers (MapCreature, wander radius 3) on the test world, far from the start tile.</summary>
        [MenuItem("Tools/Pocket Isle/Creatures/Add Wild Creatures to MR_Main", priority = 42)]
        public static void AddWildToMain()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainPath)
            {
                if (!SaveOrAbort()) return;
                scene = EditorSceneManager.OpenScene(MainPath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var explorer = Object.FindFirstObjectByType<GestureTestPiece>();
            if (island == null || island.world == null)
            {
                Debug.LogError("[CreatureTools] MR_Main needs the island with a world map (Step 3 menus).");
                return;
            }
            var world = island.world;
            var taken = new HashSet<Vector2Int> { world.startTile };
            foreach (var c in Object.FindObjectsByType<MapCreature>(FindObjectsSortMode.None))
                if (System.Array.IndexOf(Starters, c.name) >= 0) taken.Add(c.homeTile);
            var report = new StringBuilder("[CreatureTools] Wild creatures in MR_Main:");
            foreach (var (name, want) in Wild)
            {
                var species = AssetDatabase.LoadAssetAtPath<CreatureSpecies>($"Assets/_Project/Data/Creatures/Species/{name}.asset");
                if (species == null || species.Prefab == null)
                {
                    Debug.LogError($"[CreatureTools] {name} is not baked yet (Bake All first).");
                    return;
                }
                var old = island.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var tile = NearestWalkable(world, want, taken);
                taken.Add(tile);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(species.Prefab, island.transform);
                go.name = name;
                var mc = go.AddComponent<MapCreature>();
                mc.island = island;
                mc.explorer = explorer;
                mc.homeTile = tile;
                mc.wanderRadius = 3;
                mc.SnapToMap();
                var d = tile - world.startTile;
                report.Append($"\n  {name} on c{tile.x} r{tile.y} ('{world.At(tile.y, tile.x)}'), {Mathf.Abs(d.x) + Mathf.Abs(d.y)} tiles from the start");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(report.ToString());
        }

        static Vector2Int NearestWalkable(IslandMap map, Vector2Int want, HashSet<Vector2Int> taken)
        {
            for (int r = 0; r < 6; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var t = want + new Vector2Int(dx, dy);
                        if (IslandMap.IsWalkable(map.At(t.y, t.x)) && !taken.Contains(t)) return t;
                    }
            return want;
        }

        // ---- report ----

        [MenuItem("Tools/Pocket Isle/Creatures/Draw Call Report (MR_Main)", priority = 43)]
        public static void DrawCallReport()
        {
            if (EditorSceneManager.GetActiveScene().path != MainPath)
            {
                if (!SaveOrAbort()) return;
                EditorSceneManager.OpenScene(MainPath);
            }
            var scene = EditorSceneManager.GetActiveScene();
            bool wasDirty = scene.isDirty;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var creatures = Object.FindObjectsByType<MapCreature>(FindObjectsSortMode.None);
            if (island == null || island.world == null) { Debug.LogError("[CreatureTools] No island with a world in MR_Main."); return; }
            var poses = creatures.Select(c => (c.transform.localPosition, c.transform.localRotation, c.transform.localScale)).ToArray();

            var camGo = new GameObject("Report Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.35f, 0.38f, 0.42f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.02f;
            var it = island.transform;
            camGo.transform.position = it.TransformPoint(new Vector3(0f, 0.3f, 0.5f));
            camGo.transform.LookAt(it.TransformPoint(new Vector3(0f, 0.02f, -0.06f)));

            // Hide everything except the island and its creatures (the rig's hand meshes sit near it in edit mode).
            var hidden = new List<Renderer>();
            foreach (var rend in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (rend.enabled && !rend.transform.IsChildOf(it))
                {
                    rend.enabled = false;
                    hidden.Add(rend);
                }

            Vector2Int start = island.world.startTile;
            Vector2 center = start + new Vector2(0.5f, 0.5f);
            Directory.CreateDirectory(ShotDir);
            var sb = new StringBuilder("[CreatureTools] Draw calls in MR_Main (island + creatures; excludes explorer, label, shadow, hands):");
            try
            {
                foreach (float zoom in new[] { 1f, 3f })
                {
                    island.SetView(center, start, zoom);
                    foreach (var c in creatures)
                    {
                        c.SnapToMap();
                        var view = c.GetComponent<CreatureView>();
                        view.SetWindowClip(true);
                        view.ApplyStyles(); // window origin = where it stands now
                    }
                    Render(cam, $"{ShotDir}/MR_Main_zoom{zoom:0}.png", 1200, 900);
                    int creatureDraws = 0, creatureTris = 0, shown = 0;
                    foreach (var c in creatures)
                    {
                        var view = c.GetComponent<CreatureView>();
                        if (view.rig == null || !view.rig.gameObject.activeSelf) continue;
                        shown++;
                        foreach (var rend in view.GetComponentsInChildren<MeshRenderer>())
                        {
                            var mesh = rend.GetComponent<MeshFilter>().sharedMesh;
                            creatureDraws += rend.sharedMaterials.Length;
                            if (mesh != null) creatureTris += mesh.triangles.Length / 3 * rend.sharedMaterials.Length;
                        }
                    }
                    sb.Append($"\n  zoom x{zoom:0}: island {island.DrawCallCount} draws ({island.InstanceTotal} instances), " +
                              $"creatures {creatureDraws} draws for {shown} shown ({creatureTris} tris incl. outlines), total {island.DrawCallCount + creatureDraws}");
                }
            }
            finally
            {
                island.SetView(center, start, 1f);
                for (int i = 0; i < creatures.Length; i++)
                {
                    var t = creatures[i].transform;
                    (t.localPosition, t.localRotation, t.localScale) = poses[i];
                    var rig = creatures[i].GetComponent<CreatureView>().rig;
                    if (rig != null) rig.gameObject.SetActive(true);
                }
                foreach (var rend in hidden) rend.enabled = true;
                Object.DestroyImmediate(camGo);
                if (!wasDirty) EditorSceneManager.SaveScene(scene); // everything is back as it was; don't leave it marked dirty
            }
            Debug.Log(sb.ToString());
        }

        static bool SaveOrAbort()
        {
            if (Application.isBatchMode) return true;
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        // ---- batch (command line) ----

        /// <summary>
        /// Everything for step 4 in one go, for the command line:
        /// Unity -batchmode -projectPath . -executeMethod Diorama.EditorTools.CreatureTools.BatchStep4 -quit
        /// </summary>
        public static void BatchStep4()
        {
            StarterKit.Write();
            CreatureBaker.BakeAll();
            BuildLab();
            CaptureShots();
            AddStartersToMain();
            DrawCallReport();
        }

        /// <summary>
        /// Step 4b in one go (bridge: exec:Diorama.EditorTools.CreatureTools.BatchFire): writes the fire kit, bakes the
        /// 7 fire creatures (starters untouched), refreshes the Lab list, saves shots, adds the wanderers, reports.
        /// </summary>
        public static void BatchFire()
        {
            FireKit.Write();
            BakeFire();
            RefreshLabSpecies();
            CaptureShots();
            AddWildToMain();
            DrawCallReport();
        }

        /// <summary>Fire kit rewrite + bake + shots only (after tweaking FireKit.cs).</summary>
        public static void BatchFireShots()
        {
            FireKit.Write();
            BakeFire();
            CaptureShots();
        }

        static void BakeFire()
        {
            var sb = new StringBuilder("[CreatureTools] Baked fire creatures:");
            foreach (var name in FireRecipes)
            {
                var r = AssetDatabase.LoadAssetAtPath<CreatureRecipe>($"Assets/_Project/Data/Creatures/Recipes/{name}.asset");
                if (r != null) sb.Append("\n  ").Append(CreatureBaker.Bake(r));
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Rebake + shots only (after recipe tweaks), for the command line.</summary>
        public static void BatchRebakeShots()
        {
            StarterKit.Write();
            CreatureBaker.BakeAll();
            CaptureShots();
        }
    }

    [CustomEditor(typeof(CreatureLab))]
    sealed class CreatureLabEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var lab = (CreatureLab)target;
            EditorGUILayout.Space();
            var species = lab.CurrentSpecies;
            using (new EditorGUI.DisabledScope(species == null || species.recipe == null))
            {
                if (GUILayout.Button($"Rebake & reload {(species != null ? species.displayName : "")}", GUILayout.Height(28)))
                {
                    var recipe = species.recipe;
                    if (recipe.baseRecipe != null) CreatureBaker.Bake(recipe.baseRecipe);
                    CreatureBaker.Bake(recipe);
                    if (Application.isPlaying) lab.Reload();
                }
            }
            if (GUILayout.Button("Bake all & reload"))
            {
                CreatureBaker.BakeAll();
                if (Application.isPlaying) lab.Reload();
            }
            if (species != null && species.recipe != null && GUILayout.Button("Select recipe"))
                Selection.activeObject = species.recipe;
            EditorGUILayout.HelpBox("Play mode keys: Space / arrows cycle, T turntable, H hop, F faint, 1 / 2 / 3 front / side / three-quarter.", MessageType.None);
        }
    }
}
