using System.Collections.Generic;
using static Diorama.Audio.EditorTools.Syn;
using static Diorama.Audio.SynthWave;
using static Diorama.Audio.SynthConfidence;

namespace Diorama.Audio.EditorTools
{
    /// <summary>
    /// Step 14d (Nov 3): the realm name card. card.rise: a wooden sign knocks up out of the island, a short rising whoosh,
    /// a warm shimmer and a sparkle as it settles (the logo's family, shorter). card.sink: a soft downward whoosh and a
    /// low wooden settle.
    /// </summary>
    public static partial class SynthRemakes
    {
        /// <summary>The cue ids step 14d added (Step14dSetup bakes only these).</summary>
        public static readonly List<string> Step14Ids = new List<string>();

        static void Step14()
        {
            int before = _list.Count;
            New("Step14", "card.rise", Ok, SynthBatch1.Sfx(0.8f, 30, 1, 0f, 3f), p => p.One(1.6f,
                    Mn("Modal/Wood knock", "G3").Do(l => l.modal.exciterMs = 4f).Gain(0.7f),
                    Mn("Modal/Wood thud (log)", "D3").At(0.04f).Gain(0.45f),
                    Whoosh(-7f, 4f, 0.55f, 0.08f, 0.35f, 650f).Gain(0.45f),
                    Shimmer("D5", 0.25f, 0.9f).At(0.3f).Gain(0.3f),
                    Shimmer("A5", 0.25f, 0.8f, 6f).At(0.42f).Gain(0.22f),
                    Sparkle(0.7f).At(0.5f).Gain(0.5f)));

            New("Step14", "card.sink", Ok, SynthBatch1.Sfx(0.7f, 30, 1, 0f, 3f), p => p.One(1f,
                    Whoosh(4f, -8f, 0.5f, 0.05f, 0.35f, 520f).Gain(0.4f),
                    Mn("Modal/Wood thud (log)", "A2").At(0.45f).Gain(0.4f),
                    M("Particles/Dust fall").At(0.46f).Env(0.01f, 0.25f).Gain(0.18f)));
            Step14Ids.Clear();
            for (int i = before; i < _list.Count; i++) Step14Ids.Add(_list[i].id);
        }
    }
}
