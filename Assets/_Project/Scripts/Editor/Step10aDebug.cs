using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Claude bridge helpers for step 10a (edit mode unless noted). Shots land in Logs/BattleShots/10a_*.</summary>
    static class Step10aDebug
    {
        static readonly (string name, int x, int y)[] Spots =
        {
            ("arrival", 11, 6), ("village", 24, 20), ("mill", 31, 23), ("kites", 39, 34), ("ridge", 24, 45), ("tower", 18, 51),
            ("fire", 48, 15), ("rare", 50, 50),
        };

        static List<Renderer> HideAll()
        {
            var hidden = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && !r.forceRenderingOff)
                {
                    r.forceRenderingOff = true;
                    hidden.Add(r);
                }
            return hidden;
        }

        static void Show(List<Renderer> hidden)
        {
            foreach (var r in hidden)
                if (r != null) r.forceRenderingOff = false;
        }

        static WorldMap Map() => UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMap>(GalewickTools.MapPath);

        /// <summary>Galewick as text (north up): a prop's key on its anchor, else the terrain. Logs/galewick_map.txt.</summary>
        public static void DumpMap()
        {
            var map = Map();
            if (map == null) return;
            var sb = new StringBuilder();
            sb.Append("    ");
            for (int c = 0; c < map.Columns; c++) sb.Append(c % 10 == 0 ? (char)('0' + c / 10) : ' ');
            sb.Append('\n');
            for (int r = 0; r < map.Rows; r++)
            {
                sb.Append($"{map.Rows - 1 - r,3} ");
                for (int c = 0; c < map.Columns; c++)
                {
                    char k = map.PropAt(r, c);
                    sb.Append(k != IslandMap.Empty ? k : map.At(r, c));
                }
                sb.Append('\n');
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/galewick_map.txt", sb.ToString());
            Debug.Log("[Step10aDebug] Logs/galewick_map.txt");
        }

        /// <summary>Seated window shots of each spot, unrestored and restored, with draws / instances / triangles.</summary>
        public static void WindowStats()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var map = Map();
            if (island == null || map == null) return;
            var old = island.world;
            float oldRestored = RealmState.Restored;
            var hidden = HideAll();
            var sb = new StringBuilder("[Step10aDebug] Galewick island window (edit mode):");
            try
            {
                island.world = map;
                island.Rebuild();
                var t = island.transform;
                foreach (var restored in new[] { 0f, 1f })
                {
                    RealmState.Restored = restored;
                    foreach (var (name, x, y) in Spots)
                    {
                        Vector2 tile = GalewickTools.Tile(x, y);
                        island.SetView(tile, tile, 1f);
                        island.Prepare();
                        var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                        string suffix = restored > 0.5f ? "_restored" : "";
                        BattleDebug.Render($"Logs/BattleShots/10a_{name}{suffix}.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                        if (restored < 0.5f)
                            sb.Append($"\n  {name,-8} draws {island.DrawCallCount,3}, instances {island.InstanceTotal,5}, tris {island.TriangleTotal / 1000f:0}k ({island.TriangleBreakdown(4)})");
                    }
                }
            }
            finally
            {
                RealmState.Restored = oldRestored;
                island.world = old;
                island.Rebuild();
                Show(hidden);
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>A low close shot at one spot (name from Spots), restored or not: the props up close.</summary>
        public static void CloseShots()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var map = Map();
            if (island == null || map == null) return;
            var old = island.world;
            float oldRestored = RealmState.Restored;
            var hidden = HideAll();
            try
            {
                island.world = map;
                island.Rebuild();
                var t = island.transform;
                foreach (var (name, x, y, restored) in new[] { ("mill", 30, 21, 0f), ("tower", 18, 50, 0f), ("kites", 38, 33, 1f), ("village", 23, 16, 1f) })
                {
                    RealmState.Restored = restored;
                    Vector2 tile = GalewickTools.Tile(x, y);
                    island.SetView(tile, tile, 1f);
                    island.Prepare();
                    var look = t.position + t.rotation * new Vector3(0f, 0.07f, -0.08f);
                    var eye = t.position + t.rotation * new Vector3(0.04f, 0.2f, 0.36f);
                    BattleDebug.Render($"Logs/BattleShots/10a_close_{name}{(restored > 0.5f ? "_restored" : "")}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                }
            }
            finally
            {
                RealmState.Restored = oldRestored;
                island.world = old;
                island.Rebuild();
                Show(hidden);
            }
            Debug.Log("[Step10aDebug] Close shots in Logs/BattleShots/10a_close_*.png");
        }

        /// <summary>
        /// Top-down shot of the whole realm with zone labels: the island footprint widened to 60 x 60 for one
        /// orthographic render, then put back. Logs/BattleShots/10a_map.png.
        /// </summary>
        public static void MapShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var map = Map();
            if (island == null || map == null || island.map == null) return;
            var oldWorld = island.world;
            var oldFootprint = island.map;
            var footprint = ScriptableObject.CreateInstance<IslandMap>();
            footprint.tileSize = oldFootprint.tileSize;
            footprint.capHeight = oldFootprint.capHeight;
            var rows = new StringBuilder();
            for (int r = 0; r < map.Rows; r++) rows.Append(new string(IslandMap.Grass, map.Columns)).Append('\n');
            footprint.SetLayout(rows.ToString());
            var hidden = HideAll();
            var made = new List<GameObject>();
            try
            {
                island.map = footprint;
                island.world = map;
                island.Rebuild();
                island.SetView(island.CenteredView, island.CenteredView, 1f);
                island.Prepare();
                var t = island.transform;
                float scale = t.lossyScale.x, half = map.Columns * footprint.tileSize * 0.5f * scale;
                var rot = Quaternion.LookRotation(-t.up, -t.forward); // north up, east right
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                void Label(string text, int x, int y, float size)
                {
                    var at = t.TransformPoint(island.TileToLocal(GalewickTools.Tile(x, y), 0.5f));
                    for (int k = 0; k < 2; k++)
                    {
                        var go = new GameObject("Map Label");
                        made.Add(go);
                        var tm = go.AddComponent<TextMesh>();
                        tm.text = text;
                        tm.font = font;
                        tm.fontSize = 64;
                        tm.fontStyle = FontStyle.Bold;
                        tm.characterSize = size * scale * 0.01f;
                        tm.anchor = TextAnchor.MiddleCenter;
                        tm.color = k == 0 ? new Color(0f, 0f, 0f, 0.85f) : Color.white;
                        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                        var shadow = k == 0 ? rot * new Vector3(0.004f, -0.004f, 0.01f) * scale : Vector3.zero;
                        go.transform.SetPositionAndRotation(at + shadow, rot);
                    }
                }
                Label("ARRIVAL + GATE", 10, 3, 1f);
                Label("SHRINE", 15, 21, 0.8f);
                Label("WINDMILL VILLAGE", 25, 13, 1.1f);
                Label("BIG MILL + DITCH", 29, 29, 0.9f);
                Label("KITE FIELD", 40, 36, 1.1f);
                Label("WINDY RIDGE", 22, 44, 1f);
                Label("WIND TOWER", 18, 58, 1f);
                Label("FIRE PATCH", 49, 20, 1f);
                Label("RARE GRASS", 51, 55, 1f);

                var cam = new GameObject("Map Camera").AddComponent<Camera>();
                made.Add(cam.gameObject);
                cam.stereoTargetEye = StereoTargetEyeMask.None;
                cam.orthographic = true;
                cam.orthographicSize = half * 1.02f;
                cam.transform.SetPositionAndRotation(t.position + t.up * 2f, rot);
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
                var rt = new RenderTexture(1400, 1400, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = null;
                System.IO.Directory.CreateDirectory("Logs/BattleShots");
                System.IO.File.WriteAllBytes("Logs/BattleShots/10a_map.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
            }
            finally
            {
                foreach (var go in made) Object.DestroyImmediate(go);
                island.map = oldFootprint;
                island.world = oldWorld;
                island.Rebuild();
                Object.DestroyImmediate(footprint);
                Show(hidden);
            }
            Debug.Log("[Step10aDebug] Logs/BattleShots/10a_map.png");
        }

        /// <summary>Every 10a prop on a throwaway 14 x 14 Galewick-themed map: a seated shot and a low close one, unrestored and restored.</summary>
        public static void PropGallery()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var old = island.world;
            float oldRestored = RealmState.Restored;
            var map = ScriptableObject.CreateInstance<WorldMap>();
            map.name = "Gallery";
            map.tileSize = old.tileSize;
            map.capHeight = old.capHeight;
            map.theme = PropKit.Theme("Galewick");
            const int n = 14;
            var g = new char[n, n];
            var p = new char[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    g[r, c] = r == 0 || c == 0 || r == n - 1 || c == n - 1 ? IslandMap.Blocked : IslandMap.Grass;
                    p[r, c] = IslandMap.Empty;
                }
            var legend = new List<PropLegend>();
            void Key(char k, string recipe, bool turned = false) =>
                legend.Add(new PropLegend { key = k.ToString(), recipe = PropKit.Load(recipe), turned = turned });
            Key('M', "Windmill");
            Key('m', "SmallMill");
            Key('h', "Cottage");
            Key('w', "StoneWall");
            Key('W', "StoneWall", true);
            Key('k', "Kite");
            Key('K', "KiteTeal");
            Key('g', "KiteGold");
            Key('p', "KitePost");
            Key('b', "HayBale");
            Key('c', "CloudPuff");
            Key('T', "WindTower");
            p[3, 2] = 'M';
            p[3, 5] = 'T';
            p[3, 9] = 'h';
            p[6, 2] = 'm';
            for (int c = 4; c <= 7; c++) p[6, c] = 'w';
            p[7, 8] = 'W';
            p[6, 10] = 'p';
            p[9, 2] = 'k';
            p[9, 3] = 'K';
            p[9, 4] = 'g';
            p[9, 6] = 'b';
            p[9, 8] = 'c';
            for (int r = 10; r <= 11; r++)
                for (int c = 9; c <= 12; c++) g[r, c] = IslandMap.Tree;
            for (int c = 2; c <= 6; c++) g[11, c] = IslandMap.TallGrass;
            var layout = new StringBuilder();
            var props = new StringBuilder();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    layout.Append(g[r, c]);
                    props.Append(p[r, c]);
                }
                layout.Append('\n');
                props.Append('\n');
            }
            map.SetLayout(layout.ToString());
            map.propLegend = legend;
            map.SetProps(props.ToString());
            var hidden = HideAll();
            try
            {
                island.world = map;
                island.Rebuild();
                island.SetView(island.CenteredView, island.CenteredView, 1f);
                var t = island.transform;
                foreach (var restored in new[] { 0f, 1f })
                {
                    RealmState.Restored = restored;
                    string suffix = restored > 0.5f ? "_restored" : "";
                    var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                    BattleDebug.Render($"Logs/BattleShots/10a_gallery{suffix}.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                    var look = t.position + t.rotation * new Vector3(0f, 0.08f, -0.08f);
                    eye = t.position + t.rotation * new Vector3(0.05f, 0.18f, 0.36f);
                    BattleDebug.Render($"Logs/BattleShots/10a_gallery_low{suffix}.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                }
            }
            finally
            {
                RealmState.Restored = oldRestored;
                Show(hidden);
                island.world = old;
                island.Rebuild();
                Object.DestroyImmediate(map);
            }
            Debug.Log("[Step10aDebug] Prop gallery in Logs/BattleShots/10a_gallery*.png");
        }
    }
}
