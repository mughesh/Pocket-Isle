using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 11 evening fix list, section A (walking). Bridge methods:
    /// <see cref="Dump"/> writes every map's layout, props and legend as text (Logs/WalkSim/&lt;map&gt;_*.txt);
    /// <see cref="Sim"/> runs <see cref="WalkSim"/> over every map and writes Logs/WalkSim/report.md;
    /// <see cref="Pictures"/> draws every realm top-down (Logs/WalkSim/&lt;map&gt;_&lt;arg&gt;.png).
    /// </summary>
    public static class Nov11Walk
    {
        public const string OutDir = "Logs/WalkSim";
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        public static readonly string[] Maps = PathAudit.Maps;

        static string Arg(string fallback)
        {
            string a = File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
            return string.IsNullOrEmpty(a) ? fallback : a;
        }

        public static IEnumerable<WorldMap> AllMaps()
        {
            foreach (var p in Maps)
            {
                var m = AssetDatabase.LoadAssetAtPath<WorldMap>(p);
                if (m != null) yield return m;
            }
        }

        /// <summary>The stops the game applies on a map, from the map and the open scene's doors.</summary>
        public static System.Func<Vector2Int, Vector2Int, WalkStop> Stops(WorldMap map, bool firstGrass) =>
            WalkSim.GameStops(map, GestureTestPiece.DoorFootprints(map), firstGrass);

        public static List<WalkSim.Report> RunAll(bool firstGrass)
        {
            var list = new List<WalkSim.Report>();
            foreach (var map in AllMaps())
            {
                map.ClearRuntimeChanges();
                list.Add(WalkSim.Run(map, Stops(map, firstGrass)));
            }
            return list;
        }

        public static void Sim()
        {
            Directory.CreateDirectory(OutDir);
            var sb = new StringBuilder("# Walking simulation (Nov 11)\n\nEvery walkable tile x 4 swipes, walking on until something stops it, with the game's stops " +
                                       "(gates, doors, shrines, the healing bloom; the scene's hand moments are not simulated).\n");
            foreach (bool grass in new[] { false, true })
            {
                sb.AppendLine($"\n## {(grass ? "Before the first wild Isling (stops at the first tall grass)" : "After the first wild Isling")}\n");
                sb.AppendLine("| map | starts | plans | failed | by kind | ends |\n|---|---|---|---|---|---|");
                var reports = RunAll(grass);
                foreach (var r in reports)
                {
                    string kinds = string.Join("; ", r.byKind.Select(k => $"{k.Value} x {k.Key}"));
                    string ends = string.Join("; ", r.ends.OrderByDescending(e => e.Value).Select(e => $"{e.Key} {e.Value}"));
                    sb.AppendLine($"| {r.map} | {r.starts} | {r.plans} | {r.failed} | {kinds} | {ends} |");
                }
                foreach (var r in reports)
                {
                    if (r.failed == 0) continue;
                    sb.AppendLine($"\n### {r.map}: failing start tiles ({Mathf.Min(r.failures.Count, WalkSim.Report.MaxListed)} of {r.failed})\n");
                    foreach (var f in r.failures) sb.AppendLine("- " + f);
                }
                Debug.Log($"[Nov11Walk] Sim ({(grass ? "first grass on" : "first grass off")}): " + string.Join(" | ", reports.Select(r => r.ToString())));
            }
            File.WriteAllText($"{OutDir}/report.md", sb.ToString());
            Debug.Log($"[Nov11Walk] Sim: report {OutDir}/report.md");
        }

        public static void Dump()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var map in AllMaps())
            {
                var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var props = (map.props ?? "").Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                sb.AppendLine($"# {map.name} {map.Columns} x {map.Rows}, start c{map.startTile.x} r{map.startTile.y}");
                sb.AppendLine("legend:");
                foreach (var e in map.propLegend)
                    if (e.recipe != null)
                        sb.AppendLine($"  {e.Key} {e.recipe.name} blocks={e.recipe.blocksWalking} footprint={WorldMap.FootprintOf(e).x}x{WorldMap.FootprintOf(e).y}{(e.turned ? " turned" : "")}");
                sb.AppendLine("gates: " + string.Join("; ", map.gates.Select(g => $"{g.id} c{g.tile.x} r{g.tile.y} -> {(g.to != null ? g.to.name : "sealed")} arrive c{g.arrive.x} r{g.arrive.y}")));
                sb.AppendLine("landmarks: " + string.Join("; ", map.landmarks.Select(l => $"{l.name} c{l.tile.x} r{l.tile.y}")));
                sb.AppendLine("\nlayout (row: tiles):");
                for (int r = 0; r < rows.Length; r++) sb.AppendLine($"{r,3}: {rows[r]}");
                sb.AppendLine("\nprops (row: tiles):");
                for (int r = 0; r < props.Length; r++) sb.AppendLine($"{r,3}: {props[r]}");
                sb.AppendLine("\nlayout with props ('.' = walkable, '=' road, '#' blocked terrain, prop keys where they block, lower-case where they don't):");
                for (int r = 0; r < rows.Length; r++)
                {
                    var line = new StringBuilder($"{r,3}: ");
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        char t = rows[r][c], p = map.PropAt(r, c);
                        bool has = map.TryGetProp(p, out var e);
                        if (has && e.recipe.blocksWalking) line.Append(p);
                        else if (has) line.Append(char.ToLowerInvariant(p));
                        else line.Append(t == IslandMap.Path ? '=' : IslandMap.IsWalkable(t) ? '.' : '#');
                    }
                    sb.AppendLine(line.ToString());
                }
                File.WriteAllText($"{OutDir}/{map.name}_dump.txt", sb.ToString());
            }
            Debug.Log($"[Nov11Walk] Dump: {OutDir}/<map>_dump.txt");
        }

        // ---- A4: small blocking props on or beside roads ----

        /// <summary>Hand moments and landmarks that must stay where they are, whatever tile they sit on.</summary>
        static readonly HashSet<string> Keep = new HashSet<string>
        {
            "SmokeCloud", "ThornVine", "FallenLog", "CloudPuff", "Drawbridge", "Firebox", "KitePost", "StoneLantern", "Signpost", "HearthShrine",
            "ReturnGate", "Windmill", "SmallMill", "Cottage", "ClayHut", "StiltHut", "WindTower", "LakeTemple", "ForgeDoor", "Bellows", "Brazier",
            "StoneBasin", "CliffBowl", "CliffWall", "Kiln", "RuinedKiln", "HubLantern", "LanternPost", "Statue", "FireGate", "WaterGate", "WindGate",
            "Fence", "FenceCorner", "ChannelGap", "BedGap",
        };

        /// <summary>Small clutter that may move a tile away from a road, or go.</summary>
        static readonly HashSet<string> Small = new HashSet<string> { "HayBale", "MossBoulder", "ReedClump", "BerryBush", "Woodpile" };

        static readonly Vector2Int[] Four = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        /// <summary>Bridge: arg "audit" lists, "apply" edits the map assets. Report Logs/WalkSim/stones.md.</summary>
        public static void Stones()
        {
            bool apply = Arg("audit") == "apply";
            Directory.CreateDirectory(OutDir);
            var md = new StringBuilder($"# Small props on or beside roads ({(apply ? "applied" : "audit")})\n\n" +
                                       "On a road tile: removed (hand moments and landmarks stay). Beside a road, small clutter (hay, boulders, reeds, bushes, woodpiles): " +
                                       "moved one tile further from the road when that tile is free grass, else removed. Everything else beside a road is listed and left.\n");
            foreach (var map in AllMaps())
            {
                if (map.Rows < 20) continue; // the halls have no clutter
                var props = (map.props ?? "").Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
                var landmarks = new HashSet<Vector2Int>(map.landmarks.Select(l => l.tile));
                bool Road(Vector2Int t) => map.At(t.y, t.x) == IslandMap.Path;
                bool BesideRoad(Vector2Int t) => Four.Any(d => Road(t + d));
                char PropAt(Vector2Int t) => t.y >= 0 && t.y < props.Length && t.x >= 0 && t.x < props[t.y].Length ? props[t.y][t.x] : '.';
                var lines = new List<string>();
                var freed = new List<Vector2Int>();
                int changed = 0;
                for (int r = 0; r < props.Length; r++)
                    for (int c = 0; c < props[r].Length; c++)
                    {
                        var t = new Vector2Int(c, r);
                        if (!map.TryGetProp(props[r][c], out var e) || !e.recipe.blocksWalking) continue;
                        var f = WorldMap.FootprintOf(e);
                        if (f != Vector2Int.one) continue;
                        string name = e.recipe.name;
                        if (Road(t))
                        {
                            if (Keep.Contains(name))
                            {
                                lines.Add($"- c{c} r{r} **{name}** on the road: kept (a hand moment or landmark)");
                                continue;
                            }
                            lines.Add($"- c{c} r{r} **{name}** on the road: removed");
                            props[r][c] = '.';
                            changed++;
                            continue;
                        }
                        if (!BesideRoad(t)) continue;
                        if (!Small.Contains(name))
                        {
                            lines.Add($"- c{c} r{r} {name} beside the road: left");
                            continue;
                        }
                        // Away from the nearest road tile, one more tile out.
                        var away = Four.Where(d => Road(t + d)).Select(d => -d).FirstOrDefault();
                        var to = t + away;
                        bool free = away != Vector2Int.zero && map.At(to.y, to.x) == IslandMap.Grass && PropAt(to) == '.' && !Road(to) && !BesideRoad(to) &&
                                    !landmarks.Contains(to) && !map.gates.Any(g => g.Covers(to) || g.Front(0) == to || g.Front(1) == to);
                        if (free)
                        {
                            lines.Add($"- c{c} r{r} {name} beside the road: moved to c{to.x} r{to.y}");
                            props[to.y][to.x] = props[r][c];
                        }
                        else lines.Add($"- c{c} r{r} {name} beside the road: removed (no free tile further out)");
                        props[r][c] = '.';
                        freed.Add(t);
                        changed++;
                    }
                // A wall or fence tile sitting in a road's lane (the road runs on both sides of it): the lane goes through.
                var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
                bool layoutChanged = false;
                for (int r = 0; r < props.Length; r++)
                    for (int c = 0; c < props[r].Length; c++)
                    {
                        var t = new Vector2Int(c, r);
                        if (Road(t) || !map.TryGetProp(props[r][c], out var e) || !e.recipe.blocksWalking) continue;
                        if (e.recipe.name != "StoneWall" && e.recipe.name != "Fence") continue;
                        bool gap = Road(t + Vector2Int.left) && Road(t + Vector2Int.right) || Road(t + Vector2Int.up) && Road(t + Vector2Int.down);
                        if (!gap) continue;
                        lines.Add($"- c{c} r{r} **{e.recipe.name}** across a road lane: removed, the lane goes through");
                        props[r][c] = '.';
                        rows[r][c] = IslandMap.Path;
                        layoutChanged = true;
                        changed++;
                    }
                // A tile freed beside the road that the road runs round on both sides (a hay bale in a lane): road now.
                foreach (var t in freed)
                {
                    if (Road(t) || !IslandMap.IsWalkable(map.At(t.y, t.x))) continue;
                    bool gap = Road(t + Vector2Int.left) && Road(t + Vector2Int.right) || Road(t + Vector2Int.up) && Road(t + Vector2Int.down);
                    if (!gap) continue;
                    lines.Add($"- c{t.x} r{t.y}: the freed tile lay in a lane: road now");
                    rows[t.y][t.x] = IslandMap.Path;
                    layoutChanged = true;
                }
                md.AppendLine($"\n## {map.name}: {changed} change(s)\n");
                foreach (var l in lines) md.AppendLine(l);
                if (apply && changed > 0)
                {
                    map.SetProps(string.Join("\n", props.Select(p => new string(p))));
                    if (layoutChanged) map.SetLayout(string.Join("\n", rows.Select(r => new string(r))));
                    EditorUtility.SetDirty(map);
                }
            }
            if (apply) AssetDatabase.SaveAssets();
            File.WriteAllText($"{OutDir}/stones.md", md.ToString());
            Debug.Log($"[Nov11Walk] Stones ({(apply ? "applied" : "audit")}): {OutDir}/stones.md");
        }

        // ---- A5: one-tile grass holes inside roads (they make staircase jogs and stalls) ----

        /// <summary>Bridge: arg "audit" lists, "apply" fills. A grass tile with no prop and road on both opposite sides becomes road.</summary>
        public static void Holes()
        {
            bool apply = Arg("audit") == "apply";
            Directory.CreateDirectory(OutDir);
            var md = new StringBuilder($"# Grass holes inside roads ({(apply ? "filled" : "audit")})\n");
            foreach (var map in AllMaps())
            {
                if (map.Rows < 20) continue;
                var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
                bool Road(Vector2Int t) => t.y >= 0 && t.y < rows.Length && t.x >= 0 && t.x < rows[t.y].Length && rows[t.y][t.x] == IslandMap.Path;
                var filled = new List<Vector2Int>();
                for (int pass = 0; pass < 3; pass++)
                {
                    var now = new List<Vector2Int>();
                    for (int r = 0; r < rows.Length; r++)
                        for (int c = 0; c < rows[r].Length; c++)
                        {
                            var t = new Vector2Int(c, r);
                            if (rows[r][c] != IslandMap.Grass || map.PropAt(r, c) != IslandMap.Empty) continue;
                            if (Road(t + Vector2Int.left) && Road(t + Vector2Int.right) || Road(t + Vector2Int.up) && Road(t + Vector2Int.down)) now.Add(t);
                        }
                    foreach (var t in now) rows[t.y][t.x] = IslandMap.Path;
                    filled.AddRange(now);
                    if (now.Count == 0) break;
                }
                md.AppendLine($"\n## {map.name}: {filled.Count} hole(s)\n");
                foreach (var t in filled) md.AppendLine($"- c{t.x} r{t.y}");
                if (apply && filled.Count > 0)
                {
                    map.SetLayout(string.Join("\n", rows.Select(r => new string(r))));
                    EditorUtility.SetDirty(map);
                }
            }
            if (apply) AssetDatabase.SaveAssets();
            File.WriteAllText($"{OutDir}/holes.md", md.ToString());
            Debug.Log($"[Nov11Walk] Holes ({(apply ? "filled" : "audit")}): {OutDir}/holes.md");
        }

        // ---- E12: Galewick's lodge (the hearth shrine) moves to the roadside by the mill ----

        public static readonly Vector2Int OldShrine = new Vector2Int(14, 40), NewShrine = new Vector2Int(34, 33);

        /// <summary>
        /// Bridge: the Galewick shrine goes from its inner pocket (reached by a road stub) to the roadside north-east of the big
        /// mill, south of the ditch; the stub becomes grass; the hay bale on its new front steps goes; the landmark follows.
        /// </summary>
        public static void Lodge()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_Galewick.asset");
            var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            var props = map.props.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            if (props[OldShrine.y][OldShrine.x] != 'Y')
            {
                Debug.LogWarning($"[Nov11Walk] Lodge: no shrine at c{OldShrine.x} r{OldShrine.y}; already moved?");
                return;
            }
            props[OldShrine.y][OldShrine.x] = '.';
            props[NewShrine.y][NewShrine.x] = 'Y';
            // Its body and front steps on plain grass (tall grass under the steps would start encounters at the door).
            for (int dy = 0; dy < 3; dy++)
                for (int dx = 0; dx < 2; dx++)
                {
                    var t = NewShrine + new Vector2Int(dx, dy);
                    rows[t.y][t.x] = IslandMap.Grass;
                    if (props[t.y][t.x] != 'Y') props[t.y][t.x] = '.';
                }
            // The road stub that only reached the old pocket.
            foreach (var t in new[] { new Vector2Int(14, 43), new Vector2Int(15, 43), new Vector2Int(14, 44), new Vector2Int(15, 44), new Vector2Int(16, 44) })
                if (rows[t.y][t.x] == IslandMap.Path) rows[t.y][t.x] = IslandMap.Grass;
            map.SetLayout(string.Join("\n", rows.Select(r => new string(r))));
            map.SetProps(string.Join("\n", props.Select(r => new string(r))));
            for (int i = 0; i < map.landmarks.Count; i++)
                if (map.landmarks[i].name == "shrine")
                {
                    var l = map.landmarks[i];
                    l.tile = NewShrine;
                    map.landmarks[i] = l;
                }
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nov11Walk] Lodge: the Galewick shrine moved c{OldShrine.x} r{OldShrine.y} -> c{NewShrine.x} r{NewShrine.y} (roadside by the mill); the stub road is grass.");
        }

        // ---- A5: the smoky hollow's staircase becomes a straight 2-wide band (cols 29-30, rows 22-30) ----

        static readonly Vector2Int[] HollowSmoke =
        {
            new Vector2Int(29, 26), new Vector2Int(30, 26), new Vector2Int(29, 25), new Vector2Int(30, 25),
            new Vector2Int(29, 24), new Vector2Int(30, 24), new Vector2Int(29, 23), new Vector2Int(30, 23),
        };

        static readonly Vector2Int[] HollowLanterns =
        {
            new Vector2Int(31, 26), new Vector2Int(28, 25), new Vector2Int(31, 25), new Vector2Int(28, 24), new Vector2Int(31, 24), new Vector2Int(28, 23),
        };

        public static readonly Vector2Int HollowMark = new Vector2Int(29, 27);

        /// <summary>Bridge (Play stopped): straightens the hollow in the Cinderwood map and moves the scene's lanterns and smoke with it; saves both.</summary>
        public static void Hollow()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Nov11Walk] Hollow: stop Play first.");
                return;
            }
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap_Cinderwood.asset");
            var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            var props = map.props.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            var piece = UnityEngine.Object.FindFirstObjectByType<LanternHollowPiece>(FindObjectsInactive.Include);
            if (piece == null || piece.smoke.Length != 8 || piece.lanterns.Length != 6)
            {
                Debug.LogError("[Nov11Walk] Hollow: no LanternHollowPiece with 8 smoke and 6 lanterns in the open scene.");
                return;
            }
            if (piece.smoke[0] == HollowSmoke[0])
            {
                Debug.LogWarning("[Nov11Walk] Hollow: already straight.");
                return;
            }
            // Old props off.
            foreach (var t in piece.smoke) props[t.y][t.x] = '.';
            foreach (var t in piece.lanterns) props[t.y][t.x] = '.';
            // The band: road from row 22 to row 30 on cols 29-30.
            for (int r = 22; r <= 30; r++)
                for (int c = 29; c <= 30; c++) rows[r][c] = IslandMap.Path;
            // The old staircase tiles beside it: rock, so the smoke stays the only way through.
            foreach (var t in new[] { new Vector2Int(31, 23), new Vector2Int(28, 26), new Vector2Int(28, 27), new Vector2Int(27, 28), new Vector2Int(28, 28) })
                rows[t.y][t.x] = IslandMap.Rock;
            foreach (var t in HollowSmoke) props[t.y][t.x] = 'q';
            foreach (var t in HollowLanterns)
            {
                rows[t.y][t.x] = IslandMap.Grass;
                props[t.y][t.x] = 'n';
            }
            map.SetLayout(string.Join("\n", rows.Select(r => new string(r))));
            map.SetProps(string.Join("\n", props.Select(r => new string(r))));
            for (int i = 0; i < map.landmarks.Count; i++)
                if (map.landmarks[i].name == "hollow")
                {
                    var l = map.landmarks[i];
                    l.tile = HollowMark;
                    map.landmarks[i] = l;
                }
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Undo.RecordObject(piece, "Nov 11 hollow");
            piece.smoke = (Vector2Int[])HollowSmoke.Clone();
            piece.lanterns = (Vector2Int[])HollowLanterns.Clone();
            EditorUtility.SetDirty(piece);
            var scene = piece.gameObject.scene;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Nov11Walk] Hollow: straight band cols 29-30 rows 22-30; 8 smoke and 6 lanterns moved; landmark hollow c{HollowMark.x} r{HollowMark.y}; scene saved.");
        }

        /// <summary>Bridge: arg "Cinderwood:20,14,P;21,14,P" sets layout tiles (column, row, char) on a realm map and saves it.</summary>
        public static void SetTiles()
        {
            string arg = Arg("");
            int colon = arg.IndexOf(':');
            if (colon < 0) return;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{arg.Substring(0, colon)}.asset");
            var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            var done = new List<string>();
            foreach (var item in arg.Substring(colon + 1).Split(';'))
            {
                var p = item.Split(',');
                if (p.Length != 3) continue;
                int c = int.Parse(p[0]), r = int.Parse(p[1]);
                done.Add($"c{c} r{r} {rows[r][c]} -> {p[2][0]}");
                rows[r][c] = p[2][0];
            }
            map.SetLayout(string.Join("\n", rows.Select(r => new string(r))));
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nov11Walk] SetTiles {map.name}: {string.Join("; ", done)}");
        }

        /// <summary>Bridge: arg "Galewick:38,25,.;38,24,b" sets props grid tiles (column, row, char) on a realm map and saves it.</summary>
        public static void SetProps()
        {
            string arg = Arg("");
            int colon = arg.IndexOf(':');
            if (colon < 0) return;
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{arg.Substring(0, colon)}.asset");
            var rows = map.props.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Select(r => r.ToCharArray()).ToArray();
            var done = new List<string>();
            foreach (var item in arg.Substring(colon + 1).Split(';'))
            {
                var p = item.Split(',');
                if (p.Length != 3) continue;
                int c = int.Parse(p[0]), r = int.Parse(p[1]);
                done.Add($"c{c} r{r} {rows[r][c]} -> {p[2][0]}");
                rows[r][c] = p[2][0];
            }
            map.SetProps(string.Join("\n", rows.Select(r => new string(r))));
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nov11Walk] SetProps {map.name}: {string.Join("; ", done)}");
        }

        // ---- A5: zig-zag roads ----

        /// <summary>Bridge: every map's one-tile jogs (staircase steps) as tile pairs, to Logs/WalkSim/jogs.md.</summary>
        public static void Jogs()
        {
            Directory.CreateDirectory(OutDir);
            var md = new StringBuilder("# Road jogs (staircase steps: two road corners within 2 tiles along, 1-2 across)\n");
            foreach (var map in AllMaps())
            {
                var pairs = PathAudit.JogPairs(map);
                md.AppendLine($"\n## {map.name}: {pairs.Count}\n");
                foreach (var (a, b) in pairs) md.AppendLine($"- corner c{a.x} r{a.y} and corner c{b.x} r{b.y}");
            }
            File.WriteAllText($"{OutDir}/jogs.md", md.ToString());
            Debug.Log($"[Nov11Walk] Jogs: {OutDir}/jogs.md");
        }

        /// <summary>Bridge: top-down pictures of every realm, tagged by arg.txt (before / after).</summary>
        public static void Pictures()
        {
            Directory.CreateDirectory(OutDir);
            string tag = Arg("now");
            foreach (var map in AllMaps())
            {
                var rows = map.layout.Replace("\r", "").Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                Step29Paths.Picture(rows, null, $"{OutDir}/{map.name}_{tag}.png");
            }
            Debug.Log($"[Nov11Walk] Pictures: {OutDir}/<map>_{tag}.png");
        }
    }
}
