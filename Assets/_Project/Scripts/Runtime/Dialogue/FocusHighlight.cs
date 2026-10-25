using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// One focus language for every hint (step 7c): "the Spirit shines its lantern on it".
    /// Gold = do this now: while a dialogue line has a target (<see cref="Dialogue.GetTarget"/>), anything whose origin is
    /// inside the target's sphere gets a gold pulsing outline (Toon, Toon Outline and Prop outlines; pods glow gold at the
    /// rim), a soft gold halo sits on it, and the Spirit's lantern brightens with a short faint cone below it (step 8c: the
    /// old beam line to the target crossed the scene and looked bad). After the line the gold stays for
    /// <see cref="lingerSeconds"/> (or until the target disappears or <see cref="Clear"/> is called); a hint step still
    /// waiting (<see cref="SetPending"/>) then gets a softer pulse every <see cref="softPulseGap"/> seconds.
    /// White = what the gaze is on: selectables call <see cref="Hover"/> each frame; a thin white outline, never gold.
    /// Both go to the shader globals in DioramaWindow.hlsl.
    /// </summary>
    public class FocusHighlight : MonoBehaviour
    {
        [Tooltip("Diorama/Lantern Beam material (halo on the target, short cone below the lantern).")]
        public Material beamMaterial;
        public Color gold = new Color(1f, 0.74f, 0.16f);
        public Color hoverColor = new Color(1f, 1f, 1f);
        [Tooltip("The gold stays this long after the line ends (s).")]
        public float lingerSeconds = 6f;
        [Tooltip("A hint step still waiting pulses softly this often (s).")]
        public float softPulseGap = 4f;
        [Tooltip("Halo diameter as a share of the target's focus sphere diameter.")]
        public float haloSize = 1.5f;
        [Tooltip("Short cone below the lantern: length and width at its open end (metres at island scale 1). Length 0 = no cone.")]
        public float coneLength = 0.035f;
        public float coneWidth = 0.02f;
        [Tooltip("The halo stays this long after the line ends (s).")]
        public float beamLinger = 1.5f;
        public IslandBuilder island;

        static readonly int HighlightId = Shader.PropertyToID("_DioramaHighlight");
        static readonly int HighlightColorId = Shader.PropertyToID("_DioramaHighlightColor");
        static readonly int HoverId = Shader.PropertyToID("_DioramaHover");
        static readonly int HoverColorId = Shader.PropertyToID("_DioramaHoverColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        static FocusHighlight _instance;
        static Transform _lantern;
        static Vector3 _hoverPoint;
        static float _hoverRadius;
        static int _hoverFrame = -10;
        static Func<Vector3?> _pending;
        static float _pendingRadius, _pendingSince;
        static bool _cleared;

        static readonly int RadialId = Shader.PropertyToID("_Radial");

        Dialogue.Target _last;
        float _lastEnded = -999f, _beam, _haloRadius;
        Vector3 _beamTo;
        Mesh _coneMesh, _haloMesh;
        MeshRenderer _coneRenderer, _haloRenderer;
        MaterialPropertyBlock _block;
        readonly Vector3[] _verts = new Vector3[4];

        /// <summary>Where the cone starts (the Spirit's lantern). LanternSpirit sets it.</summary>
        public static void SetLantern(Transform lantern) => _lantern = lantern;

        /// <summary>0..1: the lantern is shining on a hint target now (the Spirit brightens).</summary>
        public static float Shine => _instance != null ? _instance._beam : 0f;

        /// <summary>The gaze is on something at <paramref name="world"/> (radius in world metres): white for this frame.</summary>
        public static void Hover(Vector3 world, float radius)
        {
            _hoverPoint = world;
            _hoverRadius = radius;
            _hoverFrame = Time.frameCount;
            GazeTarget.Mark();
        }

        /// <summary>A hint step waits for its action at this target: after the line's gold, a soft pulse now and then.</summary>
        public static void SetPending(Func<Vector3?> point, float radius)
        {
            _pending = point;
            _pendingRadius = radius;
            _pendingSince = Time.time;
            _cleared = false;
        }

        /// <summary>The hinted thing was used: the gold goes (lingering and pending).</summary>
        public static void Clear()
        {
            _pending = null;
            _cleared = true;
        }

        void OnEnable() => _instance = this;

        void OnDisable()
        {
            if (_instance == this) _instance = null;
            Shader.SetGlobalVector(HighlightId, Vector4.zero);
            Shader.SetGlobalVector(HoverId, Vector4.zero);
        }

        void Start()
        {
            if (beamMaterial == null) Debug.LogError("[FocusHighlight] No beam material: hints show the gold outline but no halo (Set Up MR_Main (7c)).", this);
            _block = new MaterialPropertyBlock();
            _coneRenderer = MakeQuad("Lantern Cone", out _coneMesh, true);
            _haloRenderer = MakeQuad("Lantern Halo", out _haloMesh, false);
        }

        MeshRenderer MakeQuad(string name, out Mesh mesh, bool dynamic)
        {
            mesh = new Mesh { name = name };
            if (dynamic) mesh.MarkDynamic();
            mesh.vertices = dynamic ? _verts : new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = beamMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            go.SetActive(false);
            return r;
        }

        void OnDestroy()
        {
            if (_coneMesh != null) Destroy(_coneMesh);
            if (_haloMesh != null) Destroy(_haloMesh);
        }

        float Scale => island != null ? island.transform.lossyScale.x : 1f;

        void LateUpdate()
        {
            // ---- gold ----
            Vector4 sphere = Vector4.zero;
            float amount = 0f;
            bool beam = false;
            var state = Dialogue.GetTarget(out var world, out var target);
            if (state == Dialogue.TargetState.Active)
            {
                if (target != _last) _cleared = false;
                _last = target;
                _lastEnded = Time.time;
                sphere = new Vector4(world.x, world.y, world.z, RadiusOf(target));
                amount = 0.55f + 0.45f * Mathf.Sin(Time.time * 5f);
                beam = true;
                _beamTo = world;
                _haloRadius = HaloRadius(target);
            }
            else if (!_cleared && _last != null && Time.time - _lastEnded < lingerSeconds && state != Dialogue.TargetState.Lost)
            {
                var p = _last.point != null ? _last.point() : null;
                if (p.HasValue)
                {
                    sphere = new Vector4(p.Value.x, p.Value.y, p.Value.z, RadiusOf(_last));
                    amount = 0.45f + 0.35f * Mathf.Sin(Time.time * 4f);
                    _beamTo = p.Value;
                    _haloRadius = HaloRadius(_last);
                }
            }
            else if (_pending != null && !_cleared)
            {
                var p = _pending();
                if (p.HasValue)
                {
                    // A soft swell every softPulseGap seconds.
                    float u = Mathf.Repeat(Time.time - _pendingSince, Mathf.Max(1f, softPulseGap)) / 1.2f;
                    amount = u < 1f ? 0.6f * Mathf.Sin(u * Mathf.PI) : 0f;
                    sphere = new Vector4(p.Value.x, p.Value.y, p.Value.z, _pendingRadius);
                }
            }
            Shader.SetGlobalVector(HighlightId, amount > 0.001f ? sphere : Vector4.zero);
            Shader.SetGlobalColor(HighlightColorId, new Color(gold.r, gold.g, gold.b, amount));

            // ---- white hover ----
            bool hovering = _hoverFrame >= Time.frameCount - 1;
            Shader.SetGlobalVector(HoverId, hovering ? new Vector4(_hoverPoint.x, _hoverPoint.y, _hoverPoint.z, _hoverRadius) : Vector4.zero);
            Shader.SetGlobalColor(HoverColorId, new Color(hoverColor.r, hoverColor.g, hoverColor.b, hovering ? 1f : 0f));

            UpdateBeam(beam || Time.time - _lastEnded < beamLinger && !_cleared && _last != null);
        }

        float RadiusOf(Dialogue.Target t)
        {
            float r = t != null && t.radius != null ? t.radius() : 0f;
            return r > 0f ? r : 0.035f * Scale;
        }

        /// <summary>Halo radius; none for targets that glow themselves or have no size (rim, ghost, room centre).</summary>
        float HaloRadius(Dialogue.Target t)
        {
            float r = t != null && t.radius != null ? t.radius() : 0.035f * Scale;
            return r > 0.005f * Scale ? r * haloSize : 0f;
        }

        /// <summary>
        /// The lantern's light: a soft gold halo facing the camera on the target, and a short cone from the lantern
        /// toward it that fades out within a few centimetres (it never crosses the scene).
        /// </summary>
        void UpdateBeam(bool on)
        {
            if (_coneRenderer == null) return;
            _beam = Mathf.MoveTowards(_beam, on ? 1f : 0f, Time.deltaTime * 3f);
            var cam = Camera.main;
            bool show = _beam > 0.01f && cam != null;

            bool halo = show && _haloRadius > 0f;
            _haloRenderer.gameObject.SetActive(halo);
            if (halo)
            {
                var ht = _haloRenderer.transform;
                ht.SetPositionAndRotation(_beamTo, Quaternion.LookRotation(_beamTo - cam.transform.position, Vector3.up));
                float parent = ht.parent != null ? ht.parent.lossyScale.x : 1f;
                ht.localScale = Vector3.one * (_haloRadius * 2f / Mathf.Max(1e-5f, parent));
                _block.Clear();
                _block.SetFloat(IntensityId, _beam);
                _block.SetFloat(RadialId, 1f);
                _haloRenderer.SetPropertyBlock(_block);
            }

            bool cone = show && _lantern != null && coneLength > 0f;
            _coneRenderer.gameObject.SetActive(cone);
            if (!cone) return;
            Vector3 a = _lantern.position;
            Vector3 along = _beamTo - a;
            if (along.sqrMagnitude < 1e-8f)
            {
                _coneRenderer.gameObject.SetActive(false);
                return;
            }
            Vector3 b = a + along.normalized * Mathf.Min(along.magnitude * 0.5f, coneLength * Scale);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 side = Vector3.Cross(along, mid - cam.transform.position).normalized;
            float w0 = 0.002f * Scale, w1 = coneWidth * Scale * 0.5f;
            var inv = _coneRenderer.transform.worldToLocalMatrix;
            _verts[0] = inv.MultiplyPoint3x4(a - side * w0);
            _verts[1] = inv.MultiplyPoint3x4(a + side * w0);
            _verts[2] = inv.MultiplyPoint3x4(b - side * w1);
            _verts[3] = inv.MultiplyPoint3x4(b + side * w1);
            _coneMesh.vertices = _verts;
            _coneMesh.RecalculateBounds();
            _block.Clear();
            _block.SetFloat(IntensityId, _beam);
            _block.SetFloat(RadialId, 0f);
            _coneRenderer.SetPropertyBlock(_block);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _lantern = null;
            _hoverFrame = -10;
            _pending = null;
            _cleared = false;
        }
    }
}
