using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Hearth Shrines on the world map: props whose recipe is <see cref="RecipeName"/>. Stepping onto a shrine's front
    /// tile (its +row side, toward the player) flares the flame, rolls a warm wave over the rim pods, heals everyone (pods
    /// and storage), raises <see cref="Rested"/> (the save listens) and shows the storage shelf until the explorer walks
    /// off. The last shrine used is where the explorer returns after losing (<see cref="RespawnTile"/>).
    /// The Spirit's <c>shrine</c> hint target points at the shrine being used, or the nearest one.
    /// </summary>
    public class HearthShrines : MonoBehaviour
    {
        public const string RecipeName = "HearthShrine";

        public IslandBuilder island;
        public GestureTestPiece explorer;
        public Party party;
        public PodRack pods;
        public StorageShelf shelf;
        public BattleFx fx;

        /// <summary>The explorer rested at this shrine (anchor tile): healed, time to save.</summary>
        public event Action<Vector2Int> Rested;

        /// <summary>The last shrine used (anchor tile), or null. Saved and loaded with the game.</summary>
        public Vector2Int? LastShrine { get; set; }

        /// <summary>Where the explorer goes after losing: the last shrine's front tile, or the world's start tile.</summary>
        public Vector2Int RespawnTile => LastShrine.HasValue ? FrontOf(LastShrine.Value) : explorer.SpawnTile;

        public IReadOnlyList<Vector2Int> Shrines => _shrines;

        readonly List<Vector2Int> _shrines = new List<Vector2Int>();
        Vector2Int? _at;
        WorldMap _scanned;
        int _scannedVersion = -1;

        static Vector2Int FrontOf(Vector2Int shrine) => shrine + Vector2Int.up;

        void Start()
        {
            if (island == null || explorer == null || party == null || pods == null)
            {
                Debug.LogError("[HearthShrines] Needs the island, explorer, party and pod rack (Set Up MR_Main (7 progression)).", this);
                enabled = false;
                return;
            }
            if (shelf == null) Debug.LogError("[HearthShrines] No storage shelf; shrines heal and save but can't swap.", this);
            explorer.OnStep += OnStep;
            Dialogue.RegisterTarget("shrine", ShrinePoint, k => { });
            Scan();
        }

        void OnDestroy()
        {
            if (explorer != null) explorer.OnStep -= OnStep;
        }

        void Scan()
        {
            var world = island.world;
            if (world == _scanned && (world == null || world.version == _scannedVersion)) return;
            _scanned = world;
            _scannedVersion = world != null ? world.version : -1;
            _shrines.Clear();
            if (world == null) return;
            for (int r = 0; r < world.Rows; r++)
                for (int c = 0; c < world.Columns; c++)
                    if (world.TryGetProp(world.PropAt(r, c), out var e) && e.recipe != null && e.recipe.name == RecipeName)
                        _shrines.Add(new Vector2Int(c, r));
            Debug.Log($"[HearthShrines] {_shrines.Count} shrine(s) on {world.name}: {string.Join(", ", _shrines.ConvertAll(s => $"c{s.x} r{s.y}"))}.");
        }

        void OnStep(Vector2Int tile)
        {
            Scan();
            Vector2Int? here = null;
            foreach (var s in _shrines)
                if (FrontOf(s) == tile) here = s;
            if (here.HasValue)
            {
                if (_at != here) Rest(here.Value);
            }
            else if (_at.HasValue)
            {
                _at = null;
                if (shelf != null) shelf.Hide();
            }
        }

        /// <summary>Heal, warm wave, flare, save and the shelf. Also callable from code (testing).</summary>
        public void Rest(Vector2Int shrine)
        {
            _at = shrine;
            LastShrine = shrine;
            party.HealEveryone();
            pods.WarmWave();
            StartCoroutine(Flare(shrine));
            Dialogue.Say("shrine.first");
            if (shelf != null) shelf.Show();
            Debug.Log($"[HearthShrines] Rested at the shrine c{shrine.x} r{shrine.y}: everyone healed; it is the respawn point now.");
            Rested?.Invoke(shrine);
        }

        Vector3 FlameWorld(Vector2Int shrine)
        {
            float tile = island.map.tileSize;
            return island.transform.TransformPoint(island.TileToLocal(shrine, island.map.capHeight + 0.22f * tile * island.Zoom));
        }

        /// <summary>The flame flares: a bright orb swells and fades, embers fly up.</summary>
        IEnumerator Flare(Vector2Int shrine)
        {
            if (fx == null) yield break;
            float scale = island.transform.lossyScale.x * island.map.tileSize;
            var warm = new Color(1f, 0.7f, 0.3f);
            fx.Burst(FlameWorld(shrine), 30, warm, scale * 0.6f, 0.9f, scale * 0.06f, Vector3.up * scale * 0.8f);
            var orb = fx.Get(BattleFx.Shape.Orb);
            if (orb == null) yield break;
            orb.Set(Color.white, warm, 1f, 1.5f, 1f, 0.3f);
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                float u = t / 0.7f;
                orb.transform.position = FlameWorld(shrine);
                orb.transform.localScale = Vector3.one * (scale * (0.3f + 0.9f * Mathf.Sin(u * Mathf.PI)) / Mathf.Max(1e-4f, fx.transform.lossyScale.x));
                orb.SetAlpha(1f - u * u);
                yield return null;
            }
            fx.Release(orb);
        }

        Vector3? ShrinePoint()
        {
            if (this == null || _shrines.Count == 0) return null;
            if (_at.HasValue) return FlameWorld(_at.Value);
            Vector2Int best = _shrines[0];
            foreach (var s in _shrines)
                if ((s - explorer.Tile).sqrMagnitude < (best - explorer.Tile).sqrMagnitude) best = s;
            return (best - explorer.Tile).sqrMagnitude <= 36 ? FlameWorld(best) : (Vector3?)null;
        }
    }
}
