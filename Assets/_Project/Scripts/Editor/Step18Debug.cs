using System.IO;
using System.Reflection;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Bridge hooks for the Nov 7 headset notes (Play mode): close ghost shots, shard blast, finale layout.</summary>
    public static class Step18Debug
    {
        const string ArgFile = "Temp/ClaudeBridge/arg.txt";
        static string Arg => File.Exists(ArgFile) ? File.ReadAllText(ArgFile).Trim() : "";

        static IslandBuilder Island => Object.FindFirstObjectByType<IslandBuilder>();

        /// <summary>
        /// Close shots of the ghost (arg = "name"): from the seated head 40 cm out toward the ghost's key point
        /// (Logs/BattleShots/name.png) and from the side (name_side.png). Falls back to the island centre.
        /// </summary>
        public static void GhostClose()
        {
            var island = Island;
            var g = Object.FindFirstObjectByType<GhostHands>();
            if (island == null) return;
            string name = string.IsNullOrEmpty(Arg) ? "18_ghost" : Arg;
            var t = island.transform;
            Vector3 head = t.position + t.rotation * new Vector3(0f, 0.55f, 0.45f);
            Vector3 at = t.TransformPoint(new Vector3(0f, island.CapTopLocal, 0f));
            if (g != null)
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                    if (r.enabled && r is SkinnedMeshRenderer)
                    {
                        at = r.bounds.center;
                        break;
                    }
            Vector3 eye = at + (head - at).normalized * 0.4f;
            BattleDebug.Render($"Logs/BattleShots/{name}.png", eye, Quaternion.LookRotation(at - eye, Vector3.up));
            Vector3 side = at + (t.rotation * new Vector3(-0.35f, 0.12f, 0.05f));
            BattleDebug.Render($"Logs/BattleShots/{name}_side.png", side, Quaternion.LookRotation(at - side, Vector3.up));
            Debug.Log($"[Step18Debug] Ghost close shots {name} at {at}.");
        }

        /// <summary>The Keeper-shard blast at the island centre (arg = shard id, e.g. shard.wind).</summary>
        public static void Blast()
        {
            var island = Island;
            var fx = Object.FindFirstObjectByType<BattleFx>();
            if (island == null || fx == null) return;
            var t = island.transform;
            Vector3 at = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.15f, 0.1f));
            ShardBurst.Blast(fx, island, at, string.IsNullOrEmpty(Arg) ? "shard.wind" : Arg);
            Debug.Log($"[Step18Debug] Blast {Arg}.");
        }

        /// <summary>A seated shot of the whole island (arg = name), wider than Step16Debug.Shot.</summary>
        public static void Wide()
        {
            var island = Island;
            if (island == null) return;
            var t = island.transform;
            var eye = t.position + t.rotation * new Vector3(0f, 0.7f, 0.85f);
            var look = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.1f, 0f));
            BattleDebug.Render($"Logs/BattleShots/{(string.IsNullOrEmpty(Arg) ? "18_wide" : Arg)}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
        }

        /// <summary>Edit mode: wires BattleRunes.pods to the scene's PodRack and saves the scene.</summary>
        public static void WireRunes()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            var runes = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
            var pods = Object.FindFirstObjectByType<PodRack>(FindObjectsInactive.Include);
            if (runes == null || pods == null) return;
            UnityEditor.Undo.RecordObject(runes, "Wire rune pods");
            runes.pods = pods;
            UnityEditor.EditorUtility.SetDirty(runes);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(runes.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(runes.gameObject.scene);
            Debug.Log($"[Step18Debug] BattleRunes.pods = {pods.name}; scene saved.");
        }

        /// <summary>Calls a private parameterless method on the first object of a type (arg = "Type.Method").</summary>
        public static void Call()
        {
            var a = Arg.Split('.');
            if (a.Length != 2) return;
            var type = typeof(IslandBuilder).Assembly.GetType("Diorama." + a[0]);
            var o = type != null ? Object.FindFirstObjectByType(type) : null;
            var m = type?.GetMethod(a[1], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (o == null || m == null)
            {
                Debug.LogWarning($"[Step18Debug] No {Arg}.");
                return;
            }
            var r = m.Invoke(o, null);
            if (r is System.Collections.IEnumerator e) ((MonoBehaviour)o).StartCoroutine(e);
            Debug.Log($"[Step18Debug] Called {Arg}.");
        }
    }
}
