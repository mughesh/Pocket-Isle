using System.Collections;
using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Galewick's big mill (step 10b), the scene side of a <see cref="WheelPiece"/>: the gear by its door turns with the
    /// sails, and once they keep turning (three flicks in four seconds) the drawbridge over the ditch swings down with a
    /// creak and dust and the crossing opens (saved with the wheel). While it is up, a strip of ditch water shows under it.
    /// The island doesn't draw the map's drawbridge: this does (<see cref="PropView"/>).
    /// </summary>
    public class BigMill : MonoBehaviour
    {
        public WheelPiece wheel;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public BattleFx fx;

        [Header("Map anchors (props grid: column, row)")]
        [Tooltip("The drawbridge (2 x 2, its north-west tile): the two crossing tiles over the ditch.")]
        public Vector2Int bridge;
        [Tooltip("The gear's spot (tiles, map column / row), by the mill's door, and its height.")]
        public Vector2 gearTile;
        public float gearHeight = 0.55f;

        [Header("Looks")]
        public PropRecipe bridgeRecipe, gapRecipe, gearRecipe;
        [Tooltip("Gear turns this many times per sail turn.")]
        public float gearRatio = 2.5f;
        public float lowerSeconds = 2.2f;

        [Header("Lines")]
        public string doneLine = "gw.mill.done";
        [Range(0f, 1f)] public float volume = 0.7f;

        PropView _bridge, _gap, _gear;
        AudioSource _audio;
        LoopAnchor _turn;
        float _down; // 0 = raised, 1 = lowered
        bool _lowering;

        WorldMap Map => wheel != null ? wheel.map : null;

        void Start()
        {
            if (wheel == null || island == null || Map == null)
            {
                Debug.LogError("[BigMill] Needs its WheelPiece (with a map) and the island (Set Up MR_Main (10b Galewick hands)).", this);
                enabled = false;
                return;
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _bridge = new PropView("Drawbridge", island, bridgeRecipe);
            _gap = new PropView("Ditch Gap", island, gapRecipe);
            _gear = new PropView("Mill Gear", island, gearRecipe);
            wheel.Solved += OnSolved;
            PuzzleState.Restored += Apply;
            Apply();
        }

        void OnDestroy()
        {
            if (wheel != null) wheel.Solved -= OnSolved;
            PuzzleState.Restored -= Apply;
            _bridge?.Destroy();
            _gap?.Destroy();
            _gear?.Destroy();
            var map = Map;
            if (map == null) return;
            map.SetPropHidden(bridge, false);
            map.SetPropOpen(bridge, false);
        }

        void Apply()
        {
            StopAllCoroutines();
            _lowering = false;
            bool open = wheel.IsSolved;
            _down = open ? 1f : 0f;
            Map.SetPropHidden(bridge, true);
            Map.SetPropOpen(bridge, open);
            Debug.Log($"[BigMill] Drawbridge {(open ? "down" : "up")}.");
        }

        void OnSolved(HandPuzzle p)
        {
            if (!_lowering && _down < 1f) StartCoroutine(Lower());
        }

        IEnumerator Lower()
        {
            _lowering = true;
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
            var hub = MapWorld(new Vector3(bridge.x + 0.5f, 0.3f, bridge.y + 0.5f));
            _turn ??= new LoopAnchor(transform, "Mill Sound");
            _turn.Set(AudioIds.MillTurn, hub, 0.3f);
            if (!_turn.Playing) GameAudio.Procedural(_audio, PuzzleSounds.Rumble, volume);
            yield return new WaitForSeconds(0.6f);
            for (float t = 0f; t < lowerSeconds; t += Time.deltaTime)
            {
                // Eases down, then a little bounce as it lands.
                float u = t / lowerSeconds;
                _down = u < 0.85f ? Mathf.SmoothStep(0f, 1.04f, u / 0.85f) : Mathf.Lerp(1.04f, 1f, (u - 0.85f) / 0.15f);
                yield return null;
            }
            _down = 1f;
            _turn.Off(0.25f);
            GameAudio.Play(AudioIds.MillStop, hub, _audio, PuzzleSounds.Thud, volume);
            if (fx != null)
            {
                float tile = TileWorld;
                for (int i = 0; i < 4; i++)
                {
                    var at = MapWorld(new Vector3(bridge.x + 0.5f + Random.Range(-0.5f, 1.5f) - 0.5f, 0.1f, bridge.y + Random.Range(0f, 1f)));
                    fx.Burst(at, 6, new Color(0.85f, 0.8f, 0.7f, 0.8f), tile * 0.5f, 0.9f, tile * 0.08f, Vector3.up * tile * 0.4f);
                }
            }
            Map.SetPropOpen(bridge, true);
            _lowering = false;
            Debug.Log("[BigMill] The drawbridge is down: the way north is open.");
        }

        void Update()
        {
            if (_bridge == null) return;
            bool here = wheel.Here;
            // Hinged on the south bank (the bridge's near edge): up = standing on its end, down = flat over the ditch.
            Vector2 centre = bridge + new Vector2(0.5f, 0.5f);
            float hingeRow = bridge.y + 1.5f;
            // Raised: turned +88 about X (its far, north edge up); the deck's centre sits on the circle round the hinge.
            float angle = Mathf.LerpUnclamped(88f, 0f, _down);
            float half = 1f;
            float rad = angle * Mathf.Deg2Rad;
            var at = new Vector2(centre.x, hingeRow - half * Mathf.Cos(rad));
            float lift = half * Mathf.Sin(rad);
            _bridge.Place(at, lift, Quaternion.Euler(angle, 0f, 0f), here);
            _gap.Place(centre, -0.08f, Quaternion.identity, here && _down < 0.98f);
            _gear.spinSelf = wheel.Spin.Angle * gearRatio;
            _gear.Place(gearTile, gearHeight, Quaternion.identity, here);
        }

        float TileWorld => island.map.tileSize * island.Zoom * island.transform.lossyScale.x;

        Vector3 MapWorld(Vector3 m) =>
            island.transform.TransformPoint(island.TileToLocal(new Vector2(m.x, m.z), island.Content.capHeight + m.y * island.map.tileSize));

        /// <summary>Debug (bridge): lowered or raised at once.</summary>
        public float DebugDown => _down;
    }
}
