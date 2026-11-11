using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 12 (Mugesh: "these main assets look too broken... it could have some details to it. Look at that statue. It
    /// looks like a deformed dog"): rewrites and bakes only the re-sculpted landmark props: the hub statue (and its two
    /// wake stages), the hearth shrine (lit and cold) and Galewick's wind tower (shut and open). Re-run safe.
    /// Look at them with <see cref="PropAudit.Some"/>, arg "front;pitch=20;Statue,HearthShrine,WindTower".
    /// </summary>
    public static class Nov12Props
    {
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Nov12Props] Stop Play first.");
                return;
            }
            var arg = System.IO.File.Exists("Temp/ClaudeBridge/arg.txt") ? System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt") : "";
            var list = new List<PropRecipe>();
            if (arg.Contains("statue") || arg.Contains("all")) list.AddRange(PropKit.WriteStatues());
            if (arg.Contains("shrine") || arg.Contains("all"))
            {
                list.Add(PropKit.WriteHearthShrine());
                AssetDatabase.SaveAssets();
                list.Add(PropKit.WriteHearthShrineCold());
            }
            if (arg.Contains("tower") || arg.Contains("all"))
                foreach (var r in PropKit.WriteWindTowers())
                    if (r.name != "WindTowerDoor") list.Add(r);
            AssetDatabase.SaveAssets();
            foreach (var r in list)
            {
                if (r == null) continue;
                EditorUtility.SetDirty(r);
                PropBaker.Bake(r);
                Debug.Log($"[Nov12Props] {r.name}: {(r.bakedMesh != null ? r.bakedMesh.triangles.Length / 3 : 0)} tris at {r.resolution} (budget {r.triangleBudget}).");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Bridge (arg: a recipe name): each piece's raw and simplified triangle count against its share of the budget.</summary>
        public static void Pieces()
        {
            var r = PropKit.Load(System.IO.File.ReadAllText("Temp/ClaudeBridge/arg.txt").Trim());
            if (r == null) return;
            float total = 0f;
            float Weight(PropPiece p)
            {
                if (p.budgetWeight > 0f) return p.budgetWeight;
                var size = new SdfEvaluator(p.shape).Bounds.size * (p.scale > 0f ? p.scale : 1f);
                float l = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                return l * l;
            }
            foreach (var p in r.pieces) if (p.shape != null) total += Weight(p) * (p.mirrorX ? 2f : 1f);
            var sb = new System.Text.StringBuilder();
            foreach (var p in r.pieces)
            {
                if (p.shape == null) continue;
                var eval = new SdfEvaluator(p.shape);
                int budget = Mathf.Max(64, Mathf.RoundToInt(r.triangleBudget * Weight(p) / total));
                var mesh = CreatureBaker.BakeShape(eval, r.resolution, budget, Vector3.zero, "probe", out int raw, out int open);
                sb.AppendLine($"{p.shape.name}: raw {raw}, budget {budget}, got {mesh.triangles.Length / 3}, open {open}, bounds {eval.Bounds.size}");
                Object.DestroyImmediate(mesh);
            }
            System.IO.File.WriteAllText("Logs/PropAudit/pieces.txt", sb.ToString());
            Debug.Log("[Nov12Props] Pieces of " + r.name + " in Logs/PropAudit/pieces.txt");
        }
    }
}
