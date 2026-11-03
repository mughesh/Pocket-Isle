using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 13 B: the Heart of the World, the statue come alive, standing on the statue's spot on the hub. Four baked
    /// props (PropKit.Heart: the body, the head eyes closed or open, a wing drawn twice, the right one mirrored), posed
    /// every frame in the statue's shape space: it breathes (the body's height pulses, the head rides up), blinks (a short
    /// swap to the closed head), turns its head toward the player and nods, unfolds its wings, its tail's wind ribbon
    /// sways (in the shader), its veins pulse like a heartbeat from the chest core, the crown's three pieces and the
    /// collar's three gems light up. <see cref="FinaleDirector"/> drives the pose through the public values; after the
    /// finale it stays awake (<see cref="FinaleLogic.DoneId"/>). The statue isn't drawn while it shows
    /// (<see cref="StatueSocket.HideStatue"/>). 8 draw calls while shown.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class HeartOfWorld : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("The hub: it only shows while this map is outside.")]
        public WorldMap map;
        [Tooltip("The statue's drawing socket (its centre and turn).")]
        public StatueSocket statue;
        public PropRecipe bodyRecipe, headRecipe, headOpenRecipe, wingRecipe;
        [Tooltip("Step 14f: the gold crystal set in the forehead (head shape space, rides the head).")]
        public PropRecipe crystalRecipe;

        [Header("Rig (body shape units, mirror PropKit.Heart)")]
        public Vector3 headPivot = new Vector3(0f, 1.86f, 0.1f);
        public Vector3 wingHinge = new Vector3(0.34f, 1.62f, -0.3f);
        public Vector3 foreheadSocket = new Vector3(0f, 2.37f, 0.57f);
        public Vector3 chestCore = new Vector3(0f, 1.0f, 0.5f);
        [Tooltip("The left wing folded on its back / unfolded: x = pitch, y = yaw (back), z = roll (up); applied yaw, then roll, then pitch. The right is mirrored.")]
        public Vector3 wingFolded = new Vector3(0f, 70f, -62f), wingOpen = new Vector3(0f, 12f, 28f);

        [Header("Life")]
        public float breathe = 0.012f, breathSeconds = 4.2f;
        [Tooltip("Seconds between blinks (random in this range) and a blink's length.")]
        public Vector2 blinkEvery = new Vector2(3.5f, 6.5f);
        public float blinkSeconds = 0.14f;
        [Tooltip("Head turn toward the player at most (degrees) and how high it can look.")]
        public float maxYaw = 28f, maxPitch = 16f;
        [Tooltip("Heartbeat: beats per second and the glow it adds to the veins.")]
        public float beatRate = 0.9f, beatGlow = 0.9f;

        /// <summary>0 hidden .. 1 full size (the wake grows it out of the statue).</summary>
        [NonSerialized] public float appear;
        /// <summary>0 closed .. 1 open (the open head shows from 0.5; blinks while open).</summary>
        [NonSerialized] public float eyes;
        /// <summary>0 folded .. 1 unfolded.</summary>
        [NonSerialized] public float wings;
        /// <summary>0 looks ahead .. 1 follows the player's head.</summary>
        [NonSerialized] public float look;
        /// <summary>Head lifted (degrees up) and a nod on top (degrees down).</summary>
        [NonSerialized] public float lift, nod;
        /// <summary>Vein glow 0..1 (pulses with the heartbeat), crown pieces and collar gems 0..1 (fire, water, wind).</summary>
        [NonSerialized] public float veins;
        [NonSerialized] public Vector3 crown, gems;
        /// <summary>Extra: the crown and the veins blaze (the eyes opening).</summary>
        [NonSerialized] public float blaze;
        /// <summary>Step 14f: the forehead crystal, 0 dark (faint gold stone) .. 1 lit by the Spirit (pulses with the heartbeat).</summary>
        [NonSerialized] public float crystal;

        PropView _body, _head, _wingL, _wingR, _crystal;
        float _blinkAt, _blinkUntil, _yaw, _pitch;

        public static HeartOfWorld Instance { get; private set; }

        public bool Here => island != null && map != null && island.world == map && !SceneSwap.IsInside && !SceneSwap.IsBusy && island.Content != null;
        public bool Showing => appear > 0.001f && Here;

        Vector2 Center => statue != null ? statue.Center : Vector2.zero;
        Quaternion BodyRot => Quaternion.Euler(0f, statue != null ? statue.Yaw : 0f, 0f);
        Quaternion HeadRot => Quaternion.Euler(nod - lift + _pitch, _yaw, 0f);
        float Breath => 1f + breathe * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.5f, breathSeconds));
        float Size => Mathf.Lerp(0.82f, 1f, Mathf.SmoothStep(0f, 1f, appear));

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _body?.Destroy();
            _head?.Destroy();
            _wingL?.Destroy();
            _wingR?.Destroy();
            _crystal?.Destroy();
        }

        void Start()
        {
            if (island == null || map == null || statue == null || bodyRecipe == null || headRecipe == null || headOpenRecipe == null || wingRecipe == null)
            {
                Debug.LogError("[HeartOfWorld] Needs the island, the hub map, the statue socket and the four Heart props (Set Up MR_Main (13 finale)).", this);
                enabled = false;
                return;
            }
            _body = new PropView("Heart (body)", island, bodyRecipe) { restoredSelf = 1f };
            _head = new PropView("Heart (head)", island, headRecipe) { restoredSelf = 1f };
            _wingL = new PropView("Heart (wing L)", island, wingRecipe) { restoredSelf = 1f };
            _wingR = new PropView("Heart (wing R)", island, wingRecipe) { restoredSelf = 1f };
            if (crystalRecipe != null) _crystal = new PropView("Heart (crystal)", island, crystalRecipe) { restoredSelf = 1f };
            else Debug.LogError("[HeartOfWorld] No forehead crystal recipe (Set Up MR_Main (14f menus and fixes)): the forehead stays empty.", this);
            _blinkAt = Time.time + 4f;
        }

        void Update()
        {
            if (_body == null) return;
            bool show = Showing;
            if (!show)
            {
                _body.Hide();
                _head.Hide();
                _wingL.Hide();
                _wingR.Hide();
                _crystal?.Hide();
                return;
            }
            Look();
            float size = Size, breath = Breath;
            var body = BodyRot;
            _body.Place(Center, 0f, body, true, new Vector3(size, size * breath, size));

            // The head turns about its pivot and rides the breath.
            var head = HeadRot;
            Vector3 pivot = Scaled(headPivot, size, breath);
            PlaceAt(_head, pivot - head * (headPivot * size), body * head, Vector3.one * size);
            _head.SetRecipe(EyesShowOpen ? headOpenRecipe : headRecipe);
            if (_crystal != null) PlaceAt(_crystal, pivot - head * (headPivot * size), body * head, Vector3.one * size);

            // The wings fold flat on its back or open out and up; the right one is the left mirrored.
            var w = Quaternion.Slerp(WingRot(wingFolded), WingRot(wingOpen), Mathf.SmoothStep(0f, 1f, wings));
            Vector3 hinge = Scaled(wingHinge, size, breath);
            PlaceAt(_wingL, hinge, body * w, Vector3.one * size);
            PlaceAt(_wingR, new Vector3(-hinge.x, hinge.y, hinge.z), body * new Quaternion(w.x, -w.y, -w.z, w.w), new Vector3(-size, size, size));

            Glow();
        }

        static Quaternion WingRot(Vector3 e) => Quaternion.Euler(0f, e.y, 0f) * Quaternion.Euler(0f, 0f, e.z) * Quaternion.Euler(e.x, 0f, 0f);

        /// <summary>The eyes show open (from half open), except during a blink.</summary>
        bool EyesShowOpen
        {
            get
            {
                if (eyes < 0.5f) return false;
                if (Time.time >= _blinkAt)
                {
                    _blinkUntil = Time.time + blinkSeconds;
                    _blinkAt = Time.time + UnityEngine.Random.Range(blinkEvery.x, blinkEvery.y);
                }
                return Time.time >= _blinkUntil;
            }
        }

        /// <summary>The head eases toward the player's head (within its turn limits), by <see cref="look"/>.</summary>
        void Look()
        {
            float yaw = 0f, pitch = 0f;
            var cam = Camera.main;
            if (cam != null && look > 0f)
            {
                Vector3 h = island.transform.InverseTransformPoint(cam.transform.position) - ShapeLocal(headPivot);
                h = Quaternion.Inverse(BodyRot) * h;
                yaw = Mathf.Clamp(Mathf.Atan2(h.x, h.z) * Mathf.Rad2Deg, -maxYaw, maxYaw) * look;
                pitch = -Mathf.Clamp(Mathf.Atan2(h.y, new Vector2(h.x, h.z).magnitude) * Mathf.Rad2Deg * 0.5f, -maxPitch, maxPitch) * look;
            }
            float k = 1f - Mathf.Exp(-Time.deltaTime * 2.5f);
            _yaw = Mathf.Lerp(_yaw, yaw, k);
            _pitch = Mathf.Lerp(_pitch, pitch, k);
        }

        void Glow()
        {
            float beat = Heartbeat(Time.time * beatRate);
            float v = Mathf.Clamp01(veins) * (0.5f + beatGlow * beat) + blaze * 1.2f;
            float b = blaze * 1.5f;
            _body.glowBoostA = new Vector4(gems.x * 1.4f + b, gems.y * 1.4f + b, 0f, 0f);
            _body.glowBoostB = new Vector4(v, gems.z * 1.1f + b * 0.6f, 0f, 0f);
            _head.glowBoostA = new Vector4(crown.x * 1.6f + b, crown.y * 1.3f + b, 0f, 0f);
            _head.glowBoostB = new Vector4(v + (eyes > 0.5f ? 0.6f : 0f), crown.z * 1.1f + b * 0.6f, 0f, 0f);
            _wingL.glowBoostB = _wingR.glowBoostB = new Vector4(v * 0.8f, wings * 0.5f + b * 0.4f, 0f, 0f);
            if (_crystal != null)
            {
                // Glow = the gem, Accent = its table facet.
                float c = Mathf.Clamp01(crystal);
                _crystal.glowBoostB = new Vector4(c * (2.2f + 0.9f * beat), c * (1.6f + 0.7f * beat), 0f, 0f);
            }
        }

        /// <summary>A double thump (lub-dub) once per cycle, 0..1.</summary>
        public static float Heartbeat(float cycles)
        {
            float f = cycles - Mathf.Floor(cycles);
            float a = Mathf.Exp(-Mathf.Pow((f - 0.08f) / 0.05f, 2f));
            float c = 0.7f * Mathf.Exp(-Mathf.Pow((f - 0.28f) / 0.06f, 2f));
            return Mathf.Clamp01(a + c);
        }

        Vector3 Scaled(Vector3 p, float size, float breath) => new Vector3(p.x * size, p.y * size * breath, p.z * size);

        /// <summary>Places a view whose mesh origin sits at <paramref name="offset"/> (body shape units, unturned) from the centre.</summary>
        void PlaceAt(PropView view, Vector3 offset, Quaternion rotation, Vector3 scale)
        {
            var r = BodyRot * offset;
            view.Place(Center + new Vector2(-r.x, r.z), r.y, rotation, true, scale);
        }

        // ---- points (world) ----

        /// <summary>A point in the body's shape space (as posed now) in island-local space.</summary>
        Vector3 ShapeLocal(Vector3 shape)
        {
            var r = BodyRot * shape;
            return island.TileToLocal(Center + new Vector2(-r.x, r.z), island.Content.capHeight + r.y * island.map.tileSize);
        }

        /// <summary>A point on the head (shape units, eyes forward) where it is now.</summary>
        public Vector3 HeadPointWorld(Vector3 shape)
        {
            if (!Here) return transform.position;
            float size = Size, breath = Breath;
            Vector3 p = Scaled(headPivot, size, breath) + HeadRot * ((shape - headPivot) * size);
            return island.transform.TransformPoint(ShapeLocal(p));
        }

        /// <summary>A point on the body (shape units) where it is now.</summary>
        public Vector3 BodyPointWorld(Vector3 shape) => Here ? island.transform.TransformPoint(ShapeLocal(Scaled(shape, Size, Breath))) : transform.position;

        /// <summary>The forehead's empty setting, where the Spirit goes.</summary>
        public Vector3 SocketWorld => HeadPointWorld(foreheadSocket);
        public Vector3 ChestWorld => BodyPointWorld(chestCore);
        /// <summary>One shape unit (a tile) in world metres right now.</summary>
        public float UnitWorld => island != null && island.map != null ? island.map.tileSize * island.Zoom * island.transform.lossyScale.x : 0.05f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
