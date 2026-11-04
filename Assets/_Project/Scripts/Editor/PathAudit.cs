using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14i F (Mugesh: paths zig-zag one tile at a time): measures every map's paths and draws each map top-down.
    /// Turns = 2 x 2 windows with exactly three path tiles (each right-angle corner and each one-tile jog makes them; a
    /// straight 2-wide band makes none). Jogs = path legs shorter than <see cref="MinLeg"/> between two turns (row or column
    /// runs of a 1- or 2-wide band). Short stops = smart steps (a swipe on a path tile) that stop on a junction after
    /// fewer than 3 tiles (swipes along the path only). Shots: Logs/BattleShots/14i_paths_&lt;map&gt;_&lt;arg&gt;.png (arg "before" / "after").
    /// </summary>
    public static class PathAudit
    {
        public const int MinLeg = 3;
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        public static readonly string[] Maps =
        {
            "Assets/_Project/Data/WorldMap_StillIsle.asset", "Assets/_Project/Data/WorldMap_Cinderwood.asset",
            "Assets/_Project/Data/WorldMap_Galewick.asset", "Assets/_Project/Data/WorldMap_Lilymere.asset",
            "Assets/_Project/Data/Interiors/Lodge.asset", "Assets/_Project/Data/Interiors/ForgeHall.asset",
            "Assets/_Project/Data/Interiors/LakeTempleHall.asset", "Assets/_Project/Data/Interiors/TowerHall.asset",
        };

        [MenuItem("Tools/Pocket Isle/Maps/Path Audit (14i)")]
        public static void Run()
        {
            string tag = File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "now";
            if (string.IsNullOrEmpty(tag)) tag = "now";
            var sb = new StringBuilder($"[PathAudit] Paths ({tag}):");
            foreach (var path in Maps)
            {
                var map = AssetDatabase.LoadAssetAtPath<WorldMap>(path);
                if (map == null) continue;
                var m = Measure(map);
                sb.Append($"\n  {map.name}: {m.pathTiles} path tiles, {m.turns} turns, {m.jogs} jogs (legs < {MinLeg}), {m.shortStops}/{m.swipes} smart steps stop on a junction within 3 tiles");
                Draw(map, $"Logs/BattleShots/14i_paths_{map.name.Replace("WorldMap_", "")}_{tag}.png");
            }
            Debug.Log(sb.ToString());
        }

        public struct Result
        {
            public int pathTiles, turns, jogs, shortStops, swipes;
        }

        static bool P(IslandMap map, int c, int r) => map.At(r, c) == IslandMap.Path;

        public static Result Measure(IslandMap map)
        {
            var res = new Result();
            int rows = map.Rows, cols = map.Columns;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (P(map, c, r)) res.pathTiles++;
                    if (r + 1 >= rows || c + 1 >= cols) continue;
                    int n = (P(map, c, r) ? 1 : 0) + (P(map, c + 1, r) ? 1 : 0) + (P(map, c, r + 1) ? 1 : 0) + (P(map, c + 1, r + 1) ? 1 : 0);
                    if (n == 3) res.turns++;
                }
            res.jogs = Jogs(map);
            var tiles = new List<Vector2Int>();
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (!P(map, c, r) || !map.CanWalk(new Vector2Int(c, r))) continue;
                    foreach (var d in dirs)
                    {
                        var next = new Vector2Int(c, r) + d;
                        if (!P(map, next.x, next.y) || !map.CanWalk(next)) continue;
                        // Only swipes along the path (it carries on behind): not across a 2-wide band onto its other lane.
                        if (!P(map, c - d.x, r - d.y)) continue;
                        res.swipes++;
                        string why = SmartStep.Plan(map, new Vector2Int(c, r), d, false, SmartStep.SmartTiles, null, tiles);
                        if (why == SmartStep.Junction && tiles.Count < 3) res.shortStops++;
                    }
                }
            return res;
        }

        /// <summary>Bridge: the map in arg.txt (asset name) as text with '!' where a swipe stops on a junction within 3 tiles.</summary>
        public static void ShortStops()
        {
            string name = File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "WorldMap_Cinderwood";
            string path = System.Array.Find(Maps, p => Path.GetFileNameWithoutExtension(p) == name);
            var map = path != null ? AssetDatabase.LoadAssetAtPath<WorldMap>(path) : null;
            if (map == null) return;
            var tiles = new List<Vector2Int>();
            var marks = new HashSet<Vector2Int>();
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                    foreach (var d in dirs)
                    {
                        var at = new Vector2Int(c, r);
                        if (!P(map, c, r) || !P(map, c + d.x, r + d.y) || !P(map, c - d.x, r - d.y) || !map.CanWalk(at + d)) continue;
                        if (SmartStep.Plan(map, at, d, false, SmartStep.SmartTiles, null, tiles) == SmartStep.Junction && tiles.Count < 3)
                            marks.Add(tiles.Count > 0 ? tiles[tiles.Count - 1] : at);
                    }
            var sb = new StringBuilder($"[PathAudit] {name}: '!' = a smart step's junction stop within 3 tiles ({marks.Count} tiles):\n");
            for (int r = 0; r < map.Rows; r++)
            {
                for (int c = 0; c < map.Columns; c++)
                    sb.Append(marks.Contains(new Vector2Int(c, r)) ? '!' : P(map, c, r) ? '=' : IslandMap.IsWalkable(map.At(r, c)) ? '.' : '#');
                sb.Append('\n');
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// One-tile jogs: a path run (row or column, a band 1-2 wide) that steps sideways by one or two tiles and carries on
        /// within fewer than <see cref="MinLeg"/> tiles: counted as staircase steps, a 2 x 2 window of 3 path tiles whose
        /// nearest other such window is within MinLeg - 1 tiles along a row or column.
        /// </summary>
        static int Jogs(IslandMap map)
        {
            var corners = new List<Vector2Int>();
            int rows = map.Rows, cols = map.Columns;
            for (int r = 0; r + 1 < rows; r++)
                for (int c = 0; c + 1 < cols; c++)
                {
                    int n = (P(map, c, r) ? 1 : 0) + (P(map, c + 1, r) ? 1 : 0) + (P(map, c, r + 1) ? 1 : 0) + (P(map, c + 1, r + 1) ? 1 : 0);
                    if (n == 3) corners.Add(new Vector2Int(c, r));
                }
            int jogs = 0;
            for (int i = 0; i < corners.Count; i++)
                for (int j = i + 1; j < corners.Count; j++)
                {
                    var d = corners[j] - corners[i];
                    int along = Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)), across = Mathf.Min(Mathf.Abs(d.x), Mathf.Abs(d.y));
                    // Two corners of one staircase step: close along, offset by 1-2 across (not the two corners of a 2-wide L).
                    if (along >= 1 && along < MinLeg && across >= 1 && across <= 2 && Mathf.Abs(d.x) != Mathf.Abs(d.y)) jogs++;
                }
            return jogs;
        }

        static readonly Dictionary<char, Color32> Colors = new Dictionary<char, Color32>
        {
            { IslandMap.Path, new Color32(222, 205, 160, 255) }, { IslandMap.Grass, new Color32(120, 180, 90, 255) },
            { IslandMap.TallGrass, new Color32(60, 125, 50, 255) }, { IslandMap.Tree, new Color32(30, 75, 35, 255) },
            { IslandMap.Rock, new Color32(110, 110, 110, 255) }, { IslandMap.Water, new Color32(70, 130, 200, 255) },
            { IslandMap.HotSpring, new Color32(120, 200, 210, 255) }, { IslandMap.Blocked, new Color32(20, 20, 24, 255) },
            { IslandMap.DryBed, new Color32(170, 150, 110, 255) }, { IslandMap.Mud, new Color32(110, 85, 60, 255) },
        };

        /// <summary>The map top-down, 8 px a tile, row 0 at the top; props as a red dot.</summary>
        public static void Draw(WorldMap map, string file)
        {
            const int px = 8;
            int rows = map.Rows, cols = map.Columns;
            var tex = new Texture2D(cols * px, rows * px, TextureFormat.RGBA32, false);
            var pixels = new Color32[cols * px * rows * px];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char t = map.At(r, c);
                    var col = Colors.TryGetValue(t, out var k) ? k : new Color32(200, 60, 200, 255);
                    bool prop = map.PropAt(r, c) != IslandMap.Empty;
                    for (int y = 0; y < px; y++)
                        for (int x = 0; x < px; x++)
                        {
                            var cc = col;
                            if (prop && x >= 2 && x < 6 && y >= 2 && y < 6) cc = new Color32(210, 50, 40, 255);
                            if (x == 0 || y == 0) cc = new Color32((byte)(cc.r * 0.85f), (byte)(cc.g * 0.85f), (byte)(cc.b * 0.85f), 255);
                            // Row 0 (the back edge) at the top of the picture.
                            pixels[((rows - 1 - r) * px + (px - 1 - y)) * cols * px + c * px + x] = cc;
                        }
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
