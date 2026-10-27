using UnityEngine;

namespace Diorama
{
    /// <summary>Procedural placeholder sounds for the hand puzzles (step 9b) until the audio step: built once, shared.</summary>
    public static class PuzzleSounds
    {
        const int Rate = 44100;
        static AudioClip _thud, _puff, _whoosh, _rumble, _breeze;

        /// <summary>A heavy wooden thud: low knock plus a short noisy body.</summary>
        public static AudioClip Thud => _thud != null ? _thud : _thud = Make("Puzzle Thud", 0.55f, (t, n) =>
            Mathf.Exp(-t * 11f) * (Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.9f + Mathf.Sin(2f * Mathf.PI * 97f * t) * 0.4f) +
            Mathf.Exp(-t * 40f) * n * 0.5f);

        /// <summary>A bellows puff: a soft breath of noise.</summary>
        public static AudioClip Puff => _puff != null ? _puff : _puff = Make("Puzzle Puff", 0.35f, (t, n) =>
            n * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.35f)) * 0.45f);

        /// <summary>A flame catching: noise swelling and fading with a low roar.</summary>
        public static AudioClip Whoosh => _whoosh != null ? _whoosh : _whoosh = Make("Puzzle Whoosh", 0.8f, (t, n) =>
        {
            float env = Mathf.Min(1f, t / 0.08f) * Mathf.Exp(-t * 3.5f);
            return env * (n * 0.6f + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.3f);
        });

        /// <summary>Stone grinding: a slow low rumble (the forge door).</summary>
        public static AudioClip Rumble => _rumble != null ? _rumble : _rumble = Make("Puzzle Rumble", 2.2f, (t, n) =>
        {
            float env = Mathf.Min(1f, t / 0.3f) * Mathf.Clamp01((2.2f - t) / 0.6f);
            return env * (Mathf.Sin(2f * Mathf.PI * 45f * t + Mathf.Sin(t * 13f)) * 0.5f + n * 0.25f);
        });

        /// <summary>A gust of wind (Galewick's hand sweep, Oct 27): airy noise swelling and falling, a faint rising whistle.</summary>
        public static AudioClip Breeze => _breeze != null ? _breeze : _breeze = Make("Puzzle Breeze", 1.1f, (t, n) =>
        {
            float u = Mathf.Clamp01(t / 1.1f);
            float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f)), 1.5f);
            float whistle = Mathf.Sin(2f * Mathf.PI * (380f + 260f * u) * t) * n * 0.35f;
            return env * (n * 0.5f + whistle);
        });

        static AudioClip Make(string name, float seconds, System.Func<float, float, float> f)
        {
            int count = (int)(Rate * seconds);
            var data = new float[count];
            var rnd = new System.Random(name.GetHashCode());
            float lp = 0f;
            for (int i = 0; i < count; i++)
            {
                // Low-passed noise sounds like air and dust, not hiss.
                lp = Mathf.Lerp(lp, (float)rnd.NextDouble() * 2f - 1f, 0.12f);
                data[i] = Mathf.Clamp(f(i / (float)Rate, lp * 2.5f), -1f, 1f) * 0.8f;
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _thud = _puff = _whoosh = _rumble = _breeze = null;
    }
}
