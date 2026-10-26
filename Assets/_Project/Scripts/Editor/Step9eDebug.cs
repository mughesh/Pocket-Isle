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

        // ---- Hearthstag's fury ----

        static KeeperFury ActiveFury()
        {
            foreach (var f in Object.FindObjectsByType<KeeperFury>(FindObjectsSortMode.None))
                if (f.isActiveAndEnabled) return f;
            return null;
        }

        /// <summary>Close-up of the showing Keeper from the player's side: Logs/BattleShots/9e_fury_{name}.png.</summary>
        public static void FuryShot(string name)
        {
            var f = ActiveFury();
            var island = Find<IslandBuilder>();
            if (f == null || island == null)
            {
                Debug.Log("[Step9eDebug] No Keeper showing.");
                return;
            }
            var view = f.GetComponent<CreatureView>();
            float h = view.height * f.transform.lossyScale.x;
            var t = island.transform;
            Vector3 c = f.transform.position + Vector3.up * (h * 0.55f);
            var eye = c + t.rotation * new Vector3(h * 0.4f, h * 0.55f, h * 2.6f);
            BattleDebug.Render($"Logs/BattleShots/9e_fury_{name}.png", eye, Quaternion.LookRotation(c - eye, Vector3.up));
            Debug.Log($"[Step9eDebug] 9e_fury_{name}.png (fury {f.fury:0.00}, phase 2 {f.phase2}, stomping {f.Stomping}).");
        }

        public static void ShotMeet() => FuryShot("meet");
        public static void ShotBattle() => FuryShot("battle");
        public static void ShotPhase2() => FuryShot("phase2");
        public static void ShotStomp() => FuryShot("stomp");
        public static void ShotBurnOff() => FuryShot("burnoff");
        public static void ShotCalm() => FuryShot("calm");

        public static void Stomp()
        {
            var f = ActiveFury();
            if (f != null) f.Stomp(26f);
        }

        /// <summary>The Keeper just above half HP (the next hit starts phase 2).</summary>
        public static void KeeperToHalf()
        {
            var enc = Find<EncounterDirector>();
            var b = enc != null ? enc.CurrentBattle : null;
            if (b == null || b.keeper == null) return;
            b.wild.isling.currentHp = Mathf.CeilToInt(b.wild.MaxHp * 0.52f);
            Debug.Log($"[Step9eDebug] Keeper at {b.wild.Hp}/{b.wild.MaxHp}.");
        }

        /// <summary>Editor frame numbers (fps is the Editor's, not the device's).</summary>
        public static void Stats()
        {
            Debug.Log($"[Step9eDebug] Stats: {1f / Mathf.Max(1e-4f, Time.smoothDeltaTime):0} fps (Editor), batches {UnityEditor.UnityStats.batches}, " +
                      $"set-pass {UnityEditor.UnityStats.setPassCalls}, tris {UnityEditor.UnityStats.triangles / 1000f:0}k.");
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
