using UnityEngine;

namespace Diorama
{
    /// <summary>Colours per element for runes and effects (from the prototype's fire / water / gust effects).</summary>
    public readonly struct ElementStyle
    {
        public readonly Color main, core, edge, trail, burst, shield;

        ElementStyle(string main, string core, string edge, string trail, string burst, string shield)
        {
            this.main = Hex(main);
            this.core = Hex(core);
            this.edge = Hex(edge);
            this.trail = Hex(trail);
            this.burst = Hex(burst);
            this.shield = Hex(shield);
        }

        static readonly ElementStyle Fire = new ElementStyle("#E8632A", "#FFF2A8", "#FF4A12", "#FF8A2A", "#FFB347", "#FFB36B");
        static readonly ElementStyle Water = new ElementStyle("#3C8FE0", "#EAFCFF", "#2F86E0", "#7CC8FF", "#9FDCFF", "#8FE0FF");
        static readonly ElementStyle Wind = new ElementStyle("#4CBF93", "#F2FFF9", "#5FD6A8", "#D8FFF2", "#E6FFF6", "#C9F7E4");
        static readonly ElementStyle Neutral = new ElementStyle("#B8AD9C", "#FFFFFF", "#D8CFC0", "#FFF3E0", "#FFFFFF", "#F3EAD8");

        public static ElementStyle Of(Element e) => e switch
        {
            Element.Fire => Fire,
            Element.Water => Water,
            Element.Wind => Wind,
            _ => Neutral,
        };

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
