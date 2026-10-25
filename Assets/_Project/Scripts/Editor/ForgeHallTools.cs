using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// The Forge Hall interior (step 9b, placeholder until 9c adds Hearthstag): a 10 x 10 WorldMap of stone floor
    /// (P tiles), dollhouse cutaway like the Lodge (back wall and the back half of the side walls), pillars, the big cold
    /// forge at the back, an anvil, chain posts, dim ember crystals, the door arch on the front edge. The Keeper's spot
    /// (an empty glowing ring, <see cref="ForgeHall"/>) is the middle. Row 0 is the back wall, row 9 the door.
    /// Re-running rewrites it (same files, references survive).
    /// </summary>
    public static class ForgeHallTools
    {
        public const string HallPath = LodgeTools.Dir + "/ForgeHall.asset";
        public const string ThemePath = LodgeTools.Dir + "/ForgeHallTheme.asset";
        public const int Size = 10;
        /// <summary>Where the explorer appears inside (just in from the door, facing the forge).</summary>
        public static readonly Vector2Int EntryTile = new Vector2Int(4, 8);
        public static readonly Vector2Int[] DoorTiles = { new Vector2Int(4, 9), new Vector2Int(5, 9) };
        /// <summary>The Keeper's spot: the middle of the hall (tile units, fractions).</summary>
        public static readonly Vector2 KeeperSpot = new Vector2(4.5f, 4f);

        public static WorldMap WriteHall(StringBuilder log)
        {
            DioramaSceneSetup.EnsureFolder(LodgeTools.Dir);
            var theme = Asset<RealmTheme>(ThemePath);
            var look = new ThemeColors
            {
                grass = Hex("#5E5652"), tallGrass = Hex("#5E5652"), path = Hex("#6E6460"),
                water = Hex("#4E9DB0"), hotSpring = Hex("#E86A2A"),
                shortBase = Hex("#4A3A30"), shortTip = Hex("#7A5A40"), tallBase = Hex("#4A3A30"), tallTip = Hex("#7A5A40"),
                dirt = Hex("#4E4440"), underside = Hex("#3A3230"),
                trunk = Hex("#3A2A22"), leavesLow = Hex("#7A3A22"), leavesHigh = Hex("#A2562E"), rock = Hex("#5E5652"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.restored = theme.unrestored = look;
            theme.treeProp = null;
            theme.treePropAlt = null;
            theme.startRestored = 1f;
            theme.overrideLight = true;
            theme.lightColor = new Color(1f, 0.62f, 0.42f);
            theme.lightIntensity = 0.8f;
            EditorUtility.SetDirty(theme);

            var hall = Asset<WorldMap>(HallPath);
            var rows = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                rows.Append(new string(IslandMap.Path, Size));
                if (r < Size - 1) rows.Append('\n');
            }
            hall.SetLayout(rows.ToString());
            hall.tileSize = 0.05f;
            hall.startTile = EntryTile;
            hall.theme = theme;
            hall.viewZoom = 1.3f;
            WriteProps(hall, log);
            EditorUtility.SetDirty(hall);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {HallPath}: {Size}x{Size} stone floor, zoom {hall.viewZoom}, entry c{EntryTile.x} r{EntryTile.y}; theme {ThemePath}");
            return hall;
        }

        /// <summary>Writes and bakes the props, then the 10 x 10 grid. Multi-tile props: the key sits on their top-left tile.</summary>
        static void WriteProps(WorldMap hall, StringBuilder log)
        {
            var recipes = PropKit.WriteForgeHallProps();
            AssetDatabase.SaveAssets();
            foreach (var r in recipes.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            hall.propLegend = new List<PropLegend>
            {
                Entry('w', recipes["HallWall"]), Entry('v', recipes["HallWall"], true), Entry('F', recipes["HallForge"]),
                Entry('P', recipes["HallPillar"]), Entry('A', recipes["Anvil"]), Entry('c', recipes["HallChains"]),
                Entry('k', recipes["EmberCrystal"]), Entry('D', recipes["HallDoor"]),
            };
            string grid =
                "wwwwF.wwww\n" +
                "vk......kv\n" +
                "v.P....P.v\n" +
                "v........v\n" +
                "v........v\n" +
                "..P....P..\n" +
                "k......A..\n" +
                ".c......c.\n" +
                "..........\n" +
                "....D.....";
            hall.SetProps(grid);
        }

        static PropLegend Entry(char key, PropRecipe recipe, bool turned = false) =>
            new PropLegend { key = key.ToString(), recipe = recipe, turned = turned };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
