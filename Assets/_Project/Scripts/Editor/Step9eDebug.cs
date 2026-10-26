using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 9e checks through the Claude bridge (exec:Diorama.EditorTools.Step9eDebug.X), in Play. Ghost shots: show
    /// (GhostRim / GhostPod / GhostShard / GhostLog), hold the key moment (GhostKey), then Step8bDebug.GhostShot.
    /// </summary>
    static class Step9eDebug
    {
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        public static void GhostKeys()
        {
            var g = Find<GhostHands>();
            if (g != null) Debug.Log($"[Step9eDebug] Ghost keys:{g.DescribeKeys()}");
        }

        static void Show(string clip, GhostPlace place, string target)
        {
            var g = Find<GhostHands>();
            if (g == null) return;
            g.previewing = true;
            g.Show(clip, place, target, 1f, false, false);
        }

        public static void GhostRim() => Show("grab_rim_slide", GhostPlace.IslandSides, "rim");
        public static void GhostPod() => Show("grab_toss", GhostPlace.AtTarget, "leadPod");
        public static void GhostShard() => Show("grab_drop", GhostPlace.AtTarget, "fireShard");
        public static void GhostLog() => Show("grab_drop", GhostPlace.AtTarget, "puzzle:cinder.log");
        public static void GhostBellows() => Show("poke", GhostPlace.AtTarget, "puzzle:cinder.bellows");

        /// <summary>Holds the showing clip at its key time.</summary>
        public static void GhostKey()
        {
            var g = Find<GhostHands>();
            var lib = GhostClipTools.Library();
            var c = lib != null && g != null && g.ClipName != null ? lib.Find(g.ClipName) : null;
            if (g == null) return;
            g.debugFreeze = c != null ? c.keyTime : 0.5f;
            Debug.Log($"[Step9eDebug] Holding {g.ClipName} at {g.debugFreeze:0.00}, key point {(g.KeyPoint.HasValue ? g.KeyPoint.Value.ToString("F3") : "-")}.");
        }

        public static void GhostDone()
        {
            var g = Find<GhostHands>();
            if (g == null) return;
            g.debugFreeze = -1f;
            g.previewing = false;
            g.Hide();
        }

        /// <summary>Puts the Spirit's home 5 cm below the cap top (inside the base) and reports where it ends up.</summary>
        public static void SpiritSink()
        {
            var s = Find<LanternSpirit>();
            if (s == null) return;
            s.cornerOffset.y = -0.05f;
            Debug.Log("[Step9eDebug] Spirit home moved 5 cm below the cap: SpiritReport next frame.");
        }

        public static void SpiritReport()
        {
            var s = Find<LanternSpirit>();
            var island = Find<IslandBuilder>();
            if (s == null || island == null) return;
            var p = island.transform.InverseTransformPoint(s.transform.position);
            var half = island.FootprintHalfSize;
            Debug.Log($"[Step9eDebug] Spirit island-local {p.ToString("F3")}, cap top {island.CapTopLocal:F3}, footprint half {half.x:F3} x {half.y:F3}: " +
                      $"{(p.y >= island.CapTopLocal || Mathf.Abs(p.x) > half.x || Mathf.Abs(p.z) > half.y ? "outside" : "INSIDE")}.");
            s.cornerOffset.y = 0.075f;
        }

        /// <summary>Holds the hover ring on a tile behind a tree near the explorer and shoots it from the seat.</summary>
        public static void RingShot()
        {
            var cursor = Find<TileCursor>();
            var island = Find<IslandBuilder>();
            var explorer = Find<GestureTestPiece>();
            var w = island != null ? island.world : null;
            if (cursor == null || w == null || explorer == null) return;
            var at = explorer.Tile;
            Vector2Int best = at;
            for (int r = 1; r < 4 && best == at; r++)
                for (int dy = -r; dy <= r && best == at; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        var t = at + new Vector2Int(dx, dy);
                        // A walkable tile with a tree beside it (in front as the player sees it, either way round).
                        if (w.CanWalk(t) && (w.At(t + Vector2Int.up) == IslandMap.Tree || w.At(t + Vector2Int.down) == IslandMap.Tree))
                        {
                            best = t;
                            break;
                        }
                    }
            cursor.debugHover = best;
            Debug.Log($"[Step9eDebug] Hover ring held on {best} (explorer {at}). Shoot with RingShoot.");
        }

        public static void RingShoot()
        {
            var island = Find<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var seat = t.position + t.rotation * new Vector3(0f, 0.32f, 0.5f);
            BattleDebug.Render("Logs/BattleShots/9e_ring_seat.png", seat, Quaternion.LookRotation(t.position - seat, Vector3.up));
            var low = t.position + t.rotation * new Vector3(0f, 0.12f, 0.45f);
            BattleDebug.Render("Logs/BattleShots/9e_ring_low.png", low, Quaternion.LookRotation(t.position - low, Vector3.up));
            var cursor = Find<TileCursor>();
            if (cursor != null) cursor.debugHover = null;
            Debug.Log("[Step9eDebug] Ring shots 9e_ring_seat.png, 9e_ring_low.png");
        }
    }
}
