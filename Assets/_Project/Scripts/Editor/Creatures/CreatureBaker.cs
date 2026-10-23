using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Bakes creature recipes: SDF → marching cubes → simplify → SDF normals → mesh assets, then a prefab with a
    /// <see cref="CreatureView"/> that paints every piece through property blocks (all creatures share 3 materials).
    /// Deterministic; re-baking overwrites the same assets, so prefab and scene references survive.
    /// Body meshes: Meshes/Generated/&lt;Recipe&gt;/. Kit parts: Meshes/Generated/Parts/ (shared). Prefabs: Prefabs/Creatures/.
    /// </summary>
    public static class CreatureBaker
    {
        const string MeshRoot = "Assets/_Project/Meshes/Generated";
        const string PrefabDir = "Assets/_Project/Prefabs/Creatures";
        const string MaterialDir = "Assets/_Project/Materials/Creatures";

        public struct Report
        {
            public string name;
            public int bodyTriangles, bodyRawTriangles, partTriangles, faceTriangles, drawCalls, openEdges;
            public float seconds;
            public int Total => bodyTriangles + partTriangles + faceTriangles;
            public override string ToString() =>
                $"{name}: body {bodyTriangles} tris (from {bodyRawTriangles}), parts {partTriangles}, face {faceTriangles}, " +
                $"total {Total}, {drawCalls} draw calls{(openEdges > 0 ? $", WARNING {openEdges} open edges" : "")}, {seconds:0.0} s";
        }

        [MenuItem("Tools/Pocket Isle/Creatures/Bake Selected", priority = 1)]
        static void BakeSelectedMenu()
        {
            var recipes = new List<CreatureRecipe>();
            foreach (var o in Selection.objects)
            {
                if (o is CreatureRecipe r) recipes.Add(r);
                else if (o is CreatureSpecies s && s.recipe != null) recipes.Add(s.recipe);
            }
            if (recipes.Count == 0)
            {
                Debug.LogWarning("[CreatureBaker] Select creature recipes or species in the Project window first.");
                return;
            }
            foreach (var r in recipes.Distinct()) Bake(r);
        }

        [MenuItem("Tools/Pocket Isle/Creatures/Bake All", priority = 2)]
        public static List<Report> BakeAll()
        {
            var recipes = AssetDatabase.FindAssets("t:CreatureRecipe")
                .Select(g => AssetDatabase.LoadAssetAtPath<CreatureRecipe>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null)
                .OrderBy(r => r.baseRecipe != null) // base recipes first
                .ThenBy(r => r.name)
                .ToList();
            var reports = new List<Report>();
            var partCache = new Dictionary<CreaturePart, Mesh>();
            foreach (var r in recipes) reports.Add(Bake(r, partCache));
            var sb = new StringBuilder($"[CreatureBaker] Baked {reports.Count} creatures:");
            foreach (var rep in reports) sb.Append("\n  ").Append(rep);
            Debug.Log(sb.ToString());
            return reports;
        }

        public static Report Bake(CreatureRecipe recipe) => Bake(recipe, new Dictionary<CreaturePart, Mesh>());

        static Report Bake(CreatureRecipe recipe, Dictionary<CreaturePart, Mesh> partCache)
        {
            double t0 = EditorApplication.timeSinceStartup;
            var r = recipe.Resolve();
            var report = new Report { name = r.name };
            if (r.body == null || r.body.primitives.Count == 0)
            {
                Debug.LogError($"[CreatureBaker] {recipe.name} has no body shape.", recipe);
                return report;
            }

            var bodyEval = new SdfEvaluator(r.body);
            string dir = $"{MeshRoot}/{r.name}";
            DioramaSceneSetup.EnsureFolder(dir);
            var bodyMesh = BakeShape(bodyEval, r.resolution, r.triangleBudget, Vector3.zero, $"{r.name}_Body", out report.bodyRawTriangles, out report.openEdges);
            bodyMesh = SaveMesh(bodyMesh, $"{dir}/{r.name}_Body.asset");
            report.bodyTriangles = bodyMesh.triangles.Length / 3;

            Bounds b = bodyMesh.bounds;
            float unitHeight = Mathf.Max(0.01f, b.max.y - b.min.y);
            float scale = r.height / unitHeight;

            var mats = Materials();
            var root = new GameObject(r.name);
            var view = root.AddComponent<CreatureView>();
            var rig = new GameObject("Rig").transform;
            rig.SetParent(root.transform, false);
            rig.localScale = Vector3.one * scale;
            var model = new GameObject("Model").transform;
            model.SetParent(rig, false);
            model.localPosition = new Vector3(0f, -b.min.y, 0f); // feet at the rig origin

            view.rig = rig;
            view.rigPosition = Vector3.zero;
            view.rigScale = scale;
            view.height = r.height;
            view.centerHeight = unitHeight * 0.5f * scale;
            view.sideRestHeight = Mathf.Max(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x)) * scale;
            view.idle = r.idle;
            view.hopHeight = r.height * 0.3f;

            // body
            var bodyRenderer = AddRenderer(model, "Body", bodyMesh, mats.toon, r.outline > 0f ? mats.outline : null);
            view.pieces.Add(new CreatureView.Piece { renderer = bodyRenderer, style = BodyStyle(r, b.min.y) });
            report.drawCalls += bodyRenderer.sharedMaterials.Length;

            // parts
            var instances = new List<List<Transform>>();
            for (int i = 0; i < r.parts.Count; i++)
            {
                var placed = r.parts[i];
                var list = new List<Transform>();
                instances.Add(list);
                if (placed.part == null || placed.part.shape == null) continue;
                var mesh = PartMesh(placed.part, partCache);
                int tris = mesh.triangles.Length / 3;

                var parents = new List<(Transform t, bool inheritsMirror)>();
                if (placed.parent >= 0 && placed.parent < i && instances[placed.parent].Count > 0)
                {
                    var ps = instances[placed.parent];
                    for (int k = 0; k < ps.Count; k++) parents.Add((ps[k], k > 0));
                }
                else parents.Add((model, false));

                foreach (var (parent, inherits) in parents)
                {
                    int sides = placed.mirror && !inherits && parents.Count == 1 ? 2 : 1;
                    for (int side = 0; side < sides; side++)
                    {
                        bool mirrored = side == 1;
                        string suffix = sides == 2 ? (mirrored ? "_R" : "_L") : inherits ? "_R" : "";
                        var t = AddPart(parent, placed, mirrored, $"Part_{placed.part.name}{suffix}", mesh, mats, r, view);
                        list.Add(t);
                        report.partTriangles += tris;
                        report.drawCalls += t.GetComponent<Renderer>().sharedMaterials.Length;
                    }
                }
            }

            // face
            var shared = SharedMeshes();
            float front = b.max.z + 0.5f;
            for (int s = -1; s <= 1; s += 2)
            {
                if (r.face.eyeSize <= 0f) break;
                if (!bodyEval.Surface(new Vector3(s * r.face.eyeSpacing, r.face.eyeHeight, front), Vector3.back, out var p, out var n))
                {
                    Debug.LogWarning($"[CreatureBaker] {r.name}: eye ray missed the body.");
                    continue;
                }
                float size = r.face.eyeSize;
                var eye = new GameObject(s < 0 ? "Eye_R" : "Eye_L").transform;
                eye.SetParent(model, false);
                eye.localPosition = p - n * (size * 0.12f);
                eye.localRotation = Quaternion.FromToRotation(Vector3.forward, n);
                eye.localScale = new Vector3(size, size * (r.face.eyeStretch > 0f ? r.face.eyeStretch : 1.2f), size * 0.45f);
                eye.gameObject.AddComponent<MeshFilter>().sharedMesh = shared.sphere;
                var er = eye.gameObject.AddComponent<MeshRenderer>();
                er.sharedMaterial = mats.eye;
                Quiet(er);
                view.eyes.Add(new CreatureView.Eye { transform = eye, renderer = er, baseScale = eye.localScale, iris = r.palette.iris });
                report.faceTriangles += shared.sphere.triangles.Length / 3;
                report.drawCalls++;
            }
            int decalIndex = 0;
            foreach (var d in r.face.decals ?? new List<FaceDecal>())
            {
                for (int s = 0; s < (d.mirrorX ? 2 : 1); s++)
                {
                    float x = s == 0 ? d.at.x : -d.at.x;
                    if (!bodyEval.Surface(new Vector3(x, d.at.y, front), Vector3.back, out var p, out var n))
                    {
                        Debug.LogWarning($"[CreatureBaker] {r.name}: face decal {decalIndex} missed the body.");
                        continue;
                    }
                    var go = new GameObject($"Face_{d.kind}_{decalIndex}{(d.mirrorX ? (s == 0 ? "_L" : "_R") : "")}").transform;
                    go.SetParent(model, false);
                    var rot = Quaternion.FromToRotation(Vector3.forward, n);
                    Mesh mesh;
                    if (d.kind == DecalKind.Smile)
                    {
                        go.localPosition = p + n * 0.004f;
                        go.localRotation = rot * Quaternion.Euler(0f, 0f, 180f);
                        go.localScale = Vector3.one * d.size.x;
                        mesh = shared.smile;
                    }
                    else
                    {
                        go.localPosition = p - n * 0.004f;
                        go.localRotation = rot;
                        go.localScale = new Vector3(d.size.x, d.size.y, 0.02f);
                        mesh = shared.disc;
                    }
                    var rend = AddRenderer(go, mesh, mats.toon, null);
                    var style = new PieceStyle { baseColor = r.palette[d.color], top = r.palette[d.color], spec = 0f };
                    view.pieces.Add(new CreatureView.Piece { renderer = rend, style = style });
                    report.faceTriangles += mesh.triangles.Length / 3;
                    report.drawCalls++;
                }
                decalIndex++;
            }

            DioramaSceneSetup.EnsureFolder(PrefabDir);
            string prefabPath = $"{PrefabDir}/{r.name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            recipe.bakedPrefab = prefab;
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();

            report.seconds = (float)(EditorApplication.timeSinceStartup - t0);
            if (report.bodyTriangles > 6000) Debug.LogWarning($"[CreatureBaker] {r.name} body is {report.bodyTriangles} triangles (budget 6000).");
            Debug.Log($"[CreatureBaker] {report}");
            return report;
        }

        // ---- pieces ----

        static Transform AddPart(Transform parent, PlacedPart placed, bool mirrored, string name, Mesh mesh,
                                 (Material toon, Material outline, Material eye) mats, CreatureRecipe.Resolved r, CreatureView view)
        {
            var part = placed.part;
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            Vector3 scale = placed.scale == Vector3.zero ? Vector3.one : placed.scale;
            var rot = Quaternion.Euler(placed.rotation);
            Vector3 pos = placed.position;
            if (mirrored)
            {
                pos.x = -pos.x;
                rot = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
                scale.x = -scale.x;
            }
            t.localPosition = pos;
            t.localRotation = rot;
            t.localScale = scale;

            var st = part.style;
            var rend = AddRenderer(t, mesh, mats.toon, st.outline > 0f ? mats.outline : null);
            Color baseColor = r.palette[st.color];
            var style = new PieceStyle
            {
                baseColor = baseColor,
                top = st.gradient ? r.palette[st.gradientTo] : baseColor,
                gradient = st.gradient,
                belly = st.inner,
                bellyColor = r.palette[st.innerColor],
                bellyDirection = st.innerDirection.sqrMagnitude > 0f ? st.innerDirection : Vector3.forward,
                bellyAt = st.innerAt,
                bellyMaxY = 9f,
                tip = st.tip,
                tipColor = r.palette[st.tipColor],
                tipAxis = st.tipAxis.sqrMagnitude > 0f ? st.tipAxis.normalized : Vector3.up,
                tipAt = st.tipAt,
                spec = st.spec,
                glow = st.glow,
                jiggle = st.jiggle,
                outlineWidth = st.outline,
                outlineColor = OutlineColor(baseColor),
            };
            view.pieces.Add(new CreatureView.Piece { renderer = rend, style = style });

            if (part.motion != PartMotion.None)
                view.motions.Add(new CreatureView.Motion
                {
                    pivot = t,
                    kind = part.motion,
                    axis = part.motionAxis,
                    amount = part.motionAmount,
                    speed = part.motionSpeed,
                    phase = 0f,
                    mirrored = mirrored,
                    basePosition = t.localPosition,
                    baseRotation = t.localRotation,
                    baseScale = t.localScale,
                });
            return t;
        }

        static PieceStyle BodyStyle(CreatureRecipe.Resolved r, float feetY)
        {
            var z = r.zones;
            var pal = r.palette;
            var patches = new List<Vector4>();
            var colors = new List<Color>();
            foreach (var p in z.patches ?? new List<PatternPatch>())
            {
                patches.Add(new Vector4(p.center.x, p.center.y, p.center.z, p.radius));
                colors.Add(pal[p.color]);
                if (p.mirrorX)
                {
                    patches.Add(new Vector4(-p.center.x, p.center.y, p.center.z, p.radius));
                    colors.Add(pal[p.color]);
                }
            }
            if (patches.Count > PieceStyle.MaxPatches)
            {
                Debug.LogWarning($"[CreatureBaker] {r.name}: {patches.Count} patches, only {PieceStyle.MaxPatches} are painted.");
                patches.RemoveRange(PieceStyle.MaxPatches, patches.Count - PieceStyle.MaxPatches);
                colors.RemoveRange(PieceStyle.MaxPatches, colors.Count - PieceStyle.MaxPatches);
            }
            return new PieceStyle
            {
                baseColor = pal.baseColor,
                top = pal.baseColor,
                belly = z.belly,
                bellyColor = pal.belly,
                bellyDirection = z.bellyDirection.sqrMagnitude > 0f ? z.bellyDirection : Vector3.forward,
                bellyAt = z.bellyAt,
                bellyMaxY = z.bellyMaxY,
                bellyDrips = z.bellyDrips,
                bellyDripLength = z.bellyDripLength,
                socks = z.socks,
                socksColor = pal.socks,
                socksY = z.socksY,
                tip = z.tips,
                tipColor = pal.accent,
                tipAxis = z.tipAxis.sqrMagnitude > 0f ? z.tipAxis.normalized : Vector3.up,
                tipAt = z.tipAt,
                spots = z.spots,
                spotColor = pal.accent,
                spotScale = z.spotScale,
                cracks = z.cracks,
                crackColor = pal.glow,
                crackScale = z.crackScale,
                crackWidth = z.crackWidth,
                crackPulse = z.crackPulse,
                patches = patches.ToArray(),
                patchColors = colors.ToArray(),
                spec = r.spec,
                breathe = r.idle.breathe,
                breatheSpeed = r.idle.breatheSpeed,
                jiggle = r.idle.jiggle,
                outlineWidth = r.outline,
                outlineColor = OutlineColor(pal.baseColor),
                pivotY = feetY,
            };
        }

        // Prototype: outline = base colour * 0.28 in linear space.
        static Color OutlineColor(Color c) => (c.linear * 0.28f).gamma;

        static MeshRenderer AddRenderer(Transform parent, string name, Mesh mesh, Material toon, Material outline)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return AddRenderer(t, mesh, toon, outline);
        }

        static MeshRenderer AddRenderer(Transform t, Mesh mesh, Material toon, Material outline)
        {
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = t.gameObject.AddComponent<MeshRenderer>();
            // A second material on a single-submesh mesh draws the same mesh again: the inverted-hull outline.
            rend.sharedMaterials = outline != null ? new[] { toon, outline } : new[] { toon };
            Quiet(rend);
            return rend;
        }

        static void Quiet(Renderer rend)
        {
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = LightProbeUsage.Off;
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            rend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        // ---- meshes ----

        static Mesh PartMesh(CreaturePart part, Dictionary<CreaturePart, Mesh> cache)
        {
            if (cache.TryGetValue(part, out var cached) && cached != null) return cached;
            string dir = $"{MeshRoot}/Parts";
            DioramaSceneSetup.EnsureFolder(dir);
            var mesh = BakeShape(new SdfEvaluator(part.shape), part.resolution, part.triangleBudget, part.pivot, $"Part_{part.name}", out _, out int open);
            if (open > 0) Debug.LogWarning($"[CreatureBaker] Part {part.name}: {open} open edges.");
            mesh = SaveMesh(mesh, $"{dir}/Part_{part.name}.asset");
            cache[part] = mesh;
            return mesh;
        }

        /// <summary>SDF → welded marching cubes mesh → simplified to the budget → normals from the SDF gradient.</summary>
        public static Mesh BakeShape(SdfEvaluator sdf, int resolution, int budget, Vector3 pivot, string name, out int rawTriangles, out int openEdges)
        {
            Bounds bounds = sdf.Bounds;
            float cell = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) / Mathf.Max(8, resolution);
            var mc = MarchingCubes.Build(sdf.Evaluate, bounds, cell);
            rawTriangles = mc.triangles.Count / 3;
            int rawOpen = MarchingCubes.OpenEdges(mc.triangles);
            if (rawOpen > 0) Debug.Log($"[CreatureBaker] {name}: marching cubes left {rawOpen} non-manifold edges (thin spots touching).");
            MeshSimplifier.Simplify(mc.vertices, mc.triangles, Mathf.Max(64, budget), out var verts, out var tris);
            openEdges = MarchingCubes.OpenEdges(tris);

            var normals = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++) normals[i] = sdf.Normal(verts[i], cell * 0.5f);
            for (int i = 0; i < verts.Count; i++) verts[i] -= pivot;

            var mesh = new Mesh { name = name };
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

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

        static (Mesh sphere, Mesh disc, Mesh smile) SharedMeshes()
        {
            string dir = $"{MeshRoot}/Shared";
            DioramaSceneSetup.EnsureFolder(dir);
            var sphere = AssetDatabase.LoadAssetAtPath<Mesh>($"{dir}/EyeSphere.asset");
            if (sphere == null)
            {
                sphere = UvSphere(24, 16, "EyeSphere");
                AssetDatabase.CreateAsset(sphere, $"{dir}/EyeSphere.asset");
            }
            var disc = AssetDatabase.LoadAssetAtPath<Mesh>($"{dir}/DecalSphere.asset");
            if (disc == null)
            {
                disc = UvSphere(12, 8, "DecalSphere"); // flattened into face marks; low detail is enough
                AssetDatabase.CreateAsset(disc, $"{dir}/DecalSphere.asset");
            }
            var smile = AssetDatabase.LoadAssetAtPath<Mesh>($"{dir}/Smile.asset");
            if (smile == null)
            {
                smile = HalfTorus(1f, 0.2f, 6, 16);
                AssetDatabase.CreateAsset(smile, $"{dir}/Smile.asset");
            }
            return (sphere, disc, smile);
        }

        static Mesh UvSphere(int lon, int lat, string name)
        {
            var v = new List<Vector3>();
            var tris = new List<int>();
            for (int y = 0; y <= lat; y++)
            {
                float th = Mathf.PI * y / lat;
                for (int x = 0; x <= lon; x++)
                {
                    float ph = 2f * Mathf.PI * x / lon;
                    v.Add(new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a = y * (lon + 1) + x, b = a + lon + 1;
                    Tri(tris, v, a, b, a + 1);
                    Tri(tris, v, a + 1, b, b + 1);
                }
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetNormals(v);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Half ring in the XY plane (upper half), like three.js TorusGeometry(radius, tube, 6, 16, PI).</summary>
        static Mesh HalfTorus(float radius, float tube, int tubeSegments, int arcSegments)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i <= arcSegments; i++)
            {
                float a = Mathf.PI * i / arcSegments;
                var center = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                for (int j = 0; j <= tubeSegments; j++)
                {
                    float b = 2f * Mathf.PI * j / tubeSegments;
                    var dir = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a) * Mathf.Cos(b), Mathf.Sin(b));
                    v.Add(center + dir * tube);
                    n.Add(dir);
                }
            }
            for (int i = 0; i < arcSegments; i++)
                for (int j = 0; j < tubeSegments; j++)
                {
                    int a = i * (tubeSegments + 1) + j, b = a + tubeSegments + 1;
                    TriOut(tris, v, n, a, b, a + 1);
                    TriOut(tris, v, n, a + 1, b, b + 1);
                }
            var m = new Mesh { name = "Smile" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        // Front faces point away from the sphere centre (Unity: clockwise = front).
        static void Tri(List<int> tris, List<Vector3> v, int a, int b, int c)
        {
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, v[a] + v[b] + v[c]) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static void TriOut(List<int> tris, List<Vector3> v, List<Vector3> normals, int a, int b, int c)
        {
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (n.sqrMagnitude < 1e-12f) return;
            if (Vector3.Dot(n, normals[a] + normals[b] + normals[c]) < 0f) (b, c) = (c, b);
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        // ---- materials ----

        public static (Material toon, Material outline, Material eye) Materials()
        {
            DioramaSceneSetup.EnsureFolder(MaterialDir);
            return (Mat("Diorama/Toon", "Creature"), Mat("Diorama/Toon Outline", "Creature_Outline"), Mat("Diorama/Creature Eye", "Creature_Eye"));
        }

        static Material Mat(string shaderName, string name)
        {
            var shader = Shader.Find(shaderName);
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }
    }
}
