using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    public enum MoveMode
    {
        [Tooltip("Swipe starts walking; it keeps going until another swipe, a tap or the edge.")]
        AutoWalk,
        [Tooltip("Swipe starts walking; it keeps going only while the thumb stays off-centre.")]
        HoldToWalk,
    }

    /// <summary>
    /// Step 2 microgesture test: a placeholder cube that walks across the island, plus a small world-space
    /// label beside the island showing the last gesture, the hand and placement.
    /// AutoWalk: a swipe sets the walking direction and the cube keeps walking until another swipe, the island
    /// edge, or a tap stops it. HoldToWalk: same, but it also stops when the thumb returns to rest (read from
    /// ThumbHoldProbe; in the Editor, releasing the arrow key). In both modes a second swipe in the same direction
    /// while moving switches to run, turning keeps the current speed, and tap while standing still makes it hop.
    /// Swipes map to the user's view of the island: forward = away, back = toward, left / right = their left / right.
    /// </summary>
    public class GestureTestPiece : MonoBehaviour
    {
        public IslandBuilder island;
        public MicrogestureInput input;
        public IslandPlacer placer;
        public ThumbHoldProbe probe;
        public Material cubeMaterial;

        [Tooltip("Can be switched live in Play mode.")]
        public MoveMode moveMode = MoveMode.HoldToWalk;

        [Tooltip("Start tile (prototype explorer start).")]
        public Vector2Int startTile = new Vector2Int(10, 7); // (column, row)

        public float cubeSize = 0.028f;   // island-local metres
        [Tooltip("Seconds per tile when walking.")]
        public float walkSeconds = 0.6f;
        [Tooltip("Seconds per tile when running.")]
        public float runSeconds = 0.3f;
        public float hopHeight = 0.045f;
        public float hopTime = 0.35f;

        [Header("Hold to walk")]
        [Tooltip("Stop when the thumb comes back within this distance (mm) of its rest point.")]
        public float holdReleaseMm = 5f;
        [Tooltip("Ignore the hold check for this long after a swipe, while the thumb settles.")]
        public float holdGraceSeconds = 0.25f;

        Transform _cube;
        TextMeshPro _label;
        Vector2Int _tile;
        Vector2Int _heading;
        bool _running;
        GestureSource _source;
        float _swipeTime;
        Vector3 _from, _to;
        float _stepT = 1f, _hopT = 1f;
        int _count;
        string _last = "none yet";

        void Start()
        {
            _tile = startTile;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Gesture Test Cube";
            Destroy(go.GetComponent<Collider>());
            if (cubeMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
            _cube = go.transform;
            _cube.SetParent(island.transform, false);
            _cube.localScale = Vector3.one * cubeSize;
            _from = _to = TilePosition(_tile);
            _cube.localPosition = _to;

            var labelGo = new GameObject("Gesture Label");
            labelGo.transform.SetParent(island.transform, false);
            _label = labelGo.AddComponent<TextMeshPro>();
            _label.fontSize = 0.25f;
            _label.alignment = TextAlignmentOptions.Left;
            _label.color = Color.white;
            _label.outlineWidth = 0.2f;
            _label.outlineColor = new Color32(20, 30, 25, 255);
            _label.rectTransform.sizeDelta = new Vector2(0.22f, 0.12f);

            input.Gesture += OnGesture;
            if (placer != null) placer.Placed += PositionLabel;
            if (probe == null) Debug.LogError("[GestureTestPiece] No ThumbHoldProbe assigned; HoldToWalk will stop right after each swipe.");
            PositionLabel();
            RefreshLabel();
        }

        void OnDestroy()
        {
            if (input != null) input.Gesture -= OnGesture;
            if (placer != null) placer.Placed -= PositionLabel;
        }

        void OnGesture(ThumbGesture gesture, GestureSource source)
        {
            _count++;
            _last = $"{gesture} ({source})";
            Vector2Int d = gesture switch
            {
                ThumbGesture.SwipeLeft => new Vector2Int(-1, 0),
                ThumbGesture.SwipeRight => new Vector2Int(1, 0),
                ThumbGesture.SwipeForward => new Vector2Int(0, -1),
                ThumbGesture.SwipeBack => new Vector2Int(0, 1),
                _ => Vector2Int.zero,
            };
            if (gesture == ThumbGesture.Tap)
            {
                if (_heading != Vector2Int.zero) Stop();
                else _hopT = 0f;
            }
            else
            {
                if (d == _heading) _running = true;
                _heading = d;
                _source = source;
                _swipeTime = Time.unscaledTime;
                if (_stepT >= 1f) TryStep();
            }
            RefreshLabel();
        }

        void TryStep()
        {
            var next = _tile + _heading;
            if (island.map.At(next.y, next.x) == IslandMap.Empty)
            {
                Stop();
                _last += " - edge";
                RefreshLabel();
                return;
            }
            _tile = next;
            _from = _to;
            _to = TilePosition(_tile);
            _stepT = 0f;
            RefreshLabel();
        }

        void Stop()
        {
            _heading = Vector2Int.zero;
            _running = false;
        }

        bool ThumbStillHeld()
        {
            if (_source == GestureSource.Keyboard)
            {
                var kb = Keyboard.current;
                if (kb == null) return false;
                if (_heading.x < 0) return kb.leftArrowKey.isPressed;
                if (_heading.x > 0) return kb.rightArrowKey.isPressed;
                return _heading.y < 0 ? kb.upArrowKey.isPressed : kb.downArrowKey.isPressed;
            }
            // Lost tracking counts as released.
            return probe != null && probe.TryGetOffsetMm(_source == GestureSource.LeftHand, out var mm) && mm.magnitude >= holdReleaseMm;
        }

        Vector3 TilePosition(Vector2Int tile)
        {
            var map = island.map;
            float y = map.capHeight + cubeSize * 0.5f;
            if (map.At(tile.y, tile.x) == IslandMap.Water) y -= 0.008f;
            return map.TileCenter(tile.y, tile.x) + Vector3.up * y;
        }

        void Update()
        {
            if (_cube == null) return;
            if (moveMode == MoveMode.HoldToWalk && _heading != Vector2Int.zero &&
                Time.unscaledTime - _swipeTime > holdGraceSeconds && !ThumbStillHeld())
            {
                Stop(); // the current step still finishes on its tile
                RefreshLabel();
            }
            if (_stepT >= 1f && _heading != Vector2Int.zero) TryStep();
            Vector3 p = _to;
            if (_stepT < 1f)
            {
                // Linear so consecutive steps join into a steady walk.
                _stepT = Mathf.Min(1f, _stepT + Time.deltaTime / (_running ? runSeconds : walkSeconds));
                p = Vector3.Lerp(_from, _to, _stepT);
            }
            if (_hopT < 1f)
            {
                _hopT = Mathf.Min(1f, _hopT + Time.deltaTime / hopTime);
                p.y += Mathf.Sin(_hopT * Mathf.PI) * hopHeight;
            }
            _cube.localPosition = p;

            // Face the label to the user (yaw only).
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 to = _label.transform.position - cam.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-6f) _label.transform.rotation = Quaternion.LookRotation(to, Vector3.up);
            }
        }

        void PositionLabel()
        {
            // Beside the island, on the user's right (island -X), a little above the surface.
            var b = island.LocalBounds;
            _label.transform.localPosition = new Vector3(b.min.x - 0.13f, 0.08f, 0f);
            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (_label == null) return;
            string place = placer != null ? placer.Status : "-";
            string move = _heading == Vector2Int.zero ? "Stopped" : $"{(_running ? "Run" : "Walk")} {HeadingName(_heading)}";
            _label.text = $"<b>Mode:</b> {moveMode}\n<b>Last:</b> {_last}\n<b>Count:</b> {_count}\n<b>Tile:</b> r{_tile.y} c{_tile.x}  <b>{move}</b>\n<b>Place:</b> {place}";
        }

        static string HeadingName(Vector2Int h)
        {
            if (h.x < 0) return "left";
            if (h.x > 0) return "right";
            return h.y < 0 ? "forward" : "back";
        }
    }
}
