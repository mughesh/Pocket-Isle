using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Lilymere's lake temple interior (step 11c, like <see cref="TowerHallTools"/>): a 10 x 10 round flooded hall. The
    /// floor is shallow water (the theme's path colour; walkable), with stepping stones; deep pools with lilies in the
    /// front corners; pale jade walls with rounded corners and a lily frieze, vine-wound pillars, a high window of still
    /// water-light in the back wall; behind Tidemother the hoarded water piled in a glassy dome (drawn by
    /// <see cref="LakeTempleHall"/>); the door arch on the front edge. Row 0 is the back wall, row 9 the door.
    /// </summary>
    public static class LakeTempleTools
    {
        public const string HallPath = LodgeTools.Dir + "/LakeTempleHall.asset";
        public const string ThemePath = LodgeTools.Dir + "/LakeTempleTheme.asset";
        public const int Size = 10;
        public static readonly Vector2Int EntryTile = new Vector2Int(4, 8);
        public static readonly Vector2Int[] DoorTiles = { new Vector2Int(4, 9), new Vector2Int(5, 9) };
        /// <summary>Tidemother in the middle of the hall (tiles, fractions).</summary>
        public static readonly Vector2 KeeperSpot = new Vector2(4.5f, 3.4f);
        /// <summary>The hoarded water dome's middle, behind her.</summary>
        public static readonly Vector2 DomeAt = new Vector2(4.5f, 1.4f);
        /// <summary>The high window's 2 x 1 anchor on the back wall (right of the dome's top).</summary>
        public static readonly Vector2Int WindowAnchor = new Vector2Int(6, 0);

        public static WorldMap WriteHall(StringBuilder log)
        {
            DioramaSceneSetup.EnsureFolder(LodgeTools.Dir);
            var theme = Asset<RealmTheme>(ThemePath);
            var look = new ThemeColors
            {
                grass = Hex("#6FA8A4"), tallGrass = Hex("#6FA8A4"), path = Hex("#7FC6CC"),
                water = Hex("#4F9FB8"), hotSpring = Hex("#4F9FB8"),
                shortBase = Hex("#5E8E88"), shortTip = Hex("#8CC0B4"), tallBase = Hex("#5E8E88"), tallTip = Hex("#8CC0B4"),
                dirt = Hex("#5E7E78"), underside = Hex("#3E5A56"),
                trunk = Hex("#6E5236"), leavesLow = Hex("#5AA868"), leavesHigh = Hex("#7CC27E"), rock = Hex("#9EC2B4"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.restored = theme.unrestored = look;
            theme.treeProp = null;
            theme.treePropAlt = null;
            theme.startRestored = 1f;
            theme.overrideLight = true;
            theme.lightColor = new Color(0.82f, 0.95f, 1f);
            theme.lightIntensity = 0.85f;
            EditorUtility.SetDirty(theme);

            var hall = Asset<WorldMap>(HallPath);
            // Shallow water floor everywhere (walkable), deep pools in the front corners for the lilies.
            var rows = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                var line = new char[Size];
                for (int c = 0; c < Size; c++)
                    line[c] = (r >= 6 && r <= 7 && (c <= 1 || c >= 8)) ? IslandMap.Water : IslandMap.Path;
                rows.Append(line);
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
            log.Append($"\n  {HallPath}: {Size}x{Size} flooded hall, zoom {hall.viewZoom}, entry c{EntryTile.x} r{EntryTile.y}; theme {ThemePath}");
            return hall;
        }

        static void WriteProps(WorldMap hall, StringBuilder log)
        {
            var recipes = PropKit.WriteLakeTempleProps();
            AssetDatabase.SaveAssets();
            foreach (var r in recipes.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            PropRecipe Lily(string name) => PropKit.Load(name);
            hall.propLegend = new List<PropLegend>
            {
                Entry('w', recipes["TempleWall"]), Entry('v', recipes["TempleWall"], true),
                Entry('q', recipes["TempleCornerL"]), Entry('r', recipes["TempleCornerR"]),
                Entry('W', recipes["TempleWindow"]), Entry('P', recipes["TemplePillar"]), Entry('D', recipes["TempleHallDoor"]),
                Entry('l', Lily("LilyPad")), Entry('o', Lily("Lotus")), Entry('s', Lily("SteppingStone")),
            };
            string grid =
                "qwwwwwW.wr\n" +
                "v........v\n" +
                "v........v\n" +
                "v........v\n" +
                "v.s....s.v\n" +
                "..P....P..\n" +
                "lo......ol\n" +
                "o.s....s.l\n" +
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
