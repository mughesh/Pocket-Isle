using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 27 E (a player couldn't read because the text "keeps moving spots"): where the Lantern Spirit is parked. It holds
    /// one goal: small changes (the head moving, the side check) don't move it, a new target waits until the Spirit has
    /// stayed put for the hold time, and only a target that really moved starts a glide at once. Pure, tested.
    /// </summary>
    public class SpiritPark
    {
        object _key;
        bool _started;
        float _flightStart = -999f;

        /// <summary>The spot it is parked at, or gliding to.</summary>
        public Vector3 Goal { get; private set; }

        /// <summary>
        /// A frame's wish: be at <paramref name="wanted"/> for <paramref name="key"/> (a target, or a named spot).
        /// <paramref name="force"/> moves past the hold (the battle, a lesson's own spot). True = a new glide starts now.
        /// </summary>
        public bool Update(object key, Vector3 wanted, float now, bool force, float hold, float moved, float glide)
        {
            if (!_started)
            {
                _started = true;
                _key = key;
                Goal = wanted;
                return false;
            }
            if (Equals(key, _key))
            {
                // The same target: a small change stays parked, a target that really moved is followed, one glide at a time.
                if ((wanted - Goal).magnitude <= moved || now - _flightStart < glide) return false;
            }
            else if (!force && now - (_flightStart + glide) < hold) return false;
            _key = key;
            Goal = wanted;
            _flightStart = now;
            return true;
        }
    }
}
