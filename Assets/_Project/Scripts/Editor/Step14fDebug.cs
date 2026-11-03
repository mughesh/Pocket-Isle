using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 14f bridge checks (flat Editor Play): the last walk lesson, the pinch guard on hand moments.</summary>
    public static class Step14fDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static Onboarding Onboarding_ => Object.FindFirstObjectByType<Onboarding>();

        /// <summary>Puts the tutorial at the lesson in arg (as an old save would resume there).</summary>
        public static void Lesson()
        {
            var o = Onboarding_;
            if (o == null) return;
            string id = string.IsNullOrEmpty(Arg) ? Onboarding.FarLesson : Arg;
            o.Restore(id, false);
            o.Runner.RewindTo(id); // forgets events from earlier checks this session
            LessonState();
        }

        public static void LessonState()
        {
            var o = Onboarding_;
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (o == null || e == null) return;
            var r = o.Runner;
            Debug.Log($"[Step14fDebug] lesson '{o.CurrentId ?? "none"}' {r.State} teaching {r.Teaching:0.0} s, last '{r.LastReason}', " +
                      $"gold {(o.GoldTile.HasValue ? $"c{o.GoldTile.Value.x} r{o.GoldTile.Value.y}" : "none")}, explorer c{e.Tile.x} r{e.Tile.y} " +
                      $"plan {e.PlanIndex}/{e.Plan.Count} ({e.PlanEnd}), moving {e.Heading != Vector2Int.zero}, finished {o.IsFinished}.");
        }

        /// <summary>Every hand moment's guard points now, and whether a pinch at each would be claimed.</summary>
        public static void Guards() => Debug.Log("[Step14fDebug] Hand moments:" + HandPuzzle.GuardReport());

        /// <summary>A pinch at the world point "x,y,z" (or at the explorer + arg metres up): claimed by a hand moment?</summary>
        public static void Claims()
        {
            var v = Arg.Split(',');
            Vector3 p;
            if (v.Length == 3) p = new Vector3(F(v[0]), F(v[1]), F(v[2]));
            else
            {
                var e = Object.FindFirstObjectByType<GestureTestPiece>();
                p = e != null ? e.transform.position : Vector3.zero;
            }
            bool claimed = HandPuzzle.ClaimsPinch(p, out var by);
            Debug.Log($"[Step14fDebug] Pinch at {p}: {(claimed ? $"claimed by {by.id}" : "free (walks)")}.");
        }

        /// <summary>A close shot of the Heart's forehead: arg "name[,tiles away]".</summary>
        public static void CrystalShot()
        {
            var h = Object.FindFirstObjectByType<HeartOfWorld>();
            if (h == null) return;
            var a = Arg.Split(',');
            float away = a.Length > 1 ? F(a[1]) : 2.5f;
            var t = h.island.transform;
            Vector3 c = h.SocketWorld;
            Vector3 eye = c + t.forward * (away * h.UnitWorld) + t.up * (0.3f * h.UnitWorld);
            BattleDebug.Render($"Logs/BattleShots/{a[0]}.png", eye, Quaternion.LookRotation(c - eye, Vector3.up));
            Debug.Log($"[Step14fDebug] Crystal shot {a[0]}.png, crystal {h.crystal:0.##}.");
        }

        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
