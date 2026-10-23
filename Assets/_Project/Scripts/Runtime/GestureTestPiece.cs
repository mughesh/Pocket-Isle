using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    public enum MoveMode
    {
        [Tooltip("Swipe starts walking; it keeps going until another swipe, a tap or a blocked tile.")]
        AutoWalk,
        [Tooltip("Swipe starts walking; it keeps going only while the thumb stays off-centre.")]
        HoldToWalk,
    }

    /// <summary>
    /// Placeholder explorer: a cube that walks the world map tile by tile, shown through the island window, plus a
    /// small world-space label beside the island (last gesture, tile, zoom, placement).
    /// AutoWalk: a swipe sets the walking direction and the cube keeps walking until another swipe, a blocked tile
    /// (water, tree, rock, border), or a tap stops it. HoldToWalk: same, but it also stops when the thumb returns to
    /// rest (read from ThumbHoldProbe; in the Editor, releasing the arrow key). In both modes a second swipe in the
    /// same direction while moving switches to run, and turning keeps the current speed.
    /// Tap while standing still toggles the battle zoom (temporary test; Z in the Editor). No walking while zoomed.
    /// Swipes map to the user's view of the island: forward = away, back = toward, left / right = their left / right.
    /// </summary>
    public class GestureTestPiece : MonoBehaviour
    {
        public IslandBuilder island;
        public WorldWindow window;
        public MicrogestureInput input;
        public IslandPlacer placer;
        public ThumbHoldProbe probe;
        public Material cubeMaterial;

        [Tooltip("Can be switched live in Play mode.")]
        public MoveMode moveMode = MoveMode.HoldToWalk;

        [Tooltip("Start tile (column, row) when the island has no world map; a world map has its own start tile.")]
        public Vector2Int startTile = new Vector2Int(10, 7);

        public float cubeSize = 0.028f;   // map metres at zoom 1
        [Tooltip("Seconds per tile when walking.")]
        public float walkSeconds = 0.6f;
        [Tooltip("Seconds per tile when running.")]
        public float runSeconds = 0.3f;

        [Header("Hold to walk")]
        [Tooltip("Stop when the thumb comes back within this distance (mm) of its rest point.")]
        public float holdReleaseMm = 5f;
        [Tooltip("Ignore the hold check for this long after a swipe, while the thumb settles.")]
        public float holdGraceSeconds = 0.25f;

        /// <summary>Raised when the explorer arrives on a tall grass tile (column, row).</summary>
        public event Action<Vector2Int> OnTallGrassStep;

        Transform _cube;
        TextMeshPro _label;
        Vector2Int _tile, _fromTile;
        Vector2Int _heading;
        bool _running;
        GestureSource _source;
        float _swipeTime;
        float _stepT = 1f;
        int _count;
        string _last = "none yet";
        bool _zoomedIn;

        IslandMap Map => island.Content;

        void Start()
        {
            if (window == null) Debug.LogError("[GestureTestPiece] No WorldWindow assigned; the map won't scroll or zoom.");
            _tile = _fromTile = island.world != null ? island.world.startTile : startTile;
            if (!IslandMap.IsWalkable(Map.At(_tile.y, _tile.x)))
                Debug.LogWarning($"[GestureTestPiece] Start tile c{_tile.x} r{_tile.y} is '{Map.At(_tile.y, _tile.x)}', not walkable.");

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Gesture Test Cube";
            Destroy(go.GetComponent<Collider>());
            if (cubeMaterial != null) go.GetComponent<MeshRenderer>().sharedMaterial = cubeMaterial;
            _cube = go.transform;
            _cube.SetParent(island.transform, false);

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
            OnTallGrassStep += FlashTallGrass;
            PositionLabel();
            UpdateExplorer();
            RefreshLabel();
        }

        void OnDestroy()
        {
            if (input != null) input.Gesture -= OnGesture;
            if (placer != null) placer.Placed -= PositionLabel;
        }

        bool Zoomed => window != null && window.IsZoomed;

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
                else if (_stepT >= 1f) ToggleZoom();
            }
            else if (Zoomed)
            {
                _last += " - zoomed, no walking";
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

        void ToggleZoom()
        {
            if (window == null) return;
            // Toggles the target, so a second tap mid-animation reverses it.
            if (_zoomedIn) window.ZoomOut();
            else window.ZoomTo(_tile);
            _zoomedIn = !_zoomedIn;
            RefreshLabel();
            Debug.Log($"[GestureTestPiece] Zoom {(_zoomedIn ? "in" : "out")} on c{_tile.x} r{_tile.y}.");
        }

        void TryStep()
        {
            var next = _tile + _heading;
            char ch = Map.At(next.y, next.x);
            if (!IslandMap.IsWalkable(ch))
            {
                Stop();
                _last += ch == IslandMap.Empty ? " - edge" : $" - blocked ({ch})";
                RefreshLabel();
                return;
            }
            _fromTile = _tile;
            _tile = next;
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
            // Lost tracking or a hand busy with the island counts as released.
            if (input.IsBlocked(_source)) return false;
            return probe != null && probe.TryGetOffsetMm(_source == GestureSource.LeftHand, out var mm) && mm.magnitude >= holdReleaseMm;
        }

        void Update()
        {
            if (_cube == null) return;
            var kb = Keyboard.current;
            if (kb != null && kb.zKey.wasPressedThisFrame && _heading == Vector2Int.zero && _stepT >= 1f) ToggleZoom();

            if (moveMode == MoveMode.HoldToWalk && _heading != Vector2Int.zero &&
                Time.unscaledTime - _swipeTime > holdGraceSeconds && !ThumbStillHeld())
            {
                Stop(); // the current step still finishes on its tile
                RefreshLabel();
            }
            if (_stepT < 1f)
            {
                // Linear so consecutive steps join into a steady walk.
                _stepT = Mathf.Min(1f, _stepT + Time.deltaTime / (_running ? runSeconds : walkSeconds));
                if (_stepT >= 1f && Map.At(_tile.y, _tile.x) == IslandMap.TallGrass) OnTallGrassStep?.Invoke(_tile);
            }
            if (_stepT >= 1f && _heading != Vector2Int.zero) TryStep();
            UpdateExplorer();

            // Face the label to the user (yaw only).
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 to = _label.transform.position - cam.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 1e-6f) _label.transform.rotation = Quaternion.LookRotation(to, Vector3.up);
            }
        }

        /// <summary>Scrolls the window to the explorer, then places the cube against that view.</summary>
        void UpdateExplorer()
        {
            Vector2 p = Vector2.Lerp(_fromTile, _tile, _stepT);
            if (window != null) window.Follow(p);
            _cube.localPosition = island.TileToLocal(p, Map.capHeight + cubeSize * 0.5f);
            _cube.localScale = Vector3.one * (cubeSize * island.Zoom);
        }

        void FlashTallGrass(Vector2Int tile)
        {
            island.FlashTile(tile);
            Debug.Log($"[GestureTestPiece] Tall grass step at c{tile.x} r{tile.y}.");
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
            string move = _zoomedIn ? "Zoomed" : _heading == Vector2Int.zero ? "Stopped" : $"{(_running ? "Run" : "Walk")} {HeadingName(_heading)}";
            _label.text = $"<b>Mode:</b> {moveMode}\n<b>Last:</b> {_last}\n<b>Count:</b> {_count}\n<b>Tile:</b> c{_tile.x} r{_tile.y}  <b>{move}</b>\n<b>Place:</b> {place}";
        }

        static string HeadingName(Vector2Int h)
        {
            if (h.x < 0) return "left";
            if (h.x > 0) return "right";
            return h.y < 0 ? "forward" : "back";
        }
    }
}
