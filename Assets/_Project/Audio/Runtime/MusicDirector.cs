using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>What the music should be playing (one cue each, see <see cref="MusicDirector.CueFor"/>).</summary>
    public enum MusicState { None, Hub, Cinderwood, Galewick, Lilymere, BattleWild, BattleKeeper }

    /// <summary>
    /// One music cue per game state, crossfaded through <see cref="AudioService.SetMusic"/>, plus level offsets in dB that
    /// stack: a restored realm (+2), dialogue (-4), the pause menu (-8) and a jingle (-8 while it plays). The game sets the
    /// state and flags (the main lane's AudioDirector polls them each frame); a cue without clips yet plays silence.
    /// Created on first use, survives scene swaps.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public const float AreaCrossfade = 2f, BattleCrossfade = 0.6f;
        public const float RestoredDb = 2f, DialogueDb = -4f, PauseDb = -8f, JingleDb = -8f;

        static MusicDirector _instance;

        MusicState _state;
        bool _restored, _dialogue, _paused;
        float _jingleUntil, _db;

        static MusicDirector Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("MusicDirector");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MusicDirector>();
                }
                return _instance;
            }
        }

        public static MusicState State => _instance != null ? _instance._state : MusicState.None;

        public static string CueFor(MusicState s)
        {
            switch (s)
            {
                case MusicState.Hub: return AudioIds.MusicHub;
                case MusicState.Cinderwood: return AudioIds.MusicCinderwood;
                case MusicState.Galewick: return AudioIds.MusicGalewick;
                case MusicState.Lilymere: return AudioIds.MusicLilymere;
                case MusicState.BattleWild: return AudioIds.MusicBattleWild;
                case MusicState.BattleKeeper: return AudioIds.MusicBattleKeeper;
                default: return null;
            }
        }

        /// <summary>Crossfades to the state's cue (battle states use the short battle crossfade unless given one).</summary>
        public static void SetState(MusicState state, float crossfade = -1f)
        {
            var d = Instance;
            if (d == null || d._state == state) return;
            bool battle = state == MusicState.BattleWild || state == MusicState.BattleKeeper;
            float fade = crossfade >= 0f ? crossfade : battle ? BattleCrossfade : AreaCrossfade;
            Debug.Log($"[Music] {d._state} -> {state} ({fade:0.#} s).");
            d._state = state;
            var id = CueFor(state);
            if (id == null) AudioService.StopMusic(fade);
            else AudioService.SetMusic(id, fade);
        }

        /// <summary>The area's realm is restored (Cinderwood plays +2 dB).</summary>
        public static void SetRestored(bool on)
        {
            if (Instance != null) _instance._restored = on;
        }

        public static void SetDialogue(bool on)
        {
            if (Instance != null) _instance._dialogue = on;
        }

        public static void SetPaused(bool on)
        {
            if (Instance != null) _instance._paused = on;
        }

        /// <summary>Ducks the music under a jingle for its length, then lets it come back (victory, catch, level up).</summary>
        public static void Jingle(float seconds = 2.5f)
        {
            if (Instance != null) _instance._jingleUntil = Mathf.Max(_instance._jingleUntil, Time.unscaledTime + seconds);
        }

        void Update()
        {
            float target = (_restored ? RestoredDb : 0f) + (_dialogue ? DialogueDb : 0f) + (_paused ? PauseDb : 0f) +
                           (Time.unscaledTime < _jingleUntil ? JingleDb : 0f);
            // Down fast (0.25 s for 8 dB), back up slowly (2 s after a jingle, as the step asks).
            float rate = target < _db ? 32f : 4f;
            _db = Mathf.MoveTowards(_db, target, rate * Time.unscaledDeltaTime);
            AudioService.MusicGain = Mathf.Pow(10f, _db / 20f);
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
