using System.Collections.Generic;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step audio-D (P1): one 3D loop per kind of landmark in the island window: the nearest lit Hearth Shrine's flame,
    /// ember vent, hot spring, and (once the realm is restored) mill. Scans the outdoor map's props once per map version;
    /// every few frames picks the nearest of each kind within the window and moves its loop there. Silent inside halls
    /// and the lodge and during battles. Lives on the [AudioDirector] object (survives scene swaps).
    /// </summary>
    public class LandmarkLoops : MonoBehaviour
    {
        const float Every = 0.25f;

        class Kind
        {
            public string cue;
            public string[] recipes;
            public bool restoredOnly;
            public float volume = 1f;
            public readonly List<Vector2> spots = new List<Vector2>();
            public LoopAnchor loop;
        }

        readonly Kind[] _kinds =
        {
            new Kind { cue = AudioIds.ShrineFlame, recipes = new[] { HearthShrines.RecipeName } },
            new Kind { cue = AudioIds.EmberCrackle, recipes = new[] { "EmberVent" }, volume = 0.8f },
            new Kind { cue = AudioIds.SteamHiss, recipes = new[] { "HotSpringSteam" }, volume = 0.8f },
            new Kind { cue = AudioIds.MillTurn, recipes = new[] { "Windmill", "SmallMill" }, restoredOnly = true, volume = 0.6f },
        };

        readonly List<Vector2Int> _shrineAnchors = new List<Vector2Int>();
        IslandBuilder _island;
        EncounterDirector _encounters;
        WorldMap _scanned;
        int _scannedVersion = -1;
        float _next;

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + Every;
            if (_island == null) _island = FindFirstObjectByType<IslandBuilder>();
            if (_encounters == null) _encounters = FindFirstObjectByType<EncounterDirector>();
            var swap = SceneSwap.Current;
            bool outside = _island != null && _island.Content != null && (swap == null || !swap.Inside);
            bool quiet = !outside || _encounters != null && _encounters.InBattle || GamePause.IsPaused;
            var world = outside ? swap != null ? swap.Outdoor : _island.world : null;
            if (quiet || world == null)
            {
                foreach (var k in _kinds) k.loop?.Off(0.6f);
                return;
            }
            if (world != _scanned || world.version != _scannedVersion) Scan(world);
            bool restored = (swap != null ? swap.OutdoorRestored : RealmState.Restored) >= 0.99f;
            Vector2 view = _island.ViewCenterTile;
            float reach = Mathf.Min(_island.map.Columns, _island.map.Rows) * 0.5f / Mathf.Max(0.01f, _island.Zoom);
            foreach (var k in _kinds)
            {
                k.loop ??= new LoopAnchor(transform, $"Landmark {k.cue}");
                int best = -1;
                float bestD = reach;
                for (int i = 0; i < k.spots.Count; i++)
                {
                    if (k.cue == AudioIds.ShrineFlame && HearthShrines.IsDormant(world.name, _shrineAnchors[i])) continue;
                    float d = (k.spots[i] - view).magnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
                if (best < 0 || k.restoredOnly && !restored)
                {
                    k.loop.Off(0.8f);
                    continue;
                }
                var at = _island.transform.TransformPoint(_island.TileToLocal(k.spots[best], _island.Content.capHeight + 0.5f * _island.map.tileSize));
                k.loop.Set(k.cue, at, 1f);
                k.loop.Handle.SetVolume(k.volume);
            }
        }

        void Scan(WorldMap world)
        {
            _scanned = world;
            _scannedVersion = world.version;
            _shrineAnchors.Clear();
            foreach (var k in _kinds) k.spots.Clear();
            for (int r = 0; r < world.Rows; r++)
                for (int c = 0; c < world.Columns; c++)
                {
                    if (!world.TryGetProp(world.PropAt(r, c), out var e) || e.recipe == null) continue;
                    foreach (var k in _kinds)
                        if (System.Array.IndexOf(k.recipes, e.recipe.name) >= 0)
                        {
                            // Shrines are 2 x 2 props: their middle is a tile corner.
                            bool shrine = k.cue == AudioIds.ShrineFlame;
                            k.spots.Add(new Vector2(c, r) + (shrine ? new Vector2(0.5f, 0.5f) : Vector2.zero));
                            if (shrine) _shrineAnchors.Add(new Vector2Int(c, r));
                        }
                }
            var counts = new List<string>();
            foreach (var k in _kinds) counts.Add($"{k.cue} {k.spots.Count}");
            Debug.Log($"[LandmarkLoops] {world.name}: {string.Join(", ", counts)}.");
        }
    }
}
