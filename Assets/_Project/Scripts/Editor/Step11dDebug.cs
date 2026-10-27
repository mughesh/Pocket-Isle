using System.IO;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11d checks through the Claude bridge (exec:Diorama.EditorTools.Step11dDebug.X), in Play. Start with
    /// GalewickPlay.Start, go with GalewickPlay.Go, shots with GalewickPlay.Shot / CloseShot or Step10fDebug.ShotAt.
    /// Puzzle state with Step10fDebug.Reset / Solve (the save is real: put it back after). Args in Temp/ClaudeBridge/arg.txt.
    /// </summary>
    static class Step11dDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static float F(string s) =>
            float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;

        /// <summary>Every hand moment that's usable or pinned: its chip, gold marks and arrows; the bubble's chip; the window focus.</summary>
        public static void Objectives()
        {
            var sb = new StringBuilder("[Step11dDebug] Objectives:");
            foreach (var p in Object.FindObjectsByType<HandPuzzle>(FindObjectsSortMode.None))
            {
                if (!p.Usable && !p.IsPinned && p.MarksShown == 0 && p.ArrowsShown == 0) continue;
                p.CountObjective(out int done, out int total);
                sb.Append($"\n  {p.id}: usable {p.Usable}, solved {p.IsSolved}, pinned {p.IsPinned}, chip '{p.ChipText}', count {done}/{total}, " +
                          $"gold marks {p.MarksShown}, arrows {p.ArrowsShown}, lesson {p.LessonShowing}");
            }
            var bubble = Find<DialogueBubble>();
            var window = Find<WorldWindow>();
            var tower = Find<TowerDoor>();
            var e = Find<GestureTestPiece>();
            sb.Append($"\n  bubble chip '{(bubble != null ? bubble.ChipText : null)}', dialogue pinned {Dialogue.IsPinned}, window focus {(window != null && window.HasFocus)}, " +
                      $"tower open {(tower != null && tower.IsOpen)}, tower glow {(tower != null && tower.Glowing)}, explorer c{(e != null ? e.Tile.x : 0)} r{(e != null ? e.Tile.y : 0)}, " +
                      $"touch walk {GameSettings.TouchWalk}");
            Debug.Log(sb.ToString());
        }

        /// <summary>arg "where,height,up[,pair]": the scoop's synthetic hand (where = spot, basin or bowl), both hands together when pair = 1.</summary>
        public static void Hand()
        {
            var s = Find<ScoopPour>();
            var parts = Arg.Split(',');
            if (s == null || parts.Length < 3) return;
            // Step 12c: "bowl" = the cliff bowl (height in tiles above the cap, so over its rim is about 2.7).
            var at = parts[0] == "basin" ? s.basin : parts[0] == "bowl" ? s.bowl : s.scoopSpot;
            s.DebugHand(new Vector3(at.x, F(parts[1]), at.y), F(parts[2]), parts.Length > 3 && parts[3] == "1");
            Debug.Log($"[Step11dDebug] Hand at {parts[0]} h {F(parts[1]):0.##} up {F(parts[2]):0.##}{(parts.Length > 3 && parts[3] == "1" ? ", both hands" : "")}: {s.Describe()}, holder {s.Scoop.Holder}.");
        }

        /// <summary>arg 0..1: the ghost frozen at that point of its clip (for a shot); negative = plays again.</summary>
        public static void Ghost()
        {
            var g = Find<GhostHands>();
            if (g == null) return;
            g.debugFreeze = F(Arg);
            g.DebugPose();
            Debug.Log($"[Step11dDebug] Ghost {g.ClipName} frozen at {g.debugFreeze:0.##}.");
        }

        /// <summary>The explorer at the Lilymere tile "x,y" (map column, row), travelling there if needed.</summary>
        public static void LmTeleport()
        {
            var parts = Arg.Split(',');
            var e = Find<GestureTestPiece>();
            if (parts.Length < 2 || e == null) return;
            var t = new Vector2Int(Mathf.RoundToInt(F(parts[0])), Mathf.RoundToInt(F(parts[1])));
            e.Teleport(t, new Vector2Int(0, -1));
            Debug.Log($"[Step11dDebug] At c{e.Tile.x} r{e.Tile.y}.");
        }

        /// <summary>The garden's buds and the window: are all five inside the island window?</summary>
        public static void Buds()
        {
            var g = Find<RainGarden>();
            var island = Find<IslandBuilder>();
            if (g == null || island == null) return;
            int inside = 0;
            foreach (var b in g.buds)
                if (island.FootprintContains(island.TileToLocal(b, island.Content.capHeight))) inside++;
            Debug.Log($"[Step11dDebug] Buds in the window: {inside}/{g.buds.Length}, settled {g.Settled}, buds middle c{g.BudsCenter.x:0.#} r{g.BudsCenter.y:0.#}.");
        }

        /// <summary>The lily pads' views: shown, where (island-local) and how big.</summary>
        public static void PadViews()
        {
            var p = Find<LilyPadBridge>();
            if (p == null) return;
            var f = typeof(LilyPadBridge).GetField("_views", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var views = f != null ? f.GetValue(p) as PropView[] : null;
            var sb = new StringBuilder($"[Step11dDebug] Pad views (here {p.Here}, in view {p.InView}):");
            if (views != null)
                foreach (var v in views)
                    if (v != null) sb.Append($" [{v.Visible}, local {v.transform.localPosition.ToString("F3")}, scale {v.transform.localScale.x:0.000}, recipe {(v.Recipe != null ? v.Recipe.name : "none")}]");
            Debug.Log(sb.ToString());
        }

        /// <summary>A close shot of one map tile: arg "name,column,row[,dist[,height]]" (current map tiles) to Logs/BattleShots/{name}.png.</summary>
        public static void ShotTile()
        {
            var parts = Arg.Split(',');
            var island = Find<IslandBuilder>();
            if (parts.Length < 3 || island == null) return;
            float dist = parts.Length > 3 ? F(parts[3]) : 0.18f;
            float h = parts.Length > 4 ? F(parts[4]) : 0.6f;
            var t = island.transform;
            var at = t.TransformPoint(island.TileToLocal(new Vector2(F(parts[1]), F(parts[2])), island.Content.capHeight + h * island.map.tileSize));
            var head = Camera.main != null ? Camera.main.transform.position : t.position + t.forward * 0.5f + Vector3.up * 0.4f;
            var flat = Vector3.ProjectOnPlane(head - at, Vector3.up).normalized;
            var eye = at + flat * dist + Vector3.up * dist * 0.55f;
            BattleDebug.Render($"Logs/BattleShots/{parts[0]}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Debug.Log($"[Step11dDebug] Shot {parts[0]}.png at c{parts[1]} r{parts[2]}.");
        }

        /// <summary>The settings row: arg 1 = touch walking on, 0 = off.</summary>
        public static void TouchWalk() => GameSettings.TouchWalk = Arg == "1";
    }
}
