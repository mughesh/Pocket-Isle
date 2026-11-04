using System.Collections.Generic;

namespace Diorama
{
    /// <summary>
    /// Step 14i: the Journal card's text that isn't game data: one short, cozy line per Isling (from the concept
    /// summaries, at most <see cref="MaxLength"/> characters), the type matchups in words and the evolution chain. Pure.
    /// </summary>
    public static class JournalLore
    {
        public const int MaxLength = 70;

        /// <summary>Keyed by the species asset's name.</summary>
        public static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            // Fire
            { "Kilnpot", "A clay-pot cub. Its chimney tail puffs when it's happy." },
            { "Kilnhearth", "Keeps a warm oven in its back. Friends nap against it." },
            { "Toastling", "Smells like fresh bread. Its spines toast darker when proud." },
            { "Crumbhog", "Wears a crown of golden crumbs. Birds follow it everywhere." },
            { "Waxle", "A slow candle snail. It leaves a soft glow where it naps." },
            { "Lanternsnail", "Carries a lantern on its back to light lost travellers home." },
            { "Kettlewing", "Whistles like a kettle when it's excited, which is often." },
            { "Steamswan", "Glides through the hot springs, spout steaming like tea time." },
            { "Glazegolem", "Three pots stacked high. It wobbles, but it never tips over." },
            { "Kilngolem", "A tower of fired pots with an ember heart and a tiny chimney." },
            { "Coalbeetle", "Its horn glows like a coal. Bakers love to have one around." },
            { "Forgehorn", "Taps its glowing horn on stone like a little blacksmith." },
            { "Mapadillo", "Rolls into a ball of maple leaves when someone says hello." },
            { "Emberdillo", "Its leaf plates glow at the edges like an autumn bonfire." },
            { "Ashmole", "Digs warm tunnels under Cinderwood. Its nose is a tiny coal." },
            { "Cindermole", "Wears a coal headlamp to dig. Very proud of its big mitts." },
            { "Cindercap", "A mushroom with lava cracks. Its spores drift up like sparks." },
            { "Cinderbloom", "Its cap opened into a flower of embers. It loves the sun." },
            { "Embercrane", "Stands on one leg by the forge, crest glowing like a candle." },
            { "Pyrecrane", "A proud crane with a crown of flame feathers. Rarely seen." },
            // Water
            { "Pebbloop", "A bubbly baby hippo. It blows bubble rings when it's happy." },
            { "Riverloom", "Turns the waterwheel on its back to keep the river singing." },
            { "Marimoss", "A round moss ball that rolls along the lagoon floor." },
            { "Marimound", "So mossy and round that flowers grow on top of it." },
            { "Drizzcan", "Waters the lily pads with its spout every single morning." },
            { "Rainurn", "Carries rain in its belly and shares it on dry days." },
            { "Lilyvole", "Wears a lily pad hat to keep the sun off its nose." },
            { "Lotusvole", "Its lotus hat blooms pink when it's feeling calm." },
            { "Sailfold", "A little paper boat that loves a gentle breeze." },
            { "Galleyfold", "Three sails and nowhere to be. It drifts wherever it likes." },
            { "Bottlewhale", "A tiny ship sails inside it. Nobody knows how it got there." },
            { "Lighthousewhale", "Its lighthouse glows to guide Islings home through the fog." },
            { "Steambara", "Soaks in the hot springs all day with a fruit on its head." },
            { "Onsenbara", "The calmest Isling. Warm steam follows it everywhere." },
            // Wind
            { "Pufflet", "A fluffy lamb whose seed rotor spins when it's curious." },
            { "Nimbulpaca", "So fluffy it floats a little on windy days. Hold on tight!" },
            { "Kitelet", "A small kite with a bow tail. It loves a strong breeze." },
            { "Kitewyrm", "A friendly kite dragon that dances on the wind." },
            { "Whirlby", "Its pinwheel tail spins faster the happier it gets." },
            { "Windwhirl", "Two pinwheels on its back keep the windmills turning." },
            { "Samarling", "A seed that twirls down from the trees and lands with a giggle." },
            { "Samaroak", "Spins its four wings to hover over the treetops." },
            { "Vanehare", "Its arrow ears always point where the wind is going." },
            { "Compasshare", "Never gets lost. Its compass ears point the way home." },
            { "Zeppelope", "A floaty fawn shaped like a small airship." },
            { "Skyshiplope", "Carries little passengers in its gondola across the sky." },
        };

        public static string Line(IslingSpecies s) => s != null && Lines.TryGetValue(s.name, out var l) ? l : "";

        /// <summary>The element this one's moves hit hard (Fire beats Wind, Wind beats Water, Water beats Fire).</summary>
        public static Element StrongAgainst(Element e) =>
            e == Element.Fire ? Element.Wind : e == Element.Water ? Element.Fire : e == Element.Wind ? Element.Water : Element.Neutral;

        /// <summary>The element that hits this one hard.</summary>
        public static Element WeakAgainst(Element e) =>
            e == Element.Fire ? Element.Water : e == Element.Water ? Element.Wind : e == Element.Wind ? Element.Fire : Element.Neutral;

        /// <summary>
        /// The whole evolution line <paramref name="s"/> belongs to, base first, each with the level it evolves at (0 for
        /// the last form). <paramref name="from"/> gives a form's previous form (the Journal's entries know it).
        /// </summary>
        public static List<(IslingSpecies species, int evolveLevel)> Chain(IslingSpecies s, System.Func<IslingSpecies, IslingSpecies> from)
        {
            var list = new List<(IslingSpecies, int)>();
            if (s == null) return list;
            var root = s;
            for (int guard = 0; guard < 8 && from != null; guard++)
            {
                var prev = from(root);
                if (prev == null) break;
                root = prev;
            }
            var seen = new HashSet<IslingSpecies>();
            for (var f = root; f != null && seen.Add(f); f = f.evolvesTo) list.Add((f, f.evolvesTo != null ? f.evolveLevel : 0));
            return list;
        }

        /// <summary>"Kilnpot › Lv 16 › ???": names the player has met, "???" for the rest.</summary>
        public static string ChainLine(List<(IslingSpecies species, int evolveLevel)> chain, System.Func<IslingSpecies, bool> known)
        {
            if (chain == null || chain.Count < 2) return "Doesn't evolve";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < chain.Count; i++)
            {
                var (sp, lv) = chain[i];
                sb.Append(known != null && known(sp) ? sp.displayName : "???");
                if (i < chain.Count - 1) sb.Append($"  › Lv {lv} ›  ");
            }
            return sb.ToString();
        }
    }
}
