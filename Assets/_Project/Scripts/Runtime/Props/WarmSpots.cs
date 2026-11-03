using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 10f: rekindled places in a realm that is still cold (Cinderwood's village flame, a lit lantern, the roaring
    /// forge). Props whose origin lies inside a spot burn and smoke as if restored and get most of their colour back
    /// (Diorama/Prop reads the shader global array <c>_WarmSpots</c>). No extra draws: the island's instanced props
    /// light themselves. Positions are map tiles (column, row); the owner sets its slot every frame its map shows and
    /// clears it otherwise (other maps share the same slots).
    /// </summary>
    public static class WarmSpots
    {
        // Step 14c: 12 (Cinderwood's arrival lanterns take 8-11). Diorama/Prop's _WarmSpots array has the same size.
        public const int Count = 12;
        static readonly int SpotsId = Shader.PropertyToID("_WarmSpots");
        static readonly Vector4[] _spots = new Vector4[Count];
        static bool _dirty = true;

        /// <summary>
        /// Slot <paramref name="slot"/> warms everything within <paramref name="radius"/> tiles of <paramref name="tile"/>,
        /// fading over <paramref name="edge"/> tiles at its border. <paramref name="tileSize"/> = the map's tile size.
        /// </summary>
        public static void Set(int slot, Vector2 tile, float radius, float edge, float tileSize)
        {
            if (slot < 0 || slot >= Count) return;
            // Map space as the restoration wave uses it: columns run toward -X.
            var v = radius > 0f ? new Vector4(-tile.x * tileSize, tile.y * tileSize, radius * tileSize, Mathf.Max(0.01f, edge) * tileSize) : Vector4.zero;
            if (_spots[slot] == v) return;
            _spots[slot] = v;
            _dirty = true;
            Push();
        }

        public static void Clear(int slot)
        {
            if (slot < 0 || slot >= Count || _spots[slot] == Vector4.zero) return;
            _spots[slot] = Vector4.zero;
            _dirty = true;
            Push();
        }

        /// <summary>Radius of a slot right now (map metres, 0 = off). Tests and the dev readout.</summary>
        public static float Radius(int slot) => slot >= 0 && slot < Count ? _spots[slot].z : 0f;

        static void Push()
        {
            if (!_dirty) return;
            _dirty = false;
            Shader.SetGlobalVectorArray(SpotsId, _spots);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            for (int i = 0; i < Count; i++) _spots[i] = Vector4.zero;
            _dirty = true;
            Push();
        }
    }
}
