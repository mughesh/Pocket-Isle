using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Step 24 bridge checks: what the battle row covers from the seat, and how far it is from the rim pods.</summary>
    public static class Step24Debug
    {
        struct Item
        {
            public string name;
            public Rect rect;
        }

        /// <summary>Logs every overlap (from the current camera) between the row or its caption and the things that must stay visible.</summary>
        public static void Seat()
        {
            var cam = Camera.main;
            var runes = Object.FindFirstObjectByType<BattleRunes>();
            if (cam == null || runes == null)
            {
                Debug.LogWarning("[Step24Debug] No camera or no battle row showing.");
                return;
            }
            var row = new List<Item>();
            var plates = new List<(string name, Bounds b)>();
            // The plates' real size (a tablet's renderer bounds are padded for its halo and read as covering everything).
            foreach (var c in runes.GetComponentsInChildren<Oculus.Interaction.Surfaces.BoundsClipper>())
            {
                var b = Flat(c.transform, c.Size.x, c.Size.y);
                row.Add(new Item { name = Owner(c.transform), rect = View(cam, b) });
                plates.Add((Owner(c.transform), b));
            }
            foreach (var r in runes.GetComponentsInChildren<MeshRenderer>())
                if (r.name == "Caption Pill") row.Add(new Item { name = "caption", rect = View(cam, Flat(r.transform, 1f, 1f)) });
            var keep = new List<Item>();
            foreach (var a in Object.FindObjectsByType<StageActor>(FindObjectsSortMode.None))
                if (a.TryGetVisualBounds(out var b)) keep.Add(new Item { name = a.name, rect = View(cam, b) });
            var hud = Object.FindFirstObjectByType<BattleHud>();
            if (hud != null)
                foreach (Transform c in hud.transform)
                    if (c.name.StartsWith("HP "))
                        foreach (var r in c.GetComponentsInChildren<MeshRenderer>())
                            if (r.enabled && r.name != "Text") keep.Add(new Item { name = "label " + c.name, rect = View(cam, r.bounds) });
            var rack = Object.FindFirstObjectByType<PodRack>();
            float nearest = float.MaxValue;
            string nearestPair = "-";
            if (rack != null)
                for (int i = 0; i <= Party.Size; i++)
                {
                    var pod = rack.Pod(i);
                    if (pod == null || !pod.isActiveAndEnabled) continue;
                    foreach (var r in pod.GetComponentsInChildren<Renderer>())
                    {
                        if (!r.enabled) continue;
                        keep.Add(new Item { name = "pod " + i, rect = View(cam, r.bounds) });
                        foreach (var p in plates)
                        {
                            float d = Mathf.Sqrt(p.b.SqrDistance(pod.transform.position)) - pod.WorldRadius;
                            if (d < nearest)
                            {
                                nearest = d;
                                nearestPair = $"{p.name} / pod {i}";
                            }
                        }
                    }
                }
            var sb = new StringBuilder($"[Step24Debug] Seat check, {keep.Count} things to keep clear, {row.Count} row parts:");
            int hits = 0;
            var seen = new HashSet<string>();
            foreach (var a in row)
                foreach (var k in keep)
                {
                    if (!a.rect.Overlaps(k.rect)) continue;
                    var o = Rect.MinMaxRect(Mathf.Max(a.rect.xMin, k.rect.xMin), Mathf.Max(a.rect.yMin, k.rect.yMin), Mathf.Min(a.rect.xMax, k.rect.xMax), Mathf.Min(a.rect.yMax, k.rect.yMax));
                    float share = o.width * o.height / Mathf.Max(1e-6f, k.rect.width * k.rect.height);
                    if (share < 0.005f || !seen.Add(a.name + k.name)) continue;
                    hits++;
                    sb.Append($"\n  {a.name} covers {k.name}: {share * 100f:0}% of its box");
                }
            if (hits == 0) sb.Append("\n  nothing covered");
            sb.Append($"\n  nearest move to a rim pod: {nearest * 100f:0.0} cm in the room ({nearestPair}).");
            Debug.Log(sb.ToString());
        }

        /// <summary>Reloads the open scene from disk: a new field default stays at its old value in memory across domain reloads.</summary>
        public static void ReloadScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                Debug.LogWarning("[Step24Debug] The scene has unsaved changes: not reloading.");
                return;
            }
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);
            Debug.Log("[Step24Debug] Scene reloaded from disk.");
        }

        static Bounds Flat(Transform t, float w, float h)
        {
            var b = new Bounds(t.TransformPoint(new Vector3(-w, -h, 0f) * 0.5f), Vector3.zero);
            b.Encapsulate(t.TransformPoint(new Vector3(w, -h, 0f) * 0.5f));
            b.Encapsulate(t.TransformPoint(new Vector3(-w, h, 0f) * 0.5f));
            b.Encapsulate(t.TransformPoint(new Vector3(w, h, 0f) * 0.5f));
            return b;
        }

        static string Owner(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.name.StartsWith("Rune ") && t.name != "Rune Row") return t.name;
            return "row";
        }

        static Rect View(Camera cam, Bounds b)
        {
            float x0 = 9f, y0 = 9f, x1 = -9f, y1 = -9f;
            for (int i = 0; i < 8; i++)
            {
                var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1));
                var v = cam.WorldToViewportPoint(c);
                x0 = Mathf.Min(x0, v.x);
                y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x);
                y1 = Mathf.Max(y1, v.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
    }
}
