using System;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// The battle choices: three move runes and a Run rune floating just above the island's near rim, facing the head,
    /// with a caption line above them. Pick with head gaze (soft reticle from the centre eye; Quest 3 has no eye
    /// tracking) and a pinch on either hand, or poke a rune with an index finger.
    /// Editor: 1-4 pick directly, Enter picks the rune under the gaze.
    /// </summary>
    public class BattleRunes : MonoBehaviour
    {
        public IslandBuilder island;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;
        public Material hudMaterial;

        [Header("Layout (island-local metres)")]
        [Tooltip("Row centre relative to the middle of the near rim, at the cap height.")]
        public Vector3 offset = new Vector3(0f, 0.05f, 0.02f);
        public Vector2 runeSize = new Vector2(0.085f, 0.04f);
        public float spacing = 0.095f;
        public float fontSize = 0.11f;
        public float captionFontSize = 0.12f;

        [Header("Poke")]
        [Tooltip("A fingertip this close to a rune's face (metres, world) counts as a poke.")]
        public float pokeDepth = 0.012f;

        /// <summary>0-2 = move slot, 3 = run.</summary>
        public event Action<int> Chosen;

        public const int RunIndex = 3;

        class Rune
        {
            public Transform root;
            public MeshRenderer plate;
            public TextMeshPro label;
            public Color tint;
            public bool usable;
            public float hover;
        }

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        readonly Rune[] _runes = new Rune[4];
        Transform _row;
        TextMeshPro _caption;
        MeshRenderer _reticle;
        Mesh _quad;
        MaterialPropertyBlock _block;
        bool _interactable;
        int _hovered = -1;
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = new bool[2];
        readonly bool[] _pokeArmed = { true, true };

        void Awake()
        {
            if (hudMaterial == null) Debug.LogError("[BattleRunes] No HUD material assigned.", this);
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            Build();
            Hide();
        }

        void OnDestroy() => Destroy(_quad);

        void Build()
        {
            _row = new GameObject("Rune Row").transform;
            _row.SetParent(transform, false);
            for (int i = 0; i < _runes.Length; i++)
            {
                var root = new GameObject($"Rune {i + 1}").transform;
                root.SetParent(_row, false);
                root.localPosition = new Vector3((i - 1.5f) * spacing, 0f, 0f);
                var plateGo = new GameObject("Plate");
                plateGo.transform.SetParent(root, false);
                plateGo.transform.localScale = new Vector3(runeSize.x, runeSize.y, 1f);
                plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
                var plate = plateGo.AddComponent<MeshRenderer>();
                plate.sharedMaterial = hudMaterial;
                plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var label = MakeText(root, fontSize);
                label.transform.localPosition = new Vector3(0f, 0f, -0.001f);
                _runes[i] = new Rune { root = root, plate = plate, label = label };
            }
            _caption = MakeText(_row, captionFontSize);
            _caption.transform.localPosition = new Vector3(0f, runeSize.y * 0.5f + 0.018f, 0f);

            var reticleGo = new GameObject("Gaze Reticle");
            reticleGo.transform.SetParent(_row, false);
            reticleGo.transform.localScale = Vector3.one * 0.008f;
            reticleGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            _reticle = reticleGo.AddComponent<MeshRenderer>();
            _reticle.sharedMaterial = hudMaterial;
            _block.Clear();
            _block.SetColor(BackColorId, new Color(1f, 1f, 1f, 0.55f));
            _block.SetColor(BorderColorId, new Color(1f, 1f, 1f, 0.9f));
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, 1f);
            _block.SetFloat(RadiusId, 0.5f);
            _block.SetFloat(BorderId, 0.12f);
            _reticle.SetPropertyBlock(_block);
        }

        /// <summary>Shows the row with these three moves plus Run. Not pickable until <see cref="SetInteractable"/>.</summary>
        public void Show(MoveData[] moves)
        {
            gameObject.SetActive(true);
            for (int i = 0; i < 3; i++)
            {
                var m = moves != null && i < moves.Length ? moves[i] : null;
                var r = _runes[i];
                r.usable = m != null;
                r.tint = m != null ? ElementStyle.Of(m.element).main : Color.gray;
                string kind = m == null ? "" : m.kind == MoveKind.Support ? "Support" : m.element.ToString();
                r.label.text = m != null ? $"<b>{m.Name}</b>\n<size=70%>{kind}</size>" : "-";
            }
            _runes[RunIndex].usable = true;
            _runes[RunIndex].tint = new Color(0.55f, 0.58f, 0.56f);
            _runes[RunIndex].label.text = "<b>Run</b>";
            _hovered = -1;
            PlaceRow(true);
            Paint();
        }

        public void Hide()
        {
            _interactable = false;
            gameObject.SetActive(false);
        }

        /// <summary>Dims the runes and ignores input while a turn plays.</summary>
        public void SetInteractable(bool on)
        {
            _interactable = on;
            for (int i = 0; i < 2; i++) _wasPinching[i] = true; // a pinch already held doesn't count
            Paint();
        }

        public void SetCaption(string text)
        {
            if (_caption != null) _caption.text = text;
        }

        void Update()
        {
            PlaceRow(false);
            _hovered = _interactable ? GazeHover() : -1;
            for (int i = 0; i < _runes.Length; i++)
            {
                var r = _runes[i];
                r.hover = Mathf.MoveTowards(r.hover, i == _hovered ? 1f : 0f, Time.deltaTime * 8f);
                r.root.localScale = Vector3.one * (1f + 0.12f * r.hover);
            }
            Paint();
            if (!_interactable) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) Pick(0, "key");
                else if (kb.digit2Key.wasPressedThisFrame) Pick(1, "key");
                else if (kb.digit3Key.wasPressedThisFrame) Pick(2, "key");
                else if (kb.digit4Key.wasPressedThisFrame) Pick(RunIndex, "key");
                else if (kb.enterKey.wasPressedThisFrame && _hovered >= 0) Pick(_hovered, "gaze + Enter");
                if (!_interactable) return;
            }
            for (int h = 0; h < 2; h++)
            {
                var hand = HandLookup.Refresh(ref _hands[h], h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid)
                {
                    _wasPinching[h] = false;
                    continue;
                }
                bool pinching = hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(h == 0);
                if (pinching && !_wasPinching[h] && !busy && _hovered >= 0) Pick(_hovered, $"gaze + pinch ({(h == 0 ? "left" : "right")})");
                _wasPinching[h] = pinching;
                if (!_interactable) return;
                if (HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip)) Poke(h, tip);
                if (!_interactable) return;
            }
        }

        /// <summary>Picks from code (testing). Ignored unless the runes are waiting for a choice.</summary>
        public void Choose(int index)
        {
            if (_interactable) Pick(index, "code");
        }

        void Pick(int index, string how)
        {
            if (index < 0 || index >= _runes.Length || !_runes[index].usable) return;
            Debug.Log($"[BattleRunes] Picked {(index == RunIndex ? "Run" : $"move {index + 1}")} by {how}.");
            _interactable = false;
            Paint();
            Chosen?.Invoke(index);
        }

        /// <summary>Keeps the row above the middle of the near rim (the side toward the player), facing the head.</summary>
        void PlaceRow(bool snap)
        {
            if (island == null || island.map == null) return;
            var b = island.LocalBounds;
            _row.localPosition = new Vector3(b.center.x, island.map.capHeight, b.max.z) + offset;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 away = _row.position - cam.transform.position;
            if (away.sqrMagnitude < 1e-6f) return;
            var target = Quaternion.LookRotation(away, Vector3.up);
            _row.rotation = snap ? target : Quaternion.Slerp(_row.rotation, target, 1f - Mathf.Exp(-Time.deltaTime * 4f));
        }

        int GazeHover()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                _reticle.enabled = false;
                return -1;
            }
            // The row faces the head: its plane is z = 0 in row space, the front looks toward -Z.
            Vector3 o = _row.InverseTransformPoint(cam.transform.position);
            Vector3 d = _row.InverseTransformDirection(cam.transform.forward);
            if (d.z <= 1e-4f)
            {
                _reticle.enabled = false;
                return -1;
            }
            Vector3 hit = o + d * (-o.z / d.z);
            float scale = _row.lossyScale.x;
            bool near = Mathf.Abs(hit.x) < spacing * 2.6f && Mathf.Abs(hit.y) < runeSize.y * 2.5f;
            _reticle.enabled = near;
            if (near) _reticle.transform.localPosition = new Vector3(hit.x, hit.y, -0.002f / Mathf.Max(1e-4f, scale));
            for (int i = 0; i < _runes.Length; i++)
            {
                Vector3 p = hit - _runes[i].root.localPosition;
                // A little bigger than the plate, so the gaze doesn't flicker between runes.
                if (Mathf.Abs(p.x) < spacing * 0.5f && Mathf.Abs(p.y) < runeSize.y * 0.75f) return _runes[i].usable ? i : -1;
            }
            return -1;
        }

        void Poke(int hand, Vector3 tipWorld)
        {
            float scale = Mathf.Max(1e-4f, _row.lossyScale.x);
            float depth = pokeDepth / scale;
            for (int i = 0; i < _runes.Length; i++)
            {
                Vector3 p = _runes[i].root.InverseTransformPoint(tipWorld);
                bool inside = Mathf.Abs(p.x) < runeSize.x * 0.5f && Mathf.Abs(p.y) < runeSize.y * 0.5f;
                if (inside && p.z > -depth && p.z < depth * 2f)
                {
                    if (_pokeArmed[hand])
                    {
                        _pokeArmed[hand] = false;
                        Pick(i, $"poke ({(hand == 0 ? "left" : "right")})");
                    }
                    return;
                }
            }
            _pokeArmed[hand] = true;
        }

        void Paint()
        {
            foreach (var r in _runes)
            {
                if (r == null || r.plate == null) continue;
                float dim = _interactable && r.usable ? 1f : 0.45f;
                var back = Color.Lerp(new Color(0.07f, 0.09f, 0.08f), r.tint, Mathf.Lerp(0.3f, 0.6f, dim) + 0.25f * r.hover);
                back.a = 0.9f * Mathf.Lerp(0.6f, 1f, dim);
                var border = Color.Lerp(r.tint, Color.white, r.hover * 0.6f);
                border.a = dim;
                _block.Clear();
                _block.SetColor(BackColorId, back);
                _block.SetColor(BorderColorId, border);
                _block.SetFloat(FillId, 0f);
                _block.SetFloat(AspectId, runeSize.x / runeSize.y);
                _block.SetFloat(RadiusId, 0.3f);
                _block.SetFloat(BorderId, 0.07f + 0.05f * r.hover);
                r.plate.SetPropertyBlock(_block);
                var c = r.label.color;
                c.a = Mathf.Lerp(0.5f, 1f, dim);
                r.label.color = c;
            }
        }

        static TextMeshPro MakeText(Transform parent, float size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.color = Color.white;
            t.outlineWidth = 0.2f;
            t.outlineColor = new Color32(20, 30, 25, 255);
            t.rectTransform.sizeDelta = new Vector2(0.3f, 0.04f);
            return t;
        }
    }
}
