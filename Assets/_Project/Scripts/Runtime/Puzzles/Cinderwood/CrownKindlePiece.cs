using System.Collections;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 34, the Forge Hall (Mugesh's design): Hearthstag kneels like a hurt deer (a second bake of it,
    /// <c>Hearthstag_Kneel</c>: legs folded under it, head low, the frown), the fire on its crown nearly out: a tiny
    /// sputtering wisp, the crystals dark. <b>Cup your hands round the dying fire</b>: the village flame's cup
    /// (<see cref="CupMeter"/>, <see cref="CupRules"/>: both palms turned toward it ~2 s, one hand curled round it ~3 s,
    /// or look + pinch-hold 3 s) with the wide bright hold ring and the rising tone. While held the fire grows and sparks
    /// rise between the fingers; let go and it shrinks back. Full: the crown fire roars up, the stag rises under the
    /// flames (the hall's standing model) and rears with an angry bellow and a stomp, then the meeting and the battle
    /// (<see cref="KeeperHall"/>). Saved: <see cref="HandPuzzle.id"/> "cw.kindle", stage 1.
    /// </summary>
    [DefaultExecutionOrder(-20)] // before the gaze walk and tile touch read the held hands
    public class CrownKindlePiece : KeeperWakePiece
    {
        [Header("Cup (metres at the placed island size; the village flame's numbers)")]
        public float radius = 0.14f;
        [Range(10f, 90f)] public float facingAngle = 75f;
        public float keepRadius = 1.3f, keepAngle = 15f;
        public float twoHandSeconds = 2f, oneHandSeconds = 3f, lookSeconds = 3f;

        [Header("Dying fire")]
        [Tooltip("The crown flames' size while nearly out, and when the ring is full (x the raging fire).")]
        public Vector2 fireSize = new Vector2(0.1f, 0.95f);
        [Tooltip("Crystal brightness while nearly out.")]
        public float darkCrystals = 0.08f;

        [Header("Hold ring (Diorama/Beacon, as the shrines' step-30 ring)")]
        public Material ringMaterial;
        [Tooltip("Ring size in body heights, its band width and brightness.")]
        public float ringSize = 1.5f, ringBand = 0.24f, ringIntensity = 2.2f;

        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int BandId = Shader.PropertyToID("_Band");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        readonly CupMeter _meter = new CupMeter();
        readonly bool[] _cupping = new bool[2];
        LoopAnchor _tone;
        Transform _ring;
        MeshRenderer _ringRenderer;
        Mesh _quad;
        MaterialPropertyBlock _block;
        bool _gazed, _lookHold;
        float _shown, _sparkAt, _debugUntil;

        public CupMeter Meter => _meter;
        public override bool Busy => _meter.Held;
        public override float FocusRadius => 0.6f * TileWorld;
        protected override bool GhostBothHands => true;
        protected override float GhostSpread => 0.11f * Scale;

        /// <summary>The crown (the dying fire), world.</summary>
        Vector3 Crown(StageActor a)
        {
            var look = a == Body ? BodyLook : keeper != null ? keeper.Look : null;
            return look != null && look.CrystalCount > 0 ? look.CrownWorld : a.WorldCenter + island.transform.up * (BodyHeight(a) * 0.5f);
        }

        public override Vector3? FocusPoint
        {
            get
            {
                var a = Actor;
                return Here && Available && a != null ? Crown(a) : (Vector3?)null;
            }
        }

        /// <summary>The crown fire's size for a cup progress (pure, tested): nearly out at 0, almost raging when full.</summary>
        public static float FireSize(Vector2 size, float progress) => Mathf.Lerp(size.x, size.y, Mathf.Clamp01(progress));

        protected override void Start()
        {
            _meter.twoHands = twoHandSeconds;
            _meter.oneHand = oneHandSeconds;
            _meter.look = lookSeconds;
            _block = new MaterialPropertyBlock();
            if (ringMaterial != null && island != null)
            {
                _quad = BattleHud.VerticalQuad();
                var go = new GameObject("Crown Hold Ring");
                go.layer = island.gameObject.layer;
                go.transform.SetParent(island.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _quad;
                _ringRenderer = go.AddComponent<MeshRenderer>();
                _ringRenderer.sharedMaterial = ringMaterial;
                _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _ringRenderer.receiveShadows = false;
                _ring = go.transform;
                go.SetActive(false);
            }
            else Debug.LogError("[CrownKindlePiece] No ring material (Diorama/Beacon): the hold ring won't show.", this);
            base.Start();
        }

        protected override void OnDestroy()
        {
            _tone?.Off(0f);
            if (_ring != null) Destroy(_ring.gameObject);
            if (_quad != null) Destroy(_quad);
            base.OnDestroy();
        }

        protected override void OnDisable()
        {
            _tone?.Off(0f);
            if (_ring != null) _ring.gameObject.SetActive(false);
            base.OnDisable();
        }

        protected override void ApplyState()
        {
            _meter.Restore(IsSolved);
            _shown = IsSolved ? 1f : 0f;
            SetProgressQuiet(IsSolved ? 1f : 0f);
        }

        /// <summary>Hurt and still: the frown, no aura, no trembling, the fire nearly out.</summary>
        protected override void Dress(StageActor body)
        {
            BodyFury.fury = 0.6f; // over a half: the baked smile turns into the frown
            BodyFury.auraScale = 0f;
            BodyFury.flameScale = fireSize.x;
            BodyLook.SetAllCrystals(darkCrystals);
            BodyLook.Snap();
            body.View.lookAtPlayer = false; // head low, eyes on the floor
        }

        protected override void Pose(StageActor body)
        {
            // The fire follows the cup: grows while held, shrinks back when let go.
            _shown = Mathf.MoveTowards(_shown, Waking ? 1f : _meter.Progress, Time.deltaTime * 3f);
            if (!Waking) BodyFury.flameScale = FireSize(fireSize, _shown);
            BodyLook.SetAllCrystals(Mathf.Lerp(darkCrystals, 1f, _shown));
            // Slow, shallow breathing.
            float breath = Mathf.Sin(Time.time * 0.9f);
            body.squash = new Vector3(1f - 0.008f * breath, 1f + 0.02f * breath, 1f - 0.008f * breath);
        }

        protected override void Rest()
        {
            _gazed = false;
            _cupping[0] = _cupping[1] = false;
            if (!IsSolved) _meter.Feed(Time.deltaTime, 0, false, false);
            Feedback(null);
        }

        protected override void Work(StageActor actor)
        {
            Vector3 flame = Crown(actor);
            float r = radius * Mathf.Max(1f, Scale);
            int facing = 0;
            bool wrapped = false;
            for (int h = 0; h < 2; h++)
            {
                bool was = _cupping[h];
                _cupping[h] = false;
                if (!Hands[h].tracked || Hands[h].busy) continue;
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (!HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var palm)) continue;
                Vector3 normal = palm.rotation * Vector3.down;
                // Sticky: a hand already in stays in a little farther and more turned (tracking wobble).
                float rr = was ? r * keepRadius : r, aa = was ? facingAngle + keepAngle : facingAngle;
                if (!CupRules.Facing(palm.position, normal, flame, rr, aa)) continue;
                _cupping[h] = true;
                facing++;
                if (HandLookup.TryGetJoint(hand, HandJointId.HandMiddleTip, out var middle) && CupRules.Wraps(palm.position, normal, middle, flame, r, facingAngle))
                    wrapped = true;
                IslandHandle.MarkHandHeld(h == 0);
                TileTouch.Block(h == 0, 0.4f);
            }
            if (Time.time < _debugUntil) _lookHold = true; // bridge: as a look + pinch-hold
            else
            {
                _lookHold = false;
                if (facing == 0) LookHold(flame);
                else _gazed = false;
            }
            bool wasHeld = _meter.Held;
            bool done = _meter.Feed(Time.deltaTime, facing, wrapped, _lookHold);
            if (_meter.Held && !wasHeld)
            {
                Touched();
                Debug.Log($"[CrownKindlePiece] {id}: cupped ({(facing >= 2 ? "two hands" : wrapped ? "one hand round it" : _lookHold ? "look + pinch-hold" : "one palm")}).");
            }
            if (!_meter.Held && wasHeld) Debug.Log($"[CrownKindlePiece] {id}: let go at warmth {_meter.Progress:0.00}: the fire shrinks back.");
            SetProgress(_meter.Progress);
            if (_meter.Held && Time.time >= _sparkAt && fx != null)
            {
                // Sparks rise between the fingers while it's held.
                _sparkAt = Time.time + 0.09f;
                float tile = TileWorld;
                fx.Burst(flame + island.transform.up * tile * 0.1f, 2, new Color(1f, 0.66f, 0.28f), tile * 0.25f, 0.8f, tile * 0.05f, island.transform.up * tile * 1.1f);
            }
            Feedback(actor);
            if (!done) return;
            Debug.Log($"[CrownKindlePiece] {id}: the ring is full: the crown fire roars up.");
            GameAudio.At(HearthShrines.HoldDoneCue, flame).SetVolume(0.8f);
            Feedback(null);
            Waking = true;
            LessonComplete();
            MarkSolved(1);
        }

        void LookHold(Vector3 flame)
        {
            if (!HeadGaze.TryGetRay(out var ray))
            {
                _gazed = false;
                return;
            }
            var pick = new HeadGaze.ConePick(ray, _gazed ? 0 : -1);
            pick.Consider(0, flame, Mathf.Max(HeadGaze.MinRadius * Scale, 0.9f * TileWorld));
            _gazed = pick.Best == 0;
            if (!_gazed) return;
            FocusHighlight.Hover(flame, FocusRadius);
            foreach (var h in Hands)
                if (h.pinch && !h.busy) _lookHold = true;
        }

        /// <summary>The hold ring round the crown and the rising tone, while it's held (null = off).</summary>
        void Feedback(StageActor actor)
        {
            bool held = actor != null && !IsSolved && _meter.Held;
            if (_ring != null)
            {
                bool show = actor != null && !IsSolved && _meter.Progress > 0.01f;
                if (_ring.gameObject.activeSelf != show) _ring.gameObject.SetActive(show);
                if (show)
                {
                    Vector3 local = island.transform.InverseTransformPoint(Crown(actor));
                    var cam = Camera.main;
                    Vector3 to = cam != null ? island.transform.InverseTransformPoint(cam.transform.position) - local : Vector3.forward;
                    _ring.localPosition = local;
                    _ring.localRotation = to.sqrMagnitude > 1e-8f ? Quaternion.LookRotation(-to.normalized, Vector3.up) : Quaternion.identity;
                    float size = actor.View.height * island.Zoom * actor.grow * ringSize * (0.9f + 0.1f * _meter.Progress);
                    _ring.localScale = Vector3.one * size;
                    _block.Clear();
                    _block.SetFloat(ModeId, 1f);
                    _block.SetFloat(FillId, _meter.Progress);
                    _block.SetFloat(BandId, ringBand);
                    _block.SetFloat(IntensityId, ringIntensity);
                    _ringRenderer.SetPropertyBlock(_block);
                }
            }
            if (!AudioService.Has(AudioIds.FlameCup)) return;
            _tone ??= new LoopAnchor(transform, "Crown Kindle Sound");
            if (!held)
            {
                _tone.Off(0.5f);
                return;
            }
            _tone.Set(AudioIds.FlameCup, Crown(actor), 0.3f);
            _tone.Handle.SetVolume(0.55f + 0.45f * _meter.Progress);
            _tone.Handle.SetPitch(0.92f + 0.3f * _meter.Progress);
        }

        /// <summary>The crown fire roars up big, the stag rises under the flames and embers, rears with an angry bellow and stomps.</summary>
        public override IEnumerator WakeBeat(KeeperHall hall)
        {
            float tile = TileWorld;
            Vector3 crown = Body != null ? Crown(Body) : hall.Model.WorldCenter;
            GameAudio.At(AudioIds.FlameRekindle, crown).SetVolume(volume);
            if (fx != null) fx.Burst(crown, 50, new Color(1f, 0.6f, 0.2f), tile * 1.2f, 1f, tile * 0.07f, island.transform.up * tile * 1.2f);
            for (float t = 0f; t < 0.45f && Body != null; t += Time.deltaTime)
            {
                BodyFury.flameScale = Mathf.Lerp(fireSize.y, 1.8f, t / 0.45f);
                yield return null;
            }
            SwapToStanding(hall);
            var model = hall.Model;
            if (model == null) yield break;
            if (hall.Fury != null)
            {
                hall.Fury.fury = 1f;
                hall.Fury.flameScale = 1.5f;
            }
            if (hall.Look != null) hall.Look.SetAllCrystals(1f);
            if (fx != null) fx.Burst(model.WorldCenter, 40, new Color(1f, 0.45f, 0.12f), tile * 1.4f, 0.9f, tile * 0.06f, island.transform.up * tile * 0.8f);
            // It rises: up off its knees with a small overshoot.
            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                float y = Mathf.Lerp(0.62f, 1f, k) + 0.08f * Mathf.Sin(k * Mathf.PI);
                model.squash = new Vector3(Mathf.Lerp(1.08f, 1f, k), y, Mathf.Lerp(1.08f, 1f, k));
                yield return null;
            }
            model.squash = Vector3.one;
            GameAudio.At(AudioIds.VoiceCallHearthstag, model.WorldCenter);
            Debug.Log($"[CrownKindlePiece] {id}: {hall.KeeperLabel} rises under the flames and rears with an angry bellow.");
            var stomp = hall.Fury != null ? hall.Fury.Stomp(26f) : null;
            if (stomp != null) yield return stomp;
            else yield return new WaitForSeconds(1f);
            if (hall.Fury != null) hall.Fury.flameScale = 1.2f;
        }

        /// <summary>Bridge, smoke run: a look + pinch-hold for a little longer than it needs.</summary>
        public override void DebugLookPinch()
        {
            if (!Here || IsSolved || Waking || !Available || Time.time < _debugUntil) return;
            _debugUntil = Time.time + lookSeconds + 0.5f;
            Debug.Log($"[CrownKindlePiece] {id}: look + pinch-hold for {lookSeconds + 0.5f:0.#} s (bridge; usable {Usable}).");
        }

        public override string Describe() => $"warmth {_meter.Progress:0.00}, held {_meter.Held}, before body {(BeforeShown ? "shown" : "hidden")}, available {Available}, usable {Usable}, hints {Hints}";
    }
}
