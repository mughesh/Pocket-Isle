using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 8d checks through the Claude bridge (exec:Diorama.EditorTools.Step8dDebug.Method).</summary>
    static class Step8dDebug
    {
        /// <summary>Every ISDK Hand in the scene, in FindObjectsByType order (HandLookup takes the first non-synthetic one per side).</summary>
        public static void ListHands()
        {
            foreach (var hand in Object.FindObjectsByType<Hand>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var path = hand.name;
                for (var t = hand.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                Debug.Log($"[Step8dDebug] Hand {path}: {hand.GetType().Name}, {hand.Handedness}, active {hand.isActiveAndEnabled}, " +
                          $"valid {(Application.isPlaying && hand.isActiveAndEnabled ? hand.IsTrackedDataValid.ToString() : "-")}, from {Name(hand.ModifyDataFromSource as Object)}, " +
                          $"tracking-to-world {Name(hand.TrackingToWorldTransformer as Object)}");
            }
            foreach (var side in new[] { Handedness.Left, Handedness.Right })
            {
                var h = HandRegistry.Get(side);
                Debug.Log($"[Step8dDebug] HandRegistry {side}: {(h != null ? h.name : "none")}");
            }
        }

        static string Name(Object o) => o == null ? "-" : o is Component c ? c.gameObject.name + "(" + c.GetType().Name + ")" : o.name;

        /// <summary>The opening shard's contact sphere against its mesh: how far the visible surface is from the sphere, by direction.</summary>
        public static void ShardRadius()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            var shard = GameObject.Find("Opening Shard");
            if (o == null || shard == null)
            {
                Debug.Log("[Step8dDebug] No opening shard showing.");
                return;
            }
            var mesh = shard.GetComponent<MeshFilter>().sharedMesh;
            var e = mesh.bounds.extents;
            float sphere = (e.x + e.y + e.z) / 3f * o.shardSize;
            float min = float.MaxValue, max = 0f;
            foreach (var v in mesh.vertices)
            {
                float d = shard.transform.TransformVector(v).magnitude;
                min = Mathf.Min(min, d);
                max = Mathf.Max(max, d);
            }
            Debug.Log($"[Step8dDebug] Shard: lossy scale {shard.transform.lossyScale.x:0.000}, contact sphere {sphere * 100f:0.0} cm (+slack {o.contactSlack * 100f:0.0}), " +
                      $"visible surface {min * 100f:0.0}..{max * 100f:0.0} cm from the centre, mesh extents {e * o.shardSize * 100f} cm");
        }

        /// <summary>Probes the shard's contact surface from the head's side: closest-point distances at a few points along the view ray.</summary>
        public static void ShardProbe()
        {
            var shard = GameObject.Find("Opening Shard");
            var cam = Camera.main;
            var col = shard != null ? shard.GetComponent<MeshCollider>() : null;
            if (col == null || cam == null)
            {
                Debug.Log("[Step8dDebug] No shard collider.");
                return;
            }
            Vector3 c = shard.transform.position, dir = (cam.transform.position - c).normalized;
            var sb = new System.Text.StringBuilder($"[Step8dDebug] Shard collider convex {col.convex}, trigger {col.isTrigger}, layer {shard.layer}; from the centre toward the head:");
            foreach (float r in new[] { 0f, 0.03f, 0.05f, 0.06f, 0.065f, 0.07f, 0.08f, 0.1f })
            {
                Vector3 p = c + dir * r;
                float d = (Physics.ClosestPoint(p, col, shard.transform.position, shard.transform.rotation) - p).magnitude;
                sb.Append($"\n  {r * 100f:0.0} cm out: {d * 100f:0.0} cm to the surface{(d <= 0.015f ? " (touch)" : "")}");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>Grab frame without hands: a fake open right palm 5 cm off the base's right side (FrameOff clears it).</summary>
        public static void FrameNear() => FramePalm(0.05f);
        /// <summary>The fake palm inside the grab zone (hover).</summary>
        public static void FrameHover() => FramePalm(0f);
        public static void FrameOff() => IslandGrabFrame.DebugPalm = null;

        static void FramePalm(float out_)
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            IslandGrabFrame.DebugPalm = () =>
            {
                var half = island.FootprintHalfSize;
                var t = island.transform;
                return t.TransformPoint(new Vector3(-half.x, island.CapTopLocal - 0.05f, half.y * 0.4f)) - t.right * (0.01f + out_);
            };
        }

        /// <summary>Seat view of the island (the grab frame, pods, bubble): Logs/BattleShots/8d_seat.png, and a close view of the right side.</summary>
        public static void IslandShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var seat = t.position + t.rotation * new Vector3(0f, 0.32f, 0.55f);
            BattleDebug.Render("Logs/BattleShots/8d_seat.png", seat, Quaternion.LookRotation(t.position - seat, Vector3.up));
            var side = t.TransformPoint(new Vector3(island.LocalBounds.min.x, 0f, 0f));
            var near = side + t.rotation * new Vector3(-0.12f, 0.1f, 0.22f);
            BattleDebug.Render("Logs/BattleShots/8d_side.png", near, Quaternion.LookRotation(side - near, Vector3.up));
            Debug.Log("[Step8dDebug] Shots 8d_seat.png, 8d_side.png");
        }

        /// <summary>The move lesson's ghost: one right palm on the base's right side (held as a preview).</summary>
        public static void GhostSide()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            g.previewing = true;
            g.Show("grab_rim_slide", GhostPlace.IslandSides, "rim", 1f, false, false);
        }

        /// <summary>Logs the opening's stage and whether the Spirit is talking.</summary>
        public static void Stage()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o == null) return;
            var f = typeof(Opening).GetField("_stage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Debug.Log($"[Step8dDebug] Opening stage {f.GetValue(o)}, talking {Dialogue.IsTalking}.");
        }

        /// <summary>Game time x5 (reach the starter pods in a short Play run); Normal puts it back.</summary>
        public static void Fast() => Time.timeScale = 5f;
        public static void Normal() => Time.timeScale = 1f;

        /// <summary>Starter pods: looks at the middle one (the next frames), logs the gaps between the pods.</summary>
        public static void GazeMiddlePod()
        {
            var pods = new System.Collections.Generic.List<PodView>();
            foreach (var p in Object.FindObjectsByType<PodView>(FindObjectsSortMode.None))
                if (p.name.StartsWith("Starter Pod")) pods.Add(p);
            pods.Sort((a, b) => b.transform.localPosition.x.CompareTo(a.transform.localPosition.x));
            if (pods.Count < 3)
            {
                Debug.Log($"[Step8dDebug] {pods.Count} starter pods.");
                return;
            }
            var mid = pods[1];
            HeadGaze.DebugAim = () => mid != null ? mid.transform.position : (Vector3?)null;
            var sb = new System.Text.StringBuilder("[Step8dDebug] Starter pods (left to right):");
            for (int i = 0; i < pods.Count; i++)
            {
                sb.Append($" {pods[i].name.Replace("Starter Pod ", "")} r {pods[i].WorldRadius * 100f:0.0} cm");
                if (i > 0)
                {
                    float gap = Vector3.Distance(pods[i].transform.position, pods[i - 1].transform.position) - pods[i].WorldRadius - pods[i - 1].WorldRadius;
                    sb.Append($" (gap {gap * 100f:0.0} cm = {gap / (2f * pods[i].WorldRadius):0.0} diameters)");
                }
            }
            Debug.Log(sb.ToString());
        }

        public static void GazeOff() => HeadGaze.DebugAim = null;
    }
}
