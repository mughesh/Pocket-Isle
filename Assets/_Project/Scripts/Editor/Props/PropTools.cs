using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 5b: dresses the test world as Cinderwood (theme, a few of each prop near spawn, a hot spring, a fallen log
    /// across a path) and wires MR_Main (prop material, hot spring cap and steam, realm restoration with the G key).
    /// </summary>
    public static class PropTools
    {
        const string ScenePath = "Assets/_Project/Scenes/MR_Main.unity";
        const string WorldPath = "Assets/_Project/Data/WorldMap_Test.asset";

        // Props grid characters.
        const char Kiln = 'K', Lantern = 'L', Brazier = 'B', BrazierLit = 'b', Bush = 'U', Tree = 'A',
                   Log = 'O', LogTurned = 'o', StoneLantern = 'S', Pots = 'J';

        [MenuItem("Tools/Pocket Isle/Props/Dress Test World", priority = 40)]
        public static void Dress()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[PropTools] Stop Play mode first (Dress Test World edits and saves MR_Main).");
                return;
            }
            var active = EditorSceneManager.GetActiveScene();
            if (active.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                active = EditorSceneManager.OpenScene(ScenePath);
            }
            var world = AssetDatabase.LoadAssetAtPath<WorldMap>(WorldPath);
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (world == null || island == null)
            {
                Debug.LogError("[PropTools] Needs Data/WorldMap_Test and the island in MR_Main (Step 3 menus).");
                return;
            }

            if (!PropKit.Exists) PropKit.Write();
            PropBaker.BakeAll();

            var report = new StringBuilder("[PropTools] Dressed the test world as Cinderwood:");
            world.theme = PropKit.Theme("Cinderwood");
            world.propLegend = new List<PropLegend>
            {
                Entry(Kiln, "Kiln"), Entry(Lantern, "LanternPost"), Entry(Brazier, "Brazier"), Entry(BrazierLit, "BrazierLit"),
                Entry(Bush, "BerryBush"), Entry(Tree, "AutumnTree"), Entry(Log, "FallenLog"), Entry(LogTurned, "FallenLog", true),
                Entry(StoneLantern, "StoneLantern"), Entry(Pots, "PotCluster"),
            };

            int rows = world.Rows, cols = world.Columns;
            var terrain = new char[rows, cols];
            var props = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    char t = world.At(r, c);
                    terrain[r, c] = t == IslandMap.HotSpring ? IslandMap.Grass : t; // re-dressing moves the spring
                    props[r, c] = IslandMap.Empty;
                }

            var start = world.startTile;
            // Keep the start, the starters' spots (CreatureTools.AddStartersToMain) and the tiles around them free.
            var reserved = new HashSet<Vector2Int>();
            void Reserve(Vector2Int t, int radius)
            {
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                        reserved.Add(t + new Vector2Int(dx, dy));
            }
            Reserve(start, 1);
            foreach (var mc in Object.FindObjectsByType<MapCreature>(FindObjectsSortMode.None)) Reserve(mc.homeTile, 1);
            foreach (var o in new[] { new Vector2Int(2, -2), new Vector2Int(0, -3), new Vector2Int(-2, -2) }) Reserve(start + o, 0);

            bool In(Vector2Int t) => t.x > 1 && t.y > 1 && t.x < cols - 2 && t.y < rows - 2;
            char T(Vector2Int t) => In(t) ? terrain[t.y, t.x] : IslandMap.Blocked;
            bool Free(Vector2Int t) => In(t) && terrain[t.y, t.x] == IslandMap.Grass && props[t.y, t.x] == IslandMap.Empty && !reserved.Contains(t);
            int Dist(Vector2Int t) => Mathf.Max(Mathf.Abs(t.x - start.x), Mathf.Abs(t.y - start.y));

            // Hot spring: the nearest 3x2 patch of free grass, 3 to 7 tiles from the start.
            Vector2Int? spring = null;
            for (int d = 3; d <= 7 && spring == null; d++)
                foreach (var t in Ring(start, d))
                {
                    bool ok = true;
                    for (int dy = -1; dy <= 2 && ok; dy++)
                        for (int dx = -1; dx <= 3 && ok; dx++)
                        {
                            var q = t + new Vector2Int(dx, dy);
                            bool inside = dx >= 0 && dx < 3 && dy >= 0 && dy < 2;
                            // The spring itself on free grass, with a ring of anything but path around it.
                            ok = inside ? Free(q) : In(q) && T(q) != IslandMap.Path && !reserved.Contains(q);
                        }
                    if (!ok) continue;
                    spring = t;
                    break;
                }
            if (spring.HasValue)
            {
                for (int dy = 0; dy < 2; dy++)
                    for (int dx = 0; dx < 3; dx++)
                    {
                        var q = spring.Value + new Vector2Int(dx, dy);
                        terrain[q.y, q.x] = IslandMap.HotSpring;
                    }
                Reserve(spring.Value + new Vector2Int(1, 0), 2); // keep props off its rim
                report.Append($"\n  hot spring 3x2 at c{spring.Value.x} r{spring.Value.y}");
            }
            else report.Append("\n  WARNING: no room for a hot spring near the start");

            // Fallen log across a straight bit of path, 3 to 7 tiles from the start; ahead of the player first.
            bool logPlaced = false;
            var pathTiles = new List<Vector2Int>();
            for (int d = 3; d <= 7; d++) pathTiles.AddRange(Ring(start, d).Where(t => T(t) == IslandMap.Path && !reserved.Contains(t)));
            foreach (var p in pathTiles.OrderBy(t => t.y < start.y ? 0 : 1).ThenBy(Dist))
            {
                bool vertical = T(p + Vector2Int.up) == IslandMap.Path && T(p + Vector2Int.down) == IslandMap.Path
                                && T(p + Vector2Int.left) != IslandMap.Path && T(p + Vector2Int.right) != IslandMap.Path;
                bool horizontal = T(p + Vector2Int.left) == IslandMap.Path && T(p + Vector2Int.right) == IslandMap.Path
                                  && T(p + Vector2Int.up) != IslandMap.Path && T(p + Vector2Int.down) != IslandMap.Path;
                // The log lies across the path: along columns over a vertical path, along rows over a horizontal one.
                var side = vertical ? Vector2Int.right : Vector2Int.up;
                if (!vertical && !horizontal) continue;
                Vector2Int anchor;
                if (Free(p + side)) anchor = p;
                else if (Free(p - side)) anchor = p - side;
                else continue;
                props[anchor.y, anchor.x] = vertical ? Log : LogTurned;
                Reserve(p, 1);
                report.Append($"\n  fallen log across the path at c{p.x} r{p.y} ({(vertical ? "path runs along rows" : "path runs along columns")})");
                logPlaced = true;
                break;
            }
            if (!logPlaced) report.Append("\n  WARNING: no straight path near the start for the fallen log");

            // The rest: a few of each, 2 to 6 tiles from the start, spaced out so walking routes stay open.
            var rnd = new SeededRandom(world.seed + 5);
            var candidates = new List<Vector2Int>();
            for (int d = 2; d <= 6; d++) candidates.AddRange(Ring(start, d));
            candidates = candidates.OrderBy(_ => rnd.Next()).ToList();
            bool NearPath(Vector2Int t) => Sides.Any(s => T(t + s) == IslandMap.Path);
            bool Spaced(Vector2Int t)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (In(t + new Vector2Int(dx, dy)) && props[t.y + dy, t.x + dx] != IslandMap.Empty) return false;
                return true;
            }
            var counts = new Dictionary<char, int>();
            void Place(char key, int count, System.Func<Vector2Int, bool> prefer)
            {
                int placed = 0;
                foreach (var pass in new[] { true, false })
                    foreach (var t in candidates)
                    {
                        if (placed >= count) break;
                        if (!Free(t) || !Spaced(t) || pass && !prefer(t)) continue;
                        props[t.y, t.x] = key;
                        placed++;
                    }
                counts[key] = placed;
            }
            Place(Kiln, 2, t => !NearPath(t));
            Place(Lantern, 3, NearPath);
            Place(StoneLantern, 3, NearPath);
            Place(BrazierLit, 1, NearPath);
            Place(Brazier, 1, NearPath);
            Place(Bush, 3, t => !NearPath(t));
            Place(Tree, 3, t => !NearPath(t));
            Place(Pots, 2, t => true);
            report.Append("\n  " + string.Join(", ", counts.Select(kv => $"{kv.Value}x {world.propLegend.First(e => e.Key == kv.Key).recipe.name}")));

            world.SetLayout(Grid(terrain));
            world.SetProps(Grid(props));
            EditorUtility.SetDirty(world);
            AssetDatabase.SaveAssets();

            WireScene(island, report);
            CreatureTools.RefreshLabSpecies();
            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log(report.ToString());
        }

        /// <summary>Prop material, hot spring cap and steam on the island; realm restoration (G key) beside it.</summary>
        static void WireScene(IslandBuilder island, StringBuilder report)
        {
            Undo.RecordObject(island, "Props");
            island.propMaterial = PropBaker.PropMaterial();
            var cap = DioramaSceneSetup.ToonMaterial("Island_CapHotSpring", "#5FC4B4");
            cap.SetFloat("_OriginJitter", 0.04f);
            EditorUtility.SetDirty(cap);
            island.capHotSpring = cap;
            island.hotSpringSteam = PropKit.Load("HotSpringSteam");
            EditorUtility.SetDirty(island);

            if (!island.TryGetComponent(out RealmRestoration restoration)) restoration = Undo.AddComponent<RealmRestoration>(island.gameObject);
            restoration.island = island;
            restoration.input = Object.FindFirstObjectByType<MicrogestureInput>();
            EditorUtility.SetDirty(restoration);
            AssetDatabase.SaveAssets();
            island.Rebuild();
            report.Append($"\n  MR_Main: prop material, hot spring cap + steam, RealmRestoration on {island.name}" +
                          $"{(restoration.input == null ? " (no MicrogestureInput found: no device toggle)" : "")}");
        }

        static readonly Vector2Int[] Sides = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        static PropLegend Entry(char key, string recipe, bool turned = false)
        {
            var r = PropKit.Load(recipe);
            if (r == null) Debug.LogError($"[PropTools] Prop recipe {recipe} is missing (Write Prop Kit).");
            return new PropLegend { key = key.ToString(), recipe = r, turned = turned };
        }

        /// <summary>Tiles at exactly Chebyshev distance d around a centre, row by row.</summary>
        static IEnumerable<Vector2Int> Ring(Vector2Int center, int d)
        {
            for (int dy = -d; dy <= d; dy++)
                for (int dx = -d; dx <= d; dx++)
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == d) yield return center + new Vector2Int(dx, dy);
        }

        static string Grid(char[,] g)
        {
            int rows = g.GetLength(0), cols = g.GetLength(1);
            var sb = new StringBuilder(rows * (cols + 1));
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++) sb.Append(g[r, c]);
                if (r < rows - 1) sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
