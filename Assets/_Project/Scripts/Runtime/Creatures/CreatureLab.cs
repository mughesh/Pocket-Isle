using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Creature Lab (Scenes/CreatureLab): shows one baked species or prop at a time on a turntable (species first,
    /// then props). Keys in Play mode: Space / Right = next, Left = previous, T = turntable on/off, H = hop,
    /// F = faint / revive, G = realm restored / unrestored, 1 / 2 / 3 = front / side / three-quarter view.
    /// The Inspector has "Rebake & reload".
    /// </summary>
    public class CreatureLab : MonoBehaviour
    {
        public List<CreatureSpecies> species = new List<CreatureSpecies>();
        public List<PropRecipe> props = new List<PropRecipe>();
        [Tooltip("Diorama/Prop material for showing props.")]
        public Material propMaterial;
        public int index;
        public Transform turntable;
        [Tooltip("Degrees per second.")]
        public float turnSpeed = 25f;
        public bool turning = true;
        [Tooltip("Creatures are tiny (a few cm); scale them up for the lab camera.")]
        public float displayScale = 8f;
        [Tooltip("Props are shown this tall (metres), whatever their size.")]
        public float propDisplayHeight = 0.42f;

        CreatureView _current;
        GameObject _shown;
        float _restoreFrom = 1f, _restoreTo = 1f, _restoreT = 1f;

        int Count => species.Count + props.Count;

        public CreatureView Current => _current;
        public CreatureSpecies CurrentSpecies => index < species.Count && species.Count > 0 ? species[Mathf.Clamp(index, 0, species.Count - 1)] : null;
        public PropRecipe CurrentProp => index >= species.Count && index - species.Count < props.Count ? props[index - species.Count] : null;

        void Start()
        {
            RealmState.Restored = 1f;
            Show(index);
        }

        /// <summary>Re-instantiates the current species from its (re)baked prefab.</summary>
        public void Reload() => Show(index);

        public void Show(int i)
        {
            if (Count == 0) return;
            index = (i % Count + Count) % Count;
            if (_shown != null) Destroy(_shown);
            _shown = null;
            _current = null;
            if (index >= species.Count)
            {
                ShowProp(props[index - species.Count]);
                return;
            }
            var s = species[index];
            if (s == null || s.Prefab == null)
            {
                Debug.LogError($"[CreatureLab] {(s != null ? s.name : "empty slot")} has no baked prefab. Tools > Pocket Isle > Creatures > Bake All.");
                return;
            }
            var go = Instantiate(s.Prefab, turntable != null ? turntable : transform, false);
            go.transform.localScale = Vector3.one * displayScale;
            _shown = go;
            _current = go.GetComponent<CreatureView>();
            Debug.Log($"[CreatureLab] {s.displayName} ({s.element})");
        }

        void ShowProp(PropRecipe p)
        {
            if (p == null || p.bakedMesh == null)
            {
                Debug.LogError($"[CreatureLab] {(p != null ? p.name : "empty slot")} is not baked. Tools > Pocket Isle > Props > Bake All.");
                return;
            }
            if (propMaterial == null)
            {
                Debug.LogError("[CreatureLab] No Prop Material assigned (Diorama/Prop).");
                return;
            }
            var go = new GameObject($"Prop_{p.name}");
            go.transform.SetParent(turntable != null ? turntable : transform, false);
            float h = Mathf.Max(0.05f, p.bakedMesh.bounds.max.y);
            go.transform.localScale = Vector3.one * (propDisplayHeight / h);
            go.AddComponent<MeshFilter>().sharedMesh = p.bakedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = propMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            p.ApplyTo(block);
            r.SetPropertyBlock(block);
            _shown = go;
            Debug.Log($"[CreatureLab] Prop {p.name} ({p.bakedMesh.triangles.Length / 3} tris, footprint {p.Footprint.x}x{p.Footprint.y}{(p.blocksWalking ? ", blocks" : "")})");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Show(index + 1);
                if (kb.leftArrowKey.wasPressedThisFrame) Show(index - 1);
                if (kb.tKey.wasPressedThisFrame) turning = !turning;
                if (kb.gKey.wasPressedThisFrame)
                {
                    _restoreFrom = RealmState.Restored;
                    _restoreTo = _restoreTo > 0.5f ? 0f : 1f;
                    _restoreT = 0f;
                }
                if (kb.hKey.wasPressedThisFrame && _current != null) _current.Hop();
                if (kb.fKey.wasPressedThisFrame && _current != null)
                {
                    if (_current.IsFainted) _current.Revive();
                    else _current.Faint();
                }
                if (turntable != null)
                {
                    if (kb.digit1Key.wasPressedThisFrame) SetView(0f);
                    if (kb.digit2Key.wasPressedThisFrame) SetView(90f);
                    if (kb.digit3Key.wasPressedThisFrame) SetView(35f);
                }
            }
            if (turning && turntable != null) turntable.Rotate(0f, turnSpeed * Time.deltaTime, 0f, Space.Self);
            if (_restoreT < 1f)
            {
                _restoreT = Mathf.Min(1f, _restoreT + Time.deltaTime / 2f);
                RealmState.Restored = Mathf.Lerp(_restoreFrom, _restoreTo, Mathf.SmoothStep(0f, 1f, _restoreT));
            }
        }

        void SetView(float yaw)
        {
            turning = false;
            turntable.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
