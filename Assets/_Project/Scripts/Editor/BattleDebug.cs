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

        /// <summary>
        /// Fires a long Spirit line for shots. The view clamp follows the real head (a Link headset on the desk points
        /// anywhere), so it's switched off here.
        /// </summary>
        public static void SayTest()
        {
            var bubble = Object.FindFirstObjectByType<DialogueBubble>();
            if (bubble != null) bubble.maxViewAngle = 180f;
            Dialogue.Say("intro.explorer");
        }

        /// <summary>Realm restoration at once (no fade), for shots of both states.</summary>
        public static void Unrestored() => RealmState.Restored = 0f;
        public static void Restored() => RealmState.Restored = 1f;

        /// <summary>Seated view and a close view of the island, rendered from island-relative poses (works with a headset on Link, which owns the main camera).</summary>
        public static void IslandShots()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            foreach (var (name, eye, look) in new[]
                     {
                         ("seated", new Vector3(0f, 0.55f, 0.45f), new Vector3(0f, 0f, 0.05f)),
                         ("close", new Vector3(0f, 0.26f, 0.26f), new Vector3(0f, 0f, -0.02f)),
                     })
            {
                var pos = t.position + t.rotation * eye;
                var rot = Quaternion.LookRotation(t.position + t.rotation * look - pos, Vector3.up);
                string path = $"{ShotDir}/island_{name}_{(RealmState.Restored > 0.5f ? "restored" : "unrestored")}.png";
                Render(path, pos, rot);
                Debug.Log($"[BattleDebug] Screenshot {path}");
            }
        }

        /// <summary>GoTo the farthest reachable tile within 6 steps (tests pathfinding in Play mode).</summary>
        public static void GoTo()
        {
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (e == null) return;
            Vector2Int best = e.Tile;
            for (int dx = -6; dx <= 6; dx++)
                for (int dy = -6; dy <= 6; dy++)
                {
                    var t = e.Tile + new Vector2Int(dx, dy);
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > Mathf.Abs(best.x - e.Tile.x) + Mathf.Abs(best.y - e.Tile.y) && e.CanReach(t)) best = t;
                }
            Debug.Log($"[BattleDebug] GoTo c{best.x} r{best.y}: {e.GoTo(best)}");
        }

        public static void Move1Strip() { Choose(0); Strip(); }
        public static void Move2Strip() { Choose(1); Strip(); }
        public static void Move3Strip() { Choose(2); Strip(); }

        /// <summary>Renders the main camera's view (narrower lens, bigger image) to Logs/BattleShots.</summary>
        /// <summary>Logs every PokeInteractor with its path and which object on the path is switched off.</summary>
        public static void ListPoke()
        {
            foreach (var p in Object.FindObjectsByType<Oculus.Interaction.PokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var path = new System.Text.StringBuilder();
                for (var t = p.transform; t != null; t = t.parent)
                    path.Insert(0, "/" + t.name + (t.gameObject.activeSelf ? "" : "[OFF]"));
                Debug.Log($"[BattleDebug] {path} enabled={p.enabled}");
            }
        }

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
            Render(path, main.transform.position, main.transform.rotation);
        }

        static void Render(string path, Vector3 position, Quaternion rotation)
        {
            Directory.CreateDirectory(ShotDir);
            var go = new GameObject("Shot Camera");
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(position, rotation);
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
