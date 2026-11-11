using System.Collections.Generic;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Nov 12 (Mugesh: "why are things more broken than before?": the Keeper caption behind the owl, a door chip through
    /// the arch and the tower): every visible text (speech bubble, door chips, the battle line, HP plates, toasts and
    /// cards) checked from the camera against each other and the creatures and the explorer (on screen), and against the
    /// island's props (in 3D: a text inside a prop). The smoke run calls <see cref="Check"/> every half second and fails
    /// the beat on any hit, with a seat shot.
    /// </summary>
    public static class TextLayoutCheck
    {
        public struct Item
        {
            public string name;
            public Rect view;
            public Bounds world;
        }

        /// <summary>Share of the smaller text two texts may share on screen.</summary>
        public const float TextOverlap = 0.05f;

        /// <summary>Share of a creature's or the explorer's box a text may cover on screen.</summary>
        public const float BodyOverlap = 0.15f;

        static readonly string[] TabletNames = { "New Isling tag", "Collection card", "Thank you card", "Hall of Fame card" };

        public static List<string> Check(Camera cam)
        {
            var hits = new List<string>();
            if (cam == null) return hits;
            var texts = Texts(cam);
            var bodies = Bodies(cam);
            for (int i = 0; i < texts.Count; i++)
                for (int j = i + 1; j < texts.Count; j++)
                {
                    float share = Share(texts[i].view, texts[j].view, Mathf.Min(Area(texts[i].view), Area(texts[j].view)));
                    if (share > TextOverlap) hits.Add($"{texts[i].name} overlaps {texts[j].name} ({share * 100f:0}%)");
                }
            foreach (var t in texts)
                foreach (var b in bodies)
                {
                    // A body's box is generous (round creatures, ears, crests, aura): its core must stay clear: 10% off the
                    // sides and the bottom, 20% off the top (a speaker's own bubble sits just over its crest, the tail on it).
                    var core = Rect.MinMaxRect(b.view.xMin + b.view.width * 0.1f, b.view.yMin + b.view.height * 0.1f, b.view.xMax - b.view.width * 0.1f, b.view.yMax - b.view.height * 0.2f);
                    float share = Share(t.view, core, Area(core));
                    if (share > BodyOverlap) hits.Add($"{t.name} covers {b.name} ({share * 100f:0}% of it)");
                }
            if (!EncounterDirector.AnyInBattle)
                foreach (var t in texts)
                    foreach (var p in PropBoxes())
                        if (Inside(t.world, p.world) > 0.25f) hits.Add($"{t.name} is inside {p.name}");
            return hits;
        }

        /// <summary>Share of the text's box (given some depth) that lies inside the prop's box.</summary>
        static float Inside(Bounds text, Bounds prop)
        {
            var e = text.extents;
            float depth = Mathf.Max(e.x, e.y, e.z) * 0.2f;
            text.Expand(new Vector3(e.x < depth ? depth - e.x : 0f, e.y < depth ? depth - e.y : 0f, e.z < depth ? depth - e.z : 0f) * 2f);
            if (!text.Intersects(prop)) return 0f;
            var min = Vector3.Max(text.min, prop.min);
            var max = Vector3.Min(text.max, prop.max);
            var d = max - min;
            float vol = Mathf.Max(0f, d.x) * Mathf.Max(0f, d.y) * Mathf.Max(0f, d.z);
            var s = text.size;
            return vol / Mathf.Max(1e-12f, s.x * s.y * s.z);
        }

        static float Area(Rect r) => Mathf.Max(1e-6f, r.width * r.height);

        static float Share(Rect a, Rect b, float of)
        {
            if (!a.Overlaps(b)) return 0f;
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return Mathf.Max(0f, w) * Mathf.Max(0f, h) / Mathf.Max(1e-6f, of);
        }

        public static Rect View(Camera cam, Bounds b)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            bool any = false;
            for (int i = 0; i < 8; i++)
            {
                var p = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var v = cam.WorldToViewportPoint(p);
                if (v.z <= 0f) continue;
                any = true;
                x0 = Mathf.Min(x0, v.x);
                y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x);
                y1 = Mathf.Max(y1, v.y);
            }
            return any ? Rect.MinMaxRect(x0, y0, x1, y1) : new Rect(-10f, -10f, 0f, 0f);
        }

        static bool Shown(Renderer r) => r != null && r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.sqrMagnitude > 1e-8f;

        static void Add(List<Item> list, Camera cam, string name, Bounds b) => list.Add(new Item { name = name, view = View(cam, b), world = b });

        /// <summary>Every text showing now, with its plate's bounds.</summary>
        public static List<Item> Texts(Camera cam)
        {
            var list = new List<Item>();
            foreach (var bubble in Object.FindObjectsByType<DialogueBubble>(FindObjectsSortMode.None))
            {
                if (bubble.Text == null && !bubble.IsChip) continue;
                bool any = false;
                var all = new Bounds();
                foreach (var r in bubble.GetComponentsInChildren<MeshRenderer>())
                {
                    // The tail points at the speaker on purpose: the panel is what must keep clear.
                    if (!Shown(r) || r.name == "Tail" || r.name == "Chip Hand") continue;
                    if (!any) all = r.bounds;
                    else all.Encapsulate(r.bounds);
                    any = true;
                }
                if (any) Add(list, cam, "speech bubble", all);
            }
            // A tablet's parts (plate, frame) count as one text.
            var tablets = new Dictionary<Transform, Bounds>();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!Shown(r)) continue;
                var go = r.transform;
                if (r.name == "Caption Pill") Add(list, cam, "battle line", r.bounds);
                else if (r.name == "Plate" && go.parent != null && go.parent.name.StartsWith("HP ")) Add(list, cam, $"HP plate {go.parent.name.Substring(3)}", r.bounds);
                else if (go.parent != null && r.name != "Text" && (go.parent.name.EndsWith(" Chip") || System.Array.Exists(TabletNames, n => go.parent.name.StartsWith(n))))
                {
                    if (tablets.TryGetValue(go.parent, out var b)) b.Encapsulate(r.bounds);
                    else b = r.bounds;
                    tablets[go.parent] = b;
                }
            }
            foreach (var kv in tablets) Add(list, cam, kv.Key.name, kv.Value);
            return list;
        }

        /// <summary>The battle's creatures and the explorer.</summary>
        public static List<Item> Bodies(Camera cam)
        {
            var list = new List<Item>();
            foreach (var a in Object.FindObjectsByType<StageActor>(FindObjectsSortMode.None))
                if (a.isActiveAndEnabled && a.TryGetVisualBounds(out var b)) Add(list, cam, a.name, b);
            var g = GameState.Instance;
            var body = g != null && g.explorer != null ? g.explorer.Body : null;
            if (body != null && body.gameObject.activeInHierarchy)
            {
                bool any = false;
                var all = new Bounds();
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                {
                    if (!Shown(r)) continue;
                    if (!any) all = r.bounds;
                    else all.Encapsulate(r.bounds);
                    any = true;
                }
                if (any) Add(list, cam, "explorer", all);
            }
            return list;
        }

        /// <summary>World boxes of the baked props the island shows now (map metres from their baked height).</summary>
        public static List<Item> PropBoxes()
        {
            var list = new List<Item>();
            var g = GameState.Instance;
            var island = g != null && g.explorer != null ? g.explorer.island : null;
            var world = island != null ? island.world : null;
            if (world == null || island.Content == null) return list;
            float tile = world.tileSize, cap = island.Content.capHeight;
            var t = island.transform;
            for (int r = 0; r < world.Rows; r++)
                for (int c = 0; c < world.Columns; c++)
                {
                    if (!world.TryGetProp(world.PropAt(r, c), out var e) || e.recipe == null || e.recipe.bakedMesh == null) continue;
                    if (world.IsPropHidden(new Vector2Int(c, r))) continue;
                    var f = WorldMap.FootprintOf(e);
                    var mid = new Vector2(c + (f.x - 1) * 0.5f, r + (f.y - 1) * 0.5f);
                    if (!island.FootprintContains(island.TileToLocal(mid, cap))) continue;
                    float top = e.recipe.bakedMesh.bounds.max.y * tile;
                    // A little inside the footprint: a prop's mesh rarely fills its tiles to the edge.
                    var a = t.TransformPoint(island.TileToLocal(new Vector2(c - 0.35f, r - 0.35f), cap));
                    var b = t.TransformPoint(island.TileToLocal(new Vector2(c + f.x - 0.65f, r + f.y - 0.65f), cap + top));
                    var box = new Bounds(a, Vector3.zero);
                    box.Encapsulate(b);
                    list.Add(new Item { name = $"{e.recipe.name} (c{c} r{r})", world = box });
                }
            return list;
        }
    }
}
