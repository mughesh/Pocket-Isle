using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Bakes prop recipes like creatures: each piece SDF → marching cubes → simplify → SDF normals (CreatureBaker),
    /// placed in prop units and merged into one mesh per prop. Per vertex: uv0 = (palette slot, motion, height within
    /// the piece 0..1, motion amount), uv1 = piece centre, read by the Diorama/Prop shader. All props share one
    /// material (Materials/Props/Prop.mat); palettes go in property blocks. Re-baking overwrites the same mesh asset.
    /// </summary>
    public static class PropBaker
    {
        const string MeshDir = "Assets/_Project/Meshes/Generated/Props";
        const string MaterialDir = "Assets/_Project/Materials/Props";
        const string RecipeDir = "Assets/_Project/Data/Props";

        public struct Report
        {
            public string name;
            public int triangles, budget, pieces, openEdges;
            public float seconds;
            public override string ToString() =>
                $"{name}: {triangles} tris (budget {budget}), {pieces} pieces{(openEdges > 0 ? $", {openEdges} open edges" : "")}, {seconds:0.0} s";
        }

        [MenuItem("Tools/Pocket Isle/Props/Bake Selected", priority = 1)]
        static void BakeSelectedMenu()
        {
            var recipes = Selection.objects.OfType<PropRecipe>().ToList();
            if (recipes.Count == 0)
            {
                Debug.LogWarning("[PropBaker] Select prop recipes in the Project window first.");
                return;
            }
            foreach (var r in recipes) Bake(r);
        }

        [MenuItem("Tools/Pocket Isle/Props/Bake All", priority = 2)]
        public static List<Report> BakeAll()
        {
            var reports = AllRecipes().Select(r => Bake(r, false)).ToList();
            AssetDatabase.SaveAssets();
            IslandBuilder.RebuildAll();
            var sb = new StringBuilder($"[PropBaker] Baked {reports.Count} props:");
            foreach (var rep in reports) sb.Append("\n  ").Append(rep);
            Debug.Log(sb.ToString());
            return reports;
        }

        public static List<PropRecipe> AllRecipes() =>
            AssetDatabase.FindAssets("t:PropRecipe")
                .Select(g => AssetDatabase.LoadAssetAtPath<PropRecipe>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null)
                .OrderBy(r => r.name)
                .ToList();

        public static Report Bake(PropRecipe recipe) => Bake(recipe, true);

        static Report Bake(PropRecipe recipe, bool save)
        {
            double t0 = EditorApplication.timeSinceStartup;
            var report = new Report { name = recipe.name, budget = recipe.triangleBudget };

            // Share the triangle budget by piece size (longest side squared ~ surface).
            var pieces = new List<(PropPiece piece, SdfEvaluator eval, Vector3 pivot, float weight)>();
            foreach (var piece in recipe.pieces ?? new List<PropPiece>())
            {
                var shape = piece.shape != null ? piece.shape : piece.part != null ? piece.part.shape : null;
                if (shape == null || shape.primitives.Count == 0) continue;
                var eval = new SdfEvaluator(shape);
                float s = piece.scale > 0f ? piece.scale : 1f;
                var size = eval.Bounds.size * s;
                float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                var pivot = piece.shape == null && piece.part != null ? piece.part.pivot : Vector3.zero;
                float share = piece.budgetWeight > 0f ? piece.budgetWeight : longest * longest;
                pieces.Add((piece, eval, pivot, share * (piece.mirrorX ? 2f : 1f)));
            }
            if (pieces.Count == 0)
            {
                Debug.LogError($"[PropBaker] {recipe.name} has no pieces with shapes.", recipe);
                return report;
            }
            float totalWeight = pieces.Sum(p => p.weight);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv0 = new List<Vector4>();
            var uv1 = new List<Vector3>();
            var tris = new List<int>();
            int index = 0;
            foreach (var (piece, eval, pivot, weight) in pieces)
            {
                int copies = piece.mirrorX ? 2 : 1;
                int budget = Mathf.Max(64, Mathf.RoundToInt(recipe.triangleBudget * weight / totalWeight / copies));
                var mesh = CreatureBaker.BakeShape(eval, recipe.resolution, budget, pivot, $"{recipe.name}_{index}", out _, out int open);
                report.openEdges += open;
                var pv = mesh.vertices;
                var pn = mesh.normals;
                var pt = mesh.triangles;
                Object.DestroyImmediate(mesh);

                float s = piece.scale > 0f ? piece.scale : 1f;
                var rot = Quaternion.Euler(piece.rotation);
                for (int i = 0; i < pv.Length; i++)
                {
                    pv[i] = piece.position + rot * (pv[i] * s);
                    pn[i] = rot * pn[i];
                }
                float minY = float.MaxValue, maxY = float.MinValue;
                var b = new Bounds(pv[0], Vector3.zero);
                foreach (var v in pv)
                {
                    minY = Mathf.Min(minY, v.y);
                    maxY = Mathf.Max(maxY, v.y);
                    b.Encapsulate(v);
                }
                float amount = piece.motionAmount > 0f ? piece.motionAmount : DefaultAmount(piece.motion);
                for (int copy = 0; copy < copies; copy++)
                {
                    bool mirrored = copy == 1;
                    int start = verts.Count;
                    var center = b.center;
                    if (mirrored) center.x = -center.x;
                    for (int i = 0; i < pv.Length; i++)
                    {
                        var v = pv[i];
                        var n = pn[i];
                        if (mirrored)
                        {
                            v.x = -v.x;
                            n.x = -n.x;
                        }
                        verts.Add(v);
                        normals.Add(n);
                        float h = maxY > minY ? (pv[i].y - minY) / (maxY - minY) : 0f;
                        uv0.Add(new Vector4((int)piece.slot, (int)piece.motion, h, amount));
                        uv1.Add(center);
                    }
                    for (int i = 0; i < pt.Length; i += 3)
                    {
                        // A mirror flips the winding.
                        tris.Add(start + pt[i]);
                        tris.Add(start + (mirrored ? pt[i + 2] : pt[i + 1]));
                        tris.Add(start + (mirrored ? pt[i + 1] : pt[i + 2]));
                    }
                }
                index++;
            }
            report.pieces = index;

            var baked = new Mesh { name = $"Prop_{recipe.name}" };
            baked.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            baked.SetVertices(verts);
            baked.SetNormals(normals);
            baked.SetUVs(0, uv0);
            baked.SetUVs(1, uv1);
            baked.SetTriangles(tris, 0);
            baked.RecalculateBounds();
            report.triangles = tris.Count / 3;

            DioramaSceneSetup.EnsureFolder(MeshDir);
            recipe.bakedMesh = SaveMesh(baked, $"{MeshDir}/Prop_{recipe.name}.asset");
            EditorUtility.SetDirty(recipe);
            if (save)
            {
                AssetDatabase.SaveAssets();
                IslandBuilder.RebuildAll();
            }
            report.seconds = (float)(EditorApplication.timeSinceStartup - t0);
            if (report.triangles > recipe.triangleBudget * 1.15f)
                Debug.LogWarning($"[PropBaker] {recipe.name} is {report.triangles} triangles (budget {recipe.triangleBudget}).", recipe);
            if (save) Debug.Log($"[PropBaker] {report}");
            return report;
        }

        static float DefaultAmount(PropMotion motion) => motion switch
        {
            PropMotion.Sway => 0.05f,
            PropMotion.Flicker => 0.03f,
            PropMotion.Steam => 0.06f,
            _ => 0f,
        };

        /// <summary>Writes into the existing asset when there is one, so references keep working.</summary>
        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>The shared Diorama/Prop material (created on first use, instancing on).</summary>
        public static Material PropMaterial()
        {
            DioramaSceneSetup.EnsureFolder(MaterialDir);
            string path = $"{MaterialDir}/Prop.mat";
            var shader = Shader.Find("Diorama/Prop");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = "Prop", enableInstancing = true };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            if (!m.enableInstancing)
            {
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        internal static string RecipeFolder => RecipeDir;
    }
}
