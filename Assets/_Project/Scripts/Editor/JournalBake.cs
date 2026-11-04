using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14h: bakes the Isling Journal: every Isling's portrait (front 3/4 view, transparent background) into one
    /// atlas, the portrait material, and <c>Resources/JournalData</c> (Journal order, atlas cells, where each one lives
    /// from the encounter tables). Re-run after adding a species or changing an encounter table.
    /// </summary>
    public static class JournalBake
    {
        public const string DataPath = "Assets/_Project/Data/Resources/JournalData.asset";
        public const string AtlasPath = "Assets/_Project/Data/Journal/JournalPortraits.png";
        public const string MaterialPath = "Assets/_Project/Materials/Portrait.mat";
        const int Cell = 128, Columns = 8, Rows = 8, Render = 256;

        /// <summary>Friendly names for the encounter zones ("kites" -> "kite field").</summary>
        static readonly Dictionary<string, string> Places = new Dictionary<string, string>
        {
            { "forest", "the forest" }, { "field", "the open field" }, { "glade", "the glade" }, { "grove", "the grove" },
            { "springs", "the hot springs" }, { "ruins", "the kiln ruins" }, { "village", "the village" },
            { "arrival", "the arrival meadow" }, { "fire", "the warm field" }, { "kites", "kite field" }, { "ridge", "the ridge" },
            { "wind", "the windy bank" }, { "channel", "the channel" }, { "marsh", "the marsh" }, { "garden", "the lily garden" },
            { "pond", "the pond" },
        };

        [MenuItem("Tools/Pocket Isle/Journal/Bake Journal", priority = 400)]
        public static void Bake()
        {
            var registry = Load<SpeciesRegistry>("t:SpeciesRegistry");
            if (registry == null)
            {
                Debug.LogError("[JournalBake] No SpeciesRegistry.");
                return;
            }
            var order = Journal.Order(registry.species);
            var tables = Tables();
            Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath) ?? ".");
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath) ?? ".");

            // The atlas.
            var atlas = new Texture2D(Cell * Columns, Cell * Rows, TextureFormat.RGBA32, false);
            atlas.SetPixels32(new Color32[atlas.width * atlas.height]);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var cam = Rig(preview, out var turntable);
                for (int i = 0; i < order.Count && i < Columns * Rows; i++)
                {
                    var pixels = Portrait(order[i], cam, turntable, preview);
                    if (pixels == null) continue;
                    int cx = i % Columns, cy = Rows - 1 - i / Columns;
                    atlas.SetPixels(cx * Cell, cy * Cell, Cell, Cell, pixels);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            atlas.Apply();
            File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(AtlasPath);
            if (AssetImporter.GetAtPath(AtlasPath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 1024;
                ti.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

            // The material.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Diorama/Portrait"));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.SetTexture("_MainTex", tex);
            EditorUtility.SetDirty(mat);

            // The data.
            var data = AssetDatabase.LoadAssetAtPath<JournalData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<JournalData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }
            data.atlas = tex;
            data.portraitMaterial = mat;
            data.columns = Columns;
            data.rows = Rows;
            data.entries.Clear();
            var from = new Dictionary<IslingSpecies, IslingSpecies>();
            foreach (var s in order)
                if (s.evolvesTo != null && !from.ContainsKey(s.evolvesTo)) from[s.evolvesTo] = s;
            for (int i = 0; i < order.Count; i++)
            {
                var s = order[i];
                from.TryGetValue(s, out var f);
                data.entries.Add(new JournalData.Entry { species = s, from = f, cell = i, hint = Journal.Hint(s, tables) });
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var counts = string.Join(", ", Journal.Elements.Select(e => $"{e} {Journal.Total(data.entries, e)}"));
            var noHint = data.entries.Where(e => e.from == null && string.IsNullOrEmpty(e.hint)).Select(e => e.species.name).ToList();
            Debug.Log($"[JournalBake] {data.entries.Count} Islings ({counts}) -> {DataPath}, atlas {AtlasPath}." +
                      (noHint.Count > 0 ? $" No home found for: {string.Join(", ", noHint)}." : " Every base form has a home."));
        }

        /// <summary>Encounter tables in the order the player meets them: the Still Isle, Galewick, Cinderwood, Lilymere.</summary>
        static List<(string, string, EncounterTable)> Tables()
        {
            var list = new List<(string, string, EncounterTable)>();
            var maps = AssetDatabase.FindAssets("t:WorldMap").Select(g => AssetDatabase.LoadAssetAtPath<WorldMap>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && !string.IsNullOrEmpty(m.realm)).ToList();
            foreach (var realm in new[] { "stillisle", "galewick", "cinderwood", "lilymere" })
                foreach (var m in maps.Where(m => m.realm == realm))
                {
                    string name = FinaleLogic.RealmName(m.realm);
                    if (m.zoneLegend != null)
                        foreach (var z in m.zoneLegend)
                        {
                            if (z.grassTable == null) continue;
                            if (z.id == "rare") list.Add(("Rare", null, z.grassTable));
                            else list.Add((name, Places.TryGetValue(z.id ?? "", out var p) ? p : z.id, z.grassTable));
                        }
                    if (m.grassTable != null) list.Add((name, "tall grass", m.grassTable));
                    if (m.waterTable != null) list.Add((name, "the water", m.waterTable));
                }
            return list;
        }

        static Camera Rig(UnityEngine.SceneManagement.Scene preview, out Transform turntable)
        {
            var camGo = new GameObject("Portrait Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
            var cam = camGo.AddComponent<Camera>();
            cam.scene = preview;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = 22f;
            cam.nearClipPlane = 0.05f;
            var lightGo = new GameObject("Portrait Light");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, preview);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);
            turntable = new GameObject("Turntable").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(turntable.gameObject, preview);
            turntable.localRotation = Quaternion.Euler(0f, -30f, 0f);
            return cam;
        }

        /// <summary>
        /// One portrait, Cell x Cell: rendered twice (on black and on white) at twice the size; alpha = 1 - (white - black),
        /// colour = black / alpha (clean edges, no fringe), then halved.
        /// </summary>
        static Color[] Portrait(IslingSpecies s, Camera cam, Transform turntable, UnityEngine.SceneManagement.Scene preview)
        {
            if (s.Prefab == null)
            {
                Debug.LogWarning($"[JournalBake] {s.name} has no baked prefab: empty portrait.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(s.Prefab, preview);
            try
            {
                go.transform.SetParent(turntable, false);
                go.transform.localScale = Vector3.one * 8f;
                var view = go.GetComponent<CreatureView>();
                if (view != null) view.ApplyStyles();
                var b = new Bounds(go.transform.position, Vector3.zero);
                bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    if (first) b = r.bounds;
                    else b.Encapsulate(r.bounds);
                    first = false;
                }
                float radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 1.08f;
                float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cam.transform.position = b.center + new Vector3(0f, 0.22f, 1f).normalized * dist;
                cam.transform.LookAt(b.center);
                var black = Shot(cam, Color.black);
                var white = Shot(cam, Color.white);
                var full = new Color[Render * Render];
                for (int i = 0; i < full.Length; i++)
                {
                    Color k = black[i], w = white[i];
                    float a = Mathf.Clamp01(1f - ((w.r - k.r) + (w.g - k.g) + (w.b - k.b)) / 3f);
                    full[i] = a > 0.004f ? new Color(Mathf.Clamp01(k.r / a), Mathf.Clamp01(k.g / a), Mathf.Clamp01(k.b / a), a) : Color.clear;
                }
                var half = new Color[Cell * Cell];
                for (int y = 0; y < Cell; y++)
                    for (int x = 0; x < Cell; x++)
                    {
                        Color sum = Color.clear;
                        float aSum = 0f;
                        for (int dy = 0; dy < 2; dy++)
                            for (int dx = 0; dx < 2; dx++)
                            {
                                var c = full[(y * 2 + dy) * Render + x * 2 + dx];
                                sum += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                                aSum += c.a;
                            }
                        half[y * Cell + x] = aSum > 1e-4f ? new Color(sum.r / aSum, sum.g / aSum, sum.b / aSum, aSum / 4f) : Color.clear;
                    }
                return half;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static Color[] Shot(Camera cam, Color background)
        {
            cam.backgroundColor = background;
            var rt = new RenderTexture(Render, Render, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            var tex = new Texture2D(Render, Render, TextureFormat.RGBA32, false);
            var prev = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Render, Render), 0, 0);
                tex.Apply();
                return tex.GetPixels();
            }
            finally
            {
                RenderTexture.active = prev;
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
        }

        static T Load<T>(string filter) where T : Object
        {
            var g = AssetDatabase.FindAssets(filter).FirstOrDefault();
            return g != null ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)) : null;
        }
    }
}
