using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 12b (Note: "out door of temple doesn't look like a door"): inside any interior (the Keeper halls, the lodge)
    /// the way out reads as a door: the 11d door glow (a warm portal swirl and a gold ring on the threshold) on the exit
    /// tiles, and a gold arrow on the floor pointing out, once its Keeper is calmed (the lodge's is always on). Oct 28 notes:
    /// while the Keeper still needs you it stays dark (a bright arch on arrival hid the explorer and pointed the wrong way).
    /// </summary>
    public class ExitDoorGuide : MonoBehaviour
    {
        public SceneSwap swap;
        public IslandBuilder island;
        public BattleFx fx;
        [Tooltip("Diorama/Beacon (the door glow, portal and arrow modes).")]
        public Material beaconMaterial;
        [Tooltip("Doorway size in tiles (width, height).")]
        public Vector2 doorSize = new Vector2(2f, 2.2f);

        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        DoorGlow _glow;
        MeshRenderer _arrow;
        MaterialPropertyBlock _block;
        ForgeHall[] _halls;
        KeeperHall[] _keepers;
        LodgeRoom[] _lodges;

        void Start()
        {
            if (island == null || beaconMaterial == null)
            {
                Debug.LogError("[ExitDoorGuide] Needs the island and the beacon material (Set Up MR_Main (12b night notes)).", this);
                enabled = false;
                return;
            }
            _glow = new DoorGlow("Exit Door", island, beaconMaterial, fx);
            var go = new GameObject("Exit Arrow");
            go.layer = island.gameObject.layer;
            go.transform.SetParent(island.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = BattleHud.VerticalQuad();
            _arrow = go.AddComponent<MeshRenderer>();
            _arrow.sharedMaterial = beaconMaterial;
            _arrow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            _block = new MaterialPropertyBlock();
            _halls = FindObjectsByType<ForgeHall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _keepers = FindObjectsByType<KeeperHall>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _lodges = FindObjectsByType<LodgeRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        void OnDestroy()
        {
            _glow?.Destroy();
            if (_arrow != null) Destroy(_arrow.gameObject);
        }

        /// <summary>The current interior's exit tiles and whether its job is done; false outside or for an unknown room.</summary>
        bool Exit(out Vector2 a, out Vector2 b, out bool done)
        {
            a = b = default;
            done = false;
            if (swap == null || !swap.Inside || swap.Interior == null || SceneSwap.IsBusy) return false;
            var room = swap.Interior;
            foreach (var h in _halls)
                if (h != null && h.hall == room && h.doorTiles != null && h.doorTiles.Length > 0)
                {
                    a = h.doorTiles[0];
                    b = h.doorTiles[h.doorTiles.Length - 1];
                    foreach (var k in _keepers)
                        if (k != null && k.hall == room) done = PuzzleState.IsSolved(k.keeperId);
                    return true;
                }
            foreach (var l in _lodges)
                if (l != null && l.Active && l.doorTiles != null && l.doorTiles.Length > 0)
                {
                    a = l.doorTiles[0];
                    b = l.doorTiles[l.doorTiles.Length - 1];
                    done = true;
                    return true;
                }
            return false;
        }

        void LateUpdate()
        {
            if (_glow == null) return;
            bool on = Exit(out var a, out var b, out bool done) && done;
            var map = island.Content;
            Vector2 mid = (a + b) * 0.5f;
            // Out = toward the nearer edge row (the halls' door is on the near edge, the lodge's on the far one).
            float outward = map != null && mid.y > (map.Rows - 1) * 0.5f ? 1f : -1f;
            _glow.Draw(on, new Vector3(mid.x, doorSize.y * 0.5f, mid.y + outward * 0.5f), doorSize, mid);
            bool arrow = on && map != null && island.FootprintContains(island.TileToLocal(mid, map.capHeight));
            if (_arrow.gameObject.activeSelf != arrow) _arrow.gameObject.SetActive(arrow);
            if (!arrow) return;
            // Lying flat on the tile inside the threshold, pointing out (the arrow runs along its +v; rows run toward +Z).
            var at = mid - new Vector2(0f, outward * 1.1f);
            _arrow.transform.localPosition = island.TileToLocal(at, map.capHeight + 0.06f * island.map.tileSize);
            _arrow.transform.localRotation = Quaternion.Euler(0f, outward > 0f ? 0f : 180f, 0f) * Quaternion.Euler(90f, 0f, 0f);
            _arrow.transform.localScale = Vector3.one * (island.map.tileSize * island.Zoom * 1.3f);
            _block.Clear();
            _block.SetFloat(ModeId, 5f);
            _block.SetFloat(IntensityId, done ? 2.2f : 1f);
            _arrow.SetPropertyBlock(_block);
        }
    }
}
