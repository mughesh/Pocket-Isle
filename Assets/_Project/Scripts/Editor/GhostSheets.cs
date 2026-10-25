using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Play mode (bridge: exec:Diorama.EditorTools.GhostSheets.All / Left): one contact sheet per ghost clip in
    /// Logs/GhostSheets: five moments of the kept part (start, 25%, key, 75%, end), from the player's seat above the
    /// island. Skinning updates once per frame, so it poses on one editor tick and renders on the next.
    /// </summary>
    static class GhostSheets
    {
        const int W = 420, H = 320;
        static readonly Queue<System.Action> _steps = new Queue<System.Action>();
        static int _wait;

        public static void All() => Queue(false);
        public static void Left() => Queue(true);

        static void Queue(bool left)
        {
            var g = Object.FindFirstObjectByType<GhostHands>();
            var lib = GhostClipTools.Library();
            if (g == null || !Application.isPlaying) return;
            Directory.CreateDirectory("Logs/GhostSheets");
            foreach (var w in GhostClipLibrary.Wanted)
            {
                var clip = lib.Find(w.name);
                string name = w.name;
                float key = clip != null ? clip.keyTime : 0.5f;
                Texture2D sheet = null;
                _steps.Enqueue(() =>
                {
                    g.previewing = true;
                    g.Show(name, GhostPlace.AboveIsland, null, 1f, left, name == "hands_up");
                    sheet = new Texture2D(W * 5, H, TextureFormat.RGB24, false);
                });
                var phases = new[] { 0f, 0.25f, key, 0.75f, 1f };
                for (int i = 0; i < phases.Length; i++)
                {
                    int tile = i;
                    float u = phases[i];
                    _steps.Enqueue(() => g.debugFreeze = u);
                    _steps.Enqueue(() => Tile(g, sheet, tile));
                }
                _steps.Enqueue(() =>
                {
                    string path = $"Logs/GhostSheets/{name}{(left ? "_left" : "")}.png";
                    File.WriteAllBytes(path, sheet.EncodeToPNG());
                    Object.DestroyImmediate(sheet);
                    Debug.Log($"[GhostSheets] {path} ({(clip != null ? "recorded" : "placeholder")}, key {key:0.00}).");
                });
            }
            _steps.Enqueue(() =>
            {
                g.debugFreeze = -1f;
                g.previewing = false;
                g.Hide();
                Debug.Log("[GhostSheets] Done.");
            });
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!Application.isPlaying)
            {
                _steps.Clear();
                EditorApplication.update -= Tick;
                return;
            }
            if (_wait-- > 0) return;
            if (_steps.Count == 0)
            {
                EditorApplication.update -= Tick;
                return;
            }
            _steps.Dequeue()();
            _wait = 2; // let a frame (pose + skinning) pass
        }

        static void Tile(GhostHands g, Texture2D sheet, int tile)
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || sheet == null) return;
            var t = island.transform;
            var key = g.KeyPoint ?? t.position;
            // The player's seat: in front of the island (its front faces the player), above it.
            var eye = key + t.rotation * new Vector3(0f, 0.16f, 0.38f);
            var go = new GameObject("Sheet Camera");
            var cam = go.AddComponent<Camera>();
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(key - eye, Vector3.up));
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            sheet.ReadPixels(new Rect(0, 0, W, H), tile * W, 0);
            sheet.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
