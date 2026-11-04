using Diorama.Audio;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: a Journal tablet in the Hearth Lodge: poke it (or look at it and pinch) and the Isling Journal opens (the
    /// explorer waits while it is open). Made at runtime by the lodge; shown only inside. Step 14i (Mugesh: the book in the
    /// middle of the room was in the way): a small tablet on the near rim, just left of the pods.
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
            // Step 14i: on the near rim, left of the pods (the player's left is island +X), one pod step past the last one.
            // Island +Z is the player's side; the slab's face is its -Z: turned round, then its top leans back a little.
            transform.localPosition = RimSpot();
            transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(15f, 0f, 0f);
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

        /// <summary>Step 14j: the clear gap between the tablet and the socket band (island-local m; ~1 cm at scale 0.6).</summary>
        const float RimGap = 0.016f;
        bool _spotLogged;

        /// <summary>
        /// Island-local: left of the pods (seen from the player), clear of the whole socket band (the six sockets and a
        /// grown pod's reach, <see cref="PodRack.SocketBandLocal"/>) by <see cref="RimGap"/>; at the pods' height. Step 14j
        /// (Mugesh: it touched the first pod): was one pod step past the leftmost pod's centre.
        /// </summary>
        Vector3 RimSpot()
        {
            var pods = _lodge.pods;
            if (pods == null) return _lodge.RoomCenterLocal;
            var home = pods.HomeLocal(0);
            if (!pods.SocketBandLocal(out float minX, out float maxX, out _)) return home;
            float hw = Size.x / 0.6f * 0.5f;
            float x = maxX + RimGap + hw;
            bool right = x + hw > island.FootprintHalfSize.x;
            if (right) x = minX - RimGap - hw;
            if (!_spotLogged)
            {
                _spotLogged = true;
                Debug.Log($"[LodgeRoom] Journal tablet {(right ? "right" : "left")} of the pods: x {x:0.000} (socket band {minX:0.000}..{maxX:0.000}, " +
                          $"gap {RimGap * island.transform.lossyScale.x * 100f:0.0} cm, island half width {island.FootprintHalfSize.x:0.000}).");
            }
            return new Vector3(x, home.y, home.z);
        }

        /// <summary>Step 14j: the tablet's rect along the rim (island-local x min / max), for the layout check.</summary>
        public Vector2 RimSpan()
        {
            float hw = Size.x / 0.6f * 0.5f, x = RimSpot().x;
            return new Vector2(x - hw, x + hw);
        }

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
