using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>Shader values for one creature mesh (Diorama/Toon). Colours are stored as authored (sRGB).</summary>
    [Serializable]
    public struct PieceStyle
    {
        public const int MaxPatches = 6;

        public Color baseColor;
        public Color top;
        public bool gradient;

        public bool belly;
        public Color bellyColor;
        public Vector3 bellyDirection;
        public float bellyAt;
        public float bellyMaxY;
        public float bellyDrips;
        public float bellyDripLength;

        public bool socks;
        public Color socksColor;
        public float socksY;

        public bool tip;
        public Color tipColor;
        public Vector3 tipAxis;
        public float tipAt;

        public bool spots;
        public Color spotColor;
        public float spotScale;

        public bool cracks;
        public Color crackColor;
        public float crackScale;
        public float crackWidth;
        public float crackPulse;

        [Tooltip("xyz = centre, w = radius (0 = off).")]
        public Vector4[] patches;
        public Color[] patchColors;

        public float spec;
        public float glow;
        public float breathe;
        public float breatheSpeed;
        public float jiggle;
        public float outlineWidth;
        public Color outlineColor;
        [Tooltip("Breathing squashes toward this height (the feet).")]
        public float pivotY;
    }

    /// <summary>
    /// A baked creature (made by the creature baker from a recipe). Paints its meshes through property blocks, so
    /// every creature shares the same few materials, and runs the idle loop: breathing (shader), blinking, part
    /// motions, looking at the player's head when close. <see cref="Hop"/> and <see cref="Faint"/> are for battles.
    /// The root's scale is free for the world window zoom; the size in metres lives on the rig.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // after MapCreature has moved the root this frame
    public class CreatureView : MonoBehaviour
    {
        [Serializable]
        public class Piece
        {
            public Renderer renderer;
            public PieceStyle style;
        }

        [Serializable]
        public class Motion
        {
            public Transform pivot;
            public PartMotion kind;
            public Vector3 axis;
            public float amount;
            public float speed;
            public float phase;
            [Tooltip("Mirrored copy: turns the other way.")]
            public bool mirrored;
            public Vector3 basePosition;
            public Quaternion baseRotation;
            public Vector3 baseScale;
        }

        [Serializable]
        public class Eye
        {
            public Transform transform;
            public Renderer renderer;
            public Vector3 baseScale;
            public Color iris;
        }

        [Header("Baked (written by the baker)")]
        [Tooltip("Origin at the feet; scaled from shape units to metres.")]
        public Transform rig;
        public Vector3 rigPosition;
        public float rigScale = 1f;
        [Tooltip("Body height in metres at scale 1.")]
        public float height;
        [Tooltip("Height of the body centre above the feet, metres at scale 1.")]
        public float centerHeight;
        [Tooltip("Rig height when lying on its side (faint).")]
        public float sideRestHeight;
        public IdleStyle idle;
        public List<Piece> pieces = new List<Piece>();
        public List<Motion> motions = new List<Motion>();
        public List<Eye> eyes = new List<Eye>();

        [Header("Behaviour")]
        [Tooltip("Looks at the player's head within this distance (metres).")]
        public float lookDistance = 1.2f;
        [Tooltip("How far the body turns toward the player, degrees.")]
        public float maxHeadTurn = 30f;
        public float hopSeconds = 0.35f;
        [Tooltip("Hop height in metres at scale 1.")]
        public float hopHeight = 0.012f;

        /// <summary>Off in battle: the creature faces its opponent instead of turning to the player's head.</summary>
        [NonSerialized] public bool lookAtPlayer = true;

        /// <summary>Raised when a hop touches down.</summary>
        public event Action Landed;

        public bool IsHopping => _hopStart >= 0f;
        public bool IsFainted => _fainted;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        static readonly int GradId = Shader.PropertyToID("_Grad");
        static readonly int BellyOnId = Shader.PropertyToID("_BellyOn");
        static readonly int BellyColorId = Shader.PropertyToID("_BellyColor");
        static readonly int BellyDirId = Shader.PropertyToID("_BellyDir");
        static readonly int BellyAtId = Shader.PropertyToID("_BellyAt");
        static readonly int BellyMaxYId = Shader.PropertyToID("_BellyMaxY");
        static readonly int BellyDripsId = Shader.PropertyToID("_BellyDrips");
        static readonly int BellyDripLenId = Shader.PropertyToID("_BellyDripLen");
        static readonly int CrackOnId = Shader.PropertyToID("_CrackOn");
        static readonly int CrackColorId = Shader.PropertyToID("_CrackColor");
        static readonly int CrackScaleId = Shader.PropertyToID("_CrackScale");
        static readonly int CrackWidthId = Shader.PropertyToID("_CrackWidth");
        static readonly int CrackPulseId = Shader.PropertyToID("_CrackPulse");
        static readonly int SockOnId = Shader.PropertyToID("_SockOn");
        static readonly int SockColorId = Shader.PropertyToID("_SockColor");
        static readonly int SockYId = Shader.PropertyToID("_SockY");
        static readonly int TipOnId = Shader.PropertyToID("_TipOn");
        static readonly int TipColorId = Shader.PropertyToID("_TipColor");
        static readonly int TipAxisId = Shader.PropertyToID("_TipAxis");
        static readonly int TipAtId = Shader.PropertyToID("_TipAt");
        static readonly int SpotOnId = Shader.PropertyToID("_SpotOn");
        static readonly int SpotColorId = Shader.PropertyToID("_SpotColor");
        static readonly int SpotScaleId = Shader.PropertyToID("_SpotScale");
        static readonly int SpecId = Shader.PropertyToID("_Spec");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");
        static readonly int BreatheId = Shader.PropertyToID("_Breathe");
        static readonly int BreatheSpeedId = Shader.PropertyToID("_BreatheSpeed");
        static readonly int JiggleId = Shader.PropertyToID("_Jiggle");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int PivotYId = Shader.PropertyToID("_DeformPivotY");
        static readonly int TimeOffsetId = Shader.PropertyToID("_TimeOffset");
        static readonly int WindowClipId = Shader.PropertyToID("_WindowClip");
        static readonly int WindowOriginId = Shader.PropertyToID("_WindowOrigin");
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int IrisId = Shader.PropertyToID("_IrisColor");
        static readonly int LookId = Shader.PropertyToID("_Look");
        static readonly int[] PatchIds = new int[PieceStyle.MaxPatches];
        static readonly int[] PatchColorIds = new int[PieceStyle.MaxPatches];

        static CreatureView()
        {
            for (int i = 0; i < PieceStyle.MaxPatches; i++)
            {
                PatchIds[i] = Shader.PropertyToID("_Patch" + i);
                PatchColorIds[i] = Shader.PropertyToID("_PatchColor" + i);
            }
        }

        MaterialPropertyBlock[] _blocks;
        MaterialPropertyBlock _eyeBlock;
        bool _windowClip;
        float _timeOffset;
        float _breatheScale = 1f;
        float _flash;

        float[] _nextFlick, _flickStart;
        float _nextBlink, _blinkStart = -1f;
        Vector2 _look;
        float _headYaw;
        float _hopStart = -1f;
        bool _landedRaised;
        bool _fainted;
        float _faint;

        void OnEnable()
        {
            _timeOffset = Application.isPlaying ? UnityEngine.Random.value * 10f : 0f;
            ApplyStyles();
        }

        void OnValidate()
        {
            if (!Application.isPlaying) ApplyStyles();
        }

        /// <summary>Clip and sink at the island rim like the map content (on when it stands on the world window).</summary>
        public void SetWindowClip(bool on)
        {
            if (_windowClip == on) return;
            _windowClip = on;
            ApplyStyles();
        }

        /// <summary>A small hop on the spot (a mover can slide the creature to the next tile meanwhile).</summary>
        public void Hop()
        {
            if (_fainted || IsHopping) return;
            _hopStart = Time.time;
            _landedRaised = false;
        }

        /// <summary>Tips over with eyes shut and stops breathing, until <see cref="Revive"/>.</summary>
        public void Faint()
        {
            _fainted = true;
            _hopStart = -1f;
            _breatheScale = 0f;
            ApplyStyles();
        }

        public void Revive()
        {
            _fainted = false;
            _breatheScale = 1f;
            ApplyStyles();
        }

        /// <summary>White hit flash on the body (0 = off, 1 = white).</summary>
        public void SetFlash(float amount)
        {
            if (Mathf.Approximately(_flash, amount)) return;
            _flash = amount;
            if (_blocks == null) return;
            for (int i = 0; i < pieces.Count && i < _blocks.Length; i++)
            {
                if (_blocks[i] == null || pieces[i].renderer == null) continue;
                _blocks[i].SetFloat(FlashId, _flash);
                pieces[i].renderer.SetPropertyBlock(_blocks[i]);
            }
        }

        /// <summary>Writes every piece's palette and zones into its property block.</summary>
        public void ApplyStyles()
        {
            if (_blocks == null || _blocks.Length != pieces.Count) _blocks = new MaterialPropertyBlock[pieces.Count];
            for (int b = 0; b < pieces.Count; b++)
            {
                var p = pieces[b];
                if (p == null || p.renderer == null) continue;
                var s = p.style;
                var block = _blocks[b] ??= new MaterialPropertyBlock();
                block.Clear();
                block.SetVector(BaseColorId, s.baseColor.linear);
                block.SetVector(TopColorId, s.top.linear);
                block.SetFloat(GradId, s.gradient ? 1f : 0f);
                block.SetFloat(BellyOnId, s.belly ? 1f : 0f);
                block.SetVector(BellyColorId, s.bellyColor.linear);
                block.SetVector(BellyDirId, s.bellyDirection);
                block.SetFloat(BellyAtId, s.bellyAt);
                block.SetFloat(BellyMaxYId, s.bellyMaxY);
                block.SetFloat(BellyDripsId, s.bellyDrips);
                block.SetFloat(BellyDripLenId, s.bellyDripLength);
                block.SetFloat(CrackOnId, s.cracks ? 1f : 0f);
                block.SetVector(CrackColorId, s.crackColor.linear);
                block.SetFloat(CrackScaleId, s.crackScale > 0f ? s.crackScale : 2.5f);
                block.SetFloat(CrackWidthId, s.crackWidth > 0f ? s.crackWidth : 0.04f);
                block.SetFloat(CrackPulseId, s.crackPulse);
                block.SetFloat(SockOnId, s.socks ? 1f : 0f);
                block.SetVector(SockColorId, s.socksColor.linear);
                block.SetFloat(SockYId, s.socksY);
                block.SetFloat(TipOnId, s.tip ? 1f : 0f);
                block.SetVector(TipColorId, s.tipColor.linear);
                block.SetVector(TipAxisId, s.tipAxis);
                block.SetFloat(TipAtId, s.tipAt);
                block.SetFloat(SpotOnId, s.spots ? 1f : 0f);
                block.SetVector(SpotColorId, s.spotColor.linear);
                block.SetFloat(SpotScaleId, s.spotScale);
                for (int i = 0; i < PieceStyle.MaxPatches; i++)
                {
                    bool has = s.patches != null && i < s.patches.Length;
                    block.SetVector(PatchIds[i], has ? s.patches[i] : Vector4.zero);
                    block.SetVector(PatchColorIds[i], has && s.patchColors != null && i < s.patchColors.Length ? s.patchColors[i].linear : Color.black);
                }
                block.SetFloat(SpecId, s.spec);
                block.SetFloat(EmissiveId, s.glow);
                block.SetFloat(BreatheId, s.breathe * _breatheScale);
                block.SetFloat(BreatheSpeedId, s.breatheSpeed > 0f ? s.breatheSpeed : 2.6f);
                block.SetFloat(JiggleId, s.jiggle * _breatheScale);
                block.SetFloat(OutlineWidthId, s.outlineWidth);
                block.SetVector(OutlineColorId, s.outlineColor.linear);
                block.SetFloat(PivotYId, s.pivotY);
                block.SetFloat(TimeOffsetId, _timeOffset);
                block.SetFloat(WindowClipId, _windowClip ? 1f : 0f);
                block.SetVector(WindowOriginId, WindowOrigin);
                block.SetFloat(FlashId, _flash);
                p.renderer.SetPropertyBlock(block);
            }
            ApplyEyes();
        }

        // All pieces sink at the island rim by the creature's feet, not each by its own origin (zoomed in, the eyes
        // are centimetres ahead of the body and would stay up while the body sinks).
        Vector4 WindowOrigin
        {
            get
            {
                Vector3 p = transform.position;
                return _windowClip ? new Vector4(p.x, p.y, p.z, 1f) : Vector4.zero;
            }
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (_windowClip && _blocks != null)
            {
                var origin = WindowOrigin;
                for (int i = 0; i < pieces.Count && i < _blocks.Length; i++)
                {
                    if (_blocks[i] == null || pieces[i].renderer == null) continue;
                    _blocks[i].SetVector(WindowOriginId, origin);
                    pieces[i].renderer.SetPropertyBlock(_blocks[i]);
                }
            }
            ApplyEyes();
        }

        void ApplyEyes()
        {
            _eyeBlock ??= new MaterialPropertyBlock();
            var origin = WindowOrigin;
            foreach (var e in eyes)
            {
                if (e == null || e.renderer == null) continue;
                _eyeBlock.Clear();
                _eyeBlock.SetVector(IrisId, e.iris.linear);
                _eyeBlock.SetVector(LookId, _look);
                _eyeBlock.SetFloat(WindowClipId, _windowClip ? 1f : 0f);
                _eyeBlock.SetVector(WindowOriginId, origin);
                e.renderer.SetPropertyBlock(_eyeBlock);
            }
        }

        void Update()
        {
            if (!Application.isPlaying || rig == null) return;
            float t = Time.time;
            float dt = Time.deltaTime;

            AnimateParts(t);
            Blink(t);
            Look(dt);
            PoseRig(t, dt);
        }

        void AnimateParts(float t)
        {
            if (_nextFlick == null || _nextFlick.Length != motions.Count)
            {
                _nextFlick = new float[motions.Count];
                _flickStart = new float[motions.Count];
                for (int i = 0; i < motions.Count; i++)
                {
                    _nextFlick[i] = t + 1f + UnityEngine.Random.value * 3f;
                    _flickStart[i] = -1f;
                }
            }
            for (int i = 0; i < motions.Count; i++)
            {
                var m = motions[i];
                if (m.pivot == null) continue;
                float speed = _fainted ? 0f : m.speed;
                Vector3 axis = m.axis.sqrMagnitude > 1e-6f ? m.axis.normalized : Vector3.right;
                float angle = 0f;
                Vector3 offset = Vector3.zero;
                Vector3 scale = Vector3.one;
                switch (m.kind)
                {
                    case PartMotion.Flick:
                        if (!_fainted && t >= _nextFlick[i])
                        {
                            _flickStart[i] = t;
                            _nextFlick[i] = t + (2.5f + UnityEngine.Random.value * 3f) / Mathf.Max(0.1f, m.speed);
                        }
                        if (_flickStart[i] >= 0f)
                        {
                            float u = (t - _flickStart[i]) / 0.25f;
                            if (u >= 1f) _flickStart[i] = -1f;
                            else angle = Mathf.Sin(u * Mathf.PI) * m.amount;
                        }
                        break;
                    case PartMotion.Wag:
                    case PartMotion.Flap:
                        angle = Mathf.Sin((t + m.phase) * speed * Mathf.PI * 2f) * m.amount;
                        break;
                    case PartMotion.Spin:
                        angle = (t + m.phase) * speed * 360f;
                        break;
                    case PartMotion.Bob:
                        offset = axis * (Mathf.Sin((t + m.phase) * speed * Mathf.PI * 2f) * m.amount);
                        break;
                    case PartMotion.Flicker:
                        float f = Mathf.Sin(t * speed * 13f + m.phase) * 0.7f + Mathf.Sin(t * speed * 23f + m.phase * 2f) * 0.3f;
                        scale = Vector3.one + axis * (f * m.amount);
                        break;
                }
                var q = Quaternion.AngleAxis(angle, axis);
                if (m.mirrored) q = new Quaternion(q.x, -q.y, -q.z, q.w);
                m.pivot.localRotation = m.baseRotation * q;
                m.pivot.localPosition = m.basePosition + m.baseRotation * offset;
                m.pivot.localScale = Vector3.Scale(m.baseScale, scale);
            }
        }

        void Blink(float t)
        {
            if (_nextBlink <= 0f) _nextBlink = t + 1f + UnityEngine.Random.value * 3f;
            if (t >= _nextBlink)
            {
                _blinkStart = t;
                _nextBlink = t + 2.5f + UnityEngine.Random.value * 3f;
            }
            bool closed = _fainted || _blinkStart >= 0f && t - _blinkStart < 0.12f;
            foreach (var e in eyes)
            {
                if (e == null || e.transform == null) continue;
                var s = e.baseScale;
                if (closed) s.y *= 0.1f;
                e.transform.localScale = s;
            }
        }

        void Look(float dt)
        {
            var cam = Camera.main;
            Vector2 lookTarget = Vector2.zero;
            float yawTarget = 0f;
            if (cam != null && !_fainted && lookAtPlayer)
            {
                Vector3 head = cam.transform.position;
                Vector3 focus = transform.TransformPoint(0f, height * 0.6f, 0f);
                if ((head - focus).sqrMagnitude < lookDistance * lookDistance)
                {
                    Vector3 local = transform.InverseTransformPoint(head);
                    yawTarget = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -maxHeadTurn, maxHeadTurn);
                    Vector3 r = rig.InverseTransformPoint(head).normalized;
                    lookTarget = new Vector2(Mathf.Clamp(r.x * 1.4f, -1f, 1f), Mathf.Clamp(r.y * 1.2f - 0.3f, -1f, 1f));
                }
            }
            float k = 1f - Mathf.Exp(-dt * 6f);
            _look = Vector2.Lerp(_look, lookTarget, k);
            _headYaw = Mathf.Lerp(_headYaw, yawTarget, 1f - Mathf.Exp(-dt * 3f));
        }

        void PoseRig(float t, float dt)
        {
            float y = rigPosition.y + idle.hover + Mathf.Sin((t + _timeOffset) * 2.2f) * idle.bob;
            Vector3 squash = Vector3.one;
            if (_hopStart >= 0f)
            {
                float u = (t - _hopStart) / Mathf.Max(0.05f, hopSeconds);
                if (u < 1f)
                {
                    y += hopHeight * 4f * u * (1f - u);
                    float s = 0.08f * Mathf.Sin(u * Mathf.PI);
                    squash = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);
                }
                else
                {
                    if (!_landedRaised)
                    {
                        _landedRaised = true;
                        Landed?.Invoke();
                    }
                    float v = (u - 1f) / 0.3f;
                    if (v >= 1f) _hopStart = -1f;
                    else
                    {
                        float s = 0.12f * Mathf.Sin(v * Mathf.PI);
                        squash = new Vector3(1f + s * 0.5f, 1f - s, 1f + s * 0.5f);
                    }
                }
            }

            _faint = Mathf.MoveTowards(_faint, _fainted ? 1f : 0f, dt / 0.4f);
            float f = Mathf.SmoothStep(0f, 1f, _faint);
            // Lying on its side, the body's half width is its new height above the ground.
            y = Mathf.Lerp(y, rigPosition.y + sideRestHeight - centerHeight, f);
            float roll = Mathf.Sin((t + _timeOffset) * (idle.swaySpeed > 0f ? idle.swaySpeed : 1.2f)) * idle.sway * (1f - f) + 90f * f;

            // The rig origin is at the feet; turn around the body centre instead.
            var rotation = Quaternion.Euler(0f, _headYaw, roll);
            Vector3 c = Vector3.up * centerHeight;
            rig.localPosition = new Vector3(rigPosition.x, y, rigPosition.z) + c - rotation * c;
            rig.localRotation = rotation;
            rig.localScale = squash * rigScale;
        }
    }
}
