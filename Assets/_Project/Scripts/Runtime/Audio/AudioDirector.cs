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
            // Step 16: while only the shard floats in the room, no island ambience or music (they start as it unfolds).
            bool shardOnly = Opening.ShardOnly;
            if (shardOnly)
            {
                area = "(shard)";
                music = MusicState.None;
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
            if (battle) music = _encounters.Keeper != null ? MusicState.BattleKeeper : MusicState.BattleWild;
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
            if (!finale) MusicDirector.SetState(music);
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
            if (swap != null && swap.Inside && swap.Interior != null)
            {
                // The lodge is home (hub music); the realm halls are big stone rooms that keep the realm's music.
                bool lodge = swap.Interior.name.Contains("Lodge");
                area = lodge ? "Lodge" : "ForgeHall";
                if (lodge) music = MusicState.Hub;
                return;
            }
            if (realm == "StillIsle") area = "StillIsle";
            else area = realm + (restored ? "Restored" : "Faded");
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
