using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11c A: the water roster (<see cref="WaterRosterKit"/>), baked, registered, turnaround shots
    /// (Logs/BattleShots/11c_&lt;name&gt;.png), a lineup with the Kilnpot and Pebbloop lines for scale, and Lilymere's tables
    /// rewritten with them (~70% water, ~20% wind visitors, the rare pond's Bottlewhale ~5%). Bridge:
    /// exec:Diorama.EditorTools.Step11cSetup.Roster (or a line: Marimoss, Drizzcan, Lilyvole, Sailfold, Bottlewhale).
    /// </summary>
    public static class Step11cSetup
    {
        [MenuItem("Tools/Pocket Isle/Set Up Water Roster (11c)", priority = 71)]
        public static void Roster()
        {
            Marimoss();
            Drizzcan();
            Lilyvole();
            Sailfold();
            Bottlewhale();
            Tables();
            Lineup();
        }

        static void Build(string what, params IslingSpecies[] species)
        {
            var log = new StringBuilder($"[Step11c] {what}:");
            foreach (var s in species)
            {
                if (s == null || s.recipe == null)
                {
                    log.Append("\n  MISSING species");
                    continue;
                }
                var r = CreatureBaker.Bake(s.recipe);
                log.Append($"\n  {s.name}: {r}");
                IslingShots.Turnaround(s, $"{Step9cSetup.ShotDir}/11c_{s.name.ToLowerInvariant()}.png");
            }
            Step7Setup.Registry();
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void Line(string what, IslingSpecies s) => Build(what, s, s != null ? s.evolvesTo : null);

        public static void Marimoss() => Line("Marimoss -> Marimound", WaterRosterKit.Marimoss());
        public static void Drizzcan() => Line("Drizzcan -> Rainurn", WaterRosterKit.Drizzcan());
        public static void Lilyvole() => Line("Lilyvole -> Lotusvole", WaterRosterKit.Lilyvole());
        public static void Sailfold() => Line("Sailfold -> Galleyfold", WaterRosterKit.Sailfold());
        public static void Bottlewhale() => Line("Bottlewhale -> Lighthousewhale (rare)", WaterRosterKit.Bottlewhale());

        /// <summary>Lilymere's tables with the water roster (rewrites the map's tables; layout unchanged, same seed).</summary>
        public static void Tables()
        {
            var log = new StringBuilder("[Step11c] Lilymere tables:");
            var map = LilymereTools.Write(log);
            if (map == null) return;
            var problems = LilymereTools.Check(map, log);
            foreach (var p in problems) log.Append($"\n  PROBLEM: {p}");
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            DialogueTools.Import();
            log.Append("\n  Script_v1 reimported (lm.meet.sailfold, lm.meet.bottlewhale)");
            Debug.Log(log.ToString());
        }

        /// <summary>The water Islings in a row with the Kilnpot and Pebbloop lines for scale: Logs/BattleShots/11c_lineup.png.</summary>
        public static void Lineup()
        {
            var names = new[] { "Kilnpot", "Kilnhearth", "Pebbloop", "Riverloom", "Marimoss", "Marimound", "Drizzcan", "Rainurn", "Lilyvole", "Lotusvole",
                "Sailfold", "Galleyfold", "Bottlewhale", "Lighthousewhale" };
            var preview = EditorSceneManager.NewPreviewScene();
            const int w = 2400, h = 700;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            try
            {
                var camGo = new GameObject("Lineup Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.8f, 0.82f, 0.83f);
                cam.orthographic = true;
                cam.stereoTargetEye = StereoTargetEyeMask.None;
                var lightGo = new GameObject("Lineup Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);
                float x = 0f, top = 0f;
                var placed = new List<GameObject>();
                foreach (var n in names)
                {
                    var s = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{n}.asset");
                    if (s == null || s.Prefab == null) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(s.Prefab, preview);
                    go.transform.localScale = Vector3.one * 8f;
                    go.transform.rotation = Quaternion.Euler(0f, -25f, 0f);
                    go.GetComponent<CreatureView>().ApplyStyles();
                    var b = new Bounds(go.transform.position, Vector3.zero);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    go.transform.position = new Vector3(x + b.extents.x - (b.center.x - go.transform.position.x), -b.min.y, 0f);
                    x += b.size.x + 0.08f;
                    top = Mathf.Max(top, b.size.y);
                    placed.Add(go);
                }
                float width = x;
                cam.orthographicSize = Mathf.Max(top * 0.6f, width / (2f * w / h));
                cam.transform.position = new Vector3(width * 0.5f, cam.orthographicSize * 0.92f, 4f);
                cam.transform.rotation = Quaternion.LookRotation(Vector3.back);
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Directory.CreateDirectory(Step9cSetup.ShotDir);
                File.WriteAllBytes($"{Step9cSetup.ShotDir}/11c_lineup.png", tex.EncodeToPNG());
                Debug.Log($"[Step11c] Lineup of {placed.Count}: {Step9cSetup.ShotDir}/11c_lineup.png");
            }
            finally
            {
                Object.DestroyImmediate(tex);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
