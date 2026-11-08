using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>What the music should be playing (one cue each, see <see cref="MusicDirector.CueFor"/>).</summary>
    public enum MusicState { None, Hub, Cinderwood, Galewick, Lilymere, BattleWild, BattleKeeper, Title, Temple, Lodge }

    /// <summary>
    /// One music cue per game state, crossfaded through <see cref="AudioService.SetMusic"/>, plus level offsets in dB that
    /// stack: a restored realm (+2), dialogue (-4), the pause menu (-8) and a jingle (-8 while it plays). The game sets the
    /// state and flags (the main lane's AudioDirector polls them each frame); a cue without clips yet plays silence.
    /// Created on first use, survives scene swaps.
    /// Step 20 (music as the reward for restoring a realm): the title, the Still Isle's welcome, the lodge and the finale
    /// play once; the Still Isle's welcome once per arrival from a realm or a load; the title and the finale carry on to
    /// their end over silence instead of being cut; Keeper halls and Keeper battles share music.temple (no restart); a
    /// calmed Keeper fades it over <see cref="CalmFade"/>. Which realm states have music is the AudioDirector's call
    /// (<see cref="RealmMusicBeforeRestore"/>).
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public const float AreaCrossfade = 3f, BattleCrossfade = 0.6f; // Step 20: areas 2 -> 3 s
        /// <summary>Step 20: the Keeper calmed: the temple track fades to silence this long.</summary>
        public const float CalmFade = 2.5f;
        /// <summary>Step 20: the finale's Skip fades its track this long.</summary>
        public const float SkipFade = 1.5f;
        public const float RestoredDb = 2f, DialogueDb = -4f, PauseDb = -8f, JingleDb = -4f; // Step 16: -8 -> -4 (the jingles are 9 dB quieter, so the music ducks less)
        /// <summary>Step 18: inside a realm hall (the tower) the realm's music keeps going this much lower.</summary>
        public const float HallDb = -6f;
        /// <summary>Step 18: the first play on arriving in a realm fades in this long (after the realm name card).</summary>
        public const float ArrivalFadeIn = 3f;
        /// <summary>Step 23: the realm's track fading in as its shard is taken (the bloom).</summary>
        public const float BloomFadeIn = 1.8f;
        /// <summary>Step 23: leaving the lodge, its track fades this long.</summary>
        public const float LodgeExitFade = 1.8f;

        static MusicDirector _instance;

        MusicState _state;
        bool _restored, _dialogue, _paused, _hall;
        // Step 20: the Still Isle's welcome has played since the last arrival.
        bool _hubPlayed;

        const string RealmMusicKey = "music.realmBeforeRestore";
        static int _realmMusicBeforeRestore = -1;

        /// <summary>
        /// Step 20 A/B switch (Dev panel > Tools > "Realm music: reward / always", remembered on this device). Off (the
        /// default, "reward"): an unrestored realm has no music, only its ambience; its track comes with the restoration.
        /// On ("always"): the realm's track plays restored or not (step 18).
        /// </summary>
        public static bool RealmMusicBeforeRestore
        {
            get
            {
                if (_realmMusicBeforeRestore < 0) _realmMusicBeforeRestore = PlayerPrefs.GetInt(RealmMusicKey, 0);
                return _realmMusicBeforeRestore == 1;
            }
            set
            {
                _realmMusicBeforeRestore = value ? 1 : 0;
                PlayerPrefs.SetInt(RealmMusicKey, _realmMusicBeforeRestore);
                Debug.Log($"[Music] Realm music: {(value ? "always" : "reward (after the restoration)")}.");
            }
        }
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
                // Step 20: Keeper battles carry the hall's temple track on (same cue: no restart). Without it, step 18's fallbacks.
                case MusicState.BattleKeeper:
                    return AudioService.Has(AudioIds.MusicTemple) ? AudioIds.MusicTemple
                         : AudioService.Has(AudioIds.MusicBattleKeeper) ? AudioIds.MusicBattleKeeper : AudioIds.MusicBattleWild;
                case MusicState.Title: return AudioIds.MusicTitle;
                case MusicState.Temple: return AudioIds.MusicTemple;
                case MusicState.Lodge: return AudioIds.MusicLodge;
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
            var id = CueFor(state);
            string why = "";
            // Step 20: the Still Isle's welcome plays once per arrival; while the title still plays (the opening flowing
            // into the tutorial, or Continue) it counts as the welcome.
            if (state == MusicState.Hub)
            {
                if (d._hubPlayed || AudioService.IsSounding(AudioIds.MusicTitle))
                {
                    id = null;
                    why = d._hubPlayed ? ", welcome already played" : ", the title is the welcome";
                }
                d._hubPlayed = true;
            }
            // Step 20: the title is this visit's welcome (Continue, or the opening into the tutorial).
            if (state == MusicState.Title) d._hubPlayed = true;
            // Step 18: after a battle, a resting track (rest > 0) doesn't snap back: its rest first, then from the top.
            bool fromBattle = d._state == MusicState.BattleWild || d._state == MusicState.BattleKeeper;
            float delay = fromBattle && !battle ? AudioService.RestOf(id) : 0f;
            if (delay > 0f) fade = 1.5f;
            // Step 20: the Keeper calmed (the hall goes quiet until the shard is taken).
            if ((d._state == MusicState.BattleKeeper || d._state == MusicState.Temple) && id == null && crossfade < 0f) fade = CalmFade;
            if (d._state == MusicState.Lodge && id == null && crossfade < 0f) fade = LodgeExitFade;
            // Step 20: the title and the finale aren't cut by silence: they play to their end (they fade themselves).
            string current = AudioService.CurrentMusic;
            bool carry = id == null && (current == AudioIds.MusicTitle || current == AudioIds.MusicFinale) && AudioService.IsSounding(current);
            if (carry) why += $", {current} plays on to its end";
            Debug.Log($"[Music] {d._state} -> {state} ({(carry ? "-" : $"{fade:0.#} s")}{(delay > 0f ? $", after {delay:0} s of rest" : "")}{why}).");
            d._state = state;
            if (carry) return;
            if (id == null) AudioService.StopMusic(fade);
            else AudioService.SetMusic(id, fade, delay);
        }

        /// <summary>Step 20: back on the Still Isle from a realm: its welcome may play again.</summary>
        public static void ArrivedHome()
        {
            if (Instance == null || !_instance._hubPlayed) return;
            _instance._hubPlayed = false;
            Debug.Log("[Music] Home from a realm: the Still Isle's welcome may play again.");
        }

        /// <summary>Step 20: the finale's Skip: its track fades over <see cref="SkipFade"/>.</summary>
        public static void SkipFinale()
        {
            if (!AudioService.IsSounding(AudioIds.MusicFinale)) return;
            Debug.Log($"[Music] Finale skipped: music.finale fades ({SkipFade:0.#} s).");
            AudioService.StopMusic(SkipFade);
        }

        /// <summary>Step 18: inside a realm hall (not the lodge): the realm's music at <see cref="HallDb"/>. Step 20: unused (halls have their own rule).</summary>
        public static void SetHall(bool on)
        {
            if (Instance != null) _instance._hall = on;
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
        public static void Jingle(float seconds = 2f)
        {
            if (Instance != null) _instance._jingleUntil = Mathf.Max(_instance._jingleUntil, Time.unscaledTime + seconds);
        }

        void Update()
        {
            float target = (_restored ? RestoredDb : 0f) + (_dialogue ? DialogueDb : 0f) + (_paused ? PauseDb : 0f) + (_hall ? HallDb : 0f) +
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
        static void ResetStatics()
        {
            _instance = null;
            _realmMusicBeforeRestore = -1;
        }
    }
}
