using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Audio hookup, parts 3-4: reads the game's state every frame (which map or interior shows, its restoration, a battle,
    /// a Keeper battle, dialogue, the pause menu) and sets the ambience (<see cref="AmbienceDirector"/>, profiles from
    /// Resources/Ambience) and the music (<see cref="MusicDirector"/>) to match. Polling, not events, so a late listener
    /// never misses a change. Also keeps <see cref="AudioService.DistanceScale"/> and the sweeteners' island bounds on the
    /// island. Created by <see cref="GameAudio"/> after the scene loads; survives scene swaps.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        const float RestoredAt = 0.99f;

        static AudioDirector _instance;
        bool _finale;

        readonly Dictionary<string, AmbienceProfile> _profiles = new Dictionary<string, AmbienceProfile>();
        IslandBuilder _island;
        EncounterDirector _encounters;
        string _area, _realm;
        bool _wasRestored;
        float _ambienceDuck;
        bool _sweetPaused;
        // Step 18: arriving in a realm, its music waits for the realm name card, then fades in.
        MusicState _areaMusic;
        float _arrivalHold = -1f;
        // Step 20: this battle is a Keeper's (its Keeper reference goes before the battle ends).
        bool _keeperFight;

        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("[AudioDirector]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<AudioDirector>();
            go.AddComponent<LandmarkLoops>();
        }

        /// <summary>The ambience profile name in use (StillIsle, CinderwoodFaded...), for the dev readout and tests.</summary>
        public static string Area => _instance != null ? _instance._area : null;

        void Update()
        {
            if (_island == null) _island = FindFirstObjectByType<IslandBuilder>();
            if (_encounters == null) _encounters = FindFirstObjectByType<EncounterDirector>();
            if (_island == null) return;
            FollowIsland();
            Pick(out string area, out string realm, out bool restored, out MusicState music);
            // Step 20: back home from a realm, the Still Isle's welcome may play again.
            if (realm == "StillIsle" && _realm != null && _realm != "StillIsle") MusicDirector.ArrivedHome();
            // Step 20: the opening is quiet on the Still Isle (the title's track carries it); the title has its own track (once).
            if (Opening.InProgress && music == MusicState.Hub) music = MusicState.None;
            if (TitleScreen.Showing && !Opening.ShardOnly) music = MusicState.Title;
            // Step 16: while only the shard floats in the room, no island ambience or music (they start as it unfolds).
            bool shardOnly = Opening.ShardOnly;
            if (shardOnly)
            {
                area = "(shard)";
                music = MusicState.None;
            }
            // Step 21 (Mugesh: the song kept playing while he poked the shard; my step 20 carry-over broke the Nov 6 rule):
            // the title's track doesn't carry into the shard moment, it fades out as the shard arrives.
            if (shardOnly && AudioService.IsSounding(Diorama.Audio.AudioIds.MusicTitle))
            {
                AudioService.StopMusic(2f);
                Debug.Log("[AudioDirector] Only the shard: music.title fades out (2 s).");
            }
            if (area != _area && shardOnly)
            {
                AmbienceDirector.SetArea(null, 0.5f);
                _area = area;
                Debug.Log("[AudioDirector] Only the shard: no island ambience or music yet.");
            }
            else if (area != _area)
            {
                float fade = realm == _realm && restored != _wasRestored ? 4f : SceneSwap.IsInside || _area == "Lodge" || _area == "ForgeHall" ? 1.5f : 2f;
                AmbienceDirector.SetArea(Profile(area), fade);
                _area = area;
            }
            _realm = realm;
            _wasRestored = restored;
            bool battle = _encounters != null && _encounters.InBattle;
            if (battle)
            {
                if (_encounters.Keeper != null) _keeperFight = true;
                music = _keeperFight ? MusicState.BattleKeeper : MusicState.BattleWild;
            }
            else _keeperFight = false;
            // Step 13: the finale has its own cue (silent until a track lands); the area music comes back after.
            bool finale = FinaleDirector.Running;
            if (finale != _finale)
            {
                _finale = finale;
                if (finale)
                {
                    MusicDirector.SetState(MusicState.None, 2f);
                    if (AudioService.Has(Diorama.Audio.AudioIds.MusicFinale)) AudioService.SetMusic(Diorama.Audio.AudioIds.MusicFinale, 2f);
                    Debug.Log("[AudioDirector] Finale music (music.finale).");
                }
            }
            // Step 20: the title's track starts right away (no arrival hold).
            if (music == MusicState.Title && !finale)
            {
                _areaMusic = music;
                _arrivalHold = -1f;
            }
            // Step 18: arriving in a new realm (not back from a battle or a hall), hold its music until the realm name card
            // is gone, then fade it in over 3 s.
            if (!battle && music != _areaMusic && music != MusicState.None)
            {
                _areaMusic = music;
                _arrivalHold = Time.unscaledTime + 1.2f;
            }
            // Under the title the realm's music waits too (it started under Continue / New journey), then the card.
            if (_arrivalHold >= 0f && (TitleScreen.Showing || Opening.ReturningUp)) _arrivalHold = Time.unscaledTime + 1.2f;
            bool holding = _arrivalHold >= 0f && (Time.unscaledTime < _arrivalHold || RealmNameCard.Showing) && !battle;
            if (_arrivalHold >= 0f && !holding && !battle)
            {
                _arrivalHold = -1f;
                if (!finale) MusicDirector.SetState(music, MusicDirector.ArrivalFadeIn);
            }
            if (!finale && !holding) MusicDirector.SetState(music);
            else if (!finale && holding && MusicDirector.State != MusicState.None && MusicDirector.State != music) MusicDirector.SetState(MusicState.None, 1.5f);
            // Step 20: halls have their own rule now (temple; the realm's track after the restoration at full level).
            MusicDirector.SetHall(false);
            MusicDirector.SetRestored(restored && realm == "Cinderwood");
            bool talking = Dialogue.IsTalking;
            MusicDirector.SetDialogue(talking);
            MusicDirector.SetPaused(GamePause.IsPaused || GameMenu.IsOpen);
            float duck = battle ? -6f : talking ? -3f : 0f;
            if (duck != _ambienceDuck || battle != _sweetPaused)
            {
                AmbienceDirector.Duck(duck, duck < _ambienceDuck ? 0.4f : 0.8f, battle);
                _ambienceDuck = duck;
                _sweetPaused = battle;
            }
        }

        /// <summary>Cue distances and the sweeteners' disc follow the island's place and size.</summary>
        void FollowIsland()
        {
            var t = _island.transform;
            float scale = t.lossyScale.x;
            AudioService.DistanceScale = scale;
            var centre = t.TransformPoint(new Vector3(0f, _island.CapTopLocal, 0f));
            AmbienceDirector.Bounds = new IslandBounds(centre, 0.35f * scale, 0.05f * scale);
        }

        void Pick(out string area, out string realm, out bool restored, out MusicState music)
        {
            var swap = SceneSwap.Current;
            var outdoor = swap != null ? swap.Outdoor : _island.world;
            restored = (swap != null ? swap.OutdoorRestored : RealmState.Restored) >= RestoredAt;
            string name = outdoor != null ? outdoor.name : "";
            realm = name.Contains("Cinderwood") ? "Cinderwood" : name.Contains("Galewick") ? "Galewick" : name.Contains("Lilymere") ? "Lilymere" : "StillIsle";
            music = realm switch
            {
                "Cinderwood" => MusicState.Cinderwood,
                "Galewick" => MusicState.Galewick,
                "Lilymere" => MusicState.Lilymere,
                _ => MusicState.Hub,
            };
            // Step 20: an unrestored realm has no music (ambience only); its track comes back with the restoration, from the
            // moment its shard is taken in the hall (the realm's own restoration may still be filling in then).
            bool musicRestored = restored || ShardTaken(realm);
            if (realm != "StillIsle" && !musicRestored && !MusicDirector.RealmMusicBeforeRestore) music = MusicState.None;
            if (swap != null && swap.Inside && swap.Interior != null)
            {
                // The lodge is home (its own track, once); the realm halls are the Keepers' temples.
                bool lodge = swap.Interior.name.Contains("Lodge");
                area = lodge ? "Lodge" : "ForgeHall";
                if (lodge) music = MusicState.Lodge;
                else
                {
                    // Step 20: the temple track until the Keeper is calmed, then quiet until its shard is taken (the bloom).
                    var hall = KeeperHall.Current;
                    if (hall != null && !musicRestored) music = hall.Calmed ? MusicState.None : MusicState.Temple;
                }
                return;
            }
            if (realm == "StillIsle") area = "StillIsle";
            else area = realm + (restored ? "Restored" : "Faded");
        }

        /// <summary>Step 20: the realm's shard has been taken from its hall (held by the Spirit or in the statue).</summary>
        static bool ShardTaken(string realm)
        {
            string id = realm switch
            {
                "Cinderwood" => ShardHolder.FireId,
                "Galewick" => ShardHolder.WindId,
                "Lilymere" => ShardHolder.WaterId,
                _ => null,
            };
            return id != null && (PuzzleState.Stage(id) >= 1 || PuzzleState.IsSolved(id));
        }

        AmbienceProfile Profile(string name)
        {
            if (_profiles.TryGetValue(name, out var p)) return p;
            p = Resources.Load<AmbienceProfile>("Ambience/" + name);
            if (p == null) Debug.LogError($"[AudioDirector] No ambience profile Resources/Ambience/{name}: that area plays no ambience.");
            _profiles[name] = p;
            return p;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _instance = null;
    }
}
