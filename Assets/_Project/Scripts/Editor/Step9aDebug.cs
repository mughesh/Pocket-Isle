using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Claude bridge helpers for step 9a (edit mode). Shots land in Logs/BattleShots.</summary>
    static class Step9aDebug
    {
        static readonly (string name, int x, int y)[] Spots =
        {
            ("arrival", 10, 6), ("forest", 10, 16), ("village", 16, 33), ("springs", 37, 40), ("log", 21, 43),
            ("temple", 20, 52), ("field", 38, 8), ("glade", 37, 21), ("grove", 50, 31), ("ruins", 50, 51),
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

        /// <summary>
        /// Seated shots of each Cinderwood spot through the island window (edit mode, the hub stays the scene's map),
        /// with the island's draw calls, instances and triangles there (outline passes counted).
        /// </summary>
        public static void WindowStats()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
            if (island == null || map == null) return;
            var old = island.world;
            var hidden = HideAll();
            var sb = new StringBuilder("[Step9aDebug] Cinderwood island window (edit mode, unrestored):");
            try
            {
                island.world = map;
                island.Rebuild();
                var t = island.transform;
                foreach (var (name, x, y) in Spots)
                {
                    Vector2 tile = CinderwoodTools.Tile(x, y);
                    island.SetView(tile, tile, 1f);
                    island.Prepare();
                    var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                    BattleDebug.Render($"Logs/BattleShots/9a_{name}.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                    sb.Append($"\n  {name,-8} draws {island.DrawCallCount,3}, instances {island.InstanceTotal,5}, tris {island.TriangleTotal / 1000f:0}k ({island.TriangleBreakdown(4)})");
                }
            }
            finally
            {
                island.world = old;
                island.Rebuild();
                Show(hidden);
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>The same numbers for the hub at its start tile (72 fps on the device, Oct 25): the baseline.</summary>
        public static void HubStats()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var hidden = HideAll();
            try
            {
                island.Rebuild();
                Vector2 tile = island.world.startTile + Vector2Int.down;
                island.SetView(tile, tile, 1f);
                island.Prepare();
                var t = island.transform;
                var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                BattleDebug.Render("Logs/BattleShots/9a_hub_baseline.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                Debug.Log($"[Step9aDebug] hub ({island.world.name}): draws {island.DrawCallCount}, instances {island.InstanceTotal}, tris {island.TriangleTotal / 1000f:0}k ({island.TriangleBreakdown(4)})");
            }
            finally
            {
                Show(hidden);
            }
        }

        /// <summary>
        /// Top-down shot of the whole realm with zone labels: the island footprint is widened to 60 x 60 for one
        /// orthographic render, then put back. Logs/BattleShots/9a_map.png.
        /// </summary>
        public static void MapShot()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
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
                    var at = t.TransformPoint(island.TileToLocal(CinderwoodTools.Tile(x, y), 0.5f));
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
                Label("ARRIVAL + GATE", 10, 4, 1f);
                Label("FOREST TRAIL", 8, 17, 1f);
                Label("EMBER FIELD", 38, 7, 1.2f);
                Label("LANTERN GLADE", 37, 21, 1f);
                Label("KILN VILLAGE", 15, 36, 1.1f);
                Label("SHRINE", 25, 31, 0.8f);
                Label("BERRY GROVE", 51, 37, 1f);
                Label("HOT SPRINGS", 38, 40, 1.1f);
                Label("FALLEN LOG", 21, 43, 0.8f);
                Label("FORGE TEMPLE", 20, 51, 1.1f);
                Label("KILN RUINS", 50, 52, 1f);

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
                System.IO.File.WriteAllBytes("Logs/BattleShots/9a_map.png", tex.EncodeToPNG());
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
            Debug.Log("[Step9aDebug] Logs/BattleShots/9a_map.png");
        }

        /// <summary>Cinderwood as text (north up, y labels left): a prop's key on its anchor, else the terrain.</summary>
        public static void DumpMap()
        {
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldMap>(CinderwoodTools.MapPath);
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
            System.IO.File.WriteAllText("Logs/cinderwood_map.txt", sb.ToString());
            Debug.Log("[Step9aDebug] Logs/cinderwood_map.txt");
        }

        /// <summary>Every 9a prop and both trees on a throwaway 14 x 14 map: a seated shot and a low close one.</summary>
        public static void PropGallery()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null || island.world == null) return;
            var old = island.world;
            var map = ScriptableObject.CreateInstance<WorldMap>();
            map.name = "Gallery";
            map.tileSize = old.tileSize;
            map.capHeight = old.capHeight;
            map.theme = PropKit.Theme("Cinderwood");
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
            void Key(char k, string recipe, float yaw = 0f, bool turned = false) =>
                legend.Add(new PropLegend { key = k.ToString(), recipe = PropKit.Load(recipe), yaw = yaw, turned = turned });
            Key('c', "CliffWall");
            Key('C', "CliffWall", 180f);
            Key('d', "ForgeDoor");
            Key('h', "ClayHut");
            Key('f', "Fence");
            Key('F', "Fence", 0f, true);
            Key('q', "FenceCorner", 90f);
            Key('s', "Signpost");
            Key('w', "Woodpile");
            Key('l', "LeafPile");
            Key('r', "RuinedKiln");
            Key('p', "PotShards");
            Key('e', "EmberVent");
            Key('k', "Kiln");
            for (int c = 1; c <= 3; c++) p[2, c] = c % 2 == 0 ? 'C' : 'c';
            p[2, 4] = 'd';
            for (int c = 7; c <= 12; c++) p[2, c] = c % 2 == 0 ? 'C' : 'c';
            p[5, 2] = 'h';
            p[6, 5] = 'k';
            for (int c = 7; c <= 9; c++) p[6, c] = 'f';
            p[6, 10] = 'q';
            p[7, 10] = 'F';
            p[6, 12] = 's';
            p[9, 2] = 'w';
            p[9, 3] = 'l';
            p[9, 5] = 'r';
            p[9, 6] = 'p';
            p[9, 7] = 'e';
            for (int r = 9; r <= 11; r++)
                for (int c = 9; c <= 12; c++) g[r, c] = IslandMap.Tree;
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

            // Hide every scene renderer (explorer, hands, rings, bubbles): the island draws without renderers.
            // forceRenderingOff is not serialized, so the scene stays clean.
            var hidden = new List<Renderer>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (r.enabled && !r.forceRenderingOff)
                {
                    r.forceRenderingOff = true;
                    hidden.Add(r);
                }
            try
            {
                island.world = map;
                island.Rebuild();
                island.SetView(island.CenteredView, island.CenteredView, 1f);
                var t = island.transform;
                var eye = t.position + t.rotation * new Vector3(0f, 0.5f, 0.42f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery.png", eye, Quaternion.LookRotation(t.position + t.rotation * new Vector3(0f, 0f, 0.02f) - eye, Vector3.up));
                var look = t.position + t.rotation * new Vector3(0f, 0.06f, -0.08f);
                eye = t.position + t.rotation * new Vector3(0.05f, 0.16f, 0.36f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery_low.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
                look = t.position + t.rotation * new Vector3(0.02f, 0.02f, 0.06f);
                eye = t.position + t.rotation * new Vector3(0.02f, 0.13f, 0.3f);
                BattleDebug.Render("Logs/BattleShots/9a_gallery_small.png", eye, Quaternion.LookRotation(look - eye, Vector3.up));
            }
            finally
            {
                foreach (var r in hidden)
                    if (r != null) r.forceRenderingOff = false;
                island.world = old;
                island.Rebuild();
                Object.DestroyImmediate(map);
            }
            Debug.Log("[Step9aDebug] Prop gallery in Logs/BattleShots/9a_gallery*.png");
        }
    }
}
