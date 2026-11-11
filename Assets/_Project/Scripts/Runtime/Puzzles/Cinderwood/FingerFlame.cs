using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 29 D (playtest: the ember carry got stuck and never reached the forge): the burning fingertip. A layered toon
    /// flame on the index tip: a bright yellow-white core, an orange body and two red-orange tongues that flicker and lean
    /// against the hand's motion, a few embers rising, and a warm glow round the finger. A crackle loop follows the tip and
    /// swells a little while the hand moves; <see cref="Catch"/> says "whoomp", <see cref="Light"/> hisses and flares.
    /// Pooled <see cref="BattleFx"/> orbs and particles only (cheap). Sized in metres to read from the seat. Plain C#.
    /// </summary>
    public class FingerFlame
    {
        static readonly Color Core = new Color(1f, 0.96f, 0.72f), Body = new Color(1f, 0.62f, 0.2f), Tongue = new Color(1f, 0.38f, 0.12f),
            Glow = new Color(1f, 0.55f, 0.2f), Spark = new Color(1f, 0.75f, 0.35f);

        /// <summary>Flame height on the finger (m).</summary>
        public float height = 0.03f;

        readonly BattleFx _fx;
        BattleFx.Item _core, _body, _tongueA, _tongueB, _glow;
        AudioHandle _loop;
        Vector3 _last, _lean;
        float _speed, _sparkAt, _seed;
        bool _shown;

        public FingerFlame(BattleFx fx)
        {
            _fx = fx;
            _seed = Random.value * 10f;
        }

        public bool Shown => _shown;

        /// <summary>One frame on the fingertip (<paramref name="up"/> = world up).</summary>
        public void Show(Vector3 tip, Vector3 up)
        {
            if (_fx == null) return;
            if (!_shown)
            {
                _shown = true;
                _core = Get(Core, Core, 1f, 2f);
                _body = Get(Body, Tongue, 0.85f, 1.4f);
                _tongueA = Get(Tongue, new Color(0.9f, 0.2f, 0.08f), 0.75f, 1.2f);
                _tongueB = Get(Tongue, new Color(0.9f, 0.2f, 0.08f), 0.7f, 1.2f);
                _glow = Get(Glow, new Color(1f, 0.35f, 0.1f), 0.28f, 0.8f);
                _last = tip;
                _loop = GameAudio.Loop(AudioIds.EmberCrackle, tip, 0.1f);
            }
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            var v = (tip - _last) / dt;
            _last = tip;
            _speed = Mathf.Lerp(_speed, v.magnitude, 1f - Mathf.Exp(-dt * 8f));
            // The flame trails the motion a little (leans back against it).
            _lean = Vector3.Lerp(_lean, Vector3.ClampMagnitude(-v * 0.04f, height * 0.6f), 1f - Mathf.Exp(-dt * 10f));
            float t = Time.time + _seed;
            float f1 = 0.85f + 0.15f * Mathf.Sin(t * 23f) + 0.08f * Mathf.Sin(t * 37f);
            float f2 = 0.8f + 0.2f * Mathf.Sin(t * 17f + 1.3f);
            float h = height * (1f + Mathf.Clamp01(_speed * 2f) * 0.25f);
            Place(_core, tip + up * h * 0.22f + _lean * 0.3f, new Vector3(h * 0.32f, h * 0.55f * f1, h * 0.32f), up);
            Place(_body, tip + up * h * 0.32f + _lean * 0.6f, new Vector3(h * 0.5f, h * 0.85f * f2, h * 0.5f), up);
            var side = Vector3.Cross(up, Vector3.forward).normalized * h * 0.12f;
            Place(_tongueA, tip + up * h * (0.55f + 0.1f * Mathf.Sin(t * 13f)) + side * Mathf.Sin(t * 9f) + _lean, new Vector3(h * 0.22f, h * 0.55f * f1, h * 0.22f), up);
            Place(_tongueB, tip + up * h * (0.5f + 0.1f * Mathf.Sin(t * 11f + 2f)) - side * Mathf.Sin(t * 7f + 1f) + _lean * 0.8f, new Vector3(h * 0.18f, h * 0.45f * f2, h * 0.18f), up);
            Place(_glow, tip + up * h * 0.2f, Vector3.one * h * (1.5f + 0.1f * Mathf.Sin(t * 5f)), up);
            if (Time.time >= _sparkAt)
            {
                _sparkAt = Time.time + 0.09f;
                _fx.Burst(tip + up * h * 0.6f, 1, Spark, h * 0.3f, 0.7f, h * 0.06f, up * h * 2.2f);
            }
            if (_loop.IsValid)
            {
                _loop.SetVolume(0.55f + 0.35f * Mathf.Clamp01(_speed * 3f));
            }
            else _loop = GameAudio.Loop(AudioIds.EmberCrackle, tip, 0.1f);
            // The loop follows the tip: restart it where the finger is if it drifts far (a point loop).
            if (_loop.IsValid && (tip - _loopAt).sqrMagnitude > 0.04f * 0.04f)
            {
                _loop.Stop(0.1f);
                _loop = GameAudio.Loop(AudioIds.EmberCrackle, tip, 0.05f);
                _loopAt = tip;
            }
        }

        Vector3 _loopAt;

        /// <summary>The flame catches on the finger: a soft "whoomp" and a puff of sparks.</summary>
        public void Catch(Vector3 tip, Vector3 up)
        {
            GameAudio.At(AudioService.Has(AudioIds.FingerCatch) ? AudioIds.FingerCatch : AudioIds.FlameRekindle, tip).SetVolume(0.7f);
            if (_fx != null) _fx.Burst(tip, 10, Spark, height * 1.2f, 0.5f, height * 0.08f, up * height * 2f);
        }

        /// <summary>The finger lights the target: a hiss and a flare, then the flame leaves the finger.</summary>
        public void Light(Vector3 at, Vector3 up)
        {
            GameAudio.At(AudioIds.SteamHiss, at).SetVolume(0.5f);
            GameAudio.At(AudioIds.EmberDrop, at);
            if (_fx != null) _fx.Burst(at, 18, Body, height * 2.5f, 0.7f, height * 0.12f, up * height * 3f);
            Hide();
        }

        /// <summary>Nov 11 (the hollow's lanterns): the finger lights a target and keeps burning: a hiss and a flare, no hide.</summary>
        public void Flare(Vector3 at, Vector3 up)
        {
            GameAudio.At(AudioIds.SteamHiss, at).SetVolume(0.4f);
            if (_fx != null) _fx.Burst(at, 14, Body, height * 2f, 0.6f, height * 0.1f, up * height * 2.5f);
        }

        public void Hide()
        {
            if (!_shown) return;
            _shown = false;
            Release(ref _core);
            Release(ref _body);
            Release(ref _tongueA);
            Release(ref _tongueB);
            Release(ref _glow);
            if (_loop.IsValid) _loop.Stop(0.25f);
        }

        BattleFx.Item Get(Color core, Color edge, float alpha, float power)
        {
            var item = _fx.Get(BattleFx.Shape.Orb);
            if (item != null) item.Set(core, edge, alpha, power, 1f, 0.4f);
            return item;
        }

        void Place(BattleFx.Item item, Vector3 world, Vector3 size, Vector3 up)
        {
            if (item == null) return;
            item.transform.position = world;
            item.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
            float toLocal = 1f / Mathf.Max(1e-5f, _fx.transform.lossyScale.x);
            item.transform.localScale = size * toLocal;
        }

        void Release(ref BattleFx.Item item)
        {
            if (item != null) _fx.Release(item);
            item = null;
        }
    }
}
