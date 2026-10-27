using System;
using System.Collections;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A frightened Keeper's fury (step 9e, Hearthstag): cozy, never gory, but overwhelmed and dangerous. Driven by
    /// <see cref="fury"/> (1 = frightened, 0 = at peace) and <see cref="phase2"/> (below half HP: everything stronger):
    /// - a dark grey-violet smoke aura round the body (its own small particle system, soft alpha billboards) with ember
    ///   sparks (the pooled <see cref="BattleFx"/> dots);
    /// - flame tongues on the crown crystals (one mesh of camera-facing quads, Diorama/Beacon flame column);
    /// - constant trembling (<see cref="StageActor.shake"/>), red-ember eyes (<see cref="CreatureView.eyeGlow"/>) and its
    ///   baked smile turned over into a frown (the <c>Face_Smile</c> piece) until it calms;
    /// - a snort of smoke every few seconds and, while <see cref="idleStomps"/>, a stomp now and then (<see cref="Stomp"/>:
    ///   it rears and slams down, lava cracks spread on the ground, dust, the island's tiles bounce).
    /// <see cref="BurnOff"/> calms it: the aura burns off into rising gold motes, the flames shrink away, the eyes blink
    /// and soften to warm amber, the trembling stops. Needs a <see cref="KeeperLook"/> for the flames (crystal spots).
    /// </summary>
    [RequireComponent(typeof(CreatureView))]
    public class KeeperFury : MonoBehaviour
    {
        public IslandBuilder island;
        public BattleFx fx;
        [Tooltip("Diorama/Smoke: the aura, snorts and stomp dust.")]
        public Material smokeMaterial;
        [Tooltip("Diorama/Beacon (copied): the crown flames and the stomp's lava cracks.")]
        public Material beaconMaterial;

        [Range(0f, 1f)] public float fury = 1f;
        public bool phase2;
        [Tooltip("Stomps now and then on its own (off while it lies dazed, kneels or is shown in a shot).")]
        public bool idleStomps = true;
        public Vector2 snortEvery = new Vector2(3f, 6f);
        public Vector2 stompEvery = new Vector2(7f, 12f);

        [Header("Look")]
        public Color smokeLight = new Color(0.36f, 0.31f, 0.42f, 0.62f);
        public Color smokeDark = new Color(0.15f, 0.13f, 0.19f, 0.8f);
        public Color ember = new Color(1f, 0.45f, 0.12f);
        public Color gold = new Color(1f, 0.84f, 0.42f);
        public Color dust = new Color(0.55f, 0.46f, 0.38f, 0.6f);
        public Color eyeFrightened = new Color(1f, 0.22f, 0.05f, 0.9f);
        public Color eyeCalm = new Color(1f, 0.62f, 0.25f, 0.25f);
        [Tooltip("Trembling in tiles (map offset) at full fury; phase 2 x1.8.")]
        public float trembleTiles = 0.02f;
        [Tooltip("Smoke puffs per second at full fury; phase 2 x1.9.")]
        public float smokeRate = 22f;

        /// <summary>A stomp hit the ground (world point at its feet).</summary>
        public event Action<Vector3> Stomped;

        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int CoreId = Shader.PropertyToID("_Core");

        CreatureView _view;
        StageActor _actor;
        KeeperLook _look;
        ParticleSystem _smoke;
        Mesh _flameMesh, _crackQuad;
        Transform _flames, _cracks;
        MeshRenderer _flameRenderer, _crackRenderer;
        Material _flameMaterial, _crackMaterial;
        readonly Vector3[] _flameVerts = new Vector3[20];
        float _nextSnort, _nextStomp, _nextEmber, _crackStart = -99f;
        Coroutine _stomp;
        float _rearPitch;
        Transform _mouth;
        Quaternion _mouthSmile;

        public bool Stomping => _stomp != null;
        float Height => _view.height;
        float WorldPerLocal => Mathf.Max(1e-5f, transform.lossyScale.x);

        void Awake()
        {
            _view = GetComponent<CreatureView>();
            _actor = GetComponent<StageActor>();
        }

        void Start()
        {
            _look = GetComponent<KeeperLook>();
            foreach (var piece in GetComponentsInChildren<Transform>(true))
                if (piece.name.StartsWith("Face_Smile"))
                {
                    _mouth = piece;
                    _mouthSmile = piece.localRotation;
                    break;
                }
            if (smokeMaterial == null || beaconMaterial == null)
                Debug.LogError("[KeeperFury] Missing the smoke or beacon material (Set Up MR_Main (9e dev panel and fury)): no aura or flames.", this);
            if (smokeMaterial != null) BuildSmoke();
            if (beaconMaterial != null)
            {
                BuildFlames();
                BuildCracks();
            }
            float t = Time.time;
            _nextSnort = t + UnityEngine.Random.Range(snortEvery.x, snortEvery.y);
            _nextStomp = t + UnityEngine.Random.Range(stompEvery.x, stompEvery.y);
        }

        void OnDestroy()
        {
            if (_flameMesh != null) Destroy(_flameMesh);
            if (_crackQuad != null) Destroy(_crackQuad);
            if (_flameMaterial != null) Destroy(_flameMaterial);
            if (_crackMaterial != null) Destroy(_crackMaterial);
            if (_cracks != null) Destroy(_cracks.gameObject);
            if (_actor != null)
            {
                _actor.shake = Vector2.zero;
                _actor.shakeRoll = 0f;
            }
            if (island != null && _stomp != null) island.contentLift = 0f;
        }

        void OnDisable()
        {
            if (_stomp != null)
            {
                StopCoroutine(_stomp);
                _stomp = null;
                if (_actor != null) _actor.pitch -= _rearPitch;
                _rearPitch = 0f;
                if (island != null) island.contentLift = 0f;
            }
            if (_cracks != null) _cracks.gameObject.SetActive(false);
        }

        // ---- building ----

        void BuildSmoke()
        {
            var go = new GameObject("Keeper Smoke");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * _view.centerHeight;
            _smoke = go.AddComponent<ParticleSystem>();
            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            float h = Height;
            var main = _smoke.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 70;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(h * 0.04f, h * 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(h * 0.45f, h * 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(smokeLight, smokeDark);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var shape = _smoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = h * 0.52f;
            shape.scale = new Vector3(1f, 0.8f, 1.2f);
            var vel = _smoke.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(h * 0.22f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            var fade = _smoke.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var size = _smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var emission = _smoke.emission;
            emission.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = smokeMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _smoke.Play();
        }

        void BuildFlames()
        {
            var go = new GameObject("Keeper Crown Flames");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            _flames = go.transform;
            _flameMesh = new Mesh { name = "Crown Flames" };
            _flameMesh.MarkDynamic();
            var uv = new Vector2[20];
            var tris = new int[30];
            for (int i = 0; i < 5; i++)
            {
                uv[i * 4] = new Vector2(0f, 0f);
                uv[i * 4 + 1] = new Vector2(1f, 0f);
                uv[i * 4 + 2] = new Vector2(0f, 1f);
                uv[i * 4 + 3] = new Vector2(1f, 1f);
                int v = i * 4, k = i * 6;
                tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
                tris[k + 3] = v + 1; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
            }
            _flameMesh.vertices = _flameVerts;
            _flameMesh.uv = uv;
            _flameMesh.triangles = tris;
            _flameMesh.bounds = new Bounds(Vector3.up * Height, Vector3.one * Height * 3f);
            go.AddComponent<MeshFilter>().sharedMesh = _flameMesh;
            _flameRenderer = go.AddComponent<MeshRenderer>();
            _flameMaterial = new Material(beaconMaterial) { name = "Keeper Flames" };
            _flameMaterial.SetFloat(ModeId, 0f);
            _flameMaterial.SetColor(ColorId, new Color(1f, 0.32f, 0.08f));
            _flameMaterial.SetColor(CoreId, new Color(1f, 0.85f, 0.45f));
            _flameRenderer.sharedMaterial = _flameMaterial;
            _flameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _flameRenderer.receiveShadows = false;
        }

        void BuildCracks()
        {
            var go = new GameObject("Keeper Stomp Cracks");
            go.layer = gameObject.layer;
            // On the ground, not tipping with the body: a sibling under the island.
            go.transform.SetParent(transform.parent, false);
            _cracks = go.transform;
            _crackQuad = BattleHud.VerticalQuad();
            go.AddComponent<MeshFilter>().sharedMesh = _crackQuad;
            _crackRenderer = go.AddComponent<MeshRenderer>();
            _crackMaterial = new Material(beaconMaterial) { name = "Keeper Cracks", renderQueue = 3001 };
            _crackMaterial.SetFloat(ModeId, 3f);
            _crackMaterial.SetColor(ColorId, new Color(1f, 0.28f, 0.05f));
            _crackMaterial.SetColor(CoreId, new Color(1f, 0.8f, 0.35f));
            _crackRenderer.sharedMaterial = _crackMaterial;
            _crackRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _crackRenderer.receiveShadows = false;
            go.SetActive(false);
        }

        // ---- every frame ----

        void LateUpdate()
        {
            if (_view == null) return;
            float t = Time.time, f = Mathf.Clamp01(fury), p2 = phase2 ? 1f : 0f;
            // Aura.
            if (_smoke != null)
            {
                var emission = _smoke.emission;
                emission.rateOverTime = smokeRate * f * Mathf.Lerp(1f, 1.9f, p2);
            }
            // Embers drifting up off the body.
            if (fx != null && f > 0.05f && t >= _nextEmber)
            {
                _nextEmber = t + Mathf.Lerp(0.35f, 0.12f, f) / Mathf.Lerp(1f, 1.6f, p2);
                Vector3 local = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.3f, 1.05f), UnityEngine.Random.Range(-0.4f, 0.4f)) * Height;
                float w = WorldPerLocal * Height;
                fx.Burst(transform.TransformPoint(local), 2, ember, w * 0.05f, 0.9f, w * 0.022f, Vector3.up * (w * 0.35f));
            }
            // Trembling.
            if (_actor != null)
            {
                float amp = trembleTiles * f * Mathf.Lerp(1f, 1.8f, p2);
                _actor.shake = new Vector2(Mathf.PerlinNoise(t * 23f, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t * 21f) - 0.5f) * (2f * amp);
                _actor.shakeRoll = (Mathf.PerlinNoise(t * 19f, 4.1f) - 0.5f) * 5f * f * Mathf.Lerp(1f, 1.6f, p2);
            }
            // Mouth: a frown while frightened (the baked smile turned over), the smile back once calm.
            if (_mouth != null) _mouth.localRotation = f > 0.5f ? _mouthSmile * Quaternion.Euler(0f, 0f, 180f) : _mouthSmile;
            // Eyes.
            _view.eyeGlow = Color.Lerp(eyeCalm, eyeFrightened, f) * new Color(1f, 1f, 1f, 0.85f + 0.15f * Mathf.Sin(t * 9f) * f);
            UpdateFlames(t, f, p2);
            UpdateCracks(t);
            // Snorts and stomps.
            if (f > 0.3f && t >= _nextSnort)
            {
                _nextSnort = t + UnityEngine.Random.Range(snortEvery.x, snortEvery.y) / Mathf.Lerp(1f, 1.5f, p2);
                if (!_view.IsDazed && !_view.IsFainted) Snort();
            }
            if (idleStomps && f > 0.5f && _stomp == null && t >= _nextStomp && !_view.IsDazed && !_view.IsFainted)
                Stomp();
        }

        void UpdateFlames(float t, float f, float p2)
        {
            if (_flameRenderer == null) return;
            int n = _look != null ? Mathf.Min(5, _look.CrystalCount) : 0;
            bool show = f > 0.02f && n > 0 && !_view.IsFainted;
            _flameRenderer.enabled = show;
            if (!show) return;
            var cam = Camera.main;
            Vector3 up = _flames.InverseTransformDirection(Vector3.up).normalized;
            Vector3 toCam = cam != null ? _flames.InverseTransformDirection(cam.transform.position - _flames.position) : Vector3.forward;
            Vector3 right = Vector3.Cross(up, toCam);
            right = right.sqrMagnitude > 1e-8f ? right.normalized : Vector3.right;
            float h = Height;
            for (int i = 0; i < 5; i++)
            {
                int v = i * 4;
                if (i >= n)
                {
                    _flameVerts[v] = _flameVerts[v + 1] = _flameVerts[v + 2] = _flameVerts[v + 3] = Vector3.zero;
                    continue;
                }
                Vector3 c = _flames.InverseTransformPoint(_look.CrystalWorld(i));
                float flick = 0.75f + 0.25f * Mathf.PerlinNoise(t * 6f + i * 2.3f, i);
                float tall = h * 0.55f * f * flick * Mathf.Lerp(1f, 1.7f, p2) * (i == 2 ? 1.25f : 1f);
                float wide = h * 0.22f * Mathf.Lerp(0.6f, 1f, f) * Mathf.Lerp(1f, 1.35f, p2);
                Vector3 b = c - up * (h * 0.04f);
                _flameVerts[v] = b - right * wide * 0.5f;
                _flameVerts[v + 1] = b + right * wide * 0.5f;
                _flameVerts[v + 2] = b - right * wide * 0.5f + up * tall;
                _flameVerts[v + 3] = b + right * wide * 0.5f + up * tall;
            }
            _flameMesh.vertices = _flameVerts;
            _flameMaterial.SetFloat(IntensityId, Mathf.Lerp(0.8f, 2.6f, f) * Mathf.Lerp(1f, 1.4f, p2));
        }

        void UpdateCracks(float t)
        {
            if (_cracks == null) return;
            float age = t - _crackStart;
            bool on = age >= 0f && age < 2.6f;
            if (_cracks.gameObject.activeSelf != on) _cracks.gameObject.SetActive(on);
            if (!on) return;
            // At its feet on the ground, lying flat in the island's frame.
            Vector3 feet = transform.localPosition;
            if (_actor != null && island != null && island.Content != null)
                feet = island.TileToLocal(_actor.tile + _actor.nudge, island.Content.capHeight + _actor.baseLift + 0.0015f);
            _cracks.localPosition = feet;
            _cracks.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float s = Height * 2.4f * transform.localScale.x;
            _cracks.localScale = new Vector3(s, s, 1f);
            _crackMaterial.SetFloat(FillId, Mathf.SmoothStep(0f, 1f, age / 0.45f));
            _crackMaterial.SetFloat(IntensityId, 2.4f * (1f - Mathf.SmoothStep(0.9f, 2.6f, age)));
        }

        // ---- moments ----

        /// <summary>A snort: a puff of dark smoke and embers from its nose.</summary>
        public void Snort()
        {
            if (_smoke == null) return;
            GameAudio.At(AudioIds.KeeperSnort, transform.position);
            float h = Height;
            Vector3 nose = new Vector3(0f, h * 0.72f, h * 0.42f) - _smoke.transform.localPosition;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < 7; i++)
            {
                p.position = nose + UnityEngine.Random.insideUnitSphere * (h * 0.04f);
                p.velocity = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0.12f, 1f) * (h * UnityEngine.Random.Range(0.5f, 0.9f));
                p.startColor = Color.Lerp(smokeLight, smokeDark, UnityEngine.Random.value);
                p.startSize = h * UnityEngine.Random.Range(0.14f, 0.24f);
                p.startLifetime = UnityEngine.Random.Range(0.7f, 1.1f);
                _smoke.Emit(p, 1);
            }
            if (fx != null)
            {
                float w = WorldPerLocal * h;
                fx.Burst(transform.TransformPoint(nose + _smoke.transform.localPosition), 5, ember, w * 0.2f, 0.5f, w * 0.02f, transform.forward * (w * 0.5f));
            }
        }

        /// <summary>A stomp on its own (rears a little and slams down).</summary>
        public void Stomp() => Stomp(16f);

        /// <summary>Rears up <paramref name="rear"/> degrees and slams down; returns the coroutine (null when one is on).</summary>
        public Coroutine Stomp(float rear)
        {
            if (_stomp != null || _actor == null || !isActiveAndEnabled) return null;
            _stomp = StartCoroutine(StompRoutine(rear));
            return _stomp;
        }

        IEnumerator StompRoutine(float rear)
        {
            _nextStomp = Time.time + UnityEngine.Random.Range(stompEvery.x, stompEvery.y);
            // Rear back (the actor's pitch is shared with the kneel and nod: add and take away our part only).
            yield return Tilt(-rear, 0.38f);
            yield return new WaitForSeconds(0.12f);
            yield return Tilt(4f, 0.08f);
            Impact();
            yield return new WaitForSeconds(0.12f);
            yield return Tilt(0f, 0.3f);
            _stomp = null;
        }

        IEnumerator Tilt(float to, float seconds)
        {
            float from = _rearPitch;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                SetRear(Mathf.SmoothStep(from, to, t / seconds));
                yield return null;
            }
            SetRear(to);
        }

        void SetRear(float value)
        {
            _actor.pitch += value - _rearPitch;
            _rearPitch = value;
        }

        void Impact()
        {
            _crackStart = Time.time;
            GameAudio.At(AudioIds.KeeperStomp, transform.position);
            float h = Height;
            Vector3 feetLocal = Vector3.zero;
            if (_smoke != null)
            {
                var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
                Vector3 baseInSmoke = feetLocal - _smoke.transform.localPosition + Vector3.up * (h * 0.05f);
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    p.position = baseInSmoke + dir * (h * 0.3f);
                    p.velocity = (dir * UnityEngine.Random.Range(0.5f, 0.9f) + Vector3.up * 0.25f) * h;
                    p.startColor = dust * new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.7f, 1f));
                    p.startSize = h * UnityEngine.Random.Range(0.18f, 0.3f);
                    p.startLifetime = UnityEngine.Random.Range(0.7f, 1.2f);
                    _smoke.Emit(p, 1);
                }
            }
            Vector3 feet = transform.TransformPoint(feetLocal);
            if (fx != null)
            {
                float w = WorldPerLocal * h;
                fx.Burst(feet + Vector3.up * (w * 0.05f), 18, ember, w * 0.6f, 0.6f, w * 0.025f, Vector3.up * (w * 0.4f), w);
            }
            if (island != null) StartCoroutine(Bounce(0.0025f));
            Stomped?.Invoke(feet);
        }

        /// <summary>The tiles dip and spring back (the base stays put), like the fallen log's landing.</summary>
        IEnumerator Bounce(float depth)
        {
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                island.contentLift = -depth * Mathf.Exp(-t * 9f) * Mathf.Cos(t * 2f * Mathf.PI * 6f);
                yield return null;
            }
            island.contentLift = 0f;
        }

        /// <summary>
        /// Calms it over <paramref name="seconds"/>: the aura burns off into rising gold motes, the flames shrink away,
        /// the eyes blink and soften to warm amber, the trembling stops.
        /// </summary>
        public IEnumerator BurnOff(float seconds)
        {
            idleStomps = false;
            float start = fury;
            _view.BlinkNow();
            bool blinked = false;
            float h = Height;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                fury = start * (1f - Mathf.SmoothStep(0f, 1f, k));
                if (!blinked && k > 0.55f)
                {
                    _view.BlinkNow();
                    blinked = true;
                }
                if (fx != null)
                {
                    float w = WorldPerLocal * h;
                    int count = Mathf.RoundToInt(Mathf.Lerp(3f, 0.6f, k) + UnityEngine.Random.value);
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 local = new Vector3(UnityEngine.Random.Range(-0.45f, 0.45f), UnityEngine.Random.Range(0.15f, 1.1f), UnityEngine.Random.Range(-0.45f, 0.45f)) * h;
                        fx.Burst(transform.TransformPoint(local), 1, gold, w * 0.06f, 1.4f, w * 0.03f, Vector3.up * (w * 0.55f));
                    }
                }
                yield return null;
            }
            fury = 0f;
            if (_actor != null)
            {
                _actor.shake = Vector2.zero;
                _actor.shakeRoll = 0f;
            }
        }
    }
}
