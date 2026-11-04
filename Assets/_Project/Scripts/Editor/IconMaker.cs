using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Overnight Nov 5: the app icon and the Devpost key art, rendered from the game's own baked logo (the letters on their
    /// mini island, TitleLogo's two props) over a soft sky-teal gradient. Play mode (the props need the running lighting):
    /// Tools > Pocket Isle > Release > Make Icon + Key Art, or the bridge exec:Diorama.EditorTools.IconMaker.Make.
    /// Each picture is rendered twice (black and white backgrounds) to get a clean alpha, then laid on the gradient.
    /// Icon PNGs: Assets/_Project/Art/Icon/PocketIsle_Icon_{1024,512,432,192,144,96,72,48,36}.png. Key art:
    /// Builds/Marketing/PocketIsle_KeyArt_1920x1080.png (the island from a seated head, the logo over it).
    /// Assign (only if asked): <see cref="AssignIcon"/> sets the Default Icon and nothing else.
    /// </summary>
    public static class IconMaker
    {
        public const string IconDir = "Assets/_Project/Art/Icon", IconMaster = IconDir + "/PocketIsle_Icon_1024.png";
        const string KeyArtPath = "Builds/Marketing/PocketIsle_KeyArt_1920x1080.png";
        static readonly int[] Sizes = { 1024, 512, 432, 192, 144, 96, 72, 48, 36 };
        static readonly Color SkyTop = new Color(0.66f, 0.89f, 0.95f), SkyBottom = new Color(0.33f, 0.73f, 0.72f);

        [MenuItem("Tools/Pocket Isle/Release/Make Icon + Key Art (Play mode)", priority = 61)]
        public static void Make()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[IconMaker] Enter Play first (the logo props render with the game's lighting).");
                return;
            }
            var logo = Object.FindFirstObjectByType<TitleLogo>(FindObjectsInactive.Include);
            if (logo == null || logo.recipe == null || logo.recipe.bakedMesh == null || logo.material == null)
            {
                Debug.LogError("[IconMaker] No TitleLogo with a baked mesh and material in the scene.");
                return;
            }
            int layer = FreeLayer();
            var root = BuildLogo(logo, layer, new Vector3(0f, -400f, 0f), out var bounds);
            try
            {
                // The icon: the logo square, a little margin.
                var c = bounds.center;
                float size = Mathf.Max(bounds.size.x, bounds.size.y);
                var eye = c + new Vector3(0f, size * 0.18f, size * 2.6f);
                var icon = RenderOver(eye, c, 1024, 1024, 1 << layer, 21f, null);
                Directory.CreateDirectory(IconDir);
                foreach (int s in Sizes)
                {
                    var t = s == 1024 ? icon : Scale(icon, s);
                    File.WriteAllBytes($"{IconDir}/PocketIsle_Icon_{s}.png", t.EncodeToPNG());
                    if (t != icon) Object.DestroyImmediate(t);
                }
                Object.DestroyImmediate(icon);
                AssetDatabase.Refresh();

                // The key art: the island from a seated head, then the logo laid over its top third.
                var island = Object.FindFirstObjectByType<IslandBuilder>();
                Directory.CreateDirectory(Path.GetDirectoryName(KeyArtPath));
                if (island != null)
                {
                    var t = island.transform;
                    Vector3 front = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
                    Vector3 centre = t.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f));
                    float w = island.FootprintHalfSize.x * t.lossyScale.x * 2f;
                    Vector3 islandEye = centre + front * w * 1.05f + Vector3.up * w * 0.62f;
                    int all = ~(1 << layer);
                    var art = RenderOver(islandEye, centre - Vector3.up * w * 0.08f + Vector3.up * w * 0.1f, 1920, 1080, all, 38f, null);
                    // The logo, transparent, then over the upper middle of the art.
                    var logoArt = RenderAlpha(eye, c, 900, 900, 1 << layer, 21f, out var alpha);
                    Paste(art, logoArt, alpha, new RectInt(510, 1080 - 470, 900, 900), 0.52f);
                    File.WriteAllBytes(KeyArtPath, art.EncodeToPNG());
                    Object.DestroyImmediate(art);
                    Object.DestroyImmediate(logoArt);
                }
                Debug.Log($"[IconMaker] Icon PNGs in {IconDir} ({string.Join(", ", Sizes)}); key art {KeyArtPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Sets Player Settings > Default Icon to the 1024 icon (nothing else).</summary>
        [MenuItem("Tools/Pocket Isle/Release/Assign App Icon", priority = 62)]
        public static void AssignIcon()
        {
            var imp = AssetImporter.GetAtPath(IconMaster) as TextureImporter;
            if (imp == null)
            {
                Debug.LogError($"[IconMaker] No {IconMaster}: run Make Icon + Key Art first.");
                return;
            }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled)
            {
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconMaster);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { tex }, IconKind.Application);
            AssetDatabase.SaveAssets();
            Debug.Log($"[IconMaker] Default Icon = {IconMaster}.");
        }

        static int FreeLayer()
        {
            for (int i = 31; i > 8; i--)
                if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
            return 31;
        }

        static GameObject BuildLogo(TitleLogo logo, int layer, Vector3 at, out Bounds bounds)
        {
            var root = new GameObject("Icon Logo") { hideFlags = HideFlags.DontSave };
            root.transform.position = at;
            var mesh = logo.recipe.bakedMesh;
            var b = mesh.bounds;
            var baseMesh = logo.baseRecipe != null ? logo.baseRecipe.bakedMesh : null;
            if (baseMesh != null) b.Encapsulate(baseMesh.bounds);
            float scale = 1f / Mathf.Max(1e-4f, b.size.x);
            void Part(Mesh m, PropRecipe r)
            {
                var go = new GameObject(m.name) { layer = layer, hideFlags = HideFlags.DontSave };
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = logo.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();
                r.ApplyTo(block);
                block.SetFloat("_WindowClip", 0f);
                block.SetFloat("_RestoredSelf", 1f);
                block.SetFloat("_ClearSink", 0f);
                mr.SetPropertyBlock(block);
                go.transform.localScale = Vector3.one * scale;
                go.transform.localPosition = -b.center * scale;
                // Facing the camera (+Z toward it): the logo's letters read from its front.
                go.transform.localRotation = Quaternion.identity;
            }
            Part(mesh, logo.recipe);
            if (baseMesh != null) Part(baseMesh, logo.baseRecipe);
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            bounds = new Bounds(at, b.size * scale);
            return root;
        }

        /// <summary>Renders on black and on white: colour from the black pass, alpha from the difference.</summary>
        static Texture2D RenderAlpha(Vector3 eye, Vector3 look, int w, int h, int mask, float fov, out float[] alpha)
        {
            var black = Shoot(eye, look, w, h, mask, fov, Color.black);
            var white = Shoot(eye, look, w, h, mask, fov, Color.white);
            var b = black.GetPixels();
            var wh = white.GetPixels();
            alpha = new float[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float a = 1f - Mathf.Clamp01(((wh[i].r - b[i].r) + (wh[i].g - b[i].g) + (wh[i].b - b[i].b)) / 3f);
                alpha[i] = a;
                // Un-premultiply the black pass.
                b[i] = a > 0.004f ? new Color(b[i].r / a, b[i].g / a, b[i].b / a, a) : new Color(0f, 0f, 0f, 0f);
            }
            black.SetPixels(b);
            black.Apply();
            Object.DestroyImmediate(white);
            return black;
        }

        /// <summary>The picture laid over the sky-teal gradient (soft glow under the subject).</summary>
        static Texture2D RenderOver(Vector3 eye, Vector3 look, int w, int h, int mask, float fov, Texture2D _)
        {
            var subject = RenderAlpha(eye, look, w, h, mask, fov, out var alpha);
            var px = subject.GetPixels();
            var outPx = new Color[px.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = (float)y / (h - 1);
                    Color bg = Color.Lerp(SkyBottom, SkyTop, Mathf.SmoothStep(0f, 1f, v));
                    // A soft light pool behind the middle.
                    float dx = (x - w * 0.5f) / (w * 0.5f), dy = (y - h * 0.48f) / (h * 0.5f);
                    float pool = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    bg = Color.Lerp(bg, new Color(0.95f, 0.99f, 0.97f), pool * pool * 0.35f);
                    outPx[i] = Color.Lerp(bg, px[i], alpha[i]);
                    outPx[i].a = 1f;
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(outPx);
            tex.Apply();
            Object.DestroyImmediate(subject);
            return tex;
        }

        static Texture2D Shoot(Vector3 eye, Vector3 look, int w, int h, int mask, float fov, Color bg)
        {
            var go = new GameObject("Icon Camera") { hideFlags = HideFlags.DontSave };
            var cam = go.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.005f;
            cam.farClipPlane = 50f;
            cam.cullingMask = mask;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            return tex;
        }

        static void Paste(Texture2D into, Texture2D src, float[] alpha, RectInt at, float scale)
        {
            int w = Mathf.RoundToInt(src.width * scale), h = Mathf.RoundToInt(src.height * scale);
            int ox = at.x + (at.width - w) / 2, oy = at.y + (at.height - h) / 2;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int tx = ox + x, ty = oy + y;
                    if (tx < 0 || ty < 0 || tx >= into.width || ty >= into.height) continue;
                    float u = (float)x / w, v = (float)y / h;
                    var c = src.GetPixelBilinear(u, v);
                    int sx = Mathf.Clamp(Mathf.RoundToInt(u * (src.width - 1)), 0, src.width - 1), sy = Mathf.Clamp(Mathf.RoundToInt(v * (src.height - 1)), 0, src.height - 1);
                    float a = alpha[sy * src.width + sx];
                    into.SetPixel(tx, ty, Color.Lerp(into.GetPixel(tx, ty), new Color(c.r, c.g, c.b, 1f), a));
                }
            into.Apply();
        }

        static Texture2D Scale(Texture2D src, int size)
        {
            // Halving steps first (one bilinear blit from 1024 to 36 would alias).
            Texture cur = src;
            RenderTexture step = null;
            while (cur.width / 2 >= size * 2)
            {
                var half = RenderTexture.GetTemporary(cur.width / 2, cur.height / 2, 0, RenderTextureFormat.ARGB32);
                half.filterMode = FilterMode.Bilinear;
                Graphics.Blit(cur, half);
                if (step != null) RenderTexture.ReleaseTemporary(step);
                step = half;
                cur = half;
            }
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            Graphics.Blit(cur, rt);
            if (step != null) RenderTexture.ReleaseTemporary(step);
            RenderTexture.active = rt;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return t;
        }
    }
}
