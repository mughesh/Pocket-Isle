using System.Text;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 15: trims the four recorded walk swipes (thumb_swipe_forward / back / left / right) for the walk demo, which uses
    /// only their thumb joints. Each take starts with the thumb open, comes onto the index finger, swipes, lifts off. Kept:
    /// from the first frame the thumb tip is on the finger (within <see cref="OnFinger"/> of the index's middle joint) to
    /// <see cref="After"/> past the furthest point of the swipe (measured across the finger's surface, so the approach and
    /// the lift don't count), searched while the thumb stays near the finger (<see cref="NearFinger"/>). Other clips untouched.
    /// </summary>
    public static class GhostSwipeTrim
    {
        static readonly string[] Names = { "thumb_swipe_forward", "thumb_swipe_back", "thumb_swipe_left", "thumb_swipe_right" };
        const float OnFinger = 0.038f, NearFinger = 0.055f, After = 0.2f;

        [MenuItem("Tools/Pocket Isle/Ghost Hands/Trim Walk Swipes", priority = 64)]
        public static void Run() => Go(true);

        public static void Check() => Go(false);

        /// <summary>Nov 15: the generic trim (GhostClipCheck.Measure: whole-hand motion, idle cut) on the named clips only (bridge arg, comma list).</summary>
        public static void TrimNamed()
        {
            var lib = GhostClipTools.Library();
            var log = new StringBuilder("[GhostSwipeTrim] Trimmed by motion:");
            foreach (var name in (BridgeArg ?? "").Split(','))
            {
                var c = lib.Find(name.Trim());
                if (c == null || c.frames.Count < 2) { log.Append($"\n  {name}: not recorded."); continue; }
                var r = GhostClipCheck.Measure(c);
                float raw = c.RawDuration;
                Undo.RecordObject(c, "Trim ghost clip");
                c.trimStart = r.start;
                c.trimEnd = Mathf.Max(0f, raw - r.end);
                c.keyTime = r.key;
                EditorUtility.SetDirty(c);
                log.Append($"\n  {c.clipName}: keep {r.start:0.00}-{r.end:0.00} s of {raw:0.00}, key {r.key:0.00} ({r.why}), palm travel {r.travel * 100f:0} cm.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static string BridgeArg => System.IO.File.Exists("Temp/ClaudeBridge/arg.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "";

        static void Go(bool apply)
        {
            var lib = GhostClipTools.Library();
            var log = new StringBuilder($"[GhostSwipeTrim] {(apply ? "Trimmed" : "Checked")}:");
            int palm = (int)HandJointId.HandPalm, th = (int)HandJointId.HandThumbTip, ix2 = (int)HandJointId.HandIndex2;
            foreach (var name in Names)
            {
                var c = lib.Find(name);
                if (c == null || c.frames.Count < 2)
                {
                    log.Append($"\n  {name}: not recorded.");
                    continue;
                }
                int n = c.frames.Count;
                float fps = Mathf.Max(1f, c.fps), raw = c.RawDuration;
                var tip = new Vector3[n];
                var gap = new float[n];
                Vector3 along = Vector3.forward, dorsal = Vector3.up;
                for (int i = 0; i < n; i++)
                {
                    var p = GhostClipCheck.Solve(c.frames[i]);
                    var inv = Quaternion.Inverse(c.frames[i].rootRot);
                    tip[i] = inv * (p[th] - p[palm]);
                    gap[i] = (p[th] - p[ix2]).magnitude;
                    if (i != 0) continue;
                    Vector3 w = inv * p[(int)HandJointId.HandWristRoot], m = inv * p[(int)HandJointId.HandMiddle1];
                    Vector3 fingers = m - w, across = inv * (p[(int)HandJointId.HandIndex1] - p[(int)HandJointId.HandPinky1]);
                    dorsal = Vector3.Cross(across, fingers).normalized * (c.leftHand ? -1f : 1f);
                    along = (fingers - dorsal * Vector3.Dot(fingers, dorsal)).normalized;
                }
                int a = 0;
                while (a < n && gap[a] > OnFinger) a++;
                if (a >= n)
                {
                    log.Append($"\n  {name}: the thumb never reaches the index finger (closest {Mathf.Min(gap) * 1000f:0} mm): not trimmed, re-record.");
                    continue;
                }
                // Across the finger's surface only (along the fingers, and the back of the hand): approach and lift are the third axis.
                Vector2 Plane(int i)
                {
                    Vector3 d = tip[i] - tip[a];
                    return new Vector2(Vector3.Dot(d, along), Vector3.Dot(d, dorsal));
                }
                int far = a;
                for (int i = a; i < n && gap[i] <= NearFinger; i++)
                    if (Plane(i).sqrMagnitude > Plane(far).sqrMagnitude) far = i;
                int b = Mathf.Min(n - 1, far + Mathf.RoundToInt(After * fps));
                var sw = Plane(far) * 1000f;
                string way = Mathf.Abs(sw.x) >= Mathf.Abs(sw.y) ? (sw.x > 0 ? "forward" : "back") : (sw.y > 0 ? "under the finger" : "over the finger");
                log.Append($"\n  {name}: {raw:0.00} s raw; on the finger at {a / fps:0.00} s, furthest at {far / fps:0.00} s: {sw.magnitude:0} mm " +
                           $"({sw.x:0} along, {sw.y:0} across) = {way}; keep {a / fps:0.00}-{b / fps:0.00} s. Was trim {c.trimStart:0.00} / {c.trimEnd:0.00}.");
                if (!apply) continue;
                Undo.RecordObject(c, "Trim walk swipe");
                c.trimStart = a / fps;
                c.trimEnd = Mathf.Max(0f, raw - b / fps);
                c.keyTime = (far - a) / Mathf.Max(1f, b - a);
                EditorUtility.SetDirty(c);
            }
            if (apply) AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }
    }
}
