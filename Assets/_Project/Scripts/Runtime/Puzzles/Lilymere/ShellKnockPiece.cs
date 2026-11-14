using System.Collections;
using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 34, the lake temple: Tidemother has pulled its head and flippers into its shell (a second bake of it,
    /// <c>Tidemother_Shell</c>), the shell slowly rising and settling, a few bubbles. <b>Knock on the shell 3 times</b>: a
    /// fingertip touching it, or look + pinch on it. Each knock: a hollow knock (rising pitch), the shell wobbles, a ring
    /// of ripples and bubbles, one of the three dots over the shell turns gold. On the third the head and flippers burst
    /// out under a splash (the hall's standing model), it rears with its call, then the meeting and the battle
    /// (<see cref="KeeperHall"/>). Saved: <see cref="HandPuzzle.id"/> "lm.shell", stage = knocks done.
    /// </summary>
    public class ShellKnockPiece : KeeperWakePiece
    {
        public const int Knocks = 3;

        [Header("Shell")]
        [Tooltip("The shell's half size in body heights (x across, y up, z toward the player): a fingertip inside it knocks.")]
        public Vector3 shellHalf = new Vector3(0.85f, 0.62f, 0.85f);
        [Tooltip("A fingertip must leave the shell by this much (x its size) before it can knock again.")]
        public float leave = 1.15f;
        [Tooltip("Shortest time between two knocks (s).")]
        public float rearm = 0.3f;
        public Color bubble = new Color(0.78f, 0.93f, 1f, 0.9f);
        public Color gold = new Color(1f, 0.82f, 0.35f);

        int _count;
        float _nextKnock, _wobbleAt = -9f, _bubbleAt;
        readonly bool[] _inside = { true, true };
        readonly List<BattleFx.Item> _dots = new List<BattleFx.Item>();

        public int Count => _count;
        public override bool Busy => false;
        public override float FocusRadius => 0.5f * TileWorld;

        public override Vector3? FocusPoint
        {
            get
            {
                var a = Actor;
                if (!Here || !Available || a == null) return null;
                // The top of the shell, a little toward the player: where a finger lands.
                return a.WorldCenter + island.transform.up * (BodyHeight(a) * 0.45f) + island.transform.forward * (BodyHeight(a) * 0.25f);
            }
        }

        /// <summary>A point (offset from the shell's centre, in the island's axes, world metres) is inside the shell (pure, tested).</summary>
        public static bool InShell(Vector3 offset, Vector3 half, float grow = 1f)
        {
            half *= grow;
            if (half.x <= 0f || half.y <= 0f || half.z <= 0f) return false;
            float x = offset.x / half.x, y = offset.y / half.y, z = offset.z / half.z;
            return x * x + y * y + z * z <= 1f;
        }

        public Color wave = new Color(0.35f, 0.72f, 1f);
        public Color waveLight = new Color(0.72f, 0.93f, 1f);

        /// <summary>The knock's pitch rises a little with each one; low enough to thump (Nov 14; pure, tested).</summary>
        public static float KnockPitch(int knock) => 1.0f + 0.12f * Mathf.Clamp(knock - 1, 0, Knocks);

        /// <summary>Nov 14 (Mugesh: louder for the next two knocks): 0.55, 0.78, 1 for knocks 1-3 (pure, tested).</summary>
        public static float KnockStrength(int knock) => Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((knock - 1) / (float)(Knocks - 1)));

        protected override void ApplyState()
        {
            _count = IsSolved ? Knocks : Mathf.Clamp(PuzzleState.Stage(id), 0, Knocks - 1);
            SetProgressQuiet(_count / (float)Knocks);
        }

        protected override void OnDestroy()
        {
            ReleaseDots();
            base.OnDestroy();
        }

        protected override void OnDisable()
        {
            ReleaseDots();
            base.OnDisable();
        }

        /// <summary>Withdrawn: no eyes, no face (the head is inside).</summary>
        protected override void Dress(StageActor body)
        {
            foreach (var e in body.View.eyes)
                if (e.renderer != null) e.renderer.enabled = false;
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Face_")) r.enabled = false;
            BodyFury.fury = 0.3f;
            BodyFury.auraScale = 0f;
        }

        protected override void Pose(StageActor body)
        {
            float t = Time.time;
            // Slow breathing: the shell rises and settles; a knock makes it wobble.
            float breath = Mathf.Sin(t * 1.1f);
            float w = t - _wobbleAt;
            float wobble = w >= 0f && w < 0.8f ? Mathf.Exp(-w * 5f) * Mathf.Sin(w * 30f) : 0f;
            wobble *= 0.7f + 0.5f * KnockStrength(_count);
            body.squash = new Vector3(1f - 0.012f * breath, 1f + 0.03f * breath + 0.05f * wobble, 1f - 0.012f * breath);
            body.shakeRoll = 9f * wobble;
            if (fx != null && Here && !Waking && t >= _bubbleAt)
            {
                _bubbleAt = t + Random.Range(0.7f, 1.4f);
                float h = BodyHeight(body), tile = TileWorld;
                Vector3 at = body.WorldCenter + island.transform.right * (Random.Range(-0.7f, 0.7f) * h) + island.transform.forward * (Random.Range(0.2f, 0.8f) * h);
                fx.Burst(at, 1, bubble, tile * 0.05f, 1.4f, tile * 0.05f, island.transform.up * tile * 0.5f);
            }
            Dots(body, Here && !IsSolved);
        }

        protected override void Rest()
        {
            _inside[0] = _inside[1] = true;
            if (!BeforeShown) ReleaseDots();
        }

        protected override void Work(StageActor actor)
        {
            Vector3 c = actor.WorldCenter;
            Vector3 half = shellHalf * BodyHeight(actor);
            var t = island.transform;
            for (int h = 0; h < 2; h++)
            {
                var s = Hands[h];
                if (!s.tracked || !s.tipTracked || s.busy)
                {
                    // A hand that appears inside the shell must leave first (no knock from a tracking pop).
                    _inside[h] = true;
                    continue;
                }
                Vector3 d = s.indexTip - c;
                var local = new Vector3(Vector3.Dot(d, t.right), Vector3.Dot(d, t.up), Vector3.Dot(d, t.forward));
                bool was = _inside[h];
                bool now = InShell(local, half, was ? leave : 1f);
                _inside[h] = now;
                if (now && !was && Time.time >= _nextKnock)
                {
                    Knock($"{(h == 0 ? "left" : "right")} fingertip", s.indexTip);
                    return;
                }
            }
            if (!HeadGaze.TryGetRay(out var ray)) return;
            var pick = new HeadGaze.ConePick(ray, -1);
            pick.Consider(0, c, Mathf.Max(HeadGaze.MinRadius * Scale, half.x));
            if (pick.Best != 0) return;
            FocusHighlight.Hover(c, half.y);
            if (GazePinch(Hands) && Time.time >= _nextKnock) Knock("look + pinch", FocusPoint ?? c);
        }

        void Knock(string how, Vector3 at)
        {
            _count++;
            _nextKnock = Time.time + rearm;
            _wobbleAt = Time.time;
            Touched();
            var actor = Actor;
            Vector3 c = actor != null ? actor.WorldCenter : at;
            float h = actor != null ? BodyHeight(actor) : TileWorld, tile = TileWorld;
            // Nov 14 (Mugesh: more thump, louder for the next two): a hollow knock, lower than before, with the Keeper's
            // stomp under it, both louder each knock; ripples under it.
            float s = KnockStrength(_count);
            var knock = GameAudio.At(AudioIds.LogDrop, at);
            knock.SetPitch(KnockPitch(_count));
            knock.SetVolume(volume * (0.5f + 0.9f * s));
            var thump = GameAudio.At(AudioIds.KeeperStomp, c);
            thump.SetPitch(1.45f);
            thump.SetVolume(volume * 0.6f * s);
            GameAudio.At(AudioIds.ChannelRipple, c).SetVolume(volume * (0.3f + 0.4f * s));
            if (fx != null)
            {
                var up = island.transform.up;
                fx.Burst(at, Mathf.RoundToInt(10 * (0.6f + s)), gold, tile * 0.35f, 0.5f, tile * 0.04f);
                // Nov 14: the lodge's heal wave, in blue, out from under the shell; wider and brighter each knock.
                fx.StartCoroutine(HealFx.Ripple(fx, c - up * (h * 0.42f), island.transform.rotation, up, tile,
                    h * 0.9f / Mathf.Max(1e-5f, tile) + 0.7f * _count, wave, waveLight, s, 0.8f + 0.25f * s));
                // A ring of ripples and bubbles round the shell.
                int n = Mathf.RoundToInt(14 * (0.6f + s));
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    Vector3 dir = island.transform.right * Mathf.Cos(a) + island.transform.forward * Mathf.Sin(a);
                    fx.Burst(c + dir * (h * 0.8f) - up * (h * 0.3f), 1, bubble, tile * 0.04f, 1f, tile * 0.055f, dir * tile * (0.35f + 0.3f * s) + up * tile * 0.35f);
                }
            }
            Debug.Log($"[ShellKnockPiece] {id}: knock {_count} / {Knocks} ({how}).");
            if (_count >= Knocks)
            {
                Waking = true;
                LessonComplete();
                MarkSolved(Knocks);
                return;
            }
            PuzzleState.SetStage(id, _count);
            SetProgress(_count / (float)Knocks);
        }

        /// <summary>Three small dots over the shell: gold per knock done.</summary>
        void Dots(StageActor body, bool show)
        {
            if (!show || fx == null)
            {
                ReleaseDots();
                return;
            }
            float h = BodyHeight(body);
            Vector3 mid = body.WorldCenter + island.transform.up * (h * 1.05f);
            for (int i = 0; i < Knocks; i++)
            {
                if (i >= _dots.Count) _dots.Add(fx.Get(BattleFx.Shape.Orb));
                var o = _dots[i];
                if (o == null) continue;
                bool done = i < _count;
                float r = h * (done ? 0.085f : 0.06f) * (done ? 1f + 0.1f * Mathf.Sin(Time.time * 4f + i) : 1f);
                o.transform.position = mid + island.transform.right * ((i - 1) * h * 0.26f);
                o.transform.localScale = Vector3.one * (r / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                if (done) o.Set(Color.Lerp(gold, Color.white, 0.5f), gold, 0.95f, 2.4f, 1f, 0.15f);
                else o.Set(new Color(0.8f, 0.9f, 0.95f), new Color(0.45f, 0.6f, 0.7f), 0.55f, 0.9f, 1f, 0.15f);
            }
        }

        void ReleaseDots()
        {
            if (fx != null)
                foreach (var o in _dots)
                    if (o != null) fx.Release(o);
            _dots.Clear();
        }

        /// <summary>The shell shudders, bursts open under a splash, the head and flippers come out angry; it rears with its call.</summary>
        public override IEnumerator WakeBeat(KeeperHall hall)
        {
            ReleaseDots();
            float tile = TileWorld;
            Vector3 c = Body != null ? Body.WorldCenter : hall.Model.WorldCenter;
            for (float t = 0f; t < 0.4f && Body != null; t += Time.deltaTime)
            {
                Body.shakeRoll = 10f * Mathf.Sin(t * 60f) * (t / 0.4f);
                yield return null;
            }
            float h = Body != null ? BodyHeight(Body) : tile;
            var up = island.transform.up;
            Vector3 feet = c - up * (h * 0.42f);
            if (fx != null)
            {
                fx.Burst(c, 50, bubble, tile * 1.6f, 0.9f, tile * 0.06f, up * tile * 0.9f);
                fx.Burst(c, 24, Color.white, tile * 1.1f, 0.7f, tile * 0.05f, up * tile * 0.5f);
                // Nov 14 (Mugesh: no water effects when it wakes): a splash all round the shell, a column of spray and
                // the biggest blue wave of all.
                Splash(feet, h, tile, 1f);
                fx.StartCoroutine(HealFx.Ripple(fx, feet, island.transform.rotation, up, tile, h * 0.9f / Mathf.Max(1e-5f, tile) + 3.2f,
                    wave, waveLight, 1.4f, 1.4f));
            }
            GameAudio.At(AudioIds.PondSplash, c).SetVolume(volume * 1.3f);
            SwapToStanding(hall);
            var model = hall.Model;
            if (model == null) yield break;
            if (hall.Fury != null) hall.Fury.fury = 1f;
            // Out it comes: a quick swell past its size and back.
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                float k = t / 0.35f;
                model.squash = Vector3.one * (Mathf.Lerp(0.82f, 1f, k) + 0.12f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            model.squash = Vector3.one;
            GameAudio.At(AudioIds.VoiceCallTidemother, model.WorldCenter);
            Debug.Log($"[ShellKnockPiece] {id}: the head and flippers burst out; {hall.KeeperLabel} rears, angry.");
            if (hall.Fury != null) hall.Fury.Rear(16f, 0.5f);
            // The flippers slap down after the rear: a second, smaller splash and wave.
            yield return new WaitForSeconds(0.55f);
            if (fx != null && model != null)
            {
                Splash(feet, h, tile, 0.6f);
                fx.StartCoroutine(HealFx.Ripple(fx, feet, island.transform.rotation, up, tile, h * 0.9f / Mathf.Max(1e-5f, tile) + 2f,
                    wave, waveLight, 0.8f, 1.1f));
                GameAudio.At(AudioIds.PondSplash, model.WorldCenter).SetVolume(volume * 0.7f);
            }
            yield return new WaitForSeconds(0.65f);
        }

        /// <summary>Nov 14: water thrown up and out all round the shell, a white crown of spray and a column in the middle.</summary>
        void Splash(Vector3 feet, float h, float tile, float size)
        {
            var up = island.transform.up;
            int n = Mathf.RoundToInt(24 * size);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 dir = island.transform.right * Mathf.Cos(a) + island.transform.forward * Mathf.Sin(a);
                Vector3 rim = feet + dir * (h * 0.85f);
                fx.Burst(rim, 2, wave, tile * 0.25f, 0.8f, tile * 0.07f, dir * (tile * 1.1f * size) + up * (tile * 1.6f * size));
                fx.Burst(rim, 1, Color.white, tile * 0.2f, 0.6f, tile * 0.05f, dir * (tile * 0.7f * size) + up * (tile * 2.1f * size));
            }
            fx.Burst(feet + up * (h * 0.3f), Mathf.RoundToInt(30 * size), waveLight, tile * 0.5f, 0.9f, tile * 0.07f, up * (tile * 2.6f * size));
        }

        public override void DebugLookPinch()
        {
            if (!Here || IsSolved || Waking || !Available || Time.time < _nextKnock) return;
            Knock("look + pinch (bridge)", FocusPoint ?? (Actor != null ? Actor.WorldCenter : transform.position));
        }

        public override string Describe() => $"knocks {_count}/{Knocks}, before body {(BeforeShown ? "shown" : "hidden")}, available {Available}, usable {Usable}, hints {Hints}";
    }
}
