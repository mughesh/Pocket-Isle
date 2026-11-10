using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 11 (Mugesh: "many assets look kind of broken; the tower pillars look like a jagged mesh"): every baked prop,
    /// alone and close, on contact sheets (Logs/PropAudit/sheet_NN.png, 4 x 3 per sheet, name and triangles under each)
    /// plus a table (Logs/PropAudit/report.md): triangles, budget, resolution, voxel size against the prop's thinnest
    /// part. Edit mode only; draws far below the scene and removes everything after.
    /// </summary>
    public static class PropAudit
    {
        const string Dir = "Logs/PropAudit";
        const int Cols = 4, Rows = 3, W = 1600, H = 1200;

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");

        static float _pitch = 24f;

        public static void All()
        {
            _pitch = 24f;
            Run(null);
        }

        /// <summary>Only the recipes named in Temp/ClaudeBridge/arg.txt (comma separated), one per cell, bigger.</summary>
        public static void Some()
        {
            var arg = File.Exists("Temp/ClaudeBridge/arg.txt") ? File.ReadAllText("Temp/ClaudeBridge/arg.txt") : "";
            _pitch = 24f;
            if (arg.StartsWith("pitch=") && arg.Contains(";"))
            {
                float.TryParse(arg.Substring(6, arg.IndexOf(';') - 6), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _pitch);
                arg = arg.Substring(arg.IndexOf(';') + 1);
            }
            Run(new HashSet<string>(arg.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0)));
        }

        static void Run(HashSet<string> only)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[PropAudit] Stop Play first.");
                return;
            }
            Directory.CreateDirectory(Dir);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Step14cSetup.LogoMaterialPath);
            var recipes = AssetDatabase.FindAssets("t:PropRecipe", new[] { "Assets/_Project/Data/Props" })
                .Select(g => AssetDatabase.LoadAssetAtPath<PropRecipe>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null && r.bakedMesh != null && (only == null || only.Contains(r.name)))
                .OrderBy(r => r.name).ToList();
            var report = new StringBuilder("# Prop audit\n\n| Prop | Tris | Budget | Res | Voxel (tiles) | Size (tiles) |\n|---|---|---|---|---|---|\n");
            int perSheet = only != null && only.Count <= 4 ? Mathf.Max(1, only.Count) : Cols * Rows;
            int cols = perSheet == Cols * Rows ? Cols : perSheet, rows = perSheet == Cols * Rows ? Rows : 1;
            int sheet = 0;
            for (int start = 0; start < recipes.Count; start += perSheet)
            {
                var batch = recipes.Skip(start).Take(perSheet).ToList();
                Sheet(batch, material, cols, rows, $"{Dir}/{(only != null ? "some" : "sheet")}_{sheet++:00}.png");
            }
            foreach (var r in recipes)
            {
                var b = r.bakedMesh.bounds;
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                report.AppendLine($"| {r.name} | {r.bakedMesh.triangles.Length / 3} | {r.triangleBudget} | {r.resolution} | {longest / Mathf.Max(1, r.resolution):0.000} | {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} |");
            }
            File.WriteAllText($"{Dir}/{(only != null ? "some" : "report")}.md", report.ToString());
            Debug.Log($"[PropAudit] {recipes.Count} props on {sheet} sheet(s) in {Dir}.");
        }

        static void Sheet(List<PropRecipe> batch, Material material, int cols, int rows, string path)
        {
            var root = new GameObject("PropAudit") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(0f, -500f, 0f);
            var camGo = new GameObject("PropAudit Camera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.66f, 0.7f);
            cam.fieldOfView = 28f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var block = new MaterialPropertyBlock();
            var labels = new List<(Rect rect, string text)>();
            try
            {
                for (int i = 0; i < batch.Count; i++)
                {
                    var r = batch[i];
                    var go = new GameObject(r.name) { hideFlags = HideFlags.HideAndDontSave };
                    go.transform.SetParent(root.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = r.bakedMesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = material;
                    block.Clear();
                    r.ApplyTo(block);
                    block.SetFloat(WindowClipId, 0f);
                    block.SetFloat(RestoredSelfId, 1f);
                    block.SetFloat(ClearSinkId, 0f);
                    mr.SetPropertyBlock(block);
                    var b = mr.bounds;
                    float radius = b.extents.magnitude;
                    float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                    var dir = Quaternion.Euler(_pitch, -28f, 0f) * Vector3.back;
                    cam.transform.SetPositionAndRotation(b.center + dir * dist, Quaternion.LookRotation(-dir, Vector3.up));
                    int cx = i % cols, cy = i / cols;
                    cam.rect = new Rect(cx / (float)cols, 1f - (cy + 1) / (float)rows, 1f / cols, 1f / rows);
                    cam.Render();
                    labels.Add((cam.rect, $"{r.name} {r.bakedMesh.triangles.Length / 3}t r{r.resolution}"));
                    Object.DestroyImmediate(go);
                }
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                RenderTexture.active = null;
                // Cell borders, so the sheet reads as a grid.
                for (int c = 1; c < cols; c++)
                    for (int y = 0; y < H; y++) tex.SetPixel(c * W / cols, y, Color.black);
                for (int rr = 1; rr < rows; rr++)
                    for (int x = 0; x < W; x++) tex.SetPixel(x, rr * H / rows, Color.black);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                File.WriteAllText(Path.ChangeExtension(path, ".txt"), string.Join("\n", labels.Select((l, k) => $"{k}: {l.text}")));
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(root);
            }
        }
    }
}
