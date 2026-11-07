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

        /// <summary>Seconds of the blast's charge (the whirl gathering into the shard) before it goes off.</summary>
        public const float ChargeSeconds = 1.1f;

        /// <summary>
        /// Nov 7 (Mugesh: the touch effect should be strong, longer, whirling and island-wide, like the opening's): the
        /// Keeper shard's blast. Motes whirl in round the shard and it glows up (<see cref="ChargeSeconds"/>), then a flash,
        /// a wave ring across the whole cap, a big tinted burst with the element's flourish scaled up, and sparkles rising
        /// over the island. About 2.8 s in all. Runs on the fx's own coroutine.
        /// </summary>
        public static void Blast(BattleFx fx, IslandBuilder island, Vector3 at, string shardId)
        {
            if (fx == null || island == null) return;
            fx.StartCoroutine(BlastRoutine(fx, island, at, shardId));
        }

        static System.Collections.IEnumerator BlastRoutine(BattleFx fx, IslandBuilder island, Vector3 at, string shardId)
        {
            var t = island.transform;
            float s = Mathf.Max(0.3f, t.lossyScale.x);
            float inv = 1f / Mathf.Max(1e-4f, fx.transform.lossyScale.x);
            Vector3 up = t.up;
            var tint = Tint(shardId);
            var bright = Color.Lerp(tint, Color.white, 0.45f);
            Debug.Log($"[ShardBurst] {shardId}: island blast (charge {ChargeSeconds:0.0} s, then flash, wave and burst).");

            // 1. Charge: motes whirl in round the shard, the glow grows.
            var core = fx.Get(BattleFx.Shape.Orb);
            if (core != null) core.Set(Color.white, tint, 0f, 1.5f, 1f, 0.3f);
            float spin = Random.value * Mathf.PI * 2f;
            float carry = 0f;
            for (float u = 0f; u < ChargeSeconds; u += Time.deltaTime)
            {
                float k = u / ChargeSeconds;
                carry += Time.deltaTime * 110f; // motes per second
                for (; carry >= 1f; carry -= 1f)
                {
                    spin += 0.9f;
                    float r = Random.Range(0.07f, 0.14f) * s * (1f - 0.4f * k);
                    Vector3 radial = Quaternion.AngleAxis(spin * Mathf.Rad2Deg, up) * t.right;
                    Vector3 tangent = Vector3.Cross(up, radial);
                    Vector3 from = at + radial * r + up * (Random.Range(-0.04f, 0.04f) * s);
                    const float life = 0.45f;
                    // Round and in: they reach the shard about as they fade.
                    Vector3 vel = tangent * (r * 4f) - radial * (r / life) + up * (Random.Range(-0.02f, 0.02f) * s);
                    fx.Burst(from, 1, Random.value < 0.3f ? Color.white : bright, 0f, life, 0.004f * s, vel);
                }
                if (core != null)
                {
                    core.transform.position = at;
                    core.transform.localScale = Vector3.one * (0.03f * s * (0.5f + k + 0.15f * Mathf.Sin(u * 30f)) * inv);
                    core.SetAlpha(0.3f + 0.7f * k);
                }
                yield return null;
            }
            if (core != null) fx.Release(core);

            // 2. The blast: a flash, a wave across the cap, the burst and the element's flourish, island-sized.
            var flash = fx.Get(BattleFx.Shape.Orb);
            if (flash != null) flash.Set(Color.white, tint, 1f, 1.5f, 1f, 0.3f);
            var wave = fx.Get(BattleFx.Shape.Ring);
            if (wave != null) wave.Set(bright, tint, 1f);
            Vector3 cap = t.TransformPoint(new Vector3(0f, island.CapTopLocal + 0.01f, 0f));
            Vector3 waveAt = cap + Vector3.ProjectOnPlane(at - cap, up);
            float reach = Mathf.Max(island.FootprintHalfSize.x, island.FootprintHalfSize.y) * 1.25f * s;
            fx.Burst(at, 110, bright, 0.42f * s, 1.3f, 0.005f * s, up * (0.05f * s), 0.05f * s);
            Play(fx, at, shardId, s * 2.5f, up);
            const float blastSeconds = 0.9f;
            for (float u = 0f; u < blastSeconds; u += Time.deltaTime)
            {
                float k = u / blastSeconds;
                if (flash != null)
                {
                    flash.transform.position = at;
                    flash.transform.localScale = Vector3.one * (0.17f * s * Mathf.Sin(Mathf.Min(1f, k * 1.6f) * Mathf.PI * 0.5f + 0.2f) * inv);
                    flash.SetAlpha(Mathf.Clamp01(1f - k * 1.4f));
                }
                if (wave != null)
                {
                    wave.transform.SetPositionAndRotation(waveAt, t.rotation);
                    wave.transform.localScale = Vector3.one * (reach * (0.1f + 0.9f * Mathf.Sqrt(k)) * inv);
                    wave.SetAlpha(0.9f * (1f - k));
                }
                yield return null;
            }
            if (flash != null) fx.Release(flash);
            if (wave != null) fx.Release(wave);

            // 3. Sparkles rising all over the island.
            var half = island.FootprintHalfSize;
            for (float u = 0f; u < 0.9f; u += Time.deltaTime)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = t.TransformPoint(new Vector3(Random.Range(-half.x, half.x), island.CapTopLocal + 0.02f, Random.Range(-half.y, half.y)));
                    fx.Burst(p, 1, Random.value < 0.5f ? bright : tint, 0.01f * s, Random.Range(0.8f, 1.3f), 0.004f * s, up * (Random.Range(0.05f, 0.1f) * s));
                }
                yield return null;
            }
        }
    }
}
