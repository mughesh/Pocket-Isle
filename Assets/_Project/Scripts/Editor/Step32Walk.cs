using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 32 B: Lilymere's extra stops. Bridge <see cref="Lily"/> (arg = tag): the main route and 100 random landmark
    /// pairs through <see cref="SwipeSim"/>, every unexplained stop; then every road tile x 4 swipes, listing the short
    /// stops (a walk that ends after 1-2 tiles and, swiped the same way again, ends after 1-2 tiles again: "like a
    /// junction") with the rule that ended them. Writes Logs/WalkSim/lily_&lt;tag&gt;.md.
    /// </summary>
    public static class Step32Walk
    {
        const string MapPath = "Assets/_Project/Data/WorldMap_Lilymere.asset";
        static readonly Vector2Int[] Ways = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public struct Short
        {
            public Vector2Int from, way, end1, end2;
            public int n1, n2;
            public string why1, why2;
        }

        /// <summary>Every road tile x 4: a swipe that ends after 1-2 tiles twice in a row (walls and the view excepted).</summary>
        public static List<Short> ShortStops(WorldMap map, System.Func<Vector2Int, Vector2Int, WalkStop> stops)
        {
            var list = new List<Short>();
            var walked = new List<Vector2Int>();
            for (int r = 0; r < map.Rows; r++)
                for (int c = 0; c < map.Columns; c++)
                {
                    var t = new Vector2Int(c, r);
                    if (!map.CanWalk(t) || map.At(r, c) != IslandMap.Path) continue;
                    foreach (var w in Ways)
                    {
                        string e1 = SwipeSim.Swipe(map, t, w, stops, walked);
                        int n1 = walked.Count;
                        if (n1 == 0 || n1 > 2 || e1 == SmartStep.Blocked || e1 == SmartStep.View) continue;
                        var at = walked[n1 - 1];
                        var h = n1 > 1 ? walked[n1 - 1] - walked[n1 - 2] : walked[0] - t;
                        string e2 = SwipeSim.Swipe(map, at, h, stops, walked);
                        int n2 = walked.Count;
                        if (n2 == 0 || n2 > 2 || e2 == SmartStep.Blocked || e2 == SmartStep.View) continue;
                        list.Add(new Short { from = t, way = h, end1 = at, end2 = walked[n2 - 1], n1 = n1, n2 = n2, why1 = e1, why2 = e2 });
                    }
                }
            return list;
        }

        public static void Lily()
        {
            string tag = File.Exists("Temp/ClaudeBridge/arg.txt") ? File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim() : "now";
            if (string.IsNullOrEmpty(tag)) tag = "now";
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>(MapPath);
            Nov12Walk.OpenSolved(map);
            var stops = Nov11Walk.Stops(map, false);
            var sb = new StringBuilder($"# Lilymere stops ({tag})\n\n");

            // The main route and 100 random landmark pairs.
            var legs = new List<SwipeSim.Leg>();
            var res = new Nov12Walk.RealmResult { map = map.name };
            var names = Nov12Walk.MainRoutes["WorldMap_Lilymere"];
            for (int i = 0; i + 1 < names.Length; i++) AddLeg(map, names[i], names[i + 1], stops, res, legs);
            var marks = map.landmarks.Select(l => l.name).Where(n => n != "start").Distinct().ToList();
            var rng = new System.Random(3232);
            for (int k = 0; k < 100 && marks.Count >= 2;)
            {
                string a = marks[rng.Next(marks.Count)], b = marks[rng.Next(marks.Count)];
                if (a == b) continue;
                AddLeg(map, a, b, stops, res, legs);
                k++;
            }
            int unexplained = legs.Sum(l => l.Unexplained), notReached = legs.Count(l => !l.reached);
            sb.AppendLine($"Legs: {legs.Count} (main route {names.Length - 1} + 100 pairs); unexplained stops {unexplained}; not reached {notReached}.\n");
            sb.AppendLine("| leg | swipes | stop | tile | end | kind |\n|---|---|---|---|---|---|");
            foreach (var l in legs)
                foreach (var s in l.stops.Where(s => !s.Explained))
                    sb.AppendLine($"| {l.from} -> {l.to} | {l.swipes} | unexplained | c{s.tile.x} r{s.tile.y} | {s.end} | {s.why} |");

            var shorts = ShortStops(map, stops);
            sb.AppendLine($"\n## Short stops twice in a row: {shorts.Count}\n");
            sb.AppendLine("| from | way | 1st stop | why | 2nd stop | why |\n|---|---|---|---|---|---|");
            foreach (var s in shorts)
                sb.AppendLine($"| c{s.from.x} r{s.from.y} | {s.way} | c{s.end1.x} r{s.end1.y} ({s.n1}) | {s.why1} | c{s.end2.x} r{s.end2.y} ({s.n2}) | {s.why2} |");
            map.ClearRuntimeChanges();
            Directory.CreateDirectory(Nov11Walk.OutDir);
            File.WriteAllText($"{Nov11Walk.OutDir}/lily_{tag}.md", sb.ToString());
            Debug.Log($"[Step32Walk] Lilymere ({tag}): {legs.Count} legs, unexplained {unexplained}, not reached {notReached}, short stops twice {shorts.Count}; {Nov11Walk.OutDir}/lily_{tag}.md");
        }

        static void AddLeg(WorldMap map, string a, string b, System.Func<Vector2Int, Vector2Int, WalkStop> stops, Nov12Walk.RealmResult res, List<SwipeSim.Leg> legs)
        {
            if (!Nov12Walk.Place(map, a, out var pa, out var za) || !Nov12Walk.Place(map, b, out _, out var zb)) return;
            legs.Add(SwipeSim.Route(map, Nov12Walk.StartTile(map, pa, za), zb, stops, a, b));
        }
    }
}
