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
        [Tooltip("Animation height above its stand, map metres.")]
        public float lift;
        [Tooltip("Height of what it stands on (its battle pad), map metres.")]
        public float baseLift;
        [Tooltip("Degrees; 0 faces the player (+Z).")]
        public float yaw;
        public float grow = 1f;
        public Vector3 squash = Vector3.one;
        [Tooltip("Degrees it tips forward round its feet (a Keeper kneels and nods, step 9c).")]
        public float pitch;
        [Tooltip("Trembling (step 9e, a frightened Keeper): extra map offset in tiles and roll in degrees, set every frame.")]
        public Vector2 shake;
        public float shakeRoll;
        [Tooltip("Step 14h: battle pads only, the readable-battle size factor (PadFit); it eases toward fitTarget.")]
        public float fit = 1f;
        public float fitTarget = 1f;

        public CreatureView View { get; private set; }

        /// <summary>Step 14h: height at grow 1 with the pad fit (map metres).</summary>
        public float Height => View.height * fit;

        /// <summary>Step 14h: body-centre height at grow 1 with the pad fit (map metres).</summary>
        public float CenterHeight => View.centerHeight * fit;

        void Awake()
        {
            View = GetComponent<CreatureView>();
            View.lookAtPlayer = false;
        }

        void Start() => View.SetWindowClip(true);

        void LateUpdate()
        {
            if (!Mathf.Approximately(fit, fitTarget)) fit = Mathf.MoveTowards(fit, fitTarget, Time.deltaTime * 2f);
            Snap();
        }

        public void Snap()
        {
            if (island == null || island.Content == null) return;
            transform.localPosition = island.TileToLocal(tile + nudge + shake, island.Content.capHeight + baseLift + lift);
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, shakeRoll);
            transform.localScale = squash * (island.Zoom * Mathf.Max(0.0001f, grow * fit));
        }

        float Cap => island.Content.capHeight;

        /// <summary>Island-local body centre, including the current animation offsets.</summary>
        public Vector3 LocalCenter => island.TileToLocal(tile + nudge, Cap + baseLift + lift + CenterHeight * grow);

        /// <summary>Island-local point just above the head, at rest (for the HP bar, so it doesn't jump with hops).</summary>
        public Vector3 LocalTop => island.TileToLocal(tile, Cap + baseLift + Height * grow);

        public Vector3 WorldCenter => island.transform.TransformPoint(LocalCenter);

        /// <summary>
        /// Step 14h: the body as drawn (renderer bounds) at grow 1 and fit 1, in island-local units: its height above the
        /// feet and its centre's height. <see cref="CreatureView.height"/> is the rig's nominal height and misses
        /// antlers, lamps and crests (the readable-battle rule needs the real top). The island stays level.
        /// </summary>
        public void Measure(out float height, out float center)
        {
            height = center = 0f;
            if (island == null || island.Content == null) return;
            var scale = transform.localScale;
            transform.localScale = squash * island.Zoom;
            float feet = transform.position.y, s = Mathf.Max(1e-5f, island.transform.lossyScale.y);
            bool any = false;
            Bounds b = default;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer) continue;
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }
            transform.localScale = scale;
            if (!any)
            {
                height = View.height * island.Zoom;
                center = View.centerHeight * island.Zoom;
                return;
            }
            height = Mathf.Max(1e-4f, (b.max.y - feet) / s);
            center = Mathf.Clamp((b.center.y - feet) / s, 0f, height);
        }

        /// <summary>Yaw that faces from one map point to another (columns run toward -X, rows toward +Z).</summary>
        public static float YawToward(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
        }
    }
}
