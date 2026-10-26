using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The "Pocket Isle" logo (step 9g, sheet 17 option 1): chunky letters on a mini floating island, a prop recipe baked
    /// at edit time (PropKit.Logo) and drawn with the prop shader, not clipped by the island window. It sits above the
    /// island's far half, bobs and sways a little and turns to face the head. <see cref="Rise"/> brings it in from a
    /// point (the statue, or the middle of a rising island), <see cref="Leave"/> floats it up and away. Unscaled time.
    /// </summary>
    public class TitleLogo : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("The baked logo prop (Data/Props/TitleLogo).")]
        public PropRecipe recipe;
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

        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int RestoredSelfId = Shader.PropertyToID("_RestoredSelf");
        static readonly int ClearSinkId = Shader.PropertyToID("_ClearSink");

        Transform _pivot;
        MeshRenderer _renderer;
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
            var go = new GameObject("Logo Mesh");
            go.transform.SetParent(_pivot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            recipe.ApplyTo(block);
            block.SetFloat(WindowClipId, 0f);
            block.SetFloat(RestoredSelfId, 1f);
            block.SetFloat(ClearSinkId, 0f);
            _renderer.SetPropertyBlock(block);
            // Centred on the mesh's bounds, scaled to the width.
            var b = mesh.bounds;
            _scale = width / Mathf.Max(1e-4f, b.size.x);
            go.transform.localScale = Vector3.one * _scale;
            go.transform.localPosition = -b.center * _scale;
            _pivot.gameObject.SetActive(false);
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
