using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Creates the step 1 content: island materials, the prototype map asset and the Diorama scene.
    /// Runs once by itself if the scene is missing; rerun from Tools > Diorama > Rebuild Diorama Scene.
    /// Materials and the map are updated in place, so references and hand tweaks to other fields survive.
    /// </summary>
    [InitializeOnLoad]
    public static class DioramaSceneSetup
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Diorama.unity";
        const string MaterialDir = Root + "/Materials/Island";
        const string MapPath = Root + "/Data/IslandMap_Prototype.asset";
        const string PreviewPath = "Temp/DioramaPreview.png";

        // Prototype camera and light, X mirrored (see IslandMap remarks).
        static readonly Vector3 CameraPosition = new Vector3(-0.05f, 0.62f, 0.98f);
        static readonly Vector3 CameraTarget = new Vector3(-0.04f, 0f, 0f);
        static readonly Vector3 DirectionToLight = new Vector3(-0.45f, 0.85f, 0.5f);

        static DioramaSceneSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (File.Exists(ScenePath)) return;
                Debug.Log("[Diorama] Diorama scene not found, creating it.");
                Build();
            };
        }

        [MenuItem("Tools/Diorama/Rebuild Diorama Scene")]
        public static void Build()
        {
            var toon = Shader.Find("Diorama/Toon");
            var outline = Shader.Find("Diorama/Toon Outline");
            var grassShader = Shader.Find("Diorama/Grass");
            if (toon == null || outline == null || grassShader == null)
            {
                Debug.LogError("[Diorama] Shaders not found. Check the Console for shader compile errors.");
                return;
            }

            foreach (var dir in new[] { "Scenes", "Materials", "Materials/Island", "Data" })
                EnsureFolder(Root + "/" + dir);

            // ---- materials ----
            var column = ToonMaterial(toon, "Island_Column", "#8a5a3b");
            var underside = ToonMaterial(toon, "Island_Underside", "#5c4636");
            var capGrass = ToonMaterial(toon, "Island_CapGrass", "#86bf5f", jitter: 0.07f);
            var capTall = ToonMaterial(toon, "Island_CapTallGrass", "#66a24b", jitter: 0.07f);
            var capPath = ToonMaterial(toon, "Island_CapPath", "#e6cc95", jitter: 0.05f);
            var capWater = ToonMaterial(toon, "Island_CapWater", "#4ea9cf", jitter: 0.04f);
            var trunk = Part(toon, outline, "Tree_Trunk", "#7a4f32", 0.0005f);
            var leavesLow = Part(toon, outline, "Tree_LeavesLow", "#4f8f45", 0.0009f);
            var leavesHigh = Part(toon, outline, "Tree_LeavesHigh", "#66a852", 0.0008f);
            var rock = Part(toon, outline, "Rock", "#a3a39a", 0.0008f);

            var grass = LoadOrCreateMaterial(grassShader, "Island_Grass");
            grass.SetColor("_ShortBase", Hex("#3f7a35"));
            grass.SetColor("_ShortTip", Hex("#b6e07a"));
            grass.SetColor("_TallBase", Hex("#2c6230"));
            grass.SetColor("_TallTip", Hex("#8ccb5c"));
            EditorUtility.SetDirty(grass);

            // ---- map ----
            var map = AssetDatabase.LoadAssetAtPath<IslandMap>(MapPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<IslandMap>();
                AssetDatabase.CreateAsset(map, MapPath);
            }
            AssetDatabase.SaveAssets();

            // ---- scene ----
            var existing = SceneManager.GetSceneByPath(ScenePath);
            bool reuseSingle = existing.isLoaded && SceneManager.sceneCount == 1;
            if (existing.isLoaded && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (existing.isLoaded && !reuseSingle) EditorSceneManager.CloseScene(existing, true);

            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, reuseSingle ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#d9ddd6");
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 20f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = CameraPosition;
            camGo.transform.LookAt(CameraTarget);

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.white;
            sun.intensity = 1f;
            sun.shadows = LightShadows.None;
            sunGo.transform.rotation = Quaternion.LookRotation(-DirectionToLight.normalized);
            RenderSettings.sun = sun;

            var islandGo = new GameObject("Island");
            var builder = islandGo.AddComponent<IslandBuilder>();
            builder.map = map;
            builder.column = column;
            builder.underside = underside;
            builder.capGrass = capGrass;
            builder.capTallGrass = capTall;
            builder.capPath = capPath;
            builder.capWater = capWater;
            builder.trunk = trunk;
            builder.leavesLow = leavesLow;
            builder.leavesHigh = leavesHigh;
            builder.rock = rock;
            builder.grass = grass;
            builder.Rebuild();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Diorama] Saved {ScenePath}: {builder.TileCount} tiles, {builder.BladeCount} grass blades, " +
                      $"{builder.DrawCallCount} instanced draws per camera.");

            SavePreview(cam);

            if (!reuseSingle)
            {
                if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Tools/Diorama/Save Preview Image")]
        static void SavePreviewMenu()
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Diorama] No Main Camera in the open scene."); return; }
            SavePreview(cam);
        }

        static void SavePreview(Camera cam)
        {
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(PreviewPath, tex.EncodeToPNG());
                Debug.Log($"[Diorama] Preview saved to {Path.GetFullPath(PreviewPath)}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Diorama] Preview render failed: {e.Message}");
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }
        }

        static IslandBuilder.ToonPart Part(Shader toon, Shader outline, string name, string hex, float outlineWidth)
        {
            var body = ToonMaterial(toon, name, hex);
            var line = LoadOrCreateMaterial(outline, name + "_Outline");
            line.SetFloat("_OutlineWidth", outlineWidth);
            // Prototype: outline colour = base colour * 0.28, in linear space.
            line.SetColor("_OutlineColor", (Hex(hex).linear * 0.28f).gamma);
            EditorUtility.SetDirty(line);
            return new IslandBuilder.ToonPart { body = body, outline = line };
        }

        static Material ToonMaterial(Shader toon, string name, string hex, float jitter = 0f)
        {
            var m = LoadOrCreateMaterial(toon, name);
            m.SetColor("_BaseColor", Hex(hex));
            m.SetFloat("_Spec", 0f); // flat island faces would catch big glint patches
            m.SetFloat("_OriginJitter", jitter);
            m.SetColor("_OutlineColor", (Hex(hex).linear * 0.28f).gamma);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material LoadOrCreateMaterial(Shader shader, string name)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader)
            {
                m.shader = shader;
            }
            m.enableInstancing = true;
            return m;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
