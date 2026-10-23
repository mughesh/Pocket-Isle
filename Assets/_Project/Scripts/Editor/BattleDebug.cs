using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Play mode helpers for the Claude bridge (exec:Diorama.EditorTools.BattleDebug.Grass, ...).</summary>
    static class BattleDebug
    {
        const string ShotDir = "Logs/BattleShots";
        static int _shot;

        static EncounterDirector Director => Object.FindFirstObjectByType<EncounterDirector>();
        static BattleRunes Runes => Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);

        public static void Grass() => Start(Habitat.Grass);
        public static void Water() => Start(Habitat.Water);
        public static void Move1() => Choose(0);
        public static void Move2() => Choose(1);
        public static void Move3() => Choose(2);
        public static void Run() => Choose(BattleRunes.RunIndex);

        static void Start(Habitat habitat)
        {
            var d = Director;
            if (d != null) d.StartEncounter(habitat);
            else Debug.LogError("[BattleDebug] No EncounterDirector (Play mode in MR_Main?).");
        }

        static void Choose(int index)
        {
            var r = Runes;
            if (r != null) r.Choose(index);
        }

        /// <summary>
        /// Flat Play has no head pose, so the camera sits at the floor. This holds the main camera where a seated
        /// player's eyes would be (0.55 m above the island, 0.45 m back), looking at the island, until Play stops.
        /// </summary>
        public static void SeatCamera()
        {
            EditorApplication.update -= HoldCamera;
            EditorApplication.update += HoldCamera;
            HoldCamera();
        }

        static void HoldCamera()
        {
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!EditorApplication.isPlaying || cam == null || island == null)
            {
                EditorApplication.update -= HoldCamera;
                return;
            }
            var t = island.transform;
            cam.transform.position = t.position + t.rotation * new Vector3(0f, 0.55f, 0.45f);
            cam.transform.LookAt(t.position + t.rotation * new Vector3(0f, 0f, 0.05f));
        }

        /// <summary>A shot every 0.12 s for 2.4 s (strip_00..19), to see effects mid-flight.</summary>
        public static void Strip()
        {
            _stripStart = EditorApplication.timeSinceStartup;
            _stripCount = 0;
            EditorApplication.update -= StripTick;
            EditorApplication.update += StripTick;
        }

        static double _stripStart;
        static int _stripCount;

        static void StripTick()
        {
            if (!EditorApplication.isPlaying || _stripCount >= 20)
            {
                EditorApplication.update -= StripTick;
                return;
            }
            if (EditorApplication.timeSinceStartup - _stripStart < _stripCount * 0.12) return;
            Render($"{ShotDir}/strip_{_stripCount++:00}.png");
        }

        public static void Move1Strip() { Choose(0); Strip(); }
        public static void Move2Strip() { Choose(1); Strip(); }
        public static void Move3Strip() { Choose(2); Strip(); }

        /// <summary>Renders the main camera's view (narrower lens, bigger image) to Logs/BattleShots.</summary>
        public static void Shot()
        {
            var main = Camera.main;
            if (main == null) return;
            string path = $"{ShotDir}/battle_{_shot++:00}.png";
            Render(path);
            Debug.Log($"[BattleDebug] Screenshot {path} (camera at {main.transform.position:F2})");
        }

        static void Render(string path)
        {
            var main = Camera.main;
            if (main == null) return;
            Directory.CreateDirectory(ShotDir);
            var go = new GameObject("Shot Camera");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            var rt = new RenderTexture(1280, 900, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
