using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// One focus language for every hint (step 7c): "the Spirit shines its lantern on it".
    /// Gold = do this now: while a dialogue line has a target (<see cref="Dialogue.GetTarget"/>), anything whose origin is
    /// inside the target's sphere gets a gold pulsing outline (Toon, Toon Outline and Prop outlines; pods glow gold at the
    /// rim) and a soft beam of warm light runs from the Spirit's lantern to it. After the line the gold stays for
    /// <see cref="lingerSeconds"/> (or until the target disappears or <see cref="Clear"/> is called); a hint step still
    /// waiting (<see cref="SetPending"/>) then gets a softer pulse every <see cref="softPulseGap"/> seconds.
    /// White = what the gaze is on: selectables call <see cref="Hover"/> each frame; a thin white outline, never gold.
    /// Both go to the shader globals in DioramaWindow.hlsl.
    /// </summary>
    public class FocusHighlight : MonoBehaviour
    {
        [Tooltip("Diorama/Lantern Beam material.")]
        public Material beamMaterial;
        public Color gold = new Color(1f, 0.74f, 0.16f);
        public Color hoverColor = new Color(1f, 1f, 1f);
        [Tooltip("The gold stays this long after the line ends (s).")]
        public float lingerSeconds = 6f;
        [Tooltip("A hint step still waiting pulses softly this often (s).")]
        public float softPulseGap = 4f;
        [Tooltip("Beam width at the lantern and at the target (metres at island scale 1).")]
        public float beamStartWidth = 0.003f;
        public float beamEndWidth = 0.02f;
        [Tooltip("The beam stays this long after the line ends (s).")]
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

        Dialogue.Target _last;
        float _lastEnded = -999f, _beam;
        Vector3 _beamTo;
        Mesh _beamMesh;
        MeshRenderer _beamRenderer;
        MaterialPropertyBlock _block;
        readonly Vector3[] _verts = new Vector3[4];

        /// <summary>Where the beam starts (the Spirit's lantern). LanternSpirit sets it.</summary>
        public static void SetLantern(Transform lantern) => _lantern = lantern;

        /// <summary>The gaze is on something at <paramref name="world"/> (radius in world metres): white for this frame.</summary>
        public static void Hover(Vector3 world, float radius)
        {
            _hoverPoint = world;
            _hoverRadius = radius;
            _hoverFrame = Time.frameCount;
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
            if (beamMaterial == null) Debug.LogError("[FocusHighlight] No beam material: hints show the gold outline but no lantern beam (Set Up MR_Main (7c)).", this);
            _block = new MaterialPropertyBlock();
            _beamMesh = new Mesh { name = "Lantern Beam" };
            _beamMesh.MarkDynamic();
            _beamMesh.vertices = _verts;
            _beamMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            _beamMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            var go = new GameObject("Lantern Beam");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = _beamMesh;
            _beamRenderer = go.AddComponent<MeshRenderer>();
            _beamRenderer.sharedMaterial = beamMaterial;
            _beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _beamRenderer.receiveShadows = false;
            go.SetActive(false);
        }

        void OnDestroy()
        {
            if (_beamMesh != null) Destroy(_beamMesh);
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
            }
            else if (!_cleared && _last != null && Time.time - _lastEnded < lingerSeconds && state != Dialogue.TargetState.Lost)
            {
                var p = _last.point != null ? _last.point() : null;
                if (p.HasValue)
                {
                    sphere = new Vector4(p.Value.x, p.Value.y, p.Value.z, RadiusOf(_last));
                    amount = 0.45f + 0.35f * Mathf.Sin(Time.time * 4f);
                    _beamTo = p.Value;
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

        /// <summary>A camera-facing strip from the lantern to the target, wider at the target (a cone seen from the side).</summary>
        void UpdateBeam(bool on)
        {
            if (_beamRenderer == null) return;
            _beam = Mathf.MoveTowards(_beam, on && _lantern != null ? 1f : 0f, Time.deltaTime * 3f);
            var cam = Camera.main;
            bool show = _beam > 0.01f && _lantern != null && cam != null;
            _beamRenderer.gameObject.SetActive(show);
            if (!show) return;
            Vector3 a = _lantern.position, b = _beamTo;
            Vector3 along = b - a;
            if (along.sqrMagnitude < 1e-8f)
            {
                _beamRenderer.gameObject.SetActive(false);
                return;
            }
            // Stop just short of the target so the gold outline stays readable.
            b -= along.normalized * Mathf.Min(along.magnitude * 0.15f, 0.012f * Scale);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 side = Vector3.Cross(along, mid - cam.transform.position).normalized;
            float w0 = beamStartWidth * Scale * 0.5f, w1 = beamEndWidth * Scale * 0.5f;
            var inv = _beamRenderer.transform.worldToLocalMatrix;
            _verts[0] = inv.MultiplyPoint3x4(a - side * w0);
            _verts[1] = inv.MultiplyPoint3x4(a + side * w0);
            _verts[2] = inv.MultiplyPoint3x4(b - side * w1);
            _verts[3] = inv.MultiplyPoint3x4(b + side * w1);
            _beamMesh.vertices = _verts;
            _beamMesh.RecalculateBounds();
            _block.Clear();
            _block.SetFloat(IntensityId, _beam);
            _beamRenderer.SetPropertyBlock(_block);
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
