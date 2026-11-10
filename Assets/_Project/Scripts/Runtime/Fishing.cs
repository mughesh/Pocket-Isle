using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 29 D20 (playtest: the "face the water" line fired on the Galewick bridge, and fishing didn't work there): a
    /// tap fishes only from a real bank, a walkable tile beside water that isn't a bridge (water, a lagoon bed or mud on
    /// both sides). Pure over the map. <see cref="FishSpots"/> marks a few good banks with soft ripples.
    /// </summary>
    public static class Fishing
    {
        public const string SpotPrefix = "fishSpot";

        static bool Wet(IslandMap map, Vector2Int t)
        {
            char c = map.At(t.y, t.x);
            return c == IslandMap.Water || c == IslandMap.DryBed || c == IslandMap.Mud;
        }

        /// <summary>A crossing: wet on both sides (left and right, or ahead and behind).</summary>
        public static bool Bridge(IslandMap map, Vector2Int t) =>
            Wet(map, t + Vector2Int.left) && Wet(map, t + Vector2Int.right) || Wet(map, t + Vector2Int.up) && Wet(map, t + Vector2Int.down);

        /// <summary>A bank: walkable, beside open water, not a bridge.</summary>
        public static bool IsBank(IslandMap map, Vector2Int t)
        {
            if (map == null || !map.CanWalk(t) || Bridge(map, t)) return false;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                if (map.At(t.y + d.y, t.x + d.x) == IslandMap.Water) return true;
            return false;
        }

        /// <summary>Standing on <paramref name="stand"/>, facing <paramref name="front"/>: a tap fishes.</summary>
        public static bool CanFish(IslandMap map, Vector2Int stand, Vector2Int front) =>
            map != null && map.At(front.y, front.x) == IslandMap.Water && !Bridge(map, stand) && map.CanWalk(stand);

        /// <summary>The marked fishing banks on a world map (landmarks named fishSpot...).</summary>
        public static IEnumerable<Vector2Int> Spots(WorldMap map)
        {
            if (map == null) yield break;
            foreach (var l in map.landmarks)
                if (l.name != null && l.name.StartsWith(SpotPrefix)) yield return l.tile;
        }

        /// <summary>The water tile a marked bank faces (the first wet neighbour), or the bank itself.</summary>
        public static Vector2Int WaterOf(IslandMap map, Vector2Int bank)
        {
            foreach (var d in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
                if (map.At(bank.y + d.y, bank.x + d.x) == IslandMap.Water) return bank + d;
            return bank;
        }
    }
}
