using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 18 (store listing art): renders the live island in Play mode at store sizes with a transparent background
    /// (rendered twice, on black and on white, alpha from the difference), so the pictures can be laid out on a painted
    /// backdrop. Bridge: exec:Diorama.EditorTools.StoreShots.Shot with arg "name;yaw;pitch;dist;aimY;w;h;fov"
    /// (yaw/pitch degrees round the island from the player's seat, dist in island widths, aimY in island widths above its
    /// middle); exec:...Go with arg = a GameState spot label. Output Logs/StoreShots/name.png (RGBA).
    /// </summary>
    public static class StoreShots
    {
        const string Dir = "Logs/StoreShots";
        static string Arg => File.Exists("Temp/ClaudeBridge/arg.txt") ? File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "";

        public static void Go()
        {
            var gs = GameState.Instance;
            if (!EditorApplication.isPlaying || gs == null)
            {
                Debug.LogWarning("[StoreShots] Go: not in Play.");
                return;
            }
            foreach (var s in gs.Spots)
                if (s.label == Arg)
                {
                    Debug.Log($"[StoreShots] Go {Arg}: {gs.GoTo(s)}");
                    return;
                }
            Debug.LogWarning($"[StoreShots] No spot '{Arg}'.");
        }

        public static void Shot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!EditorApplication.isPlaying || island == null)
            {
                Debug.LogWarning("[StoreShots] Shot: not in Play or no island.");
                return;
            }
            var p = Arg.Split(';');
            float F(int i, float d) => p.Length > i && float.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : d;
            string name = p.Length > 0 && p[0] != "" ? p[0] : "shot";
            float yaw = F(1, 0f), pitch = F(2, 40f), dist = F(3, 1.25f), aimY = F(4, 0.05f), fov = F(7, 35f);
            int w = (int)F(5, 2560), h = (int)F(6, 1440);

            var t = island.transform;
            float width = island.map.Columns * island.map.tileSize * t.lossyScale.x;
            Vector3 aim = t.position + t.up * (aimY * width);
            Vector3 dir = Quaternion.AngleAxis(yaw, t.up) * (Quaternion.AngleAxis(-pitch, t.right) * t.forward);
            Vector3 pos = aim + dir * (dist * width);
            var rot = Quaternion.LookRotation(aim - pos, t.up);

            var black = Render(pos, rot, fov, w * 2, h * 2, Color.black);
            var white = Render(pos, rot, fov, w * 2, h * 2, Color.white);
            var outTex = new Texture2D(w * 2, h * 2, TextureFormat.RGBA32, false);
            var b = black.GetPixels32();
            var wh = white.GetPixels32();
            var o = new Color32[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                // On black: c = a*C; on white: c = a*C + (1-a). Alpha from the difference, colour un-premultiplied.
                int diff = ((wh[i].r - b[i].r) + (wh[i].g - b[i].g) + (wh[i].b - b[i].b)) / 3;
                float a = Mathf.Clamp01(1f - diff / 255f);
                byte A = (byte)Mathf.RoundToInt(a * 255f);
                if (a < 0.004f) o[i] = new Color32(0, 0, 0, 0);
                else o[i] = new Color32((byte)Mathf.Min(255, b[i].r / a), (byte)Mathf.Min(255, b[i].g / a), (byte)Mathf.Min(255, b[i].b / a), A);
            }
            outTex.SetPixels32(o);
            outTex.Apply();
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, outTex.EncodeToPNG());
            Object.DestroyImmediate(black);
            Object.DestroyImmediate(white);
            Object.DestroyImmediate(outTex);
            Debug.Log($"[StoreShots] {path} ({w * 2}x{h * 2}, yaw {yaw} pitch {pitch} dist {dist}).");
        }

        static Texture2D Render(Vector3 pos, Quaternion rot, float fov, int w, int h, Color bg)
        {
            var go = new GameObject("Store Shot Camera");
            var cam = go.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(pos, rot);
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            var main = Camera.main;
            if (main != null) cam.cullingMask = main.cullingMask;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            return tex;
        }
    }
}
