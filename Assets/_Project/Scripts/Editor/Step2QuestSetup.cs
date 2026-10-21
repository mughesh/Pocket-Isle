using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 2: Quest MR project settings and the MR_Main scene (Meta Building Blocks + island + gesture test).
    /// Both are menu items so nothing changes until they are run.
    /// </summary>
    static class Step2QuestSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string DioramaScenePath = "Assets/_Project/Scenes/Diorama.unity";
        const string MobileRpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";

        // The package name must be unique on the Horizon Store.
        const string CompanyName = "Mugesh";
        const string ProductName = "Pocket isle";
        const string PackageName = "com.mugesh.pocketisle";

        // Building Block data assets (Meta XR SDK v207). Camera Rig first; the others depend on it.
        static readonly string[] BlockAssets =
        {
            "Packages/com.meta.xr.sdk.core/Editor/BuildingBlocks/BlockData/CameraRig/CameraRigBB.asset",
            "Packages/com.meta.xr.sdk.interaction.ovr/Editor/Blocks/Interactions/Interactions.asset",
            "Packages/com.meta.xr.sdk.core/Editor/BuildingBlocks/BlockData/Passthrough/Passthrough.asset",
            "Packages/com.meta.xr.mrutilitykit/Editor/BuildingBlocks/MRUtilityKit/MRUtilityKit.asset",
        };

        [MenuItem("Tools/Diorama/Step 2/1. Apply Quest MR Settings")]
        static void ApplySettings()
        {
            // Meta project config (drives the generated Android manifest: features and permissions).
            var cfg = OVRProjectConfig.CachedProjectConfig;
            cfg.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            cfg.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.HIGH;
            cfg.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            cfg.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
            cfg.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
            cfg.boundaryVisibilitySupport = OVRProjectConfig.FeatureSupport.Supported;
            cfg.targetDeviceTypes.Remove(OVRProjectConfig.DeviceType.Quest);
            OVRProjectConfig.CommitProjectConfig(cfg);

            // Player
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;

            // URP mobile asset: HDR off keeps an alpha channel for passthrough; MSAA 4x per Meta guidance.
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRpAssetPath);
            if (rp != null)
            {
                rp.supportsHDR = false;
                rp.msaaSampleCount = 4;
                rp.renderScale = 1f;
                EditorUtility.SetDirty(rp);
            }
            else Debug.LogWarning($"[Diorama] {MobileRpAssetPath} not found.");

            // TextMesh Pro essentials (font + shaders) for world-space labels.
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                TMP_PackageResourceImporter.ImportResources(true, false, false);

            AssetDatabase.SaveAssets();
            Debug.Log("[Diorama] Step 2 settings applied.");
        }

        [MenuItem("Tools/Diorama/Step 2/2. Build MR_Main Scene")]
        static async void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            foreach (var path in BlockAssets)
            {
                if (!await InstallBlock(path))
                {
                    Debug.LogError("[Diorama] Stopped: a Building Block failed to install. See the messages above.");
                    return;
                }
            }

            // The Passthrough block adds the layer but leaves OVRManager's switch for the Project Setup Tool.
            var ovrManager = UnityEngine.Object.FindFirstObjectByType<OVRManager>();
            if (ovrManager != null)
            {
                ovrManager.isInsightPassthroughEnabled = true;
                EditorUtility.SetDirty(ovrManager);
            }

            // Passthrough needs the eye camera to clear to transparent black.
            var rig =UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null && rig.centerEyeAnchor.TryGetComponent(out Camera eye))
            {
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = new Color(0f, 0f, 0f, 0f);
                EditorUtility.SetDirty(eye);
            }

            var mruk = UnityEngine.Object.FindFirstObjectByType<MRUK>();
            if (mruk != null)
            {
                // Real room only on device. In the Editor the Meta XR Simulator supplies a room;
                // with no room at all, IslandPlacer uses its in-front fallback.
                mruk.SceneSettings.DataSource = MRUK.SceneDataSource.Device;
                mruk.SceneSettings.LoadSceneOnStartup = true;
                EditorUtility.SetDirty(mruk);
            }

            // Toon light direction (colour is ignored by the toon shader).
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;
            sunGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);
            RenderSettings.sun = sun;

            var islandGo = new GameObject("Island");
            var builder = islandGo.AddComponent<IslandBuilder>();
            if (!DioramaSceneSetup.ConfigureIsland(builder)) return;
            islandGo.transform.localScale = Vector3.one * 0.6f;

            var testGo = new GameObject("Step2 Gesture Test");
            var input = testGo.AddComponent<MicrogestureInput>();
            var placer = testGo.AddComponent<IslandPlacer>();
            placer.island = builder;
            var piece = testGo.AddComponent<GestureTestPiece>();
            piece.island = builder;
            piece.input = input;
            piece.placer = placer;
            piece.cubeMaterial = DioramaSceneSetup.ToonMaterial("Placeholder_Cube", "#e0752d", 0.3f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            Debug.Log($"[Diorama] Saved {ScenePath} with Building Blocks, island and gesture test.");
        }

        static async Task<bool> InstallBlock(string assetPath)
        {
            var block = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (block == null)
            {
                Debug.LogError($"[Diorama] Building Block data not found: {assetPath}");
                return false;
            }
            string name = block.name;
            try
            {
                // BlockData.InstallWithDependencies is internal in v207; same call the Building Blocks window makes.
                var method = block.GetType().GetMethod("InstallWithDependencies",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null,
                    new[] { typeof(GameObject) }, null);
                if (method == null)
                {
                    Debug.LogError($"[Diorama] {name}: InstallWithDependencies not found (SDK API changed). Add the block from Meta > Tools > Building Blocks.");
                    return false;
                }
                await (Task)method.Invoke(block, new object[] { null });
                Debug.Log($"[Diorama] Installed Building Block: {name}");
                return true;
            }
            catch (Exception e)
            {
                var inner = e is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : e;
                if (inner.GetType().Name == "InstallationCancelledException" && inner.Message.Contains("already present"))
                {
                    Debug.Log($"[Diorama] {name} already in scene.");
                    return true;
                }
                Debug.LogError($"[Diorama] {name} failed: {inner.GetType().Name}: {inner.Message}");
                return false;
            }
        }

        static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DioramaScenePath, false),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
