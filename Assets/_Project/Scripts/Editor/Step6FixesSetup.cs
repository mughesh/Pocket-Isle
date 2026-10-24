using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 6 fixes: Cinderwood bright by default (palettes), the dialogue script re-imported (targets, new catch lines),
    /// the bigger cradle pod higher up, and the realm drift on the island (embers when restored, falling leaves when not).
    /// Re-running replaces the same objects.
    /// </summary>
    static class Step6FixesSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string MaterialDir = "Assets/_Project/Materials/Props";

        [MenuItem("Tools/Pocket Isle/Set Up MR_Main (6 fixes)", priority = 42)]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Step6Fixes] Stop Play mode first.");
                return;
            }
            PropKit.ApplyRealmLook();
            DialogueTools.Import();

            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var rack = Object.FindFirstObjectByType<PodRack>();
            if (island == null || rack == null)
            {
                Debug.LogError("[Step6Fixes] MR_Main needs the island and the Pod Rack. Run Set Up MR_Main (6 capture) first.");
                return;
            }

            Undo.RecordObject(rack, "6 fixes rack");
            rack.cradleOffset = new Vector3(0.075f, 0.1f, 0.035f);
            rack.cradleScale = 2f;
            rack.partyGrabSize = 0.05f;
            rack.cradleGrabSize = 0.08f;
            EditorUtility.SetDirty(rack);

            // Element flowing: lively embers over trees, tall grass and fire props (about 120 when fully restored).
            var embers = Drift(island, "Embers", RealmDrift.Kind.Embers);
            var emberMat = DriftMaterial("Embers", 0f, Hex(1f, 0.62f, 0.22f), Hex(1f, 0.9f, 0.55f), 0.004f,
                new Vector2(0.15f, 0.25f), new Vector2(3f, 5f), 1f, UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.One);
            embers.GetComponent<MeshRenderer>().sharedMaterial = emberMat;

            // Element still: a faint few leaves falling from the trees only (about 15 on screen).
            var leaves = Drift(island, "Falling Leaves", RealmDrift.Kind.Leaves);
            leaves.treeWeight = 0.3f;
            leaves.tallGrassWeight = leaves.fireWeight = leaves.groundWeight = leaves.waterWeight = 0f;
            var leafMat = DriftMaterial("FallingLeaves", 1f, Hex(0.77f, 0.28f, 0.16f), Hex(0.85f, 0.56f, 0.2f), 0.005f,
                new Vector2(0.06f, 0.09f), new Vector2(6f, 9f), 0.75f, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            leaves.GetComponent<MeshRenderer>().sharedMaterial = leafMat;

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log("[Step6Fixes] MR_Main: cradle 2x and higher (grab 8 cm, party 5 cm), Embers + Falling Leaves under the island, palettes and script updated.");
        }

        static RealmDrift Drift(IslandBuilder island, string name, RealmDrift.Kind kind)
        {
            var old = island.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var drift = go.AddComponent<RealmDrift>();
            drift.island = island;
            drift.kind = kind;
            return drift;
        }

        static Color Hex(float r, float g, float b) => new Color(r, g, b, 1f);

        static Material DriftMaterial(string name, float mode, Color color, Color core, float size, Vector2 travel, Vector2 period,
            float opacity, UnityEngine.Rendering.BlendMode src, UnityEngine.Rendering.BlendMode dst)
        {
            var shader = Shader.Find("Diorama/Realm Drift");
            if (shader == null) Debug.LogError("[Step6Fixes] Shader Diorama/Realm Drift not found.");
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            m.SetFloat("_Mode", mode);
            m.SetColor("_Color", color);
            m.SetColor("_Core", core);
            m.SetFloat("_Size", size);
            m.SetVector("_Travel", travel);
            m.SetVector("_Period", period);
            m.SetFloat("_Opacity", opacity);
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)dst);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }
    }
}
