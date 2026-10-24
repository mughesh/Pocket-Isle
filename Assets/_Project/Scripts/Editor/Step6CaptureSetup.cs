using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 6: bakes the capture pod (glass bubble, SDF brass band and socket ring), makes the Pod prefab (ISDK grab),
    /// wires the rim pods and the catch cradle into MR_Main, widens the rune row for the Catch rune, and applies the
    /// realm look (Cinderwood starts muted; restored is brighter). Re-running replaces the same assets and objects.
    /// </summary>
    static class Step6CaptureSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MeshDir = "Assets/_Project/Meshes/Generated/Pod";
        const string MaterialDir = "Assets/_Project/Materials/Battle";
        const string PrefabPath = "Assets/_Project/Prefabs/Battle/Pod.prefab";
        const string HudPath = "Assets/_Project/Materials/Battle/Hud.mat";
        const float Radius = 0.015f; // glass radius, metres at island scale 1 (a 3 cm pod)

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (6 capture)", priority = 41)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step6] Stop Play mode first.");
                return;
            }
            var prefab = BuildPod(out var socketMesh, out var brass);
            PropKit.ApplyRealmLook();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var director = Object.FindFirstObjectByType<EncounterDirector>();
            var party = Object.FindFirstObjectByType<Party>();
            var handle = Object.FindFirstObjectByType<IslandHandle>();
            var fx = Object.FindFirstObjectByType<BattleFx>();
            var runes = Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);
            var hud = AssetDatabase.LoadAssetAtPath<Material>(HudPath);
            if (island == null || director == null || party == null || runes == null || hud == null)
            {
                Debug.LogError("[Step6] MR_Main needs the island, the battle (EncounterDirector, Party, runes) and Materials/Battle/Hud.mat. Run the Step 5 setup first.");
                return;
            }

            var old = GameObject.Find("Pod Rack");
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject("Pod Rack");
            go.transform.SetParent(director.transform, false);
            var rack = go.AddComponent<PodRack>();
            rack.island = island;
            rack.party = party;
            rack.podPrefab = prefab;
            rack.handle = handle;
            rack.fx = fx;
            rack.hudMaterial = hud;
            rack.socketMesh = socketMesh;
            rack.socketMaterial = brass;

            Undo.RecordObject(director, "6 director");
            director.pods = rack;
            Undo.RecordObject(runes, "6 runes");
            runes.spacing = 0.088f;
            runes.runeSize = new Vector2(0.08f, 0.04f);
            // When the row would cover a creature it moves up and toward the player (down would hide the rim pods).
            runes.avoidOffset = new Vector3(0f, 0.045f, 0.045f);
            var gaze = Object.FindFirstObjectByType<TileGaze>();
            if (gaze != null)
            {
                Undo.RecordObject(gaze, "6 gaze");
                gaze.pods = rack;
            }
            foreach (var c in new Component[] { director, runes, gaze, rack })
                if (c != null) EditorUtility.SetDirty(c);

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log($"[Step6] MR_Main: Pod Rack (6 rim sockets + catch cradle) under {director.name}, 5-rune row, " +
                      $"party {party.members.Count} creature(s){(handle == null ? ", no IslandHandle (no rim guard)" : "")}" +
                      $"{(fx == null ? ", no BattleFx (no pod sparkles)" : "")}{(gaze == null ? ", no TileGaze" : "")}.");
        }

        // ---- pod ----

        static PodView BuildPod(out Mesh socketMesh, out Material brass)
        {
            DioramaSceneSetup.EnsureFolder(MeshDir);
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Prefabs/Battle");

            // Shapes in pod units (glass radius 1), scaled to metres after baking: the baker's tolerances suit unit sizes.
            var band = Bake("Pod_Band", 56, 900,
                Torus(Vector3.zero, 1.01f, 0.16f, Vector3.zero, 0.04f),
                Sphere(new Vector3(0f, 0f, 1.1f), 0.22f, 0.08f),
                Ell(new Vector3(0f, 1.0f, 0f), new Vector3(0.24f, 0.1f, 0.24f), 0.02f));
            socketMesh = Bake("Pod_Socket", 56, 900,
                Torus(Vector3.zero, 1.12f, 0.2f, new Vector3(90f, 0f, 0f), 0.05f),
                Ell(new Vector3(0f, 0f, -0.12f), new Vector3(1.32f, 1.32f, 0.1f), 0.1f));
            var glassMesh = SaveMesh(UvSphere(Radius, 24, 16, "Pod_Glass"), $"{MeshDir}/Pod_Glass.asset");

            var glass = Material("PodGlass", "Diorama/Pod Glass");
            brass = Material("PodBrass", "Diorama/Toon");
            brass.SetColor("_BaseColor", new Color(0.79f, 0.6f, 0.26f));
            brass.SetFloat("_Spec", 0.9f);
            brass.SetFloat("_Breathe", 0f);
            brass.SetFloat("_Jiggle", 0f);
            EditorUtility.SetDirty(brass);

            var root = new GameObject("Pod");
            var view = root.AddComponent<PodView>();
            view.radius = Radius;
            view.glass = Part(root.transform, "Glass", glassMesh, glass);
            view.band = Part(root.transform, "Band", band, brass);
            view.miniAnchor = new GameObject("Mini").transform;
            view.miniAnchor.SetParent(root.transform, false);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var col = root.AddComponent<SphereCollider>();
            col.radius = Radius * 1.4f; // a little bigger than the glass: easier to pick up
            var grabbable = root.AddComponent<Grabbable>();
            var interactable = root.AddComponent<HandGrabInteractable>();
            interactable.InjectRigidbody(rb);
            interactable.InjectOptionalPointableElement(grabbable);
            interactable.InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.Pinch | Oculus.Interaction.Grab.GrabTypeFlags.Palm);
            interactable.HandAlignment = HandAlignType.None;
            var grab = root.AddComponent<PodGrab>();
            grab.interactable = interactable;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Step6] Pod prefab: {PrefabPath} (glass {glassMesh.triangles.Length / 3}, band {band.triangles.Length / 3}, socket {socketMesh.triangles.Length / 3} triangles).");
            return saved.GetComponent<PodView>();
        }

        static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        static Mesh Bake(string name, int resolution, int budget, params SdfPrimitive[] prims)
        {
            var mesh = CreatureBaker.BakeShape(new SdfEvaluator(new List<SdfPrimitive>(prims)), resolution, budget, Vector3.zero, name, out _, out int open);
            if (open > 0) Debug.LogWarning($"[Step6] {name}: {open} open edges.");
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] *= Radius;
            mesh.vertices = v;
            mesh.RecalculateBounds();
            return SaveMesh(mesh, $"{MeshDir}/{name}.asset");
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
            existing.indexFormat = mesh.indexFormat;
            existing.vertices = mesh.vertices;
            existing.normals = mesh.normals;
            existing.triangles = mesh.triangles;
            existing.RecalculateBounds();
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        static Mesh UvSphere(float radius, int segments, int rings, string name)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings, phi = v * Mathf.PI;
                for (int s = 0; s <= segments; s++)
                {
                    float theta = s / (float)segments * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    normals.Add(n);
                    verts.Add(n * radius);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Material Material(string name, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) Debug.LogError($"[Step6] Shader {shaderName} not found.");
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
