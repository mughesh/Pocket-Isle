using System.IO;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 11b checks through the Claude bridge (exec:Diorama.EditorTools.Step11bDebug.X), in Play. Start with
    /// GalewickPlay.Start, go with GalewickPlay.Go ("LM: channel" ...), shots with GalewickPlay.Shot. Args in Temp/ClaudeBridge/arg.txt.
    /// Puzzle state: Step10fDebug.Reset / Solve (arg = id), the save is real (put it back after).
    /// </summary>
    static class Step11bDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";
        static T Find<T>() where T : Object => Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        static float[] Floats()
        {
            var parts = Arg.Split(',');
            var f = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                float.TryParse(parts[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f[i]);
            return f;
        }

        // ---- A: pads ----

        /// <summary>arg "i,vx,vy": push pad i at (columns, rows) a second.</summary>
        public static void Push()
        {
            var p = Find<LilyPadBridge>();
            var f = Floats();
            if (p != null && f.Length >= 3) p.DebugPush((int)f[0], new Vector2(f[1], f[2]));
        }

        /// <summary>arg "i": look + pinch pad i.</summary>
        public static void Steer()
        {
            var p = Find<LilyPadBridge>();
            if (p != null && int.TryParse(Arg, out int i)) p.DebugSteer(i);
        }

        // ---- B: rain ----

        /// <summary>arg "bud,seconds,wiggle(0/1)": a palm held down over bud number bud.</summary>
        public static void Rain()
        {
            var g = Find<RainGarden>();
            var f = Floats();
            if (g == null || f.Length < 3) return;
            int i = Mathf.Clamp((int)f[0], 0, g.buds.Length - 1);
            g.DebugPalm(g.buds[i], f[1], f[2] > 0.5f);
        }

        // ---- C: scoop ----

        /// <summary>arg "where,height,up": where = spot (the scoop spot) or basin; height in tiles above the cap; palm up -1..1.</summary>
        public static void Hand()
        {
            var s = Find<ScoopPour>();
            var parts = Arg.Split(',');
            if (s == null || parts.Length < 3) return;
            var at = parts[0] == "basin" ? s.basin : s.scoopSpot;
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float h);
            float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float up);
            s.DebugHand(new Vector3(at.x, h, at.y), up);
        }

        public static void HandOff()
        {
            var s = Find<ScoopPour>();
            if (s != null) s.DebugHandOff();
        }

        public static void LookPour()
        {
            var s = Find<ScoopPour>();
            if (s != null) s.DebugLookPour();
        }

        public static void State()
        {
            Debug.Log($"[Step11bDebug] pads: {(Find<LilyPadBridge>() is LilyPadBridge p ? $"{p.Describe()}, usable {p.Usable}, lesson {p.LessonShowing}" : "none")}; " +
                      $"rain: {(Find<RainGarden>() is RainGarden g ? $"{g.Describe()}, usable {g.Usable}, lesson {g.LessonShowing}" : "none")}; " +
                      $"scoop: {(Find<ScoopPour>() is ScoopPour s ? $"{s.Describe()}, usable {s.Usable}, lesson {s.LessonShowing}" : "none")}; " +
                      $"restored {RealmState.Restored:0.##}, puzzles {PuzzleState.Describe()}");
        }
    }
}
