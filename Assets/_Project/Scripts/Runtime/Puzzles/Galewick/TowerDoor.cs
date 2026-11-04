using System.Collections;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's wind tower door (step 10b D): the explorer reaching the tower's front with all three kites flying
    /// (<see cref="TowerRules"/>) — the kites detach from the backpack and fly up to tie onto the crown, the vanes start
    /// turning and the door sinks open (saved as <see cref="TowerId"/>). With fewer kites the Spirit says
    /// <see cref="needKitesLine"/>. Once open the island draws the tower without its door (<see cref="openRecipe"/>, vanes
    /// at their own angle) and the door leaf sinking; before that the map's own tower shows. Walking into the open door
    /// leads to the tower hall from step 10d (<see cref="hall"/>; empty = nowhere yet).
    /// </summary>
    public class TowerDoor : MonoBehaviour
    {
        public const string TowerId = "gw.tower";

        public WorldMap map;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public KitePiece kites;
        public SceneSwap swap;
        public BattleFx fx;

        [Header("Map (props grid: column, row)")]
        [Tooltip("The tower's anchor (2 x 2, north-west tile).")]
        public Vector2Int tower;
        [Tooltip("Tiles in front of the door (the explorer stands here to open it).")]
        public Vector2Int[] front = new Vector2Int[2];

        [Header("Looks")]
        public PropRecipe openRecipe, doorRecipe;
        public float vaneSpeed = 1.6f;
        public float openSeconds = 2.4f;
        [Tooltip("Step 11d: the open doorway's portal swirl and threshold ring, and the crown's three empty glowing hooks (Diorama/Beacon).")]
        public Material glowMaterial;
        [Tooltip("The portal's middle (tiles: column offset from the tower's middle, height, row offset from its north edge) and size (tiles).")]
        public Vector3 portalOffset = new Vector3(0f, 0.75f, 1.6f);
        public Vector2 portalSize = new Vector2(0.9f, 1.3f);

        [Header("Lines")]
        public string needKitesLine = "gw.tower.kites";
        public string openLine = "gw.tower.open";
        [Range(0f, 1f)] public float volume = 0.7f;

        [Header("Into the tower (step 10d)")]
        public WorldMap hall;
        public Vector2Int hallEntry = new Vector2Int(4, 8);

        PropView _open, _door;
        DoorGlow _glow;
        GuideRings _hooks;
        AudioSource _audio;
        LoopAnchor _vaneSound;
        float _sink, _vanes, _vaneSpeed;
        bool _isOpen, _opening, _warnedHall;
        float _nagged = -99f, _fewNagged = -99f;

        public bool IsOpen => _isOpen;
        bool Here => island != null && map != null && island.world == map && !SceneSwap.IsInside && !SceneSwap.IsBusy;
        Vector2 Center => tower + new Vector2(0.5f, 0.5f);

        void Start()
        {
            if (map == null || island == null || explorer == null || kites == null)
            {
                Debug.LogError("[TowerDoor] Needs the map, island, explorer and the kites (Set Up MR_Main (10b Galewick hands)).", this);
                enabled = false;
                return;
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _open = new PropView("Wind Tower (open)", island, openRecipe);
            _door = new PropView("Wind Tower Door", island, doorRecipe);
            _glow = new DoorGlow("Wind Tower Door", island, glowMaterial, fx);
            _hooks = new GuideRings("Wind Tower Hook", island, glowMaterial, 3, fx);
            explorer.OnStep += OnStep;
            explorer.OnBump += OnBump;
            PuzzleState.Restored += Apply;
            Apply();
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnStep -= OnStep;
                explorer.OnBump -= OnBump;
            }
            PuzzleState.Restored -= Apply;
            _open?.Destroy();
            _door?.Destroy();
            _glow?.Destroy();
            _hooks?.Destroy();
            if (map != null) map.SetPropHidden(tower, false);
        }

        void Apply()
        {
            StopAllCoroutines();
            _opening = false;
            _isOpen = PuzzleState.IsSolved(TowerId);
            _sink = _isOpen ? 1f : 0f;
            _vaneSpeed = _isOpen ? vaneSpeed : 0f;
            map.SetPropHidden(tower, _isOpen);
            Debug.Log($"[TowerDoor] Wind tower door {(_isOpen ? "open" : "shut")}.");
        }

        /// <summary>Step 14k: the walk-up rule: the front tiles and the front diagonals (one more each side).</summary>
        public bool AtFront(Vector2Int t)
        {
            foreach (var f in front)
                if (t.y == f.y && Mathf.Abs(t.x - f.x) <= 1) return true;
            return false;
        }

        void OnStep(Vector2Int tile)
        {
            if (!Here || !AtFront(tile) || _isOpen || _opening) return;
            Try();
        }

        void OnBump(Vector2Int blocked)
        {
            if (!Here || !AtFront(explorer.Tile)) return;
            if (_isOpen)
            {
                // Step 14h: a small team gets a warning and Go in / Not yet (TempleRules).
                if (TempleRules.Gate(true, "Galeowl", ref _fewNagged, this, Enter)) Enter();
            }
            else if (!_opening) Try();
        }

        void Try()
        {
            int flying = kites.FlyingCount;
            if (TowerRules.Opens(flying, true))
            {
                StartCoroutine(Open());
                return;
            }
            if (Time.time - _nagged < 8f) return;
            _nagged = Time.time;
            Debug.Log($"[TowerDoor] At the door with {flying} kite(s): needs {TowerRules.KitesNeeded}.");
            if (!string.IsNullOrEmpty(needKitesLine)) Dialogue.Say(needKitesLine);
        }

        IEnumerator Open()
        {
            _opening = true;
            explorer.Locked = true;
            Debug.Log("[TowerDoor] Three kites: they tie onto the crown, the vanes turn, the door opens.");
            kites.Tie();
            yield return new WaitForSeconds(1.6f);
            map.SetPropHidden(tower, true);
            var doorAt = MapWorld(new Vector3(Center.x, 0.3f, tower.y + 1.75f));
            _vaneSound ??= new LoopAnchor(transform, "Vane Sound");
            _vaneSound.Set(AudioIds.MillTurn, MapWorld(new Vector3(Center.x, 2f, tower.y + 1f)), 0.4f);
            if (!_vaneSound.Playing) GameAudio.Procedural(_audio, PuzzleSounds.Whoosh, volume);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                _vaneSpeed = Mathf.Lerp(0f, vaneSpeed, t / 1.2f);
                yield return null;
            }
            if (!string.IsNullOrEmpty(openLine)) Dialogue.Say(openLine);
            _vaneSound.Off(1.5f);
            GameAudio.Play(AudioIds.TowerDoor, doorAt, _audio, PuzzleSounds.Rumble, volume);
            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                _sink = Mathf.SmoothStep(0f, 1f, t / openSeconds);
                if (fx != null && Random.value < 0.2f)
                {
                    float tile = TileWorld;
                    var at = MapWorld(new Vector3(Center.x + Random.Range(-0.3f, 0.3f), 0.1f, tower.y + 1.75f));
                    fx.Burst(at, 4, new Color(0.88f, 0.86f, 0.8f, 0.8f), tile * 0.4f, 0.9f, tile * 0.07f, Vector3.up * tile * 0.4f);
                }
                yield return null;
            }
            _sink = 1f;
            _isOpen = true;
            _opening = false;
            explorer.Locked = false;
            PuzzleState.Solve(TowerId, 1);
            explorer.Cheer();
            Debug.Log("[TowerDoor] The wind tower is open.");
        }

        void Enter()
        {
            if (hall == null || swap == null)
            {
                if (!_warnedHall) Debug.LogWarning("[TowerDoor] The tower door leads nowhere yet (no tower hall: step 10d).", this);
                _warnedHall = true;
                return;
            }
            var stand = front.Length > 0 ? front[0] : explorer.Tile;
            Debug.Log("[TowerDoor] Into the wind tower.");
            swap.Enter(hall, hallEntry, new Vector2Int(0, -1), stand, new Vector2Int(0, 1));
        }

        void Update()
        {
            if (_open == null) return;
            bool here = Here;
            _vanes += _vaneSpeed * Time.deltaTime;
            bool drawOpen = _isOpen || _opening && map.IsPropHidden(tower);
            _open.spinSelf = _vanes;
            _open.Place(Center, 0f, Quaternion.identity, here && drawOpen);
            _door.Place(Center, -1.2f * _sink, Quaternion.identity, here && drawOpen && _sink < 0.999f);
            // Step 11d: an open door glows like the forge's; before the kites are tied, the crown shows three empty hooks.
            _glow?.Draw(here && _isOpen, new Vector3(Center.x + portalOffset.x, portalOffset.y, tower.y + portalOffset.z), portalSize,
                front.Length > 0 ? (Vector2)front[0] : Center + Vector2.up * 2f);
            if (_hooks != null && kites != null)
            {
                bool hooks = here && !_isOpen && !_opening;
                for (int i = 0; i < 3; i++)
                {
                    float pulse = 0.85f + 0.2f * Mathf.Sin(Time.time * 2.5f + i * 1.3f);
                    _hooks.Draw(i, hooks, kites.tiedAt + new Vector3((i - 1) * 0.55f, 0.05f, 0f), 0.6f, 1.2f * pulse, 0.45f * pulse);
                }
            }
        }

        /// <summary>The open door's glow shows (tests, bridge).</summary>
        public bool Glowing => _glow != null && _glow.Showing;

        float TileWorld => island.map.tileSize * island.Zoom * island.transform.lossyScale.x;

        Vector3 MapWorld(Vector3 m) =>
            island.transform.TransformPoint(island.TileToLocal(new Vector2(m.x, m.z), island.Content.capHeight + m.y * island.map.tileSize));

        /// <summary>Debug (bridge): try the door as if the explorer just arrived.</summary>
        public void DebugTry() => Try();
    }
}
