using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>
    /// The Hearth Lodge interior (step 7b): an 8 x 8 WorldMap of wooden floor (P tiles) and its warm Lodge theme
    /// (wood floor, warm stone base, hearth light). Row 0 is the back wall, row 7 the open front with the door.
    /// Re-running rewrites both (same files, references survive).
    /// </summary>
    static class LodgeTools
    {
        public const string Dir = "Assets/_Project/Data/Interiors";
        public const string LodgePath = Dir + "/Lodge.asset";
        public const string ThemePath = Dir + "/LodgeTheme.asset";
        public const int Size = 8;
        /// <summary>Where the explorer appears inside (just in from the door, facing the hearth).</summary>
        public static readonly Vector2Int EntryTile = new Vector2Int(3, 6);

        public static WorldMap WriteLodge(StringBuilder log)
        {
            DioramaSceneSetup.EnsureFolder(Dir);
            var theme = Asset<RealmTheme>(ThemePath);
            var look = new ThemeColors
            {
                grass = Hex("#B98A5E"), tallGrass = Hex("#B98A5E"), path = Hex("#B98A5E"),
                water = Hex("#4E9DB0"), hotSpring = Hex("#5FC4B4"),
                shortBase = Hex("#9C5A24"), shortTip = Hex("#E9B35A"), tallBase = Hex("#7E2E1C"), tallTip = Hex("#E5641E"),
                dirt = Hex("#8E6E54"), underside = Hex("#5E4A3E"),
                trunk = Hex("#4A3328"), leavesLow = Hex("#B5452A"), leavesHigh = Hex("#D98E32"), rock = Hex("#A39A8E"),
                leafGlow = 0f, outlineTint = Color.white,
            };
            theme.restored = theme.unrestored = look;
            theme.treeProp = null;
            theme.startRestored = 1f;
            theme.overrideLight = true;
            theme.lightColor = new Color(1f, 0.83f, 0.64f);
            theme.lightIntensity = 1.05f;
            EditorUtility.SetDirty(theme);

            var lodge = Asset<WorldMap>(LodgePath);
            var rows = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                rows.Append(new string(IslandMap.Path, Size));
                if (r < Size - 1) rows.Append('\n');
            }
            lodge.SetLayout(rows.ToString());
            lodge.tileSize = 0.05f;
            lodge.startTile = EntryTile;
            lodge.theme = theme;
            lodge.viewZoom = 1.6f;
            WriteProps(lodge, log);
            EditorUtility.SetDirty(lodge);
            AssetDatabase.SaveAssets();
            log.Append($"\n  {LodgePath}: {Size}x{Size} wooden floor, zoom {lodge.viewZoom}, entry c{EntryTile.x} r{EntryTile.y}; theme {ThemePath}");
            return lodge;
        }

        /// <summary>Props grid (none yet; the lodge room section fills it).</summary>
        static void WriteProps(WorldMap lodge, StringBuilder log)
        {
            var empty = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                empty.Append(new string(IslandMap.Empty, Size));
                if (r < Size - 1) empty.Append('\n');
            }
            lodge.propLegend = new List<PropLegend>();
            lodge.SetProps(empty.ToString());
        }

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
