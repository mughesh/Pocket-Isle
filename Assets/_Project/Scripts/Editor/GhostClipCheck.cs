using System.Text;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Checks recorded ghost clips without Play mode: how long the hand idles before and after the gesture, how far it
    /// travels, and when the key moment is (the poke's deepest point, the hand closing on a grab, the thumb's farthest
    /// slide, the tightest pinch). Auto-trim writes the trim and key moment into the clips (Undo works).
    /// </summary>
    public static class GhostClipCheck
    {
        // Parent of each OpenXR joint (ISDK skeleton): fingers chain from the wrist; the palm hangs off the wrist.
        static readonly int[] Parent =
        {
            1, -1, 1, 2, 3, 4, 1, 6, 7, 8, 9, 1, 11, 12, 13, 14, 1, 16, 17, 18, 19, 1, 21, 22, 23, 24,
        };

        /// <summary>Joint positions in the clip frame (root pose + joint-local chain), metres.</summary>
        public static Vector3[] Solve(GhostFrame f)
        {
            int n = f.rot.Length;
            var wr = new Quaternion[n];
            var wp = new Vector3[n];
            wr[1] = f.rootRot * f.rot[1];
            wp[1] = f.rootPos + f.rootRot * f.pos[1];
            // Parents always come before their children in the OpenXR order, except the palm (0) which hangs off the wrist (1).
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    if (i == 1) continue;
                    int pi = i < Parent.Length ? Parent[i] : 1;
                    wr[i] = wr[pi] * f.rot[i];
                    wp[i] = wp[pi] + wr[pi] * f.pos[i];
                }
            return wp;
        }

        public struct Report
        {
            public float start, end, key, travel, curlRange;
            public string why;
        }

        public static Report Measure(GhostHandClip c)
        {
            int n = c.frames.Count;
            var pts = new Vector3[n][];
            for (int i = 0; i < n; i++) pts[i] = Solve(c.frames[i]);
            int palm = (int)HandJointId.HandPalm;
            int idx = (int)HandJointId.HandIndexTip, thumb = (int)HandJointId.HandThumbTip;
            int[] tips = { thumb, idx, (int)HandJointId.HandMiddleTip, (int)HandJointId.HandRingTip, (int)HandJointId.HandPinkyTip };
            var motion = new float[n];
            var curl = new float[n];
            for (int i = 0; i < n; i++)
            {
                float d = 0f;
                foreach (var t in tips) d += (pts[i][t] - pts[i][palm]).magnitude;
                curl[i] = d / tips.Length;
                if (i == 0) continue;
                float m = (pts[i][palm] - pts[i - 1][palm]).magnitude;
                foreach (var t in tips) m += ((pts[i][t] - pts[i][palm]) - (pts[i - 1][t] - pts[i - 1][palm])).magnitude * 0.5f;
                motion[i] = m;
            }
            var smooth = new float[n];
            float peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float sum = 0f;
                int k = 0;
                for (int j = Mathf.Max(1, i - 2); j <= Mathf.Min(n - 1, i + 2); j++, k++) sum += motion[j];
                smooth[i] = k > 0 ? sum / k : 0f;
                peak = Mathf.Max(peak, smooth[i]);
            }
            float th = peak * 0.2f;
            int a = 0, b = n - 1;
            while (a < n - 1 && smooth[a] < th) a++;
            while (b > a && smooth[b] < th) b--;
            int pad = Mathf.RoundToInt(c.fps * 0.1f);
            a = Mathf.Max(0, a - pad);
            b = Mathf.Min(n - 1, b + pad);

            int key = (a + b) / 2;
            string why = "middle";
            float best;
            switch (c.clipName)
            {
                case "poke":
                    best = float.MinValue;
                    for (int i = a; i <= b; i++)
                    {
                        float v = pts[i][idx].z - pts[i][idx].y;
                        if (v > best) { best = v; key = i; }
                    }
                    why = "index reaches farthest";
                    break;
                case "grab_rim_slide":
                case "grab_drop":
                case "grab_toss":
                case "grab_flick":
                {
                    float open = curl[a], closed = float.MaxValue;
                    for (int i = a; i <= b; i++) closed = Mathf.Min(closed, curl[i]);
                    for (int i = a; i <= b; i++)
                        if (curl[i] <= open - (open - closed) * 0.8f) { key = i; break; }
                    why = $"hand closes by {(open - closed) * 100f:0.0} cm";
                    break;
                }
                case "pinch":
                    best = float.MaxValue;
                    for (int i = a; i <= b; i++)
                    {
                        float v = (pts[i][thumb] - pts[i][idx]).magnitude;
                        if (v < best) { best = v; key = i; }
                    }
                    why = $"thumb meets index, {best * 100f:0.0} cm apart";
                    break;
                case "pump_handle":
                {
                    // The first pinch (two strokes: the closest pinch could be the second one).
                    best = float.MaxValue;
                    for (int i = a; i <= b; i++) best = Mathf.Min(best, (pts[i][thumb] - pts[i][idx]).magnitude);
                    for (int i = a; i <= b; i++)
                        if ((pts[i][thumb] - pts[i][idx]).magnitude <= best + 0.006f)
                        {
                            key = i;
                            break;
                        }
                    why = $"first pinch, thumb and index {best * 100f:0.0} cm apart";
                    break;
                }
                case "thumb_swipe_forward":
                    best = float.MinValue;
                    for (int i = a; i <= b; i++)
                    {
                        float v = Vector3.Dot(pts[i][thumb] - pts[i][palm], (pts[i][idx] - pts[i][palm]).normalized);
                        if (v > best) { best = v; key = i; }
                    }
                    why = "thumb slid farthest";
                    break;
                case "thumb_tap":
                    best = float.MaxValue;
                    for (int i = a; i <= b; i++)
                    {
                        float v = (pts[i][thumb] - pts[i][(int)HandJointId.HandIndex2]).magnitude;
                        if (v < best) { best = v; key = i; }
                    }
                    why = "thumb down on the index";
                    break;
            }
            float travel = 0f, minCurl = float.MaxValue, maxCurl = 0f;
            for (int i = a; i <= b; i++)
            {
                if (i > a) travel += (pts[i][palm] - pts[i - 1][palm]).magnitude;
                minCurl = Mathf.Min(minCurl, curl[i]);
                maxCurl = Mathf.Max(maxCurl, curl[i]);
            }
            float fps = Mathf.Max(1f, c.fps);
            return new Report
            {
                start = a / fps, end = b / fps, key = (key - a) / Mathf.Max(1f, b - a), travel = travel, curlRange = maxCurl - minCurl, why = why,
            };
        }

        [MenuItem("Tools/Pocket Isle/Ghost Hands/Check Recorded Clips", priority = 62)]
        public static void Check() => Run(false);

        [MenuItem("Tools/Pocket Isle/Ghost Hands/Auto-Trim Recorded Clips", priority = 63)]
        public static void AutoTrim() => Run(true);

        static void Run(bool apply)
        {
            var lib = GhostClipTools.Library();
            var log = new StringBuilder($"[GhostClipCheck] {(apply ? "Auto-trimmed" : "Checked")} {lib.clips.Count} recorded clip(s):");
            foreach (var c in lib.clips)
            {
                if (c == null || c.frames.Count < 2) continue;
                var r = Measure(c);
                float raw = c.RawDuration;
                log.Append($"\n  {c.clipName}: {raw:0.00} s, {(c.leftHand ? "left" : "right")} hand, {c.anchor}. Gesture {r.start:0.00}-{r.end:0.00} s " +
                           $"(idle {r.start:0.00} s before, {raw - r.end:0.00} s after), palm travel {r.travel * 100f:0} cm, finger curl range {r.curlRange * 100f:0.0} cm, " +
                           $"key {r.key:0.00} ({r.why}). Was trim {c.trimStart:0.00} / {c.trimEnd:0.00}, key {c.keyTime:0.00}.");
                if (!apply) continue;
                Undo.RecordObject(c, "Auto-trim ghost clip");
                if (c.clipName == "palm_hover")
                {
                    // The hover is the still part after the hand arrives: keep that, drop the reach-in.
                    c.trimStart = Mathf.Min(r.end, raw - 0.6f);
                    c.trimEnd = 0f;
                    c.keyTime = 0.5f;
                    EditorUtility.SetDirty(c);
                    continue;
                }
                c.trimStart = r.start;
                c.trimEnd = Mathf.Max(0f, raw - r.end);
                c.keyTime = r.key;
                EditorUtility.SetDirty(c);
            }
            if (apply) AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
