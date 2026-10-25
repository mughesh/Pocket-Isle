using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 8: the Still Isle hub map (30 x 30), its props, theme and "visitors" encounter tables. The layout is drawn
    /// from code (deterministic from the seed), then hand-edit the text grids in the Inspector if needed; re-running
    /// overwrites them. Row 0 is the back edge, column 0 the left edge (as the player sees it), +rows face the player.
    /// </summary>
    public static class StillIsleTools
    {
        public const string MapPath = "Assets/_Project/Data/WorldMap_StillIsle.asset";
        public const string GrassTablePath = "Assets/_Project/Data/Battle/Encounters_HubGrass.asset";
        public const string WaterTablePath = "Assets/_Project/Data/Battle/Encounters_HubWater.asset";
        const int Size = 30;

        public static readonly Vector2Int StartTile = new Vector2Int(15, 22);
        public static readonly Vector2Int StatueAnchor = new Vector2Int(14, 16);
        public static readonly Vector2Int ShrineAnchor = new Vector2Int(10, 20);
        public static readonly Vector2Int FireGate = new Vector2Int(2, 15);
        public static readonly Vector2Int WaterGate = new Vector2Int(14, 2);
        public static readonly Vector2Int WindGate = new Vector2Int(26, 15);
        /// <summary>Tall grass tile the opening's gold path leads to.</summary>
        public static readonly Vector2Int FirstGrass = new Vector2Int(21, 22);
        /// <summary>Path tile facing the pond (fishing spot).</summary>
        public static readonly Vector2Int PondSpot = new Vector2Int(6, 22);

        public static WorldMap Write(StringBuilder log)
        {
            var recipes = PropKit.WriteHubProps(out var theme);
            AssetDatabase.SaveAssets();
            foreach (var r in recipes.Values) PropBaker.Bake(r);
            var shrine = PropKit.Load(HearthShrines.RecipeName);
            if (shrine == null) Debug.LogError("[StillIsle] No HearthShrine prop (run Set Up MR_Main (7b lodge) first): the hub has no shrine.");

            var map = Asset<WorldMap>(MapPath);
            map.seed = 808;
            map.tileSize = 0.05f;
            map.viewZoom = 1f;
            map.theme = theme;
            map.startTile = StartTile;
            Draw(map, recipes, shrine);
            map.grassTable = Table(GrassTablePath, Habitat.Grass, ("Toastling", 40f), ("Waxle", 35f), ("Kettlewing", 25f));
            map.waterTable = Table(WaterTablePath, Habitat.Water, ("Kettlewing", 1f));
            map.landmarks = new List<Landmark>
            {
                new Landmark { name = "start", tile = StartTile },
                new Landmark { name = "statue", tile = StatueAnchor + Vector2Int.one },
                new Landmark { name = "firstGrass", tile = FirstGrass },
                new Landmark { name = "pond", tile = PondSpot },
            };
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {MapPath}: {Size}x{Size} Still Isle (statue c{StatueAnchor.x} r{StatueAnchor.y}, shrine c{ShrineAnchor.x} r{ShrineAnchor.y}, " +
                       $"gates fire c{FireGate.x} r{FireGate.y} / water c{WaterGate.x} r{WaterGate.y} / wind c{WindGate.x} r{WindGate.y}, start c{StartTile.x} r{StartTile.y}); " +
                       $"{recipes.Count} hub props written and baked; theme StillIsle; visitors tables Toastling / Waxle / Kettlewing Lv 2-4");
            return map;
        }

        static void Draw(WorldMap map, Dictionary<string, PropRecipe> recipes, PropRecipe shrine)
        {
            var rnd = new SeededRandom(map.seed);
            var g = new char[Size, Size];
            var p = new char[Size, Size];
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    bool border = r == 0 || c == 0 || r == Size - 1 || c == Size - 1;
                    bool ring1 = r == 1 || c == 1 || r == Size - 2 || c == Size - 2;
                    bool ring2 = r == 2 || c == 2 || r == Size - 3 || c == Size - 3;
                    g[r, c] = border ? IslandMap.Blocked : ring1 && rnd.Next() < (r == Size - 2 ? 0.2f : 0.75f) || ring2 && r < Size / 2 && rnd.Next() < 0.25f ? IslandMap.Tree : IslandMap.Grass;
                    p[r, c] = IslandMap.Empty;
                }
            void Set(int c, int r, char ch)
            {
                if (r > 0 && c > 0 && r < Size - 1 && c < Size - 1) g[r, c] = ch;
            }
            void Fill(int c0, int r0, int c1, int r1, char ch)
            {
                for (int r = Mathf.Min(r0, r1); r <= Mathf.Max(r0, r1); r++)
                    for (int c = Mathf.Min(c0, c1); c <= Mathf.Max(c0, c1); c++)
                        Set(c, r, ch);
            }
            void Disc(Vector2 centre, float rx, float ry, char ch, float ragged)
            {
                for (int r = 1; r < Size - 1; r++)
                    for (int c = 1; c < Size - 1; c++)
                    {
                        float dx = (c - centre.x) / rx, dy = (r - centre.y) / ry;
                        float d = dx * dx + dy * dy;
                        if (d <= 1f && (d < 0.6f || rnd.Next() >= ragged)) g[r, c] = ch;
                    }
            }

            // Calm tree clusters and a second, small grass patch at the back (before the paths, which cut through).
            foreach (var (c, r, rad) in new[] { (5, 6, 2), (24, 6, 2), (9, 9, 1), (21, 9, 1), (4, 26, 1), (25, 26, 1) })
                Disc(new Vector2(c, r), rad + 0.4f, rad + 0.4f, IslandMap.Tree, 0.3f);
            Disc(new Vector2(22.5f, 4.5f), 1.8f, 1.4f, IslandMap.TallGrass, 0.2f);

            // Statue plaza (round), the main path to the start, the front path, the gate stubs.
            Disc(new Vector2(StatueAnchor.x + 1, StatueAnchor.y + 1), 3.6f, 3.6f, IslandMap.Path, 0f);
            Fill(15, 19, 15, 23, IslandMap.Path);
            Fill(6, 23, 21, 23, IslandMap.Path);
            Fill(15, WaterGate.y + 1, 15, StatueAnchor.y - 3, IslandMap.Path);
            Fill(WaterGate.x, WaterGate.y + 1, WaterGate.x + 1, WaterGate.y + 1, IslandMap.Path);
            Fill(FireGate.x, FireGate.y + 2, 11, FireGate.y + 2, IslandMap.Path);
            Fill(FireGate.x, FireGate.y + 1, FireGate.x + 1, FireGate.y + 1, IslandMap.Path);
            Fill(19, WindGate.y + 2, WindGate.x + 1, WindGate.y + 2, IslandMap.Path);
            Fill(WindGate.x, WindGate.y + 1, WindGate.x + 1, WindGate.y + 1, IslandMap.Path);

            // First tall grass (right of the start), the pond (left), the shrine's front step.
            Disc(new Vector2(22f, 20.5f), 3f, 2.2f, IslandMap.TallGrass, 0.25f);
            Disc(new Vector2(5f, 20f), 2.8f, 1.9f, IslandMap.Water, 0.2f);
            Fill(PondSpot.x, PondSpot.y, PondSpot.x, 23, IslandMap.Path);
            Fill(PondSpot.x, PondSpot.y - 1, PondSpot.x, PondSpot.y - 1, IslandMap.Water);
            Fill(ShrineAnchor.x, ShrineAnchor.y + 2, ShrineAnchor.x + 1, ShrineAnchor.y + 2, IslandMap.Path);

            // Ground under the multi-tile props (grass, or the plaza for the statue).
            Fill(ShrineAnchor.x, ShrineAnchor.y, ShrineAnchor.x + 1, ShrineAnchor.y + 1, IslandMap.Grass);
            foreach (var gate in new[] { FireGate, WaterGate, WindGate })
                Fill(gate.x, gate.y, gate.x + 1, gate.y, IslandMap.Grass);
            Fill(StartTile.x - 1, StartTile.y - 1, StartTile.x + 1, StartTile.y + 1, IslandMap.Path);

            var legend = new List<PropLegend>();
            void Key(char key, PropRecipe recipe)
            {
                if (recipe != null) legend.Add(new PropLegend { key = key.ToString(), recipe = recipe });
            }
            Key('S', recipes["Statue"]);
            Key('f', recipes["FireGate"]);
            Key('w', recipes["WaterGate"]);
            Key('g', recipes["WindGate"]);
            Key(ShrineTools.ShrineKey, shrine);
            Key('l', recipes["HubLantern"]);
            Key('b', recipes["MossBoulder"]);
            Key('s', recipes["SteppingStones"]);

            p[StatueAnchor.y, StatueAnchor.x] = 'S';
            p[FireGate.y, FireGate.x] = 'f';
            p[WaterGate.y, WaterGate.x] = 'w';
            p[WindGate.y, WindGate.x] = 'g';
            if (shrine != null) p[ShrineAnchor.y, ShrineAnchor.x] = ShrineTools.ShrineKey;

            // Lanterns: plaza corners, beside the start path, beside each gate.
            var lanterns = new[]
            {
                new Vector2Int(12, 13), new Vector2Int(18, 13), new Vector2Int(12, 21), new Vector2Int(18, 21),
                new Vector2Int(FireGate.x + 2, FireGate.y), new Vector2Int(WindGate.x - 1, WindGate.y),
                new Vector2Int(WaterGate.x - 1, WaterGate.y), new Vector2Int(WaterGate.x + 2, WaterGate.y),
            };
            foreach (var t in lanterns)
            {
                Set(t.x, t.y, IslandMap.Grass);
                p[t.y, t.x] = 'l';
            }
            // Stepping stones in the grass between the pond and the shrine.
            foreach (var t in new[] { new Vector2Int(8, 20), new Vector2Int(8, 21), new Vector2Int(9, 22) })
                if (g[t.y, t.x] == IslandMap.Grass) p[t.y, t.x] = 's';

            // Covered tiles of multi-tile props.
            var covered = new HashSet<Vector2Int>();
            foreach (var e in legend)
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        if (p[r, c] == e.Key)
                        {
                            var f = WorldMap.FootprintOf(e);
                            for (int dy = 0; dy < f.y; dy++)
                                for (int dx = 0; dx < f.x; dx++)
                                    covered.Add(new Vector2Int(c + dx, r + dy));
                        }

            // Mossy boulders and a few rocks in open grass, never next to a path.
            bool NearPath(int c, int r)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        char ch = g[Mathf.Clamp(r + dy, 0, Size - 1), Mathf.Clamp(c + dx, 0, Size - 1)];
                        if (ch == IslandMap.Path || ch == IslandMap.Water) return true;
                    }
                return false;
            }
            int boulders = 0, rocks = 0;
            for (int tries = 0; tries < 400 && (boulders < 14 || rocks < 6); tries++)
            {
                int c = 3 + (int)(rnd.Next() * (Size - 6)), r = 3 + (int)(rnd.Next() * (Size - 6));
                if (g[r, c] != IslandMap.Grass || p[r, c] != IslandMap.Empty || covered.Contains(new Vector2Int(c, r)) || NearPath(c, r)) continue;
                if (boulders < 14)
                {
                    p[r, c] = 'b';
                    boulders++;
                }
                else
                {
                    g[r, c] = IslandMap.Rock;
                    rocks++;
                }
            }

            var layout = new StringBuilder(Size * (Size + 1));
            var props = new StringBuilder(Size * (Size + 1));
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    layout.Append(g[r, c]);
                    props.Append(p[r, c]);
                }
                if (r < Size - 1)
                {
                    layout.Append('\n');
                    props.Append('\n');
                }
            }
            map.SetLayout(layout.ToString());
            map.propLegend = legend;
            map.SetProps(props.ToString());
        }

        static EncounterTable Table(string path, Habitat habitat, params (string species, float weight)[] entries)
        {
            var table = Asset<EncounterTable>(path);
            table.habitat = habitat;
            var list = new List<EncounterTable.Entry>();
            foreach (var (id, weight) in entries)
            {
                var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>($"Assets/_Project/Data/Creatures/Species/{id}.asset");
                if (species == null)
                {
                    Debug.LogError($"[StillIsle] Species {id} not found for {path}.");
                    continue;
                }
                list.Add(new EncounterTable.Entry { species = species, minLevel = 2, maxLevel = 4, weight = weight });
            }
            table.entries = list.ToArray();
            EditorUtility.SetDirty(table);
            return table;
        }
    }
}
