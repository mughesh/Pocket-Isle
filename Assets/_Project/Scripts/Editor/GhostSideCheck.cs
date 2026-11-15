using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 15 (Mugesh: left-hand ghosts sat wrong where the right ones were fixed): in Play, poses every ghost clip in each
    /// placement for the right and the left hand, frozen at its key moment, and compares (island frame, the left mirrored
    /// across the island's middle) where the hand's mesh centre, its gold dot and the stroke arrow sit against the
    /// demonstrated spot. A gap over 1.5 cm is flagged. Bridge: exec:Diorama.EditorTools.GhostSideCheck.Run.
    /// </summary>
    public static class GhostSideCheck
    {
        static readonly (string clip, GhostPlace place)[] Cases =
        {
            ("poke", GhostPlace.AtTarget), ("pinch", GhostPlace.AboveTarget), ("pinch", GhostPlace.AboveIsland), ("pinch", GhostPlace.AtTarget),
            ("grab_drop", GhostPlace.AtTarget), ("grab_toss", GhostPlace.AtTarget), ("grab_flick", GhostPlace.AtTarget),
            ("grab_palm_up", GhostPlace.AtTarget), ("palm_hover", GhostPlace.AtTarget), ("pinch_pull", GhostPlace.AtTarget),
            ("finger_flick", GhostPlace.AtTarget), ("palm_sweep", GhostPlace.AtTarget), ("thumb_tap", GhostPlace.AboveIsland),
            (GhostHands.SwipeAllClip, GhostPlace.AboveIsland), (GhostHands.SwipeTapClip, GhostPlace.AboveIsland), ("hands_up", GhostPlace.AboveIsland),
            ("cup_hands", GhostPlace.AtTarget), ("palm_lift", GhostPlace.AtTarget), ("pinch_drop", GhostPlace.AtTarget), ("pinch_lift", GhostPlace.AtTarget),
        };

        public static void Run()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!Application.isPlaying || g == null || island == null)
            {
                Debug.LogError("[GhostSideCheck] Needs Play with the island and ghost hands.");
                return;
            }
            var it = island.transform;
            // A head on the seat (flat Play has none), as the demos face it.
            var eye = it.position + it.rotation * new Vector3(0f, 0.5f, 0.42f);
            if (Camera.main != null) Camera.main.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(it.position - eye, Vector3.up));
            Dialogue.RegisterTarget("sidecheck", () => it.TransformPoint(new Vector3(0.05f, island.CapTopLocal + 0.03f, 0.05f)), k => { }, () => 0f);
            var log = new StringBuilder("[GhostSideCheck] left vs right (island cm: x right-mirrored, y up, z toward the player; gap = mismatch):");
            int bad = 0;
            foreach (var (clip, place) in Cases)
            {
                var r = Measure(g, island, clip, place, false);
                var l = Measure(g, island, clip, place, true);
                if (r == null || l == null)
                {
                    log.Append($"\n  {clip} {place}: not shown ({(r == null ? "right" : "")} {(l == null ? "left" : "")}).");
                    continue;
                }
                var line = new StringBuilder($"\n  {clip} {place}:");
                bool flag = false;
                foreach (var key in r.Keys)
                {
                    if (!l.TryGetValue(key, out var lv)) continue;
                    var rv = r[key];
                    var mirrored = new Vector3(-lv.x, lv.y, lv.z);
                    float gap = (rv - mirrored).magnitude;
                    bool off = gap > 0.015f;
                    flag |= off;
                    line.Append($" {key} R{rv * 100f:F1} L{mirrored * 100f:F1} gap {gap * 100f:0.0}{(off ? " <<" : "")};");
                }
                if (flag) bad++;
                log.Append(line);
            }
            g.debugFreeze = -1f;
            g.previewing = false;
            g.Hide();
            log.Append($"\n  {bad} case(s) flagged.");
            Debug.Log(log.ToString());
        }

        /// <summary>The hand mesh centre, the gold dot and the arrow head against the key point, in the island's frame (null = not shown).</summary>
        static Dictionary<string, Vector3> Measure(GhostHands g, IslandBuilder island, string clip, GhostPlace place, bool left)
        {
            g.previewing = true; // lessons and the title hold leave it alone
            g.Show(clip, place, "sidecheck", 0.6f, left, false);
            if (g.ClipName != clip) return null;
            float key = 0.5f;
            var found = typeof(GhostHands).GetField("_clip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (found != null && found.GetValue(g) is GhostHandClip c) key = c.keyTime;
            // The swipe demos: freeze mid-stroke, where the arrow shows.
            if (clip == GhostHands.SwipeAllClip || clip == GhostHands.SwipeTapClip) key = 0.12f;
            g.debugFreeze = key;
            g.DebugPose();
            g.DebugPose();
            var kp = g.KeyPoint;
            if (!kp.HasValue) return null;
            var it = island.transform;
            Vector3 Rel(Vector3 w) => it.InverseTransformVector(w - kp.Value) / Mathf.Max(1e-4f, it.lossyScale.x);
            var result = new Dictionary<string, Vector3>();
            var hand = GameObject.Find(left ? "Ghost Hand L" : "Ghost Hand R");
            if (hand != null)
                foreach (var smr in hand.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if (smr.enabled) { result["hand"] = Rel(smr.bounds.center); break; }
            var dot = GameObject.Find("Ghost Thumb Dot");
            if (dot != null && dot.GetComponent<MeshRenderer>().enabled) result["dot"] = Rel(dot.transform.position);
            var arrow = GameObject.Find("Ghost Stroke Arrow");
            if (arrow != null && arrow.GetComponent<MeshRenderer>().enabled) result["arrow"] = Rel(arrow.GetComponent<MeshRenderer>().bounds.center);
            return result.Count > 0 ? result : null;
        }
    }
}
