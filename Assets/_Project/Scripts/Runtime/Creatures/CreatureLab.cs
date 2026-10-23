using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Creature Lab (Scenes/CreatureLab): shows one baked species at a time on a turntable.
    /// Keys in Play mode: Space / Right = next, Left = previous, T = turntable on/off, H = hop, F = faint / revive,
    /// 1 / 2 / 3 = front / side / three-quarter view. The Inspector has "Rebake & reload".
    /// </summary>
    public class CreatureLab : MonoBehaviour
    {
        public List<CreatureSpecies> species = new List<CreatureSpecies>();
        public int index;
        public Transform turntable;
        [Tooltip("Degrees per second.")]
        public float turnSpeed = 25f;
        public bool turning = true;
        [Tooltip("Creatures are tiny (a few cm); scale them up for the lab camera.")]
        public float displayScale = 8f;

        CreatureView _current;

        public CreatureView Current => _current;
        public CreatureSpecies CurrentSpecies => species.Count > 0 ? species[Mathf.Clamp(index, 0, species.Count - 1)] : null;

        void Start() => Show(index);

        /// <summary>Re-instantiates the current species from its (re)baked prefab.</summary>
        public void Reload() => Show(index);

        public void Show(int i)
        {
            if (species.Count == 0) return;
            index = (i % species.Count + species.Count) % species.Count;
            if (_current != null) Destroy(_current.gameObject);
            _current = null;
            var s = species[index];
            if (s == null || s.Prefab == null)
            {
                Debug.LogError($"[CreatureLab] {(s != null ? s.name : "empty slot")} has no baked prefab. Tools > Pocket Isle > Creatures > Bake All.");
                return;
            }
            var go = Instantiate(s.Prefab, turntable != null ? turntable : transform, false);
            go.transform.localScale = Vector3.one * displayScale;
            _current = go.GetComponent<CreatureView>();
            Debug.Log($"[CreatureLab] {s.displayName} ({s.element})");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Show(index + 1);
                if (kb.leftArrowKey.wasPressedThisFrame) Show(index - 1);
                if (kb.tKey.wasPressedThisFrame) turning = !turning;
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
        }

        void SetView(float yaw)
        {
            turning = false;
            turntable.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
