using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Stands a battle creature on the world map under the island: any map point (fractions allowed), scaled with
    /// the zoom and clipped at the rim like the tiles. The battle animations move it through
    /// <see cref="nudge"/> (tiles), <see cref="lift"/> (map metres), <see cref="grow"/> and <see cref="squash"/>.
    /// Positions itself in LateUpdate, before <see cref="CreatureView"/> reads its position.
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class StageActor : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Map position (column, row).")]
        public Vector2 tile;
        [Tooltip("Extra map offset in tiles (lunges, knockback).")]
        public Vector2 nudge;
        [Tooltip("Height above the tile cap, map metres.")]
        public float lift;
        [Tooltip("Degrees; 0 faces the player (+Z).")]
        public float yaw;
        public float grow = 1f;
        public Vector3 squash = Vector3.one;

        public CreatureView View { get; private set; }

        void Awake()
        {
            View = GetComponent<CreatureView>();
            View.lookAtPlayer = false;
        }

        void Start() => View.SetWindowClip(true);

        void LateUpdate() => Snap();

        public void Snap()
        {
            if (island == null || island.Content == null) return;
            transform.localPosition = island.TileToLocal(tile + nudge, island.Content.capHeight + lift);
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            transform.localScale = squash * (island.Zoom * Mathf.Max(0.0001f, grow));
        }

        float Cap => island.Content.capHeight;

        /// <summary>Island-local body centre, including the current animation offsets.</summary>
        public Vector3 LocalCenter => island.TileToLocal(tile + nudge, Cap + lift + View.centerHeight * grow);

        /// <summary>Island-local point just above the head, at rest (for the HP bar, so it doesn't jump with hops).</summary>
        public Vector3 LocalTop => island.TileToLocal(tile, Cap + View.height * grow);

        public Vector3 WorldCenter => island.transform.TransformPoint(LocalCenter);

        /// <summary>Yaw that faces from one map point to another (columns run toward -X, rows toward +Z).</summary>
        public static float YawToward(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
        }
    }
}
