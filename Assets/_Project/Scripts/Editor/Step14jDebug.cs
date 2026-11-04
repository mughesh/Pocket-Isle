using System.IO;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 14j (Play mode): the tablet layout check, the grab zone report.</summary>
    public static class Step14jDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        /// <summary>Every board checked since Play started (each logs when its layout changes), the battle runes and the lodge tablet.</summary>
        public static void Layouts()
        {
            var sb = new StringBuilder("[Step14jDebug] Tablet layouts (rule: ")
                .Append(TabletLayout.MinGap * 1000f).Append(" mm apart, ").Append(TabletLayout.HoverGap * 1000f).Append(" mm when one grows):");
            int bad = 0;
            foreach (var kv in RuneBoard.LayoutLog)
            {
                sb.Append($"\n  {kv.Key}: {(kv.Value.Count == 0 ? "ok" : string.Join("; ", kv.Value))}");
                bad += kv.Value.Count;
            }
            var battle = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
            if (battle != null)
            {
                var p = battle.LayoutProblems();
                sb.Append($"\n  battle runes: {(p.Count == 0 ? "ok" : string.Join("; ", p))}");
                bad += p.Count;
            }
            var book = Object.FindFirstObjectByType<LodgeJournalBook>(FindObjectsInactive.Include);
            var rack = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            if (book != null && rack != null && rack.SocketBandLocal(out float minX, out float maxX, out _))
            {
                var span = book.RimSpan();
                float gap = Mathf.Max(span.x - maxX, minX - span.y);
                sb.Append($"\n  lodge Journal tablet x {span.x:0.000}..{span.y:0.000}, socket band {minX:0.000}..{maxX:0.000}: gap {gap * 100f:0.0} local cm {(gap > 0.008f ? "ok" : "TOO CLOSE")}");
                if (gap <= 0.008f) bad++;
            }
            sb.Append($"\n  {bad} problem(s).");
            Debug.Log(sb.ToString());
        }

        /// <summary>The grab zone at the near-right corner: what grabs at 0 / 1 / 1.5 / 3 / 10 cm outside the line, in, up, down.</summary>
        public static void Zone()
        {
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null || h.island == null) return;
            var s = h.ZoneShape;
            var t = h.island.transform;
            var sb = new StringBuilder($"[Step14jDebug] Grab zone (scale {s.scale:0.00}, line {s.lineOutset * s.scale * 100f:0.0} cm out of the base, " +
                                       $"band {s.bandTop:0.000}..{s.bandBottom:0.000} local, lip top {s.lipTop:0.000}, {(s.lipActive ? "explore" : "pods active")}):");
            void Probe(string name, Vector3 local) =>
                sb.Append($"\n  {name}: {h.ZoneAt(t.TransformPoint(local))} (to the zone {h.DistanceToHandles(t.TransformPoint(local)) * 100f:0.0} cm)");
            float line = s.half.x + s.lineOutset, z = s.half.y - 0.05f;
            foreach (float cm in new[] { 0f, 1f, 1.5f, 3f, 10f })
                Probe($"right corner {cm} cm out", new Vector3(line + cm * 0.01f / s.scale, -0.03f, z));
            Probe("1.5 cm in over the rock", new Vector3(line - 0.015f / s.scale, -0.03f, z));
            Probe("2.5 cm in", new Vector3(line - 0.025f / s.scale, -0.03f, z));
            Probe("on the cap edge from above", new Vector3(line, s.capTop + 0.01f, z));
            Probe("under the base", new Vector3(line, -0.2f, z));
            Probe("lip middle", new Vector3(0f, -0.05f, s.half.y + s.lineOutset));
            Probe("lip 3 cm out", new Vector3(0f, -0.05f, s.half.y + s.lineOutset + 0.03f / s.scale));
            Debug.Log(sb.ToString());
        }
    }
}
