using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 29 A0 (Mugesh: turns on one-tile paths are too sharp to swipe): every road (P) segment one tile wide gets a
    /// second lane, by hand in the saved maps (never through a generator, which would re-roll the hand edits). The lane is
    /// the neighbour on one consistent side of the segment: grass first, then trees and rocks (scenery terrain). Never a
    /// prop's footprint, water, dry bed, mud, springs, tall grass, the border, a gate or a door and its front, a landmark,
    /// or any tile a scene script names for this map (puzzles, doors, trails). A tree or rock never opens next to a
    /// blocking prop or a puzzle tile (that could walk round a log or a vine). Roads with water or a lagoon on both sides
    /// are bridges and stay as they are. Pure core (<see cref="Widen"/>) + bridge tools (Audit, Apply, Shots).
    /// </summary>
    public static class Step29Paths
    {
        public const string OutDir = "Logs/Step29";

        public sealed class Result
        {
            public readonly List<Vector2Int> Widened = new List<Vector2Int>();
            public readonly List<Vector2Int> Corners = new List<Vector2Int>();
            public readonly List<string> Segments = new List<string>();
            public readonly List<string> Exceptions = new List<string>();
            public readonly List<Vector2Int> ExceptionTiles = new List<Vector2Int>();
            public readonly List<string> Reported = new List<string>();
            public readonly List<Vector2Int> ReportedTiles = new List<Vector2Int>();
            public string[] Rows;
        }

        static char At(string[] rows, Vector2Int t) =>
            t.y >= 0 && t.y < rows.Length && t.x >= 0 && t.x < rows[t.y].Length ? rows[t.y][t.x] : IslandMap.Empty;

        static bool IsP(string[] rows, Vector2Int t) => At(rows, t) == IslandMap.Path;

        static bool Wet(char c) => c == IslandMap.Water || c == IslandMap.DryBed || c == IslandMap.Mud || c == IslandMap.HotSpring;

        /// <summary>
        /// Plans the second lanes. <paramref name="locked"/>: tiles that must never change. <paramref name="nearBlock"/>:
        /// tiles next to a blocking prop or puzzle tile (grass may still become road there, trees and rocks not).
        /// </summary>
        public static Result Widen(string[] source, Func<Vector2Int, bool> locked, Func<Vector2Int, bool> nearBlock,
            Func<Vector2Int, bool> propBlocks = null)
        {
            var res = new Result();
            var grid = source.Select(r => r.ToCharArray()).ToArray();
            var orig = source;
            int rowsN = source.Length;

            // Walkable areas before: a tree or rock that turns to road may only touch one of them, so no new way round a
            // log, a vine or a gate opens (the arrival vine in Cinderwood is the only way on).
            var area = new Dictionary<Vector2Int, int>();
            bool WalkOrig(Vector2Int t) => IslandMap.IsWalkable(At(orig, t)) && (propBlocks == null || !propBlocks(t));
            {
                int id = 0;
                for (int y = 0; y < rowsN; y++)
                    for (int x = 0; x < orig[y].Length; x++)
                    {
                        var s0 = new Vector2Int(x, y);
                        if (area.ContainsKey(s0) || !WalkOrig(s0)) continue;
                        id++;
                        var q = new Queue<Vector2Int>();
                        q.Enqueue(s0);
                        area[s0] = id;
                        while (q.Count > 0)
                        {
                            var c = q.Dequeue();
                            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                            {
                                var n = c + d;
                                if (area.ContainsKey(n) || !WalkOrig(n)) continue;
                                area[n] = id;
                                q.Enqueue(n);
                            }
                        }
                    }
            }

            // A tree or rock may open only if every walkable tile it touches is in one area (then it joins that area).
            bool JoinsOneArea(Vector2Int t)
            {
                int found = 0;
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (area.TryGetValue(t + d, out var a))
                    {
                        if (found != 0 && a != found) return false;
                        found = a;
                    }
                if (found == 0) return false;
                area[t] = found;
                return true;
            }

            // 1. One-wide road tiles, by axis (true = runs along a row).
            var oneWide = new Dictionary<Vector2Int, bool>();
            for (int y = 0; y < rowsN; y++)
                for (int x = 0; x < orig[y].Length; x++)
                {
                    var t = new Vector2Int(x, y);
                    if (!IsP(orig, t)) continue;
                    bool h = IsP(orig, t + Vector2Int.left) || IsP(orig, t + Vector2Int.right);
                    bool v = IsP(orig, t + Vector2Int.up) || IsP(orig, t + Vector2Int.down);
                    bool hw = h && !IsP(orig, t + Vector2Int.up) && !IsP(orig, t + Vector2Int.down);
                    bool vw = v && !IsP(orig, t + Vector2Int.left) && !IsP(orig, t + Vector2Int.right);
                    if (hw) oneWide[t] = true;
                    else if (vw) oneWide[t] = false;
                }

            // 2. Segments: straight runs of one-wide tiles on the same axis.
            var seen = new HashSet<Vector2Int>();
            var segments = new List<(List<Vector2Int> tiles, bool horizontal)>();
            foreach (var kv in oneWide.OrderBy(k => k.Key.y).ThenBy(k => k.Key.x))
            {
                if (seen.Contains(kv.Key)) continue;
                var axis = kv.Value ? Vector2Int.right : Vector2Int.up;
                var start = kv.Key;
                while (oneWide.TryGetValue(start - axis, out var a) && a == kv.Value) start -= axis;
                var list = new List<Vector2Int>();
                for (var t = start; oneWide.TryGetValue(t, out var b) && b == kv.Value; t += axis)
                {
                    list.Add(t);
                    seen.Add(t);
                }
                segments.Add((list, kv.Value));
            }

            bool Allowed(Vector2Int n, out int score)
            {
                score = 0;
                char c = At(orig, n);
                if (locked(n)) return false;
                if (c == IslandMap.Grass) { score = 2; return true; }
                if ((c == IslandMap.Tree || c == IslandMap.Rock) && !nearBlock(n)) { score = 1; return true; }
                return false;
            }

            // Allowed, and for a tree or rock, it joins only one walkable area (checked when it is really placed).
            bool Place(Vector2Int n)
            {
                if (!Allowed(n, out _)) return false;
                if (IslandMap.IsWalkable(At(orig, n))) return true;
                return JoinsOneArea(n);
            }

            string Name(Vector2Int t) => $"c{t.x} r{t.y}";

            // 3. One side per segment.
            foreach (var (tiles, horizontal) in segments)
            {
                var perp = horizontal ? Vector2Int.up : Vector2Int.right; // +row (toward the seat) / +column
                string label = $"{(horizontal ? "row" : "column")} {(horizontal ? tiles[0].y : tiles[0].x)}, {Name(tiles[0])} to {Name(tiles[tiles.Count - 1])} ({tiles.Count})";

                // A bridge: water (or the lagoon, mud, springs) on both sides of every tile.
                if (tiles.All(t => Wet(At(orig, t + perp)) && Wet(At(orig, t - perp))))
                {
                    res.Exceptions.Add($"{label}: bridge over water");
                    res.ExceptionTiles.AddRange(tiles);
                    continue;
                }

                int Score(Vector2Int side, out int bad)
                {
                    int s = 0;
                    bad = 0;
                    foreach (var t in tiles)
                    {
                        var n = t + side;
                        if (IsP(orig, n)) continue;
                        if (Allowed(n, out var sc)) s += sc;
                        else bad++;
                    }
                    return s;
                }

                // Tie-break: the side the road turns to at its ends (the inside of the bend, fewer new tiles).
                int Turn(Vector2Int side)
                {
                    int n = 0;
                    var axis = horizontal ? Vector2Int.right : Vector2Int.up;
                    if (IsP(orig, tiles[0] - axis + side)) n++;
                    if (IsP(orig, tiles[tiles.Count - 1] + axis + side)) n++;
                    return n;
                }

                int sa = Score(perp, out var ba), sb = Score(-perp, out var bb);
                Vector2Int pick;
                if (ba != bb) pick = ba < bb ? perp : -perp;
                else if (sa != sb) pick = sa > sb ? perp : -perp;
                else pick = Turn(perp) >= Turn(-perp) ? perp : -perp;

                int added = 0;
                var other = new List<string>();
                foreach (var t in tiles)
                {
                    var n = t + pick;
                    if (grid[n.y][n.x] == IslandMap.Path) continue;
                    if (Place(n))
                    {
                        grid[n.y][n.x] = IslandMap.Path;
                        res.Widened.Add(n);
                        added++;
                        continue;
                    }
                    // A locked tile that is already walkable (a landmark on grass): the corridor is two wide there anyway;
                    // keep to the one side rather than jog over.
                    if (IslandMap.IsWalkable(At(orig, n)))
                    {
                        res.Segments.Add($"  gap in the lane at {Name(n)} ({At(orig, n)}, kept: {DescribeLock(locked, n)})");
                        continue;
                    }
                    var m = t - pick;
                    if (grid[m.y][m.x] != IslandMap.Path && Place(m))
                    {
                        grid[m.y][m.x] = IslandMap.Path;
                        res.Widened.Add(m);
                        other.Add(Name(t));
                        added++;
                        continue;
                    }
                    if (grid[m.y][m.x] == IslandMap.Path) continue;
                    res.Reported.Add($"{Name(t)} on {label}: both sides blocked ({At(orig, n)} {DescribeLock(locked, n)} / {At(orig, m)} {DescribeLock(locked, m)})");
                    res.ReportedTiles.Add(t);
                }
                string sideName = horizontal ? (pick.y > 0 ? "+row side" : "-row side") : (pick.x > 0 ? "+column side" : "-column side");
                res.Segments.Add($"{label}: {added} tile(s) on the {sideName}{(other.Count > 0 ? $"; other side at {string.Join(", ", other)}" : "")}");
            }

            // 4. Corners: one pass. A 2 x 2 block with three road tiles, at least one old and one new, gets its fourth, so a
            // bend has no notch. One pass only: a second would grow a staircase into the grass.
            {
                var add = new List<Vector2Int>();
                foreach (var w in res.Widened)
                    for (int ox = -1; ox <= 0; ox++)
                        for (int oy = -1; oy <= 0; oy++)
                        {
                            var b = w + new Vector2Int(ox, oy);
                            var block = new[] { b, b + Vector2Int.right, b + Vector2Int.up, b + Vector2Int.one };
                            int p = 0, old = 0;
                            Vector2Int hole = default;
                            foreach (var q in block)
                                if (q.y >= 0 && q.y < rowsN && q.x >= 0 && q.x < grid[q.y].Length && grid[q.y][q.x] == IslandMap.Path)
                                {
                                    p++;
                                    if (IsP(orig, q)) old++;
                                }
                                else hole = q;
                            if (p == 3 && old > 0 && !add.Contains(hole) && Place(hole)) add.Add(hole);
                        }
                foreach (var a in add.Distinct())
                {
                    grid[a.y][a.x] = IslandMap.Path;
                    res.Corners.Add(a);
                }
            }

            res.Rows = grid.Select(r => new string(r)).ToArray();
            return res;
        }

        static string DescribeLock(Func<Vector2Int, bool> locked, Vector2Int t) => locked(t) ? "locked" : "";

        // ---- the real maps ----

        /// <summary>What may not change on a map, and what a tree or rock may not open beside.</summary>
        public static void Protection(WorldMap map, Dictionary<string, RectInt> doors, out HashSet<Vector2Int> locked, out HashSet<Vector2Int> nearBlock,
            out List<string> sources)
        {
            locked = new HashSet<Vector2Int>();
            nearBlock = new HashSet<Vector2Int>();
            sources = new List<string>();
            var blockers = new HashSet<Vector2Int>();
            // Props: every footprint (blocking or not); blocking ones also guard their ring against tree/rock openings.
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (!map.TryGetProp(map.PropAt(r, c), out var e)) continue;
                    var f = WorldMap.FootprintOf(e);
                    for (int dy = 0; dy < f.y; dy++)
                        for (int dx = 0; dx < f.x; dx++)
                        {
                            var t = new Vector2Int(c + dx, r + dy);
                            locked.Add(t);
                            if (e.recipe.blocksWalking) blockers.Add(t);
                        }
                }
            // Gates: both tiles and both front tiles.
            foreach (var g in map.gates)
                for (int s = 0; s < 2; s++)
                {
                    locked.Add(g.tile + new Vector2Int(s, 0));
                    locked.Add(g.Front(s));
                }
            // Doors (the Keeper door, the shrine) and the row in front.
            foreach (var (name, rect, _) in Step27Setup.Doors(map, doors))
                for (int y = rect.yMin; y <= rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                        locked.Add(new Vector2Int(x, y));
            foreach (var l in map.landmarks) locked.Add(l.tile);
            locked.Add(map.startTile);
            // Arrival tiles on this map from any gate on any map.
            foreach (var other in Step27Setup.Maps())
                foreach (var g in other.gates)
                    if (g.to == map) locked.Add(g.arrive);
            // Scene scripts that name this map: every tile they hold.
            foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || !NamesMap(mb, map)) continue;
                int n = 0;
                foreach (var t in TilesOf(mb))
                {
                    locked.Add(t);
                    blockers.Add(t);
                    n++;
                }
                if (n > 0) sources.Add($"{mb.GetType().Name} ({mb.name}): {n} tile(s)");
            }
            foreach (var b in blockers)
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        nearBlock.Add(b + new Vector2Int(dx, dy));
            // Border.
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                    if (map.Raw(r, c) == IslandMap.Blocked) locked.Add(new Vector2Int(c, r));
        }

        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static bool Serialized(FieldInfo f) => f.IsPublic && !f.IsDefined(typeof(NonSerializedAttribute)) || f.IsDefined(typeof(SerializeField));

        static bool NamesMap(MonoBehaviour mb, WorldMap map)
        {
            for (var t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (var f in t.GetFields(Fields | BindingFlags.DeclaredOnly))
                {
                    if (!Serialized(f)) continue;
                    if (typeof(IslandMap).IsAssignableFrom(f.FieldType) && ReferenceEquals(f.GetValue(mb), map)) return true;
                    // One level down (a puzzle holding a pump that holds the map).
                    if (typeof(MonoBehaviour).IsAssignableFrom(f.FieldType) && f.GetValue(mb) is MonoBehaviour inner && inner != null && inner != mb)
                        foreach (var g in inner.GetType().GetFields(Fields))
                            if (Serialized(g) && typeof(IslandMap).IsAssignableFrom(g.FieldType) && ReferenceEquals(g.GetValue(inner), map)) return true;
                }
            return false;
        }

        static IEnumerable<Vector2Int> TilesOf(MonoBehaviour mb)
        {
            for (var t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                foreach (var f in t.GetFields(Fields | BindingFlags.DeclaredOnly))
                {
                    if (!Serialized(f)) continue;
                    var v = f.GetValue(mb);
                    if (v is Vector2Int one)
                    {
                        if (one != Vector2Int.zero && !f.Name.ToLowerInvariant().Contains("facing") && !f.Name.ToLowerInvariant().Contains("dir")) yield return one;
                    }
                    else if (v is RectInt rect)
                    {
                        foreach (var p in rect.allPositionsWithin) yield return p;
                    }
                    else if (v is IEnumerable list && !(v is string))
                    {
                        foreach (var o in list)
                            if (o is Vector2Int p) yield return p;
                    }
                }
        }

        static WorldMap[] RealmMaps() => Step27Setup.Maps().Where(m => m.Rows > 12).OrderBy(m => m.name).ToArray();

        /// <summary>Bridge: plans every map, writes the report and before/after pictures, changes nothing.</summary>
        public static void Audit() => Run(false);

        /// <summary>Bridge: plans and writes the new layouts into the map assets.</summary>
        public static void Apply() => Run(true);

        static void Run(bool apply)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Step29Paths] Stop Play first.");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("MR_Main.unity")) EditorSceneManager.OpenScene("Assets/_Project/Scenes/MR_Main.unity");
            Directory.CreateDirectory(OutDir);
            var doors = Step27Setup.SceneDoors();
            var md = new StringBuilder();
            md.AppendLine($"# Step 29 A0: one-tile roads widened ({(apply ? "applied" : "audit only")})\n");
            int total = 0;
            foreach (var map in RealmMaps())
            {
                var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Protection(map, doors, out var locked, out var nearBlock, out var sources);
                var res = Widen(rows, locked.Contains, nearBlock.Contains, map.PropBlocks);
                md.AppendLine($"## {map.name}: {res.Widened.Count} lane tile(s), {res.Corners.Count} corner tile(s)\n");
                md.AppendLine("Scene scripts naming this map: " + (sources.Count == 0 ? "none" : string.Join("; ", sources)) + "\n");
                foreach (var s in res.Segments) md.AppendLine("- " + s);
                foreach (var s in res.Exceptions) md.AppendLine("- **Exception:** " + s);
                foreach (var s in res.Reported) md.AppendLine("- **Left one wide:** " + s);
                md.AppendLine();
                Picture(rows, null, $"{OutDir}/{map.name}_before.png");
                Picture(res.Rows, res, $"{OutDir}/{map.name}_after.png");
                var marked = res.Rows.Select(r => r.ToCharArray()).ToArray();
                foreach (var t in res.Widened) marked[t.y][t.x] = '+';
                foreach (var t in res.Corners) marked[t.y][t.x] = '*';
                foreach (var t in res.ExceptionTiles) marked[t.y][t.x] = '=';
                foreach (var t in res.ReportedTiles) marked[t.y][t.x] = '1';
                File.WriteAllText($"{OutDir}/{map.name}_after.txt", string.Join("\n", marked.Select(r => new string(r))));
                total += res.Widened.Count + res.Corners.Count;
                if (apply && res.Widened.Count + res.Corners.Count > 0)
                {
                    map.SetLayout(string.Join("\n", res.Rows));
                    EditorUtility.SetDirty(map);
                }
            }
            if (apply) AssetDatabase.SaveAssets();
            File.WriteAllText($"{OutDir}/paths.md", md.ToString());
            Debug.Log($"[Step29Paths] {(apply ? "Applied" : "Audit")}: {total} tile(s) to road. Report {OutDir}/paths.md");
        }

        // ---- top-down pictures ----

        static Color Tint(char c)
        {
            switch (c)
            {
                case IslandMap.Path: return new Color(0.86f, 0.74f, 0.52f);
                case IslandMap.Grass: return new Color(0.45f, 0.70f, 0.35f);
                case IslandMap.TallGrass: return new Color(0.22f, 0.50f, 0.20f);
                case IslandMap.Water: return new Color(0.30f, 0.55f, 0.85f);
                case IslandMap.DryBed: return new Color(0.62f, 0.55f, 0.45f);
                case IslandMap.Mud: return new Color(0.45f, 0.35f, 0.25f);
                case IslandMap.HotSpring: return new Color(0.55f, 0.80f, 0.85f);
                case IslandMap.Tree: return new Color(0.12f, 0.30f, 0.15f);
                case IslandMap.Rock: return new Color(0.45f, 0.45f, 0.48f);
                default: return new Color(0.1f, 0.1f, 0.1f);
            }
        }

        /// <summary>8 px per tile, row 0 at the top. After: new lanes magenta, corners violet, exceptions red frame, one-wide left yellow frame.</summary>
        public static void Picture(string[] rows, Result res, string path)
        {
            const int px = 8;
            int h = rows.Length, w = rows.Max(r => r.Length);
            var tex = new Texture2D(w * px, h * px, TextureFormat.RGB24, false);
            var widened = res != null ? new HashSet<Vector2Int>(res.Widened) : new HashSet<Vector2Int>();
            var corners = res != null ? new HashSet<Vector2Int>(res.Corners) : new HashSet<Vector2Int>();
            var ex = res != null ? new HashSet<Vector2Int>(res.ExceptionTiles) : new HashSet<Vector2Int>();
            var rep = res != null ? new HashSet<Vector2Int>(res.ReportedTiles) : new HashSet<Vector2Int>();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var t = new Vector2Int(x, y);
                    char c = x < rows[y].Length ? rows[y][x] : '.';
                    var col = Tint(c);
                    if (widened.Contains(t)) col = new Color(1f, 0.2f, 0.8f);
                    else if (corners.Contains(t)) col = new Color(0.6f, 0.3f, 1f);
                    Color frame = ex.Contains(t) ? Color.red : rep.Contains(t) ? Color.yellow : col * 0.85f;
                    for (int dy = 0; dy < px; dy++)
                        for (int dx = 0; dx < px; dx++)
                        {
                            bool edge = dx == 0 || dy == 0 || dx == px - 1 || dy == px - 1;
                            tex.SetPixel(x * px + dx, (h - 1 - y) * px + dy, edge ? frame : col);
                        }
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
