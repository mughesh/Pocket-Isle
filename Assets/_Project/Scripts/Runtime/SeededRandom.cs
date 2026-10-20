namespace Diorama
{
    /// <summary>
    /// mulberry32, bit-for-bit the same as rnd() in the prototype, so the same seed and call order
    /// give the same layout.
    /// </summary>
    public sealed class SeededRandom
    {
        uint _state;

        public SeededRandom(int seed) => _state = unchecked((uint)seed);

        /// <summary>Uniform value in [0, 1).</summary>
        public float Next()
        {
            unchecked
            {
                _state += 0x6D2B79F5u;
                uint t = _state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t = (t + (t ^ (t >> 7)) * (t | 61u)) ^ t;
                return (float)((t ^ (t >> 14)) / 4294967296.0);
            }
        }
    }
}
