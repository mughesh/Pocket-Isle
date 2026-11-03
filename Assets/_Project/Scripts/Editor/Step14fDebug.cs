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

        /// <summary>
        /// A2: every visible TextMeshPro, its smallest cap height (mm, at island scale 1: world / island scale), its
        /// colour and outline, against the 14 mm rule. arg = a tag for the log (the screen being audited).
        /// </summary>
        public static void TextAudit()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            float s = island != null ? Mathf.Max(1e-4f, island.transform.lossyScale.x) : 1f;
            var sb = new System.Text.StringBuilder($"[Step14fDebug] Text audit '{Arg}' (island scale {s:0.##}):");
            foreach (var t in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text) || t.color.a < 0.05f) continue;
                t.ForceMeshUpdate();
                var info = t.textInfo;
                float cap = float.MaxValue;
                var face = t.font != null ? t.font.faceInfo : default;
                for (int i = 0; i < info.characterCount; i++)
                {
                    var c = info.characterInfo[i];
                    if (!c.isVisible || char.IsWhiteSpace(c.character)) continue;
                    cap = Mathf.Min(cap, face.capLine * c.scale);
                }
                if (cap == float.MaxValue) continue;
                float mm = cap * t.transform.lossyScale.y / s * 1000f;
                string path = t.transform.parent != null ? $"{t.transform.parent.name}/{t.name}" : t.name;
                if (t.transform.parent != null && t.transform.parent.parent != null) path = $"{t.transform.parent.parent.name}/{path}";
                string line = t.GetParsedText().Replace("\n", " | ");
                if (line.Length > 40) line = line.Substring(0, 40) + "...";
                sb.Append($"\n  {(mm >= 14f ? "ok  " : "SMALL")} {mm,5:0.0} mm  '{line}'  [{path}] col #{ColorUtility.ToHtmlStringRGB(t.color)} outline {t.outlineWidth:0.##}");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// B6: arg "board,bright|mid|dark": a shot from the head over that background
        /// (Logs/BattleShots/14f_buttons_{bg}_{board}.png) and, beside it, every visible tablet's label and screen rect
        /// (.txt: label, x, y, w, h in image pixels, y down) for the contrast check.
        /// </summary>
        public static void BoardShot()
        {
            var a = Arg.Split(',');
            string board = a[0], bg = a.Length > 1 ? a[1] : "mid";
            var main = Camera.main;
            if (main == null) return;
            BattleDebug.Background = bg == "bright" ? new Color(0.93f, 0.92f, 0.9f) : bg == "dark" ? new Color(0.03f, 0.03f, 0.04f) : new Color(0.5f, 0.48f, 0.45f);
            string path = $"Logs/BattleShots/14f_buttons_{bg}_{board}.png";
            var rows = new System.Text.StringBuilder();
            var block = new MaterialPropertyBlock();
            BattleDebug.AfterRender = cam =>
            {
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                {
                    if (r.name != "Tablet" || !r.isVisible && !r.enabled) continue;
                    r.GetPropertyBlock(block);
                    var size = block.GetVector("_Size");
                    var t = r.transform;
                    Vector3 c = cam.WorldToScreenPoint(t.position);
                    Vector3 x = cam.WorldToScreenPoint(t.position + t.right * (size.x * 0.5f * t.lossyScale.x));
                    Vector3 y = cam.WorldToScreenPoint(t.position + t.up * (size.y * 0.5f * t.lossyScale.y));
                    var label = r.GetComponentInChildren<TMPro.TextMeshPro>();
                    string text = label != null ? label.GetParsedText().Replace("\n", " ").Replace(",", " ") : "(icon)";
                    rows.Append($"{text},{c.x:0},{cam.pixelHeight - c.y:0},{2f * Mathf.Abs(x.x - c.x):0},{2f * Mathf.Abs(y.y - c.y):0}\n");
                }
            };
            // From the head, aimed at the tablets' middle (flat Play's seat camera looks wherever it was left).
            // A head 0.45 m in front of the board and a little above it, looking at the tablets' middle (flat Play's seat
            // camera is wherever it was left).
            Vector3 sum = Vector3.zero, fwd = Vector3.forward;
            int n = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.name == "Tablet" && r.enabled)
                {
                    sum += r.transform.position;
                    fwd = r.transform.forward;
                    n++;
                }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            Vector3 at = n > 0 ? sum / n : island != null ? island.transform.position : main.transform.position + main.transform.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 1e-6f ? fwd.normalized : Vector3.forward;
            Vector3 eye = n > 0 ? at - fwd * 0.45f + Vector3.up * 0.16f : main.transform.position;
            try
            {
                BattleDebug.Render(path, eye, Quaternion.LookRotation(at - eye, Vector3.up));
            }
            finally
            {
                BattleDebug.Background = null;
                BattleDebug.AfterRender = null;
            }
            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(path, ".txt"), rows.ToString());
            Debug.Log($"[Step14fDebug] Board shot {path} ({rows.ToString().Split('\n').Length - 1} tablets).");
        }

        /// <summary>Lists the tablet mesh's front triangles that wind backwards (stretched like a 0.1 x 0.04 tablet).</summary>
        public static void TabletWinding()
        {
            var m = TabletKit.Build();
            var v = m.vertices;
            var n = m.normals;
            var t = m.triangles;
            var c = new System.Collections.Generic.List<Color>();
            m.GetColors(c);
            var a = new System.Collections.Generic.List<Vector2>();
            m.GetUVs(1, a);
            var sb = new System.Text.StringBuilder("[Step14fDebug] Backwards front triangles:");
            Vector3 P(int i) => v[i] + (Vector3)(a[i] * new Vector2(0.05f - TabletKit.Corner, 0.02f - TabletKit.Corner));
            for (int i = 0; i < t.Length; i += 3)
            {
                if (n[t[i]].z > -0.99f || n[t[i + 1]].z > -0.99f || n[t[i + 2]].z > -0.99f) continue;
                Vector3 p0 = P(t[i]), p1 = P(t[i + 1]), p2 = P(t[i + 2]);
                float cross = (p1.x - p0.x) * (p2.y - p0.y) - (p1.y - p0.y) * (p2.x - p0.x);
                if (cross > 1e-12f) sb.Append($"\n  tri {i / 3}: part {c[t[i]].a:0.##} verts {t[i]},{t[i + 1]},{t[i + 2]} at {p0.ToString("F4")} {p1.ToString("F4")} {p2.ToString("F4")}");
            }
            Debug.Log(sb.ToString());
            Object.DestroyImmediate(m);
        }

        /// <summary>Opens the game menu (arg "close" closes it).</summary>
        public static void Menu()
        {
            var m = Object.FindFirstObjectByType<GameMenu>();
            if (m == null) return;
            if (Arg == "close") m.Close("bridge");
            else m.TryOpen();
        }

        static float F(string s) => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
    }
}
