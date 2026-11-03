using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14f (Mugesh: tiny text on boards is unreadable): one rule for every piece of world-space text. Cap height at
    /// least <see cref="MinCapWorld"/> (9 mm, about 1 degree at the default viewing distance; Nov 3 late, Mugesh: the
    /// first 14 mm made the menus far too big) with the island at its
    /// default scale, growing with the island. TextMeshPro (3D) draws a cap height of about <see cref="CapPerFont"/> per
    /// font size unit with Liberation Sans, so text under the island needs at least <see cref="IslandFont"/> and text in
    /// world space (not under the island) <see cref="WorldFont"/>. High contrast with its background; a dark outline when
    /// it sits over the scene. Lower <see cref="MinCapWorld"/> to shrink every board at once.
    /// </summary>
    public static class TextRule
    {
        public const float MinCapWorld = 0.009f;
        /// <summary>The island's default scale (IslandPlacer.islandScale).</summary>
        public const float DefaultIslandScale = 0.6f;
        /// <summary>Liberation Sans cap height per TMP 3D font size unit (measured 0.0684-0.0691; a little under, so text lands at 14 mm or more).</summary>
        public const float CapPerFont = 0.068f;

        /// <summary>The smallest font size for text under the island (island-local units).</summary>
        public static float IslandFont => MinCapWorld / DefaultIslandScale / CapPerFont;

        /// <summary>The smallest font size for text in world space.</summary>
        public static float WorldFont => MinCapWorld / CapPerFont;

        /// <summary>The smallest capital height for text under the island (island-local metres: baked letters).</summary>
        public static float IslandCap => MinCapWorld / DefaultIslandScale;

        /// <summary>Secondary text (a realm card's line under its name): this share of the rule.</summary>
        public const float Secondary = 0.8f;

        /// <summary>Raises a font size under the island to the rule.</summary>
        public static float Island(float size) => Mathf.Max(size, IslandFont);

        /// <summary>
        /// About how wide <paramref name="text"/> (plain, one line) is at <paramref name="fontSize"/> in the default TMP font
        /// (glyph advances; bold adds a little).
        /// </summary>
        public static float Width(string text, float fontSize, bool bold = false)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            var font = TMP_Settings.defaultFontAsset;
            float em = fontSize * 0.1f;
            if (font == null) return text.Length * 0.55f * em;
            font.TryAddCharacters(text);
            float perUnit = em / Mathf.Max(1f, font.faceInfo.pointSize) * font.faceInfo.scale;
            float w = 0f;
            foreach (char c in text)
            {
                if (font.characterLookupTable.TryGetValue(c, out var ch) && ch.glyph != null) w += ch.glyph.metrics.horizontalAdvance * perUnit;
                else w += 0.55f * em;
                if (bold) w += font.boldSpacing * 0.01f * em;
            }
            return w * (bold ? 1.04f : 1f);
        }
    }
}
