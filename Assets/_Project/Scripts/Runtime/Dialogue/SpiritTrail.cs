using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12a: the Spirit's trail. Glowing gold tiles (<see cref="GoldMarks"/>, a pulse travelling toward the goal) from
    /// the explorer toward the next goal of the full adventure: in a realm the nearest unsolved hand moment, else the
    /// Keeper's door, else the return gate; in the hub the statue while a shard is held, else the next realm gate whose
    /// shard isn't home yet. It shows after the tutorial's last lesson, on entering a realm, after standing still
    /// <see cref="stillSeconds"/> with no objective pinned, and when the Spirit is poked; it fades on reaching the goal or
    /// after <see cref="showSeconds"/>. The quick slot keeps its own gold path (<see cref="QuickJourneyDirector"/>).
    /// </summary>
    public class SpiritTrail : MonoBehaviour
    {
        public GestureTestPiece explorer;
        public IslandBuilder island;
        public EncounterDirector encounters;
        public ShardGates gates;
        public Onboarding onboarding;
        [Tooltip("Diorama/GoldMark (Materials/GoldMark).")]
        public Material material;

        [Tooltip("Seconds standing still with no objective before the trail shows.")]
        public float stillSeconds = 12f;
        [Tooltip("Seconds the trail stays up.")]
        public float showSeconds = 20f;
        [Tooltip("Tiles of trail shown ahead of the explorer.")]
        [Min(2)] public int maxTiles = 14;
        [Tooltip("Gold tile size, in tiles.")]
        public float tileSize = 0.78f;
        [Range(0f, 3f)] public float intensity = 1.5f;

        /// <summary>Keeper doors by realm (landmarks).</summary>
        static readonly Dictionary<string, (string shard, string door)> Realms = new Dictionary<string, (string, string)>
        {
            { "cinderwood", (ShardHolder.FireId, "temple") },
            { "galewick", (ShardHolder.WindId, "tower") },
            { "lilymere", (ShardHolder.WaterId, "temple") },
        };

        /// <summary>Step 14c: Galewick first (the wind shard), then Cinderwood, then Lilymere. Any open gate still works.</summary>
        public static readonly (string gate, string shard)[] HubOrder =
        {
            ("wind", ShardHolder.WindId), ("fire", ShardHolder.FireId), ("water", ShardHolder.WaterId),
        };

        /// <summary>The gate the hub's guidance points to: the first in <see cref="HubOrder"/> whose shard isn't home and that's open (pure).</summary>
        public static string NextGate(System.Func<string, bool> shardHome, System.Func<string, bool> gateOpen)
        {
            foreach (var (gate, shard) in HubOrder)
                if (!shardHome(shard) && gateOpen(gate)) return gate;
            return null;
        }

        GoldMarks _marks;
        readonly List<Vector2Int> _path = new List<Vector2Int>();
        float _shownAt = -99f, _pathAt = -99f, _still;
        Vector2Int _lastTile;
        Vector2Int? _goal;
        HandPuzzle[] _pieces;

        /// <summary>The trail is showing (tests, the bridge).</summary>
        public bool Showing => _goal.HasValue && Time.time - _shownAt < showSeconds;
        public Vector2Int? Goal => _goal;
        public int Shown => _marks != null ? _marks.Shown : 0;

        void Start()
        {
            _marks = new GoldMarks("Spirit Trail", island, material) { intensity = intensity };
            SpiritPoke.Poked += OnPoked;
            var swap = FindFirstObjectByType<SceneSwap>();
            if (swap != null) swap.TravelFinished += OnTravelled;
            if (onboarding != null) onboarding.LessonEnded += OnLessonEnded;
        }

        void OnDestroy()
        {
            SpiritPoke.Poked -= OnPoked;
            var swap = FindFirstObjectByType<SceneSwap>();
            if (swap != null) swap.TravelFinished -= OnTravelled;
            if (onboarding != null) onboarding.LessonEnded -= OnLessonEnded;
            if (_marks != null) _marks.Destroy();
        }

        void OnPoked(bool objective) => Show("Spirit poked");

        void OnTravelled(WorldMap to) => Show("entered " + (to != null ? to.name : "?"));

        void OnLessonEnded(LessonData lesson, bool done)
        {
            // Only once the tutorial is over: during it the lessons draw their own gold.
            if (!Onboarding.Running) Show("tutorial done");
        }

        bool Active => !QuickJourney.Active && explorer != null && island != null && island.world != null && !SceneSwap.IsBusy &&
                       !SceneSwap.IsInside && !TitleScreen.Showing && !Opening.InProgress && !Onboarding.Running && !GameMenu.IsOpen &&
                       (encounters == null || !encounters.InBattle);

        /// <summary>Shows the trail toward the next goal now (false when there is none or it can't be reached).</summary>
        public bool Show(string why)
        {
            if (!Active) return false;
            var goal = NextGoal(island.world);
            if (!goal.HasValue || !ExplorerPath.FindAlongRoads(island.world, explorer.Tile, goal.Value, _path, 400) || _path.Count <= 1)
            {
                Debug.Log($"[SpiritTrail] {why}: no goal to show{(goal.HasValue ? $" (c{goal.Value.x} r{goal.Value.y} not reachable or here)" : "")}.");
                _goal = null;
                return false;
            }
            _goal = goal;
            _shownAt = Time.time;
            _pathAt = Time.time;
            GameAudio.At(Diorama.Audio.AudioIds.PathGold, island.transform.TransformPoint(island.TileToLocal(_path[0], island.Content.capHeight)));
            Debug.Log($"[SpiritTrail] {why}: {_path.Count} tiles toward c{goal.Value.x} r{goal.Value.y}.");
            return true;
        }

        /// <summary>Where the adventure goes next from here, or null.</summary>
        public Vector2Int? NextGoal(WorldMap world)
        {
            if (world == null || explorer == null) return null;
            if (world.realm == "stillisle")
            {
                foreach (var (_, shard) in HubOrder)
                    if (ShardHolder.Holds(shard)) return Mark(world, "statue");
                var next = NextGate(PuzzleState.IsSolved, id =>
                {
                    int g = world.gates.FindIndex(x => x.id == id);
                    return g >= 0 && (gates == null || gates.IsOpen(world.gates[g]));
                });
                int i = next != null ? world.gates.FindIndex(g => g.id == next) : -1;
                return i >= 0 ? GameState.NearestWalkable(world, world.gates[i].Front(0)) : (Vector2Int?)null;
            }
            if (!Realms.TryGetValue(world.realm ?? "", out var realm)) return null;
            // A shard won (held or home): back to the hub.
            if (ShardHolder.Holds(realm.shard) || PuzzleState.IsSolved(realm.shard)) return HomeGate(world);
            if (_pieces == null) _pieces = FindObjectsByType<HandPuzzle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Vector2Int? best = null;
            int bestDist = int.MaxValue;
            foreach (var p in _pieces)
            {
                if (p == null || p.map != world || p.IsSolved || p is StatueSocket || p is KeeperUntangle || !p.WalkStopWanted) continue;
                // Already at one: its own gold marks lead.
                if (p.InReach(explorer.Tile)) return null;
                var t = GameState.NearestWalkable(world, Vector2Int.RoundToInt(p.Home));
                int d = Mathf.Abs(t.x - explorer.Tile.x) + Mathf.Abs(t.y - explorer.Tile.y);
                if (d < bestDist && ExplorerPath.CanReach(world, explorer.Tile, t, 400))
                {
                    bestDist = d;
                    best = t;
                }
            }
            return best ?? Mark(world, realm.door);
        }

        Vector2Int? HomeGate(WorldMap world)
        {
            Vector2Int? best = null;
            foreach (var g in world.gates)
            {
                if (g.id != "home") continue;
                var t = GameState.NearestWalkable(world, g.Front(0));
                if (!best.HasValue || (t - explorer.Tile).sqrMagnitude < (best.Value - explorer.Tile).sqrMagnitude) best = t;
            }
            return best;
        }

        static Vector2Int? Mark(WorldMap world, string landmark) =>
            world.TryGetLandmark(landmark, out var t) ? GameState.NearestWalkable(world, t) : (Vector2Int?)null;

        void Update()
        {
            if (explorer == null) return;
            // Standing still with nothing to do: show the way.
            bool idle = Active && Dialogue.CurrentObjective == null && !Showing && explorer.Tile == _lastTile && !explorer.Destination.HasValue && explorer.Heading == Vector2Int.zero;
            _still = idle ? _still + Time.deltaTime : 0f;
            _lastTile = explorer.Tile;
            if (_still >= stillSeconds)
            {
                _still = 0f;
                Show("standing still");
            }
            if (!_goal.HasValue) return;
            if (!Active || explorer.Tile == _goal.Value || Time.time - _shownAt > showSeconds + 1.5f)
            {
                if (explorer.Tile == _goal.Value) Debug.Log("[SpiritTrail] Reached the goal.");
                _goal = null;
                _path.Clear();
                return;
            }
            // Keep the trail starting at the explorer as it walks.
            if (Time.time - _pathAt > 0.5f)
            {
                _pathAt = Time.time;
                if (!ExplorerPath.FindAlongRoads(island.world, explorer.Tile, _goal.Value, _path, 400)) _path.Clear();
            }
        }

        void LateUpdate()
        {
            if (_marks == null) return;
            _marks.Begin();
            if (_goal.HasValue && _path.Count > 0)
            {
                float age = Time.time - _shownAt;
                float fade = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((showSeconds + 1.5f - age) / 1.5f);
                int n = Mathf.Min(maxTiles, _path.Count);
                for (int i = 0; i < n; i++)
                {
                    // Appears outward from the explorer.
                    float a = fade * Mathf.Clamp01((age - i * 0.05f) / 0.2f);
                    _marks.Draw(_path[i], GoldMarks.Tile, 0f, i == _path.Count - 1 ? tileSize * 1.15f : tileSize, a, n > 1 ? i / (float)(n - 1) : 1f, true);
                }
            }
            _marks.End();
        }
    }
}
