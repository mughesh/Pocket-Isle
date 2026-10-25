using System.Collections;
using System.Reflection;
using System.Text;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 8e checks through the Claude bridge (exec:Diorama.EditorTools.Step8eDebug.Method).</summary>
    static class Step8eDebug
    {
        /// <summary>Which Hand each of our systems has cached per side (full hierarchy path), and every Hand under the ghosts.</summary>
        public static void ResolvedHands()
        {
            var sb = new StringBuilder("[Step8eDebug] Resolved hands:");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Namespace != "Diorama") continue;
                foreach (var f in mb.GetType().GetFields(flags))
                {
                    var v = f.GetValue(mb);
                    if (v is Hand h) sb.Append($"\n  {mb.GetType().Name}.{f.Name}: {Path(h)}");
                    else if (v is IEnumerable list && !(v is string))
                    {
                        int i = 0;
                        foreach (var item in list)
                        {
                            if (item is Hand ih) sb.Append($"\n  {mb.GetType().Name}.{f.Name}[{i}]: {Path(ih)}");
                            else if (item != null && !(item is Object))
                                foreach (var sf in item.GetType().GetFields(flags))
                                    if (sf.FieldType == typeof(Hand)) sb.Append($"\n  {mb.GetType().Name}.{f.Name}[{i}].{sf.Name}: {Path(sf.GetValue(item) as Hand)}");
                            i++;
                        }
                    }
                    else if (v != null && !(v is Object) && f.FieldType.IsClass && f.FieldType.IsSerializable)
                        foreach (var sf in f.FieldType.GetFields(flags))
                            if (sf.FieldType == typeof(Hand[]) && sf.GetValue(v) is Hand[] arr)
                                for (int k = 0; k < arr.Length; k++) sb.Append($"\n  {mb.GetType().Name}.{f.Name}.{sf.Name}[{k}]: {Path(arr[k])}");
                }
            }
            foreach (var side in new[] { Handedness.Left, Handedness.Right })
                sb.Append($"\n  HandRegistry.Get({side}): {Path(HandRegistry.Get(side))}");
            var ghosts = Object.FindFirstObjectByType<GhostHands>();
            if (ghosts != null)
                foreach (var c in ghosts.GetComponentsInChildren<Component>(true))
                    if (c is IHand || c is Hand) sb.Append($"\n  GHOST HAS {c.GetType().Name} at {Path(c)}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Starter pod layout without a new game: runs the opening's own layout on three placeholder pods (none, gaze on
        /// each) and logs whether every pod stays inside the island's footprint, and the gaps.
        /// </summary>
        public static void PodLayout()
        {
            var o = Object.FindFirstObjectByType<Opening>();
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (o == null || island == null || o.pods == null || o.pods.podPrefab == null) return;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
            var list = (System.Collections.Generic.List<PodView>)typeof(Opening).GetField("_starterPods", f).GetValue(o);
            if (list.Count > 0)
            {
                Debug.Log("[Step8eDebug] Starter pods are up: use GazeMiddlePod instead.");
                return;
            }
            var layout = typeof(Opening).GetMethod("LayoutPods", f);
            var local = typeof(Opening).GetMethod("PodLocal", f);
            float scale = (float)typeof(Opening).GetProperty("FloatScale", f).GetValue(o);
            var half = island.FootprintHalfSize;
            float r0 = o.pods.podPrefab.radius * scale;
            var sb = new StringBuilder($"[Step8eDebug] Starter pods: footprint half {half.x:0.000} x {half.y:0.000}, size x{scale:0.00} (max {o.podScale}), " +
                                       $"radius {r0 * 100f:0.0} cm local, focus x{o.podFocusGrow}:");
            for (int i = 0; i < 3; i++) list.Add(null);
            try
            {
                foreach (int focus in new[] { -1, 0, 1, 2 })
                {
                    layout.Invoke(o, new object[] { focus });
                    sb.Append($"\n  gaze {(focus < 0 ? "none" : "pod " + focus)}:");
                    bool inside = true;
                    float minGap = float.MaxValue;
                    for (int i = 0; i < 3; i++)
                    {
                        var p = (Vector3)local.Invoke(o, new object[] { i });
                        float r = r0 * (i == focus ? o.podFocusGrow : 1f);
                        inside &= Mathf.Abs(p.x) + r <= half.x && Mathf.Abs(p.z) + r <= half.y;
                        sb.Append($" ({p.x:0.000}, {p.z:0.000})");
                        if (i > 0)
                        {
                            var q = (Vector3)local.Invoke(o, new object[] { i - 1 });
                            float rq = r0 * (i - 1 == focus ? o.podFocusGrow : 1f);
                            minGap = Mathf.Min(minGap, (new Vector2(p.x - q.x, p.z - q.z)).magnitude - r - rq);
                        }
                    }
                    sb.Append($" inside {inside}, smallest gap {minGap * 100f:0.0} cm = {minGap / (2f * r0):0.00} diameters");
                }
            }
            finally
            {
                list.Clear();
            }
            Debug.Log(sb.ToString());
        }

        static string Path(Component c)
        {
            if (c == null) return "null";
            var path = c.name;
            for (var t = c.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return $"{path} ({c.GetType().Name}{(c is Hand h && Application.isPlaying ? ", valid " + h.IsTrackedDataValid : "")})";
        }
    }
}
