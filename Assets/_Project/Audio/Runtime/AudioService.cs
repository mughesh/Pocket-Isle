using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>
    /// Plays cues by id. Created on first use, survives scene swaps. Pooled AudioSources (3D and 2D), loops with handles,
    /// music with crossfade, and buses in code (no AudioMixer asset): per-bus volume saved in PlayerPrefs, Duck() for music.
    /// 3D sounds use Unity's built-in panning (spatialize off, even though Meta XR Audio is the project's spatializer plugin),
    /// no doppler, and a gentle custom rolloff sized for a tabletop: full volume inside minDistance, ~25% at maxDistance.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        // ---- configuration (set before first use) ----

        public static int Pool3D = 16;
        public static int Pool2D = 6;
        /// <summary>Multiplies every cue's min / max distance; feed it the island's scale.</summary>
        public static float DistanceScale = 1f;
        /// <summary>Music level while ducked (0.5 = about -6 dB).</summary>
        public static float DuckLevel = 0.5f;

        static AudioService _instance;
        static AudioCueTable _table;
        static readonly float[] BusVolume = { 1f, 1f, 1f, 1f, 1f };
        static bool _busLoaded;
        static readonly HashSet<string> Warned = new HashSet<string>();

        readonly List<Voice> _3d = new List<Voice>();
        readonly List<Voice> _2d = new List<Voice>();
        Voice _musicA, _musicB;
        string _musicId;
        bool _ducked;
        float _duck = 1f;
        AnimationCurve _rolloff;

        // ---- public API ----

        /// <summary>Uses this table instead of Resources/AudioCueTable.</summary>
        public static void SetTable(AudioCueTable table) => _table = table;

        public static AudioHandle Play(string id, Vector3 position, int step = 0) => Start(id, position, null, true, step, false, 0f);

        public static AudioHandle Play(string id, Transform follow, int step = 0) =>
            Start(id, follow != null ? follow.position : Vector3.zero, follow, true, step, false, 0f);

        public static AudioHandle Play2D(string id, int step = 0) => Start(id, Vector3.zero, null, false, step, false, 0f);

        /// <summary>A looping cue that runs until its handle is stopped (shrine fill tone, low-HP pulse, gate hum).</summary>
        public static AudioHandle PlayLoop(string id, Transform follow, float fadeIn = 0.15f) =>
            Start(id, follow != null ? follow.position : Vector3.zero, follow, true, 0, true, fadeIn);

        public static AudioHandle PlayLoop(string id, Vector3 position, float fadeIn = 0.15f) => Start(id, position, null, true, 0, true, fadeIn);

        public static AudioHandle PlayLoop2D(string id, float fadeIn = 0.15f) => Start(id, Vector3.zero, null, false, 0, true, fadeIn);

        /// <summary>Crossfades to a music cue (no-op if it's already playing).</summary>
        public static void SetMusic(string id, float crossfade = 1.5f)
        {
            var s = Instance;
            if (s == null || id == s._musicId) return;
            var cue = Cue(id);
            if (cue == null) return;
            var clip = cue.Pick();
            if (clip == null)
            {
                WarnOnce(id, cue.awaitingClips ? $"[Audio] Music '{id}' has no clips yet (awaiting Lyria); silence." : $"[Audio] Music cue '{id}' has no clips.", !cue.awaitingClips);
                s.FadeOutMusic(crossfade);
                s._musicId = id;
                return;
            }
            s.FadeOutMusic(crossfade);
            var v = s._musicA.Busy ? s._musicB : s._musicA;
            v.Begin(cue, clip, Vector3.zero, null, false, 1f, true, crossfade);
            s._musicId = id;
        }

        public static void StopMusic(float fade = 1f)
        {
            if (_instance == null) return;
            _instance.FadeOutMusic(fade);
            _instance._musicId = null;
        }

        /// <summary>Lowers music by about 6 dB (dialogue, jingles).</summary>
        public static void Duck(bool on)
        {
            if (Instance != null) _instance._ducked = on;
        }

        public static float GetBusVolume(AudioBus bus)
        {
            LoadBuses();
            return BusVolume[(int)bus];
        }

        public static void SetBusVolume(AudioBus bus, float volume)
        {
            LoadBuses();
            BusVolume[(int)bus] = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Key(bus), BusVolume[(int)bus]);
        }

        /// <summary>Stops every voice (scene teardown, returning to the title).</summary>
        public static void StopAll(float fade = 0.2f)
        {
            if (_instance == null) return;
            foreach (var v in _instance._3d) v.Stop(fade);
            foreach (var v in _instance._2d) v.Stop(fade);
            StopMusic(fade);
        }

        // ---- internals ----

        static AudioService Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying) return null;
                var go = new GameObject("[AudioService]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AudioService>();
                return _instance;
            }
        }

        static bool LoadTable()
        {
            if (_table != null) return true;
            _table = Resources.Load<AudioCueTable>(AudioCueTable.ResourceName);
            if (_table == null) WarnOnce("<table>", "[Audio] No AudioCueTable in Resources; every sound is silent. Run Tools/Pocket Isle/Audio/Sync Cues.", true);
            return _table != null;
        }

        static AudioCue Cue(string id)
        {
            if (!LoadTable()) return null;
            var cue = _table.Find(id);
            if (cue == null) WarnOnce(id, $"[Audio] Unknown cue id '{id}'.", true);
            return cue;
        }

        static void WarnOnce(string key, string message, bool error = false)
        {
            if (!Warned.Add(key)) return;
            if (error) Debug.LogError(message);
            else Debug.LogWarning(message);
        }

        static AudioHandle Start(string id, Vector3 pos, Transform follow, bool want3D, int step, bool loop, float fadeIn)
        {
            var s = Instance;
            if (s == null) return default;
            var cue = Cue(id);
            if (cue == null) return default;
            float now = Time.unscaledTime;
            if (!loop && now - cue.lastPlayTime < cue.minInterval) return default;
            var clip = cue.Pick();
            if (clip == null)
            {
                WarnOnce(id, cue.awaitingClips ? $"[Audio] '{id}' has no clips yet." : $"[Audio] Cue '{id}' has no clips.", !cue.awaitingClips);
                return default;
            }
            bool spatial = want3D && cue.spatial;
            var v = s.Claim(cue, spatial ? s._3d : s._2d);
            if (v == null) return default;
            cue.lastPlayTime = now;
            v.Begin(cue, clip, pos, follow, spatial, cue.PickPitch(step), loop || cue.loop, fadeIn);
            return new AudioHandle(v);
        }

        Voice Claim(AudioCue cue, List<Voice> pool)
        {
            Voice free = null, oldestSame = null, steal = null;
            int same = 0;
            foreach (var v in pool)
            {
                if (!v.Busy)
                {
                    if (free == null) free = v;
                    continue;
                }
                if (v.cue == cue)
                {
                    same++;
                    if (!v.looping && (oldestSame == null || v.started < oldestSame.started)) oldestSame = v;
                }
                // Steal the least important, then the oldest; never a loop someone holds a handle to.
                if (!v.looping && (steal == null || v.cue.priority > steal.cue.priority ||
                                   (v.cue.priority == steal.cue.priority && v.started < steal.started))) steal = v;
            }
            if (same >= cue.maxVoices) return oldestSame;
            if (free != null) return free;
            if (steal != null && steal.cue.priority >= cue.priority) return steal;
            return null;
        }

        void Awake()
        {
            LoadBuses();
            _rolloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.07f, 1f), new Keyframe(1f, 0.25f));
            for (int i = 0; i < Pool3D; i++) _3d.Add(MakeVoice($"3D {i}", true));
            for (int i = 0; i < Pool2D; i++) _2d.Add(MakeVoice($"2D {i}", false));
            _musicA = MakeVoice("Music A", false);
            _musicB = MakeVoice("Music B", false);
            Debug.Log($"[Audio] AudioService up: {Pool3D} 3D + {Pool2D} 2D voices, {(LoadTable() ? _table.cues.Count + " cues" : "no cue table")}.");
        }

        Voice MakeVoice(string name, bool spatial)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialize = false;
            src.dopplerLevel = 0f;
            src.spatialBlend = spatial ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Custom;
            src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _rolloff);
            return new Voice(src);
        }

        void FadeOutMusic(float fade)
        {
            if (_musicA.Busy) _musicA.Stop(fade);
            if (_musicB.Busy) _musicB.Stop(fade);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _duck = Mathf.MoveTowards(_duck, _ducked ? DuckLevel : 1f, dt / 0.3f);
            foreach (var v in _3d) v.Tick(dt, 1f);
            foreach (var v in _2d) v.Tick(dt, 1f);
            _musicA.Tick(dt, _duck);
            _musicB.Tick(dt, _duck);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        static void LoadBuses()
        {
            if (_busLoaded) return;
            _busLoaded = true;
            for (int i = 0; i < BusVolume.Length; i++) BusVolume[i] = PlayerPrefs.GetFloat(Key((AudioBus)i), 1f);
        }

        static string Key(AudioBus bus) => "audio.bus." + bus;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _table = null;
            _busLoaded = false;
            Warned.Clear();
            DistanceScale = 1f;
        }

        // ---- one pooled source ----

        internal sealed class Voice
        {
            public readonly AudioSource src;
            public AudioCue cue;
            public Transform follow;
            public bool following, looping;
            public float started;
            public int generation;
            float _base, _fade = 1f, _fadeTarget = 1f, _fadeRate, _volume = 1f, _pitchBase = 1f, _pitchMul = 1f;
            bool _stopping;

            public Voice(AudioSource s) => src = s;

            public bool Busy => cue != null;

            public void Begin(AudioCue c, AudioClip clip, Vector3 pos, Transform f, bool spatial, float pitch, bool loop, float fadeIn)
            {
                generation++;
                cue = c;
                follow = f;
                following = f != null;
                looping = loop;
                started = Time.unscaledTime;
                _base = c.PickVolume();
                _volume = 1f;
                _pitchBase = pitch;
                _pitchMul = 1f;
                _stopping = false;
                _fade = fadeIn > 0f ? 0f : 1f;
                _fadeTarget = 1f;
                _fadeRate = fadeIn > 0f ? 1f / fadeIn : 0f;
                src.transform.position = pos;
                src.spatialBlend = spatial ? 1f : 0f;
                src.minDistance = c.minDistance * DistanceScale;
                src.maxDistance = Mathf.Max(src.minDistance + 0.01f, c.maxDistance * DistanceScale);
                src.priority = c.priority;
                src.clip = clip;
                src.loop = loop;
                src.pitch = pitch;
                src.volume = Level(1f);
                src.Play();
            }

            public void Stop(float fade)
            {
                if (!Busy) return;
                if (fade <= 0f)
                {
                    Release();
                    return;
                }
                _stopping = true;
                _fadeTarget = 0f;
                _fadeRate = 1f / fade;
            }

            public void SetVolume(float v) => _volume = Mathf.Max(0f, v);

            public void SetPitch(float multiplier)
            {
                _pitchMul = Mathf.Max(0.01f, multiplier);
                src.pitch = _pitchBase * _pitchMul;
            }

            public void Tick(float dt, float duck)
            {
                if (!Busy) return;
                if (following)
                {
                    // Fake-null aware: a destroyed follow target ends a loop, a one-shot finishes where it was.
                    if (follow != null) src.transform.position = follow.position;
                    else
                    {
                        following = false;
                        if (looping) Stop(0.15f);
                    }
                }
                if (_fadeRate > 0f) _fade = Mathf.MoveTowards(_fade, _fadeTarget, _fadeRate * dt);
                if (_stopping && _fade <= 0f)
                {
                    Release();
                    return;
                }
                if (!src.isPlaying && !AudioListener.pause)
                {
                    Release();
                    return;
                }
                src.minDistance = cue.minDistance * DistanceScale;
                src.maxDistance = Mathf.Max(src.minDistance + 0.01f, cue.maxDistance * DistanceScale);
                src.volume = Level(duck);
            }

            float Level(float duck) => _base * _volume * _fade * BusVolume[(int)cue.bus] * (cue.bus == AudioBus.Music ? duck : 1f);

            void Release()
            {
                src.Stop();
                src.clip = null;
                cue = null;
                follow = null;
                following = false;
                looping = false;
                generation++;
            }
        }
    }

    /// <summary>A playing voice. Goes invalid (all calls no-ops) once the sound ends or the voice is reused.</summary>
    public readonly struct AudioHandle
    {
        readonly AudioService.Voice _voice;
        readonly int _generation;

        internal AudioHandle(AudioService.Voice voice)
        {
            _voice = voice;
            _generation = voice.generation;
        }

        public bool IsValid => _voice != null && _voice.generation == _generation && _voice.Busy;

        public void Stop(float fade = 0.15f)
        {
            if (IsValid) _voice.Stop(fade);
        }

        /// <summary>Multiplies the cue's picked volume (0..1+).</summary>
        public void SetVolume(float volume)
        {
            if (IsValid) _voice.SetVolume(volume);
        }

        /// <summary>Multiplies the cue's picked pitch (the shrine palm-fill tone rises with this).</summary>
        public void SetPitch(float multiplier)
        {
            if (IsValid) _voice.SetPitch(multiplier);
        }
    }
}
