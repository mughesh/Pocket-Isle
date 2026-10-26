using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The "Pocket Isle" logo: chunky letters on a mini floating island. Step 9h (v2): two props baked at edit time
    /// (PropKit.Logo: the letters, and a mini Still Isle chunk built from the island's own tiles and props), drawn with
    /// the prop shader (2 renderers, 4 draws), not clipped by the island window. The lantern on the I pulses, two specks
    /// drift up past the letters, the base bobs a little on its own. It sits above the
    /// island's far half, bobs and sways a little and turns to face the head. <see cref="Rise"/> brings it in from a
    /// point (the statue, or the middle of a rising island), <see cref="Leave"/> floats it up and away. Unscaled time.
    /// </summary>
    public class TitleLogo : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("The baked logo letters (Data/Props/TitleLogo).")]
        public PropRecipe recipe;
        [Tooltip("Step 9h: the baked mini island the letters stand on (Data/Props/TitleLogoBase), same shape units.")]
        public PropRecipe baseRecipe;
        [Tooltip("Diorama/Prop, window clip off, restored colours (Materials/Props/Logo.mat).")]
        public Material material;
        public BattleFx fx;

        [Header("Placement (island-local metres at island scale 1)")]
        [Tooltip("Width of the whole logo.")]
        public float width = 0.3f;
        [Tooltip("Logo centre this high over the island's top surface.")]
        public float height = 0.25f;
        [Tooltip("Toward the far rim: 0 = the island's middle, 1 = the far edge.")]
        [Range(0f, 1f)] public float far = 0.45f;

        [Header("Motion")]
        public float bob = 0.008f;
        public float swayDegrees = 3f;
        [Tooltip("How fast it turns toward the head (1/s).")]
        public float faceSpeed = 2.5f;
        [Tooltip("The base bobs on its own, this much (share of the logo's width).")]
        public float baseBob = 0.012f;
        [Tooltip("The lantern on the I pulses between these glow amounts.")]
        public Vector2 lanternGlow = new Vector2(0.7f, 1.6f);
        [Tooltip("A drifting speck (ember / sparkle) this often (s); two are in the air at a time.")]
        public float speckEvery = 1.4f;

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");
        static readonly int GlowBId = Shader.PropertyToID("_GlowB");

        Transform _pivot, _base;
        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        Vector3 _baseRest;
        float _nextSpeck;
        float _scale = 1f, _yaw;
        float _show;            // 0..1 size
        Vector3 _offset;        // added to the rest spot (rise / leave paths)
        bool _visible;
        Coroutine _move;

        public bool Visible => _visible;

        void Awake()
        {
            var mesh = recipe != null ? recipe.bakedMesh : null;
            if (mesh == null || material == null)
            {
                Debug.LogError("[TitleLogo] No baked logo mesh or material (Tools > Pocket Isle > Set Up MR_Main (9g title, menus, quick)).", this);
                enabled = false;
                return;
            }
            _pivot = new GameObject("Logo Pivot").transform;
            _pivot.SetParent(transform, false);
            // Both meshes share the shape units: centred on their joint bounds, scaled to the width.
            var b = mesh.bounds;
            var baseMesh = baseRecipe != null ? baseRecipe.bakedMesh : null;
            if (baseMesh != null) b.Encapsulate(baseMesh.bounds);
            else Debug.LogError("[TitleLogo] No baked logo base (Tools > Pocket Isle > Set Up MR_Main (9h first minutes)): letters only.", this);
            _scale = width / Mathf.Max(1e-4f, b.size.x);
            _block = new MaterialPropertyBlock();
            _renderer = Part("Logo Letters", mesh, recipe, b, _block);
            if (baseMesh != null)
            {
                Part("Logo Base", baseMesh, baseRecipe, b, new MaterialPropertyBlock());
                _base = _pivot.Find("Logo Base");
                _baseRest = _base.localPosition;
            }
            _pivot.gameObject.SetActive(false);
        }

        MeshRenderer Part(string name, Mesh mesh, PropRecipe r, Bounds joint, MaterialPropertyBlock block)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_pivot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            r.ApplyTo(block);
            block.SetFloat(WindowClipId, 0f);
            block.SetFloat(RestoredSelfId, 1f);
            block.SetFloat(ClearSinkId, 0f);
            renderer.SetPropertyBlock(block);
            go.transform.localScale = Vector3.one * _scale;
            go.transform.localPosition = -joint.center * _scale;
            return renderer;
        }

        /// <summary>Where the logo rests (island-local): over the far half, <see cref="height"/> above the cap.</summary>
        public Vector3 RestLocal
        {
            get
            {
                if (island == null || island.map == null) return Vector3.up * height;
                var b = island.LocalBounds;
                return new Vector3(b.center.x, island.CapTopLocal + height, Mathf.Lerp(b.center.z, b.min.z, far));
            }
        }

        /// <summary>Draws the logo once, tiny, so its shader pipeline exists before it shows (Vulkan builds them at first draw).</summary>
        public void WarmUp(Vector3 world)
        {
            if (_renderer == null) return;
            _pivot.gameObject.SetActive(true);
            _pivot.position = world;
            _pivot.localScale = Vector3.one * 0.01f;
        }

        public void EndWarmUp()
        {
            if (_renderer == null || _visible) return;
            _pivot.gameObject.SetActive(false);
        }

        /// <summary>Grows in at <paramref name="fromLocal"/> (island-local) and flies to its spot in <paramref name="seconds"/>.</summary>
        public void Rise(Vector3 fromLocal, float seconds, bool sparkle)
        {
            if (_renderer == null) return;
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(RiseRoutine(fromLocal, seconds, sparkle));
        }

        IEnumerator RiseRoutine(Vector3 fromLocal, float seconds, bool sparkle)
        {
            _visible = true;
            _pivot.gameObject.SetActive(true);
            Vector3 from = fromLocal - RestLocal;
            _yaw = HeadYaw();
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / seconds);
                _offset = Vector3.Lerp(from, Vector3.zero, u) + Vector3.up * (0.04f * Mathf.Sin(u * Mathf.PI));
                _show = EaseOutBack(Mathf.Clamp01(t / seconds));
                yield return null;
            }
            _offset = Vector3.zero;
            _show = 1f;
            if (sparkle) Sparkle(28);
            _move = null;
        }

        /// <summary>Floats up and shrinks away (about <paramref name="seconds"/>), with a little sparkle.</summary>
        public void Leave(float seconds = 1.2f)
        {
            if (_renderer == null || !_visible) return;
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(LeaveRoutine(seconds));
        }

        IEnumerator LeaveRoutine(float seconds)
        {
            Sparkle(20);
            Vector3 from = _offset;
            float startShow = _show;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float u = t / seconds;
                _offset = from + Vector3.up * (0.12f * u * u);
                _show = startShow * (1f - Mathf.SmoothStep(0f, 1f, u));
                yield return null;
            }
            Hide();
        }

        public void Hide()
        {
            if (_move != null) StopCoroutine(_move);
            _move = null;
            _visible = false;
            _show = 0f;
            _offset = Vector3.zero;
            if (_pivot != null) _pivot.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!_visible || _pivot == null) return;
            float t = Time.unscaledTime;
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * faceSpeed);
            _yaw = Mathf.LerpAngle(_yaw, HeadYaw(), k);
            _pivot.localPosition = RestLocal + _offset + Vector3.up * (bob * Mathf.Sin(t * 1.3f));
            _pivot.localRotation = Quaternion.Euler(0f, _yaw, swayDegrees * Mathf.Sin(t * 0.7f));
            _pivot.localScale = Vector3.one * Mathf.Max(0.001f, _show);
            Alive(t);
        }

        /// <summary>The lantern on the I pulses, the base bobs on its own, two specks drift up past the letters.</summary>
        void Alive(float t)
        {
            if (_block != null && recipe != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
                _block.SetVector(GlowBId, new Vector4(Mathf.Lerp(lanternGlow.x, lanternGlow.y, k), recipe.Color(PropSlot.Accent).glow, 0f, 0f));
                _renderer.SetPropertyBlock(_block);
            }
            if (_base != null) _base.localPosition = _baseRest + Vector3.up * (baseBob * width * Mathf.Sin(t * 1.05f + 1.3f));
            if (fx == null || _show < 0.95f || t < _nextSpeck) return;
            _nextSpeck = t + speckEvery;
            float s = island != null ? island.transform.lossyScale.x : 1f;
            var side = _pivot.right * (Random.Range(-0.45f, 0.45f) * width * s);
            var at = _pivot.position + side + _pivot.up * (Random.Range(-0.1f, 0.2f) * width * s);
            fx.Burst(at, 1, Random.value < 0.5f ? RuneBoard.Gold : new Color(1f, 0.55f, 0.25f), 0.01f * s, speckEvery * 2f, 0.004f * s, Vector3.up * (0.02f * s));
        }

        /// <summary>The island-local yaw that turns the logo's front (+Z) toward the head.</summary>
        float HeadYaw()
        {
            var cam = Camera.main;
            if (cam == null || island == null) return _yaw;
            Vector3 toHead = island.transform.InverseTransformPoint(cam.transform.position) - RestLocal;
            toHead.y = 0f;
            return toHead.sqrMagnitude < 1e-6f ? _yaw : Mathf.Atan2(toHead.x, toHead.z) * Mathf.Rad2Deg;
        }

        void Sparkle(int count)
        {
            if (fx == null || _pivot == null) return;
            float s = island != null ? island.transform.lossyScale.x : 1f;
            fx.Burst(_pivot.position, count, RuneBoard.Gold, 0.12f * s, 0.9f, 0.004f * s, Vector3.up * 0.04f);
        }

        static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c = 1.7f;
            t -= 1f;
            return 1f + (c + 1f) * t * t * t + c * t * t;
        }
    }
}
