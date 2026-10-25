using System.Collections;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Cinderwood's fallen log (step 9b), the scene side of a <see cref="CarryPiece"/>: the island stops drawing the
    /// map's log (the piece draws its own) and its tiles open once it's solved (saved). Lifting it shakes leaves and dust
    /// off and the explorer hops; set on a ledge: a thud, a dust puff and a quick bounce of the island's tiles, the trail
    /// opens, the explorer cheers and the Spirit says <see cref="doneLine"/>. Dropped elsewhere: a small thud as it
    /// falls back across the gap.
    /// </summary>
    public class FallenLog : MonoBehaviour
    {
        public CarryPiece piece;
        [Tooltip("The log's anchor in the map's props grid (top-left tile of its footprint).")]
        public Vector2Int anchor;
        public IslandBuilder island;
        public GestureTestPiece explorer;
        public BattleFx fx;
        public string doneLine = "cinder.log.done";

        [Header("Landing")]
        [Tooltip("How far the island's tiles dip on the thud (island-local metres).")]
        public float bounce = 0.004f;
        public float bounceSeconds = 0.45f;
        [Range(0f, 1f)] public float volume = 0.8f;

        AudioSource _audio;
        Coroutine _bounce;

        WorldMap Map => piece != null ? piece.map : null;

        void Start()
        {
            if (piece == null || island == null || Map == null)
            {
                Debug.LogError("[FallenLog] Needs its CarryPiece (with a map) and the island (Set Up MR_Main (9b puzzles)).", this);
                enabled = false;
                return;
            }
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            piece.Lifted += OnLifted;
            piece.Landed += OnLanded;
            PuzzleState.Restored += Apply;
            Apply();
        }

        void OnDestroy()
        {
            if (piece != null)
            {
                piece.Lifted -= OnLifted;
                piece.Landed -= OnLanded;
            }
            PuzzleState.Restored -= Apply;
            if (island != null) island.contentLift = 0f;
            var map = Map;
            if (map != null)
            {
                map.SetPropHidden(anchor, false);
                map.SetPropOpen(anchor, false);
            }
        }

        /// <summary>The map's log hidden (the piece draws it); its tiles open once solved.</summary>
        void Apply()
        {
            var map = Map;
            map.SetPropHidden(anchor, true);
            map.SetPropOpen(anchor, piece.IsSolved);
            Debug.Log($"[FallenLog] {map.name} c{anchor.x} r{anchor.y}: {(piece.IsSolved ? "moved aside, the trail is open" : "across the trail")}.");
        }

        Vector3 LogWorld(float up = 0f)
        {
            var p = piece.Position;
            float tile = island.map.tileSize;
            return island.transform.TransformPoint(island.TileToLocal(new Vector2(p.x, p.z), island.Content.capHeight + (p.y + up) * tile));
        }

        float TileWorld => island.map.tileSize * island.Zoom * island.transform.lossyScale.x;

        void OnLifted(CarryPiece p)
        {
            if (fx != null)
            {
                float t = TileWorld;
                fx.Burst(LogWorld(0.3f), 18, new Color(0.85f, 0.42f, 0.18f), t * 0.6f, 1.1f, t * 0.06f, Vector3.zero, t * 3f);
                fx.Burst(LogWorld(0.1f), 14, new Color(0.62f, 0.52f, 0.4f), t * 0.5f, 0.9f, t * 0.05f, Vector3.zero, t * 2f);
            }
            if (explorer != null) explorer.Hop();
        }

        void OnLanded(CarryPiece p, int spot)
        {
            bool onLedge = spot >= 0;
            if (_audio != null) _audio.PlayOneShot(PuzzleSounds.Thud, volume * (onLedge ? 1f : 0.6f));
            if (fx != null)
            {
                float t = TileWorld;
                fx.Burst(LogWorld(0.05f), onLedge ? 40 : 18, new Color(0.72f, 0.62f, 0.48f), t * (onLedge ? 1.2f : 0.7f), 0.9f, t * 0.08f, Vector3.up * t * 0.4f, t * 0.5f);
            }
            if (_bounce != null) StopCoroutine(_bounce);
            _bounce = StartCoroutine(Bounce(onLedge ? bounce : bounce * 0.4f));
            if (!onLedge) return;
            Map.SetPropOpen(anchor, true);
            Debug.Log($"[FallenLog] The log is on ledge {spot}: the trail is open (saved).");
            if (explorer != null) explorer.Hop();
            if (!string.IsNullOrEmpty(doneLine)) Dialogue.Say(doneLine);
        }

        /// <summary>The tiles dip and spring back (the base stays put): a camera-free shake.</summary>
        IEnumerator Bounce(float depth)
        {
            for (float t = 0f; t < bounceSeconds; t += Time.deltaTime)
            {
                island.contentLift = -depth * Mathf.Exp(-t * 9f) * Mathf.Cos(t * 2f * Mathf.PI * 6f);
                yield return null;
            }
            island.contentLift = 0f;
            _bounce = null;
        }
    }
}
