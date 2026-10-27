using UnityEngine;

namespace Diorama
{
    /// <summary>Procedural placeholder sounds for the hand puzzles (step 9b) until the audio step: built once, shared.</summary>
    public static class PuzzleSounds
    {
        const int Rate = 44100;
        static AudioClip _thud, _puff, _whoosh, _rumble, _breeze, _twang, _hoot, _crackle, _catch;

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

        /// <summary>Step 10e: a knot snapping on Galeowl, a plucked string that bends down and rings out.</summary>
        public static AudioClip Twang => _twang != null ? _twang : _twang = Make("Puzzle Twang", 0.9f, (t, n) =>
        {
            float f0 = 196f * (1f + 0.35f * Mathf.Exp(-t * 30f));
            float ph = 2f * Mathf.PI * f0 * t;
            float body = Mathf.Sin(ph) + 0.5f * Mathf.Sin(2f * ph) + 0.25f * Mathf.Sin(3f * ph);
            float pluck = n * Mathf.Exp(-t * 60f);
            return Mathf.Exp(-t * 5f) * body * 0.55f + pluck * 0.6f;
        });

        /// <summary>Step 10e: a calmed Galeowl poked, a soft two-note "hoo-hoo".</summary>
        public static AudioClip Hoot => _hoot != null ? _hoot : _hoot = Make("Puzzle Hoot", 0.85f, (t, n) =>
        {
            float a = Mathf.Clamp01(t / 0.32f), b = Mathf.Clamp01((t - 0.4f) / 0.42f);
            float env1 = t < 0.32f ? Mathf.Sin(Mathf.PI * a) : 0f;
            float env2 = t >= 0.4f ? Mathf.Sin(Mathf.PI * b) : 0f;
            float f1 = 420f * (1f - 0.06f * a), f2 = 380f * (1f - 0.1f * b);
            float s1 = Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * f1 * t);
            float s2 = Mathf.Sin(2f * Mathf.PI * f2 * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * f2 * t);
            return 0.6f * (env1 * s1 + env2 * s2) + 0.04f * n * (env1 + env2);
        });

        /// <summary>Step 10f: a fire crackling (a flame held in cupped hands, the forge roaring): soft roar with random pops.</summary>
        public static AudioClip Crackle => _crackle != null ? _crackle : _crackle = Make("Puzzle Crackle", 1.2f, (t, n) =>
        {
            float env = Mathf.Min(1f, t / 0.1f) * Mathf.Clamp01((1.2f - t) / 0.3f);
            // Pops: short noisy clicks on a hashed beat.
            float beat = Mathf.Floor(t * 23f);
            float hash = Mathf.Abs(Mathf.Sin(beat * 12.9898f) * 43758.5453f) % 1f;
            float pop = hash > 0.72f ? Mathf.Exp(-(t * 23f - beat) * 14f) * n * 1.4f : 0f;
            return env * (n * 0.22f + Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.12f + pop);
        });

        /// <summary>Step 10f: a stone lantern catching: a short bright tick and a small flame puff.</summary>
        public static AudioClip Catch => _catch != null ? _catch : _catch = Make("Puzzle Catch", 0.45f, (t, n) =>
        {
            float tick = Mathf.Exp(-t * 45f) * Mathf.Sin(2f * Mathf.PI * 1250f * t) * 0.35f;
            float puff = Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t * 9f) * n * 0.55f;
            return tick + puff;
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
        static void ResetStatics() => _thud = _puff = _whoosh = _rumble = _breeze = _twang = _hoot = _crackle = _catch = null;
    }
}
