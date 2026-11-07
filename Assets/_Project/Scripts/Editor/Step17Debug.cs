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
