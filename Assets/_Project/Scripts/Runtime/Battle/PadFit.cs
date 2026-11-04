using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: readable battles (Mugesh: an evolved Pebbloop on the near pad hid the wild Isling completely; he looks
    /// from behind his own Isling). On the battle pads only (the palm, Journal, lodge and map keep the true size):
    /// <list type="bullet">
    /// <item>no actor is taller than <see cref="MaxRatio"/> x its opponent (a Keeper is exempt: it is meant to tower);</item>
    /// <item>the near actor's top stays below the line from the player's eyes to the wild actor's centre, by
    /// 0.3 of the wild actor's height. Step 14j (Mugesh: an Lv 15 Riverloom still covered most of a
    /// Kettlewing): stricter: below the line to a point <see cref="VisibleFrom"/> of the wild actor's height above its
    /// feet, so all of it from there up shows.</item>
    /// </list>
    /// Shrinks only, never below <see cref="MinFit"/>. Pure (island-local points), EditMode-tested.
    /// </summary>
    public static class PadFit
    {
        public const float MaxRatio = 1.3f, MinFit = 0.45f;
        /// <summary>Step 14j: the wild actor shows from this share of its height up (was: its centre less 0.3 of its height at the near pad).</summary>
        public const float VisibleFrom = 0.05f;

        public struct Result
        {
            public float near, wild;
            public bool sightline, ratio;
        }

        /// <summary>
        /// The fits (scale factors) for the near (player) and wild actors. Heights are the natural ones (fit 1), in the
        /// same units as the points; <paramref name="wildCenterShare"/> = centre height / height of the wild actor.
        /// </summary>
        public static Result Fit(Vector3 eye, Vector3 nearFeet, float nearHeight, Vector3 wildFeet, float wildHeight, bool wildIsKeeper)
        {
            var r = new Result { near = 1f, wild = 1f };
            if (nearHeight <= 1e-6f || wildHeight <= 1e-6f) return r;
            // A few passes: shrinking one can make the other too tall in turn.
            for (int pass = 0; pass < 4; pass++)
            {
                float n = nearHeight * r.near, w = wildHeight * r.wild;
                if (n > MaxRatio * w)
                {
                    r.near = Mathf.Max(MinFit, MaxRatio * w / nearHeight);
                    r.ratio = true;
                }
                else if (!wildIsKeeper && w > MaxRatio * n)
                {
                    r.wild = Mathf.Max(MinFit, MaxRatio * n / wildHeight);
                    r.ratio = true;
                }
                float allowed = AllowedNearHeight(eye, nearFeet, wildFeet, wildHeight * r.wild);
                if (nearHeight * r.near > allowed + 1e-6f)
                {
                    float f = Mathf.Max(MinFit, allowed / nearHeight);
                    if (f < r.near)
                    {
                        r.near = f;
                        r.sightline = true;
                    }
                }
            }
            return r;
        }

        /// <summary>
        /// How tall the near actor may stand (from its feet) so its top stays under the line from the eye to the wild actor
        /// at <see cref="VisibleFrom"/> of its height. Infinite when the eye isn't above the near feet (looking from the
        /// side: shrinking can't help).
        /// </summary>
        public static float AllowedNearHeight(Vector3 eye, Vector3 nearFeet, Vector3 wildFeet, float wildHeight)
        {
            Vector3 c = wildFeet + Vector3.up * (wildHeight * VisibleFrom);
            if (eye.y <= nearFeet.y + 1e-4f) return float.PositiveInfinity;
            Vector2 e = new Vector2(eye.x, eye.z), toC = new Vector2(c.x, c.z) - e, toN = new Vector2(nearFeet.x, nearFeet.z) - e;
            float len = toC.sqrMagnitude;
            if (len < 1e-8f) return float.PositiveInfinity;
            float t = Mathf.Clamp01(Vector2.Dot(toN, toC) / len);
            float lineY = Mathf.Lerp(eye.y, c.y, t);
            return lineY - nearFeet.y;
        }
    }
}
