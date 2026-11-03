using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 14a bridge helpers (exec:Diorama.EditorTools.Step14aDebug.X; arguments in Temp/ClaudeBridge/arg.txt):
    /// map rewrites, a state line (opening stage, placement, lesson, bubble, chip, Spirit vs its target, the arrival
    /// pieces), a shard poke, lesson pinches, handle zones at test points, the arrival gusts and rain, a held pod near
    /// the rim.
    /// </summary>
    public static class Step14aDebug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        public static void GalewickMap()
        {
            var log = new StringBuilder("[Step14aDebug] Galewick:");
            var map = GalewickTools.Write(log);
            var problems = GalewickTools.Check(map, log);
            log.Append(problems.Count == 0 ? "\n  checks pass" : "\n  PROBLEMS: " + string.Join("; ", problems));
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        /// <summary>One line: the opening, placement, lesson, bubble, chip, the Spirit against its target, the arrival pieces.</summary>
        public static void State()
        {
            var sb = new StringBuilder("[Step14aDebug] ");
            var ob = Object.FindFirstObjectByType<Onboarding>();
            sb.Append($"opening {Opening.InProgress}, lesson '{(ob != null ? ob.CurrentId : "-")}'; placement: {IslandPlacer.LastPlacement}");
            var d = Object.FindFirstObjectByType<Dialogue>();
            if (d != null && d.bubble != null) sb.Append($"; bubble '{d.bubble.Text}' chip {d.bubble.IsChip} '{d.bubble.ChipText}', objective '{Dialogue.CurrentObjective}'");
            var spirit = Object.FindFirstObjectByType<LanternSpirit>();
            var state = Dialogue.GetTarget(out var target);
            if (spirit != null)
            {
                sb.Append($"; Spirit {spirit.transform.position:F3}");
                if (state == Dialogue.TargetState.Active) sb.Append($", target {target:F3} (Spirit {(spirit.transform.position - target).y * 100f:0.0} cm above, {new Vector2(spirit.transform.position.x - target.x, spirit.transform.position.z - target.z).magnitude * 100f:0.0} cm aside)");
                else sb.Append($", target {state}");
            }
            foreach (var b in Object.FindObjectsByType<CloudBankPiece>(FindObjectsSortMode.None))
                if (b.Here) sb.Append($"; bank {b.id}: {b.Describe()}, solved {b.IsSolved}, pinned {b.IsPinned}, chip '{b.ChipText}'");
            foreach (var g in Object.FindObjectsByType<RainGarden>(FindObjectsSortMode.None))
                if (g.Here) sb.Append($"; garden {g.id}: {g.Describe()}, solved {g.IsSolved}, pinned {g.IsPinned}, chip '{g.ChipText}', usable {g.Usable}");
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (e != null) sb.Append($"; explorer c{e.Tile.x} r{e.Tile.y} locked {e.Locked}");
            Debug.Log(sb.ToString());
        }

        public static void SkipTutorial() => Debug.Log($"[Step14aDebug] Skip tutorial: {(GameState.Instance != null && GameState.Instance.SkipTutorial())}");

        /// <summary>A shot from in front of the near rim, looking at it (arg: name), to Logs/BattleShots.</summary>
        public static void NearShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            var half = island.FootprintHalfSize;
            var eye = t.TransformPoint(new Vector3(0.25f, island.CapTopLocal + 0.12f, half.y + 0.55f));
            var look = t.TransformPoint(new Vector3(0.12f, island.CapTopLocal - 0.04f, half.y));
            BattleDebug.Render($"Logs/BattleShots/{(string.IsNullOrEmpty(Arg) ? "14a_near" : Arg)}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
        }

        public static void PokeShard()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugPoke();
        }

        public static void Pinch()
        {
            var ob = Object.FindFirstObjectByType<Onboarding>();
            if (ob != null) ob.DebugPinch();
            State();
        }

        public static void Choose()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            if (o != null) o.DebugChoose(int.TryParse(Arg, out int i) ? i : 0);
        }

        /// <summary>Handle zones at island-local test points: each corner, the lip, a pod, the side middle, the back middle.</summary>
        public static void Zones()
        {
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (handle == null || island == null) return;
            var t = island.transform;
            var half = island.FootprintHalfSize;
            float cap = island.CapTopLocal, s = t.lossyScale.x;
            var podLine = t.InverseTransformPoint(new Vector3(0f, handle.PodLineY, 0f) + t.position - new Vector3(0f, t.position.y, 0f)).y;
            var sb = new StringBuilder($"[Step14aDebug] Handle zones (pod line local y {podLine:0.000}, cap {cap:0.000}, mode {(handle.LipActive ? "explore" : "pods active")}):");
            void Test(string name, Vector3 local)
            {
                var w = t.TransformPoint(local);
                sb.Append($"\n  {name}: {handle.ZoneAt(w)} (to handles {handle.DistanceToHandles(w) * 100f:0.0} cm)");
            }
            float out1 = 0.012f / s;
            Test("near-left corner", new Vector3(half.x + out1 * 0.5f, cap - 0.03f, half.y + out1 * 0.5f));
            Test("near-right corner", new Vector3(-half.x - out1 * 0.5f, cap - 0.03f, half.y + out1 * 0.5f));
            Test("far-left corner", new Vector3(half.x + out1 * 0.5f, cap - 0.03f, -half.y - out1 * 0.5f));
            Test("front lip (under the pods)", new Vector3(0.05f, podLine - 0.05f / s, half.y + out1 * 0.5f));
            Test("front at the pod line", new Vector3(0.0f, podLine, half.y + out1 * 0.5f));
            Test("side middle", new Vector3(half.x + out1 * 0.5f, cap - 0.03f, 0f));
            Test("back middle", new Vector3(0f, cap - 0.03f, -half.y - out1 * 0.5f));
            var pods = Object.FindFirstObjectByType<PodRack>();
            if (pods != null && pods.Pod(0) != null) sb.Append($"\n  on pod 1: {handle.ZoneAt(pods.Pod(0).transform.position)}");
            Debug.Log(sb.ToString());
        }

        /// <summary>A gust over the arrival clouds (strength in arg.txt, default 1).</summary>
        public static void ArriveGust()
        {
            float strength = float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1f;
            foreach (var b in Object.FindObjectsByType<CloudBankPiece>(FindObjectsSortMode.None))
                if (b.id == "gw.arrive") b.DebugGust(strength);
            State();
        }

        /// <summary>A palm held down over the arrival stream: arg "column,row,seconds" (default the pads' middle, 3 s).</summary>
        public static void StreamRain()
        {
            foreach (var g in Object.FindObjectsByType<RainGarden>(FindObjectsSortMode.None))
            {
                if (g.id != "lm.stream") continue;
                var parts = Arg.Split(',');
                var at = g.BudsCenter;
                float seconds = 3f;
                if (parts.Length >= 2 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float c) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r)) at = new Vector2(c, r);
                if (parts.Length >= 3) float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
                g.DebugPalm(at, seconds, true);
                Debug.Log($"[Step14aDebug] Rain over the stream at {at} for {seconds} s.");
            }
        }

        /// <summary>Holds the rim preview (a pod 6 cm in front of socket 1) for 3 s, for a shot.</summary>
        public static void HoldPreview()
        {
            double until = EditorApplication.timeSinceStartup + 3.0;
            void Tick()
            {
                var pods = Object.FindFirstObjectByType<PodRack>();
                var island = Object.FindFirstObjectByType<IslandBuilder>();
                if (!EditorApplication.isPlaying || pods == null || island == null || EditorApplication.timeSinceStartup > until)
                {
                    EditorApplication.update -= Tick;
                    return;
                }
                pods.PreviewDrop(pods.HomeWorld(0) + island.transform.forward * 0.06f);
            }
            EditorApplication.update += Tick;
        }

        /// <summary>A fake open right palm for the grab frame: arg "corner" (near-left corner), "lip", or "off".</summary>
        public static void Palm()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            if (island == null || handle == null || Arg == "off")
            {
                IslandGrabFrame.DebugPalm = null;
                return;
            }
            var t = island.transform;
            var half = island.FootprintHalfSize;
            bool lip = Arg == "lip";
            IslandGrabFrame.DebugPalm = () =>
            {
                var front = t.TransformPoint(new Vector3(lip ? 0.05f : half.x, island.CapTopLocal - 0.02f, half.y));
                if (lip) front.y = handle.PodLineY - 0.03f;
                return front + t.forward * 0.02f;
            };
            Debug.Log($"[Step14aDebug] Fake palm at the {(lip ? "front lip" : "near-left corner")}.");
        }

        /// <summary>Previews a rim drop from a point "dx" metres in front of socket 1 (arg, default 0.08) for this frame.</summary>
        public static void Preview()
        {
            var pods = Object.FindFirstObjectByType<PodRack>();
            if (pods == null) return;
            float dx = float.TryParse(Arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.08f;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var at = pods.HomeWorld(0) + island.transform.forward * dx;
            int slot = pods.PreviewDrop(at);
            Debug.Log($"[Step14aDebug] A pod {dx * 100f:0} cm in front of socket 1 previews socket {slot + 1} ({(slot >= 0 ? "ghost shows" : "none")}).");
        }
    }
}
