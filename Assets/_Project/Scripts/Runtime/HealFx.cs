using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Nov 11 (Mugesh: "don't be shy, the heal effects were simple"): the shared heal moment for the healing blooms and
    /// the lodge, built from the pooled <see cref="BattleFx"/> shapes and dots. Per realm a puff (<see cref="Puff"/>:
    /// Galewick dandelion seeds drifting on the wind, Cinderwood embers, Lilymere petals and drops), then the heal itself
    /// (<see cref="Wave"/>): a bright green flash, two ground shockwaves (green, then gold), a halo that rises up round
    /// the healed spot, a column of green sparkles and a ring of "+" going up. All sizes are in tiles (world metres per
    /// tile passed in), so it reads the same at every island size and zoom.
    /// </summary>
    public static class HealFx
    {
        public static readonly Color Green = new Color(0.45f, 1f, 0.55f), DeepGreen = new Color(0.16f, 0.78f, 0.36f),
            Gold = new Color(1f, 0.85f, 0.35f);

        /// <summary>A realm's puff colours: main, accent, and how the dots move.</summary>
        public struct Palette
        {
            public Color main, accent, glow;
            public float rise, wind, gravity, life;
            public int count;
        }

        /// <summary>The puff for a realm (pure). Unknown realms get a soft green puff.</summary>
        public static Palette For(string realm)
        {
            switch (realm)
            {
                case "galewick":
                    // Dandelion seeds: white and cream, slow, carried sideways on the breeze, long-lived.
                    return new Palette { main = new Color(1f, 1f, 0.97f), accent = new Color(1f, 0.95f, 0.78f), glow = new Color(0.85f, 1f, 0.9f),
                        rise = 0.55f, wind = 0.6f, gravity = 0f, life = 2.6f, count = 70 };
                case "cinderwood":
                    // Ember-lily: orange and gold sparks that leap up fast and wink out.
                    return new Palette { main = new Color(1f, 0.55f, 0.18f), accent = new Color(1f, 0.86f, 0.4f), glow = new Color(1f, 0.72f, 0.35f),
                        rise = 1.4f, wind = 0.1f, gravity = 0f, life = 1.3f, count = 80 };
                case "lilymere":
                    // Lotus: pink petals that float up and settle, with cyan water drops.
                    return new Palette { main = new Color(1f, 0.66f, 0.82f), accent = new Color(0.55f, 0.9f, 1f), glow = new Color(0.8f, 0.9f, 1f),
                        rise = 0.7f, wind = 0.25f, gravity = 0.5f, life = 2f, count = 70 };
                default:
                    return new Palette { main = Green, accent = Gold, glow = Green, rise = 0.8f, wind = 0.2f, gravity = 0f, life = 1.6f, count = 50 };
            }
        }

        static float Local(BattleFx fx, float world) => world / Mathf.Max(1e-5f, fx.transform.lossyScale.x);

        /// <summary>
        /// The realm's puff at <paramref name="at"/> (world): a burst of the realm's dots in two waves, a soft flash of its
        /// glow. <paramref name="up"/> is the island's up, <paramref name="tile"/> one tile in world metres.
        /// </summary>
        public static IEnumerator Puff(BattleFx fx, Vector3 at, Vector3 up, float tile, string realm)
        {
            if (fx == null) yield break;
            var p = For(realm);
            var windDir = Vector3.Cross(up, Vector3.forward).normalized;
            if (windDir.sqrMagnitude < 0.5f) windDir = Vector3.right;
            Vector3 drift = up * (tile * p.rise) + windDir * (tile * p.wind);
            fx.Burst(at, p.count * 2 / 3, p.main, tile * 2.4f, p.life, tile * 0.1f, drift, tile * p.gravity);
            fx.Burst(at, p.count / 3, p.accent, tile * 1.1f, p.life * 0.8f, tile * 0.08f, drift * 1.2f, tile * p.gravity);
            var orb = fx.Get(BattleFx.Shape.Orb);
            orb.Set(Color.white, p.glow, 1f, 1.5f, 1f, 0.35f);
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float u = t / 0.5f;
                orb.transform.position = at;
                orb.transform.localScale = Vector3.one * Local(fx, tile * (0.2f + 0.8f * Mathf.Sin(u * Mathf.PI * 0.5f)));
                orb.SetAlpha((1f - u) * (1f - u) * 0.6f);
                if (t > 0.12f && t - Time.deltaTime <= 0.12f)
                    fx.Burst(at, p.count / 3, p.main, tile * 0.9f, p.life * 1.1f, tile * 0.09f, drift * 0.8f, tile * p.gravity);
                yield return null;
            }
            fx.Release(orb);
        }

        /// <summary>
        /// The heal itself, centred on <paramref name="feet"/> (world, on the ground; <paramref name="flat"/> lays a ring on
        /// it): a green flash, two shockwaves out to <paramref name="radiusTiles"/>, a halo rising up the healed one, a
        /// column of green sparkles and "+" going up (through <paramref name="stage"/>, may be null).
        /// </summary>
        public static IEnumerator Wave(BattleFx fx, BattleStage stage, Vector3 feet, Quaternion flat, Vector3 up, float tile, float radiusTiles,
            float seconds = 1.6f)
        {
            if (fx == null) yield break;
            var flash = fx.Get(BattleFx.Shape.Orb);
            flash.Set(Color.white, Green, 1f, 1.4f, 1f, 0.3f);
            var ring1 = fx.Get(BattleFx.Shape.Ring);
            ring1.Set(Green, Color.white, 1f, 1.3f);
            var ring2 = fx.Get(BattleFx.Shape.Ring);
            ring2.Set(Gold, Color.white, 1f, 1.3f);
            var halo = fx.Get(BattleFx.Shape.Ring);
            halo.Set(Green, Gold, 1f, 1.6f);
            var rnd = new System.Random(feet.GetHashCode());
            float nextDots = 0f, nextPlus = 0.05f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float u = t / seconds;
                // Flash: swells up from the feet, fades in the first third.
                float fu = Mathf.Clamp01(t / 0.45f);
                flash.transform.position = feet + up * (tile * 0.5f);
                flash.transform.localScale = Vector3.one * Local(fx, tile * (0.4f + 1.6f * Mathf.Sqrt(fu)));
                flash.SetAlpha((1f - fu) * 0.8f);
                // Two shockwaves along the ground.
                Shock(fx, ring1, feet, flat, up, tile, radiusTiles, t / 0.9f);
                Shock(fx, ring2, feet, flat, up, tile, radiusTiles * 0.8f, (t - 0.22f) / 0.9f);
                // The halo rises up round the healed one and narrows.
                float hu = Mathf.Clamp01((t - 0.1f) / 1.1f);
                halo.transform.SetPositionAndRotation(feet + up * (tile * (0.05f + 1.6f * hu)), flat);
                halo.transform.localScale = Vector3.one * Local(fx, tile * Mathf.Lerp(1.5f, 0.6f, hu));
                halo.SetAlpha(hu <= 0f ? 0f : Mathf.Sin(hu * Mathf.PI) * 0.95f);
                // Green sparkles rising in a column, thickest at the start.
                if (t >= nextDots)
                {
                    nextDots = t + 0.04f;
                    int n = u < 0.5f ? 6 : 3;
                    for (int i = 0; i < n; i++)
                    {
                        float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = tile * (0.2f + 0.9f * (float)rnd.NextDouble());
                        var side = flat * new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                        var c = rnd.NextDouble() < 0.25 ? Gold : rnd.NextDouble() < 0.5 ? Green : DeepGreen;
                        fx.Burst(feet + side + up * (tile * 0.1f), 1, c, tile * 0.15f, 1.1f, tile * 0.05f, up * (tile * 1.8f));
                    }
                }
                if (stage != null && t >= nextPlus && t < seconds * 0.8f)
                {
                    nextPlus = t + 0.09f;
                    float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = tile * (0.4f + 0.8f * (float)rnd.NextDouble());
                    stage.PopHealAt(feet + flat * new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r) + up * (tile * 0.3f * (float)rnd.NextDouble()),
                        1.5f, 1.7f);
                }
                yield return null;
            }
            fx.Release(flash);
            fx.Release(ring1);
            fx.Release(ring2);
            fx.Release(halo);
        }

        static void Shock(BattleFx fx, BattleFx.Item ring, Vector3 feet, Quaternion flat, Vector3 up, float tile, float radiusTiles, float u, float alpha = 0.95f)
        {
            if (u <= 0f || u >= 1f)
            {
                ring.SetAlpha(0f);
                return;
            }
            float e = 1f - (1f - u) * (1f - u);
            ring.transform.SetPositionAndRotation(feet + up * (tile * 0.04f), flat);
            ring.transform.localScale = Vector3.one * Local(fx, tile * (0.3f + 2f * radiusTiles * e));
            ring.SetAlpha((1f - u) * alpha);
        }

        /// <summary>
        /// Nov 14 (Mugesh: "reuse the wave we have for the healing effect in the lodge, with blue colour"): the heal's two
        /// ground shockwaves and its flash alone, in any colours. <paramref name="strength"/> (0..1, more allowed) scales
        /// how bright and how big the flash is; the reach is <paramref name="radiusTiles"/>.
        /// </summary>
        public static IEnumerator Ripple(BattleFx fx, Vector3 feet, Quaternion flat, Vector3 up, float tile, float radiusTiles,
            Color main, Color accent, float strength = 1f, float seconds = 1f)
        {
            if (fx == null) yield break;
            var flash = fx.Get(BattleFx.Shape.Orb);
            flash.Set(Color.white, main, 1f, 1.4f, 1f, 0.3f);
            var ring1 = fx.Get(BattleFx.Shape.Ring);
            ring1.Set(main, Color.white, 1f, 1.3f);
            var ring2 = fx.Get(BattleFx.Shape.Ring);
            ring2.Set(accent, Color.white, 1f, 1.3f);
            float alpha = Mathf.Clamp01(0.55f + 0.4f * strength), travel = seconds * 0.85f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float fu = Mathf.Clamp01(t / 0.35f);
                flash.transform.position = feet + up * (tile * 0.3f);
                flash.transform.localScale = Vector3.one * Local(fx, tile * (0.3f + 1.1f * strength * Mathf.Sqrt(fu)));
                flash.SetAlpha((1f - fu) * Mathf.Clamp01(0.4f + 0.35f * strength));
                Shock(fx, ring1, feet, flat, up, tile, radiusTiles, t / travel, alpha);
                Shock(fx, ring2, feet, flat, up, tile, radiusTiles * 0.75f, (t - 0.18f) / travel, alpha);
                yield return null;
            }
            fx.Release(flash);
            fx.Release(ring1);
            fx.Release(ring2);
        }

        /// <summary>
        /// The idle "I heal" cue: one soft mote rising from <paramref name="at"/> in the realm's glow or green, now and then
        /// (call often; it keeps its own rate). Brighter and faster while <paramref name="eager"/> (a fingertip near).
        /// </summary>
        public static void Mote(BattleFx fx, Vector3 at, Vector3 up, float tile, string realm, bool eager)
        {
            if (fx == null) return;
            var p = For(realm);
            var side = Random.insideUnitSphere * (tile * 0.35f);
            side -= up * Vector3.Dot(side, up);
            var c = Random.value < 0.55f ? Green : p.glow;
            fx.Burst(at + side, 1, c, tile * 0.04f, eager ? 1.1f : 1.6f, tile * (eager ? 0.05f : 0.035f), up * (tile * (eager ? 1.2f : 0.55f)));
        }
    }
}
