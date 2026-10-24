using System.Collections.Generic;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// The storage shelf shown while the explorer stands at a Hearth Shrine: a floating panel beside the island's right
    /// side with two rows of 6 pods (more pages behind small arrows). Swaps:
    /// - grab a storage pod and drop it on a rim pod (swap) or the next empty socket (it joins the party);
    /// - look + pinch a storage pod, then a rim pod or the empty socket (or the other way round);
    /// - look + pinch a rim pod, then an empty shelf cell = it goes to storage (never the last one).
    /// Arrows: poke or look + pinch. Sizes are island-local metres.
    /// </summary>
    public class StorageShelf : MonoBehaviour
    {
        public IslandBuilder island;
        public Party party;
        public PodRack pods;
        public PodView podPrefab;
        public Material hudMaterial;
        public BattleFx fx;
        [Tooltip("Pinches from a hand that is grabbing or touching the rim are ignored.")]
        public IslandHandle grabGuard;

        [Header("Layout (island-local metres)")]
        [Tooltip("Panel centre from the near-right footprint corner: x outward (to the right), y above the cap, z inward.")]
        public Vector3 offset = new Vector3(-0.04f, 0.15f, 0.06f);
        [Tooltip("The panel pitches back so its face points at the eyes, clamped to this (degrees).")]
        public float maxTilt = 40f;
        public float spacing = 0.042f;
        public float rowGap = 0.05f;
        [Tooltip("Grab volume of a storage pod (diameter).")]
        public float grabSize = 0.04f;
        [Tooltip("A dropped storage pod lands in a rim socket within this distance (island-local metres).")]
        public float dropDistance = 0.05f;
        public float gazeRadius = 2.2f;

        const int Columns = 6, Rows = 2, PerPage = Columns * Rows;

        static readonly int BackColorId = Shader.PropertyToID("_BackColor");
        static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        public bool Visible => _target > 0.5f;

        Transform _root;
        MeshRenderer _plate;
        TextMeshPro _title;
        readonly PodView[] _cells = new PodView[PerPage];
        readonly CreatureInstance[] _shown = new CreatureInstance[PerPage];
        Transform[] _arrows;
        Mesh _quad;
        MaterialPropertyBlock _block;
        float _shownAmount, _target, _yaw, _tilt;
        int _page, _gazed = -1, _gazedEmpty = -1, _gazedArrow = -1, _selected = -1;
        readonly Hand[] _hands = new Hand[2];
        readonly bool[] _wasPinching = { true, true };
        readonly bool[] _wasPoking = new bool[2];

        int Pages => Mathf.Max(1, (party.storage.Count + PerPage - 1) / PerPage);

        void Start()
        {
            if (island == null || party == null || pods == null || podPrefab == null || hudMaterial == null)
            {
                Debug.LogError("[StorageShelf] Needs the island, party, pod rack, pod prefab and HUD material (Set Up MR_Main (7 progression)).", this);
                enabled = false;
                return;
            }
            _quad = BattleHud.VerticalQuad();
            _block = new MaterialPropertyBlock();
            Build();
            _root.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_quad != null) Destroy(_quad);
            if (pods != null && pods.PickHandler == OnRimPicked) pods.PickHandler = null;
        }

        void Build()
        {
            _root = new GameObject("Storage Shelf").transform;
            _root.SetParent(island.transform, false);
            var plateGo = new GameObject("Plate");
            plateGo.transform.SetParent(_root, false);
            plateGo.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            plateGo.transform.localScale = new Vector3(Columns * spacing + 0.02f, Rows * rowGap + 0.03f, 1f);
            plateGo.AddComponent<MeshFilter>().sharedMesh = _quad;
            _plate = plateGo.AddComponent<MeshRenderer>();
            _plate.sharedMaterial = hudMaterial;
            _plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Paint(_plate, new Color(0.08f, 0.1f, 0.09f, 0.7f), new Color(0.79f, 0.6f, 0.26f, 0.9f), plateGo.transform.localScale);

            _title = Text(_root, 0.1f);
            _title.transform.localPosition = new Vector3(0f, Rows * rowGap * 0.5f + 0.025f, 0f);

            _arrows = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var a = new GameObject(i == 0 ? "Prev" : "Next").transform;
                a.SetParent(_root, false);
                a.localPosition = new Vector3((i == 0 ? -1f : 1f) * (Columns * spacing * 0.5f + 0.025f), 0f, 0f);
                var t = Text(a, 0.2f);
                t.text = i == 0 ? "<" : ">";
                _arrows[i] = a;
            }
        }

        static TextMeshPro Text(Transform parent, float size)
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

        void Paint(MeshRenderer r, Color back, Color border, Vector3 scale)
        {
            _block.Clear();
            _block.SetColor(BackColorId, back);
            _block.SetColor(BorderColorId, border);
            _block.SetFloat(FillId, 0f);
            _block.SetFloat(AspectId, scale.x / scale.y);
            _block.SetFloat(RadiusId, 0.15f);
            _block.SetFloat(BorderId, 0.03f);
            r.SetPropertyBlock(_block);
        }

        /// <summary>The explorer reached a shrine: the shelf grows in.</summary>
        public void Show()
        {
            if (!enabled) return;
            _target = 1f;
            _page = Mathf.Clamp(_page, 0, Pages - 1);
            pods.PickHandler = OnRimPicked;
            Debug.Log($"[StorageShelf] Shown: {party.storage.Count} in storage, {party.members.Count} in the pods.");
        }

        /// <summary>The explorer walked off: the shelf shrinks away.</summary>
        public void Hide()
        {
            if (_target <= 0f) return;
            _target = 0f;
            Debug.Log("[StorageShelf] Hidden (walked off the shrine).");
            ClearSelection();
            if (pods.PickHandler == OnRimPicked) pods.PickHandler = null;
        }

        void ClearSelection()
        {
            _selected = -1;
            pods.SelectedSlot = -1;
        }

        Vector3 CellLocal(int cell)
        {
            int c = cell % Columns, r = cell / Columns;
            return new Vector3((c - (Columns - 1) * 0.5f) * spacing, (0.5f - r) * rowGap, 0f);
        }

        void Update()
        {
            if (_root == null) return;
            _shownAmount = Mathf.MoveTowards(_shownAmount, _target, Time.deltaTime / (_target > 0f ? 0.25f : 0.2f));
            bool on = _shownAmount > 0.001f;
            if (_root.gameObject.activeSelf != on) _root.gameObject.SetActive(on);
            pods.ExternalGaze = false;
            if (!on) return;
            Place();
            Sync();
            UpdateInput();
            _title.text = $"<b>Storage</b> <size=75%>{party.storage.Count}{(Pages > 1 ? $"  ·  page {_page + 1}/{Pages}" : "")}</size>";
            for (int i = 0; i < 2; i++)
            {
                bool usable = Pages > 1;
                _arrows[i].gameObject.SetActive(usable);
                _arrows[i].localScale = Vector3.one * (i == _gazedArrow ? 1.25f : 1f);
            }
        }

        /// <summary>Beside the near-right corner, turned (yaw) toward the head, scaled in by the show amount.</summary>
        void Place()
        {
            var b = island.LocalBounds;
            _root.localPosition = new Vector3(b.min.x - offset.x, island.map.capHeight + offset.y, b.max.z - offset.z);
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 away = island.transform.InverseTransformDirection(_root.position - cam.transform.position);
                float below = -away.y;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-8f)
                {
                    float yaw = Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg;
                    float tilt = Mathf.Clamp(Mathf.Atan2(below, away.magnitude) * Mathf.Rad2Deg, 0f, maxTilt);
                    float k = _shownAmount < 0.05f ? 1f : 1f - Mathf.Exp(-Time.deltaTime * 3f);
                    _yaw = Mathf.LerpAngle(_yaw, yaw, k);
                    _tilt = Mathf.Lerp(_tilt, tilt, k);
                }
            }
            _root.localRotation = Quaternion.Euler(_tilt, _yaw, 0f);
            float s = Mathf.SmoothStep(0f, 1f, _shownAmount);
            _root.localScale = Vector3.one * Mathf.Max(0.001f, s);
        }

        /// <summary>One pod per storage creature on this page.</summary>
        void Sync()
        {
            for (int i = 0; i < PerPage; i++)
            {
                int index = _page * PerPage + i;
                var c = index < party.storage.Count ? party.storage[index] : null;
                var p = _cells[i];
                if (c == null)
                {
                    // A held pod stays until it is let go.
                    if (p != null && !Held(p))
                    {
                        Destroy(p.gameObject);
                        _cells[i] = null;
                    }
                    _shown[i] = null;
                    continue;
                }
                if (p == null)
                {
                    p = _cells[i] = Instantiate(podPrefab, _root, false);
                    p.name = $"Storage Pod {i + 1}";
                    p.BaseScale = Vector3.one;
                    p.transform.localPosition = CellLocal(i);
                    var g = p.GetComponent<PodGrab>();
                    if (g != null)
                    {
                        int cell = i;
                        g.Released += (_, v) => OnReleased(cell);
                    }
                }
                if (_shown[i] != c || p.ShownSpecies != c.species)
                {
                    p.Show(c);
                    _shown[i] = c;
                }
                var grab = p.GetComponent<PodGrab>();
                if (grab != null) grab.CanGrab = _target > 0.5f;
                if (!Held(p))
                {
                    p.transform.localPosition = Vector3.Lerp(p.transform.localPosition, CellLocal(i), 1f - Mathf.Exp(-Time.deltaTime * 12f));
                    p.transform.localRotation = Quaternion.identity;
                    p.SetGrabDiameter(grabSize);
                }
                p.Glow = i == _selected ? 1f : 0f;
                p.Hover = i == _gazed ? 1f : 0f;
            }
        }

        static bool Held(PodView p)
        {
            var g = p != null ? p.GetComponent<PodGrab>() : null;
            return g != null && g.IsHeld;
        }

        void OnReleased(int cell)
        {
            var p = _cells[cell];
            if (p == null) return;
            float max = dropDistance * island.transform.lossyScale.x;
            int slot = pods.NearestSocket(p.transform.position, max);
            int index = _page * PerPage + cell;
            if (slot < 0)
            {
                Debug.Log("[StorageShelf] Storage pod dropped away from the rim: back to the shelf.");
                return;
            }
            Swap(index, slot, "drop");
        }

        void Swap(int storageIndex, int slot, string how)
        {
            var incoming = storageIndex < party.storage.Count ? party.storage[storageIndex] : null;
            var outgoing = party.At(slot);
            if (!party.SwapWithStorage(storageIndex, slot))
            {
                Debug.Log($"[StorageShelf] Swap refused (storage {storageIndex}, pod {slot + 1}).");
                return;
            }
            Debug.Log($"[StorageShelf] {how}: {(incoming != null ? incoming.Name : "?")} into pod {slot + 1}" +
                      $"{(outgoing != null ? $", {outgoing.Name} to storage" : " (new pod)")}.");
            if (fx != null) fx.Burst(pods.HomeWorld(slot), 14, new Color(1f, 0.88f, 0.54f), 0.03f * island.transform.lossyScale.x, 0.45f, 0.003f * island.transform.lossyScale.x);
            ClearSelection();
            // The cell's pod now shows whoever went to storage (or empties); re-sync it from scratch.
            for (int i = 0; i < PerPage; i++) _shown[i] = null;
        }

        /// <summary>Look + pinch on a rim pod (or the empty socket) while the shelf is up.</summary>
        bool OnRimPicked(int slot)
        {
            if (_target <= 0.5f) return false;
            if (_selected >= 0)
            {
                Swap(_page * PerPage + _selected, slot, "look + pinch");
                return true;
            }
            if (slot >= party.members.Count) return true; // empty socket with nothing picked on the shelf
            pods.SelectedSlot = pods.SelectedSlot == slot ? -1 : slot;
            Debug.Log($"[StorageShelf] Pod {slot + 1} {(pods.SelectedSlot >= 0 ? "picked" : "unpicked")}: now pick a storage pod (swap) or an empty cell (store it).");
            return true;
        }

        void UpdateInput()
        {
            _gazed = _gazedEmpty = _gazedArrow = -1;
            if (_target > 0.5f && HeadGaze.TryGetRay(out var ray))
            {
                float r = podPrefab.radius * _root.lossyScale.x * gazeRadius, best = float.MaxValue;
                for (int i = 0; i < PerPage; i++)
                {
                    Vector3 at = _root.TransformPoint(CellLocal(i)), to = at - ray.origin;
                    float along = Vector3.Dot(to, ray.direction);
                    if (along <= 0f) continue;
                    float off = (to - ray.direction * along).magnitude;
                    if (off >= r || off >= best) continue;
                    best = off;
                    if (_cells[i] != null) { _gazed = i; _gazedEmpty = -1; }
                    else { _gazedEmpty = i; _gazed = -1; }
                }
                if (Pages > 1)
                    for (int i = 0; i < 2; i++)
                    {
                        Vector3 to = _arrows[i].position - ray.origin;
                        float along = Vector3.Dot(to, ray.direction);
                        if (along > 0f && (to - ray.direction * along).magnitude < r) _gazedArrow = i;
                    }
            }
            pods.ExternalGaze = _gazed >= 0 || _gazedEmpty >= 0 || _gazedArrow >= 0;

            for (int h = 0; h < 2; h++)
            {
                bool left = h == 0;
                var hand = HandLookup.Refresh(ref _hands[h], left ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);

                // Poke an arrow.
                bool poking = false;
                if (tracked && Pages > 1)
                    for (int i = 0; i < 2; i++)
                        if ((_arrows[i].position - tip).sqrMagnitude < Sq(0.02f * _root.lossyScale.x))
                        {
                            poking = true;
                            if (!_wasPoking[h]) Turn(i == 0 ? -1 : 1, "poke");
                        }
                _wasPoking[h] = poking;

                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = grabGuard != null && grabGuard.IsHandBusy(left);
                if (pinching && !_wasPinching[h] && !busy) Pinch();
                _wasPinching[h] = pinching;
            }
        }

        void Pinch()
        {
            if (_gazedArrow >= 0)
            {
                Turn(_gazedArrow == 0 ? -1 : 1, "look + pinch");
                return;
            }
            if (_gazed >= 0)
            {
                if (pods.SelectedSlot >= 0) Swap(_page * PerPage + _gazed, pods.SelectedSlot, "look + pinch");
                else
                {
                    _selected = _selected == _gazed ? -1 : _gazed;
                    Debug.Log($"[StorageShelf] Storage pod {_gazed + 1} {(_selected >= 0 ? "picked" : "unpicked")}: now pick a rim pod or the empty socket.");
                }
                return;
            }
            if (_gazedEmpty >= 0 && pods.SelectedSlot >= 0)
            {
                int slot = pods.SelectedSlot;
                var c = party.At(slot);
                if (party.Deposit(slot)) Debug.Log($"[StorageShelf] {(c != null ? c.Name : "?")} from pod {slot + 1} to storage.");
                else Debug.Log("[StorageShelf] Can't store the last creature in the pods.");
                ClearSelection();
                for (int i = 0; i < PerPage; i++) _shown[i] = null;
            }
        }

        void Turn(int step, string how)
        {
            _page = (_page + step + Pages) % Pages;
            _selected = -1;
            Debug.Log($"[StorageShelf] Page {_page + 1}/{Pages} ({how}).");
        }

        static float Sq(float x) => x * x;
    }
}
