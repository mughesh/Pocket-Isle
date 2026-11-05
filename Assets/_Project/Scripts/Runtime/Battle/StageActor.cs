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
        [Tooltip("Step 16: the idle life on the pad (bob in map metres, sway roll and glance yaw in degrees), set every frame by the stage.")]
        public float idleLift, idleRoll, idleYaw;

        public CreatureView View { get; private set; }

        /// <summary>Step 14j: renderers drawn on it that aren't its children (a raised shield): its label clears them too.</summary>
        public readonly System.Collections.Generic.List<Renderer> attached = new System.Collections.Generic.List<Renderer>();

        Renderer[] _renderers;
        float _renderersAt = -1f;

        /// <summary>
        /// Step 14j: the world top of everything drawn on it right now: body, parts, aura, flames, smoke (its children)
        /// and <see cref="attached"/> fx. False when nothing is drawn.
        /// </summary>
        public bool TryGetVisualTop(out float top)
        {
            bool any = TryGetVisualBounds(out var b);
            top = any ? b.max.y : float.MinValue;
            return any;
        }

        /// <summary>Step 14j: the world bounds of everything drawn on it right now (see <see cref="TryGetVisualTop"/>).</summary>
        public bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            // Children change (fury flames and smoke are built after spawning): re-list now and then.
            if (_renderers == null || Time.time - _renderersAt > 0.5f)
            {
                _renderers = GetComponentsInChildren<Renderer>(false);
                _renderersAt = Time.time;
            }
            bool any = false;
            foreach (var r in _renderers) Add(r, ref bounds, ref any);
            foreach (var r in attached) Add(r, ref bounds, ref any);
            return any;
        }

        static void Add(Renderer r, ref Bounds bounds, ref bool any)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) return;
            if (r is ParticleSystemRenderer)
            {
                var ps = r.GetComponent<ParticleSystem>();
                if (ps == null || ps.particleCount == 0) return;
            }
            var b = r.bounds;
            if (b.size.sqrMagnitude < 1e-12f) return;
            if (any) bounds.Encapsulate(b);
            else bounds = b;
            any = true;
        }

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
            transform.localPosition = island.TileToLocal(tile + nudge + shake, island.Content.capHeight + baseLift + lift + idleLift);
            transform.localRotation = Quaternion.Euler(0f, yaw + idleYaw, 0f) * Quaternion.Euler(pitch, 0f, shakeRoll + idleRoll);
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
        public bool Measure(out float height, out float center)
        {
            height = center = 0f;
            if (island == null || island.Content == null) return false;
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
                return false;
            }
            height = Mathf.Max(1e-4f, (b.max.y - feet) / s);
            center = Mathf.Clamp((b.center.y - feet) / s, 0f, height);
            return true;
        }

        float _measuredH, _measuredC, _measuredZoom;
        bool _measured;

        /// <summary>
        /// Step 14j: <see cref="Measure"/> once per spawn, scaled with the zoom since (the battle re-fits every 0.3 s and
        /// a fresh measure jitters 20% with the idle animation: the fit pumped).
        /// </summary>
        public void MeasureCached(out float height, out float center)
        {
            if (!_measured || island == null)
            {
                // Cached only once something is drawn (a pod still opening falls back to the nominal height).
                _measured = Measure(out _measuredH, out _measuredC);
                _measuredZoom = island != null ? Mathf.Max(1e-5f, island.Zoom) : 1f;
            }
            float k = island != null ? island.Zoom / _measuredZoom : 1f;
            height = _measuredH * k;
            center = _measuredC * k;
        }

        /// <summary>Yaw that faces from one map point to another (columns run toward -X, rows toward +Z).</summary>
        public static float YawToward(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            return d.sqrMagnitude < 1e-6f ? 0f : Mathf.Atan2(-d.x, d.y) * Mathf.Rad2Deg;
        }
    }
}
