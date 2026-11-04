using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 14k (Play mode). Arguments come from Temp/ClaudeBridge/arg.txt.</summary>
    public static class Step14kDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        // ---- A: the grab band ----

        /// <summary>The grab band at the near-right corner: what grabs at 1 cm outside, on the line, 1 / 3 / 5 cm inside, high and low.</summary>
        public static void Zone()
        {
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null || h.island == null) return;
            var s = h.ZoneShape;
            var t = h.island.transform;
            var sb = new StringBuilder($"[Step14kDebug] Grab band (scale {s.scale:0.00}, {h.grabInward * 100f:0.0} cm in, {h.grabPastLine * 1000f:0} mm slack, " +
                                       $"top {s.bandTop:0.000} local = {(s.bandTop - s.capTop) * s.scale * 100f:0.0} cm over the cap, {(s.lipActive ? "explore" : "pods active")}):");
            void Probe(string name, Vector3 local) =>
                sb.Append($"\n  {name}: {h.ZoneAt(t.TransformPoint(local))} (to the zone {h.DistanceToHandles(t.TransformPoint(local)) * 100f:0.0} cm)");
            float line = s.half.x + s.lineOutset, z = s.half.y - 0.09f, y = s.capTop + 0.01f / s.scale;
            foreach (float cm in new[] { 3f, 1f, 0f, -1f, -3f, -5f })
                Probe(cm > 0 ? $"{cm} cm outside the line, 1 cm over the cap" : cm == 0 ? "on the line" : $"{-cm} cm inside, 1 cm over the cap",
                    new Vector3(line + cm * 0.01f / s.scale, y, z));
            Probe("3 cm inside on the rock face (3 cm under the cap)", new Vector3(line - 0.03f / s.scale, s.capTop - 0.03f / s.scale, z));
            Probe("1 cm inside, 4 cm over the cap", new Vector3(line - 0.01f / s.scale, s.capTop + 0.04f / s.scale, z));
            Probe("lip 2 cm in", new Vector3(0f, -0.05f, s.half.y + s.lineOutset - 0.02f / s.scale));
            Probe("lip 1 cm out", new Vector3(0f, -0.05f, s.half.y + s.lineOutset + 0.01f / s.scale));
            var rim = h.rimColliders;
            if (rim != null)
                foreach (var c in rim)
                    if (c is BoxCollider b) sb.Append($"\n  shell box {b.center.ToString("F3")} size {b.size.ToString("F3")}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// The band lit: a fake palm 2 cm inside the near-right corner (arg "off" clears it), the camera low over that corner,
        /// shot 14k_band.png after the glow has eased in.
        /// </summary>
        public static void BandShot()
        {
            if (Arg == "off")
            {
                IslandGrabFrame.DebugPalm = null;
                return;
            }
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null || h.island == null) return;
            var s = h.ZoneShape;
            var t = h.island.transform;
            Vector3 palm = t.TransformPoint(new Vector3(s.half.x + s.lineOutset - 0.02f / s.scale, s.capTop + 0.01f / s.scale, s.half.y - 0.09f));
            IslandGrabFrame.DebugPalm = () => palm;
            Vector3 corner = t.TransformPoint(new Vector3(s.half.x, s.capTop, s.half.y));
            Vector3 eye = corner + t.rotation * new Vector3(-0.08f, 0.14f, 0.16f);
            var steps = new List<(double, System.Action)>
            {
                (EditorApplication.timeSinceStartup + 0.6, () =>
                {
                    BattleDebug.Render("Logs/BattleShots/14k_band.png", eye, Quaternion.LookRotation(corner - eye, Vector3.up));
                    Debug.Log($"[Step14kDebug] Band shot 14k_band.png (fake palm in zone: {h.ZoneAt(palm)}).");
                }),
            };
            Schedule(steps);
        }

        /// <summary>Logs and clears the Editor's pause (Error Pause can freeze a flat Play on an exception).</summary>
        public static void Unpause()
        {
            Debug.Log($"[Step14kDebug] Editor paused {EditorApplication.isPaused}, time scale {Time.timeScale}, frame {Time.frameCount}: unpausing.");
            EditorApplication.isPaused = false;
        }

        // ---- B: the Journal ----

        /// <summary>Hovers a Journal cell as a fingertip would (arg = cell, -1 = off); logs its labels.</summary>
        public static void JournalHover()
        {
            JournalBoard.DebugHover = int.TryParse(Arg, out int c) ? c : -1;
            var j = JournalBoard.Instance;
            var sb = new StringBuilder($"[Step14kDebug] Journal hover {JournalBoard.DebugHover}; labels:");
            if (j != null && j.runes != null && j.runes.Root != null)
                foreach (var t in j.runes.Root.GetComponentsInChildren<TMPro.TextMeshPro>())
                    if (t.name.StartsWith("Cell") && !string.IsNullOrEmpty(t.text))
                        sb.Append($"\n  {t.name}: {t.text.Replace("\n", " / ")} (lines {t.textInfo.lineCount}, size {t.fontSize:0.000})");
            Debug.Log(sb.ToString());
        }

        // ---- C: lines in context ----

        static void Go(string label)
        {
            var s = Object.FindFirstObjectByType<GameState>();
            if (s == null) return;
            foreach (var spot in s.Spots)
                if (spot.label == label)
                {
                    s.GoTo(spot);
                    return;
                }
            Debug.LogWarning($"[Step14kDebug] No spot '{label}'.");
        }

        static void Idle()
        {
            var d = Object.FindFirstObjectByType<Dialogue>();
            if (d != null) d.DebugIdle();
        }

        /// <summary>
        /// The flat pass: hub -> lodge -> Cinderwood -> the Forge Hall -> a wild battle, the intros forgotten first; an idle
        /// hint asked for in each place, and a few lines fired in the wrong place. Every line shown logs "[Dialogue] Show ...".
        /// About 100 s. Back up the Editor save first (Editor Play writes it).
        /// </summary>
        public static void Pass()
        {
            Dialogue.DebugForget("lodge.first", "lodge.first.empty", "lodge.inspect", "lodge.take", "lodge.leave", "lodge.exit", "lodge.toss",
                "lodge.inspect.one", "cw.arrive", "realm.cinderwood.first", "cinder.hall.first", "cinder.keeper.first", "hub.return.first",
                "tut.grass", "pod.first", "battle.first", "menu.first", "cinder.village.first");
            double t0 = EditorApplication.timeSinceStartup;
            var steps = new List<(double, System.Action)>();
            void At(double s, string what, System.Action act) => steps.Add((t0 + s, () =>
            {
                Debug.Log($"[Step14kDebug] Pass {s:0} s: {what} (place {Places.Now}).");
                act();
            }));
            At(0, "hub: to the shrine", () => Go("Hub: shrine"));
            At(10, "hub: idle hint", Idle);
            At(14, "hub: lodge and Cinderwood lines fired here", () =>
            {
                Dialogue.Say("lodge.first");
                Dialogue.Say("cw.arrive");
            });
            At(18, "enter the lodge", BattleDebug.EnterLodge);
            At(24, "lodge: grass and hub lines fired here", () =>
            {
                Dialogue.Say("tut.grass");
                Dialogue.Say("hub.return.first");
                Dialogue.Say("gate.fire.first");
            });
            At(32, "lodge: idle hint", Idle);
            At(38, "leave the lodge", BattleDebug.LeaveLodge);
            At(44, "travel to Cinderwood", () => Go("CW: arrival"));
            At(56, "Cinderwood: idle hint", Idle);
            At(58, "Cinderwood: lodge and hub lines fired here", () =>
            {
                Dialogue.Say("lodge.last");
                Dialogue.Say("hub.return.first");
            });
            At(62, "into the Forge Hall", () => Go("Forge Hall"));
            At(74, "hall: idle hint, a Cinderwood map line", () =>
            {
                Idle();
                Dialogue.Say("cinder.village.first");
            });
            At(80, "out to the village", () => Go("CW: village"));
            At(90, "wild battle", BattleDebug.Grass);
            At(100, "battle: idle hint, a map line", () =>
            {
                Idle();
                Dialogue.Say("menu.first");
                Dialogue.Say("lodge.last");
                Dialogue.Say("gate.sealed");
            });
            At(108, "run", BattleDebug.Run);
            At(116, "done", () => { });
            Schedule(steps);
        }

        // ---- F: walk-up triggers ----

        /// <summary>
        /// Every walk-up trigger on every outdoor map (edit mode or Play): shrines (ring), gates and doors (front row and
        /// its diagonals), hand moments (reach), and where the Spirit's trail ends for each (does that tile trigger it?).
        /// "WAS OFF" marks what the old rule missed.
        /// </summary>
        public static void WalkUpAudit()
        {
            var swap = Object.FindFirstObjectByType<SceneSwap>(FindObjectsInactive.Include);
            if (swap == null) return;
            var pieces = Object.FindObjectsByType<HandPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var forge = Object.FindFirstObjectByType<ForgeBellows>(FindObjectsInactive.Include);
            var towers = Object.FindObjectsByType<TowerDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var temples = Object.FindObjectsByType<TempleDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var sb = new StringBuilder("[Step14kDebug] Walk-up audit (rule: any tile within 1 of the footprint, diagonals too; doors and gates: the front row and its diagonals):");
            int off = 0;
            foreach (var world in swap.worlds)
            {
                if (world == null) continue;
                sb.Append($"\n{world.name}:");
                // Shrines.
                for (int r = 0; r < world.Rows; r++)
                    for (int c = 0; c < world.Columns; c++)
                        if (world.TryGetProp(world.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == HearthShrines.RecipeName)
                        {
                            var s = new Vector2Int(c, r);
                            int walk = 0, miss = 0, was = 0;
                            var oldFoot = new RectInt(c, r, 2, 2);
                            for (int y = r - 1; y <= r + 3; y++)
                                for (int x = c - 1; x <= c + 2; x++)
                                {
                                    var t = new Vector2Int(x, y);
                                    if (!In(world, t) || !world.CanWalk(t) || WalkUp.Distance(t, HearthShrines.Footprint(s)) > 1) continue;
                                    walk++;
                                    if (!HearthShrines.IsTouching(s, t)) miss++;
                                    else if (!WalkUp.Next(t, oldFoot)) was++;
                                }
                            sb.Append($"\n  shrine c{c} r{r} (body r{r}..{r + 1}, front steps r{r + 2}): {walk} walkable tiles within one, all trigger{(miss > 0 ? $" EXCEPT {miss}" : "")}" +
                                      (was > 0 ? $"; WAS OFF from {was} of them (the path row in front of the steps and its diagonals; now on)" : ""));
                            off += miss + was;
                        }
                // Gates.
                foreach (var g in world.gates)
                {
                    var fp = ShardGates.Footprint(g);
                    var added = new List<string>();
                    foreach (var t in new[] { new Vector2Int(fp.xMin - 1, fp.yMax), new Vector2Int(fp.xMax, fp.yMax) })
                        if (In(world, t) && world.CanWalk(t)) added.Add($"c{t.x} r{t.y}");
                    var end = GameState.NearestWalkable(world, g.Front(0));
                    sb.Append($"\n  gate {g.id} c{g.tile.x} r{g.tile.y}: front c{g.Front(0).x}..{g.Front(1).x} r{g.Front(0).y}" +
                              (added.Count > 0 ? $"; WAS OFF from the front diagonals {string.Join(", ", added)} (now on)" : "") +
                              $"; trail end c{end.x} r{end.y} {(ShardGates.AtFront(g, end) ? "triggers" : "DOESN'T TRIGGER")}");
                    off += added.Count + (ShardGates.AtFront(g, end) ? 0 : 1);
                }
                // Doors.
                foreach (var d in towers)
                    if (d != null && d.map == world) off += Door(sb, world, "wind tower door", d.front, d.AtFront, "tower");
                foreach (var d in temples)
                    if (d != null && d.map == world) off += Door(sb, world, "lake temple door", d.front, d.AtFront, "temple");
                if (forge != null && forge.pump != null && forge.pump.map == world)
                {
                    var door = forge.door;
                    bool Walk(Vector2Int t) => t.y == door.y && t.x >= door.x && t.x <= door.x + 2;
                    string mark = world.TryGetLandmark("temple", out var lt) ? $"; trail end (landmark temple) c{GameState.NearestWalkable(world, lt).x} r{GameState.NearestWalkable(world, lt).y}" : "";
                    sb.Append($"\n  forge door c{door.x}..{door.x + 2} r{door.y}: walk into the doorway (a walk-in, not a stand-next-to){mark}");
                }
                // Hand moments.
                foreach (var p in pieces)
                {
                    if (p == null || p.map != world || p is StatueSocket) continue;
                    var home = Vector2Int.RoundToInt(p.Home);
                    var oldEnd = GameState.NearestWalkable(world, home);
                    var newEnd = SpiritTrail.TrailEnd(world, p, oldEnd + Vector2Int.up * 3);
                    bool oldOk = p.InReach(oldEnd);
                    int ringMiss = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var t = home + new Vector2Int(dx, dy);
                            if (In(world, t) && world.CanWalk(t) && !p.InReach(t)) ringMiss++;
                        }
                    sb.Append($"\n  {p.id} ({p.GetType().Name}) home c{p.Home.x:0.#} r{p.Home.y:0.#} reach {p.reach}: trail end was c{oldEnd.x} r{oldEnd.y} " +
                              $"{(oldOk ? "in reach" : "OUT OF REACH (WAS OFF)")}, now c{newEnd.x} r{newEnd.y} {(p.InReach(newEnd) ? "in reach" : "OUT OF REACH")}" +
                              (ringMiss > 0 ? $"; {ringMiss} neighbour tiles out of reach" : ""));
                    off += (oldOk ? 0 : 1) + ringMiss;
                }
            }
            sb.Append($"\n{off} thing(s) the old rules missed (fixed by the 14k rule where marked).");
            Debug.Log(sb.ToString());
        }

        static bool In(WorldMap w, Vector2Int t) => t.x >= 0 && t.y >= 0 && t.x < w.Columns && t.y < w.Rows;

        static int Door(StringBuilder sb, WorldMap world, string name, Vector2Int[] front, System.Func<Vector2Int, bool> atFront, string landmark)
        {
            if (front == null || front.Length == 0) return 0;
            int minX = int.MaxValue, maxX = int.MinValue, y = front[0].y;
            foreach (var f in front)
            {
                minX = Mathf.Min(minX, f.x);
                maxX = Mathf.Max(maxX, f.x);
            }
            var added = new List<string>();
            foreach (var t in new[] { new Vector2Int(minX - 1, y), new Vector2Int(maxX + 1, y) })
                if (In(world, t) && world.CanWalk(t) && atFront(t)) added.Add($"c{t.x} r{t.y}");
            string mark = "";
            int bad = 0;
            if (world.TryGetLandmark(landmark, out var lt))
            {
                var end = GameState.NearestWalkable(world, lt);
                bool ok = atFront(end);
                if (!ok) bad++;
                mark = $"; trail end (landmark {landmark}) c{end.x} r{end.y} {(ok ? "triggers" : "DOESN'T TRIGGER")}";
            }
            sb.Append($"\n  {name} front c{minX}..{maxX} r{y}{(added.Count > 0 ? $"; WAS OFF from the front diagonals {string.Join(", ", added)} (now on)" : "")}{mark}");
            return added.Count + bad;
        }

        internal static void Schedule(List<(double at, System.Action act)> steps)
        {
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.update -= tick;
                    return;
                }
                while (steps.Count > 0 && EditorApplication.timeSinceStartup >= steps[0].at)
                {
                    var act = steps[0].act;
                    steps.RemoveAt(0);
                    act();
                }
                if (steps.Count == 0) EditorApplication.update -= tick;
            };
            EditorApplication.update += tick;
        }
    }
}
