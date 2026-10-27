using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Galewick's tower hall interior (step 10e, like <see cref="ForgeHallTools"/>): a 10 x 10 WorldMap of cool stone
    /// floor, dollhouse cutaway (back wall with rounded corners and the back half of the side walls), slim pillars, beams
    /// out of the back wall with tangled kite strings hanging, heaps of tangled kites, the big round window in the middle
    /// in the back wall beside Galeowl's empty perch, the door arch on the front edge. Row 0 is the back wall,
    /// row 9 the door. Re-running rewrites it (same files, references survive).
    /// </summary>
    public static class TowerHallTools
    {
        public const string HallPath = LodgeTools.Dir + "/TowerHall.asset";
        public const string ThemePath = LodgeTools.Dir + "/TowerHallTheme.asset";
        public const int Size = 10;
        public static readonly Vector2Int EntryTile = new Vector2Int(4, 8);
        public static readonly Vector2Int[] DoorTiles = { new Vector2Int(4, 9), new Vector2Int(5, 9) };
        /// <summary>Galeowl on its perch (tile units, fractions; the perch bar runs along row ~2.55).</summary>
        public static readonly Vector2 KeeperSpot = new Vector2(4.5f, 2.6f);
        /// <summary>The window's 2 x 1 anchor (top-left tile) on the back wall, right of the perch so Galeowl does not hide it.</summary>
        public static readonly Vector2Int WindowAnchor = new Vector2Int(6, 0);
        /// <summary>The perch bar's height in tiles (Galeowl sits on it).</summary>
        public const float PerchHeight = 0.5f;

        public static WorldMap WriteHall(StringBuilder log)
        {
            DioramaSceneSetup.EnsureFolder(LodgeTools.Dir);
            var theme = Asset<RealmTheme>(ThemePath);
            var look = new ThemeColors
            {
                grass = Hex("#767C82"), tallGrass = Hex("#767C82"), path = Hex("#80868C"),
                water = Hex("#6FB3C8"), hotSpring = Hex("#6FB3C8"),
                shortBase = Hex("#5E646A"), shortTip = Hex("#8A9096"), tallBase = Hex("#5E646A"), tallTip = Hex("#8A9096"),
                dirt = Hex("#6A6E72"), underside = Hex("#4A4E52"),
                trunk = Hex("#6E5236"), leavesLow = Hex("#5E8A6A"), leavesHigh = Hex("#7AA884"), rock = Hex("#8E949A"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.restored = theme.unrestored = look;
            theme.treeProp = null;
            theme.treePropAlt = null;
            theme.startRestored = 1f;
            theme.overrideLight = true;
            theme.lightColor = new Color(0.84f, 0.9f, 1f);
            theme.lightIntensity = 0.85f;
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

        /// <summary>Writes and bakes the props, then the grid. Multi-tile props: the key sits on their top-left tile.</summary>
        static void WriteProps(WorldMap hall, StringBuilder log)
        {
            var recipes = PropKit.WriteTowerHallProps();
            AssetDatabase.SaveAssets();
            foreach (var r in recipes.Values)
            {
                var report = PropBaker.Bake(r);
                log.Append($"\n  {r.name}: {report.triangles} tris (budget {r.triangleBudget})");
            }
            hall.propLegend = new List<PropLegend>
            {
                Entry('w', recipes["TowerWall"]), Entry('v', recipes["TowerWall"], true),
                Entry('q', recipes["TowerCornerL"]), Entry('r', recipes["TowerCornerR"]),
                Entry('W', recipes["TowerWindow"]), Entry('P', recipes["TowerPillar"]), Entry('B', recipes["TowerBeam"]),
                Entry('K', recipes["TangledKites"]), Entry('p', recipes["TowerPerch"]), Entry('D', recipes["TowerHallDoor"]),
            };
            string grid =
                "qwwwwwW.wr\n" +
                "v.B.....Bv\n" +
                "v...p....v\n" +
                "v........v\n" +
                "v.......Kv\n" +
                "..P....P..\n" +
                "K.........\n" +
                "..........\n" +
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
