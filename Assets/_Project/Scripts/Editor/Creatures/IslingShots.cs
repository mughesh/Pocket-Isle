using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// One image per Isling with its front, side and three-quarter views side by side (steps 9c / 9d:
    /// Logs/BattleShots/9c_hearthstag.png, 9d_&lt;name&gt;.png), rendered in an isolated preview scene like the Creature
    /// Lab shots. <c>prepare</c> can repaint the instance first (the Keeper's frightened look).
    /// </summary>
    public static class IslingShots
    {
        const int Size = 600;

        public static string Turnaround(IslingSpecies species, string path, Action<GameObject> prepare = null)
        {
            if (species == null || species.Prefab == null)
            {
                Debug.LogError($"[IslingShots] {(species != null ? species.name : "null")} has no baked prefab.");
                return null;
            }
            var preview = EditorSceneManager.NewPreviewScene();
            var sheet = new Texture2D(Size * 3, Size, TextureFormat.RGB24, false);
            try
            {
                var camGo = new GameObject("Shot Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.scene = preview;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.8f, 0.82f, 0.83f);
                cam.fieldOfView = 24f;
                cam.nearClipPlane = 0.05f;
                var lightGo = new GameObject("Shot Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.None;
                lightGo.transform.rotation = Quaternion.LookRotation(-new Vector3(-0.45f, 0.85f, 0.5f).normalized);
                var turntable = new GameObject("Turntable").transform;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(turntable.gameObject, preview);

                var go = (GameObject)PrefabUtility.InstantiatePrefab(species.Prefab, preview);
                go.transform.SetParent(turntable, false);
                go.transform.localScale = Vector3.one * 8f;
                go.GetComponent<CreatureView>().ApplyStyles();
                prepare?.Invoke(go);

                var b = new Bounds(go.transform.position, Vector3.zero);
                foreach (var rend in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(rend.bounds);
                float radius = new Vector2(Mathf.Max(b.extents.x, b.extents.z), b.extents.y).magnitude;
                float fit = radius * 1.15f / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                Vector3 target = new Vector3(0f, b.center.y, 0f);
                cam.transform.position = target + new Vector3(0f, 0.11f, 1.45f).normalized * fit;
                cam.transform.LookAt(target);

                var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                var prev = RenderTexture.active;
                int x = 0;
                foreach (float yaw in new[] { 0f, -90f, -35f })
                {
                    turntable.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    sheet.ReadPixels(new Rect(0, 0, Size, Size), x, 0);
                    x += Size;
                }
                RenderTexture.active = prev;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                Debug.Log($"[IslingShots] {species.name}: front / side / three-quarter -> {path}");
                return path;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
