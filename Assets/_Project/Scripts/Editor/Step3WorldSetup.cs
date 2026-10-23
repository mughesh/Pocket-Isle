using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 3: the test world map and the world window in MR_Main.
    /// The generator is deterministic from the WorldMap's seed; later realms use it as a base, then get hand-placed
    /// story tiles in the text grid.
    /// </summary>
    static class Step3WorldSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string WorldPath = "Assets/_Project/Data/WorldMap_Test.asset";
        const int Size = 56;

        [MenuItem("Tools/Diorama/Step 3/1. Generate Test World")]
        static void GenerateMenu()
        {
            var world = LoadOrCreateWorld();
            Selection.activeObject = world;
            EditorGUIUtility.PingObject(world);
        }

        [MenuItem("Tools/Diorama/Step 3/2. Add World Window to MR_Main")]
        static void AddWindowMenu()
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
            var builder = Object.FindFirstObjectByType<IslandBuilder>();
            var piece = Object.FindFirstObjectByType<GestureTestPiece>();
            if (builder == null || piece == null)
            {
                Debug.LogError("[Diorama] MR_Main is missing the island or the gesture test piece.");
                return;
            }
            var world = AssetDatabase.LoadAssetAtPath<WorldMap>(WorldPath);
            if (world == null) world = LoadOrCreateWorld();

            Undo.RecordObject(builder, "World window");
            builder.world = world;
            EditorUtility.SetDirty(builder);

            if (!builder.TryGetComponent(out WorldWindow window)) window = Undo.AddComponent<WorldWindow>(builder.gameObject);
            window.island = builder;
            EditorUtility.SetDirty(window);

            Undo.RecordObject(piece, "World window");
            piece.window = window;
            EditorUtility.SetDirty(piece);

            EditorSceneManager.SaveScene(active);
            Debug.Log($"[Diorama] World window added to MR_Main: {world.Columns}x{world.Rows} map, start c{world.startTile.x} r{world.startTile.y}.");
        }

        static WorldMap LoadOrCreateWorld()
        {
            var world = AssetDatabase.LoadAssetAtPath<WorldMap>(WorldPath);
            bool created = world == null;
            if (created)
            {
                DioramaSceneSetup.EnsureFolder("Assets/_Project/Data");
                world = ScriptableObject.CreateInstance<WorldMap>();
            }
            Generate(world);
            if (created) AssetDatabase.CreateAsset(world, WorldPath);
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();
            return world;
        }

        // ---- generator ----

        static void Generate(WorldMap world)
        {
            var rnd = new SeededRandom(world.seed);
            var g = new char[Size, Size]; // [row, column]
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    g[r, c] = r == 0 || c == 0 || r == Size - 1 || c == Size - 1 ? IslandMap.Blocked : IslandMap.Grass;

            // Winding paths through waypoints spread over the map, as a loop with a spur to the middle.
            var waypoints = new List<Vector2Int>();
            Vector2Int[] anchors =
            {
                new Vector2Int(9, 9), new Vector2Int(28, 7), new Vector2Int(46, 10), new Vector2Int(47, 28),
                new Vector2Int(45, 46), new Vector2Int(28, 48), new Vector2Int(9, 46), new Vector2Int(8, 27),
            };
            foreach (var a in anchors) waypoints.Add(a + new Vector2Int(Jitter(rnd, 3), Jitter(rnd, 3)));
            for (int i = 0; i < waypoints.Count; i++) CarvePath(g, rnd, waypoints[i], waypoints[(i + 1) % waypoints.Count]);
            var middle = new Vector2Int(Size / 2 + Jitter(rnd, 2), Size / 2 + Jitter(rnd, 2));
            CarvePath(g, rnd, waypoints[7], middle);
            CarvePath(g, rnd, middle, waypoints[3]);

            // Pond: the candidate spot that crosses the fewest path tiles.
            Vector2Int pond = default;
            int pondRx = 3 + (int)(rnd.Next() * 3), pondRy = 2 + (int)(rnd.Next() * 3), best = int.MaxValue;
            for (int k = 0; k < 24; k++)
            {
                var p = new Vector2Int(10 + (int)(rnd.Next() * (Size - 20)), 10 + (int)(rnd.Next() * (Size - 20)));
                int hits = CountInBlob(g, p, pondRx, pondRy, IslandMap.Path);
                if (hits < best)
                {
                    best = hits;
                    pond = p;
                }
            }
            Blob(g, rnd, pond, pondRx, pondRy, 1f, IslandMap.Water);

            // Tall grass patches beside the paths.
            int patches = 4 + (int)(rnd.Next() * 3);
            var pathCells = Cells(g, IslandMap.Path);
            for (int i = 0; i < patches && pathCells.Count > 0; i++)
            {
                var at = pathCells[(int)(rnd.Next() * pathCells.Count)];
                var offset = new Vector2Int(Jitter(rnd, 4), Jitter(rnd, 4));
                Blob(g, rnd, at + offset, 2 + (int)(rnd.Next() * 3), 2 + (int)(rnd.Next() * 2), 1f, IslandMap.TallGrass);
            }

            // Tree clusters, plus a ragged forest edge inside the border.
            int clusters = 10 + (int)(rnd.Next() * 5);
            for (int i = 0; i < clusters; i++)
            {
                var at = new Vector2Int(3 + (int)(rnd.Next() * (Size - 6)), 3 + (int)(rnd.Next() * (Size - 6)));
                int rad = 1 + (int)(rnd.Next() * 3);
                Blob(g, rnd, at, rad, rad, 0.75f, IslandMap.Tree);
            }
            for (int r = 1; r < Size - 1; r++)
                for (int c = 1; c < Size - 1; c++)
                {
                    bool edge = r == 1 || c == 1 || r == Size - 2 || c == Size - 2;
                    if (edge && g[r, c] == IslandMap.Grass && rnd.Next() < 0.6f) g[r, c] = IslandMap.Tree;
                }

            // Scattered rocks.
            int rocks = 20 + (int)(rnd.Next() * 11);
            for (int i = 0; i < rocks; i++)
            {
                int r = 2 + (int)(rnd.Next() * (Size - 4)), c = 2 + (int)(rnd.Next() * (Size - 4));
                if (g[r, c] == IslandMap.Grass) g[r, c] = IslandMap.Rock;
            }

            // Start on the path tile nearest the middle.
            var start = middle;
            float bestDist = float.MaxValue;
            foreach (var p in Cells(g, IslandMap.Path))
            {
                float d = (p - new Vector2Int(Size / 2, Size / 2)).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    start = p;
                }
            }

            var sb = new StringBuilder(Size * (Size + 1));
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++) sb.Append(g[r, c]);
                if (r < Size - 1) sb.Append('\n');
            }
            world.SetLayout(sb.ToString());
            world.startTile = start;
            Debug.Log($"[Diorama] Generated {WorldPath} (seed {world.seed}): {Size}x{Size}, {patches} tall grass patches, " +
                      $"{clusters} tree clusters, pond at c{pond.x} r{pond.y}, start c{start.x} r{start.y}.");
        }

        static int Jitter(SeededRandom rnd, int range) => Mathf.RoundToInt((rnd.Next() * 2f - 1f) * range);

        static bool Interior(Vector2Int p) => p.x > 0 && p.y > 0 && p.x < Size - 1 && p.y < Size - 1;

        /// <summary>Random walk biased toward the target, with sideways wiggles.</summary>
        static void CarvePath(char[,] g, SeededRandom rnd, Vector2Int from, Vector2Int to)
        {
            var p = from;
            for (int guard = 0; p != to && guard < Size * Size; guard++)
            {
                if (Interior(p)) g[p.y, p.x] = IslandMap.Path;
                Vector2Int d = to - p;
                bool alongX = Mathf.Abs(d.x) > 0 && (d.y == 0 || rnd.Next() < Mathf.Abs(d.x) / (float)(Mathf.Abs(d.x) + Mathf.Abs(d.y)));
                Vector2Int step = alongX ? new Vector2Int((int)Mathf.Sign(d.x), 0) : new Vector2Int(0, (int)Mathf.Sign(d.y));
                if (rnd.Next() < 0.2f) step = new Vector2Int(step.y, step.x) * (rnd.Next() < 0.5f ? 1 : -1);
                var next = p + step;
                if (next.x > 1 && next.y > 1 && next.x < Size - 2 && next.y < Size - 2) p = next;
            }
            if (Interior(to)) g[to.y, to.x] = IslandMap.Path;
        }

        /// <summary>Fills a noisy ellipse; only grass is replaced, so paths and water stay.</summary>
        static void Blob(char[,] g, SeededRandom rnd, Vector2Int center, int rx, int ry, float density, char tile)
        {
            for (int r = center.y - ry - 1; r <= center.y + ry + 1; r++)
                for (int c = center.x - rx - 1; c <= center.x + rx + 1; c++)
                {
                    if (!Interior(new Vector2Int(c, r))) continue;
                    float dx = (c - center.x) / (rx + 0.5f), dy = (r - center.y) / (ry + 0.5f);
                    float noise = rnd.Next() * 0.35f;
                    if (dx * dx + dy * dy > 1f - noise + 0.15f || rnd.Next() > density) continue;
                    if (g[r, c] == IslandMap.Grass) g[r, c] = tile;
                }
        }

        static int CountInBlob(char[,] g, Vector2Int center, int rx, int ry, char tile)
        {
            int n = 0;
            for (int r = center.y - ry - 1; r <= center.y + ry + 1; r++)
                for (int c = center.x - rx - 1; c <= center.x + rx + 1; c++)
                    if (Interior(new Vector2Int(c, r)) && g[r, c] == tile) n++;
            return n;
        }

        static List<Vector2Int> Cells(char[,] g, char tile)
        {
            var list = new List<Vector2Int>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (g[r, c] == tile) list.Add(new Vector2Int(c, r));
            return list;
        }
    }
}
