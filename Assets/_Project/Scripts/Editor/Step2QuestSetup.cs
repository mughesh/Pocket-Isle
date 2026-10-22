using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
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

            DisableRigLocomotion();

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
            var probe = testGo.AddComponent<ThumbHoldProbe>();
            probe.input = input;
            piece.probe = probe;
            AddIslandHandling(builder, placer, input);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            Debug.Log($"[Diorama] Saved {ScenePath} with Building Blocks, island and gesture test.");
        }

        // The comprehensive Interactions Rig ships with locomotion: a gravity character controller (falls forever
        // in MR with no floor collider), thumb-microgesture teleport / snap turn / step, and controller thumbstick
        // locomotion. Pocket isle is seated and uses the thumb swipes itself, so all of it goes.
        static readonly string[] LocomotionObjects =
        {
            "Locomotor",
            "MicroGesturesLocomotionHandInteractorGroup",
            "LocomotionControllerInteractorGroup",
            "LocomotionOutput",
        };

        [MenuItem("Tools/Diorama/Step 2/3. Disable Rig Locomotion (open scene)")]
        static void DisableRigLocomotion()
        {
            int count = 0;
            var rig = UnityEngine.Object.FindFirstObjectByType<OVRCameraRig>(FindObjectsInactive.Include);
            if (rig == null)
            {
                Debug.LogWarning("[Diorama] No OVRCameraRig in the open scene.");
                return;
            }
            foreach (var t in rig.GetComponentsInChildren<Transform>(true))
            {
                if (Array.IndexOf(LocomotionObjects, t.name) < 0 || !t.gameObject.activeSelf) continue;
                Undo.RecordObject(t.gameObject, "Disable rig locomotion");
                t.gameObject.SetActive(false);
                count++;
            }
            EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
            Debug.Log($"[Diorama] Disabled {count} rig locomotion objects.");
        }

        [MenuItem("Tools/Diorama/Step 2/4. Add Thumb Hold Probe to MR_Main")]
        static void AddThumbHoldProbe()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (active.isDirty)
                {
                    Debug.LogError($"[Diorama] Save or discard changes in {active.name} first.");
                    return;
                }
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var input = UnityEngine.Object.FindFirstObjectByType<MicrogestureInput>();
            if (input == null)
            {
                Debug.LogError("[Diorama] No MicrogestureInput in MR_Main.");
                return;
            }
            if (!input.TryGetComponent(out ThumbHoldProbe probe)) probe = Undo.AddComponent<ThumbHoldProbe>(input.gameObject);
            probe.input = input;
            EditorUtility.SetDirty(probe);
            if (input.TryGetComponent(out GestureTestPiece piece))
            {
                Undo.RecordObject(piece, "Wire thumb probe");
                piece.probe = probe;
                // Halved after the first headset test; the scene had the old values serialized.
                piece.walkSeconds = 0.6f;
                piece.runSeconds = 0.3f;
                EditorUtility.SetDirty(piece);
            }
            EditorSceneManager.SaveScene(active);
            Debug.Log("[Diorama] ThumbHoldProbe added to MR_Main.");
        }

        [MenuItem("Tools/Diorama/Step 2/5. Add Island Handling to MR_Main")]
        static void AddIslandHandlingMenu()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (active.isDirty)
                {
                    Debug.LogError($"[Diorama] Save or discard changes in {active.name} first.");
                    return;
                }
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var builder = UnityEngine.Object.FindFirstObjectByType<IslandBuilder>();
            var placer = UnityEngine.Object.FindFirstObjectByType<IslandPlacer>();
            var input = UnityEngine.Object.FindFirstObjectByType<MicrogestureInput>();
            if (builder == null || placer == null || input == null)
            {
                Debug.LogError("[Diorama] MR_Main is missing the island, placer or microgesture input.");
                return;
            }
            AddIslandHandling(builder, placer, input);
            EditorSceneManager.SaveScene(active);
            Debug.Log("[Diorama] Island handling added to MR_Main.");
        }

        // Rim grab box sizes, island-local metres (the island is 14 tiles x 5 cm, cap top at +0.006).
        const float RimInset = 0.04f, RimOutset = 0.035f, RimTop = 0f, RimBottom = -0.10f;

        /// <summary>Grabbable + rim HandGrabInteractable + IslandHandle on the island, shadow, palm summon, gesture guard.</summary>
        static void AddIslandHandling(IslandBuilder builder, IslandPlacer placer, MicrogestureInput input)
        {
            var islandGo = builder.gameObject;
            var grabbable = islandGo.GetComponent<Grabbable>();
            if (grabbable == null) grabbable = Undo.AddComponent<Grabbable>(islandGo);
            var handle = islandGo.GetComponent<IslandHandle>();
            if (handle == null) handle = Undo.AddComponent<IslandHandle>(islandGo);

            // Rim: kinematic body + a box along each side, below the cap so the play surface can't start a grab.
            var rimT = islandGo.transform.Find("Rim Grab");
            if (rimT != null) Undo.DestroyObjectImmediate(rimT.gameObject);
            var rimGo = new GameObject("Rim Grab");
            Undo.RegisterCreatedObjectUndo(rimGo, "Rim Grab");
            rimGo.transform.SetParent(islandGo.transform, false);
            var rb = rimGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var map = builder.map;
            float halfX = map.Columns * map.tileSize * 0.5f, halfZ = map.Rows * map.tileSize * 0.5f;
            float depth = RimInset + RimOutset, height = RimTop - RimBottom, y = (RimTop + RimBottom) * 0.5f;
            float edge = (RimOutset - RimInset) * 0.5f; // box centre offset from the island edge, outward
            var colliders = new List<Collider>();
            void AddBox(Vector3 center, Vector3 size)
            {
                var box = rimGo.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
                colliders.Add(box);
            }
            AddBox(new Vector3(0f, y, halfZ + edge), new Vector3(halfX * 2f + depth, height, depth));
            AddBox(new Vector3(0f, y, -halfZ - edge), new Vector3(halfX * 2f + depth, height, depth));
            AddBox(new Vector3(halfX + edge, y, 0f), new Vector3(depth, height, halfZ * 2f + depth));
            AddBox(new Vector3(-halfX - edge, y, 0f), new Vector3(depth, height, halfZ * 2f + depth));

            var interactable = rimGo.AddComponent<HandGrabInteractable>();
            interactable.InjectRigidbody(rb);
            interactable.InjectOptionalPointableElement(grabbable);
            interactable.InjectSupportedGrabTypes(Oculus.Interaction.Grab.GrabTypeFlags.All);
            interactable.HandAlignment = HandAlignType.None; // free grab, the hand doesn't snap to a pose

            var g = new SerializedObject(grabbable);
            g.FindProperty("_oneGrabTransformer").objectReferenceValue = handle;
            g.FindProperty("_twoGrabTransformer").objectReferenceValue = handle;
            g.FindProperty("_maxGrabPoints").intValue = 2;
            g.ApplyModifiedPropertiesWithoutUndo();

            handle.island = builder;
            handle.placer = placer;
            handle.rimInteractable = interactable;
            handle.rimColliders = colliders.ToArray();
            EditorUtility.SetDirty(handle);

            input.grabGuard = handle;
            EditorUtility.SetDirty(input);

            var shadow = UnityEngine.Object.FindFirstObjectByType<IslandShadow>();
            if (shadow == null)
            {
                var shadowGo = new GameObject("Island Shadow");
                Undo.RegisterCreatedObjectUndo(shadowGo, "Island Shadow");
                shadow = shadowGo.AddComponent<IslandShadow>();
            }
            shadow.island = builder;
            shadow.shadowMaterial = BlobShadowMaterial();
            EditorUtility.SetDirty(shadow);


            if (input.TryGetComponent(out GestureTestPiece piece))
            {
                piece.moveMode = MoveMode.HoldToWalk;
                EditorUtility.SetDirty(piece);
            }
        }

        static Material BlobShadowMaterial()
        {
            const string path = "Assets/_Project/Materials/BlobShadow.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Diorama/BlobShadow");
            if (shader == null)
            {
                Debug.LogError("[Diorama] Shader Diorama/BlobShadow not found.");
                return null;
            }
            DioramaSceneSetup.EnsureFolder("Assets/_Project/Materials");
            mat = new Material(shader) { name = "BlobShadow", enableInstancing = true };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        /// <summary>Batch mode: -executeMethod Diorama.EditorTools.Step2QuestSetup.FixMrMainLocomotion</summary>
        static void FixMrMainLocomotion()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            DisableRigLocomotion();
            EditorSceneManager.SaveScene(scene);
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
