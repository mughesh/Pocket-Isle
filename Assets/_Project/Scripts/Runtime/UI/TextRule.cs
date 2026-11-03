using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14g (Nov 4, replaces 14f's minimum-size rule, which grew whole boards): a label uses the largest font that fits
    /// its element at the element's own size, down to <see cref="MinShare"/> of it; a label that still doesn't fit is
    /// shortened in the data. Boards never grow for their text.
    /// </summary>
    public static class TextRule
    {
        /// <summary>The smallest a label may shrink to, as a share of its full size.</summary>
        public const float MinShare = 0.6f;

        /// <summary>Lets <paramref name="t"/> shrink (never grow) from <paramref name="size"/> to fit its box.</summary>
        public static void Fit(TextMeshPro t, float size)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * MinShare;
            t.fontSize = size;
        }
    }
}
