using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 11c: the lake temple's door on Lilymere's map. Shut (sunk, the water hoarded) until the lagoon rises
    /// (<see cref="openWhen"/>, 11b's scoop and pour); then it glows open like every enterable door (<see cref="DoorGlow"/>)
    /// and walking into it from the front goes into the temple hall. Bumping it shut says <see cref="shutLine"/>.
    /// </summary>
    public class TempleDoor : MonoBehaviour
    {
        public WorldMap map;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public SceneSwap swap;
        public BattleFx fx;
        public Material glowMaterial;
        [Tooltip("The puzzle that opens it (the lagoon rising).")]
        public string openWhen = "lm.lagoon";
        [Tooltip("Tiles in front of the door (map: column, row).")]
        public Vector2Int[] front = new Vector2Int[1];
        [Tooltip("The door's middle (column, height in tiles, row) and the portal's size (tiles).")]
        public Vector3 doorway;
        public Vector2 portalSize = new Vector2(1.1f, 1.3f);
        public WorldMap hall;
        public Vector2Int hallEntry = new Vector2Int(4, 8);
        public string shutLine = "lm.temple.shut";
        public string openLine = "lm.temple.open";

        DoorGlow _glow;
        float _nagged = -99f;
        bool _wasOpen;

        public bool IsOpen => PuzzleState.IsSolved(openWhen);
        bool Here => island != null && map != null && island.world == map && !SceneSwap.IsInside && !SceneSwap.IsBusy;

        void Start()
        {
            if (map == null || island == null || explorer == null || swap == null)
            {
                Debug.LogError("[TempleDoor] Needs the map, island, explorer and scene swap (Set Up MR_Main (11c Tidemother)).", this);
                enabled = false;
                return;
            }
            _glow = new DoorGlow("Lake Temple Door", island, glowMaterial, fx);
            explorer.OnBump += OnBump;
            explorer.OnStep += OnStep;
            _wasOpen = IsOpen;
        }

        void OnDestroy()
        {
            if (explorer != null)
            {
                explorer.OnBump -= OnBump;
                explorer.OnStep -= OnStep;
            }
            _glow?.Destroy();
        }

        bool AtFront(Vector2Int t)
        {
            foreach (var f in front)
                if (f == t) return true;
            return false;
        }

        void OnStep(Vector2Int tile)
        {
            if (!Here || !AtFront(tile) || IsOpen || Time.time - _nagged < 8f) return;
            _nagged = Time.time;
            if (!string.IsNullOrEmpty(shutLine)) Dialogue.Say(shutLine);
        }

        void OnBump(Vector2Int blocked)
        {
            if (!Here || !AtFront(explorer.Tile)) return;
            if (!IsOpen)
            {
                if (Time.time - _nagged < 8f) return;
                _nagged = Time.time;
                if (!string.IsNullOrEmpty(shutLine)) Dialogue.Say(shutLine);
                return;
            }
            Enter();
        }

        public void Enter()
        {
            if (hall == null)
            {
                Debug.LogWarning("[TempleDoor] The temple door leads nowhere (no hall).", this);
                return;
            }
            var stand = front.Length > 0 ? front[0] : explorer.Tile;
            Debug.Log("[TempleDoor] Into the lake temple.");
            swap.Enter(hall, hallEntry, new Vector2Int(0, -1), stand, new Vector2Int(0, 1));
        }

        void Update()
        {
            bool open = IsOpen;
            if (open && !_wasOpen && Here && !string.IsNullOrEmpty(openLine)) Dialogue.Say(openLine);
            _wasOpen = open;
            _glow?.Draw(Here && open, doorway, portalSize, front.Length > 0 ? (Vector2)front[0] : new Vector2(doorway.x, doorway.z + 1f));
        }

        /// <summary>Debug (bridge): into the temple now (if open).</summary>
        public void DebugEnter()
        {
            if (IsOpen) Enter();
        }
    }
}
