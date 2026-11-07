using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 7 (Mugesh: use the circular half-posed character, not the photo): cuts Assets/_Project/Art/Credits/Home_char.png
    /// to its dark ring (the round portrait without the orange glow round it) with a soft round edge, and writes
    /// CreditsPortrait.png beside it (transparent outside). The credits standee uses that file (<see cref="CreditsStandeeHook"/>).
    /// Menu: Tools > Diorama > Credits > Make Round Portrait.
    /// </summary>
    public static class CreditsPortraitTool
    {
        public const string Source = "Assets/_Project/Art/Credits/Home_char.png";
        public const string Output = "Assets/_Project/Art/Credits/CreditsPortrait.png";

        [MenuItem("Tools/Diorama/Credits/Make Round Portrait")]
        public static void Make()
        {
            if (!File.Exists(Source))
            {
                Debug.LogError($"[CreditsPortrait] No {Source}.");
                return;
            }
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(Source));
            int w = src.width, h = src.height;
            // The portrait's dark ring: centred, its outer edge at about 0.345 of the width (measured on the 1158 px picture).
            float cx = w * 0.5f, cy = h * 0.5f + h * 0.0026f, r = w * 0.3465f;
            int size = Mathf.CeilToInt(r * 2f) + 4;
            var dst = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float ox = cx - size * 0.5f, oy = cy - size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float sx = ox + x + 0.5f, sy = oy + y + 0.5f;
                    float d = Mathf.Sqrt((sx - cx) * (sx - cx) + (sy - cy) * (sy - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f); // a 1 px soft edge
                    var c = (Color32)src.GetPixelBilinear(sx / w, sy / h);
                    c.a = (byte)(a * 255f);
                    px[y * size + x] = c;
                }
            dst.SetPixels32(px);
            dst.Apply();
            File.WriteAllBytes(Output, dst.EncodeToPNG());
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(dst);
            AssetDatabase.ImportAsset(Output);
            if (AssetImporter.GetAtPath(Output) is TextureImporter imp)
            {
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            Debug.Log($"[CreditsPortrait] {Output}: {size}x{size}, cut from {Source} (ring radius {r:0} px).");
        }
    }
}
