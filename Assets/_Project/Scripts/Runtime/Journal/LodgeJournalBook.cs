using Diorama.Audio;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: a stone book leaning on the front of the Hearth Lodge's pedestal: poke it (or look at it and pinch) and
    /// the Isling Journal opens (the explorer waits while it is open). Made at runtime by the lodge; shown only inside.
    /// </summary>
    public class LodgeJournalBook : MonoBehaviour
    {
        LodgeRoom _lodge;
        TabletKit.View _slab;
        TextMeshPro _label;
        float _cooldown, _hover;
        readonly bool[] _wasPinching = { true, true };
        bool _wasLocked;

        /// <summary>World metres of the book (at island scale 0.6; it grows with the island).</summary>
        static readonly Vector2 Size = new Vector2(0.05f, 0.034f);

        public static LodgeJournalBook Ensure(LodgeRoom lodge)
        {
            if (lodge == null || lodge.island == null) return null;
            var existing = lodge.island.GetComponentInChildren<LodgeJournalBook>(true);
            if (existing != null) return existing;
            var go = new GameObject("Lodge Journal Book");
            go.layer = lodge.island.gameObject.layer;
            go.transform.SetParent(lodge.island.transform, false);
            var b = go.AddComponent<LodgeJournalBook>();
            b._lodge = lodge;
            return b;
        }

        void Build()
        {
            var menu = FindFirstObjectByType<GameMenu>();
            var mat = menu != null && menu.runes != null ? menu.runes.tabletMaterial : null;
            if (mat == null) return;
            _slab = TabletKit.Make(transform, mat, Size, TabletKit.GoldInlay);
            var go = new GameObject("Label");
            go.layer = gameObject.layer;
            _label = go.AddComponent<TextMeshPro>();
            _label.text = "<b>Journal</b>";
            _label.fontSize = 0.075f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.rectTransform.sizeDelta = new Vector2(Size.x, Size.y);
            go.transform.SetParent(_slab.transform, false);
            TabletKit.StyleLabel(_label);
        }

        bool Shown => _lodge != null && _lodge.Active && !JournalBoard.IsOpen && !GameMenu.IsOpen;

        void LateUpdate()
        {
            bool shown = Shown;
            if (_slab == null && shown) Build();
            if (_slab != null && _slab.renderer.gameObject.activeSelf != shown) _slab.renderer.gameObject.SetActive(shown);
            if (!shown || _slab == null) return;
            // Leaning back on the pedestal's front edge, facing the player's side of the island.
            // Island +Z is the player's side; the slab's face is its -Z: turned round, then its top leans away.
            transform.localPosition = _lodge.PedestalTopLocal + new Vector3(0f, -0.1f * _lodge.TileLocal, 0.55f * _lodge.TileLocal);
            transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(35f, 0f, 0f);
            // World size = Size at the default island scale (0.6), growing with the island.
            transform.localScale = Vector3.one / 0.6f;
            _slab.Paint(_hover, 0f, 0f, 0.5f + 0.5f * Mathf.Sin(Time.time * 2.5f));
            Input();
        }

        void Input()
        {
            if (Time.time < _cooldown) return;
            Vector3 c = _slab.transform.position;
            float r = Size.x * 0.6f * island.transform.lossyScale.x / 0.6f;
            bool gazed = HeadGaze.TryGetRay(out var ray) && Vector3.Cross(ray.direction, c - ray.origin).magnitude < r * 1.2f && Vector3.Dot(ray.direction, c - ray.origin) > 0f;
            _hover = Mathf.MoveTowards(_hover, gazed ? 1f : 0f, Time.deltaTime * 6f);
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                if (tracked && (tip - c).magnitude < r) Open("poke");
                else if (pinching && !_wasPinching[h] && gazed) Open("look + pinch");
                _wasPinching[h] = pinching;
            }
        }

        IslandBuilder island => _lodge.island;

        /// <summary>Opens the Journal (also the bridge).</summary>
        public void Open(string how)
        {
            var menu = FindFirstObjectByType<GameMenu>();
            var j = menu != null ? JournalBoard.Ensure(menu.runes) : null;
            if (j == null || JournalBoard.IsOpen) return;
            _cooldown = Time.time + 1f;
            GameAudio.At(AudioIds.UiPebble, _slab.transform.position);
            var explorer = _lodge.explorer;
            if (explorer != null)
            {
                _wasLocked = explorer.Locked;
                explorer.Locked = true;
            }
            Debug.Log($"[LodgeRoom] The Journal book ({how}).");
            j.Open(() =>
            {
                if (explorer != null && !_wasLocked) explorer.Locked = false;
                _cooldown = Time.time + 1f;
            });
        }
    }
}
