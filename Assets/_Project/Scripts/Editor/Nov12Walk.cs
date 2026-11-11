using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 12 overnight, section A: the swipe count. Bridge: <see cref="Routes"/> (arg = tag, e.g. "before" / "after")
    /// writes Logs/WalkSim/swipes_&lt;tag&gt;.md with every realm's main route and 20 random landmark pairs: swipes per leg,
    /// taps, the target (explained stops + 1) and every unexplained stop.
    /// </summary>
    public static class Nov12Walk
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";

        static string Arg(string fallback)
        {
            string a = File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
            return string.IsNullOrEmpty(a) ? fallback : a;
        }

        /// <summary>Each realm's main route in story order ("gate" = the arrival gate; "gate:id" = that gate).</summary>
        public static readonly Dictionary<string, string[]> MainRoutes = new Dictionary<string, string[]>
        {
            ["WorldMap_StillIsle"] = new[] { "start", "firstGrass", "statue", "pond", "gate:fire", "gate:water", "gate:wind" },
            ["WorldMap_Galewick"] = new[] { "start", "firstGrass", "plaza", "shrine", "mill", "kites", "ridge", "healBloom", "tower", "gate" },
            ["WorldMap_Cinderwood"] = new[] { "start", "vine", "firstGrass", "plaza", "shrine", "hollow", "springs", "log", "healBloom", "temple", "gate" },
            ["WorldMap_Lilymere"] = new[] { "start", "stream", "firstGrass", "village", "shrine", "channel", "garden", "basin", "healBloom", "temple", "gate" },
        };

        /// <summary>Blockers a hand moment clears: open for the routes (the route walks the realm as it is once solved).</summary>
        static readonly HashSet<string> Solved = new HashSet<string>
        {
            "CloudPuff", "Drawbridge", "FallenLog", "SmokeCloud", "ThornVine", "ChannelGap", "BedGap",
        };

        public static void OpenSolved(WorldMap map)
        {
            map.ClearRuntimeChanges();
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                    if (map.TryGetProp(map.PropAt(r, c), out var e) && e.recipe != null && Solved.Contains(e.recipe.name))
                        map.SetPropOpen(new Vector2Int(c, r), true);
            // A stream a lily pad bridge crosses (mud between two road ends, 1-2 tiles): bridged.
            bool Road(int c, int r) => r >= 0 && c >= 0 && r < map.Rows && c < map.Columns && map.Raw(r, c) == IslandMap.Path;
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    if (map.Raw(r, c) != IslandMap.Mud) continue;
                    bool v = (Road(c, r - 1) || Road(c, r - 2)) && (Road(c, r + 1) || Road(c, r + 2));
                    bool h = (Road(c - 1, r) || Road(c - 2, r)) && (Road(c + 1, r) || Road(c + 2, r));
                    if (v || h) map.SetBridged(new Vector2Int(c, r), true);
                }
        }

        public static bool Place(WorldMap map, string name, out Vector2Int tile, out HashSet<Vector2Int> zone)
        {
            zone = null;
            tile = default;
            if (name.StartsWith("gate"))
            {
                string id = name.Length > 5 ? name.Substring(5) : null;
                var gates = map.gates.Where(g => id == null || g.id == id).ToList();
                if (gates.Count == 0) return false;
                var g0 = gates[0];
                tile = g0.tile;
                zone = new HashSet<Vector2Int>(new[] { g0.Front(0), g0.Front(1) }.Where(map.CanWalk));
                return zone.Count > 0;
            }
            if (!map.TryGetLandmark(name, out tile)) return false;
            zone = SwipeSim.Zone(map, tile);
            return zone.Count > 0;
        }

        /// <summary>The stop tile a leg starts from: the place itself when walkable, else the zone tile nearest to it.</summary>
        static Vector2Int StartTile(WorldMap map, Vector2Int place, HashSet<Vector2Int> zone)
        {
            if (map.CanWalk(place)) return place;
            return zone.OrderBy(t => (t - place).sqrMagnitude).ThenBy(t => t.y).ThenBy(t => t.x).First();
        }

        public sealed class RealmResult
        {
            public string map;
            public readonly List<SwipeSim.Leg> main = new List<SwipeSim.Leg>();
            public readonly List<SwipeSim.Leg> pairs = new List<SwipeSim.Leg>();
            public readonly List<string> missing = new List<string>();
        }

        public static List<RealmResult> RunAll(string folder = "Assets/_Project/Data")
        {
            var list = new List<RealmResult>();
            foreach (var kv in MainRoutes)
            {
                var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"{folder}/{kv.Key}.asset");
                if (map == null) continue;
                OpenSolved(map);
                var stops = Nov11Walk.Stops(map, false);
                var res = new RealmResult { map = map.name };
                var names = kv.Value;
                for (int i = 0; i + 1 < names.Length; i++)
                {
                    var leg = Leg(map, names[i], names[i + 1], stops, res);
                    if (leg != null) res.main.Add(leg);
                }
                // 20 random pairs of landmarks (fixed seed).
                var marks = map.landmarks.Select(l => l.name).Where(n => n != "start").Distinct().ToList();
                var rng = new System.Random(1112);
                for (int k = 0; k < 20 && marks.Count >= 2; k++)
                {
                    string a = marks[rng.Next(marks.Count)], b = marks[rng.Next(marks.Count)];
                    if (a == b)
                    {
                        k--;
                        continue;
                    }
                    var leg = Leg(map, a, b, stops, res);
                    if (leg != null) res.pairs.Add(leg);
                }
                map.ClearRuntimeChanges();
                list.Add(res);
            }
            return list;
        }

        static SwipeSim.Leg Leg(WorldMap map, string a, string b, System.Func<Vector2Int, Vector2Int, WalkStop> stops, RealmResult res)
        {
            if (!Place(map, a, out var pa, out var za))
            {
                if (!res.missing.Contains(a)) res.missing.Add(a);
                return null;
            }
            if (!Place(map, b, out _, out var zb))
            {
                if (!res.missing.Contains(b)) res.missing.Add(b);
                return null;
            }
            return SwipeSim.Route(map, StartTile(map, pa, za), zb, stops, a, b);
        }

        public static string Table(List<RealmResult> results, out int unexplained, out int failed)
        {
            unexplained = 0;
            failed = 0;
            var sb = new StringBuilder();
            sb.AppendLine("| Realm | Main route swipes | Target | Taps | Unexplained stops | Not reached | Pairs: swipes / target | Pairs unexplained |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var r in results)
            {
                int ms = r.main.Sum(l => l.swipes), mt = r.main.Sum(l => l.Target), mu = r.main.Sum(l => l.Unexplained), mn = r.main.Count(l => !l.reached);
                int ps = r.pairs.Sum(l => l.swipes), pt = r.pairs.Sum(l => l.Target), pu = r.pairs.Sum(l => l.Unexplained);
                unexplained += mu + pu;
                failed += mn + r.pairs.Count(l => !l.reached);
                sb.AppendLine($"| {Short(r.map)} | {ms} | {mt} | {r.main.Sum(l => l.taps)} | {mu} | {mn} | {ps} / {pt} | {pu} |");
            }
            return sb.ToString();
        }

        static string Short(string map) => map.Replace("WorldMap_", "");

        public static void Routes()
        {
            string tag = Arg("now");
            string folder = "Assets/_Project/Data";
            if (tag.Contains("|"))
            {
                folder = tag.Substring(tag.IndexOf('|') + 1);
                tag = tag.Substring(0, tag.IndexOf('|'));
            }
            Directory.CreateDirectory(Nov11Walk.OutDir);
            SmartStep.FollowBends = tag.Contains("bends");
            List<RealmResult> results;
            try { results = RunAll(folder); }
            finally { SmartStep.FollowBends = false; }
            var sb = new StringBuilder($"# Swipe count ({tag})\n\nThe fewest swipes a player needs per leg (a swipe at every stop, one tap when the walk passes the place). " +
                                       "Target = explained stops + 1. Explained: a real turn (the road can't go straight, runs on 3+ tiles to a side), a real side road " +
                                       "(2-4 rows wide, reaches 3+ tiles past the corridor), one tile before a door or a stop. Blockers a hand moment clears are open.\n\n");
            sb.AppendLine(Table(results, out int unexplained, out int failed));
            sb.AppendLine($"Unexplained stops: **{unexplained}**, legs not reached: **{failed}**.\n");
            foreach (var r in results)
            {
                sb.AppendLine($"## {Short(r.map)}\n");
                if (r.missing.Count > 0) sb.AppendLine($"Missing places: {string.Join(", ", r.missing)}\n");
                sb.AppendLine("| Leg | Swipes | Target | Taps | Tiles | Stops |\n|---|---|---|---|---|---|");
                foreach (var l in r.main.Concat(r.pairs))
                {
                    string stops = string.Join("; ", l.stops.Select(s => $"c{s.tile.x} r{s.tile.y} {(s.Explained ? s.kind : "**" + s.why + "**")}"));
                    string mark = l.reached ? (l.Unexplained > 0 ? " ⚠" : "") : " **not reached**";
                    if (r.pairs.Contains(l) && l == r.pairs[0]) sb.AppendLine("| *random pairs* | | | | | |");
                    sb.AppendLine($"| {l.from} → {l.to}{mark} | {l.swipes} | {l.Target} | {l.taps} | {l.tiles} | {stops} |");
                }
                sb.AppendLine();
            }
            // The budget lines the test reads (realm|from|to|swipes).
            var budget = new StringBuilder();
            foreach (var r in results)
                foreach (var l in r.main.Concat(r.pairs))
                    budget.AppendLine($"{r.map}|{l.from}|{l.to}|{l.swipes}");
            File.WriteAllText($"{Nov11Walk.OutDir}/swipes_{tag}.md", sb.ToString());
            File.WriteAllText($"{Nov11Walk.OutDir}/swipes_{tag}_budget.txt", budget.ToString());
            Debug.Log($"[Nov12Walk] Routes ({tag}): unexplained {unexplained}, not reached {failed}; {Nov11Walk.OutDir}/swipes_{tag}.md\n" + Table(results, out _, out _));
        }
    
        /// <summary>Bridge: arg "Galewick:24,40" prints the four swipes from that tile (solved blockers open).</summary>
        public static void Probe()
        {
            string arg = Arg("");
            int colon = arg.IndexOf(':');
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>($"Assets/_Project/Data/WorldMap_{arg.Substring(0, colon)}.asset");
            var p = arg.Substring(colon + 1).Split(',');
            var from = new Vector2Int(int.Parse(p[0]), int.Parse(p[1]));
            OpenSolved(map);
            var stops = Nov11Walk.Stops(map, false);
            var walked = new List<Vector2Int>();
            var sb = new StringBuilder($"[Nov12Walk] Probe {map.name} c{from.x} r{from.y}:");
            foreach (var (w, n) in new[] { (new Vector2Int(0, -1), "north"), (new Vector2Int(0, 1), "south"), (new Vector2Int(-1, 0), "west"), (new Vector2Int(1, 0), "east") })
            {
                string end = SwipeSim.Swipe(map, from, w, stops, walked);
                sb.Append($"\n  {n}: {walked.Count} tiles, ends {end}" + (walked.Count > 0 ? $" at c{walked[walked.Count - 1].x} r{walked[walked.Count - 1].y}: " : ": ") +
                          string.Join(" ", walked.Select(t => $"{t.x},{t.y}")));
            }
            map.ClearRuntimeChanges();
            File.WriteAllText($"{Nov11Walk.OutDir}/probe.txt", sb.ToString());
        }
    }
}
