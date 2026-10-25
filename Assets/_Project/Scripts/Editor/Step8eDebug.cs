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

        static string Path(Component c)
        {
            if (c == null) return "null";
            var path = c.name;
            for (var t = c.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return $"{path} ({c.GetType().Name}{(c is Hand h && Application.isPlaying ? ", valid " + h.IsTrackedDataValid : "")})";
        }
    }
}
