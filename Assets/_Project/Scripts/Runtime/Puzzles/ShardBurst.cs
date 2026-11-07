using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 17 I: touching a Keeper's shard and slotting it into the statue rhyme with the very first touch of the game
    /// (the opening shard's gold burst, <see cref="Opening"/>): the same burst tinted per element, plus one flourish.
    /// Wind: a swirling ring and a few leaves. Fire: rising embers. Water: a ripple ring and bubbles. Under 1.5 s, small
    /// counts (about 70 particles in all) on the one pooled particle system.
    /// </summary>
    public static class ShardBurst
    {
        public static Color Tint(string shardId)
        {
            switch (FinaleLogic.Element(shardId))
            {
                case "fire": return new Color(1f, 0.6f, 0.28f);
                case "wind": return new Color(0.62f, 0.96f, 0.78f);
                case "water": return new Color(0.5f, 0.78f, 1f);
                default: return new Color(1f, 0.85f, 0.45f);
            }
        }

        /// <summary>
        /// The burst at <paramref name="at"/> (world). <paramref name="scale"/> = the island's world scale (the opening's
        /// burst is sized for scale 1); <paramref name="up"/> = the island's up.
        /// </summary>
        public static void Play(BattleFx fx, Vector3 at, string shardId, float scale, Vector3 up)
        {
            if (fx == null) return;
            float s = Mathf.Max(0.3f, scale);
            var tint = Tint(shardId);
            // The opening's burst (46 at speed 0.25, life 0.9, size 0.004), a little smaller.
            fx.Burst(at, 30, Color.Lerp(tint, Color.white, 0.25f), 0.2f * s, 0.9f, 0.004f * s, up * (0.05f * s));
            string element = FinaleLogic.Element(shardId);
            Vector3 side = Vector3.Cross(up, Vector3.forward);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(up, Vector3.right);
            side.Normalize();
            Vector3 fwd = Vector3.Cross(side, up);
            switch (element)
            {
                case "wind":
                {
                    // A ring that swirls: each mote leaves round the ring's tangent, rising a little.
                    const int n = 16;
                    float r = 0.035f * s;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        Vector3 radial = side * Mathf.Cos(a) + fwd * Mathf.Sin(a);
                        Vector3 tangent = Vector3.Cross(up, radial);
                        fx.Burst(at + radial * r, 1, tint, 0.004f * s, 1.1f, 0.0035f * s, tangent * (0.12f * s) + up * (0.03f * s));
                    }
                    // A few leaves drifting down and out.
                    fx.Burst(at, 6, new Color(0.45f, 0.75f, 0.3f), 0.08f * s, 1.4f, 0.006f * s, up * (0.02f * s), 0.1f * s);
                    break;
                }
                case "fire":
                    // Embers rising, warm orange to red.
                    for (int i = 0; i < 14; i++)
                    {
                        Vector3 off = (side * Random.Range(-1f, 1f) + fwd * Random.Range(-1f, 1f)) * (0.02f * s);
                        var c = Color.Lerp(new Color(1f, 0.75f, 0.3f), new Color(1f, 0.35f, 0.15f), Random.value);
                        fx.Burst(at + off, 1, c, 0.01f * s, Random.Range(0.9f, 1.4f), 0.003f * s, up * (Random.Range(0.08f, 0.16f) * s));
                    }
                    break;
                case "water":
                {
                    // A flat ripple ring going out, then bubbles rising slowly.
                    const int n = 18;
                    for (int i = 0; i < n; i++)
                    {
                        float a = i / (float)n * Mathf.PI * 2f;
                        Vector3 radial = side * Mathf.Cos(a) + fwd * Mathf.Sin(a);
                        fx.Burst(at + radial * (0.01f * s), 1, tint, 0.002f * s, 0.8f, 0.003f * s, radial * (0.1f * s));
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 off = (side * Random.Range(-1f, 1f) + fwd * Random.Range(-1f, 1f)) * (0.025f * s);
                        fx.Burst(at + off, 1, new Color(0.8f, 0.93f, 1f), 0.005f * s, Random.Range(1f, 1.4f), 0.005f * s, up * (Random.Range(0.04f, 0.07f) * s));
                    }
                    break;
                }
            }
            Debug.Log($"[ShardBurst] {shardId}: the opening's burst, tinted, with the {(string.IsNullOrEmpty(element) ? "plain" : element)} flourish.");
        }
    }
}
