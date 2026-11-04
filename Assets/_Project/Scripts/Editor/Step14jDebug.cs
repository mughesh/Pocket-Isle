using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
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

        /// <summary>
        /// Section C shots: arg "wildId,Species1,Species2,...": each species (Lv 15) in pod 1 on the near pad against the
        /// wild one (Lv 2), the camera at the seated head (BattleDebug.SeatCamera); shot 14j_labels_&lt;species&gt;.png, its pad
        /// fit logged, then it runs. One every 14 s.
        /// </summary>
        public static void LabelShots()
        {
            var a = Arg.Split(',');
            if (a.Length < 2) return;
            var g = GameState.Instance;
            var wild = g.registry.Find(a[0]);
            var steps = new List<(double at, System.Action act)>();
            double t0 = EditorApplication.timeSinceStartup;
            HoldLow();
            for (int i = 1; i < a.Length; i++)
            {
                string id = a[i];
                double t = t0 + (i - 1) * 14.0;
                steps.Add((t, () =>
                {
                    var s = g.registry.Find(id);
                    if (s == null || wild == null)
                    {
                        Debug.LogWarning($"[Step14jDebug] No species '{id}' or '{a[0]}'.");
                        return;
                    }
                    g.party.members[0] = new IslingInstance(s, 15);
                    g.party.NotifyChanged();
                    bool ok = g.encounters.DebugEncounter(new IslingInstance(wild, 2));
                    Debug.Log($"[Step14jDebug] Label shot {id} vs {wild.displayName}: {(ok ? "battle" : "refused")}.");
                }));
                steps.Add((t + 4.0, () => BattleDebug.SwitchNext()));
                steps.Add((t + 9.0, () =>
                {
                    Shoot($"14j_labels_{id.ToLowerInvariant()}");
                    Fits();
                }));
                steps.Add((t + 10.0, () => BattleDebug.Run()));
            }
            Schedule(steps);
        }

        /// <summary>
        /// A Keeper battle in the hall of arg "keeperId[,shot]" (e.g. cinder.keeper): travels there, makes it frightened
        /// again and starts the meeting; with a shot name, shoots 16 s later (after the intro) from the seated head.
        /// </summary>
        public static void Keeper()
        {
            var a = Arg.Split(',');
            KeeperHall hall = null;
            foreach (var h in Object.FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (h.keeperId == a[0]) hall = h;
            var g = GameState.Instance;
            if (hall == null || g == null)
            {
                Debug.LogWarning($"[Step14jDebug] No hall for '{a[0]}'.");
                return;
            }
            var steps = new List<(double at, System.Action act)>();
            double t0 = EditorApplication.timeSinceStartup;
            foreach (var s in g.Spots)
                if (s.map == hall.hall)
                {
                    g.GoTo(s);
                    Debug.Log($"[Step14jDebug] Going to {s.label}.");
                    break;
                }
            steps.Add((t0 + 5.0, () =>
            {
                // Frightened again, in memory only (GameState.SetPuzzle reloads the save: the title came up over the battle).
                if (hall.Calmed)
                {
                    var saved = PuzzleState.ToSaved();
                    foreach (var p in saved)
                        if (p.id == hall.keeperId) p.solved = false;
                    PuzzleState.Restore(saved);
                }
                var meet = typeof(KeeperHall).GetMethod("Meet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                hall.StartCoroutine((System.Collections.IEnumerator)meet.Invoke(hall, null));
                HoldLow();
                Debug.Log($"[Step14jDebug] {a[0]}: the meeting starts.");
            }));
            if (a.Length > 1)
                steps.Add((t0 + 21.0, () =>
                {
                    Shoot(a[1]);
                    Fits();
                }));
            Schedule(steps);
        }

        /// <summary>A shot from the camera as it is (the seated head when SeatCamera holds it); arg = name.</summary>
        public static void Shot() => Shoot(string.IsNullOrEmpty(Arg) ? "14j_shot" : Arg);

        static void Shoot(string name)
        {
            var cam = Camera.main;
            if (cam == null) return;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", cam.transform.position, cam.transform.rotation);
            Debug.Log($"[Step14jDebug] Shot {name}.png.");
        }

        /// <summary>Each battle actor's pad fit and drawn height, and its label's place against its visual top.</summary>
        public static void Fits()
        {
            var sb = new StringBuilder("[Step14jDebug] On the pads:");
            var hud = Object.FindFirstObjectByType<BattleHud>();
            foreach (var actor in Object.FindObjectsByType<StageActor>(FindObjectsSortMode.None))
            {
                actor.Measure(out float h, out _);
                bool top = actor.TryGetVisualTop(out float vt);
                sb.Append($"\n  {actor.name}: fit x{actor.fit:0.00} (target {actor.fitTarget:0.00}), drawn height {h:0.000} local, visual top {(top ? $"{vt:0.000}" : "-")} y");
            }
            if (hud != null)
                foreach (Transform c in hud.transform)
                    if (c.name.StartsWith("HP "))
                    {
                        var cam = Camera.main;
                        var v = cam != null ? cam.WorldToViewportPoint(c.position) : Vector3.zero;
                        sb.Append($"\n  label {c.name} at y {c.position.y:0.000}, view {v.x:0.00},{v.y:0.00} (z {v.z:0.00}), active {c.gameObject.activeInHierarchy}");
                    }
            sb.Append($"\n  title showing: {TitleScreen.Showing}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Holds the camera at a low seated head like Mugesh's headset shots (0.25 m over the island, 0.45 m in front,
        /// looking at its middle): the seat camera's (0.55 m up) looks almost straight down. Stops when Play ends.
        /// </summary>
        public static void HoldLow()
        {
            EditorApplication.update -= Low;
            EditorApplication.update += Low;
            Low();
        }

        static void Low()
        {
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!EditorApplication.isPlaying || cam == null || island == null)
            {
                EditorApplication.update -= Low;
                return;
            }
            var t = island.transform;
            cam.transform.position = t.position + t.rotation * new Vector3(0f, 0.25f, 0.45f);
            cam.transform.LookAt(t.position + Vector3.up * 0.06f);
            // The shot camera's field of view (BattleDebug.Render): the labels clamp to what the shot shows.
            cam.fieldOfView = 55f;
        }

        /// <summary>The readable-battle rule's inputs and result now (eye, feet, heights, allowed near height).</summary>
        public static void Sight()
        {
            var stage = Object.FindFirstObjectByType<BattleStage>();
            var cam = Camera.main;
            if (stage == null || cam == null) return;
            var f = typeof(BattleStage).GetField("_player", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var w = typeof(BattleStage).GetField("_wild", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var near = f.GetValue(stage) as StageActor;
            var wild = w.GetValue(stage) as StageActor;
            if (near == null || wild == null) return;
            var island = near.island;
            float cap = island.Content.capHeight;
            Vector3 eye = island.transform.InverseTransformPoint(cam.transform.position);
            Vector3 nf = island.TileToLocal(near.tile, cap + near.baseLift), wf = island.TileToLocal(wild.tile, cap + wild.baseLift);
            near.MeasureCached(out float nh, out _);
            wild.MeasureCached(out float wh, out _);
            float allowed = PadFit.AllowedNearHeight(eye, nf, wf, wh * wild.fit);
            Debug.Log($"[Step14jDebug] Sight: eye {eye.ToString("F3")}, near feet {nf.ToString("F3")} h {nh:0.000} x{near.fit:0.00} (grow {near.grow:0.00}, lift {near.lift:0.000}), " +
                      $"wild feet {wf.ToString("F3")} h {wh:0.000} x{wild.fit:0.00} (grow {wild.grow:0.00}): allowed near {allowed:0.000} (fit {allowed / Mathf.Max(1e-5f, nh):0.00}).");
        }

        /// <summary>The explorer see-through: arg "off" / "on"; logs hidden, amount and the check's CPU time.</summary>
        public static void Explorer()
        {
            if (Arg == "off") ExplorerSeeThrough.DebugOff = true;
            else if (Arg == "on") ExplorerSeeThrough.DebugOff = false;
            var e = Object.FindFirstObjectByType<ExplorerSeeThrough>();
            Debug.Log($"[Step14jDebug] Explorer see-through {(ExplorerSeeThrough.DebugOff ? "OFF" : "on")}: " +
                      (e != null ? $"hidden {e.Hidden}, amount {e.Amount:0.00}, check {ExplorerSeeThrough.LastMs:0.000} ms" : "no explorer"));
        }

        /// <summary>A close-up of the explorer along the head's sightline, 12 cm from it (arg = name).</summary>
        public static void ExplorerShot()
        {
            var e = Object.FindFirstObjectByType<ExplorerView>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (e == null || island == null) return;
            // From the head toward the explorer, 12 cm short of it (a close-up along the same sightline).
            var cam = Camera.main;
            Vector3 target = e.transform.position + Vector3.up * 0.01f;
            Vector3 eye = cam != null ? target + (cam.transform.position - target).normalized * 0.12f : target + Vector3.up * 0.1f;
            BattleDebug.Render($"Logs/BattleShots/{(string.IsNullOrEmpty(Arg) ? "14j_explorer" : Arg)}.png", eye, Quaternion.LookRotation(e.transform.position - eye, Vector3.up));
            Debug.Log($"[Step14jDebug] Explorer shot {Arg}.png.");
        }

        /// <summary>Teleports the explorer to a tile of the current map (arg "column,row").</summary>
        public static void Teleport()
        {
            var a = Arg.Split(',');
            var g = GameState.Instance;
            if (g == null || g.explorer == null || a.Length < 2) return;
            g.explorer.Teleport(new Vector2Int(int.Parse(a[0]), int.Parse(a[1])));
            Debug.Log($"[Step14jDebug] Teleported to {Arg}.");
        }

        static void Schedule(List<(double at, System.Action act)> steps)
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
