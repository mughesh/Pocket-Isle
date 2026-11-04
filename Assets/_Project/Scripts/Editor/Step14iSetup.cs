using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14i F: straight paths. Rewrites a realm map from its generator (straight legs, right-angle turns, legs of 3+
    /// tiles), runs its layout check and the path audit (exec:Diorama.EditorTools.Step14iSetup.X).
    /// </summary>
    public static class Step14iSetup
    {
        public static void Cinderwood() => Realm("Cinderwood", l => CinderwoodTools.Write(l), (m, l) => CinderwoodTools.Check(m, l));
        public static void Galewick() => Realm("Galewick", l => GalewickTools.Write(l), (m, l) => GalewickTools.Check(m, l));
        public static void Lilymere() => Realm("Lilymere", l => LilymereTools.Write(l), (m, l) => LilymereTools.Check(m, l));

        static void Realm(string name, System.Func<StringBuilder, WorldMap> write, System.Func<WorldMap, StringBuilder, System.Collections.Generic.List<string>> check)
        {
            var log = new StringBuilder($"[Step14i] {name}:");
            var map = write(log);
            if (map == null) return;
            var problems = check(map, log);
            foreach (var p in problems) Debug.LogError($"[Step14i] {name} check: {p}");
            var m = PathAudit.Measure(map);
            log.Append($"\n  paths: {m.pathTiles} path tiles, {m.turns} turns, {m.jogs} jogs, {m.shortStops}/{m.swipes} short smart steps; check {(problems.Count == 0 ? "OK" : $"{problems.Count} problem(s)")}");
            PathAudit.Draw(map, $"Logs/BattleShots/14i_paths_{name}_after.png");
            Debug.Log(log.ToString());
        }
    }
}
