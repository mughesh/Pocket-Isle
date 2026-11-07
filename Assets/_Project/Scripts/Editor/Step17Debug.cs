using System.IO;
using System.Reflection;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for step 17 (Play mode): simulated catch throws, grab-lock and pose reports.</summary>
    public static class Step17Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static EncounterDirector Director => Object.FindFirstObjectByType<EncounterDirector>();

        /// <summary>
        /// A catch-pod release as if thrown by hand (arg = "yawDegrees,worldSpeed[,upDegrees]"): from the cradle pod, the
        /// flat direction turned yawDegrees off the line to the wild creature. Goes through the real throw handler.
        /// </summary>
        public static void Throw()
        {
            var d = Director;
            var pods = Object.FindFirstObjectByType<PodRack>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var stage = Object.FindFirstObjectByType<BattleStage>();
            if (d == null || pods == null || island == null || stage == null || stage.Wild == null)
            {
                Debug.LogWarning("[Step17Debug] Throw: no battle with a wild creature.");
                return;
            }
            var a = Arg.Split(',');
            float yaw = a.Length > 0 && float.TryParse(a[0], out var y) ? y : 0f;
            float speed = a.Length > 1 && float.TryParse(a[1], out var s) ? s : 1f;
            float up = a.Length > 2 && float.TryParse(a[2], out var u) ? u : 10f;
            var pod = pods.Pod(PodRack.CradleSlot);
            Vector3 from = pod != null ? pod.transform.localPosition : pods.HomeLocal(PodRack.CradleSlot);
            Vector3 to = stage.Wild.LocalCenter - from;
            to.y = 0f;
            Vector3 dir = Quaternion.Euler(-up, yaw, 0f) * Quaternion.LookRotation(to.normalized) * Vector3.forward;
            Vector3 vel = dir * (speed / Mathf.Max(1e-4f, island.transform.lossyScale.x));
            var m = typeof(EncounterDirector).GetMethod("OnPodThrown", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Log($"[Step17Debug] Throw {yaw:0} deg off, {speed:0.00} m/s, {up:0} deg up.");
            m.Invoke(d, new object[] { PodRack.CradleSlot, from, vel });
        }

        /// <summary>Presses a rune by its label (arg), e.g. "Continue" on the title.</summary>
        public static void Press()
        {
            RuneBoard.DebugPress(Arg);
            Debug.Log($"[Step17Debug] Pressed '{Arg}'.");
        }

        /// <summary>
        /// Shows a ghost clip frozen at a point (arg = "clip,u[,target]"): with a target id, AtTarget (aside, as the
        /// evolution does); without, AboveIsland. "off" ends it. Then shots: front (seated) and side.
        /// </summary>
        public static void GhostAt()
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (g == null) return;
            if (Arg == "off")
            {
                g.debugFreeze = -1f;
                g.Hide();
                return;
            }
            var a = Arg.Split(',');
            float u = a.Length > 1 && float.TryParse(a[1], out var f) ? f : 1f;
            g.debugFreeze = u;
            if (a.Length > 2) g.Show(a[0], GhostPlace.AtTarget, a[2], 1f, DominantHand.Left, false, aside: true);
            else g.Show(a[0], GhostPlace.AboveIsland, "ghost", 0.6f, DominantHand.Left, false);
            Debug.Log($"[Step17Debug] Ghost {a[0]} frozen at {u:0.00}{(a.Length > 2 ? $" at {a[2]}" : "")}.");
        }

        /// <summary>
        /// Step 17 G: holds the main camera as a head leaning in for fine work: 28 cm in front of and 22 cm above the
        /// island's front middle, shifted sideways by arg cm (the island's right), looking at the near half. "off" stops.
        /// </summary>
        public static void Lean()
        {
            UnityEditor.EditorApplication.update -= HoldLean;
            if (Arg == "off") return;
            _lean = float.TryParse(Arg, out var cm) ? cm / 100f : 0f;
            UnityEditor.EditorApplication.update += HoldLean;
            HoldLean();
            Debug.Log($"[Step17Debug] Leaning in, {_lean * 100f:0} cm to the side.");
        }

        static float _lean;

        static void HoldLean()
        {
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!UnityEditor.EditorApplication.isPlaying || cam == null || island == null)
            {
                UnityEditor.EditorApplication.update -= HoldLean;
                return;
            }
            var t = island.transform;
            // Fixed in the world from where the island was when the lean started (a turning island must not drag the head).
            if (_leanAnchor == null || _leanAnchorFor != _lean)
            {
                _leanAnchorFor = _lean;
                var front = t.TransformPoint(new Vector3(0f, island.CapTopLocal, island.FootprintHalfSize.y));
                Vector3 fwd = t.forward;
                fwd.y = 0f;
                fwd.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                _leanAnchor = (front + fwd * 0.28f + Vector3.up * 0.22f - right * _lean, front - fwd * 0.08f);
            }
            cam.transform.position = _leanAnchor.Value.eye;
            cam.transform.LookAt(_leanAnchor.Value.look);
        }

        static (Vector3 eye, Vector3 look)? _leanAnchor;
        static float _leanAnchorFor = float.NaN;

        /// <summary>
        /// Step 17 J: plays only the finale's credits roll where the island is (arg = an optional picture path for the
        /// standee preview, e.g. Assets/_Project/Art/Credits/character.png; the real one comes from Mugesh.png).
        /// </summary>
        public static void Credits()
        {
            var f = Object.FindFirstObjectByType<FinaleDirector>();
            if (f == null) return;
            if (!string.IsNullOrEmpty(Arg)) f.standee = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(Arg);
            var m = typeof(FinaleDirector).GetMethod("Credits", BindingFlags.Instance | BindingFlags.NonPublic);
            f.StartCoroutine((System.Collections.IEnumerator)m.Invoke(f, null));
            Debug.Log($"[Step17Debug] Credits roll (standee {(f.standee != null ? f.standee.name : "none")}).");
        }

        /// <summary>Opens the game menu (its private Open, as the menu stone does).</summary>
        public static void Menu()
        {
            var m = Object.FindFirstObjectByType<GameMenu>();
            if (m == null) return;
            typeof(GameMenu).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m, null);
            Debug.Log("[Step17Debug] Game menu opened.");
        }

        /// <summary>Logs the island grab locks and whether the rim can be grabbed.</summary>
        public static void Locks()
        {
            var h = Object.FindFirstObjectByType<IslandHandle>();
            if (h == null) return;
            Debug.Log($"[Step17Debug] Grab locked {h.GrabLocked} ({h.GrabLockReasons}); rim enabled {(h.rimInteractable != null && h.rimInteractable.enabled)}.");
        }

        /// <summary>Says a trigger (arg = id) the normal way (not forced): shows whether the battle gate lets it through.</summary>
        public static void Say()
        {
            Dialogue.Say(Arg);
            Debug.Log($"[Step17Debug] Said '{Arg}'.");
        }
    }
}
