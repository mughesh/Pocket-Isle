using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 10f checks through the Claude bridge (exec:Diorama.EditorTools.Step10fDebug.X), in Play. Start with
    /// GalewickPlay.Start; shots with GalewickPlay.Shot / CloseShot (name in arg.txt). Arguments in Temp/ClaudeBridge/arg.txt.
    /// </summary>
    static class Step10fDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static float ArgFloat(float fallback) =>
            float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : fallback;

        /// <summary>Teleport to the Cinderwood tile "x,y" (map x, y as in CinderwoodTools) in arg.txt, travelling there if needed.</summary>
        public static void Teleport()
        {
            var parts = Arg.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)) return;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            var swap = Find<SceneSwap>();
            var e = Find<GestureTestPiece>();
            if (map == null || swap == null || e == null) return;
            var tile = CinderwoodTools.Tile(x, y);
            if (SceneSwap.IsInside) swap.ReturnNow();
            if (e.island.world != map) swap.TravelNow(map, tile, new Vector2Int(0, -1));
            else e.Teleport(tile, new Vector2Int(0, -1));
            Debug.Log($"[Step10fDebug] At x{x} y{y} (c{tile.x} r{tile.y}), walkable {map.CanWalk(tile)}.");
        }

        /// <summary>
        /// A close shot of one spot: arg.txt "name,x,y[,dist]" (Cinderwood map x, y): from the player's side, 18 cm away
        /// (or dist metres), a little above, to Logs/BattleShots/{name}.png.
        /// </summary>
        public static void ShotAt()
        {
            var parts = Arg.Split(',');
            var island = Find<IslandBuilder>();
            if (parts.Length < 3 || island == null || !int.TryParse(parts[1], out int x) || !int.TryParse(parts[2], out int y)) return;
            float dist = parts.Length > 3 && float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float d) ? d : 0.18f;
            var t = island.transform;
            var tile = CinderwoodTools.Tile(x, y);
            var at = t.TransformPoint(island.TileToLocal(tile, island.Content.capHeight + 0.6f * island.map.tileSize));
            var head = Camera.main != null ? Camera.main.transform.position : t.position + t.forward * 0.5f + Vector3.up * 0.4f;
            var flat = Vector3.ProjectOnPlane(head - at, Vector3.up).normalized;
            var eye = at + flat * dist + Vector3.up * dist * 0.55f;
            BattleDebug.Render($"Logs/BattleShots/{parts[0]}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Debug.Log($"[Step10fDebug] Shot {parts[0]}.png at x{x} y{y}.");
        }

        /// <summary>Drops the puzzle id in arg.txt from the save state (back to unsolved), every puzzle re-reads.</summary>
        public static void Reset()
        {
            var saved = PuzzleState.ToSaved();
            int n = saved.RemoveAll(p => p.id == Arg);
            PuzzleState.Restore(saved);
            Debug.Log($"[Step10fDebug] Reset {Arg} ({n} entry).");
        }

        /// <summary>
        /// D: the XP trail check on the current maps (KeeperSim.Trail: start, first grass, plaza, springs, log, temple; the
        /// hollow is on the way now). Works in Edit mode.
        /// </summary>
        public static void Xp()
        {
            var cinder = AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            var sb = new System.Text.StringBuilder("[Step10fDebug] XP trail check (starter Lv 5, 1000 runs):");
            if (cinder != null)
            {
                var path = KeeperSim.Chain(cinder, KeeperSim.Mark(cinder, "start"), KeeperSim.Mark(cinder, "firstGrass"), KeeperSim.Mark(cinder, "plaza"),
                    KeeperSim.Mark(cinder, "springs"), KeeperSim.Mark(cinder, "log"), KeeperSim.Mark(cinder, "temple"));
                sb.Append($"\n  Cinderwood trail arrival -> temple: {path.Count} tiles");
            }
            foreach (float wander in new[] { 1f, 2f, 3f })
            {
                var t = KeeperSim.Trail(1000, wander);
                sb.Append($"\n  x{wander:0} grass: tall grass hub {t.grassHub} + Cinderwood {t.grassCinder}, battles hub {t.battlesHub:0.0} + Cinderwood {t.battlesCinder:0.0}, " +
                          $"starter at the temple Lv {t.levelP10:0} / {t.level:0.0} / {t.levelP90:0} (p10 / mean / p90)");
            }
            Debug.Log(sb.ToString());
        }

        // ---- A: the village flame ----

        /// <summary>Cups the flame with both hands for arg.txt seconds (default 2.5).</summary>
        public static void Cup()
        {
            var cup = Find<FlameCupPiece>();
            if (cup == null) return;
            cup.DebugCup(ArgFloat(2.5f));
        }

        // ---- C: the forge's ember ----

        /// <summary>Look + pinch the ember's vent (it rises and waits).</summary>
        public static void EmberVent()
        {
            var e = Find<ForgeEmberPiece>();
            if (e != null) e.DebugLookPinchVent();
        }

        /// <summary>Look + pinch the firebox while the ember waits (it flies in).</summary>
        public static void EmberFirebox()
        {
            var e = Find<ForgeEmberPiece>();
            if (e != null) e.DebugLookPinchFirebox();
        }

        /// <summary>One bellows pump (blocked while the firebox is cold).</summary>
        public static void Pump()
        {
            foreach (var p in Object.FindObjectsByType<PumpPiece>(FindObjectsSortMode.None))
                if (p.id == GameState.BellowsId) p.DebugStroke();
        }

        /// <summary>
        /// The ember on a synthetic palm: the lesson's palm_carry ghost frozen palm-up (arg.txt = the clip time 0..1, default
        /// 0.5) at the vent, the waiting ember moved onto its palm, then a close shot 10f_ember_palm.png.
        /// </summary>
        public static void EmberPalm()
        {
            var e = Find<ForgeEmberPiece>();
            var g = Find<GhostHands>();
            if (e == null || g == null) return;
            if (e.Carry.State == EmberState.InVent) e.DebugLookPinchVent();
            g.Show("palm_carry", GhostPlace.AtTarget, "puzzle:" + e.id, 1f, DominantHand.Left, false);
            g.debugFreeze = ArgFloat(0.5f);
            g.DebugPose();
            if (g.KeyPoint.HasValue) e.debugAt = g.KeyPoint.Value + Vector3.up * 0.016f;
            Debug.Log($"[Step10fDebug] Ember on the ghost palm at {e.debugAt}.");
        }

        public static void EmberPalmOff()
        {
            var e = Find<ForgeEmberPiece>();
            var g = Find<GhostHands>();
            if (e != null) e.debugAt = null;
            if (g != null)
            {
                g.debugFreeze = -1f;
                g.Hide();
            }
        }

        // ---- B: the smoky hollow ----

        /// <summary>A fingertip touch on the hollow lantern number in arg.txt.</summary>
        public static void Touch()
        {
            var h = Find<LanternHollowPiece>();
            if (h != null && int.TryParse(Arg, out int i)) h.DebugTouch(i);
        }

        /// <summary>Look + pinch on the hollow lantern number in arg.txt.</summary>
        public static void Nudge()
        {
            var h = Find<LanternHollowPiece>();
            if (h != null && int.TryParse(Arg, out int i)) h.DebugNudge(i);
        }

        public static void State()
        {
            var cup = Find<FlameCupPiece>();
            var shrines = Find<HearthShrines>();
            Debug.Log($"[Step10fDebug] State: {(GameState.Instance != null ? GameState.Instance.Where : "?")}, restored {RealmState.Restored:0.##}; " +
                      $"flame: {(cup != null ? $"{cup.Describe()}, usable {cup.Usable}, lesson {cup.LessonShowing}" : "none")}; " +
                      $"ember: {(Find<ForgeEmberPiece>() is ForgeEmberPiece em ? $"{em.Describe()}, usable {em.Usable}, lesson {em.LessonShowing}" : "none")}; " +
                      $"hollow: {(Find<LanternHollowPiece>() is LanternHollowPiece h ? $"{h.Describe()}, usable {h.Usable}, lesson {h.LessonShowing}" : "none")}; " +
                      $"shrine dormant {(cup != null && HearthShrines.IsDormant(cup.map.name, cup.shrine))}, last shrine {(shrines != null && shrines.LastShrine.HasValue ? shrines.LastShrine.Value.ToString() : "none")}; " +
                      $"puzzles {PuzzleState.Describe()}");
        }
    }
}
